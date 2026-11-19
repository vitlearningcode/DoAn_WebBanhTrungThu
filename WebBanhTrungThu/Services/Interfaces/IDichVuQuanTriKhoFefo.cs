using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuQuanTriKhoFefo : IDichVuCoSo
    {
        Task<KiemKhoLoAdminViewModel> XayDungDuLieuKiemKhoAsync(
            string? tuKhoa,
            string locTrangThai = "ALL",
            string locGiamGia = "ALL");
        Task<(bool HopLe, string ThongBao)> ThemLoBanhMoiAsync(
            int maBanh,
            string? maLoCode,
            DateTime? ngaySanXuat,
            DateTime? hanSuDung,
            int slNhapBanDau = 50,
            decimal phanTramGiamCanDate = 0m,
            string? nguonHangPhanPhoi = null,
            decimal donGiaNhapSi = 0m,
            bool tuDongGhiPhieuChi = false);
        Task<string?> CapNhatGiamGiaCanDateAsync(
            int maLo,
            decimal phanTramGiam,
            string? trangThaiLo,
            int? slConLai);
        Task<string?> XoaLoBanhAsync(int maLo);
        Task<object> KichHoatXaKhoNhanhAsync(int maLo, int mucGiam = 30);
    }
}
