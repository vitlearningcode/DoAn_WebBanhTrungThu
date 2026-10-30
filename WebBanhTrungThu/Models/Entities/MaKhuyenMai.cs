using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("MaKhuyenMai")]
[Index("CodeKm", Name = "UQ__MaKhuyen__C6DE3C7A5A87E297", IsUnique = true)]
public partial class MaKhuyenMai
{
    [Key]
    [Column("MaKM")]
    public int MaKm { get; set; }

    [Column("CodeKM")]
    [StringLength(30)]
    [Unicode(false)]
    public string CodeKm { get; set; } = null!;

    [StringLength(200)]
    public string TenChuongTrinh { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string LoaiGiam { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaTriGiam { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiamToiDa { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonToiThieu { get; set; }

    [Column("SLToiDa")]
    public int SltoiDa { get; set; }

    [Column("SLDaDung")]
    public int SldaDung { get; set; }

    public bool ChoPhepKhachVangLai { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayBatDau { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayKetThuc { get; set; }
}
