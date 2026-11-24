using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    /// <summary>
    /// Controller mỏng nhẹ điều phối luồng Thanh toán, Captcha SVG,
    /// Cổng thanh toán QR MoMo 15 phút và Webhook tự động.
    /// </summary>
    public class ThanhToanController : ControllerKhachHangCoSo
    {
        private readonly IDichVuThanhToan _dichVuThanhToan;
        private readonly IDichVuCongThanhToanMoMo _dichVuCongMoMo;
        private readonly IDichVuKhuyenMaiGioHang _dichVuKhuyenMai;
        private readonly IDichVuBaoMatCaptcha _dichVuCaptcha;

        public ThanhToanController(
            CSDLBanhTrungThuContext db,
            IDichVuThanhToan dichVuThanhToan,
            IDichVuCongThanhToanMoMo dichVuCongMoMo,
            IDichVuKhuyenMaiGioHang dichVuKhuyenMai,
            IDichVuBaoMatCaptcha dichVuCaptcha) : base(db)
        {
            _dichVuThanhToan = dichVuThanhToan;
            _dichVuCongMoMo = dichVuCongMoMo;
            _dichVuKhuyenMai = dichVuKhuyenMai;
            _dichVuCaptcha = dichVuCaptcha;
        }

        private string LayMaCaptchaTuRequest(params string?[] nguonUuTien)
        {
            foreach (var item in nguonUuTien)
                if (!string.IsNullOrWhiteSpace(item)) return item.Trim();
            return Request.HasFormContentType
                ? (Request.Form["maCaptchaNhap"].FirstOrDefault() ?? Request.Form["MaCaptcha"].FirstOrDefault() ?? "").Trim()
                : "";
        }

        private static string LayMaTraCuu(string? id, string? maDon)
            => !string.IsNullOrWhiteSpace(maDon) ? maDon.Trim() : (id?.Trim() ?? "");

        private void XoaCaptchaSession()
        {
            HttpContext.Session.Remove("CaptchaThanhToan");
            HttpContext.Session.Remove("MaCaptchaThanhToan");
            HttpContext.Session.Remove("DaXacThucCaptcha");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (maNd, maPhienKvl) = LayThongTinPhienHienTai();
            var (vm, tenBanhHet, thongBaoHet) = await _dichVuThanhToan.XayDungDuLieuTrangThanhToanAsync(maNd, maPhienKvl);
            if (vm == null) return RedirectToAction("Index", "Home");

            if (!string.IsNullOrWhiteSpace(tenBanhHet))
            {
                ViewBag.TenBanhHetHangKhiTaiTrang = tenBanhHet;
                ViewBag.ThongBaoHetHangKhiTaiTrang = thongBaoHet;
            }
            return View(vm);
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AnhCaptcha()
        {
            string svg = _dichVuCaptcha.TaoChuoiSvgCaptchaThuan(out string maCaptcha);
            HttpContext.Session.SetString("CaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("MaCaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("DaXacThucCaptcha", "false");

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml; charset=utf-8");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult LayMaCaptchaMoi()
        {
            string anhSvgBase64 = _dichVuCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session);
            return Json(new { thanhCong = true, anhCaptchaSvg = anhSvgBase64, thoiGianSinh = DateTime.Now.ToString("HH:mm:ss") });
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> ApDungMaKhuyenMai([FromBody] ApDungVoucherRequest? req, [FromQuery] string? codeKm)
        {
            var (maNd, maPhienKvl) = LayThongTinPhienHienTai();
            var gioHang = await _dichVuThanhToan.LayGioHangDayDuAsync(maNd, maPhienKvl);
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
                return Json(new { thanhCong = false, thongBao = "Giỏ quà đang trống, vui lòng chọn bánh trước khi áp mã!" });

            return Json(await _dichVuKhuyenMai.ApDungVoucherChoGioHangAsync(gioHang, maNd, req, codeKm));
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> GoMaKhuyenMai()
        {
            var (maNd, maPhienKvl) = LayThongTinPhienHienTai();
            await _dichVuThanhToan.GoMaKhuyenMaiGioHangAsync(maNd, maPhienKvl);
            return Json(new { thanhCong = true, thongBao = "Đã gỡ mã khuyến mãi.", codeKmDangDung = (string?)null, tienGiamVoucher = 0m });
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> XacThucCaptchaVaKiemTraKho([FromBody] XacThucCaptchaMoRongRequest? req, string? maCaptchaNhap = null)
        {
            string maKhachNhap = LayMaCaptchaTuRequest(maCaptchaNhap, req?.MaCaptchaNhap, req?.MaCaptcha);
            if (!string.IsNullOrWhiteSpace(maKhachNhap) && !_dichVuCaptcha.KiemTraCaptcha(HttpContext.Session, maKhachNhap))
            {
                return Json(new
                {
                    thanhCong = false, loaiLoi = "CAPTCHA",
                    thongBao = "Mã xác thực Captcha không chính xác. Vui lòng nhập lại mã hiển thị trong hình!",
                    anhCaptchaSvg = _dichVuCaptcha.CapMoiCaptchaVaoSession(HttpContext.Session)
                });
            }

            var (maNd, maPhienKvl) = LayThongTinPhienHienTai();
            var gioHang = await _dichVuThanhToan.LayGioHangDayDuAsync(maNd, maPhienKvl);
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
                return Json(new { thanhCong = false, loaiLoi = "GIO_HANG_TRONG", thongBao = "Giỏ quà của Quý khách hiện đang trống." });

            var kqKho = await _dichVuThanhToan.KiemTraTonKhoGioHangAsync(gioHang);
            if (!kqKho.HopLe)
            {
                HttpContext.Session.SetString("DaXacThucCaptcha", "false");
                return Json(new
                {
                    thanhCong = false, loaiLoi = "HET_HANG",
                    tenBanh = kqKho.TenSanPhamThieu ?? "Bánh Trung Thu",
                    thongBao = kqKho.ThongBaoChiTiet, danhSachCanhBao = kqKho.DanhSachCanhBao
                });
            }

            HttpContext.Session.SetString("DaXacThucCaptcha", "true");
            return Json(new { thanhCong = true, thongBao = "✓ Đã kiểm tra còn hàng trong kho" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(TaoDonHangRequest req, string? maCaptchaNhap = null, string? MaCaptcha = null)
        {
            var (maNd, maPhienKvlCookie) = LayThongTinPhienHienTai();
            var gioHang = await _dichVuThanhToan.LayGioHangDayDuAsync(maNd, maPhienKvlCookie);
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any()) return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(req.HoTenNguoiMua) || string.IsNullOrWhiteSpace(req.SdtNguoiMua))
            {
                TempData["LoiThanhToan"] = "Vui lòng nhập đầy đủ Họ tên và Số điện thoại người mua!";
                return RedirectToAction(nameof(Index));
            }

            req.SuDungDiemTichLuy = req.SuDungDiemTichLuy || req.DungDiemTichLuy;
            req.DungDiemTichLuy = req.SuDungDiemTichLuy;

            var kqDat = await _dichVuThanhToan.XuLyDatHangAsync(
                gioHang, req, maNd, maNd.HasValue ? null : maPhienKvlCookie,
                string.IsNullOrWhiteSpace(req.HinhThucNhanHang) ? "GIAO_TAN_NOI" : req.HinhThucNhanHang,
                string.IsNullOrWhiteSpace(req.TocDoGiaoHang) ? "GIAO_TIEU_CHUAN" : req.TocDoGiaoHang);

            if (!kqDat.ThanhCong || kqDat.DonHangDaTao == null)
            {
                if (!string.IsNullOrWhiteSpace(kqDat.TenSanPhamHetHang)) TempData["TenBanhHetHang"] = kqDat.TenSanPhamHetHang;
                TempData["LoiThanhToan"] = kqDat.ThongBaoLoi;
                return RedirectToAction(nameof(Index));
            }

            XoaCaptchaSession();
            return RedirectToAction(kqDat.LaThanhToanQrMoMo ? nameof(CongThanhToanMoMo) : nameof(ThanhCong), new { id = kqDat.DonHangDaTao.MaTraCuuDh });
        }

        [HttpGet]
        public async Task<IActionResult> CongThanhToanMoMo(string? id, string? maDon)
        {
            string maTraCuu = LayMaTraCuu(id, maDon);
            if (string.IsNullOrWhiteSpace(maTraCuu)) return RedirectToAction("Index", "Home");

            var (trangThaiDieuHuong, vm) = await _dichVuCongMoMo.XayDungDuLieuCongMoMoAsync(maTraCuu);
            if (trangThaiDieuHuong is "DA_THANH_TOAN" or "DA_HUY_HET_HAN") return RedirectToAction(nameof(ThanhCong), new { id = maTraCuu });
            return (trangThaiDieuHuong == "HIEN_CONG_MOMO" && vm != null) ? View(vm) : RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ThanhCong(string? id, string? maDon)
        {
            string maTraCuu = LayMaTraCuu(id, maDon);
            if (string.IsNullOrWhiteSpace(maTraCuu)) return RedirectToAction("Index", "Home");

            var vm = await _dichVuThanhToan.XayDungDuLieuDonHangThanhCongAsync(maTraCuu, Request.Scheme, Request.Host.ToString());
            return vm != null ? View(vm) : RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> KiemTraTrangThaiDon(string maTraCuu)
        {
            if (string.IsNullOrWhiteSpace(maTraCuu)) return Json(new { thanhCong = false });
            var (dh, soGiayConLai) = await _dichVuCongMoMo.KiemTraVaCapNhatTrangThaiDonAsync(maTraCuu);
            if (dh == null) return Json(new { thanhCong = false });

            return Json(new
            {
                thanhCong = true, maTraCuu = dh.MaTraCuuDh,
                trangThaiTt = dh.TrangThaiTt, trangThaiDh = dh.TrangThaiDh,
                daThanhToan = dh.TrangThaiTt is "DA_THANH_TOAN" or "DA_TT",
                daHetHan = dh.TrangThaiDh == "DA_HUY" || dh.TrangThaiTt is "HET_HAN_HUY" or "HET_HAN_15P",
                soGiayConLai
            });
        }

        [HttpPost]
        public async Task<IActionResult> HuyDonHetHanVaHoanKho(string maTraCuu)
        {
            var kq = await _dichVuCongMoMo.HuyDonHetHanTheoMaTraCuuAsync(maTraCuu);
            if (!kq.ThanhCong) return Json(new { thanhCong = false, daThanhToan = kq.DaThanhToan, thongBao = kq.ThongBao });
            return Json(new { thanhCong = true, trangThaiTt = kq.TrangThaiTt, trangThaiDh = kq.TrangThaiDh, thongBao = kq.ThongBao });
        }

        [HttpPost("/api/webhook/momo"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> NhanWebhookMoMo([FromBody] MomoWebhookPayload? payload)
        {
            if (payload == null) return BadRequest(new { thanhCong = false, thongBao = "Payload Webhook rỗng." });
            var kq = await _dichVuCongMoMo.XuLyWebhookMoMoAsync(payload);
            if (kq.MaTrangThaiHttp == 404) return NotFound(new { thanhCong = false, thongBao = kq.ThongBao });
            if (!kq.ThanhCong) return Ok(new { thanhCong = false, thongBao = kq.ThongBao });

            return Ok(new
            {
                thanhCong = true, maTraCuu = kq.MaTraCuu, trangThaiTt = kq.TrangThaiTt,
                trangThaiDh = kq.TrangThaiDh, soTienDaNhan = kq.SoTienDaNhan, thongBao = kq.ThongBao
            });
        }
    }
}
