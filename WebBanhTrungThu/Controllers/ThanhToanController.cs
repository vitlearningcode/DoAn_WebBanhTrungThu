using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuKiotVietMetadata _kiotMeta;

        public ThanhToanController(CSDLBanhTrungThuContext db, DichVuKiotVietMetadata kiotMeta)
        {
            _db = db;
            _kiotMeta = kiotMeta;
        }

        private async Task<GioHang?> LayGioHangDayDuAsync()
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            IQueryable<GioHang> query = _db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.MaBanhNavigation)
                        .ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.MaCbNavigation)
                        .ThenInclude(cb => cb!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.MaCbNavigation)
                        .ThenInclude(cb => cb!.ChiTietComboMaus)
                            .ThenInclude(ctcb => ctcb.MaBanhNavigation)
                                .ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.MaHopTcNavigation)
                        .ThenInclude(h => h!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.MaHopTcNavigation)
                        .ThenInclude(h => h!.ChiTietHopTuChons)
                            .ThenInclude(cth => cth.MaBanhNavigation)
                                .ThenInclude(b => b!.LoSanXuatBanhs);

            if (maNd.HasValue)
            {
                return await query.FirstOrDefaultAsync(g => g.MaNd == maNd.Value);
            }

            if (!string.IsNullOrWhiteSpace(maPhienKvl))
            {
                return await query.FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl);
            }

            return null;
        }

        /// <summary>
        /// Tính lại 100% đơn giá của từng dòng giỏ hàng trực tiếp từ SQL Server (Anti-Tampering).
        /// </summary>
        private static decimal TinhDonGiaThucTeTuCsdl(ChiTietGioHang ct)
        {
            if (ct.LoaiSanPham == 1 && ct.MaBanhNavigation != null)
            {
                return Math.Round(ct.MaBanhNavigation.DonGiaThucTe, 0);
            }
            if (ct.LoaiSanPham == 2 && ct.MaCbNavigation != null)
            {
                return Math.Round(ct.MaCbNavigation.GiaUuDaiCombo, 0);
            }
            if (ct.LoaiSanPham == 3 && ct.MaHopTcNavigation != null)
            {
                var hop = ct.MaHopTcNavigation;
                decimal giaVoHopUuDai = hop.MaVhNavigation != null
                    ? Math.Round(hop.MaVhNavigation.GiaVoHopRieng * 0.95m, 0)
                    : 0m;
                decimal tongGiaBanh = hop.ChiTietHopTuChons.Sum(x =>
                {
                    decimal giaBanh = x.MaBanhNavigation != null
                        ? x.MaBanhNavigation.DonGiaThucTe
                        : x.DonGiaBanhLucChon;
                    x.DonGiaBanhLucChon = giaBanh;
                    return giaBanh;
                });
                decimal tongDonGiaHop = Math.Round(giaVoHopUuDai + tongGiaBanh, 0);
                hop.TongGiaMotHop = tongDonGiaHop;
                return tongDonGiaHop;
            }
            return Math.Round(ct.DonGiaTamTinh, 0);
        }

        /// <summary>
        /// Kiểm tra xem Tỉnh/Thành phố hoặc địa chỉ có thuộc vùng hỗ trợ Giao hỏa tốc 6h (TP. Hồ Chí Minh / An Giang - Long Xuyên) hay không.
        /// </summary>
        private static bool KiemTraVungHoTroHoaToc(string? tinhThanh, string? diaChiChiTiet)
        {
            string gop = $"{tinhThanh} {diaChiChiTiet}".Trim();
            if (string.IsNullOrWhiteSpace(gop)) return false;

            return gop.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase)
                || gop.Contains("TP.HCM", StringComparison.OrdinalIgnoreCase)
                || gop.Contains("TPHCM", StringComparison.OrdinalIgnoreCase)
                || gop.Contains("An Giang", StringComparison.OrdinalIgnoreCase)
                || gop.Contains("Long Xuyên", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra tồn kho thực tế từ bảng LoSanXuatBanh (NgayHetHan > DateTime.Today && TrangThaiLo == DANG_BAN/XA_CAN_DATE) và MauVoHop.
        /// </summary>
        private static (bool HopLe, string? TenSanPhamThieu, string? ThongBaoChiTiet, List<string> DanhSachCanhBao) KiemTraTonKhoThucTeGioHang(GioHang gioHang)
        {
            DateOnly homNay = DateOnly.FromDateTime(DateTime.Today);
            var nhuCauBanh = new Dictionary<int, (string TenBanh, int SoLuongCan, int TonKhoThucTe)>();
            var nhuCauVoHop = new Dictionary<int, (string TenVoHop, int SoLuongCan, int TonKhoThucTe)>();
            var danhSachCanhBao = new List<string>();

            static int TinhTonKhoLoConHan(SanPhamBanh banh, DateOnly today)
            {
                if (banh.LoSanXuatBanhs != null && banh.LoSanXuatBanhs.Any())
                {
                    int tonLo = banh.LoSanXuatBanhs
                        .Where(l => l.HanSuDung > today && (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "XA_CAN_DATE") && l.SlconLai > 0)
                        .Sum(l => l.SlconLai);
                    return Math.Min(banh.TongTonKho, tonLo);
                }
                return Math.Max(0, banh.TongTonKho);
            }

            foreach (var ct in gioHang.ChiTietGioHangs)
            {
                if (ct.LoaiSanPham == 1 && ct.MaBanhNavigation != null)
                {
                    var b = ct.MaBanhNavigation;
                    int ton = TinhTonKhoLoConHan(b, homNay);
                    int slCu = nhuCauBanh.ContainsKey(b.MaBanh) ? nhuCauBanh[b.MaBanh].SoLuongCan : 0;
                    nhuCauBanh[b.MaBanh] = (b.TenBanh, slCu + ct.SoLuong, ton);
                }
                else if (ct.LoaiSanPham == 2 && ct.MaCbNavigation != null)
                {
                    if (ct.MaCbNavigation.MaVhNavigation != null)
                    {
                        var vh = ct.MaCbNavigation.MaVhNavigation;
                        int slVoCu = nhuCauVoHop.ContainsKey(vh.MaVh) ? nhuCauVoHop[vh.MaVh].SoLuongCan : 0;
                        nhuCauVoHop[vh.MaVh] = (vh.TenVoHop, slVoCu + ct.SoLuong, Math.Max(0, vh.SltonKhoVo));
                    }
                    foreach (var ctCb in ct.MaCbNavigation.ChiTietComboMaus)
                    {
                        if (ctCb.MaBanhNavigation != null)
                        {
                            var b = ctCb.MaBanhNavigation;
                            int ton = TinhTonKhoLoConHan(b, homNay);
                            int slCu = nhuCauBanh.ContainsKey(b.MaBanh) ? nhuCauBanh[b.MaBanh].SoLuongCan : 0;
                            nhuCauBanh[b.MaBanh] = (b.TenBanh, slCu + (ctCb.SoLuong * ct.SoLuong), ton);
                        }
                    }
                }
                else if (ct.LoaiSanPham == 3 && ct.MaHopTcNavigation != null)
                {
                    if (ct.MaHopTcNavigation.MaVhNavigation != null)
                    {
                        var vh = ct.MaHopTcNavigation.MaVhNavigation;
                        int slVoCu = nhuCauVoHop.ContainsKey(vh.MaVh) ? nhuCauVoHop[vh.MaVh].SoLuongCan : 0;
                        nhuCauVoHop[vh.MaVh] = (vh.TenVoHop, slVoCu + ct.SoLuong, Math.Max(0, vh.SltonKhoVo));
                    }
                    foreach (var ctHop in ct.MaHopTcNavigation.ChiTietHopTuChons)
                    {
                        if (ctHop.MaBanhNavigation != null)
                        {
                            var b = ctHop.MaBanhNavigation;
                            int ton = TinhTonKhoLoConHan(b, homNay);
                            int slCu = nhuCauBanh.ContainsKey(b.MaBanh) ? nhuCauBanh[b.MaBanh].SoLuongCan : 0;
                            nhuCauBanh[b.MaBanh] = (b.TenBanh, slCu + ct.SoLuong, ton);
                        }
                    }
                }
            }

            string? tenDauTien = null;
            string? thongBaoDauTien = null;

            foreach (var itemBanh in nhuCauBanh.Values)
            {
                if (itemBanh.TonKhoThucTe < itemBanh.SoLuongCan)
                {
                    string msg = $"Rất tiếc! Sản phẩm {itemBanh.TenBanh} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho (cần {itemBanh.SoLuongCan}, kho còn {itemBanh.TonKhoThucTe}). Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn vị bánh khác.";
                    danhSachCanhBao.Add(msg);
                    tenDauTien ??= itemBanh.TenBanh;
                    thongBaoDauTien ??= $"Rất tiếc! Sản phẩm {itemBanh.TenBanh} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho. Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn vị bánh khác.";
                }
            }

            foreach (var itemVo in nhuCauVoHop.Values)
            {
                if (itemVo.TonKhoThucTe < itemVo.SoLuongCan)
                {
                    string msg = $"Rất tiếc! Mẫu vỏ hộp {itemVo.TenVoHop} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho (cần {itemVo.SoLuongCan}, kho còn {itemVo.TonKhoThucTe}). Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn mẫu hộp khác.";
                    danhSachCanhBao.Add(msg);
                    tenDauTien ??= itemVo.TenVoHop;
                    thongBaoDauTien ??= $"Rất tiếc! Sản phẩm {itemVo.TenVoHop} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho. Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn vị bánh khác.";
                }
            }

            return (!danhSachCanhBao.Any(), tenDauTien, thongBaoDauTien, danhSachCanhBao);
        }

        // =========================================================================
        // 1. TRANG THANH TOÁN KÉP (GUEST & MEMBER, B2C/B2B, GIAO ĐA ĐIỂM & THIỆP QR)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var gioHang = await LayGioHangDayDuAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            // BẢO MẬT GIÁ CẢ 100% TỪ CSDL (ANTI-TAMPERING)
            bool coCapNhatGia = false;
            foreach (var ct in gioHang.ChiTietGioHangs)
            {
                decimal giaChuan = TinhDonGiaThucTeTuCsdl(ct);
                if (ct.DonGiaTamTinh != giaChuan)
                {
                    ct.DonGiaTamTinh = giaChuan;
                    coCapNhatGia = true;
                }
            }
            if (coCapNhatGia)
            {
                await _db.SaveChangesAsync();
            }

            var danhSachMon = gioHang.ChiTietGioHangs.OrderByDescending(c => c.NgayThem).ToList();
            decimal tongTienHang = danhSachMon.Sum(c => c.SoLuong * c.DonGiaTamTinh);

            // Tính tổng số hộp quà (Combo mẫu LoaiSanPham == 2 hoặc Hộp tự chọn LoaiSanPham == 3)
            int tongSoHop = danhSachMon
                .Where(c => c.LoaiSanPham == 2 || c.LoaiSanPham == 3)
                .Sum(c => c.SoLuong);

            // Đối chiếu bảng chiết khấu sỉ B2B từ SQL Server
            var bangB2B = await _db.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
            var mocB2B = bangB2B.FirstOrDefault(b => tongSoHop >= b.SlhopToiThieu && tongSoHop <= b.SlhopToiDa)
                         ?? (tongSoHop > 0 && bangB2B.Any() && tongSoHop > bangB2B.Last().SlhopToiDa ? bangB2B.Last() : null);

            decimal phanTramCkB2B = mocB2B?.PhanTramChietKhau ?? 0m;
            decimal tienChietKhauB2B = phanTramCkB2B > 0 ? Math.Round(tongTienHang * (phanTramCkB2B / 100m), 0) : 0m;
            bool mienPhiInLogo = mocB2B?.MienPhiInLogo ?? false;
            decimal phiInLogoMacDinh = mocB2B?.PhiKhuonInLogo ?? (bangB2B.FirstOrDefault()?.PhiKhuonInLogo ?? 500000m);

            // Lấy thông tin mã gợi ý TRANGTRON2026 từ CSDL
            var kmGoiY = await _db.MaKhuyenMais.AsNoTracking().FirstOrDefaultAsync(k => k.CodeKm == "TRANGTRON2026");
            string thongTinVoucherGoiY = kmGoiY != null
                ? $"HSD: {kmGoiY.NgayKetThuc:dd/MM/yyyy} • Đơn từ {(kmGoiY.DonToiThieu / 1000m):N0}k"
                : "HSD: 31/12/2027 • Đơn từ 200k";

            // Tính lại giảm giá Voucher nếu có (Kiểm tra đủ 4 điều kiện trong CSDL)
            decimal tienGiamVoucher = 0m;
            string? codeKmDangDung = gioHang.CodeKmdangApDung;
            if (!string.IsNullOrWhiteSpace(codeKmDangDung))
            {
                var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k => k.CodeKm == codeKmDangDung);
                DateTime bayGio = DateTime.Now;

                if (km != null
                    && km.TrangThaiKichHoat
                    && bayGio >= km.NgayBatDau && bayGio <= km.NgayKetThuc
                    && km.SoLuotDaDung < km.SoLuotToiDa
                    && (maNd.HasValue || km.ChoPhepKhachVangLai)
                    && tongTienHang >= km.GiaTriDonToiThieu)
                {
                    if (km.LoaiGiam == "PHAN_TRAM")
                    {
                        tienGiamVoucher = Math.Round(tongTienHang * (km.GiaTriGiam / 100m), 0);
                        if (km.GiamToiDa > 0 && tienGiamVoucher > km.GiamToiDa)
                            tienGiamVoucher = km.GiamToiDa;
                    }
                    else
                    {
                        tienGiamVoucher = km.GiaTriGiam;
                        if (km.GiamToiDa > 0 && tienGiamVoucher > km.GiamToiDa)
                            tienGiamVoucher = km.GiamToiDa;
                    }
                }
                else
                {
                    codeKmDangDung = null;
                    gioHang.CodeKmdangApDung = null;
                    await _db.SaveChangesAsync();
                }
            }

            // Lớp 2: Kiểm tra tồn kho thực tế từ CSDL ngay khi tải trang /ThanhToan/Index
            var ketQuaKho = KiemTraTonKhoThucTeGioHang(gioHang);
            if (!ketQuaKho.HopLe && !string.IsNullOrWhiteSpace(ketQuaKho.TenSanPhamThieu))
            {
                ViewBag.TenBanhHetHangKhiTaiTrang = ketQuaKho.TenSanPhamThieu;
                ViewBag.ThongBaoHetHangKhiTaiTrang = ketQuaKho.ThongBaoChiTiet;
            }

            var vm = new TrangThanhToanViewModel
            {
                GioHang = gioHang,
                DanhSachMon = danhSachMon,
                TongSoHop = tongSoHop,
                TongTienHang = tongTienHang,
                PhanTramCkB2B = phanTramCkB2B,
                TienChietKhauB2B = tienChietKhauB2B,
                MienPhiInLogo = mienPhiInLogo,
                PhiInLogoMacDinh = phiInLogoMacDinh,
                TienGiamVoucher = tienGiamVoucher,
                CodeKmDangDung = codeKmDangDung,
                ThongTinVoucherGoiY = thongTinVoucherGoiY,
                DanhSachCanhBaoTonKho = ketQuaKho.DanhSachCanhBao,
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
        // 1B. SINH MÃ CAPTCHA HÌNH ẢNH NHIỄU CHỐNG BOT & KIỂM TRA TỒN KHO LỚP 3 (TIMING GATE)
        // =========================================================================
        private static string SinhChuoiCaptchaNgauNhien()
        {
            // Bỏ các ký tự dễ nhầm như 0, O, 1, I
            const string kyTuChoPhep = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] mangKyTu = new char[5];
            for (int i = 0; i < mangKyTu.Length; i++)
            {
                mangKyTu[i] = kyTuChoPhep[Random.Shared.Next(kyTuChoPhep.Length)];
            }
            return new string(mangKyTu);
        }

        private static string TaoAnhSvgCaptchaNhieu(string maCaptcha)
        {
            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='135' height='44' viewBox='0 0 135 44'>");
            sb.Append("<rect width='135' height='44' rx='6' fill='#F9FAFB'/>");

            string[] bangMauNhieu = { "#9CA3AF", "#D1D5DB", "#E5E7EB", "#7A1215", "#B45309", "#6B7280" };

            // 5 đường cong nhiễu ngẫu nhiên (Bezier noise lines)
            for (int i = 0; i < 5; i++)
            {
                int x1 = Random.Shared.Next(2, 22);
                int y1 = Random.Shared.Next(4, 40);
                int cx1 = Random.Shared.Next(28, 68);
                int cy1 = Random.Shared.Next(2, 42);
                int cx2 = Random.Shared.Next(68, 108);
                int cy2 = Random.Shared.Next(2, 42);
                int x2 = Random.Shared.Next(112, 133);
                int y2 = Random.Shared.Next(4, 40);
                string mauLine = bangMauNhieu[Random.Shared.Next(bangMauNhieu.Length)];
                sb.Append($"<path d='M{x1} {y1} C {cx1} {cy1}, {cx2} {cy2}, {x2} {y2}' stroke='{mauLine}' stroke-width='1.35' fill='none' opacity='0.65'/>");
            }

            // 24 chấm nhiễu ngẫu nhiên (Noise dots)
            for (int i = 0; i < 24; i++)
            {
                int cx = Random.Shared.Next(4, 131);
                int cy = Random.Shared.Next(4, 40);
                int r = Random.Shared.Next(1, 3);
                string mauDot = bangMauNhieu[Random.Shared.Next(bangMauNhieu.Length)];
                sb.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='{mauDot}' opacity='0.55'/>");
            }

            // Vẽ từng ký tự trong 5 ký tự với góc xoay nghiêng (-18deg .. +18deg) và lệch trục Y khác nhau
            string[] bangMauChu = { "#111827", "#7A1215", "#1F2937", "#374151", "#4A0709" };
            for (int i = 0; i < maCaptcha.Length; i++)
            {
                int x = 19 + (i * 24) + Random.Shared.Next(-2, 3);
                int y = 28 + Random.Shared.Next(-5, 6);
                int gocXoay = Random.Shared.Next(-18, 19);
                string mauChu = bangMauChu[i % bangMauChu.Length];
                sb.Append($"<text x='{x}' y='{y}' transform='rotate({gocXoay} {x} {y})' font-family='Consolas, Courier New, monospace' font-size='21' font-weight='800' fill='{mauChu}' text-anchor='middle'>{maCaptcha[i]}</text>");
            }

            sb.Append("</svg>");
            return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        [HttpGet]
        public IActionResult LayMaCaptchaMoi()
        {
            string maCaptcha = SinhChuoiCaptchaNgauNhien();

            HttpContext.Session.SetString("CaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("MaCaptchaThanhToan", maCaptcha);
            HttpContext.Session.SetString("DaXacThucCaptcha", "false");

            string anhSvgBase64 = TaoAnhSvgCaptchaNhieu(maCaptcha);

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

            // BẮT BUỘC kiểm tra chuỗi maCaptchaNhap người dùng gõ vào phải trùng khớp 100% (không phân biệt hoa/thường) với Session["CaptchaThanhToan"]
            if (string.IsNullOrWhiteSpace(maKhachNhap)
                || string.IsNullOrWhiteSpace(maTrongSession)
                || !string.Equals(maKhachNhap, maTrongSession, StringComparison.OrdinalIgnoreCase))
            {
                string maMoi = SinhChuoiCaptchaNgauNhien();
                HttpContext.Session.SetString("CaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("MaCaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("DaXacThucCaptcha", "false");

                return Json(new
                {
                    thanhCong = false,
                    loaiLoi = "CAPTCHA",
                    thongBao = "Mã xác thực Captcha không chính xác. Vui lòng nhập lại mã hiển thị trong hình!",
                    anhCaptchaSvg = TaoAnhSvgCaptchaNhieu(maMoi)
                });
            }

            var gioHang = await LayGioHangDayDuAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return Json(new
                {
                    thanhCong = false,
                    loaiLoi = "GIO_HANG_TRONG",
                    thongBao = "Giỏ quà của Quý khách hiện đang trống."
                });
            }

            var ketQuaKho = KiemTraTonKhoThucTeGioHang(gioHang);
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
        // 2. XỬ LÝ ĐẶT HÀNG (ANTI-TAMPERING, ĐA ĐIỂM NHẬN, THIỆP QR 3D, FEFO & MOMO QR)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(TaoDonHangRequest req, string? maCaptchaNhap = null, string? MaCaptcha = null)
        {
            var gioHang = await LayGioHangDayDuAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // BẮT BUỘC kiểm tra chuỗi maCaptchaNhap khớp 100% với Session["CaptchaThanhToan"]
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
                string maMoi = SinhChuoiCaptchaNgauNhien();
                HttpContext.Session.SetString("CaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("MaCaptchaThanhToan", maMoi);
                HttpContext.Session.SetString("DaXacThucCaptcha", "false");

                TempData["LoiThanhToan"] = "Mã xác thực Captcha không chính xác. Vui lòng nhập lại mã hiển thị trong hình!";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra chặt chẽ tồn kho thực tế từ LoSanXuatBanh và MauVoHop trước khi tạo đơn
            var ketQuaKho = KiemTraTonKhoThucTeGioHang(gioHang);
            if (!ketQuaKho.HopLe)
            {
                TempData["TenBanhHetHang"] = ketQuaKho.TenSanPhamThieu ?? "Bánh Trung Thu";
                TempData["LoiThanhToan"] = ketQuaKho.ThongBaoChiTiet;
                return RedirectToAction(nameof(Index));
            }

            if (req.DanhSachDotGiao == null || !req.DanhSachDotGiao.Any())
            {
                req.DanhSachDotGiao = new List<DotGiaoHangInput>
                {
                    new DotGiaoHangInput
                    {
                        TenNguoiNhan = req.HoTenNguoiMua,
                        SdtNguoiNhan = req.SdtNguoiMua,
                        DiaChiGiaoChiTiet = "Nhận tại Cửa hàng Nguyệt Cát",
                        NgayHenGiao = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                        KhungGioGiao = "Sáng (08:00 - 11:30)",
                        CoKemThiepQr = req.CoThiep3D
                    }
                };
            }

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = maNd.HasValue ? null : ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Sinh mã tra cứu đơn hàng duy nhất: TT2026-XXXX
                string maTraCuu;
                do
                {
                    maTraCuu = "TT2026-" + Random.Shared.Next(1000, 9999);
                } while (await _db.DonHangs.AnyAsync(d => d.MaTraCuuDh == maTraCuu));

                // 2. BẢO MẬT GIÁ CẢ 100% TỪ CSDL (ANTI-TAMPERING)
                var danhSachMon = gioHang.ChiTietGioHangs.ToList();
                foreach (var ct in danhSachMon)
                {
                    ct.DonGiaTamTinh = TinhDonGiaThucTeTuCsdl(ct);
                }

                decimal tongTienHang = danhSachMon.Sum(c => c.SoLuong * c.DonGiaTamTinh);
                int tongSoHop = danhSachMon.Where(c => c.LoaiSanPham == 2 || c.LoaiSanPham == 3).Sum(c => c.SoLuong);

                var bangB2B = await _db.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
                var mocB2B = bangB2B.FirstOrDefault(b => tongSoHop >= b.SlhopToiThieu && tongSoHop <= b.SlhopToiDa)
                             ?? (tongSoHop > 0 && bangB2B.Any() && tongSoHop > bangB2B.Last().SlhopToiDa ? bangB2B.Last() : null);

                decimal phanTramCkB2B = mocB2B?.PhanTramChietKhau ?? 0m;
                decimal tienChietKhauB2B = phanTramCkB2B > 0 ? Math.Round(tongTienHang * (phanTramCkB2B / 100m), 0) : 0m;
                bool mienPhiInLogo = mocB2B?.MienPhiInLogo ?? false;
                decimal phiKhuonMacDinh = mocB2B?.PhiKhuonInLogo ?? (bangB2B.FirstOrDefault()?.PhiKhuonInLogo ?? 500000m);

                decimal phiInLogo = (req.LaDonDoanhNghiepB2B && req.YeuCauInLogo && !mienPhiInLogo)
                    ? phiKhuonMacDinh
                    : 0m;

                // Kiểm tra đủ 4 điều kiện Voucher từ CSDL
                decimal tienGiamVoucher = 0m;
                if (!string.IsNullOrWhiteSpace(gioHang.CodeKmdangApDung))
                {
                    var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k => k.CodeKm == gioHang.CodeKmdangApDung);
                    DateTime bayGio = DateTime.Now;

                    if (km != null
                        && km.TrangThaiKichHoat
                        && bayGio >= km.NgayBatDau && bayGio <= km.NgayKetThuc
                        && km.SoLuotDaDung < km.SoLuotToiDa
                        && (maNd.HasValue || km.ChoPhepKhachVangLai)
                        && tongTienHang >= km.GiaTriDonToiThieu)
                    {
                        if (km.LoaiGiam == "PHAN_TRAM")
                        {
                            tienGiamVoucher = Math.Round(tongTienHang * (km.GiaTriGiam / 100m), 0);
                            if (km.GiamToiDa > 0 && tienGiamVoucher > km.GiamToiDa)
                                tienGiamVoucher = km.GiamToiDa;
                        }
                        else
                        {
                            tienGiamVoucher = km.GiaTriGiam;
                            if (km.GiamToiDa > 0 && tienGiamVoucher > km.GiamToiDa)
                                tienGiamVoucher = km.GiamToiDa;
                        }
                        km.SldaDung += 1;
                    }
                }

                // 3. TÍNH PHÍ VẬN CHUYỂN ĐỘNG THEO CÔNG THỨC CHUẨN (MỐC FREESHIP 500.000Đ & VÙNG HỎA TỐC)
                string hinhThucNhanHang = !string.IsNullOrWhiteSpace(req.HinhThucNhanHang)
                    ? req.HinhThucNhanHang
                    : (Request.Form["HinhThucNhanHang"].FirstOrDefault() ?? "GIAO_TAN_NOI");

                string tocDoGiao = !string.IsNullOrWhiteSpace(req.TocDoGiaoHang)
                    ? req.TocDoGiaoHang
                    : (Request.Form["TocDoGiaoHang"].FirstOrDefault() ?? "GIAO_TIEU_CHUAN");

                bool laNhanTaiCuaHang = string.Equals(hinhThucNhanHang, "NHAN_TAI_CUA_HANG", StringComparison.OrdinalIgnoreCase)
                    || req.DanhSachDotGiao.Any(d => !string.IsNullOrWhiteSpace(d.DiaChiGiaoChiTiet) &&
                        (d.DiaChiGiaoChiTiet.Contains("[Nhận tại cửa hàng]", StringComparison.OrdinalIgnoreCase) ||
                         d.DiaChiGiaoChiTiet.Contains("Nhận tại Cửa hàng", StringComparison.OrdinalIgnoreCase)));

                if (laNhanTaiCuaHang && req.DanhSachDotGiao.Count > 1)
                {
                    req.DanhSachDotGiao = req.DanhSachDotGiao.Take(1).ToList();
                }

                decimal tienHangSauGiam = Math.Max(0m, tongTienHang - tienChietKhauB2B - tienGiamVoucher);
                bool duMocFreeship500k = tienHangSauGiam >= 500000m;

                bool yeuCauHoaToc = !laNhanTaiCuaHang && (
                    string.Equals(tocDoGiao, "GIAO_HOA_TOC", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tocDoGiao, "HOA_TOC_6H", StringComparison.OrdinalIgnoreCase));

                // Kiểm tra địa bàn hoạt động Hỏa tốc (chỉ TP. Hồ Chí Minh và An Giang / TP. Long Xuyên)
                bool hopLeVungHoaToc = yeuCauHoaToc && req.DanhSachDotGiao.All(d =>
                    KiemTraVungHoTroHoaToc(d.TinhThanhPho, d.DiaChiGiaoChiTiet));

                decimal phiMoiDotGiao;
                if (laNhanTaiCuaHang)
                {
                    phiMoiDotGiao = 0m;
                }
                else if (hopLeVungHoaToc)
                {
                    // Giao hỏa tốc: >= 500k là 30.000đ/điểm (hỗ trợ 50%), < 500k là 60.000đ/điểm
                    phiMoiDotGiao = duMocFreeship500k ? 30000m : 60000m;
                }
                else
                {
                    // Giao tiêu chuẩn: >= 500k là 0đ (Miễn phí), < 500k là 30.000đ/điểm
                    phiMoiDotGiao = duMocFreeship500k ? 0m : 30000m;
                }

                int soDiemGiao = Math.Max(1, req.DanhSachDotGiao.Count);
                decimal tongPhiVanChuyen = laNhanTaiCuaHang ? 0m : (soDiemGiao * phiMoiDotGiao);
                decimal tongThanhToanCuoi = Math.Max(0m, tienHangSauGiam + phiInLogo + tongPhiVanChuyen);

                string phuongThucTt = string.IsNullOrWhiteSpace(req.PhuongThucTt) ? "COD" : req.PhuongThucTt.Trim().ToUpperInvariant();
                bool laThanhToanQrMoMo = phuongThucTt == "MOMO_QR" || phuongThucTt == "VIETQR" || phuongThucTt == "MOMO";

                // 4. Lưu bản ghi DonHang
                var donHang = new DonHang
                {
                    MaTraCuuDh = maTraCuu,
                    MaNd = maNd,
                    MaPhienKvl = maPhienKvl,
                    HoTenNguoiMua = string.IsNullOrWhiteSpace(req.HoTenNguoiMua) ? "Khách Hàng Nguyệt Cát" : req.HoTenNguoiMua.Trim(),
                    SdtnguoiMua = string.IsNullOrWhiteSpace(req.SdtNguoiMua) ? "0909000000" : req.SdtNguoiMua.Trim(),
                    EmailNguoiMua = string.IsNullOrWhiteSpace(req.EmailNguoiMua) ? null : req.EmailNguoiMua.Trim(),
                    LaDonDoanhNghiepB2b = req.LaDonDoanhNghiepB2B,
                    TenCongTy = req.LaDonDoanhNghiepB2B ? req.TenCongTy?.Trim() : null,
                    MaSoThue = req.LaDonDoanhNghiepB2B ? req.MaSoThue?.Trim() : null,
                    TongTienHang = tongTienHang,
                    PhanTramCkB2b = phanTramCkB2B,
                    TienChietKhauB2b = tienChietKhauB2B,
                    TienGiamVoucher = tienGiamVoucher,
                    PhiInLogo = phiInLogo,
                    TongPhiVanChuyen = tongPhiVanChuyen,
                    TongThanhToanCuoi = tongThanhToanCuoi,
                    PhuongThucTt = phuongThucTt,
                    TrangThaiTt = laThanhToanQrMoMo ? "CHO_THANH_TOAN" : "CHUA_TT",
                    TrangThaiDh = "CHO_XAC_NHAN",
                    NgayDat = DateTime.Now
                };

                _db.DonHangs.Add(donHang);
                await _db.SaveChangesAsync();

                // 5. Lưu danh sách DotGiaoHangDaDiems (CHỈ tạo Thiệp 3D khi khách chủ động tích chọn CoThiep3D / CoKemThiepQr == true)
                var cacDotDaLuu = new List<DotGiaoHangDaDiem>();
                var mapThiepMeta = new Dictionary<string, ThongTinThiep3DMeta>(StringComparer.OrdinalIgnoreCase);

                foreach (var dotInput in req.DanhSachDotGiao)
                {
                    bool coTaoThiep3D = req.CoThiep3D || dotInput.CoKemThiepQr;
                    string? tokenQr = null;
                    string? loiChucLuu = null;
                    string? mauGiaoDienLuu = null;

                    if (coTaoThiep3D)
                    {
                        tokenQr = "QR-" + Guid.NewGuid().ToString("N").ToUpperInvariant()[..12];
                        loiChucLuu = string.IsNullOrWhiteSpace(dotInput.LoiChucThiep)
                            ? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!"
                            : dotInput.LoiChucThiep.Trim();
                        mauGiaoDienLuu = string.IsNullOrWhiteSpace(dotInput.MauGiaoDienThiep)
                            ? "CO_PHONG_HOANG_GIA"
                            : dotInput.MauGiaoDienThiep.Trim();

                        string kinhGuiTrenThiep = !string.IsNullOrWhiteSpace(dotInput.TenNguoiNhanThiep)
                            ? dotInput.TenNguoiNhanThiep.Trim()
                            : (!string.IsNullOrWhiteSpace(dotInput.TenNguoiNhan) ? dotInput.TenNguoiNhan.Trim() : "Quý Khách & Gia Đình");

                        string nguoiGuiTrenThiep = !string.IsNullOrWhiteSpace(dotInput.TenNguoiGuiThiep)
                            ? dotInput.TenNguoiGuiThiep.Trim()
                            : donHang.HoTenNguoiMua;

                        mapThiepMeta[tokenQr] = new ThongTinThiep3DMeta
                        {
                            MaTokenThiepQr = tokenQr,
                            TenNguoiNhanThiep = kinhGuiTrenThiep,
                            TenNguoiGuiThiep = nguoiGuiTrenThiep,
                            LoiChucThiep = loiChucLuu,
                            MauGiaoDienThiep = mauGiaoDienLuu
                        };
                    }

                    bool dotNhanTaiCuaHang = laNhanTaiCuaHang || (!string.IsNullOrWhiteSpace(dotInput.DiaChiGiaoChiTiet) &&
                        (dotInput.DiaChiGiaoChiTiet.Contains("[Nhận tại cửa hàng]", StringComparison.OrdinalIgnoreCase) ||
                         dotInput.DiaChiGiaoChiTiet.Contains("Nhận tại Cửa hàng", StringComparison.OrdinalIgnoreCase)));

                    decimal phiVanChuyenDot = dotNhanTaiCuaHang ? 0m : phiMoiDotGiao;

                    var dotEntity = new DotGiaoHangDaDiem
                    {
                        MaDh = donHang.MaDh,
                        TenNguoiNhan = string.IsNullOrWhiteSpace(dotInput.TenNguoiNhan) ? donHang.HoTenNguoiMua : dotInput.TenNguoiNhan.Trim(),
                        SdtnguoiNhan = string.IsNullOrWhiteSpace(dotInput.SdtNguoiNhan) ? donHang.SdtnguoiMua : dotInput.SdtNguoiNhan.Trim(),
                        DiaChiGiaoChiTiet = string.IsNullOrWhiteSpace(dotInput.DiaChiGiaoChiTiet) ? "Địa chỉ nhận quà Trung Thu" : dotInput.DiaChiGiaoChiTiet.Trim(),
                        NgayHenGiao = (dotInput.NgayHenGiao.HasValue && dotInput.NgayHenGiao.Value >= DateOnly.FromDateTime(DateTime.Today))
                            ? dotInput.NgayHenGiao.Value
                            : DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                        KhungGioGiao = string.IsNullOrWhiteSpace(dotInput.KhungGioGiao) ? "Sáng (08:00 - 11:30)" : dotInput.KhungGioGiao,
                        PhiShipDotNay = phiVanChuyenDot,
                        TrangThaiGiao = "CHO_DONG_GOI",
                        CoKemThiepQr = coTaoThiep3D,
                        MaTokenThiepQr = tokenQr,
                        LoiChucThiep = loiChucLuu,
                        MauGiaoDienThiep = mauGiaoDienLuu,
                        SoLanMoThiep = 0
                    };

                    _db.DotGiaoHangDaDiems.Add(dotEntity);
                    cacDotDaLuu.Add(dotEntity);
                }
                await _db.SaveChangesAsync();

                int? maDotDauTien = cacDotDaLuu.FirstOrDefault()?.MaDotGh;

                // 6. Lưu ChiTietDonHangs & Tạm trừ tồn kho theo chuẩn FEFO (LoSanXuatBanh + SanPhamBanh + MauVoHop)
                DateOnly homNay = DateOnly.FromDateTime(DateTime.Today);
                var danhSachLoDaTru = new List<NhatKyTruLoFefo>();
                var danhSachVoHopDaTru = new List<NhatKyTruVoHop>();

                void TruTonKhoBanhTheoFefo(SanPhamBanh banh, int soLuongCanTru)
                {
                    if (soLuongCanTru <= 0) return;

                    banh.TongTonKho = Math.Max(0, banh.TongTonKho - soLuongCanTru);

                    int conPhaiTru = soLuongCanTru;
                    var cacLoKhaDung = (banh.LoSanXuatBanhs ?? Enumerable.Empty<LoSanXuatBanh>())
                        .Where(l => l.HanSuDung > homNay && (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "XA_CAN_DATE") && l.SlconLai > 0)
                        .OrderBy(l => l.HanSuDung)
                        .ThenBy(l => l.MaLo)
                        .ToList();

                    foreach (var lo in cacLoKhaDung)
                    {
                        if (conPhaiTru <= 0) break;
                        int slTruLoNay = Math.Min(lo.SlconLai, conPhaiTru);
                        if (slTruLoNay > 0)
                        {
                            lo.SlconLai -= slTruLoNay;
                            conPhaiTru -= slTruLoNay;

                            var daCo = danhSachLoDaTru.FirstOrDefault(x => x.MaLo == lo.MaLo);
                            if (daCo != null)
                            {
                                daCo.SoLuongDaTru += slTruLoNay;
                            }
                            else
                            {
                                danhSachLoDaTru.Add(new NhatKyTruLoFefo
                                {
                                    MaLo = lo.MaLo,
                                    MaBanh = banh.MaBanh,
                                    SoLuongDaTru = slTruLoNay
                                });
                            }
                        }
                    }
                }

                void TruTonKhoVoHop(MauVoHop voHop, int soLuongCanTru)
                {
                    if (soLuongCanTru <= 0) return;
                    voHop.SltonKhoVo = Math.Max(0, voHop.SltonKhoVo - soLuongCanTru);

                    var daCo = danhSachVoHopDaTru.FirstOrDefault(x => x.MaVh == voHop.MaVh);
                    if (daCo != null)
                    {
                        daCo.SoLuongDaTru += soLuongCanTru;
                    }
                    else
                    {
                        danhSachVoHopDaTru.Add(new NhatKyTruVoHop
                        {
                            MaVh = voHop.MaVh,
                            SoLuongDaTru = soLuongCanTru
                        });
                    }
                }

                for (int idx = 0; idx < danhSachMon.Count; idx++)
                {
                    var ctGio = danhSachMon[idx];
                    int? maDotGan = cacDotDaLuu.Any()
                        ? cacDotDaLuu[idx % cacDotDaLuu.Count].MaDotGh
                        : maDotDauTien;

                    string tenLuuCung = "Sản phẩm Nguyệt Cát";
                    string? cauHinhJson = null;

                    if (ctGio.LoaiSanPham == 1 && ctGio.MaBanhNavigation != null)
                    {
                        tenLuuCung = ctGio.MaBanhNavigation.TenBanh;
                        TruTonKhoBanhTheoFefo(ctGio.MaBanhNavigation, ctGio.SoLuong);
                    }
                    else if (ctGio.LoaiSanPham == 2 && ctGio.MaCbNavigation != null)
                    {
                        tenLuuCung = ctGio.MaCbNavigation.TenCombo;
                        if (ctGio.MaCbNavigation.MaVhNavigation != null)
                        {
                            TruTonKhoVoHop(ctGio.MaCbNavigation.MaVhNavigation, ctGio.SoLuong);
                        }
                        foreach (var ctCombo in ctGio.MaCbNavigation.ChiTietComboMaus)
                        {
                            if (ctCombo.MaBanhNavigation != null)
                            {
                                int slTru = ctCombo.SoLuong * ctGio.SoLuong;
                                TruTonKhoBanhTheoFefo(ctCombo.MaBanhNavigation, slTru);
                            }
                        }
                    }
                    else if (ctGio.LoaiSanPham == 3 && ctGio.MaHopTcNavigation != null)
                    {
                        var hopTc = ctGio.MaHopTcNavigation;
                        tenLuuCung = $"{hopTc.TenHopKhachDat} ({hopTc.MaBamCauHinh})";

                        if (hopTc.MaVhNavigation != null)
                        {
                            TruTonKhoVoHop(hopTc.MaVhNavigation, ctGio.SoLuong);
                        }

                        var dsBanhTrongHop = hopTc.ChiTietHopTuChons
                            .OrderBy(x => x.ViTriOtrongHop)
                            .Select(x => new
                            {
                                ViTriOTrongHop = x.ViTriOtrongHop,
                                MaBanh = x.MaBanh,
                                TenBanh = x.MaBanhNavigation?.TenBanh ?? "Bánh Trung Thu",
                                DonGia = x.DonGiaBanhLucChon
                            })
                            .ToList();

                        cauHinhJson = JsonSerializer.Serialize(dsBanhTrongHop, new JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });

                        foreach (var ctHop in hopTc.ChiTietHopTuChons)
                        {
                            if (ctHop.MaBanhNavigation != null)
                            {
                                TruTonKhoBanhTheoFefo(ctHop.MaBanhNavigation, ctGio.SoLuong);
                            }
                        }
                    }

                    var ctdh = new ChiTietDonHang
                    {
                        MaDh = donHang.MaDh,
                        MaDotGh = maDotGan,
                        LoaiSanPham = ctGio.LoaiSanPham,
                        MaBanh = ctGio.MaBanh,
                        MaCb = ctGio.MaCb,
                        MaHopTc = ctGio.MaHopTc,
                        TenSanPhamLuuCung = tenLuuCung,
                        CauHinhHopJson = cauHinhJson,
                        SoLuong = ctGio.SoLuong,
                        DonGiaLucMua = ctGio.DonGiaTamTinh,
                        ThanhTien = ctGio.SoLuong * ctGio.DonGiaTamTinh
                    };

                    _db.ChiTietDonHangs.Add(ctdh);
                }

                // 7. Cộng điểm tích lũy cho Member (10.000đ = 1 điểm)
                if (maNd.HasValue)
                {
                    var nd = await _db.NguoiDungs.FindAsync(maNd.Value);
                    if (nd != null)
                    {
                        int diemCong = (int)(tongThanhToanCuoi / 10000m);
                        nd.DiemTichLuy += diemCong;
                        if (nd.DiemTichLuy >= 1500) nd.HangTv = "Kim Cương";
                        else if (nd.DiemTichLuy >= 500) nd.HangTv = "Vàng";
                        else if (nd.DiemTichLuy >= 200) nd.HangTv = "Bạc";
                    }
                }

                // 8. Xóa các dòng trong ChiTietGioHangs sau khi đặt thành công
                _db.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = DateTime.Now;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // 9. Lưu Metadata KiotViet (Hạn thanh toán 15 phút, Nhật ký trừ lô FEFO để Rollback, và Thông tin Thiệp 3D)
                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, meta =>
                {
                    meta.KenhBan = "WEB_ONLINE";
                    meta.HanThanhToan = laThanhToanQrMoMo ? donHang.NgayDat.AddMinutes(15) : null;
                    meta.DaHoanKhoDoHetHan = false;
                    meta.DanhSachLoDaTru = danhSachLoDaTru;
                    meta.DanhSachVoHopDaTru = danhSachVoHopDaTru;
                    meta.Thiep3DTheoToken = mapThiepMeta;
                });

                HttpContext.Session.Remove("CaptchaThanhToan");
                HttpContext.Session.Remove("MaCaptchaThanhToan");
                HttpContext.Session.Remove("DaXacThucCaptcha");

                if (laThanhToanQrMoMo)
                {
                    return RedirectToAction("CongThanhToanMoMo", new { id = donHang.MaTraCuuDh });
                }

                return RedirectToAction("ThanhCong", new { id = donHang.MaTraCuuDh });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =========================================================================
        // 3A. CỔNG THANH TOÁN MOMO ĐỘC LẬP (/ThanhToan/CongThanhToanMoMo/{maDon})
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> CongThanhToanMoMo(string? id, string? maDon)
        {
            string maTraCuu = !string.IsNullOrWhiteSpace(maDon) ? maDon.Trim() : (id?.Trim() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return RedirectToAction("Index", "Home");
            }

            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu);

            if (donHang == null)
            {
                return RedirectToAction("Index", "Home");
            }

            _kiotMeta.GanMetadataChoDonHang(donHang);

            if (donHang.TrangThaiTt == "DA_THANH_TOAN" || donHang.TrangThaiTt == "DA_TT")
            {
                return RedirectToAction("ThanhCong", new { id = donHang.MaTraCuuDh });
            }

            DateTime hanThanhToan = donHang.HanThanhToan ?? donHang.NgayDat.AddMinutes(15);
            donHang.HanThanhToan = hanThanhToan;

            if ((donHang.TrangThaiTt == "CHO_THANH_TOAN" || donHang.TrangThaiTt == "CHO_XAC_MINH_CK") && DateTime.Now >= hanThanhToan)
            {
                await ThucHienRollbackKhoDonHetHanAsync(donHang);
                return RedirectToAction("Index", "Home");
            }

            if (donHang.TrangThaiTt == "HET_HAN_15P" || donHang.TrangThaiDh == "DA_HUY")
            {
                return RedirectToAction("Index", "Home");
            }

            int soGiayConLai = Math.Max(0, (int)Math.Ceiling((hanThanhToan - DateTime.Now).TotalSeconds));

            var cauHinhQr = _kiotMeta.LayCauHinhQr();
            string sdtMoMoRaw = string.IsNullOrWhiteSpace(cauHinhQr.SoTaiKhoanMoMo) ? "0814585526" : cauHinhQr.SoTaiKhoanMoMo.Trim();
            string sdtMoMoHienThi = string.IsNullOrWhiteSpace(cauHinhQr.SoDienThoaiHienThi) ? "0814 585 526" : cauHinhQr.SoDienThoaiHienThi.Trim();
            string tenChuTk = string.IsNullOrWhiteSpace(cauHinhQr.TenChuTaiKhoan) ? "LE MINH DUC" : cauHinhQr.TenChuTaiKhoan.Trim();

            string noiDungCk = (cauHinhQr.MauNoiDungCk ?? "NGUYETCAT {MaTraCuuDh}")
                .Replace("{MaTraCuuDh}", donHang.MaTraCuuDh, StringComparison.OrdinalIgnoreCase);

            string bin = string.IsNullOrWhiteSpace(cauHinhQr.MaBinVietQr) ? "970422" : cauHinhQr.MaBinVietQr.Trim();
            string stk = Uri.EscapeDataString(sdtMoMoRaw);
            string addInfo = Uri.EscapeDataString(noiDungCk);
            string accName = Uri.EscapeDataString(tenChuTk);
            long soTienNguyen = (long)Math.Round(donHang.TongThanhToanCuoi, 0);

            string urlVietQr = $"https://img.vietqr.io/image/{bin}-{stk}-compact2.png?amount={soTienNguyen}&addInfo={addInfo}&accountName={accName}";

            using var qrGenerator = new QRCodeGenerator();
            string payloadQrMoMo = $"2|99|{sdtMoMoRaw}|{tenChuTk}||0|0|{soTienNguyen}|{noiDungCk}|transfer_myqr";
            using QRCodeData dataQrTt = qrGenerator.CreateQrCode(payloadQrMoMo, QRCodeGenerator.ECCLevel.Q);
            using var pngQrTt = new PngByteQRCode(dataQrTt);
            string qrThanhToanBase64 = "data:image/png;base64," + Convert.ToBase64String(pngQrTt.GetGraphic(10));

            var vm = new CongThanhToanMoMoViewModel
            {
                DonHang = donHang,
                MaTraCuuDh = donHang.MaTraCuuDh,
                TongThanhToan = donHang.TongThanhToanCuoi,
                TongThanhToanCuoi = donHang.TongThanhToanCuoi,
                NoiDungChuyenKhoan = noiDungCk,
                SoDienThoaiMoMo = sdtMoMoHienThi,
                SoTaiKhoanMoMo = sdtMoMoRaw,
                TenChuTaiKhoan = tenChuTk,
                AnhQrThanhToanBase64 = qrThanhToanBase64,
                UrlVietQr = urlVietQr,
                CauHinhQr = cauHinhQr,
                HanThanhToan = hanThanhToan,
                SoGiayConLai = soGiayConLai
            };

            return View(vm);
        }

        // =========================================================================
        // 3. TRANG ĐẶT HÀNG THÀNH CÔNG, KHUNG QR MOMO 15 PHÚT & MÃ QR THIỆP 3D
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ThanhCong(string? id, string? maDon)
        {
            string maTraCuu = !string.IsNullOrWhiteSpace(maDon) ? maDon.Trim() : (id?.Trim() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return RedirectToAction("Index", "Home");
            }

            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu);

            if (donHang == null)
            {
                return RedirectToAction("Index", "Home");
            }

            _kiotMeta.GanMetadataChoDonHang(donHang);

            bool laDonQrMoMo = donHang.PhuongThucTt == "MOMO_QR"
                            || donHang.PhuongThucTt == "VIETQR"
                            || donHang.PhuongThucTt == "MOMO";

            DateTime hanThanhToan = donHang.HanThanhToan ?? donHang.NgayDat.AddMinutes(15);
            donHang.HanThanhToan = hanThanhToan;

            // Nếu đơn đang CHO_THANH_TOAN mà đã quá 15 phút thì tự động Rollback kho ngay
            if (laDonQrMoMo && (donHang.TrangThaiTt == "CHO_THANH_TOAN" || donHang.TrangThaiTt == "CHO_XAC_MINH_CK") && DateTime.Now >= hanThanhToan)
            {
                await ThucHienRollbackKhoDonHetHanAsync(donHang);
            }

            int soGiayConLai = 0;
            bool dangChoThanhToanQr = laDonQrMoMo && (donHang.TrangThaiTt == "CHO_THANH_TOAN" || donHang.TrangThaiTt == "CHO_XAC_MINH_CK");
            if (dangChoThanhToanQr)
            {
                soGiayConLai = Math.Max(0, (int)Math.Ceiling((hanThanhToan - DateTime.Now).TotalSeconds));
            }

            var cauHinhQr = _kiotMeta.LayCauHinhQr();
            string noiDungCk = (cauHinhQr.MauNoiDungCk ?? "NGUYETCAT {MaTraCuuDh}")
                .Replace("{MaTraCuuDh}", donHang.MaTraCuuDh, StringComparison.OrdinalIgnoreCase);

            // Sinh ảnh QR Thanh toán MoMo / VietQR sắc nét
            string qrThanhToanBase64 = string.Empty;
            string urlVietQr = string.Empty;
            using var qrGenerator = new QRCodeGenerator();

            if (laDonQrMoMo)
            {
                string bin = string.IsNullOrWhiteSpace(cauHinhQr.MaBinVietQr) ? "970422" : cauHinhQr.MaBinVietQr.Trim();
                string stk = Uri.EscapeDataString(cauHinhQr.SoTaiKhoanMoMo.Trim());
                string addInfo = Uri.EscapeDataString(noiDungCk);
                string accName = Uri.EscapeDataString(cauHinhQr.TenChuTaiKhoan.Trim());
                long soTienNguyen = (long)Math.Round(donHang.TongThanhToanCuoi, 0);

                urlVietQr = $"https://img.vietqr.io/image/{bin}-{stk}-compact2.png?amount={soTienNguyen}&addInfo={addInfo}&accountName={accName}";

                string payloadQrMoMo = $"2|99|{cauHinhQr.SoTaiKhoanMoMo.Trim()}|{cauHinhQr.TenChuTaiKhoan.Trim()}||0|0|{soTienNguyen}|{noiDungCk}|transfer_myqr";
                using QRCodeData dataQrTt = qrGenerator.CreateQrCode(payloadQrMoMo, QRCodeGenerator.ECCLevel.Q);
                using var pngQrTt = new PngByteQRCode(dataQrTt);
                qrThanhToanBase64 = "data:image/png;base64," + Convert.ToBase64String(pngQrTt.GetGraphic(10));
            }

            var vm = new DonHangThanhCongViewModel
            {
                DonHang = donHang,
                DiemCongThem = donHang.MaNd.HasValue ? (int)(donHang.TongThanhToanCuoi / 10000m) : 0,
                CauHinhQr = cauHinhQr,
                NoiDungChuyenKhoan = noiDungCk,
                AnhQrThanhToanBase64 = qrThanhToanBase64,
                UrlVietQr = urlVietQr,
                DangChoThanhToanQr = dangChoThanhToanQr,
                HanThanhToan = hanThanhToan,
                SoGiayConLai = soGiayConLai
            };

            // CHỈ sinh QR Thiệp 3D cho các đợt giao thực sự có CoKemThiepQr == true và có MaTokenThiepQr
            foreach (var dot in donHang.DotGiaoHangDaDiems.Where(d => d.CoKemThiepQr && !string.IsNullOrWhiteSpace(d.MaTokenThiepQr)))
            {
                var metaThiep = _kiotMeta.LayThongTinThiepTheoToken(donHang.MaDh, dot.MaTokenThiepQr);
                if (metaThiep != null)
                {
                    dot.TenNguoiNhanThiep = metaThiep.TenNguoiNhanThiep;
                    dot.TenNguoiGuiThiep = metaThiep.TenNguoiGuiThiep;
                }

                string urlThiep = $"{Request.Scheme}://{Request.Host}/Thiep3D/Xem/{dot.MaTokenThiepQr}";
                using QRCodeData qrCodeData = qrGenerator.CreateQrCode(urlThiep, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(10);
                string qrBase64 = "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);

                vm.DanhSachDotGiaoQr.Add(new DotGiaoKemQrItem
                {
                    DotGiao = dot,
                    UrlThiep3D = urlThiep,
                    AnhQrBase64 = qrBase64
                });
            }

            return View(vm);
        }

        // =========================================================================
        // 4. API POLLING KIỂM TRA TRẠNG THÁI ĐƠN & TỰ ĐỘNG ROLLBACK KHO KHI QUÁ 15 PHÚT
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> KiemTraTrangThaiDon(string maTraCuu)
        {
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return Json(new { thanhCong = false });
            }

            var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu.Trim());
            if (donHang == null)
            {
                return Json(new { thanhCong = false });
            }

            _kiotMeta.GanMetadataChoDonHang(donHang);
            DateTime hanTt = donHang.HanThanhToan ?? donHang.NgayDat.AddMinutes(15);

            if ((donHang.TrangThaiTt == "CHO_THANH_TOAN" || donHang.TrangThaiTt == "CHO_XAC_MINH_CK") && DateTime.Now >= hanTt)
            {
                await ThucHienRollbackKhoDonHetHanAsync(donHang);
            }

            int soGiayConLai = Math.Max(0, (int)Math.Ceiling((hanTt - DateTime.Now).TotalSeconds));

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

            await ThucHienRollbackKhoDonHetHanAsync(donHang);

            return Json(new
            {
                thanhCong = true,
                trangThaiTt = donHang.TrangThaiTt,
                trangThaiDh = donHang.TrangThaiDh,
                thongBao = "Đơn hàng đã hết hạn thanh toán 15 phút. Hệ thống đã hoàn trả sản phẩm về kho"
            });
        }

        private async Task ThucHienRollbackKhoDonHetHanAsync(DonHang donHang)
        {
            var meta = _kiotMeta.LayMetadataDonHang(donHang.MaDh);
            if (meta.DaHoanKhoDoHetHan && donHang.TrangThaiTt == "HET_HAN_15P")
            {
                return;
            }

            if (!meta.DaHoanKhoDoHetHan)
            {
                // 1. Hoàn trả chính xác các lô bánh FEFO đã trừ
                if (meta.DanhSachLoDaTru != null && meta.DanhSachLoDaTru.Any())
                {
                    foreach (var nhatKyLo in meta.DanhSachLoDaTru)
                    {
                        var lo = await _db.LoSanXuatBanhs.FirstOrDefaultAsync(l => l.MaLo == nhatKyLo.MaLo);
                        if (lo != null)
                        {
                            lo.SlconLai += nhatKyLo.SoLuongDaTru;
                        }

                        var banh = await _db.SanPhamBanhs.FirstOrDefaultAsync(b => b.MaBanh == nhatKyLo.MaBanh);
                        if (banh != null)
                        {
                            banh.TongTonKho += nhatKyLo.SoLuongDaTru;
                        }
                    }
                }
                else
                {
                    // Fallback nếu đơn cũ không có nhật ký lô chi tiết
                    var chiTiets = await _db.ChiTietDonHangs
                        .Where(c => c.MaDh == donHang.MaDh)
                        .ToListAsync();

                    foreach (var ct in chiTiets)
                    {
                        if (ct.LoaiSanPham == 1 && ct.MaBanh.HasValue)
                        {
                            var banh = await _db.SanPhamBanhs
                                .Include(b => b.LoSanXuatBanhs)
                                .FirstOrDefaultAsync(b => b.MaBanh == ct.MaBanh.Value);
                            if (banh != null)
                            {
                                banh.TongTonKho += ct.SoLuong;
                                var loConHan = banh.LoSanXuatBanhs
                                    .OrderByDescending(l => l.HanSuDung)
                                    .FirstOrDefault();
                                if (loConHan != null) loConHan.SlconLai += ct.SoLuong;
                            }
                        }
                    }
                }

                // 2. Hoàn trả tồn kho vỏ hộp
                if (meta.DanhSachVoHopDaTru != null && meta.DanhSachVoHopDaTru.Any())
                {
                    foreach (var nhatKyVo in meta.DanhSachVoHopDaTru)
                    {
                        var vo = await _db.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == nhatKyVo.MaVh);
                        if (vo != null)
                        {
                            vo.SltonKhoVo += nhatKyVo.SoLuongDaTru;
                        }
                    }
                }
            }

            donHang.TrangThaiDh = "DA_HUY";
            donHang.TrangThaiTt = "HET_HAN_15P";
            await _db.SaveChangesAsync();

            _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, m =>
            {
                m.DaHoanKhoDoHetHan = true;
            });
        }

        // =========================================================================
        // 5. WEBHOOK NHẬN BIẾN ĐỘNG SỐ DƯ TỰ ĐỘNG (/api/webhook/momo)
        // =========================================================================
        [HttpPost("/api/webhook/momo")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> NhanWebhookMoMo([FromBody] MomoWebhookPayload? payload)
        {
            if (payload == null)
            {
                return BadRequest(new { thanhCong = false, thongBao = "Payload Webhook rỗng." });
            }

            string noiDung = payload.Content
                ?? payload.Description
                ?? payload.OrderInfo
                ?? payload.Data?.FirstOrDefault()?.Content
                ?? payload.Data?.FirstOrDefault()?.Description
                ?? string.Empty;

            decimal soTienNhan = payload.TransferAmount
                ?? payload.Amount
                ?? payload.SoTien
                ?? payload.Data?.FirstOrDefault()?.TransferAmount
                ?? payload.Data?.FirstOrDefault()?.Amount
                ?? 0m;

            string? maDonThang = payload.MaDonHang;
            if (string.IsNullOrWhiteSpace(maDonThang))
            {
                var match = Regex.Match(noiDung, @"TT2026[- ]?\d{4}", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string raw = match.Value.ToUpperInvariant().Replace(" ", "");
                    if (!raw.Contains('-'))
                    {
                        raw = "TT2026-" + raw.Substring(6);
                    }
                    maDonThang = raw;
                }
            }

            if (string.IsNullOrWhiteSpace(maDonThang))
            {
                return Ok(new
                {
                    thanhCong = false,
                    thongBao = "Không tìm thấy mã đơn hàng hợp lệ (TT2026-XXXX) trong nội dung chuyển khoản."
                });
            }

            var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaTraCuuDh == maDonThang.Trim());
            if (donHang == null)
            {
                return NotFound(new
                {
                    thanhCong = false,
                    thongBao = $"Không tìm thấy đơn hàng #{maDonThang} trong CSDL."
                });
            }

            _kiotMeta.GanMetadataChoDonHang(donHang);
            DateTime hanTt = donHang.HanThanhToan ?? donHang.NgayDat.AddMinutes(15);

            if (donHang.TrangThaiTt == "HET_HAN_15P" || donHang.TrangThaiDh == "DA_HUY" || DateTime.Now > hanTt)
            {
                return Ok(new
                {
                    thanhCong = false,
                    thongBao = $"Đơn hàng #{donHang.MaTraCuuDh} đã quá hạn thanh toán 15 phút hoặc đã hủy."
                });
            }

            if (soTienNhan < donHang.TongThanhToanCuoi)
            {
                return Ok(new
                {
                    thanhCong = false,
                    thongBao = $"Số tiền chuyển khoản ({soTienNhan:N0} đ) nhỏ hơn tổng thanh toán của đơn ({donHang.TongThanhToanCuoi:N0} đ)."
                });
            }

            if (donHang.TrangThaiTt != "DA_THANH_TOAN")
            {
                donHang.TrangThaiTt = "DA_THANH_TOAN";
                donHang.TrangThaiDh = "DA_XAC_NHAN";
                await _db.SaveChangesAsync();

                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, m =>
                {
                    m.SoTienKhachDaTra = soTienNhan;
                });

                // Tự động sinh bản ghi PhieuThuChi vào Sổ quỹ KiotViet
                _kiotMeta.GhiNhanPhieuThuChi(new PhieuThuChiKiot
                {
                    MaDonHang = donHang.MaDh,
                    MaTraCuuDh = donHang.MaTraCuuDh,
                    LoaiPhieu = "THU",
                    PhuongThuc = "MOMO_QR",
                    SoTien = soTienNhan,
                    NguoiNopNhan = donHang.HoTenNguoiMua,
                    NoiDung = $"Thu tự động Webhook MoMo: {(string.IsNullOrWhiteSpace(noiDung) ? "NGUYETCAT " + donHang.MaTraCuuDh : noiDung.Trim())}",
                    ThoiGian = DateTime.Now
                });
            }

            return Ok(new
            {
                thanhCong = true,
                maTraCuu = donHang.MaTraCuuDh,
                trangThaiTt = donHang.TrangThaiTt,
                trangThaiDh = donHang.TrangThaiDh,
                soTienDaNhan = soTienNhan,
                thongBao = $"Đã xác nhận thanh toán tự động thành công cho đơn #{donHang.MaTraCuuDh}!"
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
