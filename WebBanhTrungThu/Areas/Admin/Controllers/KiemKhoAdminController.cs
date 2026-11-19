using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Roles = "QUAN_TRI,ADMIN")]
    public class KiemKhoAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuKiemKho _dichVuKiemKho;

        public KiemKhoAdminController(CSDLBanhTrungThuContext db, IDichVuKiemKho dichVuKiemKho) : base(db)
        {
            _dichVuKiemKho = dichVuKiemKho;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? tuKhoa, string locTrangThai = "ALL")
        {
            var vm = await _dichVuKiemKho.XayDungTrangKiemKhoAsync(tuKhoa, locTrangThai);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ChiTietJson(int id)
        {
            var data = await _dichVuKiemKho.LayChiTietPhieuKiemJsonAsync(id);
            if (data == null)
            {
                return JsonThatBai("Không tìm thấy phiếu kiểm kho.");
            }
            return Json(new { thanhCong = true, duLieu = data });
        }

        [HttpGet]
        public async Task<IActionResult> TaiFileMauExcel(string dinhDang = "xlsx")
        {
            var (noiDung, contentType, tenFile) = await _dichVuKiemKho.XuatFileMauKiemKhoAsync(dinhDang);
            return File(noiDung, contentType, tenFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocFileExcel(IFormFile? fileExcel)
        {
            var kq = await _dichVuKiemKho.DocFileExcelKiemKhoAsync(fileExcel);
            return Json(kq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile? fileExcel, string? ghiChu, bool canBangNgay = false)
        {
            var (ok, thongBao) = await _dichVuKiemKho.ImportExcelTaoPhieuKiemKhoAsync(
                fileExcel,
                ghiChu,
                canBangNgay,
                LayHoTenNhanSuHienTai(),
                LayMaNhanSuHienTai());

            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuPhieuKiemKho(LuuPhieuKiemKhoRequest req)
        {
            var (ok, thongBao) = await _dichVuKiemKho.LuuPhieuKiemKhoAsync(
                req,
                LayHoTenNhanSuHienTai(),
                LayMaNhanSuHienTai());

            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CanBangKho(int maPkk)
        {
            var (ok, thongBao) = await _dichVuKiemKho.CanBangPhieuKiemKhoAsync(
                maPkk,
                LayHoTenNhanSuHienTai(),
                LayMaNhanSuHienTai());

            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaPhieuTam(int maPkk)
        {
            var (ok, thongBao) = await _dichVuKiemKho.XoaHoacHuyPhieuKiemKhoAsync(maPkk);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }
    }
}
