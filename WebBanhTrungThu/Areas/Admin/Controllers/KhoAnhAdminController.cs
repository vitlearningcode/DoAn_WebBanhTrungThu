using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class KhoAnhAdminController : ControllerQuanTriCoSo
    {
        private readonly IWebHostEnvironment _env;
        private readonly IDichVuHinhAnh _dichVuHinhAnh;

        public KhoAnhAdminController(
            CSDLBanhTrungThuContext db,
            IWebHostEnvironment env,
            IDichVuHinhAnh dichVuHinhAnh) : base(db)
        {
            _env = env;
            _dichVuHinhAnh = dichVuHinhAnh;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await _dichVuHinhAnh.LayDuLieuKhoAnhAdminAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemAnhMoi(ThemAnhDaNguonRequest req)
        {
            TempData["ThongBaoAdmin"] = await _dichVuHinhAnh.ThemAnhDaNguonAsync(req, _env.WebRootPath);
            return RedirectToAction(nameof(Index));
        }
    }
}
