using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class SanPhamLienQuanItemViewModel
    {
        public int MaBanh { get; set; }
        public string TenBanh { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = string.Empty;
        public string TenDanhMuc { get; set; } = string.Empty;
        public string AnhDaiDien { get; set; } = "/images/default/banh-mac-dinh.png";
        public decimal GiaGoc { get; set; }
        public decimal GiaBanThucTe { get; set; }
        public bool DangGiamCanDate { get; set; }
        public decimal PhanTramGiamCanDate { get; set; }
        public int NangLuongCalo { get; set; }
        public int ChiSoGi { get; set; }
        public bool LaBanhThuongHang { get; set; }
        public bool LaBanhChay { get; set; }
        public bool LaIsomalt { get; set; }
        public int TrongLuongGram { get; set; }
    }

    public class ChiTietSanPhamViewModel
    {
        public SanPhamBanh SanPham { get; set; } = null!;
        public string TenDanhMuc { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = string.Empty;
        public string NhaCungCap { get; set; } = string.Empty;
        public string AnhChinh { get; set; } = "/images/default/banh-mac-dinh.png";
        public List<string> DanhSachAnh { get; set; } = new();
        public decimal GiaGoc { get; set; }
        public decimal GiaBanThucTe { get; set; }
        public decimal PhanTramGiamCanDate { get; set; }
        public bool DangGiamCanDate { get; set; }
        public int TonKhoKhaDung { get; set; }
        public bool ConHang { get; set; }
        public LoSanXuatBanh? LoFefoGanNhat { get; set; }
        public int? SoNgayConLaiHsd { get; set; }
        public ThongSoDinhDuong? ThongSoDinhDuong { get; set; }
        public string NhanMucDoNgot { get; set; } = "Ngọt hài hòa truyền thống";
        public string GoiYThuongTra { get; set; } = string.Empty;
        public string HuongDanBaoQuan { get; set; } = string.Empty;
        public List<SanPhamLienQuanItemViewModel> SanPhamLienQuan { get; set; } = new();
    }
}
