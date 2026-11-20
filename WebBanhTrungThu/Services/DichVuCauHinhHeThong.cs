using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Services
{
    public class CauHinhHangHoaMoRong
    {
        public string Key { get; set; } = string.Empty; // "BANH_1", "COMBO_1"
        public string MaVach { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = "Đồng Khánh";
        public string NhaCungCap { get; set; } = "Lò Bánh Đồng Khánh Chính Gốc";
        public string ViTriKho { get; set; } = "Kệ A1";
        public string DonViTinh { get; set; } = "Cái";
        public bool BanTrucTiep { get; set; } = true;
        public int? MaDmCombo { get; set; }
        public string? TenDmCombo { get; set; }
        public int? TonKhoCombo { get; set; }
        public int? TrongLuongComboGram { get; set; }
        public List<string> DanhSachAnh { get; set; } = new();
    }

    public class CauHinhThanhToanQr
    {
        public string SoTaiKhoanMoMo { get; set; } = "0814585526";
        public string SoDienThoaiHienThi { get; set; } = "0814 585 526";
        public string TenChuTaiKhoan { get; set; } = "LE MINH DUC";
        public string TenNganHangVi { get; set; } = "MOMO";
        public string MaBinNganHang { get; set; } = "970422";
        public string CuPhapChuyenKhoan { get; set; } = "NGUYETCAT {MaTraCuuDh}";
        public int ThoiGianChoPhut { get; set; } = 15;
        public string? SoTaiKhoanNganHang { get; set; }
        public string? TenNganHangVietQr { get; set; }

        [JsonIgnore]
        public string MaNganHang
        {
            get => TenNganHangVi;
            set => TenNganHangVi = value;
        }

        [JsonIgnore]
        public string MaBinVietQr
        {
            get => MaBinNganHang;
            set => MaBinNganHang = value;
        }

        [JsonIgnore]
        public string MauNoiDungCk
        {
            get => CuPhapChuyenKhoan;
            set => CuPhapChuyenKhoan = value;
        }
    }

    public class PhieuThuChiMoRong
    {
        public string MaPhieu { get; set; } = string.Empty;
        public int? MaDh { get; set; }
        public string MaTraCuuDh { get; set; } = string.Empty;
        public string LoaiPhieu { get; set; } = "THU";
        public string PhuongThuc { get; set; } = "MOMO_QR";
        public decimal SoTien { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string NguoiNopNhan { get; set; } = string.Empty;
        public string SdtNguoiNopNhan { get; set; } = string.Empty;
        public DateTime ThoiGianTao { get; set; } = DateTime.Now;

        [JsonIgnore]
        public int? MaDonHang
        {
            get => MaDh;
            set => MaDh = value;
        }

        [JsonIgnore]
        public DateTime ThoiGian
        {
            get => ThoiGianTao;
            set => ThoiGianTao = value;
        }
    }

    public class NhatKyTruLoFefo
    {
        public int MaLo { get; set; }
        public int MaBanh { get; set; }
        public int SoLuongDaTru { get; set; }
    }

    public class NhatKyTruVoHop
    {
        public int MaVh { get; set; }
        public int SoLuongDaTru { get; set; }
    }

    public class ThongTinThiep3DMeta
    {
        public string MaTokenThiepQr { get; set; } = string.Empty;
        public string TenNguoiNhanThiep { get; set; } = string.Empty;
        public string TenNguoiGuiThiep { get; set; } = string.Empty;
        public string? LoiChucThiep { get; set; }
        public string? MauGiaoDienThiep { get; set; }
    }

    public class ThongTinDonHangMoRong
    {
        public int MaDh { get; set; }
        public string KenhBan { get; set; } = "WEB_ONLINE";
        public decimal? SoTienKhachDaTra { get; set; }
        public string? MaNganHangHoan { get; set; }
        public string? SoTaiKhoanHoan { get; set; }
        public string? TenChuTkHoan { get; set; }
        public string? LyDoHuyHoan { get; set; }
        public string? GhiChuDieuChinhAdmin { get; set; }
        public string? ThoiGianCapNhatCuoi { get; set; }
        public string? NguoiCapNhatCuoi { get; set; }
        public DateTime? HanThanhToan { get; set; }
        public bool DaRollbackKho { get; set; }
        public int DiemDaDung { get; set; }
        public decimal TienGiamDiem { get; set; }
        public string? HangTvLucDat { get; set; }
        public decimal PhanTramGiamVip { get; set; }
        public decimal TienGiamVip { get; set; }
        public bool DaCongDiemThuong { get; set; }
        public int DiemThuongDaCong { get; set; }
        public bool DaHoanDiemKhiHuy { get; set; }
        public List<NhatKyTruLoFefo> DanhSachLoDaTru { get; set; } = new();
        public List<NhatKyTruVoHop> DanhSachVoHopDaTru { get; set; } = new();
        public Dictionary<string, ThongTinThiep3DMeta> Thiep3DTheoToken { get; set; } = new();

        [JsonIgnore]
        public bool DaHoanKhoDoHetHan
        {
            get => DaRollbackKho;
            set => DaRollbackKho = value;
        }
    }

    /// <summary>
    /// Dịch vụ SQL-First truy xuất và đồng bộ dữ liệu trực tiếp qua EF Core trên các bảng SQL Server thật:
    /// ThuongHieu, NhaCungCap, SanPhamBanh, ComboMauCoSan, DonHang, NhatKyTruKhoDonHang,
    /// ThiepChucMung3D, PhieuThuChi và khóa cài đặt hệ thống CAU_HINH_QR_MOMO trong CauHinhHeThong.
    /// </summary>
    public class DichVuCauHinhHeThong : DichVuCoSo, IDichVuCauHinhHeThong
    {
        public const string KHOA_CAU_HINH_QR_MOMO = "CAU_HINH_QR_MOMO";

        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly object _lock = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DichVuCauHinhHeThong(CSDLBanhTrungThuContext context, IServiceScopeFactory scopeFactory) : base(context)
        {
            _scopeFactory = scopeFactory;
        }

        public DuLieuMoRongHeThongRoot DocDuLieu()
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                return DocDuLieuTuSqlInternal(db);
            }
        }

        public void LuuDuLieu(DuLieuMoRongHeThongRoot store)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                LuuDuLieuVaoSqlInternal(db, store);
            }
        }

        private DuLieuMoRongHeThongRoot DocDuLieuTuSqlInternal(CSDLBanhTrungThuContext db)
        {
            var store = new DuLieuMoRongHeThongRoot();

            // 1. Đọc danh sách Thương hiệu từ dbo.ThuongHieu
            var dsThuongHieuDb = db.ThuongHieus
                .AsNoTracking()
                .Where(t => t.DangHoatDong)
                .OrderBy(t => t.MaThuongHieu)
                .Select(t => t.TenThuongHieu)
                .ToList();
            if (dsThuongHieuDb.Count > 0)
            {
                store.DanhSachThuongHieu = dsThuongHieuDb;
            }

            // 2. Đọc danh sách Nhà cung cấp từ dbo.NhaCungCap
            var dsNccDb = db.NhaCungCaps
                .AsNoTracking()
                .Where(n => n.DangHoatDong)
                .OrderBy(n => n.MaCodeNcc)
                .ThenBy(n => n.MaNcc)
                .Select(n => n.TenNcc)
                .ToList();
            if (dsNccDb.Count > 0)
            {
                store.DanhSachNhaCungCap = dsNccDb;
            }

            // 3. Đọc Vị trí kho & Đơn vị tính thực tế từ dbo.SanPhamBanh & dbo.ComboMauCoSan
            var dsBanh = db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.MaNccMacDinhNavigation)
                .OrderBy(b => b.MaBanh)
                .ToList();

            var dsCombo = db.ComboMauCoSans
                .AsNoTracking()
                .Include(c => c.MaThuongHieuNavigation)
                .Include(c => c.MaDmNavigation)
                .OrderBy(c => c.MaCb)
                .ToList();

            var setViTri = new HashSet<string>(new[] { "Kệ A1", "Kệ A2", "Kệ B1", "Kệ B2", "Tủ Mát C1" }, StringComparer.OrdinalIgnoreCase);
            var setDvt = new HashSet<string>(new[] { "Cái", "Hộp", "Lốc 4 cái", "Thùng 20 hộp" }, StringComparer.OrdinalIgnoreCase);

            foreach (var vt in dsBanh.Select(b => b.ViTriKho).Concat(dsCombo.Select(c => c.ViTriKho)))
            {
                if (!string.IsNullOrWhiteSpace(vt)) setViTri.Add(vt.Trim());
            }
            foreach (var dvt in dsBanh.Select(b => b.DonViTinh).Concat(dsCombo.Select(c => c.DonViTinh)))
            {
                if (!string.IsNullOrWhiteSpace(dvt)) setDvt.Add(dvt.Trim());
            }
            store.DanhSachViTriKho = setViTri.ToList();
            store.DanhSachDonViTinh = setDvt.ToList();

            // 4. Đọc liên kết hình ảnh từ dbo.LienKetHinhAnh & dbo.KhoHinhAnh
            var lienKetAnhs = db.LienKetHinhAnhs
                .AsNoTracking()
                .Include(l => l.MaHaNavigation)
                .Where(l => l.LoaiDoiTuong == "BANH" || l.LoaiDoiTuong == "COMBO")
                .OrderBy(l => l.ThuTuHienThi)
                .ToList();

            var anhTheoKhoa = lienKetAnhs
                .Where(l => l.MaHaNavigation != null && !string.IsNullOrWhiteSpace(l.MaHaNavigation.UrlHienThi))
                .GroupBy(l => $"{l.LoaiDoiTuong}_{l.MaDoiTuong}")
                .ToDictionary(g => g.Key, g => g.Select(x => x.MaHaNavigation.UrlHienThi).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            store.Items = new Dictionary<string, CauHinhHangHoaMoRong>();
            foreach (var b in dsBanh)
            {
                string key = $"BANH_{b.MaBanh}";
                store.Items[key] = new CauHinhHangHoaMoRong
                {
                    Key = key,
                    MaVach = string.IsNullOrWhiteSpace(b.MaVach) ? $"8934680010{b.MaBanh:D3}" : b.MaVach,
                    ThuongHieu = b.ThuongHieu,
                    NhaCungCap = b.NhaCungCap,
                    ViTriKho = string.IsNullOrWhiteSpace(b.ViTriKho) ? "Kệ A1" : b.ViTriKho,
                    DonViTinh = string.IsNullOrWhiteSpace(b.DonViTinh) ? "Cái" : b.DonViTinh,
                    BanTrucTiep = b.BanTrucTiep,
                    DanhSachAnh = anhTheoKhoa.TryGetValue(key, out var dsAnhBanh) ? dsAnhBanh : new List<string>()
                };
            }

            foreach (var cb in dsCombo)
            {
                string key = $"COMBO_{cb.MaCb}";
                store.Items[key] = new CauHinhHangHoaMoRong
                {
                    Key = key,
                    MaVach = string.IsNullOrWhiteSpace(cb.MaVach) ? $"8934680990{cb.MaCb:D3}" : cb.MaVach,
                    ThuongHieu = cb.ThuongHieu,
                    NhaCungCap = cb.NhaCungCap,
                    ViTriKho = string.IsNullOrWhiteSpace(cb.ViTriKho) ? "Kệ B1" : cb.ViTriKho,
                    DonViTinh = string.IsNullOrWhiteSpace(cb.DonViTinh) ? "Hộp" : cb.DonViTinh,
                    BanTrucTiep = cb.BanTrucTiep,
                    MaDmCombo = cb.MaDm,
                    TenDmCombo = cb.TenDm,
                    TonKhoCombo = cb.TongTonKho,
                    TrongLuongComboGram = cb.TrongLuongGram,
                    DanhSachAnh = anhTheoKhoa.TryGetValue(key, out var dsAnhCombo) ? dsAnhCombo : new List<string>()
                };
            }

            // 5. Đọc cấu hình QR MoMo từ khóa duy nhất CAU_HINH_QR_MOMO trong dbo.CauHinhHeThong
            store.CauHinhQr = DocHoacKhoiTaoCauHinhQrInternal(db);

            // 6. Đọc thông tin Đơn hàng + Nhật ký trừ kho FEFO + Thiệp 3D từ các bảng vật lý
            var dsDonHang = db.DonHangs.AsNoTracking().ToList();
            var dsNhatKy = db.NhatKyTruKhoDonHangs.AsNoTracking().ToList().ToLookup(n => n.MaDh);
            var dsThiepDb = db.ThiepChucMung3ds.AsNoTracking().ToList().ToLookup(t => t.MaDonHang);

            store.DonHangs = new Dictionary<int, ThongTinDonHangMoRong>();
            foreach (var dh in dsDonHang)
            {
                store.DonHangs[dh.MaDh] = TaoThongTinDonHangMoRongTuSql(dh, dsNhatKy[dh.MaDh], dsThiepDb[dh.MaDh]);
            }

            // 7. Đọc danh sách Phiếu Thu Chi từ bảng vật lý dbo.PhieuThuChi
            var dsPhieuDb = db.PhieuThuChis
                .AsNoTracking()
                .OrderByDescending(p => p.NgayLap)
                .ThenByDescending(p => p.MaPhieu)
                .ToList();

            var mapMaTraCuu = dsDonHang.ToDictionary(d => d.MaDh, d => (d.MaTraCuuDh, d.SdtnguoiMua));

            store.DanhSachPhieuThuChi = dsPhieuDb.Select(p =>
            {
                string maTraCuu = string.Empty;
                string sdt = string.Empty;
                if (p.MaDonHangLienKet.HasValue && mapMaTraCuu.TryGetValue(p.MaDonHangLienKet.Value, out var dhInfo))
                {
                    maTraCuu = dhInfo.MaTraCuuDh;
                    sdt = dhInfo.SdtnguoiMua ?? string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(p.GhiChu))
                {
                    var match = Regex.Match(p.GhiChu, @"(TT2026-\d{4}|POS26-\d{5})", RegexOptions.IgnoreCase);
                    if (match.Success) maTraCuu = match.Value.ToUpperInvariant();
                }

                return new PhieuThuChiMoRong
                {
                    MaPhieu = string.IsNullOrWhiteSpace(p.MaPhieuHienThi) ? $"PTH-{p.MaPhieu:D6}" : p.MaPhieuHienThi,
                    MaDh = p.MaDonHangLienKet,
                    MaTraCuuDh = maTraCuu,
                    LoaiPhieu = p.LoaiPhieu,
                    PhuongThuc = p.PhuongThuc,
                    SoTien = p.SoTien,
                    NoiDung = p.GhiChu ?? p.HangMuc,
                    NguoiNopNhan = p.NguoiNopNhan,
                    SdtNguoiNopNhan = sdt,
                    ThoiGianTao = p.NgayLap
                };
            }).ToList();

            return store;
        }

        private void LuuDuLieuVaoSqlInternal(CSDLBanhTrungThuContext db, DuLieuMoRongHeThongRoot store)
        {
            store.CauHinhQr ??= new CauHinhThanhToanQr();

            // 1. Lưu cấu hình QR MoMo vào khóa CAU_HINH_QR_MOMO trong dbo.CauHinhHeThong
            LuuCauHinhQrInternal(db, store.CauHinhQr);

            // 2. Đồng bộ Thương hiệu mới vào dbo.ThuongHieu
            var dsThuongHieu = db.ThuongHieus.ToList();
            if (store.DanhSachThuongHieu != null)
            {
                foreach (var tenTh in store.DanhSachThuongHieu)
                {
                    if (string.IsNullOrWhiteSpace(tenTh)) continue;
                    string tenSach = tenTh.Trim();
                    if (!dsThuongHieu.Any(t => string.Equals(t.TenThuongHieu, tenSach, StringComparison.OrdinalIgnoreCase)))
                    {
                        var thMoi = new ThuongHieu
                        {
                            MaCodeThuongHieu = SinhMaCodeThuongHieu(tenSach, dsThuongHieu),
                            TenThuongHieu = tenSach,
                            DangHoatDong = true
                        };
                        db.ThuongHieus.Add(thMoi);
                        dsThuongHieu.Add(thMoi);
                    }
                }
                db.SaveChanges();
            }

            // 3. Đồng bộ thuộc tính hàng hóa mở rộng lên cột thật dbo.SanPhamBanh & dbo.ComboMauCoSan
            if (store.Items != null && store.Items.Count > 0)
            {
                var dsNcc = db.NhaCungCaps.AsNoTracking().ToList();
                var dsBanh = db.SanPhamBanhs.ToList();
                foreach (var b in dsBanh)
                {
                    if (!store.Items.TryGetValue($"BANH_{b.MaBanh}", out var meta)) continue;
                    b.MaVach = string.IsNullOrWhiteSpace(meta.MaVach) ? $"8934680010{b.MaBanh:D3}" : meta.MaVach.Trim();
                    b.ViTriKho = string.IsNullOrWhiteSpace(meta.ViTriKho) ? "Kệ A1" : meta.ViTriKho.Trim();
                    b.DonViTinh = string.IsNullOrWhiteSpace(meta.DonViTinh) ? "Cái" : meta.DonViTinh.Trim();
                    b.BanTrucTiep = meta.BanTrucTiep;

                    if (!string.IsNullOrWhiteSpace(meta.ThuongHieu))
                    {
                        var th = TimHoacTaoThuongHieu(db, dsThuongHieu, meta.ThuongHieu.Trim());
                        b.MaThuongHieu = th.MaThuongHieu;
                    }
                    if (!string.IsNullOrWhiteSpace(meta.NhaCungCap))
                    {
                        var ncc = dsNcc.FirstOrDefault(n => string.Equals(n.TenNcc, meta.NhaCungCap.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (ncc != null) b.MaNccMacDinh = ncc.MaNcc;
                    }
                }

                var dsCombo = db.ComboMauCoSans.ToList();
                foreach (var cb in dsCombo)
                {
                    if (!store.Items.TryGetValue($"COMBO_{cb.MaCb}", out var meta)) continue;
                    cb.MaVach = string.IsNullOrWhiteSpace(meta.MaVach) ? $"8934680990{cb.MaCb:D3}" : meta.MaVach.Trim();
                    cb.ViTriKho = string.IsNullOrWhiteSpace(meta.ViTriKho) ? "Kệ B1" : meta.ViTriKho.Trim();
                    cb.DonViTinh = string.IsNullOrWhiteSpace(meta.DonViTinh) ? "Hộp" : meta.DonViTinh.Trim();
                    cb.BanTrucTiep = meta.BanTrucTiep;
                    if (meta.MaDmCombo.HasValue && meta.MaDmCombo.Value > 0) cb.MaDm = meta.MaDmCombo.Value;
                    if (meta.TonKhoCombo.HasValue && meta.TonKhoCombo.Value >= 0) cb.TongTonKho = meta.TonKhoCombo.Value;
                    if (meta.TrongLuongComboGram.HasValue && meta.TrongLuongComboGram.Value > 0) cb.TrongLuongGram = meta.TrongLuongComboGram.Value;

                    if (!string.IsNullOrWhiteSpace(meta.ThuongHieu))
                    {
                        var th = TimHoacTaoThuongHieu(db, dsThuongHieu, meta.ThuongHieu.Trim());
                        cb.MaThuongHieu = th.MaThuongHieu;
                    }
                }
            }

            // 4. Đồng bộ thông tin mở rộng Đơn hàng, Nhật ký FEFO & Thiệp 3D lên bảng SQL thật
            if (store.DonHangs != null && store.DonHangs.Count > 0)
            {
                var dsMaDh = store.DonHangs.Keys.ToList();
                var mapDh = db.DonHangs.Where(d => dsMaDh.Contains(d.MaDh)).ToDictionary(d => d.MaDh);
                var mapNhatKy = db.NhatKyTruKhoDonHangs.Where(n => dsMaDh.Contains(n.MaDh)).ToLookup(n => n.MaDh);
                var tokenDaCo = db.ThiepChucMung3ds.ToDictionary(t => t.MaTokenQr, StringComparer.OrdinalIgnoreCase);

                foreach (var kvDh in store.DonHangs)
                {
                    int maDh = kvDh.Key;
                    var dhMeta = kvDh.Value;
                    if (dhMeta == null) continue;

                    if (mapDh.TryGetValue(maDh, out var dhEntity))
                    {
                        GhiThongTinMoRongLenDonHang(dhEntity, dhMeta);
                        DongBoNhatKyTruKhoDonHang(db, dhEntity, dhMeta, mapNhatKy[maDh].ToList());
                    }

                    if (dhMeta.Thiep3DTheoToken != null)
                    {
                        DongBoThiep3D(db, maDh, dhMeta.Thiep3DTheoToken, tokenDaCo);
                    }
                }
            }

            // 5. Đồng bộ DanhSachPhieuThuChi vào bảng vật lý dbo.PhieuThuChi
            if (store.DanhSachPhieuThuChi != null && store.DanhSachPhieuThuChi.Count > 0)
            {
                var dsHienCo = db.PhieuThuChis.AsNoTracking().ToList();
                foreach (var p in store.DanhSachPhieuThuChi)
                {
                    string maHienThi = string.IsNullOrWhiteSpace(p.MaPhieu)
                        ? $"PTH-{DateTime.Now:yyMMddHHmmss}"
                        : p.MaPhieu.Trim();

                    bool daTonTai = dsHienCo.Any(x =>
                        string.Equals(x.MaPhieuHienThi, maHienThi, StringComparison.OrdinalIgnoreCase) ||
                        (p.MaDh.HasValue && x.MaDonHangLienKet == p.MaDh.Value && x.LoaiPhieu == p.LoaiPhieu));

                    if (!daTonTai)
                    {
                        db.PhieuThuChis.Add(new PhieuThuChi
                        {
                            MaPhieuHienThi = maHienThi,
                            LoaiPhieu = string.IsNullOrWhiteSpace(p.LoaiPhieu) ? "THU" : p.LoaiPhieu.Trim().ToUpperInvariant(),
                            HangMuc = p.LoaiPhieu == "CHI" ? "Chi hoàn tiền đơn hàng" : "Thu tiền bán hàng",
                            SoTien = p.SoTien,
                            PhuongThuc = string.IsNullOrWhiteSpace(p.PhuongThuc) ? "MOMO_QR" : p.PhuongThuc.Trim().ToUpperInvariant(),
                            MaDonHangLienKet = p.MaDh,
                            NguoiNopNhan = string.IsNullOrWhiteSpace(p.NguoiNopNhan) ? "Khách Hàng Nguyệt Cát" : p.NguoiNopNhan.Trim(),
                            GhiChu = string.IsNullOrWhiteSpace(p.NoiDung) ? $"Giao dịch đơn #{p.MaTraCuuDh}" : p.NoiDung.Trim(),
                            NgayLap = p.ThoiGianTao == default ? DateTime.Now : p.ThoiGianTao
                        });
                    }
                }
            }

            db.SaveChanges();
        }

        public void GanMetadataChoBanh(SanPhamBanh banh, DuLieuMoRongHeThongRoot store)
        {
            string key = $"BANH_{banh.MaBanh}";
            store.Items.TryGetValue(key, out var meta);

            banh.MaVach = !string.IsNullOrWhiteSpace(banh.MaVach)
                ? banh.MaVach
                : (!string.IsNullOrWhiteSpace(meta?.MaVach) ? meta.MaVach : $"8934680010{banh.MaBanh:D3}");

            if (banh.MaThuongHieuNavigation != null && !string.IsNullOrWhiteSpace(banh.MaThuongHieuNavigation.TenThuongHieu))
            {
                banh.ThuongHieu = banh.MaThuongHieuNavigation.TenThuongHieu;
            }
            else if (meta != null && !string.IsNullOrWhiteSpace(meta.ThuongHieu))
            {
                banh.ThuongHieu = meta.ThuongHieu;
            }

            if (banh.MaNccMacDinhNavigation != null && !string.IsNullOrWhiteSpace(banh.MaNccMacDinhNavigation.TenNcc))
            {
                banh.NhaCungCap = banh.MaNccMacDinhNavigation.TenNcc;
            }
            else if (meta != null && !string.IsNullOrWhiteSpace(meta.NhaCungCap))
            {
                banh.NhaCungCap = meta.NhaCungCap;
            }

            if (string.IsNullOrWhiteSpace(banh.ViTriKho))
            {
                banh.ViTriKho = !string.IsNullOrWhiteSpace(meta?.ViTriKho) ? meta.ViTriKho : "Kệ A1";
            }
            if (string.IsNullOrWhiteSpace(banh.DonViTinh))
            {
                banh.DonViTinh = !string.IsNullOrWhiteSpace(meta?.DonViTinh) ? meta.DonViTinh : "Cái";
            }
        }

        public void GanMetadataChoCombo(ComboMauCoSan combo, DuLieuMoRongHeThongRoot store)
        {
            string key = $"COMBO_{combo.MaCb}";
            store.Items.TryGetValue(key, out var meta);

            combo.MaVach = !string.IsNullOrWhiteSpace(combo.MaVach)
                ? combo.MaVach
                : (!string.IsNullOrWhiteSpace(meta?.MaVach) ? meta.MaVach : $"8934680990{combo.MaCb:D3}");

            if (combo.MaThuongHieuNavigation != null && !string.IsNullOrWhiteSpace(combo.MaThuongHieuNavigation.TenThuongHieu))
            {
                combo.ThuongHieu = combo.MaThuongHieuNavigation.TenThuongHieu;
            }
            else if (meta != null && !string.IsNullOrWhiteSpace(meta.ThuongHieu))
            {
                combo.ThuongHieu = meta.ThuongHieu;
            }

            if (meta != null && !string.IsNullOrWhiteSpace(meta.NhaCungCap))
            {
                combo.NhaCungCap = meta.NhaCungCap;
            }

            if (combo.MaDmNavigation != null && !string.IsNullOrWhiteSpace(combo.MaDmNavigation.TenDm))
            {
                combo.TenDm = combo.MaDmNavigation.TenDm;
            }
            else if (meta != null && !string.IsNullOrWhiteSpace(meta.TenDmCombo))
            {
                combo.TenDm = meta.TenDmCombo;
            }

            if (string.IsNullOrWhiteSpace(combo.ViTriKho))
            {
                combo.ViTriKho = !string.IsNullOrWhiteSpace(meta?.ViTriKho) ? meta.ViTriKho : "Kệ B1";
            }
            if (string.IsNullOrWhiteSpace(combo.DonViTinh))
            {
                combo.DonViTinh = !string.IsNullOrWhiteSpace(meta?.DonViTinh) ? meta.DonViTinh : "Hộp";
            }
            if (combo.TongTonKho <= 0 && meta?.TonKhoCombo > 0)
            {
                combo.TongTonKho = meta.TonKhoCombo.Value;
            }
            if (combo.TrongLuongGram <= 0)
            {
                combo.TrongLuongGram = meta?.TrongLuongComboGram > 0 ? meta.TrongLuongComboGram.Value : 600;
            }
        }

        public void GanMetadataChoDonHang(DonHang dh, DuLieuMoRongHeThongRoot? store = null)
        {
            if (dh.DotGiaoHangDaDiems == null || dh.DotGiaoHangDaDiems.Count == 0)
            {
                return;
            }

            Dictionary<string, ThongTinThiep3DMeta>? thiepTheoToken = null;
            if (store?.DonHangs != null && store.DonHangs.TryGetValue(dh.MaDh, out var meta))
            {
                thiepTheoToken = meta.Thiep3DTheoToken;
            }
            else
            {
                lock (_lock)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                    thiepTheoToken = db.ThiepChucMung3ds
                        .AsNoTracking()
                        .Where(t => t.MaDonHang == dh.MaDh)
                        .ToDictionary(
                            t => t.MaTokenQr,
                            t => new ThongTinThiep3DMeta
                            {
                                MaTokenThiepQr = t.MaTokenQr,
                                TenNguoiNhanThiep = t.TenNguoiNhan,
                                TenNguoiGuiThiep = t.TenNguoiGui,
                                LoiChucThiep = t.LoiChuc,
                                MauGiaoDienThiep = t.MauGiaoDien
                            },
                            StringComparer.OrdinalIgnoreCase);
                }
            }

            if (thiepTheoToken == null || thiepTheoToken.Count == 0) return;

            foreach (var dot in dh.DotGiaoHangDaDiems)
            {
                if (!string.IsNullOrWhiteSpace(dot.MaTokenThiepQr) &&
                    thiepTheoToken.TryGetValue(dot.MaTokenThiepQr, out var thiepMeta))
                {
                    dot.TenNguoiNhanThiep ??= thiepMeta.TenNguoiNhanThiep;
                    dot.TenNguoiGuiThiep ??= thiepMeta.TenNguoiGuiThiep;
                }
            }
        }

        public ThongTinDonHangMoRong LayMetadataDonHang(int maDh)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                var dh = db.DonHangs.AsNoTracking().FirstOrDefault(d => d.MaDh == maDh);
                if (dh == null) return new ThongTinDonHangMoRong { MaDh = maDh };

                var nhatKy = db.NhatKyTruKhoDonHangs.AsNoTracking().Where(n => n.MaDh == maDh).ToList();
                var thiep = db.ThiepChucMung3ds.AsNoTracking().Where(t => t.MaDonHang == maDh).ToList();

                return TaoThongTinDonHangMoRongTuSql(dh, nhatKy, thiep);
            }
        }

        public void CapNhatMetadataDonHang(int maDh, Action<ThongTinDonHangMoRong> capNhat)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                var dh = db.DonHangs.FirstOrDefault(d => d.MaDh == maDh);
                if (dh == null) return;

                var nhatKyHienTai = db.NhatKyTruKhoDonHangs.Where(n => n.MaDh == maDh).ToList();
                var thiepHienTai = db.ThiepChucMung3ds.Where(t => t.MaDonHang == maDh).ToList();

                var meta = TaoThongTinDonHangMoRongTuSql(dh, nhatKyHienTai, thiepHienTai);
                capNhat(meta);

                GhiThongTinMoRongLenDonHang(dh, meta);
                DongBoNhatKyTruKhoDonHang(db, dh, meta, nhatKyHienTai);

                if (meta.Thiep3DTheoToken != null && meta.Thiep3DTheoToken.Count > 0)
                {
                    var tokenDaCo = thiepHienTai.ToDictionary(t => t.MaTokenQr, StringComparer.OrdinalIgnoreCase);
                    DongBoThiep3D(db, maDh, meta.Thiep3DTheoToken, tokenDaCo);
                }

                db.SaveChanges();
            }
        }

        public CauHinhThanhToanQr LayCauHinhQr()
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                return DocHoacKhoiTaoCauHinhQrInternal(db);
            }
        }

        public CauHinhThanhToanQr LayCauHinhThanhToan() => LayCauHinhQr();

        public void CapNhatCauHinhQr(Action<CauHinhThanhToanQr> capNhat, int? maNdCapNhat = null)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                var cfg = DocHoacKhoiTaoCauHinhQrInternal(db);
                capNhat(cfg);
                LuuCauHinhQrInternal(db, cfg, maNdCapNhat);
                db.SaveChanges();
            }
        }

        public CauHinhThanhToanQr CapNhatCauHinhQr(string? soTaiKhoanMoMo, string? tenChuTaiKhoan, string? tenNganHangVi, string? maBinNganHang = null, string? cuPhap = null)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                var cfg = DocHoacKhoiTaoCauHinhQrInternal(db);

                string stkSach = (soTaiKhoanMoMo ?? "0814585526").Replace(" ", "").Trim();
                if (string.IsNullOrWhiteSpace(stkSach)) stkSach = "0814585526";

                string sdtHienThi = stkSach.Length == 10
                    ? $"{stkSach[..4]} {stkSach.Substring(4, 3)} {stkSach.Substring(7, 3)}"
                    : stkSach;

                cfg.SoTaiKhoanMoMo = stkSach;
                cfg.SoDienThoaiHienThi = sdtHienThi;
                cfg.TenChuTaiKhoan = string.IsNullOrWhiteSpace(tenChuTaiKhoan) ? "LE MINH DUC" : tenChuTaiKhoan.Trim().ToUpperInvariant();
                cfg.TenNganHangVi = string.IsNullOrWhiteSpace(tenNganHangVi) ? "MOMO" : tenNganHangVi.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(maBinNganHang))
                {
                    cfg.MaBinNganHang = maBinNganHang.Trim();
                }
                if (!string.IsNullOrWhiteSpace(cuPhap))
                {
                    cfg.CuPhapChuyenKhoan = cuPhap.Trim();
                }

                LuuCauHinhQrInternal(db, cfg);
                db.SaveChanges();
                return cfg;
            }
        }

        public void GhiNhanPhieuThuChi(PhieuThuChiMoRong phieu)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                string loai = string.IsNullOrWhiteSpace(phieu.LoaiPhieu) ? "THU" : phieu.LoaiPhieu.Trim().ToUpperInvariant();
                if (phieu.MaDh.HasValue && db.PhieuThuChis.Any(p => p.MaDonHangLienKet == phieu.MaDh.Value && p.LoaiPhieu == loai))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(phieu.MaPhieu))
                {
                    string tienTo = loai == "CHI" ? "PCH" : "PTH";
                    phieu.MaPhieu = $"{tienTo}-{DateTime.Now:yyMMddHHmmss}";
                }

                db.PhieuThuChis.Add(new PhieuThuChi
                {
                    MaPhieuHienThi = phieu.MaPhieu,
                    LoaiPhieu = loai,
                    HangMuc = loai == "CHI" ? "Chi hoàn tiền đơn hàng" : "Thu tiền bán hàng",
                    SoTien = phieu.SoTien,
                    PhuongThuc = string.IsNullOrWhiteSpace(phieu.PhuongThuc) ? "MOMO_QR" : phieu.PhuongThuc.Trim().ToUpperInvariant(),
                    MaDonHangLienKet = phieu.MaDh,
                    NguoiNopNhan = string.IsNullOrWhiteSpace(phieu.NguoiNopNhan) ? "Khách Hàng Nguyệt Cát" : phieu.NguoiNopNhan.Trim(),
                    GhiChu = string.IsNullOrWhiteSpace(phieu.NoiDung) ? $"Giao dịch đơn #{phieu.MaTraCuuDh}" : phieu.NoiDung.Trim(),
                    NgayLap = phieu.ThoiGianTao == default ? DateTime.Now : phieu.ThoiGianTao
                });

                db.SaveChanges();
            }
        }

        public void GhiPhieuThuChi(PhieuThuChiMoRong phieu) => GhiNhanPhieuThuChi(phieu);

        public ThongTinThiep3DMeta? LayThongTinThiepTheoToken(string? maToken)
        {
            if (string.IsNullOrWhiteSpace(maToken)) return null;
            string tokenChuan = maToken.Trim();

            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                var thiepDb = db.ThiepChucMung3ds
                    .AsNoTracking()
                    .FirstOrDefault(t => t.MaTokenQr == tokenChuan);

                if (thiepDb != null)
                {
                    return new ThongTinThiep3DMeta
                    {
                        MaTokenThiepQr = thiepDb.MaTokenQr,
                        TenNguoiNhanThiep = thiepDb.TenNguoiNhan,
                        TenNguoiGuiThiep = thiepDb.TenNguoiGui,
                        LoiChucThiep = thiepDb.LoiChuc,
                        MauGiaoDienThiep = thiepDb.MauGiaoDien
                    };
                }

                var dotDb = db.DotGiaoHangDaDiems
                    .AsNoTracking()
                    .Include(d => d.MaDhNavigation)
                    .FirstOrDefault(d => d.MaTokenThiepQr == tokenChuan && d.CoKemThiepQr);

                if (dotDb != null)
                {
                    return new ThongTinThiep3DMeta
                    {
                        MaTokenThiepQr = tokenChuan,
                        TenNguoiNhanThiep = dotDb.TenNguoiNhan,
                        TenNguoiGuiThiep = dotDb.MaDhNavigation?.HoTenNguoiMua ?? "Khách Hàng Nguyệt Cát",
                        LoiChucThiep = dotDb.LoiChucThiep,
                        MauGiaoDienThiep = dotDb.MauGiaoDienThiep
                    };
                }

                return null;
            }
        }

        public ThongTinThiep3DMeta? LayThongTinThiepTheoToken(int maDh, string? maToken)
        {
            if (string.IsNullOrWhiteSpace(maToken)) return null;
            string tokenChuan = maToken.Trim();

            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                var thiepDb = db.ThiepChucMung3ds
                    .AsNoTracking()
                    .FirstOrDefault(t => t.MaTokenQr == tokenChuan && (t.MaDonHang == maDh || maDh <= 0));

                if (thiepDb != null)
                {
                    return new ThongTinThiep3DMeta
                    {
                        MaTokenThiepQr = thiepDb.MaTokenQr,
                        TenNguoiNhanThiep = thiepDb.TenNguoiNhan,
                        TenNguoiGuiThiep = thiepDb.TenNguoiGui,
                        LoiChucThiep = thiepDb.LoiChuc,
                        MauGiaoDienThiep = thiepDb.MauGiaoDien
                    };
                }
            }

            return LayThongTinThiepTheoToken(maToken);
        }

        private CauHinhThanhToanQr DocHoacKhoiTaoCauHinhQrInternal(CSDLBanhTrungThuContext db)
        {
            var rowQr = db.CauHinhHeThongs
                .AsNoTracking()
                .FirstOrDefault(c => c.KhoaCauHinh == KHOA_CAU_HINH_QR_MOMO);

            if (rowQr != null && !string.IsNullOrWhiteSpace(rowQr.GiaTriJson))
            {
                try
                {
                    var cfg = JsonSerializer.Deserialize<CauHinhThanhToanQr>(rowQr.GiaTriJson, _jsonOptions);
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.SoTaiKhoanMoMo))
                    {
                        return cfg;
                    }
                }
                catch
                {
                    // Khởi tạo lại mặc định bên dưới
                }
            }

            var macDinh = new CauHinhThanhToanQr();
            LuuCauHinhQrInternal(db, macDinh);
            db.SaveChanges();
            return macDinh;
        }

        private void LuuCauHinhQrInternal(CSDLBanhTrungThuContext db, CauHinhThanhToanQr cfg, int? maNdCapNhat = null)
        {
            int? maNdHopLe = null;
            if (maNdCapNhat.HasValue && db.NguoiDungs.AsNoTracking().Any(u => u.MaNd == maNdCapNhat.Value))
            {
                maNdHopLe = maNdCapNhat.Value;
            }

            string jsonQr = JsonSerializer.Serialize(cfg, _jsonOptions);
            var rowQr = db.CauHinhHeThongs.FirstOrDefault(c => c.KhoaCauHinh == KHOA_CAU_HINH_QR_MOMO);
            if (rowQr == null)
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = KHOA_CAU_HINH_QR_MOMO,
                    GiaTriJson = jsonQr,
                    MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                    NgayCapNhat = DateTime.Now,
                    MaNdCapNhat = maNdHopLe
                });
            }
            else
            {
                rowQr.GiaTriJson = jsonQr;
                rowQr.NgayCapNhat = DateTime.Now;
                if (maNdHopLe.HasValue) rowQr.MaNdCapNhat = maNdHopLe.Value;
            }
        }

        private static ThongTinDonHangMoRong TaoThongTinDonHangMoRongTuSql(
            DonHang dh,
            IEnumerable<NhatKyTruKhoDonHang> nhatKys,
            IEnumerable<ThiepChucMung3d> thieps)
        {
            var meta = new ThongTinDonHangMoRong
            {
                MaDh = dh.MaDh,
                KenhBan = string.IsNullOrWhiteSpace(dh.KenhBan) ? "WEB_ONLINE" : dh.KenhBan,
                SoTienKhachDaTra = dh.SoTienKhachDaTra,
                MaNganHangHoan = dh.MaNganHangHoan,
                SoTaiKhoanHoan = dh.SoTaiKhoanHoan,
                TenChuTkHoan = dh.TenChuTkHoan,
                LyDoHuyHoan = dh.LyDoHuy,
                GhiChuDieuChinhAdmin = dh.GhiChuDieuChinhAdmin,
                ThoiGianCapNhatCuoi = dh.ThoiGianCapNhatCuoi?.ToString("dd/MM/yyyy HH:mm"),
                NguoiCapNhatCuoi = dh.NguoiCapNhatCuoi,
                HanThanhToan = dh.HanThanhToanQr,
                DaRollbackKho = dh.DaRollbackKho,
                DiemDaDung = dh.SoDiemDaDung,
                TienGiamDiem = dh.TienGiamDiemVip,
                PhanTramGiamVip = dh.PhanTramGiamVip,
                TienGiamVip = dh.TienGiamHangVip,
                DaCongDiemThuong = dh.DaCongDiemThuong,
                DaHoanDiemKhiHuy = dh.DaHoanDiemKhiHuy
            };

            foreach (var nk in nhatKys)
            {
                if (string.Equals(nk.LoaiDoiTuong, "LO_BANH", StringComparison.OrdinalIgnoreCase))
                {
                    meta.DanhSachLoDaTru.Add(new NhatKyTruLoFefo
                    {
                        MaLo = nk.MaLo ?? 0,
                        MaBanh = nk.MaBanh ?? 0,
                        SoLuongDaTru = nk.SoLuongDaTru
                    });
                }
                else if (string.Equals(nk.LoaiDoiTuong, "VO_HOP", StringComparison.OrdinalIgnoreCase))
                {
                    meta.DanhSachVoHopDaTru.Add(new NhatKyTruVoHop
                    {
                        MaVh = nk.MaVh ?? 0,
                        SoLuongDaTru = nk.SoLuongDaTru
                    });
                }
            }

            foreach (var t in thieps)
            {
                meta.Thiep3DTheoToken[t.MaTokenQr] = new ThongTinThiep3DMeta
                {
                    MaTokenThiepQr = t.MaTokenQr,
                    TenNguoiNhanThiep = t.TenNguoiNhan,
                    TenNguoiGuiThiep = t.TenNguoiGui,
                    LoiChucThiep = t.LoiChuc,
                    MauGiaoDienThiep = t.MauGiaoDien
                };
            }

            return meta;
        }

        private static void GhiThongTinMoRongLenDonHang(DonHang dh, ThongTinDonHangMoRong meta)
        {
            if (!string.IsNullOrWhiteSpace(meta.KenhBan)) dh.KenhBan = meta.KenhBan.Trim();
            if (meta.SoTienKhachDaTra.HasValue) dh.SoTienKhachDaTra = meta.SoTienKhachDaTra;
            if (!string.IsNullOrWhiteSpace(meta.MaNganHangHoan)) dh.MaNganHangHoan = meta.MaNganHangHoan.Trim();
            if (!string.IsNullOrWhiteSpace(meta.SoTaiKhoanHoan)) dh.SoTaiKhoanHoan = meta.SoTaiKhoanHoan.Trim();
            if (!string.IsNullOrWhiteSpace(meta.TenChuTkHoan)) dh.TenChuTkHoan = meta.TenChuTkHoan.Trim();
            if (!string.IsNullOrWhiteSpace(meta.LyDoHuyHoan)) dh.LyDoHuy = meta.LyDoHuyHoan.Trim();
            if (!string.IsNullOrWhiteSpace(meta.GhiChuDieuChinhAdmin)) dh.GhiChuDieuChinhAdmin = meta.GhiChuDieuChinhAdmin.Trim();
            if (!string.IsNullOrWhiteSpace(meta.NguoiCapNhatCuoi)) dh.NguoiCapNhatCuoi = meta.NguoiCapNhatCuoi.Trim();
            if (!string.IsNullOrWhiteSpace(meta.ThoiGianCapNhatCuoi))
            {
                dh.ThoiGianCapNhatCuoi = DateTime.TryParseExact(
                    meta.ThoiGianCapNhatCuoi.Trim(),
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dtExact)
                    ? dtExact
                    : (DateTime.TryParse(meta.ThoiGianCapNhatCuoi.Trim(), out var dtAny) ? dtAny : DateTime.Now);
            }
            if (meta.DaRollbackKho) dh.DaRollbackKho = true;
        }

        private static void DongBoNhatKyTruKhoDonHang(
            CSDLBanhTrungThuContext db,
            DonHang dh,
            ThongTinDonHangMoRong meta,
            List<NhatKyTruKhoDonHang> nhatKyHienTai)
        {
            if (nhatKyHienTai.Count == 0)
            {
                if (meta.DanhSachLoDaTru != null)
                {
                    foreach (var lo in meta.DanhSachLoDaTru)
                    {
                        db.NhatKyTruKhoDonHangs.Add(new NhatKyTruKhoDonHang
                        {
                            MaDh = dh.MaDh,
                            LoaiDoiTuong = "LO_BANH",
                            MaLo = lo.MaLo > 0 ? lo.MaLo : null,
                            MaBanh = lo.MaBanh > 0 ? lo.MaBanh : null,
                            SoLuongDaTru = lo.SoLuongDaTru,
                            DaHoanKho = meta.DaRollbackKho,
                            NgayGhi = DateTime.Now
                        });
                    }
                }
                if (meta.DanhSachVoHopDaTru != null)
                {
                    foreach (var vo in meta.DanhSachVoHopDaTru)
                    {
                        db.NhatKyTruKhoDonHangs.Add(new NhatKyTruKhoDonHang
                        {
                            MaDh = dh.MaDh,
                            LoaiDoiTuong = "VO_HOP",
                            MaVh = vo.MaVh > 0 ? vo.MaVh : null,
                            SoLuongDaTru = vo.SoLuongDaTru,
                            DaHoanKho = meta.DaRollbackKho,
                            NgayGhi = DateTime.Now
                        });
                    }
                }
            }
            else if (meta.DaRollbackKho)
            {
                foreach (var nk in nhatKyHienTai)
                {
                    nk.DaHoanKho = true;
                }
            }
        }

        private static void DongBoThiep3D(
            CSDLBanhTrungThuContext db,
            int maDh,
            Dictionary<string, ThongTinThiep3DMeta> thiepTheoToken,
            Dictionary<string, ThiepChucMung3d> tokenDaCo)
        {
            foreach (var kvThiep in thiepTheoToken)
            {
                string token = (kvThiep.Key ?? string.Empty).Trim();
                var info = kvThiep.Value;
                if (string.IsNullOrWhiteSpace(token) || info == null) continue;

                if (tokenDaCo.TryGetValue(token, out var thiepEntity))
                {
                    thiepEntity.TenNguoiGui = string.IsNullOrWhiteSpace(info.TenNguoiGuiThiep) ? thiepEntity.TenNguoiGui : info.TenNguoiGuiThiep;
                    thiepEntity.TenNguoiNhan = string.IsNullOrWhiteSpace(info.TenNguoiNhanThiep) ? thiepEntity.TenNguoiNhan : info.TenNguoiNhanThiep;
                    if (!string.IsNullOrWhiteSpace(info.LoiChucThiep)) thiepEntity.LoiChuc = info.LoiChucThiep;
                    if (!string.IsNullOrWhiteSpace(info.MauGiaoDienThiep)) thiepEntity.MauGiaoDien = info.MauGiaoDienThiep;
                }
                else
                {
                    int? maDotGiao = db.DotGiaoHangDaDiems
                        .AsNoTracking()
                        .Where(d => d.MaTokenThiepQr == token)
                        .Select(d => (int?)d.MaDotGh)
                        .FirstOrDefault();

                    var thiepMoi = new ThiepChucMung3d
                    {
                        MaTokenQr = token,
                        MaDonHang = maDh,
                        MaDotGiao = maDotGiao,
                        TenNguoiGui = string.IsNullOrWhiteSpace(info.TenNguoiGuiThiep) ? "Khách Hàng Nguyệt Cát" : info.TenNguoiGuiThiep,
                        TenNguoiNhan = string.IsNullOrWhiteSpace(info.TenNguoiNhanThiep) ? "Quý Khách & Gia Đình" : info.TenNguoiNhanThiep,
                        LoiChuc = string.IsNullOrWhiteSpace(info.LoiChucThiep)
                            ? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!"
                            : info.LoiChucThiep,
                        MauGiaoDien = string.IsNullOrWhiteSpace(info.MauGiaoDienThiep) ? "CO_PHONG_HOANG_GIA" : info.MauGiaoDienThiep,
                        SoLanMoThiep = 0,
                        NgayTao = DateTime.Now
                    };
                    db.ThiepChucMung3ds.Add(thiepMoi);
                    tokenDaCo[token] = thiepMoi;
                }
            }
        }

        private static ThuongHieu TimHoacTaoThuongHieu(CSDLBanhTrungThuContext db, List<ThuongHieu> dsThuongHieu, string tenThuongHieu)
        {
            var tonTai = dsThuongHieu.FirstOrDefault(t => string.Equals(t.TenThuongHieu, tenThuongHieu, StringComparison.OrdinalIgnoreCase));
            if (tonTai != null) return tonTai;

            var moi = new ThuongHieu
            {
                MaCodeThuongHieu = SinhMaCodeThuongHieu(tenThuongHieu, dsThuongHieu),
                TenThuongHieu = tenThuongHieu,
                DangHoatDong = true
            };
            db.ThuongHieus.Add(moi);
            db.SaveChanges();
            dsThuongHieu.Add(moi);
            return moi;
        }

        private static string SinhMaCodeThuongHieu(string tenThuongHieu, IEnumerable<ThuongHieu> hienCo)
        {
            string normalized = tenThuongHieu.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            string ascii = sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .ToUpperInvariant();
            string codeBase = Regex.Replace(ascii, @"[^A-Z0-9]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(codeBase)) codeBase = "THUONG_HIEU";
            if (codeBase.Length > 24) codeBase = codeBase[..24];

            string candidate = codeBase;
            int stt = 2;
            while (hienCo.Any(t => string.Equals(t.MaCodeThuongHieu, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = $"{codeBase}_{stt++}";
                if (candidate.Length > 30) candidate = candidate[..30];
            }
            return candidate;
        }
    }
}
