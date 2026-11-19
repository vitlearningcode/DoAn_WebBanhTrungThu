using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuQuanTriDonHang : IDichVuCoSo
    {
        Task<List<DonHang>> LayDanhSachDonHangLocAsync(
            string? tuKhoa,
            string? locTrangThai,
            string? locLoaiDon,
            string? locPhuongThuc,
            string? locThoiGian);
        Task<DonHangThanhCongViewModel?> XayDungChiTietDonHangQrAsync(int id, string scheme, string host, bool asNoTracking = false);
        Task<string?> XacNhanChuyenKhoanQrAsync(int maDh, string nguoiCapNhat);
        Task<string?> CapNhatTrangThaiDonHangAsync(
            int maDh,
            string? trangThaiDh,
            string? trangThaiTt,
            bool laAdminOverride,
            string? ghiChuDieuChinh,
            string nguoiCapNhat);
        Task<string?> HuyDonVaHoanTienAsync(
            int maDh,
            string? maNganHangHoan,
            string? soTaiKhoanHoan,
            string? tenChuTkHoan,
            string? lyDoHuy,
            string nguoiThucHien,
            bool? hoanNhapKho = null);
    }
}
