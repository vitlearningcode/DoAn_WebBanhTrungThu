using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;

namespace WebBanhTrungThu.Data
{
    /// <summary>
    /// Lớp chuyên trách khởi tạo cấu trúc bảng mở rộng (PhieuThuChi, ThiepChucMung3D, CauHinhHeThong),
    /// di trú dữ liệu KiotViet, chuẩn hóa hạn mã khuyến mãi 2027, hạn sử dụng lô bánh FEFO và ảnh khay hộp 6 ô.
    /// Giúp giữ cho Program.cs tinh gọn đúng chuẩn Clean Architecture.
    /// </summary>
    public static class KhoiTaoCsdl
    {
        public static async Task KhoiTaoVaChuanHoaDuLieuAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                await TaoCacBangMoRongNeuChuaCoAsync(db);
                await DiTruDuLieuKiotVietVaCauHinhAsync(app, db);
                await ChuanHoaChiMucTokenThiepQrAsync(db);
                await ChuanHoaMaKhuyenMaiVaLoBanhFefoAsync(db);
                await CapNhatAnhKhayHop6OSonMaiAsync(db);
            }
            catch
            {
                // Bỏ qua nếu CSDL đang trong quá trình khởi tạo lần đầu
            }
        }

        private static async Task TaoCacBangMoRongNeuChuaCoAsync(CSDLBanhTrungThuContext db)
        {
            await db.Database.ExecuteSqlRawAsync(@"
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
            ");
        }

        private static async Task DiTruDuLieuKiotVietVaCauHinhAsync(WebApplication app, CSDLBanhTrungThuContext db)
        {
            string thuMucDataCu = Path.Combine(app.Environment.WebRootPath, "data");
            string fileJsonCu = Path.Combine(thuMucDataCu, "kiotviet_metadata.json");
            var jsonOpt = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            if (File.Exists(fileJsonCu))
            {
                try
                {
                    string noiDungJsonCu = await File.ReadAllTextAsync(fileJsonCu);
                    var storeCu = JsonSerializer.Deserialize<KiotMetadataStore>(noiDungJsonCu, jsonOpt);
                    if (storeCu != null)
                    {
                        if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO))
                        {
                            db.CauHinhHeThongs.Add(new CauHinhHeThong
                            {
                                KhoaCauHinh = DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO,
                                GiaTriJson = JsonSerializer.Serialize(storeCu.CauHinhQr ?? new CauHinhThanhToanQr(), jsonOpt),
                                MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                                NgayCapNhat = DateTime.Now
                            });
                        }

                        if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET))
                        {
                            db.CauHinhHeThongs.Add(new CauHinhHeThong
                            {
                                KhoaCauHinh = DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET,
                                GiaTriJson = JsonSerializer.Serialize(storeCu, jsonOpt),
                                MoTa = "Dữ liệu mở rộng Hàng hóa, Thương hiệu, Vị trí kho và Nhật ký FEFO KiotViet",
                                NgayCapNhat = DateTime.Now
                            });
                        }

                        if (storeCu.DanhSachPhieuThuChi != null)
                        {
                            foreach (var p in storeCu.DanhSachPhieuThuChi)
                            {
                                string maHienThi = string.IsNullOrWhiteSpace(p.MaPhieu) ? $"PTH-{DateTime.Now:yyMMddHHmmss}" : p.MaPhieu;
                                if (!await db.PhieuThuChis.AnyAsync(x => x.MaPhieuHienThi == maHienThi))
                                {
                                    db.PhieuThuChis.Add(new PhieuThuChi
                                    {
                                        MaPhieuHienThi = maHienThi,
                                        LoaiPhieu = string.IsNullOrWhiteSpace(p.LoaiPhieu) ? "THU" : p.LoaiPhieu,
                                        HangMuc = p.LoaiPhieu == "CHI" ? "Chi hoàn tiền đơn hàng" : "Thu tiền bán hàng",
                                        SoTien = p.SoTien,
                                        PhuongThuc = string.IsNullOrWhiteSpace(p.PhuongThuc) ? "MOMO_QR" : p.PhuongThuc,
                                        MaDonHangLienKet = p.MaDh,
                                        NguoiNopNhan = string.IsNullOrWhiteSpace(p.NguoiNopNhan) ? "Khách Hàng Nguyệt Cát" : p.NguoiNopNhan,
                                        GhiChu = p.NoiDung,
                                        NgayLap = p.ThoiGianTao == default ? DateTime.Now : p.ThoiGianTao
                                    });
                                }
                            }
                        }

                        if (storeCu.DonHangs != null)
                        {
                            foreach (var kvDh in storeCu.DonHangs)
                            {
                                if (kvDh.Value?.Thiep3DTheoToken == null) continue;
                                foreach (var kvThiep in kvDh.Value.Thiep3DTheoToken)
                                {
                                    string token = (kvThiep.Key ?? string.Empty).Trim();
                                    var thiepInfo = kvThiep.Value;
                                    if (!string.IsNullOrWhiteSpace(token) && thiepInfo != null &&
                                        !await db.ThiepChucMung3ds.AnyAsync(t => t.MaTokenQr == token))
                                    {
                                        int? maDot = await db.DotGiaoHangDaDiems
                                            .Where(d => d.MaTokenThiepQr == token)
                                            .Select(d => (int?)d.MaDotGh)
                                            .FirstOrDefaultAsync();

                                        db.ThiepChucMung3ds.Add(new ThiepChucMung3d
                                        {
                                            MaTokenQr = token,
                                            MaDonHang = kvDh.Key,
                                            MaDotGiao = maDot,
                                            TenNguoiGui = string.IsNullOrWhiteSpace(thiepInfo.TenNguoiGuiThiep) ? "Khách Hàng Nguyệt Cát" : thiepInfo.TenNguoiGuiThiep,
                                            TenNguoiNhan = string.IsNullOrWhiteSpace(thiepInfo.TenNguoiNhanThiep) ? "Quý Khách & Gia Đình" : thiepInfo.TenNguoiNhanThiep,
                                            LoiChuc = string.IsNullOrWhiteSpace(thiepInfo.LoiChucThiep)
                                                ? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!"
                                                : thiepInfo.LoiChucThiep,
                                            MauGiaoDien = string.IsNullOrWhiteSpace(thiepInfo.MauGiaoDienThiep) ? "CO_PHONG_HOANG_GIA" : thiepInfo.MauGiaoDienThiep,
                                            SoLanMoThiep = 0,
                                            NgayTao = DateTime.Now
                                        });
                                    }
                                }
                            }
                        }

                        await db.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Bỏ qua lỗi đọc file cũ nếu có
                }

                File.Delete(fileJsonCu);
            }

            if (Directory.Exists(thuMucDataCu))
            {
                Directory.Delete(thuMucDataCu, true);
            }

            if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO))
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO,
                    GiaTriJson = JsonSerializer.Serialize(new CauHinhThanhToanQr(), jsonOpt),
                    MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                    NgayCapNhat = DateTime.Now
                });
            }

            if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET))
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET,
                    GiaTriJson = JsonSerializer.Serialize(DichVuKiotVietMetadata.TaoDuLieuMacDinh(), jsonOpt),
                    MoTa = "Dữ liệu mở rộng Hàng hóa, Thương hiệu, Vị trí kho và Nhật ký FEFO KiotViet",
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
    }
}
