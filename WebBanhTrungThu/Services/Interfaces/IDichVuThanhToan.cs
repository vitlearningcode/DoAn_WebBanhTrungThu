using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuThanhToan : IDichVuCoSo
    {
        Task<KetQuaKiemTraTonKhoGioHang> KiemTraTonKhoGioHangAsync(Models.Entities.GioHang gioHang);
        Task<Models.Entities.GioHang?> LayGioHangDayDuAsync(int? maNd, string? maPhienKvl);
        Task<(TrangThanhToanViewModel? ViewModel, string? TenBanhHetHang, string? ThongBaoHetHang)> XayDungDuLieuTrangThanhToanAsync(int? maNd, string? maPhienKvl);
        Task GoMaKhuyenMaiGioHangAsync(int? maNd, string? maPhienKvl);
        Task<KetQuaXuLyDatHang> XuLyDatHangAsync(
            Models.Entities.GioHang gioHang,
            TaoDonHangRequest req,
            int? maNd,
            string? maPhienKvl,
            string hinhThucNhanHang,
            string tocDoGiao);
        Task<DonHangThanhCongViewModel?> XayDungDuLieuDonHangThanhCongAsync(string maTraCuu, string scheme, string host);
    }
}
