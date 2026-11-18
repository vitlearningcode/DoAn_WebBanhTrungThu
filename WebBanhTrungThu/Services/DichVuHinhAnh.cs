using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services
{
    /// <summary>
    /// Dịch vụ quản lý và ánh xạ từ điển hình ảnh đa nguồn (WebRoot, Ổ cứng, Google Drive), kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuHinhAnh : DichVuCoSo
    {
        public DichVuHinhAnh(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        /// <summary>
        /// Tải toàn bộ danh sách Kho ảnh, Liên kết, Bánh và Vỏ hộp phục vụ trang Quản trị Kho ảnh.
        /// </summary>
        public async Task<KhoAnhAdminViewModel> LayDuLieuKhoAnhAdminAsync()
        {
            return new KhoAnhAdminViewModel
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
        }

        /// <summary>
        /// Lưu hình ảnh mới từ 3 nguồn (WebRoot Upload, Ổ đĩa máy, Google Drive) và gắn liên kết sản phẩm trong 1 giao dịch ACID.
        /// </summary>
        public async Task<string> ThemAnhDaNguonAsync(ThemAnhDaNguonRequest req, string webRootPath)
        {
            string tenTep = ChuanHoaChuoi(req.TenTepGoc, "anh-banh-moi.png");
            string? duongDanCucBo = req.DuongDanCucBo?.Trim();
            string? duongDanDamMay = req.DuongDanDamMay?.Trim();
            string? maFileDrive = req.MaFileDrive?.Trim();

            // Nguồn 1: Upload trực tiếp vào wwwroot/images/banh
            if (req.LoaiLuuTru == 1 && req.TepAnhTaiLen != null && req.TepAnhTaiLen.Length > 0)
            {
                string thuMucLuu = Path.Combine(webRootPath, "images", "banh");
                if (!Directory.Exists(thuMucLuu))
                {
                    Directory.CreateDirectory(thuMucLuu);
                }

                string duoiFile = Path.GetExtension(req.TepAnhTaiLen.FileName);
                if (string.IsNullOrWhiteSpace(duoiFile)) duoiFile = ".png";
                string tenFileAnToan = "banh_" + LayThoiGianHeThong().ToString("yyyyMMdd_HHmmss") + duoiFile;
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
                TieuDeAlt = ChuanHoaChuoi(req.TieuDeAlt, "Hình ảnh sản phẩm Nguyệt Cát"),
                LoaiLuuTru = req.LoaiLuuTru,
                DuongDanCucBo = duongDanCucBo,
                DuongDanDamMay = duongDanDamMay,
                MaFileDrive = maFileDrive,
                KichThuocPixel = "800x800",
                NgayTaiLen = LayThoiGianHeThong()
            };

            await ThucThiTrongGiaoDichAsync(async () =>
            {
                _db.KhoHinhAnhs.Add(hinhMoi);
                await _db.SaveChangesAsync();

                // Nếu có chọn đối tượng gắn ảnh (BANH hoặc VOHOP), tạo LienKetHinhAnh
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
            });

            return $"Đã thêm hình ảnh mới #{hinhMoi.MaHa} (Nguồn {hinhMoi.LoaiLuuTru}) và gắn cho {req.LoaiDoiTuong} #{req.MaDoiTuong}!";
        }

        /// <summary>
        /// Lấy toàn bộ từ điển ảnh của danh sách Bánh/Vỏ hộp trong 1 lần truy vấn duy nhất (tránh lỗi N+1 Query).
        /// </summary>
        public async Task<Dictionary<string, string>> LayTuDienUrlAnhAsync()
        {
            var danhSachLK = await _db.LienKetHinhAnhs
                .Include(lk => lk.MaHaNavigation)
                .OrderBy(lk => lk.ThuTuHienThi)
                .ToListAsync();

            var ketQua = new Dictionary<string, string>();

            foreach (var lk in danhSachLK)
            {
                string key = $"{lk.LoaiDoiTuong}_{lk.MaDoiTuong}_{lk.VaiTroAnh}";
                if (!ketQua.ContainsKey(key) && lk.MaHaNavigation != null)
                {
                    ketQua[key] = lk.MaHaNavigation.UrlHienThi;
                }
            }

            return ketQua;
        }

        /// <summary>
        /// Hàm lấy nhanh URL ảnh từ Từ điển đã nạp (có sẵn ảnh Fallback nếu thiếu).
        /// </summary>
        public static string LayUrl(Dictionary<string, string>? tuDien, string loaiDoiTuong, int maDoiTuong, string vaiTroAnh)
        {
            if (tuDien != null)
            {
                string key = $"{loaiDoiTuong}_{maDoiTuong}_{vaiTroAnh}";
                if (tuDien.TryGetValue(key, out string? url) && !string.IsNullOrEmpty(url))
                {
                    return url;
                }

                string fallbackKey = $"{loaiDoiTuong}_{maDoiTuong}_TACH_NEN_SLOT";
                if (tuDien.TryGetValue(fallbackKey, out string? fallbackUrl))
                {
                    return fallbackUrl;
                }
            }

            return loaiDoiTuong == "VOHOP"
                ? "/images/default/vohop-mac-dinh.png"
                : "/images/default/banh-mac-dinh.png";
        }
    }
}
