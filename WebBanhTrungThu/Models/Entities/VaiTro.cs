using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("VaiTro")]
public partial class VaiTro
{
    [Key]
    [Column("MaVT")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaVt { get; set; } = null!;

    [Column("TenVT")]
    [StringLength(100)]
    public string TenVt { get; set; } = null!;

    [StringLength(250)]
    public string? MoTa { get; set; }

    [InverseProperty("MaVtNavigation")]
    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
