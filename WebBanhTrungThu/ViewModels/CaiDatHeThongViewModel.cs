using WebBanhTrungThu.Services;

namespace WebBanhTrungThu.ViewModels
{
    public class CauHinhCuaHang
    {
        public string TenDaiLy { get; set; } = "Đại Lý Phân Phối Bánh Trung Thu Nguyệt Cát";
        public string Hotline { get; set; } = "1900 6886 – 0909 123 456";
        public string EmailLienHe { get; set; } = "cskh@nguyetcat.vn";
        public string GioMoCua { get; set; } = "07:30 – 22:00 (Tất cả các ngày trong tuần)";

        public string TenChiNhanh1 { get; set; } = "Chi nhánh TP. Long Xuyên (An Giang)";
        public string DiaChiChiNhanh1 { get; set; } = "Số 88 Trần Hưng Đạo, Phường Mỹ Bình, TP. Long Xuyên, An Giang";
        public string SdtChiNhanh1 { get; set; } = "0296 385 6886";

        public string TenChiNhanh2 { get; set; } = "Chi nhánh Quận 1 (TP. Hồ Chí Minh)";
        public string DiaChiChiNhanh2 { get; set; } = "128 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh";
        public string SdtChiNhanh2 { get; set; } = "028 3829 6886";
    }

    public class CauHinhVanChuyen
    {
        public decimal MocFreeship { get; set; } = 500000m;
        public decimal PhiGiaoTieuChuan { get; set; } = 30000m;
        public decimal PhiGiaoHoaToc6h { get; set; } = 60000m;
        public decimal PhiHoaTocSauFreeship { get; set; } = 30000m;
        public string KhuVucHoTroHoaToc { get; set; } = "TP. Hồ Chí Minh, TP. Long Xuyên (An Giang)";
        public string GhiChuGiaoHang { get; set; } = "Miễn phí giao hàng tiêu chuẩn toàn quốc với đơn hàng từ 500.000đ trở lên. Hỗ trợ giao quà đa điểm kèm thiệp QR 3D.";
    }

    public class CauHinhHoiVienVip
    {
        public decimal SoTienMotDiem { get; set; } = 10000m;
        public decimal GiaTriQuyDoiMotDiem { get; set; } = 1000m;
        public int TranTieuDiemPhanTram { get; set; } = 50;
        public int DiemTangDangKyMoi { get; set; } = 50;

        public int MocBacDiem { get; set; } = 100;
        public decimal PhanTramGiamBac { get; set; } = 3m;

        public int MocVangDiem { get; set; } = 300;
        public decimal PhanTramGiamVang { get; set; } = 5m;

        public int MocKimCuongDiem { get; set; } = 600;
        public decimal PhanTramGiamKimCuong { get; set; } = 10m;
    }

    public class CaiDatHeThongViewModel
    {
        public string MucDangChon { get; set; } = "momo";
        public CauHinhThanhToanQr CauHinhQr { get; set; } = new();
        public string AnhQrMauBase64 { get; set; } = string.Empty;
        public string NoiDungCkMau { get; set; } = "NGUYETCAT TT2026-8888";
        public CauHinhCuaHang CauHinhCuaHang { get; set; } = new();
        public CauHinhVanChuyen CauHinhVanChuyen { get; set; } = new();
        public CauHinhHoiVienVip CauHinhHoiVienVip { get; set; } = new();
    }
}
