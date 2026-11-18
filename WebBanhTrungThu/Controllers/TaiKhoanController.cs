using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    /// <summary>
    /// Controller mỏng nhẹ (< 7KB) điều phối xác thực Cookie Authentication, Hồ sơ thành viên và Sổ địa chỉ.
    /// Kế thừa từ ControllerKhachHangCoSo và ủy quyền nghiệp vụ sang DichVuHoiVien & DichVuGioHang.
    /// </summary>
    public class TaiKhoanController : ControllerKhachHangCoSo
    {
        private readonly DichVuHoiVien _dichVuHoiVien;
        private readonly DichVuGioHang _dichVuGioHang;

        public TaiKhoanController(
            CSDLBanhTrungThuContext db,
            DichVuHoiVien dichVuHoiVien,
            DichVuGioHang dichVuGioHang) : base(db)
        {
            _dichVuHoiVien = dichVuHoiVien;
            _dichVuGioHang = dichVuGioHang;
        }

        [HttpPost]
        public async Task<IActionResult> DangNhapAjax([FromBody] DangNhapAjaxRequest? req, [FromQuery] string? tenDangNhap, [FromQuery] string? matKhau)
        {
            var (hopLe, thongBao, nd) = await _dichVuHoiVien.XacThucDangNhapAsync(
                req?.TenDangNhap ?? tenDangNhap,
                req?.MatKhau ?? matKhau);

            if (!hopLe || nd == null) return Json(new { thanhCong = false, thongBao });

            await ThucHienDangNhapVaGopGioAsync(nd);
            return Json(new { thanhCong = true, hoTen = nd.HoTen, maVt = nd.MaVt, hangTv = nd.HangTv, diem = nd.DiemTichLuy, thongBao });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenNhanhVaiTroDemo(string maVt, string? returnUrl)
        {
            var nd = await _dichVuHoiVien.LayTaiKhoanChuyenVaiTroDemoAsync(maVt);
            if (nd != null)
            {
                await ThucHienDangNhapVaGopGioAsync(nd);
                TempData["ThongBaoAdmin"] = $"Đã chuyển nhanh sang phiên làm việc: {nd.HoTen} (Quyền: {nd.MaVt})!";
            }

            return !string.IsNullOrWhiteSpace(returnUrl) ? Redirect(returnUrl) : Redirect("/Admin/DonHangAdmin/Index");
        }

        [HttpPost]
        public async Task<IActionResult> DangKyAjax(
            [FromBody] DangKyAjaxRequest? req,
            [FromQuery] string? hoTen,
            [FromQuery] string? sdt,
            [FromQuery] string? email,
            [FromQuery] string? matKhau)
        {
            var (hopLe, thongBao, ndMoi) = await _dichVuHoiVien.DangKyHoiVienMoiAsync(
                req?.HoTen ?? hoTen, req?.Sdt ?? sdt, req?.Email ?? email, req?.MatKhau ?? matKhau);

            if (!hopLe || ndMoi == null) return Json(new { thanhCong = false, thongBao });

            await ThucHienDangNhapVaGopGioAsync(ndMoi);
            return Json(new { thanhCong = true, hoTen = ndMoi.HoTen, maVt = ndMoi.MaVt, hangTv = ndMoi.HangTv, diem = ndMoi.DiemTichLuy, thongBao });
        }

        [HttpGet]
        public IActionResult DangNhap() => RedirectToAction("Index", "Home");

        [HttpGet]
        public IActionResult TuChoiTruyCap() => RedirectToAction("Index", "Home");

        [HttpPost]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HoSo(string? tab)
        {
            int? maNd = LayMaNguoiDungDangNhap();
            if (!maNd.HasValue) return RedirectToAction("Index", "Home");

            var vm = await _dichVuHoiVien.XayDungHoSoThanhVienAsync(maNd.Value, tab);
            if (vm == null) return RedirectToAction("Index", "Home");

            ViewBag.TabMacDinh = vm.TabMacDinh;
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemDiaChiMoi(
            string tenNguoiNhan, string sdtNguoiNhan, string tinhThanh,
            string quanHuyen, string phuongXa, string diaChiCuThe,
            string? nhanDiaChi, bool laMacDinh = false)
        {
            int? maNd = LayMaNguoiDungDangNhap();
            if (!maNd.HasValue) return RedirectToAction("Index", "Home");

            TempData["ThongBaoHoSo"] = await _dichVuHoiVien.ThemDiaChiMoiAsync(
                maNd.Value, tenNguoiNhan, sdtNguoiNhan, tinhThanh, quanHuyen, phuongXa, diaChiCuThe, nhanDiaChi, laMacDinh);

            return RedirectToAction(nameof(HoSo), new { tab = "DIA_CHI" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDiaChi(int maDc)
        {
            int? maNd = LayMaNguoiDungDangNhap();
            if (!maNd.HasValue) return RedirectToAction("Index", "Home");

            string? thongBao = await _dichVuHoiVien.XoaDiaChiAsync(maNd.Value, maDc);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoHoSo"] = thongBao;

            return RedirectToAction(nameof(HoSo), new { tab = "DIA_CHI" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLaiHopTuChon(int maHopTc)
        {
            int? maNd = LayMaNguoiDungDangNhap();
            if (!maNd.HasValue) return RedirectToAction("Index", "Home");

            TempData["ThongBaoHoSo"] = await _dichVuHoiVien.DatLaiHopTuChonVaoGioAsync(maNd.Value, maHopTc);
            return RedirectToAction(nameof(HoSo), new { tab = "HOP_TU_CHON" });
        }

        private async Task ThucHienDangNhapVaGopGioAsync(NguoiDung nd)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, nd.MaNd.ToString()),
                new(ClaimTypes.Name, nd.HoTen),
                new(ClaimTypes.Email, nd.Email),
                new(ClaimTypes.Role, nd.MaVt),
                new("HangTV", nd.HangTv)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

            string? maPhienKVL = LayMaPhienKhachVangLai();
            if (!string.IsNullOrWhiteSpace(maPhienKVL))
            {
                await _dichVuGioHang.GopGioHangAsync(maPhienKVL, nd.MaNd);
                Response.Cookies.Delete(PhienKhachVangLaiMiddleware.TenCookieKVL);
            }
        }
    }
}
