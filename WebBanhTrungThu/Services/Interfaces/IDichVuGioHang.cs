using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuGioHang : IDichVuCoSo
    {
        Task<Models.Entities.GioHang> LayHoacTaoGioHangAsync(HttpContext httpContext);
        Task<object> ThemHopTuChonAsync(HttpContext httpContext, LuuHopTuChonRequest? req);
        Task<object> ThemBanhHoacComboAsync(HttpContext httpContext, ThemMonLeRequest? req);
        Task<object> CapNhatSoLuongAsync(HttpContext httpContext, CapNhatSoLuongGioRequest? req);
        Task<object> XoaMonAsync(HttpContext httpContext, CapNhatSoLuongGioRequest? req);
        Task<object> ApDungVoucherAsync(HttpContext httpContext, ApDungVoucherRequest? req, string? codeKm);
        Task GopGioHangAsync(string? maPhienKVL, int maND);
        Task<object> DatLaiDonHangAsync(HttpContext httpContext, int maDonHang);
        Task<object> LayThongTinGioHangJsonAsync(HttpContext httpContext);
        Task<object> KiemTraTonKhoThucTeAsync(HttpContext httpContext);
        Task<int> DonDepPhienKhachVangLaiQuaHanAsync(CancellationToken cancellationToken = default);
    }
}
