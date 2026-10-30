using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("ChiTietComboMau")]
public partial class ChiTietComboMau
{
    [Key]
    [Column("MaCTCB")]
    public int MaCtcb { get; set; }

    [Column("MaCB")]
    public int MaCb { get; set; }

    public int MaBanh { get; set; }

    public int ViTriO { get; set; }

    public int SoLuong { get; set; }

    [ForeignKey("MaBanh")]
    [InverseProperty("ChiTietComboMaus")]
    public virtual SanPhamBanh MaBanhNavigation { get; set; } = null!;

    [ForeignKey("MaCb")]
    [InverseProperty("ChiTietComboMaus")]
    public virtual ComboMauCoSan MaCbNavigation { get; set; } = null!;
}
