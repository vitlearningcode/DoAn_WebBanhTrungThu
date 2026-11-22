using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Policy = "ChinhSachQuanTriHeThong")]
    public class XuatKhoAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuXuatKho _dichVuXuatKho;

        public XuatKhoAdminController(CSDLBanhTrungThuContext db, IDichVuXuatKho dichVuXuatKho) : base(db)
        {
            _dichVuXuatKho = dichVuXuatKho;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string locLoaiXuat = "ALL",
            string locTrangThai = "ALL")
        {
            var vm = await _dichVuXuatKho.XayDungTrangXuatKhoAsync(tuKhoa, locLoaiXuat, locTrangThai);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoPhieuXuat(TaoPhieuXuatRequest req)
        {
            try
            {
                var (ok, thongBao) = await _dichVuXuatKho.TaoPhieuXuatAsync(
                    req,
                    LayHoTenNhanSuHienTai(),
                    LayMaNhanSuHienTai());
                TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            }
            catch (InvalidOperationException ex)
            {
                TempData["LoiAdmin"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyPhieuXuat(int maPx)
        {
            var (ok, thongBao) = await _dichVuXuatKho.HuyPhieuXuatAsync(maPx);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }
    }
}
