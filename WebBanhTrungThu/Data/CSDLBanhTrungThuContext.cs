using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.Data;

public partial class CSDLBanhTrungThuContext : DbContext
{
    public CSDLBanhTrungThuContext()
    {
    }

    public CSDLBanhTrungThuContext(DbContextOptions<CSDLBanhTrungThuContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BangChietKhauSiB2b> BangChietKhauSiB2bs { get; set; }

    public virtual DbSet<CauHinhLandingPage> CauHinhLandingPages { get; set; }

    public virtual DbSet<ChiTietComboMau> ChiTietComboMaus { get; set; }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }

    public virtual DbSet<ChiTietHopTuChon> ChiTietHopTuChons { get; set; }

    public virtual DbSet<ComboMauCoSan> ComboMauCoSans { get; set; }

    public virtual DbSet<CongThucBanhNl> CongThucBanhNls { get; set; }

    public virtual DbSet<DanhMucBanh> DanhMucBanhs { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<DotGiaoHangDaDiem> DotGiaoHangDaDiems { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HopQuaTuChon> HopQuaTuChons { get; set; }

    public virtual DbSet<KhoHinhAnh> KhoHinhAnhs { get; set; }

    public virtual DbSet<LienKetHinhAnh> LienKetHinhAnhs { get; set; }

    public virtual DbSet<LoSanXuatBanh> LoSanXuatBanhs { get; set; }

    public virtual DbSet<MaKhuyenMai> MaKhuyenMais { get; set; }

    public virtual DbSet<MauVoHop> MauVoHops { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NguyenLieu> NguyenLieus { get; set; }

    public virtual DbSet<PhienKhachVangLai> PhienKhachVangLais { get; set; }

    public virtual DbSet<SanPhamBanh> SanPhamBanhs { get; set; }

    public virtual DbSet<SoDiaChi> SoDiaChis { get; set; }

    public virtual DbSet<ThongSoDinhDuong> ThongSoDinhDuongs { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-R0BDS56;Database=CSDL_BanhTrungThu;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BangChietKhauSiB2b>(entity =>
        {
            entity.HasKey(e => e.MaMocCk).HasName("PK__BangChie__B3E85F61AEDBAA52");

            entity.Property(e => e.PhiKhuonInLogo).HasDefaultValue(500000m);
            entity.Property(e => e.SoNgaySanXuatCongThem).HasDefaultValue(2);
        });

        modelBuilder.Entity<CauHinhLandingPage>(entity =>
        {
            entity.HasKey(e => e.MaPhanKhu).HasName("PK__CauHinhL__341EAF030C07BD65");

            entity.Property(e => e.DangKichHoat).HasDefaultValue(true);
            entity.Property(e => e.ThuTuHienThi).HasDefaultValue(1);

            entity.HasOne(d => d.MaAnhNenNavigation).WithMany(p => p.CauHinhLandingPages).HasConstraintName("FK_Landing_AnhNen");
        });

        modelBuilder.Entity<ChiTietComboMau>(entity =>
        {
            entity.HasKey(e => e.MaCtcb).HasName("PK__ChiTietC__1E4E48EB1758B041");

            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaBanhNavigation).WithMany(p => p.ChiTietComboMaus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTCB_Banh");

            entity.HasOne(d => d.MaCbNavigation).WithMany(p => p.ChiTietComboMaus).HasConstraintName("FK_CTCB_Combo");
        });

        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.MaCtdh).HasName("PK__ChiTietD__1E4E40F0EC76C2CB");

            entity.HasOne(d => d.MaDhNavigation).WithMany(p => p.ChiTietDonHangs).HasConstraintName("FK_CTDH_DonHang");

            entity.HasOne(d => d.MaDotGhNavigation).WithMany(p => p.ChiTietDonHangs).HasConstraintName("FK_CTDH_DotGH");
        });

        modelBuilder.Entity<ChiTietGioHang>(entity =>
        {
            entity.HasKey(e => e.MaCtgh).HasName("PK__ChiTietG__1E4FAF548F8D42A7");

            entity.Property(e => e.NgayThem).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaBanhNavigation).WithMany(p => p.ChiTietGioHangs).HasConstraintName("FK_CTGH_Banh");

            entity.HasOne(d => d.MaCbNavigation).WithMany(p => p.ChiTietGioHangs).HasConstraintName("FK_CTGH_Combo");

            entity.HasOne(d => d.MaGhNavigation).WithMany(p => p.ChiTietGioHangs).HasConstraintName("FK_CTGH_GioHang");

            entity.HasOne(d => d.MaHopTcNavigation).WithMany(p => p.ChiTietGioHangs).HasConstraintName("FK_CTGH_HopTC");
        });

        modelBuilder.Entity<ChiTietHopTuChon>(entity =>
        {
            entity.HasKey(e => e.MaCthopTc).HasName("PK__ChiTietH__B016CA599DBD38F7");

            entity.HasOne(d => d.MaBanhNavigation).WithMany(p => p.ChiTietHopTuChons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHopTC_Banh");

            entity.HasOne(d => d.MaHopTcNavigation).WithMany(p => p.ChiTietHopTuChons).HasConstraintName("FK_CTHopTC_HopTC");
        });

        modelBuilder.Entity<ComboMauCoSan>(entity =>
        {
            entity.HasKey(e => e.MaCb).HasName("PK__ComboMau__27258E1ACF216E7D");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaVhNavigation).WithMany(p => p.ComboMauCoSans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Combo_MauVoHop");
        });

        modelBuilder.Entity<CongThucBanhNl>(entity =>
        {
            entity.HasKey(e => new { e.MaBanh, e.MaNl }).HasName("PK__CongThuc__5F83E17C0491859F");

            entity.HasOne(d => d.MaBanhNavigation).WithMany(p => p.CongThucBanhNls).HasConstraintName("FK_CTNL_Banh");

            entity.HasOne(d => d.MaNlNavigation).WithMany(p => p.CongThucBanhNls).HasConstraintName("FK_CTNL_NguyenLieu");
        });

        modelBuilder.Entity<DanhMucBanh>(entity =>
        {
            entity.HasKey(e => e.MaDm).HasName("PK__DanhMucB__2725866E6306940B");

            entity.Property(e => e.ThuTuSx).HasDefaultValue(1);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaDmchaNavigation).WithMany(p => p.InverseMaDmchaNavigation).HasConstraintName("FK_DMBanh_Cha");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.MaDh).HasName("PK__DonHang__272586613CF2AF77");

            entity.Property(e => e.NgayDat).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PhuongThucTt).HasDefaultValue("COD");
            entity.Property(e => e.TrangThaiDh).HasDefaultValue("CHO_XAC_NHAN");
            entity.Property(e => e.TrangThaiTt).HasDefaultValue("CHUA_TT");

            entity.HasOne(d => d.MaNdNavigation).WithMany(p => p.DonHangs).HasConstraintName("FK_DH_NguoiDung");

            entity.HasOne(d => d.MaPhienKvlNavigation).WithMany(p => p.DonHangs).HasConstraintName("FK_DH_PhienKVL");
        });

        modelBuilder.Entity<DotGiaoHangDaDiem>(entity =>
        {
            entity.HasKey(e => e.MaDotGh).HasName("PK__DotGiaoH__C316CD4F412BA7F5");

            entity.Property(e => e.CoKemThiepQr).HasDefaultValue(true);
            entity.Property(e => e.KhungGioGiao).HasDefaultValue("Sáng (08:00 - 11:30)");
            entity.Property(e => e.MauGiaoDienThiep).HasDefaultValue("CO_PHONG_HOANG_GIA");
            entity.Property(e => e.PhiShipDotNay).HasDefaultValue(30000m);
            entity.Property(e => e.TrangThaiGiao).HasDefaultValue("CHO_DONG_GOI");

            entity.HasOne(d => d.MaDhNavigation).WithMany(p => p.DotGiaoHangDaDiems).HasConstraintName("FK_DotGH_DonHang");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.MaGh).HasName("PK__GioHang__2725AE85EFC554A5");

            entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaNdNavigation).WithMany(p => p.GioHangs).HasConstraintName("FK_GH_NguoiDung");

            entity.HasOne(d => d.MaPhienKvlNavigation).WithMany(p => p.GioHangs).HasConstraintName("FK_GH_PhienKVL");
        });

        modelBuilder.Entity<HopQuaTuChon>(entity =>
        {
            entity.HasKey(e => e.MaHopTc).HasName("PK__HopQuaTu__08E887DFB0B42856");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TenHopKhachDat).HasDefaultValue("Hộp Quà Tự Chọn");

            entity.HasOne(d => d.MaNdNavigation).WithMany(p => p.HopQuaTuChons).HasConstraintName("FK_HopTC_NguoiDung");

            entity.HasOne(d => d.MaPhienKvlNavigation).WithMany(p => p.HopQuaTuChons).HasConstraintName("FK_HopTC_PhienKVL");

            entity.HasOne(d => d.MaVhNavigation).WithMany(p => p.HopQuaTuChons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HopTC_MauVoHop");
        });

        modelBuilder.Entity<KhoHinhAnh>(entity =>
        {
            entity.HasKey(e => e.MaHa).HasName("PK__KhoHinhA__2725A6FD30981DD1");

            entity.Property(e => e.LoaiLuuTru).HasDefaultValue(1);
            entity.Property(e => e.NgayTaiLen).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<LienKetHinhAnh>(entity =>
        {
            entity.HasKey(e => e.MaLk).HasName("PK__LienKetH__2725C77A9A0C6269");

            entity.Property(e => e.ThuTuHienThi).HasDefaultValue(1);

            entity.HasOne(d => d.MaHaNavigation).WithMany(p => p.LienKetHinhAnhs).HasConstraintName("FK_LKHA_KhoHA");
        });

        modelBuilder.Entity<LoSanXuatBanh>(entity =>
        {
            entity.HasKey(e => e.MaLo).HasName("PK__LoSanXua__2725C7564B925741");

            entity.Property(e => e.TrangThaiLo).HasDefaultValue("DANG_BAN");

            entity.HasOne(d => d.MaBanhNavigation).WithMany(p => p.LoSanXuatBanhs).HasConstraintName("FK_LoSX_Banh");
        });

        modelBuilder.Entity<MaKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.MaKm).HasName("PK__MaKhuyen__2725CF15D90098C0");

            entity.Property(e => e.ChoPhepKhachVangLai).HasDefaultValue(true);
            entity.Property(e => e.LoaiGiam).HasDefaultValue("PHAN_TRAM");
            entity.Property(e => e.SltoiDa).HasDefaultValue(100);
        });

        modelBuilder.Entity<MauVoHop>(entity =>
        {
            entity.HasKey(e => e.MaVh).HasName("PK__MauVoHop__272510328E056350");

            entity.Property(e => e.KieuBoCucLuoi).HasDefaultValue("grid-2x2");
            entity.Property(e => e.SltonKhoVo).HasDefaultValue(50);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNd).HasName("PK__NguoiDun__2725D7241D49192A");

            entity.Property(e => e.HangTv).HasDefaultValue("Đồng");
            entity.Property(e => e.MaVt).HasDefaultValue("KHACH_TV");
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaVtNavigation).WithMany(p => p.NguoiDungs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ND_VaiTro");
        });

        modelBuilder.Entity<NguyenLieu>(entity =>
        {
            entity.HasKey(e => e.MaNl).HasName("PK__NguyenLi__2725D73C41190141");

            entity.Property(e => e.LaNguyenLieuChay).HasDefaultValue(true);
        });

        modelBuilder.Entity<PhienKhachVangLai>(entity =>
        {
            entity.HasKey(e => e.MaPhienKvl).HasName("PK__PhienKha__D3B00463407751DA");

            entity.Property(e => e.NgayHetHan).HasDefaultValueSql("(dateadd(day,(14),getdate()))");
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaNdDaGopNavigation).WithMany(p => p.PhienKhachVangLais).HasConstraintName("FK_PhienKVL_NguoiDung");
        });

        modelBuilder.Entity<SanPhamBanh>(entity =>
        {
            entity.HasKey(e => e.MaBanh).HasName("PK__SanPhamB__8DF1BC0FEE7828E0");

            entity.Property(e => e.MucDoNgot).HasDefaultValue("BINH_THUONG");
            entity.Property(e => e.NhomHuongVi).HasDefaultValue("MAN_TRUYENTHONG");
            entity.Property(e => e.SoTrungMuoi).HasDefaultValue(1);
            entity.Property(e => e.TrangThaiBan).HasDefaultValue(true);
            entity.Property(e => e.TrongLuongGram).HasDefaultValue(150);

            entity.HasOne(d => d.MaDmNavigation).WithMany(p => p.SanPhamBanhs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SPBanh_DanhMuc");
        });

        modelBuilder.Entity<SoDiaChi>(entity =>
        {
            entity.HasKey(e => e.MaDc).HasName("PK__SoDiaChi__27258664D28A7BA7");

            entity.HasOne(d => d.MaNdNavigation).WithMany(p => p.SoDiaChis).HasConstraintName("FK_SoDC_NguoiDung");
        });

        modelBuilder.Entity<ThongSoDinhDuong>(entity =>
        {
            entity.HasKey(e => e.MaDd).HasName("PK__ThongSoD__272586652191881B");

            entity.Property(e => e.ChiSoDuongHuyetGi).HasDefaultValue(55);

            entity.HasOne(d => d.MaBanhNavigation).WithOne(p => p.ThongSoDinhDuong).HasConstraintName("FK_TSDD_SPBanh");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVt).HasName("PK__VaiTro__2725103ECB282279");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
