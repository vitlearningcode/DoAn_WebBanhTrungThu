using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuNhapKho : IDichVuCoSo
    {
        Task<NhapKhoAdminViewModel> XayDungTrangNhapKhoAsync(
            string? tuKhoa,
            string locTrangThai = "ALL",
            string locThanhToan = "ALL",
            string locNhaCungCap = "ALL");

        Task<object?> LayChiTietPhieuNhapJsonAsync(int maPn);

        Task<(bool ThanhCong, string ThongBao)> LuuPhieuNhapAsync(
            LuuPhieuNhapRequest req,
            string nguoiLap,
            int? maNdLap);

        Task<(bool ThanhCong, string ThongBao)> HoanThanhPhieuTamAsync(int maPn);

        Task<(bool ThanhCong, string ThongBao)> XoaHoacHuyPhieuNhapAsync(int maPn);

        Task<(bool ThanhCong, string ThongBao)> ThanhToanPhieuNhapAsync(
            ThanhToanPhieuNhapRequest req,
            string nguoiLap);
    }
}
