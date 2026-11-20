using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class LoXuatOptionViewModel
    {
        public int MaLo { get; set; }
        public string MaLoCode { get; set; } = string.Empty;
        public string NgaySanXuat { get; set; } = string.Empty;
        public string HanSuDung { get; set; } = string.Empty;
        public int SlConLai { get; set; }
        public string TrangThaiLo { get; set; } = "DANG_BAN";
    }

    public class SanPhamKhoOptionViewModel
    {
        public int MaBanh { get; set; }
        public string MaSku { get; set; } = string.Empty;
        public string TenBanh { get; set; } = string.Empty;
        public decimal DonGiaLe { get; set; }
        public decimal GiaVon { get; set; }
        public int TonKhoHeThong { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = string.Empty;
        public List<LoXuatOptionViewModel> DanhSachLo { get; set; } = new();
    }

    // =========================================================================
    // 1. NHẬP HÀNG (PHIẾU NHẬP KHO)
    // =========================================================================
    public class NhapKhoAdminViewModel
    {
        public List<PhieuNhapKho> DanhSachPhieuNhap { get; set; } = new();
        public List<SanPhamKhoOptionViewModel> DanhSachSanPham { get; set; } = new();
        public List<NhaCungCapOptionViewModel> DanhSachNccChiTiet { get; set; } = new();
        public List<string> DanhSachThuongHieu { get; set; } = new();
        public List<string> DanhSachNhaCungCap { get; set; } = new();
        public string? TuKhoa { get; set; }
        public string LocTrangThai { get; set; } = "ALL";
        public string LocThanhToan { get; set; } = "ALL";
        public string LocNhaCungCap { get; set; } = "ALL";

        public int TongSoPhieu { get; set; }
        public int SoPhieuTam { get; set; }
        public int SoPhieuDaNhap { get; set; }
        public decimal TongGiaTriNhap { get; set; }
        public decimal TongDaThanhToan { get; set; }
        public decimal TongConNo => Math.Max(0m, TongGiaTriNhap - TongDaThanhToan);
    }

    public class DongChiTietNhapDto
    {
        public int MaSp { get; set; }
        public int SoLuong { get; set; }
        public decimal GiaNiemYet { get; set; }
        public decimal PhanTramChietKhau { get; set; }
        public decimal DonGiaNhap { get; set; }
        public DateTime? NgaySanXuat { get; set; }
        public DateTime? HanSuDung { get; set; }
    }

    public class LuuPhieuNhapRequest
    {
        public int MaPn { get; set; }
        public int? MaNcc { get; set; }
        public string NhaCungCap { get; set; } = string.Empty;
        public decimal GiamGia { get; set; }
        public string? GhiChu { get; set; }
        public bool HoanThanhNhapKho { get; set; }
        public decimal SoTienThanhToanNgay { get; set; }
        public string PhuongThucThanhToan { get; set; } = "CHUYEN_KHOAN";
        public string? ChiTietJson { get; set; }
    }

    public class ThanhToanPhieuNhapRequest
    {
        public int MaPn { get; set; }
        public decimal SoTienTra { get; set; }
        public string PhuongThuc { get; set; } = "CHUYEN_KHOAN";
        public string? GhiChu { get; set; }
    }

    // =========================================================================
    // 2. XUẤT HÀNG / XUẤT HỦY / TIÊU DÙNG NỘI BỘ
    // =========================================================================
    public class XuatKhoAdminViewModel
    {
        public List<PhieuXuatKho> DanhSachPhieuXuat { get; set; } = new();
        public List<SanPhamKhoOptionViewModel> DanhSachSanPham { get; set; } = new();
        public string? TuKhoa { get; set; }
        public string LocLoaiXuat { get; set; } = "ALL";
        public string LocTrangThai { get; set; } = "ALL";

        public int TongSoPhieu { get; set; }
        public int SoPhieuHuyLoi { get; set; }
        public int SoPhieuTraNcc { get; set; }
        public int SoPhieuNoiBo { get; set; }
        public int SoPhieuKiemKho { get; set; }
        public decimal TongGiaTriVonXuat { get; set; }
    }

    public class DongChiTietXuatDto
    {
        public int MaSp { get; set; }
        public int? MaLo { get; set; }
        public int SoLuongXuat { get; set; }
        public string? GhiChuDong { get; set; }
    }

    public class TaoPhieuXuatRequest
    {
        public string LoaiXuat { get; set; } = "XUAT_HUY_LOI";
        public string DoiTuongLienQuan { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public string? ChiTietJson { get; set; }
    }

    // =========================================================================
    // 3. KIỂM KHO ĐỐI SOÁT THỰC TẾ & CÂN BẰNG TỰ ĐỘNG
    // =========================================================================
    public class KiemKhoAdminViewModel
    {
        public List<PhieuKiemKho> DanhSachPhieuKiem { get; set; } = new();
        public List<SanPhamKhoOptionViewModel> DanhSachSanPham { get; set; } = new();
        public List<LoSanXuatBanh> DanhSachLoCanDate { get; set; } = new();
        public string? TuKhoa { get; set; }
        public string LocTrangThai { get; set; } = "ALL";

        public int TongSoPhieu { get; set; }
        public int SoPhieuTam { get; set; }
        public int SoPhieuDaCanBang { get; set; }
        public int TongLechTang { get; set; }
        public int TongLechGiam { get; set; }
        public decimal TongGiaTriLech { get; set; }
    }

    public class DongChiTietKiemKhoDto
    {
        public int MaSp { get; set; }
        public string? MaSku { get; set; }
        public string? TenBanh { get; set; }
        public int TonHeThong { get; set; }
        public int TonThucTe { get; set; }
        public decimal GiaVon { get; set; }
        public string? LyDoLech { get; set; }
    }

    public class LuuPhieuKiemKhoRequest
    {
        public int MaPkk { get; set; }
        public string? GhiChu { get; set; }
        public bool CanBangNgay { get; set; }
        public string? ChiTietJson { get; set; }
    }

    public class KetQuaDocExcelKiemKhoViewModel
    {
        public bool ThanhCong { get; set; }
        public string ThongBao { get; set; } = string.Empty;
        public int SoDongHopLe { get; set; }
        public int SoDongBoQua { get; set; }
        public List<DongChiTietKiemKhoDto> DanhSachDong { get; set; } = new();
        public List<string> DanhSachLoi { get; set; } = new();
    }

    // =========================================================================
    // 4. IMPORT EXCEL HÀNG HÓA (SANPHAMADMIN)
    // =========================================================================
    public class KetQuaImportExcelSanPham
    {
        public bool ThanhCong { get; set; }
        public string ThongBao { get; set; } = string.Empty;
        public int SoThemMoi { get; set; }
        public int SoCapNhat { get; set; }
        public int SoBoQua { get; set; }
        public List<string> DanhSachLoi { get; set; } = new();
    }
}
