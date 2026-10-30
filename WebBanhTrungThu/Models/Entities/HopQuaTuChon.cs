using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("HopQuaTuChon")]
public partial class HopQuaTuChon
{
    [Key]
    [Column("MaHopTC")]
    public int MaHopTc { get; set; }

    [Column("MaVH")]
    public int MaVh { get; set; }

    [Column("MaND")]
    public int? MaNd { get; set; }

    [Column("MaPhienKVL")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MaPhienKvl { get; set; }

    [StringLength(150)]
    public string TenHopKhachDat { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string MaBamCauHinh { get; set; } = null!;

    public int TongCaloHop { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongGiaMotHop { get; set; }

    public bool DaLuuMauYeuThich { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    [InverseProperty("MaHopTcNavigation")]
    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    [InverseProperty("MaHopTcNavigation")]
    public virtual ICollection<ChiTietHopTuChon> ChiTietHopTuChons { get; set; } = new List<ChiTietHopTuChon>();

    [ForeignKey("MaNd")]
    [InverseProperty("HopQuaTuChons")]
    public virtual NguoiDung? MaNdNavigation { get; set; }

    [ForeignKey("MaPhienKvl")]
    [InverseProperty("HopQuaTuChons")]
    public virtual PhienKhachVangLai? MaPhienKvlNavigation { get; set; }

    [ForeignKey("MaVh")]
    [InverseProperty("HopQuaTuChons")]
    public virtual MauVoHop MaVhNavigation { get; set; } = null!;
}
