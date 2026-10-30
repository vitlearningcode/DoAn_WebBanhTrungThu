using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[PrimaryKey("MaBanh", "MaNl")]
[Table("CongThucBanh_NL")]
public partial class CongThucBanhNl
{
    [Key]
    public int MaBanh { get; set; }

    [Key]
    [Column("MaNL")]
    public int MaNl { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? TyLePhanTram { get; set; }

    [StringLength(100)]
    public string? VaiTroThanhPhan { get; set; }

    [ForeignKey("MaBanh")]
    [InverseProperty("CongThucBanhNls")]
    public virtual SanPhamBanh MaBanhNavigation { get; set; } = null!;

    [ForeignKey("MaNl")]
    [InverseProperty("CongThucBanhNls")]
    public virtual NguyenLieu MaNlNavigation { get; set; } = null!;
}
