using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("GioHang")]
public partial class GioHang
{
    [Key]
    [Column("MaGH")]
    public int MaGh { get; set; }

    [Column("MaND")]
    public int? MaNd { get; set; }

    [Column("MaPhienKVL")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MaPhienKvl { get; set; }

    [Column("CodeKMDangApDung")]
    [StringLength(30)]
    [Unicode(false)]
    public string? CodeKmdangApDung { get; set; }

    [Column("LaCheDoMuaSiB2B")]
    public bool LaCheDoMuaSiB2b { get; set; }

    [Column("YeuCauInLogoB2B")]
    public bool YeuCauInLogoB2b { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayCapNhat { get; set; }

    [InverseProperty("MaGhNavigation")]
    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    [ForeignKey("MaNd")]
    [InverseProperty("GioHangs")]
    public virtual NguoiDung? MaNdNavigation { get; set; }

    [ForeignKey("MaPhienKvl")]
    [InverseProperty("GioHangs")]
    public virtual PhienKhachVangLai? MaPhienKvlNavigation { get; set; }
}
