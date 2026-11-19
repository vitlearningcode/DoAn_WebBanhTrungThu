using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.GiaoDien;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuTrangChu : IDichVuCoSo
    {
        Task<LandingPageViewModel> XayDungDuLieuTrangChuAsync(int? maND, string? maPhienKVL);
        Task<List<GoiYBanhKetQua>> GoiYBanhThongMinhAsync(string? doiTuong, string? sucKhoe, string? huongVi, decimal nganSachMax);
        Task<object> TraCuuDonHangNhanhAsync(string? maTraCuu, string? sdt);
    }
}
