namespace WebBanhTrungThu.ViewModels
{
    public class LuuHopTuChonRequest
    {
        public int MaVh { get; set; }
        public string TenHopKhachDat { get; set; } = "Hộp Quà Đoàn Viên Tự Phối";
        public List<int> DanhSachMaBanh { get; set; } = new();
        public int SoLuongHop { get; set; } = 1;
    }

    public class ThemMonLeRequest
    {
        /// <summary>
        /// 1 = Bánh lẻ (SanPhamBanh), 2 = Combo mẫu có sẵn (ComboMauCoSan)
        /// </summary>
        public int LoaiSanPham { get; set; } = 1;
        public int MaSanPham { get; set; }
        public int SoLuong { get; set; } = 1;
    }

    public class CapNhatSoLuongGioRequest
    {
        public int MaCtgh { get; set; }
        public int SoLuongMoi { get; set; }
    }
}

