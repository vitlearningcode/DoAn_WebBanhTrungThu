using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ComboMauCoSan")]
[Index("MaCodeCb", Name = "UQ__ComboMau__13468825C0736A5E", IsUnique = true)]
public partial class ComboMauCoSan
{
    [Key]
    [Column("MaCB")]
    public int MaCb { get; set; }

    [Column("MaCodeCB")]
    [StringLength(30)]
    [Unicode(false)]
    public string MaCodeCb { get; set; } = null!;

    [StringLength(200)]
    public string TenCombo { get; set; } = null!;

    [Column("MaVH")]
    public int MaVh { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaNiemYet { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaUuDaiCombo { get; set; }

    [Column("ThongDiepYNgia")]
    [StringLength(500)]
    public string? ThongDiepYngia { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? TheDoiTuongPhuHop { get; set; }

    public bool TrangThai { get; set; }

    [InverseProperty("MaCbNavigation")]
    public virtual ICollection<ChiTietComboMau> ChiTietComboMaus { get; set; } = new List<ChiTietComboMau>();

    [InverseProperty("MaCbNavigation")]
    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    [ForeignKey("MaVh")]
    [InverseProperty("ComboMauCoSans")]
    public virtual MauVoHop MaVhNavigation { get; set; } = null!;
}
