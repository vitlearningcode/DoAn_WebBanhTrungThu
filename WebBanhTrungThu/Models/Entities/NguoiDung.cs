using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("NguoiDung")]
[Index("Email", Name = "UQ__NguoiDun__A9D10534E93C524D", IsUnique = true)]
[Index("Sdt", Name = "UQ__NguoiDun__CA1930A5922A5960", IsUnique = true)]
public partial class NguoiDung
{
    [Key]
    [Column("MaND")]
    public int MaNd { get; set; }

    [Column("MaVT")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaVt { get; set; } = null!;

    [StringLength(150)]
    public string HoTen { get; set; } = null!;

    [Column("SDT")]
    [StringLength(15)]
    [Unicode(false)]
    public string Sdt { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [StringLength(500)]
    [Unicode(false)]
    public string MatKhauHash { get; set; } = null!;

    public int DiemTichLuy { get; set; }

    [Column("HangTV")]
    [StringLength(30)]
    public string HangTv { get; set; } = null!;

    public bool TrangThai { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    [InverseProperty("MaNdNavigation")]
    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    [InverseProperty("MaNdNavigation")]
    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    [InverseProperty("MaNdNavigation")]
    public virtual ICollection<HopQuaTuChon> HopQuaTuChons { get; set; } = new List<HopQuaTuChon>();

    [ForeignKey("MaVt")]
    [InverseProperty("NguoiDungs")]
    public virtual VaiTro MaVtNavigation { get; set; } = null!;

    [InverseProperty("MaNdDaGopNavigation")]
    public virtual ICollection<PhienKhachVangLai> PhienKhachVangLais { get; set; } = new List<PhienKhachVangLai>();

    [InverseProperty("MaNdNavigation")]
    public virtual ICollection<SoDiaChi> SoDiaChis { get; set; } = new List<SoDiaChi>();
}
