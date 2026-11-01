using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TongDoanhThuThucTe { get; set; }
        public int TongSoDonHang { get; set; }
        public int SoDonGuest { get; set; }
        public int SoDonMember { get; set; }
        public int SoDonB2B { get; set; }
        public int TongSoHopTuChonDaTao { get; set; }
        public int TongLuotMoThiepQr3D { get; set; }
        public int SoLoSapHetHan { get; set; }
        public int SoBanhTonKhoThap { get; set; }

        public List<TopBanhBanChayItem> TopBanhBanChay { get; set; } = new();
        public List<HoatDongGanDayItem> DanhSachHoatDong { get; set; } = new();
        public List<LoSanXuatBanh> DanhSachLoCanDate { get; set; } = new();
        public List<MauVoHop> DanhSachVoHopTonKho { get; set; } = new();
        public List<DonHang> DanhSachDonHangMoiNhat { get; set; } = new();
    }

    public class TopBanhBanChayItem
    {
        public string TenBanh { get; set; } = string.Empty;
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; }
        public int PhanTram { get; set; }
    }

    public class HoatDongGanDayItem
    {
        public string BieuTuong { get; set; } = "🧾";
        public string MauSac { get; set; } = "primary";
        public string TieuDe { get; set; } = string.Empty;
        public string MoTaChiTiet { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
    }

    public class HangThanhPhanItemViewModel
    {
        public int MaBanh { get; set; }
        public string MaHang { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public int SoLuong { get; set; } = 1;
        public decimal GiaVon { get; set; }
        public decimal GiaBan { get; set; }
        public decimal TongGiaVon => GiaVon * SoLuong;
        public decimal TongGiaBan => GiaBan * SoLuong;
    }

    public class HangHoaKiotItemViewModel
    {
        public string KeyId => $"{(LaCombo ? "CB" : "SP")}_{IdGoc}";
        public int IdGoc { get; set; }
        public bool LaCombo { get; set; } // false = Hàng hóa thường (Bánh lẻ), true = Combo - đóng gói
        public string MaHang { get; set; } = string.Empty;
        public string MaVach { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public int MaDm { get; set; }
        public string TenNhomHang { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = string.Empty;
        public string NhaCungCap { get; set; } = string.Empty;
        public string ViTriKho { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = "Cái";
        public decimal GiaBan { get; set; }
        public decimal GiaVon { get; set; }
        public int TonKho { get; set; }
        public string DuKienHetHang { get; set; } = "---";
        public int TrongLuongGram { get; set; } = 150;
        public bool BanTrucTiep { get; set; } = true;
        public bool LaBanhChay { get; set; }
        public bool LaBanhThuongHang { get; set; }
        public string MucDoNgot { get; set; } = "BINH_THUONG";
        public int SoTrungMuoi { get; set; } = 1;
        public string? MoTaNgan { get; set; }
        public string? CauChuyenBanh { get; set; }
        public int? MaVhCombo { get; set; }
        public string? TenVoHopCombo { get; set; }

        // Thông số dinh dưỡng (nếu là Bánh lẻ)
        public int NangLuongCalo { get; set; } = 380;
        public decimal LuongDuongGram { get; set; } = 14m;
        public int ChiSoDuongHuyetGi { get; set; } = 52;
        public string CanhBaoDiUng { get; set; } = "Không";

        // Hình ảnh
        public string UrlAnhChinh { get; set; } = "/images/default/banh-mac-dinh.png";
        public List<string> DanhSachAnh { get; set; } = new();

        // Danh sách hàng thành phần (dành cho Combo - đóng gói hoặc cấu thành)
        public List<HangThanhPhanItemViewModel> HangThanhPhans { get; set; } = new();

        // Lô hạn sử dụng (dành cho Bánh lẻ)
        public List<LoSanXuatBanh> DanhSachLo { get; set; } = new();
    }

    public class SanPhamAdminViewModel
    {
        public List<HangHoaKiotItemViewModel> DanhSachHangHoa { get; set; } = new();
        public List<SanPhamBanh> DanhSachBanh { get; set; } = new();
        public List<ComboMauCoSan> DanhSachCombo { get; set; } = new();
        public List<DanhMucBanh> DanhSachDanhMuc { get; set; } = new();
        public List<MauVoHop> DanhSachVoHop { get; set; } = new();
        public List<LoSanXuatBanh> DanhSachLoSanXuat { get; set; } = new();
        public Dictionary<string, string> TuDienAnh { get; set; } = new();

        // Danh mục KiotViet mở rộng (Thương hiệu, Nhà cung cấp, Vị trí, ĐVT)
        public List<string> DanhSachThuongHieu { get; set; } = new();
        public List<string> DanhSachNhaCungCap { get; set; } = new();
        public List<string> DanhSachViTriKho { get; set; } = new();
        public List<string> DanhSachDonViTinh { get; set; } = new();

        // 9 Bộ lọc cột trái chuẩn KiotViet
        public string? TuKhoa { get; set; }
        public int? MaDmLoc { get; set; }
        public string? TenDmLoc { get; set; }
        public string? LocDacTinh { get; set; }
        public string LocTonKho { get; set; } = "ALL";
        public string DuKienHetHang { get; set; } = "ALL";
        public string ThoiGianTao { get; set; } = "ALL";
        public string? NhaCungCapLoc { get; set; }
        public string? ThuongHieuLoc { get; set; }
        public string? ViTriKhoLoc { get; set; }
        public string LoaiHangLoc { get; set; } = "ALL"; // ALL, BANH_LE, COMBO
        public string BanTrucTiepLoc { get; set; } = "ALL"; // ALL, CO, KHONG
    }

    public class LuuHangHoaKiotRequest
    {
        public int IdGoc { get; set; } // 0 = Tạo mới, >0 = Cập nhật
        public bool LaCombo { get; set; }
        public string? MaHang { get; set; }
        public string? MaVach { get; set; }
        public string TenHang { get; set; } = string.Empty;
        public int MaDm { get; set; }
        public string? TenNhomHang { get; set; }
        public string? ThuongHieu { get; set; }
        public string? NhaCungCap { get; set; }
        public decimal GiaBan { get; set; }
        public decimal GiaVon { get; set; }
        public int TonKho { get; set; } = 50;
        public string? ViTriKho { get; set; }
        public int TrongLuong { get; set; } = 150;
        public string DonViTrongLuong { get; set; } = "g"; // "g" hoặc "kg"
        public string? DonViTinh { get; set; } = "Cái";
        public bool BanTrucTiep { get; set; } = true;

        // Cho Combo
        public int? MaVhCombo { get; set; }
        public string? HangThanhPhanJson { get; set; }

        // Tab Mô tả & Dinh dưỡng
        public string? MoTaNgan { get; set; }
        public string? CauChuyenBanh { get; set; }
        public int NangLuongCalo { get; set; } = 380;
        public decimal LuongDuongGram { get; set; } = 14m;
        public int ChiSoDuongHuyetGi { get; set; } = 52;
        public string? CanhBaoDiUng { get; set; } = "Không";
        public string? MucDoNgot { get; set; } = "BINH_THUONG";
        public bool LaBanhChay { get; set; }
        public bool LaBanhThuongHang { get; set; }

        // Upload ảnh hoặc nhập đường dẫn ảnh
        public IFormFile? FileAnhTaiLen { get; set; }
        public string? UrlAnhNhapTay { get; set; }
    }

    public class ThietLapGiaAdminViewModel
    {
        public List<MauVoHop> DanhSachVoHop { get; set; } = new();
        public List<ComboMauCoSan> DanhSachCombo { get; set; } = new();
        public List<BangChietKhauSiB2b> BangChietKhauB2B { get; set; } = new();
    }

    public class CapNhatBanhNhanhRequest
    {
        public int MaBanh { get; set; }
        public string TenBanh { get; set; } = string.Empty;
        public decimal DonGiaLe { get; set; }
        public decimal GiaVon { get; set; }
        public int TongTonKho { get; set; }
        public int NangLuongCalo { get; set; }
        public decimal LuongDuongGram { get; set; }
        public int ChiSoDuongHuyetGi { get; set; }
        public string? CanhBaoDiUng { get; set; }
    }

    public class ThemBanhMoiAdminRequest
    {
        public int MaDm { get; set; }
        public string MaSku { get; set; } = string.Empty;
        public string TenBanh { get; set; } = string.Empty;
        public decimal DonGiaLe { get; set; }
        public decimal GiaVon { get; set; }
        public int TrongLuongGram { get; set; } = 150;
        public int SoTrungMuoi { get; set; } = 1;
        public string MucDoNgot { get; set; } = "BINH_THUONG";
        public bool LaBanhChay { get; set; }
        public bool LaBanhThuongHang { get; set; }
        public string NhomHuongVi { get; set; } = "MAN_TRUYENTHONG";
        public string? TheDoiTuongPhuHop { get; set; } = "ONG_BA,SEP_VIP,BAN_BE";
        public string? MoTaNgan { get; set; }
        public int TongTonKho { get; set; } = 100;

        // Thông số dinh dưỡng 1-1
        public int NangLuongCalo { get; set; } = 380;
        public decimal LuongDuongGram { get; set; } = 14m;
        public decimal ChatDamGram { get; set; } = 8m;
        public decimal ChatBeoGram { get; set; } = 11m;
        public int ChiSoDuongHuyetGi { get; set; } = 52;
        public string? CanhBaoDiUng { get; set; } = "Không";

        // Lô đầu tiên
        public string? MaLoCodeDauTien { get; set; }
        public DateOnly? HanSuDungLoDau { get; set; }
    }

    public class KhoAnhAdminViewModel
    {
        public List<KhoHinhAnh> DanhSachHinhAnh { get; set; } = new();
        public List<LienKetHinhAnh> DanhSachLienKet { get; set; } = new();
        public List<SanPhamBanh> DanhSachBanh { get; set; } = new();
        public List<MauVoHop> DanhSachVoHop { get; set; } = new();
    }

    public class ThemAnhDaNguonRequest
    {
        public string? TenTepGoc { get; set; }
        public string? TieuDeAlt { get; set; }
        public int LoaiLuuTru { get; set; } = 1; // 1 = WebRoot, 2 = Ổ đĩa máy, 3 = Google Drive
        public IFormFile? TepAnhTaiLen { get; set; }
        public string? DuongDanCucBo { get; set; }
        public string? DuongDanDamMay { get; set; }
        public string? MaFileDrive { get; set; }

        // Gắn liên kết ảnh cho Bánh hoặc Vỏ hộp
        public string LoaiDoiTuong { get; set; } = "BANH";
        public int MaDoiTuong { get; set; } = 1;
        public string VaiTroAnh { get; set; } = "TACH_NEN_SLOT";
    }

    public class KhachHangAdminViewModel
    {
        public List<NguoiDung> DanhSachThanhVien { get; set; } = new();
        public List<PhienKhachVangLai> DanhSachPhienGuest { get; set; } = new();
        public string? TuKhoa { get; set; }
        public string? LocHangTv { get; set; }
    }

    public class KhuyenMaiAdminViewModel
    {
        public List<MaKhuyenMai> DanhSachKhuyenMai { get; set; } = new();
    }

    public class ThemVoucherMoiRequest
    {
        public string CodeKm { get; set; } = string.Empty;
        public string TenChuongTrinh { get; set; } = string.Empty;
        public string LoaiGiam { get; set; } = "PHAN_TRAM";
        public decimal GiaTriGiam { get; set; } = 10m;
        public decimal GiamToiDa { get; set; } = 150000m;
        public decimal DonToiThieu { get; set; } = 300000m;
        public int SltoiDa { get; set; } = 100;
        public bool ChoPhepKhachVangLai { get; set; } = true;
        public DateTime? NgayKetThuc { get; set; }
    }

    public class SoQuyAdminViewModel
    {
        public decimal QuyDauKy { get; set; } = 25000000m;
        public decimal TongThuTrongKy { get; set; }
        public decimal ThuTienMatCod { get; set; }
        public decimal ThuChuyenKhoanVietQr { get; set; }
        public decimal ThuViMoMo { get; set; }
        public decimal ThuVnPay { get; set; }
        public decimal TongGiaVonUocTinh { get; set; }
        public decimal LoiNhuanGop { get; set; }
        public decimal TonQuyCuoiKy => QuyDauKy + TongThuTrongKy;
        public List<DonHang> DanhSachPhieuThuDonHang { get; set; } = new();
    }
}
