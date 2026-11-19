using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

/// <summary>
/// Bảng vật lý lưu trữ Sổ quỹ Phiếu Thu / Phiếu Chi chuẩn Nguyệt Cát ERP trong SQL Server.
/// </summary>
[Table("PhieuThuChi")]
public partial class PhieuThuChi
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaPhieu { get; set; }

    [StringLength(50)]
    public string MaPhieuHienThi { get; set; } = string.Empty;

    [StringLength(20)]
    public string LoaiPhieu { get; set; } = "THU";

    [StringLength(100)]
    public string HangMuc { get; set; } = "Thu tiền bán hàng";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SoTien { get; set; }

    [StringLength(30)]
    public string PhuongThuc { get; set; } = "MOMO_QR";

    public int? MaDonHangLienKet { get; set; }

    public int? MaPhieuNhapLienKet { get; set; }

    [ForeignKey("MaPhieuNhapLienKet")]
    [InverseProperty("PhieuThuChis")]
    public virtual PhieuNhapKho? MaPhieuNhapLienKetNavigation { get; set; }

    [StringLength(150)]
    public string NguoiNopNhan { get; set; } = string.Empty;

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayLap { get; set; } = DateTime.Now;
}
