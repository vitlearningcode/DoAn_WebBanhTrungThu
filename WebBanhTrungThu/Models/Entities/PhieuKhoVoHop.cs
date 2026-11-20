using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

/// <summary>
/// Bảng thứ 37 lưu trữ nhật ký chứng từ Nhập kho / Xuất hủy Vỏ hộp quà tặng trong SQL Server.
/// </summary>
[Table("PhieuKhoVoHop")]
[Index("SoPhieu", Name = "UQ_PhieuKhoVoHop_SoPhieu", IsUnique = true)]
public partial class PhieuKhoVoHop
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaPvh { get; set; }

    [StringLength(50)]
    public string SoPhieu { get; set; } = string.Empty;

    [Column("MaVH")]
    public int MaVh { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string LoaiGiaoDich { get; set; } = "NHAP_VO_HOP";

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGiaNhap { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ThanhTien { get; set; }

    public int? MaNcc { get; set; }

    public int? MaPhieuChi { get; set; }

    public int? MaNdLap { get; set; }

    [StringLength(150)]
    public string NguoiLap { get; set; } = string.Empty;

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

    [ForeignKey("MaVh")]
    [InverseProperty("PhieuKhoVoHops")]
    public virtual MauVoHop MaVhNavigation { get; set; } = null!;

    [ForeignKey("MaNcc")]
    [InverseProperty("PhieuKhoVoHops")]
    public virtual NhaCungCap? MaNccNavigation { get; set; }

    [ForeignKey("MaPhieuChi")]
    [InverseProperty("PhieuKhoVoHops")]
    public virtual PhieuThuChi? MaPhieuChiNavigation { get; set; }

    [ForeignKey("MaNdLap")]
    [InverseProperty("PhieuKhoVoHops")]
    public virtual NguoiDung? MaNdLapNavigation { get; set; }
}
