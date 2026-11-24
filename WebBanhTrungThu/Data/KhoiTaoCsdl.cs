using Microsoft.EntityFrameworkCore;

namespace WebBanhTrungThu.Data
{
    /// <summary>
    /// Lớp chuyên trách khởi tạo cấu trúc bảng mở rộng (PhieuThuChi, ThiepChucMung3D, CauHinhHeThong, PhieuKhoVoHop),
    /// dọn dẹp 2 bảng thừa (NguyenLieu, CongThucBanhNl), gắn đầy đủ khóa ngoại chuẩn 37/37 bảng SQL-First
    /// và điều phối nạp dữ liệu mẫu / chuẩn hóa CSDL khi ứng dụng khởi động (chia thành partial class).
    /// </summary>
    public static partial class KhoiTaoCsdl
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
                await DiTruDuLieuCauHinhMoRongAsync(db);
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
    }
}
