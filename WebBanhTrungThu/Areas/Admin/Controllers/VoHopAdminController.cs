using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản trị danh mục Vỏ hộp quà tặng và nhập/xuất hủy tồn kho vỏ hộp (chỉ dành cho ADMIN).
    /// </summary>
    [Authorize(Policy = "ChinhSachQuanTriHeThong")]
    public class VoHopAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuQuanTriVoHop _dichVuVoHop;

        public VoHopAdminController(CSDLBanhTrungThuContext db, IDichVuQuanTriVoHop dichVuVoHop) : base(db)
        {
            _dichVuVoHop = dichVuVoHop;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? tuKhoa, int? sucChua, string? trangThaiTon = "ALL")
        {
            var vm = await _dichVuVoHop.LayDuLieuTrangQuanTriAsync(tuKhoa, sucChua, trangThaiTon);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuMauVoHop(LuuMauVoHopRequest req)
        {
            var (ok, thongBao) = await _dichVuVoHop.LuuMauVoHopAsync(req);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenTrangThai(int maVh)
        {
            var (ok, thongBao) = await _dichVuVoHop.ChuyenTrangThaiVoHopAsync(maVh);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NhapXuatKhoVoHop(NhapXuatVoHopRequest req)
        {
            var (ok, thongBao) = await _dichVuVoHop.ThucHienNhapXuatVoHopAsync(
                req,
                LayMaNhanSuHienTai(),
                LayHoTenNhanSuHienTai());
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }
    }
}
