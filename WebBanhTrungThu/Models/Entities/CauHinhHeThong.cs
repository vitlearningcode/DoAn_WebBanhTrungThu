using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanhTrungThu.Models.Entities;

/// <summary>
/// Bảng vật lý lưu trữ Cấu hình Hệ thống (Tài khoản QR MoMo 0814585526 & Metadata KiotViet) trong SQL Server.
/// </summary>
[Table("CauHinhHeThong")]
public partial class CauHinhHeThong
{
    [Key]
    [StringLength(100)]
    public string KhoaCauHinh { get; set; } = string.Empty;

    public string GiaTriJson { get; set; } = "{}";

    [StringLength(255)]
    public string? MoTa { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;
}
