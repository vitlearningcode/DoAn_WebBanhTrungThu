using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WebBanhTrungThu.Models.Entities
{
    public partial class DonHang
    {
        [NotMapped]
        public string? MaNganHangHoan { get; set; }

        [NotMapped]
        public string? SoTaiKhoanHoan { get; set; }

        [NotMapped]
        public string? TenChuTkHoan { get; set; }

        [NotMapped]
        public string? LyDoHuyHoan { get; set; }

        [NotMapped]
        public string? GhiChuDieuChinhAdmin { get; set; }

        [NotMapped]
        public string? ThoiGianCapNhatCuoi { get; set; }

        [NotMapped]
        public string? NguoiCapNhatCuoi { get; set; }

        [NotMapped]
        public DateTime? HanThanhToan { get; set; }

        [NotMapped]
        public decimal TongThanhToan => TongThanhToanCuoi;

        [NotMapped]
        public IEnumerable<DotGiaoHangDaDiem> ThiepChucMung3Ds =>
            DotGiaoHangDaDiems?.Where(d => d.CoKemThiepQr && !string.IsNullOrWhiteSpace(d.MaTokenThiepQr))
            ?? Enumerable.Empty<DotGiaoHangDaDiem>();

        [NotMapped]
        public string TenTrangThaiDhHienThi => TrangThaiDh switch
        {
            "CHO_XAC_NHAN" => "Chờ xác nhận",
            "DA_XAC_NHAN" => "Đã xác nhận",
            "CHO_LAY_HANG" => "Chờ lấy hàng (Đã đóng gói)",
            "DANG_DONG_GOI_HOP" => "Chờ lấy hàng (Đã đóng gói)",
            "DANG_GIAO" => "Đang giao hàng",
            "HOAN_TAT" => "Hoàn tất",
            "DA_HUY" => "Đã hủy",
            _ => TrangThaiDh
        };

        [NotMapped]
        public string TenTrangThaiTtHienThi => TrangThaiTt switch
        {
            "CHUA_TT" => "Chưa thanh toán",
            "CHO_THANH_TOAN" => "Chờ thanh toán QR (15 phút)",
            "CHO_XAC_MINH_CK" => "Chờ thanh toán QR",
            "DA_TT" => "Đã thanh toán",
            "DA_THANH_TOAN" => "Đã thanh toán",
            "HET_HAN_15P" => "Hết hạn thanh toán 15p (Đã hoàn kho)",
            "DA_HOAN_TIEN" => "Đã hoàn tiền ngân hàng",
            _ => TrangThaiTt
        };

        [NotMapped]
        public string TenTrangThaiThanhToanHienThi => TenTrangThaiTtHienThi;
    }
}

