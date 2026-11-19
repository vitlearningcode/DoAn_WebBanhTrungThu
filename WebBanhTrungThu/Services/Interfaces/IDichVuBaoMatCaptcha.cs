using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Services.CoSo;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuBaoMatCaptcha : IDichVuCoSo
    {
        string TaoAnhSvgCaptcha(out string maCaptcha);
        string TaoAnhSvgCaptchaTheoMa(string maCaptcha);
        bool KiemTraMaCaptchaHopLe(ISession phienLamViec, string? maKhachNhap);
        string CapMoiCaptchaVaoSession(ISession phienLamViec);
    }
}
