using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Services.CoSo;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuBaoMatCaptcha : IDichVuCoSo
    {
        string TaoChuoiSvgCaptchaThuan(out string maCaptcha);
        string TaoChuoiSvgCaptchaThuanTheoMa(string maCaptcha);
        string TaoAnhSvgCaptcha(out string maCaptcha);
        string TaoAnhSvgCaptchaTheoMa(string maCaptcha);
        bool KiemTraCaptcha(ISession phienLamViec, string? maKhachNhap);
        bool KiemTraMaCaptchaHopLe(ISession phienLamViec, string? maKhachNhap);
        string CapMoiCaptchaVaoSession(ISession phienLamViec);
    }
}
