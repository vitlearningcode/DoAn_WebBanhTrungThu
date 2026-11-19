using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách nghiệp vụ Bán hàng tại quầy Nguyệt Cát ERP Web POS:
    /// Tìm/mở thẻ khách hàng thành viên, Tạo đơn tại quầy POS, Trừ kho FEFO tại quầy và Ghi phiếu thu POS vào Sổ quỹ.
    /// Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuBanHangPos : DichVuCoSo, IDichVuBanHangPos
    {
        private readonly IDichVuHinhAnh _dichVuHinhAnh;
        private readonly IDichVuCauHinhHeThong _kiotMeta;

        public DichVuBanHangPos(
            CSDLBanhTrungThuContext db,
            IDichVuHinhAnh dichVuHinhAnh,
            IDichVuCauHinhHeThong kiotMeta) : base(db)
        {
            _dichVuHinhAnh = dichVuHinhAnh;
            _kiotMeta = kiotMeta;
        }

        public async Task<BanHangPosViewModel> XayDungDuLieuManHinhPosAsync(string tenThuNgan, string vaiTro)
        {
            var store = _kiotMeta.DocDuLieu();
            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();

            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .Include(b => b.MaDmNavigation)
                .Where(b => b.TrangThaiBan)
                .OrderBy(b => b.MaBanh)
                .ToListAsync();

            foreach (var b in dsBanh)
            {
                _kiotMeta.GanMetadataChoBanh(b, store);
                string keyMeta = $"BANH_{b.MaBanh}";
                if (store.Items.TryGetValue(keyMeta, out var metaItem) && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                {
                    tuDienAnh[$"BANH_{b.MaBanh}_ANH_CHINH"] = metaItem.DanhSachAnh[0];
                }
            }

            var dsCombo = await _db.ComboMauCoSans
                .AsNoTracking()
                .Include(c => c.MaVhNavigation)
                .Include(c => c.ChiTietComboMaus).ThenInclude(ct => ct.MaBanhNavigation)
                .Where(c => c.TrangThai)
                .OrderBy(c => c.MaCb)
                .ToListAsync();

            foreach (var cb in dsCombo)
            {
                _kiotMeta.GanMetadataChoCombo(cb, store);
                string keyMeta = $"COMBO_{cb.MaCb}";
                if (store.Items.TryGetValue(keyMeta, out var metaItem) && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                {
                    tuDienAnh[$"COMBO_{cb.MaCb}_ANH_CHINH"] = metaItem.DanhSachAnh[0];
                }
            }

            return new BanHangPosViewModel
            {
                DanhSachBanh = dsBanh,
                DanhSachCombo = dsCombo,
                DanhSachVoHop = await _db.MauVoHops.AsNoTracking().Where(v => v.TrangThai).OrderBy(v => v.MaVh).ToListAsync(),
                DanhSachDanhMuc = await _db.DanhMucBanhs.AsNoTracking().Where(d => d.TrangThai).OrderBy(d => d.ThuTuSx).ToListAsync(),
                BangChietKhauB2B = await _db.BangChietKhauSiB2bs.AsNoTracking().OrderBy(b => b.SlhopToiThieu).ToListAsync(),
                DanhSachKhuyenMai = await _db.MaKhuyenMais.AsNoTracking().Where(k => k.NgayKetThuc >= DateTime.Now && k.SldaDung < k.SltoiDa).OrderBy(k => k.CodeKm).ToListAsync(),
                TuDienAnh = tuDienAnh,
                TenThuNgan = tenThuNgan,
                VaiTroThuNgan = vaiTro
            };
        }

        private static (string HangTv, decimal PhanTramUuDai) ChuanHoaHangVip(string? hangTv)
        {
            string chuan = (hangTv ?? string.Empty).Trim();
            if (chuan.Contains("Kim Cương", StringComparison.OrdinalIgnoreCase) || chuan.Contains("Kim Cuong", StringComparison.OrdinalIgnoreCase))
                return ("Kim Cương", 10m);
            if (chuan.Contains("Vàng", StringComparison.OrdinalIgnoreCase) || chuan.Contains("Vang", StringComparison.OrdinalIgnoreCase))
                return ("Vàng", 5m);
            if (chuan.Contains("Bạc", StringComparison.OrdinalIgnoreCase) || chuan.Contains("Bac", StringComparison.OrdinalIgnoreCase))
                return ("Bạc", 3m);
            return ("Đồng", 0m);
        }

        private async Task<(string HangTv, decimal PhanTramUuDai)> LayThongTinHangVipPosAsync(NguoiDung nd)
        {
            var (hangTheoChiTieu, phanTramTheoChiTieu, _, _) =
                await DichVuHoiVien.TinhThongTinHangVipAsync(_db, nd.MaNd, nd.DiemTichLuy);
            var (hangLuuSan, phanTramLuuSan) = ChuanHoaHangVip(nd.HangTv);
            return phanTramLuuSan > phanTramTheoChiTieu
                ? (hangLuuSan, phanTramLuuSan)
                : (hangTheoChiTieu, phanTramTheoChiTieu);
        }

        public async Task<object> TraCuuHoiVienPosAsync(string? sdt)
        {
            string sdtTrim = (sdt ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sdtTrim))
            {
                return new { thanhCong = false, thongBao = "Vui lòng nhập số điện thoại hội viên cần tra cứu!" };
            }

            string sdtChiSo = new string(sdtTrim.Where(char.IsDigit).ToArray());
            var nd = await _db.NguoiDungs
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TrangThai && u.Sdt == sdtTrim);

            if (nd == null && sdtChiSo.Length >= 8)
            {
                nd = await _db.NguoiDungs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.TrangThai && (u.Sdt == sdtChiSo || u.Sdt.Contains(sdtChiSo)));
            }

            if (nd == null)
            {
                return new { thanhCong = false, thongBao = $"Không tìm thấy hội viên nào có SĐT \"{sdtTrim}\"." };
            }

            var (hangThanhVien, phanTramUuDaiVip) = await LayThongTinHangVipPosAsync(nd);

            return new
            {
                thanhCong = true,
                maNd = nd.MaNd,
                hoTen = nd.HoTen,
                sdt = nd.Sdt,
                email = nd.Email,
                hangThanhVien,
                hangTv = hangThanhVien,
                phanTramUuDaiVip,
                diemTichLuy = nd.DiemTichLuy
            };
        }

        public async Task<object> TimKhachHangPosAsync(string? tuKhoa)
        {
            string kw = ChuanHoaChuoi(tuKhoa);
            var query = _db.NguoiDungs.AsNoTracking().Where(u => u.TrangThai);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                query = query.Where(u => u.Sdt.Contains(kw) || u.HoTen.Contains(kw) || u.Email.Contains(kw));
            }

            var dsNguoiDung = await query
                .OrderByDescending(u => u.DiemTichLuy)
                .Take(10)
                .ToListAsync();

            var ds = new List<object>();
            foreach (var u in dsNguoiDung)
            {
                var (hangTv, phanTramUuDaiVip) = await LayThongTinHangVipPosAsync(u);
                ds.Add(new
                {
                    maNd = u.MaNd,
                    hoTen = u.HoTen,
                    sdt = u.Sdt,
                    email = u.Email,
                    hangThanhVien = hangTv,
                    hangTv,
                    phanTramUuDaiVip,
                    diemTichLuy = u.DiemTichLuy
                });
            }

            return new { thanhCong = true, danhSach = ds };
        }

        public async Task<object> ThemKhachHangNhanhPosAsync(string hoTen, string sdt, string? email)
        {
            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(sdt))
            {
                return new { thanhCong = false, thongBao = "Vui lòng nhập đầy đủ Họ tên và Số điện thoại khách hàng!" };
            }

            string sdtChuan = sdt.Trim();
            var daTonTai = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.Sdt == sdtChuan);
            if (daTonTai != null)
            {
                return new
                {
                    thanhCong = true,
                    thongBao = $"Số điện thoại {sdtChuan} đã là thành viên ({daTonTai.HoTen}) - Đã chọn vào hóa đơn!",
                    khach = new
                    {
                        maNd = daTonTai.MaNd,
                        hoTen = daTonTai.HoTen,
                        sdt = daTonTai.Sdt,
                        email = daTonTai.Email,
                        hangTv = daTonTai.HangTv,
                        diemTichLuy = daTonTai.DiemTichLuy
                    }
                };
            }

            string emailChuan = string.IsNullOrWhiteSpace(email)
                ? $"khachpos.{sdtChuan}.{Random.Shared.Next(10, 99)}@nguyetcat.vn"
                : email.Trim();

            if (await _db.NguoiDungs.AnyAsync(u => u.Email == emailChuan))
            {
                emailChuan = $"khachpos.{sdtChuan}.{DateTime.Now:HHmmss}@nguyetcat.vn";
            }

            if (!await _db.VaiTros.AnyAsync(v => v.MaVt == "KHACH_HANG"))
            {
                _db.VaiTros.Add(new VaiTro
                {
                    MaVt = "KHACH_HANG",
                    TenVt = "Khách hàng thành viên",
                    MoTa = "Tích điểm và hưởng đặc quyền thành viên Nguyệt Cát"
                });
                await _db.SaveChangesAsync();
            }

            var khachMoi = new NguoiDung
            {
                MaVt = "KHACH_HANG",
                HoTen = hoTen.Trim(),
                Sdt = sdtChuan,
                Email = emailChuan,
                MatKhauHash = ThongTinPhienHelper.BamMatKhauSHA256("123456"),
                DiemTichLuy = 50,
                HangTv = "Đồng",
                TrangThai = true,
                NgayTao = LayThoiGianHeThong()
            };

            _db.NguoiDungs.Add(khachMoi);
            await _db.SaveChangesAsync();

            return new
            {
                thanhCong = true,
                thongBao = $"Đã tạo thẻ thành viên cho '{khachMoi.HoTen}' (Tặng +50 điểm mở thẻ, MK mặc định: 123456)!",
                khach = new
                {
                    maNd = khachMoi.MaNd,
                    hoTen = khachMoi.HoTen,
                    sdt = khachMoi.Sdt,
                    email = khachMoi.Email,
                    hangTv = khachMoi.HangTv,
                    diemTichLuy = khachMoi.DiemTichLuy
                }
            };
        }

        public async Task<object> ThanhToanTaiQuayAsync(PosCheckoutRequest? req, bool dungDiemTichLuy = false)
        {
            if (req == null || req.DanhSachMon == null || !req.DanhSachMon.Any())
            {
                return new { thanhCong = false, thongBao = "Hóa đơn hiện chưa có sản phẩm nào. Vui lòng chọn bánh hoặc hộp quà!" };
            }

            var homNay = LayNgayHienTai();
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var dictBanh = await _db.SanPhamBanhs
                    .Include(b => b.LoSanXuatBanhs)
                    .Include(b => b.ThongSoDinhDuong)
                    .ToDictionaryAsync(b => b.MaBanh);

                var dictCombo = await _db.ComboMauCoSans
                    .Include(c => c.MaVhNavigation)
                    .Include(c => c.ChiTietComboMaus).ThenInclude(ct => ct.MaBanhNavigation)
                    .ToDictionaryAsync(c => c.MaCb);

                var dictVoHop = await _db.MauVoHops.ToDictionaryAsync(v => v.MaVh);

                var nhuCauBanh = new Dictionary<int, int>();
                var nhuCauVoHop = new Dictionary<int, int>();
                decimal tongTienHang = 0m;
                int tongSoHop = 0;

                foreach (var item in req.DanhSachMon)
                {
                    int sl = Math.Max(1, item.SoLuong);
                    if (item.LoaiSanPham == 1)
                    {
                        if (!item.MaBanh.HasValue || !dictBanh.TryGetValue(item.MaBanh.Value, out var banh))
                        {
                            return new { thanhCong = false, thongBao = "Có sản phẩm bánh lẻ không tồn tại trong hệ thống!" };
                        }
                        nhuCauBanh[banh.MaBanh] = nhuCauBanh.GetValueOrDefault(banh.MaBanh) + sl;
                        tongTienHang += Math.Round(banh.DonGiaThucTe, 0) * sl;
                    }
                    else if (item.LoaiSanPham == 2)
                    {
                        if (!item.MaCb.HasValue || !dictCombo.TryGetValue(item.MaCb.Value, out var combo))
                        {
                            return new { thanhCong = false, thongBao = "Có bộ Combo không tồn tại trong hệ thống!" };
                        }
                        nhuCauVoHop[combo.MaVh] = nhuCauVoHop.GetValueOrDefault(combo.MaVh) + sl;
                        foreach (var ct in combo.ChiTietComboMaus)
                        {
                            nhuCauBanh[ct.MaBanh] = nhuCauBanh.GetValueOrDefault(ct.MaBanh) + (ct.SoLuong * sl);
                        }
                        tongTienHang += Math.Round(combo.GiaUuDaiCombo, 0) * sl;
                        tongSoHop += sl;
                    }
                    else if (item.LoaiSanPham == 3)
                    {
                        int maVh = item.MaVh ?? dictVoHop.Keys.FirstOrDefault();
                        if (!dictVoHop.TryGetValue(maVh, out var voHop))
                        {
                            return new { thanhCong = false, thongBao = "Mẫu vỏ hộp tự chọn không hợp lệ!" };
                        }
                        if (item.DanhSachMaBanhTrongHop == null || item.DanhSachMaBanhTrongHop.Count == 0)
                        {
                            return new { thanhCong = false, thongBao = $"Hộp tự chọn '{voHop.TenVoHop}' chưa có bánh thành phần bên trong!" };
                        }

                        nhuCauVoHop[voHop.MaVh] = nhuCauVoHop.GetValueOrDefault(voHop.MaVh) + sl;
                        decimal giaMotHop = Math.Round(voHop.GiaVoHopRieng, 0);
                        foreach (var mb in item.DanhSachMaBanhTrongHop)
                        {
                            if (!dictBanh.TryGetValue(mb, out var bHop))
                            {
                                return new { thanhCong = false, thongBao = "Bánh trong hộp tự chọn không tồn tại!" };
                            }
                            nhuCauBanh[bHop.MaBanh] = nhuCauBanh.GetValueOrDefault(bHop.MaBanh) + sl;
                            giaMotHop += Math.Round(bHop.DonGiaThucTe, 0);
                        }
                        tongTienHang += giaMotHop * sl;
                        tongSoHop += sl;
                    }
                }

                foreach (var kv in nhuCauBanh)
                {
                    var banh = dictBanh[kv.Key];
                    if (banh.TongTonKho < kv.Value)
                    {
                        return new { thanhCong = false, thongBao = $"Sản phẩm [{banh.MaSku}] {banh.TenBanh} chỉ còn tồn {banh.TongTonKho} bánh (cần xuất {kv.Value} bánh)!" };
                    }

                    bool coLoConHan = !banh.LoSanXuatBanhs.Any()
                        || banh.LoSanXuatBanhs.Any(l => l.HanSuDung >= homNay && l.TrangThaiLo != "HET_HAN" && l.SlconLai > 0);
                    if (!coLoConHan)
                    {
                        return new { thanhCong = false, thongBao = $"Sản phẩm [{banh.MaSku}] {banh.TenBanh} hiện chỉ còn lô đã hết hạn sử dụng, không thể xuất bán!" };
                    }
                }

                foreach (var kv in nhuCauVoHop)
                {
                    if (dictVoHop.TryGetValue(kv.Key, out var vh) && vh.SltonKhoVo < kv.Value)
                    {
                        return new { thanhCong = false, thongBao = $"Mẫu vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop} chỉ còn tồn {vh.SltonKhoVo} vỏ (cần {kv.Value} vỏ)!" };
                    }
                }

                var bangB2B = await _db.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
                var mocB2B = bangB2B.FirstOrDefault(b => tongSoHop >= b.SlhopToiThieu && tongSoHop <= b.SlhopToiDa)
                             ?? (tongSoHop > 0 && bangB2B.Any() && tongSoHop > bangB2B.Last().SlhopToiDa ? bangB2B.Last() : null);

                decimal phanTramCkB2B = mocB2B?.PhanTramChietKhau ?? 0m;
                decimal tienChietKhauB2B = phanTramCkB2B > 0 ? Math.Round(tongTienHang * (phanTramCkB2B / 100m), 0) : 0m;
                bool mienPhiInLogo = mocB2B?.MienPhiInLogo ?? false;
                decimal phiKhuonMacDinh = mocB2B?.PhiKhuonInLogo ?? (bangB2B.FirstOrDefault()?.PhiKhuonInLogo ?? 500000m);
                decimal phiInLogo = (req.YeuCauInLogo && !mienPhiInLogo) ? phiKhuonMacDinh : 0m;

                decimal tienGiamVoucher = 0m;
                if (!string.IsNullOrWhiteSpace(req.CodeKm))
                {
                    string code = req.CodeKm.Trim().ToUpperInvariant();
                    var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k =>
                        k.CodeKm == code && k.NgayKetThuc >= DateTime.Now && k.SldaDung < k.SltoiDa);

                    if (km != null && tongTienHang >= km.DonToiThieu)
                    {
                        tienGiamVoucher = km.LoaiGiam == "PHAN_TRAM"
                            ? Math.Round(tongTienHang * (km.GiaTriGiam / 100m), 0)
                            : km.GiaTriGiam;
                        if (km.GiamToiDa > 0 && tienGiamVoucher > km.GiamToiDa) tienGiamVoucher = km.GiamToiDa;
                        km.SldaDung += 1;
                    }
                }

                NguoiDung? khachMember = (req.MaNd.HasValue && req.MaNd.Value > 0)
                    ? await _db.NguoiDungs.FindAsync(req.MaNd.Value)
                    : null;

                if (khachMember == null && !string.IsNullOrWhiteSpace(req.SdtKhach) && req.SdtKhach.Trim() != "0900000000")
                {
                    string sdtLookup = req.SdtKhach.Trim();
                    khachMember = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.TrangThai && u.Sdt == sdtLookup);
                }

                decimal tienHangSauCkVaVoucher = Math.Max(0m, tongTienHang - tienChietKhauB2B - tienGiamVoucher);
                string hangVipLucDat = "Đồng";
                decimal phanTramGiamVip = 0m;
                decimal tienGiamVip = 0m;

                if (khachMember != null)
                {
                    (hangVipLucDat, phanTramGiamVip) = await LayThongTinHangVipPosAsync(khachMember);
                    if (phanTramGiamVip > 0m)
                    {
                        tienGiamVip = Math.Round(tienHangSauCkVaVoucher * (phanTramGiamVip / 100m), 0);
                    }
                }

                decimal tienHangSauVip = Math.Max(0m, tienHangSauCkVaVoucher - tienGiamVip);
                int diemDaDung = 0;
                decimal tienGiamDiem = 0m;

                if (dungDiemTichLuy && khachMember != null && khachMember.DiemTichLuy > 0 && tienHangSauVip >= 1000m)
                {
                    int diemToiDaTheoDon = (int)(tienHangSauVip / 1000m);
                    diemDaDung = Math.Min(khachMember.DiemTichLuy, diemToiDaTheoDon);
                    tienGiamDiem = diemDaDung * 1000m;
                    khachMember.DiemTichLuy = Math.Max(0, khachMember.DiemTichLuy - diemDaDung);
                }

                bool laGiaoHang = string.Equals(req.CheDoMua, "GIAO_HANG_QR", StringComparison.OrdinalIgnoreCase);
                decimal phiVanChuyen = laGiaoHang ? 30000m : 0m;
                decimal tongThanhToanCuoi = Math.Max(0m, tienHangSauVip - tienGiamDiem + phiInLogo + phiVanChuyen);

                string maTraCuu;
                do
                {
                    maTraCuu = "POS26-" + Random.Shared.Next(10000, 99999);
                } while (await _db.DonHangs.AnyAsync(d => d.MaTraCuuDh == maTraCuu));

                string hoTenMua = khachMember?.HoTen ?? ChuanHoaChuoi(req.HoTenKhach, "Khách lẻ tại quầy");
                string sdtMua = khachMember?.Sdt ?? ChuanHoaChuoi(req.SdtKhach, "0900000000");
                string? emailMua = khachMember?.Email ?? (!string.IsNullOrWhiteSpace(req.EmailKhach) ? req.EmailKhach.Trim() : null);

                var donHang = new DonHang
                {
                    MaTraCuuDh = maTraCuu,
                    MaNd = khachMember?.MaNd,
                    MaPhienKvl = null,
                    HoTenNguoiMua = hoTenMua,
                    SdtnguoiMua = sdtMua,
                    EmailNguoiMua = emailMua,
                    LaDonDoanhNghiepB2b = req.LaDonDoanhNghiepB2B || tongSoHop >= 10,
                    TenCongTy = !string.IsNullOrWhiteSpace(req.TenCongTy) ? req.TenCongTy.Trim() : null,
                    MaSoThue = !string.IsNullOrWhiteSpace(req.MaSoThue) ? req.MaSoThue.Trim() : null,
                    TongTienHang = tongTienHang,
                    PhanTramCkB2b = phanTramCkB2B,
                    TienChietKhauB2b = tienChietKhauB2B,
                    TienGiamVoucher = tienGiamVoucher,
                    PhiInLogo = phiInLogo,
                    TongPhiVanChuyen = phiVanChuyen,
                    TongThanhToanCuoi = tongThanhToanCuoi,
                    TienGiamHangVip = Math.Round(tienGiamVip, 0),
                    PhanTramGiamVip = (int)Math.Round(phanTramGiamVip, 0),
                    SoDiemDaDung = diemDaDung,
                    TienGiamDiemVip = Math.Round(tienGiamDiem, 0),
                    HanThanhToanQr = null,
                    DaCongDiemThuong = false,
                    DaHoanDiemKhiHuy = false,
                    LyDoHuy = null,
                    PhuongThucTt = ChuanHoaChuoi(req.PhuongThucTt, "TIEN_MAT"),
                    TrangThaiTt = "DA_TT",
                    TrangThaiDh = laGiaoHang ? "CHO_LAY_HANG" : "HOAN_TAT",
                    NgayDat = LayThoiGianHeThong()
                };

                _db.DonHangs.Add(donHang);
                await _db.SaveChangesAsync();

                int? maDotGh = null;
                string? maTokenQr = null;
                if (laGiaoHang)
                {
                    if (req.CoKemThiepQr)
                    {
                        maTokenQr = "QR-" + Guid.NewGuid().ToString("N").ToUpperInvariant()[..12];
                    }

                    DateOnly ngayGiao = homNay.AddDays(1);
                    if (!string.IsNullOrWhiteSpace(req.NgayHenGiao) && DateOnly.TryParse(req.NgayHenGiao, out var parsedDate) && parsedDate >= homNay)
                    {
                        ngayGiao = parsedDate;
                    }

                    var dotGiao = new DotGiaoHangDaDiem
                    {
                        MaDh = donHang.MaDh,
                        TenNguoiNhan = ChuanHoaChuoi(req.TenNguoiNhan, hoTenMua),
                        SdtnguoiNhan = ChuanHoaChuoi(req.SdtNguoiNhan, sdtMua),
                        DiaChiGiaoChiTiet = ChuanHoaChuoi(req.DiaChiGiao, "Giao tận nơi theo yêu cầu tại quầy POS"),
                        NgayHenGiao = ngayGiao,
                        KhungGioGiao = ChuanHoaChuoi(req.KhungGioGiao, "Sáng (08:00 - 11:30)"),
                        PhiShipDotNay = phiVanChuyen,
                        TrangThaiGiao = "CHO_DONG_GOI",
                        CoKemThiepQr = req.CoKemThiepQr,
                        MaTokenThiepQr = maTokenQr,
                        LoiChucThiep = req.CoKemThiepQr
                            ? ChuanHoaChuoi(req.LoiChucThiep, "Kính chúc Quý khách & Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!")
                            : null,
                        MauGiaoDienThiep = ChuanHoaChuoi(req.MauGiaoDienThiep, "CO_PHONG_HOANG_GIA"),
                        SoLanMoThiep = 0
                    };

                    _db.DotGiaoHangDaDiems.Add(dotGiao);
                    await _db.SaveChangesAsync();
                    maDotGh = dotGiao.MaDotGh;

                    if (req.CoKemThiepQr && !string.IsNullOrWhiteSpace(maTokenQr))
                    {
                        _db.ThiepChucMung3ds.Add(new ThiepChucMung3d
                        {
                            MaTokenQr = maTokenQr,
                            MaDonHang = donHang.MaDh,
                            MaDotGiao = dotGiao.MaDotGh,
                            TenNguoiGui = hoTenMua,
                            TenNguoiNhan = dotGiao.TenNguoiNhan,
                            LoiChuc = dotGiao.LoiChucThiep ?? "Kính chúc Quý khách & Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!",
                            MauGiaoDien = dotGiao.MauGiaoDienThiep ?? "CO_PHONG_HOANG_GIA",
                            SoLanMoThiep = 0,
                            NgayTao = LayThoiGianHeThong()
                        });
                    }
                }

                var danhSachLoDaTru = new List<NhatKyTruLoFefo>();
                var danhSachVoHopDaTru = new List<NhatKyTruVoHop>();

                static void TruTonKhoBanhVaLoFefo(SanPhamBanh banh, int soLuongCanTru, List<NhatKyTruLoFefo> nhatKyLo)
                {
                    banh.TongTonKho = Math.Max(0, banh.TongTonKho - soLuongCanTru);
                    int conPhaiTru = soLuongCanTru;
                    var cacLoKhaDung = banh.LoSanXuatBanhs
                        .Where(l => l.SlconLai > 0 && l.TrangThaiLo != "HET_HAN")
                        .OrderBy(l => l.HanSuDung)
                        .ToList();

                    foreach (var lo in cacLoKhaDung)
                    {
                        if (conPhaiTru <= 0) break;
                        int truLoNay = Math.Min(lo.SlconLai, conPhaiTru);
                        lo.SlconLai -= truLoNay;
                        conPhaiTru -= truLoNay;

                        var daCo = nhatKyLo.FirstOrDefault(x => x.MaLo == lo.MaLo && x.MaBanh == banh.MaBanh);
                        if (daCo != null) daCo.SoLuongDaTru += truLoNay;
                        else nhatKyLo.Add(new NhatKyTruLoFefo { MaLo = lo.MaLo, MaBanh = banh.MaBanh, SoLuongDaTru = truLoNay });
                    }
                }

                foreach (var item in req.DanhSachMon)
                {
                    int sl = Math.Max(1, item.SoLuong);

                    if (item.LoaiSanPham == 1 && item.MaBanh.HasValue && dictBanh.TryGetValue(item.MaBanh.Value, out var banh))
                    {
                        decimal donGia = Math.Round(banh.DonGiaThucTe, 0);
                        TruTonKhoBanhVaLoFefo(banh, sl, danhSachLoDaTru);

                        _db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDh = donHang.MaDh,
                            MaDotGh = maDotGh,
                            LoaiSanPham = 1,
                            MaBanh = banh.MaBanh,
                            TenSanPhamLuuCung = banh.TenBanh,
                            SoLuong = sl,
                            DonGiaLucMua = donGia,
                            ThanhTien = donGia * sl
                        });
                    }
                    else if (item.LoaiSanPham == 2 && item.MaCb.HasValue && dictCombo.TryGetValue(item.MaCb.Value, out var combo))
                    {
                        decimal donGia = Math.Round(combo.GiaUuDaiCombo, 0);
                        if (dictVoHop.TryGetValue(combo.MaVh, out var vhCombo))
                        {
                            vhCombo.SltonKhoVo = Math.Max(0, vhCombo.SltonKhoVo - sl);
                            danhSachVoHopDaTru.Add(new NhatKyTruVoHop { MaVh = vhCombo.MaVh, SoLuongDaTru = sl });
                        }
                        foreach (var ct in combo.ChiTietComboMaus)
                        {
                            if (dictBanh.TryGetValue(ct.MaBanh, out var bThanhPhan))
                            {
                                TruTonKhoBanhVaLoFefo(bThanhPhan, ct.SoLuong * sl, danhSachLoDaTru);
                            }
                        }

                        var dsBanhCombo = combo.ChiTietComboMaus
                            .OrderBy(x => x.ViTriO)
                            .Select(x => new
                            {
                                ViTriOTrongHop = x.ViTriO,
                                MaBanh = x.MaBanh,
                                TenBanh = x.MaBanhNavigation?.TenBanh ?? "Bánh Trung Thu",
                                DonGia = Math.Round(x.MaBanhNavigation?.DonGiaLe ?? 0m, 0)
                            })
                            .ToList();

                        _db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDh = donHang.MaDh,
                            MaDotGh = maDotGh,
                            LoaiSanPham = 2,
                            MaCb = combo.MaCb,
                            TenSanPhamLuuCung = combo.TenCombo,
                            CauHinhHopJson = JsonSerializer.Serialize(dsBanhCombo, jsonOptions),
                            SoLuong = sl,
                            DonGiaLucMua = donGia,
                            ThanhTien = donGia * sl
                        });
                    }
                    else if (item.LoaiSanPham == 3)
                    {
                        int maVh = item.MaVh ?? dictVoHop.Keys.First();
                        var voHop = dictVoHop[maVh];
                        voHop.SltonKhoVo = Math.Max(0, voHop.SltonKhoVo - sl);
                        danhSachVoHopDaTru.Add(new NhatKyTruVoHop { MaVh = voHop.MaVh, SoLuongDaTru = sl });

                        int tongCalo = 0;
                        decimal tongGiaMotHop = Math.Round(voHop.GiaVoHopRieng, 0);
                        var dsBanhTrongHopJson = new List<object>();

                        foreach (var mb in item.DanhSachMaBanhTrongHop)
                        {
                            var bHop = dictBanh[mb];
                            tongCalo += bHop.ThongSoDinhDuong?.NangLuongCalo ?? 380;
                            tongGiaMotHop += Math.Round(bHop.DonGiaThucTe, 0);
                        }

                        string maBam = $"POS-VH{voHop.MaVh}-" + string.Join("-", item.DanhSachMaBanhTrongHop);
                        string tenHop = ChuanHoaChuoi(item.TenHopTuChon, $"Hộp Tự Chọn Tại Quầy ({voHop.TenVoHop})");

                        var hopTc = new HopQuaTuChon
                        {
                            MaVh = voHop.MaVh,
                            MaNd = khachMember?.MaNd,
                            MaPhienKvl = null,
                            TenHopKhachDat = tenHop,
                            MaBamCauHinh = maBam.Length > 140 ? maBam[..140] : maBam,
                            TongCaloHop = tongCalo,
                            TongGiaMotHop = tongGiaMotHop,
                            DaLuuMauYeuThich = false,
                            NgayTao = LayThoiGianHeThong()
                        };
                        _db.HopQuaTuChons.Add(hopTc);
                        await _db.SaveChangesAsync();

                        for (int viTri = 0; viTri < item.DanhSachMaBanhTrongHop.Count; viTri++)
                        {
                            int mb = item.DanhSachMaBanhTrongHop[viTri];
                            var bHop = dictBanh[mb];
                            decimal giaBanh = Math.Round(bHop.DonGiaThucTe, 0);

                            _db.ChiTietHopTuChons.Add(new ChiTietHopTuChon
                            {
                                MaHopTc = hopTc.MaHopTc,
                                MaBanh = bHop.MaBanh,
                                ViTriOtrongHop = viTri + 1,
                                DonGiaBanhLucChon = giaBanh
                            });

                            dsBanhTrongHopJson.Add(new
                            {
                                ViTriOTrongHop = viTri + 1,
                                MaBanh = bHop.MaBanh,
                                TenBanh = bHop.TenBanh,
                                DonGia = giaBanh
                            });

                            TruTonKhoBanhVaLoFefo(bHop, sl, danhSachLoDaTru);
                        }

                        _db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDh = donHang.MaDh,
                            MaDotGh = maDotGh,
                            LoaiSanPham = 3,
                            MaHopTc = hopTc.MaHopTc,
                            TenSanPhamLuuCung = $"{tenHop} ({voHop.TenVoHop})",
                            CauHinhHopJson = JsonSerializer.Serialize(dsBanhTrongHopJson, jsonOptions),
                            SoLuong = sl,
                            DonGiaLucMua = tongGiaMotHop,
                            ThanhTien = tongGiaMotHop * sl
                        });
                    }
                }

                int diemCong = 0;
                if (khachMember != null)
                {
                    diemCong = tongThanhToanCuoi > 0m ? (int)(tongThanhToanCuoi / 10000m) : 0;
                    khachMember.DiemTichLuy += diemCong;
                    var (hangVipMoi, _, _, _) = await DichVuHoiVien.TinhThongTinHangVipAsync(_db, khachMember.MaNd, khachMember.DiemTichLuy);
                    var (_, pctMoi) = ChuanHoaHangVip(hangVipMoi);
                    khachMember.HangTv = pctMoi >= phanTramGiamVip ? hangVipMoi : hangVipLucDat;
                    donHang.DaCongDiemThuong = diemCong > 0;
                }

                _db.PhieuThuChis.Add(new PhieuThuChi
                {
                    MaPhieuHienThi = $"PTH-{DateTime.Now:yyMMddHHmmss}",
                    LoaiPhieu = "THU",
                    HangMuc = "Thu tiền bán hàng POS tại quầy",
                    SoTien = donHang.TongThanhToanCuoi,
                    PhuongThuc = donHang.PhuongThucTt,
                    MaDonHangLienKet = donHang.MaDh,
                    NguoiNopNhan = donHang.HoTenNguoiMua,
                    GhiChu = $"Thu tiền hóa đơn POS #{donHang.MaTraCuuDh}",
                    NgayLap = LayThoiGianHeThong()
                });

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, meta =>
                {
                    meta.KenhBan = "POS_TAI_QUAY";
                    meta.DanhSachLoDaTru = danhSachLoDaTru;
                    meta.DanhSachVoHopDaTru = danhSachVoHopDaTru;
                });

                return new
                {
                    thanhCong = true,
                    maDon = donHang.MaTraCuuDh,
                    maDh = donHang.MaDh,
                    ngayDat = donHang.NgayDat.ToString("dd/MM/yyyy HH:mm"),
                    hoTenKhach = donHang.HoTenNguoiMua,
                    sdtKhach = donHang.SdtnguoiMua,
                    hangTv = hangVipLucDat,
                    phanTramGiamVip,
                    tienGiamVip,
                    diemDaDung,
                    tienGiamDiem,
                    tongTienHang = donHang.TongTienHang,
                    tienChietKhauB2B = donHang.TienChietKhauB2b,
                    tienGiamVoucher = donHang.TienGiamVoucher,
                    phiInLogo = donHang.PhiInLogo,
                    phiVanChuyen = donHang.TongPhiVanChuyen,
                    tongThanhToan = donHang.TongThanhToanCuoi,
                    phuongThucTt = donHang.PhuongThucTt,
                    cheDoMua = laGiaoHang ? "GIAO_HANG_QR" : "TAI_QUAY",
                    maTokenQr,
                    diemCong,
                    diemHienTaiMoi = khachMember?.DiemTichLuy ?? 0
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new
                {
                    thanhCong = false,
                    thongBao = $"Lỗi xử lý thanh toán POS: {ex.Message}"
                };
            }
        }
    }
}
