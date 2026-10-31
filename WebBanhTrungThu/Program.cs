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
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();