using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách nghiệp vụ Xuất kho (Xuất hủy lỗi, Xuất trả nhà sản xuất, Xuất tiêu dùng nội bộ)
    /// với trừ lô trực tiếp hoặc FEFO tự động trong SQL Transaction và hoàn kho khi hủy phiếu.
    /// </summary>
    public class DichVuXuatKho : DichVuCoSo, IDichVuXuatKho
    {
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private static readonly HashSet<string> _loaiXuatHopLe = new(StringComparer.OrdinalIgnoreCase)
        {
            "XUAT_HUY_LOI",
            "TRA_NHA_SAN_XUAT",
            "TIEU_DUNG_NOI_BO"
        };

        public DichVuXuatKho(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        public async Task<XuatKhoAdminViewModel> XayDungTrangXuatKhoAsync(
            string? tuKhoa,
            string locLoaiXuat = "ALL",
            string locTrangThai = "ALL")
        {
            var dsPhieu = await _db.PhieuXuatKhos
                .AsNoTracking()
                .Include(p => p.ChiTietPhieuXuats)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .Include(p => p.ChiTietPhieuXuats)
                    .ThenInclude(ct => ct.MaLoNavigation)
                .OrderByDescending(p => p.NgayXuat)
                .ThenByDescending(p => p.MaPx)
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
                var cacLoKhaDung = b.LoSanXuatBanhs
                    .Where(l => l.SlconLai > 0 && l.TrangThaiLo != "DA_HUY")
                    .OrderBy(l => l.HanSuDung)
                    .ThenBy(l => l.MaLo)
                    .Select(l => new LoXuatOptionViewModel
                    {
                        MaLo = l.MaLo,
                        MaLoCode = l.MaLoCode,
                        NgaySanXuat = l.NgaySanXuat.ToString("dd/MM/yyyy"),
                        HanSuDung = l.HanSuDung.ToString("dd/MM/yyyy"),
                        SlConLai = l.SlconLai,
                        TrangThaiLo = l.TrangThaiLo
                    })
                    .ToList();

                return new SanPhamKhoOptionViewModel
                {
                    MaBanh = b.MaBanh,
                    MaSku = b.MaSku,
                    TenBanh = b.TenBanh,
                    DonGiaLe = Math.Round(b.DonGiaLe, 0),
                    GiaVon = Math.Round(b.GiaVon, 0),
                    TonKhoHeThong = cacLoKhaDung.Sum(l => l.SlConLai),
                    TenDanhMuc = b.MaDmNavigation?.TenDm ?? "Bánh Trung Thu",
                    ThuongHieu = b.ThuongHieu,
                    DanhSachLo = cacLoKhaDung
                };
            }).ToList();

            string lxChuan = ChuanHoaChuoi(locLoaiXuat, "ALL").ToUpperInvariant();
            string ttChuan = ChuanHoaChuoi(locTrangThai, "ALL").ToUpperInvariant();
            string tkChuan = ChuanHoaChuoi(tuKhoa);

            var dsHienThi = dsPhieu.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tkChuan))
            {
                dsHienThi = dsHienThi.Where(p =>
                    p.SoPhieuXuat.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || p.DoiTuongLienQuan.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || (p.GhiChu ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || p.ChiTietPhieuXuats.Any(ct =>
                        (ct.MaSpNavigation?.TenBanh ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                        || (ct.MaSpNavigation?.MaSku ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)));
            }
            if (lxChuan != "ALL") dsHienThi = dsHienThi.Where(p => p.LoaiXuat == lxChuan);
            if (ttChuan != "ALL") dsHienThi = dsHienThi.Where(p => p.TrangThai == ttChuan);

            var hieuLuc = dsPhieu.Where(p => p.TrangThai == "HOAN_THANH").ToList();

            return new XuatKhoAdminViewModel
            {
                DanhSachPhieuXuat = dsHienThi.ToList(),
                DanhSachSanPham = dsSanPhamOption,
                TuKhoa = tuKhoa,
                LocLoaiXuat = lxChuan,
                LocTrangThai = ttChuan,
                TongSoPhieu = dsPhieu.Count,
                SoPhieuHuyLoi = hieuLuc.Count(p => p.LoaiXuat == "XUAT_HUY_LOI"),
                SoPhieuTraNcc = hieuLuc.Count(p => p.LoaiXuat == "TRA_NHA_SAN_XUAT"),
                SoPhieuNoiBo = hieuLuc.Count(p => p.LoaiXuat == "TIEU_DUNG_NOI_BO"),
                SoPhieuKiemKho = hieuLuc.Count(p => p.LoaiXuat == "CHINH_LECH_KIEM_KHO"),
                TongGiaTriVonXuat = hieuLuc.Sum(p => p.TongGiaTriVon)
            };
        }

        public async Task<(bool ThanhCong, string ThongBao)> TaoPhieuXuatAsync(
            TaoPhieuXuatRequest req,
            string nguoiLap,
            int? maNdLap)
        {
            string loaiXuat = ChuanHoaChuoi(req.LoaiXuat).ToUpperInvariant();
            if (!_loaiXuatHopLe.Contains(loaiXuat))
            {
                return (false, "Nhóm lý do xuất kho không hợp lệ.");
            }

            string doiTuong = ChuanHoaChuoi(req.DoiTuongLienQuan);
            if (string.IsNullOrWhiteSpace(doiTuong))
            {
                return (false, "Vui lòng nhập Người nhận / Đối tượng liên quan.");
            }

            string? ghiChu = string.IsNullOrWhiteSpace(req.GhiChu) ? null : req.GhiChu.Trim();
            if (loaiXuat == "TIEU_DUNG_NOI_BO" && string.IsNullOrWhiteSpace(ghiChu))
            {
                return (false, "Phiếu xuất tiêu dùng nội bộ bắt buộc phải nhập Ghi chú chi tiết.");
            }

            var dongXuats = ParseChiTietXuat(req.ChiTietJson);
            if (dongXuats.Count == 0)
            {
                return (false, "Phiếu xuất kho phải có ít nhất 1 dòng sản phẩm với Số lượng > 0.");
            }

            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                DateTime bayGio = LayThoiGianHeThong();
                int? maNdHopLe = null;
                if (maNdLap.HasValue && await _db.NguoiDungs.AsNoTracking().AnyAsync(u => u.MaNd == maNdLap.Value))
                {
                    maNdHopLe = maNdLap.Value;
                }

                string soPhieuXuat = await SinhSoPhieuXuatAsync(bayGio);
                var dsMaSp = dongXuats.Select(d => d.MaSp).Distinct().ToList();
                var mapBanh = await _db.SanPhamBanhs
                    .Include(b => b.LoSanXuatBanhs)
                    .Where(b => dsMaSp.Contains(b.MaBanh))
                    .ToDictionaryAsync(b => b.MaBanh);

                var phieu = new PhieuXuatKho
                {
                    SoPhieuXuat = soPhieuXuat,
                    LoaiXuat = loaiXuat,
                    DoiTuongLienQuan = doiTuong.Length > 250 ? doiTuong[..250] : doiTuong,
                    NgayXuat = bayGio,
                    TrangThai = "HOAN_THANH",
                    GhiChu = ghiChu != null && ghiChu.Length > 500 ? ghiChu[..500] : ghiChu,
                    NguoiLap = ChuanHoaChuoi(nguoiLap, "Quản trị viên Nguyệt Cát"),
                    MaNdLap = maNdHopLe
                };

                decimal tongGiaTriVon = 0m;

                foreach (var d in dongXuats)
                {
                    if (!mapBanh.TryGetValue(d.MaSp, out var banh))
                    {
                        throw new InvalidOperationException($"Không tìm thấy sản phẩm mã #{d.MaSp}.");
                    }

                    decimal giaVon = Math.Round(banh.GiaVon, 0);
                    string? ghiChuDong = string.IsNullOrWhiteSpace(d.GhiChuDong) ? null : d.GhiChuDong.Trim();

                    if (d.MaLo.HasValue && d.MaLo.Value > 0)
                    {
                        var lo = banh.LoSanXuatBanhs.FirstOrDefault(l => l.MaLo == d.MaLo.Value && l.TrangThaiLo != "DA_HUY");
                        if (lo == null)
                        {
                            throw new InvalidOperationException($"Lô #{d.MaLo.Value} không thuộc sản phẩm '{banh.TenBanh}' hoặc đã bị hủy.");
                        }
                        if (lo.SlconLai < d.SoLuongXuat)
                        {
                            throw new InvalidOperationException($"Lô [{lo.MaLoCode}] của '{banh.TenBanh}' chỉ còn {lo.SlconLai:N0}, không đủ xuất {d.SoLuongXuat:N0}.");
                        }

                        lo.SlconLai -= d.SoLuongXuat;
                        decimal thanhTienVon = d.SoLuongXuat * giaVon;
                        tongGiaTriVon += thanhTienVon;

                        phieu.ChiTietPhieuXuats.Add(new ChiTietPhieuXuat
                        {
                            MaSp = banh.MaBanh,
                            MaLo = lo.MaLo,
                            SoLuongXuat = d.SoLuongXuat,
                            GiaVonLucXuat = giaVon,
                            ThanhTienVon = thanhTienVon,
                            GhiChuDong = ghiChuDong
                        });
                    }
                    else
                    {
                        var cacLoKhaDung = banh.LoSanXuatBanhs
                            .Where(l => l.SlconLai > 0 && l.TrangThaiLo != "DA_HUY")
                            .OrderBy(l => l.HanSuDung)
                            .ThenBy(l => l.MaLo)
                            .ToList();

                        int tongTonKhaDung = cacLoKhaDung.Sum(l => l.SlconLai);
                        if (tongTonKhaDung < d.SoLuongXuat)
                        {
                            throw new InvalidOperationException($"Sản phẩm '{banh.TenBanh}' chỉ còn {tongTonKhaDung:N0} trong các lô khả dụng, không đủ xuất {d.SoLuongXuat:N0}.");
                        }

                        int conCanTru = d.SoLuongXuat;
                        foreach (var lo in cacLoKhaDung)
                        {
                            if (conCanTru <= 0) break;
                            int slTru = Math.Min(lo.SlconLai, conCanTru);
                            if (slTru <= 0) continue;

                            lo.SlconLai -= slTru;
                            conCanTru -= slTru;
                            decimal thanhTienVon = slTru * giaVon;
                            tongGiaTriVon += thanhTienVon;

                            phieu.ChiTietPhieuXuats.Add(new ChiTietPhieuXuat
                            {
                                MaSp = banh.MaBanh,
                                MaLo = lo.MaLo,
                                SoLuongXuat = slTru,
                                GiaVonLucXuat = giaVon,
                                ThanhTienVon = thanhTienVon,
                                GhiChuDong = ghiChuDong
                            });
                        }
                    }

                    banh.TongTonKho = banh.LoSanXuatBanhs
                        .Where(l => l.TrangThaiLo != "DA_HUY")
                        .Sum(l => Math.Max(0, l.SlconLai));
                }

                phieu.TongGiaTriVon = tongGiaTriVon;
                _db.PhieuXuatKhos.Add(phieu);
                await _db.SaveChangesAsync();

                return (true, $"Đã lập phiếu xuất kho {soPhieuXuat} (Giá trị vốn: {tongGiaTriVon:N0} đ) và trừ tồn kho FEFO!");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> HuyPhieuXuatAsync(int maPx)
        {
            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                var phieu = await _db.PhieuXuatKhos
                    .Include(p => p.ChiTietPhieuXuats)
                    .FirstOrDefaultAsync(p => p.MaPx == maPx);

                if (phieu == null) return (false, "Không tìm thấy phiếu xuất kho.");
                if (phieu.TrangThai == "DA_HUY")
                {
                    return (false, $"Phiếu xuất {phieu.SoPhieuXuat} đã được hủy trước đó.");
                }

                bool ganVoiKiemKho = phieu.LoaiXuat == "CHINH_LECH_KIEM_KHO"
                    && await _db.PhieuKiemKhos.AsNoTracking().AnyAsync(k => k.MaPxLienKet == phieu.MaPx && k.TrangThai == "DA_CAN_BANG");
                if (ganVoiKiemKho)
                {
                    return (false, $"Phiếu {phieu.SoPhieuXuat} sinh tự động từ Kiểm kho đã cân bằng, không thể hủy lẻ.");
                }

                var dsMaSp = phieu.ChiTietPhieuXuats.Select(x => x.MaSp).Distinct().ToList();
                var mapBanh = await _db.SanPhamBanhs
                    .Include(b => b.LoSanXuatBanhs)
                    .Where(b => dsMaSp.Contains(b.MaBanh))
                    .ToDictionaryAsync(b => b.MaBanh);

                foreach (var ct in phieu.ChiTietPhieuXuats)
                {
                    if (!mapBanh.TryGetValue(ct.MaSp, out var banh)) continue;

                    LoSanXuatBanh? loHoan = ct.MaLo.HasValue
                        ? banh.LoSanXuatBanhs.FirstOrDefault(l => l.MaLo == ct.MaLo.Value)
                        : banh.LoSanXuatBanhs.Where(l => l.TrangThaiLo != "DA_HUY").OrderByDescending(l => l.HanSuDung).FirstOrDefault();

                    if (loHoan != null)
                    {
                        loHoan.SlconLai += ct.SoLuongXuat;
                    }
                    else
                    {
                        banh.TongTonKho += ct.SoLuongXuat;
                    }
                }

                foreach (var banh in mapBanh.Values)
                {
                    if (banh.LoSanXuatBanhs.Any())
                    {
                        banh.TongTonKho = banh.LoSanXuatBanhs
                            .Where(l => l.TrangThaiLo != "DA_HUY")
                            .Sum(l => Math.Max(0, l.SlconLai));
                    }
                }

                phieu.TrangThai = "DA_HUY";
                await _db.SaveChangesAsync();

                return (true, $"Đã hủy phiếu xuất {phieu.SoPhieuXuat} và hoàn trả đầy đủ số lượng về đúng các lô bánh!");
            });
        }

        private async Task<string> SinhSoPhieuXuatAsync(DateTime ngay)
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

        private static List<DongChiTietXuatDto> ParseChiTietXuat(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                return JsonSerializer.Deserialize<List<DongChiTietXuatDto>>(json, _jsonOpts)
                       ?.Where(x => x.MaSp > 0 && x.SoLuongXuat > 0)
                       .ToList() ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
