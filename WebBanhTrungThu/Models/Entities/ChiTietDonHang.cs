using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ChiTietDonHang")]
public partial class ChiTietDonHang
{
    [Key]
    [Column("MaCTDH")]
    public int MaCtdh { get; set; }

    [Column("MaDH")]
    public int MaDh { get; set; }

    [Column("MaDotGH")]
    public int? MaDotGh { get; set; }

    public int LoaiSanPham { get; set; }

    public int? MaBanh { get; set; }

    [Column("MaCB")]
    public int? MaCb { get; set; }

    [Column("MaHopTC")]
    public int? MaHopTc { get; set; }

    [Column("TenSanPham_LuuCung")]
    [StringLength(250)]
    public string TenSanPhamLuuCung { get; set; } = null!;

    [Column("CauHinhHop_JSON")]
    public string? CauHinhHopJson { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGiaLucMua { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ThanhTien { get; set; }

    [ForeignKey("MaDh")]
    [InverseProperty("ChiTietDonHangs")]
    public virtual DonHang MaDhNavigation { get; set; } = null!;

    [ForeignKey("MaDotGh")]
    [InverseProperty("ChiTietDonHangs")]
    public virtual DotGiaoHangDaDiem? MaDotGhNavigation { get; set; }
}
