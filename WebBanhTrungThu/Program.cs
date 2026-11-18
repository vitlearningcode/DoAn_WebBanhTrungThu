using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký MVC Controllers và Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Đăng ký các Service nghiệp vụ dùng chung toàn hệ thống (Đặt ngay dưới AddDbContext):
builder.Services.AddScoped<DichVuHinhAnh>();
builder.Services.AddScoped<DichVuGioHang>();
builder.Services.AddSingleton<DichVuKiotVietMetadata>();

// 2. Đăng ký DbContext kết nối tới SQL Server (25 bảng)
builder.Services.AddDbContext<CSDLBanhTrungThuContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ChuoiKetNoiCSDL")));

// 3. Cấu hình Cookie Authentication thuần (Thay thế Individual Accounts rườm rà)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "NguyetCat_ThanhVienAuth";
        options.LoginPath = "/TaiKhoan/DangNhap";
        options.LogoutPath = "/TaiKhoan/DangXuat";
        options.AccessDeniedPath = "/TaiKhoan/TuChoiTruyCap";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// 4. Đăng ký Session & HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Khởi tạo & Chuẩn hóa dữ liệu CSDL khi khởi động (Tạo 3 bảng vật lý mới, Migrate kiotviet_metadata.json, Hạn Voucher 2027 & Lô FEFO)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
        var db = context;

        // 0. Tự động tạo 3 bảng vật lý PhieuThuChi, ThiepChucMung3D, CauHinhHeThong trong SQL Server nếu chưa tồn tại
        context.Database.ExecuteSqlRaw(@"
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

        // 0B. Tự động Migrate dữ liệu từ wwwroot/data/kiotviet_metadata.json (nếu còn tồn tại) vào SQL Server rồi xóa vĩnh viễn file & thư mục
        string thuMucDataCu = Path.Combine(app.Environment.WebRootPath, "data");
        string fileJsonCu = Path.Combine(thuMucDataCu, "kiotviet_metadata.json");
        var jsonOpt = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        if (File.Exists(fileJsonCu))
        {
            try
            {
                string noiDungJsonCu = await File.ReadAllTextAsync(fileJsonCu);
                var storeCu = System.Text.Json.JsonSerializer.Deserialize<KiotMetadataStore>(noiDungJsonCu, jsonOpt);
                if (storeCu != null)
                {
                    if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO))
                    {
                        db.CauHinhHeThongs.Add(new WebBanhTrungThu.Models.Entities.CauHinhHeThong
                        {
                            KhoaCauHinh = DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO,
                            GiaTriJson = System.Text.Json.JsonSerializer.Serialize(storeCu.CauHinhQr ?? new CauHinhThanhToanQr(), jsonOpt),
                            MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                            NgayCapNhat = DateTime.Now
                        });
                    }

                    if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET))
                    {
                        db.CauHinhHeThongs.Add(new WebBanhTrungThu.Models.Entities.CauHinhHeThong
                        {
                            KhoaCauHinh = DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET,
                            GiaTriJson = System.Text.Json.JsonSerializer.Serialize(storeCu, jsonOpt),
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
                                db.PhieuThuChis.Add(new WebBanhTrungThu.Models.Entities.PhieuThuChi
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

                                    db.ThiepChucMung3ds.Add(new WebBanhTrungThu.Models.Entities.ThiepChucMung3d
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

        // 0C. Đảm bảo 3 bảng vật lý luôn có dữ liệu chuẩn trong SQL Server (kể cả sau khi file JSON đã xóa)
        if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO))
        {
            db.CauHinhHeThongs.Add(new WebBanhTrungThu.Models.Entities.CauHinhHeThong
            {
                KhoaCauHinh = DichVuKiotVietMetadata.KHOA_CAU_HINH_QR_MOMO,
                GiaTriJson = System.Text.Json.JsonSerializer.Serialize(new CauHinhThanhToanQr(), jsonOpt),
                MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                NgayCapNhat = DateTime.Now
            });
        }

        if (!await db.CauHinhHeThongs.AnyAsync(c => c.KhoaCauHinh == DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET))
        {
            db.CauHinhHeThongs.Add(new WebBanhTrungThu.Models.Entities.CauHinhHeThong
            {
                KhoaCauHinh = DichVuKiotVietMetadata.KHOA_METADATA_KIOTVIET,
                GiaTriJson = System.Text.Json.JsonSerializer.Serialize(DichVuKiotVietMetadata.TaoDuLieuMacDinh(), jsonOpt),
                MoTa = "Dữ liệu mở rộng Hàng hóa, Thương hiệu, Vị trí kho và Nhật ký FEFO KiotViet",
                NgayCapNhat = DateTime.Now
            });
        }

        // Đồng bộ các đợt giao hàng có Thiệp QR trong DotGiaoHang_DaDiem sang bảng vật lý dbo.ThiepChucMung3D
        var cacDotCoThiep = await db.DotGiaoHangDaDiems
            .Include(d => d.MaDhNavigation)
            .Where(d => d.CoKemThiepQr && d.MaTokenThiepQr != null && d.MaTokenThiepQr != "")
            .ToListAsync();

        foreach (var dot in cacDotCoThiep)
        {
            string token = dot.MaTokenThiepQr!.Trim();
            if (!await db.ThiepChucMung3ds.AnyAsync(t => t.MaTokenQr == token))
            {
                db.ThiepChucMung3ds.Add(new WebBanhTrungThu.Models.Entities.ThiepChucMung3d
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

        // Đồng bộ các đơn hàng đã thanh toán sang bảng vật lý dbo.PhieuThuChi nếu bảng đang trống
        if (!await db.PhieuThuChis.AnyAsync())
        {
            var cacDonDaThu = await db.DonHangs
                .Where(d => d.TrangThaiTt == "DA_TT" || d.TrangThaiTt == "DA_THANH_TOAN")
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            foreach (var dh in cacDonDaThu)
            {
                db.PhieuThuChis.Add(new WebBanhTrungThu.Models.Entities.PhieuThuChi
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

        // 1. Cho phép nhiều bản ghi DotGiaoHang_DaDiem có MaTokenThiepQR = NULL khi khách không chọn Thiệp 3D
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

        // 2. Đảm bảo các mã giảm giá (đặc biệt là TRANGTRON2026) có NgayBatDau <= DateTime.Now và NgayKetThuc >= 31/12/2027
        var mocHetHanChuan = new DateTime(2027, 12, 31, 23, 59, 59);
        var dsKm = await db.MaKhuyenMais.ToListAsync();
        bool coCapNhatKm = false;

        var trangTron = dsKm.FirstOrDefault(k => k.CodeKm == "TRANGTRON2026");
        if (trangTron == null)
        {
            db.MaKhuyenMais.Add(new WebBanhTrungThu.Models.Entities.MaKhuyenMai
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

        // 3. Đảm bảo các lô bánh đang bán (LoSanXuatBanh) có hạn sử dụng thực tế còn hạn so với ngày hiện tại
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
                    db.LoSanXuatBanhs.Add(new WebBanhTrungThu.Models.Entities.LoSanXuatBanh
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
    catch
    {
        // Bỏ qua nếu CSDL đang khởi tạo lần đầu
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 5. Phục vụ file tĩnh mặc định trong wwwroot
app.UseStaticFiles();

// 6. Ánh xạ thư mục trên ổ cứng máy tính (C:\Users\MinhDuc\KhoAnhBanhTrungThu) thành đường dẫn "/kho-anh-may"
var thuMucODia = builder.Configuration["CauHinhKhoAnh:ThuMucODiaMay"] ?? @"C:\Users\MinhDuc\KhoAnhBanhTrungThu";
if (!Directory.Exists(thuMucODia))
{
    Directory.CreateDirectory(thuMucODia);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(thuMucODia),
    RequestPath = builder.Configuration["CauHinhKhoAnh:DuongDanAo"] ?? "/kho-anh-may"
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// 7. Kích hoạt Middleware tự động cấp mã Phiên Khách Vãng Lai (Guest Cart)
app.UseMiddleware<PhienKhachVangLaiMiddleware>();

// 8. Cấu hình Route cho phân khu Admin (Areas) và trang khách hàng
app.MapControllerRoute(
    name: "admin_soquy",
    pattern: "Admin/SoQuy",
    defaults: new { area = "Admin", controller = "SoQuyAdmin", action = "Index" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
