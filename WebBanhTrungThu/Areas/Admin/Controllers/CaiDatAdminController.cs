using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Roles = "QUAN_TRI,ADMIN")]
    public class CaiDatAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuCaiDatHeThong _dichVuCaiDat;

        public CaiDatAdminController(
            CSDLBanhTrungThuContext db,
            IDichVuCaiDatHeThong dichVuCaiDat) : base(db)
        {
            _dichVuCaiDat = dichVuCaiDat;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? muc = "momo")
        {
            var vm = await _dichVuCaiDat.XayDungTrangCaiDatAsync(muc);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuThanhToanMoMo(CauHinhThanhToanQr model)
        {
            var (thanhCong, thongBao) = await _dichVuCaiDat.LuuCauHinhMoMoAsync(model, LayMaNhanSuHienTai());
            TempData[thanhCong ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index), new { muc = "momo" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuCuaHang(CauHinhCuaHang model)
        {
            var (thanhCong, thongBao) = await _dichVuCaiDat.LuuCauHinhCuaHangAsync(model, LayMaNhanSuHienTai());
            TempData[thanhCong ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index), new { muc = "cuahang" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuVanChuyen(CauHinhVanChuyen model)
        {
            var (thanhCong, thongBao) = await _dichVuCaiDat.LuuCauHinhVanChuyenAsync(model, LayMaNhanSuHienTai());
            TempData[thanhCong ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index), new { muc = "vanchuyen" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuHoiVienVip(CauHinhHoiVienVip model)
        {
            var (thanhCong, thongBao) = await _dichVuCaiDat.LuuCauHinhHoiVienVipAsync(model, LayMaNhanSuHienTai());
            TempData[thanhCong ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index), new { muc = "hoivien" });
        }
    }
}
