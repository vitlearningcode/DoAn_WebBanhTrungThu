using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;

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
        public string ThongTinVoucherGoiY { get; set; } = "HSD: 31/12/2027 • Đơn từ 200k";
        public List<string> DanhSachCanhBaoTonKho { get; set; } = new();

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
        // 1. Thông tin người mua & vận chuyển
        public string HoTenNguoiMua { get; set; } = string.Empty;
        public string SdtNguoiMua { get; set; } = string.Empty;
        public string? EmailNguoiMua { get; set; }
        public string HinhThucNhanHang { get; set; } = "GIAO_TAN_NOI";
        public string GoiTocDoGiao { get; set; } = "GIAO_TIEU_CHUAN";
        public string TocDoGiaoHang { get; set; } = "GIAO_TIEU_CHUAN";
        public string? TinhThanhNhanHang { get; set; }
        public string PhuongThucTt { get; set; } = "COD";
        public bool CoThiep3D { get; set; } = false;
        public string? MaCaptcha { get; set; }
        public bool DaXacThucCaptcha { get; set; }

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
        public string? TinhThanhPho { get; set; }
        public DateOnly? NgayHenGiao { get; set; }
        public string KhungGioGiao { get; set; } = "Sáng (08:00 - 11:30)";
        public bool CoKemThiepQr { get; set; } = false;
        public string? TenNguoiNhanThiep { get; set; }
        public string? TenNguoiGuiThiep { get; set; }
        public string? LoiChucThiep { get; set; }
        public string MauGiaoDienThiep { get; set; } = "CO_PHONG_HOANG_GIA";
    }

    public class DonHangThanhCongViewModel
    {
        public DonHang DonHang { get; set; } = null!;
        public int DiemCongThem { get; set; }
        public List<DotGiaoKemQrItem> DanhSachDotGiaoQr { get; set; } = new();
        public CauHinhThanhToanQr CauHinhQr { get; set; } = new();
        public string AnhQrThanhToanBase64 { get; set; } = string.Empty;
        public string UrlVietQr { get; set; } = string.Empty;
        public string NoiDungChuyenKhoan { get; set; } = string.Empty;
        public DateTime HanThanhToan { get; set; }
        public int SoGiayConLai { get; set; }
        public bool DangChoThanhToanQr { get; set; }
    }

    public class DotGiaoKemQrItem
    {
        public DotGiaoHangDaDiem DotGiao { get; set; } = null!;
        public string UrlThiep3D { get; set; } = string.Empty;
        public string AnhQrBase64 { get; set; } = string.Empty;
    }

    public class XacThucCaptchaRequest
    {
        public string? MaCaptcha { get; set; }
        public bool DaTichKhongPhaiRobot { get; set; } = true;
    }

    public class MomoWebhookPayload
    {
        public string? Content { get; set; }
        public string? Description { get; set; }
        public string? OrderInfo { get; set; }
        public string? MaDonHang { get; set; }
        public decimal? TransferAmount { get; set; }
        public decimal? Amount { get; set; }
        public decimal? SoTien { get; set; }
        public List<MomoWebhookTransactionItem>? Data { get; set; }
    }

    public class MomoWebhookTransactionItem
    {
        public string? Description { get; set; }
        public string? Content { get; set; }
        public decimal? Amount { get; set; }
        public decimal? TransferAmount { get; set; }
    }
}
