using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Policy = "ChinhSachQuanTriHeThong")]
    public class SoQuyAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuThongKeVaSoQuy _dichVuThongKeVaSoQuy;

        public SoQuyAdminController(CSDLBanhTrungThuContext db, IDichVuThongKeVaSoQuy dichVuThongKeVaSoQuy) : base(db)
        {
            _dichVuThongKeVaSoQuy = dichVuThongKeVaSoQuy;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? locPhuongThuc, string? locLoaiPhieu, string? tuKhoa)
        {
            var (vm, locHienTai) = await _dichVuThongKeVaSoQuy.XayDungSoQuyAsync(locPhuongThuc, locLoaiPhieu, tuKhoa);
            ViewBag.LocPhuongThuc = locHienTai;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LapPhieuThuChi(
            string loaiPhieu,
            string hangMuc,
            decimal soTien,
            string phuongThuc,
            string doiTuong,
            string? maTraCuuDh,
            string? ghiChu)
        {
            var (thanhCong, thongBao) = await _dichVuThongKeVaSoQuy.LapPhieuThuChiThuCongAsync(
                loaiPhieu, hangMuc, soTien, phuongThuc, doiTuong, maTraCuuDh, ghiChu);

            if (thanhCong)
            {
                TempData["ThongBaoAdmin"] = thongBao;
            }
            else
            {
                TempData["LoiAdmin"] = thongBao;
            }

            return RedirectToAction(nameof(Index));
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
