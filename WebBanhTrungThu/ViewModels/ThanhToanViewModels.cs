using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class TrangThanhToanViewModel
    {
        // 1. Thông tin Giỏ hàng & Chính sách B2B / Voucher
        public GioHang? GioHang { get; set; }
        public List<ChiTietGioHang> DanhSachMon { get; set; } = new();
        public int TongSoHop { get; set; }
        public decimal TongTienHang { get; set; }
        public decimal PhanTramCkB2B { get; set; }
        public decimal TienChietKhauB2B { get; set; }
        public bool MienPhiInLogo { get; set; }
        public decimal PhiInLogoMacDinh { get; set; } = 500000m;
        public decimal TienGiamVoucher { get; set; }
        public string? CodeKmDangDung { get; set; }

        // 2. Thông tin Người đặt (Member hoặc Guest)
        public bool DaDangNhap { get; set; }
        public string MaPhienHienTai { get; set; } = string.Empty;
        public string HoTenNguoiMua { get; set; } = string.Empty;
        public string SdtNguoiMua { get; set; } = string.Empty;
        public string EmailNguoiMua { get; set; } = string.Empty;
        public List<SoDiaChi> DanhSachSoDiaChi { get; set; } = new();
    }

    public class TaoDonHangRequest
    {
        // 1. Thông tin người mua
        public string HoTenNguoiMua { get; set; } = string.Empty;
        public string SdtNguoiMua { get; set; } = string.Empty;
        public string? EmailNguoiMua { get; set; }
        public string PhuongThucTt { get; set; } = "COD";

        // 2. Tùy chọn Doanh nghiệp B2B
        public bool LaDonDoanhNghiepB2B { get; set; }
        public string? TenCongTy { get; set; }
        public string? MaSoThue { get; set; }
        public bool YeuCauInLogo { get; set; }

        // 3. Danh sách các đợt giao hàng đa điểm nhận
        public List<DotGiaoHangInput> DanhSachDotGiao { get; set; } = new();
    }

    public class DotGiaoHangInput
    {
        public string TenNguoiNhan { get; set; } = string.Empty;
        public string SdtNguoiNhan { get; set; } = string.Empty;
        public string DiaChiGiaoChiTiet { get; set; } = string.Empty;
        public DateOnly? NgayHenGiao { get; set; }
        public string KhungGioGiao { get; set; } = "Sáng (08:00 - 11:30)";
        public bool CoKemThiepQr { get; set; } = true;
        public string? LoiChucThiep { get; set; }
        public string MauGiaoDienThiep { get; set; } = "CO_PHONG_HOANG_GIA";
    }

    public class DonHangThanhCongViewModel
    {
        public DonHang DonHang { get; set; } = null!;
        public int DiemCongThem { get; set; }
        public List<DotGiaoKemQrItem> DanhSachDotGiaoQr { get; set; } = new();
    }

    public class DotGiaoKemQrItem
    {
        public DotGiaoHangDaDiem DotGiao { get; set; } = null!;
        public string UrlThiep3D { get; set; } = string.Empty;
        public string AnhQrBase64 { get; set; } = string.Empty;
    }
}

