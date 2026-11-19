using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuThongKeVaSoQuy : IDichVuCoSo
    {
        Task<AdminDashboardViewModel> XayDungDashboardAsync();
        Task<string> KhoiPhucDuLieuGocAsync();
        Task<(SoQuyAdminViewModel ViewModel, string LocPhuongThucHienTai)> XayDungSoQuyAsync(
            string? locPhuongThuc,
            string? locLoaiPhieu = null,
            string? tuKhoa = null);
        Task<(bool ThanhCong, string ThongBao)> LapPhieuThuChiThuCongAsync(
            string loaiPhieu,
            string hangMuc,
            decimal soTien,
            string phuongThuc,
            string doiTuong,
            string? maTraCuuDh,
            string? ghiChu);
        string CapNhatCauHinhQrMoMo(
            string soTaiKhoanMoMo,
            string tenChuTaiKhoan,
            string maNganHang,
            string maBinVietQr,
            string mauNoiDungCk);
    }
}
