using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

/// <summary>
/// Bảng vật lý lưu trữ Thiệp Chúc Mừng Tết Đoàn Viên QR 3D trong SQL Server.
/// </summary>
[Table("ThiepChucMung3D")]
[Index("MaTokenQr", Name = "UQ_ThiepChucMung3D_MaTokenQr", IsUnique = true)]
public partial class ThiepChucMung3d
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaThiep { get; set; }

    [StringLength(60)]
    public string MaTokenQr { get; set; } = string.Empty;

    public int MaDonHang { get; set; }

    public int? MaDotGiao { get; set; }

    [StringLength(150)]
    public string TenNguoiGui { get; set; } = string.Empty;

    [StringLength(150)]
    public string TenNguoiNhan { get; set; } = string.Empty;

    [StringLength(1000)]
    public string LoiChuc { get; set; } = string.Empty;

    [StringLength(100)]
    public string MauGiaoDien { get; set; } = "CO_PHONG_HOANG_GIA";

    public int SoLanMoThiep { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [ForeignKey("MaDonHang")]
    [InverseProperty("ThiepChucMung3ds")]
    public virtual DonHang MaDonHangNavigation { get; set; } = null!;

    [ForeignKey("MaDotGiao")]
    [InverseProperty("ThiepChucMung3ds")]
    public virtual DotGiaoHangDaDiem? MaDotGiaoNavigation { get; set; }
}
