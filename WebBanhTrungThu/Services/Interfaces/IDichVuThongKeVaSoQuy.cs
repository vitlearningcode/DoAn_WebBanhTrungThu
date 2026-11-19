using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuThongKeVaSoQuy : IDichVuCoSo
    {
        Task<AdminDashboardViewModel> XayDungDashboardAsync();
        Task<string> KhoiPhucDuLieuGocAsync();
        Task<(SoQuyAdminViewModel ViewModel, string LocPhuongThucHienTai)> XayDungSoQuyAsync(string? locPhuongThuc);
        string CapNhatCauHinhQrMoMo(
            string soTaiKhoanMoMo,
            string tenChuTaiKhoan,
            string maNganHang,
            string maBinVietQr,
            string mauNoiDungCk);
    }
}
