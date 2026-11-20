using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuNhaCungCap : IDichVuCoSo
    {
        Task<NhaCungCapAdminViewModel> XayDungTrangNhaCungCapAsync(
            string? tuKhoa,
            string locLoaiNcc = "ALL",
            string locTrangThai = "ALL");

        Task<List<NhaCungCapOptionViewModel>> LayDanhSachNccHoatDongAsync();

        Task<object?> LayChiTietNhaCungCapJsonAsync(int maNcc);

        Task<(bool ThanhCong, string ThongBao)> LuuNhaCungCapAsync(LuuNhaCungCapRequest req);

        Task<(bool ThanhCong, string ThongBao)> XoaHoacNgungNhaCungCapAsync(int maNcc);

        Task<(bool ThanhCong, string ThongBao)> KhoiPhucNhaCungCapAsync(int maNcc);
    }
}
