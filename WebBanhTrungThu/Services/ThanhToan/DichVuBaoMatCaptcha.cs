using System.Text;
using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Services.ThanhToan
{
    /// <summary>
    /// Dịch vụ chuyên trách bảo mật Captcha hình ảnh SVG chống bot và đối chiếu xác thực mã Captcha với Session.
    /// </summary>
    public class DichVuBaoMatCaptcha : DichVuCoSo, IDichVuBaoMatCaptcha
    {
        public DichVuBaoMatCaptcha(CSDLBanhTrungThuContext context) : base(context)
        {
        }

        /// <summary>
        /// Sinh chuỗi 5 ký tự ngẫu nhiên (loại bỏ 0, O, 1, I) và trả về chuỗi SVG thuần chuẩn XML (image/svg+xml).
        /// </summary>
        public string TaoChuoiSvgCaptchaThuan(out string maCaptcha)
        {
            const string kyTuChoPhep = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] mangKyTu = new char[5];
            for (int i = 0; i < mangKyTu.Length; i++)
            {
                mangKyTu[i] = kyTuChoPhep[Random.Shared.Next(kyTuChoPhep.Length)];
            }
            maCaptcha = new string(mangKyTu);
            return TaoChuoiSvgCaptchaThuanTheoMa(maCaptcha);
        }

        /// <summary>
        /// Vẽ chuỗi SVG Captcha thuần từ mã truyền vào (nền kem #FAF6EE, chữ đỏ đô tương phản cao).
        /// </summary>
        public string TaoChuoiSvgCaptchaThuanTheoMa(string maCaptcha)
        {
            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='135' height='44' viewBox='0 0 135 44'>");
            sb.Append("<rect width='135' height='44' rx='6' fill='#FAF6EE' stroke='#E6D5B8' stroke-width='1'/>");

            string[] bangMauNhieu = { "#D6C4A8", "#C89B3C", "#A84A4D" };

            for (int i = 0; i < 4; i++)
            {
                int x1 = Random.Shared.Next(3, 20);
                int y1 = Random.Shared.Next(6, 38);
                int cx1 = Random.Shared.Next(30, 66);
                int cy1 = Random.Shared.Next(4, 40);
                int cx2 = Random.Shared.Next(70, 106);
                int cy2 = Random.Shared.Next(4, 40);
                int x2 = Random.Shared.Next(114, 132);
                int y2 = Random.Shared.Next(6, 38);
                string mauDuong = bangMauNhieu[Random.Shared.Next(bangMauNhieu.Length)];
                sb.Append($"<path d='M{x1} {y1} C {cx1} {cy1}, {cx2} {cy2}, {x2} {y2}' stroke='{mauDuong}' stroke-width='1.15' fill='none' opacity='0.45'/>");
            }

            for (int i = 0; i < 16; i++)
            {
                int cx = Random.Shared.Next(5, 130);
                int cy = Random.Shared.Next(5, 39);
                int r = Random.Shared.Next(1, 2);
                string mauCham = bangMauNhieu[Random.Shared.Next(bangMauNhieu.Length)];
                sb.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='{mauCham}' opacity='0.4'/>");
            }

            string[] bangMauChu = { "#7A1215", "#5E0B0E", "#8B1A1E", "#4A0709" };
            for (int i = 0; i < maCaptcha.Length; i++)
            {
                int x = 19 + (i * 24) + Random.Shared.Next(-2, 3);
                int y = 29 + Random.Shared.Next(-3, 4);
                int gocXoay = Random.Shared.Next(-14, 15);
                string mauChu = bangMauChu[i % bangMauChu.Length];
                sb.Append($"<text x='{x}' y='{y}' transform='rotate({gocXoay} {x} {y})' font-family='Consolas, Courier New, monospace' font-size='22' font-weight='800' fill='{mauChu}' text-anchor='middle'>{maCaptcha[i]}</text>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>
        /// Sinh chuỗi 5 ký tự ngẫu nhiên (loại bỏ 0, O, 1, I) và tạo chuỗi hình ảnh SVG Captcha dạng data URI Base64.
        /// </summary>
        public string TaoAnhSvgCaptcha(out string maCaptcha)
        {
            string svgThuan = TaoChuoiSvgCaptchaThuan(out maCaptcha);
            return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svgThuan));
        }

        /// <summary>
        /// Vẽ ảnh SVG Captcha dạng Base64 từ chuỗi mã truyền vào.
        /// </summary>
        public string TaoAnhSvgCaptchaTheoMa(string maCaptcha)
        {
            string svgThuan = TaoChuoiSvgCaptchaThuanTheoMa(maCaptcha);
            return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svgThuan));
        }

        /// <summary>
        /// Kiểm tra mã Captcha khách nhập có trùng khớp 100% (không phân biệt hoa/thường) với mã lưu trong Session hay không.
        /// </summary>
        public bool KiemTraCaptcha(ISession phienLamViec, string? maKhachNhap)
        {
            string? maTrongSession = phienLamViec.GetString("CaptchaThanhToan")
                                  ?? phienLamViec.GetString("MaCaptchaThanhToan");
            string maNhapChuan = ChuanHoaChuoi(maKhachNhap);

            return !string.IsNullOrWhiteSpace(maNhapChuan)
                && !string.IsNullOrWhiteSpace(maTrongSession)
                && string.Equals(maNhapChuan, maTrongSession, StringComparison.OrdinalIgnoreCase);
        }

        public bool KiemTraMaCaptchaHopLe(ISession phienLamViec, string? maKhachNhap)
            => KiemTraCaptcha(phienLamViec, maKhachNhap);

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
