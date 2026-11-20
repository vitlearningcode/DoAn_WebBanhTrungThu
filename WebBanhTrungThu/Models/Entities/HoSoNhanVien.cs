using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("HoSoNhanVien")]
[Index("MaNd", Name = "UQ_HoSoNhanVien_MaNd", IsUnique = true)]
[Index("MaCodeNv", Name = "UQ_HoSoNhanVien_MaCodeNv", IsUnique = true)]
public partial class HoSoNhanVien
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaNv { get; set; }

    public int MaNd { get; set; }

    [StringLength(20)]
    public string MaCodeNv { get; set; } = string.Empty;

    [StringLength(100)]
    public string ChucVu { get; set; } = "Nhân viên Bán hàng";

    [StringLength(150)]
    public string ChiNhanh { get; set; } = "TP. Long Xuyên";

    [StringLength(100)]
    public string CaLamViec { get; set; } = "Toàn thời gian";

    [Column(TypeName = "date")]
    public DateOnly NgayVaoLam { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(30)]
    [Unicode(false)]
    public string TrangThaiLamViec { get; set; } = "DANG_LAM_VIEC";

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaNd")]
    [InverseProperty("HoSoNhanVien")]
    public virtual NguoiDung MaNdNavigation { get; set; } = null!;
}
