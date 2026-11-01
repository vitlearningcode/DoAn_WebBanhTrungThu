using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class KhachHangAdminController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;

        public KhachHangAdminController(CSDLBanhTrungThuContext db)
        {
            _db = db;
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
            var vm = new KhuyenMaiAdminViewModel
            {
                DanhSachKhuyenMai = await _db.MaKhuyenMais
                    .AsNoTracking()
                    .OrderByDescending(k => k.NgayKetThuc)
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemVoucherMoi(ThemVoucherMoiRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CodeKm) || string.IsNullOrWhiteSpace(req.TenChuongTrinh))
            {
                TempData["ThongBaoAdmin"] = "Vui lòng nhập đầy đủ Mã CodeKM và Tên chương trình khuyến mãi!";
                return RedirectToAction(nameof(KhuyenMai));
            }

            string codeChuan = req.CodeKm.Trim().ToUpper();
            if (await _db.MaKhuyenMais.AnyAsync(k => k.CodeKm == codeChuan))
            {
                TempData["ThongBaoAdmin"] = $"Mã khuyến mãi '{codeChuan}' đã tồn tại!";
                return RedirectToAction(nameof(KhuyenMai));
            }

            var kmMoi = new MaKhuyenMai
            {
                CodeKm = codeChuan,
                TenChuongTrinh = req.TenChuongTrinh.Trim(),
                LoaiGiam = req.LoaiGiam,
                GiaTriGiam = req.GiaTriGiam,
                GiamToiDa = req.GiamToiDa,
                DonToiThieu = req.DonToiThieu,
                SltoiDa = req.SltoiDa > 0 ? req.SltoiDa : 100,
                SldaDung = 0,
                ChoPhepKhachVangLai = req.ChoPhepKhachVangLai,
                NgayBatDau = DateTime.Now,
                NgayKetThuc = req.NgayKetThuc ?? DateTime.Now.AddDays(45)
            };

            _db.MaKhuyenMais.Add(kmMoi);
            await _db.SaveChangesAsync();

            TempData["ThongBaoAdmin"] = $"Đã tạo mã khuyến mãi mới [{kmMoi.CodeKm}] - {kmMoi.TenChuongTrinh}!";
            return RedirectToAction(nameof(KhuyenMai));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatVoucher(int maKm, int slToiDa, DateTime ngayKetThuc, bool choPhepKhachVangLai)
        {
            var km = await _db.MaKhuyenMais.FindAsync(maKm);
            if (km != null)
            {
                km.SltoiDa = slToiDa;
                km.NgayKetThuc = ngayKetThuc;
                km.ChoPhepKhachVangLai = choPhepKhachVangLai;
                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã cập nhật cấu hình Voucher [{km.CodeKm}]!";
            }

            return RedirectToAction(nameof(KhuyenMai));
        }
    }
}

