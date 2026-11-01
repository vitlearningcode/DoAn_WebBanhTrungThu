using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class SanPhamAdminController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuHinhAnh _dichVuHinhAnh;
        private readonly DichVuKiotVietMetadata _kiotMeta;
        private readonly IWebHostEnvironment _env;
        private static bool _daKiemTraDanhMuc = false;

        public SanPhamAdminController(
            CSDLBanhTrungThuContext db,
            DichVuHinhAnh dichVuHinhAnh,
            DichVuKiotVietMetadata kiotMeta,
            IWebHostEnvironment env)
        {
            _db = db;
            _dichVuHinhAnh = dichVuHinhAnh;
            _kiotMeta = kiotMeta;
            _env = env;
        }

        private async Task DamBaoDanhMucKiotVietAsync()
        {
            if (_daKiemTraDanhMuc) return;

            bool coDongGia = await _db.DanhMucBanhs.AsNoTracking().AnyAsync(d => d.TenDm == "ĐỒNG GIÁ 20K" || d.DuongDanSlug == "dong-gia-20k");
            if (!coDongGia)
            {
                _db.DanhMucBanhs.Add(new DanhMucBanh
                {
                    TenDm = "ĐỒNG GIÁ 20K",
                    DuongDanSlug = "dong-gia-20k",
                    MoTa = "Nhóm hàng bánh lẻ và combo ưu đãi đồng giá KiotViet",
                    ThuTuSx = 10,
                    TrangThai = true
                });
                await _db.SaveChangesAsync();
            }
            _daKiemTraDanhMuc = true;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            int? maDm,
            string? tenDm,
            string? locDacTinh,
            string? locTonKho,
            string? duKienHetHang,
            string? thoiGianTao,
            string? nhaCungCap,
            string? thuongHieu,
            string? viTriKho,
            string? loaiHang,
            string? banTrucTiep)
        {
            await DamBaoDanhMucKiotVietAsync();

            var store = _kiotMeta.DocDuLieu();
            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();
            var danhSachDanhMuc = await _db.DanhMucBanhs.AsNoTracking().Where(d => d.TrangThai).OrderBy(d => d.ThuTuSx).ToListAsync();
            var danhSachVoHop = await _db.MauVoHops.AsNoTracking().OrderBy(v => v.MaVh).ToListAsync();
            var danhSachLo = await _db.LoSanXuatBanhs.AsNoTracking().Include(l => l.MaBanhNavigation).OrderBy(l => l.HanSuDung).ToListAsync();

            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .OrderBy(b => b.MaBanh)
                .ToListAsync();

            var dsCombo = await _db.ComboMauCoSans
                .AsNoTracking()
                .Include(c => c.MaVhNavigation)
                .Include(c => c.ChiTietComboMaus)
                    .ThenInclude(ct => ct.MaBanhNavigation)
                .OrderBy(c => c.MaCb)
                .ToListAsync();

            var danhSachHangHoa = new List<HangHoaKiotItemViewModel>();

            // 1. Ánh xạ Bánh lẻ (Hàng hóa thường)
            foreach (var b in dsBanh)
            {
                _kiotMeta.GanMetadataChoBanh(b, store);
                string keyMeta = $"BANH_{b.MaBanh}";
                store.Items.TryGetValue(keyMeta, out var metaItem);

                string urlAnh = DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", b.MaBanh, "ANH_CHINH");
                if (metaItem != null && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                {
                    urlAnh = metaItem.DanhSachAnh[0];
                }

                var listAnh = new List<string> { urlAnh };
                if (metaItem != null && metaItem.DanhSachAnh.Count > 1)
                {
                    listAnh.AddRange(metaItem.DanhSachAnh.Skip(1));
                }

                var loGanNhat = b.LoSanXuatBanhs.OrderBy(l => l.HanSuDung).FirstOrDefault();
                string duKienTxt = b.TongTonKho <= 0
                    ? "Đã hết hàng"
                    : (loGanNhat != null
                        ? $"{Math.Max(1, loGanNhat.HanSuDung.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber)} ngày ({loGanNhat.HanSuDung:dd/MM})"
                        : "---");

                danhSachHangHoa.Add(new HangHoaKiotItemViewModel
                {
                    IdGoc = b.MaBanh,
                    LaCombo = false,
                    MaHang = b.MaSku,
                    MaVach = b.MaVach,
                    TenHang = b.TenBanh,
                    MaDm = b.MaDm,
                    TenNhomHang = b.MaDmNavigation?.TenDm ?? "Bánh Nướng",
                    ThuongHieu = b.ThuongHieu,
                    NhaCungCap = b.NhaCungCap,
                    ViTriKho = b.ViTriKho,
                    DonViTinh = b.DonViTinh,
                    GiaBan = Math.Round(b.DonGiaLe, 0),
                    GiaVon = Math.Round(b.GiaVon, 0),
                    TonKho = b.TongTonKho,
                    DuKienHetHang = duKienTxt,
                    TrongLuongGram = b.TrongLuongGram,
                    BanTrucTiep = b.BanTrucTiep,
                    LaBanhChay = b.LaBanhChay,
                    LaBanhThuongHang = b.LaBanhThuongHang,
                    MucDoNgot = b.MucDoNgot,
                    SoTrungMuoi = b.SoTrungMuoi,
                    MoTaNgan = b.MoTaNgan,
                    CauChuyenBanh = b.CauChuyenBanh,
                    NangLuongCalo = b.ThongSoDinhDuong?.NangLuongCalo ?? 380,
                    LuongDuongGram = Math.Round(b.ThongSoDinhDuong?.LuongDuongGram ?? 14m, 0),
                    ChiSoDuongHuyetGi = b.ThongSoDinhDuong?.ChiSoDuongHuyetGi ?? 52,
                    CanhBaoDiUng = b.ThongSoDinhDuong?.CanhBaoDiUng ?? "Không",
                    UrlAnhChinh = urlAnh,
                    DanhSachAnh = listAnh,
                    DanhSachLo = b.LoSanXuatBanhs.OrderBy(l => l.HanSuDung).ToList()
                });
            }

            // 2. Ánh xạ Combo - đóng gói (Ưu tiên lấy ảnh chiếc bánh đầu tiên trong Combo hoặc ảnh vỏ hộp mặc định)
            foreach (var cb in dsCombo)
            {
                _kiotMeta.GanMetadataChoCombo(cb, store);
                string keyMeta = $"COMBO_{cb.MaCb}";
                store.Items.TryGetValue(keyMeta, out var metaItem);

                var banhDauTien = cb.ChiTietComboMaus.OrderBy(x => x.ViTriO).FirstOrDefault();
                string urlAnhCombo;
                if (metaItem != null && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                {
                    urlAnhCombo = metaItem.DanhSachAnh[0];
                }
                else if (banhDauTien != null)
                {
                    urlAnhCombo = DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", banhDauTien.MaBanh, "TACH_NEN_SLOT");
                }
                else
                {
                    urlAnhCombo = DichVuHinhAnh.LayUrl(tuDienAnh, "VOHOP", cb.MaVh, "KHAY_HOP_RONG");
                }

                var hangThanhPhans = new List<HangThanhPhanItemViewModel>();
                foreach (var ct in cb.ChiTietComboMaus.OrderBy(x => x.ViTriO))
                {
                    if (ct.MaBanhNavigation != null)
                    {
                        var existingTp = hangThanhPhans.FirstOrDefault(x => x.MaBanh == ct.MaBanh);
                        if (existingTp != null)
                        {
                            existingTp.SoLuong += ct.SoLuong;
                        }
                        else
                        {
                            hangThanhPhans.Add(new HangThanhPhanItemViewModel
                            {
                                MaBanh = ct.MaBanh,
                                MaHang = ct.MaBanhNavigation.MaSku,
                                TenHang = ct.MaBanhNavigation.TenBanh,
                                SoLuong = ct.SoLuong,
                                GiaVon = Math.Round(ct.MaBanhNavigation.GiaVon, 0),
                                GiaBan = Math.Round(ct.MaBanhNavigation.DonGiaLe, 0)
                            });
                        }
                    }
                }

                decimal tongGiaVonCombo = hangThanhPhans.Sum(x => x.TongGiaVon);
                if (tongGiaVonCombo <= 0)
                {
                    tongGiaVonCombo = Math.Round(cb.GiaUuDaiCombo * 0.6m, 0);
                }

                var dmTim = danhSachDanhMuc.FirstOrDefault(d => d.MaDm == cb.MaDm)
                            ?? danhSachDanhMuc.FirstOrDefault(d => d.TenDm == cb.TenDm);

                danhSachHangHoa.Add(new HangHoaKiotItemViewModel
                {
                    IdGoc = cb.MaCb,
                    LaCombo = true,
                    MaHang = cb.MaCodeCb,
                    MaVach = cb.MaVach,
                    TenHang = cb.TenCombo,
                    MaDm = dmTim?.MaDm ?? (danhSachDanhMuc.FirstOrDefault()?.MaDm ?? 1),
                    TenNhomHang = dmTim?.TenDm ?? cb.TenDm,
                    ThuongHieu = cb.ThuongHieu,
                    NhaCungCap = cb.NhaCungCap,
                    ViTriKho = cb.ViTriKho,
                    DonViTinh = cb.DonViTinh,
                    GiaBan = Math.Round(cb.GiaUuDaiCombo, 0),
                    GiaVon = Math.Round(tongGiaVonCombo, 0),
                    TonKho = cb.TongTonKho,
                    DuKienHetHang = "42 ngày",
                    TrongLuongGram = cb.TrongLuongGram,
                    BanTrucTiep = cb.BanTrucTiep,
                    MoTaNgan = cb.ThongDiepYngia,
                    MaVhCombo = cb.MaVh,
                    TenVoHopCombo = cb.MaVhNavigation?.TenVoHop,
                    UrlAnhChinh = urlAnhCombo,
                    DanhSachAnh = new List<string> { urlAnhCombo },
                    HangThanhPhans = hangThanhPhans
                });
            }

            // 3. Áp dụng bộ lọc trên URL (nếu được truyền từ query string)
            IEnumerable<HangHoaKiotItemViewModel> query = danhSachHangHoa;

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.MaHang.ToLowerInvariant().Contains(tk) ||
                    x.TenHang.ToLowerInvariant().Contains(tk) ||
                    x.MaVach.ToLowerInvariant().Contains(tk) ||
                    x.ThuongHieu.ToLowerInvariant().Contains(tk));
            }

            if (maDm.HasValue && maDm.Value > 0)
            {
                query = query.Where(x => x.MaDm == maDm.Value);
            }
            else if (!string.IsNullOrWhiteSpace(tenDm) && tenDm != "ALL")
            {
                query = query.Where(x => string.Equals(x.TenNhomHang, tenDm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(locTonKho) && locTonKho != "ALL")
            {
                switch (locTonKho)
                {
                    case "DUOI_DINH_MUC":
                    case "SAP_HET":
                        query = query.Where(x => x.TonKho > 0 && x.TonKho <= 30);
                        break;
                    case "VUOT_DINH_MUC":
                        query = query.Where(x => x.TonKho > 100);
                        break;
                    case "CON_HANG":
                        query = query.Where(x => x.TonKho > 0);
                        break;
                    case "HET_HANG":
                        query = query.Where(x => x.TonKho <= 0);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(nhaCungCap) && nhaCungCap != "ALL")
            {
                query = query.Where(x => string.Equals(x.NhaCungCap, nhaCungCap, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(thuongHieu) && thuongHieu != "ALL")
            {
                query = query.Where(x => string.Equals(x.ThuongHieu, thuongHieu, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(viTriKho) && viTriKho != "ALL")
            {
                query = query.Where(x => string.Equals(x.ViTriKho, viTriKho, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(loaiHang) && loaiHang != "ALL")
            {
                if (loaiHang == "BANH_LE")
                {
                    query = query.Where(x => !x.LaCombo);
                }
                else if (loaiHang == "COMBO")
                {
                    query = query.Where(x => x.LaCombo);
                }
            }

            if (!string.IsNullOrWhiteSpace(banTrucTiep) && banTrucTiep != "ALL")
            {
                if (banTrucTiep == "CO")
                {
                    query = query.Where(x => x.BanTrucTiep);
                }
                else if (banTrucTiep == "KHONG")
                {
                    query = query.Where(x => !x.BanTrucTiep);
                }
            }

            if (!string.IsNullOrWhiteSpace(locDacTinh) && locDacTinh != "ALL")
            {
                if (locDacTinh == "ISOMALT")
                    query = query.Where(x => x.MucDoNgot == "ISOMALT_TIEUDUONG");
                else if (locDacTinh == "CHAY")
                    query = query.Where(x => x.LaBanhChay);
                else if (locDacTinh == "VIP")
                    query = query.Where(x => x.LaBanhThuongHang);
            }

            var vm = new SanPhamAdminViewModel
            {
                DanhSachHangHoa = query.ToList(),
                DanhSachBanh = dsBanh,
                DanhSachCombo = dsCombo,
                DanhSachDanhMuc = danhSachDanhMuc,
                DanhSachVoHop = danhSachVoHop,
                DanhSachLoSanXuat = danhSachLo,
                TuDienAnh = tuDienAnh,
                DanhSachThuongHieu = store.DanhSachThuongHieu,
                DanhSachNhaCungCap = store.DanhSachNhaCungCap,
                DanhSachViTriKho = store.DanhSachViTriKho,
                DanhSachDonViTinh = store.DanhSachDonViTinh,
                TuKhoa = tuKhoa,
                MaDmLoc = maDm,
                TenDmLoc = tenDm,
                LocDacTinh = locDacTinh ?? "ALL",
                LocTonKho = locTonKho ?? "ALL",
                DuKienHetHang = duKienHetHang ?? "ALL",
                ThoiGianTao = thoiGianTao ?? "ALL",
                NhaCungCapLoc = nhaCungCap ?? "ALL",
                ThuongHieuLoc = thuongHieu ?? "ALL",
                ViTriKhoLoc = viTriKho ?? "ALL",
                LoaiHangLoc = loaiHang ?? "ALL",
                BanTrucTiepLoc = banTrucTiep ?? "ALL"
            };

            return View(vm);
        }

        // =========================================================================
        // LƯU HÀNG HÓA / COMBO - ĐÓNG GÓI TỪ MODAL KIOTVIET (TẠO MỚI HOẶC CẬP NHẬT)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuHangHoaKiot(LuuHangHoaKiotRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.TenHang))
            {
                TempData["ThongBaoAdmin"] = "Vui lòng nhập Tên hàng!";
                return RedirectToAction(nameof(Index));
            }

            var store = _kiotMeta.DocDuLieu();

            string thuongHieu = string.IsNullOrWhiteSpace(req.ThuongHieu) ? "Đồng Khánh" : req.ThuongHieu.Trim();
            string nhaCungCap = string.IsNullOrWhiteSpace(req.NhaCungCap) ? "Lò Bánh Đồng Khánh Chính Gốc" : req.NhaCungCap.Trim();
            string viTriKho = string.IsNullOrWhiteSpace(req.ViTriKho) ? "Kệ A1" : req.ViTriKho.Trim();
            string donViTinh = string.IsNullOrWhiteSpace(req.DonViTinh) ? (req.LaCombo ? "Hộp" : "Cái") : req.DonViTinh.Trim();

            if (!store.DanhSachThuongHieu.Contains(thuongHieu))
                store.DanhSachThuongHieu.Add(thuongHieu);
            if (!store.DanhSachNhaCungCap.Contains(nhaCungCap))
                store.DanhSachNhaCungCap.Add(nhaCungCap);
            if (!store.DanhSachViTriKho.Contains(viTriKho))
                store.DanhSachViTriKho.Add(viTriKho);
            if (!store.DanhSachDonViTinh.Contains(donViTinh))
                store.DanhSachDonViTinh.Add(donViTinh);

            int trongLuongGram = req.DonViTrongLuong == "kg"
                ? Math.Max(50, req.TrongLuong * 1000)
                : Math.Max(50, req.TrongLuong);

            // Xử lý ảnh tải lên hoặc đường dẫn ảnh nhập tay
            string? urlAnhMoi = null;
            if (req.FileAnhTaiLen != null && req.FileAnhTaiLen.Length > 0)
            {
                string ext = Path.GetExtension(req.FileAnhTaiLen.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                string fileName = $"kv-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}{ext}";
                string dirPath = Path.Combine(_env.WebRootPath, "images", "banh");
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                string fullPath = Path.Combine(dirPath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await req.FileAnhTaiLen.CopyToAsync(stream);
                }
                urlAnhMoi = $"/images/banh/{fileName}";
            }
            else if (!string.IsNullOrWhiteSpace(req.UrlAnhNhapTay))
            {
                urlAnhMoi = req.UrlAnhNhapTay.Trim();
            }

            // Parse danh sách Hàng thành phần từ JSON (nếu có)
            var dsThanhPhan = new List<HangThanhPhanItemViewModel>();
            if (!string.IsNullOrWhiteSpace(req.HangThanhPhanJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<HangThanhPhanItemViewModel>>(
                        req.HangThanhPhanJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null)
                    {
                        dsThanhPhan = parsed.Where(x => x.MaBanh > 0 && x.SoLuong > 0).ToList();
                    }
                }
                catch
                {
                    // Bỏ qua nếu JSON rỗng hoặc không hợp lệ
                }
            }

            if (req.LaCombo)
            {
                ComboMauCoSan? combo = null;
                if (req.IdGoc > 0)
                {
                    combo = await _db.ComboMauCoSans
                        .Include(c => c.ChiTietComboMaus)
                        .FirstOrDefaultAsync(c => c.MaCb == req.IdGoc);
                }

                if (combo == null)
                {
                    string maCode = string.IsNullOrWhiteSpace(req.MaHang)
                        ? $"CB{Random.Shared.Next(100000, 999999)}"
                        : req.MaHang.Trim().ToUpperInvariant();

                    if (await _db.ComboMauCoSans.AnyAsync(c => c.MaCodeCb == maCode))
                    {
                        maCode += Random.Shared.Next(10, 99);
                    }

                    int maVhMacDinh = req.MaVhCombo ?? (await _db.MauVoHops.Select(v => v.MaVh).FirstOrDefaultAsync());
                    if (maVhMacDinh <= 0) maVhMacDinh = 1;

                    combo = new ComboMauCoSan
                    {
                        MaCodeCb = maCode,
                        TenCombo = req.TenHang.Trim(),
                        MaVh = maVhMacDinh,
                        GiaUuDaiCombo = Math.Max(0, Math.Round(req.GiaBan, 0)),
                        GiaNiemYet = Math.Max(Math.Round(req.GiaBan, 0), Math.Round(req.GiaVon * 1.3m, 0)),
                        ThongDiepYngia = string.IsNullOrWhiteSpace(req.MoTaNgan) ? "Combo hộp quà Trung Thu tuyển chọn" : req.MoTaNgan.Trim(),
                        TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE",
                        TrangThai = req.BanTrucTiep
                    };
                    _db.ComboMauCoSans.Add(combo);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(req.MaHang))
                    {
                        combo.MaCodeCb = req.MaHang.Trim();
                    }
                    combo.TenCombo = req.TenHang.Trim();
                    combo.GiaUuDaiCombo = Math.Max(0, Math.Round(req.GiaBan, 0));
                    if (combo.GiaNiemYet < combo.GiaUuDaiCombo)
                    {
                        combo.GiaNiemYet = combo.GiaUuDaiCombo;
                    }
                    if (!string.IsNullOrWhiteSpace(req.MoTaNgan))
                    {
                        combo.ThongDiepYngia = req.MoTaNgan.Trim();
                    }
                    if (req.MaVhCombo.HasValue && req.MaVhCombo.Value > 0)
                    {
                        combo.MaVh = req.MaVhCombo.Value;
                    }
                    combo.TrangThai = req.BanTrucTiep;
                    await _db.SaveChangesAsync();
                }

                // Cập nhật Hàng thành phần (ChiTietComboMau)
                if (dsThanhPhan.Count > 0)
                {
                    var oldItems = await _db.ChiTietComboMaus.Where(x => x.MaCb == combo.MaCb).ToListAsync();
                    if (oldItems.Count > 0)
                    {
                        _db.ChiTietComboMaus.RemoveRange(oldItems);
                    }

                    int viTri = 1;
                    foreach (var tp in dsThanhPhan)
                    {
                        _db.ChiTietComboMaus.Add(new ChiTietComboMau
                        {
                            MaCb = combo.MaCb,
                            MaBanh = tp.MaBanh,
                            ViTriO = viTri++,
                            SoLuong = tp.SoLuong
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                var dmChon = await _db.DanhMucBanhs.FindAsync(req.MaDm);
                string keyCombo = $"COMBO_{combo.MaCb}";
                if (!store.Items.TryGetValue(keyCombo, out var metaCombo))
                {
                    metaCombo = new KiotItemMeta { Key = keyCombo };
                    store.Items[keyCombo] = metaCombo;
                }
                metaCombo.MaVach = string.IsNullOrWhiteSpace(req.MaVach) ? $"8934680990{combo.MaCb:D3}" : req.MaVach.Trim();
                metaCombo.ThuongHieu = thuongHieu;
                metaCombo.NhaCungCap = nhaCungCap;
                metaCombo.ViTriKho = viTriKho;
                metaCombo.DonViTinh = donViTinh;
                metaCombo.BanTrucTiep = req.BanTrucTiep;
                metaCombo.MaDmCombo = dmChon?.MaDm ?? req.MaDm;
                metaCombo.TenDmCombo = dmChon?.TenDm ?? "ĐỒNG GIÁ 20K";
                metaCombo.TonKhoCombo = Math.Max(0, req.TonKho);
                metaCombo.TrongLuongComboGram = trongLuongGram;
                if (!string.IsNullOrWhiteSpace(urlAnhMoi))
                {
                    metaCombo.DanhSachAnh.Insert(0, urlAnhMoi);
                }
                _kiotMeta.LuuDuLieu(store);

                TempData["ThongBaoAdmin"] = $"Đã lưu Combo - đóng gói [{combo.MaCodeCb}] {combo.TenCombo} ({thuongHieu}) thành công!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                SanPhamBanh? banh = null;
                if (req.IdGoc > 0)
                {
                    banh = await _db.SanPhamBanhs
                        .Include(b => b.ThongSoDinhDuong)
                        .FirstOrDefaultAsync(b => b.MaBanh == req.IdGoc);
                }

                int maDmHopLe = req.MaDm > 0
                    ? req.MaDm
                    : (await _db.DanhMucBanhs.Select(d => d.MaDm).FirstOrDefaultAsync());

                if (banh == null)
                {
                    string maSku = string.IsNullOrWhiteSpace(req.MaHang)
                        ? $"SP{Random.Shared.Next(100000, 999999)}"
                        : req.MaHang.Trim().ToUpperInvariant();

                    if (await _db.SanPhamBanhs.AnyAsync(b => b.MaSku == maSku))
                    {
                        maSku += "-" + Random.Shared.Next(10, 99);
                    }

                    banh = new SanPhamBanh
                    {
                        MaDm = maDmHopLe,
                        MaSku = maSku,
                        TenBanh = req.TenHang.Trim(),
                        DonGiaLe = Math.Max(0, Math.Round(req.GiaBan, 0)),
                        GiaVon = Math.Max(0, Math.Round(req.GiaVon, 0)),
                        TrongLuongGram = trongLuongGram,
                        SoTrungMuoi = Math.Clamp(req.SoTrungMuoi, 0, 10),
                        MucDoNgot = string.IsNullOrWhiteSpace(req.MucDoNgot) ? "BINH_THUONG" : req.MucDoNgot,
                        LaBanhChay = req.LaBanhChay,
                        LaBanhThuongHang = req.LaBanhThuongHang,
                        NhomHuongVi = "MAN_TRUYENTHONG",
                        TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE",
                        MoTaNgan = string.IsNullOrWhiteSpace(req.MoTaNgan) ? $"Bánh Trung Thu {thuongHieu}" : req.MoTaNgan.Trim(),
                        CauChuyenBanh = req.CauChuyenBanh,
                        TongTonKho = Math.Max(0, req.TonKho),
                        TrangThaiBan = req.BanTrucTiep
                    };
                    _db.SanPhamBanhs.Add(banh);
                    await _db.SaveChangesAsync();

                    _db.ThongSoDinhDuongs.Add(new ThongSoDinhDuong
                    {
                        MaBanh = banh.MaBanh,
                        NangLuongCalo = req.NangLuongCalo > 0 ? req.NangLuongCalo : 380,
                        LuongDuongGram = Math.Max(0, req.LuongDuongGram),
                        ChatDamGram = 8m,
                        ChatBeoGram = 11m,
                        ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi > 0 ? req.ChiSoDuongHuyetGi : 52,
                        CanhBaoDiUng = string.IsNullOrWhiteSpace(req.CanhBaoDiUng) ? "Không" : req.CanhBaoDiUng.Trim()
                    });

                    _db.LoSanXuatBanhs.Add(new LoSanXuatBanh
                    {
                        MaBanh = banh.MaBanh,
                        MaLoCode = $"LO-{DateTime.Now:yyyyMM}-{banh.MaBanh:D2}",
                        NgaySanXuat = DateOnly.FromDateTime(DateTime.Now),
                        HanSuDung = DateOnly.FromDateTime(DateTime.Now.AddDays(40)),
                        SlnhapBanDau = Math.Max(1, banh.TongTonKho),
                        SlconLai = Math.Max(0, banh.TongTonKho),
                        PhanTramGiamCanDate = 0m,
                        TrangThaiLo = "DANG_BAN"
                    });
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(req.MaHang) && req.MaHang.Trim() != banh.MaSku)
                    {
                        string skuMoi = req.MaHang.Trim();
                        bool trungSku = await _db.SanPhamBanhs.AnyAsync(b => b.MaSku == skuMoi && b.MaBanh != banh.MaBanh);
                        if (!trungSku)
                        {
                            banh.MaSku = skuMoi;
                        }
                    }

                    banh.TenBanh = req.TenHang.Trim();
                    banh.MaDm = maDmHopLe;
                    banh.DonGiaLe = Math.Max(0, Math.Round(req.GiaBan, 0));
                    banh.GiaVon = Math.Max(0, Math.Round(req.GiaVon, 0));
                    banh.TongTonKho = Math.Max(0, req.TonKho);
                    banh.TrongLuongGram = trongLuongGram;
                    banh.SoTrungMuoi = Math.Clamp(req.SoTrungMuoi, 0, 10);
                    banh.LaBanhChay = req.LaBanhChay;
                    banh.LaBanhThuongHang = req.LaBanhThuongHang;
                    if (!string.IsNullOrWhiteSpace(req.MucDoNgot))
                    {
                        banh.MucDoNgot = req.MucDoNgot;
                    }
                    if (!string.IsNullOrWhiteSpace(req.MoTaNgan))
                    {
                        banh.MoTaNgan = req.MoTaNgan.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(req.CauChuyenBanh))
                    {
                        banh.CauChuyenBanh = req.CauChuyenBanh.Trim();
                    }
                    banh.TrangThaiBan = req.BanTrucTiep;

                    if (banh.ThongSoDinhDuong != null)
                    {
                        banh.ThongSoDinhDuong.NangLuongCalo = req.NangLuongCalo > 0 ? req.NangLuongCalo : 380;
                        banh.ThongSoDinhDuong.LuongDuongGram = Math.Max(0, req.LuongDuongGram);
                        banh.ThongSoDinhDuong.ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi > 0 ? req.ChiSoDuongHuyetGi : 52;
                        banh.ThongSoDinhDuong.CanhBaoDiUng = string.IsNullOrWhiteSpace(req.CanhBaoDiUng) ? "Không" : req.CanhBaoDiUng.Trim();
                    }
                    await _db.SaveChangesAsync();
                }

                string keyBanh = $"BANH_{banh.MaBanh}";
                if (!store.Items.TryGetValue(keyBanh, out var metaBanh))
                {
                    metaBanh = new KiotItemMeta { Key = keyBanh };
                    store.Items[keyBanh] = metaBanh;
                }
                metaBanh.MaVach = string.IsNullOrWhiteSpace(req.MaVach) ? $"8934680010{banh.MaBanh:D3}" : req.MaVach.Trim();
                metaBanh.ThuongHieu = thuongHieu;
                metaBanh.NhaCungCap = nhaCungCap;
                metaBanh.ViTriKho = viTriKho;
                metaBanh.DonViTinh = donViTinh;
                metaBanh.BanTrucTiep = req.BanTrucTiep;
                if (!string.IsNullOrWhiteSpace(urlAnhMoi))
                {
                    metaBanh.DanhSachAnh.Insert(0, urlAnhMoi);
                }
                _kiotMeta.LuuDuLieu(store);

                TempData["ThongBaoAdmin"] = $"Đã lưu hàng hóa [{banh.MaSku}] {banh.TenBanh} (Thương hiệu: {thuongHieu}) thành công!";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoNhomHangNhanh(string tenNhomHang, string? moTa)
        {
            if (!string.IsNullOrWhiteSpace(tenNhomHang))
            {
                string ten = tenNhomHang.Trim();
                string slug = "nhom-" + DateTime.Now.ToString("MMddHHmmss") + "-" + Random.Shared.Next(10, 99);
                _db.DanhMucBanhs.Add(new DanhMucBanh
                {
                    TenDm = ten,
                    DuongDanSlug = slug,
                    MoTa = string.IsNullOrWhiteSpace(moTa) ? $"Nhóm hàng {ten}" : moTa.Trim(),
                    ThuTuSx = 20,
                    TrangThai = true
                });
                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã tạo nhóm hàng mới: {ten}!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoThuongHieuNhanh(string tenThuongHieu, string? tenNhaCungCap)
        {
            if (!string.IsNullOrWhiteSpace(tenThuongHieu))
            {
                var store = _kiotMeta.DocDuLieu();
                string th = tenThuongHieu.Trim();
                if (!store.DanhSachThuongHieu.Contains(th))
                {
                    store.DanhSachThuongHieu.Add(th);
                }
                if (!string.IsNullOrWhiteSpace(tenNhaCungCap))
                {
                    string ncc = tenNhaCungCap.Trim();
                    if (!store.DanhSachNhaCungCap.Contains(ncc))
                    {
                        store.DanhSachNhaCungCap.Add(ncc);
                    }
                }
                _kiotMeta.LuuDuLieu(store);
                TempData["ThongBaoAdmin"] = $"Đã thêm thương hiệu '{tenThuongHieu.Trim()}' vào hệ thống đại lý!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoViTriNhanh(string tenViTri)
        {
            if (!string.IsNullOrWhiteSpace(tenViTri))
            {
                var store = _kiotMeta.DocDuLieu();
                string vt = tenViTri.Trim();
                if (!store.DanhSachViTriKho.Contains(vt))
                {
                    store.DanhSachViTriKho.Add(vt);
                    _kiotMeta.LuuDuLieu(store);
                }
                TempData["ThongBaoAdmin"] = $"Đã thêm vị trí lưu kho: {vt}!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoNhaCungCapNhanh(string tenNhaCungCap)
        {
            if (!string.IsNullOrWhiteSpace(tenNhaCungCap))
            {
                var store = _kiotMeta.DocDuLieu();
                string ncc = tenNhaCungCap.Trim();
                if (!store.DanhSachNhaCungCap.Contains(ncc))
                {
                    store.DanhSachNhaCungCap.Add(ncc);
                    _kiotMeta.LuuDuLieu(store);
                }
                TempData["ThongBaoAdmin"] = $"Đã thêm nhà cung cấp / lò bánh: {ncc}!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoDanhMucNhanhAjax(string loaiDanhMuc, string tenMoi, string? moTaPhu)
        {
            if (string.IsNullOrWhiteSpace(tenMoi))
            {
                return Json(new { ok = false, message = "Vui lòng nhập tên hợp lệ!" });
            }

            string ten = tenMoi.Trim();
            string loai = (loaiDanhMuc ?? "").Trim().ToUpperInvariant();

            if (loai == "NHOM_HANG")
            {
                var existing = await _db.DanhMucBanhs.FirstOrDefaultAsync(d => d.TenDm == ten);
                if (existing == null)
                {
                    string slug = "nhom-" + DateTime.Now.ToString("MMddHHmmss") + "-" + Random.Shared.Next(10, 99);
                    existing = new DanhMucBanh
                    {
                        TenDm = ten,
                        DuongDanSlug = slug,
                        MoTa = string.IsNullOrWhiteSpace(moTaPhu) ? $"Nhóm hàng {ten}" : moTaPhu.Trim(),
                        ThuTuSx = 20,
                        TrangThai = true
                    };
                    _db.DanhMucBanhs.Add(existing);
                    await _db.SaveChangesAsync();
                }
                return Json(new
                {
                    ok = true,
                    loai = "NHOM_HANG",
                    value = existing.MaDm.ToString(),
                    text = existing.TenDm,
                    message = $"Đã tạo nhóm hàng: {existing.TenDm}"
                });
            }
            else if (loai == "THUONG_HIEU")
            {
                var store = _kiotMeta.DocDuLieu();
                if (!store.DanhSachThuongHieu.Contains(ten))
                {
                    store.DanhSachThuongHieu.Add(ten);
                }
                if (!string.IsNullOrWhiteSpace(moTaPhu))
                {
                    string ncc = moTaPhu.Trim();
                    if (!store.DanhSachNhaCungCap.Contains(ncc))
                    {
                        store.DanhSachNhaCungCap.Add(ncc);
                    }
                }
                _kiotMeta.LuuDuLieu(store);
                return Json(new
                {
                    ok = true,
                    loai = "THUONG_HIEU",
                    value = ten,
                    text = ten,
                    extraNcc = string.IsNullOrWhiteSpace(moTaPhu) ? null : moTaPhu.Trim(),
                    message = $"Đã thêm thương hiệu: {ten}"
                });
            }
            else if (loai == "NHA_CUNG_CAP")
            {
                var store = _kiotMeta.DocDuLieu();
                if (!store.DanhSachNhaCungCap.Contains(ten))
                {
                    store.DanhSachNhaCungCap.Add(ten);
                    _kiotMeta.LuuDuLieu(store);
                }
                return Json(new
                {
                    ok = true,
                    loai = "NHA_CUNG_CAP",
                    value = ten,
                    text = ten,
                    message = $"Đã thêm nhà cung cấp: {ten}"
                });
            }
            else if (loai == "VI_TRI")
            {
                var store = _kiotMeta.DocDuLieu();
                if (!store.DanhSachViTriKho.Contains(ten))
                {
                    store.DanhSachViTriKho.Add(ten);
                    _kiotMeta.LuuDuLieu(store);
                }
                return Json(new
                {
                    ok = true,
                    loai = "VI_TRI",
                    value = ten,
                    text = ten,
                    message = $"Đã thêm vị trí lưu kho: {ten}"
                });
            }

            return Json(new { ok = false, message = "Loại danh mục không hợp lệ." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaHangHoa(int idGoc, bool laCombo)
        {
            var store = _kiotMeta.DocDuLieu();
            if (laCombo)
            {
                var cb = await _db.ComboMauCoSans.FindAsync(idGoc);
                if (cb != null)
                {
                    cb.TrangThai = false;
                    await _db.SaveChangesAsync();
                    if (store.Items.TryGetValue($"COMBO_{idGoc}", out var m))
                    {
                        m.BanTrucTiep = false;
                        _kiotMeta.LuuDuLieu(store);
                    }
                    TempData["ThongBaoAdmin"] = $"Đã ngừng kinh doanh Combo [{cb.MaCodeCb}] {cb.TenCombo}.";
                }
            }
            else
            {
                var banh = await _db.SanPhamBanhs.FindAsync(idGoc);
                if (banh != null)
                {
                    banh.TrangThaiBan = false;
                    await _db.SaveChangesAsync();
                    if (store.Items.TryGetValue($"BANH_{idGoc}", out var m))
                    {
                        m.BanTrucTiep = false;
                        _kiotMeta.LuuDuLieu(store);
                    }
                    TempData["ThongBaoAdmin"] = $"Đã ngừng bán trực tiếp hàng hóa [{banh.MaSku}] {banh.TenBanh}.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatBanhNhanh(CapNhatBanhNhanhRequest req)
        {
            var banh = await _db.SanPhamBanhs
                .Include(b => b.ThongSoDinhDuong)
                .FirstOrDefaultAsync(b => b.MaBanh == req.MaBanh);

            if (banh != null)
            {
                if (!string.IsNullOrWhiteSpace(req.TenBanh))
                {
                    banh.TenBanh = req.TenBanh.Trim();
                }
                banh.DonGiaLe = Math.Round(req.DonGiaLe, 0);
                banh.GiaVon = Math.Round(req.GiaVon, 0);
                banh.TongTonKho = req.TongTonKho;

                if (banh.ThongSoDinhDuong != null)
                {
                    banh.ThongSoDinhDuong.NangLuongCalo = req.NangLuongCalo;
                    banh.ThongSoDinhDuong.LuongDuongGram = req.LuongDuongGram;
                    banh.ThongSoDinhDuong.ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi;
                    banh.ThongSoDinhDuong.CanhBaoDiUng = string.IsNullOrWhiteSpace(req.CanhBaoDiUng) ? "Không" : req.CanhBaoDiUng.Trim();
                }

                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã cập nhật nhanh thông tin cho [{banh.MaSku}] {banh.TenBanh}!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemBanhMoi(ThemBanhMoiAdminRequest req)
        {
            return await LuuHangHoaKiot(new LuuHangHoaKiotRequest
            {
                IdGoc = 0,
                LaCombo = false,
                MaHang = req.MaSku,
                TenHang = req.TenBanh,
                MaDm = req.MaDm,
                GiaBan = req.DonGiaLe,
                GiaVon = req.GiaVon,
                TonKho = req.TongTonKho,
                TrongLuong = req.TrongLuongGram,
                DonViTrongLuong = "g",
                MoTaNgan = req.MoTaNgan,
                NangLuongCalo = req.NangLuongCalo,
                LuongDuongGram = req.LuongDuongGram,
                ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi,
                CanhBaoDiUng = req.CanhBaoDiUng,
                MucDoNgot = req.MucDoNgot,
                LaBanhChay = req.LaBanhChay,
                LaBanhThuongHang = req.LaBanhThuongHang,
                BanTrucTiep = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatGiamGiaCanDate(int maLo, decimal phanTramGiam, string? trangThaiLo, int? slConLai, string? returnUrl)
        {
            var lo = await _db.LoSanXuatBanhs
                .Include(l => l.MaBanhNavigation)
                .FirstOrDefaultAsync(l => l.MaLo == maLo);

            if (lo != null)
            {
                lo.PhanTramGiamCanDate = Math.Clamp(Math.Round(phanTramGiam, 0), 0m, 90m);
                if (slConLai.HasValue && slConLai.Value >= 0)
                {
                    int chenhLech = slConLai.Value - lo.SlconLai;
                    lo.SlconLai = slConLai.Value;
                    if (lo.MaBanhNavigation != null)
                    {
                        lo.MaBanhNavigation.TongTonKho = Math.Max(0, lo.MaBanhNavigation.TongTonKho + chenhLech);
                    }
                }

                if (!string.IsNullOrWhiteSpace(trangThaiLo))
                {
                    lo.TrangThaiLo = trangThaiLo;
                }
                else if (lo.PhanTramGiamCanDate > 0)
                {
                    lo.TrangThaiLo = "XA_CAN_DATE";
                }
                else if (lo.HanSuDung < DateOnly.FromDateTime(DateTime.Today))
                {
                    lo.TrangThaiLo = "HET_HAN";
                }
                else
                {
                    lo.TrangThaiLo = "DANG_BAN";
                }

                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã cập nhật lô {lo.MaLoCode} ({lo.MaBanhNavigation?.TenBanh}) -> Giảm cận Date: {lo.PhanTramGiamCanDate:N0}% ({lo.TrangThaiLo})!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // TRANG KIỂM KHO / QUẢN LÝ LÔ SẢN XUẤT & HẠN SỬ DỤNG (/Admin/SanPhamAdmin/KiemKho)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> KiemKho(string? tuKhoa, string locTrangThai = "ALL", string locGiamGia = "ALL")
        {
            var dsLo = await _db.LoSanXuatBanhs
                .AsNoTracking()
                .Include(l => l.MaBanhNavigation)
                .OrderBy(l => l.HanSuDung)
                .ToListAsync();

            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .OrderBy(b => b.TenBanh)
                .ToListAsync();

            var vm = new KiemKhoLoAdminViewModel
            {
                DanhSachLoBanh = dsLo,
                DanhSachBanh = dsBanh,
                TuKhoa = tuKhoa,
                LocTrangThai = locTrangThai,
                LocGiamGia = locGiamGia
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemLoBanhMoi(
            int maBanh,
            string? maLoCode,
            DateTime? ngaySanXuat,
            DateTime? hanSuDung,
            int slNhapBanDau = 50,
            decimal phanTramGiamCanDate = 0m,
            string? returnUrl = null)
        {
            var banh = await _db.SanPhamBanhs.FindAsync(maBanh);
            if (banh == null)
            {
                TempData["LoiAdmin"] = "Không tìm thấy sản phẩm bánh để nhập lô mới!";
                return RedirectToAction(nameof(KiemKho));
            }

            string code = string.IsNullOrWhiteSpace(maLoCode)
                ? $"LO-{DateTime.Now:yyyyMMdd}-{banh.MaBanh:D2}-{Random.Shared.Next(10, 99)}"
                : maLoCode.Trim().ToUpperInvariant();

            if (await _db.LoSanXuatBanhs.AnyAsync(l => l.MaLoCode == code))
            {
                code += "-" + Random.Shared.Next(10, 99);
            }

            DateOnly nsx = ngaySanXuat.HasValue
                ? DateOnly.FromDateTime(ngaySanXuat.Value)
                : DateOnly.FromDateTime(DateTime.Today);
            DateOnly hsd = hanSuDung.HasValue
                ? DateOnly.FromDateTime(hanSuDung.Value)
                : nsx.AddDays(35);
            if (hsd < nsx)
            {
                hsd = nsx.AddDays(30);
            }

            int slNhap = Math.Max(1, slNhapBanDau);
            decimal ptGiam = Math.Clamp(Math.Round(phanTramGiamCanDate, 0), 0m, 90m);
            string trangThai = ptGiam > 0
                ? "XA_CAN_DATE"
                : (hsd < DateOnly.FromDateTime(DateTime.Today) ? "HET_HAN" : "DANG_BAN");

            var loMoi = new LoSanXuatBanh
            {
                MaBanh = banh.MaBanh,
                MaLoCode = code,
                NgaySanXuat = nsx,
                HanSuDung = hsd,
                SlnhapBanDau = slNhap,
                SlconLai = slNhap,
                PhanTramGiamCanDate = ptGiam,
                TrangThaiLo = trangThai
            };

            _db.LoSanXuatBanhs.Add(loMoi);
            banh.TongTonKho = Math.Max(0, banh.TongTonKho) + slNhap;
            await _db.SaveChangesAsync();

            TempData["ThongBaoAdmin"] = $"Đã nhập lô mới [{code}] cho bánh '{banh.TenBanh}' (+{slNhap:N0} bánh vào tổng tồn kho)!";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(KiemKho));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaLoBanh(int maLo, string? returnUrl = null)
        {
            var lo = await _db.LoSanXuatBanhs
                .Include(l => l.MaBanhNavigation)
                .FirstOrDefaultAsync(l => l.MaLo == maLo);

            if (lo != null)
            {
                string code = lo.MaLoCode;
                string tenBanh = lo.MaBanhNavigation?.TenBanh ?? "";
                if (lo.MaBanhNavigation != null && lo.SlconLai > 0)
                {
                    lo.MaBanhNavigation.TongTonKho = Math.Max(0, lo.MaBanhNavigation.TongTonKho - lo.SlconLai);
                }

                _db.LoSanXuatBanhs.Remove(lo);
                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã xóa lô [{code}] của sản phẩm '{tenBanh}' và đồng bộ lại tồn kho!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(KiemKho));
        }

        // =====================================================================
        // TRANG THIẾT LẬP GIÁ: VỎ HỘP, CHIẾT KHẤU SỈ B2B, COMBO (/Admin/SanPhamAdmin/ThietLapGia)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> ThietLapGia()
        {
            var vm = new ThietLapGiaAdminViewModel
            {
                DanhSachVoHop = await _db.MauVoHops.AsNoTracking().OrderBy(v => v.MaVh).ToListAsync(),
                DanhSachCombo = await _db.ComboMauCoSans
                    .AsNoTracking()
                    .Include(c => c.MaVhNavigation)
                    .Include(c => c.ChiTietComboMaus)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                    .OrderBy(c => c.MaCb)
                    .ToListAsync(),
                BangChietKhauB2B = await _db.BangChietKhauSiB2bs.AsNoTracking().OrderBy(b => b.SlhopToiThieu).ToListAsync(),
                DanhSachBanh = await _db.SanPhamBanhs.AsNoTracking().OrderBy(b => b.TenBanh).ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemVoHopMoi(
            string? maCodeVh,
            string tenVoHop,
            int sucChuaBanh = 4,
            string? kieuBoCucLuoi = "LUOI_2X2",
            decimal giaVoHopRieng = 150000m,
            int slTonKhoVo = 50,
            int slBanhThuongHangToiThieu = 0,
            string? moTaChatLieu = null)
        {
            if (string.IsNullOrWhiteSpace(tenVoHop))
            {
                TempData["LoiAdmin"] = "Vui lòng nhập tên mẫu vỏ hộp!";
                return RedirectToAction(nameof(ThietLapGia));
            }

            string code = string.IsNullOrWhiteSpace(maCodeVh)
                ? $"VH-{Random.Shared.Next(100, 999)}"
                : maCodeVh.Trim().ToUpperInvariant();

            if (await _db.MauVoHops.AnyAsync(v => v.MaCodeVh == code))
            {
                code += "-" + Random.Shared.Next(10, 99);
            }

            var vh = new MauVoHop
            {
                MaCodeVh = code,
                TenVoHop = tenVoHop.Trim(),
                SucChuaBanh = Math.Clamp(sucChuaBanh, 1, 12),
                KieuBoCucLuoi = string.IsNullOrWhiteSpace(kieuBoCucLuoi) ? "LUOI_2X2" : kieuBoCucLuoi.Trim(),
                GiaVoHopRieng = Math.Max(0m, Math.Round(giaVoHopRieng, 0)),
                SltonKhoVo = Math.Max(0, slTonKhoVo),
                SlbanhThuongHangToiThieu = Math.Max(0, slBanhThuongHangToiThieu),
                MoTaChatLieu = string.IsNullOrWhiteSpace(moTaChatLieu) ? "Gỗ sơn mài / Giấy mỹ thuật ép kim" : moTaChatLieu.Trim(),
                TrangThai = true
            };

            _db.MauVoHops.Add(vh);
            await _db.SaveChangesAsync();
            TempData["ThongBaoAdmin"] = $"Đã thêm mẫu vỏ hộp mới: [{vh.MaCodeVh}] {vh.TenVoHop}!";
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatGiaVoHop(int maVh, decimal giaVoHopRieng, int slTonKhoVo, int slBanhVipToiThieu)
        {
            var vh = await _db.MauVoHops.FindAsync(maVh);
            if (vh != null)
            {
                vh.GiaVoHopRieng = Math.Round(giaVoHopRieng, 0);
                vh.SltonKhoVo = slTonKhoVo;
                vh.SlbanhThuongHangToiThieu = slBanhVipToiThieu;
                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã cập nhật giá & tồn kho cho vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop}!";
            }
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaVoHop(int maVh)
        {
            var vh = await _db.MauVoHops.FindAsync(maVh);
            if (vh != null)
            {
                bool dangDung = await _db.ComboMauCoSans.AnyAsync(c => c.MaVh == maVh)
                                || await _db.HopQuaTuChons.AnyAsync(h => h.MaVh == maVh);
                if (dangDung)
                {
                    vh.TrangThai = false;
                    await _db.SaveChangesAsync();
                    TempData["ThongBaoAdmin"] = $"Mẫu vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop} đã có đơn/combo liên kết -> Đã chuyển sang Ngừng sử dụng!";
                }
                else
                {
                    _db.MauVoHops.Remove(vh);
                    await _db.SaveChangesAsync();
                    TempData["ThongBaoAdmin"] = $"Đã xóa mẫu vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop}!";
                }
            }
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemMocB2BMoi(
            string tenMocB2b,
            int slHopToiThieu = 10,
            int? slHopToiDa = 50,
            decimal phanTramChietKhau = 10m,
            decimal phiKhuonInLogo = 500000m,
            bool mienPhiInLogo = false)
        {
            if (string.IsNullOrWhiteSpace(tenMocB2b))
            {
                TempData["LoiAdmin"] = "Vui lòng nhập tên mốc chiết khấu B2B!";
                return RedirectToAction(nameof(ThietLapGia));
            }

            var moc = new BangChietKhauSiB2b
            {
                TenMocB2b = tenMocB2b.Trim(),
                SlhopToiThieu = Math.Max(1, slHopToiThieu),
                SlhopToiDa = (slHopToiDa.HasValue && slHopToiDa.Value >= slHopToiThieu) ? slHopToiDa.Value : 9999,
                PhanTramChietKhau = Math.Clamp(Math.Round(phanTramChietKhau, 0), 0m, 80m),
                PhiKhuonInLogo = Math.Max(0m, Math.Round(phiKhuonInLogo, 0)),
                MienPhiInLogo = mienPhiInLogo,
                SoNgaySanXuatCongThem = 2
            };

            _db.BangChietKhauSiB2bs.Add(moc);
            await _db.SaveChangesAsync();
            TempData["ThongBaoAdmin"] = $"Đã thêm mốc chiết khấu B2B mới: {moc.TenMocB2b} (-{moc.PhanTramChietKhau:N0}%)!";
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatMocB2B(
            int maMocCk,
            int? slHopToiThieu,
            int? slHopToiDa,
            decimal phanTramChietKhau,
            decimal phiKhuonInLogo,
            bool mienPhiInLogo)
        {
            var moc = await _db.BangChietKhauSiB2bs.FindAsync(maMocCk);
            if (moc != null)
            {
                if (slHopToiThieu.HasValue && slHopToiThieu.Value > 0)
                {
                    moc.SlhopToiThieu = slHopToiThieu.Value;
                }
                if (slHopToiDa.HasValue && slHopToiDa.Value >= moc.SlhopToiThieu)
                {
                    moc.SlhopToiDa = slHopToiDa.Value;
                }
                moc.PhanTramChietKhau = Math.Round(phanTramChietKhau, 0);
                moc.PhiKhuonInLogo = Math.Round(phiKhuonInLogo, 0);
                moc.MienPhiInLogo = mienPhiInLogo;
                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã cập nhật mốc chiết khấu B2B: {moc.TenMocB2b} (-{moc.PhanTramChietKhau:N0}%)!";
            }
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaMocB2B(int maMocCk)
        {
            var moc = await _db.BangChietKhauSiB2bs.FindAsync(maMocCk);
            if (moc != null)
            {
                string ten = moc.TenMocB2b;
                _db.BangChietKhauSiB2bs.Remove(moc);
                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã xóa mốc chiết khấu B2B: {ten}!";
            }
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemComboGiaMoi(
            string? maCodeCb,
            string tenCombo,
            int maVh,
            decimal giaNiemYet,
            decimal giaUuDaiCombo,
            string? thongDiepYngia)
        {
            if (string.IsNullOrWhiteSpace(tenCombo))
            {
                TempData["LoiAdmin"] = "Vui lòng nhập tên Combo!";
                return RedirectToAction(nameof(ThietLapGia));
            }

            string code = string.IsNullOrWhiteSpace(maCodeCb)
                ? $"CB-{Random.Shared.Next(100, 999)}"
                : maCodeCb.Trim().ToUpperInvariant();

            if (await _db.ComboMauCoSans.AnyAsync(c => c.MaCodeCb == code))
            {
                code += "-" + Random.Shared.Next(10, 99);
            }

            int maVhHopLe = maVh > 0 ? maVh : (await _db.MauVoHops.Select(v => v.MaVh).FirstOrDefaultAsync());
            var cb = new ComboMauCoSan
            {
                MaCodeCb = code,
                TenCombo = tenCombo.Trim(),
                MaVh = maVhHopLe,
                GiaNiemYet = Math.Max(0m, Math.Round(giaNiemYet, 0)),
                GiaUuDaiCombo = Math.Max(0m, Math.Round(giaUuDaiCombo, 0)),
                ThongDiepYngia = string.IsNullOrWhiteSpace(thongDiepYngia) ? "Combo lễ hộp Trung Thu tuyển chọn" : thongDiepYngia.Trim(),
                TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE",
                TrangThai = true
            };

            _db.ComboMauCoSans.Add(cb);
            await _db.SaveChangesAsync();
            TempData["ThongBaoAdmin"] = $"Đã thêm Combo mới: [{cb.MaCodeCb}] {cb.TenCombo}!";
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatGiaCombo(int maCb, decimal giaNiemYet, decimal giaUuDaiCombo)
        {
            var cb = await _db.ComboMauCoSans.FindAsync(maCb);
            if (cb != null)
            {
                cb.GiaNiemYet = Math.Max(0m, Math.Round(giaNiemYet, 0));
                cb.GiaUuDaiCombo = Math.Max(0m, Math.Round(giaUuDaiCombo, 0));
                await _db.SaveChangesAsync();
                TempData["ThongBaoAdmin"] = $"Đã cập nhật bảng giá cho Combo [{cb.MaCodeCb}] {cb.TenCombo}!";
            }
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaComboGia(int maCb)
        {
            var cb = await _db.ComboMauCoSans
                .Include(c => c.ChiTietComboMaus)
                .FirstOrDefaultAsync(c => c.MaCb == maCb);

            if (cb != null)
            {
                bool coTrongGio = await _db.ChiTietGioHangs.AnyAsync(g => g.MaCb == maCb);
                if (coTrongGio)
                {
                    cb.TrangThai = false;
                    await _db.SaveChangesAsync();
                    TempData["ThongBaoAdmin"] = $"Combo [{cb.MaCodeCb}] đang có trong giỏ hàng -> Đã chuyển sang Ngừng kinh doanh!";
                }
                else
                {
                    if (cb.ChiTietComboMaus.Any())
                    {
                        _db.ChiTietComboMaus.RemoveRange(cb.ChiTietComboMaus);
                    }
                    _db.ComboMauCoSans.Remove(cb);
                    await _db.SaveChangesAsync();
                    TempData["ThongBaoAdmin"] = $"Đã xóa Combo [{cb.MaCodeCb}] {cb.TenCombo}!";
                }
            }
            return RedirectToAction(nameof(ThietLapGia));
        }
    }
}
