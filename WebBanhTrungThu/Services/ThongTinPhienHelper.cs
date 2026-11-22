using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.Services
{
    public static class ThongTinPhienHelper
    {
        private static readonly PasswordHasher<NguoiDung> _boBamMatKhau = new();

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

        /// <summary>
        /// Băm mật khẩu chuẩn ASP.NET Core Identity PBKDF2 với salt ngẫu nhiên cho từng người dùng.
        /// </summary>
        public static string BamMatKhauPbkdf2(NguoiDung? nguoiDung, string matKhauThuan)
        {
            return _boBamMatKhau.HashPassword(nguoiDung ?? new NguoiDung(), matKhauThuan);
        }

        /// <summary>
        /// Xác thực mật khẩu theo chuẩn PBKDF2, trả về Failed an toàn nếu chuỗi lưu trữ là định dạng legacy không phải PBKDF2.
        /// </summary>
        public static PasswordVerificationResult XacThucMatKhauPbkdf2(
            NguoiDung? nguoiDung,
            string? matKhauHashLuuTru,
            string matKhauThuan)
        {
            if (string.IsNullOrWhiteSpace(matKhauHashLuuTru))
            {
                return PasswordVerificationResult.Failed;
            }

            try
            {
                return _boBamMatKhau.VerifyHashedPassword(
                    nguoiDung ?? new NguoiDung(),
                    matKhauHashLuuTru.Trim(),
                    matKhauThuan);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
        }

        // Băm mật khẩu SHA256 gọn nhẹ, dùng để đối chiếu tương thích ngược với SQL Server Seed Data cũ
        public static string BamMatKhauSHA256(string matKhauThuan)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(matKhauThuan + "_NguyetCatSalt2026"));
            return Convert.ToHexString(bytes);
        }
    }
}