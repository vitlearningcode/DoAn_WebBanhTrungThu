using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN")]
    public class KhoAnhAdminController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly IWebHostEnvironment _env;

        public KhoAnhAdminController(CSDLBanhTrungThuContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = new KhoAnhAdminViewModel
            {
                DanhSachHinhAnh = await _db.KhoHinhAnhs
                    .AsNoTracking()
                    .Include(h => h.LienKetHinhAnhs)
                    .OrderByDescending(h => h.MaHa)
                    .ToListAsync(),
                DanhSachLienKet = await _db.LienKetHinhAnhs
                    .AsNoTracking()
                    .Include(l => l.MaHaNavigation)
                    .OrderBy(l => l.LoaiDoiTuong)
                    .ThenBy(l => l.MaDoiTuong)
                    .ToListAsync(),
                DanhSachBanh = await _db.SanPhamBanhs.AsNoTracking().OrderBy(b => b.MaBanh).ToListAsync(),
                DanhSachVoHop = await _db.MauVoHops.AsNoTracking().OrderBy(v => v.MaVh).ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemAnhMoi(ThemAnhDaNguonRequest req)
        {
            string tenTep = string.IsNullOrWhiteSpace(req.TenTepGoc) ? "anh-banh-moi.png" : req.TenTepGoc.Trim();
            string? duongDanCucBo = req.DuongDanCucBo?.Trim();
            string? duongDanDamMay = req.DuongDanDamMay?.Trim();
            string? maFileDrive = req.MaFileDrive?.Trim();

            // Nguồn 1: Upload trực tiếp vào wwwroot/images/banh
            if (req.LoaiLuuTru == 1 && req.TepAnhTaiLen != null && req.TepAnhTaiLen.Length > 0)
            {
                string thuMucLuu = Path.Combine(_env.WebRootPath, "images", "banh");
                if (!Directory.Exists(thuMucLuu))
                {
                    Directory.CreateDirectory(thuMucLuu);
                }

                string duoiFile = Path.GetExtension(req.TepAnhTaiLen.FileName);
                if (string.IsNullOrWhiteSpace(duoiFile)) duoiFile = ".png";
                string tenFileAnToan = "banh_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + duoiFile;
                string duongDanVatLy = Path.Combine(thuMucLuu, tenFileAnToan);

                using (var stream = new FileStream(duongDanVatLy, FileMode.Create))
                {
                    await req.TepAnhTaiLen.CopyToAsync(stream);
                }

                tenTep = req.TepAnhTaiLen.FileName;
                duongDanCucBo = $"/images/banh/{tenFileAnToan}";
            }
            else if (req.LoaiLuuTru == 2)
            {
                // Nguồn 2: Ổ đĩa máy C:\Users\MinhDuc\KhoAnhBanhTrungThu ánh xạ qua /kho-anh-may/...
                if (string.IsNullOrWhiteSpace(duongDanCucBo))
                {
                    duongDanCucBo = "/kho-anh-may/banh-mac-dinh.png";
                }
                else if (!duongDanCucBo.StartsWith("/"))
                {
                    duongDanCucBo = "/kho-anh-may/" + duongDanCucBo;
                }
                tenTep = Path.GetFileName(duongDanCucBo);
            }
            else if (req.LoaiLuuTru == 3)
            {
                // Nguồn 3: Google Drive -> Tự động tách MaFileDrive từ link chia sẻ
                if (string.IsNullOrWhiteSpace(maFileDrive) && !string.IsNullOrWhiteSpace(duongDanDamMay))
                {
                    var match = Regex.Match(duongDanDamMay, @"(?:\/d\/|id=)([a-zA-Z0-9_-]{20,})");
                    if (match.Success)
                    {
                        maFileDrive = match.Groups[1].Value;
                    }
                }

                if (string.IsNullOrWhiteSpace(duongDanCucBo))
                {
                    duongDanCucBo = "/images/banh/fallback-lava.png";
                }
            }

            var hinhMoi = new KhoHinhAnh
            {
                TenTepGoc = tenTep,
                TieuDeAlt = string.IsNullOrWhiteSpace(req.TieuDeAlt) ? "Hình ảnh sản phẩm Nguyệt Cát" : req.TieuDeAlt.Trim(),
                LoaiLuuTru = req.LoaiLuuTru,
                DuongDanCucBo = duongDanCucBo,
                DuongDanDamMay = duongDanDamMay,
                MaFileDrive = maFileDrive,
                KichThuocPixel = "800x800",
                NgayTaiLen = DateTime.Now
            };

            _db.KhoHinhAnhs.Add(hinhMoi);
            await _db.SaveChangesAsync();

            // Nếu có chọn đối tượng gắn ảnh (BANH hoặc VOHOP), tạo hoặc cập nhật LienKetHinhAnh
            if (!string.IsNullOrWhiteSpace(req.LoaiDoiTuong) && req.MaDoiTuong > 0)
            {
                string vaiTro = string.IsNullOrWhiteSpace(req.VaiTroAnh)
                    ? (req.LoaiDoiTuong == "VOHOP" ? "KHAY_HOP_RONG" : "TACH_NEN_SLOT")
                    : req.VaiTroAnh;

                var lkMoi = new LienKetHinhAnh
                {
                    MaHa = hinhMoi.MaHa,
                    LoaiDoiTuong = req.LoaiDoiTuong,
                    MaDoiTuong = req.MaDoiTuong,
                    VaiTroAnh = vaiTro,
                    ThuTuHienThi = 1
                };
                _db.LienKetHinhAnhs.Add(lkMoi);
                await _db.SaveChangesAsync();
            }

            TempData["ThongBaoAdmin"] = $"Đã thêm hình ảnh mới #{hinhMoi.MaHa} (Nguồn {hinhMoi.LoaiLuuTru}) và gắn cho {req.LoaiDoiTuong} #{req.MaDoiTuong}!";
            return RedirectToAction(nameof(Index));
        }
    }
}

