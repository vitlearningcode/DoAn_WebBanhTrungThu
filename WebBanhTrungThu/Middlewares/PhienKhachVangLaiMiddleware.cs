using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.Middlewares
{
    public class PhienKhachVangLaiMiddleware
    {
        private readonly RequestDelegate _next;
        public const string TenCookieKVL = "NguyetCat_PhienKVL";

        public PhienKhachVangLaiMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, CSDLBanhTrungThuContext db)
        {
            // Bỏ qua các request tải file tĩnh (css, js, hình ảnh) để tối ưu tốc độ
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images") || path.StartsWith("/kho-anh-may") || path.StartsWith("/lib"))
            {
                await _next(context);
                return;
            }

            // Nếu người dùng CHƯA đăng nhập thì kiểm tra/khởi tạo Phiên Khách Vãng Lai
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                string? maPhien = context.Request.Cookies[TenCookieKVL];
                bool canTaoMoi = string.IsNullOrWhiteSpace(maPhien);

                if (!canTaoMoi)
                {
                    // Kiểm tra xem mã trong Cookie có tồn tại trong CSDL không
                    bool tonTaiTrongDB = await db.PhienKhachVangLais.AnyAsync(p => p.MaPhienKvl == maPhien && !p.DaGopGioHang);
                    if (!tonTaiTrongDB) canTaoMoi = true;
                }

                if (canTaoMoi)
                {
                    maPhien = "KVL-" + Guid.NewGuid().ToString("N").ToUpper()[..20];

                    var phienMoi = new PhienKhachVangLai
                    {
                        MaPhienKvl = maPhien,
                        DiaChiIp = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        ThongTinTrinhDuyet = context.Request.Headers["User-Agent"].ToString()[..Math.Min(200, context.Request.Headers["User-Agent"].ToString().Length)],
                        DaGopGioHang = false,
                        NgayTao = DateTime.Now,
                        NgayHetHan = DateTime.Now.AddDays(14)
                    };

                    db.PhienKhachVangLais.Add(phienMoi);
                    await db.SaveChangesAsync();

                    // Ghi vào Cookie HTTP-Only sống trong 14 ngày
                    context.Response.Cookies.Append(TenCookieKVL, maPhien, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true,
                        Expires = DateTimeOffset.UtcNow.AddDays(14)
                    });
                }

                // Lưu vào HttpContext.Items để Controller dùng ngay trong cùng Request
                context.Items["MaPhienKVL"] = maPhien;
            }

            await _next(context);
        }
    }
}