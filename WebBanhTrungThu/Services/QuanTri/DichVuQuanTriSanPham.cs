using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Danh mục Hàng hóa Đại lý Nguyệt Cát ERP (~22KB):
    /// Danh mục bánh, sản phẩm bánh, vỏ hộp và combo mẫu. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuQuanTriSanPham : DichVuCoSo, IDichVuQuanTriSanPham
    {
        private readonly IDichVuHinhAnh _dichVuHinhAnh;
        private readonly IDichVuCauHinhHeThong _cauHinhHeThong;
        private readonly IWebHostEnvironment _env;
        private static bool _daKiemTraDanhMuc = false;

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

        private async Task DamBaoDanhMucChuanDaiLyAsync()
        {
            if (_daKiemTraDanhMuc) return;
            if (!await _db.DanhMucBanhs.AsNoTracking().AnyAsync(d => d.TenDm == "ĐỒNG GIÁ 20K" || d.DuongDanSlug == "dong-gia-20k"))
            {
                _db.DanhMucBanhs.Add(new DanhMucBanh
                {
                    TenDm = "ĐỒNG GIÁ 20K",
                    DuongDanSlug = "dong-gia-20k",
                    MoTa = "Nhóm hàng bánh lẻ và combo ưu đãi đồng giá Đại lý Nguyệt Cát ERP",
                    ThuTuSx = 10,
                    TrangThai = true
                });
                await _db.SaveChangesAsync();
            }
            _daKiemTraDanhMuc = true;
        }

        public async Task<SanPhamAdminViewModel> XayDungDanhMucHangHoaAsync(
            string? tuKhoa, int? maDm, string? tenDm, string? locDacTinh, string? locTonKho,
            string? duKienHetHang, string? thoiGianTao, string? nhaCungCap, string? thuongHieu,
            string? viTriKho, string? loaiHang, string? banTrucTiep)
        {
            await DamBaoDanhMucChuanDaiLyAsync();

            var store = _cauHinhHeThong.DocDuLieu();
            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();
            var danhSachDanhMuc = await _db.DanhMucBanhs.AsNoTracking().Where(d => d.TrangThai).OrderBy(d => d.ThuTuSx).ToListAsync();
            var danhSachVoHop = await _db.MauVoHops.AsNoTracking().OrderBy(v => v.MaVh).ToListAsync();
            var danhSachLo = await _db.LoSanXuatBanhs.AsNoTracking().Include(l => l.MaBanhNavigation).OrderBy(l => l.HanSuDung).ToListAsync();

            var dsBanh = await _db.SanPhamBanhs.AsNoTracking()
                .Include(b => b.MaDmNavigation).Include(b => b.ThongSoDinhDuong).Include(b => b.LoSanXuatBanhs)
                .OrderBy(b => b.MaBanh).ToListAsync();

            var dsCombo = await _db.ComboMauCoSans.AsNoTracking()
                .Include(c => c.MaVhNavigation).Include(c => c.ChiTietComboMaus).ThenInclude(ct => ct.MaBanhNavigation)
                .OrderBy(c => c.MaCb).ToListAsync();

            var danhSachHangHoa = new List<HangHoaKiotItemViewModel>();

            foreach (var b in dsBanh)
            {
                _cauHinhHeThong.GanMetadataChoBanh(b, store);
                store.Items.TryGetValue($"BANH_{b.MaBanh}", out var metaItem);

                string urlAnh = (metaItem != null && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                    ? metaItem.DanhSachAnh[0]
                    : DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", b.MaBanh, "ANH_CHINH");

                var listAnh = new List<string> { urlAnh };
                if (metaItem != null && metaItem.DanhSachAnh.Count > 1) listAnh.AddRange(metaItem.DanhSachAnh.Skip(1));

                var loGanNhat = b.LoSanXuatBanhs.OrderBy(l => l.HanSuDung).FirstOrDefault();
                string duKienTxt = b.TongTonKho <= 0 ? "Đã hết hàng"
                    : (loGanNhat != null ? $"{Math.Max(1, loGanNhat.HanSuDung.DayNumber - LayNgayHienTai().DayNumber)} ngày ({loGanNhat.HanSuDung:dd/MM})" : "---");

                danhSachHangHoa.Add(new HangHoaKiotItemViewModel
                {
                    IdGoc = b.MaBanh, LaCombo = false, MaHang = b.MaSku, MaVach = b.MaVach, TenHang = b.TenBanh,
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
                _cauHinhHeThong.GanMetadataChoCombo(cb, store);
                store.Items.TryGetValue($"COMBO_{cb.MaCb}", out var metaItem);

                var banhDauTien = cb.ChiTietComboMaus.OrderBy(x => x.ViTriO).FirstOrDefault();
                string urlAnhCombo = (metaItem != null && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                    ? metaItem.DanhSachAnh[0]
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
                var dmTim = danhSachDanhMuc.FirstOrDefault(d => d.MaDm == cb.MaDm) ?? danhSachDanhMuc.FirstOrDefault(d => d.TenDm == cb.TenDm);

                danhSachHangHoa.Add(new HangHoaKiotItemViewModel
                {
                    IdGoc = cb.MaCb, LaCombo = true, MaHang = cb.MaCodeCb, MaVach = cb.MaVach, TenHang = cb.TenCombo,
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
                TuDienAnh = tuDienAnh, DanhSachThuongHieu = store.DanhSachThuongHieu,
                DanhSachNhaCungCap = store.DanhSachNhaCungCap, DanhSachViTriKho = store.DanhSachViTriKho,
                DanhSachDonViTinh = store.DanhSachDonViTinh, TuKhoa = tuKhoa, MaDmLoc = maDm, TenDmLoc = tenDm,
                LocDacTinh = locDacTinh ?? "ALL", LocTonKho = locTonKho ?? "ALL",
                DuKienHetHang = duKienHetHang ?? "ALL", ThoiGianTao = thoiGianTao ?? "ALL",
                NhaCungCapLoc = nhaCungCap ?? "ALL", ThuongHieuLoc = thuongHieu ?? "ALL",
                ViTriKhoLoc = viTriKho ?? "ALL", LoaiHangLoc = loaiHang ?? "ALL", BanTrucTiepLoc = banTrucTiep ?? "ALL"
            };
        }

        public async Task<string> LuuHangHoaKiotAsync(LuuHangHoaKiotRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.TenHang)) return "Vui lòng nhập Tên hàng!";

            var store = _cauHinhHeThong.DocDuLieu();
            string thuongHieu = ChuanHoaChuoi(req.ThuongHieu, "Đồng Khánh");
            string nhaCungCap = ChuanHoaChuoi(req.NhaCungCap, "Lò Bánh Đồng Khánh Chính Gốc");
            string viTriKho = ChuanHoaChuoi(req.ViTriKho, "Kệ A1");
            string donViTinh = ChuanHoaChuoi(req.DonViTinh, req.LaCombo ? "Hộp" : "Cái");

            if (!store.DanhSachThuongHieu.Contains(thuongHieu)) store.DanhSachThuongHieu.Add(thuongHieu);
            if (!store.DanhSachNhaCungCap.Contains(nhaCungCap)) store.DanhSachNhaCungCap.Add(nhaCungCap);
            if (!store.DanhSachViTriKho.Contains(viTriKho)) store.DanhSachViTriKho.Add(viTriKho);
            if (!store.DanhSachDonViTinh.Contains(donViTinh)) store.DanhSachDonViTinh.Add(donViTinh);

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
                        TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE", TrangThai = req.BanTrucTiep
                    };
                    _db.ComboMauCoSans.Add(combo);
                    await _db.SaveChangesAsync();
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

                var dmChon = await _db.DanhMucBanhs.FindAsync(req.MaDm);
                string keyCombo = $"COMBO_{combo.MaCb}";
                if (!store.Items.TryGetValue(keyCombo, out var metaCombo))
                {
                    metaCombo = new CauHinhHangHoaMoRong { Key = keyCombo };
                    store.Items[keyCombo] = metaCombo;
                }
                metaCombo.MaVach = ChuanHoaChuoi(req.MaVach, $"8934680990{combo.MaCb:D3}");
                metaCombo.ThuongHieu = thuongHieu; metaCombo.NhaCungCap = nhaCungCap;
                metaCombo.ViTriKho = viTriKho; metaCombo.DonViTinh = donViTinh;
                metaCombo.BanTrucTiep = req.BanTrucTiep; metaCombo.MaDmCombo = dmChon?.MaDm ?? req.MaDm;
                metaCombo.TenDmCombo = dmChon?.TenDm ?? "ĐỒNG GIÁ 20K"; metaCombo.TonKhoCombo = Math.Max(0, req.TonKho);
                metaCombo.TrongLuongComboGram = trongLuongGram;
                if (!string.IsNullOrWhiteSpace(urlAnhMoi)) metaCombo.DanhSachAnh.Insert(0, urlAnhMoi);
                _cauHinhHeThong.LuuDuLieu(store);

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
                    CauChuyenBanh = req.CauChuyenBanh, TongTonKho = Math.Max(0, req.TonKho), TrangThaiBan = req.BanTrucTiep
                };
                _db.SanPhamBanhs.Add(banh);
                await _db.SaveChangesAsync();

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

                if (banh.ThongSoDinhDuong != null)
                {
                    banh.ThongSoDinhDuong.NangLuongCalo = req.NangLuongCalo > 0 ? req.NangLuongCalo : 380;
                    banh.ThongSoDinhDuong.LuongDuongGram = Math.Max(0, req.LuongDuongGram);
                    banh.ThongSoDinhDuong.ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi > 0 ? req.ChiSoDuongHuyetGi : 52;
                    banh.ThongSoDinhDuong.CanhBaoDiUng = ChuanHoaChuoi(req.CanhBaoDiUng, "Không");
                }
                await _db.SaveChangesAsync();
            }

            string keyBanh = $"BANH_{banh.MaBanh}";
            if (!store.Items.TryGetValue(keyBanh, out var metaBanh))
            {
                metaBanh = new CauHinhHangHoaMoRong { Key = keyBanh };
                store.Items[keyBanh] = metaBanh;
            }
            metaBanh.MaVach = ChuanHoaChuoi(req.MaVach, $"8934680010{banh.MaBanh:D3}");
            metaBanh.ThuongHieu = thuongHieu; metaBanh.NhaCungCap = nhaCungCap;
            metaBanh.ViTriKho = viTriKho; metaBanh.DonViTinh = donViTinh; metaBanh.BanTrucTiep = req.BanTrucTiep;
            if (!string.IsNullOrWhiteSpace(urlAnhMoi)) metaBanh.DanhSachAnh.Insert(0, urlAnhMoi);
            _cauHinhHeThong.LuuDuLieu(store);

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
            var store = _cauHinhHeThong.DocDuLieu();
            string th = tenThuongHieu.Trim();
            if (!store.DanhSachThuongHieu.Contains(th)) store.DanhSachThuongHieu.Add(th);
            if (!string.IsNullOrWhiteSpace(tenNhaCungCap) && !store.DanhSachNhaCungCap.Contains(tenNhaCungCap.Trim()))
            {
                store.DanhSachNhaCungCap.Add(tenNhaCungCap.Trim());
            }
            _cauHinhHeThong.LuuDuLieu(store);
            return $"Đã thêm thương hiệu '{th}' vào hệ thống đại lý!";
        }

        public string? TaoViTriNhanh(string tenViTri)
        {
            if (string.IsNullOrWhiteSpace(tenViTri)) return null;
            var store = _cauHinhHeThong.DocDuLieu();
            string vt = tenViTri.Trim();
            if (!store.DanhSachViTriKho.Contains(vt)) { store.DanhSachViTriKho.Add(vt); _cauHinhHeThong.LuuDuLieu(store); }
            return $"Đã thêm vị trí lưu kho: {vt}!";
        }

        public string? TaoNhaCungCapNhanh(string tenNhaCungCap)
        {
            if (string.IsNullOrWhiteSpace(tenNhaCungCap)) return null;
            var store = _cauHinhHeThong.DocDuLieu();
            string ncc = tenNhaCungCap.Trim();
            if (!store.DanhSachNhaCungCap.Contains(ncc)) { store.DanhSachNhaCungCap.Add(ncc); _cauHinhHeThong.LuuDuLieu(store); }
            return $"Đã thêm nhà cung cấp / lò bánh: {ncc}!";
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
            if (loai == "THUONG_HIEU") { TaoThuongHieuNhanh(ten, moTaPhu); return new { ok = true, loai = "THUONG_HIEU", value = ten, text = ten, extraNcc = string.IsNullOrWhiteSpace(moTaPhu) ? null : moTaPhu.Trim(), message = $"Đã thêm thương hiệu: {ten}" }; }
            if (loai == "NHA_CUNG_CAP") { TaoNhaCungCapNhanh(ten); return new { ok = true, loai = "NHA_CUNG_CAP", value = ten, text = ten, message = $"Đã thêm nhà cung cấp: {ten}" }; }
            if (loai == "VI_TRI") { TaoViTriNhanh(ten); return new { ok = true, loai = "VI_TRI", value = ten, text = ten, message = $"Đã thêm vị trí lưu kho: {ten}" }; }

            return new { ok = false, message = "Loại danh mục không hợp lệ." };
        }

        public async Task<string?> XoaHangHoaAsync(int idGoc, bool laCombo)
        {
            var store = _cauHinhHeThong.DocDuLieu();
            if (laCombo)
            {
                var cb = await _db.ComboMauCoSans.FindAsync(idGoc);
                if (cb == null) return null;
                cb.TrangThai = false;
                await _db.SaveChangesAsync();
                if (store.Items.TryGetValue($"COMBO_{idGoc}", out var m)) { m.BanTrucTiep = false; _cauHinhHeThong.LuuDuLieu(store); }
                return $"Đã ngừng kinh doanh Combo [{cb.MaCodeCb}] {cb.TenCombo}.";
            }

            var banh = await _db.SanPhamBanhs.FindAsync(idGoc);
            if (banh == null) return null;
            banh.TrangThaiBan = false;
            await _db.SaveChangesAsync();
            if (store.Items.TryGetValue($"BANH_{idGoc}", out var mb)) { mb.BanTrucTiep = false; _cauHinhHeThong.LuuDuLieu(store); }
            return $"Đã ngừng bán trực tiếp hàng hóa [{banh.MaSku}] {banh.TenBanh}.";
        }
    }
}
