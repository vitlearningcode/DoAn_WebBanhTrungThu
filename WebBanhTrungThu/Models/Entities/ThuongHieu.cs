using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ThuongHieu")]
[Index("MaCodeThuongHieu", Name = "UQ_ThuongHieu_MaCodeThuongHieu", IsUnique = true)]
[Index("TenThuongHieu", Name = "UQ_ThuongHieu_TenThuongHieu", IsUnique = true)]
public partial class ThuongHieu
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaThuongHieu { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string MaCodeThuongHieu { get; set; } = string.Empty;

    [StringLength(120)]
    public string TenThuongHieu { get; set; } = string.Empty;

    [StringLength(150)]
    public string? XuatXu { get; set; }

    [StringLength(500)]
    public string? MoTa { get; set; }

    public bool DangHoatDong { get; set; } = true;

    [InverseProperty("MaThuongHieuNavigation")]
    public virtual ICollection<SanPhamBanh> SanPhamBanhs { get; set; } = new List<SanPhamBanh>();

    [InverseProperty("MaThuongHieuNavigation")]
    public virtual ICollection<ComboMauCoSan> ComboMauCoSans { get; set; } = new List<ComboMauCoSan>();
}
