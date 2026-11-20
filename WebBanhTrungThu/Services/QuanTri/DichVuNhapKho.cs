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
    /// Dịch vụ chuyên trách nghiệp vụ Nhập hàng đa sản phẩm (PhieuNhapKho & ChiTietPhieuNhap),
    /// tự động sinh lô FEFO (LoSanXuatBanh), cập nhật giá vốn/tồn kho và ghi Phiếu Chi vào dbo.PhieuThuChi.
    /// </summary>
    public class DichVuNhapKho : DichVuCoSo, IDichVuNhapKho
    {
        private readonly IDichVuCauHinhHeThong _cauHinhHeThong;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DichVuNhapKho(CSDLBanhTrungThuContext db, IDichVuCauHinhHeThong cauHinhHeThong) : base(db)
        {
            _cauHinhHeThong = cauHinhHeThong;
        }

        public async Task<NhapKhoAdminViewModel> XayDungTrangNhapKhoAsync(
            string? tuKhoa,
            string locTrangThai = "ALL",
            string locThanhToan = "ALL",
            string locNhaCungCap = "ALL")
        {
            var dsPhieu = await _db.PhieuNhapKhos
                .AsNoTracking()
                .Include(p => p.ChiTietPhieuNhaps)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .Include(p => p.ChiTietPhieuNhaps)
                    .ThenInclude(ct => ct.MaLoNavigation)
                .Include(p => p.PhieuThuChis)
                .OrderByDescending(p => p.NgayNhap)
                .ThenByDescending(p => p.MaPn)
                .ToListAsync();

            var dsSanPhamDb = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.LoSanXuatBanhs)
                .OrderBy(b => b.TenBanh)
                .ToListAsync();

            var dsSanPhamOption = dsSanPhamDb.Select(b =>
            {
                int tonLo = b.LoSanXuatBanhs.Where(l => l.TrangThaiLo != "DA_HUY").Sum(l => l.SlconLai);
                return new SanPhamKhoOptionViewModel
                {
                    MaBanh = b.MaBanh,
                    MaSku = b.MaSku,
                    TenBanh = b.TenBanh,
                    DonGiaLe = Math.Round(b.DonGiaLe, 0),
                    GiaVon = Math.Round(b.GiaVon, 0),
                    TonKhoHeThong = b.LoSanXuatBanhs.Any() ? tonLo : b.TongTonKho,
                    TenDanhMuc = b.MaDmNavigation?.TenDm ?? "Bánh Trung Thu",
                    ThuongHieu = b.ThuongHieu
                };
            }).ToList();

            var dsNccChiTiet = await _db.NhaCungCaps
                .AsNoTracking()
                .Where(n => n.DangHoatDong)
                .OrderBy(n => n.MaCodeNcc)
                .ThenBy(n => n.TenNcc)
                .Select(n => new NhaCungCapOptionViewModel
                {
                    MaNcc = n.MaNcc,
                    MaCodeNcc = n.MaCodeNcc,
                    TenNcc = n.TenNcc,
                    LoaiNcc = n.LoaiNcc,
                    ThuongHieuCungCap = n.ThuongHieuCungCap,
                    PhanTramChietKhauMacDinh = Math.Round(n.PhanTramChietKhauMacDinh, 2)
                })
                .ToListAsync();

            var dsThuongHieuDb = await _db.ThuongHieus
                .AsNoTracking()
                .Where(t => t.DangHoatDong)
                .OrderBy(t => t.MaThuongHieu)
                .Select(t => t.TenThuongHieu)
                .ToListAsync();

            var dsThuongHieu = new List<string>();
            foreach (var th in dsSanPhamOption.Select(sp => sp.ThuongHieu).Concat(dsThuongHieuDb))
            {
                string sach = ChuanHoaChuoi(th);
                if (!string.IsNullOrWhiteSpace(sach) && !dsThuongHieu.Contains(sach, StringComparer.OrdinalIgnoreCase))
                    dsThuongHieu.Add(sach);
            }

            var dsNcc = new List<string>();
            foreach (var ncc in dsNccChiTiet.Select(x => x.TenNcc))
            {
                if (!string.IsNullOrWhiteSpace(ncc) && !dsNcc.Contains(ncc.Trim(), StringComparer.OrdinalIgnoreCase))
                    dsNcc.Add(ncc.Trim());
            }
            foreach (var ncc in dsPhieu.Select(p => p.NhaCungCap).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!dsNcc.Contains(ncc.Trim(), StringComparer.OrdinalIgnoreCase))
                    dsNcc.Add(ncc.Trim());
            }

            string ttChuan = ChuanHoaChuoi(locTrangThai, "ALL").ToUpperInvariant();
            string ttTtChuan = ChuanHoaChuoi(locThanhToan, "ALL").ToUpperInvariant();
            string nccChuan = ChuanHoaChuoi(locNhaCungCap, "ALL");
            string tkChuan = ChuanHoaChuoi(tuKhoa);

            var dsHienThi = dsPhieu.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tkChuan))
            {
                dsHienThi = dsHienThi.Where(p =>
                    p.SoPhieuNhap.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || p.NhaCungCap.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || (p.GhiChu ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || p.ChiTietPhieuNhaps.Any(ct =>
                        (ct.MaSpNavigation?.TenBanh ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                        || (ct.MaSpNavigation?.MaSku ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)));
            }
            if (ttChuan != "ALL")
            {
                dsHienThi = dsHienThi.Where(p => p.TrangThai == ttChuan);
            }
            if (ttTtChuan != "ALL")
            {
                dsHienThi = dsHienThi.Where(p => p.TrangThaiThanhToan == ttTtChuan);
            }
            if (!string.Equals(nccChuan, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                dsHienThi = dsHienThi.Where(p => string.Equals(p.NhaCungCap, nccChuan, StringComparison.OrdinalIgnoreCase));
            }

            var phieuHieuLuc = dsPhieu.Where(p => p.TrangThai != "DA_HUY").ToList();

            return new NhapKhoAdminViewModel
            {
                DanhSachPhieuNhap = dsHienThi.ToList(),
                DanhSachSanPham = dsSanPhamOption,
                DanhSachNccChiTiet = dsNccChiTiet,
                DanhSachThuongHieu = dsThuongHieu,
                DanhSachNhaCungCap = dsNcc,
                TuKhoa = tuKhoa,
                LocTrangThai = ttChuan,
                LocThanhToan = ttTtChuan,
                LocNhaCungCap = nccChuan,
                TongSoPhieu = dsPhieu.Count,
                SoPhieuTam = dsPhieu.Count(p => p.TrangThai == "PHIEU_TAM"),
                SoPhieuDaNhap = dsPhieu.Count(p => p.TrangThai == "DA_NHAP_KHO"),
                TongGiaTriNhap = phieuHieuLuc.Where(p => p.TrangThai == "DA_NHAP_KHO").Sum(p => p.TongPhaiTra),
                TongDaThanhToan = phieuHieuLuc.Where(p => p.TrangThai == "DA_NHAP_KHO").Sum(p => p.DaThanhToan)
            };
        }

        public async Task<object?> LayChiTietPhieuNhapJsonAsync(int maPn)
        {
            var phieu = await _db.PhieuNhapKhos
                .AsNoTracking()
                .Include(p => p.ChiTietPhieuNhaps)
                    .ThenInclude(ct => ct.MaSpNavigation)
                .Include(p => p.ChiTietPhieuNhaps)
                    .ThenInclude(ct => ct.MaLoNavigation)
                .FirstOrDefaultAsync(p => p.MaPn == maPn);

            if (phieu == null) return null;

            return new
            {
                maPn = phieu.MaPn,
                soPhieuNhap = phieu.SoPhieuNhap,
                maNcc = phieu.MaNcc,
                nhaCungCap = phieu.NhaCungCap,
                ngayNhap = phieu.NgayNhap.ToString("dd/MM/yyyy HH:mm"),
                tongTienHang = Math.Round(phieu.TongTienHang, 0),
                giamGia = Math.Round(phieu.GiamGia, 0),
                tongPhaiTra = Math.Round(phieu.TongPhaiTra, 0),
                daThanhToan = Math.Round(phieu.DaThanhToan, 0),
                trangThai = phieu.TrangThai,
                trangThaiThanhToan = phieu.TrangThaiThanhToan,
                ghiChu = phieu.GhiChu ?? string.Empty,
                chiTiets = phieu.ChiTietPhieuNhaps.Select(ct => new
                {
                    maCtpn = ct.MaCtpn,
                    maSp = ct.MaSp,
                    maSku = ct.MaSpNavigation?.MaSku ?? string.Empty,
                    tenBanh = ct.MaSpNavigation?.TenBanh ?? $"SP #{ct.MaSp}",
                    soLuong = ct.SoLuong,
                    giaNiemYet = ct.GiaNiemYet > 0m
                        ? Math.Round(ct.GiaNiemYet, 0)
                        : Math.Round(ct.MaSpNavigation?.DonGiaLe ?? 0m, 0),
                    phanTramChietKhau = Math.Round(ct.PhanTramChietKhau, 2),
                    donGiaNhap = Math.Round(ct.DonGiaNhap, 0),
                    thanhTien = Math.Round(ct.ThanhTien, 0),
                    ngaySanXuat = ct.NgaySanXuat.ToString("yyyy-MM-dd"),
                    hanSuDung = ct.HanSuDung.ToString("yyyy-MM-dd"),
                    maLoCode = ct.MaLoNavigation?.MaLoCode
                }).ToList()
            };
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuPhieuNhapAsync(
            LuuPhieuNhapRequest req,
            string nguoiLap,
            int? maNdLap)
        {
            int? maNccChuan = null;
            string ncc = ChuanHoaChuoi(req.NhaCungCap);
            if (req.MaNcc.HasValue && req.MaNcc.Value > 0)
            {
                var nccDb = await _db.NhaCungCaps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.MaNcc == req.MaNcc.Value);
                if (nccDb != null)
                {
                    maNccChuan = nccDb.MaNcc;
                    ncc = nccDb.TenNcc;
                }
            }
            else if (!string.IsNullOrWhiteSpace(ncc))
            {
                var nccDb = await _db.NhaCungCaps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.TenNcc.ToLower() == ncc.ToLower());
                if (nccDb != null)
                {
                    maNccChuan = nccDb.MaNcc;
                    ncc = nccDb.TenNcc;
                }
            }

            if (string.IsNullOrWhiteSpace(ncc))
            {
                return (false, "Vui lòng chọn Nhà cung cấp!");
            }

            var dongNhaps = ParseChiTietNhap(req.ChiTietJson);

            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                DateTime bayGio = LayThoiGianHeThong();
                int? maNdHopLe = null;
                if (maNdLap.HasValue && await _db.NguoiDungs.AsNoTracking().AnyAsync(u => u.MaNd == maNdLap.Value))
                {
                    maNdHopLe = maNdLap.Value;
                }

                PhieuNhapKho phieu;
                if (req.MaPn > 0)
                {
                    var phieuCu = await _db.PhieuNhapKhos
                        .Include(p => p.ChiTietPhieuNhaps)
                        .FirstOrDefaultAsync(p => p.MaPn == req.MaPn);

                    if (phieuCu == null)
                    {
                        return (false, "Không tìm thấy phiếu nhập cần cập nhật.");
                    }
                    if (phieuCu.TrangThai == "DA_HUY")
                    {
                        return (false, $"Phiếu nhập {phieuCu.SoPhieuNhap} đã hủy, không thể chỉnh sửa.");
                    }

                    if (phieuCu.TrangThai == "DA_NHAP_KHO")
                    {
                        phieuCu.MaNcc = maNccChuan;
                        phieuCu.NhaCungCap = ncc.Length > 200 ? ncc[..200] : ncc;
                        phieuCu.GhiChu = string.IsNullOrWhiteSpace(req.GhiChu) ? null : req.GhiChu.Trim();
                        await _db.SaveChangesAsync();
                        return (true, $"Đã cập nhật thông tin Nhà cung cấp & Ghi chú cho phiếu {phieuCu.SoPhieuNhap}.");
                    }

                    // Phiếu đang ở trạng thái PHIEU_TAM -> cho phép cập nhật toàn bộ danh sách sản phẩm
                    phieu = phieuCu;
                    if (phieu.ChiTietPhieuNhaps.Count > 0)
                    {
                        _db.ChiTietPhieuNhaps.RemoveRange(phieu.ChiTietPhieuNhaps);
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    string soPhieu = await SinhSoPhieuNhapAsync(bayGio);
                    phieu = new PhieuNhapKho
                    {
                        SoPhieuNhap = soPhieu,
                        NgayNhap = bayGio,
                        NguoiLap = ChuanHoaChuoi(nguoiLap, "Quản trị viên Nguyệt Cát"),
                        MaNdLap = maNdHopLe,
                        TrangThai = "PHIEU_TAM",
                        TrangThaiThanhToan = "CHUA_THANH_TOAN"
                    };
                    _db.PhieuNhapKhos.Add(phieu);
                }

                if (dongNhaps.Count == 0)
                {
                    return (false, "Phiếu nhập phải có ít nhất 1 dòng sản phẩm hợp lệ (Số lượng > 0).");
                }

                var dsMaSp = dongNhaps.Select(x => x.MaSp).Distinct().ToList();
                var mapSanPham = await _db.SanPhamBanhs
                    .Where(b => dsMaSp.Contains(b.MaBanh))
                    .ToDictionaryAsync(b => b.MaBanh);

                decimal tongTienHang = 0m;
                var danhSachDongMoi = new List<ChiTietPhieuNhap>();
                DateOnly homNay = LayNgayHienTai();

                foreach (var d in dongNhaps)
                {
                    if (!mapSanPham.TryGetValue(d.MaSp, out var banh))
                    {
                        return (false, $"Không tìm thấy sản phẩm mã #{d.MaSp} trong danh mục.");
                    }
                    if (d.SoLuong <= 0)
                    {
                        return (false, $"Số lượng nhập của '{banh.TenBanh}' phải lớn hơn 0.");
                    }
                    if (d.DonGiaNhap < 0)
                    {
                        return (false, $"Đơn giá nhập của '{banh.TenBanh}' không được âm.");
                    }

                    DateOnly nsx = d.NgaySanXuat.HasValue ? DateOnly.FromDateTime(d.NgaySanXuat.Value) : homNay;
                    DateOnly hsd = d.HanSuDung.HasValue ? DateOnly.FromDateTime(d.HanSuDung.Value) : nsx.AddDays(40);
                    if (hsd < nsx)
                    {
                        return (false, $"Hạn sử dụng của '{banh.TenBanh}' không thể nhỏ hơn Ngày sản xuất.");
                    }

                    decimal giaNiemYet = d.GiaNiemYet > 0m ? Math.Round(d.GiaNiemYet, 0) : Math.Round(banh.DonGiaLe, 0);
                    decimal phanTramCk = Math.Clamp(Math.Round(d.PhanTramChietKhau, 2), 0m, 100m);
                    decimal donGia = Math.Round(d.DonGiaNhap, 0);
                    decimal thanhTien = d.SoLuong * donGia;
                    tongTienHang += thanhTien;

                    danhSachDongMoi.Add(new ChiTietPhieuNhap
                    {
                        MaSp = d.MaSp,
                        SoLuong = d.SoLuong,
                        GiaNiemYet = giaNiemYet,
                        PhanTramChietKhau = phanTramCk,
                        DonGiaNhap = donGia,
                        ThanhTien = thanhTien,
                        NgaySanXuat = nsx,
                        HanSuDung = hsd
                    });
                }

                decimal giamGia = Math.Clamp(Math.Round(req.GiamGia, 0), 0m, tongTienHang);
                decimal tongPhaiTra = Math.Max(0m, tongTienHang - giamGia);

                phieu.MaNcc = maNccChuan;
                phieu.NhaCungCap = ncc.Length > 200 ? ncc[..200] : ncc;
                phieu.TongTienHang = tongTienHang;
                phieu.GiamGia = giamGia;
                phieu.TongPhaiTra = tongPhaiTra;
                phieu.GhiChu = string.IsNullOrWhiteSpace(req.GhiChu) ? null : req.GhiChu.Trim();
                phieu.TrangThai = req.HoanThanhNhapKho ? "DA_NHAP_KHO" : "PHIEU_TAM";

                decimal traNgay = req.HoanThanhNhapKho
                    ? Math.Clamp(Math.Round(req.SoTienThanhToanNgay, 0), 0m, tongPhaiTra)
                    : 0m;
                phieu.DaThanhToan = traNgay;
                phieu.TrangThaiThanhToan = XacDinhTrangThaiThanhToan(tongPhaiTra, traNgay);

                foreach (var ct in danhSachDongMoi)
                {
                    phieu.ChiTietPhieuNhaps.Add(ct);
                }

                await _db.SaveChangesAsync();

                if (req.HoanThanhNhapKho)
                {
                    await SinhLoVaCapNhatTonKhoKhiNhapAsync(phieu);
                    if (traNgay > 0m)
                    {
                        await GhiPhieuChiSoQuyChoPhieuNhapAsync(
                            phieu,
                            traNgay,
                            req.PhuongThucThanhToan,
                            $"Thanh toán ngay khi nhập kho phiếu {phieu.SoPhieuNhap}");
                    }
                    await _db.SaveChangesAsync();
                    return (true, $"Đã hoàn thành nhập kho phiếu {phieu.SoPhieuNhap} ({danhSachDongMoi.Count} dòng hàng, tổng {tongPhaiTra:N0} đ) và tự động sinh lô FEFO!");
                }

                return (true, $"Đã lưu phiếu tạm {phieu.SoPhieuNhap} ({danhSachDongMoi.Count} dòng hàng).");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> HoanThanhPhieuTamAsync(int maPn)
        {
            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                var phieu = await _db.PhieuNhapKhos
                    .Include(p => p.ChiTietPhieuNhaps)
                    .FirstOrDefaultAsync(p => p.MaPn == maPn);

                if (phieu == null) return (false, "Không tìm thấy phiếu nhập kho.");
                if (phieu.TrangThai != "PHIEU_TAM")
                {
                    return (false, $"Phiếu {phieu.SoPhieuNhap} không ở trạng thái Phiếu tạm.");
                }
                if (phieu.ChiTietPhieuNhaps.Count == 0)
                {
                    return (false, $"Phiếu {phieu.SoPhieuNhap} chưa có dòng sản phẩm nào.");
                }

                phieu.TrangThai = "DA_NHAP_KHO";
                phieu.NgayNhap = LayThoiGianHeThong();
                phieu.TrangThaiThanhToan = XacDinhTrangThaiThanhToan(phieu.TongPhaiTra, phieu.DaThanhToan);

                await SinhLoVaCapNhatTonKhoKhiNhapAsync(phieu);
                await _db.SaveChangesAsync();

                return (true, $"Đã nhập kho thành công phiếu {phieu.SoPhieuNhap} và sinh {phieu.ChiTietPhieuNhaps.Count} lô bánh mới!");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> XoaHoacHuyPhieuNhapAsync(int maPn)
        {
            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                var phieu = await _db.PhieuNhapKhos
                    .Include(p => p.ChiTietPhieuNhaps)
                        .ThenInclude(ct => ct.MaSpNavigation)
                    .Include(p => p.ChiTietPhieuNhaps)
                        .ThenInclude(ct => ct.MaLoNavigation)
                    .FirstOrDefaultAsync(p => p.MaPn == maPn);

                if (phieu == null) return (false, "Không tìm thấy phiếu nhập kho.");
                if (phieu.TrangThai == "DA_HUY")
                {
                    return (false, $"Phiếu {phieu.SoPhieuNhap} đã bị hủy trước đó.");
                }

                if (phieu.TrangThai == "PHIEU_TAM")
                {
                    string soPhieu = phieu.SoPhieuNhap;
                    _db.PhieuNhapKhos.Remove(phieu);
                    await _db.SaveChangesAsync();
                    return (true, $"Đã xóa phiếu nhập tạm {soPhieu}.");
                }

                // Kiểm tra các lô thuộc phiếu chưa bị bán/xuất hụt
                foreach (var ct in phieu.ChiTietPhieuNhaps)
                {
                    if (ct.MaLoNavigation != null && ct.MaLoNavigation.SlconLai < ct.SoLuong)
                    {
                        string tenSp = ct.MaSpNavigation?.TenBanh ?? $"SP #{ct.MaSp}";
                        return (false, $"Không thể hủy phiếu {phieu.SoPhieuNhap} vì lô [{ct.MaLoNavigation.MaLoCode}] của '{tenSp}' đã bán/xuất bớt (còn {ct.MaLoNavigation.SlconLai}/{ct.SoLuong}).");
                    }
                }

                var dsMaSp = phieu.ChiTietPhieuNhaps.Select(x => x.MaSp).Distinct().ToList();

                foreach (var ct in phieu.ChiTietPhieuNhaps)
                {
                    if (ct.MaLoNavigation != null)
                    {
                        ct.MaLoNavigation.SlconLai = Math.Max(0, ct.MaLoNavigation.SlconLai - ct.SoLuong);
                        ct.MaLoNavigation.TrangThaiLo = "DA_HUY";
                    }
                }

                phieu.TrangThai = "DA_HUY";
                await _db.SaveChangesAsync();

                await DongBoTongTonKhoSanPhamAsync(dsMaSp);
                await _db.SaveChangesAsync();

                return (true, $"Đã hủy phiếu nhập {phieu.SoPhieuNhap} và trừ ngược tồn kho các lô liên quan.");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> ThanhToanPhieuNhapAsync(
            ThanhToanPhieuNhapRequest req,
            string nguoiLap)
        {
            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                var phieu = await _db.PhieuNhapKhos.FirstOrDefaultAsync(p => p.MaPn == req.MaPn);
                if (phieu == null) return (false, "Không tìm thấy phiếu nhập kho.");
                if (phieu.TrangThai != "DA_NHAP_KHO")
                {
                    return (false, "Chỉ thanh toán được cho phiếu đã hoàn thành nhập kho.");
                }

                decimal conNo = Math.Max(0m, phieu.TongPhaiTra - phieu.DaThanhToan);
                if (conNo <= 0m)
                {
                    return (false, $"Phiếu {phieu.SoPhieuNhap} đã thanh toán đủ 100%.");
                }

                decimal soTienTra = Math.Round(req.SoTienTra, 0);
                if (soTienTra <= 0m)
                {
                    return (false, "Số tiền thanh toán phải lớn hơn 0 đ.");
                }
                if (soTienTra > conNo)
                {
                    return (false, $"Số tiền thanh toán ({soTienTra:N0} đ) không được vượt quá phần còn nợ ({conNo:N0} đ).");
                }

                phieu.DaThanhToan += soTienTra;
                phieu.TrangThaiThanhToan = XacDinhTrangThaiThanhToan(phieu.TongPhaiTra, phieu.DaThanhToan);

                string dienGiai = string.IsNullOrWhiteSpace(req.GhiChu)
                    ? $"Chi trả công nợ phiếu nhập {phieu.SoPhieuNhap} ({phieu.NhaCungCap})"
                    : $"{req.GhiChu.Trim()} (Phiếu nhập {phieu.SoPhieuNhap})";

                string maPhieuChi = await GhiPhieuChiSoQuyChoPhieuNhapAsync(phieu, soTienTra, req.PhuongThuc, dienGiai);
                await _db.SaveChangesAsync();

                return (true, $"Đã thanh toán {soTienTra:N0} đ cho phiếu {phieu.SoPhieuNhap} và ghi Phiếu Chi [{maPhieuChi}] vào Sổ quỹ!");
            });
        }

        private async Task SinhLoVaCapNhatTonKhoKhiNhapAsync(PhieuNhapKho phieu)
        {
            var dsMaSp = phieu.ChiTietPhieuNhaps.Select(x => x.MaSp).Distinct().ToList();
            var mapBanh = await _db.SanPhamBanhs
                .Where(b => dsMaSp.Contains(b.MaBanh))
                .ToDictionaryAsync(b => b.MaBanh);

            // Tính tổng tồn hiện tại trên các lô đang có trước khi cộng lô mới để bình quân giá vốn chính xác
            var mapTonTruocNhap = await _db.LoSanXuatBanhs
                .AsNoTracking()
                .Where(l => dsMaSp.Contains(l.MaBanh) && l.TrangThaiLo != "DA_HUY")
                .GroupBy(l => l.MaBanh)
                .Select(g => new { MaBanh = g.Key, Tong = g.Sum(x => x.SlconLai) })
                .ToDictionaryAsync(x => x.MaBanh, x => x.Tong);

            int stt = 1;
            var loMoiTao = new List<(ChiTietPhieuNhap Ct, LoSanXuatBanh Lo)>();

            foreach (var ct in phieu.ChiTietPhieuNhaps)
            {
                if (mapBanh.TryGetValue(ct.MaSp, out var banh))
                {
                    int tonCu = Math.Max(0, mapTonTruocNhap.GetValueOrDefault(ct.MaSp, banh.TongTonKho));
                    if (ct.DonGiaNhap > 0m)
                    {
                        banh.GiaVon = (tonCu > 0 && banh.GiaVon > 0m)
                            ? Math.Round(((tonCu * banh.GiaVon) + (ct.SoLuong * ct.DonGiaNhap)) / (tonCu + ct.SoLuong), 0)
                            : Math.Round(ct.DonGiaNhap, 0);
                    }
                    mapTonTruocNhap[ct.MaSp] = tonCu + ct.SoLuong;
                }

                string baseCode = $"LO-{phieu.SoPhieuNhap}-{ct.MaSp:D2}-{stt:D2}";
                if (baseCode.Length > 45) baseCode = baseCode[..45];
                string maLoCode = baseCode;
                while (await _db.LoSanXuatBanhs.AsNoTracking().AnyAsync(l => l.MaLoCode == maLoCode))
                {
                    maLoCode = $"{baseCode}-{Random.Shared.Next(10, 99)}";
                    if (maLoCode.Length > 50) maLoCode = maLoCode[..50];
                }

                var lo = new LoSanXuatBanh
                {
                    MaBanh = ct.MaSp,
                    MaLoCode = maLoCode,
                    NgaySanXuat = ct.NgaySanXuat,
                    HanSuDung = ct.HanSuDung,
                    SlnhapBanDau = ct.SoLuong,
                    SlconLai = ct.SoLuong,
                    PhanTramGiamCanDate = 0m,
                    TrangThaiLo = ct.HanSuDung < LayNgayHienTai() ? "HET_HAN" : "DANG_BAN"
                };

                _db.LoSanXuatBanhs.Add(lo);
                loMoiTao.Add((ct, lo));
                stt++;
            }

            await _db.SaveChangesAsync();

            foreach (var (ct, lo) in loMoiTao)
            {
                ct.MaLo = lo.MaLo;
            }

            await DongBoTongTonKhoSanPhamAsync(dsMaSp);
        }

        private async Task DongBoTongTonKhoSanPhamAsync(List<int> dsMaSp)
        {
            if (dsMaSp.Count == 0) return;

            var dsBanh = await _db.SanPhamBanhs
                .Include(b => b.LoSanXuatBanhs)
                .Where(b => dsMaSp.Contains(b.MaBanh))
                .ToListAsync();

            foreach (var banh in dsBanh)
            {
                banh.TongTonKho = banh.LoSanXuatBanhs
                    .Where(l => l.TrangThaiLo != "DA_HUY")
                    .Sum(l => Math.Max(0, l.SlconLai));
            }
        }

        private async Task<string> GhiPhieuChiSoQuyChoPhieuNhapAsync(
            PhieuNhapKho phieu,
            decimal soTienChi,
            string? phuongThuc,
            string ghiChu)
        {
            DateTime bayGio = LayThoiGianHeThong();
            string ptChuan = ChuanHoaChuoi(phuongThuc, "CHUYEN_KHOAN").ToUpperInvariant();
            if (ptChuan != "TIEN_MAT" && ptChuan != "CHUYEN_KHOAN")
            {
                ptChuan = ptChuan.Contains("MAT") ? "TIEN_MAT" : "CHUYEN_KHOAN";
            }

            string maPhieuHienThi = $"PCH-PN-{phieu.SoPhieuNhap.Replace("PN-", "")}-{bayGio:HHmmss}";
            if (maPhieuHienThi.Length > 45) maPhieuHienThi = maPhieuHienThi[..45];
            while (await _db.PhieuThuChis.AsNoTracking().AnyAsync(p => p.MaPhieuHienThi == maPhieuHienThi))
            {
                maPhieuHienThi = $"PCH-PN-{phieu.MaPn}-{bayGio:HHmmss}-{Random.Shared.Next(10, 99)}";
                if (maPhieuHienThi.Length > 50) maPhieuHienThi = maPhieuHienThi[..50];
            }

            string ghiChuChuan = ghiChu.Length > 500 ? ghiChu[..500] : ghiChu;
            string doiTuong = phieu.NhaCungCap.Length > 150 ? phieu.NhaCungCap[..150] : phieu.NhaCungCap;

            _db.PhieuThuChis.Add(new PhieuThuChi
            {
                MaPhieuHienThi = maPhieuHienThi,
                LoaiPhieu = "CHI",
                HangMuc = "Chi thanh toán phiếu nhập hàng",
                SoTien = soTienChi,
                PhuongThuc = ptChuan,
                MaDonHangLienKet = null,
                MaPhieuNhapLienKet = phieu.MaPn,
                NguoiNopNhan = doiTuong,
                GhiChu = ghiChuChuan,
                NgayLap = bayGio
            });

            return maPhieuHienThi;
        }

        private async Task<string> SinhSoPhieuNhapAsync(DateTime ngay)
        {
            string tienTo = $"PN-{ngay:yyyyMMdd}-";
            var dsCungNgay = await _db.PhieuNhapKhos
                .AsNoTracking()
                .Where(p => p.SoPhieuNhap.StartsWith(tienTo))
                .Select(p => p.SoPhieuNhap)
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

        private static string XacDinhTrangThaiThanhToan(decimal tongPhaiTra, decimal daThanhToan)
        {
            if (daThanhToan <= 0m) return "CHUA_THANH_TOAN";
            if (daThanhToan >= tongPhaiTra) return "DA_THANH_TOAN";
            return "THANH_TOAN_MOT_PHAN";
        }

        private static List<DongChiTietNhapDto> ParseChiTietNhap(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                return JsonSerializer.Deserialize<List<DongChiTietNhapDto>>(json, _jsonOpts)
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
