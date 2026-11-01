using System.ComponentModel.DataAnnotations.Schema;

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
        public string TenTrangThaiDhHienThi => TrangThaiDh switch
        {
            "CHO_XAC_NHAN" => "Chờ xác nhận",
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
            "DA_TT" => "Đã thanh toán",
            "DA_HOAN_TIEN" => "Đã hoàn tiền ngân hàng",
            _ => TrangThaiTt
        };
    }
}

