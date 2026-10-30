using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("SanPhamBanh")]
[Index("MaSku", Name = "UQ__SanPhamB__318FCC4919E2174D", IsUnique = true)]
public partial class SanPhamBanh
{
    [Key]
    public int MaBanh { get; set; }

    [Column("MaDM")]
    public int MaDm { get; set; }

    [Column("MaSKU")]
    [StringLength(30)]
    [Unicode(false)]
    public string MaSku { get; set; } = null!;

    [StringLength(200)]
    public string TenBanh { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGiaLe { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaVon { get; set; }

    public int TrongLuongGram { get; set; }

    public int SoTrungMuoi { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string MucDoNgot { get; set; } = null!;

    public bool LaBanhChay { get; set; }

    public bool LaBanhThuongHang { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string NhomHuongVi { get; set; } = null!;

    [StringLength(200)]
    [Unicode(false)]
    public string? TheDoiTuongPhuHop { get; set; }

    [StringLength(500)]
    public string? MoTaNgan { get; set; }

    public string? CauChuyenBanh { get; set; }

    public int TongTonKho { get; set; }

    public bool TrangThaiBan { get; set; }

    [InverseProperty("MaBanhNavigation")]
    public virtual ICollection<ChiTietComboMau> ChiTietComboMaus { get; set; } = new List<ChiTietComboMau>();

    [InverseProperty("MaBanhNavigation")]
    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    [InverseProperty("MaBanhNavigation")]
    public virtual ICollection<ChiTietHopTuChon> ChiTietHopTuChons { get; set; } = new List<ChiTietHopTuChon>();

    [InverseProperty("MaBanhNavigation")]
    public virtual ICollection<CongThucBanhNl> CongThucBanhNls { get; set; } = new List<CongThucBanhNl>();

    [InverseProperty("MaBanhNavigation")]
    public virtual ICollection<LoSanXuatBanh> LoSanXuatBanhs { get; set; } = new List<LoSanXuatBanh>();

    [ForeignKey("MaDm")]
    [InverseProperty("SanPhamBanhs")]
    public virtual DanhMucBanh MaDmNavigation { get; set; } = null!;

    [InverseProperty("MaBanhNavigation")]
    public virtual ThongSoDinhDuong? ThongSoDinhDuong { get; set; }
}
