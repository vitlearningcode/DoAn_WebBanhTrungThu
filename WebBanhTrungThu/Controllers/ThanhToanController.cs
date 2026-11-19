using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    /// <summary>
    /// Controller điều phối luồng Thanh toán, Captcha hình ảnh, Đặt hàng đa điểm,
    /// Cổng thanh toán QR MoMo 15 phút và Webhook tự động (Kế thừa ControllerKhachHangCoSo).
    /// </summary>
    public class ThanhToanController : ControllerKhachHangCoSo
    {
        private readonly IDichVuThanhToan _dichVuThanhToan;

        public ThanhToanController(CSDLBanhTrungThuContext db, IDichVuThanhToan dichVuThanhToan) : base(db)
        {
            _dichVuThanhToan = dichVuThanhToan;
        }

        private async Task<GioHang?> LayGioHangHienTaiAsync()
        {
            var (maNd, maPhienKvl) = LayThongTinPhienHienTai();
            return await _dichVuThanhToan.LayGioHangDayDuAsync(maNd, maPhienKvl);
        }

        // =========================================================================
        // 1. TRANG THANH TOÁN KÉP (GUEST & MEMBER, B2C/B2B, GIAO ĐA ĐIỂM & THIỆP QR)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var gioHang = await LayGioHangHienTaiAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            var chiPhi = await _dichVuThanhToan.TinhToanChiPhiDonHangAsync(
                gioHang: gioHang,
                maNd: maNd,
                luuCapNhatGiaGioHang: true);

            var ketQuaKho = await _dichVuThanhToan.KiemTraTonKhoGioHangAsync(gioHang);
            if (!ketQuaKho.HopLe && !string.IsNullOrWhiteSpace(ketQuaKho.TenSanPhamThieu))
            {
                ViewBag.TenBanhHetHangKhiTaiTrang = ketQuaKho.TenSanPhamThieu;
                ViewBag.ThongBaoHetHangKhiTaiTrang = ketQuaKho.ThongBaoChiTiet;
            }

            var vm = new TrangThanhToanViewModel
            {
                GioHang = gioHang,
                DanhSachMon = chiPhi.DanhSachMon,
                TongSoHop = chiPhi.TongSoHop,
                TongTienHang = chiPhi.TongTienHang,
                PhanTramCkB2B = chiPhi.PhanTramCkB2B,
                TienChietKhauB2B = chiPhi.TienChietKhauB2B,
                MienPhiInLogo = chiPhi.MienPhiInLogo,
                PhiInLogoMacDinh = chiPhi.PhiInLogoMacDinh,
                TienGiamVoucher = chiPhi.TienGiamVoucher,
                CodeKmDangDung = chiPhi.CodeKmDangDung,
                ThongTinVoucherGoiY = chiPhi.ThongTinVoucherGoiY,
                DanhSachCanhBaoTonKho = ketQuaKho.DanhSachCanhBao,
                DiemTichLuy = chiPhi.DiemTichLuyHienCo,
                HangTv = chiPhi.HangTv,
                PhanTramGiamVip = chiPhi.PhanTramGiamVip,
                TienGiamVip = chiPhi.TienGiamVip,
                DaDangNhap = maNd.HasValue,
                MaPhienHienTai = maNd.HasValue ? $"MEMBER #{maNd.Value}" : (maPhienKvl ?? "GUEST")
            };

            if (maNd.HasValue)
            {
                var nd = await _db.NguoiDungs
                    .Include(u => u.SoDiaChis)
                    .FirstOrDefaultAsync(u => u.MaNd == maNd.Value);

                if (nd != null)
                {
                    vm.HoTenNguoiMua = nd.HoTen;
                    vm.SdtNguoiMua = nd.Sdt;
                    vm.EmailNguoiMua = nd.Email;
                    vm.DanhSachSoDiaChi = nd.SoDiaChis.OrderByDescending(d => d.LaMacDinh).ToList();
                }
            }

            return View(vm);
        }

        // =========================================================================
        // 2. SINH MÃ CAPTCHA HÌNH ẢNH SVG & XÁC THỰC CAPTCHA + KIỂM TRA KHO FEFO
        // =========================================================================
        [HttpGet]
        public IActionResult LayMaCaptchaMoi()
        {
            string anhSvgBase64 = _dichVuThanhToan.TaoAnhSvgCaptcha(out string maCaptcha);

            HttpContext.Session.SetString("CaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("MaCaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("DaXacThucCaptcha", "false");

            return Json(new
            {
                thanhCong = true,
                anhCaptchaSvg = anhSvgBase64,
                thoiGianSinh = DateTime.Now.ToString("HH:mm:ss")
            });
        }

        [HttpPost]
        public async Task<IActionResult> XacThucCaptchaVaKiemTraKho([FromBody] XacThucCaptchaMoRongRequest? req, string? maCaptchaNhap = null)
        {
            string? maTrongSession = HttpContext.Session.GetString("CaptchaThanhToan")
                                  ?? HttpContext.Session.GetString("MaCaptchaThanhToan");

            string maKhachNhap = (!string.IsNullOrWhiteSpace(maCaptchaNhap)
                ? maCaptchaNhap
                : (!string.IsNullOrWhiteSpace(req?.MaCaptchaNhap)
                    ? req.MaCaptchaNhap
                    : (!string.IsNullOrWhiteSpace(req?.MaCaptcha)
                        ? req.MaCaptcha
                        : (Request.Form.ContainsKey("maCaptchaNhap") ? Request.Form["maCaptchaNhap"].FirstOrDefault() : null))))?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(maKhachNhap)
                || string.IsNullOrWhiteSpace(maTrongSession)
                || !string.Equals(maKhachNhap, maTrongSession, StringComparison.OrdinalIgnoreCase))
            {
                string anhMoi = _dichVuThanhToan.TaoAnhSvgCaptcha(out string maMoi);
                HttpContext.Session.SetString("CaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("MaCaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("DaXacThucCaptcha", "false");

                return Json(new
                {
                    thanhCong = false,
                    loaiLoi = "CAPTCHA",
                    thongBao = "Mã xác thực Captcha không chính xác. Vui lòng nhập lại mã hiển thị trong hình!",
                    anhCaptchaSvg = anhMoi
                });
            }

            var gioHang = await LayGioHangHienTaiAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return Json(new
                {
                    thanhCong = false,
                    loaiLoi = "GIO_HANG_TRONG",
                    thongBao = "Giỏ quà của Quý khách hiện đang trống."
                });
            }

            var ketQuaKho = await _dichVuThanhToan.KiemTraTonKhoGioHangAsync(gioHang);
            if (!ketQuaKho.HopLe)
            {
                HttpContext.Session.SetString("DaXacThucCaptcha", "false");
                return Json(new
                {
                    thanhCong = false,
                    loaiLoi = "HET_HANG",
                    tenBanh = ketQuaKho.TenSanPhamThieu ?? "Bánh Trung Thu",
                    thongBao = ketQuaKho.ThongBaoChiTiet,
                    danhSachCanhBao = ketQuaKho.DanhSachCanhBao
                });
            }

            HttpContext.Session.SetString("DaXacThucCaptcha", "true");
            return Json(new
            {
                thanhCong = true,
                thongBao = "✓ Đã xác thực & kiểm tra còn hàng"
            });
        }

        // =========================================================================
        // 3. ĐẶT HÀNG (ĐIỀU PHỐI SANG DỊCH VỤ THANH TOÁN TRONG TRANSACTION ACID)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(TaoDonHangRequest req, string? maCaptchaNhap = null, string? MaCaptcha = null)
        {
            var gioHang = await LayGioHangHienTaiAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            string? maTrongSession = HttpContext.Session.GetString("CaptchaThanhToan")
                                  ?? HttpContext.Session.GetString("MaCaptchaThanhToan");
            string maNhap = (!string.IsNullOrWhiteSpace(maCaptchaNhap)
                ? maCaptchaNhap
                : (!string.IsNullOrWhiteSpace(MaCaptcha)
                    ? MaCaptcha
                    : (Request.Form["maCaptchaNhap"].FirstOrDefault() ?? Request.Form["MaCaptcha"].FirstOrDefault() ?? string.Empty))).Trim();

            if (string.IsNullOrWhiteSpace(maNhap)
                || string.IsNullOrWhiteSpace(maTrongSession)
                || !string.Equals(maNhap, maTrongSession, StringComparison.OrdinalIgnoreCase))
            {
                _dichVuThanhToan.TaoAnhSvgCaptcha(out string maMoi);
                HttpContext.Session.SetString("CaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("MaCaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("DaXacThucCaptcha", "false");

                TempData["LoiThanhToan"] = "Mã xác thực Captcha không chính xác. Vui lòng nhập lại mã hiển thị trong hình!";
                return RedirectToAction(nameof(Index));
            }

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = maNd.HasValue ? null : ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            string hinhThucNhanHang = !string.IsNullOrWhiteSpace(req.HinhThucNhanHang)
                ? req.HinhThucNhanHang
                : (Request.Form["HinhThucNhanHang"].FirstOrDefault() ?? "GIAO_TAN_NOI");

            string tocDoGiao = !string.IsNullOrWhiteSpace(req.TocDoGiaoHang)
                ? req.TocDoGiaoHang
                : (Request.Form["TocDoGiaoHang"].FirstOrDefault() ?? "GIAO_TIEU_CHUAN");

            var ketQuaDat = await _dichVuThanhToan.XuLyDatHangAsync(
                gioHang: gioHang,
                req: req,
                maNd: maNd,
                maPhienKvl: maPhienKvl,
                hinhThucNhanHang: hinhThucNhanHang,
                tocDoGiao: tocDoGiao);

            if (!ketQuaDat.ThanhCong || ketQuaDat.DonHangDaTao == null)
            {
                TempData["TenBanhHetHang"] = ketQuaDat.TenSanPhamHetHang ?? "Bánh Trung Thu";
                TempData["LoiThanhToan"] = ketQuaDat.ThongBaoLoi;
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.Remove("CaptchaThanhToan");
            HttpContext.Session.Remove("MaCaptchaThanhToan");
            HttpContext.Session.Remove("DaXacThucCaptcha");

            if (ketQuaDat.LaThanhToanQrMoMo)
            {
                return RedirectToAction("CongThanhToanMoMo", new { id = ketQuaDat.DonHangDaTao.MaTraCuuDh });
            }

            return RedirectToAction("ThanhCong", new { id = ketQuaDat.DonHangDaTao.MaTraCuuDh });
        }

        // =========================================================================
        // 4. CỔNG THANH TOÁN MOMO ĐỘC LẬP & TRANG ĐẶT HÀNG THÀNH CÔNG
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> CongThanhToanMoMo(string? id, string? maDon)
        {
            string maTraCuu = !string.IsNullOrWhiteSpace(maDon) ? maDon.Trim() : (id?.Trim() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return RedirectToAction("Index", "Home");
            }

            var (trangThaiDieuHuong, vm) = await _dichVuThanhToan.XayDungDuLieuCongMoMoAsync(maTraCuu);

            if (trangThaiDieuHuong == "DA_THANH_TOAN")
            {
                return RedirectToAction("ThanhCong", new { id = maTraCuu });
            }

            if (trangThaiDieuHuong != "HIEN_CONG_MOMO" || vm == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ThanhCong(string? id, string? maDon)
        {
            string maTraCuu = !string.IsNullOrWhiteSpace(maDon) ? maDon.Trim() : (id?.Trim() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return RedirectToAction("Index", "Home");
            }

            var vm = await _dichVuThanhToan.XayDungDuLieuDonHangThanhCongAsync(
                maTraCuu: maTraCuu,
                scheme: Request.Scheme,
                host: Request.Host.ToString());

            if (vm == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(vm);
        }

        // =========================================================================
        // 5. KIỂM TRA TRẠNG THÁI ĐƠN, HỦY ĐƠN HOÀN KHO 15 PHÚT & WEBHOOK MOMO
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> KiemTraTrangThaiDon(string maTraCuu)
        {
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return Json(new { thanhCong = false });
            }

            var (donHang, soGiayConLai) = await _dichVuThanhToan.KiemTraVaCapNhatTrangThaiDonAsync(maTraCuu);
            if (donHang == null)
            {
                return Json(new { thanhCong = false });
            }

            return Json(new
            {
                thanhCong = true,
                maTraCuu = donHang.MaTraCuuDh,
                trangThaiTt = donHang.TrangThaiTt,
                trangThaiDh = donHang.TrangThaiDh,
                daThanhToan = donHang.TrangThaiTt == "DA_THANH_TOAN" || donHang.TrangThaiTt == "DA_TT",
                daHetHan = donHang.TrangThaiTt == "HET_HAN_15P" || donHang.TrangThaiDh == "DA_HUY",
                soGiayConLai
            });
        }

        [HttpPost]
        public async Task<IActionResult> HuyDonHetHanVaHoanKho(string maTraCuu)
        {
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return Json(new { thanhCong = false, thongBao = "Không tìm thấy mã đơn hàng." });
            }

            var donHang = await _db.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu.Trim());

            if (donHang == null)
            {
                return Json(new { thanhCong = false, thongBao = "Không tìm thấy đơn hàng." });
            }

            if (donHang.TrangThaiTt == "DA_THANH_TOAN" || donHang.TrangThaiTt == "DA_TT")
            {
                return Json(new
                {
                    thanhCong = false,
                    daThanhToan = true,
                    thongBao = "Đơn hàng đã được thanh toán thành công!"
                });
            }

            await _dichVuThanhToan.HoanTraTonKhoRollbackAsync(donHang);

            return Json(new
            {
                thanhCong = true,
                trangThaiTt = donHang.TrangThaiTt,
                trangThaiDh = donHang.TrangThaiDh,
                thongBao = "Đơn hàng đã hết hạn thanh toán 15 phút. Hệ thống đã hoàn trả sản phẩm về kho"
            });
        }

        [HttpPost("/api/webhook/momo")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> NhanWebhookMoMo([FromBody] MomoWebhookPayload? payload)
        {
            if (payload == null)
            {
                return BadRequest(new { thanhCong = false, thongBao = "Payload Webhook rỗng." });
            }

            var ketQua = await _dichVuThanhToan.XuLyWebhookMoMoAsync(payload);

            if (ketQua.MaTrangThaiHttp == 404)
            {
                return NotFound(new
                {
                    thanhCong = false,
                    thongBao = ketQua.ThongBao
                });
            }

            if (!ketQua.ThanhCong)
            {
                return Ok(new
                {
                    thanhCong = false,
                    thongBao = ketQua.ThongBao
                });
            }

            return Ok(new
            {
                thanhCong = true,
                maTraCuu = ketQua.MaTraCuu,
                trangThaiTt = ketQua.TrangThaiTt,
                trangThaiDh = ketQua.TrangThaiDh,
                soTienDaNhan = ketQua.SoTienDaNhan,
                thongBao = ketQua.ThongBao
            });
        }
    }
}

namespace WebBanhTrungThu.ViewModels
{
    public class XacThucCaptchaMoRongRequest : XacThucCaptchaRequest
    {
        public string? MaCaptchaNhap { get; set; }
    }

    public class CongThanhToanMoMoViewModel
    {
        public Models.Entities.DonHang DonHang { get; set; } = null!;
        public string MaTraCuuDh { get; set; } = string.Empty;
        public decimal TongThanhToan { get; set; }
        public decimal TongThanhToanCuoi { get; set; }
        public string NoiDungChuyenKhoan { get; set; } = string.Empty;
        public string SoDienThoaiMoMo { get; set; } = "0814 585 526";
        public string SoTaiKhoanMoMo { get; set; } = "0814585526";
        public string TenChuTaiKhoan { get; set; } = "LE MINH DUC";
        public string AnhQrThanhToanBase64 { get; set; } = string.Empty;
        public string UrlVietQr { get; set; } = string.Empty;
        public Services.CauHinhThanhToanQr CauHinhQr { get; set; } = new();
        public DateTime HanThanhToan { get; set; }
        public int SoGiayConLai { get; set; }
    }
}
