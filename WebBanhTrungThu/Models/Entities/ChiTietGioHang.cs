using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ChiTietGioHang")]
public partial class ChiTietGioHang
{
    [Key]
    [Column("MaCTGH")]
    public int MaCtgh { get; set; }

    [Column("MaGH")]
    public int MaGh { get; set; }

    public int LoaiSanPham { get; set; }

    public int? MaBanh { get; set; }

    [Column("MaCB")]
    public int? MaCb { get; set; }

    [Column("MaHopTC")]
    public int? MaHopTc { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGiaTamTinh { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayThem { get; set; }

    [ForeignKey("MaBanh")]
    [InverseProperty("ChiTietGioHangs")]
    public virtual SanPhamBanh? MaBanhNavigation { get; set; }

    [ForeignKey("MaCb")]
    [InverseProperty("ChiTietGioHangs")]
    public virtual ComboMauCoSan? MaCbNavigation { get; set; }

    [ForeignKey("MaGh")]
    [InverseProperty("ChiTietGioHangs")]
    public virtual GioHang MaGhNavigation { get; set; } = null!;

    [ForeignKey("MaHopTc")]
    [InverseProperty("ChiTietGioHangs")]
    public virtual HopQuaTuChon? MaHopTcNavigation { get; set; }
}
