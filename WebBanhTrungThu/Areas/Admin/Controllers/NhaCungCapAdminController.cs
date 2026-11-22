using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Policy = "ChinhSachQuanTriHeThong")]
    public class NhaCungCapAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuNhaCungCap _dichVuNhaCungCap;

        public NhaCungCapAdminController(CSDLBanhTrungThuContext db, IDichVuNhaCungCap dichVuNhaCungCap) : base(db)
        {
            _dichVuNhaCungCap = dichVuNhaCungCap;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string locLoaiNcc = "ALL",
            string locTrangThai = "ALL")
        {
            var vm = await _dichVuNhaCungCap.XayDungTrangNhaCungCapAsync(tuKhoa, locLoaiNcc, locTrangThai);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ChiTietJson(int id)
        {
            var data = await _dichVuNhaCungCap.LayChiTietNhaCungCapJsonAsync(id);
            if (data == null)
            {
                return JsonThatBai("Không tìm thấy thông tin nhà cung cấp.");
            }
            return Json(new { thanhCong = true, duLieu = data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuNhaCungCap(LuuNhaCungCapRequest req)
        {
            var (ok, thongBao) = await _dichVuNhaCungCap.LuuNhaCungCapAsync(req);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaNhaCungCap(int maNcc)
        {
            var (ok, thongBao) = await _dichVuNhaCungCap.XoaHoacNgungNhaCungCapAsync(maNcc);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KhoiPhucNhaCungCap(int maNcc)
        {
            var (ok, thongBao) = await _dichVuNhaCungCap.KhoiPhucNhaCungCapAsync(maNcc);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }
    }
}
