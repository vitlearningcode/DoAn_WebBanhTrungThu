using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("MauVoHop")]
[Index("MaCodeVh", Name = "UQ__MauVoHop__13492AA5703DB7F8", IsUnique = true)]
public partial class MauVoHop
{
    [Key]
    [Column("MaVH")]
    public int MaVh { get; set; }

    [Column("MaCodeVH")]
    [StringLength(30)]
    [Unicode(false)]
    public string MaCodeVh { get; set; } = null!;

    [StringLength(200)]
    public string TenVoHop { get; set; } = null!;

    public int SucChuaBanh { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaVoHopRieng { get; set; }

    [Column("SLBanhThuongHangToiThieu")]
    public int SlbanhThuongHangToiThieu { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string KieuBoCucLuoi { get; set; } = null!;

    [Column("KemTraOLong")]
    public bool KemTraOlong { get; set; }

    [Column("SLTonKhoVo")]
    public int SltonKhoVo { get; set; }

    [StringLength(500)]
    public string? MoTaChatLieu { get; set; }

    public bool TrangThai { get; set; }

    [InverseProperty("MaVhNavigation")]
    public virtual ICollection<ComboMauCoSan> ComboMauCoSans { get; set; } = new List<ComboMauCoSan>();

    [InverseProperty("MaVhNavigation")]
    public virtual ICollection<HopQuaTuChon> HopQuaTuChons { get; set; } = new List<HopQuaTuChon>();
}
