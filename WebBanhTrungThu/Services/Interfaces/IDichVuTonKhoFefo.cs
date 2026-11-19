using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuTonKhoFefo : IDichVuCoSo
    {
        Task<KetQuaKiemTraTonKhoGioHang> KiemTraTonKhoGioHangAsync(Models.Entities.GioHang gioHang);
        Task<List<NhatKyTruLoFefo>> TruTonKhoFefoAsync(
            int maBanh,
            int soLuongCanTru,
            List<NhatKyTruLoFefo>? danhSachLoDaTru = null);
        Task<List<NhatKyTruVoHop>> TruTonKhoVoHopAsync(
            int maVh,
            int soLuongCanTru,
            List<NhatKyTruVoHop>? danhSachVoHopDaTru = null);
        Task HoanTraTonKhoTheoDonAsync(DonHang donHang);
        Task HoanTraTonKhoRollbackAsync(DonHang donHang);
    }
}
