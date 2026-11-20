using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller điều phối Quản trị Nhân viên &amp; Phân quyền 2 vai trò (Chủ cửa hàng / Nhân viên Bán hàng).
    /// </summary>
    [Authorize(Roles = "ADMIN,QUAN_TRI")]
    public class NhanVienAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuQuanTriNhanVien _dichVuNhanVien;

        public NhanVienAdminController(CSDLBanhTrungThuContext db, IDichVuQuanTriNhanVien dichVuNhanVien) : base(db)
        {
            _dichVuNhanVien = dichVuNhanVien;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string locVaiTro = "ALL",
            string locChiNhanh = "ALL",
            string locCaLamViec = "ALL",
            string locTrangThai = "ALL")
        {
            var vm = await _dichVuNhanVien.XayDungTrangNhanVienAsync(
                tuKhoa, locVaiTro, locChiNhanh, locCaLamViec, locTrangThai, LayMaNhanSuHienTai());
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ChiTietJson(int id)
        {
            var data = await _dichVuNhanVien.LayChiTietNhanVienJsonAsync(id);
            if (data == null)
            {
                return JsonThatBai("Không tìm thấy thông tin nhân viên.");
            }
            return Json(new { thanhCong = true, duLieu = data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuNhanVien(LuuNhanVienRequest req)
        {
            var (ok, thongBao) = await _dichVuNhanVien.LuuNhanVienAsync(req, LayMaNhanSuHienTai());
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLaiMatKhau(DatLaiMatKhauNvRequest req)
        {
            var (ok, thongBao) = await _dichVuNhanVien.DatLaiMatKhauAsync(req);
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenTrangThaiKhoa(int maNd)
        {
            var (ok, thongBao) = await _dichVuNhanVien.ChuyenTrangThaiKhoaAsync(maNd, LayMaNhanSuHienTai());
            TempData[ok ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }
    }
}
