using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class HoSoThanhVienViewModel
    {
        public NguoiDung NguoiDung { get; set; } = null!;
        public List<SoDiaChi> DanhSachSoDiaChi { get; set; } = new();
        public List<DonHang> LichSuDonHang { get; set; } = new();
        public List<HopQuaTuChon> DanhSachHopTuChon { get; set; } = new();

        public decimal GiaTriQuyDoiDiem => NguoiDung.DiemTichLuy * 1000m;
    }
}

