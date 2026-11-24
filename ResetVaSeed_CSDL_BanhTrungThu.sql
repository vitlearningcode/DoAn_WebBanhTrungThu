-- =============================================================================
-- HỆ THỐNG MINI-ERP PHÂN PHỐI BÁNH TRUNG THU NGUYỆT CÁT (SQL-FIRST 37 BẢNG)
-- BẢN FULL SCHEMA + SEED ĐA MÁY (TỰ TẠO DB + 37 BẢNG + 71 KHÓA NGOẠI + DỮ LIỆU MẪU)
-- Chạy trực tiếp trên cả máy mới (chưa có DB) hoặc máy cũ (cần Reset dữ liệu sạch).
-- Quy mô: 6 Thương hiệu (5 hãng × 12 bánh = 60 SP), 6 NCC, 4 Tài khoản (MK: 123456),
--         3 Mẫu Vỏ hộp + 4 Phiếu kho Vỏ hộp, 6 Phiếu nhập (67 Lô FEFO), 10 Đơn hàng,
--         3 Phiếu xuất, 1 Phiếu kiểm kho cân bằng, 17 Chứng từ Sổ quỹ khớp 0 đ sai số.
-- =============================================================================
USE [master];
GO
IF DB_ID(N'CSDL_BanhTrungThu') IS NULL
BEGIN
    CREATE DATABASE [CSDL_BanhTrungThu];
END;
GO
USE [CSDL_BanhTrungThu];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- PHẦN 1: KHỞI TẠO ĐẦY ĐỦ 37 BẢNG VẬT LÝ, RÀNG BUỘC MẶC ĐỊNH & CHỈ MỤC
-- =============================================================================
IF OBJECT_ID(N'[dbo].[BangChietKhauSiB2B]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BangChietKhauSiB2B](
	[MaMocCK] [int] IDENTITY(1,1) NOT NULL,
	[TenMocB2B] [nvarchar](100) NOT NULL,
	[SLHopToiThieu] [int] NOT NULL,
	[SLHopToiDa] [int] NOT NULL,
	[PhanTramChietKhau] [decimal](5, 2) NOT NULL,
	[MienPhiInLogo] [bit] NOT NULL,
	[PhiKhuonInLogo] [decimal](18, 2) NOT NULL,
	[SoNgaySanXuatCongThem] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaMocCK] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[CauHinhHeThong]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[CauHinhHeThong](
	[KhoaCauHinh] [nvarchar](100) NOT NULL,
	[GiaTriJson] [nvarchar](max) NOT NULL,
	[MoTa] [nvarchar](255) NULL,
	[NgayCapNhat] [datetime] NOT NULL,
	[MaNdCapNhat] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[KhoaCauHinh] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[CauHinhLandingPage]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[CauHinhLandingPage](
	[MaPhanKhu] [int] IDENTITY(1,1) NOT NULL,
	[MaCodeKhuVuc] [varchar](50) NOT NULL,
	[TieuDeChinh] [nvarchar](250) NOT NULL,
	[TieuDePhu] [nvarchar](500) NULL,
	[NoiDungMoTa] [nvarchar](max) NULL,
	[MaAnhNen] [int] NULL,
	[MaAnhPhu_Parallax] [int] NULL,
	[ChuNutBamCTA] [nvarchar](100) NULL,
	[DuongDanCTA] [varchar](250) NULL,
	[MocThoiGianDemNguoc] [datetime] NULL,
	[ThuTuHienThi] [int] NOT NULL,
	[DangKichHoat] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaPhanKhu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaCodeKhuVuc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ChiTietComboMau]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ChiTietComboMau](
	[MaCTCB] [int] IDENTITY(1,1) NOT NULL,
	[MaCB] [int] NOT NULL,
	[MaBanh] [int] NOT NULL,
	[ViTriO] [int] NOT NULL,
	[SoLuong] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCTCB] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ChiTietDonHang]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ChiTietDonHang](
	[MaCTDH] [int] IDENTITY(1,1) NOT NULL,
	[MaDH] [int] NOT NULL,
	[MaDotGH] [int] NULL,
	[LoaiSanPham] [int] NOT NULL,
	[MaBanh] [int] NULL,
	[MaCB] [int] NULL,
	[MaHopTC] [int] NULL,
	[TenSanPham_LuuCung] [nvarchar](250) NOT NULL,
	[CauHinhHop_JSON] [nvarchar](max) NULL,
	[SoLuong] [int] NOT NULL,
	[DonGiaLucMua] [decimal](18, 2) NOT NULL,
	[ThanhTien] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCTDH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ChiTietGioHang]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ChiTietGioHang](
	[MaCTGH] [int] IDENTITY(1,1) NOT NULL,
	[MaGH] [int] NOT NULL,
	[LoaiSanPham] [int] NOT NULL,
	[MaBanh] [int] NULL,
	[MaCB] [int] NULL,
	[MaHopTC] [int] NULL,
	[SoLuong] [int] NOT NULL,
	[DonGiaTamTinh] [decimal](18, 2) NOT NULL,
	[NgayThem] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCTGH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ChiTietHopTuChon]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ChiTietHopTuChon](
	[MaCTHopTC] [int] IDENTITY(1,1) NOT NULL,
	[MaHopTC] [int] NOT NULL,
	[MaBanh] [int] NOT NULL,
	[ViTriOTrongHop] [int] NOT NULL,
	[DonGiaBanhLucChon] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCTHopTC] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ChiTietKiemKho]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ChiTietKiemKho](
	[MaCtkk] [int] IDENTITY(1,1) NOT NULL,
	[MaPkk] [int] NOT NULL,
	[MaSp] [int] NOT NULL,
	[TonHeThong] [int] NOT NULL,
	[TonThucTe] [int] NOT NULL,
	[SoLuongLech] [int] NOT NULL,
	[GiaTriLech] [decimal](18, 2) NOT NULL,
	[LyDoLech] [nvarchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCtkk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ChiTietPhieuNhap]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ChiTietPhieuNhap](
	[MaCtpn] [int] IDENTITY(1,1) NOT NULL,
	[MaPn] [int] NOT NULL,
	[MaSp] [int] NOT NULL,
	[MaLo] [int] NULL,
	[SoLuong] [int] NOT NULL,
	[DonGiaNhap] [decimal](18, 2) NOT NULL,
	[ThanhTien] [decimal](18, 2) NOT NULL,
	[NgaySanXuat] [date] NOT NULL,
	[HanSuDung] [date] NOT NULL,
	[PhanTramChietKhau] [decimal](5, 2) NOT NULL,
	[GiaNiemYet] [decimal](18, 0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCtpn] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ChiTietPhieuXuat]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ChiTietPhieuXuat](
	[MaCtpx] [int] IDENTITY(1,1) NOT NULL,
	[MaPx] [int] NOT NULL,
	[MaSp] [int] NOT NULL,
	[MaLo] [int] NULL,
	[SoLuongXuat] [int] NOT NULL,
	[GiaVonLucXuat] [decimal](18, 2) NOT NULL,
	[ThanhTienVon] [decimal](18, 2) NOT NULL,
	[GhiChuDong] [nvarchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCtpx] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ComboMauCoSan]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ComboMauCoSan](
	[MaCB] [int] IDENTITY(1,1) NOT NULL,
	[MaCodeCB] [varchar](30) NOT NULL,
	[TenCombo] [nvarchar](200) NOT NULL,
	[MaVH] [int] NOT NULL,
	[GiaNiemYet] [decimal](18, 2) NOT NULL,
	[GiaUuDaiCombo] [decimal](18, 2) NOT NULL,
	[ThongDiepYNgia] [nvarchar](500) NULL,
	[TheDoiTuongPhuHop] [varchar](200) NULL,
	[TrangThai] [bit] NOT NULL,
	[MaThuongHieu] [int] NULL,
	[MaDM] [int] NULL,
	[MaVach] [varchar](50) NULL,
	[ViTriKho] [nvarchar](100) NOT NULL,
	[DonViTinh] [nvarchar](50) NOT NULL,
	[BanTrucTiep] [bit] NOT NULL,
	[TongTonKho] [int] NOT NULL,
	[TrongLuongGram] [int] NOT NULL,
	[MaAnhChinh] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaCB] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaCodeCB] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[DanhMucBanh]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[DanhMucBanh](
	[MaDM] [int] IDENTITY(1,1) NOT NULL,
	[MaDMCha] [int] NULL,
	[TenDM] [nvarchar](150) NOT NULL,
	[DuongDanSlug] [varchar](150) NOT NULL,
	[MoTa] [nvarchar](500) NULL,
	[ThuTuSX] [int] NOT NULL,
	[TrangThai] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaDM] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[DuongDanSlug] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[DonHang]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[DonHang](
	[MaDH] [int] IDENTITY(1,1) NOT NULL,
	[MaTraCuuDH] [varchar](25) NOT NULL,
	[MaND] [int] NULL,
	[MaPhienKVL] [varchar](64) NULL,
	[HoTenNguoiMua] [nvarchar](150) NOT NULL,
	[SDTNguoiMua] [varchar](15) NOT NULL,
	[EmailNguoiMua] [varchar](150) NULL,
	[LaDonDoanhNghiepB2B] [bit] NOT NULL,
	[TenCongTy] [nvarchar](250) NULL,
	[MaSoThue] [varchar](50) NULL,
	[MaAnhLogoCongTy] [int] NULL,
	[TongTienHang] [decimal](18, 2) NOT NULL,
	[PhanTramCK_B2B] [decimal](5, 2) NOT NULL,
	[TienChietKhauB2B] [decimal](18, 2) NOT NULL,
	[TienGiamVoucher] [decimal](18, 2) NOT NULL,
	[PhiInLogo] [decimal](18, 2) NOT NULL,
	[TongPhiVanChuyen] [decimal](18, 2) NOT NULL,
	[TongThanhToanCuoi] [decimal](18, 2) NOT NULL,
	[PhuongThucTT] [varchar](30) NOT NULL,
	[TrangThaiTT] [varchar](30) NOT NULL,
	[TrangThaiDH] [varchar](30) NOT NULL,
	[NgayDat] [datetime] NOT NULL,
	[TienGiamHangVip] [decimal](18, 0) NOT NULL,
	[PhanTramGiamVip] [int] NOT NULL,
	[SoDiemDaDung] [int] NOT NULL,
	[TienGiamDiemVip] [decimal](18, 0) NOT NULL,
	[HanThanhToanQr] [datetime] NULL,
	[DaCongDiemThuong] [bit] NOT NULL,
	[DaHoanDiemKhiHuy] [bit] NOT NULL,
	[LyDoHuy] [nvarchar](500) NULL,
	[MaNdLap] [int] NULL,
	[KenhBan] [varchar](30) NOT NULL,
	[SoTienKhachDaTra] [decimal](18, 2) NULL,
	[MaNganHangHoan] [varchar](50) NULL,
	[SoTaiKhoanHoan] [varchar](50) NULL,
	[TenChuTkHoan] [nvarchar](150) NULL,
	[GhiChuDieuChinhAdmin] [nvarchar](500) NULL,
	[ThoiGianCapNhatCuoi] [datetime] NULL,
	[NguoiCapNhatCuoi] [nvarchar](150) NULL,
	[DaRollbackKho] [bit] NOT NULL,
	[MaKM] [int] NULL,
	[MaMocCK] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaDH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaTraCuuDH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[DotGiaoHang_DaDiem](
	[MaDotGH] [int] IDENTITY(1,1) NOT NULL,
	[MaDH] [int] NOT NULL,
	[TenNguoiNhan] [nvarchar](150) NOT NULL,
	[SDTNguoiNhan] [varchar](15) NOT NULL,
	[DiaChiGiaoChiTiet] [nvarchar](500) NOT NULL,
	[NgayHenGiao] [date] NOT NULL,
	[KhungGioGiao] [nvarchar](50) NOT NULL,
	[PhiShipDotNay] [decimal](18, 2) NOT NULL,
	[TrangThaiGiao] [varchar](30) NOT NULL,
	[CoKemThiepQR] [bit] NOT NULL,
	[MaTokenThiepQR] [varchar](50) NULL,
	[LoiChucThiep] [nvarchar](1000) NULL,
	[MauGiaoDienThiep] [varchar](50) NULL,
	[SoLanMoThiep] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaDotGH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[GioHang]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[GioHang](
	[MaGH] [int] IDENTITY(1,1) NOT NULL,
	[MaND] [int] NULL,
	[MaPhienKVL] [varchar](64) NULL,
	[CodeKMDangApDung] [varchar](30) NULL,
	[LaCheDoMuaSiB2B] [bit] NOT NULL,
	[YeuCauInLogoB2B] [bit] NOT NULL,
	[NgayCapNhat] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaGH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[HopQuaTuChon]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[HopQuaTuChon](
	[MaHopTC] [int] IDENTITY(1,1) NOT NULL,
	[MaVH] [int] NOT NULL,
	[MaND] [int] NULL,
	[MaPhienKVL] [varchar](64) NULL,
	[TenHopKhachDat] [nvarchar](150) NOT NULL,
	[MaBamCauHinh] [varchar](150) NOT NULL,
	[TongCaloHop] [int] NOT NULL,
	[TongGiaMotHop] [decimal](18, 2) NOT NULL,
	[DaLuuMauYeuThich] [bit] NOT NULL,
	[NgayTao] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaHopTC] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[HoSoNhanVien]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[HoSoNhanVien](
	[MaNv] [int] IDENTITY(1,1) NOT NULL,
	[MaNd] [int] NOT NULL,
	[MaCodeNv] [nvarchar](20) NOT NULL,
	[ChucVu] [nvarchar](100) NOT NULL,
	[ChiNhanh] [nvarchar](150) NOT NULL,
	[CaLamViec] [nvarchar](100) NOT NULL,
	[NgayVaoLam] [date] NOT NULL,
	[TrangThaiLamViec] [nvarchar](30) NOT NULL,
	[GhiChu] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaNv] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaCodeNv] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaNd] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[KhoHinhAnh]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[KhoHinhAnh](
	[MaHA] [int] IDENTITY(1,1) NOT NULL,
	[TenTepGoc] [nvarchar](200) NOT NULL,
	[TieuDeAlt] [nvarchar](250) NULL,
	[LoaiLuuTru] [int] NOT NULL,
	[DuongDanCucBo] [nvarchar](500) NULL,
	[DuongDanDamMay] [nvarchar](500) NULL,
	[MaFileDrive] [varchar](100) NULL,
	[KichThuocPixel] [varchar](50) NULL,
	[NgayTaiLen] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaHA] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[LienKetHinhAnh]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[LienKetHinhAnh](
	[MaLK] [int] IDENTITY(1,1) NOT NULL,
	[MaHA] [int] NOT NULL,
	[LoaiDoiTuong] [varchar](30) NOT NULL,
	[MaDoiTuong] [int] NOT NULL,
	[VaiTroAnh] [varchar](30) NOT NULL,
	[ThuTuHienThi] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaLK] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[LoSanXuatBanh]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[LoSanXuatBanh](
	[MaLo] [int] IDENTITY(1,1) NOT NULL,
	[MaBanh] [int] NOT NULL,
	[MaLoCode] [varchar](50) NOT NULL,
	[NgaySanXuat] [date] NOT NULL,
	[HanSuDung] [date] NOT NULL,
	[SLNhapBanDau] [int] NOT NULL,
	[SLConLai] [int] NOT NULL,
	[PhanTramGiamCanDate] [decimal](5, 2) NOT NULL,
	[TrangThaiLo] [varchar](30) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaLo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaLoCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[MaKhuyenMai]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[MaKhuyenMai](
	[MaKM] [int] IDENTITY(1,1) NOT NULL,
	[CodeKM] [varchar](30) NOT NULL,
	[TenChuongTrinh] [nvarchar](200) NOT NULL,
	[LoaiGiam] [varchar](20) NOT NULL,
	[GiaTriGiam] [decimal](18, 2) NOT NULL,
	[GiamToiDa] [decimal](18, 2) NOT NULL,
	[DonToiThieu] [decimal](18, 2) NOT NULL,
	[SLToiDa] [int] NOT NULL,
	[SLDaDung] [int] NOT NULL,
	[ChoPhepKhachVangLai] [bit] NOT NULL,
	[NgayBatDau] [datetime] NOT NULL,
	[NgayKetThuc] [datetime] NOT NULL,
	[TrangThaiKichHoat] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaKM] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[CodeKM] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[MauVoHop]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[MauVoHop](
	[MaVH] [int] IDENTITY(1,1) NOT NULL,
	[MaCodeVH] [varchar](30) NOT NULL,
	[TenVoHop] [nvarchar](200) NOT NULL,
	[SucChuaBanh] [int] NOT NULL,
	[GiaVoHopRieng] [decimal](18, 2) NOT NULL,
	[SLBanhThuongHangToiThieu] [int] NOT NULL,
	[KieuBoCucLuoi] [varchar](50) NOT NULL,
	[KemTraOLong] [bit] NOT NULL,
	[SLTonKhoVo] [int] NOT NULL,
	[MoTaChatLieu] [nvarchar](500) NULL,
	[TrangThai] [bit] NOT NULL,
	[MaAnhChinh] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaVH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaCodeVH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[NguoiDung]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[NguoiDung](
	[MaND] [int] IDENTITY(1,1) NOT NULL,
	[MaVT] [varchar](20) NOT NULL,
	[HoTen] [nvarchar](150) NOT NULL,
	[SDT] [varchar](15) NOT NULL,
	[Email] [varchar](150) NOT NULL,
	[MatKhauHash] [varchar](500) NOT NULL,
	[DiemTichLuy] [int] NOT NULL,
	[HangTV] [nvarchar](30) NOT NULL,
	[TrangThai] [bit] NOT NULL,
	[NgayTao] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaND] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[SDT] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[NhaCungCap]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[NhaCungCap](
	[MaNcc] [int] IDENTITY(1,1) NOT NULL,
	[MaCodeNcc] [nvarchar](30) NOT NULL,
	[TenNcc] [nvarchar](200) NOT NULL,
	[LoaiNcc] [nvarchar](50) NOT NULL,
	[ThuongHieuCungCap] [nvarchar](100) NOT NULL,
	[PhanTramChietKhauMacDinh] [decimal](5, 2) NOT NULL,
	[SoDienThoai] [nvarchar](20) NULL,
	[Email] [nvarchar](150) NULL,
	[DiaChi] [nvarchar](300) NULL,
	[GhiChu] [nvarchar](500) NULL,
	[DangHoatDong] [bit] NOT NULL,
	[NgayTao] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaNcc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaCodeNcc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[NhatKyTruKhoDonHang]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[NhatKyTruKhoDonHang](
	[MaNk] [int] IDENTITY(1,1) NOT NULL,
	[MaDH] [int] NOT NULL,
	[LoaiDoiTuong] [varchar](20) NOT NULL,
	[MaLo] [int] NULL,
	[MaBanh] [int] NULL,
	[MaVH] [int] NULL,
	[SoLuongDaTru] [int] NOT NULL,
	[DaHoanKho] [bit] NOT NULL,
	[NgayGhi] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaNk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[PhienKhachVangLai]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[PhienKhachVangLai](
	[MaPhienKVL] [varchar](64) NOT NULL,
	[DiaChiIP] [varchar](50) NULL,
	[ThongTinTrinhDuyet] [nvarchar](250) NULL,
	[MaND_DaGop] [int] NULL,
	[DaGopGioHang] [bit] NOT NULL,
	[NgayTao] [datetime] NOT NULL,
	[NgayHetHan] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaPhienKVL] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[PhieuKhoVoHop]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[PhieuKhoVoHop](
	[MaPvh] [int] IDENTITY(1,1) NOT NULL,
	[SoPhieu] [nvarchar](50) NOT NULL,
	[MaVH] [int] NOT NULL,
	[LoaiGiaoDich] [varchar](30) NOT NULL,
	[SoLuong] [int] NOT NULL,
	[DonGiaNhap] [decimal](18, 2) NOT NULL,
	[ThanhTien] [decimal](18, 2) NOT NULL,
	[MaNcc] [int] NULL,
	[MaPhieuChi] [int] NULL,
	[MaNdLap] [int] NULL,
	[NguoiLap] [nvarchar](150) NOT NULL,
	[GhiChu] [nvarchar](500) NULL,
	[NgayGiaoDich] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaPvh] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[SoPhieu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[PhieuKiemKho]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[PhieuKiemKho](
	[MaPkk] [int] IDENTITY(1,1) NOT NULL,
	[SoPhieuKiem] [nvarchar](50) NOT NULL,
	[NgayKiem] [datetime] NOT NULL,
	[NguoiKiem] [nvarchar](150) NOT NULL,
	[TrangThai] [varchar](30) NOT NULL,
	[TongLechTang] [int] NOT NULL,
	[TongLechGiam] [int] NOT NULL,
	[TongGiaTriLech] [decimal](18, 2) NOT NULL,
	[GhiChu] [nvarchar](500) NULL,
	[MaPxLienKet] [int] NULL,
	[MaNdKiem] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaPkk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[SoPhieuKiem] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[PhieuNhapKho]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[PhieuNhapKho](
	[MaPn] [int] IDENTITY(1,1) NOT NULL,
	[SoPhieuNhap] [nvarchar](50) NOT NULL,
	[NhaCungCap] [nvarchar](200) NOT NULL,
	[NgayNhap] [datetime] NOT NULL,
	[TongTienHang] [decimal](18, 2) NOT NULL,
	[GiamGia] [decimal](18, 2) NOT NULL,
	[TongPhaiTra] [decimal](18, 2) NOT NULL,
	[DaThanhToan] [decimal](18, 2) NOT NULL,
	[TrangThai] [varchar](30) NOT NULL,
	[TrangThaiThanhToan] [varchar](30) NOT NULL,
	[GhiChu] [nvarchar](500) NULL,
	[NguoiLap] [nvarchar](150) NOT NULL,
	[MaNdLap] [int] NULL,
	[MaNcc] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaPn] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[SoPhieuNhap] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[PhieuThuChi]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[PhieuThuChi](
	[MaPhieu] [int] IDENTITY(1,1) NOT NULL,
	[MaPhieuHienThi] [nvarchar](50) NOT NULL,
	[LoaiPhieu] [nvarchar](20) NOT NULL,
	[HangMuc] [nvarchar](100) NOT NULL,
	[SoTien] [decimal](18, 2) NOT NULL,
	[PhuongThuc] [nvarchar](30) NOT NULL,
	[MaDonHangLienKet] [int] NULL,
	[NguoiNopNhan] [nvarchar](150) NOT NULL,
	[GhiChu] [nvarchar](500) NULL,
	[NgayLap] [datetime] NOT NULL,
	[MaPhieuNhapLienKet] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaPhieu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[PhieuXuatKho]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[PhieuXuatKho](
	[MaPx] [int] IDENTITY(1,1) NOT NULL,
	[SoPhieuXuat] [nvarchar](50) NOT NULL,
	[LoaiXuat] [varchar](40) NOT NULL,
	[DoiTuongLienQuan] [nvarchar](250) NOT NULL,
	[NgayXuat] [datetime] NOT NULL,
	[TongGiaTriVon] [decimal](18, 2) NOT NULL,
	[TrangThai] [varchar](30) NOT NULL,
	[GhiChu] [nvarchar](500) NULL,
	[NguoiLap] [nvarchar](150) NOT NULL,
	[MaNdLap] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaPx] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[SoPhieuXuat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[SanPhamBanh]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[SanPhamBanh](
	[MaBanh] [int] IDENTITY(1,1) NOT NULL,
	[MaDM] [int] NOT NULL,
	[MaSKU] [varchar](30) NOT NULL,
	[TenBanh] [nvarchar](200) NOT NULL,
	[DonGiaLe] [decimal](18, 2) NOT NULL,
	[GiaVon] [decimal](18, 2) NOT NULL,
	[TrongLuongGram] [int] NOT NULL,
	[SoTrungMuoi] [int] NOT NULL,
	[MucDoNgot] [varchar](30) NOT NULL,
	[LaBanhChay] [bit] NOT NULL,
	[LaBanhThuongHang] [bit] NOT NULL,
	[NhomHuongVi] [varchar](30) NOT NULL,
	[TheDoiTuongPhuHop] [varchar](200) NULL,
	[MoTaNgan] [nvarchar](500) NULL,
	[CauChuyenBanh] [nvarchar](max) NULL,
	[TongTonKho] [int] NOT NULL,
	[TrangThaiBan] [bit] NOT NULL,
	[MaThuongHieu] [int] NULL,
	[MaNccMacDinh] [int] NULL,
	[MaVach] [varchar](50) NULL,
	[ViTriKho] [nvarchar](100) NOT NULL,
	[DonViTinh] [nvarchar](50) NOT NULL,
	[BanTrucTiep] [bit] NOT NULL,
	[MaAnhChinh] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaBanh] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaSKU] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[SoDiaChi]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[SoDiaChi](
	[MaDC] [int] IDENTITY(1,1) NOT NULL,
	[MaND] [int] NOT NULL,
	[TenNguoiNhan] [nvarchar](150) NOT NULL,
	[SDTNguoiNhan] [varchar](15) NOT NULL,
	[TinhThanh] [nvarchar](100) NOT NULL,
	[QuanHuyen] [nvarchar](100) NOT NULL,
	[PhuongXa] [nvarchar](100) NOT NULL,
	[DiaChiCuThe] [nvarchar](250) NOT NULL,
	[NhanDiaChi] [nvarchar](50) NULL,
	[LaMacDinh] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaDC] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ThiepChucMung3D]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ThiepChucMung3D](
	[MaThiep] [int] IDENTITY(1,1) NOT NULL,
	[MaTokenQr] [nvarchar](60) NOT NULL,
	[MaDonHang] [int] NOT NULL,
	[MaDotGiao] [int] NULL,
	[TenNguoiGui] [nvarchar](150) NOT NULL,
	[TenNguoiNhan] [nvarchar](150) NOT NULL,
	[LoiChuc] [nvarchar](1000) NOT NULL,
	[MauGiaoDien] [nvarchar](100) NOT NULL,
	[SoLanMoThiep] [int] NOT NULL,
	[NgayTao] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaThiep] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaTokenQr] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ThongSoDinhDuong]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ThongSoDinhDuong](
	[MaDD] [int] IDENTITY(1,1) NOT NULL,
	[MaBanh] [int] NOT NULL,
	[NangLuongCalo] [int] NOT NULL,
	[LuongDuongGram] [decimal](6, 2) NOT NULL,
	[ChatDamGram] [decimal](6, 2) NOT NULL,
	[ChatBeoGram] [decimal](6, 2) NOT NULL,
	[ChiSoDuongHuyet_GI] [int] NOT NULL,
	[CanhBaoDiUng] [nvarchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaDD] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaBanh] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[ThuongHieu]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ThuongHieu](
	[MaThuongHieu] [int] IDENTITY(1,1) NOT NULL,
	[MaCodeThuongHieu] [varchar](30) NOT NULL,
	[TenThuongHieu] [nvarchar](120) NOT NULL,
	[XuatXu] [nvarchar](150) NULL,
	[MoTa] [nvarchar](500) NULL,
	[DangHoatDong] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaThuongHieu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[MaCodeThuongHieu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[TenThuongHieu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO
IF OBJECT_ID(N'[dbo].[VaiTro]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[VaiTro](
	[MaVT] [varchar](20) NOT NULL,
	[TenVT] [nvarchar](100) NOT NULL,
	[MoTa] [nvarchar](250) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaVT] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
END;
GO

-- Bổ sung cột mở rộng an toàn nếu nâng cấp từ phiên bản CSDL cũ
IF COL_LENGTH('dbo.SanPhamBanh', 'MaAnhChinh') IS NULL ALTER TABLE dbo.SanPhamBanh ADD MaAnhChinh INT NULL;
IF COL_LENGTH('dbo.MauVoHop', 'MaAnhChinh') IS NULL ALTER TABLE dbo.MauVoHop ADD MaAnhChinh INT NULL;
IF COL_LENGTH('dbo.ComboMauCoSan', 'MaAnhChinh') IS NULL ALTER TABLE dbo.ComboMauCoSan ADD MaAnhChinh INT NULL;
IF COL_LENGTH('dbo.DonHang', 'MaKM') IS NULL ALTER TABLE dbo.DonHang ADD MaKM INT NULL;
IF COL_LENGTH('dbo.DonHang', 'MaMocCK') IS NULL ALTER TABLE dbo.DonHang ADD MaMocCK INT NULL;
IF COL_LENGTH('dbo.PhieuKiemKho', 'MaNdKiem') IS NULL ALTER TABLE dbo.PhieuKiemKho ADD MaNdKiem INT NULL;
IF COL_LENGTH('dbo.CauHinhHeThong', 'MaNdCapNhat') IS NULL ALTER TABLE dbo.CauHinhHeThong ADD MaNdCapNhat INT NULL;
GO

-- Gắn đầy đủ Default Constraints an toàn (Idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[BangChietKhauSiB2B]') AND c.name = N'MienPhiInLogo')
    ALTER TABLE [dbo].[BangChietKhauSiB2B] ADD DEFAULT ((0)) FOR [MienPhiInLogo];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[BangChietKhauSiB2B]') AND c.name = N'PhiKhuonInLogo')
    ALTER TABLE [dbo].[BangChietKhauSiB2B] ADD DEFAULT ((500000)) FOR [PhiKhuonInLogo];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[BangChietKhauSiB2B]') AND c.name = N'SoNgaySanXuatCongThem')
    ALTER TABLE [dbo].[BangChietKhauSiB2B] ADD DEFAULT ((2)) FOR [SoNgaySanXuatCongThem];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[CauHinhHeThong]') AND c.name = N'NgayCapNhat')
    ALTER TABLE [dbo].[CauHinhHeThong] ADD DEFAULT (getdate()) FOR [NgayCapNhat];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[CauHinhLandingPage]') AND c.name = N'ThuTuHienThi')
    ALTER TABLE [dbo].[CauHinhLandingPage] ADD DEFAULT ((1)) FOR [ThuTuHienThi];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[CauHinhLandingPage]') AND c.name = N'DangKichHoat')
    ALTER TABLE [dbo].[CauHinhLandingPage] ADD DEFAULT ((1)) FOR [DangKichHoat];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ChiTietComboMau]') AND c.name = N'SoLuong')
    ALTER TABLE [dbo].[ChiTietComboMau] ADD DEFAULT ((1)) FOR [SoLuong];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ChiTietGioHang]') AND c.name = N'SoLuong')
    ALTER TABLE [dbo].[ChiTietGioHang] ADD DEFAULT ((1)) FOR [SoLuong];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ChiTietGioHang]') AND c.name = N'NgayThem')
    ALTER TABLE [dbo].[ChiTietGioHang] ADD DEFAULT (getdate()) FOR [NgayThem];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ChiTietPhieuNhap]') AND c.name = N'PhanTramChietKhau')
    ALTER TABLE [dbo].[ChiTietPhieuNhap] ADD DEFAULT ((0)) FOR [PhanTramChietKhau];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ChiTietPhieuNhap]') AND c.name = N'GiaNiemYet')
    ALTER TABLE [dbo].[ChiTietPhieuNhap] ADD DEFAULT ((0)) FOR [GiaNiemYet];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ComboMauCoSan]') AND c.name = N'TrangThai')
    ALTER TABLE [dbo].[ComboMauCoSan] ADD DEFAULT ((1)) FOR [TrangThai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ComboMauCoSan]') AND c.name = N'ViTriKho')
    ALTER TABLE [dbo].[ComboMauCoSan] ADD DEFAULT (N'Kệ B1') FOR [ViTriKho];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ComboMauCoSan]') AND c.name = N'DonViTinh')
    ALTER TABLE [dbo].[ComboMauCoSan] ADD DEFAULT (N'Hộp') FOR [DonViTinh];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ComboMauCoSan]') AND c.name = N'BanTrucTiep')
    ALTER TABLE [dbo].[ComboMauCoSan] ADD DEFAULT ((1)) FOR [BanTrucTiep];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ComboMauCoSan]') AND c.name = N'TongTonKho')
    ALTER TABLE [dbo].[ComboMauCoSan] ADD DEFAULT ((40)) FOR [TongTonKho];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ComboMauCoSan]') AND c.name = N'TrongLuongGram')
    ALTER TABLE [dbo].[ComboMauCoSan] ADD DEFAULT ((600)) FOR [TrongLuongGram];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DanhMucBanh]') AND c.name = N'ThuTuSX')
    ALTER TABLE [dbo].[DanhMucBanh] ADD DEFAULT ((1)) FOR [ThuTuSX];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DanhMucBanh]') AND c.name = N'TrangThai')
    ALTER TABLE [dbo].[DanhMucBanh] ADD DEFAULT ((1)) FOR [TrangThai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'LaDonDoanhNghiepB2B')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [LaDonDoanhNghiepB2B];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'PhanTramCK_B2B')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [PhanTramCK_B2B];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'TienChietKhauB2B')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [TienChietKhauB2B];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'TienGiamVoucher')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [TienGiamVoucher];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'PhiInLogo')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [PhiInLogo];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'TongPhiVanChuyen')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [TongPhiVanChuyen];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'PhuongThucTT')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ('COD') FOR [PhuongThucTT];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'TrangThaiTT')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ('CHUA_TT') FOR [TrangThaiTT];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'TrangThaiDH')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ('CHO_XAC_NHAN') FOR [TrangThaiDH];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'NgayDat')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT (getdate()) FOR [NgayDat];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'TienGiamHangVip')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [TienGiamHangVip];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'PhanTramGiamVip')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [PhanTramGiamVip];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'SoDiemDaDung')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [SoDiemDaDung];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'TienGiamDiemVip')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [TienGiamDiemVip];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'DaCongDiemThuong')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [DaCongDiemThuong];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'DaHoanDiemKhiHuy')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [DaHoanDiemKhiHuy];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'KenhBan')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ('WEB_ONLINE') FOR [KenhBan];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DonHang]') AND c.name = N'DaRollbackKho')
    ALTER TABLE [dbo].[DonHang] ADD DEFAULT ((0)) FOR [DaRollbackKho];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]') AND c.name = N'KhungGioGiao')
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem] ADD DEFAULT (N'Sáng (08:00 - 11:30)') FOR [KhungGioGiao];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]') AND c.name = N'PhiShipDotNay')
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem] ADD DEFAULT ((30000)) FOR [PhiShipDotNay];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]') AND c.name = N'TrangThaiGiao')
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem] ADD DEFAULT ('CHO_DONG_GOI') FOR [TrangThaiGiao];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]') AND c.name = N'CoKemThiepQR')
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem] ADD DEFAULT ((1)) FOR [CoKemThiepQR];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]') AND c.name = N'MauGiaoDienThiep')
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem] ADD DEFAULT ('CO_PHONG_HOANG_GIA') FOR [MauGiaoDienThiep];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]') AND c.name = N'SoLanMoThiep')
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem] ADD DEFAULT ((0)) FOR [SoLanMoThiep];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[GioHang]') AND c.name = N'LaCheDoMuaSiB2B')
    ALTER TABLE [dbo].[GioHang] ADD DEFAULT ((0)) FOR [LaCheDoMuaSiB2B];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[GioHang]') AND c.name = N'YeuCauInLogoB2B')
    ALTER TABLE [dbo].[GioHang] ADD DEFAULT ((0)) FOR [YeuCauInLogoB2B];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[GioHang]') AND c.name = N'NgayCapNhat')
    ALTER TABLE [dbo].[GioHang] ADD DEFAULT (getdate()) FOR [NgayCapNhat];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HopQuaTuChon]') AND c.name = N'TenHopKhachDat')
    ALTER TABLE [dbo].[HopQuaTuChon] ADD DEFAULT (N'Hộp Quà Tự Chọn') FOR [TenHopKhachDat];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HopQuaTuChon]') AND c.name = N'TongCaloHop')
    ALTER TABLE [dbo].[HopQuaTuChon] ADD DEFAULT ((0)) FOR [TongCaloHop];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HopQuaTuChon]') AND c.name = N'DaLuuMauYeuThich')
    ALTER TABLE [dbo].[HopQuaTuChon] ADD DEFAULT ((0)) FOR [DaLuuMauYeuThich];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HopQuaTuChon]') AND c.name = N'NgayTao')
    ALTER TABLE [dbo].[HopQuaTuChon] ADD DEFAULT (getdate()) FOR [NgayTao];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HoSoNhanVien]') AND c.name = N'ChiNhanh')
    ALTER TABLE [dbo].[HoSoNhanVien] ADD DEFAULT (N'TP. Long Xuyên') FOR [ChiNhanh];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HoSoNhanVien]') AND c.name = N'CaLamViec')
    ALTER TABLE [dbo].[HoSoNhanVien] ADD DEFAULT (N'Toàn thời gian') FOR [CaLamViec];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HoSoNhanVien]') AND c.name = N'NgayVaoLam')
    ALTER TABLE [dbo].[HoSoNhanVien] ADD DEFAULT (CONVERT([date],getdate())) FOR [NgayVaoLam];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[HoSoNhanVien]') AND c.name = N'TrangThaiLamViec')
    ALTER TABLE [dbo].[HoSoNhanVien] ADD DEFAULT ('DANG_LAM_VIEC') FOR [TrangThaiLamViec];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[KhoHinhAnh]') AND c.name = N'LoaiLuuTru')
    ALTER TABLE [dbo].[KhoHinhAnh] ADD DEFAULT ((1)) FOR [LoaiLuuTru];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[KhoHinhAnh]') AND c.name = N'NgayTaiLen')
    ALTER TABLE [dbo].[KhoHinhAnh] ADD DEFAULT (getdate()) FOR [NgayTaiLen];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[LienKetHinhAnh]') AND c.name = N'ThuTuHienThi')
    ALTER TABLE [dbo].[LienKetHinhAnh] ADD DEFAULT ((1)) FOR [ThuTuHienThi];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[LoSanXuatBanh]') AND c.name = N'PhanTramGiamCanDate')
    ALTER TABLE [dbo].[LoSanXuatBanh] ADD DEFAULT ((0)) FOR [PhanTramGiamCanDate];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[LoSanXuatBanh]') AND c.name = N'TrangThaiLo')
    ALTER TABLE [dbo].[LoSanXuatBanh] ADD DEFAULT ('DANG_BAN') FOR [TrangThaiLo];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND c.name = N'LoaiGiam')
    ALTER TABLE [dbo].[MaKhuyenMai] ADD DEFAULT ('PHAN_TRAM') FOR [LoaiGiam];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND c.name = N'GiamToiDa')
    ALTER TABLE [dbo].[MaKhuyenMai] ADD DEFAULT ((0)) FOR [GiamToiDa];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND c.name = N'DonToiThieu')
    ALTER TABLE [dbo].[MaKhuyenMai] ADD DEFAULT ((0)) FOR [DonToiThieu];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND c.name = N'SLToiDa')
    ALTER TABLE [dbo].[MaKhuyenMai] ADD DEFAULT ((100)) FOR [SLToiDa];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND c.name = N'SLDaDung')
    ALTER TABLE [dbo].[MaKhuyenMai] ADD DEFAULT ((0)) FOR [SLDaDung];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND c.name = N'ChoPhepKhachVangLai')
    ALTER TABLE [dbo].[MaKhuyenMai] ADD DEFAULT ((1)) FOR [ChoPhepKhachVangLai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND c.name = N'TrangThaiKichHoat')
    ALTER TABLE [dbo].[MaKhuyenMai] ADD DEFAULT ((1)) FOR [TrangThaiKichHoat];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MauVoHop]') AND c.name = N'SLBanhThuongHangToiThieu')
    ALTER TABLE [dbo].[MauVoHop] ADD DEFAULT ((0)) FOR [SLBanhThuongHangToiThieu];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MauVoHop]') AND c.name = N'KieuBoCucLuoi')
    ALTER TABLE [dbo].[MauVoHop] ADD DEFAULT ('grid-2x2') FOR [KieuBoCucLuoi];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MauVoHop]') AND c.name = N'KemTraOLong')
    ALTER TABLE [dbo].[MauVoHop] ADD DEFAULT ((0)) FOR [KemTraOLong];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MauVoHop]') AND c.name = N'SLTonKhoVo')
    ALTER TABLE [dbo].[MauVoHop] ADD DEFAULT ((50)) FOR [SLTonKhoVo];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[MauVoHop]') AND c.name = N'TrangThai')
    ALTER TABLE [dbo].[MauVoHop] ADD DEFAULT ((1)) FOR [TrangThai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NguoiDung]') AND c.name = N'MaVT')
    ALTER TABLE [dbo].[NguoiDung] ADD DEFAULT ('KHACH_TV') FOR [MaVT];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NguoiDung]') AND c.name = N'DiemTichLuy')
    ALTER TABLE [dbo].[NguoiDung] ADD DEFAULT ((0)) FOR [DiemTichLuy];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NguoiDung]') AND c.name = N'HangTV')
    ALTER TABLE [dbo].[NguoiDung] ADD DEFAULT (N'Đồng') FOR [HangTV];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NguoiDung]') AND c.name = N'TrangThai')
    ALTER TABLE [dbo].[NguoiDung] ADD DEFAULT ((1)) FOR [TrangThai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NguoiDung]') AND c.name = N'NgayTao')
    ALTER TABLE [dbo].[NguoiDung] ADD DEFAULT (getdate()) FOR [NgayTao];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NhaCungCap]') AND c.name = N'LoaiNcc')
    ALTER TABLE [dbo].[NhaCungCap] ADD DEFAULT (N'HANG_CHINH_HANG') FOR [LoaiNcc];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NhaCungCap]') AND c.name = N'ThuongHieuCungCap')
    ALTER TABLE [dbo].[NhaCungCap] ADD DEFAULT (N'Đa thương hiệu') FOR [ThuongHieuCungCap];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NhaCungCap]') AND c.name = N'PhanTramChietKhauMacDinh')
    ALTER TABLE [dbo].[NhaCungCap] ADD DEFAULT ((0)) FOR [PhanTramChietKhauMacDinh];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NhaCungCap]') AND c.name = N'DangHoatDong')
    ALTER TABLE [dbo].[NhaCungCap] ADD DEFAULT ((1)) FOR [DangHoatDong];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NhaCungCap]') AND c.name = N'NgayTao')
    ALTER TABLE [dbo].[NhaCungCap] ADD DEFAULT (getdate()) FOR [NgayTao];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NhatKyTruKhoDonHang]') AND c.name = N'DaHoanKho')
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang] ADD DEFAULT ((0)) FOR [DaHoanKho];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[NhatKyTruKhoDonHang]') AND c.name = N'NgayGhi')
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang] ADD DEFAULT (getdate()) FOR [NgayGhi];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhienKhachVangLai]') AND c.name = N'DaGopGioHang')
    ALTER TABLE [dbo].[PhienKhachVangLai] ADD DEFAULT ((0)) FOR [DaGopGioHang];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhienKhachVangLai]') AND c.name = N'NgayTao')
    ALTER TABLE [dbo].[PhienKhachVangLai] ADD DEFAULT (getdate()) FOR [NgayTao];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhienKhachVangLai]') AND c.name = N'NgayHetHan')
    ALTER TABLE [dbo].[PhienKhachVangLai] ADD DEFAULT (dateadd(day,(14),getdate())) FOR [NgayHetHan];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKhoVoHop]') AND c.name = N'DonGiaNhap')
    ALTER TABLE [dbo].[PhieuKhoVoHop] ADD DEFAULT ((0)) FOR [DonGiaNhap];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKhoVoHop]') AND c.name = N'ThanhTien')
    ALTER TABLE [dbo].[PhieuKhoVoHop] ADD DEFAULT ((0)) FOR [ThanhTien];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKhoVoHop]') AND c.name = N'NgayGiaoDich')
    ALTER TABLE [dbo].[PhieuKhoVoHop] ADD DEFAULT (getdate()) FOR [NgayGiaoDich];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKiemKho]') AND c.name = N'NgayKiem')
    ALTER TABLE [dbo].[PhieuKiemKho] ADD DEFAULT (getdate()) FOR [NgayKiem];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKiemKho]') AND c.name = N'TrangThai')
    ALTER TABLE [dbo].[PhieuKiemKho] ADD DEFAULT ('PHIEU_TAM') FOR [TrangThai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKiemKho]') AND c.name = N'TongLechTang')
    ALTER TABLE [dbo].[PhieuKiemKho] ADD DEFAULT ((0)) FOR [TongLechTang];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKiemKho]') AND c.name = N'TongLechGiam')
    ALTER TABLE [dbo].[PhieuKiemKho] ADD DEFAULT ((0)) FOR [TongLechGiam];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuKiemKho]') AND c.name = N'TongGiaTriLech')
    ALTER TABLE [dbo].[PhieuKiemKho] ADD DEFAULT ((0)) FOR [TongGiaTriLech];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND c.name = N'NgayNhap')
    ALTER TABLE [dbo].[PhieuNhapKho] ADD DEFAULT (getdate()) FOR [NgayNhap];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND c.name = N'TongTienHang')
    ALTER TABLE [dbo].[PhieuNhapKho] ADD DEFAULT ((0)) FOR [TongTienHang];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND c.name = N'GiamGia')
    ALTER TABLE [dbo].[PhieuNhapKho] ADD DEFAULT ((0)) FOR [GiamGia];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND c.name = N'TongPhaiTra')
    ALTER TABLE [dbo].[PhieuNhapKho] ADD DEFAULT ((0)) FOR [TongPhaiTra];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND c.name = N'DaThanhToan')
    ALTER TABLE [dbo].[PhieuNhapKho] ADD DEFAULT ((0)) FOR [DaThanhToan];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND c.name = N'TrangThai')
    ALTER TABLE [dbo].[PhieuNhapKho] ADD DEFAULT ('PHIEU_TAM') FOR [TrangThai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND c.name = N'TrangThaiThanhToan')
    ALTER TABLE [dbo].[PhieuNhapKho] ADD DEFAULT ('CHUA_THANH_TOAN') FOR [TrangThaiThanhToan];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuThuChi]') AND c.name = N'NgayLap')
    ALTER TABLE [dbo].[PhieuThuChi] ADD DEFAULT (getdate()) FOR [NgayLap];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuXuatKho]') AND c.name = N'NgayXuat')
    ALTER TABLE [dbo].[PhieuXuatKho] ADD DEFAULT (getdate()) FOR [NgayXuat];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuXuatKho]') AND c.name = N'TongGiaTriVon')
    ALTER TABLE [dbo].[PhieuXuatKho] ADD DEFAULT ((0)) FOR [TongGiaTriVon];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PhieuXuatKho]') AND c.name = N'TrangThai')
    ALTER TABLE [dbo].[PhieuXuatKho] ADD DEFAULT ('HOAN_THANH') FOR [TrangThai];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'GiaVon')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((0)) FOR [GiaVon];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'TrongLuongGram')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((150)) FOR [TrongLuongGram];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'SoTrungMuoi')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((1)) FOR [SoTrungMuoi];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'MucDoNgot')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ('BINH_THUONG') FOR [MucDoNgot];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'LaBanhChay')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((0)) FOR [LaBanhChay];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'LaBanhThuongHang')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((0)) FOR [LaBanhThuongHang];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'NhomHuongVi')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ('MAN_TRUYENTHONG') FOR [NhomHuongVi];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'TongTonKho')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((0)) FOR [TongTonKho];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'TrangThaiBan')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((1)) FOR [TrangThaiBan];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'ViTriKho')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT (N'Kệ A1') FOR [ViTriKho];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'DonViTinh')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT (N'Cái') FOR [DonViTinh];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND c.name = N'BanTrucTiep')
    ALTER TABLE [dbo].[SanPhamBanh] ADD DEFAULT ((1)) FOR [BanTrucTiep];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SoDiaChi]') AND c.name = N'LaMacDinh')
    ALTER TABLE [dbo].[SoDiaChi] ADD DEFAULT ((0)) FOR [LaMacDinh];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ThiepChucMung3D]') AND c.name = N'SoLanMoThiep')
    ALTER TABLE [dbo].[ThiepChucMung3D] ADD DEFAULT ((0)) FOR [SoLanMoThiep];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ThiepChucMung3D]') AND c.name = N'NgayTao')
    ALTER TABLE [dbo].[ThiepChucMung3D] ADD DEFAULT (getdate()) FOR [NgayTao];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ThongSoDinhDuong]') AND c.name = N'ChiSoDuongHuyet_GI')
    ALTER TABLE [dbo].[ThongSoDinhDuong] ADD DEFAULT ((55)) FOR [ChiSoDuongHuyet_GI];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ThuongHieu]') AND c.name = N'DangHoatDong')
    ALTER TABLE [dbo].[ThuongHieu] ADD DEFAULT ((1)) FOR [DangHoatDong];
GO

-- Khởi tạo Filtered Unique Index cho Token Thiệp 3D QR
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DotGiaoHang_MaTokenThiepQR_Filtered' AND object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DotGiaoHang_MaTokenThiepQR_Filtered] ON [dbo].[DotGiaoHang_DaDiem] ([MaTokenThiepQR] ASC) WHERE ([MaTokenThiepQR] IS NOT NULL);
GO

-- =============================================================================
-- PHẦN 2: RESET SẠCH TOÀN BỘ 37 BẢNG, RESEED IDENTITY & NỐI ĐỦ 71 KHÓA NGOẠI
-- =============================================================================
BEGIN TRANSACTION;

UPDATE [dbo].[DanhMucBanh] SET [MaDMCha] = NULL;
DELETE FROM [dbo].[PhieuKhoVoHop];
DELETE FROM [dbo].[NhatKyTruKhoDonHang];
DELETE FROM [dbo].[ThiepChucMung3D];
DELETE FROM [dbo].[PhieuThuChi];
DELETE FROM [dbo].[ChiTietKiemKho];
DELETE FROM [dbo].[PhieuKiemKho];
DELETE FROM [dbo].[ChiTietPhieuXuat];
DELETE FROM [dbo].[PhieuXuatKho];
DELETE FROM [dbo].[ChiTietPhieuNhap];
DELETE FROM [dbo].[PhieuNhapKho];
DELETE FROM [dbo].[ChiTietDonHang];
DELETE FROM [dbo].[DotGiaoHang_DaDiem];
DELETE FROM [dbo].[DonHang];
DELETE FROM [dbo].[ChiTietGioHang];
DELETE FROM [dbo].[GioHang];
DELETE FROM [dbo].[ChiTietHopTuChon];
DELETE FROM [dbo].[HopQuaTuChon];
DELETE FROM [dbo].[PhienKhachVangLai];
DELETE FROM [dbo].[ChiTietComboMau];
DELETE FROM [dbo].[ComboMauCoSan];
DELETE FROM [dbo].[LienKetHinhAnh];
DELETE FROM [dbo].[CauHinhLandingPage];
DELETE FROM [dbo].[LoSanXuatBanh];
DELETE FROM [dbo].[ThongSoDinhDuong];
DELETE FROM [dbo].[SanPhamBanh];
DELETE FROM [dbo].[MauVoHop];
DELETE FROM [dbo].[KhoHinhAnh];
DELETE FROM [dbo].[DanhMucBanh];
DELETE FROM [dbo].[ThuongHieu];
DELETE FROM [dbo].[NhaCungCap];
DELETE FROM [dbo].[MaKhuyenMai];
DELETE FROM [dbo].[BangChietKhauSiB2B];
DELETE FROM [dbo].[CauHinhHeThong];
DELETE FROM [dbo].[SoDiaChi];
DELETE FROM [dbo].[HoSoNhanVien];
DELETE FROM [dbo].[NguoiDung];
DELETE FROM [dbo].[VaiTro];

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[PhieuKhoVoHop]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[PhieuKhoVoHop]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[NhatKyTruKhoDonHang]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[NhatKyTruKhoDonHang]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ThiepChucMung3D]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ThiepChucMung3D]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[PhieuThuChi]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[PhieuThuChi]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietKiemKho]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ChiTietKiemKho]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[PhieuKiemKho]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[PhieuKiemKho]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietPhieuXuat]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ChiTietPhieuXuat]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[PhieuXuatKho]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[PhieuXuatKho]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietPhieuNhap]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ChiTietPhieuNhap]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[PhieuNhapKho]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[PhieuNhapKho]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietDonHang]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ChiTietDonHang]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[DotGiaoHang_DaDiem]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[DotGiaoHang_DaDiem]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[DonHang]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[DonHang]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietGioHang]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ChiTietGioHang]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[GioHang]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[GioHang]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietHopTuChon]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ChiTietHopTuChon]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[HopQuaTuChon]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[HopQuaTuChon]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ChiTietComboMau]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ChiTietComboMau]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ComboMauCoSan]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ComboMauCoSan]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[LienKetHinhAnh]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[LienKetHinhAnh]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[CauHinhLandingPage]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[CauHinhLandingPage]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[LoSanXuatBanh]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[LoSanXuatBanh]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ThongSoDinhDuong]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ThongSoDinhDuong]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[SanPhamBanh]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[SanPhamBanh]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[MauVoHop]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[MauVoHop]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[KhoHinhAnh]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[KhoHinhAnh]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[DanhMucBanh]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[DanhMucBanh]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[ThuongHieu]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[ThuongHieu]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[NhaCungCap]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[NhaCungCap]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[MaKhuyenMai]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[MaKhuyenMai]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[BangChietKhauSiB2B]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[BangChietKhauSiB2B]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[SoDiaChi]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[SoDiaChi]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[HoSoNhanVien]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[HoSoNhanVien]', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'[dbo].[NguoiDung]') AND last_value IS NOT NULL) DBCC CHECKIDENT (N'[dbo].[NguoiDung]', RESEED, 0);


-- Gắn đầy đủ 71 Khóa Ngoại liên thông 37/37 bảng (Idempotent)
IF OBJECT_ID(N'[dbo].[FK_CauHinhHeThong_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[CauHinhHeThong]  WITH CHECK ADD  CONSTRAINT [FK_CauHinhHeThong_NguoiDung] FOREIGN KEY([MaNdCapNhat]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[CauHinhHeThong] CHECK CONSTRAINT [FK_CauHinhHeThong_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_Landing_AnhNen]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[CauHinhLandingPage]  WITH CHECK ADD  CONSTRAINT [FK_Landing_AnhNen] FOREIGN KEY([MaAnhNen]) REFERENCES [dbo].[KhoHinhAnh] ([MaHA]);
    ALTER TABLE [dbo].[CauHinhLandingPage] CHECK CONSTRAINT [FK_Landing_AnhNen];
END;
IF OBJECT_ID(N'[dbo].[FK_Landing_AnhPhu]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[CauHinhLandingPage]  WITH CHECK ADD  CONSTRAINT [FK_Landing_AnhPhu] FOREIGN KEY([MaAnhPhu_Parallax]) REFERENCES [dbo].[KhoHinhAnh] ([MaHA]);
    ALTER TABLE [dbo].[CauHinhLandingPage] CHECK CONSTRAINT [FK_Landing_AnhPhu];
END;
IF OBJECT_ID(N'[dbo].[FK_CTCB_Banh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietComboMau]  WITH CHECK ADD  CONSTRAINT [FK_CTCB_Banh] FOREIGN KEY([MaBanh]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[ChiTietComboMau] CHECK CONSTRAINT [FK_CTCB_Banh];
END;
IF OBJECT_ID(N'[dbo].[FK_CTCB_Combo]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietComboMau]  WITH CHECK ADD  CONSTRAINT [FK_CTCB_Combo] FOREIGN KEY([MaCB]) REFERENCES [dbo].[ComboMauCoSan] ([MaCB]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ChiTietComboMau] CHECK CONSTRAINT [FK_CTCB_Combo];
END;
IF OBJECT_ID(N'[dbo].[FK_CTDH_ComboMau]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietDonHang]  WITH CHECK ADD  CONSTRAINT [FK_CTDH_ComboMau] FOREIGN KEY([MaCB]) REFERENCES [dbo].[ComboMauCoSan] ([MaCB]);
    ALTER TABLE [dbo].[ChiTietDonHang] CHECK CONSTRAINT [FK_CTDH_ComboMau];
END;
IF OBJECT_ID(N'[dbo].[FK_CTDH_DonHang]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietDonHang]  WITH CHECK ADD  CONSTRAINT [FK_CTDH_DonHang] FOREIGN KEY([MaDH]) REFERENCES [dbo].[DonHang] ([MaDH]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ChiTietDonHang] CHECK CONSTRAINT [FK_CTDH_DonHang];
END;
IF OBJECT_ID(N'[dbo].[FK_CTDH_DotGH]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietDonHang]  WITH CHECK ADD  CONSTRAINT [FK_CTDH_DotGH] FOREIGN KEY([MaDotGH]) REFERENCES [dbo].[DotGiaoHang_DaDiem] ([MaDotGH]);
    ALTER TABLE [dbo].[ChiTietDonHang] CHECK CONSTRAINT [FK_CTDH_DotGH];
END;
IF OBJECT_ID(N'[dbo].[FK_CTDH_HopTuChon]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietDonHang]  WITH CHECK ADD  CONSTRAINT [FK_CTDH_HopTuChon] FOREIGN KEY([MaHopTC]) REFERENCES [dbo].[HopQuaTuChon] ([MaHopTC]);
    ALTER TABLE [dbo].[ChiTietDonHang] CHECK CONSTRAINT [FK_CTDH_HopTuChon];
END;
IF OBJECT_ID(N'[dbo].[FK_CTDH_SanPhamBanh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietDonHang]  WITH CHECK ADD  CONSTRAINT [FK_CTDH_SanPhamBanh] FOREIGN KEY([MaBanh]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[ChiTietDonHang] CHECK CONSTRAINT [FK_CTDH_SanPhamBanh];
END;
IF OBJECT_ID(N'[dbo].[FK_CTGH_Banh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietGioHang]  WITH CHECK ADD  CONSTRAINT [FK_CTGH_Banh] FOREIGN KEY([MaBanh]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[ChiTietGioHang] CHECK CONSTRAINT [FK_CTGH_Banh];
END;
IF OBJECT_ID(N'[dbo].[FK_CTGH_Combo]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietGioHang]  WITH CHECK ADD  CONSTRAINT [FK_CTGH_Combo] FOREIGN KEY([MaCB]) REFERENCES [dbo].[ComboMauCoSan] ([MaCB]);
    ALTER TABLE [dbo].[ChiTietGioHang] CHECK CONSTRAINT [FK_CTGH_Combo];
END;
IF OBJECT_ID(N'[dbo].[FK_CTGH_GioHang]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietGioHang]  WITH CHECK ADD  CONSTRAINT [FK_CTGH_GioHang] FOREIGN KEY([MaGH]) REFERENCES [dbo].[GioHang] ([MaGH]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ChiTietGioHang] CHECK CONSTRAINT [FK_CTGH_GioHang];
END;
IF OBJECT_ID(N'[dbo].[FK_CTGH_HopTC]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietGioHang]  WITH CHECK ADD  CONSTRAINT [FK_CTGH_HopTC] FOREIGN KEY([MaHopTC]) REFERENCES [dbo].[HopQuaTuChon] ([MaHopTC]);
    ALTER TABLE [dbo].[ChiTietGioHang] CHECK CONSTRAINT [FK_CTGH_HopTC];
END;
IF OBJECT_ID(N'[dbo].[FK_CTHopTC_Banh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietHopTuChon]  WITH CHECK ADD  CONSTRAINT [FK_CTHopTC_Banh] FOREIGN KEY([MaBanh]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[ChiTietHopTuChon] CHECK CONSTRAINT [FK_CTHopTC_Banh];
END;
IF OBJECT_ID(N'[dbo].[FK_CTHopTC_HopTC]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietHopTuChon]  WITH CHECK ADD  CONSTRAINT [FK_CTHopTC_HopTC] FOREIGN KEY([MaHopTC]) REFERENCES [dbo].[HopQuaTuChon] ([MaHopTC]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ChiTietHopTuChon] CHECK CONSTRAINT [FK_CTHopTC_HopTC];
END;
IF OBJECT_ID(N'[dbo].[FK_CTKK_PhieuKiemKho]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietKiemKho]  WITH CHECK ADD  CONSTRAINT [FK_CTKK_PhieuKiemKho] FOREIGN KEY([MaPkk]) REFERENCES [dbo].[PhieuKiemKho] ([MaPkk]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ChiTietKiemKho] CHECK CONSTRAINT [FK_CTKK_PhieuKiemKho];
END;
IF OBJECT_ID(N'[dbo].[FK_CTKK_SanPham]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietKiemKho]  WITH CHECK ADD  CONSTRAINT [FK_CTKK_SanPham] FOREIGN KEY([MaSp]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[ChiTietKiemKho] CHECK CONSTRAINT [FK_CTKK_SanPham];
END;
IF OBJECT_ID(N'[dbo].[FK_CTPN_LoSanXuat]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietPhieuNhap]  WITH CHECK ADD  CONSTRAINT [FK_CTPN_LoSanXuat] FOREIGN KEY([MaLo]) REFERENCES [dbo].[LoSanXuatBanh] ([MaLo]);
    ALTER TABLE [dbo].[ChiTietPhieuNhap] CHECK CONSTRAINT [FK_CTPN_LoSanXuat];
END;
IF OBJECT_ID(N'[dbo].[FK_CTPN_PhieuNhap]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietPhieuNhap]  WITH CHECK ADD  CONSTRAINT [FK_CTPN_PhieuNhap] FOREIGN KEY([MaPn]) REFERENCES [dbo].[PhieuNhapKho] ([MaPn]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ChiTietPhieuNhap] CHECK CONSTRAINT [FK_CTPN_PhieuNhap];
END;
IF OBJECT_ID(N'[dbo].[FK_CTPN_SanPham]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietPhieuNhap]  WITH CHECK ADD  CONSTRAINT [FK_CTPN_SanPham] FOREIGN KEY([MaSp]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[ChiTietPhieuNhap] CHECK CONSTRAINT [FK_CTPN_SanPham];
END;
IF OBJECT_ID(N'[dbo].[FK_CTPX_LoSanXuat]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietPhieuXuat]  WITH CHECK ADD  CONSTRAINT [FK_CTPX_LoSanXuat] FOREIGN KEY([MaLo]) REFERENCES [dbo].[LoSanXuatBanh] ([MaLo]);
    ALTER TABLE [dbo].[ChiTietPhieuXuat] CHECK CONSTRAINT [FK_CTPX_LoSanXuat];
END;
IF OBJECT_ID(N'[dbo].[FK_CTPX_PhieuXuat]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietPhieuXuat]  WITH CHECK ADD  CONSTRAINT [FK_CTPX_PhieuXuat] FOREIGN KEY([MaPx]) REFERENCES [dbo].[PhieuXuatKho] ([MaPx]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ChiTietPhieuXuat] CHECK CONSTRAINT [FK_CTPX_PhieuXuat];
END;
IF OBJECT_ID(N'[dbo].[FK_CTPX_SanPham]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiTietPhieuXuat]  WITH CHECK ADD  CONSTRAINT [FK_CTPX_SanPham] FOREIGN KEY([MaSp]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[ChiTietPhieuXuat] CHECK CONSTRAINT [FK_CTPX_SanPham];
END;
IF OBJECT_ID(N'[dbo].[FK_Combo_AnhChinh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ComboMauCoSan]  WITH CHECK ADD  CONSTRAINT [FK_Combo_AnhChinh] FOREIGN KEY([MaAnhChinh]) REFERENCES [dbo].[KhoHinhAnh] ([MaHA]);
    ALTER TABLE [dbo].[ComboMauCoSan] CHECK CONSTRAINT [FK_Combo_AnhChinh];
END;
IF OBJECT_ID(N'[dbo].[FK_Combo_DanhMuc]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ComboMauCoSan]  WITH CHECK ADD  CONSTRAINT [FK_Combo_DanhMuc] FOREIGN KEY([MaDM]) REFERENCES [dbo].[DanhMucBanh] ([MaDM]);
    ALTER TABLE [dbo].[ComboMauCoSan] CHECK CONSTRAINT [FK_Combo_DanhMuc];
END;
IF OBJECT_ID(N'[dbo].[FK_Combo_MauVoHop]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ComboMauCoSan]  WITH CHECK ADD  CONSTRAINT [FK_Combo_MauVoHop] FOREIGN KEY([MaVH]) REFERENCES [dbo].[MauVoHop] ([MaVH]);
    ALTER TABLE [dbo].[ComboMauCoSan] CHECK CONSTRAINT [FK_Combo_MauVoHop];
END;
IF OBJECT_ID(N'[dbo].[FK_Combo_ThuongHieu]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ComboMauCoSan]  WITH CHECK ADD  CONSTRAINT [FK_Combo_ThuongHieu] FOREIGN KEY([MaThuongHieu]) REFERENCES [dbo].[ThuongHieu] ([MaThuongHieu]);
    ALTER TABLE [dbo].[ComboMauCoSan] CHECK CONSTRAINT [FK_Combo_ThuongHieu];
END;
IF OBJECT_ID(N'[dbo].[FK_DMBanh_Cha]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DanhMucBanh]  WITH CHECK ADD  CONSTRAINT [FK_DMBanh_Cha] FOREIGN KEY([MaDMCha]) REFERENCES [dbo].[DanhMucBanh] ([MaDM]);
    ALTER TABLE [dbo].[DanhMucBanh] CHECK CONSTRAINT [FK_DMBanh_Cha];
END;
IF OBJECT_ID(N'[dbo].[FK_DH_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DonHang]  WITH CHECK ADD  CONSTRAINT [FK_DH_NguoiDung] FOREIGN KEY([MaND]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[DonHang] CHECK CONSTRAINT [FK_DH_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_DH_PhienKVL]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DonHang]  WITH CHECK ADD  CONSTRAINT [FK_DH_PhienKVL] FOREIGN KEY([MaPhienKVL]) REFERENCES [dbo].[PhienKhachVangLai] ([MaPhienKVL]);
    ALTER TABLE [dbo].[DonHang] CHECK CONSTRAINT [FK_DH_PhienKVL];
END;
IF OBJECT_ID(N'[dbo].[FK_DonHang_AnhLogoCongTy]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DonHang]  WITH CHECK ADD  CONSTRAINT [FK_DonHang_AnhLogoCongTy] FOREIGN KEY([MaAnhLogoCongTy]) REFERENCES [dbo].[KhoHinhAnh] ([MaHA]);
    ALTER TABLE [dbo].[DonHang] CHECK CONSTRAINT [FK_DonHang_AnhLogoCongTy];
END;
IF OBJECT_ID(N'[dbo].[FK_DonHang_BangChietKhau]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DonHang]  WITH CHECK ADD  CONSTRAINT [FK_DonHang_BangChietKhau] FOREIGN KEY([MaMocCK]) REFERENCES [dbo].[BangChietKhauSiB2B] ([MaMocCK]);
    ALTER TABLE [dbo].[DonHang] CHECK CONSTRAINT [FK_DonHang_BangChietKhau];
END;
IF OBJECT_ID(N'[dbo].[FK_DonHang_MaKhuyenMai]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DonHang]  WITH CHECK ADD  CONSTRAINT [FK_DonHang_MaKhuyenMai] FOREIGN KEY([MaKM]) REFERENCES [dbo].[MaKhuyenMai] ([MaKM]);
    ALTER TABLE [dbo].[DonHang] CHECK CONSTRAINT [FK_DonHang_MaKhuyenMai];
END;
IF OBJECT_ID(N'[dbo].[FK_DonHang_NguoiLap]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DonHang]  WITH CHECK ADD  CONSTRAINT [FK_DonHang_NguoiLap] FOREIGN KEY([MaNdLap]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[DonHang] CHECK CONSTRAINT [FK_DonHang_NguoiLap];
END;
IF OBJECT_ID(N'[dbo].[FK_DotGH_DonHang]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem]  WITH CHECK ADD  CONSTRAINT [FK_DotGH_DonHang] FOREIGN KEY([MaDH]) REFERENCES [dbo].[DonHang] ([MaDH]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[DotGiaoHang_DaDiem] CHECK CONSTRAINT [FK_DotGH_DonHang];
END;
IF OBJECT_ID(N'[dbo].[FK_GH_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[GioHang]  WITH CHECK ADD  CONSTRAINT [FK_GH_NguoiDung] FOREIGN KEY([MaND]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[GioHang] CHECK CONSTRAINT [FK_GH_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_GH_PhienKVL]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[GioHang]  WITH CHECK ADD  CONSTRAINT [FK_GH_PhienKVL] FOREIGN KEY([MaPhienKVL]) REFERENCES [dbo].[PhienKhachVangLai] ([MaPhienKVL]);
    ALTER TABLE [dbo].[GioHang] CHECK CONSTRAINT [FK_GH_PhienKVL];
END;
IF OBJECT_ID(N'[dbo].[FK_GioHang_MaKhuyenMai]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[GioHang]  WITH CHECK ADD  CONSTRAINT [FK_GioHang_MaKhuyenMai] FOREIGN KEY([CodeKMDangApDung]) REFERENCES [dbo].[MaKhuyenMai] ([CodeKM]);
    ALTER TABLE [dbo].[GioHang] CHECK CONSTRAINT [FK_GioHang_MaKhuyenMai];
END;
IF OBJECT_ID(N'[dbo].[FK_HopTC_MauVoHop]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[HopQuaTuChon]  WITH CHECK ADD  CONSTRAINT [FK_HopTC_MauVoHop] FOREIGN KEY([MaVH]) REFERENCES [dbo].[MauVoHop] ([MaVH]);
    ALTER TABLE [dbo].[HopQuaTuChon] CHECK CONSTRAINT [FK_HopTC_MauVoHop];
END;
IF OBJECT_ID(N'[dbo].[FK_HopTC_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[HopQuaTuChon]  WITH CHECK ADD  CONSTRAINT [FK_HopTC_NguoiDung] FOREIGN KEY([MaND]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[HopQuaTuChon] CHECK CONSTRAINT [FK_HopTC_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_HopTC_PhienKVL]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[HopQuaTuChon]  WITH CHECK ADD  CONSTRAINT [FK_HopTC_PhienKVL] FOREIGN KEY([MaPhienKVL]) REFERENCES [dbo].[PhienKhachVangLai] ([MaPhienKVL]);
    ALTER TABLE [dbo].[HopQuaTuChon] CHECK CONSTRAINT [FK_HopTC_PhienKVL];
END;
IF OBJECT_ID(N'[dbo].[FK_HoSoNhanVien_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[HoSoNhanVien]  WITH CHECK ADD  CONSTRAINT [FK_HoSoNhanVien_NguoiDung] FOREIGN KEY([MaNd]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[HoSoNhanVien] CHECK CONSTRAINT [FK_HoSoNhanVien_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_LKHA_KhoHA]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[LienKetHinhAnh]  WITH CHECK ADD  CONSTRAINT [FK_LKHA_KhoHA] FOREIGN KEY([MaHA]) REFERENCES [dbo].[KhoHinhAnh] ([MaHA]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[LienKetHinhAnh] CHECK CONSTRAINT [FK_LKHA_KhoHA];
END;
IF OBJECT_ID(N'[dbo].[FK_LoSX_Banh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[LoSanXuatBanh]  WITH CHECK ADD  CONSTRAINT [FK_LoSX_Banh] FOREIGN KEY([MaBanh]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[LoSanXuatBanh] CHECK CONSTRAINT [FK_LoSX_Banh];
END;
IF OBJECT_ID(N'[dbo].[FK_MauVoHop_AnhChinh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[MauVoHop]  WITH CHECK ADD  CONSTRAINT [FK_MauVoHop_AnhChinh] FOREIGN KEY([MaAnhChinh]) REFERENCES [dbo].[KhoHinhAnh] ([MaHA]);
    ALTER TABLE [dbo].[MauVoHop] CHECK CONSTRAINT [FK_MauVoHop_AnhChinh];
END;
IF OBJECT_ID(N'[dbo].[FK_ND_VaiTro]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[NguoiDung]  WITH CHECK ADD  CONSTRAINT [FK_ND_VaiTro] FOREIGN KEY([MaVT]) REFERENCES [dbo].[VaiTro] ([MaVT]);
    ALTER TABLE [dbo].[NguoiDung] CHECK CONSTRAINT [FK_ND_VaiTro];
END;
IF OBJECT_ID(N'[dbo].[FK_NKTK_DonHang]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang]  WITH CHECK ADD  CONSTRAINT [FK_NKTK_DonHang] FOREIGN KEY([MaDH]) REFERENCES [dbo].[DonHang] ([MaDH]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang] CHECK CONSTRAINT [FK_NKTK_DonHang];
END;
IF OBJECT_ID(N'[dbo].[FK_NKTK_LoSanXuat]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang]  WITH CHECK ADD  CONSTRAINT [FK_NKTK_LoSanXuat] FOREIGN KEY([MaLo]) REFERENCES [dbo].[LoSanXuatBanh] ([MaLo]);
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang] CHECK CONSTRAINT [FK_NKTK_LoSanXuat];
END;
IF OBJECT_ID(N'[dbo].[FK_NKTK_MauVoHop]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang]  WITH CHECK ADD  CONSTRAINT [FK_NKTK_MauVoHop] FOREIGN KEY([MaVH]) REFERENCES [dbo].[MauVoHop] ([MaVH]);
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang] CHECK CONSTRAINT [FK_NKTK_MauVoHop];
END;
IF OBJECT_ID(N'[dbo].[FK_NKTK_SanPhamBanh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang]  WITH CHECK ADD  CONSTRAINT [FK_NKTK_SanPhamBanh] FOREIGN KEY([MaBanh]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]);
    ALTER TABLE [dbo].[NhatKyTruKhoDonHang] CHECK CONSTRAINT [FK_NKTK_SanPhamBanh];
END;
IF OBJECT_ID(N'[dbo].[FK_PhienKVL_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhienKhachVangLai]  WITH CHECK ADD  CONSTRAINT [FK_PhienKVL_NguoiDung] FOREIGN KEY([MaND_DaGop]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[PhienKhachVangLai] CHECK CONSTRAINT [FK_PhienKVL_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_PKVH_MauVoHop]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuKhoVoHop]  WITH CHECK ADD  CONSTRAINT [FK_PKVH_MauVoHop] FOREIGN KEY([MaVH]) REFERENCES [dbo].[MauVoHop] ([MaVH]);
    ALTER TABLE [dbo].[PhieuKhoVoHop] CHECK CONSTRAINT [FK_PKVH_MauVoHop];
END;
IF OBJECT_ID(N'[dbo].[FK_PKVH_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuKhoVoHop]  WITH CHECK ADD  CONSTRAINT [FK_PKVH_NguoiDung] FOREIGN KEY([MaNdLap]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[PhieuKhoVoHop] CHECK CONSTRAINT [FK_PKVH_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_PKVH_NhaCungCap]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuKhoVoHop]  WITH CHECK ADD  CONSTRAINT [FK_PKVH_NhaCungCap] FOREIGN KEY([MaNcc]) REFERENCES [dbo].[NhaCungCap] ([MaNcc]);
    ALTER TABLE [dbo].[PhieuKhoVoHop] CHECK CONSTRAINT [FK_PKVH_NhaCungCap];
END;
IF OBJECT_ID(N'[dbo].[FK_PKVH_PhieuThuChi]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuKhoVoHop]  WITH CHECK ADD  CONSTRAINT [FK_PKVH_PhieuThuChi] FOREIGN KEY([MaPhieuChi]) REFERENCES [dbo].[PhieuThuChi] ([MaPhieu]);
    ALTER TABLE [dbo].[PhieuKhoVoHop] CHECK CONSTRAINT [FK_PKVH_PhieuThuChi];
END;
IF OBJECT_ID(N'[dbo].[FK_PhieuKiemKho_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuKiemKho]  WITH CHECK ADD  CONSTRAINT [FK_PhieuKiemKho_NguoiDung] FOREIGN KEY([MaNdKiem]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[PhieuKiemKho] CHECK CONSTRAINT [FK_PhieuKiemKho_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_PhieuKiemKho_PhieuXuat]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuKiemKho]  WITH CHECK ADD  CONSTRAINT [FK_PhieuKiemKho_PhieuXuat] FOREIGN KEY([MaPxLienKet]) REFERENCES [dbo].[PhieuXuatKho] ([MaPx]);
    ALTER TABLE [dbo].[PhieuKiemKho] CHECK CONSTRAINT [FK_PhieuKiemKho_PhieuXuat];
END;
IF OBJECT_ID(N'[dbo].[FK_PhieuNhapKho_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuNhapKho]  WITH CHECK ADD  CONSTRAINT [FK_PhieuNhapKho_NguoiDung] FOREIGN KEY([MaNdLap]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[PhieuNhapKho] CHECK CONSTRAINT [FK_PhieuNhapKho_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_PhieuNhapKho_NhaCungCap]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuNhapKho]  WITH CHECK ADD  CONSTRAINT [FK_PhieuNhapKho_NhaCungCap] FOREIGN KEY([MaNcc]) REFERENCES [dbo].[NhaCungCap] ([MaNcc]);
    ALTER TABLE [dbo].[PhieuNhapKho] CHECK CONSTRAINT [FK_PhieuNhapKho_NhaCungCap];
END;
IF OBJECT_ID(N'[dbo].[FK_PhieuThuChi_DonHang]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuThuChi]  WITH CHECK ADD  CONSTRAINT [FK_PhieuThuChi_DonHang] FOREIGN KEY([MaDonHangLienKet]) REFERENCES [dbo].[DonHang] ([MaDH]);
    ALTER TABLE [dbo].[PhieuThuChi] CHECK CONSTRAINT [FK_PhieuThuChi_DonHang];
END;
IF OBJECT_ID(N'[dbo].[FK_PhieuThuChi_PhieuNhapKho]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuThuChi]  WITH CHECK ADD  CONSTRAINT [FK_PhieuThuChi_PhieuNhapKho] FOREIGN KEY([MaPhieuNhapLienKet]) REFERENCES [dbo].[PhieuNhapKho] ([MaPn]);
    ALTER TABLE [dbo].[PhieuThuChi] CHECK CONSTRAINT [FK_PhieuThuChi_PhieuNhapKho];
END;
IF OBJECT_ID(N'[dbo].[FK_PhieuXuatKho_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[PhieuXuatKho]  WITH CHECK ADD  CONSTRAINT [FK_PhieuXuatKho_NguoiDung] FOREIGN KEY([MaNdLap]) REFERENCES [dbo].[NguoiDung] ([MaND]);
    ALTER TABLE [dbo].[PhieuXuatKho] CHECK CONSTRAINT [FK_PhieuXuatKho_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_SPBanh_AnhChinh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[SanPhamBanh]  WITH CHECK ADD  CONSTRAINT [FK_SPBanh_AnhChinh] FOREIGN KEY([MaAnhChinh]) REFERENCES [dbo].[KhoHinhAnh] ([MaHA]);
    ALTER TABLE [dbo].[SanPhamBanh] CHECK CONSTRAINT [FK_SPBanh_AnhChinh];
END;
IF OBJECT_ID(N'[dbo].[FK_SPBanh_DanhMuc]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[SanPhamBanh]  WITH CHECK ADD  CONSTRAINT [FK_SPBanh_DanhMuc] FOREIGN KEY([MaDM]) REFERENCES [dbo].[DanhMucBanh] ([MaDM]);
    ALTER TABLE [dbo].[SanPhamBanh] CHECK CONSTRAINT [FK_SPBanh_DanhMuc];
END;
IF OBJECT_ID(N'[dbo].[FK_SPBanh_NhaCungCap]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[SanPhamBanh]  WITH CHECK ADD  CONSTRAINT [FK_SPBanh_NhaCungCap] FOREIGN KEY([MaNccMacDinh]) REFERENCES [dbo].[NhaCungCap] ([MaNcc]);
    ALTER TABLE [dbo].[SanPhamBanh] CHECK CONSTRAINT [FK_SPBanh_NhaCungCap];
END;
IF OBJECT_ID(N'[dbo].[FK_SPBanh_ThuongHieu]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[SanPhamBanh]  WITH CHECK ADD  CONSTRAINT [FK_SPBanh_ThuongHieu] FOREIGN KEY([MaThuongHieu]) REFERENCES [dbo].[ThuongHieu] ([MaThuongHieu]);
    ALTER TABLE [dbo].[SanPhamBanh] CHECK CONSTRAINT [FK_SPBanh_ThuongHieu];
END;
IF OBJECT_ID(N'[dbo].[FK_SoDC_NguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[SoDiaChi]  WITH CHECK ADD  CONSTRAINT [FK_SoDC_NguoiDung] FOREIGN KEY([MaND]) REFERENCES [dbo].[NguoiDung] ([MaND]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[SoDiaChi] CHECK CONSTRAINT [FK_SoDC_NguoiDung];
END;
IF OBJECT_ID(N'[dbo].[FK_Thiep3D_DonHang]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ThiepChucMung3D]  WITH CHECK ADD  CONSTRAINT [FK_Thiep3D_DonHang] FOREIGN KEY([MaDonHang]) REFERENCES [dbo].[DonHang] ([MaDH]);
    ALTER TABLE [dbo].[ThiepChucMung3D] CHECK CONSTRAINT [FK_Thiep3D_DonHang];
END;
IF OBJECT_ID(N'[dbo].[FK_Thiep3D_DotGiaoHang]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ThiepChucMung3D]  WITH CHECK ADD  CONSTRAINT [FK_Thiep3D_DotGiaoHang] FOREIGN KEY([MaDotGiao]) REFERENCES [dbo].[DotGiaoHang_DaDiem] ([MaDotGH]);
    ALTER TABLE [dbo].[ThiepChucMung3D] CHECK CONSTRAINT [FK_Thiep3D_DotGiaoHang];
END;
IF OBJECT_ID(N'[dbo].[FK_TSDD_SPBanh]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[ThongSoDinhDuong]  WITH CHECK ADD  CONSTRAINT [FK_TSDD_SPBanh] FOREIGN KEY([MaBanh]) REFERENCES [dbo].[SanPhamBanh] ([MaBanh]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ThongSoDinhDuong] CHECK CONSTRAINT [FK_TSDD_SPBanh];
END;

-- =============================================================================
-- PHẦN 3: VAI TRÒ, 4 TÀI KHOẢN MẪU (MK: 123456), HỒ SƠ NHÂN VIÊN & SỔ ĐỊA CHỈ
-- =============================================================================
INSERT INTO [dbo].[VaiTro] ([MaVT], [TenVT], [MoTa]) VALUES
('ADMIN',     N'Chủ cửa hàng',       N'Toàn quyền quản trị cửa hàng, kho bãi, nhân sự và sổ quỹ Nguyệt Cát ERP'),
('NHAN_VIEN', N'Nhân viên Bán hàng', N'Nhân viên bán hàng tại quầy POS và xử lý đơn hàng trực tuyến'),
('KHACH_TV',  N'Khách thành viên',   N'Khách hàng thành viên tích điểm đổi quà VIP');

SET IDENTITY_INSERT [dbo].[NguoiDung] ON;
INSERT INTO [dbo].[NguoiDung] ([MaND], [MaVT], [HoTen], [SDT], [Email], [MatKhauHash], [DiemTichLuy], [HangTV], [TrangThai], [NgayTao]) VALUES
(1, 'ADMIN',     N'Lê Minh Đức (Chủ cửa hàng)',          '0814585526', 'admin@nguyetcat.vn',    '1AD9A80E6530A8292F66894205960F658A5869D616983CE098D5154DF0FA6700', 500, N'Kim Cương', 1, '2026-10-01 08:00:00'),
(2, 'NHAN_VIEN', N'Trần Thu Ngân (Nhân viên Bán hàng)',  '0908111222', 'thungan@nguyetcat.vn',  '1AD9A80E6530A8292F66894205960F658A5869D616983CE098D5154DF0FA6700', 500, N'Vàng',      1, '2026-10-05 08:00:00'),
(3, 'NHAN_VIEN', N'Nguyễn Văn Giao (Nhân viên Bán hàng)','0908333444', 'giaohang@nguyetcat.vn', '1AD9A80E6530A8292F66894205960F658A5869D616983CE098D5154DF0FA6700', 500, N'Bạc',       1, '2026-10-10 08:00:00'),
(4, 'KHACH_TV',  N'Nguyễn Minh Châu',                    '0909123456', 'minhchau@gmail.com',    '1AD9A80E6530A8292F66894205960F658A5869D616983CE098D5154DF0FA6700', 802, N'Kim Cương', 1, '2026-10-15 09:30:00');
SET IDENTITY_INSERT [dbo].[NguoiDung] OFF;

SET IDENTITY_INSERT [dbo].[HoSoNhanVien] ON;
INSERT INTO [dbo].[HoSoNhanVien] ([MaNv], [MaNd], [MaCodeNv], [ChucVu], [ChiNhanh], [CaLamViec], [NgayVaoLam], [TrangThaiLamViec], [GhiChu]) VALUES
(1, 1, N'NV001', N'Chủ cửa hàng',       N'TP. Long Xuyên', N'Toàn thời gian',          '2026-10-01', N'DANG_LAM_VIEC', N'Chủ cửa hàng quản trị toàn hệ thống Nguyệt Cát ERP'),
(2, 2, N'NV002', N'Nhân viên Bán hàng', N'TP. Long Xuyên', N'Ca sáng (07:30 - 14:30)', '2026-10-05', N'DANG_LAM_VIEC', N'Phụ trách quầy POS và tiếp nhận đơn hàng doanh nghiệp'),
(3, 3, N'NV003', N'Nhân viên Bán hàng', N'TP. Long Xuyên', N'Ca chiều (14:00 - 21:00)','2026-10-10', N'DANG_LAM_VIEC', N'Phụ trách quầy POS ca chiều và bàn giao đơn vận chuyển');
SET IDENTITY_INSERT [dbo].[HoSoNhanVien] OFF;

SET IDENTITY_INSERT [dbo].[SoDiaChi] ON;
INSERT INTO [dbo].[SoDiaChi] ([MaDC], [MaND], [TenNguoiNhan], [SDTNguoiNhan], [TinhThanh], [QuanHuyen], [PhuongXa], [DiaChiCuThe], [NhanDiaChi], [LaMacDinh]) VALUES
(1, 4, N'Nguyễn Minh Châu',        '0909123456', N'An Giang',        N'TP. Long Xuyên', N'Phường Mỹ Xuyên', N'Số 18 Ung Văn Khiêm',         N'Nhà riêng', 1),
(2, 4, N'Trần Quốc Bảo (Đối tác)', '0918777888', N'TP. Hồ Chí Minh', N'Quận 1',         N'Phường Bến Nghé', N'Tầng 12 Tòa nhà Vietcombank', N'Văn phòng', 0);
SET IDENTITY_INSERT [dbo].[SoDiaChi] OFF;

-- =============================================================================
-- PHẦN 4: THƯƠNG HIỆU (6), NHÀ CUNG CẤP (6), DANH MỤC (4) & KHO HÌNH ẢNH LOCAL
-- =============================================================================
SET IDENTITY_INSERT [dbo].[ThuongHieu] ON;
INSERT INTO [dbo].[ThuongHieu] ([MaThuongHieu], [MaCodeThuongHieu], [TenThuongHieu], [XuatXu], [MoTa], [DangHoatDong]) VALUES
(1, 'KINH_DO',    N'Kinh Đô',    N'TP. Hồ Chí Minh', N'Thương hiệu bánh trung thu quốc dân thuộc Mondelez Kinh Đô Việt Nam', 1),
(2, 'DONG_KHANH', N'Đồng Khánh', N'TP. Hồ Chí Minh', N'Lò bánh trung thu gia truyền Bông Lúa Vàng hơn 70 năm tại Chợ Lớn', 1),
(3, 'NHU_LAN',    N'Như Lan',    N'TP. Hồ Chí Minh', N'Thương hiệu bánh trung thu thủ công trứ danh đường Hàm Nghi, Quận 1', 1),
(4, 'BRODARD',    N'Brodard',    N'TP. Hồ Chí Minh', N'Thương hiệu bánh ngọt phong cách Pháp cao cấp thành lập từ năm 1948', 1),
(5, 'GIVRAL',     N'Givral',     N'TP. Hồ Chí Minh', N'Thương hiệu bánh tươi Sài Gòn không chất bảo quản thành lập từ năm 1950', 1),
(6, 'NGUYET_CAT', N'Nguyệt Cát', N'An Giang',        N'Đại lý phân phối chính hãng & tuyển chọn lễ hộp quà tặng Nguyệt Cát', 1);
SET IDENTITY_INSERT [dbo].[ThuongHieu] OFF;

SET IDENTITY_INSERT [dbo].[NhaCungCap] ON;
INSERT INTO [dbo].[NhaCungCap] ([MaNcc], [MaCodeNcc], [TenNcc], [LoaiNcc], [ThuongHieuCungCap], [PhanTramChietKhauMacDinh], [SoDienThoai], [Email], [DiaChi], [GhiChu], [DangHoatDong], [NgayTao]) VALUES
(1, N'NCC001', N'Nhà phân phối Kinh Đô Chính Hãng',          N'HANG_CHINH_HANG',  N'Kinh Đô',       28.00, N'02838270838', N'phanphoi.kinhdo@mondelez.vn',    N'Số 138 Hai Bà Trưng, Quận 1, TP. Hồ Chí Minh',   N'NPP Cấp 1 chính thức các dòng Kinh Đô Trăng Vàng & Truyền Thống', 1, '2026-10-01 08:00:00'),
(2, N'NCC002', N'Lò Bánh Đồng Khánh Chính Gốc',              N'HANG_CHINH_HANG',  N'Đồng Khánh',    26.00, N'02838552418', N'kinhdoanh@banhdongkhanh.vn',     N'Số 62 Trần Hưng Đạo, Quận 5, TP. Hồ Chí Minh',   N'Lò bánh gia truyền Đồng Khánh Bông Lúa Vàng', 1, '2026-10-01 08:00:00'),
(3, N'NCC003', N'Bánh Trung Thu Như Lan TP.HCM',             N'HANG_CHINH_HANG',  N'Như Lan',       25.00, N'02838292970', N'daily@nhulanbakery.vn',          N'Số 50 Hàm Nghi, Quận 1, TP. Hồ Chí Minh',        N'Nguồn hàng chính gốc Như Lan TP.HCM, chiết khấu theo lô mùa vụ', 1, '2026-10-01 08:00:00'),
(4, N'NCC004', N'Brodard Bakery Saigon',                     N'HANG_CHINH_HANG',  N'Brodard',       24.00, N'02838223966', N'b2c-wholesale@brodardbakery.vn', N'Số 11 Nguyễn Thiệp, Quận 1, TP. Hồ Chí Minh',    N'Dòng bánh Pháp thượng hạng Brodard, vận chuyển bảo quản lạnh', 1, '2026-10-01 08:00:00'),
(5, N'NCC005', N'Givral Bakery Chính Hãng',                  N'HANG_CHINH_HANG',  N'Givral',        25.00, N'02838684377', N'nguonhang@givralbakery.vn',      N'Lô II-1B, KCN Tân Bình, Quận Tân Phú, TP.HCM',   N'Hãng bánh tươi Givral ít ngọt & dòng ăn kiêng Isomalt', 1, '2026-10-01 08:00:00'),
(6, N'NCC006', N'Đại Lý Điều Tiết Bánh Miền Tây (An Giang)', N'DAI_LY_DIEU_TIET', N'Đa thương hiệu',22.00, N'02963841225', N'dieutiet.mientay@gmail.com',     N'Số 215 Trần Hưng Đạo, TP. Long Xuyên, An Giang', N'Đại lý điều tiết hàng bổ sung & cung cấp vỏ hộp quà tặng tại An Giang', 1, '2026-10-01 08:00:00');
SET IDENTITY_INSERT [dbo].[NhaCungCap] OFF;

SET IDENTITY_INSERT [dbo].[DanhMucBanh] ON;
INSERT INTO [dbo].[DanhMucBanh] ([MaDM], [MaDMCha], [TenDM], [DuongDanSlug], [MoTa], [ThuTuSX], [TrangThai]) VALUES
(1, NULL, N'Bánh Nướng Thượng Hạng',          'banh-nuong-thuong-hang',  N'Tuyển phẩm Bào ngư, Vi cá, Yến sào, Tôm hùm cao cấp dành biếu tặng', 1, 1),
(2, NULL, N'Bánh Nướng Truyền Thống',         'banh-nuong-truyen-thong', N'Hương vị thập cẩm lạp xưởng, gà quay, hạt sen, đậu xanh cổ điển', 2, 1),
(3, NULL, N'Bánh Hiện Đại & Lava Trứng Chảy', 'banh-hien-dai-lava',      N'Dòng Kim sa Lava tan chảy, Trà xanh Matcha, Tiramisu, Socola Bỉ', 3, 1),
(4, NULL, N'Bánh Dẻo & Thuần Chay Ít Đường',  'banh-deo-chay-it-duong',  N'Bánh dẻo hoa bưởi, nhân thuần chay và dòng ăn kiêng đường Isomalt', 4, 1);
SET IDENTITY_INSERT [dbo].[DanhMucBanh] OFF;

SET IDENTITY_INSERT [dbo].[KhoHinhAnh] ON;
INSERT INTO [dbo].[KhoHinhAnh] ([MaHA], [TenTepGoc], [TieuDeAlt], [LoaiLuuTru], [DuongDanCucBo], [DuongDanDamMay], [MaFileDrive], [KichThuocPixel], [NgayTaiLen]) VALUES
(1, N'hero-banner-trungthu.jpg', N'Banner Trung Thu Nguyệt Cát 2026',                   1, N'/images/landing/hero-banner-trungthu.jpg', NULL, NULL, '1920x1080', '2026-10-01 08:00:00'),
(2, N'khay-4-o-rong.png',        N'Khay hộp quà 4 bánh rỗng Studio phối hộp',           1, N'/images/vohop/khay-4-o-rong.png',          NULL, NULL, '800x800',   '2026-10-01 08:00:00'),
(3, N'khay-6-o-rong.png',        N'Khay hộp quà sơn mài 6 bánh Hoàng Gia',              1, N'/images/vohop/khay-6-o-rong.png',          NULL, NULL, '800x800',   '2026-10-01 08:00:00'),
(4, N'slot-bao-ngu.png',         N'Bánh nướng nhân thượng hạng & thập cẩm truyền thống', 1, N'/images/banh/slot-bao-ngu.png',           NULL, NULL, '800x800',   '2026-10-01 08:00:00'),
(5, N'banh-hat-sen-hue.jpg',     N'Bánh nướng nhân ngọt hạt sen, đậu xanh, khoai môn',  1, N'/images/banh/banh-hat-sen-hue.jpg',        NULL, NULL, '800x800',   '2026-10-01 08:00:00'),
(6, N'fallback-lava.png',        N'Bánh nướng hiện đại Kim Sa Lava, Matcha, Tiramisu',  1, N'/images/banh/fallback-lava.png',           NULL, NULL, '800x800',   '2026-10-01 08:00:00'),
(7, N'slot-sua-dua-chay.png',    N'Bánh dẻo, thuần chay & đường ăn kiêng Isomalt',      1, N'/images/banh/slot-sua-dua-chay.png',       NULL, NULL, '800x800',   '2026-10-01 08:00:00'),
(8, N'banh-mac-dinh.png',        N'Ảnh dự phòng sản phẩm bánh trung thu',               1, N'/images/default/banh-mac-dinh.png',        NULL, NULL, '800x800',   '2026-10-01 08:00:00'),
(9, N'vohop-mac-dinh.png',       N'Ảnh dự phòng vỏ hộp quà tặng',                       1, N'/images/default/vohop-mac-dinh.png',       NULL, NULL, '800x800',   '2026-10-01 08:00:00');
SET IDENTITY_INSERT [dbo].[KhoHinhAnh] OFF;

-- =============================================================================
-- PHẦN 5: 60 SẢN PHẨM BÁNH (CÓ MaAnhChinh FK) & 60 THÔNG SỐ DINH DƯỠNG
-- =============================================================================
SET IDENTITY_INSERT [dbo].[SanPhamBanh] ON;
INSERT INTO [dbo].[SanPhamBanh] ([MaBanh], [MaDM], [MaSKU], [TenBanh], [DonGiaLe], [GiaVon], [TrongLuongGram], [SoTrungMuoi], [MucDoNgot], [LaBanhChay], [LaBanhThuongHang], [NhomHuongVi], [TheDoiTuongPhuHop], [MoTaNgan], [CauChuyenBanh], [TongTonKho], [TrangThaiBan], [MaThuongHieu], [MaNccMacDinh], [MaVach], [ViTriKho], [DonViTinh], [BanTrucTiep], [MaAnhChinh]) VALUES
(1, 1, 'KD-01', N'Kinh Đô Trăng Vàng Bào Ngư Thượng Hạng (2 Trứng)', 235000.00, 173045.00, 210, 2, 'BINH_THUONG', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Bào ngư nguyên chất kết hợp Jambon ngũ hạt và 2 trứng muối vàng ươm.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bào ngư nguyên chất kết hợp Jambon ngũ hạt và 2 trứng muối vàng ươm. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 42, 1, 1, 1, '8934680000019', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(2, 1, 'KD-02', N'Kinh Đô Gà Quay Sốt X.O Jambon (2 Trứng)', 168000.00, 120960.00, 210, 2, 'BINH_THUONG', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,GIA_DINH', N'Thịt gà quay đậm đà hòa quyện sốt X.O thượng hạng cùng jambon sợi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Thịt gà quay đậm đà hòa quyện sốt X.O thượng hạng cùng jambon sợi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 37, 1, 1, 1, '8934680000029', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(3, 1, 'KD-03', N'Kinh Đô Cua Bát Bửu Hạt Sen (2 Trứng)', 198000.00, 142560.00, 210, 2, 'BINH_THUONG', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Thịt cua biển tươi ngọt phối tám loại hạt quý bát bửu đặc trưng mùa Trăng.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Thịt cua biển tươi ngọt phối tám loại hạt quý bát bửu đặc trưng mùa Trăng. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 1, 1, '8934680000039', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(4, 2, 'KD-04', N'Kinh Đô Thập Cẩm Lạp Xưởng Ngũ Hạt (2 Trứng)', 125000.00, 90000.00, 180, 2, 'BINH_THUONG', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH,NGUOI_LON_TUOI', N'Công thức thập cẩm truyền thống Kinh Đô với lạp xưởng Mai Quế Lộ thơm nồng.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Công thức thập cẩm truyền thống Kinh Đô với lạp xưởng Mai Quế Lộ thơm nồng. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 56, 1, 1, 1, '8934680000049', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(5, 2, 'KD-05', N'Kinh Đô Thập Cẩm Jambon Bát Bửu (1 Trứng)', 98000.00, 70560.00, 150, 1, 'BINH_THUONG', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH', N'Nhân thập cẩm jambon cân bằng vị mặn béo vừa phải cho mọi thành viên.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Nhân thập cẩm jambon cân bằng vị mặn béo vừa phải cho mọi thành viên. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 53, 1, 1, 1, '8934680000059', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(6, 2, 'KD-06', N'Kinh Đô Đậu Xanh Hạt Dưa (1 Trứng)', 72000.00, 51840.00, 150, 1, 'BINH_THUONG', 0, 0, 'NGOT_THANH', 'GIA_DINH,TRE_EM', N'Đậu xanh đãi vỏ sên mịn dẻo thơm hòa hạt dưa rang giòn bùi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Đậu xanh đãi vỏ sên mịn dẻo thơm hòa hạt dưa rang giòn bùi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 58, 1, 1, 1, '8934680000069', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(7, 2, 'KD-07', N'Kinh Đô Hạt Sen Huế Trứng Muối (2 Trứng)', 115000.00, 82800.00, 180, 2, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH,NGUOI_LON_TUOI', N'Hạt sen tuyển chọn sên giảm đường thanh tao kèm 2 lòng đỏ trứng muối.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Hạt sen tuyển chọn sên giảm đường thanh tao kèm 2 lòng đỏ trứng muối. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 42, 1, 1, 1, '8934680000079', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(8, 2, 'KD-08', N'Kinh Đô Khoai Môn Hạt Sen (1 Trứng)', 76000.00, 54720.00, 150, 1, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH,GIOI_TRE', N'Nhân khoai môn tím bùi béo tự nhiên kết hợp trứng muối tan dịu.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Nhân khoai môn tím bùi béo tự nhiên kết hợp trứng muối tan dịu. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 45, 1, 1, 1, '8934680000089', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(9, 3, 'KD-09', N'Kinh Đô Lava Trứng Chảy Phô Mai Hoàng Kim', 135000.00, 98936.00, 120, 1, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Vỏ nướng mềm mịn bao bọc lõi nhân trứng muối phô mai tan chảy sánh mịn.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Vỏ nướng mềm mịn bao bọc lõi nhân trứng muối phô mai tan chảy sánh mịn. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 58, 1, 1, 1, '8934680000099', N'Kệ B1 - Hiện đại & Lava', N'Cái', 1, 6),
(10, 3, 'KD-10', N'Kinh Đô Tuyết Trà Xanh Matcha Hạnh Nhân', 118000.00, 84960.00, 150, 0, 'IT_NGOT', 0, 0, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Vị trà xanh Matcha Nhật Bản chát dịu cân bằng cùng hạnh nhân lát bùi giòn.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Vị trà xanh Matcha Nhật Bản chát dịu cân bằng cùng hạnh nhân lát bùi giòn. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 43, 1, 1, 1, '8934680000109', N'Kệ B1 - Hiện đại & Lava', N'Cái', 1, 6),
(11, 4, 'KD-11', N'Kinh Đô Bánh Dẻo Hạt Sen Hạt Dưa (0 Trứng)', 68000.00, 48960.00, 180, 0, 'IT_NGOT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,GIA_DINH', N'Vỏ nếp rang thơm hương hoa bưởi tinh khiết, nhân hạt sen thuần chay thanh mát.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Vỏ nếp rang thơm hương hoa bưởi tinh khiết, nhân hạt sen thuần chay thanh mát. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 50, 1, 1, 1, '8934680000119', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(12, 4, 'KD-12', N'Kinh Đô Xanh Hạt Sen Hạnh Nhân Đường Isomalt', 108000.00, 77760.00, 150, 0, 'DUONG_ISOMALT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,TIEU_DUONG', N'Sử dụng 100% đường ăn kiêng Isomalt chiết xuất củ cải đường, phù hợp người tiểu đường.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Sử dụng 100% đường ăn kiêng Isomalt chiết xuất củ cải đường, phù hợp người tiểu đường. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 38, 1, 1, 1, '8934680000129', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(13, 1, 'DK-01', N'Đồng Khánh Bông Lúa Vàng Bào Ngư Hải Sâm (2 Trứng)', 192000.00, 144384.00, 210, 2, 'BINH_THUONG', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Tinh hoa lò bánh Đồng Khánh hơn 70 năm với bào ngư, hải sâm và 2 trứng muối.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Tinh hoa lò bánh Đồng Khánh hơn 70 năm với bào ngư, hải sâm và 2 trứng muối. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 47, 1, 2, 2, '8935012000139', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(14, 1, 'DK-02', N'Đồng Khánh Gà Quay Vi Cá Thượng Hạng (2 Trứng)', 165000.00, 122100.00, 210, 2, 'BINH_THUONG', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,GIA_DINH', N'Gà quay mật ong xé sợi quyện vi cá thượng hạng và hạt điều giòn rụm.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Gà quay mật ong xé sợi quyện vi cá thượng hạng và hạt điều giòn rụm. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 43, 1, 2, 2, '8935012000149', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(15, 1, 'DK-03', N'Đồng Khánh Yến Sào Hạt Sen Thượng Hạng (2 Trứng)', 188000.00, 139120.00, 200, 2, 'IT_NGOT', 0, 1, 'NGOT_THANH', 'BIEU_SEP,NGUOI_LON_TUOI', N'Yến sào chưng thanh mát kết hợp nhân hạt sen bùi dịu, quà biếu sức khỏe đẳng cấp.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Yến sào chưng thanh mát kết hợp nhân hạt sen bùi dịu, quà biếu sức khỏe đẳng cấp. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 34, 1, 2, 2, '8935012000159', N'Kệ A1 - Thượng hạng', N'Cái', 1, 5),
(16, 2, 'DK-04', N'Đồng Khánh Thập Cẩm Gà Quay Jambon (2 Trứng)', 118000.00, 87320.00, 180, 2, 'BINH_THUONG', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH', N'Hương vị thập cẩm cổ truyền Chợ Lớn đậm đà mứt bí, lá chanh, jambon và gà quay.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Hương vị thập cẩm cổ truyền Chợ Lớn đậm đà mứt bí, lá chanh, jambon và gà quay. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 54, 1, 2, 2, '8935012000169', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(17, 2, 'DK-05', N'Đồng Khánh Thập Cẩm Lạp Xưởng Truyền Thống (1 Trứng)', 86000.00, 63640.00, 150, 1, 'BINH_THUONG', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH', N'Bánh nướng vàng óng, nhân lạp xưởng thơm rượu Mai Quế Lộ chuẩn vị xưa.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh nướng vàng óng, nhân lạp xưởng thơm rượu Mai Quế Lộ chuẩn vị xưa. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 54, 1, 2, 2, '8935012000179', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(18, 2, 'DK-06', N'Đồng Khánh Đậu Xanh Sầu Riêng (2 Trứng)', 105000.00, 77700.00, 180, 2, 'BINH_THUONG', 0, 0, 'NGOT_THANH', 'GIA_DINH,GIOI_TRE', N'Cơm sầu riêng Ri6 thơm lừng quyện đậu xanh bùi mịn và 2 trứng muối béo ngậy.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Cơm sầu riêng Ri6 thơm lừng quyện đậu xanh bùi mịn và 2 trứng muối béo ngậy. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 47, 1, 2, 2, '8935012000189', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(19, 2, 'DK-07', N'Đồng Khánh Khoai Môn Hạt Dưa (1 Trứng)', 69000.00, 51060.00, 150, 1, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH,TRE_EM', N'Khoai môn sáp sên mịn dẻo dịu ngọt kết hợp hạt dưa bùi bùi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Khoai môn sáp sên mịn dẻo dịu ngọt kết hợp hạt dưa bùi bùi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 45, 1, 2, 2, '8935012000199', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(20, 2, 'DK-08', N'Đồng Khánh Đậu Đỏ Hạt Sen (1 Trứng)', 68000.00, 50320.00, 150, 1, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH', N'Đậu đỏ thanh nhiệt phối hạt sen nguyên chất, vị ngọt thanh không gắt.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Đậu đỏ thanh nhiệt phối hạt sen nguyên chất, vị ngọt thanh không gắt. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 45, 1, 2, 2, '8935012000209', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(21, 3, 'DK-09', N'Đồng Khánh Kim Sa Lava Trứng Muối Tan Chảy', 122000.00, 90280.00, 150, 1, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Lớp nhân kim sa trứng muối tan chảy óng ánh khi cắt đôi, thơm béo sữa tươi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Lớp nhân kim sa trứng muối tan chảy óng ánh khi cắt đôi, thơm béo sữa tươi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 44, 1, 2, 2, '8935012000219', N'Kệ B1 - Hiện đại & Lava', N'Cái', 1, 6),
(22, 3, 'DK-10', N'Đồng Khánh Trà Xanh Hạt Sen Trứng Muối (1 Trứng)', 89000.00, 65860.00, 150, 1, 'IT_NGOT', 0, 0, 'HIEN_DAI_LAVA', 'GIOI_TRE,GIA_DINH', N'Hương trà xanh thanh khiết quyện hạt sen bùi và 1 trứng muối tròn vị.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Hương trà xanh thanh khiết quyện hạt sen bùi và 1 trứng muối tròn vị. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 40, 1, 2, 2, '8935012000229', N'Kệ B1 - Hiện đại & Lava', N'Cái', 1, 6),
(23, 4, 'DK-11', N'Đồng Khánh Bánh Dẻo Sữa Dừa Hạt Dưa (Thuần Chay)', 62000.00, 45880.00, 180, 0, 'IT_NGOT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,GIA_DINH', N'Nhân sữa dừa Bến Tre béo thanh kết hợp vỏ bánh dẻo mềm thơm hoa bưởi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Nhân sữa dừa Bến Tre béo thanh kết hợp vỏ bánh dẻo mềm thơm hoa bưởi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 45, 1, 2, 2, '8935012000239', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(24, 4, 'DK-12', N'Đồng Khánh Chay Hạt Sen Đường Ăn Kiêng Isomalt', 92000.00, 68080.00, 150, 0, 'DUONG_ISOMALT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,TIEU_DUONG', N'Bánh nướng chay nhân hạt sen nguyên chất dùng đường Isomalt chỉ số đường huyết thấp.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh nướng chay nhân hạt sen nguyên chất dùng đường Isomalt chỉ số đường huyết thấp. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 38, 1, 2, 2, '8935012000249', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(25, 1, 'NL-01', N'Như Lan Vi Cá Thập Cẩm Gà Quay (2 Trứng - 250g)', 245000.00, 185955.00, 250, 2, 'BINH_THUONG', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Dòng bánh trứ danh số 1 của Như Lan Sài Gòn, trọng lượng lớn 250g đầy đặn vi cá và gà quay.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Dòng bánh trứ danh số 1 của Như Lan Sài Gòn, trọng lượng lớn 250g đầy đặn vi cá và gà quay. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 44, 1, 3, 3, '8936025000259', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(26, 1, 'NL-02', N'Như Lan Yến Sào Vi Cá Thượng Hạng (2 Trứng - 250g)', 278000.00, 208500.00, 250, 2, 'IT_NGOT', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Kết hợp yến sào tinh chế và vi cá hảo hạng, lựa chọn hàng đầu biếu tặng đối tác.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Kết hợp yến sào tinh chế và vi cá hảo hạng, lựa chọn hàng đầu biếu tặng đối tác. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 30, 1, 3, 3, '8936025000269', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(27, 1, 'NL-03', N'Như Lan Bào Ngư Hải Sản Sốt Tiêu Đen (2 Trứng)', 225000.00, 168750.00, 200, 2, 'BINH_THUONG', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Bào ngư, sò điệp khô xào sốt tiêu đen Phú Quốc thơm cay nhẹ đầy lôi cuốn.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bào ngư, sò điệp khô xào sốt tiêu đen Phú Quốc thơm cay nhẹ đầy lôi cuốn. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 3, 3, '8936025000279', N'Kệ A1 - Thượng hạng', N'Cái', 1, 4),
(28, 2, 'NL-04', N'Như Lan Thập Cẩm Gà Quay Jambon (2 Trứng - 200g)', 142000.00, 106500.00, 200, 2, 'BINH_THUONG', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH,NGUOI_LON_TUOI', N'Bánh nướng thủ công vỏ mỏng mềm, nhân gà quay jambon đậm đà chuẩn vị Hàm Nghi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh nướng thủ công vỏ mỏng mềm, nhân gà quay jambon đậm đà chuẩn vị Hàm Nghi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 46, 1, 3, 3, '8936025000289', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(29, 2, 'NL-05', N'Như Lan Thập Cẩm Lạp Xưởng Truyền Thống (2 Trứng)', 128000.00, 96000.00, 200, 2, 'BINH_THUONG', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH', N'Lạp xưởng tươi Như Lan tự chế biến kết hợp hạt điều, mứt bí, mỡ đường giòn thơm.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Lạp xưởng tươi Như Lan tự chế biến kết hợp hạt điều, mứt bí, mỡ đường giòn thơm. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 45, 1, 3, 3, '8936025000299', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(30, 2, 'NL-06', N'Như Lan Đậu Xanh Sen Sầu Riêng (2 Trứng)', 118000.00, 88500.00, 200, 2, 'BINH_THUONG', 0, 0, 'NGOT_THANH', 'GIA_DINH,GIOI_TRE', N'Đậu xanh hòa hạt sen sên cùng sầu riêng tươi thơm ngào ngạt.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Đậu xanh hòa hạt sen sên cùng sầu riêng tươi thơm ngào ngạt. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 45, 1, 3, 3, '8936025000309', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(31, 2, 'NL-07', N'Như Lan Sen Hạnh Nhân Trứng Muối (2 Trứng)', 125000.00, 93750.00, 200, 2, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH,NGUOI_LON_TUOI', N'Hạt sen bùi mịn kết hợp hạnh nhân Mỹ nướng giòn và 2 trứng muối dẻo thơm.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Hạt sen bùi mịn kết hợp hạnh nhân Mỹ nướng giòn và 2 trứng muối dẻo thơm. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 3, 3, '8936025000319', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(32, 2, 'NL-08', N'Như Lan Môn Sen Trứng Muối (1 Trứng)', 92000.00, 69000.00, 150, 1, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH', N'Nhân khoai môn quyện hạt sen bùi thơm, ngọt nhẹ dễ ăn.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Nhân khoai môn quyện hạt sen bùi thơm, ngọt nhẹ dễ ăn. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 45, 1, 3, 3, '8936025000329', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(33, 3, 'NL-09', N'Như Lan Tiramisu Cà Phê Phô Mai (1 Trứng)', 132000.00, 99000.00, 150, 1, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Hương cà phê Espresso hòa phô mai Mascarpone béo nhẹ phong cách Ý hiện đại.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Hương cà phê Espresso hòa phô mai Mascarpone béo nhẹ phong cách Ý hiện đại. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 39, 1, 3, 3, '8936025000339', N'Kệ B1 - Hiện đại & Lava', N'Cái', 1, 6),
(34, 3, 'NL-10', N'Như Lan Matcha Trà Xanh Kim Sa Trứng Chảy', 138000.00, 103500.00, 150, 1, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Vỏ trà xanh thơm mát kết hợp nhân kim sa trứng muối tan chảy béo ngậy.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Vỏ trà xanh thơm mát kết hợp nhân kim sa trứng muối tan chảy béo ngậy. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 41, 1, 3, 3, '8936025000349', N'Kệ B1 - Hiện đại & Lava', N'Cái', 1, 6),
(35, 4, 'NL-11', N'Như Lan Bánh Dẻo Hạt Sen Trứng Muối (1 Trứng)', 88000.00, 66000.00, 200, 1, 'BINH_THUONG', 0, 0, 'CHAY_THUAN_KHIET', 'GIA_DINH', N'Bánh dẻo trắng ngần thơm hương hoa bưởi, nhân hạt sen mịn kèm 1 trứng muối.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh dẻo trắng ngần thơm hương hoa bưởi, nhân hạt sen mịn kèm 1 trứng muối. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 40, 1, 3, 3, '8936025000359', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(36, 4, 'NL-12', N'Như Lan Đậu Xanh Hạt Sen Đường Ăn Kiêng (Chay)', 105000.00, 78750.00, 150, 0, 'DUONG_ISOMALT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,TIEU_DUONG', N'Dòng bánh dinh dưỡng Như Lan dành riêng cho người ăn chay và kiêng đường tinh luyện.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Dòng bánh dinh dưỡng Như Lan dành riêng cho người ăn chay và kiêng đường tinh luyện. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 40, 1, 3, 3, '8936025000369', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(37, 1, 'BR-01', N'Brodard Thập Cẩm Bào Ngư Sò Điệp Sốt X.O (2 Trứng)', 285000.00, 218500.00, 200, 2, 'IT_NGOT', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Bánh nướng phong cách Pháp Brodard nhân bào ngư, sò điệp Nhật và sốt X.O thượng hạng.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh nướng phong cách Pháp Brodard nhân bào ngư, sò điệp Nhật và sốt X.O thượng hạng. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 44, 1, 4, 4, '8936088000379', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 4),
(38, 1, 'BR-02', N'Brodard Yến Sào Gà Quay Jambon (2 Trứng)', 265000.00, 201400.00, 200, 2, 'IT_NGOT', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Sự kết hợp tinh tế giữa yến sào cao cấp và thịt gà quay mật ong chuẩn vị Sài Gòn.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Sự kết hợp tinh tế giữa yến sào cao cấp và thịt gà quay mật ong chuẩn vị Sài Gòn. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 30, 1, 4, 4, '8936088000389', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 4),
(39, 1, 'BR-03', N'Brodard Hải Sản Tôm Hùm Sốt Hồng Kông (2 Trứng)', 275000.00, 209000.00, 200, 2, 'IT_NGOT', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Thịt tôm hùm ngọt chắc tẩm ướp sốt Hồng Kông đậm vị kết hợp 2 trứng muối.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Thịt tôm hùm ngọt chắc tẩm ướp sốt Hồng Kông đậm vị kết hợp 2 trứng muối. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 25, 1, 4, 4, '8936088000399', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 4),
(40, 2, 'BR-04', N'Brodard Thập Cẩm Gà Quay Lá Chanh (2 Trứng)', 168000.00, 127680.00, 200, 2, 'IT_NGOT', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH,BIEU_SEP', N'Thập cẩm gà quay thơm thanh lá chanh non, ít dầu mỡ và không dùng chất bảo quản.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Thập cẩm gà quay thơm thanh lá chanh non, ít dầu mỡ và không dùng chất bảo quản. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 39, 1, 4, 4, '8936088000409', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(41, 2, 'BR-05', N'Brodard Hạt Sen Trà Xanh Trứng Muối (2 Trứng)', 148000.00, 112480.00, 200, 2, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH,NGUOI_LON_TUOI', N'Nhân hạt sen Huế xay nhuyễn hòa bột trà xanh thanh tao, hậu vị dịu nhẹ.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Nhân hạt sen Huế xay nhuyễn hòa bột trà xanh thanh tao, hậu vị dịu nhẹ. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 40, 1, 4, 4, '8936088000419', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(42, 2, 'BR-06', N'Brodard Sầu Riêng 6 Ri Nguyên Chất (2 Trứng)', 158000.00, 120080.00, 200, 2, 'BINH_THUONG', 0, 0, 'NGOT_THANH', 'GIA_DINH,GIOI_TRE', N'Sầu riêng 6 Ri cơm vàng hạt lép thơm béo đặc trưng kết hợp 2 lòng đỏ trứng muối.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Sầu riêng 6 Ri cơm vàng hạt lép thơm béo đặc trưng kết hợp 2 lòng đỏ trứng muối. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 4, 4, '8936088000429', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 5),
(43, 2, 'BR-07', N'Brodard Mè Đen Hạnh Nhân Hạt Sen (2 Trứng)', 145000.00, 110200.00, 200, 2, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'NGUOI_LON_TUOI,GIA_DINH', N'Mè đen rang thơm giàu dưỡng chất phối hạnh nhân giòn bùi, tốt cho tim mạch.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Mè đen rang thơm giàu dưỡng chất phối hạnh nhân giòn bùi, tốt cho tim mạch. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 4, 4, '8936088000439', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(44, 2, 'BR-08', N'Brodard Khoai Môn Hạt Sen Ít Ngọt (1 Trứng)', 128000.00, 97280.00, 150, 1, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH', N'Nhân khoai môn tím mịn mượt phối hạt sen giảm 30% độ ngọt.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Nhân khoai môn tím mịn mượt phối hạt sen giảm 30% độ ngọt. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 4, 4, '8936088000449', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(45, 3, 'BR-09', N'Brodard Tiramisu Cà Phê Phô Mai Pháp', 162000.00, 123120.00, 150, 0, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Bánh nướng Tiramisu nổi tiếng nhất của Brodard từ công thức bánh ngọt Pháp 1948.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh nướng Tiramisu nổi tiếng nhất của Brodard từ công thức bánh ngọt Pháp 1948. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 42, 1, 4, 4, '8936088000459', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 6),
(46, 3, 'BR-10', N'Brodard Socola Bỉ Hạt Phỉ Lava Tan Chảy', 168000.00, 127680.00, 150, 0, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Socola đen Bỉ nguyên chất 70% đắng nhẹ quyện hạt phỉ rang và lõi lava mềm mịn.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Socola đen Bỉ nguyên chất 70% đắng nhẹ quyện hạt phỉ rang và lõi lava mềm mịn. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 39, 1, 4, 4, '8936088000469', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 6),
(47, 4, 'BR-11', N'Brodard Bánh Dẻo Hạt Sen Hạt Chia (Thuần Chay)', 118000.00, 89680.00, 180, 0, 'IT_NGOT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,GIA_DINH', N'Vỏ bánh dẻo thanh nhẹ kết hợp nhân hạt sen và hạt chia hữu cơ giàu Omega-3.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Vỏ bánh dẻo thanh nhẹ kết hợp nhân hạt sen và hạt chia hữu cơ giàu Omega-3. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 4, 4, '8936088000479', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(48, 4, 'BR-12', N'Brodard Hạt Sen Hạt Chia Đường Isomalt (Chay)', 135000.00, 102600.00, 150, 0, 'DUONG_ISOMALT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,TIEU_DUONG', N'Bánh nướng chay không đường tinh luyện, dùng đường Isomalt an toàn cho người kiêng ngọt.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh nướng chay không đường tinh luyện, dùng đường Isomalt an toàn cho người kiêng ngọt. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 33, 1, 4, 4, '8936088000489', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(49, 1, 'GV-01', N'Givral Thập Cẩm Gà Quay Vi Cá Yến Sào (2 Trứng)', 278000.00, 208500.00, 200, 2, 'IT_NGOT', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Dòng bánh tươi Givral danh tiếng hội tụ đủ 3 cực phẩm: Yến sào, Vi cá và Gà quay.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Dòng bánh tươi Givral danh tiếng hội tụ đủ 3 cực phẩm: Yến sào, Vi cá và Gà quay. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 28, 1, 5, 5, '8936099000499', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 4),
(50, 1, 'GV-02', N'Givral Bào Ngư Sốt X.O Thượng Hạng (2 Trứng)', 262000.00, 196500.00, 200, 2, 'IT_NGOT', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,DOI_TAC', N'Bào ngư tươi ngon quyện sốt X.O thượng hạng trong lớp vỏ nướng mỏng mềm đặc trưng Givral.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bào ngư tươi ngon quyện sốt X.O thượng hạng trong lớp vỏ nướng mỏng mềm đặc trưng Givral. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 28, 1, 5, 5, '8936099000509', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 4),
(51, 1, 'GV-03', N'Givral Thập Cẩm Gà Quay Vi Cá (2 Trứng)', 215000.00, 161250.00, 200, 2, 'IT_NGOT', 0, 1, 'MAN_TRUYENTHONG', 'BIEU_SEP,GIA_DINH', N'Hương vị bánh tươi Sài Gòn hơn 70 năm với gà quay xé sợi và vi cá hảo hạng.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Hương vị bánh tươi Sài Gòn hơn 70 năm với gà quay xé sợi và vi cá hảo hạng. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 5, 5, '8936099000519', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 4),
(52, 2, 'GV-04', N'Givral Thập Cẩm Ngũ Nhân Truyền Thống (2 Trứng)', 152000.00, 114000.00, 200, 2, 'IT_NGOT', 0, 0, 'MAN_TRUYENTHONG', 'GIA_DINH,NGUOI_LON_TUOI', N'Ngũ nhân hạt điều, hạnh nhân, óc chó, hạt dưa, mè trắng hòa jambon thơm bùi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Ngũ nhân hạt điều, hạnh nhân, óc chó, hạt dưa, mè trắng hòa jambon thơm bùi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 38, 1, 5, 5, '8936099000529', N'Kệ A2 - Truyền thống', N'Cái', 1, 4),
(53, 2, 'GV-05', N'Givral Đậu Đen Mè Đen Hạt Sen (2 Trứng)', 138000.00, 103500.00, 200, 2, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'NGUOI_LON_TUOI,GIA_DINH', N'Đậu đen xanh lòng và mè đen rang thơm bùi, bổ dưỡng và thanh ngọt.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Đậu đen xanh lòng và mè đen rang thơm bùi, bổ dưỡng và thanh ngọt. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 5, 5, '8936099000539', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(54, 2, 'GV-06', N'Givral Hạt Sen Bắc Trứng Muối (2 Trứng)', 142000.00, 106500.00, 200, 2, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH,NGUOI_LON_TUOI', N'Hạt sen bắc bùi dẻo sên thủ công không chất bảo quản kèm 2 trứng muối vàng ươm.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Hạt sen bắc bùi dẻo sên thủ công không chất bảo quản kèm 2 trứng muối vàng ươm. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 38, 1, 5, 5, '8936099000549', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(55, 2, 'GV-07', N'Givral Đậu Xanh Sầu Riêng Tươi (2 Trứng)', 148000.00, 111000.00, 200, 2, 'BINH_THUONG', 0, 0, 'NGOT_THANH', 'GIA_DINH,GIOI_TRE', N'Cơm sầu riêng tươi 100% hòa quyện đậu xanh mịn màng thơm nồng.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Cơm sầu riêng tươi 100% hòa quyện đậu xanh mịn màng thơm nồng. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 5, 5, '8936099000559', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 5),
(56, 2, 'GV-08', N'Givral Khoai Môn Hạt Sen (1 Trứng)', 118000.00, 88500.00, 150, 1, 'IT_NGOT', 0, 0, 'NGOT_THANH', 'GIA_DINH', N'Khoai môn dẻo bùi ít ngọt, vỏ bánh mỏng tan ngay đầu lưỡi.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Khoai môn dẻo bùi ít ngọt, vỏ bánh mỏng tan ngay đầu lưỡi. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 40, 1, 5, 5, '8936099000569', N'Kệ A2 - Truyền thống', N'Cái', 1, 5),
(57, 3, 'GV-09', N'Givral Việt Quất Nam Việt Quất (Berry Hiện Đại)', 145000.00, 108750.00, 150, 0, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Vị chua ngọt tươi mới từ quả Việt quất và Nam việt quất nhập khẩu giàu chất chống oxy hóa.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Vị chua ngọt tươi mới từ quả Việt quất và Nam việt quất nhập khẩu giàu chất chống oxy hóa. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 36, 1, 5, 5, '8936099000579', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 6),
(58, 3, 'GV-10', N'Givral Phô Mai Trứng Chảy Kim Sa', 156000.00, 117000.00, 150, 1, 'IT_NGOT', 0, 1, 'HIEN_DAI_LAVA', 'GIOI_TRE,TREND', N'Lõi nhân phô mai trứng muối tan chảy quyến rũ, độ béo hài hòa không ngấy.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Lõi nhân phô mai trứng muối tan chảy quyến rũ, độ béo hài hòa không ngấy. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 42, 1, 5, 5, '8936099000589', N'Tủ Mát C1 - Dòng tươi', N'Cái', 1, 6),
(59, 4, 'GV-11', N'Givral Bánh Dẻo Hạt Sen Chay (0 Trứng)', 112000.00, 84000.00, 180, 0, 'IT_NGOT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,GIA_DINH', N'Bánh dẻo tươi Givral nhân hạt sen mềm dẻo, hoàn toàn thuần chay thanh khiết.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Bánh dẻo tươi Givral nhân hạt sen mềm dẻo, hoàn toàn thuần chay thanh khiết. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 35, 1, 5, 5, '8936099000599', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7),
(60, 4, 'GV-12', N'Givral Đậu Xanh Hạt Sen Đường Isomalt (Ăn Kiêng)', 128000.00, 96000.00, 150, 0, 'DUONG_ISOMALT', 1, 0, 'CHAY_THUAN_KHIET', 'AN_CHAY,TIEU_DUONG', N'Công thức bánh nướng ăn kiêng chuyên biệt của Givral sử dụng 100% đường Isomalt.', N'Sản phẩm chính hãng thuộc bộ sưu tập Trung Thu 2026. Công thức bánh nướng ăn kiêng chuyên biệt của Givral sử dụng 100% đường Isomalt. Bảo quản nơi khô ráo thoáng mát, ngon hơn khi thưởng thức cùng trà Ô Long nóng.', 38, 1, 5, 5, '8936099000609', N'Kệ B2 - Bánh dẻo & Chay', N'Cái', 1, 7);
SET IDENTITY_INSERT [dbo].[SanPhamBanh] OFF;

SET IDENTITY_INSERT [dbo].[ThongSoDinhDuong] ON;
INSERT INTO [dbo].[ThongSoDinhDuong] ([MaDD], [MaBanh], [NangLuongCalo], [LuongDuongGram], [ChatDamGram], [ChatBeoGram], [ChiSoDuongHuyet_GI], [CanhBaoDiUng]) VALUES
(1, 1, 760, 28.50, 22.40, 34.00, 56, N'Hải sản (bào ngư), trứng muối, mè, đậu phộng, gluten'),
(2, 2, 745, 29.00, 21.80, 33.20, 57, N'Trứng muối, đậu phộng, mè, hải sản sốt X.O, gluten'),
(3, 3, 730, 27.20, 23.00, 31.50, 55, N'Giáp xác (cua), trứng muối, hạt điều, mè, gluten'),
(4, 4, 690, 31.00, 16.50, 29.80, 60, N'Trứng muối, đậu phộng, mè, hạt dưa, gluten'),
(5, 5, 580, 26.50, 14.20, 24.50, 58, N'Trứng muối, đậu phộng, mè, gluten'),
(6, 6, 540, 38.00, 10.50, 19.20, 62, N'Trứng muối, đậu nành, gluten'),
(7, 7, 620, 32.00, 13.40, 23.80, 54, N'Trứng muối, gluten'),
(8, 8, 535, 34.50, 9.80, 18.60, 56, N'Trứng muối, sữa, gluten'),
(9, 9, 490, 26.00, 11.20, 24.80, 52, N'Trứng, sữa, phô mai, gluten'),
(10, 10, 510, 28.00, 9.60, 18.00, 50, N'Hạnh nhân, sữa, gluten'),
(11, 11, 520, 39.00, 7.20, 11.50, 58, N'Gluten, hạt dưa (Thuần chay)'),
(12, 12, 445, 9.50, 10.80, 16.40, 34, N'Hạnh nhân, gluten (Thuần chay - Ít GI)'),
(13, 13, 750, 27.80, 22.00, 33.50, 56, N'Hải sản, trứng muối, đậu phộng, mè, gluten'),
(14, 14, 735, 28.20, 21.50, 32.00, 56, N'Hải sản, trứng muối, hạt điều, mè, gluten'),
(15, 15, 640, 26.50, 15.80, 22.00, 51, N'Trứng muối, gluten'),
(16, 16, 685, 30.20, 16.80, 29.40, 59, N'Trứng muối, đậu phộng, mè, gluten'),
(17, 17, 575, 27.00, 13.90, 24.00, 58, N'Trứng muối, đậu phộng, mè, gluten'),
(18, 18, 665, 36.50, 12.60, 26.20, 61, N'Trứng muối, sầu riêng, gluten'),
(19, 19, 530, 33.80, 9.50, 18.20, 56, N'Trứng muối, gluten'),
(20, 20, 525, 32.50, 10.20, 17.50, 54, N'Trứng muối, gluten'),
(21, 21, 545, 27.50, 12.00, 26.50, 53, N'Trứng, sữa, bơ, gluten'),
(22, 22, 520, 29.00, 10.40, 18.80, 52, N'Trứng muối, gluten'),
(23, 23, 515, 35.00, 6.80, 14.20, 56, N'Dừa, hạt dưa, gluten (Thuần chay)'),
(24, 24, 438, 8.80, 10.20, 15.80, 33, N'Gluten (Thuần chay - Ít GI)'),
(25, 25, 820, 31.50, 26.00, 37.50, 58, N'Hải sản (vi cá), trứng muối, đậu phộng, mè, gluten'),
(26, 26, 805, 29.00, 27.40, 35.00, 55, N'Hải sản, trứng muối, hạt điều, mè, gluten'),
(27, 27, 725, 26.80, 23.60, 31.80, 55, N'Hải sản, trứng muối, mè, gluten'),
(28, 28, 710, 29.80, 18.50, 31.00, 58, N'Trứng muối, đậu phộng, mè, gluten'),
(29, 29, 700, 30.50, 17.20, 30.40, 59, N'Trứng muối, đậu phộng, mè, gluten'),
(30, 30, 680, 37.00, 13.20, 26.80, 60, N'Trứng muối, sầu riêng, gluten'),
(31, 31, 650, 31.00, 14.50, 25.00, 53, N'Hạnh nhân, trứng muối, gluten'),
(32, 32, 540, 32.00, 10.10, 18.80, 55, N'Trứng muối, gluten'),
(33, 33, 535, 28.40, 11.00, 23.50, 52, N'Trứng, sữa, phô mai, cà phê, gluten'),
(34, 34, 540, 27.00, 11.80, 25.20, 52, N'Trứng, sữa, gluten'),
(35, 35, 580, 41.00, 9.80, 15.60, 59, N'Trứng muối, gluten'),
(36, 36, 442, 9.20, 10.60, 15.50, 34, N'Gluten (Thuần chay - Ít GI)'),
(37, 37, 740, 25.50, 24.20, 32.80, 54, N'Hải sản (bào ngư, sò điệp), trứng muối, mè, gluten'),
(38, 38, 730, 26.20, 23.50, 31.40, 54, N'Trứng muối, hạt điều, mè, gluten'),
(39, 39, 735, 25.00, 25.10, 31.00, 53, N'Giáp xác (tôm hùm), trứng muối, mè, gluten'),
(40, 40, 670, 27.50, 18.20, 28.60, 56, N'Trứng muối, đậu phộng, mè, gluten'),
(41, 41, 615, 28.50, 13.00, 22.40, 52, N'Trứng muối, gluten'),
(42, 42, 675, 34.00, 12.80, 27.20, 58, N'Trứng muối, sầu riêng, gluten'),
(43, 43, 630, 27.00, 14.80, 26.50, 50, N'Mè đen, hạnh nhân, trứng muối, gluten'),
(44, 44, 515, 26.50, 10.00, 18.00, 52, N'Trứng muối, sữa, gluten'),
(45, 45, 525, 26.00, 10.60, 22.80, 51, N'Sữa, phô mai, cà phê, cacao, gluten'),
(46, 46, 550, 24.50, 10.80, 27.00, 49, N'Hạt phỉ, sữa, cacao, gluten'),
(47, 47, 495, 31.00, 8.40, 12.60, 52, N'Hạt chia, gluten (Thuần chay)'),
(48, 48, 430, 8.50, 10.40, 15.20, 32, N'Hạt chia, gluten (Thuần chay - Ít GI)'),
(49, 49, 745, 26.00, 24.80, 32.20, 54, N'Hải sản, trứng muối, hạt điều, mè, gluten'),
(50, 50, 735, 25.80, 23.80, 31.60, 54, N'Hải sản, trứng muối, mè, gluten'),
(51, 51, 715, 27.00, 21.60, 30.50, 55, N'Hải sản, trứng muối, đậu phộng, mè, gluten'),
(52, 52, 680, 28.00, 17.50, 29.80, 56, N'Các loại hạt, trứng muối, mè, gluten'),
(53, 53, 620, 27.50, 14.40, 24.00, 50, N'Mè đen, trứng muối, gluten'),
(54, 54, 610, 29.00, 13.20, 22.60, 53, N'Trứng muối, gluten'),
(55, 55, 665, 33.50, 12.60, 26.80, 58, N'Trứng muối, sầu riêng, gluten'),
(56, 56, 518, 27.00, 9.80, 18.20, 52, N'Trứng muối, gluten'),
(57, 57, 485, 25.00, 8.60, 16.50, 48, N'Sữa, gluten'),
(58, 58, 545, 26.50, 12.20, 26.00, 52, N'Trứng, sữa, phô mai, gluten'),
(59, 59, 498, 32.00, 7.60, 11.80, 53, N'Gluten (Thuần chay)'),
(60, 60, 428, 8.20, 10.50, 15.00, 32, N'Gluten (Thuần chay - Ít GI)');
SET IDENTITY_INSERT [dbo].[ThongSoDinhDuong] OFF;

-- =============================================================================
-- PHẦN 6: VỎ HỘP (CÓ MaAnhChinh FK), COMBO (5), HỘP TỰ CHỌN (1) & LIÊN KẾT ẢNH
-- =============================================================================
SET IDENTITY_INSERT [dbo].[MauVoHop] ON;
INSERT INTO [dbo].[MauVoHop] ([MaVH], [MaCodeVH], [TenVoHop], [SucChuaBanh], [GiaVoHopRieng], [SLBanhThuongHangToiThieu], [KieuBoCucLuoi], [KemTraOLong], [SLTonKhoVo], [MoTaChatLieu], [TrangThai], [MaAnhChinh]) VALUES
(1, 'VH-4O-THANH-NGUYET', N'Hộp Sơn Mài Thanh Nguyệt 4 Bánh', 4, 85000.00, 0, 'grid-2x2', 0, 119, N'Hộp giấy mỹ thuật ép kim nhũ vàng 4 ô kèm túi xách sang trọng', 1, 2),
(2, 'VH-4O-HOANG-KIM', N'Hộp Quà Hoàng Kim Đính Trà Ô Long 4 Bánh', 4, 145000.00, 1, 'grid-2x2', 1, 66, N'Hộp cứng cao cấp phủ nhung đỏ đô 4 ô kèm hộp trà Ô Long Bảo Lộc 50g', 1, 2),
(3, 'VH-6O-HOANG-GIA', N'Hộp Sơn Mài Hoàng Gia Đế Vương 6 Bánh', 6, 220000.00, 2, 'grid-3x2', 1, 58, N'Hộp sơn mài 2 tầng 6 bánh kèm hộp trà Ô Long thượng hạng và dao cắt mạ vàng', 1, 3);
SET IDENTITY_INSERT [dbo].[MauVoHop] OFF;

SET IDENTITY_INSERT [dbo].[ComboMauCoSan] ON;
INSERT INTO [dbo].[ComboMauCoSan] ([MaCB], [MaCodeCB], [TenCombo], [MaVH], [GiaNiemYet], [GiaUuDaiCombo], [ThongDiepYNgia], [TheDoiTuongPhuHop], [TrangThai], [MaThuongHieu], [MaDM], [MaVach], [ViTriKho], [DonViTinh], [BanTrucTiep], [TongTonKho], [TrongLuongGram], [MaAnhChinh]) VALUES
(1, 'CB-KD-01', N'Lễ Hộp Kinh Đô Trăng Vàng Vinh Hoa (4 Bánh + Trà)', 2, 798000.00, 734000.00, N'Biểu tượng đoàn viên thịnh vượng với 4 vị bán chạy nhất Kinh Đô kèm Trà Ô Long.', 'BIEU_SEP,DOI_TAC,GIA_DINH', 1, 1, 1, '8939990000018', N'Kệ B1 - Lễ hộp trưng bày', N'Hộp', 1, 25, 720, 2),
(2, 'CB-DK-01', N'Lễ Hộp Đồng Khánh Bông Lúa Vàng Sum Vầy (4 Bánh)', 1, 622000.00, 572000.00, N'Tuyển tập hương vị gia truyền Đồng Khánh từ Bào Ngư đến Kim Sa Lava.', 'GIA_DINH,BIEU_SEP', 1, 2, 2, '8939990000028', N'Kệ B1 - Lễ hộp trưng bày', N'Hộp', 1, 30, 720, 2),
(3, 'CB-NL-01', N'Lễ Hộp Như Lan Phú Quý Đoàn Viên (4 Bánh + Trà)', 2, 795000.00, 731000.00, N'Lễ hộp 4 vị thủ công trứ danh Như Lan Sài Gòn dành biếu tặng trân quý.', 'BIEU_SEP,DOI_TAC', 1, 3, 1, '8939990000038', N'Kệ B1 - Lễ hộp trưng bày', N'Hộp', 1, 20, 800, 2),
(4, 'CB-BR-01', N'Lễ Hộp Brodard Sài Gòn Tài Lộc (4 Bánh + Trà)', 2, 928000.00, 854000.00, N'Giao hòa ẩm thực Pháp Brodard giữa Bào Ngư X.O, Gà Quay và Tiramisu Socola.', 'BIEU_SEP,DOI_TAC,GIOI_TRE', 1, 4, 3, '8939990000048', N'Kệ B1 - Lễ hộp trưng bày', N'Hộp', 1, 20, 700, 2),
(5, 'CB-GV-01', N'Lễ Hộp Givral Hoàng Gia 6 Vị An Khang (6 Bánh + Trà)', 3, 1355000.00, 1247000.00, N'Bộ lễ hộp sơn mài 6 vị bánh tươi Givral cao cấp bậc nhất mùa Trăng 2026.', 'BIEU_SEP,DOI_TAC', 1, 5, 1, '8939990000058', N'Kệ B1 - Lễ hộp trưng bày', N'Hộp', 1, 15, 1100, 3);
SET IDENTITY_INSERT [dbo].[ComboMauCoSan] OFF;

INSERT INTO [dbo].[ChiTietComboMau] ([MaCB], [MaBanh], [ViTriO], [SoLuong]) VALUES
(1, 1, 1, 1),
(1, 2, 2, 1),
(1, 7, 3, 1),
(1, 9, 4, 1),
(2, 13, 1, 1),
(2, 16, 2, 1),
(2, 18, 3, 1),
(2, 21, 4, 1),
(3, 25, 1, 1),
(3, 28, 2, 1),
(3, 31, 3, 1),
(3, 34, 4, 1),
(4, 37, 1, 1),
(4, 40, 2, 1),
(4, 45, 3, 1),
(4, 46, 4, 1),
(5, 49, 1, 1),
(5, 50, 2, 1),
(5, 52, 3, 1),
(5, 54, 4, 1),
(5, 57, 5, 1),
(5, 58, 6, 1);

SET IDENTITY_INSERT [dbo].[HopQuaTuChon] ON;
INSERT INTO [dbo].[HopQuaTuChon] ([MaHopTC], [MaVH], [MaND], [MaPhienKVL], [TenHopKhachDat], [MaBamCauHinh], [TongCaloHop], [TongGiaMotHop], [DaLuuMauYeuThich], [NgayTao]) VALUES
(1, 2, 4, NULL, N'Hộp Quà Tự Phối Tứ Đại Danh Hãng', 'VH2-1-15-33-58', 2480, 856000.00, 1, '2026-11-08 13:55:00');
SET IDENTITY_INSERT [dbo].[HopQuaTuChon] OFF;

INSERT INTO [dbo].[ChiTietHopTuChon] ([MaHopTC], [MaBanh], [ViTriOTrongHop], [DonGiaBanhLucChon]) VALUES
(1, 1, 1, 235000.00),
(1, 15, 2, 188000.00),
(1, 33, 3, 132000.00),
(1, 58, 4, 156000.00);

INSERT INTO [dbo].[LienKetHinhAnh] ([MaHA], [LoaiDoiTuong], [MaDoiTuong], [VaiTroAnh], [ThuTuHienThi]) VALUES
(2, 'VOHOP', 1, 'KHAY_HOP_RONG', 1),
(2, 'VOHOP', 1, 'ANH_CHINH', 2),
(2, 'VOHOP', 2, 'KHAY_HOP_RONG', 1),
(2, 'VOHOP', 2, 'ANH_CHINH', 2),
(3, 'VOHOP', 3, 'KHAY_HOP_RONG', 1),
(3, 'VOHOP', 3, 'ANH_CHINH', 2),
(2, 'COMBO', 1, 'ANH_CHINH', 1),
(2, 'COMBO', 1, 'TACH_NEN_SLOT', 2),
(2, 'COMBO', 2, 'ANH_CHINH', 1),
(2, 'COMBO', 2, 'TACH_NEN_SLOT', 2),
(2, 'COMBO', 3, 'ANH_CHINH', 1),
(2, 'COMBO', 3, 'TACH_NEN_SLOT', 2),
(2, 'COMBO', 4, 'ANH_CHINH', 1),
(2, 'COMBO', 4, 'TACH_NEN_SLOT', 2),
(3, 'COMBO', 5, 'ANH_CHINH', 1),
(3, 'COMBO', 5, 'TACH_NEN_SLOT', 2),
(4, 'BANH', 1, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 1, 'ANH_CHINH', 2),
(4, 'BANH', 2, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 2, 'ANH_CHINH', 2),
(4, 'BANH', 3, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 3, 'ANH_CHINH', 2),
(4, 'BANH', 4, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 4, 'ANH_CHINH', 2),
(4, 'BANH', 5, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 5, 'ANH_CHINH', 2),
(5, 'BANH', 6, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 6, 'ANH_CHINH', 2),
(5, 'BANH', 7, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 7, 'ANH_CHINH', 2),
(5, 'BANH', 8, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 8, 'ANH_CHINH', 2),
(6, 'BANH', 9, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 9, 'ANH_CHINH', 2),
(6, 'BANH', 10, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 10, 'ANH_CHINH', 2),
(7, 'BANH', 11, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 11, 'ANH_CHINH', 2),
(7, 'BANH', 12, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 12, 'ANH_CHINH', 2),
(4, 'BANH', 13, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 13, 'ANH_CHINH', 2),
(4, 'BANH', 14, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 14, 'ANH_CHINH', 2),
(5, 'BANH', 15, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 15, 'ANH_CHINH', 2),
(4, 'BANH', 16, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 16, 'ANH_CHINH', 2),
(4, 'BANH', 17, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 17, 'ANH_CHINH', 2),
(5, 'BANH', 18, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 18, 'ANH_CHINH', 2),
(5, 'BANH', 19, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 19, 'ANH_CHINH', 2),
(5, 'BANH', 20, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 20, 'ANH_CHINH', 2),
(6, 'BANH', 21, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 21, 'ANH_CHINH', 2),
(6, 'BANH', 22, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 22, 'ANH_CHINH', 2),
(7, 'BANH', 23, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 23, 'ANH_CHINH', 2),
(7, 'BANH', 24, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 24, 'ANH_CHINH', 2),
(4, 'BANH', 25, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 25, 'ANH_CHINH', 2),
(4, 'BANH', 26, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 26, 'ANH_CHINH', 2),
(4, 'BANH', 27, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 27, 'ANH_CHINH', 2),
(4, 'BANH', 28, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 28, 'ANH_CHINH', 2),
(4, 'BANH', 29, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 29, 'ANH_CHINH', 2),
(5, 'BANH', 30, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 30, 'ANH_CHINH', 2),
(5, 'BANH', 31, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 31, 'ANH_CHINH', 2),
(5, 'BANH', 32, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 32, 'ANH_CHINH', 2),
(6, 'BANH', 33, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 33, 'ANH_CHINH', 2),
(6, 'BANH', 34, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 34, 'ANH_CHINH', 2),
(7, 'BANH', 35, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 35, 'ANH_CHINH', 2),
(7, 'BANH', 36, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 36, 'ANH_CHINH', 2),
(4, 'BANH', 37, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 37, 'ANH_CHINH', 2),
(4, 'BANH', 38, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 38, 'ANH_CHINH', 2),
(4, 'BANH', 39, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 39, 'ANH_CHINH', 2),
(4, 'BANH', 40, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 40, 'ANH_CHINH', 2),
(5, 'BANH', 41, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 41, 'ANH_CHINH', 2),
(5, 'BANH', 42, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 42, 'ANH_CHINH', 2),
(5, 'BANH', 43, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 43, 'ANH_CHINH', 2),
(5, 'BANH', 44, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 44, 'ANH_CHINH', 2),
(6, 'BANH', 45, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 45, 'ANH_CHINH', 2),
(6, 'BANH', 46, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 46, 'ANH_CHINH', 2),
(7, 'BANH', 47, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 47, 'ANH_CHINH', 2),
(7, 'BANH', 48, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 48, 'ANH_CHINH', 2),
(4, 'BANH', 49, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 49, 'ANH_CHINH', 2),
(4, 'BANH', 50, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 50, 'ANH_CHINH', 2),
(4, 'BANH', 51, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 51, 'ANH_CHINH', 2),
(4, 'BANH', 52, 'TACH_NEN_SLOT', 1),
(4, 'BANH', 52, 'ANH_CHINH', 2),
(5, 'BANH', 53, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 53, 'ANH_CHINH', 2),
(5, 'BANH', 54, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 54, 'ANH_CHINH', 2),
(5, 'BANH', 55, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 55, 'ANH_CHINH', 2),
(5, 'BANH', 56, 'TACH_NEN_SLOT', 1),
(5, 'BANH', 56, 'ANH_CHINH', 2),
(6, 'BANH', 57, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 57, 'ANH_CHINH', 2),
(6, 'BANH', 58, 'TACH_NEN_SLOT', 1),
(6, 'BANH', 58, 'ANH_CHINH', 2),
(7, 'BANH', 59, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 59, 'ANH_CHINH', 2),
(7, 'BANH', 60, 'TACH_NEN_SLOT', 1),
(7, 'BANH', 60, 'ANH_CHINH', 2);

-- =============================================================================
-- PHẦN 7: KHUYẾN MÃI, CHIẾT KHẤU SỐ LƯỢNG, LANDING PAGE & CẤU HÌNH HỆ THỐNG
-- =============================================================================
SET IDENTITY_INSERT [dbo].[BangChietKhauSiB2B] ON;
INSERT INTO [dbo].[BangChietKhauSiB2B] ([MaMocCK], [TenMocB2B], [SLHopToiThieu], [SLHopToiDa], [PhanTramChietKhau], [MienPhiInLogo], [PhiKhuonInLogo], [SoNgaySanXuatCongThem]) VALUES
(1, N'Ưu Đãi Đơn Nhóm (10 - 29 Hộp)',      10,  29,  8.00, 0, 500000.00, 2),
(2, N'Ưu Đãi Quà Tặng (30 - 49 Hộp)',      30,  49, 12.00, 1,      0.00, 3),
(3, N'Ưu Đãi Đại Lý Nhỏ (50 - 99 Hộp)',    50,  99, 16.00, 1,      0.00, 4),
(4, N'Ưu Đãi Số Lượng Lớn (Từ 100 Hộp)',  100, 500, 22.00, 1,      0.00, 5);
SET IDENTITY_INSERT [dbo].[BangChietKhauSiB2B] OFF;

SET IDENTITY_INSERT [dbo].[MaKhuyenMai] ON;
INSERT INTO [dbo].[MaKhuyenMai] ([MaKM], [CodeKM], [TenChuongTrinh], [LoaiGiam], [GiaTriGiam], [GiamToiDa], [DonToiThieu], [SLToiDa], [SLDaDung], [ChoPhepKhachVangLai], [NgayBatDau], [NgayKetThuc], [TrangThaiKichHoat]) VALUES
(1, 'TRANGTRON2026', N'Ưu Đãi Đêm Trăng Đoàn Viên (-10%)',     'PHAN_TRAM', 10.00,    150000.00,  200000.00,  500, 1, 1, '2026-01-01 00:00:00', '2027-12-31 23:59:59', 1),
(2, 'QUATANG15K',    N'Quà Tặng Tri Ân Khách Mới (-15.000đ)',  'TIEN_MAT',  15000.00,  15000.00,  100000.00, 1000, 0, 1, '2026-01-01 00:00:00', '2027-12-31 23:59:59', 1),
(3, 'DOANVIEN50K',   N'Lễ Hộp Đoàn Viên (-50.000đ)',           'TIEN_MAT',  50000.00,  50000.00,  600000.00,  300, 0, 1, '2026-01-01 00:00:00', '2027-12-31 23:59:59', 1),
(4, 'VIPNGUYETCAT',  N'Đặc Quyền Hội Viên Nguyệt Cát (-12%)',  'PHAN_TRAM', 12.00,    250000.00, 1000000.00,  200, 0, 0, '2026-01-01 00:00:00', '2027-12-31 23:59:59', 1);
SET IDENTITY_INSERT [dbo].[MaKhuyenMai] OFF;

INSERT INTO [dbo].[CauHinhLandingPage] ([MaCodeKhuVuc], [TieuDeChinh], [TieuDePhu], [NoiDungMoTa], [MaAnhNen], [MaAnhPhu_Parallax], [ChuNutBamCTA], [DuongDanCTA], [MocThoiGianDemNguoc], [ThuTuHienThi], [DangKichHoat]) VALUES
('TANG_1_HERO',     N'Nguyệt Cát — Tinh Hoa Trăng Rằm 2026', N'Đại lý phân phối chính hãng Kinh Đô, Đồng Khánh, Như Lan, Brodard & Givral', N'Tuyển chọn 60 hương vị bánh trung thu chính gốc cùng Studio phối hộp quà tặng & Thiệp chúc mừng 3D QR.', 1, NULL, N'Khám Phá Studio Phối Hộp', '#studio-phoi-hop', '2026-12-31 23:59:59', 1, 1),
('TANG_2_STUDIO',   N'Studio Tự Phối Hộp Quà 4 & 6 Bánh',    N'Tùy chọn đa thương hiệu trong cùng một lễ hộp sơn mài sang trọng', N'Xem trực tiếp tổng trọng lượng, bảng dinh dưỡng Calo và chi phí minh bạch theo từng vị bánh.', 2, NULL, N'Tạo Hộp Quà Ngay', '#studio-phoi-hop', NULL, 2, 1),
('TANG_3_BOSUUTAP', N'Bộ Sưu Tập 60 Vị Bánh Từ 5 Danh Hãng', N'Đầy đủ dòng Thượng hạng, Truyền thống, Kim Sa Lava và Thuần chay Isomalt', N'Cam kết hàng mới xuất lò, kiểm soát hạn sử dụng FEFO và hóa đơn VAT đầy đủ.', 4, NULL, N'Xem Tất Cả Sản Phẩm', '#bo-suu-tap-banh', NULL, 3, 1),
('TANG_4_CHIETKHAU',N'Ưu Đãi Chiết Khấu Số Lượng Đến 22%',   N'Miễn phí ép kim logo từ 30 hộp — Giao hàng đa điểm toàn quốc', N'Giải pháp quà tặng trọn gói cho cơ quan, công ty và khách hàng đặt nhóm.', 3, NULL, N'Nhận Báo Giá Ngay', '#chiet-khau-so-luong', NULL, 4, 1),
('TANG_5_THIEPQR',  N'Thiệp Chúc Mừng 3D Kèm Mã QR Cá Nhân', N'Gửi trọn tâm tình mùa Trăng Rằm đến người thân & đối tác', N'Mỗi lễ hộp có thể đính kèm thiệp điện tử 3D riêng biệt mở ngay bằng điện thoại.', 1, NULL, N'Trải Nghiệm Đặt Quà', '/ThanhToan', NULL, 5, 1);

INSERT INTO [dbo].[CauHinhHeThong] ([KhoaCauHinh], [GiaTriJson], [MoTa], [NgayCapNhat], [MaNdCapNhat]) VALUES
(N'CAU_HINH_QR_MOMO', N'{"SoTaiKhoanMoMo": "0814585526", "SoDienThoaiHienThi": "0814 585 526", "TenChuTaiKhoan": "LE MINH DUC", "TenNganHangVi": "MOMO", "MaBinNganHang": "970422", "CuPhapChuyenKhoan": "NGUYETCAT {MaTraCuuDh}", "ThoiGianChoPhut": 15, "SoTaiKhoanNganHang": "0814585526", "TenNganHangVietQr": "MBBank"}', N'Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)', '2026-11-01 08:00:00', 1);

-- =============================================================================
-- PHẦN 8: 67 LÔ SẢN XUẤT FEFO & 6 PHIẾU NHẬP KHO
-- =============================================================================
SET IDENTITY_INSERT [dbo].[LoSanXuatBanh] ON;
INSERT INTO [dbo].[LoSanXuatBanh] ([MaLo], [MaBanh], [MaLoCode], [NgaySanXuat], [HanSuDung], [SLNhapBanDau], [SLConLai], [PhanTramGiamCanDate], [TrangThaiLo]) VALUES
(1, 1, 'LO-2611-KD-01-01', '2026-10-28', '2026-12-27', 40, 27, 0.00, 'DANG_BAN'),
(2, 2, 'LO-2611-KD-02-01', '2026-10-28', '2026-12-27', 45, 37, 0.00, 'DANG_BAN'),
(3, 3, 'LO-2611-KD-03-01', '2026-10-28', '2026-12-27', 35, 35, 0.00, 'DANG_BAN'),
(4, 4, 'LO-2611-KD-04-01', '2026-10-28', '2026-12-27', 60, 56, 0.00, 'DANG_BAN'),
(5, 5, 'LO-2611-KD-05-01', '2026-10-28', '2026-12-27', 55, 53, 0.00, 'DANG_BAN'),
(6, 6, 'LO-2611-KD-06-01', '2026-10-28', '2026-12-27', 60, 58, 0.00, 'DANG_BAN'),
(7, 7, 'LO-2611-KD-07-01', '2026-10-28', '2026-12-27', 50, 42, 0.00, 'DANG_BAN'),
(8, 8, 'LO-2611-KD-08-01', '2026-10-15', '2026-11-29', 10, 10, 20.00, 'XA_CAN_DATE'),
(9, 8, 'LO-2611-KD-08-02', '2026-10-28', '2026-12-27', 35, 35, 0.00, 'DANG_BAN'),
(10, 9, 'LO-2611-KD-09-01', '2026-10-28', '2026-12-27', 55, 43, 0.00, 'DANG_BAN'),
(11, 10, 'LO-2611-KD-10-01', '2026-10-28', '2026-12-27', 45, 43, 0.00, 'DANG_BAN'),
(12, 11, 'LO-2611-KD-11-01', '2026-10-28', '2026-12-27', 50, 50, 0.00, 'DANG_BAN'),
(13, 12, 'LO-2611-KD-12-01', '2026-10-28', '2026-12-27', 40, 38, 0.00, 'DANG_BAN'),
(14, 13, 'LO-2611-DK-01-01', '2026-10-29', '2026-12-28', 35, 32, 0.00, 'DANG_BAN'),
(15, 14, 'LO-2611-DK-02-01', '2026-10-29', '2026-12-28', 45, 43, 0.00, 'DANG_BAN'),
(16, 15, 'LO-2611-DK-03-01', '2026-10-29', '2026-12-28', 35, 34, 0.00, 'DANG_BAN'),
(17, 16, 'LO-2611-DK-04-01', '2026-10-29', '2026-12-28', 55, 54, 0.00, 'DANG_BAN'),
(18, 17, 'LO-2611-DK-05-01', '2026-10-29', '2026-12-28', 55, 54, 0.00, 'DANG_BAN'),
(19, 18, 'LO-2611-DK-06-01', '2026-10-29', '2026-12-28', 50, 47, 0.00, 'DANG_BAN'),
(20, 19, 'LO-2611-DK-07-01', '2026-10-29', '2026-12-28', 45, 45, 0.00, 'DANG_BAN'),
(21, 20, 'LO-2611-DK-08-01', '2026-10-15', '2026-11-29', 10, 10, 20.00, 'XA_CAN_DATE'),
(22, 20, 'LO-2611-DK-08-02', '2026-10-29', '2026-12-28', 35, 35, 0.00, 'DANG_BAN'),
(23, 21, 'LO-2611-DK-09-01', '2026-10-29', '2026-12-28', 45, 44, 0.00, 'DANG_BAN'),
(24, 22, 'LO-2611-DK-10-01', '2026-10-29', '2026-12-28', 40, 40, 0.00, 'DANG_BAN'),
(25, 23, 'LO-2611-DK-11-01', '2026-10-29', '2026-12-28', 45, 45, 0.00, 'DANG_BAN'),
(26, 24, 'LO-2611-DK-12-01', '2026-10-29', '2026-12-28', 40, 38, 0.00, 'DANG_BAN'),
(27, 25, 'LO-2611-NL-01-01', '2026-11-01', '2026-12-31', 35, 29, 0.00, 'DANG_BAN'),
(28, 26, 'LO-2611-NL-02-01', '2026-11-01', '2026-12-31', 30, 30, 0.00, 'DANG_BAN'),
(29, 27, 'LO-2611-NL-03-01', '2026-11-01', '2026-12-31', 35, 35, 0.00, 'DANG_BAN'),
(30, 28, 'LO-2611-NL-04-01', '2026-11-01', '2026-12-31', 50, 46, 0.00, 'DANG_BAN'),
(31, 29, 'LO-2611-NL-05-01', '2026-11-01', '2026-12-31', 45, 45, 0.00, 'DANG_BAN'),
(32, 30, 'LO-2611-NL-06-01', '2026-11-01', '2026-12-31', 45, 45, 0.00, 'DANG_BAN'),
(33, 31, 'LO-2611-NL-07-01', '2026-11-01', '2026-12-31', 40, 35, 0.00, 'DANG_BAN'),
(34, 32, 'LO-2611-NL-08-01', '2026-11-01', '2026-12-31', 45, 45, 0.00, 'DANG_BAN'),
(35, 33, 'LO-2611-NL-09-01', '2026-11-01', '2026-12-31', 40, 39, 0.00, 'DANG_BAN'),
(36, 34, 'LO-2611-NL-10-01', '2026-11-01', '2026-12-31', 45, 41, 0.00, 'DANG_BAN'),
(37, 35, 'LO-2611-NL-11-01', '2026-11-01', '2026-12-31', 40, 40, 0.00, 'DANG_BAN'),
(38, 36, 'LO-2611-NL-12-01', '2026-11-01', '2026-12-31', 40, 40, 0.00, 'DANG_BAN'),
(39, 37, 'LO-2611-BR-01-01', '2026-11-03', '2026-12-20', 30, 29, 0.00, 'DANG_BAN'),
(40, 38, 'LO-2611-BR-02-01', '2026-11-03', '2026-12-20', 30, 30, 0.00, 'DANG_BAN'),
(41, 39, 'LO-2611-BR-03-01', '2026-11-03', '2026-12-20', 25, 25, 0.00, 'DANG_BAN'),
(42, 40, 'LO-2611-BR-04-01', '2026-11-03', '2026-12-20', 40, 39, 0.00, 'DANG_BAN'),
(43, 41, 'LO-2611-BR-05-01', '2026-11-03', '2026-12-20', 40, 40, 0.00, 'DANG_BAN'),
(44, 42, 'LO-2611-BR-06-01', '2026-11-03', '2026-12-20', 35, 35, 0.00, 'DANG_BAN'),
(45, 43, 'LO-2611-BR-07-01', '2026-11-03', '2026-12-20', 35, 35, 0.00, 'DANG_BAN'),
(46, 44, 'LO-2611-BR-08-01', '2026-11-03', '2026-12-20', 35, 35, 0.00, 'DANG_BAN'),
(47, 45, 'LO-2611-BR-09-01', '2026-11-03', '2026-12-20', 45, 42, 0.00, 'DANG_BAN'),
(48, 46, 'LO-2611-BR-10-01', '2026-11-03', '2026-12-20', 40, 39, 0.00, 'DANG_BAN'),
(49, 47, 'LO-2611-BR-11-01', '2026-11-03', '2026-12-20', 35, 35, 0.00, 'DANG_BAN'),
(50, 48, 'LO-2611-BR-12-01', '2026-11-03', '2026-12-20', 35, 33, 0.00, 'DANG_BAN'),
(51, 49, 'LO-2611-GV-01-01', '2026-11-05', '2026-12-22', 30, 28, 0.00, 'DANG_BAN'),
(52, 50, 'LO-2611-GV-02-01', '2026-11-05', '2026-12-22', 30, 28, 0.00, 'DANG_BAN'),
(53, 51, 'LO-2611-GV-03-01', '2026-11-05', '2026-12-22', 35, 35, 0.00, 'DANG_BAN'),
(54, 52, 'LO-2611-GV-04-01', '2026-11-05', '2026-12-22', 40, 38, 0.00, 'DANG_BAN'),
(55, 53, 'LO-2611-GV-05-01', '2026-11-05', '2026-12-22', 35, 35, 0.00, 'DANG_BAN'),
(56, 54, 'LO-2611-GV-06-01', '2026-11-05', '2026-12-22', 40, 38, 0.00, 'DANG_BAN'),
(57, 55, 'LO-2611-GV-07-01', '2026-11-05', '2026-12-22', 35, 35, 0.00, 'DANG_BAN'),
(58, 56, 'LO-2611-GV-08-01', '2026-11-05', '2026-12-22', 40, 40, 0.00, 'DANG_BAN'),
(59, 57, 'LO-2611-GV-09-01', '2026-11-05', '2026-12-22', 40, 36, 0.00, 'DANG_BAN'),
(60, 58, 'LO-2611-GV-10-01', '2026-11-05', '2026-12-22', 45, 42, 0.00, 'DANG_BAN'),
(61, 59, 'LO-2611-GV-11-01', '2026-11-05', '2026-12-22', 35, 35, 0.00, 'DANG_BAN'),
(62, 60, 'LO-2611-GV-12-01', '2026-11-05', '2026-12-22', 40, 38, 0.00, 'DANG_BAN'),
(63, 1, 'LO-2611-KD-01-02', '2026-11-10', '2027-01-10', 15, 15, 0.00, 'DANG_BAN'),
(64, 9, 'LO-2611-KD-09-02', '2026-11-10', '2027-01-10', 15, 15, 0.00, 'DANG_BAN'),
(65, 13, 'LO-2611-DK-01-02', '2026-11-10', '2027-01-10', 15, 15, 0.00, 'DANG_BAN'),
(66, 25, 'LO-2611-NL-01-02', '2026-11-10', '2027-01-10', 15, 15, 0.00, 'DANG_BAN'),
(67, 37, 'LO-2611-BR-01-02', '2026-11-10', '2027-01-10', 15, 15, 0.00, 'DANG_BAN');
SET IDENTITY_INSERT [dbo].[LoSanXuatBanh] OFF;

SET IDENTITY_INSERT [dbo].[PhieuNhapKho] ON;
INSERT INTO [dbo].[PhieuNhapKho] ([MaPn], [SoPhieuNhap], [NhaCungCap], [NgayNhap], [TongTienHang], [GiamGia], [TongPhaiTra], [DaThanhToan], [TrangThai], [TrangThaiThanhToan], [GhiChu], [NguoiLap], [MaNdLap], [MaNcc]) VALUES
(1, N'PN-20261101-001', N'Nhà phân phối Kinh Đô Chính Hãng', '2026-11-01 08:30:00', 50922000.00, 0.00, 50922000.00, 50922000.00, 'DA_NHAP_KHO', 'DA_THANH_TOAN', N'Nhập lô đầu mùa 12 vị Kinh Đô chính hãng', N'Lê Minh Đức (Chủ cửa hàng)', 1, 1),
(2, N'PN-20261102-002', N'Lò Bánh Đồng Khánh Chính Gốc', '2026-11-02 09:15:00', 43571200.00, 0.00, 43571200.00, 43571200.00, 'DA_NHAP_KHO', 'DA_THANH_TOAN', N'Nhập lô đầu mùa 12 vị Đồng Khánh Bông Lúa Vàng', N'Lê Minh Đức (Chủ cửa hàng)', 1, 2),
(3, N'PN-20261104-003', N'Bánh Trung Thu Như Lan TP.HCM', '2026-11-04 08:45:00', 53482500.00, 0.00, 53482500.00, 53482500.00, 'DA_NHAP_KHO', 'DA_THANH_TOAN', N'Nhập lô đầu mùa 12 vị Như Lan Hàm Nghi', N'Lê Minh Đức (Chủ cửa hàng)', 1, 3),
(4, N'PN-20261106-004', N'Brodard Bakery Saigon', '2026-11-06 10:00:00', 56213400.00, 0.00, 56213400.00, 33728040.00, 'DA_NHAP_KHO', 'THANH_TOAN_MOT_PHAN', N'Nhập lô 12 vị bánh tươi Brodard (thanh toán trước 60%, còn lại công nợ 15 ngày)', N'Lê Minh Đức (Chủ cửa hàng)', 1, 4),
(5, N'PN-20261108-005', N'Givral Bakery Chính Hãng', '2026-11-08 09:30:00', 54056250.00, 0.00, 54056250.00, 0.00, 'DA_NHAP_KHO', 'CHUA_THANH_TOAN', N'Nhập lô 12 vị bánh tươi Givral (công nợ gối đầu cuối đợt)', N'Lê Minh Đức (Chủ cửa hàng)', 1, 5),
(6, N'PN-20261112-006', N'Đại Lý Điều Tiết Bánh Miền Tây (An Giang)', '2026-11-12 14:20:00', 12776400.00, 0.00, 12776400.00, 12776400.00, 'DA_NHAP_KHO', 'DA_THANH_TOAN', N'Nhập bổ sung gấp 5 dòng thượng hạng bán chạy (lô mới date dài hơn phục vụ xuất FEFO)', N'Lê Minh Đức (Chủ cửa hàng)', 1, 6);
SET IDENTITY_INSERT [dbo].[PhieuNhapKho] OFF;

INSERT INTO [dbo].[ChiTietPhieuNhap] ([MaPn], [MaSp], [MaLo], [SoLuong], [DonGiaNhap], [ThanhTien], [NgaySanXuat], [HanSuDung], [PhanTramChietKhau], [GiaNiemYet]) VALUES
(1, 1, 1, 40, 169200.00, 6768000.00, '2026-10-28', '2026-12-27', 28.00, 235000),
(1, 2, 2, 45, 120960.00, 5443200.00, '2026-10-28', '2026-12-27', 28.00, 168000),
(1, 3, 3, 35, 142560.00, 4989600.00, '2026-10-28', '2026-12-27', 28.00, 198000),
(1, 4, 4, 60, 90000.00, 5400000.00, '2026-10-28', '2026-12-27', 28.00, 125000),
(1, 5, 5, 55, 70560.00, 3880800.00, '2026-10-28', '2026-12-27', 28.00, 98000),
(1, 6, 6, 60, 51840.00, 3110400.00, '2026-10-28', '2026-12-27', 28.00, 72000),
(1, 7, 7, 50, 82800.00, 4140000.00, '2026-10-28', '2026-12-27', 28.00, 115000),
(1, 8, 8, 10, 54720.00, 547200.00, '2026-10-15', '2026-11-29', 28.00, 76000),
(1, 8, 9, 35, 54720.00, 1915200.00, '2026-10-28', '2026-12-27', 28.00, 76000),
(1, 9, 10, 55, 97200.00, 5346000.00, '2026-10-28', '2026-12-27', 28.00, 135000),
(1, 10, 11, 45, 84960.00, 3823200.00, '2026-10-28', '2026-12-27', 28.00, 118000),
(1, 11, 12, 50, 48960.00, 2448000.00, '2026-10-28', '2026-12-27', 28.00, 68000),
(1, 12, 13, 40, 77760.00, 3110400.00, '2026-10-28', '2026-12-27', 28.00, 108000),
(2, 13, 14, 35, 142080.00, 4972800.00, '2026-10-29', '2026-12-28', 26.00, 192000),
(2, 14, 15, 45, 122100.00, 5494500.00, '2026-10-29', '2026-12-28', 26.00, 165000),
(2, 15, 16, 35, 139120.00, 4869200.00, '2026-10-29', '2026-12-28', 26.00, 188000),
(2, 16, 17, 55, 87320.00, 4802600.00, '2026-10-29', '2026-12-28', 26.00, 118000),
(2, 17, 18, 55, 63640.00, 3500200.00, '2026-10-29', '2026-12-28', 26.00, 86000),
(2, 18, 19, 50, 77700.00, 3885000.00, '2026-10-29', '2026-12-28', 26.00, 105000),
(2, 19, 20, 45, 51060.00, 2297700.00, '2026-10-29', '2026-12-28', 26.00, 69000),
(2, 20, 21, 10, 50320.00, 503200.00, '2026-10-15', '2026-11-29', 26.00, 68000),
(2, 20, 22, 35, 50320.00, 1761200.00, '2026-10-29', '2026-12-28', 26.00, 68000),
(2, 21, 23, 45, 90280.00, 4062600.00, '2026-10-29', '2026-12-28', 26.00, 122000),
(2, 22, 24, 40, 65860.00, 2634400.00, '2026-10-29', '2026-12-28', 26.00, 89000),
(2, 23, 25, 45, 45880.00, 2064600.00, '2026-10-29', '2026-12-28', 26.00, 62000),
(2, 24, 26, 40, 68080.00, 2723200.00, '2026-10-29', '2026-12-28', 26.00, 92000),
(3, 25, 27, 35, 183750.00, 6431250.00, '2026-11-01', '2026-12-31', 25.00, 245000),
(3, 26, 28, 30, 208500.00, 6255000.00, '2026-11-01', '2026-12-31', 25.00, 278000),
(3, 27, 29, 35, 168750.00, 5906250.00, '2026-11-01', '2026-12-31', 25.00, 225000),
(3, 28, 30, 50, 106500.00, 5325000.00, '2026-11-01', '2026-12-31', 25.00, 142000),
(3, 29, 31, 45, 96000.00, 4320000.00, '2026-11-01', '2026-12-31', 25.00, 128000),
(3, 30, 32, 45, 88500.00, 3982500.00, '2026-11-01', '2026-12-31', 25.00, 118000),
(3, 31, 33, 40, 93750.00, 3750000.00, '2026-11-01', '2026-12-31', 25.00, 125000),
(3, 32, 34, 45, 69000.00, 3105000.00, '2026-11-01', '2026-12-31', 25.00, 92000),
(3, 33, 35, 40, 99000.00, 3960000.00, '2026-11-01', '2026-12-31', 25.00, 132000),
(3, 34, 36, 45, 103500.00, 4657500.00, '2026-11-01', '2026-12-31', 25.00, 138000),
(3, 35, 37, 40, 66000.00, 2640000.00, '2026-11-01', '2026-12-31', 25.00, 88000),
(3, 36, 38, 40, 78750.00, 3150000.00, '2026-11-01', '2026-12-31', 25.00, 105000),
(4, 37, 39, 30, 216600.00, 6498000.00, '2026-11-03', '2026-12-20', 24.00, 285000),
(4, 38, 40, 30, 201400.00, 6042000.00, '2026-11-03', '2026-12-20', 24.00, 265000),
(4, 39, 41, 25, 209000.00, 5225000.00, '2026-11-03', '2026-12-20', 24.00, 275000),
(4, 40, 42, 40, 127680.00, 5107200.00, '2026-11-03', '2026-12-20', 24.00, 168000),
(4, 41, 43, 40, 112480.00, 4499200.00, '2026-11-03', '2026-12-20', 24.00, 148000),
(4, 42, 44, 35, 120080.00, 4202800.00, '2026-11-03', '2026-12-20', 24.00, 158000),
(4, 43, 45, 35, 110200.00, 3857000.00, '2026-11-03', '2026-12-20', 24.00, 145000),
(4, 44, 46, 35, 97280.00, 3404800.00, '2026-11-03', '2026-12-20', 24.00, 128000),
(4, 45, 47, 45, 123120.00, 5540400.00, '2026-11-03', '2026-12-20', 24.00, 162000),
(4, 46, 48, 40, 127680.00, 5107200.00, '2026-11-03', '2026-12-20', 24.00, 168000),
(4, 47, 49, 35, 89680.00, 3138800.00, '2026-11-03', '2026-12-20', 24.00, 118000),
(4, 48, 50, 35, 102600.00, 3591000.00, '2026-11-03', '2026-12-20', 24.00, 135000),
(5, 49, 51, 30, 208500.00, 6255000.00, '2026-11-05', '2026-12-22', 25.00, 278000),
(5, 50, 52, 30, 196500.00, 5895000.00, '2026-11-05', '2026-12-22', 25.00, 262000),
(5, 51, 53, 35, 161250.00, 5643750.00, '2026-11-05', '2026-12-22', 25.00, 215000),
(5, 52, 54, 40, 114000.00, 4560000.00, '2026-11-05', '2026-12-22', 25.00, 152000),
(5, 53, 55, 35, 103500.00, 3622500.00, '2026-11-05', '2026-12-22', 25.00, 138000),
(5, 54, 56, 40, 106500.00, 4260000.00, '2026-11-05', '2026-12-22', 25.00, 142000),
(5, 55, 57, 35, 111000.00, 3885000.00, '2026-11-05', '2026-12-22', 25.00, 148000),
(5, 56, 58, 40, 88500.00, 3540000.00, '2026-11-05', '2026-12-22', 25.00, 118000),
(5, 57, 59, 40, 108750.00, 4350000.00, '2026-11-05', '2026-12-22', 25.00, 145000),
(5, 58, 60, 45, 117000.00, 5265000.00, '2026-11-05', '2026-12-22', 25.00, 156000),
(5, 59, 61, 35, 84000.00, 2940000.00, '2026-11-05', '2026-12-22', 25.00, 112000),
(5, 60, 62, 40, 96000.00, 3840000.00, '2026-11-05', '2026-12-22', 25.00, 128000),
(6, 1, 63, 15, 183300.00, 2749500.00, '2026-11-10', '2027-01-10', 22.00, 235000),
(6, 9, 64, 15, 105300.00, 1579500.00, '2026-11-10', '2027-01-10', 22.00, 135000),
(6, 13, 65, 15, 149760.00, 2246400.00, '2026-11-10', '2027-01-10', 22.00, 192000),
(6, 25, 66, 15, 191100.00, 2866500.00, '2026-11-10', '2027-01-10', 22.00, 245000),
(6, 37, 67, 15, 222300.00, 3334500.00, '2026-11-10', '2027-01-10', 22.00, 285000);

-- =============================================================================
-- PHẦN 9: 10 ĐƠN HÀNG (GẮN MaKM, MaMocCK, KenhBan='POS_TAI_QUAY'), THIỆP 3D & NHẬT KÝ
-- =============================================================================
SET IDENTITY_INSERT [dbo].[DonHang] ON;
INSERT INTO [dbo].[DonHang] ([MaDH], [MaTraCuuDH], [MaND], [MaPhienKVL], [HoTenNguoiMua], [SDTNguoiMua], [EmailNguoiMua], [LaDonDoanhNghiepB2B], [TenCongTy], [MaSoThue], [MaAnhLogoCongTy], [TongTienHang], [PhanTramCK_B2B], [TienChietKhauB2B], [TienGiamVoucher], [PhiInLogo], [TongPhiVanChuyen], [TongThanhToanCuoi], [PhuongThucTT], [TrangThaiTT], [TrangThaiDH], [NgayDat], [TienGiamHangVip], [PhanTramGiamVip], [SoDiemDaDung], [TienGiamDiemVip], [HanThanhToanQr], [DaCongDiemThuong], [DaHoanDiemKhiHuy], [LyDoHuy], [MaNdLap], [KenhBan], [SoTienKhachDaTra], [DaRollbackKho], [MaKM], [MaMocCK]) VALUES
(1, 'TT2026-1001', 4, NULL, N'Nguyễn Minh Châu', '0909123456', 'minhchau@gmail.com', 0, NULL, NULL, NULL, 1958000.00, 0.00, 0.00, 0.00, 0.00, 0.00, 1860100.00, 'MOMO_QR', 'DA_TT', 'HOAN_TAT', '2026-11-05 09:20:00', 97900, 5, 0, 0, NULL, 1, 0, NULL, NULL, 'WEB_ONLINE', 1860100.00, 0, NULL, NULL),
(2, 'TT2026-1002', 4, NULL, N'Nguyễn Minh Châu', '0909123456', 'minhchau@gmail.com', 0, NULL, NULL, NULL, 1180000.00, 0.00, 0.00, 118000.00, 0.00, 0.00, 1003000.00, 'MOMO_QR', 'DA_TT', 'HOAN_TAT', '2026-11-08 14:10:00', 59000, 5, 0, 0, NULL, 1, 0, NULL, NULL, 'WEB_ONLINE', 1003000.00, 0, 1, NULL),
(3, 'TT2026-1003', 4, NULL, N'Nguyễn Minh Châu', '0909123456', 'minhchau@gmail.com', 0, NULL, NULL, NULL, 3008000.00, 0.00, 0.00, 0.00, 0.00, 0.00, 2857600.00, 'COD', 'DA_TT', 'HOAN_TAT', '2026-11-11 10:45:00', 150400, 5, 0, 0, NULL, 1, 0, NULL, NULL, 'WEB_ONLINE', 2857600.00, 0, NULL, NULL),
(4, 'POS26-1004', NULL, NULL, N'Lê Hoàng Nam (Khách lẻ POS)', '0939445566', NULL, 0, NULL, NULL, NULL, 1354000.00, 0.00, 0.00, 0.00, 0.00, 0.00, 1354000.00, 'TIEN_MAT', 'DA_TT', 'HOAN_TAT', '2026-11-12 15:30:00', 0, 0, 0, 0, NULL, 0, 0, NULL, 2, 'POS_TAI_QUAY', 1354000.00, 0, NULL, NULL),
(5, 'POS26-1005', NULL, NULL, N'Công ty TNHH TM-DV Mekong An Giang', '02963855999', 'ketoan@mekongag.vn', 1, N'Công ty TNHH TM-DV Mekong An Giang', '1602158492', NULL, 7328000.00, 8.00, 586240.00, 0.00, 0.00, 0.00, 6741760.00, 'MOMO_QR', 'DA_TT', 'HOAN_TAT', '2026-11-14 09:50:00', 0, 0, 0, 0, NULL, 0, 0, NULL, 2, 'POS_TAI_QUAY', 6741760.00, 0, NULL, 1),
(6, 'TT2026-1006', 4, NULL, N'Nguyễn Minh Châu', '0909123456', 'minhchau@gmail.com', 0, NULL, NULL, NULL, 570000.00, 0.00, 0.00, 0.00, 0.00, 25000.00, 528000.00, 'COD', 'CHUA_TT', 'DA_HUY', '2026-11-15 08:50:00', 57000, 10, 10, 10000, NULL, 0, 1, N'Khách hàng muốn đổi sang lễ hộp Brodard 4 bánh kèm trà', NULL, 'WEB_ONLINE', 0.00, 1, NULL, NULL),
(7, 'POS26-1007', NULL, NULL, N'Phạm Thị Thanh Trúc (Khách lẻ POS)', '0977223344', NULL, 0, NULL, NULL, NULL, 1066000.00, 0.00, 0.00, 0.00, 0.00, 0.00, 1066000.00, 'TIEN_MAT', 'DA_TT', 'HOAN_TAT', '2026-11-16 17:15:00', 0, 0, 0, 0, NULL, 0, 0, NULL, 3, 'POS_TAI_QUAY', 1066000.00, 0, NULL, NULL),
(8, 'TT2026-1008', 4, NULL, N'Nguyễn Minh Châu', '0909123456', 'minhchau@gmail.com', 0, NULL, NULL, NULL, 1144000.00, 0.00, 0.00, 0.00, 0.00, 0.00, 1029600.00, 'MOMO_QR', 'DA_TT', 'DANG_GIAO', '2026-11-18 11:05:00', 114400, 10, 0, 0, NULL, 0, 0, NULL, NULL, 'WEB_ONLINE', 1029600.00, 0, NULL, NULL),
(9, 'TT2026-1009', 4, NULL, N'Nguyễn Minh Châu', '0909123456', 'minhchau@gmail.com', 0, NULL, NULL, NULL, 572000.00, 0.00, 0.00, 0.00, 0.00, 25000.00, 539800.00, 'COD', 'CHUA_TT', 'DA_XAC_NHAN', '2026-11-19 16:40:00', 57200, 10, 0, 0, NULL, 0, 0, NULL, NULL, 'WEB_ONLINE', 0.00, 0, NULL, NULL),
(10, 'TT2026-1010', 4, NULL, N'Nguyễn Minh Châu', '0909123456', 'minhchau@gmail.com', 0, NULL, NULL, NULL, 686000.00, 0.00, 0.00, 0.00, 0.00, 25000.00, 622400.00, 'COD', 'CHUA_TT', 'CHO_XAC_NHAN', '2026-11-20 09:30:00', 68600, 10, 20, 20000, NULL, 0, 0, NULL, NULL, 'WEB_ONLINE', 0.00, 0, NULL, NULL);
SET IDENTITY_INSERT [dbo].[DonHang] OFF;

SET IDENTITY_INSERT [dbo].[DotGiaoHang_DaDiem] ON;
INSERT INTO [dbo].[DotGiaoHang_DaDiem] ([MaDotGH], [MaDH], [TenNguoiNhan], [SDTNguoiNhan], [DiaChiGiaoChiTiet], [NgayHenGiao], [KhungGioGiao], [PhiShipDotNay], [TrangThaiGiao], [CoKemThiepQR], [MaTokenThiepQR], [LoiChucThiep], [MauGiaoDienThiep], [SoLanMoThiep]) VALUES
(1, 1, N'Nguyễn Minh Châu', '0909123456', N'Số 18 Ung Văn Khiêm, P. Mỹ Xuyên, TP. Long Xuyên, An Giang', '2026-11-06', N'Sáng (08:00 - 11:30)', 0.00, 'DA_GIAO_THANH_CONG', 1, 'THIEP-NC-2026-001', N'Kính chúc Ba Mẹ mùa Trung Thu 2026 luôn mạnh khỏe, an nhiên và tròn đầy hạnh phúc!', 'CO_PHONG_HOANG_GIA', 4),
(2, 2, N'Trần Quốc Bảo (Đối tác)', '0918777888', N'Tầng 12, Tòa nhà Vietcombank, Quận 1, TP. Hồ Chí Minh', '2026-11-09', N'Chiều (13:30 - 17:30)', 0.00, 'DA_GIAO_THANH_CONG', 1, 'THIEP-NC-2026-002', N'Chúc Quý Đối tác mùa Trăng Rằm viên mãn, hợp tác bền vững và gặt hái nhiều thành công!', 'CO_PHONG_HOANG_GIA', 2),
(3, 3, N'Nguyễn Minh Châu', '0909123456', N'Số 18 Ung Văn Khiêm, P. Mỹ Xuyên, TP. Long Xuyên, An Giang', '2026-11-12', N'Sáng (08:00 - 11:30)', 0.00, 'DA_GIAO_THANH_CONG', 0, NULL, NULL, 'CO_PHONG_HOANG_GIA', 0),
(4, 6, N'Nguyễn Minh Châu', '0909123456', N'Số 18 Ung Văn Khiêm, P. Mỹ Xuyên, TP. Long Xuyên, An Giang', '2026-11-17', N'Sáng (08:00 - 11:30)', 25000.00, 'DA_HUY', 0, NULL, NULL, 'CO_PHONG_HOANG_GIA', 0),
(5, 8, N'Nguyễn Thị Lan Anh', '0903667788', N'Số 45 Nguyễn Trãi, P. Mỹ Long, TP. Long Xuyên, An Giang', '2026-11-21', N'Chiều (13:30 - 17:30)', 0.00, 'DANG_VAN_CHUYEN', 1, 'THIEP-NC-2026-003', N'Gửi tặng chị yêu hộp bánh Brodard & Givral ít ngọt đón rằm tháng Tám thật vui vẻ!', 'CO_PHONG_HOANG_GIA', 1),
(6, 9, N'Nguyễn Minh Châu', '0909123456', N'Số 18 Ung Văn Khiêm, P. Mỹ Xuyên, TP. Long Xuyên, An Giang', '2026-11-22', N'Sáng (08:00 - 11:30)', 25000.00, 'CHO_DONG_GOI', 0, NULL, NULL, 'CO_PHONG_HOANG_GIA', 0),
(7, 10, N'Nguyễn Minh Châu', '0909123456', N'Số 18 Ung Văn Khiêm, P. Mỹ Xuyên, TP. Long Xuyên, An Giang', '2026-11-23', N'Sáng (08:00 - 11:30)', 25000.00, 'CHO_DONG_GOI', 0, NULL, NULL, 'CO_PHONG_HOANG_GIA', 0);
SET IDENTITY_INSERT [dbo].[DotGiaoHang_DaDiem] OFF;

INSERT INTO [dbo].[ChiTietDonHang] ([MaDH], [MaDotGH], [LoaiSanPham], [MaBanh], [MaCB], [MaHopTC], [TenSanPham_LuuCung], [CauHinhHop_JSON], [SoLuong], [DonGiaLucMua], [ThanhTien]) VALUES
(1, 1, 2, NULL, 1, NULL, N'Lễ Hộp Kinh Đô Trăng Vàng Vinh Hoa (4 Bánh + Trà)', N'[{"ViTriOTrongHop":1,"MaBanh":1,"TenBanh":"Kinh Đô Trăng Vàng Bào Ngư Thượng Hạng (2 Trứng)","DonGia":235000},{"ViTriOTrongHop":2,"MaBanh":2,"TenBanh":"Kinh Đô Gà Quay Sốt X.O Jambon (2 Trứng)","DonGia":168000},{"ViTriOTrongHop":3,"MaBanh":7,"TenBanh":"Kinh Đô Hạt Sen Huế Trứng Muối (2 Trứng)","DonGia":115000},{"ViTriOTrongHop":4,"MaBanh":9,"TenBanh":"Kinh Đô Lava Trứng Chảy Phô Mai Hoàng Kim","DonGia":135000}]', 2, 734000.00, 1468000.00),
(1, 1, 1, 25, NULL, NULL, N'Như Lan Vi Cá Thập Cẩm Gà Quay (2 Trứng - 250g)', NULL, 2, 245000.00, 490000.00),
(2, 2, 3, NULL, NULL, 1, N'Hộp Quà Tự Phối Tứ Đại Danh Hãng (4 Bánh + Trà)', N'[{"ViTriOTrongHop":1,"MaBanh":1,"TenBanh":"Kinh Đô Trăng Vàng Bào Ngư Thượng Hạng (2 Trứng)","DonGia":235000},{"ViTriOTrongHop":2,"MaBanh":15,"TenBanh":"Đồng Khánh Yến Sào Hạt Sen Thượng Hạng (2 Trứng)","DonGia":188000},{"ViTriOTrongHop":3,"MaBanh":33,"TenBanh":"Như Lan Tiramisu Cà Phê Phô Mai (1 Trứng)","DonGia":132000},{"ViTriOTrongHop":4,"MaBanh":58,"TenBanh":"Givral Phô Mai Trứng Chảy Kim Sa","DonGia":156000}]', 1, 856000.00, 856000.00),
(2, 2, 1, 45, NULL, NULL, N'Brodard Tiramisu Cà Phê Phô Mai Pháp', NULL, 2, 162000.00, 324000.00),
(3, 3, 2, NULL, 5, NULL, N'Lễ Hộp Givral Hoàng Gia 6 Vị An Khang (6 Bánh + Trà)', N'[{"ViTriOTrongHop":1,"MaBanh":49,"TenBanh":"Givral Thập Cẩm Gà Quay Vi Cá Yến Sào (2 Trứng)","DonGia":278000},{"ViTriOTrongHop":2,"MaBanh":50,"TenBanh":"Givral Bào Ngư Sốt X.O Thượng Hạng (2 Trứng)","DonGia":262000},{"ViTriOTrongHop":3,"MaBanh":52,"TenBanh":"Givral Thập Cẩm Ngũ Nhân Truyền Thống (2 Trứng)","DonGia":152000},{"ViTriOTrongHop":4,"MaBanh":54,"TenBanh":"Givral Hạt Sen Bắc Trứng Muối (2 Trứng)","DonGia":142000},{"ViTriOTrongHop":5,"MaBanh":57,"TenBanh":"Givral Việt Quất Nam Việt Quất (Berry Hiện Đại)","DonGia":145000},{"ViTriOTrongHop":6,"MaBanh":58,"TenBanh":"Givral Phô Mai Trứng Chảy Kim Sa","DonGia":156000}]', 2, 1247000.00, 2494000.00),
(3, 3, 1, 14, NULL, NULL, N'Đồng Khánh Gà Quay Vi Cá Thượng Hạng (2 Trứng)', NULL, 2, 165000.00, 330000.00),
(3, 3, 1, 24, NULL, NULL, N'Đồng Khánh Chay Hạt Sen Đường Ăn Kiêng Isomalt', NULL, 2, 92000.00, 184000.00),
(4, NULL, 1, 1, NULL, NULL, N'Kinh Đô Trăng Vàng Bào Ngư Thượng Hạng (2 Trứng)', NULL, 2, 235000.00, 470000.00),
(4, NULL, 1, 4, NULL, NULL, N'Kinh Đô Thập Cẩm Lạp Xưởng Ngũ Hạt (2 Trứng)', NULL, 4, 125000.00, 500000.00),
(4, NULL, 1, 13, NULL, NULL, N'Đồng Khánh Bông Lúa Vàng Bào Ngư Hải Sâm (2 Trứng)', NULL, 2, 192000.00, 384000.00),
(5, NULL, 2, NULL, 1, NULL, N'Lễ Hộp Kinh Đô Trăng Vàng Vinh Hoa (4 Bánh + Trà)', N'[{"ViTriOTrongHop":1,"MaBanh":1,"TenBanh":"Kinh Đô Trăng Vàng Bào Ngư Thượng Hạng (2 Trứng)","DonGia":235000},{"ViTriOTrongHop":2,"MaBanh":2,"TenBanh":"Kinh Đô Gà Quay Sốt X.O Jambon (2 Trứng)","DonGia":168000},{"ViTriOTrongHop":3,"MaBanh":7,"TenBanh":"Kinh Đô Hạt Sen Huế Trứng Muối (2 Trứng)","DonGia":115000},{"ViTriOTrongHop":4,"MaBanh":9,"TenBanh":"Kinh Đô Lava Trứng Chảy Phô Mai Hoàng Kim","DonGia":135000}]', 6, 734000.00, 4404000.00),
(5, NULL, 2, NULL, 3, NULL, N'Lễ Hộp Như Lan Phú Quý Đoàn Viên (4 Bánh + Trà)', N'[{"ViTriOTrongHop":1,"MaBanh":25,"TenBanh":"Như Lan Vi Cá Thập Cẩm Gà Quay (2 Trứng - 250g)","DonGia":245000},{"ViTriOTrongHop":2,"MaBanh":28,"TenBanh":"Như Lan Thập Cẩm Gà Quay Jambon (2 Trứng - 200g)","DonGia":142000},{"ViTriOTrongHop":3,"MaBanh":31,"TenBanh":"Như Lan Sen Hạnh Nhân Trứng Muối (2 Trứng)","DonGia":125000},{"ViTriOTrongHop":4,"MaBanh":34,"TenBanh":"Như Lan Matcha Trà Xanh Kim Sa Trứng Chảy","DonGia":138000}]', 4, 731000.00, 2924000.00),
(6, 4, 1, 37, NULL, NULL, N'Brodard Thập Cẩm Bào Ngư Sò Điệp Sốt X.O (2 Trứng)', NULL, 2, 285000.00, 570000.00),
(7, NULL, 1, 48, NULL, NULL, N'Brodard Hạt Sen Hạt Chia Đường Isomalt (Chay)', NULL, 2, 135000.00, 270000.00),
(7, NULL, 1, 60, NULL, NULL, N'Givral Đậu Xanh Hạt Sen Đường Isomalt (Ăn Kiêng)', NULL, 2, 128000.00, 256000.00),
(7, NULL, 1, 9, NULL, NULL, N'Kinh Đô Lava Trứng Chảy Phô Mai Hoàng Kim', NULL, 4, 135000.00, 540000.00),
(8, 5, 2, NULL, 4, NULL, N'Lễ Hộp Brodard Sài Gòn Tài Lộc (4 Bánh + Trà)', N'[{"ViTriOTrongHop":1,"MaBanh":37,"TenBanh":"Brodard Thập Cẩm Bào Ngư Sò Điệp Sốt X.O (2 Trứng)","DonGia":285000},{"ViTriOTrongHop":2,"MaBanh":40,"TenBanh":"Brodard Thập Cẩm Gà Quay Lá Chanh (2 Trứng)","DonGia":168000},{"ViTriOTrongHop":3,"MaBanh":45,"TenBanh":"Brodard Tiramisu Cà Phê Phô Mai Pháp","DonGia":162000},{"ViTriOTrongHop":4,"MaBanh":46,"TenBanh":"Brodard Socola Bỉ Hạt Phỉ Lava Tan Chảy","DonGia":168000}]', 1, 854000.00, 854000.00),
(8, 5, 1, 57, NULL, NULL, N'Givral Việt Quất Nam Việt Quất (Berry Hiện Đại)', NULL, 2, 145000.00, 290000.00),
(9, 6, 2, NULL, 2, NULL, N'Lễ Hộp Đồng Khánh Bông Lúa Vàng Sum Vầy (4 Bánh)', N'[{"ViTriOTrongHop":1,"MaBanh":13,"TenBanh":"Đồng Khánh Bông Lúa Vàng Bào Ngư Hải Sâm (2 Trứng)","DonGia":192000},{"ViTriOTrongHop":2,"MaBanh":16,"TenBanh":"Đồng Khánh Thập Cẩm Gà Quay Jambon (2 Trứng)","DonGia":118000},{"ViTriOTrongHop":3,"MaBanh":18,"TenBanh":"Đồng Khánh Đậu Xanh Sầu Riêng (2 Trứng)","DonGia":105000},{"ViTriOTrongHop":4,"MaBanh":21,"TenBanh":"Đồng Khánh Kim Sa Lava Trứng Muối Tan Chảy","DonGia":122000}]', 1, 572000.00, 572000.00),
(10, 7, 1, 1, NULL, NULL, N'Kinh Đô Trăng Vàng Bào Ngư Thượng Hạng (2 Trứng)', NULL, 2, 235000.00, 470000.00),
(10, 7, 1, 12, NULL, NULL, N'Kinh Đô Xanh Hạt Sen Hạnh Nhân Đường Isomalt', NULL, 2, 108000.00, 216000.00);

INSERT INTO [dbo].[ThiepChucMung3D] ([MaTokenQr], [MaDonHang], [MaDotGiao], [TenNguoiGui], [TenNguoiNhan], [LoiChuc], [MauGiaoDien], [SoLanMoThiep], [NgayTao]) VALUES
(N'THIEP-NC-2026-001', 1, 1, N'Nguyễn Minh Châu', N'Nguyễn Minh Châu', N'Kính chúc Ba Mẹ mùa Trung Thu 2026 luôn mạnh khỏe, an nhiên và tròn đầy hạnh phúc!', N'CO_PHONG_HOANG_GIA', 4, '2026-11-05 09:20:00'),
(N'THIEP-NC-2026-002', 2, 2, N'Nguyễn Minh Châu', N'Trần Quốc Bảo (Đối tác)', N'Chúc Quý Đối tác mùa Trăng Rằm viên mãn, hợp tác bền vững và gặt hái nhiều thành công!', N'CO_PHONG_HOANG_GIA', 2, '2026-11-08 14:10:00'),
(N'THIEP-NC-2026-003', 8, 5, N'Nguyễn Minh Châu', N'Nguyễn Thị Lan Anh', N'Gửi tặng chị yêu hộp bánh Brodard & Givral ít ngọt đón rằm tháng Tám thật vui vẻ!', N'CO_PHONG_HOANG_GIA', 1, '2026-11-18 11:05:00');

INSERT INTO [dbo].[NhatKyTruKhoDonHang] ([MaDH], [LoaiDoiTuong], [MaLo], [MaBanh], [MaVH], [SoLuongDaTru], [DaHoanKho], [NgayGhi]) VALUES
(1, 'VO_HOP', NULL, NULL, 2, 2, 0, '2026-11-05 09:20:00'),
(1, 'LO_BANH', 1, 1, NULL, 2, 0, '2026-11-05 09:20:00'),
(1, 'LO_BANH', 2, 2, NULL, 2, 0, '2026-11-05 09:20:00'),
(1, 'LO_BANH', 7, 7, NULL, 2, 0, '2026-11-05 09:20:00'),
(1, 'LO_BANH', 10, 9, NULL, 2, 0, '2026-11-05 09:20:00'),
(1, 'LO_BANH', 27, 25, NULL, 2, 0, '2026-11-05 09:20:00'),
(2, 'VO_HOP', NULL, NULL, 2, 1, 0, '2026-11-08 14:10:00'),
(2, 'LO_BANH', 1, 1, NULL, 1, 0, '2026-11-08 14:10:00'),
(2, 'LO_BANH', 16, 15, NULL, 1, 0, '2026-11-08 14:10:00'),
(2, 'LO_BANH', 35, 33, NULL, 1, 0, '2026-11-08 14:10:00'),
(2, 'LO_BANH', 60, 58, NULL, 1, 0, '2026-11-08 14:10:00'),
(2, 'LO_BANH', 47, 45, NULL, 2, 0, '2026-11-08 14:10:00'),
(3, 'VO_HOP', NULL, NULL, 3, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 51, 49, NULL, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 52, 50, NULL, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 54, 52, NULL, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 56, 54, NULL, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 59, 57, NULL, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 60, 58, NULL, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 15, 14, NULL, 2, 0, '2026-11-11 10:45:00'),
(3, 'LO_BANH', 26, 24, NULL, 2, 0, '2026-11-11 10:45:00'),
(4, 'LO_BANH', 1, 1, NULL, 2, 0, '2026-11-12 15:30:00'),
(4, 'LO_BANH', 4, 4, NULL, 4, 0, '2026-11-12 15:30:00'),
(4, 'LO_BANH', 14, 13, NULL, 2, 0, '2026-11-12 15:30:00'),
(5, 'VO_HOP', NULL, NULL, 2, 6, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 1, 1, NULL, 6, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 2, 2, NULL, 6, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 7, 7, NULL, 6, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 10, 9, NULL, 6, 0, '2026-11-14 09:50:00'),
(5, 'VO_HOP', NULL, NULL, 2, 4, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 27, 25, NULL, 4, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 30, 28, NULL, 4, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 33, 31, NULL, 4, 0, '2026-11-14 09:50:00'),
(5, 'LO_BANH', 36, 34, NULL, 4, 0, '2026-11-14 09:50:00'),
(6, 'LO_BANH', 39, 37, NULL, 2, 1, '2026-11-15 08:50:00'),
(7, 'LO_BANH', 50, 48, NULL, 2, 0, '2026-11-16 17:15:00'),
(7, 'LO_BANH', 62, 60, NULL, 2, 0, '2026-11-16 17:15:00'),
(7, 'LO_BANH', 10, 9, NULL, 4, 0, '2026-11-16 17:15:00'),
(8, 'VO_HOP', NULL, NULL, 2, 1, 0, '2026-11-18 11:05:00'),
(8, 'LO_BANH', 39, 37, NULL, 1, 0, '2026-11-18 11:05:00'),
(8, 'LO_BANH', 42, 40, NULL, 1, 0, '2026-11-18 11:05:00'),
(8, 'LO_BANH', 47, 45, NULL, 1, 0, '2026-11-18 11:05:00'),
(8, 'LO_BANH', 48, 46, NULL, 1, 0, '2026-11-18 11:05:00'),
(8, 'LO_BANH', 59, 57, NULL, 2, 0, '2026-11-18 11:05:00'),
(9, 'VO_HOP', NULL, NULL, 1, 1, 0, '2026-11-19 16:40:00'),
(9, 'LO_BANH', 14, 13, NULL, 1, 0, '2026-11-19 16:40:00'),
(9, 'LO_BANH', 17, 16, NULL, 1, 0, '2026-11-19 16:40:00'),
(9, 'LO_BANH', 19, 18, NULL, 1, 0, '2026-11-19 16:40:00'),
(9, 'LO_BANH', 23, 21, NULL, 1, 0, '2026-11-19 16:40:00'),
(10, 'LO_BANH', 1, 1, NULL, 2, 0, '2026-11-20 09:30:00'),
(10, 'LO_BANH', 13, 12, NULL, 2, 0, '2026-11-20 09:30:00');

-- =============================================================================
-- PHẦN 10: 3 PHIẾU XUẤT KHO, 1 PHIẾU KIỂM KHO (MaNdKiem=1), SỔ QUỸ (17) & 4 PHIẾU VỎ HỘP
-- =============================================================================
SET IDENTITY_INSERT [dbo].[PhieuXuatKho] ON;
INSERT INTO [dbo].[PhieuXuatKho] ([MaPx], [SoPhieuXuat], [LoaiXuat], [DoiTuongLienQuan], [NgayXuat], [TongGiaTriVon], [TrangThai], [GhiChu], [NguoiLap], [MaNdLap]) VALUES
(1, N'PX-20261113-001', 'TIEU_DUNG_NOI_BO', N'Quầy trưng bày Nguyệt Cát Long Xuyên', '2026-11-13 10:00:00', 273600.00, 'HOAN_THANH', N'Xuất mẫu bánh dùng thử cho khách tham quan tại showroom', N'Lê Minh Đức (Chủ cửa hàng)', 1),
(2, N'PX-20261116-002', 'XUAT_HUY_HONG', N'Kho trung tâm TP. Long Xuyên', '2026-11-16 14:30:00', 249150.00, 'HOAN_THANH', N'Xuất hủy bánh hư hỏng bao bì theo biên bản kiểm nhận', N'Lê Minh Đức (Chủ cửa hàng)', 1),
(3, N'PX-20261120-003', 'CHINH_LECH_KIEM_KHO', N'Kiểm kê kho định kỳ KK-20261120-001', '2026-11-20 11:15:00', 204760.00, 'HOAN_THANH', N'Phiếu xuất tự động sinh khi cân bằng phiếu kiểm kho KK-20261120-001', N'Lê Minh Đức (Chủ cửa hàng)', 1);
SET IDENTITY_INSERT [dbo].[PhieuXuatKho] OFF;

INSERT INTO [dbo].[ChiTietPhieuXuat] ([MaPx], [MaSp], [MaLo], [SoLuongXuat], [GiaVonLucXuat], [ThanhTienVon], [GhiChuDong]) VALUES
(1, 6, 6, 2, 51840.00, 103680.00, N'Cắt mẫu thử vị Đậu Xanh cho khách tại quầy Long Xuyên'),
(1, 10, 11, 2, 84960.00, 169920.00, N'Mời khách doanh nghiệp thử vị Trà Xanh Matcha'),
(2, 18, 19, 2, 77700.00, 155400.00, N'Vỡ nứt vỏ bánh trong quá trình bốc xếp từ chành xe'),
(2, 31, 33, 1, 93750.00, 93750.00, N'Rách bao bì hút chân không khi kiểm nhận kho'),
(3, 5, 5, 2, 70560.00, 141120.00, N'Cân bằng thiếu hụt từ phiếu kiểm kho KK-20261120-001: Thiếu 2 bánh khi kiểm đếm kệ A2 (đã đối chiếu camera, xuất cân bằng kho)'),
(3, 17, 18, 1, 63640.00, 63640.00, N'Cân bằng thiếu hụt từ phiếu kiểm kho KK-20261120-001: Thiếu 1 bánh do rơi vỡ chưa lập phiếu xuất');

SET IDENTITY_INSERT [dbo].[PhieuKiemKho] ON;
INSERT INTO [dbo].[PhieuKiemKho] ([MaPkk], [SoPhieuKiem], [NgayKiem], [NguoiKiem], [TrangThai], [TongLechTang], [TongLechGiam], [TongGiaTriLech], [GhiChu], [MaPxLienKet], [MaNdKiem]) VALUES
(1, N'KK-20261120-001', '2026-11-20 11:00:00', N'Lê Minh Đức (Chủ cửa hàng)', 'DA_CAN_BANG', 0, 3, -204760.00, N'Kiểm kê định kỳ 6 mã bánh chủ lực sau chốt đơn sáng 20/11, tự động sinh phiếu xuất PX-20261120-003', 3, 1);
SET IDENTITY_INSERT [dbo].[PhieuKiemKho] OFF;

INSERT INTO [dbo].[ChiTietKiemKho] ([MaPkk], [MaSp], [TonHeThong], [TonThucTe], [SoLuongLech], [GiaTriLech], [LyDoLech]) VALUES
(1, 1, 42, 42, 0, 0.00, N'Khớp tồn hệ thống'),
(1, 5, 55, 53, -2, -141120.00, N'Thiếu 2 bánh khi kiểm đếm kệ A2 (đã đối chiếu camera, xuất cân bằng kho)'),
(1, 13, 47, 47, 0, 0.00, N'Khớp tồn hệ thống'),
(1, 17, 55, 54, -1, -63640.00, N'Thiếu 1 bánh do rơi vỡ chưa lập phiếu xuất'),
(1, 25, 44, 44, 0, 0.00, N'Khớp tồn hệ thống'),
(1, 49, 28, 28, 0, 0.00, N'Khớp tồn hệ thống');

SET IDENTITY_INSERT [dbo].[PhieuThuChi] ON;
INSERT INTO [dbo].[PhieuThuChi] ([MaPhieu], [MaPhieuHienThi], [LoaiPhieu], [HangMuc], [SoTien], [PhuongThuc], [MaDonHangLienKet], [NguoiNopNhan], [GhiChu], [NgayLap], [MaPhieuNhapLienKet]) VALUES
(1, N'PTH-261101-000', N'THU', N'Góp vốn lưu động đầu mùa vụ', 250000000.00, N'CHUYEN_KHOAN', NULL, N'Lê Minh Đức (Chủ cửa hàng)', N'Bổ sung vốn lưu động nhập hàng mùa Trung Thu 2026', '2026-11-01 07:30:00', NULL),
(2, N'PCH-PVH-261101-001', N'CHI', N'Chi nhập kho vỏ hộp quà tặng', 6344000.00, N'CHUYEN_KHOAN', NULL, N'Đại Lý Điều Tiết Bánh Miền Tây (An Giang)', N'Thanh toán phiếu nhập vỏ hộp PVH-261101-001', '2026-11-01 08:00:00', NULL),
(3, N'PCH-PVH-261101-002', N'CHI', N'Chi nhập kho vỏ hộp quà tặng', 7360000.00, N'CHUYEN_KHOAN', NULL, N'Đại Lý Điều Tiết Bánh Miền Tây (An Giang)', N'Thanh toán phiếu nhập vỏ hộp PVH-261101-002', '2026-11-01 08:05:00', NULL),
(4, N'PCH-PVH-261101-003', N'CHI', N'Chi nhập kho vỏ hộp quà tặng', 8400000.00, N'CHUYEN_KHOAN', NULL, N'Đại Lý Điều Tiết Bánh Miền Tây (An Giang)', N'Thanh toán phiếu nhập vỏ hộp PVH-261101-003', '2026-11-01 08:10:00', NULL),
(5, N'PCH-PN-20261101-001', N'CHI', N'Thanh toán nhập hàng nhà cung cấp', 50922000.00, N'CHUYEN_KHOAN', NULL, N'Nhà phân phối Kinh Đô Chính Hãng', N'Thanh toán phiếu nhập PN-20261101-001 (DA_THANH_TOAN)', '2026-11-01 08:30:00', 1),
(6, N'PCH-PN-20261102-002', N'CHI', N'Thanh toán nhập hàng nhà cung cấp', 43571200.00, N'CHUYEN_KHOAN', NULL, N'Lò Bánh Đồng Khánh Chính Gốc', N'Thanh toán phiếu nhập PN-20261102-002 (DA_THANH_TOAN)', '2026-11-02 09:15:00', 2),
(7, N'PCH-PN-20261104-003', N'CHI', N'Thanh toán nhập hàng nhà cung cấp', 53482500.00, N'CHUYEN_KHOAN', NULL, N'Bánh Trung Thu Như Lan TP.HCM', N'Thanh toán phiếu nhập PN-20261104-003 (DA_THANH_TOAN)', '2026-11-04 08:45:00', 3),
(8, N'PCH-PN-20261106-004', N'CHI', N'Thanh toán nhập hàng nhà cung cấp', 33728040.00, N'CHUYEN_KHOAN', NULL, N'Brodard Bakery Saigon', N'Thanh toán phiếu nhập PN-20261106-004 (THANH_TOAN_MOT_PHAN)', '2026-11-06 10:00:00', 4),
(9, N'PCH-PN-20261112-006', N'CHI', N'Thanh toán nhập hàng nhà cung cấp', 12776400.00, N'CHUYEN_KHOAN', NULL, N'Đại Lý Điều Tiết Bánh Miền Tây (An Giang)', N'Thanh toán phiếu nhập PN-20261112-006 (DA_THANH_TOAN)', '2026-11-12 14:20:00', 6),
(10, N'PCH-261115-001', N'CHI', N'Chi phí vận chuyển chành xe & bao bì', 1850000.00, N'TIEN_MAT', NULL, N'Chành xe Tô Châu Long Xuyên', N'Cước vận chuyển hàng lạnh TP.HCM - Long Xuyên và túi giấy biếu tặng', '2026-11-15 16:00:00', NULL),
(11, N'PTH-261105-001', N'THU', N'Thu tiền bán hàng', 1860100.00, N'MOMO_QR', 1, N'Nguyễn Minh Châu', N'Thu tiền đơn hàng #TT2026-1001', '2026-11-05 09:20:00', NULL),
(12, N'PTH-261108-002', N'THU', N'Thu tiền bán hàng', 1003000.00, N'MOMO_QR', 2, N'Nguyễn Minh Châu', N'Thu tiền đơn hàng #TT2026-1002', '2026-11-08 14:10:00', NULL),
(13, N'PTH-261111-003', N'THU', N'Thu tiền bán hàng', 2857600.00, N'COD', 3, N'Nguyễn Minh Châu', N'Thu tiền đơn hàng #TT2026-1003', '2026-11-11 10:45:00', NULL),
(14, N'PTH-261112-004', N'THU', N'Thu tiền bán lẻ tại quầy POS', 1354000.00, N'TIEN_MAT', 4, N'Lê Hoàng Nam (Khách lẻ POS)', N'Thu tiền đơn hàng #POS26-1004', '2026-11-12 15:30:00', NULL),
(15, N'PTH-261114-005', N'THU', N'Thu tiền bán lẻ tại quầy POS', 6741760.00, N'MOMO_QR', 5, N'Công ty TNHH TM-DV Mekong An Giang', N'Thu tiền đơn hàng #POS26-1005', '2026-11-14 09:50:00', NULL),
(16, N'PTH-261116-007', N'THU', N'Thu tiền bán lẻ tại quầy POS', 1066000.00, N'TIEN_MAT', 7, N'Phạm Thị Thanh Trúc (Khách lẻ POS)', N'Thu tiền đơn hàng #POS26-1007', '2026-11-16 17:15:00', NULL),
(17, N'PTH-261118-008', N'THU', N'Thu tiền bán hàng', 1029600.00, N'MOMO_QR', 8, N'Nguyễn Minh Châu', N'Thu tiền đơn hàng #TT2026-1008', '2026-11-18 11:05:00', NULL);
SET IDENTITY_INSERT [dbo].[PhieuThuChi] OFF;

SET IDENTITY_INSERT [dbo].[PhieuKhoVoHop] ON;
INSERT INTO [dbo].[PhieuKhoVoHop] ([MaPvh], [SoPhieu], [MaVH], [LoaiGiaoDich], [SoLuong], [DonGiaNhap], [ThanhTien], [MaNcc], [MaPhieuChi], [MaNdLap], [NguoiLap], [GhiChu], [NgayGiaoDich]) VALUES
(1, N'PVH-261101-001', 1, 'NHAP_VO_HOP', 122, 52000.00, 6344000.00, 6, 2, 1, N'Lê Minh Đức (Chủ cửa hàng)', N'Nhập lô vỏ hộp Thanh Nguyệt 4 bánh đầu mùa', '2026-11-01 08:00:00'),
(2, N'PVH-261101-002', 2, 'NHAP_VO_HOP', 80, 92000.00, 7360000.00, 6, 3, 1, N'Lê Minh Đức (Chủ cửa hàng)', N'Nhập lô vỏ hộp Hoàng Kim 4 bánh kèm trà Ô Long', '2026-11-01 08:05:00'),
(3, N'PVH-261101-003', 3, 'NHAP_VO_HOP', 60, 140000.00, 8400000.00, 6, 4, 1, N'Lê Minh Đức (Chủ cửa hàng)', N'Nhập lô vỏ hộp Sơn Mài Hoàng Gia 6 bánh cao cấp', '2026-11-01 08:10:00'),
(4, N'PVH-261115-004', 1, 'XUAT_HUY_VO_HOP', 2, 52000.00, 104000.00, NULL, NULL, 1, N'Lê Minh Đức (Chủ cửa hàng)', N'Xuất hủy 2 vỏ hộp móp góc trong quá trình sắp xếp kho', '2026-11-15 15:00:00');
SET IDENTITY_INSERT [dbo].[PhieuKhoVoHop] OFF;

-- =============================================================================
-- PHẦN 11: ĐỒNG BỘ TỒN BÁNH + TỒN VỎ HỘP & KIỂM TOÁN 37/37 BẢNG LIÊN THÔNG
-- =============================================================================
UPDATE sp
SET sp.TongTonKho = ISNULL(t.TongConLai, 0)
FROM [dbo].[SanPhamBanh] sp
OUTER APPLY (
    SELECT SUM(lo.SLConLai) AS TongConLai
    FROM [dbo].[LoSanXuatBanh] lo
    WHERE lo.MaBanh = sp.MaBanh
      AND lo.TrangThaiLo NOT IN ('HET_HAN', 'DA_HUY')
) t;

UPDATE vh
SET vh.SLTonKhoVo = ISNULL(p.TongNhapXuat, 0) - ISNULL(k.TongBan, 0)
FROM [dbo].[MauVoHop] vh
OUTER APPLY (
    SELECT SUM(CASE WHEN LoaiGiaoDich = 'NHAP_VO_HOP' THEN SoLuong ELSE -SoLuong END) AS TongNhapXuat
    FROM [dbo].[PhieuKhoVoHop]
    WHERE MaVH = vh.MaVH
) p
OUTER APPLY (
    SELECT SUM(SoLuongDaTru) AS TongBan
    FROM [dbo].[NhatKyTruKhoDonHang]
    WHERE LoaiDoiTuong = 'VO_HOP' AND MaVH = vh.MaVH AND DaHoanKho = 0
) k;

COMMIT TRANSACTION;
GO

-- 1. KIỂM TRA ĐỘ LIÊN THÔNG ERD (BẮT BUỘC: SoBangCoLap = 0 / 37 BẢNG)
SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS TongSoBang,
    (SELECT COUNT(*) FROM sys.foreign_keys) AS TongSoKhoaNgoai,
    (SELECT COUNT(*) FROM sys.tables t
     WHERE t.is_ms_shipped = 0
       AND NOT EXISTS (
           SELECT 1 FROM sys.foreign_keys fk
           WHERE fk.parent_object_id = t.object_id OR fk.referenced_object_id = t.object_id
       )) AS SoBangCoLap;

	   SELECT name AS BangCoLap
FROM sys.tables t
WHERE t.is_ms_shipped = 0
  AND NOT EXISTS (
      SELECT 1 FROM sys.foreign_keys fk
      WHERE fk.parent_object_id = t.object_id OR fk.referenced_object_id = t.object_id
  );

-- 2. KIỂM TRA TỒN KHO BÁNH, TỒN KHO VỎ HỘP & SỔ QUỸ ZERO-DIFF
SELECT
    (SELECT SUM(TongTonKho) FROM dbo.SanPhamBanh) AS TongTonBanh,
    (SELECT SUM(SLTonKhoVo) FROM dbo.MauVoHop) AS TongTonVoHop,
    (SELECT COUNT(*) FROM dbo.PhieuKhoVoHop) AS SoPhieuKhoVoHop,
    (SELECT SUM(CASE WHEN LoaiPhieu = N'THU' THEN SoTien ELSE 0 END) FROM dbo.PhieuThuChi) AS TongThuSoQuy,
    (SELECT SUM(CASE WHEN LoaiPhieu = N'CHI' THEN SoTien ELSE 0 END) FROM dbo.PhieuThuChi) AS TongChiSoQuy,
    (SELECT SUM(CASE WHEN LoaiPhieu = N'THU' THEN SoTien ELSE -SoTien END) FROM dbo.PhieuThuChi) AS TonQuyThucTe,
    (SELECT SUM(TongPhaiTra - DaThanhToan) FROM dbo.PhieuNhapKho WHERE TrangThai <> 'DA_HUY') AS CongNoNCC;
GO


