using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ComboMauCoSan")]
[Index("MaCodeCb", Name = "UQ__ComboMau__13468825C0736A5E", IsUnique = true)]
public partial class ComboMauCoSan
{
    [Key]
    [Column("MaCB")]
    public int MaCb { get; set; }

    [Column("MaCodeCB")]
    [StringLength(30)]
    [Unicode(false)]
    public string MaCodeCb { get; set; } = null!;

    [StringLength(200)]
    public string TenCombo { get; set; } = null!;

    [Column("MaVH")]
    public int MaVh { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaNiemYet { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaUuDaiCombo { get; set; }

    [Column("ThongDiepYNgia")]
    [StringLength(500)]
    public string? ThongDiepYngia { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? TheDoiTuongPhuHop { get; set; }

    public bool TrangThai { get; set; }

    public int? MaThuongHieu { get; set; }

    [Column("MaDM")]
    public int? MaDm { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? MaVach { get; set; }

    [StringLength(100)]
    public string ViTriKho { get; set; } = "Kệ B1";

    [StringLength(50)]
    public string DonViTinh { get; set; } = "Hộp";

    public bool BanTrucTiep { get; set; } = true;

    public int TongTonKho { get; set; } = 40;

    public int TrongLuongGram { get; set; } = 600;

    [InverseProperty("MaCbNavigation")]
    public virtual ICollection<ChiTietComboMau> ChiTietComboMaus { get; set; } = new List<ChiTietComboMau>();

    [InverseProperty("MaCbNavigation")]
    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    [ForeignKey("MaVh")]
    [InverseProperty("ComboMauCoSans")]
    public virtual MauVoHop MaVhNavigation { get; set; } = null!;

    [ForeignKey("MaThuongHieu")]
    [InverseProperty("ComboMauCoSans")]
    public virtual ThuongHieu? MaThuongHieuNavigation { get; set; }

    [ForeignKey("MaDm")]
    [InverseProperty("ComboMauCoSans")]
    public virtual DanhMucBanh? MaDmNavigation { get; set; }
}
