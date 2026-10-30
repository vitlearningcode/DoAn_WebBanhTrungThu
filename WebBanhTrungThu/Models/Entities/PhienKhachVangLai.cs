using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("PhienKhachVangLai")]
public partial class PhienKhachVangLai
{
    [Key]
    [Column("MaPhienKVL")]
    [StringLength(64)]
    [Unicode(false)]
    public string MaPhienKvl { get; set; } = null!;

    [Column("DiaChiIP")]
    [StringLength(50)]
    [Unicode(false)]
    public string? DiaChiIp { get; set; }

    [StringLength(250)]
    public string? ThongTinTrinhDuyet { get; set; }

    [Column("MaND_DaGop")]
    public int? MaNdDaGop { get; set; }

    public bool DaGopGioHang { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayHetHan { get; set; }

    [InverseProperty("MaPhienKvlNavigation")]
    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    [InverseProperty("MaPhienKvlNavigation")]
    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    [InverseProperty("MaPhienKvlNavigation")]
    public virtual ICollection<HopQuaTuChon> HopQuaTuChons { get; set; } = new List<HopQuaTuChon>();

    [ForeignKey("MaNdDaGop")]
    [InverseProperty("PhienKhachVangLais")]
    public virtual NguoiDung? MaNdDaGopNavigation { get; set; }
}
