using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    /// <summary>
    /// Dữ liệu màn hình bán hàng tại quầy (Web POS) – /Admin/BanHangPos/Index
    /// </summary>
    public class BanHangPosViewModel
    {
        public List<SanPhamBanh> DanhSachBanh { get; set; } = new();
        public List<ComboMauCoSan> DanhSachCombo { get; set; } = new();
        public List<MauVoHop> DanhSachVoHop { get; set; } = new();
        public List<DanhMucBanh> DanhSachDanhMuc { get; set; } = new();
        public List<BangChietKhauSiB2b> BangChietKhauSiB2B { get; set; } = new();
        public List<MaKhuyenMai> DanhSachKhuyenMai { get; set; } = new();
        public Dictionary<string, string> TuDienAnh { get; set; } = new();
        public string TenThuNgan { get; set; } = string.Empty;
        public string VaiTroThuNgan { get; set; } = string.Empty;

        /// <summary>
        /// Tên gọi khác của <see cref="BangChietKhauSiB2B"/> (Controller dùng tên này).
        /// </summary>
        public List<BangChietKhauSiB2b> BangChietKhauB2B
        {
            get => BangChietKhauSiB2B;
            set => BangChietKhauSiB2B = value ?? new();
        }
    }

    /// <summary>
    /// Yêu cầu chốt đơn thanh toán tại quầy (POST /Admin/BanHangPos/ThanhToanTaiQuay)
    /// </summary>
    public class PosCheckoutRequest
    {
        public int? MaNd { get; set; }
        public string? HoTenKhach { get; set; }
        public string? SdtKhach { get; set; }
        public string? EmailKhach { get; set; }

        /// <summary>TAI_QUAY hoặc GIAO_HANG_QR</summary>
        public string? CheDoMua { get; set; }

        public bool LaDonDoanhNghiepB2B { get; set; }
        public bool YeuCauInLogo { get; set; }
        public string? TenCongTy { get; set; }
        public string? MaSoThue { get; set; }

        public string? CodeKm { get; set; }
        public string? PhuongThucTt { get; set; }
        public decimal TienKhachDua { get; set; }

        // Thông tin giao hàng & thiệp QR (khi CheDoMua = GIAO_HANG_QR)
        public string? TenNguoiNhan { get; set; }
        public string? SdtNguoiNhan { get; set; }
        public string? DiaChiGiao { get; set; }
        public string? NgayHenGiao { get; set; }
        public string? KhungGioGiao { get; set; }
        public bool CoKemThiepQr { get; set; }
        public string? LoiChucThiep { get; set; }
        public string? MauGiaoDienThiep { get; set; }

        public List<PosCheckoutItem> DanhSachMon { get; set; } = new();
    }

    /// <summary>
    /// Một dòng hàng trên hóa đơn POS. LoaiSanPham: 1 = Bánh lẻ, 2 = Combo, 3 = Hộp tự chọn
    /// </summary>
    public class PosCheckoutItem
    {
        public int LoaiSanPham { get; set; }
        public int? MaBanh { get; set; }
        public int? MaCb { get; set; }
        public int? MaVh { get; set; }
        public string? TenHopTuChon { get; set; }
        public List<int> DanhSachMaBanhTrongHop { get; set; } = new();
        public int SoLuong { get; set; } = 1;
    }
}

