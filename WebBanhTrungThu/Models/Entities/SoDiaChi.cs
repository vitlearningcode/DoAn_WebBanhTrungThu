using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("SoDiaChi")]
public partial class SoDiaChi
{
    [Key]
    [Column("MaDC")]
    public int MaDc { get; set; }

    [Column("MaND")]
    public int MaNd { get; set; }

    [StringLength(150)]
    public string TenNguoiNhan { get; set; } = null!;

    [Column("SDTNguoiNhan")]
    [StringLength(15)]
    [Unicode(false)]
    public string SdtnguoiNhan { get; set; } = null!;

    [StringLength(100)]
    public string TinhThanh { get; set; } = null!;

    [StringLength(100)]
    public string QuanHuyen { get; set; } = null!;

    [StringLength(100)]
    public string PhuongXa { get; set; } = null!;

    [StringLength(250)]
    public string DiaChiCuThe { get; set; } = null!;

    [StringLength(50)]
    public string? NhanDiaChi { get; set; }

    public bool LaMacDinh { get; set; }

    [ForeignKey("MaNd")]
    [InverseProperty("SoDiaChis")]
    public virtual NguoiDung MaNdNavigation { get; set; } = null!;
}
