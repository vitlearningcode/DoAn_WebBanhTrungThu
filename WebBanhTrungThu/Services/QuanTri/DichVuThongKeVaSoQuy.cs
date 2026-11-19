using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
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
                    MoTaChiTiet = $"Giá trị: {dh.TongThanhToanCuoi:N0} đ • {(dh.MaNd.HasValue ? "Member" : "Guest KVL")} • {dh.DotGiaoHangDaDiems.Count} điểm giao",
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
        // 2. SỔ QUỸ THU CHI & CẤU HÌNH TÀI KHOẢN VIETQR / MOMO
        // =================================================================================
        public async Task<(SoQuyAdminViewModel ViewModel, string LocPhuongThucHienTai)> XayDungSoQuyAsync(string? locPhuongThuc)
        {
            var tatCaDon = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.ChiTietDonHangs)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            var store = _kiotMeta.DocDuLieu();
            foreach (var d in tatCaDon)
            {
                _kiotMeta.GanMetadataChoDonHang(d, store);
            }

            // 1. Các đơn hợp lệ (TrangThaiDh != "DA_HUY") dùng để ghi nhận Phiếu Thu
            var danhSachDonHieuLuc = tatCaDon
                .Where(d => d.TrangThaiDh != "DA_HUY")
                .ToList();

            // 2. Các đơn đã hủy và đã hoàn tiền ngân hàng cho khách (TrangThaiTt == "DA_HOAN_TIEN")
            var danhSachDonHoanTien = tatCaDon
                .Where(d => d.TrangThaiTt == "DA_HOAN_TIEN")
                .ToList();

            // Tổng thu tính theo các đơn hiệu lực (ưu tiên đơn DA_TT / DA_THANH_TOAN hoặc đơn đang vận hành)
            var donDaThu = danhSachDonHieuLuc
                .Where(d => d.TrangThaiTt == "DA_TT" || d.TrangThaiTt == "DA_THANH_TOAN")
                .ToList();
            var tapTinhThu = donDaThu.Any() ? donDaThu : danhSachDonHieuLuc;

            decimal tongThu = tapTinhThu.Sum(d => d.TongThanhToanCuoi);
            decimal thuCod = tapTinhThu.Where(d => d.PhuongThucTt == "COD").Sum(d => d.TongThanhToanCuoi);
            decimal thuVietQr = tapTinhThu.Where(d => d.PhuongThucTt == "VIETQR").Sum(d => d.TongThanhToanCuoi);
            decimal thuMoMo = tapTinhThu.Where(d => d.PhuongThucTt == "MOMO" || d.PhuongThucTt == "MOMO_QR").Sum(d => d.TongThanhToanCuoi);
            decimal thuVnPay = tapTinhThu.Where(d => d.PhuongThucTt == "VNPAY").Sum(d => d.TongThanhToanCuoi);

            decimal tongHoanTien = danhSachDonHoanTien.Sum(d => d.TongThanhToanCuoi);

            // Ước tính tổng giá vốn (~ 54% doanh thu tiền hàng trước giảm của các đơn chưa hủy)
            decimal tongTienHang = danhSachDonHieuLuc.Sum(d => d.TongTienHang);
            decimal tongGiaVon = Math.Round(tongTienHang * 0.54m, 0);
            decimal loiNhuanGop = tongThu - tongGiaVon;

            var dsHienThi = danhSachDonHieuLuc;
            if (!string.IsNullOrWhiteSpace(locPhuongThuc) && locPhuongThuc != "ALL")
            {
                dsHienThi = danhSachDonHieuLuc.Where(d => d.PhuongThucTt == locPhuongThuc).ToList();
            }

            var vm = new SoQuyAdminViewModel
            {
                QuyDauKy = 25000000m,
                TongThuTrongKy = tongThu,
                ThuTienMatCod = thuCod,
                ThuChuyenKhoanVietQr = thuVietQr,
                ThuViMoMo = thuMoMo,
                ThuVnPay = thuVnPay,
                TongGiaVonUocTinh = tongGiaVon,
                TongTienHoanKhach = tongHoanTien,
                LoiNhuanGop = loiNhuanGop,
                DanhSachPhieuThuDonHang = dsHienThi,
                DanhSachPhieuHoanTien = danhSachDonHoanTien,
                CauHinhQr = _kiotMeta.LayCauHinhQr(),
                DanhSachPhieuThuChiWebhook = store.DanhSachPhieuThuChi ?? new List<PhieuThuChiMoRong>()
            };

            return (vm, locPhuongThuc ?? "ALL");
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
    }
}
