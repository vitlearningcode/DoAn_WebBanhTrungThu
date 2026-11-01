using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class DonHangAdminController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuKiotVietMetadata _kiotMeta;

        public DonHangAdminController(CSDLBanhTrungThuContext db, DichVuKiotVietMetadata kiotMeta)
        {
            _db = db;
            _kiotMeta = kiotMeta;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            string? locTrangThai,
            string? locLoaiDon,
            string? locPhuongThuc,
            string? locThoiGian)
        {
            var query = _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                query = query.Where(d =>
                    d.MaTraCuuDh.Contains(tk) ||
                    d.HoTenNguoiMua.Contains(tk) ||
                    d.SdtnguoiMua.Contains(tk) ||
                    d.DotGiaoHangDaDiems.Any(g => g.TenNguoiNhan.Contains(tk) || g.SdtnguoiNhan.Contains(tk)));
            }

            if (!string.IsNullOrWhiteSpace(locTrangThai) && locTrangThai != "ALL")
            {
                if (locTrangThai == "CHO_LAY_HANG" || locTrangThai == "DANG_DONG_GOI_HOP")
                {
                    query = query.Where(d => d.TrangThaiDh == "CHO_LAY_HANG" || d.TrangThaiDh == "DANG_DONG_GOI_HOP");
                }
                else
                {
                    query = query.Where(d => d.TrangThaiDh == locTrangThai);
                }
            }

            if (locLoaiDon == "B2B")
            {
                query = query.Where(d => d.LaDonDoanhNghiepB2b);
            }
            else if (locLoaiDon == "B2C")
            {
                query = query.Where(d => !d.LaDonDoanhNghiepB2b);
            }
            else if (locLoaiDon == "GUEST")
            {
                query = query.Where(d => !d.MaNd.HasValue);
            }
            else if (locLoaiDon == "MEMBER")
            {
                query = query.Where(d => d.MaNd.HasValue);
            }

            if (!string.IsNullOrWhiteSpace(locPhuongThuc) && locPhuongThuc != "ALL")
            {
                query = query.Where(d => d.PhuongThucTt == locPhuongThuc);
            }

            if (locThoiGian == "HOM_NAY")
            {
                DateTime homNay = DateTime.Today;
                query = query.Where(d => d.NgayDat >= homNay);
            }
            else if (locThoiGian == "THANG_NAY")
            {
                DateTime dauThang = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                query = query.Where(d => d.NgayDat >= dauThang);
            }

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.LocTrangThai = locTrangThai ?? "ALL";
            ViewBag.LocLoaiDon = locLoaiDon ?? "ALL";
            ViewBag.LocPhuongThuc = locPhuongThuc ?? "ALL";
            ViewBag.LocThoiGian = locThoiGian ?? "ALL";

            var danhSach = await query.OrderByDescending(d => d.NgayDat).ToListAsync();
            var store = _kiotMeta.DocDuLieu();
            foreach (var dh in danhSach)
            {
                _kiotMeta.GanMetadataChoDonHang(dh, store);
            }

            return View(danhSach);
        }

        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == id);

            if (donHang == null)
            {
                return RedirectToAction(nameof(Index));
            }

            _kiotMeta.GanMetadataChoDonHang(donHang, _kiotMeta.DocDuLieu());
            var vm = TaoViewModelDonHangQr(donHang);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> InHoaDon(int id)
        {
            var donHang = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == id);

            if (donHang == null)
            {
                return RedirectToAction(nameof(Index));
            }

            _kiotMeta.GanMetadataChoDonHang(donHang, _kiotMeta.DocDuLieu());
            var vm = TaoViewModelDonHangQr(donHang);
            return View(vm);
        }

        private DonHangThanhCongViewModel TaoViewModelDonHangQr(Models.Entities.DonHang donHang)
        {
            var vm = new DonHangThanhCongViewModel
            {
                DonHang = donHang
            };

            using var qrGenerator = new QRCodeGenerator();
            foreach (var dot in donHang.DotGiaoHangDaDiems)
            {
                string urlThiep = string.Empty;
                string qrBase64 = string.Empty;

                if (dot.CoKemThiepQr && !string.IsNullOrWhiteSpace(dot.MaTokenThiepQr))
                {
                    urlThiep = $"{Request.Scheme}://{Request.Host}/Thiep3D/Xem/{dot.MaTokenThiepQr}";
                    using QRCodeData qrCodeData = qrGenerator.CreateQrCode(urlThiep, QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new PngByteQRCode(qrCodeData);
                    byte[] qrCodeBytes = qrCode.GetGraphic(10);
                    qrBase64 = "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
                }

                vm.DanhSachDotGiaoQr.Add(new DotGiaoKemQrItem
                {
                    DotGiao = dot,
                    UrlThiep3D = urlThiep,
                    AnhQrBase64 = qrBase64
                });
            }

            return vm;
        }

        // =========================================================================
        // 1. API / ACTION XÁC NHẬN CHUYỂN KHOẢN QR -> TỰ ĐỘNG CHUYỂN SANG CHỜ LẤY HÀNG
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanChuyenKhoanQr(int maDh, string? returnUrl)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang != null && donHang.TrangThaiDh != "DA_HUY")
            {
                donHang.TrangThaiTt = "DA_TT";
                if (donHang.TrangThaiDh == "CHO_XAC_NHAN" || donHang.TrangThaiDh == "DANG_DONG_GOI_HOP")
                {
                    donHang.TrangThaiDh = "CHO_LAY_HANG";
                }

                foreach (var dot in donHang.DotGiaoHangDaDiems)
                {
                    if (dot.TrangThaiGiao != "HOAN_TAT" && dot.TrangThaiGiao != "DA_HUY")
                    {
                        dot.TrangThaiGiao = "CHO_LAY_HANG";
                    }
                }

                await _db.SaveChangesAsync();

                string nguoiCapNhat = User.Identity?.Name ?? "Thu ngân";
                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, meta =>
                {
                    meta.ThoiGianCapNhatCuoi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    meta.NguoiCapNhatCuoi = nguoiCapNhat;
                });

                TempData["ThongBaoAdmin"] = $"⚡ Đã xác nhận nhận chuyển khoản QR cho đơn #{donHang.MaTraCuuDh} ({donHang.TongThanhToanCuoi:N0} đ) -> Tự động đẩy sang trạng thái Chờ lấy hàng (CHO_LAY_HANG)!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 2. ĐIỀU PHỐI TRẠNG THÁI THEO BƯỚC CHUẨN & QUYỀN ADMIN OVERRIDE
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(
            int maDh,
            string? trangThaiDh,
            string? trangThaiTt,
            bool laAdminOverride = false,
            string? ghiChuDieuChinh = null,
            string? returnUrl = null)
        {
            if (laAdminOverride && !User.IsInRole("QUAN_TRI"))
            {
                TempData["ThongBaoAdmin"] = "⚠️ Chỉ Quản trị viên (QUAN_TRI) mới có quyền điều chỉnh trạng thái thủ công (Admin Override)!";
                return RedirectToAction(nameof(Index));
            }

            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang != null)
            {
                if (!string.IsNullOrWhiteSpace(trangThaiDh))
                {
                    donHang.TrangThaiDh = trangThaiDh;
                    foreach (var dot in donHang.DotGiaoHangDaDiems)
                    {
                        dot.TrangThaiGiao = trangThaiDh;
                    }

                    // Nếu nhân viên giao hàng xác nhận HOAN_TAT cho đơn COD, tự động ghi nhận DA_TT
                    if (trangThaiDh == "HOAN_TAT" && string.IsNullOrWhiteSpace(trangThaiTt) && donHang.TrangThaiTt == "CHUA_TT")
                    {
                        donHang.TrangThaiTt = "DA_TT";
                    }
                }

                if (!string.IsNullOrWhiteSpace(trangThaiTt))
                {
                    donHang.TrangThaiTt = trangThaiTt;
                }

                await _db.SaveChangesAsync();

                string nguoiCapNhat = User.Identity?.Name ?? "Admin";
                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, meta =>
                {
                    meta.ThoiGianCapNhatCuoi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    meta.NguoiCapNhatCuoi = nguoiCapNhat;
                    if (!string.IsNullOrWhiteSpace(ghiChuDieuChinh))
                    {
                        meta.GhiChuDieuChinhAdmin = ghiChuDieuChinh.Trim();
                    }
                });

                string nhanCheDo = laAdminOverride ? "[Admin Override] " : "";
                TempData["ThongBaoAdmin"] = $"{nhanCheDo}Đã cập nhật hóa đơn #{donHang.MaTraCuuDh} -> Trạng thái đơn: {donHang.TrangThaiDh} | Thanh toán: {donHang.TrangThaiTt}!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 3. HỦY PHIẾU KIỂM TRA CHỨNG TỪ LIÊN QUAN + HOÀN TIỀN VIETQR + HOÀN KHO
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDonVaHoanTien(
            int maDh,
            string? maNganHangHoan,
            string? soTaiKhoanHoan,
            string? tenChuTkHoan,
            string? lyDoHuy,
            string? returnUrl)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (donHang.TrangThaiDh == "DA_HUY")
            {
                TempData["ThongBaoAdmin"] = $"Đơn hàng #{donHang.MaTraCuuDh} đã ở trạng thái Đã hủy trước đó.";
                return RedirectToAction(nameof(Index));
            }

            bool canHoanTien = donHang.TrangThaiTt == "DA_TT";
            if (canHoanTien)
            {
                if (string.IsNullOrWhiteSpace(maNganHangHoan) || string.IsNullOrWhiteSpace(soTaiKhoanHoan) || string.IsNullOrWhiteSpace(tenChuTkHoan))
                {
                    TempData["ThongBaoAdmin"] = $"⚠️ Đơn hàng #{donHang.MaTraCuuDh} đã thanh toán ({donHang.TongThanhToanCuoi:N0} đ). Bắt buộc phải nhập Ngân hàng, Số tài khoản và Tên chủ tài khoản để hoàn tiền trước khi hủy!";
                    return RedirectToAction(nameof(Index));
                }

                donHang.TrangThaiTt = "DA_HOAN_TIEN";
            }

            // 1. Chuyển trạng thái đơn hàng & toàn bộ phiếu giao hàng đa điểm sang DA_HUY
            donHang.TrangThaiDh = "DA_HUY";
            foreach (var dot in donHang.DotGiaoHangDaDiems)
            {
                dot.TrangThaiGiao = "DA_HUY";
            }

            // 2. Hoàn trả lại tồn kho bánh và vỏ hộp
            int soBanhHoanKho = 0;
            int soVoHopHoanKho = 0;

            foreach (var ct in donHang.ChiTietDonHangs)
            {
                if (ct.MaBanh.HasValue)
                {
                    var banh = await _db.SanPhamBanhs.FindAsync(ct.MaBanh.Value);
                    if (banh != null)
                    {
                        banh.TongTonKho += ct.SoLuong;
                        soBanhHoanKho += ct.SoLuong;
                    }
                }
                else if (ct.MaHopTc.HasValue)
                {
                    var hopTc = await _db.HopQuaTuChons
                        .Include(h => h.ChiTietHopTuChons)
                        .FirstOrDefaultAsync(h => h.MaHopTc == ct.MaHopTc.Value);

                    if (hopTc != null)
                    {
                        var voHop = await _db.MauVoHops.FindAsync(hopTc.MaVh);
                        if (voHop != null)
                        {
                            voHop.SltonKhoVo += ct.SoLuong;
                            soVoHopHoanKho += ct.SoLuong;
                        }

                        foreach (var cth in hopTc.ChiTietHopTuChons)
                        {
                            var banh = await _db.SanPhamBanhs.FindAsync(cth.MaBanh);
                            if (banh != null)
                            {
                                int slCong = ct.SoLuong;
                                banh.TongTonKho += slCong;
                                soBanhHoanKho += slCong;
                            }
                        }
                    }
                }
                else if (ct.MaCb.HasValue)
                {
                    var combo = await _db.ComboMauCoSans
                        .Include(c => c.ChiTietComboMaus)
                        .FirstOrDefaultAsync(c => c.MaCb == ct.MaCb.Value);

                    if (combo != null)
                    {
                        var voHop = await _db.MauVoHops.FindAsync(combo.MaVh);
                        if (voHop != null)
                        {
                            voHop.SltonKhoVo += ct.SoLuong;
                            soVoHopHoanKho += ct.SoLuong;
                        }

                        foreach (var ctc in combo.ChiTietComboMaus)
                        {
                            var banh = await _db.SanPhamBanhs.FindAsync(ctc.MaBanh);
                            if (banh != null)
                            {
                                int slCong = ctc.SoLuong * ct.SoLuong;
                                banh.TongTonKho += slCong;
                                soBanhHoanKho += slCong;
                            }
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();

            // 3. Lưu thông tin Ngân hàng hoàn tiền & lý do hủy để đồng bộ sang Sổ Quỹ
            string nguoiThucHien = User.Identity?.Name ?? "Thu ngân";
            _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, meta =>
            {
                meta.MaNganHangHoan = maNganHangHoan?.Trim();
                meta.SoTaiKhoanHoan = soTaiKhoanHoan?.Trim();
                meta.TenChuTkHoan = tenChuTkHoan?.Trim().ToUpperInvariant();
                meta.LyDoHuyHoan = string.IsNullOrWhiteSpace(lyDoHuy) ? "Khách yêu cầu hủy đơn" : lyDoHuy.Trim();
                meta.ThoiGianCapNhatCuoi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                meta.NguoiCapNhatCuoi = nguoiThucHien;
            });

            if (canHoanTien)
            {
                TempData["ThongBaoAdmin"] = $"🗑️ Đã hủy đơn #{donHang.MaTraCuuDh}, hoàn kho ({soBanhHoanKho} bánh, {soVoHopHoanKho} vỏ hộp) và ghi nhận Phiếu Chi Hoàn Tiền {donHang.TongThanhToanCuoi:N0} đ tới {maNganHangHoan} - STK: {soTaiKhoanHoan} ({tenChuTkHoan}) vào Sổ Quỹ!";
            }
            else
            {
                TempData["ThongBaoAdmin"] = $"🗑️ Đã hủy đơn #{donHang.MaTraCuuDh} (hủy {donHang.DotGiaoHangDaDiems.Count} phiếu giao, hoàn kho {soBanhHoanKho} bánh & {soVoHopHoanKho} vỏ hộp) thành công!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
