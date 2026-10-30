using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("BangChietKhauSiB2B")]
public partial class BangChietKhauSiB2b
{
    [Key]
    [Column("MaMocCK")]
    public int MaMocCk { get; set; }

    [Column("TenMocB2B")]
    [StringLength(100)]
    public string TenMocB2b { get; set; } = null!;

    [Column("SLHopToiThieu")]
    public int SlhopToiThieu { get; set; }

    [Column("SLHopToiDa")]
    public int SlhopToiDa { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PhanTramChietKhau { get; set; }

    public bool MienPhiInLogo { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PhiKhuonInLogo { get; set; }

    public int SoNgaySanXuatCongThem { get; set; }
}
