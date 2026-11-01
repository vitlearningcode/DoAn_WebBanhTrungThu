using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanhTrungThu.Models.Entities
{
    public partial class SanPhamBanh
    {
        [NotMapped]
        public string MaVach { get; set; } = string.Empty;

        [NotMapped]
        public string ThuongHieu { get; set; } = "Đồng Khánh";

        [NotMapped]
        public string NhaCungCap { get; set; } = "Lò Bánh Đồng Khánh Chính Gốc";

        [NotMapped]
        public string ViTriKho { get; set; } = "Kệ A1";

        [NotMapped]
        public string DonViTinh { get; set; } = "Cái";

        [NotMapped]
        public bool BanTrucTiep { get; set; } = true;
    }

    public partial class ComboMauCoSan
    {
        [NotMapped]
        public string MaVach { get; set; } = string.Empty;

        [NotMapped]
        public string ThuongHieu { get; set; } = "Kinh Đô";

        [NotMapped]
        public string NhaCungCap { get; set; } = "NPP Bánh Kinh Đô Miền Nam";

        [NotMapped]
        public string ViTriKho { get; set; } = "Kệ B1";

        [NotMapped]
        public string DonViTinh { get; set; } = "Hộp";

        [NotMapped]
        public bool BanTrucTiep { get; set; } = true;

        [NotMapped]
        public int MaDm { get; set; } = 1;

        [NotMapped]
        public string TenDm { get; set; } = "ĐỒNG GIÁ 20K";

        [NotMapped]
        public int TongTonKho { get; set; } = 45;

        [NotMapped]
        public int TrongLuongGram { get; set; } = 600;
    }
}

