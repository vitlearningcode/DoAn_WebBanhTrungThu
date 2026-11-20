using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("PhieuKiemKho")]
[Index("SoPhieuKiem", Name = "UQ_PhieuKiemKho_SoPhieuKiem", IsUnique = true)]
public partial class PhieuKiemKho
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaPkk { get; set; }

    [StringLength(50)]
    public string SoPhieuKiem { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime NgayKiem { get; set; } = DateTime.Now;

    [StringLength(150)]
    public string NguoiKiem { get; set; } = string.Empty;

    [StringLength(30)]
    [Unicode(false)]
    public string TrangThai { get; set; } = "PHIEU_TAM";

    public int TongLechTang { get; set; }

    public int TongLechGiam { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongGiaTriLech { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    public int? MaPxLienKet { get; set; }

    public int? MaNdKiem { get; set; }

    [ForeignKey("MaPxLienKet")]
    public virtual PhieuXuatKho? MaPxLienKetNavigation { get; set; }

    [ForeignKey("MaNdKiem")]
    [InverseProperty("PhieuKiemKhos")]
    public virtual NguoiDung? MaNdKiemNavigation { get; set; }

    [InverseProperty("MaPkkNavigation")]
    public virtual ICollection<ChiTietKiemKho> ChiTietKiemKhos { get; set; } = new List<ChiTietKiemKho>();
}
