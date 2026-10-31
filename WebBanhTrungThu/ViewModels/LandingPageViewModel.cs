using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class LandingPageViewModel
    {
        // 1. Cấu hình các phân khu trên Landing Page
        public CauHinhLandingPage? HeroBanner { get; set; }
        public CauHinhLandingPage? BoxBuilderIntro { get; set; }

        // 2. Dữ liệu Sản phẩm, Danh mục & Dinh dưỡng (Tầng 3 & Tầng 4)
        public List<DanhMucBanh> DanhMucs { get; set; } = new();
        public List<SanPhamBanh> DanhSachBanh { get; set; } = new();
        public List<MauVoHop> DanhSachVoHop { get; set; } = new();
        public List<ComboMauCoSan> DanhSachCombo { get; set; } = new();

        // 3. Bảng chiết khấu sỉ B2B (Tầng 5) & Khuyến mãi
        public List<BangChietKhauSiB2b> BangGiaSiB2B { get; set; } = new();
        public List<MaKhuyenMai> DanhSachKhuyenMai { get; set; } = new();

        // 4. Từ điển Hình ảnh đa nguồn (Local + Ổ cứng + Drive)
        public Dictionary<string, string> TuDienAnh { get; set; } = new();

        // 5. Thông tin Phiên & Giỏ hàng hiện tại (Guest hoặc Member)
        public bool DaDangNhap { get; set; }
        public string HoTenHienThi { get; set; } = "Khách Vãng Lai";
        public string MaPhienHienTai { get; set; } = string.Empty;
        public GioHang? GioHangHienTai { get; set; }
        public int TongSoLuongTrongGio { get; set; }
        public decimal TongTamTinhGioHang { get; set; }
    }
}