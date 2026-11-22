using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;

namespace WebBanhTrungThu.Data
{
    /// <summary>
    /// Lớp chuyên trách khởi tạo cấu trúc bảng mở rộng (PhieuThuChi, ThiepChucMung3D, CauHinhHeThong, PhieuKhoVoHop),
    /// dọn dẹp 2 bảng thừa (NguyenLieu, CongThucBanhNl), gắn đầy đủ khóa ngoại chuẩn 37/37 bảng SQL-First,
    /// di trú dữ liệu cấu hình mở rộng Đại lý Nguyệt Cát ERP, chuẩn hóa hạn mã khuyến mãi 2027, hạn sử dụng lô bánh FEFO và ảnh khay hộp 6 ô.
    /// </summary>
    public static class KhoiTaoCsdl
    {
        public static async Task KhoiTaoVaChuanHoaDuLieuAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                await TaoCacBangMoRongVaDonBangThuaAsync(db);
                await SeedNhaCungCapMacDinhAsync(db);
                await SeedThuongHieuVaChuanHoaHangHoaAsync(db);
                await DiTruDuLieuCauHinhMoRongAsync(app, db);
                await ChuanHoaChiMucTokenThiepQrAsync(db);
                await ChuanHoaMaKhuyenMaiVaLoBanhFefoAsync(db);
                await CapNhatAnhKhayHop6OSonMaiAsync(db);
                await CapNhatDuongDanAnhBanhHatSenHueAsync(db);
                await ChuanHoaTenMocChietKhauAsync(db);
                await ChuanHoaEmailAdminVaDongBoDonHuyAsync(db);
                await SeedHoSoNhanVienVaDongBoVaiTroAsync(db);
                await ChuanHoaMaCodeLandingPageAsync(db);
                await db.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID('dbo.SanPhamBanh', 'U') IS NOT NULL
                    BEGIN
                        UPDATE dbo.SanPhamBanh
                        SET MucDoNgot = 'ISOMALT_TIEUDUONG'
                        WHERE (MucDoNgot = 'DUONG_ISOMALT' OR TenBanh LIKE N'%Isomalt%' OR TenBanh LIKE N'%Ăn Kiêng%')
                          AND MucDoNgot <> 'ISOMALT_TIEUDUONG';
                    END;
                ");
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Bỏ qua lỗi khởi tạo/chuẩn hóa CSDL khi khởi động.");
            }
        }

        private static async Task TaoCacBangMoRongVaDonBangThuaAsync(CSDLBanhTrungThuContext db)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.CongThucBanh_NL', 'U') IS NOT NULL DROP TABLE dbo.CongThucBanh_NL;
                IF OBJECT_ID('dbo.CongThucBanhNl', 'U') IS NOT NULL DROP TABLE dbo.CongThucBanhNl;
                IF OBJECT_ID('dbo.NguyenLieu', 'U') IS NOT NULL DROP TABLE dbo.NguyenLieu;

                IF OBJECT_ID('dbo.PhieuThuChi', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.PhieuThuChi (
                        MaPhieu INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaPhieuHienThi NVARCHAR(50) NOT NULL,
                        LoaiPhieu NVARCHAR(20) NOT NULL,
                        HangMuc NVARCHAR(100) NOT NULL,
                        SoTien DECIMAL(18,2) NOT NULL,
                        PhuongThuc NVARCHAR(30) NOT NULL,
                        MaDonHangLienKet INT NULL,
                        NguoiNopNhan NVARCHAR(150) NOT NULL,
                        GhiChu NVARCHAR(500) NULL,
                        NgayLap DATETIME NOT NULL DEFAULT(GETDATE())
                    );
                END;

                IF OBJECT_ID('dbo.ThiepChucMung3D', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ThiepChucMung3D (
                        MaThiep INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaTokenQr NVARCHAR(60) NOT NULL UNIQUE,
                        MaDonHang INT NOT NULL,
                        MaDotGiao INT NULL,
                        TenNguoiGui NVARCHAR(150) NOT NULL,
                        TenNguoiNhan NVARCHAR(150) NOT NULL,
                        LoiChuc NVARCHAR(1000) NOT NULL,
                        MauGiaoDien NVARCHAR(100) NOT NULL,
                        SoLanMoThiep INT NOT NULL DEFAULT(0),
                        NgayTao DATETIME NOT NULL DEFAULT(GETDATE())
                    );
                END;

                IF OBJECT_ID('dbo.CauHinhHeThong', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.CauHinhHeThong (
                        KhoaCauHinh NVARCHAR(100) NOT NULL PRIMARY KEY,
                        GiaTriJson NVARCHAR(MAX) NOT NULL,
                        MoTa NVARCHAR(255) NULL,
                        NgayCapNhat DATETIME NOT NULL DEFAULT(GETDATE())
                    );
                END;

                IF OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('dbo.DonHang', 'TienGiamHangVip') IS NULL
                        ALTER TABLE dbo.DonHang ADD TienGiamHangVip DECIMAL(18,0) NOT NULL DEFAULT 0;
                    IF COL_LENGTH('dbo.DonHang', 'PhanTramGiamVip') IS NULL
                        ALTER TABLE dbo.DonHang ADD PhanTramGiamVip INT NOT NULL DEFAULT 0;
                    IF COL_LENGTH('dbo.DonHang', 'SoDiemDaDung') IS NULL
                        ALTER TABLE dbo.DonHang ADD SoDiemDaDung INT NOT NULL DEFAULT 0;
                    IF COL_LENGTH('dbo.DonHang', 'TienGiamDiemVip') IS NULL
                        ALTER TABLE dbo.DonHang ADD TienGiamDiemVip DECIMAL(18,0) NOT NULL DEFAULT 0;
                    IF COL_LENGTH('dbo.DonHang', 'HanThanhToanQr') IS NULL
                        ALTER TABLE dbo.DonHang ADD HanThanhToanQr DATETIME NULL;
                    IF COL_LENGTH('dbo.DonHang', 'DaCongDiemThuong') IS NULL
                        ALTER TABLE dbo.DonHang ADD DaCongDiemThuong BIT NOT NULL DEFAULT 0;
                    IF COL_LENGTH('dbo.DonHang', 'DaHoanDiemKhiHuy') IS NULL
                        ALTER TABLE dbo.DonHang ADD DaHoanDiemKhiHuy BIT NOT NULL DEFAULT 0;
                    IF COL_LENGTH('dbo.DonHang', 'LyDoHuy') IS NULL
                        ALTER TABLE dbo.DonHang ADD LyDoHuy NVARCHAR(500) NULL;
                END;

                IF OBJECT_ID('dbo.NhaCungCap', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.NhaCungCap (
                        MaNcc INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaCodeNcc NVARCHAR(30) NOT NULL UNIQUE,
                        TenNcc NVARCHAR(200) NOT NULL,
                        LoaiNcc NVARCHAR(50) NOT NULL DEFAULT(N'HANG_CHINH_HANG'),
                        ThuongHieuCungCap NVARCHAR(100) NOT NULL DEFAULT(N'Đa thương hiệu'),
                        PhanTramChietKhauMacDinh DECIMAL(5,2) NOT NULL DEFAULT(0),
                        SoDienThoai NVARCHAR(20) NULL,
                        Email NVARCHAR(150) NULL,
                        DiaChi NVARCHAR(300) NULL,
                        GhiChu NVARCHAR(500) NULL,
                        DangHoatDong BIT NOT NULL DEFAULT(1),
                        NgayTao DATETIME NOT NULL DEFAULT(GETDATE())
                    );
                END;

                IF OBJECT_ID('dbo.PhieuNhapKho', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.PhieuNhapKho (
                        MaPn INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        SoPhieuNhap NVARCHAR(50) NOT NULL UNIQUE,
                        MaNcc INT NULL,
                        NhaCungCap NVARCHAR(200) NOT NULL,
                        NgayNhap DATETIME NOT NULL DEFAULT(GETDATE()),
                        TongTienHang DECIMAL(18,2) NOT NULL DEFAULT(0),
                        GiamGia DECIMAL(18,2) NOT NULL DEFAULT(0),
                        TongPhaiTra DECIMAL(18,2) NOT NULL DEFAULT(0),
                        DaThanhToan DECIMAL(18,2) NOT NULL DEFAULT(0),
                        TrangThai VARCHAR(30) NOT NULL DEFAULT('PHIEU_TAM'),
                        TrangThaiThanhToan VARCHAR(30) NOT NULL DEFAULT('CHUA_THANH_TOAN'),
                        GhiChu NVARCHAR(500) NULL,
                        NguoiLap NVARCHAR(150) NOT NULL,
                        MaNdLap INT NULL,
                        CONSTRAINT FK_PhieuNhapKho_NhaCungCap FOREIGN KEY (MaNcc) REFERENCES dbo.NhaCungCap(MaNcc),
                        CONSTRAINT FK_PhieuNhapKho_NguoiDung FOREIGN KEY (MaNdLap) REFERENCES dbo.NguoiDung(MaND)
                    );
                END;

                IF OBJECT_ID('dbo.PhieuNhapKho', 'U') IS NOT NULL AND COL_LENGTH('dbo.PhieuNhapKho', 'MaNcc') IS NULL
                BEGIN
                    ALTER TABLE dbo.PhieuNhapKho ADD MaNcc INT NULL;
                    ALTER TABLE dbo.PhieuNhapKho ADD CONSTRAINT FK_PhieuNhapKho_NhaCungCap FOREIGN KEY (MaNcc) REFERENCES dbo.NhaCungCap(MaNcc);
                END;

                IF OBJECT_ID('dbo.ChiTietPhieuNhap', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ChiTietPhieuNhap (
                        MaCtpn INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaPn INT NOT NULL,
                        MaSp INT NOT NULL,
                        MaLo INT NULL,
                        SoLuong INT NOT NULL,
                        PhanTramChietKhau DECIMAL(5,2) NOT NULL DEFAULT(0),
                        GiaNiemYet DECIMAL(18,0) NOT NULL DEFAULT(0),
                        DonGiaNhap DECIMAL(18,2) NOT NULL,
                        ThanhTien DECIMAL(18,2) NOT NULL,
                        NgaySanXuat DATE NOT NULL,
                        HanSuDung DATE NOT NULL,
                        CONSTRAINT FK_CTPN_PhieuNhap FOREIGN KEY (MaPn) REFERENCES dbo.PhieuNhapKho(MaPn) ON DELETE CASCADE,
                        CONSTRAINT FK_CTPN_SanPham FOREIGN KEY (MaSp) REFERENCES dbo.SanPhamBanh(MaBanh),
                        CONSTRAINT FK_CTPN_LoSanXuat FOREIGN KEY (MaLo) REFERENCES dbo.LoSanXuatBanh(MaLo)
                    );
                END;

                IF OBJECT_ID('dbo.ChiTietPhieuNhap', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('dbo.ChiTietPhieuNhap', 'PhanTramChietKhau') IS NULL
                        ALTER TABLE dbo.ChiTietPhieuNhap ADD PhanTramChietKhau DECIMAL(5,2) NOT NULL DEFAULT(0);
                    IF COL_LENGTH('dbo.ChiTietPhieuNhap', 'GiaNiemYet') IS NULL
                        ALTER TABLE dbo.ChiTietPhieuNhap ADD GiaNiemYet DECIMAL(18,0) NOT NULL DEFAULT(0);
                END;

                IF OBJECT_ID('dbo.PhieuXuatKho', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.PhieuXuatKho (
                        MaPx INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        SoPhieuXuat NVARCHAR(50) NOT NULL UNIQUE,
                        LoaiXuat VARCHAR(40) NOT NULL,
                        DoiTuongLienQuan NVARCHAR(250) NOT NULL,
                        NgayXuat DATETIME NOT NULL DEFAULT(GETDATE()),
                        TongGiaTriVon DECIMAL(18,2) NOT NULL DEFAULT(0),
                        TrangThai VARCHAR(30) NOT NULL DEFAULT('HOAN_THANH'),
                        GhiChu NVARCHAR(500) NULL,
                        NguoiLap NVARCHAR(150) NOT NULL,
                        MaNdLap INT NULL,
                        CONSTRAINT FK_PhieuXuatKho_NguoiDung FOREIGN KEY (MaNdLap) REFERENCES dbo.NguoiDung(MaND)
                    );
                END;

                IF OBJECT_ID('dbo.ChiTietPhieuXuat', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ChiTietPhieuXuat (
                        MaCtpx INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaPx INT NOT NULL,
                        MaSp INT NOT NULL,
                        MaLo INT NULL,
                        SoLuongXuat INT NOT NULL,
                        GiaVonLucXuat DECIMAL(18,2) NOT NULL,
                        ThanhTienVon DECIMAL(18,2) NOT NULL,
                        GhiChuDong NVARCHAR(300) NULL,
                        CONSTRAINT FK_CTPX_PhieuXuat FOREIGN KEY (MaPx) REFERENCES dbo.PhieuXuatKho(MaPx) ON DELETE CASCADE,
                        CONSTRAINT FK_CTPX_SanPham FOREIGN KEY (MaSp) REFERENCES dbo.SanPhamBanh(MaBanh),
                        CONSTRAINT FK_CTPX_LoSanXuat FOREIGN KEY (MaLo) REFERENCES dbo.LoSanXuatBanh(MaLo)
                    );
                END;

                IF OBJECT_ID('dbo.PhieuKiemKho', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.PhieuKiemKho (
                        MaPkk INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        SoPhieuKiem NVARCHAR(50) NOT NULL UNIQUE,
                        NgayKiem DATETIME NOT NULL DEFAULT(GETDATE()),
                        NguoiKiem NVARCHAR(150) NOT NULL,
                        TrangThai VARCHAR(30) NOT NULL DEFAULT('PHIEU_TAM'),
                        TongLechTang INT NOT NULL DEFAULT(0),
                        TongLechGiam INT NOT NULL DEFAULT(0),
                        TongGiaTriLech DECIMAL(18,2) NOT NULL DEFAULT(0),
                        GhiChu NVARCHAR(500) NULL,
                        MaPxLienKet INT NULL,
                        CONSTRAINT FK_PhieuKiemKho_PhieuXuat FOREIGN KEY (MaPxLienKet) REFERENCES dbo.PhieuXuatKho(MaPx)
                    );
                END;

                IF OBJECT_ID('dbo.ChiTietKiemKho', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ChiTietKiemKho (
                        MaCtkk INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaPkk INT NOT NULL,
                        MaSp INT NOT NULL,
                        TonHeThong INT NOT NULL,
                        TonThucTe INT NOT NULL,
                        SoLuongLech INT NOT NULL,
                        GiaTriLech DECIMAL(18,2) NOT NULL,
                        LyDoLech NVARCHAR(300) NULL,
                        CONSTRAINT FK_CTKK_PhieuKiemKho FOREIGN KEY (MaPkk) REFERENCES dbo.PhieuKiemKho(MaPkk) ON DELETE CASCADE,
                        CONSTRAINT FK_CTKK_SanPham FOREIGN KEY (MaSp) REFERENCES dbo.SanPhamBanh(MaBanh)
                    );
                END;

                IF OBJECT_ID('dbo.PhieuThuChi', 'U') IS NOT NULL AND COL_LENGTH('dbo.PhieuThuChi', 'MaPhieuNhapLienKet') IS NULL
                BEGIN
                    ALTER TABLE dbo.PhieuThuChi ADD MaPhieuNhapLienKet INT NULL;
                    ALTER TABLE dbo.PhieuThuChi ADD CONSTRAINT FK_PhieuThuChi_PhieuNhapKho FOREIGN KEY (MaPhieuNhapLienKet) REFERENCES dbo.PhieuNhapKho(MaPn);
                END;

                IF OBJECT_ID('dbo.HoSoNhanVien', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.HoSoNhanVien (
                        MaNv INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaNd INT NOT NULL UNIQUE,
                        MaCodeNv NVARCHAR(20) NOT NULL UNIQUE,
                        ChucVu NVARCHAR(100) NOT NULL,
                        ChiNhanh NVARCHAR(150) NOT NULL DEFAULT(N'TP. Long Xuyên'),
                        CaLamViec NVARCHAR(100) NOT NULL DEFAULT(N'Toàn thời gian'),
                        NgayVaoLam DATE NOT NULL DEFAULT(CAST(GETDATE() AS DATE)),
                        TrangThaiLamViec NVARCHAR(30) NOT NULL DEFAULT('DANG_LAM_VIEC'),
                        GhiChu NVARCHAR(500) NULL,
                        CONSTRAINT FK_HoSoNhanVien_NguoiDung FOREIGN KEY (MaNd) REFERENCES dbo.NguoiDung(MaND)
                    );
                END;

                IF OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL AND COL_LENGTH('dbo.DonHang', 'MaNdLap') IS NULL
                BEGIN
                    ALTER TABLE dbo.DonHang ADD MaNdLap INT NULL
                        CONSTRAINT FK_DonHang_NguoiLap FOREIGN KEY REFERENCES dbo.NguoiDung(MaND);
                END;

                IF OBJECT_ID('dbo.ThuongHieu', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ThuongHieu (
                        MaThuongHieu INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaCodeThuongHieu VARCHAR(30) NOT NULL UNIQUE,
                        TenThuongHieu NVARCHAR(120) NOT NULL UNIQUE,
                        XuatXu NVARCHAR(150) NULL,
                        MoTa NVARCHAR(500) NULL,
                        DangHoatDong BIT NOT NULL DEFAULT(1)
                    );
                END;

                IF OBJECT_ID('dbo.NhatKyTruKhoDonHang', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.NhatKyTruKhoDonHang (
                        MaNk INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaDH INT NOT NULL,
                        LoaiDoiTuong VARCHAR(20) NOT NULL,
                        MaLo INT NULL,
                        MaBanh INT NULL,
                        MaVH INT NULL,
                        SoLuongDaTru INT NOT NULL,
                        DaHoanKho BIT NOT NULL DEFAULT(0),
                        NgayGhi DATETIME NOT NULL DEFAULT(GETDATE()),
                        CONSTRAINT FK_NKTK_DonHang FOREIGN KEY (MaDH) REFERENCES dbo.DonHang(MaDH) ON DELETE CASCADE,
                        CONSTRAINT FK_NKTK_LoSanXuat FOREIGN KEY (MaLo) REFERENCES dbo.LoSanXuatBanh(MaLo),
                        CONSTRAINT FK_NKTK_SanPhamBanh FOREIGN KEY (MaBanh) REFERENCES dbo.SanPhamBanh(MaBanh),
                        CONSTRAINT FK_NKTK_MauVoHop FOREIGN KEY (MaVH) REFERENCES dbo.MauVoHop(MaVH)
                    );
                END;

                IF OBJECT_ID('dbo.SanPhamBanh', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('dbo.SanPhamBanh', 'MaThuongHieu') IS NULL
                    BEGIN
                        ALTER TABLE dbo.SanPhamBanh ADD MaThuongHieu INT NULL;
                        ALTER TABLE dbo.SanPhamBanh ADD CONSTRAINT FK_SPBanh_ThuongHieu FOREIGN KEY (MaThuongHieu) REFERENCES dbo.ThuongHieu(MaThuongHieu);
                    END;
                    IF COL_LENGTH('dbo.SanPhamBanh', 'MaNccMacDinh') IS NULL
                    BEGIN
                        ALTER TABLE dbo.SanPhamBanh ADD MaNccMacDinh INT NULL;
                        ALTER TABLE dbo.SanPhamBanh ADD CONSTRAINT FK_SPBanh_NhaCungCap FOREIGN KEY (MaNccMacDinh) REFERENCES dbo.NhaCungCap(MaNcc);
                    END;
                    IF COL_LENGTH('dbo.SanPhamBanh', 'MaVach') IS NULL
                        ALTER TABLE dbo.SanPhamBanh ADD MaVach VARCHAR(50) NULL;
                    IF COL_LENGTH('dbo.SanPhamBanh', 'ViTriKho') IS NULL
                        ALTER TABLE dbo.SanPhamBanh ADD ViTriKho NVARCHAR(100) NOT NULL DEFAULT(N'Kệ A1');
                    IF COL_LENGTH('dbo.SanPhamBanh', 'DonViTinh') IS NULL
                        ALTER TABLE dbo.SanPhamBanh ADD DonViTinh NVARCHAR(50) NOT NULL DEFAULT(N'Cái');
                    IF COL_LENGTH('dbo.SanPhamBanh', 'BanTrucTiep') IS NULL
                        ALTER TABLE dbo.SanPhamBanh ADD BanTrucTiep BIT NOT NULL DEFAULT(1);
                END;

                IF OBJECT_ID('dbo.ComboMauCoSan', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'MaThuongHieu') IS NULL
                    BEGIN
                        ALTER TABLE dbo.ComboMauCoSan ADD MaThuongHieu INT NULL;
                        ALTER TABLE dbo.ComboMauCoSan ADD CONSTRAINT FK_Combo_ThuongHieu FOREIGN KEY (MaThuongHieu) REFERENCES dbo.ThuongHieu(MaThuongHieu);
                    END;
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'MaDM') IS NULL
                    BEGIN
                        ALTER TABLE dbo.ComboMauCoSan ADD MaDM INT NULL;
                        ALTER TABLE dbo.ComboMauCoSan ADD CONSTRAINT FK_Combo_DanhMuc FOREIGN KEY (MaDM) REFERENCES dbo.DanhMucBanh(MaDM);
                    END;
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'MaVach') IS NULL
                        ALTER TABLE dbo.ComboMauCoSan ADD MaVach VARCHAR(50) NULL;
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'ViTriKho') IS NULL
                        ALTER TABLE dbo.ComboMauCoSan ADD ViTriKho NVARCHAR(100) NOT NULL DEFAULT(N'Kệ B1');
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'DonViTinh') IS NULL
                        ALTER TABLE dbo.ComboMauCoSan ADD DonViTinh NVARCHAR(50) NOT NULL DEFAULT(N'Hộp');
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'BanTrucTiep') IS NULL
                        ALTER TABLE dbo.ComboMauCoSan ADD BanTrucTiep BIT NOT NULL DEFAULT(1);
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'TongTonKho') IS NULL
                        ALTER TABLE dbo.ComboMauCoSan ADD TongTonKho INT NOT NULL DEFAULT(40);
                    IF COL_LENGTH('dbo.ComboMauCoSan', 'TrongLuongGram') IS NULL
                        ALTER TABLE dbo.ComboMauCoSan ADD TrongLuongGram INT NOT NULL DEFAULT(600);
                END;

                IF OBJECT_ID('dbo.MaKhuyenMai', 'U') IS NOT NULL AND COL_LENGTH('dbo.MaKhuyenMai', 'TrangThaiKichHoat') IS NULL
                BEGIN
                    ALTER TABLE dbo.MaKhuyenMai ADD TrangThaiKichHoat BIT NOT NULL DEFAULT(1);
                END;

                IF OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('dbo.DonHang', 'KenhBan') IS NULL
                        ALTER TABLE dbo.DonHang ADD KenhBan VARCHAR(30) NOT NULL DEFAULT('WEB_ONLINE');
                    IF COL_LENGTH('dbo.DonHang', 'SoTienKhachDaTra') IS NULL
                        ALTER TABLE dbo.DonHang ADD SoTienKhachDaTra DECIMAL(18,2) NULL;
                    IF COL_LENGTH('dbo.DonHang', 'MaNganHangHoan') IS NULL
                        ALTER TABLE dbo.DonHang ADD MaNganHangHoan VARCHAR(50) NULL;
                    IF COL_LENGTH('dbo.DonHang', 'SoTaiKhoanHoan') IS NULL
                        ALTER TABLE dbo.DonHang ADD SoTaiKhoanHoan VARCHAR(50) NULL;
                    IF COL_LENGTH('dbo.DonHang', 'TenChuTkHoan') IS NULL
                        ALTER TABLE dbo.DonHang ADD TenChuTkHoan NVARCHAR(150) NULL;
                    IF COL_LENGTH('dbo.DonHang', 'GhiChuDieuChinhAdmin') IS NULL
                        ALTER TABLE dbo.DonHang ADD GhiChuDieuChinhAdmin NVARCHAR(500) NULL;
                    IF COL_LENGTH('dbo.DonHang', 'ThoiGianCapNhatCuoi') IS NULL
                        ALTER TABLE dbo.DonHang ADD ThoiGianCapNhatCuoi DATETIME NULL;
                    IF COL_LENGTH('dbo.DonHang', 'NguoiCapNhatCuoi') IS NULL
                        ALTER TABLE dbo.DonHang ADD NguoiCapNhatCuoi NVARCHAR(150) NULL;
                    IF COL_LENGTH('dbo.DonHang', 'DaRollbackKho') IS NULL
                        ALTER TABLE dbo.DonHang ADD DaRollbackKho BIT NOT NULL DEFAULT(0);
                    IF COL_LENGTH('dbo.DonHang', 'MaKM') IS NULL
                        ALTER TABLE dbo.DonHang ADD MaKM INT NULL;
                    IF COL_LENGTH('dbo.DonHang', 'MaMocCK') IS NULL
                        ALTER TABLE dbo.DonHang ADD MaMocCK INT NULL;
                END;

                IF OBJECT_ID('dbo.SanPhamBanh', 'U') IS NOT NULL AND COL_LENGTH('dbo.SanPhamBanh', 'MaAnhChinh') IS NULL
                    ALTER TABLE dbo.SanPhamBanh ADD MaAnhChinh INT NULL;

                IF OBJECT_ID('dbo.MauVoHop', 'U') IS NOT NULL AND COL_LENGTH('dbo.MauVoHop', 'MaAnhChinh') IS NULL
                    ALTER TABLE dbo.MauVoHop ADD MaAnhChinh INT NULL;

                IF OBJECT_ID('dbo.ComboMauCoSan', 'U') IS NOT NULL AND COL_LENGTH('dbo.ComboMauCoSan', 'MaAnhChinh') IS NULL
                    ALTER TABLE dbo.ComboMauCoSan ADD MaAnhChinh INT NULL;

                IF OBJECT_ID('dbo.PhieuKiemKho', 'U') IS NOT NULL AND COL_LENGTH('dbo.PhieuKiemKho', 'MaNdKiem') IS NULL
                    ALTER TABLE dbo.PhieuKiemKho ADD MaNdKiem INT NULL;

                IF OBJECT_ID('dbo.CauHinhHeThong', 'U') IS NOT NULL AND COL_LENGTH('dbo.CauHinhHeThong', 'MaNdCapNhat') IS NULL
                    ALTER TABLE dbo.CauHinhHeThong ADD MaNdCapNhat INT NULL;

                IF OBJECT_ID('dbo.PhieuKhoVoHop', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.PhieuKhoVoHop (
                        MaPvh INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        SoPhieu NVARCHAR(50) NOT NULL UNIQUE,
                        MaVH INT NOT NULL,
                        LoaiGiaoDich VARCHAR(30) NOT NULL,
                        SoLuong INT NOT NULL,
                        DonGiaNhap DECIMAL(18,2) NOT NULL DEFAULT(0),
                        ThanhTien DECIMAL(18,2) NOT NULL DEFAULT(0),
                        MaNcc INT NULL,
                        MaPhieuChi INT NULL,
                        MaNdLap INT NULL,
                        NguoiLap NVARCHAR(150) NOT NULL,
                        GhiChu NVARCHAR(500) NULL,
                        NgayGiaoDich DATETIME NOT NULL DEFAULT(GETDATE()),
                        CONSTRAINT FK_PKVH_MauVoHop FOREIGN KEY (MaVH) REFERENCES dbo.MauVoHop(MaVH) ON DELETE NO ACTION,
                        CONSTRAINT FK_PKVH_NhaCungCap FOREIGN KEY (MaNcc) REFERENCES dbo.NhaCungCap(MaNcc) ON DELETE NO ACTION,
                        CONSTRAINT FK_PKVH_PhieuThuChi FOREIGN KEY (MaPhieuChi) REFERENCES dbo.PhieuThuChi(MaPhieu) ON DELETE NO ACTION,
                        CONSTRAINT FK_PKVH_NguoiDung FOREIGN KEY (MaNdLap) REFERENCES dbo.NguoiDung(MaND) ON DELETE NO ACTION
                    );
                END;
            ");

            // Đợt 2: Làm sạch dữ liệu tham chiếu mồ côi/chuỗi rỗng và đồng bộ ảnh chính trước khi gắn toàn bộ FK
            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.GioHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.MaKhuyenMai', 'U') IS NOT NULL
                BEGIN
                    UPDATE dbo.GioHang
                    SET CodeKMDangApDung = NULL
                    WHERE LTRIM(RTRIM(ISNULL(CodeKMDangApDung, ''))) = ''
                       OR CodeKMDangApDung NOT IN (SELECT CodeKM FROM dbo.MaKhuyenMai);
                END;

                IF OBJECT_ID('dbo.ThiepChucMung3D', 'U') IS NOT NULL
                BEGIN
                    IF OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                        DELETE FROM dbo.ThiepChucMung3D WHERE MaDonHang NOT IN (SELECT MaDH FROM dbo.DonHang);
                    IF OBJECT_ID('dbo.DotGiaoHang_DaDiem', 'U') IS NOT NULL
                        UPDATE dbo.ThiepChucMung3D SET MaDotGiao = NULL
                        WHERE MaDotGiao IS NOT NULL AND MaDotGiao NOT IN (SELECT MaDotGH FROM dbo.DotGiaoHang_DaDiem);
                END;

                IF OBJECT_ID('dbo.PhieuThuChi', 'U') IS NOT NULL AND OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                BEGIN
                    UPDATE dbo.PhieuThuChi SET MaDonHangLienKet = NULL
                    WHERE MaDonHangLienKet IS NOT NULL AND MaDonHangLienKet NOT IN (SELECT MaDH FROM dbo.DonHang);
                END;

                IF OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                BEGIN
                    IF OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                        UPDATE dbo.DonHang SET MaAnhLogoCongTy = NULL
                        WHERE MaAnhLogoCongTy IS NOT NULL AND MaAnhLogoCongTy NOT IN (SELECT MaHA FROM dbo.KhoHinhAnh);
                    IF OBJECT_ID('dbo.MaKhuyenMai', 'U') IS NOT NULL
                        UPDATE dbo.DonHang SET MaKM = NULL
                        WHERE MaKM IS NOT NULL AND MaKM NOT IN (SELECT MaKM FROM dbo.MaKhuyenMai);
                    IF OBJECT_ID('dbo.BangChietKhauSiB2B', 'U') IS NOT NULL
                        UPDATE dbo.DonHang SET MaMocCK = NULL
                        WHERE MaMocCK IS NOT NULL AND MaMocCK NOT IN (SELECT MaMocCK FROM dbo.BangChietKhauSiB2B);
                END;

                IF OBJECT_ID('dbo.ChiTietDonHang', 'U') IS NOT NULL
                BEGIN
                    IF OBJECT_ID('dbo.SanPhamBanh', 'U') IS NOT NULL
                        UPDATE dbo.ChiTietDonHang SET MaBanh = NULL
                        WHERE MaBanh IS NOT NULL AND MaBanh NOT IN (SELECT MaBanh FROM dbo.SanPhamBanh);
                    IF OBJECT_ID('dbo.ComboMauCoSan', 'U') IS NOT NULL
                        UPDATE dbo.ChiTietDonHang SET MaCB = NULL
                        WHERE MaCB IS NOT NULL AND MaCB NOT IN (SELECT MaCB FROM dbo.ComboMauCoSan);
                    IF OBJECT_ID('dbo.HopQuaTuChon', 'U') IS NOT NULL
                        UPDATE dbo.ChiTietDonHang SET MaHopTC = NULL
                        WHERE MaHopTC IS NOT NULL AND MaHopTC NOT IN (SELECT MaHopTC FROM dbo.HopQuaTuChon);
                END;

                IF OBJECT_ID('dbo.CauHinhLandingPage', 'U') IS NOT NULL AND OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                BEGIN
                    UPDATE dbo.CauHinhLandingPage SET MaAnhPhu_Parallax = NULL
                    WHERE MaAnhPhu_Parallax IS NOT NULL AND MaAnhPhu_Parallax NOT IN (SELECT MaHA FROM dbo.KhoHinhAnh);
                END;

                IF OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                BEGIN
                    IF OBJECT_ID('dbo.SanPhamBanh', 'U') IS NOT NULL
                    BEGIN
                        UPDATE dbo.SanPhamBanh SET MaAnhChinh = NULL
                        WHERE MaAnhChinh IS NOT NULL AND MaAnhChinh NOT IN (SELECT MaHA FROM dbo.KhoHinhAnh);

                        IF OBJECT_ID('dbo.LienKetHinhAnh', 'U') IS NOT NULL
                        BEGIN
                            UPDATE sp
                            SET sp.MaAnhChinh = lk.MaHA
                            FROM dbo.SanPhamBanh sp
                            CROSS APPLY (
                                SELECT TOP 1 l.MaHA
                                FROM dbo.LienKetHinhAnh l
                                INNER JOIN dbo.KhoHinhAnh ha ON l.MaHA = ha.MaHA
                                WHERE l.LoaiDoiTuong = 'BANH' AND l.MaDoiTuong = sp.MaBanh
                                ORDER BY l.ThuTuHienThi ASC, l.MaLK ASC
                            ) lk
                            WHERE sp.MaAnhChinh IS NULL;
                        END;
                    END;

                    IF OBJECT_ID('dbo.MauVoHop', 'U') IS NOT NULL
                    BEGIN
                        UPDATE dbo.MauVoHop SET MaAnhChinh = NULL
                        WHERE MaAnhChinh IS NOT NULL AND MaAnhChinh NOT IN (SELECT MaHA FROM dbo.KhoHinhAnh);

                        IF OBJECT_ID('dbo.LienKetHinhAnh', 'U') IS NOT NULL
                        BEGIN
                            UPDATE vh
                            SET vh.MaAnhChinh = lk.MaHA
                            FROM dbo.MauVoHop vh
                            CROSS APPLY (
                                SELECT TOP 1 l.MaHA
                                FROM dbo.LienKetHinhAnh l
                                INNER JOIN dbo.KhoHinhAnh ha ON l.MaHA = ha.MaHA
                                WHERE l.LoaiDoiTuong = 'VOHOP' AND l.MaDoiTuong = vh.MaVH
                                ORDER BY l.ThuTuHienThi ASC, l.MaLK ASC
                            ) lk
                            WHERE vh.MaAnhChinh IS NULL;
                        END;
                    END;

                    IF OBJECT_ID('dbo.ComboMauCoSan', 'U') IS NOT NULL
                    BEGIN
                        UPDATE dbo.ComboMauCoSan SET MaAnhChinh = NULL
                        WHERE MaAnhChinh IS NOT NULL AND MaAnhChinh NOT IN (SELECT MaHA FROM dbo.KhoHinhAnh);

                        IF OBJECT_ID('dbo.LienKetHinhAnh', 'U') IS NOT NULL
                        BEGIN
                            UPDATE cb
                            SET cb.MaAnhChinh = lk.MaHA
                            FROM dbo.ComboMauCoSan cb
                            CROSS APPLY (
                                SELECT TOP 1 l.MaHA
                                FROM dbo.LienKetHinhAnh l
                                INNER JOIN dbo.KhoHinhAnh ha ON l.MaHA = ha.MaHA
                                WHERE l.LoaiDoiTuong = 'COMBO' AND l.MaDoiTuong = cb.MaCB
                                ORDER BY l.ThuTuHienThi ASC, l.MaLK ASC
                            ) lk
                            WHERE cb.MaAnhChinh IS NULL;
                        END;
                    END;
                END;

                IF OBJECT_ID('dbo.NguoiDung', 'U') IS NOT NULL
                BEGIN
                    IF OBJECT_ID('dbo.PhieuKiemKho', 'U') IS NOT NULL
                        UPDATE dbo.PhieuKiemKho SET MaNdKiem = NULL
                        WHERE MaNdKiem IS NOT NULL AND MaNdKiem NOT IN (SELECT MaND FROM dbo.NguoiDung);
                    IF OBJECT_ID('dbo.CauHinhHeThong', 'U') IS NOT NULL
                        UPDATE dbo.CauHinhHeThong SET MaNdCapNhat = NULL
                        WHERE MaNdCapNhat IS NOT NULL AND MaNdCapNhat NOT IN (SELECT MaND FROM dbo.NguoiDung);
                END;
            ");

            // Đợt 3: Gắn toàn bộ khóa ngoại (7 FK cột mới + 9 FK còn thiếu + 4 FK PhieuKhoVoHop) với ON DELETE NO ACTION
            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.PhieuKhoVoHop', 'U') IS NOT NULL
                BEGIN
                    IF OBJECT_ID('dbo.FK_PKVH_MauVoHop', 'F') IS NULL
                        ALTER TABLE dbo.PhieuKhoVoHop WITH CHECK ADD CONSTRAINT FK_PKVH_MauVoHop FOREIGN KEY (MaVH) REFERENCES dbo.MauVoHop(MaVH) ON DELETE NO ACTION;
                    IF OBJECT_ID('dbo.FK_PKVH_NhaCungCap', 'F') IS NULL
                        ALTER TABLE dbo.PhieuKhoVoHop WITH CHECK ADD CONSTRAINT FK_PKVH_NhaCungCap FOREIGN KEY (MaNcc) REFERENCES dbo.NhaCungCap(MaNcc) ON DELETE NO ACTION;
                    IF OBJECT_ID('dbo.FK_PKVH_PhieuThuChi', 'F') IS NULL
                        ALTER TABLE dbo.PhieuKhoVoHop WITH CHECK ADD CONSTRAINT FK_PKVH_PhieuThuChi FOREIGN KEY (MaPhieuChi) REFERENCES dbo.PhieuThuChi(MaPhieu) ON DELETE NO ACTION;
                    IF OBJECT_ID('dbo.FK_PKVH_NguoiDung', 'F') IS NULL
                        ALTER TABLE dbo.PhieuKhoVoHop WITH CHECK ADD CONSTRAINT FK_PKVH_NguoiDung FOREIGN KEY (MaNdLap) REFERENCES dbo.NguoiDung(MaND) ON DELETE NO ACTION;
                END;

                IF OBJECT_ID('dbo.FK_SPBanh_AnhChinh', 'F') IS NULL AND OBJECT_ID('dbo.SanPhamBanh', 'U') IS NOT NULL AND OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                    ALTER TABLE dbo.SanPhamBanh WITH CHECK ADD CONSTRAINT FK_SPBanh_AnhChinh FOREIGN KEY (MaAnhChinh) REFERENCES dbo.KhoHinhAnh(MaHA) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_MauVoHop_AnhChinh', 'F') IS NULL AND OBJECT_ID('dbo.MauVoHop', 'U') IS NOT NULL AND OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                    ALTER TABLE dbo.MauVoHop WITH CHECK ADD CONSTRAINT FK_MauVoHop_AnhChinh FOREIGN KEY (MaAnhChinh) REFERENCES dbo.KhoHinhAnh(MaHA) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_Combo_AnhChinh', 'F') IS NULL AND OBJECT_ID('dbo.ComboMauCoSan', 'U') IS NOT NULL AND OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                    ALTER TABLE dbo.ComboMauCoSan WITH CHECK ADD CONSTRAINT FK_Combo_AnhChinh FOREIGN KEY (MaAnhChinh) REFERENCES dbo.KhoHinhAnh(MaHA) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_DonHang_MaKhuyenMai', 'F') IS NULL AND OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.MaKhuyenMai', 'U') IS NOT NULL
                    ALTER TABLE dbo.DonHang WITH CHECK ADD CONSTRAINT FK_DonHang_MaKhuyenMai FOREIGN KEY (MaKM) REFERENCES dbo.MaKhuyenMai(MaKM) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_DonHang_BangChietKhau', 'F') IS NULL AND OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.BangChietKhauSiB2B', 'U') IS NOT NULL
                    ALTER TABLE dbo.DonHang WITH CHECK ADD CONSTRAINT FK_DonHang_BangChietKhau FOREIGN KEY (MaMocCK) REFERENCES dbo.BangChietKhauSiB2B(MaMocCK) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_PhieuKiemKho_NguoiDung', 'F') IS NULL AND OBJECT_ID('dbo.PhieuKiemKho', 'U') IS NOT NULL AND OBJECT_ID('dbo.NguoiDung', 'U') IS NOT NULL
                    ALTER TABLE dbo.PhieuKiemKho WITH CHECK ADD CONSTRAINT FK_PhieuKiemKho_NguoiDung FOREIGN KEY (MaNdKiem) REFERENCES dbo.NguoiDung(MaND) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_CauHinhHeThong_NguoiDung', 'F') IS NULL AND OBJECT_ID('dbo.CauHinhHeThong', 'U') IS NOT NULL AND OBJECT_ID('dbo.NguoiDung', 'U') IS NOT NULL
                    ALTER TABLE dbo.CauHinhHeThong WITH CHECK ADD CONSTRAINT FK_CauHinhHeThong_NguoiDung FOREIGN KEY (MaNdCapNhat) REFERENCES dbo.NguoiDung(MaND) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_Thiep3D_DonHang', 'F') IS NULL AND OBJECT_ID('dbo.ThiepChucMung3D', 'U') IS NOT NULL AND OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                    ALTER TABLE dbo.ThiepChucMung3D WITH CHECK ADD CONSTRAINT FK_Thiep3D_DonHang FOREIGN KEY (MaDonHang) REFERENCES dbo.DonHang(MaDH) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_Thiep3D_DotGiaoHang', 'F') IS NULL AND OBJECT_ID('dbo.ThiepChucMung3D', 'U') IS NOT NULL AND OBJECT_ID('dbo.DotGiaoHang_DaDiem', 'U') IS NOT NULL
                    ALTER TABLE dbo.ThiepChucMung3D WITH CHECK ADD CONSTRAINT FK_Thiep3D_DotGiaoHang FOREIGN KEY (MaDotGiao) REFERENCES dbo.DotGiaoHang_DaDiem(MaDotGH) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_PhieuThuChi_DonHang', 'F') IS NULL AND OBJECT_ID('dbo.PhieuThuChi', 'U') IS NOT NULL AND OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                    ALTER TABLE dbo.PhieuThuChi WITH CHECK ADD CONSTRAINT FK_PhieuThuChi_DonHang FOREIGN KEY (MaDonHangLienKet) REFERENCES dbo.DonHang(MaDH) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_DonHang_AnhLogoCongTy', 'F') IS NULL AND OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                    ALTER TABLE dbo.DonHang WITH CHECK ADD CONSTRAINT FK_DonHang_AnhLogoCongTy FOREIGN KEY (MaAnhLogoCongTy) REFERENCES dbo.KhoHinhAnh(MaHA) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_GioHang_MaKhuyenMai', 'F') IS NULL AND OBJECT_ID('dbo.GioHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.MaKhuyenMai', 'U') IS NOT NULL
                    ALTER TABLE dbo.GioHang WITH CHECK ADD CONSTRAINT FK_GioHang_MaKhuyenMai FOREIGN KEY (CodeKMDangApDung) REFERENCES dbo.MaKhuyenMai(CodeKM) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_CTDH_SanPhamBanh', 'F') IS NULL AND OBJECT_ID('dbo.ChiTietDonHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.SanPhamBanh', 'U') IS NOT NULL
                    ALTER TABLE dbo.ChiTietDonHang WITH CHECK ADD CONSTRAINT FK_CTDH_SanPhamBanh FOREIGN KEY (MaBanh) REFERENCES dbo.SanPhamBanh(MaBanh) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_CTDH_ComboMau', 'F') IS NULL AND OBJECT_ID('dbo.ChiTietDonHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.ComboMauCoSan', 'U') IS NOT NULL
                    ALTER TABLE dbo.ChiTietDonHang WITH CHECK ADD CONSTRAINT FK_CTDH_ComboMau FOREIGN KEY (MaCB) REFERENCES dbo.ComboMauCoSan(MaCB) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_CTDH_HopTuChon', 'F') IS NULL AND OBJECT_ID('dbo.ChiTietDonHang', 'U') IS NOT NULL AND OBJECT_ID('dbo.HopQuaTuChon', 'U') IS NOT NULL
                    ALTER TABLE dbo.ChiTietDonHang WITH CHECK ADD CONSTRAINT FK_CTDH_HopTuChon FOREIGN KEY (MaHopTC) REFERENCES dbo.HopQuaTuChon(MaHopTC) ON DELETE NO ACTION;

                IF OBJECT_ID('dbo.FK_Landing_AnhPhu', 'F') IS NULL AND OBJECT_ID('dbo.CauHinhLandingPage', 'U') IS NOT NULL AND OBJECT_ID('dbo.KhoHinhAnh', 'U') IS NOT NULL
                    ALTER TABLE dbo.CauHinhLandingPage WITH CHECK ADD CONSTRAINT FK_Landing_AnhPhu FOREIGN KEY (MaAnhPhu_Parallax) REFERENCES dbo.KhoHinhAnh(MaHA) ON DELETE NO ACTION;
            ");
        }

        private static async Task SeedNhaCungCapMacDinhAsync(CSDLBanhTrungThuContext db)
        {
            if (!await db.NhaCungCaps.AnyAsync())
            {
                DateTime bayGio = DateTime.Now;
                db.NhaCungCaps.AddRange(
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC001",
                        TenNcc = "Nhà phân phối Kinh Đô Chính Hãng",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Kinh Đô",
                        PhanTramChietKhauMacDinh = 28m,
                        SoDienThoai = "02838270838",
                        Email = "phanphoi.kinhdo@mondelez.vn",
                        DiaChi = "Số 138 Hai Bà Trưng, Quận 1, TP. Hồ Chí Minh",
                        GhiChu = "Hãng / NPP Cấp 1 chính thức các dòng Kinh Đô Trăng Vàng & Truyền Thống",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC002",
                        TenNcc = "Lò Bánh Đồng Khánh Chính Gốc",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Đồng Khánh",
                        PhanTramChietKhauMacDinh = 26m,
                        SoDienThoai = "02838552418",
                        Email = "kinhdoanh@banhdongkhanh.vn",
                        DiaChi = "Số 62 Trần Hưng Đạo, Quận 5, TP. Hồ Chí Minh",
                        GhiChu = "Lò bánh gia truyền Đồng Khánh Bông Lúa Vàng",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC003",
                        TenNcc = "Bánh Trung Thu Như Lan TP.HCM",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Như Lan",
                        PhanTramChietKhauMacDinh = 25m,
                        SoDienThoai = "02838292970",
                        Email = "daily@nhulanbakery.vn",
                        DiaChi = "Số 50 Hàm Nghi, Quận 1, TP. Hồ Chí Minh",
                        GhiChu = "Nguồn hàng chính gốc Như Lan TP.HCM, chiết khấu theo lô mùa vụ",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC004",
                        TenNcc = "Brodard Bakery Saigon",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Brodard",
                        PhanTramChietKhauMacDinh = 24m,
                        SoDienThoai = "02838223966",
                        Email = "b2c-wholesale@brodardbakery.vn",
                        DiaChi = "Số 11 Nguyễn Thiệp, Quận 1, TP. Hồ Chí Minh",
                        GhiChu = "Dòng bánh Pháp thượng hạng Brodard, giao lạnh bảo quản tiêu chuẩn",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC005",
                        TenNcc = "Givral Bakery Chính Hãng",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Givral",
                        PhanTramChietKhauMacDinh = 25m,
                        SoDienThoai = "02838684377",
                        Email = "nguonhang@givralbakery.vn",
                        DiaChi = "Lô II-1B, KCN Tân Bình, Quận Tân Phú, TP. Hồ Chí Minh",
                        GhiChu = "Hãng bánh tươi Givral ít ngọt & dòng ăn kiêng Isomalt",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC006",
                        TenNcc = "Đại Lý Điều Tiết Bánh Miền Tây (An Giang)",
                        LoaiNcc = "DAI_LY_DIEU_TIET",
                        ThuongHieuCungCap = "Đa thương hiệu",
                        PhanTramChietKhauMacDinh = 22m,
                        SoDienThoai = "02963841225",
                        Email = "dieutiet.mientay@gmail.com",
                        DiaChi = "Số 215 Trần Hưng Đạo, TP. Long Xuyên, An Giang",
                        GhiChu = "Đại lý điều tiết nguồn hàng đa thương hiệu bổ sung gấp trong mùa cao điểm",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    }
                );
                await db.SaveChangesAsync();
            }

            // Liên kết MaNcc cho các phiếu nhập cũ chưa có khóa ngoại MaNcc (không ghi đè chuỗi NhaCungCap cũ)
            var dsPhieuChuaGan = await db.PhieuNhapKhos
                .Where(p => p.MaNcc == null)
                .ToListAsync();

            if (dsPhieuChuaGan.Count > 0)
            {
                var dsNcc = await db.NhaCungCaps.AsNoTracking().ToListAsync();
                bool coCapNhatPn = false;

                foreach (var pn in dsPhieuChuaGan)
                {
                    string tenCu = (pn.NhaCungCap ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(tenCu)) continue;

                    var khop = dsNcc.FirstOrDefault(n =>
                        string.Equals(n.TenNcc, tenCu, StringComparison.OrdinalIgnoreCase))
                        ?? dsNcc.FirstOrDefault(n =>
                            !string.Equals(n.ThuongHieuCungCap, "Đa thương hiệu", StringComparison.OrdinalIgnoreCase) &&
                            (string.Equals(n.ThuongHieuCungCap, tenCu, StringComparison.OrdinalIgnoreCase) ||
                             tenCu.Contains(n.ThuongHieuCungCap, StringComparison.OrdinalIgnoreCase)));

                    if (khop != null)
                    {
                        pn.MaNcc = khop.MaNcc;
                        coCapNhatPn = true;
                    }
                }

                if (coCapNhatPn)
                {
                    await db.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedThuongHieuVaChuanHoaHangHoaAsync(CSDLBanhTrungThuContext db)
        {
            var danhSachMacDinh = new (string MaCode, string Ten, string XuatXu, string MoTa)[]
            {
                ("KINH_DO", "Kinh Đô", "TP. Hồ Chí Minh", "Thương hiệu bánh Trung Thu Kinh Đô Trăng Vàng & Truyền Thống"),
                ("DONG_KHANH", "Đồng Khánh", "TP. Hồ Chí Minh", "Lò bánh Trung Thu gia truyền Đồng Khánh Bông Lúa Vàng"),
                ("NHU_LAN", "Như Lan", "TP. Hồ Chí Minh", "Thương hiệu bánh Trung Thu Như Lan Sài Gòn"),
                ("BRODARD", "Brodard", "TP. Hồ Chí Minh", "Bánh Trung Thu Pháp thượng hạng Brodard Bakery"),
                ("GIVRAL", "Givral", "TP. Hồ Chí Minh", "Bánh tươi Givral Bakery ít ngọt & đường ăn kiêng Isomalt"),
                ("NGUYET_CAT", "Nguyệt Cát", "TP. Long Xuyên, An Giang", "Thương hiệu lễ hộp tuyển chọn Nguyệt Cát")
            };

            var dsThuongHieuDb = await db.ThuongHieus.ToListAsync();
            bool coThemTh = false;
            foreach (var item in danhSachMacDinh)
            {
                if (!dsThuongHieuDb.Any(t =>
                    string.Equals(t.MaCodeThuongHieu, item.MaCode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t.TenThuongHieu, item.Ten, StringComparison.OrdinalIgnoreCase)))
                {
                    var thMoi = new ThuongHieu
                    {
                        MaCodeThuongHieu = item.MaCode,
                        TenThuongHieu = item.Ten,
                        XuatXu = item.XuatXu,
                        MoTa = item.MoTa,
                        DangHoatDong = true
                    };
                    db.ThuongHieus.Add(thMoi);
                    dsThuongHieuDb.Add(thMoi);
                    coThemTh = true;
                }
            }
            if (coThemTh)
            {
                await db.SaveChangesAsync();
            }

            var dsNcc = await db.NhaCungCaps.AsNoTracking().ToListAsync();
            var dsDanhMuc = await db.DanhMucBanhs.AsNoTracking().OrderBy(d => d.ThuTuSx).ThenBy(d => d.MaDm).ToListAsync();
            int? maDmComboMacDinh = dsDanhMuc
                .FirstOrDefault(d => !string.Equals(d.DuongDanSlug, "dong-gia-20k", StringComparison.OrdinalIgnoreCase))?.MaDm
                ?? dsDanhMuc.FirstOrDefault()?.MaDm;

            string[] thuongHieuBanhMau = { "Đồng Khánh", "Kinh Đô", "Như Lan", "Brodard", "Givral", "Đồng Khánh", "Kinh Đô", "Như Lan" };
            string[] viTriBanhMau = { "Kệ A1", "Kệ A2", "Kệ B1", "Kệ B2", "Tủ Mát C1", "Kệ A1", "Kệ B1", "Kệ A2" };

            int? TimMaNccTheoThuongHieu(string tenTh)
            {
                var khop = dsNcc.FirstOrDefault(n =>
                    string.Equals(n.ThuongHieuCungCap, tenTh, StringComparison.OrdinalIgnoreCase)
                    || n.TenNcc.Contains(tenTh, StringComparison.OrdinalIgnoreCase));
                return khop?.MaNcc ?? dsNcc.FirstOrDefault()?.MaNcc;
            }

            var dsBanh = await db.SanPhamBanhs.OrderBy(b => b.MaBanh).ToListAsync();
            bool coSuaBanh = false;
            for (int i = 0; i < dsBanh.Count; i++)
            {
                var b = dsBanh[i];
                int idx = i % thuongHieuBanhMau.Length;
                string tenThMacDinh = thuongHieuBanhMau[idx];
                var thDb = dsThuongHieuDb.FirstOrDefault(t => string.Equals(t.TenThuongHieu, tenThMacDinh, StringComparison.OrdinalIgnoreCase))
                    ?? dsThuongHieuDb.FirstOrDefault();

                if (!b.MaThuongHieu.HasValue && thDb != null)
                {
                    b.MaThuongHieu = thDb.MaThuongHieu;
                    coSuaBanh = true;
                }
                if (!b.MaNccMacDinh.HasValue)
                {
                    string tenThChuan = dsThuongHieuDb.FirstOrDefault(t => t.MaThuongHieu == b.MaThuongHieu)?.TenThuongHieu ?? tenThMacDinh;
                    b.MaNccMacDinh = TimMaNccTheoThuongHieu(tenThChuan);
                    if (b.MaNccMacDinh.HasValue) coSuaBanh = true;
                }
                if (string.IsNullOrWhiteSpace(b.MaVach))
                {
                    b.MaVach = $"8934680010{b.MaBanh:D3}";
                    coSuaBanh = true;
                }
                if (string.IsNullOrWhiteSpace(b.ViTriKho))
                {
                    b.ViTriKho = viTriBanhMau[idx];
                    coSuaBanh = true;
                }
                if (string.IsNullOrWhiteSpace(b.DonViTinh))
                {
                    b.DonViTinh = "Cái";
                    coSuaBanh = true;
                }
            }
            if (coSuaBanh)
            {
                await db.SaveChangesAsync();
            }

            string[] thuongHieuComboMau = { "Đồng Khánh", "Kinh Đô", "Như Lan", "Nguyệt Cát" };
            var dsCombo = await db.ComboMauCoSans.OrderBy(c => c.MaCb).ToListAsync();
            bool coSuaCombo = false;
            for (int i = 0; i < dsCombo.Count; i++)
            {
                var cb = dsCombo[i];
                string tenThMacDinh = thuongHieuComboMau[i % thuongHieuComboMau.Length];
                var thDb = dsThuongHieuDb.FirstOrDefault(t => string.Equals(t.TenThuongHieu, tenThMacDinh, StringComparison.OrdinalIgnoreCase))
                    ?? dsThuongHieuDb.FirstOrDefault();

                if (!cb.MaThuongHieu.HasValue && thDb != null)
                {
                    cb.MaThuongHieu = thDb.MaThuongHieu;
                    coSuaCombo = true;
                }
                if (!cb.MaDm.HasValue && maDmComboMacDinh.HasValue)
                {
                    cb.MaDm = maDmComboMacDinh.Value;
                    coSuaCombo = true;
                }
                if (string.IsNullOrWhiteSpace(cb.MaVach))
                {
                    cb.MaVach = $"8934680990{cb.MaCb:D3}";
                    coSuaCombo = true;
                }
                if (string.IsNullOrWhiteSpace(cb.ViTriKho))
                {
                    cb.ViTriKho = "Kệ B1";
                    coSuaCombo = true;
                }
                if (string.IsNullOrWhiteSpace(cb.DonViTinh))
                {
                    cb.DonViTinh = "Hộp";
                    coSuaCombo = true;
                }
                if (cb.TongTonKho <= 0)
                {
                    cb.TongTonKho = 35 + ((i + 1) * 5);
                    coSuaCombo = true;
                }
                if (cb.TrongLuongGram <= 0)
                {
                    cb.TrongLuongGram = 750;
                    coSuaCombo = true;
                }
            }
            if (coSuaCombo)
            {
                await db.SaveChangesAsync();
            }
        }

        private static async Task DiTruDuLieuCauHinhMoRongAsync(WebApplication app, CSDLBanhTrungThuContext db)
        {
            var jsonOpt = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Đảm bảo khóa cấu hình tài khoản MoMo tồn tại trong dbo.CauHinhHeThong
            if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuCauHinhHeThong.KHOA_CAU_HINH_QR_MOMO))
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = DichVuCauHinhHeThong.KHOA_CAU_HINH_QR_MOMO,
                    GiaTriJson = JsonSerializer.Serialize(new CauHinhThanhToanQr(), jsonOpt),
                    MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                    NgayCapNhat = DateTime.Now
                });
            }

            // Di trú & dọn sạch mọi khóa JSON mở rộng cũ ngoài 4 khóa cài đặt hệ thống chuẩn
            var khoaHeThongChuan = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                DichVuCauHinhHeThong.KHOA_CAU_HINH_QR_MOMO,
                "CAU_HINH_CUA_HANG",
                "CAU_HINH_VAN_CHUYEN",
                "CAU_HINH_HOI_VIEN_VIP"
            };

            var cacKhoaCuThua = await db.CauHinhHeThongs
                .Where(c => !khoaHeThongChuan.Contains(c.KhoaCauHinh))
                .ToListAsync();

            if (cacKhoaCuThua.Count > 0)
            {
                foreach (var rowCu in cacKhoaCuThua)
                {
                    if (string.IsNullOrWhiteSpace(rowCu.GiaTriJson)) continue;
                    try
                    {
                        var storeCu = JsonSerializer.Deserialize<DuLieuMoRongHeThongRoot>(rowCu.GiaTriJson, jsonOpt);
                        if (storeCu != null)
                        {
                            await ChuyenDuLieuJsonCuLenBangSqlAsync(db, storeCu);
                        }
                    }
                    catch
                    {
                        // Bỏ qua JSON cũ không tương thích
                    }
                }

                db.CauHinhHeThongs.RemoveRange(cacKhoaCuThua);
                await db.SaveChangesAsync();
            }

            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.CauHinhHeThong', 'U') IS NOT NULL
                BEGIN
                    DELETE FROM dbo.CauHinhHeThong
                    WHERE KhoaCauHinh NOT IN (
                        N'CAU_HINH_QR_MOMO',
                        N'CAU_HINH_CUA_HANG',
                        N'CAU_HINH_VAN_CHUYEN',
                        N'CAU_HINH_HOI_VIEN_VIP'
                    );
                END;
            ");

            var cacDotCoThiep = await db.DotGiaoHangDaDiems
                .Include(d => d.MaDhNavigation)
                .Where(d => d.CoKemThiepQr && d.MaTokenThiepQr != null && d.MaTokenThiepQr != "")
                .ToListAsync();

            foreach (var dot in cacDotCoThiep)
            {
                string token = dot.MaTokenThiepQr!.Trim();
                if (!await db.ThiepChucMung3ds.AnyAsync(t => t.MaTokenQr == token))
                {
                    db.ThiepChucMung3ds.Add(new ThiepChucMung3d
                    {
                        MaTokenQr = token,
                        MaDonHang = dot.MaDh,
                        MaDotGiao = dot.MaDotGh,
                        TenNguoiGui = dot.MaDhNavigation?.HoTenNguoiMua ?? "Khách Hàng Nguyệt Cát",
                        TenNguoiNhan = dot.TenNguoiNhan,
                        LoiChuc = string.IsNullOrWhiteSpace(dot.LoiChucThiep)
                            ? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!"
                            : dot.LoiChucThiep,
                        MauGiaoDien = string.IsNullOrWhiteSpace(dot.MauGiaoDienThiep) ? "CO_PHONG_HOANG_GIA" : dot.MauGiaoDienThiep,
                        SoLanMoThiep = dot.SoLanMoThiep,
                        NgayTao = dot.MaDhNavigation?.NgayDat ?? DateTime.Now
                    });
                }
            }

            if (!await db.PhieuThuChis.AnyAsync())
            {
                var cacDonDaThu = await db.DonHangs
                    .Where(d => d.TrangThaiTt == "DA_TT" || d.TrangThaiTt == "DA_THANH_TOAN")
                    .OrderByDescending(d => d.NgayDat)
                    .ToListAsync();

                foreach (var dh in cacDonDaThu)
                {
                    db.PhieuThuChis.Add(new PhieuThuChi
                    {
                        MaPhieuHienThi = $"PTH-{dh.NgayDat:yyMMdd}-{dh.MaDh:D3}",
                        LoaiPhieu = "THU",
                        HangMuc = "Thu tiền bán hàng",
                        SoTien = dh.TongThanhToanCuoi,
                        PhuongThuc = string.IsNullOrWhiteSpace(dh.PhuongThucTt) ? "MOMO_QR" : dh.PhuongThucTt,
                        MaDonHangLienKet = dh.MaDh,
                        NguoiNopNhan = dh.HoTenNguoiMua,
                        GhiChu = $"Thu tiền đơn hàng #{dh.MaTraCuuDh}",
                        NgayLap = dh.NgayDat
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task ChuyenDuLieuJsonCuLenBangSqlAsync(CSDLBanhTrungThuContext db, DuLieuMoRongHeThongRoot storeCu)
        {
            var dsThuongHieu = await db.ThuongHieus.ToListAsync();
            var dsNcc = await db.NhaCungCaps.AsNoTracking().ToListAsync();

            if (storeCu.Items != null && storeCu.Items.Count > 0)
            {
                var dsBanh = await db.SanPhamBanhs.ToListAsync();
                foreach (var banh in dsBanh)
                {
                    if (!storeCu.Items.TryGetValue($"BANH_{banh.MaBanh}", out var meta)) continue;
                    if (!string.IsNullOrWhiteSpace(meta.MaVach)) banh.MaVach = meta.MaVach.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.ViTriKho)) banh.ViTriKho = meta.ViTriKho.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.DonViTinh)) banh.DonViTinh = meta.DonViTinh.Trim();
                    banh.BanTrucTiep = meta.BanTrucTiep;

                    if (!string.IsNullOrWhiteSpace(meta.ThuongHieu))
                    {
                        var th = dsThuongHieu.FirstOrDefault(t => string.Equals(t.TenThuongHieu, meta.ThuongHieu.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (th != null) banh.MaThuongHieu = th.MaThuongHieu;
                    }
                    if (!string.IsNullOrWhiteSpace(meta.NhaCungCap))
                    {
                        var ncc = dsNcc.FirstOrDefault(n => string.Equals(n.TenNcc, meta.NhaCungCap.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (ncc != null) banh.MaNccMacDinh = ncc.MaNcc;
                    }
                }

                var dsCombo = await db.ComboMauCoSans.ToListAsync();
                foreach (var cb in dsCombo)
                {
                    if (!storeCu.Items.TryGetValue($"COMBO_{cb.MaCb}", out var meta)) continue;
                    if (!string.IsNullOrWhiteSpace(meta.MaVach)) cb.MaVach = meta.MaVach.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.ViTriKho)) cb.ViTriKho = meta.ViTriKho.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.DonViTinh)) cb.DonViTinh = meta.DonViTinh.Trim();
                    cb.BanTrucTiep = meta.BanTrucTiep;
                    if (meta.TonKhoCombo.HasValue && meta.TonKhoCombo.Value > 0) cb.TongTonKho = meta.TonKhoCombo.Value;
                    if (meta.TrongLuongComboGram.HasValue && meta.TrongLuongComboGram.Value > 0) cb.TrongLuongGram = meta.TrongLuongComboGram.Value;

                    if (!string.IsNullOrWhiteSpace(meta.ThuongHieu))
                    {
                        var th = dsThuongHieu.FirstOrDefault(t => string.Equals(t.TenThuongHieu, meta.ThuongHieu.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (th != null) cb.MaThuongHieu = th.MaThuongHieu;
                    }
                }
            }

            if (storeCu.DonHangs != null && storeCu.DonHangs.Count > 0)
            {
                var maDhs = storeCu.DonHangs.Keys.ToList();
                var dsDh = await db.DonHangs.Where(d => maDhs.Contains(d.MaDh)).ToListAsync();
                var dhCoNhatKy = (await db.NhatKyTruKhoDonHangs.AsNoTracking().Select(n => n.MaDh).Distinct().ToListAsync()).ToHashSet();

                foreach (var dh in dsDh)
                {
                    if (!storeCu.DonHangs.TryGetValue(dh.MaDh, out var meta)) continue;
                    if (!string.IsNullOrWhiteSpace(meta.KenhBan)) dh.KenhBan = meta.KenhBan.Trim();
                    if (meta.SoTienKhachDaTra.HasValue) dh.SoTienKhachDaTra = meta.SoTienKhachDaTra;
                    if (!string.IsNullOrWhiteSpace(meta.MaNganHangHoan)) dh.MaNganHangHoan = meta.MaNganHangHoan.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.SoTaiKhoanHoan)) dh.SoTaiKhoanHoan = meta.SoTaiKhoanHoan.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.TenChuTkHoan)) dh.TenChuTkHoan = meta.TenChuTkHoan.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.GhiChuDieuChinhAdmin)) dh.GhiChuDieuChinhAdmin = meta.GhiChuDieuChinhAdmin.Trim();
                    if (!string.IsNullOrWhiteSpace(meta.NguoiCapNhatCuoi)) dh.NguoiCapNhatCuoi = meta.NguoiCapNhatCuoi.Trim();
                    if (DateTime.TryParseExact(meta.ThoiGianCapNhatCuoi, "dd/MM/yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out var dtCapNhat))
                        dh.ThoiGianCapNhatCuoi = dtCapNhat;
                    if (meta.DaRollbackKho) dh.DaRollbackKho = true;

                    if (!dhCoNhatKy.Contains(dh.MaDh))
                    {
                        var setLo = (await db.LoSanXuatBanhs.AsNoTracking().Select(l => l.MaLo).ToListAsync()).ToHashSet();
                        var setBanh = (await db.SanPhamBanhs.AsNoTracking().Select(b => b.MaBanh).ToListAsync()).ToHashSet();
                        var setVo = (await db.MauVoHops.AsNoTracking().Select(v => v.MaVh).ToListAsync()).ToHashSet();

                        foreach (var lo in meta.DanhSachLoDaTru ?? new List<NhatKyTruLoFefo>())
                        {
                            db.NhatKyTruKhoDonHangs.Add(new NhatKyTruKhoDonHang
                            {
                                MaDh = dh.MaDh,
                                LoaiDoiTuong = "LO_BANH",
                                MaLo = (lo.MaLo > 0 && setLo.Contains(lo.MaLo)) ? lo.MaLo : null,
                                MaBanh = (lo.MaBanh > 0 && setBanh.Contains(lo.MaBanh)) ? lo.MaBanh : null,
                                SoLuongDaTru = lo.SoLuongDaTru,
                                DaHoanKho = meta.DaRollbackKho,
                                NgayGhi = dh.NgayDat
                            });
                        }
                        foreach (var vo in meta.DanhSachVoHopDaTru ?? new List<NhatKyTruVoHop>())
                        {
                            db.NhatKyTruKhoDonHangs.Add(new NhatKyTruKhoDonHang
                            {
                                MaDh = dh.MaDh,
                                LoaiDoiTuong = "VO_HOP",
                                MaVh = (vo.MaVh > 0 && setVo.Contains(vo.MaVh)) ? vo.MaVh : null,
                                SoLuongDaTru = vo.SoLuongDaTru,
                                DaHoanKho = meta.DaRollbackKho,
                                NgayGhi = dh.NgayDat
                            });
                        }
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task ChuanHoaChiMucTokenThiepQrAsync(CSDLBanhTrungThuContext db)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                DECLARE @uqName NVARCHAR(200);
                SELECT TOP 1 @uqName = kc.name
                FROM sys.key_constraints kc
                JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE kc.parent_object_id = OBJECT_ID('DotGiaoHang_DaDiem') AND kc.type = 'UQ' AND c.name = 'MaTokenThiepQR';
                IF @uqName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE DotGiaoHang_DaDiem DROP CONSTRAINT [' + @uqName + ']');
                END
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DotGiaoHang_MaTokenThiepQR_Filtered' AND object_id = OBJECT_ID('DotGiaoHang_DaDiem'))
                BEGIN
                    CREATE UNIQUE NONCLUSTERED INDEX IX_DotGiaoHang_MaTokenThiepQR_Filtered
                    ON DotGiaoHang_DaDiem(MaTokenThiepQR) WHERE MaTokenThiepQR IS NOT NULL;
                END
            ");
        }

        private static async Task ChuanHoaMaKhuyenMaiVaLoBanhFefoAsync(CSDLBanhTrungThuContext db)
        {
            var mocHetHanChuan = new DateTime(2027, 12, 31, 23, 59, 59);
            var dsKm = await db.MaKhuyenMais.ToListAsync();
            bool coCapNhatKm = false;

            var trangTron = dsKm.FirstOrDefault(k => k.CodeKm == "TRANGTRON2026");
            if (trangTron == null)
            {
                db.MaKhuyenMais.Add(new MaKhuyenMai
                {
                    CodeKm = "TRANGTRON2026",
                    TenChuongTrinh = "Ưu Đãi Đêm Trăng Đoàn Viên (-10%)",
                    LoaiGiam = "PHAN_TRAM",
                    GiaTriGiam = 10m,
                    GiamToiDa = 150000m,
                    DonToiThieu = 200000m,
                    SltoiDa = 500,
                    SldaDung = 0,
                    ChoPhepKhachVangLai = true,
                    NgayBatDau = new DateTime(2026, 1, 1),
                    NgayKetThuc = mocHetHanChuan
                });
                coCapNhatKm = true;
            }

            var quaTang15k = dsKm.FirstOrDefault(k => k.CodeKm == "QUATANG15K");
            if (quaTang15k == null)
            {
                db.MaKhuyenMais.Add(new MaKhuyenMai
                {
                    CodeKm = "QUATANG15K",
                    TenChuongTrinh = "Quà Tặng Tri Ân (-15.000đ)",
                    LoaiGiam = "TIEN_MAT",
                    GiaTriGiam = 15000m,
                    GiamToiDa = 15000m,
                    DonToiThieu = 100000m,
                    SltoiDa = 1000,
                    SldaDung = 0,
                    ChoPhepKhachVangLai = true,
                    NgayBatDau = new DateTime(2026, 1, 1),
                    NgayKetThuc = mocHetHanChuan
                });
                coCapNhatKm = true;
            }

            foreach (var km in dsKm)
            {
                if (km.NgayBatDau > DateTime.Now)
                {
                    km.NgayBatDau = new DateTime(2026, 1, 1);
                    coCapNhatKm = true;
                }
                if (km.NgayKetThuc < mocHetHanChuan)
                {
                    km.NgayKetThuc = mocHetHanChuan;
                    coCapNhatKm = true;
                }
                if (km.SltoiDa <= km.SldaDung)
                {
                    km.SltoiDa = km.SldaDung + 100;
                    coCapNhatKm = true;
                }
            }

            if (coCapNhatKm)
            {
                await db.SaveChangesAsync();
            }

            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var tatCaBanh = await db.SanPhamBanhs.Include(b => b.LoSanXuatBanhs).ToListAsync();
            bool coCapNhatLo = false;
            foreach (var banh in tatCaBanh)
            {
                var loConHan = banh.LoSanXuatBanhs.Where(l => l.HanSuDung > homNay && l.TrangThaiLo == "DANG_BAN").ToList();
                if (!loConHan.Any() && banh.TongTonKho > 0)
                {
                    var loCu = banh.LoSanXuatBanhs.OrderByDescending(l => l.HanSuDung).FirstOrDefault();
                    if (loCu != null && loCu.TrangThaiLo != "HET_HAN" && loCu.TrangThaiLo != "DA_HUY")
                    {
                        loCu.HanSuDung = homNay.AddDays(45);
                        loCu.TrangThaiLo = "DANG_BAN";
                        if (loCu.SlconLai <= 0) loCu.SlconLai = banh.TongTonKho;
                    }
                    else
                    {
                        db.LoSanXuatBanhs.Add(new LoSanXuatBanh
                        {
                            MaBanh = banh.MaBanh,
                            MaLoCode = $"LO-2026-{banh.MaBanh:D2}",
                            NgaySanXuat = homNay.AddDays(-5),
                            HanSuDung = homNay.AddDays(45),
                            SlnhapBanDau = Math.Max(50, banh.TongTonKho),
                            SlconLai = banh.TongTonKho,
                            PhanTramGiamCanDate = 0m,
                            TrangThaiLo = "DANG_BAN"
                        });
                    }
                    coCapNhatLo = true;
                }
            }
            if (coCapNhatLo)
            {
                await db.SaveChangesAsync();
            }
        }

        private static async Task CapNhatAnhKhayHop6OSonMaiAsync(CSDLBanhTrungThuContext db)
        {
            var danhSachVoHop = await db.MauVoHops.ToListAsync();
            bool coCapNhatAnhVoHop = false;
            var cacThuocTinhChuoiVoHop = typeof(MauVoHop)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite && p.Name != nameof(MauVoHop.TenVoHop))
                .ToList();

            foreach (var voHop in danhSachVoHop)
            {
                bool laHop6O = voHop.TenVoHop.Contains("6 Bánh", StringComparison.OrdinalIgnoreCase)
                            || voHop.TenVoHop.Contains("6 Ô", StringComparison.OrdinalIgnoreCase)
                            || voHop.TenVoHop.Contains("Hoàng Gia", StringComparison.OrdinalIgnoreCase);

                if (laHop6O)
                {
                    foreach (var thuocTinh in cacThuocTinhChuoiVoHop)
                    {
                        string giaTriHienTai = (thuocTinh.GetValue(voHop) as string) ?? string.Empty;
                        bool laCotAnh = thuocTinh.Name.Contains("Anh", StringComparison.OrdinalIgnoreCase)
                                     || thuocTinh.Name.Contains("Hinh", StringComparison.OrdinalIgnoreCase)
                                     || giaTriHienTai.Contains("khay-4-o-rong", StringComparison.OrdinalIgnoreCase)
                                     || giaTriHienTai.Contains("/images/vohop/", StringComparison.OrdinalIgnoreCase);

                        if (laCotAnh && !string.Equals(giaTriHienTai, "/images/vohop/khay-6-o-rong.png", StringComparison.OrdinalIgnoreCase))
                        {
                            thuocTinh.SetValue(voHop, "/images/vohop/khay-6-o-rong.png");
                            coCapNhatAnhVoHop = true;
                        }
                    }
                }
            }

            if (coCapNhatAnhVoHop)
            {
                await db.SaveChangesAsync();
            }
        }

        private static async Task CapNhatDuongDanAnhBanhHatSenHueAsync(CSDLBanhTrungThuContext db)
        {
            const string tenAnhCu = "kv-20261101155727-573.jpg";
            const string tenAnhMoi = "banh-hat-sen-hue.jpg";
            const string duongDanMoi = "/images/banh/banh-hat-sen-hue.jpg";
            bool coThayDoi = false;

            var dsKhoAnh = await db.KhoHinhAnhs
                .Where(h => (h.DuongDanCucBo != null && h.DuongDanCucBo.Contains(tenAnhCu)) || h.TenTepGoc.Contains(tenAnhCu))
                .ToListAsync();

            foreach (var anh in dsKhoAnh)
            {
                if (anh.TenTepGoc.Contains(tenAnhCu, StringComparison.OrdinalIgnoreCase))
                {
                    anh.TenTepGoc = tenAnhMoi;
                }
                if (!string.IsNullOrWhiteSpace(anh.DuongDanCucBo) && anh.DuongDanCucBo.Contains(tenAnhCu, StringComparison.OrdinalIgnoreCase))
                {
                    anh.DuongDanCucBo = duongDanMoi;
                }
                coThayDoi = true;
            }

            var dsCauHinh = await db.CauHinhHeThongs
                .Where(c => c.GiaTriJson.Contains(tenAnhCu))
                .ToListAsync();

            foreach (var ch in dsCauHinh)
            {
                ch.GiaTriJson = ch.GiaTriJson.Replace(tenAnhCu, tenAnhMoi, StringComparison.OrdinalIgnoreCase);
                ch.NgayCapNhat = DateTime.Now;
                coThayDoi = true;
            }

            if (coThayDoi)
            {
                await db.SaveChangesAsync();
            }
        }

        private static async Task ChuanHoaTenMocChietKhauAsync(CSDLBanhTrungThuContext db)
        {
            var dsMoc = await db.BangChietKhauSiB2bs.ToListAsync();
            bool coThayDoi = false;

            foreach (var moc in dsMoc)
            {
                if (!string.IsNullOrWhiteSpace(moc.TenMocB2b)
                    && moc.TenMocB2b.Contains("B2B", StringComparison.OrdinalIgnoreCase))
                {
                    string tenMoi = Regex.Replace(moc.TenMocB2b, @"\(\s*b2b\s*\)|\bb2b\b", string.Empty, RegexOptions.IgnoreCase);
                    tenMoi = Regex.Replace(tenMoi, @"\s{2,}", " ").Trim(' ', '-', '–');
                    if (string.IsNullOrWhiteSpace(tenMoi))
                    {
                        tenMoi = $"Mốc Chiết Khấu ({moc.SlhopToiThieu}-{moc.SlhopToiDa} hộp)";
                    }
                    moc.TenMocB2b = tenMoi;
                    coThayDoi = true;
                }
            }

            if (coThayDoi)
            {
                await db.SaveChangesAsync();
            }
        }

        private static async Task ChuanHoaEmailAdminVaDongBoDonHuyAsync(CSDLBanhTrungThuContext db)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.NguoiDung', 'U') IS NOT NULL
                BEGIN
                    UPDATE dbo.NguoiDung
                    SET Email = REPLACE(LTRIM(RTRIM(Email)), ' ', '')
                    WHERE Email LIKE '% %' AND Email LIKE '%@%'
                      AND NOT EXISTS (
                          SELECT 1 FROM dbo.NguoiDung nd2
                          WHERE nd2.MaND <> dbo.NguoiDung.MaND
                            AND nd2.Email = REPLACE(LTRIM(RTRIM(dbo.NguoiDung.Email)), ' ', '')
                      );

                    IF EXISTS (SELECT 1 FROM dbo.NguoiDung WHERE Email LIKE 'admin@nguy%c%' AND Email <> 'admin@nguyetcat.vn')
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WHERE Email = 'admin@nguyetcat.vn')
                        BEGIN
                            UPDATE dbo.NguoiDung
                            SET Email = 'admin@nguyetcat.vn'
                            WHERE Email LIKE 'admin@nguy%c%' AND Email <> 'admin@nguyetcat.vn';
                        END
                        ELSE
                        BEGIN
                            UPDATE dbo.NguoiDung
                            SET Email = CONCAT('admin', MaND, '@nguyetcat.vn')
                            WHERE Email LIKE 'admin@nguy%c%' AND Email <> 'admin@nguyetcat.vn';
                        END
                    END;

                    UPDATE dbo.NguoiDung
                    SET Email = REPLACE(REPLACE(REPLACE(Email, N'nguy?tcát.vn', 'nguyetcat.vn'), N'nguy?tcat.vn', 'nguyetcat.vn'), N'nguyệtcát.vn', 'nguyetcat.vn')
                    WHERE (Email LIKE '%nguy?t%' OR Email LIKE N'%nguyệt%' OR Email LIKE N'%cát.vn%')
                      AND NOT EXISTS (
                          SELECT 1 FROM dbo.NguoiDung nd2
                          WHERE nd2.MaND <> dbo.NguoiDung.MaND
                            AND nd2.Email = REPLACE(REPLACE(REPLACE(dbo.NguoiDung.Email, N'nguy?tcát.vn', 'nguyetcat.vn'), N'nguy?tcat.vn', 'nguyetcat.vn'), N'nguyệtcát.vn', 'nguyetcat.vn')
                      );
                END;

                IF OBJECT_ID('dbo.DotGiaoHang_DaDiem', 'U') IS NOT NULL AND OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                BEGIN
                    UPDATE d
                    SET d.TrangThaiGiao = 'DA_HUY'
                    FROM dbo.DotGiaoHang_DaDiem d
                    INNER JOIN dbo.DonHang h ON d.MaDH = h.MaDH
                    WHERE h.TrangThaiDH = 'DA_HUY'
                      AND ISNULL(d.TrangThaiGiao, '') <> 'DA_HUY';
                END;
            ");
        }

        private static async Task SeedHoSoNhanVienVaDongBoVaiTroAsync(CSDLBanhTrungThuContext db)
        {
            var vaiTroBatBuoc = new (string MaVt, string TenVt, string MoTa)[]
            {
                ("ADMIN", "Chủ cửa hàng", "Toàn quyền quản trị cửa hàng, kho bãi, nhân sự và sổ quỹ Nguyệt Cát ERP"),
                ("NHAN_VIEN", "Nhân viên Bán hàng", "Nhân viên bán hàng tại quầy POS và xử lý đơn hàng trực tuyến"),
                ("KHACH_TV", "Khách thành viên", "Khách hàng thành viên tích điểm")
            };

            bool coThayDoiVt = false;
            foreach (var vt in vaiTroBatBuoc)
            {
                var vtDb = await db.VaiTros.FirstOrDefaultAsync(v => v.MaVt == vt.MaVt);
                if (vtDb == null)
                {
                    db.VaiTros.Add(new VaiTro
                    {
                        MaVt = vt.MaVt,
                        TenVt = vt.TenVt,
                        MoTa = vt.MoTa
                    });
                    coThayDoiVt = true;
                }
                else if (vt.MaVt == "NHAN_VIEN" && vtDb.TenVt != vt.TenVt)
                {
                    vtDb.TenVt = vt.TenVt;
                    coThayDoiVt = true;
                }
            }
            if (coThayDoiVt)
            {
                await db.SaveChangesAsync();
            }

            string mkSha = ThongTinPhienHelper.BamMatKhauSHA256("123456");
            var taiKhoanNoiBoMau = new (string Email, string Sdt, string HoTen, string MaVt, string HangTv)[]
            {
                ("admin@nguyetcat.vn", "0814585526", "Lê Minh Đức (Chủ cửa hàng)", "ADMIN", "Kim Cương"),
                ("thungan@nguyetcat.vn", "0908111222", "Trần Thu Ngân (Nhân viên Bán hàng)", "NHAN_VIEN", "Vàng"),
                ("giaohang@nguyetcat.vn", "0908333444", "Nguyễn Văn Giao (Nhân viên Bán hàng)", "NHAN_VIEN", "Bạc")
            };

            bool coThayDoiNd = false;
            foreach (var tk in taiKhoanNoiBoMau)
            {
                var tonTai = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == tk.Email || u.Sdt == tk.Sdt);
                if (tonTai == null)
                {
                    db.NguoiDungs.Add(new NguoiDung
                    {
                        MaVt = tk.MaVt,
                        HoTen = tk.HoTen,
                        Sdt = tk.Sdt,
                        Email = tk.Email,
                        MatKhauHash = mkSha,
                        DiemTichLuy = 500,
                        HangTv = tk.HangTv,
                        TrangThai = true,
                        NgayTao = DateTime.Now
                    });
                    coThayDoiNd = true;
                }
            }
            if (coThayDoiNd)
            {
                await db.SaveChangesAsync();
            }

            // Đồng bộ vai trò nội bộ hiện hữu về đúng 2 vai trò: ADMIN (Chủ cửa hàng) & NHAN_VIEN (Nhân viên Bán hàng)
            var cacVaiTroCuCapDuoi = new[] { "THU_NGAN", "GIAO_HANG", "NV_BAN_HANG" };
            var tatCaNoiBo = await db.NguoiDungs
                .Where(u => u.MaVt == "ADMIN"
                         || u.MaVt == "QUAN_TRI"
                         || u.MaVt == "NHAN_VIEN"
                         || cacVaiTroCuCapDuoi.Contains(u.MaVt)
                         || u.Email == "admin@nguyetcat.vn"
                         || u.Email == "thungan@nguyetcat.vn"
                         || u.Email == "giaohang@nguyetcat.vn")
                .OrderBy(u => u.MaNd)
                .ToListAsync();

            bool coDongBoVt = false;
            foreach (var nd in tatCaNoiBo)
            {
                if (string.Equals(nd.Email, "admin@nguyetcat.vn", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(nd.MaVt, "QUAN_TRI", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(nd.MaVt, "ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    if (nd.MaVt != "ADMIN")
                    {
                        nd.MaVt = "ADMIN";
                        coDongBoVt = true;
                    }
                }
                else if (nd.MaVt != "NHAN_VIEN")
                {
                    nd.MaVt = "NHAN_VIEN";
                    coDongBoVt = true;
                }
            }
            if (coDongBoVt)
            {
                await db.SaveChangesAsync();
            }

            // Seed bản ghi dbo.HoSoNhanVien cho các tài khoản nội bộ chưa có hồ sơ
            var dsMaNdDaCoHoSo = await db.HoSoNhanViens
                .AsNoTracking()
                .Select(h => h.MaNd)
                .ToListAsync();
            var setMaNdDaCo = dsMaNdDaCoHoSo.ToHashSet();

            var dsCodeDaDung = await db.HoSoNhanViens
                .AsNoTracking()
                .Select(h => h.MaCodeNv)
                .ToListAsync();
            var setCodeDaDung = new HashSet<string>(dsCodeDaDung, StringComparer.OrdinalIgnoreCase);

            int soThuTuKeTiep = 1;
            string SinhMaNvKeTiep()
            {
                while (true)
                {
                    string ungVien = $"NV{soThuTuKeTiep:D3}";
                    soThuTuKeTiep++;
                    if (!setCodeDaDung.Contains(ungVien))
                    {
                        setCodeDaDung.Add(ungVien);
                        return ungVien;
                    }
                }
            }

            bool coThemHoSo = false;
            foreach (var nd in tatCaNoiBo)
            {
                if (setMaNdDaCo.Contains(nd.MaNd)) continue;

                string emailLower = (nd.Email ?? string.Empty).Trim().ToLowerInvariant();
                string maCodeUutien;
                string chucVu;
                string caLamViec;
                string ghiChu;

                if (emailLower == "admin@nguyetcat.vn" || nd.MaVt == "ADMIN")
                {
                    maCodeUutien = "NV001";
                    chucVu = "Chủ cửa hàng";
                    caLamViec = "Toàn thời gian";
                    ghiChu = "Chủ cửa hàng quản trị toàn hệ thống Nguyệt Cát ERP";
                }
                else if (emailLower == "thungan@nguyetcat.vn")
                {
                    maCodeUutien = "NV002";
                    chucVu = "Nhân viên Bán hàng";
                    caLamViec = "Ca sáng (07:30 - 14:30)";
                    ghiChu = "Nhân viên bán hàng tại quầy POS & xử lý đơn trực tuyến";
                }
                else if (emailLower == "giaohang@nguyetcat.vn")
                {
                    maCodeUutien = "NV003";
                    chucVu = "Nhân viên Bán hàng";
                    caLamViec = "Ca chiều (14:00 - 21:00)";
                    ghiChu = "Nhân viên bán hàng & điều phối giao nhận đơn hàng";
                }
                else
                {
                    maCodeUutien = string.Empty;
                    chucVu = "Nhân viên Bán hàng";
                    caLamViec = "Toàn thời gian";
                    ghiChu = "Nhân sự nội bộ Nguyệt Cát";
                }

                string maCodeChot = !string.IsNullOrEmpty(maCodeUutien) && !setCodeDaDung.Contains(maCodeUutien)
                    ? maCodeUutien
                    : SinhMaNvKeTiep();
                setCodeDaDung.Add(maCodeChot);

                db.HoSoNhanViens.Add(new HoSoNhanVien
                {
                    MaNd = nd.MaNd,
                    MaCodeNv = maCodeChot,
                    ChucVu = chucVu,
                    ChiNhanh = "TP. Long Xuyên",
                    CaLamViec = caLamViec,
                    NgayVaoLam = DateOnly.FromDateTime(nd.NgayTao == default ? DateTime.Today : nd.NgayTao),
                    TrangThaiLamViec = nd.TrangThai ? "DANG_LAM_VIEC" : "TAM_KHOA",
                    GhiChu = ghiChu
                });
                setMaNdDaCo.Add(nd.MaNd);
                coThemHoSo = true;
            }

            if (coThemHoSo)
            {
                await db.SaveChangesAsync();
            }
        }

        private static async Task ChuanHoaMaCodeLandingPageAsync(CSDLBanhTrungThuContext db)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.CauHinhLandingPage', 'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM dbo.CauHinhLandingPage WHERE MaCodeKhuVuc = 'TANG_1_HERO')
                       AND NOT EXISTS (SELECT 1 FROM dbo.CauHinhLandingPage WHERE MaCodeKhuVuc = 'HERO_BANNER')
                    BEGIN
                        UPDATE dbo.CauHinhLandingPage
                        SET MaCodeKhuVuc = 'HERO_BANNER'
                        WHERE MaCodeKhuVuc = 'TANG_1_HERO';
                    END;

                    IF EXISTS (SELECT 1 FROM dbo.CauHinhLandingPage WHERE MaCodeKhuVuc = 'TANG_2_STUDIO')
                       AND NOT EXISTS (SELECT 1 FROM dbo.CauHinhLandingPage WHERE MaCodeKhuVuc = 'BOX_BUILDER_INTRO')
                    BEGIN
                        UPDATE dbo.CauHinhLandingPage
                        SET MaCodeKhuVuc = 'BOX_BUILDER_INTRO'
                        WHERE MaCodeKhuVuc = 'TANG_2_STUDIO';
                    END;

                    -- Chuẩn hóa mốc đếm ngược HERO_BANNER về đúng Tết Trung Thu (15/08 Âm lịch), thay cho mốc 31/12 cũ
                    UPDATE dbo.CauHinhLandingPage
                    SET MocThoiGianDemNguoc = CASE
                        WHEN GETDATE() < '2026-09-25T20:00:00' THEN '2026-09-25T20:00:00'
                        WHEN GETDATE() < '2027-09-15T20:00:00' THEN '2027-09-15T20:00:00'
                        WHEN GETDATE() < '2028-10-03T20:00:00' THEN '2028-10-03T20:00:00'
                        ELSE '2029-09-22T20:00:00'
                    END
                    WHERE MaCodeKhuVuc = 'HERO_BANNER'
                      AND (MocThoiGianDemNguoc IS NULL
                           OR MocThoiGianDemNguoc <= GETDATE()
                           OR (MONTH(MocThoiGianDemNguoc) = 12 AND DAY(MocThoiGianDemNguoc) = 31));
                END;
            ");
        }
    }
}
