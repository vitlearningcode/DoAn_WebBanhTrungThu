using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Roles = "QUAN_TRI,ADMIN")]
    public class NhapKhoAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuNhapKho _dichVuNhapKho;

        public NhapKhoAdminController(CSDLBanhTrungThuContext db, IDichVuNhapKho dichVuNhapKho) : base(db)
        {
            _dichVuNhapKho = dichVuNhapKho;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string locTrangThai = "ALL",
            string locThanhToan = "ALL",
            string locNhaCungCap = "ALL")
        {
            var vm = await _dichVuNhapKho.XayDungTrangNhapKhoAsync(tuKhoa, locTrangThai, locThanhToan, locNhaCungCap);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ChiTietJson(int id)
        {
            var data = await _dichVuNhapKho.LayChiTietPhieuNhapJsonAsync(id);
            if (data == null)
            {
                return JsonThatBai("Không tìm thấy phiếu nhập kho.");
            }
            return Json(new { thanhCong = true, duLieu = data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuPhieuNhap(LuuPhieuNhapRequest req)
        {
            var (ok, thongBao) = await _dichVuNhapKho.LuuPhieuNhapAsync(
                req,
                LayHoTenNhanSuHienTai(),
                LayMaNhanSuHienTai());

            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoanThanhPhieuTam(int maPn)
        {
            var (ok, thongBao) = await _dichVuNhapKho.HoanThanhPhieuTamAsync(maPn);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaHoacHuyPhieu(int maPn)
        {
            var (ok, thongBao) = await _dichVuNhapKho.XoaHoacHuyPhieuNhapAsync(maPn);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToanPhieuNhap(ThanhToanPhieuNhapRequest req)
        {
            var (ok, thongBao) = await _dichVuNhapKho.ThanhToanPhieuNhapAsync(req, LayHoTenNhanSuHienTai());
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }
    }
}
