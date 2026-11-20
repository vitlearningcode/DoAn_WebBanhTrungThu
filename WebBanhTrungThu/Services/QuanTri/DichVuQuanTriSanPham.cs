using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Danh mục Hàng hóa Đại lý Nguyệt Cát ERP:
    /// Danh mục bánh, sản phẩm bánh, thương hiệu, nhà cung cấp, vỏ hộp và combo mẫu trên SQL Server.
    /// </summary>
    public class DichVuQuanTriSanPham : DichVuCoSo, IDichVuQuanTriSanPham
    {
        private readonly IDichVuHinhAnh _dichVuHinhAnh;
        private readonly IDichVuCauHinhHeThong _cauHinhHeThong;
        private readonly IWebHostEnvironment _env;

        public DichVuQuanTriSanPham(
            CSDLBanhTrungThuContext db,
            IDichVuHinhAnh dichVuHinhAnh,
            IDichVuCauHinhHeThong cauHinhHeThong,
            IWebHostEnvironment env) : base(db)
        {
            _dichVuHinhAnh = dichVuHinhAnh;
            _cauHinhHeThong = cauHinhHeThong;
            _env = env;
        }

        public async Task<SanPhamAdminViewModel> XayDungDanhMucHangHoaAsync(
            string? tuKhoa, int? maDm, string? tenDm, string? locDacTinh, string? locTonKho,
            string? duKienHetHang, string? thoiGianTao, string? nhaCungCap, string? thuongHieu,
            string? viTriKho, string? loaiHang, string? banTrucTiep)
        {
            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();
            var danhSachDanhMuc = await _db.DanhMucBanhs.AsNoTracking().Where(d => d.TrangThai).OrderBy(d => d.ThuTuSx).ToListAsync();
            var danhSachVoHop = await _db.MauVoHops.AsNoTracking().OrderBy(v => v.MaVh).ToListAsync();
            var danhSachLo = await _db.LoSanXuatBanhs.AsNoTracking().Include(l => l.MaBanhNavigation).OrderBy(l => l.HanSuDung).ToListAsync();

            var dsThuongHieu = await _db.ThuongHieus.AsNoTracking()
                .Where(t => t.DangHoatDong)
                .OrderBy(t => t.MaThuongHieu)
                .Select(t => t.TenThuongHieu)
                .ToListAsync();

            var dsNhaCungCap = await _db.NhaCungCaps.AsNoTracking()
                .Where(n => n.DangHoatDong)
                .OrderBy(n => n.MaCodeNcc)
                .ThenBy(n => n.MaNcc)
                .Select(n => n.TenNcc)
                .ToListAsync();

            var dsBanh = await _db.SanPhamBanhs.AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.MaNccMacDinhNavigation)
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .OrderBy(b => b.MaBanh)
                .ToListAsync();

            var dsCombo = await _db.ComboMauCoSans.AsNoTracking()
                .Include(c => c.MaVhNavigation)
                .Include(c => c.MaThuongHieuNavigation)
                .Include(c => c.MaDmNavigation)
                .Include(c => c.ChiTietComboMaus).ThenInclude(ct => ct.MaBanhNavigation)
                .OrderBy(c => c.MaCb)
                .ToListAsync();

            var lienKetAnhTheoKhoa = (await _db.LienKetHinhAnhs
                .AsNoTracking()
                .Include(l => l.MaHaNavigation)
                .Where(l => l.LoaiDoiTuong == "BANH" || l.LoaiDoiTuong == "COMBO")
                .OrderBy(l => l.ThuTuHienThi)
                .ToListAsync())
                .Where(l => l.MaHaNavigation != null && !string.IsNullOrWhiteSpace(l.MaHaNavigation.UrlHienThi))
                .GroupBy(l => $"{l.LoaiDoiTuong}_{l.MaDoiTuong}")
                .ToDictionary(g => g.Key, g => g.Select(x => x.MaHaNavigation.UrlHienThi).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            var setViTriKho = new HashSet<string>(new[] { "Kệ A1", "Kệ A2", "Kệ B1", "Kệ B2", "Tủ Mát C1" }, StringComparer.OrdinalIgnoreCase);
            var setDonViTinh = new HashSet<string>(new[] { "Cái", "Hộp", "Lốc 4 cái", "Thùng 20 hộp" }, StringComparer.OrdinalIgnoreCase);

            foreach (var vt in dsBanh.Select(x => x.ViTriKho).Concat(dsCombo.Select(x => x.ViTriKho)))
            {
                if (!string.IsNullOrWhiteSpace(vt)) setViTriKho.Add(vt.Trim());
            }
            foreach (var dvt in dsBanh.Select(x => x.DonViTinh).Concat(dsCombo.Select(x => x.DonViTinh)))
            {
                if (!string.IsNullOrWhiteSpace(dvt)) setDonViTinh.Add(dvt.Trim());
            }

            var danhSachHangHoa = new List<HangHoaKiotItemViewModel>();

            foreach (var b in dsBanh)
            {
                string maVach = string.IsNullOrWhiteSpace(b.MaVach) ? $"8934680010{b.MaBanh:D3}" : b.MaVach;
                lienKetAnhTheoKhoa.TryGetValue($"BANH_{b.MaBanh}", out var dsAnhLienKet);

                string urlAnh = (dsAnhLienKet != null && dsAnhLienKet.Count > 0)
                    ? dsAnhLienKet[0]
                    : DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", b.MaBanh, "ANH_CHINH");

                var listAnh = new List<string> { urlAnh };
                if (dsAnhLienKet != null && dsAnhLienKet.Count > 1) listAnh.AddRange(dsAnhLienKet.Skip(1));

                var loGanNhat = b.LoSanXuatBanhs.OrderBy(l => l.HanSuDung).FirstOrDefault();
                string duKienTxt = b.TongTonKho <= 0 ? "Đã hết hàng"
                    : (loGanNhat != null ? $"{Math.Max(1, loGanNhat.HanSuDung.DayNumber - LayNgayHienTai().DayNumber)} ngày ({loGanNhat.HanSuDung:dd/MM})" : "---");

                danhSachHangHoa.Add(new HangHoaKiotItemViewModel
                {
                    IdGoc = b.MaBanh, LaCombo = false, MaHang = b.MaSku, MaVach = maVach, TenHang = b.TenBanh,
                    MaDm = b.MaDm, TenNhomHang = b.MaDmNavigation?.TenDm ?? "Bánh Nướng",
                    ThuongHieu = b.ThuongHieu, NhaCungCap = b.NhaCungCap, ViTriKho = b.ViTriKho, DonViTinh = b.DonViTinh,
                    GiaBan = Math.Round(b.DonGiaLe, 0), GiaVon = Math.Round(b.GiaVon, 0), TonKho = b.TongTonKho,
                    DuKienHetHang = duKienTxt, TrongLuongGram = b.TrongLuongGram, BanTrucTiep = b.BanTrucTiep,
                    LaBanhChay = b.LaBanhChay, LaBanhThuongHang = b.LaBanhThuongHang, MucDoNgot = b.MucDoNgot,
                    SoTrungMuoi = b.SoTrungMuoi, MoTaNgan = b.MoTaNgan, CauChuyenBanh = b.CauChuyenBanh,
                    NangLuongCalo = b.ThongSoDinhDuong?.NangLuongCalo ?? 380,
                    LuongDuongGram = Math.Round(b.ThongSoDinhDuong?.LuongDuongGram ?? 14m, 0),
                    ChiSoDuongHuyetGi = b.ThongSoDinhDuong?.ChiSoDuongHuyetGi ?? 52,
                    CanhBaoDiUng = b.ThongSoDinhDuong?.CanhBaoDiUng ?? "Không",
                    UrlAnhChinh = urlAnh, DanhSachAnh = listAnh,
                    DanhSachLo = b.LoSanXuatBanhs.OrderBy(l => l.HanSuDung).ToList()
                });
            }

            foreach (var cb in dsCombo)
            {
                string maVach = string.IsNullOrWhiteSpace(cb.MaVach) ? $"8934680990{cb.MaCb:D3}" : cb.MaVach;
                lienKetAnhTheoKhoa.TryGetValue($"COMBO_{cb.MaCb}", out var dsAnhLienKet);

                var banhDauTien = cb.ChiTietComboMaus.OrderBy(x => x.ViTriO).FirstOrDefault();
                string urlAnhCombo = (dsAnhLienKet != null && dsAnhLienKet.Count > 0)
                    ? dsAnhLienKet[0]
                    : (banhDauTien != null ? DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", banhDauTien.MaBanh, "TACH_NEN_SLOT") : DichVuHinhAnh.LayUrl(tuDienAnh, "VOHOP", cb.MaVh, "KHAY_HOP_RONG"));

                var hangThanhPhans = new List<HangThanhPhanItemViewModel>();
                foreach (var ct in cb.ChiTietComboMaus.OrderBy(x => x.ViTriO).Where(c => c.MaBanhNavigation != null))
                {
                    var existingTp = hangThanhPhans.FirstOrDefault(x => x.MaBanh == ct.MaBanh);
                    if (existingTp != null) existingTp.SoLuong += ct.SoLuong;
                    else hangThanhPhans.Add(new HangThanhPhanItemViewModel
                    {
                        MaBanh = ct.MaBanh, MaHang = ct.MaBanhNavigation!.MaSku, TenHang = ct.MaBanhNavigation.TenBanh,
                        SoLuong = ct.SoLuong, GiaVon = Math.Round(ct.MaBanhNavigation.GiaVon, 0), GiaBan = Math.Round(ct.MaBanhNavigation.DonGiaLe, 0)
                    });
                }

                decimal tongGiaVonCombo = hangThanhPhans.Sum(x => x.TongGiaVon);
                if (tongGiaVonCombo <= 0) tongGiaVonCombo = Math.Round(cb.GiaUuDaiCombo * 0.6m, 0);
                var dmTim = (cb.MaDm.HasValue ? danhSachDanhMuc.FirstOrDefault(d => d.MaDm == cb.MaDm.Value) : null)
                            ?? danhSachDanhMuc.FirstOrDefault(d => string.Equals(d.TenDm, cb.TenDm, StringComparison.OrdinalIgnoreCase));

                danhSachHangHoa.Add(new HangHoaKiotItemViewModel
                {
                    IdGoc = cb.MaCb, LaCombo = true, MaHang = cb.MaCodeCb, MaVach = maVach, TenHang = cb.TenCombo,
                    MaDm = dmTim?.MaDm ?? (danhSachDanhMuc.FirstOrDefault()?.MaDm ?? 1), TenNhomHang = dmTim?.TenDm ?? cb.TenDm,
                    ThuongHieu = cb.ThuongHieu, NhaCungCap = cb.NhaCungCap, ViTriKho = cb.ViTriKho, DonViTinh = cb.DonViTinh,
                    GiaBan = Math.Round(cb.GiaUuDaiCombo, 0), GiaVon = Math.Round(tongGiaVonCombo, 0), TonKho = cb.TongTonKho,
                    DuKienHetHang = "42 ngày", TrongLuongGram = cb.TrongLuongGram, BanTrucTiep = cb.BanTrucTiep,
                    MoTaNgan = cb.ThongDiepYngia, MaVhCombo = cb.MaVh, TenVoHopCombo = cb.MaVhNavigation?.TenVoHop,
                    UrlAnhChinh = urlAnhCombo, DanhSachAnh = new List<string> { urlAnhCombo }, HangThanhPhans = hangThanhPhans
                });
            }

            IEnumerable<HangHoaKiotItemViewModel> query = danhSachHangHoa;
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim().ToLowerInvariant();
                query = query.Where(x => x.MaHang.ToLowerInvariant().Contains(tk) || x.TenHang.ToLowerInvariant().Contains(tk) || x.MaVach.ToLowerInvariant().Contains(tk) || x.ThuongHieu.ToLowerInvariant().Contains(tk));
            }
            if (maDm.HasValue && maDm.Value > 0) query = query.Where(x => x.MaDm == maDm.Value);
            else if (!string.IsNullOrWhiteSpace(tenDm) && tenDm != "ALL") query = query.Where(x => string.Equals(x.TenNhomHang, tenDm, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(locTonKho) && locTonKho != "ALL")
            {
                query = locTonKho switch
                {
                    "DUOI_DINH_MUC" or "SAP_HET" => query.Where(x => x.TonKho > 0 && x.TonKho <= 30),
                    "VUOT_DINH_MUC" => query.Where(x => x.TonKho > 100),
                    "CON_HANG" => query.Where(x => x.TonKho > 0),
                    "HET_HANG" => query.Where(x => x.TonKho <= 0),
                    _ => query
                };
            }

            if (!string.IsNullOrWhiteSpace(nhaCungCap) && nhaCungCap != "ALL") query = query.Where(x => string.Equals(x.NhaCungCap, nhaCungCap, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(thuongHieu) && thuongHieu != "ALL") query = query.Where(x => string.Equals(x.ThuongHieu, thuongHieu, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(viTriKho) && viTriKho != "ALL") query = query.Where(x => string.Equals(x.ViTriKho, viTriKho, StringComparison.OrdinalIgnoreCase));
            if (loaiHang == "BANH_LE") query = query.Where(x => !x.LaCombo);
            else if (loaiHang == "COMBO") query = query.Where(x => x.LaCombo);

            if (banTrucTiep == "CO") query = query.Where(x => x.BanTrucTiep);
            else if (banTrucTiep == "KHONG") query = query.Where(x => !x.BanTrucTiep);

            if (locDacTinh == "ISOMALT") query = query.Where(x => x.MucDoNgot == "ISOMALT_TIEUDUONG");
            else if (locDacTinh == "CHAY") query = query.Where(x => x.LaBanhChay);
            else if (locDacTinh == "VIP") query = query.Where(x => x.LaBanhThuongHang);

            return new SanPhamAdminViewModel
            {
                DanhSachHangHoa = query.ToList(), DanhSachBanh = dsBanh, DanhSachCombo = dsCombo,
                DanhSachDanhMuc = danhSachDanhMuc, DanhSachVoHop = danhSachVoHop, DanhSachLoSanXuat = danhSachLo,
                TuDienAnh = tuDienAnh, DanhSachThuongHieu = dsThuongHieu,
                DanhSachNhaCungCap = dsNhaCungCap, DanhSachViTriKho = setViTriKho.ToList(),
                DanhSachDonViTinh = setDonViTinh.ToList(), TuKhoa = tuKhoa, MaDmLoc = maDm, TenDmLoc = tenDm,
                LocDacTinh = locDacTinh ?? "ALL", LocTonKho = locTonKho ?? "ALL",
                DuKienHetHang = duKienHetHang ?? "ALL", ThoiGianTao = thoiGianTao ?? "ALL",
                NhaCungCapLoc = nhaCungCap ?? "ALL", ThuongHieuLoc = thuongHieu ?? "ALL",
                ViTriKhoLoc = viTriKho ?? "ALL", LoaiHangLoc = loaiHang ?? "ALL", BanTrucTiepLoc = banTrucTiep ?? "ALL"
            };
        }

        public async Task<string> LuuHangHoaKiotAsync(LuuHangHoaKiotRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.TenHang)) return "Vui lòng nhập Tên hàng!";

            string thuongHieu = ChuanHoaChuoi(req.ThuongHieu, "Đồng Khánh");
            string nhaCungCap = ChuanHoaChuoi(req.NhaCungCap, "Lò Bánh Đồng Khánh Chính Gốc");
            string viTriKho = ChuanHoaChuoi(req.ViTriKho, req.LaCombo ? "Kệ B1" : "Kệ A1");
            string donViTinh = ChuanHoaChuoi(req.DonViTinh, req.LaCombo ? "Hộp" : "Cái");

            var thEntity = await TimHoacTaoThuongHieuAsync(thuongHieu);
            var nccEntity = await TimHoacTaoNhaCungCapAsync(nhaCungCap, thuongHieu);

            int trongLuongGram = req.DonViTrongLuong == "kg" ? Math.Max(50, req.TrongLuong * 1000) : Math.Max(50, req.TrongLuong);
            string? urlAnhMoi = null;

            if (req.FileAnhTaiLen != null && req.FileAnhTaiLen.Length > 0)
            {
                string ext = Path.GetExtension(req.FileAnhTaiLen.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                string fileName = $"nc-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}{ext}";
                string dirPath = Path.Combine(_env.WebRootPath, "images", "banh");
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                using var stream = new FileStream(Path.Combine(dirPath, fileName), FileMode.Create);
                await req.FileAnhTaiLen.CopyToAsync(stream);
                urlAnhMoi = $"/images/banh/{fileName}";
            }
            else if (!string.IsNullOrWhiteSpace(req.UrlAnhNhapTay))
            {
                urlAnhMoi = req.UrlAnhNhapTay.Trim();
            }

            var dsThanhPhan = new List<HangThanhPhanItemViewModel>();
            if (!string.IsNullOrWhiteSpace(req.HangThanhPhanJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<HangThanhPhanItemViewModel>>(req.HangThanhPhanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null) dsThanhPhan = parsed.Where(x => x.MaBanh > 0 && x.SoLuong > 0).ToList();
                }
                catch { }
            }

            if (req.LaCombo)
            {
                ComboMauCoSan? combo = req.IdGoc > 0
                    ? await _db.ComboMauCoSans.Include(c => c.ChiTietComboMaus).FirstOrDefaultAsync(c => c.MaCb == req.IdGoc)
                    : null;
                int? maDmCombo = req.MaDm > 0
                    ? req.MaDm
                    : await _db.DanhMucBanhs.OrderBy(d => d.ThuTuSx).Select(d => (int?)d.MaDm).FirstOrDefaultAsync();

                if (combo == null)
                {
                    string maCode = string.IsNullOrWhiteSpace(req.MaHang) ? $"CB{Random.Shared.Next(100000, 999999)}" : req.MaHang.Trim().ToUpperInvariant();
                    if (await _db.ComboMauCoSans.AnyAsync(c => c.MaCodeCb == maCode)) maCode += Random.Shared.Next(10, 99);
                    int maVhMacDinh = req.MaVhCombo ?? (await _db.MauVoHops.Select(v => v.MaVh).FirstOrDefaultAsync());

                    combo = new ComboMauCoSan
                    {
                        MaCodeCb = maCode, TenCombo = req.TenHang.Trim(), MaVh = maVhMacDinh > 0 ? maVhMacDinh : 1,
                        GiaUuDaiCombo = Math.Max(0, Math.Round(req.GiaBan, 0)),
                        GiaNiemYet = Math.Max(Math.Round(req.GiaBan, 0), Math.Round(req.GiaVon * 1.3m, 0)),
                        ThongDiepYngia = ChuanHoaChuoi(req.MoTaNgan, "Combo hộp quà Trung Thu tuyển chọn"),
                        TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE", TrangThai = req.BanTrucTiep,
                        MaThuongHieu = thEntity.MaThuongHieu, MaDm = maDmCombo,
                        MaVach = string.IsNullOrWhiteSpace(req.MaVach) ? null : req.MaVach.Trim(),
                        ViTriKho = viTriKho, DonViTinh = donViTinh, BanTrucTiep = req.BanTrucTiep,
                        TongTonKho = Math.Max(0, req.TonKho), TrongLuongGram = trongLuongGram
                    };
                    _db.ComboMauCoSans.Add(combo);
                    await _db.SaveChangesAsync();
                    if (string.IsNullOrWhiteSpace(combo.MaVach))
                    {
                        combo.MaVach = $"8934680990{combo.MaCb:D3}";
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(req.MaHang)) combo.MaCodeCb = req.MaHang.Trim();
                    combo.TenCombo = req.TenHang.Trim();
                    combo.GiaUuDaiCombo = Math.Max(0, Math.Round(req.GiaBan, 0));
                    if (combo.GiaNiemYet < combo.GiaUuDaiCombo) combo.GiaNiemYet = combo.GiaUuDaiCombo;
                    if (!string.IsNullOrWhiteSpace(req.MoTaNgan)) combo.ThongDiepYngia = req.MoTaNgan.Trim();
                    if (req.MaVhCombo.HasValue && req.MaVhCombo.Value > 0) combo.MaVh = req.MaVhCombo.Value;
                    combo.TrangThai = req.BanTrucTiep;
                    combo.MaThuongHieu = thEntity.MaThuongHieu;
                    combo.MaDm = maDmCombo ?? combo.MaDm;
                    combo.MaVach = ChuanHoaChuoi(req.MaVach, $"8934680990{combo.MaCb:D3}");
                    combo.ViTriKho = viTriKho;
                    combo.DonViTinh = donViTinh;
                    combo.BanTrucTiep = req.BanTrucTiep;
                    combo.TongTonKho = Math.Max(0, req.TonKho);
                    combo.TrongLuongGram = trongLuongGram;
                    await _db.SaveChangesAsync();
                }

                if (dsThanhPhan.Count > 0)
                {
                    var oldItems = await _db.ChiTietComboMaus.Where(x => x.MaCb == combo.MaCb).ToListAsync();
                    if (oldItems.Count > 0) _db.ChiTietComboMaus.RemoveRange(oldItems);
                    int viTri = 1;
                    foreach (var tp in dsThanhPhan)
                    {
                        _db.ChiTietComboMaus.Add(new ChiTietComboMau { MaCb = combo.MaCb, MaBanh = tp.MaBanh, ViTriO = viTri++, SoLuong = tp.SoLuong });
                    }
                    await _db.SaveChangesAsync();
                }

                if (!string.IsNullOrWhiteSpace(urlAnhMoi))
                {
                    await LuuAnhChinhChoDoiTuongAsync("COMBO", combo.MaCb, combo.TenCombo, urlAnhMoi);
                }

                return $"Đã lưu Combo - đóng gói [{combo.MaCodeCb}] {combo.TenCombo} ({thuongHieu}) thành công!";
            }

            SanPhamBanh? banh = req.IdGoc > 0
                ? await _db.SanPhamBanhs.Include(b => b.ThongSoDinhDuong).FirstOrDefaultAsync(b => b.MaBanh == req.IdGoc)
                : null;
            int maDmHopLe = req.MaDm > 0 ? req.MaDm : (await _db.DanhMucBanhs.Select(d => d.MaDm).FirstOrDefaultAsync());

            if (banh == null)
            {
                string maSku = string.IsNullOrWhiteSpace(req.MaHang) ? $"SP{Random.Shared.Next(100000, 999999)}" : req.MaHang.Trim().ToUpperInvariant();
                if (await _db.SanPhamBanhs.AnyAsync(b => b.MaSku == maSku)) maSku += "-" + Random.Shared.Next(10, 99);

                banh = new SanPhamBanh
                {
                    MaDm = maDmHopLe, MaSku = maSku, TenBanh = req.TenHang.Trim(),
                    DonGiaLe = Math.Max(0, Math.Round(req.GiaBan, 0)), GiaVon = Math.Max(0, Math.Round(req.GiaVon, 0)),
                    TrongLuongGram = trongLuongGram, SoTrungMuoi = Math.Clamp(req.SoTrungMuoi, 0, 10),
                    MucDoNgot = ChuanHoaChuoi(req.MucDoNgot, "BINH_THUONG"), LaBanhChay = req.LaBanhChay,
                    LaBanhThuongHang = req.LaBanhThuongHang, NhomHuongVi = "MAN_TRUYENTHONG",
                    TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE", MoTaNgan = ChuanHoaChuoi(req.MoTaNgan, $"Bánh Trung Thu {thuongHieu}"),
                    CauChuyenBanh = req.CauChuyenBanh, TongTonKho = Math.Max(0, req.TonKho), TrangThaiBan = req.BanTrucTiep,
                    MaThuongHieu = thEntity.MaThuongHieu, MaNccMacDinh = nccEntity.MaNcc,
                    MaVach = string.IsNullOrWhiteSpace(req.MaVach) ? null : req.MaVach.Trim(),
                    ViTriKho = viTriKho, DonViTinh = donViTinh, BanTrucTiep = req.BanTrucTiep
                };
                _db.SanPhamBanhs.Add(banh);
                await _db.SaveChangesAsync();

                if (string.IsNullOrWhiteSpace(banh.MaVach))
                {
                    banh.MaVach = $"8934680010{banh.MaBanh:D3}";
                }

                _db.ThongSoDinhDuongs.Add(new ThongSoDinhDuong
                {
                    MaBanh = banh.MaBanh, NangLuongCalo = req.NangLuongCalo > 0 ? req.NangLuongCalo : 380,
                    LuongDuongGram = Math.Max(0, req.LuongDuongGram), ChatDamGram = 8m, ChatBeoGram = 11m,
                    ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi > 0 ? req.ChiSoDuongHuyetGi : 52,
                    CanhBaoDiUng = ChuanHoaChuoi(req.CanhBaoDiUng, "Không")
                });

                _db.LoSanXuatBanhs.Add(new LoSanXuatBanh
                {
                    MaBanh = banh.MaBanh, MaLoCode = $"LO-{DateTime.Now:yyyyMM}-{banh.MaBanh:D2}",
                    NgaySanXuat = LayNgayHienTai(), HanSuDung = LayNgayHienTai().AddDays(40),
                    SlnhapBanDau = Math.Max(1, banh.TongTonKho), SlconLai = Math.Max(0, banh.TongTonKho),
                    PhanTramGiamCanDate = 0m, TrangThaiLo = "DANG_BAN"
                });
                await _db.SaveChangesAsync();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(req.MaHang) && req.MaHang.Trim() != banh.MaSku)
                {
                    string skuMoi = req.MaHang.Trim();
                    if (!await _db.SanPhamBanhs.AnyAsync(b => b.MaSku == skuMoi && b.MaBanh != banh.MaBanh)) banh.MaSku = skuMoi;
                }

                banh.TenBanh = req.TenHang.Trim(); banh.MaDm = maDmHopLe;
                banh.DonGiaLe = Math.Max(0, Math.Round(req.GiaBan, 0)); banh.GiaVon = Math.Max(0, Math.Round(req.GiaVon, 0));
                banh.TongTonKho = Math.Max(0, req.TonKho); banh.TrongLuongGram = trongLuongGram;
                banh.SoTrungMuoi = Math.Clamp(req.SoTrungMuoi, 0, 10); banh.LaBanhChay = req.LaBanhChay;
                banh.LaBanhThuongHang = req.LaBanhThuongHang;
                if (!string.IsNullOrWhiteSpace(req.MucDoNgot)) banh.MucDoNgot = req.MucDoNgot;
                if (!string.IsNullOrWhiteSpace(req.MoTaNgan)) banh.MoTaNgan = req.MoTaNgan.Trim();
                if (!string.IsNullOrWhiteSpace(req.CauChuyenBanh)) banh.CauChuyenBanh = req.CauChuyenBanh.Trim();
                banh.TrangThaiBan = req.BanTrucTiep;
                banh.MaThuongHieu = thEntity.MaThuongHieu;
                banh.MaNccMacDinh = nccEntity.MaNcc;
                banh.MaVach = ChuanHoaChuoi(req.MaVach, $"8934680010{banh.MaBanh:D3}");
                banh.ViTriKho = viTriKho;
                banh.DonViTinh = donViTinh;
                banh.BanTrucTiep = req.BanTrucTiep;

                if (banh.ThongSoDinhDuong != null)
                {
                    banh.ThongSoDinhDuong.NangLuongCalo = req.NangLuongCalo > 0 ? req.NangLuongCalo : 380;
                    banh.ThongSoDinhDuong.LuongDuongGram = Math.Max(0, req.LuongDuongGram);
                    banh.ThongSoDinhDuong.ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi > 0 ? req.ChiSoDuongHuyetGi : 52;
                    banh.ThongSoDinhDuong.CanhBaoDiUng = ChuanHoaChuoi(req.CanhBaoDiUng, "Không");
                }
                await _db.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(urlAnhMoi))
            {
                await LuuAnhChinhChoDoiTuongAsync("BANH", banh.MaBanh, banh.TenBanh, urlAnhMoi);
            }

            return $"Đã lưu hàng hóa [{banh.MaSku}] {banh.TenBanh} (Thương hiệu: {thuongHieu}) thành công!";
        }

        public async Task<string?> TaoNhomHangNhanhAsync(string tenNhomHang, string? moTa)
        {
            if (string.IsNullOrWhiteSpace(tenNhomHang)) return null;
            string ten = tenNhomHang.Trim();
            _db.DanhMucBanhs.Add(new DanhMucBanh
            {
                TenDm = ten, DuongDanSlug = "nhom-" + DateTime.Now.ToString("MMddHHmmss") + "-" + Random.Shared.Next(10, 99),
                MoTa = ChuanHoaChuoi(moTa, $"Nhóm hàng {ten}"), ThuTuSx = 20, TrangThai = true
            });
            await _db.SaveChangesAsync();
            return $"Đã tạo nhóm hàng mới: {ten}!";
        }

        public string? TaoThuongHieuNhanh(string tenThuongHieu, string? tenNhaCungCap)
        {
            if (string.IsNullOrWhiteSpace(tenThuongHieu)) return null;
            string th = tenThuongHieu.Trim();
            TimHoacTaoThuongHieuAsync(th).GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(tenNhaCungCap))
            {
                TimHoacTaoNhaCungCapAsync(tenNhaCungCap.Trim(), th).GetAwaiter().GetResult();
            }
            return $"Đã thêm thương hiệu '{th}' vào bảng ThuongHieu trong SQL Server!";
        }

        public string? TaoViTriNhanh(string tenViTri)
        {
            if (string.IsNullOrWhiteSpace(tenViTri)) return null;
            return $"Đã thêm vị trí lưu kho: {tenViTri.Trim()}!";
        }

        public string? TaoNhaCungCapNhanh(string tenNhaCungCap)
        {
            if (string.IsNullOrWhiteSpace(tenNhaCungCap)) return null;
            string ncc = tenNhaCungCap.Trim();
            TimHoacTaoNhaCungCapAsync(ncc, "Đa thương hiệu").GetAwaiter().GetResult();
            return $"Đã thêm nhà cung cấp / lò bánh: {ncc} vào bảng NhaCungCap!";
        }

        public async Task<object> TaoDanhMucNhanhAjaxAsync(string loaiDanhMuc, string tenMoi, string? moTaPhu)
        {
            if (string.IsNullOrWhiteSpace(tenMoi)) return new { ok = false, message = "Vui lòng nhập tên hợp lệ!" };
            string ten = tenMoi.Trim();
            string loai = (loaiDanhMuc ?? "").Trim().ToUpperInvariant();

            if (loai == "NHOM_HANG")
            {
                var existing = await _db.DanhMucBanhs.FirstOrDefaultAsync(d => d.TenDm == ten);
                if (existing == null)
                {
                    existing = new DanhMucBanh
                    {
                        TenDm = ten, DuongDanSlug = "nhom-" + DateTime.Now.ToString("MMddHHmmss") + "-" + Random.Shared.Next(10, 99),
                        MoTa = ChuanHoaChuoi(moTaPhu, $"Nhóm hàng {ten}"), ThuTuSx = 20, TrangThai = true
                    };
                    _db.DanhMucBanhs.Add(existing);
                    await _db.SaveChangesAsync();
                }
                return new { ok = true, loai = "NHOM_HANG", value = existing.MaDm.ToString(), text = existing.TenDm, message = $"Đã tạo nhóm hàng: {existing.TenDm}" };
            }
            if (loai == "THUONG_HIEU")
            {
                var th = await TimHoacTaoThuongHieuAsync(ten);
                if (!string.IsNullOrWhiteSpace(moTaPhu))
                {
                    await TimHoacTaoNhaCungCapAsync(moTaPhu.Trim(), th.TenThuongHieu);
                }
                return new { ok = true, loai = "THUONG_HIEU", value = th.TenThuongHieu, text = th.TenThuongHieu, extraNcc = string.IsNullOrWhiteSpace(moTaPhu) ? null : moTaPhu.Trim(), message = $"Đã thêm thương hiệu: {th.TenThuongHieu}" };
            }
            if (loai == "NHA_CUNG_CAP")
            {
                var ncc = await TimHoacTaoNhaCungCapAsync(ten, "Đa thương hiệu");
                return new { ok = true, loai = "NHA_CUNG_CAP", value = ncc.TenNcc, text = ncc.TenNcc, message = $"Đã thêm nhà cung cấp: {ncc.TenNcc}" };
            }
            if (loai == "VI_TRI")
            {
                return new { ok = true, loai = "VI_TRI", value = ten, text = ten, message = $"Đã thêm vị trí lưu kho: {ten}" };
            }

            return new { ok = false, message = "Loại danh mục không hợp lệ." };
        }

        public async Task<string?> XoaHangHoaAsync(int idGoc, bool laCombo)
        {
            if (laCombo)
            {
                var cb = await _db.ComboMauCoSans.FindAsync(idGoc);
                if (cb == null) return null;
                cb.TrangThai = false;
                cb.BanTrucTiep = false;
                await _db.SaveChangesAsync();
                return $"Đã ngừng kinh doanh Combo [{cb.MaCodeCb}] {cb.TenCombo}.";
            }

            var banh = await _db.SanPhamBanhs.FindAsync(idGoc);
            if (banh == null) return null;
            banh.TrangThaiBan = false;
            banh.BanTrucTiep = false;
            await _db.SaveChangesAsync();
            return $"Đã ngừng bán trực tiếp hàng hóa [{banh.MaSku}] {banh.TenBanh}.";
        }

        private static readonly string[] _tieuDeExcelHangHoa =
        {
            "Mã SP", "Tên bánh", "Thương hiệu", "Danh mục",
            "Giá bán", "Giá vốn", "Tồn kho", "Kcal", "Đường (g)", "GI", "Trạng thái"
        };

        public async Task<(byte[] NoiDung, string ContentType, string TenFile)> XuatExcelHangHoaAsync(string dinhDang = "xlsx")
        {
            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.ThongSoDinhDuong)
                .OrderBy(b => b.MaBanh)
                .ToListAsync();

            var dongXlsx = new List<object?[]>();
            var dongCsv = new List<string[]>();

            foreach (var b in dsBanh)
            {
                int kcal = b.ThongSoDinhDuong?.NangLuongCalo ?? 380;
                decimal duong = Math.Round(b.ThongSoDinhDuong?.LuongDuongGram ?? 14m, 0);
                int gi = b.ThongSoDinhDuong?.ChiSoDuongHuyetGi ?? 52;
                string trangThai = b.TrangThaiBan ? "Đang bán" : "Ngừng bán";
                string danhMuc = b.MaDmNavigation?.TenDm ?? "Bánh Nướng";

                dongXlsx.Add(new object?[]
                {
                    b.MaSku, b.TenBanh, b.ThuongHieu, danhMuc,
                    Math.Round(b.DonGiaLe, 0), Math.Round(b.GiaVon, 0), b.TongTonKho,
                    kcal, duong, gi, trangThai
                });
                dongCsv.Add(new[]
                {
                    b.MaSku, b.TenBanh, b.ThuongHieu, danhMuc,
                    Math.Round(b.DonGiaLe, 0).ToString("0"), Math.Round(b.GiaVon, 0).ToString("0"),
                    b.TongTonKho.ToString(), kcal.ToString(), duong.ToString("0"), gi.ToString(), trangThai
                });
            }

            if (string.Equals(dinhDang, "csv", StringComparison.OrdinalIgnoreCase))
            {
                byte[] csvBytes = TienIchExcelCsv.TaoFileCsvUtf8Bom(_tieuDeExcelHangHoa, dongCsv);
                return (csvBytes, "text/csv; charset=utf-8", $"DanhMucHangHoa_{DateTime.Now:yyyyMMdd_HHmm}.csv");
            }

            var cotSo = new HashSet<int> { 4, 5, 6, 7, 8, 9 };
            byte[] xlsxBytes = TienIchExcelCsv.TaoFileXlsx("HangHoa", _tieuDeExcelHangHoa, dongXlsx, cotSo);
            return (xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DanhMucHangHoa_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        public Task<(byte[] NoiDung, string ContentType, string TenFile)> TaoFileMauImportHangHoaAsync(string dinhDang = "xlsx")
        {
            var dongMauXlsx = new List<object?[]>
            {
                new object?[] { "NC-TRAM-01", "Bánh Trung Thu Đậu Xanh Lá Dứa (2 Trứng)", "Kinh Đô", "Bánh Nướng", 95000m, 55000m, 80, 390, 15m, 54, "Đang bán" },
                new object?[] { "NC-CHAY-02", "Bánh Trung Thu Hạt Sen Hạnh Nhân Đường Ăn Kiêng", "Như Lan", "Bánh Ít Đường", 115000m, 68000m, 50, 320, 6m, 42, "Đang bán" }
            };
            var dongMauCsv = new List<string[]>
            {
                new[] { "NC-TRAM-01", "Bánh Trung Thu Đậu Xanh Lá Dứa (2 Trứng)", "Kinh Đô", "Bánh Nướng", "95000", "55000", "80", "390", "15", "54", "Đang bán" },
                new[] { "NC-CHAY-02", "Bánh Trung Thu Hạt Sen Hạnh Nhân Đường Ăn Kiêng", "Như Lan", "Bánh Ít Đường", "115000", "68000", "50", "320", "6", "42", "Đang bán" }
            };

            if (string.Equals(dinhDang, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult((
                    TienIchExcelCsv.TaoFileCsvUtf8Bom(_tieuDeExcelHangHoa, dongMauCsv),
                    "text/csv; charset=utf-8",
                    "MauImport_HangHoa.csv"));
            }

            return Task.FromResult((
                TienIchExcelCsv.TaoFileXlsx("MauHangHoa", _tieuDeExcelHangHoa, dongMauXlsx, new HashSet<int> { 4, 5, 6, 7, 8, 9 }),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "MauImport_HangHoa.xlsx"));
        }

        public async Task<KetQuaImportExcelSanPham> NhapExcelHangHoaAsync(IFormFile? fileExcel)
        {
            var kq = new KetQuaImportExcelSanPham();
            if (fileExcel == null || fileExcel.Length == 0)
            {
                kq.ThongBao = "Vui lòng chọn tệp Excel (.xlsx) hoặc CSV (.csv) cần nhập.";
                return kq;
            }

            List<List<string>> bang;
            try
            {
                using var stream = fileExcel.OpenReadStream();
                bang = TienIchExcelCsv.DocBangDuLieu(stream, fileExcel.FileName);
            }
            catch (Exception ex)
            {
                kq.ThongBao = ex.Message;
                return kq;
            }

            if (bang.Count <= 1)
            {
                kq.ThongBao = "Tệp không có dữ liệu sản phẩm nào phía dưới dòng tiêu đề.";
                return kq;
            }

            int soThemMoi = 0;
            int soCapNhat = 0;
            int soBoQua = 0;
            var loiChiTiet = new List<string>();

            await ThucThiTrongGiaoDichAsync(async () =>
            {
                var dsDanhMuc = await _db.DanhMucBanhs.ToListAsync();
                int maDmMacDinh = dsDanhMuc.FirstOrDefault()?.MaDm ?? 1;
                var dsThuongHieuDb = await _db.ThuongHieus.ToListAsync();
                var dsBanhHienTai = await _db.SanPhamBanhs
                    .Include(b => b.ThongSoDinhDuong)
                    .Include(b => b.LoSanXuatBanhs)
                    .ToListAsync();

                for (int r = 1; r < bang.Count; r++)
                {
                    var row = bang[r];
                    string LayO(int idx) => idx < row.Count ? row[idx].Trim() : string.Empty;

                    string maSku = LayO(0);
                    string tenBanh = LayO(1);
                    string thuongHieu = ChuanHoaChuoi(LayO(2), "Đồng Khánh");
                    string tenDanhMuc = LayO(3);
                    decimal giaBan = TienIchExcelCsv.ParseDecimal(LayO(4), 0m);
                    decimal giaVon = TienIchExcelCsv.ParseDecimal(LayO(5), 0m);
                    int tonKho = TienIchExcelCsv.ParseInt(LayO(6), 0);
                    int kcal = Math.Max(0, TienIchExcelCsv.ParseInt(LayO(7), 380));
                    decimal duong = Math.Max(0m, TienIchExcelCsv.ParseDecimal(LayO(8), 14m));
                    int gi = Math.Max(0, TienIchExcelCsv.ParseInt(LayO(9), 52));
                    string trangThaiRaw = LayO(10);

                    if (string.IsNullOrWhiteSpace(tenBanh))
                    {
                        soBoQua++;
                        loiChiTiet.Add($"Dòng {r + 1}: Bỏ qua vì Tên bánh trống.");
                        continue;
                    }
                    if (giaBan <= 0m)
                    {
                        soBoQua++;
                        loiChiTiet.Add($"Dòng {r + 1} ({tenBanh}): Bỏ qua vì Giá bán phải > 0.");
                        continue;
                    }
                    if (giaVon < 0m || tonKho < 0)
                    {
                        soBoQua++;
                        loiChiTiet.Add($"Dòng {r + 1} ({tenBanh}): Bỏ qua vì Giá vốn/Tồn kho âm.");
                        continue;
                    }

                    bool dangBan = !trangThaiRaw.Contains("ngừng", StringComparison.OrdinalIgnoreCase)
                                && !trangThaiRaw.Equals("false", StringComparison.OrdinalIgnoreCase)
                                && !trangThaiRaw.Equals("0", StringComparison.OrdinalIgnoreCase);

                    var thEntity = dsThuongHieuDb.FirstOrDefault(t => string.Equals(t.TenThuongHieu, thuongHieu, StringComparison.OrdinalIgnoreCase));
                    if (thEntity == null)
                    {
                        thEntity = await TimHoacTaoThuongHieuAsync(thuongHieu);
                        dsThuongHieuDb.Add(thEntity);
                    }

                    var dm = string.IsNullOrWhiteSpace(tenDanhMuc)
                        ? null
                        : dsDanhMuc.FirstOrDefault(d => string.Equals(d.TenDm, tenDanhMuc, StringComparison.OrdinalIgnoreCase));
                    if (dm == null && !string.IsNullOrWhiteSpace(tenDanhMuc))
                    {
                        dm = new DanhMucBanh
                        {
                            TenDm = tenDanhMuc,
                            DuongDanSlug = "nhom-" + DateTime.Now.ToString("MMddHHmmss") + "-" + Random.Shared.Next(10, 99),
                            MoTa = $"Nhóm hàng {tenDanhMuc}",
                            ThuTuSx = 20,
                            TrangThai = true
                        };
                        _db.DanhMucBanhs.Add(dm);
                        await _db.SaveChangesAsync();
                        dsDanhMuc.Add(dm);
                    }
                    int maDm = dm?.MaDm ?? maDmMacDinh;

                    SanPhamBanh? banh = !string.IsNullOrWhiteSpace(maSku)
                        ? dsBanhHienTai.FirstOrDefault(b => string.Equals(b.MaSku, maSku, StringComparison.OrdinalIgnoreCase))
                        : null;
                    banh ??= dsBanhHienTai.FirstOrDefault(b => string.Equals(b.TenBanh, tenBanh, StringComparison.OrdinalIgnoreCase));

                    if (banh == null)
                    {
                        string skuMoi = string.IsNullOrWhiteSpace(maSku)
                            ? $"SP{Random.Shared.Next(100000, 999999)}"
                            : maSku.ToUpperInvariant();
                        while (dsBanhHienTai.Any(b => string.Equals(b.MaSku, skuMoi, StringComparison.OrdinalIgnoreCase)))
                        {
                            skuMoi = $"{skuMoi}-{Random.Shared.Next(10, 99)}";
                        }

                        banh = new SanPhamBanh
                        {
                            MaDm = maDm,
                            MaSku = skuMoi,
                            TenBanh = tenBanh,
                            DonGiaLe = Math.Round(giaBan, 0),
                            GiaVon = Math.Round(giaVon, 0),
                            TrongLuongGram = 150,
                            SoTrungMuoi = 1,
                            MucDoNgot = gi <= 45 ? "ISOMALT_TIEUDUONG" : "BINH_THUONG",
                            LaBanhChay = tenBanh.Contains("Chay", StringComparison.OrdinalIgnoreCase),
                            LaBanhThuongHang = giaBan >= 150000m,
                            NhomHuongVi = "MAN_TRUYENTHONG",
                            TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE",
                            MoTaNgan = $"Bánh Trung Thu {thuongHieu}",
                            TongTonKho = tonKho,
                            TrangThaiBan = dangBan,
                            MaThuongHieu = thEntity.MaThuongHieu,
                            ViTriKho = "Kệ A1",
                            DonViTinh = "Cái",
                            BanTrucTiep = dangBan
                        };
                        _db.SanPhamBanhs.Add(banh);
                        await _db.SaveChangesAsync();

                        banh.MaVach = $"8934680010{banh.MaBanh:D3}";

                        _db.ThongSoDinhDuongs.Add(new ThongSoDinhDuong
                        {
                            MaBanh = banh.MaBanh,
                            NangLuongCalo = kcal > 0 ? kcal : 380,
                            LuongDuongGram = duong,
                            ChatDamGram = 8m,
                            ChatBeoGram = 11m,
                            ChiSoDuongHuyetGi = gi > 0 ? gi : 52,
                            CanhBaoDiUng = "Không"
                        });

                        _db.LoSanXuatBanhs.Add(new LoSanXuatBanh
                        {
                            MaBanh = banh.MaBanh,
                            MaLoCode = $"LO-IMP-{DateTime.Now:yyyyMMdd}-{banh.MaBanh:D2}-{Random.Shared.Next(10, 99)}",
                            NgaySanXuat = LayNgayHienTai(),
                            HanSuDung = LayNgayHienTai().AddDays(40),
                            SlnhapBanDau = Math.Max(1, tonKho),
                            SlconLai = tonKho,
                            PhanTramGiamCanDate = 0m,
                            TrangThaiLo = "DANG_BAN"
                        });
                        await _db.SaveChangesAsync();
                        dsBanhHienTai.Add(banh);
                        soThemMoi++;
                    }
                    else
                    {
                        banh.TenBanh = tenBanh;
                        banh.MaDm = maDm;
                        banh.DonGiaLe = Math.Round(giaBan, 0);
                        if (giaVon >= 0m) banh.GiaVon = Math.Round(giaVon, 0);
                        banh.TrangThaiBan = dangBan;
                        banh.MaThuongHieu = thEntity.MaThuongHieu;
                        banh.BanTrucTiep = dangBan;

                        if (banh.ThongSoDinhDuong != null)
                        {
                            if (kcal > 0) banh.ThongSoDinhDuong.NangLuongCalo = kcal;
                            banh.ThongSoDinhDuong.LuongDuongGram = duong;
                            if (gi > 0) banh.ThongSoDinhDuong.ChiSoDuongHuyetGi = gi;
                        }

                        if (!string.IsNullOrWhiteSpace(LayO(6)))
                        {
                            int tonHienTai = banh.LoSanXuatBanhs.Where(l => l.TrangThaiLo != "DA_HUY").Sum(l => l.SlconLai);
                            int lech = tonKho - tonHienTai;
                            if (lech > 0)
                            {
                                var loMoiNhat = banh.LoSanXuatBanhs
                                    .Where(l => l.TrangThaiLo != "DA_HUY")
                                    .OrderByDescending(l => l.HanSuDung)
                                    .FirstOrDefault();
                                if (loMoiNhat != null)
                                {
                                    loMoiNhat.SlconLai += lech;
                                }
                                else
                                {
                                    _db.LoSanXuatBanhs.Add(new LoSanXuatBanh
                                    {
                                        MaBanh = banh.MaBanh,
                                        MaLoCode = $"LO-IMP-{DateTime.Now:yyyyMMdd}-{banh.MaBanh:D2}-{Random.Shared.Next(10, 99)}",
                                        NgaySanXuat = LayNgayHienTai(),
                                        HanSuDung = LayNgayHienTai().AddDays(40),
                                        SlnhapBanDau = lech,
                                        SlconLai = lech,
                                        PhanTramGiamCanDate = 0m,
                                        TrangThaiLo = "DANG_BAN"
                                    });
                                }
                            }
                            else if (lech < 0)
                            {
                                int conTru = Math.Abs(lech);
                                foreach (var lo in banh.LoSanXuatBanhs
                                    .Where(l => l.TrangThaiLo != "DA_HUY" && l.SlconLai > 0)
                                    .OrderBy(l => l.HanSuDung)
                                    .ThenBy(l => l.MaLo))
                                {
                                    if (conTru <= 0) break;
                                    int tru = Math.Min(lo.SlconLai, conTru);
                                    lo.SlconLai -= tru;
                                    conTru -= tru;
                                }
                            }
                            banh.TongTonKho = tonKho;
                        }

                        await _db.SaveChangesAsync();
                        soCapNhat++;
                    }
                }
            });

            kq.ThanhCong = true;
            kq.SoThemMoi = soThemMoi;
            kq.SoCapNhat = soCapNhat;
            kq.SoBoQua = soBoQua;
            kq.DanhSachLoi = loiChiTiet;
            kq.ThongBao = $"Import hoàn tất: Thêm mới {soThemMoi} sản phẩm, Cập nhật {soCapNhat} sản phẩm, Bỏ qua {soBoQua} dòng.";
            if (loiChiTiet.Count > 0)
            {
                kq.ThongBao += " (" + string.Join(" | ", loiChiTiet.Take(3)) + ")";
            }
            return kq;
        }

        private async Task<ThuongHieu> TimHoacTaoThuongHieuAsync(string tenThuongHieu)
        {
            string tenSach = ChuanHoaChuoi(tenThuongHieu, "Đồng Khánh");
            var tonTai = await _db.ThuongHieus.FirstOrDefaultAsync(t => t.TenThuongHieu.ToLower() == tenSach.ToLower());
            if (tonTai != null) return tonTai;

            var tatCaCode = await _db.ThuongHieus.AsNoTracking().Select(t => t.MaCodeThuongHieu).ToListAsync();
            string normalized = tenSach.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            string ascii = sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D').ToUpperInvariant();
            string codeBase = Regex.Replace(ascii, @"[^A-Z0-9]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(codeBase)) codeBase = "THUONG_HIEU";
            if (codeBase.Length > 24) codeBase = codeBase[..24];

            string code = codeBase;
            int stt = 2;
            while (tatCaCode.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)))
            {
                code = $"{codeBase}_{stt++}";
                if (code.Length > 30) code = code[..30];
            }

            var moi = new ThuongHieu
            {
                MaCodeThuongHieu = code,
                TenThuongHieu = tenSach.Length > 120 ? tenSach[..120] : tenSach,
                DangHoatDong = true
            };
            _db.ThuongHieus.Add(moi);
            await _db.SaveChangesAsync();
            return moi;
        }

        private async Task<NhaCungCap> TimHoacTaoNhaCungCapAsync(string tenNcc, string thuongHieuCungCap)
        {
            string tenSach = ChuanHoaChuoi(tenNcc, "Lò Bánh Đồng Khánh Chính Gốc");
            var tonTai = await _db.NhaCungCaps.FirstOrDefaultAsync(n => n.TenNcc.ToLower() == tenSach.ToLower());
            if (tonTai != null) return tonTai;

            var dsCode = await _db.NhaCungCaps.AsNoTracking().Select(n => n.MaCodeNcc).ToListAsync();
            int maxSo = 0;
            foreach (var c in dsCode)
            {
                if (c.StartsWith("NCC", StringComparison.OrdinalIgnoreCase) && int.TryParse(c[3..], out int so) && so > maxSo) maxSo = so;
            }
            string maCode = $"NCC{(maxSo + 1):D3}";

            var moi = new NhaCungCap
            {
                MaCodeNcc = maCode,
                TenNcc = tenSach.Length > 200 ? tenSach[..200] : tenSach,
                LoaiNcc = "HANG_CHINH_HANG",
                ThuongHieuCungCap = string.IsNullOrWhiteSpace(thuongHieuCungCap) ? "Đa thương hiệu" : thuongHieuCungCap.Trim(),
                PhanTramChietKhauMacDinh = 20m,
                DangHoatDong = true,
                NgayTao = LayThoiGianHeThong()
            };
            _db.NhaCungCaps.Add(moi);
            await _db.SaveChangesAsync();
            return moi;
        }

        private async Task LuuAnhChinhChoDoiTuongAsync(string loaiDoiTuong, int maDoiTuong, string tieuDeAlt, string urlAnh)
        {
            var lienKetCu = await _db.LienKetHinhAnhs
                .Where(l => l.LoaiDoiTuong == loaiDoiTuong && l.MaDoiTuong == maDoiTuong)
                .ToListAsync();
            foreach (var lk in lienKetCu)
            {
                lk.ThuTuHienThi += 1;
            }

            var khoAnh = new KhoHinhAnh
            {
                TenTepGoc = Path.GetFileName(urlAnh),
                TieuDeAlt = tieuDeAlt.Length > 250 ? tieuDeAlt[..250] : tieuDeAlt,
                LoaiLuuTru = 1,
                DuongDanCucBo = urlAnh.Length > 500 ? urlAnh[..500] : urlAnh,
                KichThuocPixel = "800x800",
                NgayTaiLen = LayThoiGianHeThong()
            };
            _db.KhoHinhAnhs.Add(khoAnh);
            await _db.SaveChangesAsync();

            _db.LienKetHinhAnhs.Add(new LienKetHinhAnh
            {
                MaHa = khoAnh.MaHa,
                LoaiDoiTuong = loaiDoiTuong,
                MaDoiTuong = maDoiTuong,
                VaiTroAnh = "ANH_CHINH",
                ThuTuHienThi = 0
            });
            await _db.SaveChangesAsync();
        }
    }
}
