using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanhTrungThu.Models.Entities;

[Table("ChiTietPhieuNhap")]
public partial class ChiTietPhieuNhap
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaCtpn { get; set; }

    public int MaPn { get; set; }

    public int MaSp { get; set; }

    public int? MaLo { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGiaNhap { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ThanhTien { get; set; }

    public DateOnly NgaySanXuat { get; set; }

    public DateOnly HanSuDung { get; set; }

    [ForeignKey("MaPn")]
    [InverseProperty("ChiTietPhieuNhaps")]
    public virtual PhieuNhapKho MaPnNavigation { get; set; } = null!;

    [ForeignKey("MaSp")]
    public virtual SanPhamBanh MaSpNavigation { get; set; } = null!;

    [ForeignKey("MaLo")]
    public virtual LoSanXuatBanh? MaLoNavigation { get; set; }
}
