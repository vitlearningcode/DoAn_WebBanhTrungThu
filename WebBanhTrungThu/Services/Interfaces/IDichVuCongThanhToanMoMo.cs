using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.ThanhToan;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ chuyên trách Cổng thanh toán QR MoMo / VietQR, Webhook xác nhận thu tiền
    /// và hủy đơn QR quá hạn 15 phút hoàn kho FEFO + hoàn điểm VIP.
    /// </summary>
    public interface IDichVuCongThanhToanMoMo : IDichVuCoSo
    {
        DuLieuQrMoMo TaoDuLieuQrMoMo(DonHang donHang, bool sinhAnhQr = true);
        Task<(string TrangThaiDieuHuong, CongThanhToanMoMoViewModel? DuLieuView)> XayDungDuLieuCongMoMoAsync(string maTraCuu);
        Task<(DonHang? DonHang, int SoGiayConLai)> KiemTraVaCapNhatTrangThaiDonAsync(string maTraCuu);
        Task<KetQuaHuyDonHetHan> HuyDonHetHanTheoMaTraCuuAsync(string maTraCuu);
        Task<KetQuaXuLyWebhookMoMo> XuLyWebhookMoMoAsync(MomoWebhookPayload payload);
        Task<int> KiemTraVaHuyDonQrQuaHanAsync(int? maDonHang = null);
    }
}
