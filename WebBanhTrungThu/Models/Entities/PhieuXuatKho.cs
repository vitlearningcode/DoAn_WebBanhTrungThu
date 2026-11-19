using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("PhieuXuatKho")]
[Index("SoPhieuXuat", Name = "UQ_PhieuXuatKho_SoPhieuXuat", IsUnique = true)]
public partial class PhieuXuatKho
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaPx { get; set; }

    [StringLength(50)]
    public string SoPhieuXuat { get; set; } = string.Empty;

    [StringLength(40)]
    [Unicode(false)]
    public string LoaiXuat { get; set; } = "XUAT_HUY_LOI";

    [StringLength(250)]
    public string DoiTuongLienQuan { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime NgayXuat { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongGiaTriVon { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string TrangThai { get; set; } = "HOAN_THANH";

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [StringLength(150)]
    public string NguoiLap { get; set; } = string.Empty;

    public int? MaNdLap { get; set; }

    [ForeignKey("MaNdLap")]
    public virtual NguoiDung? MaNdLapNavigation { get; set; }

    [InverseProperty("MaPxNavigation")]
    public virtual ICollection<ChiTietPhieuXuat> ChiTietPhieuXuats { get; set; } = new List<ChiTietPhieuXuat>();
}
