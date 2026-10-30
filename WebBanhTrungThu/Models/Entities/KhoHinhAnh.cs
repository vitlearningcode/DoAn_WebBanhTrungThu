using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("KhoHinhAnh")]
public partial class KhoHinhAnh
{
    [Key]
    [Column("MaHA")]
    public int MaHa { get; set; }

    [StringLength(200)]
    public string TenTepGoc { get; set; } = null!;

    [StringLength(250)]
    public string? TieuDeAlt { get; set; }

    public int LoaiLuuTru { get; set; }

    [StringLength(500)]
    public string? DuongDanCucBo { get; set; }

    [StringLength(500)]
    public string? DuongDanDamMay { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaFileDrive { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? KichThuocPixel { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTaiLen { get; set; }

    [InverseProperty("MaAnhNenNavigation")]
    public virtual ICollection<CauHinhLandingPage> CauHinhLandingPages { get; set; } = new List<CauHinhLandingPage>();

    [InverseProperty("MaHaNavigation")]
    public virtual ICollection<LienKetHinhAnh> LienKetHinhAnhs { get; set; } = new List<LienKetHinhAnh>();
}
