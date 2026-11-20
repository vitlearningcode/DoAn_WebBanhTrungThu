using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuCaiDatHeThong : IDichVuCoSo
    {
        Task<CaiDatHeThongViewModel> XayDungTrangCaiDatAsync(string? muc);
        Task<(bool ThanhCong, string ThongBao)> LuuCauHinhMoMoAsync(CauHinhThanhToanQr model);
        Task<(bool ThanhCong, string ThongBao)> LuuCauHinhCuaHangAsync(CauHinhCuaHang model);
        Task<(bool ThanhCong, string ThongBao)> LuuCauHinhVanChuyenAsync(CauHinhVanChuyen model);
        Task<(bool ThanhCong, string ThongBao)> LuuCauHinhHoiVienVipAsync(CauHinhHoiVienVip model);
    }
}
