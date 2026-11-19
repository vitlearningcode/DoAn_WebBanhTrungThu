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
    /// Lớp chuyên trách khởi tạo cấu trúc bảng mở rộng (PhieuThuChi, ThiepChucMung3D, CauHinhHeThong),
    /// dọn dẹp 2 bảng thừa (NguyenLieu, CongThucBanhNl), di trú dữ liệu cấu hình mở rộng Đại lý Nguyệt Cát ERP,
    /// chuẩn hóa hạn mã khuyến mãi 2027, hạn sử dụng lô bánh FEFO và ảnh khay hộp 6 ô.
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
                await DiTruDuLieuCauHinhMoRongAsync(app, db);
                await ChuanHoaChiMucTokenThiepQrAsync(db);
                await ChuanHoaMaKhuyenMaiVaLoBanhFefoAsync(db);
                await CapNhatAnhKhayHop6OSonMaiAsync(db);
                await CapNhatDuongDanAnhBanhHatSenHueAsync(db);
                await ChuanHoaTenMocChietKhauAsync(db);
                await ChuanHoaEmailAdminVaDongBoDonHuyAsync(db);
            }
            catch
            {
                // Bỏ qua nếu CSDL đang trong quá trình khởi tạo lần đầu
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
            ");
        }

        private static async Task DiTruDuLieuCauHinhMoRongAsync(WebApplication app, CSDLBanhTrungThuContext db)
        {
            var jsonOpt = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Tự động chuyển đổi khóa cũ ("NGUYETCAT_ERP_METADATA" / "KIOT_METADATA_STORE") sang khóa chuẩn "CAUHINH_MORONG_NGUYETCAT"
            var banGhiMoi = await db.CauHinhHeThongs
                .FirstOrDefaultAsync(c => c.KhoaCauHinh == DichVuCauHinhHeThong.KHOA_CAU_HINH_MORONG);

            var dsBanGhiCu = await db.CauHinhHeThongs
                .Where(c => c.KhoaCauHinh == DichVuCauHinhHeThong.KHOA_CU_1 || c.KhoaCauHinh == DichVuCauHinhHeThong.KHOA_CU_2)
                .ToListAsync();

            if (banGhiMoi == null && dsBanGhiCu.Count > 0)
            {
                var banGhiCu = dsBanGhiCu.First();
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = DichVuCauHinhHeThong.KHOA_CAU_HINH_MORONG,
                    GiaTriJson = banGhiCu.GiaTriJson,
                    MoTa = "Dữ liệu mở rộng Hàng hóa, Thương hiệu, Vị trí kho và Nhật ký FEFO Đại lý Nguyệt Cát ERP",
                    NgayCapNhat = DateTime.Now
                });
            }

            if (dsBanGhiCu.Count > 0)
            {
                db.CauHinhHeThongs.RemoveRange(dsBanGhiCu);
                await db.SaveChangesAsync();
            }

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

            if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuCauHinhHeThong.KHOA_CAU_HINH_MORONG))
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = DichVuCauHinhHeThong.KHOA_CAU_HINH_MORONG,
                    GiaTriJson = JsonSerializer.Serialize(DichVuCauHinhHeThong.TaoDuLieuMacDinh(), jsonOpt),
                    MoTa = "Dữ liệu mở rộng Hàng hóa, Thương hiệu, Vị trí kho và Nhật ký FEFO Đại lý Nguyệt Cát ERP",
                    NgayCapNhat = DateTime.Now
                });
            }

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
                    if (loCu != null && loCu.TrangThaiLo != "HET_HAN")
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
    }
}
