using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.QuanTri;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class KhachHangAdminController : ControllerQuanTriCoSo
    {
        private readonly DichVuQuanTriBangGia _dichVuBangGia;

        public KhachHangAdminController(CSDLBanhTrungThuContext db, DichVuQuanTriBangGia dichVuBangGia) : base(db)
        {
            _dichVuBangGia = dichVuBangGia;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? tuKhoa, string? locHangTv)
        {
            var query = _db.NguoiDungs
                .AsNoTracking()
                .Include(u => u.DonHangs)
                .Include(u => u.SoDiaChis)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                query = query.Where(u => u.HoTen.Contains(tk) || u.Sdt.Contains(tk) || u.Email.Contains(tk));
            }

            if (!string.IsNullOrWhiteSpace(locHangTv) && locHangTv != "ALL")
            {
                query = query.Where(u => u.HangTv == locHangTv);
            }

            var vm = new KhachHangAdminViewModel
            {
                DanhSachThanhVien = await query.OrderByDescending(u => u.DiemTichLuy).ToListAsync(),
                DanhSachPhienGuest = await _db.PhienKhachVangLais
                    .AsNoTracking()
                    .Include(p => p.MaNdDaGopNavigation)
                    .Include(p => p.GioHangs)
                        .ThenInclude(g => g.ChiTietGioHangs)
                    .Include(p => p.DonHangs)
                    .OrderByDescending(p => p.NgayTao)
                    .Take(25)
                    .ToListAsync(),
                TuKhoa = tuKhoa,
                LocHangTv = locHangTv ?? "ALL"
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> KhuyenMai()
        {
            var vm = await _dichVuBangGia.LayDanhSachKhuyenMaiAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemVoucherMoi(ThemVoucherMoiRequest req)
        {
            TempData["ThongBaoAdmin"] = await _dichVuBangGia.ThemVoucherMoiAsync(req);
            return RedirectToAction(nameof(KhuyenMai));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatVoucher(int maKm, int slToiDa, DateTime ngayKetThuc, bool choPhepKhachVangLai)
        {
            string? thongBao = await _dichVuBangGia.CapNhatVoucherAsync(maKm, slToiDa, ngayKetThuc, choPhepKhachVangLai);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(KhuyenMai));
        }
    }
}
