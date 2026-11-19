using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuThanhToan : IDichVuCoSo
    {
        string TaoChuoiSvgCaptchaThuan(out string maCaptcha);
        string TaoAnhSvgCaptcha(out string maCaptcha);
        Task<KetQuaKiemTraTonKhoGioHang> KiemTraTonKhoGioHangAsync(Models.Entities.GioHang gioHang);
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
        Task<List<NhatKyTruLoFefo>> TruTonKhoFefoAsync(int maBanh, int soLuongCanTru, List<NhatKyTruLoFefo>? danhSachLoDaTru = null);
        Task HoanTraTonKhoRollbackAsync(DonHang donHang);
        Task<int> KiemTraVaHuyDonQrQuaHanAsync(int? maDonHang = null);
        Task<Models.Entities.GioHang?> LayGioHangDayDuAsync(int? maNd, string? maPhienKvl);
        Task<KetQuaXuLyDatHang> XuLyDatHangAsync(
            Models.Entities.GioHang gioHang,
            TaoDonHangRequest req,
            int? maNd,
            string? maPhienKvl,
            string hinhThucNhanHang,
            string tocDoGiao);
        Task<(string TrangThaiDieuHuong, CongThanhToanMoMoViewModel? DuLieuView)> XayDungDuLieuCongMoMoAsync(string maTraCuu);
        Task<DonHangThanhCongViewModel?> XayDungDuLieuDonHangThanhCongAsync(string maTraCuu, string scheme, string host);
        Task<(DonHang? DonHang, int SoGiayConLai)> KiemTraVaCapNhatTrangThaiDonAsync(string maTraCuu);
        Task<KetQuaXuLyWebhookMoMo> XuLyWebhookMoMoAsync(MomoWebhookPayload payload);
    }
}
