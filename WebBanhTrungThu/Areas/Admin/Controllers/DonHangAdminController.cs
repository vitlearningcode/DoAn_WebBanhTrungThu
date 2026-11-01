using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN")]
    public class DonHangAdminController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;

        public DonHangAdminController(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? tuKhoa, string? locTrangThai, string? locLoaiDon)
        {
            var query = _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                query = query.Where(d =>
                    d.MaTraCuuDh.Contains(tk) ||
                    d.HoTenNguoiMua.Contains(tk) ||
                    d.SdtnguoiMua.Contains(tk) ||
                    d.DotGiaoHangDaDiems.Any(g => g.TenNguoiNhan.Contains(tk) || g.SdtnguoiNhan.Contains(tk)));
            }

            if (!string.IsNullOrWhiteSpace(locTrangThai) && locTrangThai != "ALL")
            {
                query = query.Where(d => d.TrangThaiDh == locTrangThai);
            }

            if (locLoaiDon == "B2B")
            {
                query = query.Where(d => d.LaDonDoanhNghiepB2b);
            }
            else if (locLoaiDon == "B2C")
            {
                query = query.Where(d => !d.LaDonDoanhNghiepB2b);
            }
            else if (locLoaiDon == "GUEST")
            {
                query = query.Where(d => !d.MaNd.HasValue);
            }
            else if (locLoaiDon == "MEMBER")
            {
                query = query.Where(d => d.MaNd.HasValue);
            }

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.LocTrangThai = locTrangThai ?? "ALL";
            ViewBag.LocLoaiDon = locLoaiDon ?? "ALL";

            var danhSach = await query.OrderByDescending(d => d.NgayDat).ToListAsync();
            return View(danhSach);
        }

        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == id);

            if (donHang == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = TaoViewModelDonHangQr(donHang);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> InHoaDon(int id)
        {
            var donHang = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == id);

            if (donHang == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = TaoViewModelDonHangQr(donHang);
            return View(vm);
        }

        private DonHangThanhCongViewModel TaoViewModelDonHangQr(Models.Entities.DonHang donHang)
        {
            var vm = new DonHangThanhCongViewModel
            {
                DonHang = donHang
            };

            using var qrGenerator = new QRCodeGenerator();
            foreach (var dot in donHang.DotGiaoHangDaDiems)
            {
                string urlThiep = string.Empty;
                string qrBase64 = string.Empty;

                if (dot.CoKemThiepQr && !string.IsNullOrWhiteSpace(dot.MaTokenThiepQr))
                {
                    urlThiep = $"{Request.Scheme}://{Request.Host}/Thiep3D/Xem/{dot.MaTokenThiepQr}";
                    using QRCodeData qrCodeData = qrGenerator.CreateQrCode(urlThiep, QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new PngByteQRCode(qrCodeData);
                    byte[] qrCodeBytes = qrCode.GetGraphic(10);
                    qrBase64 = "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
                }

                vm.DanhSachDotGiaoQr.Add(new DotGiaoKemQrItem
                {
                    DotGiao = dot,
                    UrlThiep3D = urlThiep,
                    AnhQrBase64 = qrBase64
                });
            }

            return vm;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int maDh, string? trangThaiDh, string? trangThaiTt, string? returnUrl)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang != null)
            {
                if (!string.IsNullOrWhiteSpace(trangThaiDh))
                {
                    donHang.TrangThaiDh = trangThaiDh;
                    foreach (var dot in donHang.DotGiaoHangDaDiems)
                    {
                        dot.TrangThaiGiao = trangThaiDh;
                    }
                }

                if (!string.IsNullOrWhiteSpace(trangThaiTt))
                {
                    donHang.TrangThaiTt = trangThaiTt;
                }

                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã cập nhật hóa đơn #{donHang.MaTraCuuDh} -> Trạng thái: {donHang.TrangThaiDh} ({donHang.TrangThaiTt})!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
