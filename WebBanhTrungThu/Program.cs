using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.Services.GiaoDien;
using WebBanhTrungThu.Services.GioHang;
using WebBanhTrungThu.Services.QuanTri;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.Services.ThanhToan;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký MVC Controllers và Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 2. Đăng ký DbContext kết nối tới SQL Server (25 bảng)
builder.Services.AddDbContext<CSDLBanhTrungThuContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ChuoiKetNoiCSDL")));

// 3. Đăng ký các Service nghiệp vụ theo kiến trúc phân tầng rễ cây (Clean MVC 3 tầng)
builder.Services.AddScoped<DichVuHinhAnh>();
builder.Services.AddScoped<DichVuHoiVien>();
builder.Services.AddScoped<DichVuHopQuaTuChon>();
builder.Services.AddScoped<DichVuKhuyenMaiGioHang>();
builder.Services.AddScoped<DichVuGioHang>();
builder.Services.AddScoped<DichVuBaoMatCaptcha>();
builder.Services.AddScoped<DichVuTinhGiaDonHang>();
builder.Services.AddScoped<DichVuTonKhoFefo>();
builder.Services.AddScoped<DichVuThanhToan>();
builder.Services.AddScoped<DichVuQuanTriSanPham>();
builder.Services.AddScoped<DichVuQuanTriBangGia>();
builder.Services.AddScoped<DichVuQuanTriKhoFefo>();
builder.Services.AddScoped<DichVuQuanTriDonHang>();
builder.Services.AddScoped<DichVuBanHangPos>();
builder.Services.AddScoped<DichVuTrangChu>();
builder.Services.AddScoped<DichVuThongKeVaSoQuy>();
builder.Services.AddSingleton<DichVuCauHinhHeThong>();

// 4. Cấu hình Cookie Authentication thuần
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

// 5. Đăng ký Session & HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 6. Khởi tạo & Chuẩn hóa dữ liệu CSDL khi khởi động (Tách riêng sang Data/KhoiTaoCsdl.cs)
await KhoiTaoCsdl.KhoiTaoVaChuanHoaDuLieuAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 7. Ánh xạ thư mục kho ảnh trên ổ cứng máy tính thành đường dẫn "/kho-anh-may"
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

// 8. Kích hoạt Middleware tự động cấp mã Phiên Khách Vãng Lai (Guest Cart)
app.UseMiddleware<PhienKhachVangLaiMiddleware>();

// 9. Cấu hình Route cho phân khu Admin (Areas) và trang khách hàng
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
