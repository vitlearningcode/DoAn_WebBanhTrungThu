using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách Thống kê KPI Dashboard Quản trị, Khôi phục dữ liệu gốc và Báo cáo Sổ quỹ Thu/Chi &amp; Cấu hình VietQR.
    /// Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuThongKeVaSoQuy : DichVuCoSo, IDichVuThongKeVaSoQuy
    {
        private readonly IDichVuCauHinhHeThong _kiotMeta;

        public DichVuThongKeVaSoQuy(CSDLBanhTrungThuContext db, IDichVuCauHinhHeThong kiotMeta) : base(db)
        {
            _kiotMeta = kiotMeta;
        }

        // =================================================================================
        // 1. THỐNG KÊ KPI & BIỂU ĐỒ DOANH THU DASHBOARD QUẢN TRỊ
        // =================================================================================
        public async Task<AdminDashboardViewModel> XayDungDashboardAsync()
        {
            var danhSachDon = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            decimal tongDoanhThu = danhSachDon
                .Where(d => d.TrangThaiDh != "DA_HUY")
                .Sum(d => d.TongThanhToanCuoi);

            int tongSoDon = danhSachDon.Count;
            int soDonMember = danhSachDon.Count(d => d.MaNd.HasValue);
            int soDonGuest = danhSachDon.Count(d => !d.MaNd.HasValue);
            int soDonB2B = danhSachDon.Count(d => d.LaDonDoanhNghiepB2b);

            int tongHopTuChon = await _db.HopQuaTuChons.CountAsync();
            int tongLuotMoThiep = await _db.DotGiaoHangDaDiems.SumAsync(d => (int?)d.SoLanMoThiep) ?? 0;

            var loCanDate = await _db.LoSanXuatBanhs
                .AsNoTracking()
                .Include(l => l.MaBanhNavigation)
                .OrderBy(l => l.HanSuDung)
                .ToListAsync();

            var voHopTonKho = await _db.MauVoHops
                .AsNoTracking()
                .OrderBy(v => v.SltonKhoVo)
                .ToListAsync();

            int soLoSapHetHan = loCanDate.Count(l => l.PhanTramGiamCanDate > 0 || l.TrangThaiLo == "XA_CAN_DATE");
            int soBanhTonThap = await _db.SanPhamBanhs.CountAsync(b => b.TongTonKho < 40);

            // Tính Top 5 sản phẩm/hộp quà doanh thu cao nhất
            var dsChiTiet = danhSachDon
                .Where(d => d.TrangThaiDh != "DA_HUY")
                .SelectMany(d => d.ChiTietDonHangs)
                .GroupBy(c => c.TenSanPhamLuuCung)
                .Select(g => new TopBanhBanChayItem
                {
                    TenBanh = g.Key,
                    SoLuongBan = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(x => x.DoanhThu)
                .Take(5)
                .ToList();

            if (!dsChiTiet.Any())
            {
                var dsBanhMau = await _db.SanPhamBanhs.AsNoTracking().Take(5).ToListAsync();
                foreach (var b in dsBanhMau)
                {
                    dsChiTiet.Add(new TopBanhBanChayItem
                    {
                        TenBanh = b.TenBanh,
                        SoLuongBan = 12,
                        DoanhThu = b.DonGiaLe * 12
                    });
                }
            }

            decimal maxDt = dsChiTiet.Any() ? Math.Max(1m, dsChiTiet.Max(x => x.DoanhThu)) : 1m;
            foreach (var item in dsChiTiet)
            {
                item.PhanTram = (int)Math.Round((item.DoanhThu / maxDt) * 100m);
            }

            // Tổng hợp Hoạt động gần đây (Đơn hàng mới, Phiên Guest đã gộp giỏ, Thiệp QR đã mở)
            var hoatDongs = new List<HoatDongGanDayItem>();

            foreach (var dh in danhSachDon.Take(4))
            {
                hoatDongs.Add(new HoatDongGanDayItem
                {
                    BieuTuong = "🧾",
                    MauSac = "primary",
                    TieuDe = $"{dh.HoTenNguoiMua} vừa đặt đơn #{dh.MaTraCuuDh}",
                    MoTaChiTiet = $"Giá trị: {dh.TongThanhToanCuoi:N0} đ • {(dh.MaNd.HasValue ? "Member" : "Guest KVL")}",
                    ThoiGian = dh.NgayDat.ToString("dd/MM/yyyy HH:mm")
                });
            }

            var phienDaGop = await _db.PhienKhachVangLais
                .AsNoTracking()
                .Include(p => p.MaNdDaGopNavigation)
                .Where(p => p.DaGopGioHang)
                .OrderByDescending(p => p.NgayTao)
                .Take(3)
                .ToListAsync();

            foreach (var p in phienDaGop)
            {
                hoatDongs.Add(new HoatDongGanDayItem
                {
                    BieuTuong = "🔄",
                    MauSac = "success",
                    TieuDe = $"Hợp nhất giỏ hàng Guest ({p.MaPhienKvl})",
                    MoTaChiTiet = $"Đã gộp vào tài khoản Member: {p.MaNdDaGopNavigation?.HoTen ?? $"#{p.MaNdDaGop}"}",
                    ThoiGian = p.NgayTao.ToString("dd/MM/yyyy HH:mm")
                });
            }

            var thiepDaMo = await _db.DotGiaoHangDaDiems
                .AsNoTracking()
                .Include(d => d.MaDhNavigation)
                .Where(d => d.SoLanMoThiep > 0)
                .OrderByDescending(d => d.SoLanMoThiep)
                .Take(3)
                .ToListAsync();

            foreach (var t in thiepDaMo)
            {
                hoatDongs.Add(new HoatDongGanDayItem
                {
                    BieuTuong = "🌕",
                    MauSac = "warning",
                    TieuDe = $"Người nhận {t.TenNguoiNhan} đã mở Thiệp QR 3D",
                    MoTaChiTiet = $"Token: {t.MaTokenThiepQr} • Đã mở xem {t.SoLanMoThiep} lần (Đơn #{t.MaDhNavigation?.MaTraCuuDh})",
                    ThoiGian = $"Hẹn giao {t.NgayHenGiao:dd/MM/yyyy}"
                });
            }

            return new AdminDashboardViewModel
            {
                TongDoanhThuThucTe = tongDoanhThu,
                TongSoDonHang = tongSoDon,
                SoDonMember = soDonMember,
                SoDonGuest = soDonGuest,
                SoDonB2B = soDonB2B,
                TongSoHopTuChonDaTao = tongHopTuChon,
                TongLuotMoThiepQr3D = tongLuotMoThiep,
                SoLoSapHetHan = soLoSapHetHan,
                SoBanhTonKhoThap = soBanhTonThap,
                TopBanhBanChay = dsChiTiet,
                DanhSachHoatDong = hoatDongs,
                DanhSachLoCanDate = loCanDate,
                DanhSachVoHopTonKho = voHopTonKho,
                DanhSachDonHangMoiNhat = danhSachDon.Take(6).ToList(),
                CauHinhQr = _kiotMeta.LayCauHinhQr()
            };
        }

        public async Task<string> KhoiPhucDuLieuGocAsync()
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync("EXEC sp_KhoiPhucDuLieuGoc");
                return "Đã thực thi EXEC sp_KhoiPhucDuLieuGoc thành công! Toàn bộ 25 bảng CSDL đã được khôi phục về trạng thái chuẩn ban đầu.";
            }
            catch (Exception ex)
            {
                return $"Thông báo khi chạy sp_KhoiPhucDuLieuGoc: {ex.Message}";
            }
        }

        // =================================================================================
        // 2. SỔ QUỸ THU CHI (SQL-FIRST TRỰC TIẾP TRÊN dbo.PhieuThuChi) & CẤU HÌNH VIETQR / MOMO
        // =================================================================================
        public async Task<(SoQuyAdminViewModel ViewModel, string LocPhuongThucHienTai)> XayDungSoQuyAsync(
            string? locPhuongThuc,
            string? locLoaiPhieu = null,
            string? tuKhoa = null)
        {
            var dsPhieuDb = await _context.PhieuThuChis
                .AsNoTracking()
                .OrderByDescending(p => p.NgayLap)
                .ThenByDescending(p => p.MaPhieu)
                .ToListAsync();

            var dsMaDhLienKet = dsPhieuDb
                .Where(p => p.MaDonHangLienKet.HasValue)
                .Select(p => p.MaDonHangLienKet!.Value)
                .Distinct()
                .ToList();

            var mapDonHang = new Dictionary<int, (string MaTraCuuDh, string? Sdt, string HoTen)>();
            if (dsMaDhLienKet.Count > 0)
            {
                var dsDh = await _context.DonHangs
                    .AsNoTracking()
                    .Where(d => dsMaDhLienKet.Contains(d.MaDh))
                    .Select(d => new { d.MaDh, d.MaTraCuuDh, d.SdtnguoiMua, d.HoTenNguoiMua })
                    .ToListAsync();

                foreach (var dh in dsDh)
                {
                    mapDonHang[dh.MaDh] = (dh.MaTraCuuDh, dh.SdtnguoiMua, dh.HoTenNguoiMua);
                }
            }

            var tatCaPhieu = dsPhieuDb.Select(p =>
            {
                string loai = string.Equals(p.LoaiPhieu?.Trim(), "CHI", StringComparison.OrdinalIgnoreCase) ? "CHI" : "THU";
                string maPhieu = !string.IsNullOrWhiteSpace(p.MaPhieuHienThi)
                    ? p.MaPhieuHienThi.Trim()
                    : $"{(loai == "CHI" ? "PCH" : "PTH")}-{p.MaPhieu:D6}";
                string phuongThuc = string.IsNullOrWhiteSpace(p.PhuongThuc)
                    ? "TIEN_MAT"
                    : p.PhuongThuc.Trim().ToUpperInvariant();

                string? maTraCuuDh = null;
                string? sdtDoiTuong = null;
                string doiTuong = string.IsNullOrWhiteSpace(p.NguoiNopNhan)
                    ? "Khách Hàng Nguyệt Cát"
                    : p.NguoiNopNhan.Trim();

                if (p.MaDonHangLienKet.HasValue && mapDonHang.TryGetValue(p.MaDonHangLienKet.Value, out var dhInfo))
                {
                    maTraCuuDh = dhInfo.MaTraCuuDh;
                    sdtDoiTuong = dhInfo.Sdt;
                    if (string.IsNullOrWhiteSpace(p.NguoiNopNhan))
                    {
                        doiTuong = dhInfo.HoTen;
                    }
                }

                return new PhieuThuChiSoQuyItem
                {
                    MaPhieuId = p.MaPhieu,
                    MaPhieu = maPhieu,
                    LoaiPhieu = loai,
                    HangMuc = string.IsNullOrWhiteSpace(p.HangMuc)
                        ? (loai == "CHI" ? "Chi khác" : "Thu tiền bán hàng")
                        : p.HangMuc.Trim(),
                    SoTien = p.SoTien,
                    PhuongThuc = phuongThuc,
                    DoiTuong = doiTuong,
                    SdtDoiTuong = sdtDoiTuong,
                    MaDh = p.MaDonHangLienKet,
                    MaTraCuuDh = maTraCuuDh,
                    GhiChu = p.GhiChu,
                    NgayLap = p.NgayLap
                };
            }).ToList();

            decimal tongThu = tatCaPhieu.Where(p => p.LoaiPhieu == "THU").Sum(p => p.SoTien);
            decimal tongChi = tatCaPhieu.Where(p => p.LoaiPhieu == "CHI").Sum(p => p.SoTien);
            int soPhieuThu = tatCaPhieu.Count(p => p.LoaiPhieu == "THU");
            int soPhieuChi = tatCaPhieu.Count(p => p.LoaiPhieu == "CHI");

            int soPhieuTienMat = tatCaPhieu.Count(p => LayNhomPhuongThuc(p.PhuongThuc) == "TIEN_MAT");
            int soPhieuChuyenKhoan = tatCaPhieu.Count(p => LayNhomPhuongThuc(p.PhuongThuc) == "CHUYEN_KHOAN");
            int soPhieuViDienTu = tatCaPhieu.Count(p => LayNhomPhuongThuc(p.PhuongThuc) == "VI_DIEN_TU");
            int soPhieuTheNganHang = tatCaPhieu.Count(p => LayNhomPhuongThuc(p.PhuongThuc) == "THE_NGAN_HANG");

            string locLoaiChuan = string.IsNullOrWhiteSpace(locLoaiPhieu)
                ? "ALL"
                : locLoaiPhieu.Trim().ToUpperInvariant();
            if (locLoaiChuan != "THU" && locLoaiChuan != "CHI")
            {
                locLoaiChuan = "ALL";
            }

            string locPtChuan = string.IsNullOrWhiteSpace(locPhuongThuc)
                ? "ALL"
                : locPhuongThuc.Trim().ToUpperInvariant();
            string tuKhoaChuan = string.IsNullOrWhiteSpace(tuKhoa) ? string.Empty : tuKhoa.Trim();

            bool daLocServer = locLoaiChuan != "ALL"
                || locPtChuan != "ALL"
                || !string.IsNullOrEmpty(tuKhoaChuan);

            IEnumerable<PhieuThuChiSoQuyItem> truyVanLoc = tatCaPhieu;

            if (locLoaiChuan != "ALL")
            {
                truyVanLoc = truyVanLoc.Where(p => p.LoaiPhieu == locLoaiChuan);
            }

            if (locPtChuan != "ALL")
            {
                truyVanLoc = truyVanLoc.Where(p =>
                    p.PhuongThuc == locPtChuan || LayNhomPhuongThuc(p.PhuongThuc) == locPtChuan);
            }

            if (!string.IsNullOrEmpty(tuKhoaChuan))
            {
                truyVanLoc = truyVanLoc.Where(p =>
                    p.MaPhieu.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(p.MaTraCuuDh) && p.MaTraCuuDh.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase))
                    || p.HangMuc.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase)
                    || p.DoiTuong.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(p.SdtDoiTuong) && p.SdtDoiTuong.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(p.GhiChu) && p.GhiChu.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase))
                    || p.PhuongThuc.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase)
                    || p.LoaiPhieu.Contains(tuKhoaChuan, StringComparison.OrdinalIgnoreCase));
            }

            var vm = new SoQuyAdminViewModel
            {
                QuyDauKy = 25000000m,
                TongThuTrongKy = tongThu,
                TongChiTrongKy = tongChi,
                TongSoPhieu = tatCaPhieu.Count,
                SoPhieuThu = soPhieuThu,
                SoPhieuChi = soPhieuChi,
                SoPhieuTienMat = soPhieuTienMat,
                SoPhieuChuyenKhoan = soPhieuChuyenKhoan,
                SoPhieuViDienTu = soPhieuViDienTu,
                SoPhieuTheNganHang = soPhieuTheNganHang,
                LocLoaiPhieu = locLoaiChuan,
                LocPhuongThuc = locPtChuan,
                TuKhoa = tuKhoaChuan,
                DaLocServer = daLocServer,
                DanhSachPhieuThuChi = truyVanLoc.ToList(),
                CauHinhQr = _kiotMeta.LayCauHinhQr()
            };

            return (vm, locPtChuan);
        }

        public async Task<(bool ThanhCong, string ThongBao)> LapPhieuThuChiThuCongAsync(
            string loaiPhieu,
            string hangMuc,
            decimal soTien,
            string phuongThuc,
            string doiTuong,
            string? maTraCuuDh,
            string? ghiChu)
        {
            string loai = string.Equals(loaiPhieu?.Trim(), "CHI", StringComparison.OrdinalIgnoreCase) ? "CHI" : "THU";
            decimal soTienLamTron = Math.Round(soTien, 0);
            if (soTienLamTron <= 0m)
            {
                return (false, "Số tiền lập phiếu phải lớn hơn 0 đ.");
            }

            string hangMucChuan = ChuanHoaChuoi(hangMuc, loai == "CHI" ? "Chi khác" : "Thu khác");
            if (hangMucChuan.Length > 100) hangMucChuan = hangMucChuan[..100];

            string doiTuongChuan = ChuanHoaChuoi(doiTuong, loai == "CHI" ? "Đối tác / Nhà cung cấp" : "Khách lẻ");
            if (doiTuongChuan.Length > 150) doiTuongChuan = doiTuongChuan[..150];

            string phuongThucChuan = ChuanHoaChuoi(phuongThuc, "TIEN_MAT").ToUpperInvariant();
            if (phuongThucChuan.Length > 30) phuongThucChuan = phuongThucChuan[..30];

            string? ghiChuChuan = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
            if (ghiChuChuan != null && ghiChuChuan.Length > 500) ghiChuChuan = ghiChuChuan[..500];

            int? maDhLienKet = null;
            if (!string.IsNullOrWhiteSpace(maTraCuuDh))
            {
                string maChuan = maTraCuuDh.Trim().TrimStart('#').ToUpperInvariant();
                var dh = await _context.DonHangs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.MaTraCuuDh == maChuan);

                if (dh == null)
                {
                    return (false, $"Không tìm thấy đơn hàng #{maChuan} trong hệ thống.");
                }

                maDhLienKet = dh.MaDh;
            }

            DateTime bayGio = LayThoiGianHeThong();
            string tienTo = loai == "CHI" ? "PCH" : "PTH";
            string maPhieuHienThi = $"{tienTo}-{bayGio:yyMMddHHmmss}";

            if (await _context.PhieuThuChis.AnyAsync(p => p.MaPhieuHienThi == maPhieuHienThi))
            {
                maPhieuHienThi = $"{maPhieuHienThi}-{bayGio:fff}";
                if (await _context.PhieuThuChis.AnyAsync(p => p.MaPhieuHienThi == maPhieuHienThi))
                {
                    maPhieuHienThi = $"{tienTo}-{bayGio:yyMMddHHmmssfff}-{Random.Shared.Next(10, 99)}";
                }
            }

            _context.PhieuThuChis.Add(new PhieuThuChi
            {
                MaPhieuHienThi = maPhieuHienThi,
                LoaiPhieu = loai,
                HangMuc = hangMucChuan,
                SoTien = soTienLamTron,
                PhuongThuc = phuongThucChuan,
                MaDonHangLienKet = maDhLienKet,
                NguoiNopNhan = doiTuongChuan,
                GhiChu = ghiChuChuan,
                NgayLap = bayGio
            });

            await _context.SaveChangesAsync();

            string nhanLoai = loai == "CHI" ? "Phiếu chi" : "Phiếu thu";
            return (true, $"Đã lập {nhanLoai} {maPhieuHienThi} ({soTienLamTron:N0} đ) vào Sổ quỹ thành công!");
        }

        private static string LayNhomPhuongThuc(string? phuongThuc)
        {
            return (phuongThuc ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "TIEN_MAT" or "COD" => "TIEN_MAT",
                "CHUYEN_KHOAN" or "VIETQR" => "CHUYEN_KHOAN",
                "MOMO" or "MOMO_QR" or "VNPAY" => "VI_DIEN_TU",
                "THE_NGAN_HANG" => "THE_NGAN_HANG",
                var khac => khac
            };
        }

        public string CapNhatCauHinhQrMoMo(
            string soTaiKhoanMoMo,
            string tenChuTaiKhoan,
            string maNganHang,
            string maBinVietQr,
            string mauNoiDungCk)
        {
            string stkChuan = string.IsNullOrWhiteSpace(soTaiKhoanMoMo)
                ? "0814585526"
                : soTaiKhoanMoMo.Replace(" ", "").Trim();

            string sdtHienThi = stkChuan.Length == 10
                ? $"{stkChuan[..4]} {stkChuan.Substring(4, 3)} {stkChuan.Substring(7, 3)}"
                : stkChuan;

            string tenChuTkChuan = string.IsNullOrWhiteSpace(tenChuTaiKhoan)
                ? "LE MINH DUC"
                : tenChuTaiKhoan.Trim().ToUpperInvariant();

            _kiotMeta.CapNhatCauHinhQr(cfg =>
            {
                cfg.SoTaiKhoanMoMo = stkChuan;
                cfg.SoDienThoaiHienThi = sdtHienThi;
                cfg.TenChuTaiKhoan = tenChuTkChuan;
                cfg.MaNganHang = string.IsNullOrWhiteSpace(maNganHang) ? "MOMO" : maNganHang.Trim().ToUpperInvariant();
                cfg.MaBinVietQr = string.IsNullOrWhiteSpace(maBinVietQr) ? "970422" : maBinVietQr.Trim();
                cfg.MauNoiDungCk = string.IsNullOrWhiteSpace(mauNoiDungCk) ? "NGUYETCAT {MaTraCuuDh}" : mauNoiDungCk.Trim();
            });

            return $"Đã cập nhật cấu hình Tài khoản QR / MoMo nhận tiền: {sdtHienThi} ({tenChuTkChuan})!";
        }

        public async Task<ThongBaoQuanTriViewModel> LayThongBaoQuanTriAsync()
        {
            var homNay = LayNgayHienTai();
            var mocCanDate = homNay.AddDays(10);

            var queryDonCho = _db.DonHangs
                .AsNoTracking()
                .Where(d => d.TrangThaiDh == "CHO_XAC_NHAN" || d.TrangThaiDh == "CHO_DUYET");

            int soDonCho = await queryDonCho.CountAsync();
            var dsDonCho = await queryDonCho
                .OrderByDescending(d => d.NgayDat)
                .Take(5)
                .Select(d => new ThongBaoDonMoiItem
                {
                    MaDh = d.MaDh,
                    MaTraCuuDh = d.MaTraCuuDh,
                    HoTenNguoiMua = d.HoTenNguoiMua,
                    TongThanhToanCuoi = d.TongThanhToanCuoi,
                    PhuongThucTt = d.PhuongThucTt,
                    NgayDat = d.NgayDat
                })
                .ToListAsync();

            var queryLoCanDate = _db.LoSanXuatBanhs
                .AsNoTracking()
                .Include(l => l.MaBanhNavigation)
                .Where(l => l.SlconLai > 0
                    && l.TrangThaiLo != "HET_HAN"
                    && (l.HanSuDung <= mocCanDate || l.TrangThaiLo == "XA_CAN_DATE" || l.PhanTramGiamCanDate > 0));

            int soLoCanDate = await queryLoCanDate.CountAsync();
            var dsLoCanDateRaw = await queryLoCanDate
                .OrderBy(l => l.HanSuDung)
                .Take(5)
                .ToListAsync();

            var dsLoCanDate = dsLoCanDateRaw.Select(l => new ThongBaoLoCanDateItem
            {
                MaLo = l.MaLo,
                MaLoCode = l.MaLoCode,
                TenBanh = l.MaBanhNavigation?.TenBanh ?? "Bánh Trung Thu",
                SlConLai = l.SlconLai,
                HanSuDung = l.HanSuDung,
                SoNgayConLai = Math.Max(0, l.HanSuDung.DayNumber - homNay.DayNumber),
                PhanTramGiamCanDate = l.PhanTramGiamCanDate
            }).ToList();

            return new ThongBaoQuanTriViewModel
            {
                SoDonChoXacNhan = soDonCho,
                SoLoSapCanDate = soLoCanDate,
                DonMoiChoXacNhan = dsDonCho,
                LoSapCanDate = dsLoCanDate
            };
        }
    }
}
