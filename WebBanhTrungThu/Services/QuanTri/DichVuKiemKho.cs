using System.Text.Json;
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
    /// Dịch vụ chuyên trách nghiệp vụ Kiểm kho Đối soát thực tế, Nhập/Xuất Excel đối soát
    /// và Cân bằng kho tự động (tự sinh PhieuXuatKho CHINH_LECH_KIEM_KHO khi thiếu hụt, cộng lô khi dư).
    /// </summary>
    public class DichVuKiemKho : DichVuCoSo, IDichVuKiemKho
    {
        private readonly IDichVuCauHinhHeThong _cauHinhHeThong;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private static readonly string[] _tieuDeExcelKiemKho =
        {
            "Mã SP", "Tên bánh", "Giá vốn", "Tồn hệ thống", "Tồn thực tế", "Lý do lệch"
        };

        public DichVuKiemKho(CSDLBanhTrungThuContext db, IDichVuCauHinhHeThong cauHinhHeThong) : base(db)
        {
            _cauHinhHeThong = cauHinhHeThong;
        }

        public async Task<KiemKhoAdminViewModel> XayDungTrangKiemKhoAsync(
            string? tuKhoa,
            string locTrangThai = "ALL")
        {
            var dsPhieu = await _db.PhieuKiemKhos
                .AsNoTracking()
                .Include(p => p.MaPxLienKetNavigation)
                .Include(p => p.ChiTietKiemKhos)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .OrderByDescending(p => p.NgayKiem)
                .ThenByDescending(p => p.MaPkk)
                .ToListAsync();

            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.LoSanXuatBanhs)
                .OrderBy(b => b.TenBanh)
                .ToListAsync();

            var dsSanPhamOption = dsBanh.Select(b =>
            {
                int tonHeThong = TinhTonKhoHeThong(b);
                return new SanPhamKhoOptionViewModel
                {
                    MaBanh = b.MaBanh,
                    MaSku = b.MaSku,
                    TenBanh = b.TenBanh,
                    DonGiaLe = Math.Round(b.DonGiaLe, 0),
                    GiaVon = Math.Round(b.GiaVon, 0),
                    TonKhoHeThong = tonHeThong,
                    TenDanhMuc = b.MaDmNavigation?.TenDm ?? "Bánh Trung Thu",
                    ThuongHieu = b.ThuongHieu
                };
            }).ToList();

            DateOnly mocCanDate = LayNgayHienTai().AddDays(15);
            var dsLoCanDate = await _db.LoSanXuatBanhs
                .AsNoTracking()
                .Include(l => l.MaBanhNavigation)
                .Where(l => l.SlconLai > 0 && l.TrangThaiLo != "DA_HUY"
                    && (l.HanSuDung <= mocCanDate || l.TrangThaiLo == "XA_CAN_DATE" || l.PhanTramGiamCanDate > 0))
                .OrderBy(l => l.HanSuDung)
                .ToListAsync();

            string ttChuan = ChuanHoaChuoi(locTrangThai, "ALL").ToUpperInvariant();
            string tkChuan = ChuanHoaChuoi(tuKhoa);

            var dsHienThi = dsPhieu.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tkChuan))
            {
                dsHienThi = dsHienThi.Where(p =>
                    p.SoPhieuKiem.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || p.NguoiKiem.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || (p.GhiChu ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || (p.MaPxLienKetNavigation?.SoPhieuXuat ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || p.ChiTietKiemKhos.Any(ct =>
                        (ct.MaSpNavigation?.TenBanh ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                        || (ct.MaSpNavigation?.MaSku ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)));
            }
            if (ttChuan != "ALL") dsHienThi = dsHienThi.Where(p => p.TrangThai == ttChuan);

            var daCanBang = dsPhieu.Where(p => p.TrangThai == "DA_CAN_BANG").ToList();

            return new KiemKhoAdminViewModel
            {
                DanhSachPhieuKiem = dsHienThi.ToList(),
                DanhSachSanPham = dsSanPhamOption,
                DanhSachLoCanDate = dsLoCanDate,
                TuKhoa = tuKhoa,
                LocTrangThai = ttChuan,
                TongSoPhieu = dsPhieu.Count,
                SoPhieuTam = dsPhieu.Count(p => p.TrangThai == "PHIEU_TAM"),
                SoPhieuDaCanBang = daCanBang.Count,
                TongLechTang = daCanBang.Sum(p => p.TongLechTang),
                TongLechGiam = daCanBang.Sum(p => p.TongLechGiam),
                TongGiaTriLech = daCanBang.Sum(p => p.TongGiaTriLech)
            };
        }

        public async Task<object?> LayChiTietPhieuKiemJsonAsync(int maPkk)
        {
            var phieu = await _db.PhieuKiemKhos
                .AsNoTracking()
                .Include(p => p.ChiTietKiemKhos)
                    .ThenInclude(ct => ct.MaSpNavigation)
                        .ThenInclude(b => b.LoSanXuatBanhs)
                .FirstOrDefaultAsync(p => p.MaPkk == maPkk);

            if (phieu == null) return null;

            return new
            {
                maPkk = phieu.MaPkk,
                soPhieuKiem = phieu.SoPhieuKiem,
                trangThai = phieu.TrangThai,
                ghiChu = phieu.GhiChu ?? string.Empty,
                chiTiets = phieu.ChiTietKiemKhos.Select(ct => new
                {
                    maSp = ct.MaSp,
                    maSku = ct.MaSpNavigation?.MaSku ?? string.Empty,
                    tenBanh = ct.MaSpNavigation?.TenBanh ?? $"SP #{ct.MaSp}",
                    tonHeThong = phieu.TrangThai == "PHIEU_TAM" && ct.MaSpNavigation != null
                        ? TinhTonKhoHeThong(ct.MaSpNavigation)
                        : ct.TonHeThong,
                    tonThucTe = ct.TonThucTe,
                    giaVon = Math.Round(ct.MaSpNavigation?.GiaVon ?? 0m, 0),
                    lyDoLech = ct.LyDoLech ?? string.Empty
                }).ToList()
            };
        }

        public async Task<(byte[] NoiDung, string ContentType, string TenFile)> XuatFileMauKiemKhoAsync(string dinhDang = "xlsx")
        {
            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.LoSanXuatBanhs)
                .OrderBy(b => b.MaBanh)
                .ToListAsync();

            var dongXlsx = new List<object?[]>();
            var dongCsv = new List<string[]>();

            foreach (var b in dsBanh)
            {
                int tonHeThong = TinhTonKhoHeThong(b);
                decimal giaVon = Math.Round(b.GiaVon, 0);
                dongXlsx.Add(new object?[] { b.MaSku, b.TenBanh, giaVon, tonHeThong, tonHeThong, "" });
                dongCsv.Add(new[] { b.MaSku, b.TenBanh, giaVon.ToString("0"), tonHeThong.ToString(), tonHeThong.ToString(), "" });
            }

            if (string.Equals(dinhDang, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    TienIchExcelCsv.TaoFileCsvUtf8Bom(_tieuDeExcelKiemKho, dongCsv),
                    "text/csv; charset=utf-8",
                    $"MauKiemKho_{DateTime.Now:yyyyMMdd}.csv");
            }

            return (
                TienIchExcelCsv.TaoFileXlsx(
                    "KiemKho",
                    _tieuDeExcelKiemKho,
                    dongXlsx,
                    cotSo: new HashSet<int> { 2, 3, 4 },
                    cotNhapTay: new HashSet<int> { 4, 5 }),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"MauKiemKho_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<KetQuaDocExcelKiemKhoViewModel> DocFileExcelKiemKhoAsync(IFormFile? fileExcel)
        {
            var kq = new KetQuaDocExcelKiemKhoViewModel();
            if (fileExcel == null || fileExcel.Length == 0)
            {
                kq.ThongBao = "Vui lòng chọn tệp Excel (.xlsx) hoặc CSV (.csv) kiểm kho.";
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
                kq.ThongBao = "Tệp không có dữ liệu sản phẩm nào để đối soát.";
                return kq;
            }

            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.LoSanXuatBanhs)
                .ToListAsync();

            var mapTheoSku = dsBanh
                .Where(b => !string.IsNullOrWhiteSpace(b.MaSku))
                .GroupBy(b => b.MaSku.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var mapTheoTen = dsBanh
                .GroupBy(b => b.TenBanh.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var mapDaDoc = new Dictionary<int, DongChiTietKiemKhoDto>();

            for (int r = 1; r < bang.Count; r++)
            {
                var row = bang[r];
                string LayO(int idx) => idx < row.Count ? row[idx].Trim() : string.Empty;

                string maSku = LayO(0);
                string tenBanh = LayO(1);

                SanPhamBanh? banh = null;
                if (!string.IsNullOrWhiteSpace(maSku)) mapTheoSku.TryGetValue(maSku, out banh);
                if (banh == null && !string.IsNullOrWhiteSpace(tenBanh)) mapTheoTen.TryGetValue(tenBanh, out banh);

                if (banh == null)
                {
                    kq.SoDongBoQua++;
                    kq.DanhSachLoi.Add($"Dòng {r + 1}: Không tìm thấy sản phẩm [{maSku}] {tenBanh}.");
                    continue;
                }

                // Hỗ trợ cả file 6 cột (Mã SP, Tên bánh, Giá vốn, Tồn hệ thống, Tồn thực tế, Lý do lệch)
                // lẫn file rút gọn 4 cột (Mã SP, Tên bánh, Tồn thực tế, Lý do lệch)
                string rawTonThucTe = row.Count >= 5 ? LayO(4) : LayO(2);
                string lyDo = row.Count >= 6 ? LayO(5) : (row.Count == 4 ? LayO(3) : string.Empty);

                int tonHeThong = TinhTonKhoHeThong(banh);
                int tonThucTe = Math.Max(0, TienIchExcelCsv.ParseInt(rawTonThucTe, tonHeThong));

                mapDaDoc[banh.MaBanh] = new DongChiTietKiemKhoDto
                {
                    MaSp = banh.MaBanh,
                    MaSku = banh.MaSku,
                    TenBanh = banh.TenBanh,
                    TonHeThong = tonHeThong,
                    TonThucTe = tonThucTe,
                    GiaVon = Math.Round(banh.GiaVon, 0),
                    LyDoLech = lyDo
                };
            }

            kq.DanhSachDong = mapDaDoc.Values.ToList();
            kq.SoDongHopLe = kq.DanhSachDong.Count;
            kq.ThanhCong = kq.SoDongHopLe > 0;
            kq.ThongBao = kq.ThanhCong
                ? $"Đã đọc thành công {kq.SoDongHopLe} dòng sản phẩm từ tệp Excel (bỏ qua {kq.SoDongBoQua} dòng)."
                : "Không đọc được dòng sản phẩm hợp lệ nào từ tệp.";

            return kq;
        }

        public async Task<(bool ThanhCong, string ThongBao)> ImportExcelTaoPhieuKiemKhoAsync(
            IFormFile? fileExcel,
            string? ghiChu,
            bool canBangNgay,
            string nguoiKiem,
            int? maNdLap)
        {
            var docKq = await DocFileExcelKiemKhoAsync(fileExcel);
            if (!docKq.ThanhCong)
            {
                return (false, docKq.ThongBao);
            }

            var req = new LuuPhieuKiemKhoRequest
            {
                MaPkk = 0,
                GhiChu = string.IsNullOrWhiteSpace(ghiChu)
                    ? $"Import đối soát từ tệp {fileExcel?.FileName}"
                    : ghiChu.Trim(),
                CanBangNgay = canBangNgay,
                ChiTietJson = JsonSerializer.Serialize(docKq.DanhSachDong)
            };

            return await LuuPhieuKiemKhoAsync(req, nguoiKiem, maNdLap);
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuPhieuKiemKhoAsync(
            LuuPhieuKiemKhoRequest req,
            string nguoiKiem,
            int? maNdLap)
        {
            var dsDong = ParseChiTietKiemKho(req.ChiTietJson);
            if (dsDong.Count == 0)
            {
                return (false, "Phiếu kiểm kho phải có ít nhất 1 sản phẩm.");
            }

            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                DateTime bayGio = LayThoiGianHeThong();
                PhieuKiemKho phieu;

                if (req.MaPkk > 0)
                {
                    var cu = await _db.PhieuKiemKhos
                        .Include(p => p.ChiTietKiemKhos)
                        .FirstOrDefaultAsync(p => p.MaPkk == req.MaPkk);

                    if (cu == null) return (false, "Không tìm thấy phiếu kiểm kho.");
                    if (cu.TrangThai != "PHIEU_TAM")
                    {
                        return (false, $"Phiếu {cu.SoPhieuKiem} đã ở trạng thái {cu.TrangThai}, không thể chỉnh sửa.");
                    }

                    phieu = cu;
                    if (phieu.ChiTietKiemKhos.Count > 0)
                    {
                        _db.ChiTietKiemKhos.RemoveRange(phieu.ChiTietKiemKhos);
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    phieu = new PhieuKiemKho
                    {
                        SoPhieuKiem = await SinhSoPhieuKiemAsync(bayGio),
                        NgayKiem = bayGio,
                        NguoiKiem = ChuanHoaChuoi(nguoiKiem, "Quản trị viên Nguyệt Cát"),
                        TrangThai = "PHIEU_TAM"
                    };
                    _db.PhieuKiemKhos.Add(phieu);
                }

                var dsMaSp = dsDong.Select(x => x.MaSp).Distinct().ToList();
                var mapBanh = await _db.SanPhamBanhs
                    .Include(b => b.LoSanXuatBanhs)
                    .Where(b => dsMaSp.Contains(b.MaBanh))
                    .ToDictionaryAsync(b => b.MaBanh);

                int tongLechTang = 0;
                int tongLechGiam = 0;
                decimal tongGiaTriLech = 0m;

                foreach (var d in dsDong.GroupBy(x => x.MaSp).Select(g => g.Last()))
                {
                    if (!mapBanh.TryGetValue(d.MaSp, out var banh)) continue;

                    int tonHeThong = TinhTonKhoHeThong(banh);
                    int tonThucTe = Math.Max(0, d.TonThucTe);
                    int soLuongLech = tonThucTe - tonHeThong;
                    decimal giaVon = Math.Round(banh.GiaVon, 0);
                    decimal giaTriLech = soLuongLech * giaVon;

                    if (soLuongLech > 0) tongLechTang += soLuongLech;
                    else if (soLuongLech < 0) tongLechGiam += Math.Abs(soLuongLech);
                    tongGiaTriLech += giaTriLech;

                    phieu.ChiTietKiemKhos.Add(new ChiTietKiemKho
                    {
                        MaSp = banh.MaBanh,
                        TonHeThong = tonHeThong,
                        TonThucTe = tonThucTe,
                        SoLuongLech = soLuongLech,
                        GiaTriLech = giaTriLech,
                        LyDoLech = string.IsNullOrWhiteSpace(d.LyDoLech) ? null : d.LyDoLech.Trim()
                    });
                }

                phieu.TongLechTang = tongLechTang;
                phieu.TongLechGiam = tongLechGiam;
                phieu.TongGiaTriLech = tongGiaTriLech;
                phieu.GhiChu = string.IsNullOrWhiteSpace(req.GhiChu) ? null : req.GhiChu.Trim();

                await _db.SaveChangesAsync();

                if (req.CanBangNgay)
                {
                    return await ThucHienCanBangNoiBoAsync(phieu, nguoiKiem, maNdLap);
                }

                return (true, $"Đã lưu phiếu kiểm kho tạm {phieu.SoPhieuKiem} ({phieu.ChiTietKiemKhos.Count} sản phẩm).");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> CanBangPhieuKiemKhoAsync(
            int maPkk,
            string nguoiThucHien,
            int? maNdLap)
        {
            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                var phieu = await _db.PhieuKiemKhos
                    .Include(p => p.ChiTietKiemKhos)
                    .FirstOrDefaultAsync(p => p.MaPkk == maPkk);

                if (phieu == null) return (false, "Không tìm thấy phiếu kiểm kho.");
                if (phieu.TrangThai != "PHIEU_TAM")
                {
                    return (false, $"Phiếu {phieu.SoPhieuKiem} không ở trạng thái Phiếu tạm.");
                }

                return await ThucHienCanBangNoiBoAsync(phieu, nguoiThucHien, maNdLap);
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> XoaHoacHuyPhieuKiemKhoAsync(int maPkk)
        {
            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                var phieu = await _db.PhieuKiemKhos.FirstOrDefaultAsync(p => p.MaPkk == maPkk);
                if (phieu == null) return (false, "Không tìm thấy phiếu kiểm kho.");
                if (phieu.TrangThai != "PHIEU_TAM")
                {
                    return (false, $"Phiếu {phieu.SoPhieuKiem} đã cân bằng/hủy, không thể xóa.");
                }

                string soPhieu = phieu.SoPhieuKiem;
                _db.PhieuKiemKhos.Remove(phieu);
                await _db.SaveChangesAsync();
                return (true, $"Đã xóa phiếu kiểm kho tạm {soPhieu}.");
            });
        }

        private async Task<(bool ThanhCong, string ThongBao)> ThucHienCanBangNoiBoAsync(
            PhieuKiemKho phieu,
            string nguoiThucHien,
            int? maNdLap)
        {
            if (phieu.ChiTietKiemKhos.Count == 0)
            {
                return (false, $"Phiếu {phieu.SoPhieuKiem} chưa có dòng hàng nào để cân bằng.");
            }

            var dsMaSp = phieu.ChiTietKiemKhos.Select(c => c.MaSp).Distinct().ToList();
            var mapBanh = await _db.SanPhamBanhs
                .Include(b => b.LoSanXuatBanhs)
                .Where(b => dsMaSp.Contains(b.MaBanh))
                .ToDictionaryAsync(b => b.MaBanh);

            DateTime bayGio = LayThoiGianHeThong();
            DateOnly homNay = LayNgayHienTai();
            int? maNdHopLe = null;
            if (maNdLap.HasValue && await _db.NguoiDungs.AsNoTracking().AnyAsync(u => u.MaNd == maNdLap.Value))
            {
                maNdHopLe = maNdLap.Value;
            }

            int tongLechTang = 0;
            int tongLechGiam = 0;
            decimal tongGiaTriLech = 0m;

            PhieuXuatKho? phieuXuatThieu = null;
            decimal tongGiaTriVonThieu = 0m;

            foreach (var ct in phieu.ChiTietKiemKhos)
            {
                if (!mapBanh.TryGetValue(ct.MaSp, out var banh)) continue;

                int tonHeThongHienTai = TinhTonKhoHeThong(banh);
                ct.TonHeThong = tonHeThongHienTai;
                ct.SoLuongLech = ct.TonThucTe - tonHeThongHienTai;
                decimal giaVon = Math.Round(banh.GiaVon, 0);
                ct.GiaTriLech = ct.SoLuongLech * giaVon;

                if (ct.SoLuongLech > 0) tongLechTang += ct.SoLuongLech;
                else if (ct.SoLuongLech < 0) tongLechGiam += Math.Abs(ct.SoLuongLech);
                tongGiaTriLech += ct.GiaTriLech;

                if (ct.SoLuongLech < 0)
                {
                    int slThieu = Math.Abs(ct.SoLuongLech);
                    phieuXuatThieu ??= new PhieuXuatKho
                    {
                        SoPhieuXuat = await SinhSoPhieuXuatKiemKhoAsync(bayGio),
                        LoaiXuat = "CHINH_LECH_KIEM_KHO",
                        DoiTuongLienQuan = $"Kiểm kho định kỳ ({phieu.SoPhieuKiem}) - {phieu.NguoiKiem}",
                        NgayXuat = bayGio,
                        TrangThai = "HOAN_THANH",
                        GhiChu = $"Tự động xuất hủy chênh lệch thiếu từ phiếu kiểm kho {phieu.SoPhieuKiem}",
                        NguoiLap = ChuanHoaChuoi(nguoiThucHien, phieu.NguoiKiem),
                        MaNdLap = maNdHopLe
                    };

                    var cacLoKhaDung = banh.LoSanXuatBanhs
                        .Where(l => l.SlconLai > 0 && l.TrangThaiLo != "DA_HUY")
                        .OrderBy(l => l.HanSuDung)
                        .ThenBy(l => l.MaLo)
                        .ToList();

                    int conCanTru = slThieu;
                    foreach (var lo in cacLoKhaDung)
                    {
                        if (conCanTru <= 0) break;
                        int slTru = Math.Min(lo.SlconLai, conCanTru);
                        if (slTru <= 0) continue;

                        lo.SlconLai -= slTru;
                        conCanTru -= slTru;
                        decimal thanhTienVon = slTru * giaVon;
                        tongGiaTriVonThieu += thanhTienVon;

                        phieuXuatThieu.ChiTietPhieuXuats.Add(new ChiTietPhieuXuat
                        {
                            MaSp = banh.MaBanh,
                            MaLo = lo.MaLo,
                            SoLuongXuat = slTru,
                            GiaVonLucXuat = giaVon,
                            ThanhTienVon = thanhTienVon,
                            GhiChuDong = string.IsNullOrWhiteSpace(ct.LyDoLech) ? "Thiếu hụt khi kiểm kho" : ct.LyDoLech
                        });
                    }

                    if (conCanTru > 0)
                    {
                        decimal thanhTienVonCon = conCanTru * giaVon;
                        tongGiaTriVonThieu += thanhTienVonCon;
                        phieuXuatThieu.ChiTietPhieuXuats.Add(new ChiTietPhieuXuat
                        {
                            MaSp = banh.MaBanh,
                            MaLo = null,
                            SoLuongXuat = conCanTru,
                            GiaVonLucXuat = giaVon,
                            ThanhTienVon = thanhTienVonCon,
                            GhiChuDong = string.IsNullOrWhiteSpace(ct.LyDoLech) ? "Thiếu hụt khi kiểm kho" : ct.LyDoLech
                        });
                    }
                }
                else if (ct.SoLuongLech > 0)
                {
                    int slDu = ct.SoLuongLech;
                    var loMoiNhat = banh.LoSanXuatBanhs
                        .Where(l => l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "XA_CAN_DATE")
                        .OrderByDescending(l => l.HanSuDung)
                        .ThenByDescending(l => l.MaLo)
                        .FirstOrDefault();

                    if (loMoiNhat != null)
                    {
                        loMoiNhat.SlconLai += slDu;
                    }
                    else
                    {
                        var loDieuChinh = new LoSanXuatBanh
                        {
                            MaBanh = banh.MaBanh,
                            MaLoCode = $"LO-KK-{phieu.SoPhieuKiem.Replace("KK-", "")}-{banh.MaBanh:D2}",
                            NgaySanXuat = homNay,
                            HanSuDung = homNay.AddDays(35),
                            SlnhapBanDau = slDu,
                            SlconLai = slDu,
                            PhanTramGiamCanDate = 0m,
                            TrangThaiLo = "DANG_BAN"
                        };
                        _db.LoSanXuatBanhs.Add(loDieuChinh);
                        banh.LoSanXuatBanhs.Add(loDieuChinh);
                    }
                }

                banh.TongTonKho = banh.LoSanXuatBanhs.Any(l => l.TrangThaiLo != "DA_HUY")
                    ? banh.LoSanXuatBanhs.Where(l => l.TrangThaiLo != "DA_HUY").Sum(l => Math.Max(0, l.SlconLai))
                    : Math.Max(0, ct.TonThucTe);
            }

            if (phieuXuatThieu != null && phieuXuatThieu.ChiTietPhieuXuats.Count > 0)
            {
                phieuXuatThieu.TongGiaTriVon = tongGiaTriVonThieu;
                _db.PhieuXuatKhos.Add(phieuXuatThieu);
                await _db.SaveChangesAsync();
                phieu.MaPxLienKet = phieuXuatThieu.MaPx;
            }

            phieu.TongLechTang = tongLechTang;
            phieu.TongLechGiam = tongLechGiam;
            phieu.TongGiaTriLech = tongGiaTriLech;
            phieu.TrangThai = "DA_CAN_BANG";
            phieu.NgayKiem = bayGio;

            await _db.SaveChangesAsync();

            string tbXuat = phieuXuatThieu != null
                ? $" Đã tự động lập phiếu xuất chênh lệch [{phieuXuatThieu.SoPhieuXuat}] cho phần hàng thiếu hụt (-{tongLechGiam:N0} sp)."
                : string.Empty;

            return (true, $"Đã hoàn thành & cân bằng kho phiếu {phieu.SoPhieuKiem}!{tbXuat}");
        }

        private static int TinhTonKhoHeThong(SanPhamBanh banh)
        {
            if (banh.LoSanXuatBanhs != null && banh.LoSanXuatBanhs.Any())
            {
                return banh.LoSanXuatBanhs
                    .Where(l => l.TrangThaiLo != "DA_HUY")
                    .Sum(l => Math.Max(0, l.SlconLai));
            }
            return Math.Max(0, banh.TongTonKho);
        }

        private async Task<string> SinhSoPhieuKiemAsync(DateTime ngay)
        {
            string tienTo = $"KK-{ngay:yyyyMMdd}-";
            var dsCungNgay = await _db.PhieuKiemKhos
                .AsNoTracking()
                .Where(p => p.SoPhieuKiem.StartsWith(tienTo))
                .Select(p => p.SoPhieuKiem)
                .ToListAsync();

            int maxStt = 0;
            foreach (var ma in dsCungNgay)
            {
                if (int.TryParse(ma.Substring(tienTo.Length), out int so) && so > maxStt)
                {
                    maxStt = so;
                }
            }
            return $"{tienTo}{(maxStt + 1):D3}";
        }

        private async Task<string> SinhSoPhieuXuatKiemKhoAsync(DateTime ngay)
        {
            string tienTo = $"PX-{ngay:yyyyMMdd}-";
            var dsCungNgay = await _db.PhieuXuatKhos
                .AsNoTracking()
                .Where(p => p.SoPhieuXuat.StartsWith(tienTo))
                .Select(p => p.SoPhieuXuat)
                .ToListAsync();

            int maxStt = 0;
            foreach (var ma in dsCungNgay)
            {
                if (int.TryParse(ma.Substring(tienTo.Length), out int so) && so > maxStt)
                {
                    maxStt = so;
                }
            }
            return $"{tienTo}{(maxStt + 1):D3}";
        }

        private static List<DongChiTietKiemKhoDto> ParseChiTietKiemKho(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                return JsonSerializer.Deserialize<List<DongChiTietKiemKhoDto>>(json, _jsonOpts)
                       ?.Where(x => x.MaSp > 0)
                       .ToList() ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
