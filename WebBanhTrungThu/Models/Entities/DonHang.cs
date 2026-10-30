using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Models.Entities;

[Table("DonHang")]
[Index("MaTraCuuDh", Name = "UQ__DonHang__DD6E355C044FB2C7", IsUnique = true)]
public partial class DonHang
{
    [Key]
    [Column("MaDH")]
    public int MaDh { get; set; }

    [Column("MaTraCuuDH")]
    [StringLength(25)]
    [Unicode(false)]
    public string MaTraCuuDh { get; set; } = null!;

    [Column("MaND")]
    public int? MaNd { get; set; }

    [Column("MaPhienKVL")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MaPhienKvl { get; set; }

    [StringLength(150)]
    public string HoTenNguoiMua { get; set; } = null!;

    [Column("SDTNguoiMua")]
    [StringLength(15)]
    [Unicode(false)]
    public string SdtnguoiMua { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string? EmailNguoiMua { get; set; }

    [Column("LaDonDoanhNghiepB2B")]
    public bool LaDonDoanhNghiepB2b { get; set; }

    [StringLength(250)]
    public string? TenCongTy { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? MaSoThue { get; set; }

    public int? MaAnhLogoCongTy { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongTienHang { get; set; }

    [Column("PhanTramCK_B2B", TypeName = "decimal(5, 2)")]
    public decimal PhanTramCkB2b { get; set; }

    [Column("TienChietKhauB2B", TypeName = "decimal(18, 2)")]
    public decimal TienChietKhauB2b { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TienGiamVoucher { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PhiInLogo { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongPhiVanChuyen { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongThanhToanCuoi { get; set; }

    [Column("PhuongThucTT")]
    [StringLength(30)]
    [Unicode(false)]
    public string PhuongThucTt { get; set; } = null!;

    [Column("TrangThaiTT")]
    [StringLength(30)]
    [Unicode(false)]
    public string TrangThaiTt { get; set; } = null!;

    [Column("TrangThaiDH")]
    [StringLength(30)]
    [Unicode(false)]
    public string TrangThaiDh { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime NgayDat { get; set; }

    [InverseProperty("MaDhNavigation")]
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [InverseProperty("MaDhNavigation")]
    public virtual ICollection<DotGiaoHangDaDiem> DotGiaoHangDaDiems { get; set; } = new List<DotGiaoHangDaDiem>();

    [ForeignKey("MaNd")]
    [InverseProperty("DonHangs")]
    public virtual NguoiDung? MaNdNavigation { get; set; }

    [ForeignKey("MaPhienKvl")]
    [InverseProperty("DonHangs")]
    public virtual PhienKhachVangLai? MaPhienKvlNavigation { get; set; }
}
