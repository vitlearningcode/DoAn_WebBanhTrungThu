using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanhTrungThu.Models.Entities;

[Table("ChiTietPhieuXuat")]
public partial class ChiTietPhieuXuat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaCtpx { get; set; }

    public int MaPx { get; set; }

    public int MaSp { get; set; }

    public int? MaLo { get; set; }

    public int SoLuongXuat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaVonLucXuat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ThanhTienVon { get; set; }

    [StringLength(300)]
    public string? GhiChuDong { get; set; }

    [ForeignKey("MaPx")]
    [InverseProperty("ChiTietPhieuXuats")]
    public virtual PhieuXuatKho MaPxNavigation { get; set; } = null!;

    [ForeignKey("MaSp")]
    public virtual SanPhamBanh MaSpNavigation { get; set; } = null!;

    [ForeignKey("MaLo")]
    public virtual LoSanXuatBanh? MaLoNavigation { get; set; }
}
