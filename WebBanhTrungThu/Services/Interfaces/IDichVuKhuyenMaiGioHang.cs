using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuKhuyenMaiGioHang : IDichVuCoSo
    {
        decimal TinhDonGiaThucTeTuCsdl(ChiTietGioHang ct);
        Task DamBaoMaKhuyenMaiMauConHanAsync();
        decimal TinhTienGiamVoucher(MaKhuyenMai km, decimal tongTamTinh);
        Task<object> ApDungVoucherChoGioHangAsync(
            Models.Entities.GioHang gioHang,
            int? maNd,
            ApDungVoucherRequest? req,
            string? codeKm);
        Task<object> TongKetThongTinGioHangJsonAsync(Models.Entities.GioHang? gioHang, int? maNd);
        Task<object> KiemTraTonKhoGioHangChiTietAsync(Models.Entities.GioHang? gioHang);
    }
}
