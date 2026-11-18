using System.ComponentModel.DataAnnotations.Schema;
using WebBanhTrungThu.Services;

namespace WebBanhTrungThu.Models.Entities
{
    /// <summary>
    /// Cấu trúc gốc lưu trữ dữ liệu mở rộng của Đại lý Phân phối Nguyệt Cát ERP
    /// (Thương hiệu, Nhà cung cấp, Vị trí kho, Đơn vị tính, Nhật ký lô FEFO và Cấu hình QR MoMo).
    /// </summary>
    public class DuLieuMoRongHeThongRoot
    {
        public List<string> DanhSachThuongHieu { get; set; } = new()
        {
            "Đồng Khánh",
            "Kinh Đô",
            "Như Lan",
            "Givral",
            "Brodard",
            "Nguyệt Cát"
        };

        public List<string> DanhSachNhaCungCap { get; set; } = new()
        {
            "Lò Bánh Đồng Khánh Chính Gốc",
            "NPP Bánh Kinh Đô Miền Nam",
            "NPP Như Lan Sài Gòn",
            "Bánh Trung Thu Givral Chính Hãng",
            "NPP Brodard Sài Gòn",
            "Đại Lý Phân Phối Nguyệt Cát"
        };

        public List<string> DanhSachViTriKho { get; set; } = new()
        {
            "Kệ A1",
            "Kệ A2",
            "Kệ B1",
            "Kệ B2",
            "Tủ Mát C1"
        };

        public List<string> DanhSachDonViTinh { get; set; } = new()
        {
            "Cái",
            "Hộp",
            "Lốc 4 cái",
            "Thùng 20 hộp"
        };

        public Dictionary<string, CauHinhHangHoaMoRong> Items { get; set; } = new();
        public Dictionary<int, ThongTinDonHangMoRong> DonHangs { get; set; } = new();
        public CauHinhThanhToanQr CauHinhQr { get; set; } = new();
        public List<PhieuThuChiMoRong> DanhSachPhieuThuChi { get; set; } = new();
    }

    public partial class SanPhamBanh
    {
        [NotMapped]
        public string MaVach { get; set; } = string.Empty;

        [NotMapped]
        public string ThuongHieu { get; set; } = "Đồng Khánh";

        [NotMapped]
        public string NhaCungCap { get; set; } = "Lò Bánh Đồng Khánh Chính Gốc";

        [NotMapped]
        public string ViTriKho { get; set; } = "Kệ A1";

        [NotMapped]
        public string DonViTinh { get; set; } = "Cái";

        [NotMapped]
        public bool BanTrucTiep { get; set; } = true;

        [NotMapped]
        public decimal PhanTramGiamCanDateHieuLuc
        {
            get
            {
                if (LoSanXuatBanhs == null || !LoSanXuatBanhs.Any()) return 0m;
                var lo = LoSanXuatBanhs
                    .Where(l => (l.TrangThaiLo == "XA_CAN_DATE" || l.TrangThaiLo == "CAN_DATE" || l.PhanTramGiamCanDate > 0) && l.PhanTramGiamCanDate > 0)
                    .OrderByDescending(l => l.PhanTramGiamCanDate)
                    .FirstOrDefault();
                return lo != null ? Math.Round(lo.PhanTramGiamCanDate, 0) : 0m;
            }
        }

        [NotMapped]
        public bool DangXaCanDate => PhanTramGiamCanDateHieuLuc > 0m;

        [NotMapped]
        public decimal DonGiaThucTe => DangXaCanDate
            ? Math.Round(DonGiaLe * (1m - (PhanTramGiamCanDateHieuLuc / 100m)), 0)
            : DonGiaLe;
    }

    public partial class ComboMauCoSan
    {
        [NotMapped]
        public string MaVach { get; set; } = string.Empty;

        [NotMapped]
        public string ThuongHieu { get; set; } = "Kinh Đô";

        [NotMapped]
        public string NhaCungCap { get; set; } = "NPP Bánh Kinh Đô Miền Nam";

        [NotMapped]
        public string ViTriKho { get; set; } = "Kệ B1";

        [NotMapped]
        public string DonViTinh { get; set; } = "Hộp";

        [NotMapped]
        public bool BanTrucTiep { get; set; } = true;

        [NotMapped]
        public int MaDm { get; set; } = 1;

        [NotMapped]
        public string TenDm { get; set; } = "ĐỒNG GIÁ 20K";

        [NotMapped]
        public int TongTonKho { get; set; } = 45;

        [NotMapped]
        public int TrongLuongGram { get; set; } = 600;
    }

    public partial class MaKhuyenMai
    {
        [NotMapped]
        public bool TrangThaiKichHoat { get; set; } = true;

        [NotMapped]
        public int SoLuotDaDung => SldaDung;

        [NotMapped]
        public int SoLuotToiDa => SltoiDa;

        [NotMapped]
        public decimal GiaTriDonToiThieu => DonToiThieu;
    }

    public partial class LoSanXuatBanh
    {
        [NotMapped]
        public DateTime NgayHetHan => HanSuDung.ToDateTime(TimeOnly.MinValue);
    }

    public partial class DotGiaoHangDaDiem
    {
        [NotMapped]
        public string? TenNguoiNhanThiep { get; set; }

        [NotMapped]
        public string? TenNguoiGuiThiep { get; set; }
    }
}
