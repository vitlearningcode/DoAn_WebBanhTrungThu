using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanhTrungThu.Models.Entities;

[Table("ChiTietKiemKho")]
public partial class ChiTietKiemKho
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaCtkk { get; set; }

    public int MaPkk { get; set; }

    public int MaSp { get; set; }

    public int TonHeThong { get; set; }

    public int TonThucTe { get; set; }

    public int SoLuongLech { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaTriLech { get; set; }

    [StringLength(300)]
    public string? LyDoLech { get; set; }

    [ForeignKey("MaPkk")]
    [InverseProperty("ChiTietKiemKhos")]
    public virtual PhieuKiemKho MaPkkNavigation { get; set; } = null!;

    [ForeignKey("MaSp")]
    public virtual SanPhamBanh MaSpNavigation { get; set; } = null!;
}
