using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("PhieuNhapKho")]
[Index("SoPhieuNhap", Name = "UQ_PhieuNhapKho_SoPhieuNhap", IsUnique = true)]
public partial class PhieuNhapKho
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaPn { get; set; }

    [StringLength(50)]
    public string SoPhieuNhap { get; set; } = string.Empty;

    public int? MaNcc { get; set; }

    [StringLength(200)]
    public string NhaCungCap { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime NgayNhap { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongTienHang { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiamGia { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongPhaiTra { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DaThanhToan { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string TrangThai { get; set; } = "PHIEU_TAM";

    [StringLength(30)]
    [Unicode(false)]
    public string TrangThaiThanhToan { get; set; } = "CHUA_THANH_TOAN";

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [StringLength(150)]
    public string NguoiLap { get; set; } = string.Empty;

    public int? MaNdLap { get; set; }

    [ForeignKey("MaNcc")]
    [InverseProperty("PhieuNhapKhos")]
    public virtual NhaCungCap? MaNccNavigation { get; set; }

    [ForeignKey("MaNdLap")]
    public virtual NguoiDung? MaNdLapNavigation { get; set; }

    [InverseProperty("MaPnNavigation")]
    public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();

    [InverseProperty("MaPhieuNhapLienKetNavigation")]
    public virtual ICollection<PhieuThuChi> PhieuThuChis { get; set; } = new List<PhieuThuChi>();
}
