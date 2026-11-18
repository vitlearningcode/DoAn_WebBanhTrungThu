using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.QuanTri;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class SoQuyAdminController : ControllerQuanTriCoSo
    {
        private readonly DichVuThongKeVaSoQuy _dichVuThongKeVaSoQuy;

        public SoQuyAdminController(CSDLBanhTrungThuContext db, DichVuThongKeVaSoQuy dichVuThongKeVaSoQuy) : base(db)
        {
            _dichVuThongKeVaSoQuy = dichVuThongKeVaSoQuy;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? locPhuongThuc)
        {
            var (vm, locHienTai) = await _dichVuThongKeVaSoQuy.XayDungSoQuyAsync(locPhuongThuc);
            ViewBag.LocPhuongThuc = locHienTai;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatCauHinhQrMoMo(
            string soTaiKhoanMoMo,
            string tenChuTaiKhoan,
            string maNganHang,
            string maBinVietQr,
            string mauNoiDungCk,
            string? returnUrl)
        {
            TempData["ThongBaoAdmin"] = _dichVuThongKeVaSoQuy.CapNhatCauHinhQrMoMo(
                soTaiKhoanMoMo, tenChuTaiKhoan, maNganHang, maBinVietQr, mauNoiDungCk);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
