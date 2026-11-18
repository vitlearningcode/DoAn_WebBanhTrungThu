using System.Text;
using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.CoSo;

namespace WebBanhTrungThu.Services.ThanhToan
{
    /// <summary>
    /// Dịch vụ chuyên trách bảo mật Captcha hình ảnh SVG chống bot và đối chiếu xác thực mã Captcha với Session.
    /// </summary>
    public class DichVuBaoMatCaptcha : DichVuCoSo
    {
        public DichVuBaoMatCaptcha(CSDLBanhTrungThuContext context) : base(context)
        {
        }

        /// <summary>
        /// Sinh chuỗi 5 ký tự ngẫu nhiên (loại bỏ 0, O, 1, I) và tạo chuỗi hình ảnh SVG Captcha có đường cong nhiễu + chữ xoay nghiêng chống bot.
        /// </summary>
        public string TaoAnhSvgCaptcha(out string maCaptcha)
        {
            const string kyTuChoPhep = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] mangKyTu = new char[5];
            for (int i = 0; i < mangKyTu.Length; i++)
            {
                mangKyTu[i] = kyTuChoPhep[Random.Shared.Next(kyTuChoPhep.Length)];
            }
            maCaptcha = new string(mangKyTu);
            return TaoAnhSvgCaptchaTheoMa(maCaptcha);
        }

        /// <summary>
        /// Vẽ ảnh SVG Captcha dạng Base64 từ chuỗi mã truyền vào.
        /// </summary>
        public string TaoAnhSvgCaptchaTheoMa(string maCaptcha)
        {
            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='135' height='44' viewBox='0 0 135 44'>");
            sb.Append("<rect width='135' height='44' rx='6' fill='#F9FAFB'/>");

            string[] bangMauNhieu = { "#9CA3AF", "#D1D5DB", "#E5E7EB", "#7A1215", "#B45309", "#6B7280" };

            // 5 đường cong nhiễu ngẫu nhiên (Bezier noise lines)
            for (int i = 0; i < 5; i++)
            {
                int x1 = Random.Shared.Next(2, 22);
                int y1 = Random.Shared.Next(4, 40);
                int cx1 = Random.Shared.Next(28, 68);
                int cy1 = Random.Shared.Next(2, 42);
                int cx2 = Random.Shared.Next(68, 108);
                int cy2 = Random.Shared.Next(2, 42);
                int x2 = Random.Shared.Next(112, 133);
                int y2 = Random.Shared.Next(4, 40);
                string mauDuong = bangMauNhieu[Random.Shared.Next(bangMauNhieu.Length)];
                sb.Append($"<path d='M{x1} {y1} C {cx1} {cy1}, {cx2} {cy2}, {x2} {y2}' stroke='{mauDuong}' stroke-width='1.35' fill='none' opacity='0.65'/>");
            }

            // 24 chấm nhiễu ngẫu nhiên (Noise dots)
            for (int i = 0; i < 24; i++)
            {
                int cx = Random.Shared.Next(4, 131);
                int cy = Random.Shared.Next(4, 40);
                int r = Random.Shared.Next(1, 3);
                string mauCham = bangMauNhieu[Random.Shared.Next(bangMauNhieu.Length)];
                sb.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='{mauCham}' opacity='0.55'/>");
            }

            // Vẽ từng ký tự trong 5 ký tự với góc xoay nghiêng (-18deg .. +18deg) và lệch trục Y khác nhau
            string[] bangMauChu = { "#111827", "#7A1215", "#1F2937", "#374151", "#4A0709" };
            for (int i = 0; i < maCaptcha.Length; i++)
            {
                int x = 19 + (i * 24) + Random.Shared.Next(-2, 3);
                int y = 28 + Random.Shared.Next(-5, 6);
                int gocXoay = Random.Shared.Next(-18, 19);
                string mauChu = bangMauChu[i % bangMauChu.Length];
                sb.Append($"<text x='{x}' y='{y}' transform='rotate({gocXoay} {x} {y})' font-family='Consolas, Courier New, monospace' font-size='21' font-weight='800' fill='{mauChu}' text-anchor='middle'>{maCaptcha[i]}</text>");
            }

            sb.Append("</svg>");
            return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        /// <summary>
        /// Kiểm tra mã Captcha khách nhập có trùng khớp 100% (không phân biệt hoa/thường) với mã lưu trong Session hay không.
        /// </summary>
        public bool KiemTraMaCaptchaHopLe(ISession phienLamViec, string? maKhachNhap)
        {
            string? maTrongSession = phienLamViec.GetString("CaptchaThanhToan")
                                  ?? phienLamViec.GetString("MaCaptchaThanhToan");
            string maNhapChuan = ChuanHoaChuoi(maKhachNhap);

            return !string.IsNullOrWhiteSpace(maNhapChuan)
                && !string.IsNullOrWhiteSpace(maTrongSession)
                && string.Equals(maNhapChuan, maTrongSession, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tạo mới mã Captcha và lưu đồng bộ vào Session.
        /// </summary>
        public string CapMoiCaptchaVaoSession(ISession phienLamViec)
        {
            string anhSvg = TaoAnhSvgCaptcha(out string maMoi);
            phienLamViec.SetString("CaptchaThanhToan", maMoi);
            phienLamViec.SetString("MaCaptchaThanhToan", maMoi);
            phienLamViec.SetString("DaXacThucCaptcha", "false");
            return anhSvg;
        }
    }
}
