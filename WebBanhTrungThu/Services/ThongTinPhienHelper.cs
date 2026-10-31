using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebBanhTrungThu.Middlewares;

namespace WebBanhTrungThu.Services
{
    public static class ThongTinPhienHelper
    {
        // Lấy MaND nếu khách đã đăng nhập (trả về null nếu là Guest)
        public static int? LayMaNguoiDung(HttpContext context)
        {
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int maND) ? maND : null;
        }

        // Lấy MaPhienKVL của khách vãng lai từ HttpContext.Items hoặc Cookie
        public static string? LayMaPhienKVL(HttpContext context)
        {
            if (context.Items.TryGetValue("MaPhienKVL", out var maItems) && maItems != null)
            {
                return maItems.ToString();
            }
            return context.Request.Cookies[PhienKhachVangLaiMiddleware.TenCookieKVL];
        }

        // Băm mật khẩu SHA256 gọn nhẹ, đồng bộ với SQL Server Seed Data
        public static string BamMatKhauSHA256(string matKhauThuan)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(matKhauThuan + "_NguyetCatSalt2026"));
            return Convert.ToHexString(bytes);
        }
    }
}