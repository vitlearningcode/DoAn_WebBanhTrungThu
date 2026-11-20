using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class NhaCungCapOptionViewModel
    {
        public int MaNcc { get; set; }
        public string MaCodeNcc { get; set; } = string.Empty;
        public string TenNcc { get; set; } = string.Empty;
        public string LoaiNcc { get; set; } = "HANG_CHINH_HANG";
        public string ThuongHieuCungCap { get; set; } = "Đa thương hiệu";
        public decimal PhanTramChietKhauMacDinh { get; set; }
    }

    public class PhieuNhapTomTatNccViewModel
    {
        public int MaPn { get; set; }
        public string SoPhieuNhap { get; set; } = string.Empty;
        public DateTime NgayNhap { get; set; }
        public decimal TongPhaiTra { get; set; }
        public decimal DaThanhToan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string TrangThaiThanhToan { get; set; } = string.Empty;
    }

    public class NhaCungCapItemViewModel
    {
        public int MaNcc { get; set; }
        public string MaCodeNcc { get; set; } = string.Empty;
        public string TenNcc { get; set; } = string.Empty;
        public string LoaiNcc { get; set; } = "HANG_CHINH_HANG";
        public string TenLoaiNccHienThi => LoaiNcc == "DAI_LY_DIEU_TIET"
            ? "Đại lý điều tiết"
            : "Hãng / NPP Cấp 1";
        public string ThuongHieuCungCap { get; set; } = "Đa thương hiệu";
        public decimal PhanTramChietKhauMacDinh { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public string? GhiChu { get; set; }
        public bool DangHoatDong { get; set; }
        public DateTime NgayTao { get; set; }

        public int SoPhieuNhap { get; set; }
        public decimal TongGiaTriDaNhap { get; set; }
        public decimal TongDaThanhToan { get; set; }
        public decimal CongNoConPhaiTra => Math.Max(0m, TongGiaTriDaNhap - TongDaThanhToan);
        public List<PhieuNhapTomTatNccViewModel> DanhSachPhieuGanDay { get; set; } = new();
    }

    public class NhaCungCapAdminViewModel
    {
        public List<NhaCungCapItemViewModel> DanhSachNhaCungCap { get; set; } = new();
        public List<string> DanhSachThuongHieu { get; set; } = new();

        public string? TuKhoa { get; set; }
        public string LocLoaiNcc { get; set; } = "ALL";
        public string LocTrangThai { get; set; } = "ALL";

        public int TongSoNcc { get; set; }
        public int SoHangChinhHang { get; set; }
        public int SoDaiLyDieuTiet { get; set; }
        public decimal ChietKhauTrungBinh { get; set; }
        public decimal TongGiaTriDaNhap { get; set; }
        public decimal TongCongNoPhaiTra { get; set; }
    }

    public class LuuNhaCungCapRequest
    {
        public int MaNcc { get; set; }
        public string? MaCodeNcc { get; set; }
        public string TenNcc { get; set; } = string.Empty;
        public string LoaiNcc { get; set; } = "HANG_CHINH_HANG";
        public string ThuongHieuCungCap { get; set; } = "Đa thương hiệu";
        public decimal PhanTramChietKhauMacDinh { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public string? GhiChu { get; set; }
        public bool DangHoatDong { get; set; } = true;
    }
}
