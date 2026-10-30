using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("DanhMucBanh")]
[Index("DuongDanSlug", Name = "UQ__DanhMucB__BD2F66935ED2523D", IsUnique = true)]
public partial class DanhMucBanh
{
    [Key]
    [Column("MaDM")]
    public int MaDm { get; set; }

    [Column("MaDMCha")]
    public int? MaDmcha { get; set; }

    [Column("TenDM")]
    [StringLength(150)]
    public string TenDm { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string DuongDanSlug { get; set; } = null!;

    [StringLength(500)]
    public string? MoTa { get; set; }

    [Column("ThuTuSX")]
    public int ThuTuSx { get; set; }

    public bool TrangThai { get; set; }

    [InverseProperty("MaDmchaNavigation")]
    public virtual ICollection<DanhMucBanh> InverseMaDmchaNavigation { get; set; } = new List<DanhMucBanh>();

    [ForeignKey("MaDmcha")]
    [InverseProperty("InverseMaDmchaNavigation")]
    public virtual DanhMucBanh? MaDmchaNavigation { get; set; }

    [InverseProperty("MaDmNavigation")]
    public virtual ICollection<SanPhamBanh> SanPhamBanhs { get; set; } = new List<SanPhamBanh>();
}
