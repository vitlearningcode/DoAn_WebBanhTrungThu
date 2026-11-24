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
    /// Phần chuẩn hóa dữ liệu hệ thống, dọn khóa cấu hình thừa, hạn dùng lô FEFO, thiệp 3D, phiếu thu chi và đường dẫn ảnh.
    /// </summary>
    public static partial class KhoiTaoCsdl
    {
        private static async Task DiTruDuLieuCauHinhMoRongAsync(CSDLBanhTrungThuContext db)
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

            // Dọn sạch mọi khóa JSON mở rộng cũ ngoài 4 khóa cài đặt hệ thống chuẩn
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
