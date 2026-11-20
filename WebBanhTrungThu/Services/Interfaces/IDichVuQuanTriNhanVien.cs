using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuQuanTriNhanVien : IDichVuCoSo
    {
        Task<NhanVienAdminViewModel> XayDungTrangNhanVienAsync(
            string? tuKhoa,
            string locVaiTro = "ALL",
            string locChiNhanh = "ALL",
            string locCaLamViec = "ALL",
            string locTrangThai = "ALL",
            int? maNdDangNhap = null);

        Task<object?> LayChiTietNhanVienJsonAsync(int maNd);

        Task<(bool ThanhCong, string ThongBao)> LuuNhanVienAsync(LuuNhanVienRequest req, int? maNdDangNhap);

        Task<(bool ThanhCong, string ThongBao)> DatLaiMatKhauAsync(DatLaiMatKhauNvRequest req);

        Task<(bool ThanhCong, string ThongBao)> ChuyenTrangThaiKhoaAsync(int maNdMucTieu, int? maNdDangNhap);
    }
}
