using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuXuatKho : IDichVuCoSo
    {
        Task<XuatKhoAdminViewModel> XayDungTrangXuatKhoAsync(
            string? tuKhoa,
            string locLoaiXuat = "ALL",
            string locTrangThai = "ALL");

        Task<(bool ThanhCong, string ThongBao)> TaoPhieuXuatAsync(
            TaoPhieuXuatRequest req,
            string nguoiLap,
            int? maNdLap);

        Task<(bool ThanhCong, string ThongBao)> HuyPhieuXuatAsync(int maPx);
    }
}
