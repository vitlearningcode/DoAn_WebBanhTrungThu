using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ quản trị danh mục Mẫu vỏ hộp và nhật ký Nhập/Xuất hủy kho vỏ hộp.
    /// </summary>
    public interface IDichVuQuanTriVoHop : IDichVuCoSo
    {
        Task<VoHopAdminIndexViewModel> LayDuLieuTrangQuanTriAsync(string? tuKhoa, int? sucChua, string? trangThaiTon);
        Task<(bool ThanhCong, string ThongBao)> LuuMauVoHopAsync(LuuMauVoHopRequest req);
        Task<(bool ThanhCong, string ThongBao)> ChuyenTrangThaiVoHopAsync(int maVh);
        Task<(bool ThanhCong, string ThongBao)> ThucHienNhapXuatVoHopAsync(NhapXuatVoHopRequest req, int? maNdLap, string tenNguoiLap);
    }
}
