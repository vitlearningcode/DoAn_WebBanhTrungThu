using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuGioHang _dichVuGioHang;

        public TaiKhoanController(CSDLBanhTrungThuContext db, DichVuGioHang dichVuGioHang)
        {
            _db = db;
            _dichVuGioHang = dichVuGioHang;
        }

        // =========================================================================
        // 1. ĐĂNG NHẬP AJAX (COOKIE AUTH + HỢP NHẤT GIỎ HÀNG GUEST -> MEMBER)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> DangNhapAjax([FromBody] DangNhapAjaxRequest? req, [FromQuery] string? tenDangNhap, [FromQuery] string? matKhau)
        {
            string taiKhoan = (req?.TenDangNhap ?? tenDangNhap ?? string.Empty).Trim();
            string mkNhap = (req?.MatKhau ?? matKhau ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrWhiteSpace(mkNhap))
            {
                return Json(new { thanhCong = false, thongBao = "Vui lòng nhập đầy đủ Email/Số điện thoại và Mật khẩu!" });
            }

            // Đảm bảo bảng VaiTro và tài khoản Demo vận hành (QUAN_TRI, THU_NGAN, GIAO_HANG) luôn sẵn sàng
            await DamBaoVaiTroVaTaiKhoanDemoAsync();

            var nd = await _db.NguoiDungs
                .FirstOrDefaultAsync(u => (u.Email == taiKhoan || u.Sdt == taiKhoan) && u.TrangThai);

            if (nd == null)
            {
                return Json(new { thanhCong = false, thongBao = "Tài khoản không tồn tại hoặc đã bị khóa!" });
            }

            string maBamSha256 = ThongTinPhienHelper.BamMatKhauSHA256(mkNhap);
            bool hopLe = false;

            if (string.Equals(nd.MatKhauHash, maBamSha256, StringComparison.OrdinalIgnoreCase))
            {
                hopLe = true;
            }
            else if (nd.MatKhauHash.StartsWith("AQAAAA", StringComparison.OrdinalIgnoreCase) && mkNhap == "123456")
            {
                // Tài khoản dữ liệu mẫu ban đầu -> tự động chuẩn hóa sang SHA256
                nd.MatKhauHash = maBamSha256;
                await _db.SaveChangesAsync();
                hopLe = true;
            }

            if (!hopLe)
            {
                return Json(new { thanhCong = false, thongBao = "Mật khẩu không chính xác. Vui lòng kiểm tra lại!" });
            }

            // Thiết lập Cookie Authentication & Hợp nhất giỏ hàng Guest -> Member
            await ThucHienDangNhapVaGopGioAsync(nd);

            return Json(new
            {
                thanhCong = true,
                hoTen = nd.HoTen,
                maVt = nd.MaVt,
                hangTv = nd.HangTv,
                diem = nd.DiemTichLuy,
                thongBao = $"Đăng nhập thành công ({nd.HoTen} - Vai trò: {nd.MaVt})!"
            });
        }

        // =========================================================================
        // 1B. CHUYỂN NHANH VAI TRÒ VẬN HÀNH (THU_NGAN / GIAO_HANG / QUAN_TRI) ĐỂ DEMO
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenNhanhVaiTroDemo(string maVt, string? returnUrl)
        {
            await DamBaoVaiTroVaTaiKhoanDemoAsync();

            string sdtMucTieu = maVt switch
            {
                "THU_NGAN" => "0908111222",
                "GIAO_HANG" => "0908333444",
                _ => "0814585526"
            };

            var nd = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.Sdt == sdtMucTieu);
            if (nd == null)
            {
                nd = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.MaVt == maVt);
            }

            if (nd != null)
            {
                await ThucHienDangNhapVaGopGioAsync(nd);
                TempData["ThongBaoAdmin"] = $"Đã chuyển nhanh sang phiên làm việc: {nd.HoTen} (Quyền: {nd.MaVt})!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("/Admin/DonHangAdmin/Index");
        }

        // =========================================================================
        // 2. ĐĂNG KÝ THÀNH VIÊN MỚI AJAX (+50 ĐIỂM CHÀO MỪNG & HỢP NHẤT GIỎ HÀNG)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> DangKyAjax(
            [FromBody] DangKyAjaxRequest? req,
            [FromQuery] string? hoTen,
            [FromQuery] string? sdt,
            [FromQuery] string? email,
            [FromQuery] string? matKhau)
        {
            string ten = (req?.HoTen ?? hoTen ?? string.Empty).Trim();
            string soDt = (req?.Sdt ?? sdt ?? string.Empty).Trim();
            string thuDienTu = (req?.Email ?? email ?? string.Empty).Trim();
            string mk = (req?.MatKhau ?? matKhau ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(ten) || string.IsNullOrWhiteSpace(soDt) || string.IsNullOrWhiteSpace(thuDienTu) || string.IsNullOrWhiteSpace(mk))
            {
                return Json(new { thanhCong = false, thongBao = "Vui lòng điền đầy đủ Họ tên, Số điện thoại, Email và Mật khẩu!" });
            }

            bool daTonTai = await _db.NguoiDungs.AnyAsync(u => u.Email == thuDienTu || u.Sdt == soDt);
            if (daTonTai)
            {
                return Json(new { thanhCong = false, thongBao = "Email hoặc Số điện thoại này đã được đăng ký!" });
            }

            var ndMoi = new NguoiDung
            {
                MaVt = "KHACH_TV",
                HoTen = ten,
                Sdt = soDt,
                Email = thuDienTu,
                MatKhauHash = ThongTinPhienHelper.BamMatKhauSHA256(mk),
                DiemTichLuy = 50,
                HangTv = "Đồng",
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _db.NguoiDungs.Add(ndMoi);
            await _db.SaveChangesAsync();

            // Tự động đăng nhập & gộp giỏ hàng từ phiên Khách vãng lai
            await ThucHienDangNhapVaGopGioAsync(ndMoi);

            return Json(new
            {
                thanhCong = true,
                hoTen = ndMoi.HoTen,
                maVt = ndMoi.MaVt,
                hangTv = ndMoi.HangTv,
                diem = ndMoi.DiemTichLuy,
                thongBao = "Đăng ký thành công (Tặng +50 điểm) & Đã hợp nhất giỏ hàng vào tài khoản!"
            });
        }

        // =========================================================================
        // 3. ĐĂNG XUẤT KHỎI HỆ THỐNG & REDIRECT KHI CHƯA PHÂN QUYỀN
        // =========================================================================
        [HttpGet]
        public IActionResult DangNhap()
        {
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult TuChoiTruyCap()
        {
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // =========================================================================
        // 4. TRANG HỒ SƠ THÀNH VIÊN & TIỆN ÍCH CÁ NHÂN ([Authorize] /TaiKhoan/HoSo)
        // =========================================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HoSo(string? tab)
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            if (!maNd.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var nd = await _db.NguoiDungs
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MaNd == maNd.Value);

            if (nd == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var soDiaChis = await _db.SoDiaChis
                .AsNoTracking()
                .Where(d => d.MaNd == maNd.Value)
                .OrderByDescending(d => d.LaMacDinh)
                .ThenByDescending(d => d.MaDc)
                .ToListAsync();

            var lichSuDonHangs = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .Where(d => d.MaNd == maNd.Value)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            var danhSachHopTuChons = await _db.HopQuaTuChons
                .AsNoTracking()
                .Include(h => h.MaVhNavigation)
                .Include(h => h.ChiTietHopTuChons)
                    .ThenInclude(ct => ct.MaBanhNavigation)
                .Where(h => h.MaNd == maNd.Value)
                .OrderByDescending(h => h.NgayTao)
                .ToListAsync();

            ViewBag.TabMacDinh = string.IsNullOrWhiteSpace(tab) ? "DON_HANG" : tab.ToUpperInvariant();

            var vm = new HoSoThanhVienViewModel
            {
                NguoiDung = nd,
                DanhSachSoDiaChi = soDiaChis,
                LichSuDonHang = lichSuDonHangs,
                DanhSachHopTuChon = danhSachHopTuChons
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemDiaChiMoi(
            string tenNguoiNhan,
            string sdtNguoiNhan,
            string tinhThanh,
            string quanHuyen,
            string phuongXa,
            string diaChiCuThe,
            string? nhanDiaChi,
            bool laMacDinh = false)
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            if (!maNd.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(tenNguoiNhan) || string.IsNullOrWhiteSpace(sdtNguoiNhan) || string.IsNullOrWhiteSpace(diaChiCuThe))
            {
                TempData["ThongBaoHoSo"] = "⚠️ Vui lòng nhập đầy đủ Tên người nhận, Số điện thoại và Địa chỉ cụ thể!";
                return RedirectToAction(nameof(HoSo), new { tab = "DIA_CHI" });
            }

            if (laMacDinh)
            {
                var cacDiaChiCu = await _db.SoDiaChis.Where(d => d.MaNd == maNd.Value && d.LaMacDinh).ToListAsync();
                foreach (var dc in cacDiaChiCu)
                {
                    dc.LaMacDinh = false;
                }
            }

            var dcMoi = new SoDiaChi
            {
                MaNd = maNd.Value,
                TenNguoiNhan = tenNguoiNhan.Trim(),
                SdtnguoiNhan = sdtNguoiNhan.Trim(),
                TinhThanh = string.IsNullOrWhiteSpace(tinhThanh) ? "TP. Hồ Chí Minh" : tinhThanh.Trim(),
                QuanHuyen = string.IsNullOrWhiteSpace(quanHuyen) ? "Quận 1" : quanHuyen.Trim(),
                PhuongXa = string.IsNullOrWhiteSpace(phuongXa) ? "Phường Bến Nghé" : phuongXa.Trim(),
                DiaChiCuThe = diaChiCuThe.Trim(),
                NhanDiaChi = string.IsNullOrWhiteSpace(nhanDiaChi) ? "Biếu Người Thân" : nhanDiaChi.Trim(),
                LaMacDinh = laMacDinh
            };

            _db.SoDiaChis.Add(dcMoi);
            await _db.SaveChangesAsync();

            TempData["ThongBaoHoSo"] = $"📍 Đã thêm địa chỉ \"{dcMoi.NhanDiaChi} - {dcMoi.TenNguoiNhan}\" vào Sổ địa chỉ biếu tặng!";
            return RedirectToAction(nameof(HoSo), new { tab = "DIA_CHI" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDiaChi(int maDc)
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            if (!maNd.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var dc = await _db.SoDiaChis.FirstOrDefaultAsync(d => d.MaDc == maDc && d.MaNd == maNd.Value);
            if (dc != null)
            {
                _db.SoDiaChis.Remove(dc);
                await _db.SaveChangesAsync();
                TempData["ThongBaoHoSo"] = "🗑️ Đã xóa địa chỉ khỏi Sổ địa chỉ biếu tặng!";
            }

            return RedirectToAction(nameof(HoSo), new { tab = "DIA_CHI" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLaiHopTuChon(int maHopTc)
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            if (!maNd.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var hopCu = await _db.HopQuaTuChons
                .Include(h => h.MaVhNavigation)
                .Include(h => h.ChiTietHopTuChons)
                .FirstOrDefaultAsync(h => h.MaHopTc == maHopTc && h.MaNd == maNd.Value);

            if (hopCu == null)
            {
                TempData["ThongBaoHoSo"] = "⚠️ Không tìm thấy mẫu hộp tự phối này!";
                return RedirectToAction(nameof(HoSo), new { tab = "HOP_TU_CHON" });
            }

            var gioHang = await _db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.MaHopTcNavigation)
                .FirstOrDefaultAsync(g => g.MaNd == maNd.Value);

            if (gioHang == null)
            {
                gioHang = new GioHang
                {
                    MaNd = maNd.Value,
                    MaPhienKvl = null,
                    LaCheDoMuaSiB2b = false,
                    YeuCauInLogoB2b = false,
                    NgayCapNhat = DateTime.Now
                };
                _db.GioHangs.Add(gioHang);
                await _db.SaveChangesAsync();
            }

            var dongDaCo = gioHang.ChiTietGioHangs.FirstOrDefault(ct =>
                ct.LoaiSanPham == 3 &&
                (ct.MaHopTc == hopCu.MaHopTc || (ct.MaHopTcNavigation != null && ct.MaHopTcNavigation.MaBamCauHinh == hopCu.MaBamCauHinh)));

            if (dongDaCo != null)
            {
                dongDaCo.SoLuong += 1;
                dongDaCo.DonGiaTamTinh = hopCu.TongGiaMotHop;
            }
            else
            {
                _db.ChiTietGioHangs.Add(new ChiTietGioHang
                {
                    MaGh = gioHang.MaGh,
                    LoaiSanPham = 3,
                    MaHopTc = hopCu.MaHopTc,
                    SoLuong = 1,
                    DonGiaTamTinh = hopCu.TongGiaMotHop,
                    NgayThem = DateTime.Now
                });
            }

            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["ThongBaoHoSo"] = $"🛒 Đã đưa mẫu hộp \"{hopCu.TenHopKhachDat}\" ({hopCu.MaBamCauHinh}) vào lại Giỏ Quà thành công!";
            return RedirectToAction(nameof(HoSo), new { tab = "HOP_TU_CHON" });
        }

        private async Task DamBaoVaiTroVaTaiKhoanDemoAsync()
        {
            var danhSachVaiTro = new (string MaVt, string TenVt, string MoTa)[]
            {
                ("QUAN_TRI", "Quản trị viên", "Toàn quyền quản trị hệ thống & Admin Override"),
                ("NHAN_VIEN", "Nhân viên bán hàng", "Nhân viên Order - Bán hàng & Thu ngân"),
                ("THU_NGAN", "Thu ngân & Bán hàng", "Xác nhận thanh toán QR, chuẩn bị hàng CHO_LAY_HANG, xử lý hoàn tiền"),
                ("GIAO_HANG", "Nhân viên giao hàng", "Nhận đơn từ CHO_LAY_HANG -> DANG_GIAO -> xác nhận HOAN_TAT"),
                ("KHACH_TV", "Khách thành viên", "Khách hàng thành viên tích điểm")
            };

            bool coThayDoiVt = false;
            foreach (var vt in danhSachVaiTro)
            {
                if (!await _db.VaiTros.AnyAsync(v => v.MaVt == vt.MaVt))
                {
                    _db.VaiTros.Add(new VaiTro
                    {
                        MaVt = vt.MaVt,
                        TenVt = vt.TenVt,
                        MoTa = vt.MoTa
                    });
                    coThayDoiVt = true;
                }
            }

            if (coThayDoiVt)
            {
                await _db.SaveChangesAsync();
            }

            string mkSha = ThongTinPhienHelper.BamMatKhauSHA256("123456");

            var taiKhoanMau = new (string Sdt, string Email, string HoTen, string MaVt, string HangTv)[]
            {
                ("0908111222", "thungan@nguyetcat.vn", "Trần Thu Ngân (Order & Thu ngân)", "THU_NGAN", "Vàng"),
                ("0908333444", "giaohang@nguyetcat.vn", "Nguyễn Văn Giao (Giao vận)", "GIAO_HANG", "Bạc"),
                ("0814585526", "admin@nguyetcat.vn", "Lê Minh Đức (Quản trị viên)", "QUAN_TRI", "Kim Cương")
            };

            bool coThayDoiNd = false;
            foreach (var tk in taiKhoanMau)
            {
                if (!await _db.NguoiDungs.AnyAsync(u => u.Sdt == tk.Sdt || u.Email == tk.Email))
                {
                    _db.NguoiDungs.Add(new NguoiDung
                    {
                        MaVt = tk.MaVt,
                        HoTen = tk.HoTen,
                        Sdt = tk.Sdt,
                        Email = tk.Email,
                        MatKhauHash = mkSha,
                        DiemTichLuy = 500,
                        HangTv = tk.HangTv,
                        TrangThai = true,
                        NgayTao = DateTime.Now
                    });
                    coThayDoiNd = true;
                }
            }

            if (coThayDoiNd)
            {
                await _db.SaveChangesAsync();
            }
        }

        private async Task ThucHienDangNhapVaGopGioAsync(NguoiDung nd)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nd.MaNd.ToString()),
                new Claim(ClaimTypes.Name, nd.HoTen),
                new Claim(ClaimTypes.Email, nd.Email),
                new Claim(ClaimTypes.Role, nd.MaVt),
                new Claim("HangTV", nd.HangTv)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Hợp nhất toàn bộ giỏ hàng và hộp tự chọn của Guest vào tài khoản Member vừa đăng nhập
            string? maPhienKVL = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);
            if (!string.IsNullOrWhiteSpace(maPhienKVL))
            {
                await _dichVuGioHang.GopGioHangAsync(maPhienKVL, nd.MaNd);
                Response.Cookies.Delete(PhienKhachVangLaiMiddleware.TenCookieKVL);
            }
        }
    }
}
