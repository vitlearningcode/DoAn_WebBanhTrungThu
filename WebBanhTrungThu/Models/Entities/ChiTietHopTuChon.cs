using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ChiTietHopTuChon")]
public partial class ChiTietHopTuChon
{
    [Key]
    [Column("MaCTHopTC")]
    public int MaCthopTc { get; set; }

    [Column("MaHopTC")]
    public int MaHopTc { get; set; }

    public int MaBanh { get; set; }

    [Column("ViTriOTrongHop")]
    public int ViTriOtrongHop { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGiaBanhLucChon { get; set; }

    [ForeignKey("MaBanh")]
    [InverseProperty("ChiTietHopTuChons")]
    public virtual SanPhamBanh MaBanhNavigation { get; set; } = null!;

    [ForeignKey("MaHopTc")]
    [InverseProperty("ChiTietHopTuChons")]
    public virtual HopQuaTuChon MaHopTcNavigation { get; set; } = null!;
}
