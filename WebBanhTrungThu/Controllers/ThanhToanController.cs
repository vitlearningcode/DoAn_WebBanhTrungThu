using System.Text;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IDichVuKhuyenMaiGioHang _dichVuKhuyenMai;
        private readonly IDichVuBaoMatCaptcha _dichVuBaoMatCaptcha;

        public ThanhToanController(
            CSDLBanhTrungThuContext db,
            IDichVuThanhToan dichVuThanhToan,
            IDichVuKhuyenMaiGioHang dichVuKhuyenMai,
            IDichVuBaoMatCaptcha dichVuBaoMatCaptcha) : base(db)
        {
            _dichVuThanhToan = dichVuThanhToan;
            _dichVuKhuyenMai = dichVuKhuyenMai;
            _dichVuBaoMatCaptcha = dichVuBaoMatCaptcha;
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

            await _dichVuKhuyenMai.DamBaoMaKhuyenMaiMauConHanAsync();

            var chiPhi = await _dichVuThanhToan.TinhToanChiPhiDonHangAsync(
                gioHang: gioHang,
                maNd: maNd,
                luuCapNhatGiaGioHang: true);

            DateTime bayGio = DateTime.Now;
            var dsVoucherGoiY = await _db.MaKhuyenMais
                .AsNoTracking()
                .Where(k => k.NgayBatDau <= bayGio && k.NgayKetThuc >= bayGio && k.SldaDung < k.SltoiDa)
                .OrderBy(k => k.DonToiThieu)
                .ThenByDescending(k => k.GiaTriGiam)
                .Take(4)
                .ToListAsync();

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
                DanhSachVoucherGoiY = dsVoucherGoiY,
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
        // 2. SINH MÃ CAPTCHA HÌNH ẢNH SVG, VOUCHER CHECKOUT & XÁC THỰC KHO FEFO
        // =========================================================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AnhCaptcha()
        {
            string svg = _dichVuThanhToan.TaoChuoiSvgCaptchaThuan(out string maCaptcha);

            HttpContext.Session.SetString("CaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("MaCaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("DaXacThucCaptcha", "false");

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml; charset=utf-8");
        }

        [HttpGet]
        [AllowAnonymous]
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
        [AllowAnonymous]
        public async Task<IActionResult> ApDungMaKhuyenMai([FromBody] ApDungVoucherRequest? req, [FromQuery] string? codeKm)
        {
            var gioHang = await LayGioHangHienTaiAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return Json(new { thanhCong = false, thongBao = "Giỏ quà đang trống, vui lòng chọn bánh trước khi áp mã!" });
            }

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            var ketQua = await _dichVuKhuyenMai.ApDungVoucherChoGioHangAsync(gioHang, maNd, req, codeKm);
            return Json(ketQua);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GoMaKhuyenMai()
        {
            var gioHang = await LayGioHangHienTaiAsync();
            if (gioHang != null && !string.IsNullOrWhiteSpace(gioHang.CodeKmdangApDung))
            {
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return Json(new
            {
                thanhCong = true,
                thongBao = "Đã gỡ mã khuyến mãi.",
                codeKmDangDung = (string?)null,
                tienGiamVoucher = 0m
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> XacThucCaptchaVaKiemTraKho([FromBody] XacThucCaptchaMoRongRequest? req, string? maCaptchaNhap = null)
        {
            string maKhachNhap = (!string.IsNullOrWhiteSpace(maCaptchaNhap)
                ? maCaptchaNhap
                : (!string.IsNullOrWhiteSpace(req?.MaCaptchaNhap)
                    ? req.MaCaptchaNhap
                    : (!string.IsNullOrWhiteSpace(req?.MaCaptcha)
                        ? req.MaCaptcha
                        : (Request.Form.ContainsKey("maCaptchaNhap") ? Request.Form["maCaptchaNhap"].FirstOrDefault() : null))))?.Trim() ?? string.Empty;

            if (!_dichVuBaoMatCaptcha.KiemTraCaptcha(HttpContext.Session, maKhachNhap))
            {
                string anhMoi = _dichVuBaoMatCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session);

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

            string maNhap = (!string.IsNullOrWhiteSpace(maCaptchaNhap)
                ? maCaptchaNhap
                : (!string.IsNullOrWhiteSpace(MaCaptcha)
                    ? MaCaptcha
                    : (Request.Form["maCaptchaNhap"].FirstOrDefault() ?? Request.Form["MaCaptcha"].FirstOrDefault() ?? string.Empty))).Trim();

            if (!_dichVuBaoMatCaptcha.KiemTraCaptcha(HttpContext.Session, maNhap))
            {
                _dichVuBaoMatCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session);
                TempData["LoiThanhToan"] = "Mã xác thực Captcha không chính xác hoặc chưa được nhập. Vui lòng nhập lại mã hiển thị trong hình!";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(req.HoTenNguoiMua) || string.IsNullOrWhiteSpace(req.SdtNguoiMua))
            {
                _dichVuBaoMatCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session);
                TempData["LoiThanhToan"] = "Vui lòng nhập đầy đủ Họ tên và Số điện thoại người mua!";
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

            bool suDungDiem = req.SuDungDiemTichLuy
                || req.DungDiemTichLuy
                || string.Equals(Request.Form["SuDungDiemTichLuy"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Request.Form["DungDiemTichLuy"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase);
            req.SuDungDiemTichLuy = suDungDiem;
            req.DungDiemTichLuy = suDungDiem;

            var ketQuaDat = await _dichVuThanhToan.XuLyDatHangAsync(
                gioHang: gioHang,
                req: req,
                maNd: maNd,
                maPhienKvl: maPhienKvl,
                hinhThucNhanHang: hinhThucNhanHang,
                tocDoGiao: tocDoGiao);

            if (!ketQuaDat.ThanhCong || ketQuaDat.DonHangDaTao == null)
            {
                if (!string.IsNullOrWhiteSpace(ketQuaDat.TenSanPhamHetHang))
                {
                    TempData["TenBanhHetHang"] = ketQuaDat.TenSanPhamHetHang;
                }
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

            if (trangThaiDieuHuong == "DA_THANH_TOAN" || trangThaiDieuHuong == "DA_HUY_HET_HAN")
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
                daHetHan = donHang.TrangThaiTt == "HET_HAN_HUY" || donHang.TrangThaiTt == "HET_HAN_15P" || donHang.TrangThaiDh == "DA_HUY",
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
                .Include(d => d.DotGiaoHangDaDiems)
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

            DateTime hanTt = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);
            if (DateTime.Now < hanTt.AddSeconds(-2) && donHang.TrangThaiDh != "DA_HUY")
            {
                return Json(new
                {
                    thanhCong = false,
                    daThanhToan = false,
                    thongBao = "Đơn hàng vẫn đang trong thời hạn thanh toán 15 phút."
                });
            }

            await _dichVuThanhToan.KiemTraVaHuyDonQrQuaHanAsync(donHang.MaDh);
            if (donHang.TrangThaiDh != "DA_HUY")
            {
                await _dichVuThanhToan.HoanTraTonKhoRollbackAsync(donHang);
            }

            return Json(new
            {
                thanhCong = true,
                trangThaiTt = donHang.TrangThaiTt,
                trangThaiDh = donHang.TrangThaiDh,
                thongBao = "Đơn hàng đã tự động hủy do hết hạn thanh toán 15 phút. Sản phẩm và điểm đã dùng đã được hoàn trả."
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
        public bool CoCauHinhVietQrNganHang => !string.IsNullOrWhiteSpace(UrlVietQr);
        public Services.CauHinhThanhToanQr CauHinhQr { get; set; } = new();
        public DateTime HanThanhToan { get; set; }
        public int SoGiayConLai { get; set; }
    }
}
