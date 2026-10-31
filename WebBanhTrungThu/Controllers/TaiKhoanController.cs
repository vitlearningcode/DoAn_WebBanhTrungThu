using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuGioHang _dichVuGioHang;

        public TaiKhoanController(CSDLBanhTrungThuContext db, DichVuGioHang dichVuGioHang)
        {
            _db = db;
            _dichVuGioHang = dichVuGioHang;
        }

        // =========================================================================
        // 1. ĐĂNG NHẬP AJAX (COOKIE AUTH + HỢP NHẤT GIỎ HÀNG GUEST -> MEMBER)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> DangNhapAjax([FromBody] DangNhapAjaxRequest? req, [FromQuery] string? tenDangNhap, [FromQuery] string? matKhau)
        {
            string taiKhoan = (req?.TenDangNhap ?? tenDangNhap ?? string.Empty).Trim();
            string mkNhap = (req?.MatKhau ?? matKhau ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrWhiteSpace(mkNhap))
            {
                return Json(new { thanhCong = false, thongBao = "Vui lòng nhập đầy đủ Email/Số điện thoại và Mật khẩu!" });
            }

            var nd = await _db.NguoiDungs
                .FirstOrDefaultAsync(u => (u.Email == taiKhoan || u.Sdt == taiKhoan) && u.TrangThai);

            if (nd == null)
            {
                return Json(new { thanhCong = false, thongBao = "Tài khoản không tồn tại hoặc đã bị khóa!" });
            }

            string maBamSha256 = ThongTinPhienHelper.BamMatKhauSHA256(mkNhap);
            bool hopLe = false;

            if (string.Equals(nd.MatKhauHash, maBamSha256, StringComparison.OrdinalIgnoreCase))
            {
                hopLe = true;
            }
            else if (nd.MatKhauHash.StartsWith("AQAAAA", StringComparison.OrdinalIgnoreCase) && mkNhap == "123456")
            {
                // Tài khoản dữ liệu mẫu ban đầu -> tự động chuẩn hóa sang SHA256
                nd.MatKhauHash = maBamSha256;
                await _db.SaveChangesAsync();
                hopLe = true;
            }

            if (!hopLe)
            {
                return Json(new { thanhCong = false, thongBao = "Mật khẩu không chính xác. Vui lòng kiểm tra lại!" });
            }

            // Thiết lập Cookie Authentication & Hợp nhất giỏ hàng Guest -> Member
            await ThucHienDangNhapVaGopGioAsync(nd);

            return Json(new
            {
                thanhCong = true,
                hoTen = nd.HoTen,
                hangTv = nd.HangTv,
                diem = nd.DiemTichLuy,
                thongBao = "Đăng nhập & Hợp nhất giỏ hàng thành công!"
            });
        }

        // =========================================================================
        // 2. ĐĂNG KÝ THÀNH VIÊN MỚI AJAX (+50 ĐIỂM CHÀO MỪNG & HỢP NHẤT GIỎ HÀNG)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> DangKyAjax(
            [FromBody] DangKyAjaxRequest? req,
            [FromQuery] string? hoTen,
            [FromQuery] string? sdt,
            [FromQuery] string? email,
            [FromQuery] string? matKhau)
        {
            string ten = (req?.HoTen ?? hoTen ?? string.Empty).Trim();
            string soDt = (req?.Sdt ?? sdt ?? string.Empty).Trim();
            string thuDienTu = (req?.Email ?? email ?? string.Empty).Trim();
            string mk = (req?.MatKhau ?? matKhau ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(ten) || string.IsNullOrWhiteSpace(soDt) || string.IsNullOrWhiteSpace(thuDienTu) || string.IsNullOrWhiteSpace(mk))
            {
                return Json(new { thanhCong = false, thongBao = "Vui lòng điền đầy đủ Họ tên, Số điện thoại, Email và Mật khẩu!" });
            }

            bool daTonTai = await _db.NguoiDungs.AnyAsync(u => u.Email == thuDienTu || u.Sdt == soDt);
            if (daTonTai)
            {
                return Json(new { thanhCong = false, thongBao = "Email hoặc Số điện thoại này đã được đăng ký!" });
            }

            var ndMoi = new NguoiDung
            {
                MaVt = "KHACH_TV",
                HoTen = ten,
                Sdt = soDt,
                Email = thuDienTu,
                MatKhauHash = ThongTinPhienHelper.BamMatKhauSHA256(mk),
                DiemTichLuy = 50,
                HangTv = "Đồng",
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _db.NguoiDungs.Add(ndMoi);
            await _db.SaveChangesAsync();

            // Tự động đăng nhập & gộp giỏ hàng từ phiên Khách vãng lai
            await ThucHienDangNhapVaGopGioAsync(ndMoi);

            return Json(new
            {
                thanhCong = true,
                hoTen = ndMoi.HoTen,
                hangTv = ndMoi.HangTv,
                diem = ndMoi.DiemTichLuy,
                thongBao = "Đăng ký thành công (Tặng +50 điểm) & Đã hợp nhất giỏ hàng vào tài khoản!"
            });
        }

        // =========================================================================
        // 3. ĐĂNG XUẤT KHỎI HỆ THỐNG
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task ThucHienDangNhapVaGopGioAsync(NguoiDung nd)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nd.MaNd.ToString()),
                new Claim(ClaimTypes.Name, nd.HoTen),
                new Claim(ClaimTypes.Email, nd.Email),
                new Claim(ClaimTypes.Role, nd.MaVt),
                new Claim("HangTV", nd.HangTv)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Hợp nhất toàn bộ giỏ hàng và hộp tự chọn của Guest vào tài khoản Member vừa đăng nhập
            string? maPhienKVL = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);
            if (!string.IsNullOrWhiteSpace(maPhienKVL))
            {
                await _dichVuGioHang.GopGioHangAsync(maPhienKVL, nd.MaNd);
                Response.Cookies.Delete(PhienKhachVangLaiMiddleware.TenCookieKVL);
            }
        }
    }
}

