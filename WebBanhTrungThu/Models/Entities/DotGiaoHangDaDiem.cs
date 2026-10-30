using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("DotGiaoHang_DaDiem")]
[Index("MaTokenThiepQr", Name = "UQ__DotGiaoH__CA775307E7414AFD", IsUnique = true)]
public partial class DotGiaoHangDaDiem
{
    [Key]
    [Column("MaDotGH")]
    public int MaDotGh { get; set; }

    [Column("MaDH")]
    public int MaDh { get; set; }

    [StringLength(150)]
    public string TenNguoiNhan { get; set; } = null!;

    [Column("SDTNguoiNhan")]
    [StringLength(15)]
    [Unicode(false)]
    public string SdtnguoiNhan { get; set; } = null!;

    [StringLength(500)]
    public string DiaChiGiaoChiTiet { get; set; } = null!;

    public DateOnly NgayHenGiao { get; set; }

    [StringLength(50)]
    public string KhungGioGiao { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PhiShipDotNay { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string TrangThaiGiao { get; set; } = null!;

    [Column("CoKemThiepQR")]
    public bool CoKemThiepQr { get; set; }

    [Column("MaTokenThiepQR")]
    [StringLength(50)]
    [Unicode(false)]
    public string? MaTokenThiepQr { get; set; }

    [StringLength(1000)]
    public string? LoiChucThiep { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? MauGiaoDienThiep { get; set; }

    public int SoLanMoThiep { get; set; }

    [InverseProperty("MaDotGhNavigation")]
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [ForeignKey("MaDh")]
    [InverseProperty("DotGiaoHangDaDiems")]
    public virtual DonHang MaDhNavigation { get; set; } = null!;
}
