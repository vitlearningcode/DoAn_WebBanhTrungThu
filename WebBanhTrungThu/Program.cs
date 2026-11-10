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

// Khởi tạo & Chuẩn hóa dữ liệu CSDL khi khởi động (Hạn Voucher 2027, Lô sản xuất còn hạn & Filtered Index Thiệp QR)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

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
