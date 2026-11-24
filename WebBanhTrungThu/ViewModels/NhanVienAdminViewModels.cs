namespace WebBanhTrungThu.ViewModels
{
    public class DonHangNhanVienTomTatViewModel
    {
        public int MaDh { get; set; }
        public string MaTraCuuDh { get; set; } = string.Empty;
        public string HoTenNguoiMua { get; set; } = string.Empty;
        public string SdtNguoiMua { get; set; } = string.Empty;
        public DateTime NgayDat { get; set; }
        public decimal TongThanhToanCuoi { get; set; }
        public string PhuongThucTt { get; set; } = string.Empty;
        public string TrangThaiDh { get; set; } = string.Empty;
        public string TrangThaiTt { get; set; } = string.Empty;
        public string TenTrangThaiDhHienThi { get; set; } = string.Empty;
        public string TenPhuongThucTtHienThi => Models.Entities.DonHang.AnhXaPhuongThucTtHienThi(PhuongThucTt);
    }

    public class NhanVienItemViewModel
    {
        public int MaNv { get; set; }
        public int MaNd { get; set; }
        public string MaCodeNv { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string Sdt { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MaVt { get; set; } = "NHAN_VIEN";
        public string TenVaiTroHienThi => MaVt == "ADMIN" ? "Chủ cửa hàng" : "Nhân viên Bán hàng";
        public string ChucVu { get; set; } = "Nhân viên Bán hàng";
        public string ChiNhanh { get; set; } = "TP. Long Xuyên";
        public string CaLamViec { get; set; } = "Toàn thời gian";
        public DateOnly NgayVaoLam { get; set; }
        public string TrangThaiLamViec { get; set; } = "DANG_LAM_VIEC";
        public bool TrangThaiTaiKhoan { get; set; } = true;
        public bool DangLamViec => TrangThaiTaiKhoan && TrangThaiLamViec == "DANG_LAM_VIEC";
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }

        public int TongDonDaLap { get; set; }
        public int SoDonHopLe { get; set; }
        public decimal TongDoanhSo { get; set; }
        public List<DonHangNhanVienTomTatViewModel> DanhSachDonGanDay { get; set; } = new();
    }

    public class NhanVienAdminViewModel
    {
        public List<NhanVienItemViewModel> DanhSachNhanVien { get; set; } = new();
        public List<string> DanhSachChiNhanh { get; set; } = new();
        public List<string> DanhSachCaLamViec { get; set; } = new();

        public string? TuKhoa { get; set; }
        public string LocVaiTro { get; set; } = "ALL";
        public string LocChiNhanh { get; set; } = "ALL";
        public string LocCaLamViec { get; set; } = "ALL";
        public string LocTrangThai { get; set; } = "ALL";
        public int? MaNdDangNhap { get; set; }

        public int TongNhanSu { get; set; }
        public int SoDangLamViec { get; set; }
        public int SoNhanVienBanHang { get; set; }
        public int SoDangTamKhoa { get; set; }
    }

    public class LuuNhanVienRequest
    {
        public int MaNv { get; set; }
        public int MaNd { get; set; }
        public string? MaCodeNv { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string Sdt { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MaVt { get; set; } = "NHAN_VIEN";
        public string ChiNhanh { get; set; } = "TP. Long Xuyên";
        public string CaLamViec { get; set; } = "Toàn thời gian";
        public DateOnly? NgayVaoLam { get; set; }
        public string TrangThaiLamViec { get; set; } = "DANG_LAM_VIEC";
        public string? MatKhauKhoiTao { get; set; }
        public string? GhiChu { get; set; }
    }

    public class DatLaiMatKhauNvRequest
    {
        public int MaNd { get; set; }
        public string MatKhauMoi { get; set; } = string.Empty;
        public string? XacNhanMatKhau { get; set; }
    }
}
