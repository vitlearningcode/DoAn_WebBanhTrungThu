using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ThongSoDinhDuong")]
[Index("MaBanh", Name = "UQ__ThongSoD__8DF1BC0EBAB21C04", IsUnique = true)]
public partial class ThongSoDinhDuong
{
    [Key]
    [Column("MaDD")]
    public int MaDd { get; set; }

    public int MaBanh { get; set; }

    public int NangLuongCalo { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal LuongDuongGram { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal ChatDamGram { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal ChatBeoGram { get; set; }

    [Column("ChiSoDuongHuyet_GI")]
    public int ChiSoDuongHuyetGi { get; set; }

    [StringLength(300)]
    public string? CanhBaoDiUng { get; set; }

    [ForeignKey("MaBanh")]
    [InverseProperty("ThongSoDinhDuong")]
    public virtual SanPhamBanh MaBanhNavigation { get; set; } = null!;
}
