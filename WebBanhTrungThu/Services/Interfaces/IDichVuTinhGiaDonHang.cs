using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuTinhGiaDonHang : IDichVuCoSo
    {
        decimal TinhDonGiaThucTeTuCsdl(ChiTietGioHang ct);
        bool KiemTraVungHoTroHoaToc(string? tinhThanh, string? diaChiChiTiet);
        Task<KetQuaTinhChiPhiDonHang> TinhToanChiPhiDonHangAsync(
            Models.Entities.GioHang gioHang,
            int? maNd,
            bool laDonDoanhNghiepB2B = false,
            bool yeuCauInLogo = false,
            string? hinhThucNhanHang = null,
            string? tocDoGiaoHang = null,
            List<DotGiaoHangInput>? danhSachDotGiao = null,
            bool capNhatLuotDungVoucher = false,
            bool luuCapNhatGiaGioHang = false,
            bool dungDiemTichLuy = false);
    }
}
