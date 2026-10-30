using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("NguyenLieu")]
public partial class NguyenLieu
{
    [Key]
    [Column("MaNL")]
    public int MaNl { get; set; }

    [Column("TenNL")]
    [StringLength(150)]
    public string TenNl { get; set; } = null!;

    [StringLength(100)]
    public string? XuatXu { get; set; }

    public bool LaNguyenLieuChay { get; set; }

    public bool LaTacNhanDiUng { get; set; }

    [InverseProperty("MaNlNavigation")]
    public virtual ICollection<CongThucBanhNl> CongThucBanhNls { get; set; } = new List<CongThucBanhNl>();
}
