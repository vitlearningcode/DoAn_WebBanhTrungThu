using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuHopQuaTuChon : IDichVuCoSo
    {
        string TinhMaBamCauHinh(int maVh, IEnumerable<int> danhSachMaBanh);
        Task<object> ThemHoacCapNhatHopTuChonAsync(
            Models.Entities.GioHang gioHang,
            int? maNd,
            string? maPhienKvl,
            LuuHopTuChonRequest? req);
        Task<HopQuaTuChon?> SaoChepHopQuaTuChonAsync(int maHopTcGoc, int? maNd, string? maPhienKvl);
        Task XoaDongChiTietVaHopMoCoiAsync(ChiTietGioHang dongGio);
        Task GopGioHangTuKhachVangLaiAsync(string? maPhienKVL, int maND);
        Task<object> SaoChepDonCuVaoGioHangAsync(Models.Entities.GioHang gioHang, int maDonHang, int? maNd);
    }
}
