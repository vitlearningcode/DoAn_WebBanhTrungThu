using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.Services.GiaoDien;
using WebBanhTrungThu.Services.GioHang;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.Nen;
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

// 2. Đăng ký DbContext kết nối tới SQL Server (37 bảng SQL-First)
builder.Services.AddDbContext<CSDLBanhTrungThuContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ChuoiKetNoiCSDL"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// 3. Đăng ký các Service nghiệp vụ theo chuẩn Dependency Inversion (Interface -> Implementation)
builder.Services.AddScoped<IDichVuHinhAnh, DichVuHinhAnh>();
builder.Services.AddScoped<IDichVuHoiVien, DichVuHoiVien>();
builder.Services.AddScoped<IDichVuHopQuaTuChon, DichVuHopQuaTuChon>();
builder.Services.AddScoped<IDichVuKhuyenMaiGioHang, DichVuKhuyenMaiGioHang>();
builder.Services.AddScoped<IDichVuGioHang, DichVuGioHang>();
builder.Services.AddScoped<IDichVuBaoMatCaptcha, DichVuBaoMatCaptcha>();
builder.Services.AddScoped<IDichVuTinhGiaDonHang, DichVuTinhGiaDonHang>();
builder.Services.AddScoped<IDichVuTonKhoFefo, DichVuTonKhoFefo>();
builder.Services.AddScoped<IDichVuCongThanhToanMoMo, DichVuCongThanhToanMoMo>();
builder.Services.AddScoped<IDichVuThanhToan, DichVuThanhToan>();
builder.Services.AddHostedService<DichVuNenHuyDonQuaHan>();
builder.Services.AddScoped<IDichVuQuanTriSanPham, DichVuQuanTriSanPham>();
builder.Services.AddScoped<IDichVuQuanTriBangGia, DichVuQuanTriBangGia>();
builder.Services.AddScoped<IDichVuQuanTriKhoFefo, DichVuQuanTriKhoFefo>();
builder.Services.AddScoped<IDichVuQuanTriDonHang, DichVuQuanTriDonHang>();
builder.Services.AddScoped<IDichVuBanHangPos, DichVuBanHangPos>();
builder.Services.AddScoped<IDichVuTrangChu, DichVuTrangChu>();
builder.Services.AddScoped<IDichVuChiTietSanPham, DichVuChiTietSanPham>();
builder.Services.AddScoped<IDichVuThongKeVaSoQuy, DichVuThongKeVaSoQuy>();
builder.Services.AddScoped<IDichVuCauHinhHeThong, DichVuCauHinhHeThong>();
builder.Services.AddScoped<IDichVuCaiDatHeThong, DichVuCaiDatHeThong>();
builder.Services.AddScoped<IDichVuNhapKho, DichVuNhapKho>();
builder.Services.AddScoped<IDichVuNhaCungCap, DichVuNhaCungCap>();
builder.Services.AddScoped<IDichVuXuatKho, DichVuXuatKho>();
builder.Services.AddScoped<IDichVuKiemKho, DichVuKiemKho>();
builder.Services.AddScoped<IDichVuQuanTriNhanVien, DichVuQuanTriNhanVien>();
builder.Services.AddScoped<IDichVuQuanTriVoHop, DichVuQuanTriVoHop>();

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

// 4.1. Đăng ký chính sách phân quyền Policy-Based Authorization (.NET 8)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ChinhSachQuanTriHeThong", policy =>
        policy.RequireRole("ADMIN", "QUAN_TRI"));

    options.AddPolicy("ChinhSachTacNghiepBanHang", policy =>
        policy.RequireRole("ADMIN", "QUAN_TRI", "NHAN_VIEN", "THU_NGAN", "NV_BAN_HANG", "GIAO_HANG"));
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
