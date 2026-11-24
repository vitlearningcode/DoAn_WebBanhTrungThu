using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WebBanhTrungThu.Models.Entities
{
    public partial class DonHang
    {
        [NotMapped]
        public string? LyDoHuyHoan
        {
            get => LyDoHuy;
            set => LyDoHuy = value;
        }

        [NotMapped]
        public DateTime? HanThanhToan => HanThanhToanQr;

        [NotMapped]
        public decimal TongThanhToan => TongThanhToanCuoi;

        [NotMapped]
        public IEnumerable<DotGiaoHangDaDiem> ThiepChucMung3Ds =>
            DotGiaoHangDaDiems?.Where(d => d.CoKemThiepQr && !string.IsNullOrWhiteSpace(d.MaTokenThiepQr))
            ?? Enumerable.Empty<DotGiaoHangDaDiem>();

        [NotMapped]
        public string TenTrangThaiDhHienThi => (TrangThaiDh ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "CHO_XAC_NHAN" or "CHO_DUYET" => "Chờ xác nhận",
            "DA_XAC_NHAN" or "CHO_DONG_GOI" => "Đã xác nhận",
            "CHO_LAY_HANG" or "DANG_DONG_GOI_HOP" => "Chờ lấy hàng (Đã đóng gói)",
            "DANG_GIAO" => "Đang giao hàng",
            "DA_GIAO" or "HOAN_TAT" => "Hoàn tất",
            "DA_HUY" => "Đã hủy",
            "HOAN_TIEN_TRA_HANG" => "Hoàn tiền / Trả hàng",
            _ => "Đang xử lý"
        };

        [NotMapped]
        public string TenTrangThaiTtHienThi => (TrangThaiTt ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "CHUA_TT" => "Chưa thanh toán",
            "CHO_THANH_TOAN" => "Chờ thanh toán QR (15 phút)",
            "CHO_XAC_MINH_CK" => "Chờ thanh toán QR",
            "DA_TT" or "DA_THANH_TOAN" => "Đã thanh toán",
            "HET_HAN_HUY" => "Hết hạn thanh toán 15p (Đã hủy)",
            "HET_HAN_15P" => "Hết hạn thanh toán 15p (Đã hoàn kho)",
            "DA_HOAN_TIEN" => "Đã hoàn tiền ngân hàng",
            _ => "Chưa thanh toán"
        };

        public static string AnhXaPhuongThucTtHienThi(string? maPhuongThuc)
        {
            return (maPhuongThuc ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "TIEN_MAT" => "Tiền mặt tại quầy",
                "COD" => "Thanh toán khi nhận hàng (COD)",
                "MOMO_QR" or "MOMO" => "Ví MoMo QR",
                "CHUYEN_KHOAN" or "CHUYEN_KHOAN_QR" or "VIETQR" => "Chuyển khoản VietQR",
                "VNPAY" => "Ví VNPay",
                "VI_DIEN_TU" or "THE_NGAN_HANG" => "Thẻ / Ví điện tử",
                _ => "Thanh toán đơn hàng"
            };
        }

        [NotMapped]
        public string TenPhuongThucTtHienThi => AnhXaPhuongThucTtHienThi(PhuongThucTt);

        [NotMapped]
        public string TenTrangThaiThanhToanHienThi => TenTrangThaiTtHienThi;
    }
}

