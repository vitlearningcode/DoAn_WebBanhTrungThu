using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Roles = "QUAN_TRI,ADMIN")]
    public class DashboardController : ControllerQuanTriCoSo
    {
        private readonly IDichVuThongKeVaSoQuy _dichVuThongKeVaSoQuy;

        public DashboardController(CSDLBanhTrungThuContext db, IDichVuThongKeVaSoQuy dichVuThongKeVaSoQuy) : base(db)
        {
            _dichVuThongKeVaSoQuy = dichVuThongKeVaSoQuy;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await _dichVuThongKeVaSoQuy.XayDungDashboardAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KhoiPhucDuLieuGoc()
        {
            TempData["ThongBaoAdmin"] = await _dichVuThongKeVaSoQuy.KhoiPhucDuLieuGocAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
