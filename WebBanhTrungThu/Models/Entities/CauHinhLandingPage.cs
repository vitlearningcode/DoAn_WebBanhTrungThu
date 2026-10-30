using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("CauHinhLandingPage")]
[Index("MaCodeKhuVuc", Name = "UQ__CauHinhL__BA092D99AF1FBDF8", IsUnique = true)]
public partial class CauHinhLandingPage
{
    [Key]
    public int MaPhanKhu { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string MaCodeKhuVuc { get; set; } = null!;

    [StringLength(250)]
    public string TieuDeChinh { get; set; } = null!;

    [StringLength(500)]
    public string? TieuDePhu { get; set; }

    public string? NoiDungMoTa { get; set; }

    public int? MaAnhNen { get; set; }

    [Column("MaAnhPhu_Parallax")]
    public int? MaAnhPhuParallax { get; set; }

    [Column("ChuNutBamCTA")]
    [StringLength(100)]
    public string? ChuNutBamCta { get; set; }

    [Column("DuongDanCTA")]
    [StringLength(250)]
    [Unicode(false)]
    public string? DuongDanCta { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? MocThoiGianDemNguoc { get; set; }

    public int ThuTuHienThi { get; set; }

    public bool DangKichHoat { get; set; }

    [ForeignKey("MaAnhNen")]
    [InverseProperty("CauHinhLandingPages")]
    public virtual KhoHinhAnh? MaAnhNenNavigation { get; set; }
}
