using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN")]
    public class DashboardController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;

        public DashboardController(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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

            var vm = new AdminDashboardViewModel
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
                DanhSachDonHangMoiNhat = danhSachDon.Take(6).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KhoiPhucDuLieuGoc()
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync("EXEC sp_KhoiPhucDuLieuGoc");
                TempData["ThongBaoAdmin"] = "Đã thực thi EXEC sp_KhoiPhucDuLieuGoc thành công! Toàn bộ 25 bảng CSDL đã được khôi phục về trạng thái chuẩn ban đầu.";
            }
            catch (Exception ex)
            {
                TempData["ThongBaoAdmin"] = $"Thông báo khi chạy sp_KhoiPhucDuLieuGoc: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
