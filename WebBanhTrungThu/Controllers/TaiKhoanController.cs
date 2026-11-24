using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    public class TaiKhoanController : ControllerKhachHangCoSo
    {
        private readonly IDichVuHoiVien _hv;
        private readonly IDichVuGioHang _gh;
        private readonly IDichVuBaoMatCaptcha _dichVuCaptcha;

        public TaiKhoanController(
            CSDLBanhTrungThuContext db,
            IDichVuHoiVien hv,
            IDichVuGioHang gh,
            IDichVuBaoMatCaptcha dichVuCaptcha) : base(db)
        {
            _hv = hv;
            _gh = gh;
            _dichVuCaptcha = dichVuCaptcha;
        }

        [HttpPost]
        public async Task<IActionResult> DangNhapAjax([FromBody] DangNhapAjaxRequest? req, [FromQuery] string? tenDangNhap, [FromQuery] string? matKhau)
        {
            var (ok, msg, nd) = await _hv.XacThucDangNhapAsync(req?.TenDangNhap ?? tenDangNhap, req?.MatKhau ?? matKhau);
            if (!ok || nd == null) return Json(new { thanhCong = false, thongBao = msg });
            await DangNhapVaGopGioAsync(nd);
            return JsonAuth(nd, msg, _hv.XacDinhDuongDanSauDangNhap(nd.MaVt));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(string? tenDangNhap, string? matKhau, string? returnUrl)
        {
            var (ok, msg, nd) = await _hv.XacThucDangNhapAsync(tenDangNhap, matKhau);
            if (!ok || nd == null) { TempData["LoiHoSo"] = msg; return RedirectToAction("Index", "Home"); }
            await DangNhapVaGopGioAsync(nd);
            string dich = _hv.XacDinhDuongDanSauDangNhap(nd.MaVt);
            if (!string.IsNullOrWhiteSpace(dich)) return Redirect(dich);
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenNhanhVaiTroDemo(string maVt, string? returnUrl)
        {
            var nd = await _hv.LayTaiKhoanChuyenVaiTroDemoAsync(maVt);
            if (nd != null) { await DangNhapVaGopGioAsync(nd); TempData["ThongBaoAdmin"] = $"Đã chuyển nhanh sang phiên làm việc: {nd.HoTen} ({Services.TaiKhoan.DichVuHoiVien.LayTenVaiTroHienThi(nd.MaVt)})!"; }
            return !string.IsNullOrWhiteSpace(returnUrl) ? Redirect(returnUrl) : Redirect("/Admin/DonHangAdmin/Index");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AnhCaptcha()
        {
            string svg = _dichVuCaptcha.TaoChuoiSvgCaptchaThuan(out string maCaptcha);
            HttpContext.Session.SetString("CaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("MaCaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("DaXacThucCaptcha", "false");

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml; charset=utf-8");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult LayMaCaptchaMoi()
        {
            string anhSvgBase64 = _dichVuCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session);
            return Json(new { thanhCong = true, anhCaptchaSvg = anhSvgBase64, thoiGianSinh = DateTime.Now.ToString("HH:mm:ss") });
        }

        [HttpPost, AllowAnonymous]
        public IActionResult XacThucCaptchaDangKy([FromBody] XacThucCaptchaMoRongRequest? req, string? maCaptchaNhap = null)
        {
            string maKhachNhap = (!string.IsNullOrWhiteSpace(maCaptchaNhap)
                ? maCaptchaNhap
                : !string.IsNullOrWhiteSpace(req?.MaCaptchaNhap)
                    ? req.MaCaptchaNhap
                    : req?.MaCaptcha ?? string.Empty).Trim();

            if (!_dichVuCaptcha.KiemTraCaptcha(HttpContext.Session, maKhachNhap))
            {
                return Json(new
                {
                    thanhCong = false,
                    loaiLoi = "CAPTCHA",
                    thongBao = "Mã xác thực Captcha không chính xác. Vui lòng nhập lại 5 ký tự trong hình!",
                    anhCaptchaSvg = _dichVuCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session)
                });
            }

            HttpContext.Session.SetString("DaXacThucCaptcha", "true");
            return Json(new { thanhCong = true, thongBao = "✓ Đã xác thực chống robot" });
        }

        [HttpPost]
        public async Task<IActionResult> DangKyAjax(
            [FromBody] DangKyAjaxRequest? req,
            [FromQuery] string? hoTen,
            [FromQuery] string? sdt,
            [FromQuery] string? email,
            [FromQuery] string? matKhau,
            [FromQuery] string? maCaptcha)
        {
            string maCaptchaNhap = (req?.MaCaptcha ?? maCaptcha ?? string.Empty).Trim();
            if (!_dichVuCaptcha.KiemTraCaptcha(HttpContext.Session, maCaptchaNhap))
            {
                return Json(new
                {
                    thanhCong = false,
                    loaiLoi = "CAPTCHA",
                    thongBao = "Mã xác thực Captcha chống robot không chính xác hoặc chưa nhập!",
                    anhCaptchaSvg = _dichVuCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session)
                });
            }

            var (ok, msg, nd) = await _hv.DangKyHoiVienMoiAsync(req?.HoTen ?? hoTen, req?.Sdt ?? sdt, req?.Email ?? email, req?.MatKhau ?? matKhau);
            if (!ok || nd == null) return Json(new { thanhCong = false, thongBao = msg });

            HttpContext.Session.Remove("CaptchaThanhToan");
            HttpContext.Session.Remove("MaCaptchaThanhToan");
            HttpContext.Session.Remove("DaXacThucCaptcha");

            await DangNhapVaGopGioAsync(nd);
            return JsonAuth(nd, msg);
        }

        [HttpGet] public IActionResult DangNhap() => RedirectToAction("Index", "Home");

        [HttpGet]
        public IActionResult TuChoiTruyCap()
        {
            string? maVt = User.FindFirstValue(ClaimTypes.Role);
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(maVt)
                && maVt != "QUAN_TRI" && maVt != "ADMIN" && maVt != "KHACH_TV" && maVt != "KHACH_HANG")
            {
                string dich = _hv.XacDinhDuongDanSauDangNhap(maVt);
                if (!string.IsNullOrWhiteSpace(dich)) return Redirect(dich);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> HoSo(string? tab)
        {
            if (LayMaNguoiDungDangNhap() is not int maNd) return RedirectToAction("Index", "Home");
            var vm = await _hv.XayDungHoSoThanhVienAsync(maNd, tab);
            if (vm == null) return RedirectToAction("Index", "Home");
            ViewBag.TabMacDinh = vm.TabMacDinh;
            return View(vm);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> HuyDonHang(int maDh, string? lyDoHuy) => ChayHoSoAsync("DON_HANG", async maNd =>
        {
            var (ok, msg) = await _hv.KhachHangHuyDonCodAsync(maNd, maDh, lyDoHuy);
            TempData[ok ? "ThongBaoHoSo" : "LoiHoSo"] = msg;
        });

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> ThemDiaChiMoi(string tenNguoiNhan, string sdtNguoiNhan, string tinhThanh, string quanHuyen, string phuongXa, string diaChiCuThe, string? nhanDiaChi, bool laMacDinh = false)
            => ChayHoSoAsync("DIA_CHI", async maNd => TempData["ThongBaoHoSo"] = await _hv.ThemDiaChiMoiAsync(maNd, tenNguoiNhan, sdtNguoiNhan, tinhThanh, quanHuyen, phuongXa, diaChiCuThe, nhanDiaChi, laMacDinh));

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> SuaDiaChi(int maDiaChi, string? nhanGoiNho, string nguoiNhan, string sdtNhan, string? tinhThanh, string? quanHuyen, string? phuongXa, string diaChiChiTiet, bool laMacDinh = false)
            => ChayHoSoAsync("DIA_CHI", async maNd => TempData["ThongBaoHoSo"] = (await _hv.CapNhatDiaChiAsync(maNd, maDiaChi, nhanGoiNho, nguoiNhan, sdtNhan, diaChiChiTiet, laMacDinh, tinhThanh, quanHuyen, phuongXa)).ThongBao);

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> DatDiaChiMacDinh(int maDiaChi)
            => ChayHoSoAsync("DIA_CHI", async maNd => TempData["ThongBaoHoSo"] = (await _hv.DatDiaChiMacDinhAsync(maNd, maDiaChi)).ThongBao);

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> XoaDiaChi(int maDc)
            => ChayHoSoAsync("DIA_CHI", async maNd => { var msg = await _hv.XoaDiaChiAsync(maNd, maDc); if (!string.IsNullOrWhiteSpace(msg)) TempData["ThongBaoHoSo"] = msg; });

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> DatLaiHopTuChon(int maHopTc)
            => ChayHoSoAsync("HOP_TU_CHON", async maNd => TempData["ThongBaoHoSo"] = await _hv.DatLaiHopTuChonVaoGioAsync(maNd, maHopTc));

        private JsonResult JsonAuth(NguoiDung nd, string msg, string redirectUrl = "")
            => Json(new { thanhCong = true, hoTen = nd.HoTen, maVt = nd.MaVt, hangTv = nd.HangTv, diem = nd.DiemTichLuy, redirectUrl, thongBao = msg });

        private async Task<IActionResult> ChayHoSoAsync(string tab, Func<int, Task> xuLy)
        {
            if (LayMaNguoiDungDangNhap() is not int maNd) return RedirectToAction("Index", "Home");
            await xuLy(maNd);
            return RedirectToAction(nameof(HoSo), new { tab });
        }

        private async Task DangNhapVaGopGioAsync(NguoiDung nd)
        {
            var scheme = CookieAuthenticationDefaults.AuthenticationScheme;
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, nd.MaNd.ToString()), new(ClaimTypes.Name, nd.HoTen), new(ClaimTypes.Email, nd.Email), new(ClaimTypes.Role, nd.MaVt), new("HangTV", nd.HangTv) };
            await HttpContext.SignInAsync(scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, scheme)), new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

            string? maKvl = LayMaPhienKhachVangLai();
            if (!string.IsNullOrWhiteSpace(maKvl)) { await _gh.GopGioHangAsync(maKvl, nd.MaNd); Response.Cookies.Delete(PhienKhachVangLaiMiddleware.TenCookieKVL); }
        }
    }
}
