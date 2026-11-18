using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.QuanTri;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller mỏng nhẹ (< 6KB) điều phối Quản trị Đơn hàng Đại lý Nguyệt Cát ERP.
    /// Kế thừa từ ControllerQuanTriCoSo và ủy quyền toàn bộ nghiệp vụ sang DichVuQuanTriDonHang.
    /// </summary>
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class DonHangAdminController : ControllerQuanTriCoSo
    {
        private readonly DichVuQuanTriDonHang _dichVuQuanTriDonHang;

        public DonHangAdminController(CSDLBanhTrungThuContext db, DichVuQuanTriDonHang dichVuQuanTriDonHang) : base(db)
        {
            _dichVuQuanTriDonHang = dichVuQuanTriDonHang;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string? locTrangThai,
            string? locLoaiDon,
            string? locPhuongThuc,
            string? locThoiGian)
        {
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.LocTrangThai = locTrangThai ?? "ALL";
            ViewBag.LocLoaiDon = locLoaiDon ?? "ALL";
            ViewBag.LocPhuongThuc = locPhuongThuc ?? "ALL";
            ViewBag.LocThoiGian = locThoiGian ?? "ALL";

            var danhSach = await _dichVuQuanTriDonHang.LayDanhSachDonHangLocAsync(
                tuKhoa, locTrangThai, locLoaiDon, locPhuongThuc, locThoiGian);

            return View(danhSach);
        }

        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var vm = await _dichVuQuanTriDonHang.XayDungChiTietDonHangQrAsync(
                id, Request.Scheme, Request.Host.ToString(), asNoTracking: false);

            if (vm == null) return RedirectToAction(nameof(Index));
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> InHoaDon(int id)
        {
            var vm = await _dichVuQuanTriDonHang.XayDungChiTietDonHangQrAsync(
                id, Request.Scheme, Request.Host.ToString(), asNoTracking: true);

            if (vm == null) return RedirectToAction(nameof(Index));
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanChuyenKhoanQr(int maDh, string? returnUrl)
        {
            string? thongBao = await _dichVuQuanTriDonHang.XacNhanChuyenKhoanQrAsync(maDh, LayTenQuanTriVienDangNhap());
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;

            if (!string.IsNullOrWhiteSpace(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(
            int maDh,
            string? trangThaiDh,
            string? trangThaiTt,
            bool laAdminOverride = false,
            string? ghiChuDieuChinh = null,
            string? returnUrl = null)
        {
            if (laAdminOverride && !User.IsInRole("QUAN_TRI"))
            {
                TempData["ThongBaoAdmin"] = "⚠️ Chỉ Quản trị viên (QUAN_TRI) mới có quyền điều chỉnh trạng thái thủ công (Admin Override)!";
                return RedirectToAction(nameof(Index));
            }

            string? thongBao = await _dichVuQuanTriDonHang.CapNhatTrangThaiDonHangAsync(
                maDh, trangThaiDh, trangThaiTt, laAdminOverride, ghiChuDieuChinh, LayTenQuanTriVienDangNhap());

            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;

            if (!string.IsNullOrWhiteSpace(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDonVaHoanTien(
            int maDh,
            string? maNganHangHoan,
            string? soTaiKhoanHoan,
            string? tenChuTkHoan,
            string? lyDoHuy,
            string? returnUrl)
        {
            string? thongBao = await _dichVuQuanTriDonHang.HuyDonVaHoanTienAsync(
                maDh, maNganHangHoan, soTaiKhoanHoan, tenChuTkHoan, lyDoHuy, LayTenQuanTriVienDangNhap());

            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;

            if (!string.IsNullOrWhiteSpace(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }
    }
}
