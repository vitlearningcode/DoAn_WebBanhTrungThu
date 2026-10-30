using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;

namespace WebBanhTrungThu.Controllers
{
    public class HomeController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;

        public HomeController(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy thử mã phiên khách vãng lai vừa được Middleware cấp tự động
            ViewBag.MaPhienKVL = HttpContext.Items["MaPhienKVL"]?.ToString() ?? "Đã đăng nhập Member";

            // Đếm nhanh dữ liệu mẫu từ CSDL 25 bảng
            ViewBag.TongSoBanh = await _db.SanPhamBanhs.CountAsync();
            ViewBag.TongSoVoHop = await _db.MauVoHops.CountAsync();

            return View();
        }
    }
}