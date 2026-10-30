using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("LoSanXuatBanh")]
[Index("MaLoCode", Name = "UQ__LoSanXua__CC35FE68121B9084", IsUnique = true)]
public partial class LoSanXuatBanh
{
    [Key]
    public int MaLo { get; set; }

    public int MaBanh { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string MaLoCode { get; set; } = null!;

    public DateOnly NgaySanXuat { get; set; }

    public DateOnly HanSuDung { get; set; }

    [Column("SLNhapBanDau")]
    public int SlnhapBanDau { get; set; }

    [Column("SLConLai")]
    public int SlconLai { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PhanTramGiamCanDate { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string TrangThaiLo { get; set; } = null!;

    [ForeignKey("MaBanh")]
    [InverseProperty("LoSanXuatBanhs")]
    public virtual SanPhamBanh MaBanhNavigation { get; set; } = null!;
}
