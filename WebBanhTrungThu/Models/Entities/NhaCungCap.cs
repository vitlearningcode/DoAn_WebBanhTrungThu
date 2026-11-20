using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("NhaCungCap")]
[Index("MaCodeNcc", Name = "UQ_NhaCungCap_MaCodeNcc", IsUnique = true)]
public partial class NhaCungCap
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaNcc { get; set; }

    [StringLength(30)]
    public string MaCodeNcc { get; set; } = string.Empty;

    [StringLength(200)]
    public string TenNcc { get; set; } = string.Empty;

    [StringLength(50)]
    public string LoaiNcc { get; set; } = "HANG_CHINH_HANG";

    [StringLength(100)]
    public string ThuongHieuCungCap { get; set; } = "Đa thương hiệu";

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PhanTramChietKhauMacDinh { get; set; }

    [StringLength(20)]
    public string? SoDienThoai { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? DiaChi { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    public bool DangHoatDong { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [InverseProperty("MaNccNavigation")]
    public virtual ICollection<PhieuNhapKho> PhieuNhapKhos { get; set; } = new List<PhieuNhapKho>();

    [InverseProperty("MaNccMacDinhNavigation")]
    public virtual ICollection<SanPhamBanh> SanPhamBanhs { get; set; } = new List<SanPhamBanh>();
}
