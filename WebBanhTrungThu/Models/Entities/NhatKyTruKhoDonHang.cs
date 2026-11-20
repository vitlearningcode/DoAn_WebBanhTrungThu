using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("NhatKyTruKhoDonHang")]
public partial class NhatKyTruKhoDonHang
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaNk { get; set; }

    [Column("MaDH")]
    public int MaDh { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string LoaiDoiTuong { get; set; } = "LO_BANH";

    public int? MaLo { get; set; }

    public int? MaBanh { get; set; }

    [Column("MaVH")]
    public int? MaVh { get; set; }

    public int SoLuongDaTru { get; set; }

    public bool DaHoanKho { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayGhi { get; set; } = DateTime.Now;

    [ForeignKey("MaDh")]
    [InverseProperty("NhatKyTruKhoDonHangs")]
    public virtual DonHang MaDhNavigation { get; set; } = null!;

    [ForeignKey("MaLo")]
    [InverseProperty("NhatKyTruKhoDonHangs")]
    public virtual LoSanXuatBanh? MaLoNavigation { get; set; }

    [ForeignKey("MaBanh")]
    [InverseProperty("NhatKyTruKhoDonHangs")]
    public virtual SanPhamBanh? MaBanhNavigation { get; set; }

    [ForeignKey("MaVh")]
    [InverseProperty("NhatKyTruKhoDonHangs")]
    public virtual MauVoHop? MaVhNavigation { get; set; }
}
