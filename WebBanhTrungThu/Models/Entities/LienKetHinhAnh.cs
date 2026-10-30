using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("LienKetHinhAnh")]
public partial class LienKetHinhAnh
{
    [Key]
    [Column("MaLK")]
    public int MaLk { get; set; }

    [Column("MaHA")]
    public int MaHa { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string LoaiDoiTuong { get; set; } = null!;

    public int MaDoiTuong { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string VaiTroAnh { get; set; } = null!;

    public int ThuTuHienThi { get; set; }

    [ForeignKey("MaHa")]
    [InverseProperty("LienKetHinhAnhs")]
    public virtual KhoHinhAnh MaHaNavigation { get; set; } = null!;
}
