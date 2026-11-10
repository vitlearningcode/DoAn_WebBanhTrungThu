using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    public class GioHangController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;

        public GioHangController(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lấy hoặc khởi tạo Giỏ hàng cho Member (MaNd) hoặc Guest (MaPhienKvl)
        /// </summary>
        private async Task<GioHang> LayHoacTaoGioHangAsync()
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            GioHang? gioHang = null;

            if (maNd.HasValue)
            {
                gioHang = await _db.GioHangs
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
                                    .ThenInclude(b => b!.LoSanXuatBanhs)
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
            }
            else
            {
                if (string.IsNullOrWhiteSpace(maPhienKvl))
                {
                    maPhienKvl = "KVL-" + Guid.NewGuid().ToString("N").ToUpper()[..20];
                    var phienMoi = new PhienKhachVangLai
                    {
                        MaPhienKvl = maPhienKvl,
                        DiaChiIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        ThongTinTrinhDuyet = "Browser Session",
                        DaGopGioHang = false,
                        NgayTao = DateTime.Now,
                        NgayHetHan = DateTime.Now.AddDays(14)
                    };
                    _db.PhienKhachVangLais.Add(phienMoi);
                    await _db.SaveChangesAsync();

                    Response.Cookies.Append(PhienKhachVangLaiMiddleware.TenCookieKVL, maPhienKvl, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true,
                        Expires = DateTimeOffset.UtcNow.AddDays(14)
                    });
                    HttpContext.Items["MaPhienKVL"] = maPhienKvl;
                }

                gioHang = await _db.GioHangs
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
                                    .ThenInclude(b => b!.LoSanXuatBanhs)
                    .FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl);

                if (gioHang == null)
                {
                    gioHang = new GioHang
                    {
                        MaNd = null,
                        MaPhienKvl = maPhienKvl,
                        LaCheDoMuaSiB2b = false,
                        YeuCauInLogoB2b = false,
                        NgayCapNhat = DateTime.Now
                    };
                    _db.GioHangs.Add(gioHang);
                    await _db.SaveChangesAsync();
                }
            }

            return gioHang;
        }

        /// <summary>
        /// Tính lại đơn giá thực tế của từng dòng giỏ hàng trực tiếp từ CSDL (Anti-Tampering 100%)
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
                decimal giaVoHop = ct.MaHopTcNavigation.MaVhNavigation?.GiaVoHopRieng ?? 85000m;
                decimal tongGiaBanh = ct.MaHopTcNavigation.ChiTietHopTuChons.Sum(x =>
                    x.MaBanhNavigation != null ? x.MaBanhNavigation.DonGiaThucTe : x.DonGiaBanhLucChon);
                return Math.Round((giaVoHop * 0.95m) + tongGiaBanh, 0);
            }
            return Math.Round(ct.DonGiaTamTinh, 0);
        }

        private async Task DamBaoMaKhuyenMaiMauConHanAsync()
        {
            var mocHetHan = new DateTime(2027, 12, 31, 23, 59, 59);
            var kmTrangTron = await _db.MaKhuyenMais.FirstOrDefaultAsync(k => k.CodeKm == "TRANGTRON2026");
            if (kmTrangTron == null)
            {
                _db.MaKhuyenMais.Add(new MaKhuyenMai
                {
                    CodeKm = "TRANGTRON2026",
                    TenChuongTrinh = "Ưu Đãi Đêm Trăng Đoàn Viên (-10%)",
                    LoaiGiam = "PHAN_TRAM",
                    GiaTriGiam = 10m,
                    GiamToiDa = 150000m,
                    DonToiThieu = 200000m,
                    SltoiDa = 500,
                    SldaDung = 0,
                    ChoPhepKhachVangLai = true,
                    NgayBatDau = new DateTime(2026, 1, 1),
                    NgayKetThuc = mocHetHan
                });
                await _db.SaveChangesAsync();
            }
            else if (kmTrangTron.NgayKetThuc < mocHetHan || kmTrangTron.NgayBatDau > DateTime.Now)
            {
                if (kmTrangTron.NgayBatDau > DateTime.Now) kmTrangTron.NgayBatDau = new DateTime(2026, 1, 1);
                if (kmTrangTron.NgayKetThuc < mocHetHan) kmTrangTron.NgayKetThuc = mocHetHan;
                if (kmTrangTron.SltoiDa <= kmTrangTron.SldaDung) kmTrangTron.SltoiDa = kmTrangTron.SldaDung + 100;
                await _db.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 1. API THÊM HỘP QUÀ TỰ CHỌN VÀO GIỎ HÀNG (CUSTOM BOX BUILDER -> SQL SERVER)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> ThemHopTuChon([FromBody] LuuHopTuChonRequest? req)
        {
            if (req == null || req.DanhSachMaBanh == null || !req.DanhSachMaBanh.Any())
            {
                return Json(new { thanhCong = false, thongBao = "Dữ liệu cấu hình hộp quà không hợp lệ!" });
            }

            var voHop = await _db.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == req.MaVh && v.TrangThai);
            if (voHop == null)
            {
                return Json(new { thanhCong = false, thongBao = "Mẫu vỏ hộp không tồn tại hoặc đã ngừng kinh doanh!" });
            }

            if (req.DanhSachMaBanh.Count != voHop.SucChuaBanh)
            {
                return Json(new
                {
                    thanhCong = false,
                    thongBao = $"Vỏ hộp '{voHop.TenVoHop}' yêu cầu xếp đủ {voHop.SucChuaBanh} bánh (hiện mới có {req.DanhSachMaBanh.Count} bánh)!"
                });
            }

            // Truy vấn thông tin các bánh được chọn
            var maBanhPhanBiet = req.DanhSachMaBanh.Distinct().ToList();
            var tuDienBanh = await _db.SanPhamBanhs
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .Where(b => maBanhPhanBiet.Contains(b.MaBanh) && b.TrangThaiBan)
                .ToDictionaryAsync(b => b.MaBanh);

            if (req.DanhSachMaBanh.Any(id => !tuDienBanh.ContainsKey(id)))
            {
                return Json(new { thanhCong = false, thongBao = "Có vị bánh trong khay không tồn tại hoặc đã ngừng bán!" });
            }

            // Kiểm tra số lượng bánh Thượng Hạng tối thiểu
            int soBanhVip = req.DanhSachMaBanh.Count(id => tuDienBanh[id].LaBanhThuongHang);
            if (soBanhVip < voHop.SlbanhThuongHangToiThieu)
            {
                return Json(new
                {
                    thanhCong = false,
                    thongBao = $"Mẫu vỏ '{voHop.TenVoHop}' yêu cầu tối thiểu {voHop.SlbanhThuongHangToiThieu} bánh Thượng Hạng (VIP)!"
                });
            }

            // Tính Tổng Calo & Tổng Giá 1 Hộp (Giảm 5% giá vỏ hộp khi tự phối + Tổng đơn giá các bánh)
            int tongCaloHop = req.DanhSachMaBanh.Sum(id => tuDienBanh[id].ThongSoDinhDuong?.NangLuongCalo ?? 400);
            decimal tongDonGiaCacBanh = req.DanhSachMaBanh.Sum(id => tuDienBanh[id].DonGiaThucTe);
            decimal tongGiaMotHop = Math.Round((voHop.GiaVoHopRieng * 0.95m) + tongDonGiaCacBanh, 0);

            // Tạo chuỗi MaBamCauHinh chuẩn: "VH" + MaVh + "_B" + string.Join("-B", DanhSachMaBanh)
            string maBamCauHinh = "VH" + req.MaVh + "_B" + string.Join("-B", req.DanhSachMaBanh);
            int soLuongThem = req.SoLuongHop > 0 ? req.SoLuongHop : 1;
            string tenHop = string.IsNullOrWhiteSpace(req.TenHopKhachDat)
                ? "Hộp Quà Đoàn Viên Tự Phối"
                : req.TenHopKhachDat.Trim();

            var gioHang = await LayHoacTaoGioHangAsync();

            // Kiểm tra xem trong GioHang hiện tại đã có HopQuaTuChon nào cùng MaBamCauHinh chưa
            var dongGioDaCo = gioHang.ChiTietGioHangs.FirstOrDefault(ct =>
                ct.LoaiSanPham == 3 &&
                ct.MaHopTcNavigation != null &&
                ct.MaHopTcNavigation.MaBamCauHinh == maBamCauHinh);

            if (dongGioDaCo != null)
            {
                dongGioDaCo.SoLuong += soLuongThem;
                dongGioDaCo.DonGiaTamTinh = tongGiaMotHop;
                if (dongGioDaCo.MaHopTcNavigation != null && !string.IsNullOrWhiteSpace(req.TenHopKhachDat))
                {
                    dongGioDaCo.MaHopTcNavigation.TenHopKhachDat = tenHop;
                }
            }
            else
            {
                int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
                string? maPhienKvl = maNd.HasValue ? null : ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

                var hopMoi = new HopQuaTuChon
                {
                    MaVh = voHop.MaVh,
                    MaNd = maNd,
                    MaPhienKvl = maPhienKvl,
                    TenHopKhachDat = tenHop,
                    MaBamCauHinh = maBamCauHinh,
                    TongCaloHop = tongCaloHop,
                    TongGiaMotHop = tongGiaMotHop,
                    DaLuuMauYeuThich = false,
                    NgayTao = DateTime.Now
                };

                _db.HopQuaTuChons.Add(hopMoi);
                await _db.SaveChangesAsync();

                // Lưu danh sách ChiTietHopTuChon (ViTriOtrongHop từ 1..N)
                for (int i = 0; i < req.DanhSachMaBanh.Count; i++)
                {
                    int maBanhO = req.DanhSachMaBanh[i];
                    var ctHop = new ChiTietHopTuChon
                    {
                        MaHopTc = hopMoi.MaHopTc,
                        MaBanh = maBanhO,
                        ViTriOtrongHop = i + 1,
                        DonGiaBanhLucChon = tuDienBanh[maBanhO].DonGiaThucTe
                    };
                    _db.ChiTietHopTuChons.Add(ctHop);
                }

                // Thêm mới dòng ChiTietGioHang (LoaiSanPham = 3)
                var ctGioMoi = new ChiTietGioHang
                {
                    MaGh = gioHang.MaGh,
                    LoaiSanPham = 3,
                    MaHopTc = hopMoi.MaHopTc,
                    SoLuong = soLuongThem,
                    DonGiaTamTinh = tongGiaMotHop,
                    NgayThem = DateTime.Now
                };
                _db.ChiTietGioHangs.Add(ctGioMoi);
            }

            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                thanhCong = true,
                thongBao = $"Đã lưu \"{tenHop}\" ({maBamCauHinh}) vào Giỏ Quà thành công!",
                maBamCauHinh = maBamCauHinh,
                donGiaHop = tongGiaMotHop
            });
        }

        // =========================================================================
        // 2. API THÊM BÁNH LẺ HOẶC COMBO MẪU CÓ SẴN VÀO GIỎ HÀNG
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> ThemBanhHoacCombo([FromBody] ThemMonLeRequest? req)
        {
            if (req == null || req.MaSanPham <= 0)
            {
                return Json(new { thanhCong = false, thongBao = "Thông tin sản phẩm không hợp lệ!" });
            }

            int soLuongThem = req.SoLuong > 0 ? req.SoLuong : 1;
            var gioHang = await LayHoacTaoGioHangAsync();
            string tenHienThi = "";

            if (req.LoaiSanPham == 1)
            {
                var banh = await _db.SanPhamBanhs.Include(b => b.LoSanXuatBanhs).FirstOrDefaultAsync(b => b.MaBanh == req.MaSanPham && b.TrangThaiBan);
                if (banh == null)
                {
                    return Json(new { thanhCong = false, thongBao = "Sản phẩm bánh không tồn tại hoặc đã ngừng bán!" });
                }

                tenHienThi = banh.TenBanh;
                var dongHienTai = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.LoaiSanPham == 1 && ct.MaBanh == banh.MaBanh);

                if (dongHienTai != null)
                {
                    dongHienTai.SoLuong += soLuongThem;
                    dongHienTai.DonGiaTamTinh = banh.DonGiaThucTe;
                }
                else
                {
                    _db.ChiTietGioHangs.Add(new ChiTietGioHang
                    {
                        MaGh = gioHang.MaGh,
                        LoaiSanPham = 1,
                        MaBanh = banh.MaBanh,
                        SoLuong = soLuongThem,
                        DonGiaTamTinh = banh.DonGiaThucTe,
                        NgayThem = DateTime.Now
                    });
                }
            }
            else if (req.LoaiSanPham == 2)
            {
                var combo = await _db.ComboMauCoSans.FirstOrDefaultAsync(c => c.MaCb == req.MaSanPham && c.TrangThai);
                if (combo == null)
                {
                    return Json(new { thanhCong = false, thongBao = "Combo lễ hộp không tồn tại hoặc đã ngừng bán!" });
                }

                tenHienThi = combo.TenCombo;
                var dongHienTai = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.LoaiSanPham == 2 && ct.MaCb == combo.MaCb);

                if (dongHienTai != null)
                {
                    dongHienTai.SoLuong += soLuongThem;
                    dongHienTai.DonGiaTamTinh = combo.GiaUuDaiCombo;
                }
                else
                {
                    _db.ChiTietGioHangs.Add(new ChiTietGioHang
                    {
                        MaGh = gioHang.MaGh,
                        LoaiSanPham = 2,
                        MaCb = combo.MaCb,
                        SoLuong = soLuongThem,
                        DonGiaTamTinh = combo.GiaUuDaiCombo,
                        NgayThem = DateTime.Now
                    });
                }
            }
            else
            {
                return Json(new { thanhCong = false, thongBao = "Loại sản phẩm không được hỗ trợ!" });
            }

            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                thanhCong = true,
                thongBao = $"Đã thêm {soLuongThem} x \"{tenHienThi}\" vào Giỏ Quà!"
            });
        }

        // =========================================================================
        // 3. API CẬP NHẬT SỐ LƯỢNG & XÓA MÓN KHỎI GIỎ HÀNG
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> CapNhatSoLuong([FromBody] CapNhatSoLuongGioRequest? req)
        {
            if (req == null || req.MaCtgh <= 0)
            {
                return Json(new { thanhCong = false, thongBao = "Dữ liệu cập nhật không hợp lệ!" });
            }

            var gioHang = await LayHoacTaoGioHangAsync();
            var dongGio = await _db.ChiTietGioHangs
                .FirstOrDefaultAsync(ct => ct.MaCtgh == req.MaCtgh && ct.MaGh == gioHang.MaGh);

            if (dongGio == null)
            {
                return Json(new { thanhCong = false, thongBao = "Không tìm thấy sản phẩm trong giỏ hàng của bạn!" });
            }

            if (req.SoLuongMoi <= 0)
            {
                await XoaDongChiTietGioHangAsync(dongGio);
                gioHang.NgayCapNhat = DateTime.Now;
                await _db.SaveChangesAsync();
                return Json(new { thanhCong = true, thongBao = "Đã xóa sản phẩm khỏi Giỏ Quà!" });
            }

            dongGio.SoLuong = req.SoLuongMoi;
            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { thanhCong = true, thongBao = "Đã cập nhật số lượng!" });
        }

        [HttpPost]
        public async Task<IActionResult> XoaMon([FromBody] CapNhatSoLuongGioRequest? req)
        {
            if (req == null || req.MaCtgh <= 0)
            {
                return Json(new { thanhCong = false, thongBao = "Mã chi tiết giỏ hàng không hợp lệ!" });
            }

            var gioHang = await LayHoacTaoGioHangAsync();
            var dongGio = await _db.ChiTietGioHangs
                .FirstOrDefaultAsync(ct => ct.MaCtgh == req.MaCtgh && ct.MaGh == gioHang.MaGh);

            if (dongGio == null)
            {
                return Json(new { thanhCong = false, thongBao = "Món quà này không tồn tại trong giỏ!" });
            }

            await XoaDongChiTietGioHangAsync(dongGio);
            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { thanhCong = true, thongBao = "Đã xóa món quà khỏi giỏ hàng!" });
        }

        private async Task XoaDongChiTietGioHangAsync(ChiTietGioHang dongGio)
        {
            int? maHopTcCanXoa = dongGio.LoaiSanPham == 3 ? dongGio.MaHopTc : null;
            int maCtghHienTai = dongGio.MaCtgh;

            _db.ChiTietGioHangs.Remove(dongGio);

            if (maHopTcCanXoa.HasValue)
            {
                bool daGanDonHang = await _db.ChiTietDonHangs.AnyAsync(d => d.MaHopTc == maHopTcCanXoa.Value);
                bool conTrongGioKhac = await _db.ChiTietGioHangs.AnyAsync(c => c.MaHopTc == maHopTcCanXoa.Value && c.MaCtgh != maCtghHienTai);

                if (!daGanDonHang && !conTrongGioKhac)
                {
                    var hopTuChon = await _db.HopQuaTuChons
                        .Include(h => h.ChiTietHopTuChons)
                        .FirstOrDefaultAsync(h => h.MaHopTc == maHopTcCanXoa.Value);

                    if (hopTuChon != null)
                    {
                        _db.ChiTietHopTuChons.RemoveRange(hopTuChon.ChiTietHopTuChons);
                        _db.HopQuaTuChons.Remove(hopTuChon);
                    }
                }
            }
        }

        // =========================================================================
        // 4. API ÁP DỤNG MÃ KHUYẾN MÃI (VOUCHER) CHO GUEST & MEMBER
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> ApDungVoucher([FromBody] ApDungVoucherRequest? req, [FromQuery] string? codeKm)
        {
            await DamBaoMaKhuyenMaiMauConHanAsync();

            string maNhap = (req?.CodeKm ?? codeKm ?? string.Empty).Trim().ToUpper();
            var gioHang = await LayHoacTaoGioHangAsync();

            if (string.IsNullOrWhiteSpace(maNhap))
            {
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = DateTime.Now;
                await _db.SaveChangesAsync();
                return Json(new { thanhCong = false, thongBao = "Vui lòng nhập mã khuyến mãi hợp lệ!" });
            }

            if (!gioHang.ChiTietGioHangs.Any())
            {
                return Json(new { thanhCong = false, thongBao = "Giỏ quà đang trống, vui lòng chọn bánh trước khi áp mã!" });
            }

            var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k => k.CodeKm == maNhap);
            if (km == null)
            {
                return Json(new { thanhCong = false, thongBao = $"Mã giảm giá '{maNhap}' không tồn tại trong hệ thống!" });
            }

            // Điều kiện 1: TrangThaiKichHoat == true
            if (!km.TrangThaiKichHoat)
            {
                return Json(new { thanhCong = false, thongBao = $"Mã ưu đãi '{maNhap}' hiện đang tạm ngưng kích hoạt!" });
            }

            // Điều kiện 2: DateTime.Now >= NgayBatDau && DateTime.Now <= NgayKetThuc
            DateTime bayGio = DateTime.Now;
            if (bayGio < km.NgayBatDau)
            {
                return Json(new { thanhCong = false, thongBao = $"Mã ưu đãi '{maNhap}' chỉ có hiệu lực từ ngày {km.NgayBatDau:dd/MM/yyyy}!" });
            }
            if (bayGio > km.NgayKetThuc)
            {
                return Json(new { thanhCong = false, thongBao = $"Mã ưu đãi đã hết hạn vào ngày {km.NgayKetThuc:dd/MM/yyyy}" });
            }

            // Điều kiện 3: SoLuotDaDung < SoLuotToiDa
            if (km.SoLuotDaDung >= km.SoLuotToiDa)
            {
                return Json(new { thanhCong = false, thongBao = $"Mã giảm giá '{maNhap}' đã hết lượt sử dụng ({km.SoLuotDaDung}/{km.SoLuotToiDa})!" });
            }

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            if (!maNd.HasValue && !km.ChoPhepKhachVangLai)
            {
                return Json(new
                {
                    thanhCong = false,
                    thongBao = $"Mã '{maNhap}' chỉ dành riêng cho Thành Viên. Vui lòng Đăng nhập để sử dụng!"
                });
            }

            // Tính lại TongTienHang 100% từ CSDL
            foreach (var ct in gioHang.ChiTietGioHangs)
            {
                ct.DonGiaTamTinh = TinhDonGiaThucTeTuCsdl(ct);
            }
            decimal tongTamTinh = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong * ct.DonGiaTamTinh);

            // Điều kiện 4: TongTienHang >= GiaTriDonToiThieu
            if (tongTamTinh < km.GiaTriDonToiThieu)
            {
                return Json(new
                {
                    thanhCong = false,
                    thongBao = $"Mã '{maNhap}' yêu cầu đơn hàng tối thiểu {km.GiaTriDonToiThieu:N0} đ (Hiện tại: {tongTamTinh:N0} đ)!"
                });
            }

            decimal tienGiam = TinhTienGiamVoucher(km, tongTamTinh);
            gioHang.CodeKmdangApDung = km.CodeKm;
            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                thanhCong = true,
                thongBao = $"Áp dụng mã '{km.CodeKm}' ({km.TenChuongTrinh} • HSD: {km.NgayKetThuc:dd/MM/yyyy}) thành công! Giảm {tienGiam:N0} đ.",
                codeKmDangDung = km.CodeKm,
                hanSuDungVoucher = km.NgayKetThuc.ToString("dd/MM/yyyy"),
                tienGiamVoucher = tienGiam,
                tongThanhToanSauGiam = Math.Max(0, tongTamTinh - tienGiam)
            });
        }

        private static decimal TinhTienGiamVoucher(MaKhuyenMai km, decimal tongTamTinh)
        {
            if (tongTamTinh < km.DonToiThieu) return 0m;

            decimal tienGiam = 0m;
            if (km.LoaiGiam == "PHAN_TRAM")
            {
                tienGiam = Math.Round(tongTamTinh * (km.GiaTriGiam / 100m), 0);
                if (km.GiamToiDa > 0 && tienGiam > km.GiamToiDa)
                {
                    tienGiam = km.GiamToiDa;
                }
            }
            else
            {
                tienGiam = km.GiaTriGiam;
                if (km.GiamToiDa > 0 && tienGiam > km.GiamToiDa)
                {
                    tienGiam = km.GiamToiDa;
                }
            }

            return Math.Min(tienGiam, tongTamTinh);
        }

        // =========================================================================
        // 5. API LẤY THÔNG TIN GIỎ HÀNG JSON (ĐỒNG BỘ OFFCANVAS & BADGE REALTIME)
        // =========================================================================
        // Nhánh 3.2.4: Đặt lại đơn hàng cũ 1-Click
        [HttpPost]
        public async Task<IActionResult> DatLaiDonHang([FromBody] int maDonHang)
        {
            if (maDonHang <= 0)
            {
                return Json(new { thanhCong = false, thongBao = "Mã đơn hàng không hợp lệ!" });
            }

            var chiTietDonCu = await _db.ChiTietDonHangs
                .AsNoTracking()
                .Where(ct => ct.MaDh == maDonHang)
                .ToListAsync();

            if (!chiTietDonCu.Any())
            {
                return Json(new { thanhCong = false, thongBao = "Không tìm thấy chi tiết sản phẩm của đơn hàng cũ!" });
            }

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            var gioHang = await LayHoacTaoGioHangAsync();
            string? maPhienKvl = maNd.HasValue ? null : gioHang.MaPhienKvl;

            foreach (var ct in chiTietDonCu)
            {
                int soLuongThem = Math.Max(1, ct.SoLuong);

                if (ct.LoaiSanPham == 1 && ct.MaBanh.HasValue)
                {
                    var banh = await _db.SanPhamBanhs
                        .AsNoTracking()
                        .Include(b => b.LoSanXuatBanhs)
                        .FirstOrDefaultAsync(b => b.MaBanh == ct.MaBanh.Value);

                    decimal donGiaHienTai = banh != null
                        ? Math.Round(banh.DonGiaThucTe, 0)
                        : ct.DonGiaLucMua;

                    var dongGio = gioHang.ChiTietGioHangs
                        .FirstOrDefault(x => x.LoaiSanPham == 1 && x.MaBanh == ct.MaBanh.Value);

                    if (dongGio != null)
                    {
                        dongGio.SoLuong += soLuongThem;
                        dongGio.DonGiaTamTinh = donGiaHienTai;
                        dongGio.NgayThem = DateTime.Now;
                    }
                    else
                    {
                        _db.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaGh = gioHang.MaGh,
                            LoaiSanPham = 1,
                            MaBanh = ct.MaBanh.Value,
                            SoLuong = soLuongThem,
                            DonGiaTamTinh = donGiaHienTai,
                            NgayThem = DateTime.Now
                        });
                    }
                }
                else if (ct.LoaiSanPham == 2 && ct.MaCb.HasValue)
                {
                    var combo = await _db.ComboMauCoSans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.MaCb == ct.MaCb.Value);

                    decimal donGiaCombo = combo != null
                        ? combo.GiaUuDaiCombo
                        : ct.DonGiaLucMua;

                    var dongGio = gioHang.ChiTietGioHangs
                        .FirstOrDefault(x => x.LoaiSanPham == 2 && x.MaCb == ct.MaCb.Value);

                    if (dongGio != null)
                    {
                        dongGio.SoLuong += soLuongThem;
                        dongGio.DonGiaTamTinh = donGiaCombo;
                        dongGio.NgayThem = DateTime.Now;
                    }
                    else
                    {
                        _db.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaGh = gioHang.MaGh,
                            LoaiSanPham = 2,
                            MaCb = ct.MaCb.Value,
                            SoLuong = soLuongThem,
                            DonGiaTamTinh = donGiaCombo,
                            NgayThem = DateTime.Now
                        });
                    }
                }
                else if (ct.LoaiSanPham == 3 && ct.MaHopTc.HasValue)
                {
                    var hopCu = await _db.HopQuaTuChons
                        .AsNoTracking()
                        .Include(h => h.ChiTietHopTuChons)
                        .FirstOrDefaultAsync(h => h.MaHopTc == ct.MaHopTc.Value);

                    if (hopCu != null)
                    {
                        var hopMoi = new HopQuaTuChon
                        {
                            MaVh = hopCu.MaVh,
                            MaNd = maNd,
                            MaPhienKvl = maPhienKvl,
                            TenHopKhachDat = hopCu.TenHopKhachDat,
                            MaBamCauHinh = hopCu.MaBamCauHinh,
                            TongCaloHop = hopCu.TongCaloHop,
                            TongGiaMotHop = hopCu.TongGiaMotHop,
                            DaLuuMauYeuThich = false,
                            NgayTao = DateTime.Now
                        };

                        _db.HopQuaTuChons.Add(hopMoi);
                        await _db.SaveChangesAsync();

                        foreach (var oBanhCu in hopCu.ChiTietHopTuChons.OrderBy(o => o.ViTriOtrongHop))
                        {
                            _db.ChiTietHopTuChons.Add(new ChiTietHopTuChon
                            {
                                MaHopTc = hopMoi.MaHopTc,
                                MaBanh = oBanhCu.MaBanh,
                                ViTriOtrongHop = oBanhCu.ViTriOtrongHop,
                                DonGiaBanhLucChon = oBanhCu.DonGiaBanhLucChon
                            });
                        }

                        _db.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaGh = gioHang.MaGh,
                            LoaiSanPham = 3,
                            MaHopTc = hopMoi.MaHopTc,
                            SoLuong = soLuongThem,
                            DonGiaTamTinh = hopMoi.TongGiaMotHop,
                            NgayThem = DateTime.Now
                        });
                    }
                }
            }

            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                thanhCong = true,
                thongBao = "Đã thêm toàn bộ sản phẩm của đơn cũ vào giỏ quà!"
            });
        }

        // 5. API LẤY THÔNG TIN GIỎ HÀNG JSON
        [HttpGet]
        public async Task<IActionResult> LayThongTinGioHangJson()
        {
            await DamBaoMaKhuyenMaiMauConHanAsync();

            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            var kmGoiY = await _db.MaKhuyenMais.AsNoTracking().FirstOrDefaultAsync(k => k.CodeKm == "TRANGTRON2026");
            string thongTinVoucherGoiY = kmGoiY != null
                ? $"HSD: {kmGoiY.NgayKetThuc:dd/MM/yyyy} • Đơn từ {(kmGoiY.DonToiThieu / 1000m):N0}k"
                : "HSD: 31/12/2027 • Đơn từ 200k";

            GioHang? gioHang = null;

            if (maNd.HasValue)
            {
                gioHang = await _db.GioHangs
                    .AsNoTracking()
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                            .ThenInclude(b => b!.ThongSoDinhDuong)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                            .ThenInclude(b => b!.LoSanXuatBanhs)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaCbNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.MaVhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.ChiTietHopTuChons)
                                .ThenInclude(cth => cth.MaBanhNavigation)
                                    .ThenInclude(b => b!.LoSanXuatBanhs)
                    .FirstOrDefaultAsync(g => g.MaNd == maNd.Value);
            }
            else if (!string.IsNullOrEmpty(maPhienKvl))
            {
                gioHang = await _db.GioHangs
                    .AsNoTracking()
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                            .ThenInclude(b => b!.ThongSoDinhDuong)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                            .ThenInclude(b => b!.LoSanXuatBanhs)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaCbNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.MaVhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.ChiTietHopTuChons)
                                .ThenInclude(cth => cth.MaBanhNavigation)
                                    .ThenInclude(b => b!.LoSanXuatBanhs)
                    .FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl);
            }

            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return Json(new
                {
                    thanhCong = true,
                    tongSoLuong = 0,
                    tongTamTinh = 0m,
                    codeKmDangDung = (string?)null,
                    thongTinVoucherGoiY = thongTinVoucherGoiY,
                    tienGiamVoucher = 0m,
                    tongThanhToanSauGiam = 0m,
                    danhSachMon = new List<object>()
                });
            }

            var danhSachMon = gioHang.ChiTietGioHangs
                .OrderByDescending(ct => ct.NgayThem)
                .Select(ct =>
                {
                    string tenMon = "Sản phẩm Trung Thu";
                    string? maBamCauHinh = null;
                    int tongCalo = 0;
                    var danhSachViBanh = new List<string>();
                    decimal donGiaCsdl = TinhDonGiaThucTeTuCsdl(ct);

                    if (ct.LoaiSanPham == 1 && ct.MaBanhNavigation != null)
                    {
                        tenMon = ct.MaBanhNavigation.TenBanh;
                        tongCalo = ct.MaBanhNavigation.ThongSoDinhDuong?.NangLuongCalo ?? 400;
                    }
                    else if (ct.LoaiSanPham == 2 && ct.MaCbNavigation != null)
                    {
                        tenMon = ct.MaCbNavigation.TenCombo;
                    }
                    else if (ct.LoaiSanPham == 3 && ct.MaHopTcNavigation != null)
                    {
                        tenMon = ct.MaHopTcNavigation.TenHopKhachDat;
                        maBamCauHinh = ct.MaHopTcNavigation.MaBamCauHinh;
                        tongCalo = ct.MaHopTcNavigation.TongCaloHop;
                        danhSachViBanh = ct.MaHopTcNavigation.ChiTietHopTuChons
                            .OrderBy(x => x.ViTriOtrongHop)
                            .Select(x => $"Ô {x.ViTriOtrongHop}: {x.MaBanhNavigation?.TenBanh ?? "Bánh"}")
                            .ToList();
                    }

                    return new
                    {
                        maCtgh = ct.MaCtgh,
                        loaiSanPham = ct.LoaiSanPham,
                        tenMon = tenMon,
                        maBamCauHinh = maBamCauHinh,
                        tongCalo = tongCalo,
                        danhSachViBanh = danhSachViBanh,
                        soLuong = ct.SoLuong,
                        donGia = donGiaCsdl,
                        thanhTien = ct.SoLuong * donGiaCsdl
                    };
                })
                .ToList();

            int tongSoLuong = danhSachMon.Sum(x => x.soLuong);
            decimal tongTamTinh = danhSachMon.Sum(x => x.thanhTien);

            string? codeKmDangDung = gioHang.CodeKmdangApDung;
            decimal tienGiamVoucher = 0m;

            if (!string.IsNullOrWhiteSpace(codeKmDangDung))
            {
                var km = await _db.MaKhuyenMais
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.CodeKm == codeKmDangDung && k.NgayBatDau <= DateTime.Now && k.NgayKetThuc >= DateTime.Now && k.SldaDung < k.SltoiDa);

                if (km != null && km.TrangThaiKichHoat && (maNd.HasValue || km.ChoPhepKhachVangLai) && tongTamTinh >= km.DonToiThieu)
                {
                    tienGiamVoucher = TinhTienGiamVoucher(km, tongTamTinh);
                }
                else
                {
                    codeKmDangDung = null;
                }
            }

            decimal tongThanhToanSauGiam = Math.Max(0m, tongTamTinh - tienGiamVoucher);

            return Json(new
            {
                thanhCong = true,
                tongSoLuong = tongSoLuong,
                tongTamTinh = tongTamTinh,
                codeKmDangDung = codeKmDangDung,
                thongTinVoucherGoiY = thongTinVoucherGoiY,
                tienGiamVoucher = tienGiamVoucher,
                tongThanhToanSauGiam = tongThanhToanSauGiam,
                danhSachMon = danhSachMon
            });
        }

        // =========================================================================
        // 6. LỚP 1 - API KIỂM TRA TỒN KHO THỰC TẾ CÒN HẠN KHI MỞ NGĂN KÉO GIỎ HÀNG
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> KiemTraTonKhoThucTe()
        {
            var gioHang = await LayHoacTaoGioHangAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return Json(new
                {
                    thanhCong = true,
                    hopLe = true,
                    danhSachLoiTheoMon = new Dictionary<int, string>(),
                    danhSachCanhBao = new List<object>()
                });
            }

            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var nhuCauBanh = new Dictionary<int, (string TenBanh, int SoLuongCan, List<int> DanhSachMaCtgh)>();
            var nhuCauVoHop = new Dictionary<int, (string TenVoHop, int SoLuongCan, int TonKhoVo, List<int> DanhSachMaCtgh)>();

            foreach (var ct in gioHang.ChiTietGioHangs)
            {
                if (ct.LoaiSanPham == 1 && ct.MaBanhNavigation != null)
                {
                    int maBanh = ct.MaBanhNavigation.MaBanh;
                    if (!nhuCauBanh.TryGetValue(maBanh, out var item))
                    {
                        item = (ct.MaBanhNavigation.TenBanh, 0, new List<int>());
                    }
                    item.SoLuongCan += ct.SoLuong;
                    if (!item.DanhSachMaCtgh.Contains(ct.MaCtgh)) item.DanhSachMaCtgh.Add(ct.MaCtgh);
                    nhuCauBanh[maBanh] = item;
                }
                else if (ct.LoaiSanPham == 2 && ct.MaCbNavigation != null)
                {
                    if (ct.MaCbNavigation.MaVhNavigation != null)
                    {
                        var vh = ct.MaCbNavigation.MaVhNavigation;
                        if (!nhuCauVoHop.TryGetValue(vh.MaVh, out var itemVo))
                        {
                            itemVo = (vh.TenVoHop, 0, vh.SltonKhoVo, new List<int>());
                        }
                        itemVo.SoLuongCan += ct.SoLuong;
                        if (!itemVo.DanhSachMaCtgh.Contains(ct.MaCtgh)) itemVo.DanhSachMaCtgh.Add(ct.MaCtgh);
                        nhuCauVoHop[vh.MaVh] = itemVo;
                    }
                    foreach (var ctCb in ct.MaCbNavigation.ChiTietComboMaus)
                    {
                        if (ctCb.MaBanhNavigation != null)
                        {
                            int maBanh = ctCb.MaBanhNavigation.MaBanh;
                            if (!nhuCauBanh.TryGetValue(maBanh, out var item))
                            {
                                item = (ctCb.MaBanhNavigation.TenBanh, 0, new List<int>());
                            }
                            item.SoLuongCan += ctCb.SoLuong * ct.SoLuong;
                            if (!item.DanhSachMaCtgh.Contains(ct.MaCtgh)) item.DanhSachMaCtgh.Add(ct.MaCtgh);
                            nhuCauBanh[maBanh] = item;
                        }
                    }
                }
                else if (ct.LoaiSanPham == 3 && ct.MaHopTcNavigation != null)
                {
                    if (ct.MaHopTcNavigation.MaVhNavigation != null)
                    {
                        var vh = ct.MaHopTcNavigation.MaVhNavigation;
                        if (!nhuCauVoHop.TryGetValue(vh.MaVh, out var itemVo))
                        {
                            itemVo = (vh.TenVoHop, 0, vh.SltonKhoVo, new List<int>());
                        }
                        itemVo.SoLuongCan += ct.SoLuong;
                        if (!itemVo.DanhSachMaCtgh.Contains(ct.MaCtgh)) itemVo.DanhSachMaCtgh.Add(ct.MaCtgh);
                        nhuCauVoHop[vh.MaVh] = itemVo;
                    }
                    foreach (var ctHop in ct.MaHopTcNavigation.ChiTietHopTuChons)
                    {
                        if (ctHop.MaBanhNavigation != null)
                        {
                            int maBanh = ctHop.MaBanhNavigation.MaBanh;
                            if (!nhuCauBanh.TryGetValue(maBanh, out var item))
                            {
                                item = (ctHop.MaBanhNavigation.TenBanh, 0, new List<int>());
                            }
                            item.SoLuongCan += ct.SoLuong;
                            if (!item.DanhSachMaCtgh.Contains(ct.MaCtgh)) item.DanhSachMaCtgh.Add(ct.MaCtgh);
                            nhuCauBanh[maBanh] = item;
                        }
                    }
                }
            }

            var danhSachMaBanhCanKiemTra = nhuCauBanh.Keys.ToList();
            var tonKhoLoTheoBanh = await _db.LoSanXuatBanhs
                .AsNoTracking()
                .Where(l => danhSachMaBanhCanKiemTra.Contains(l.MaBanh) &&
                            l.HanSuDung > homNay &&
                            (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "XA_CAN_DATE") &&
                            l.SlconLai > 0)
                .GroupBy(l => l.MaBanh)
                .Select(g => new { MaBanh = g.Key, TongTonConHan = g.Sum(x => x.SlconLai) })
                .ToDictionaryAsync(x => x.MaBanh, x => x.TongTonConHan);

            var tonKhoSanPhamBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Where(b => danhSachMaBanhCanKiemTra.Contains(b.MaBanh))
                .ToDictionaryAsync(b => b.MaBanh, b => b.TrangThaiBan ? b.TongTonKho : 0);

            var danhSachLoiTheoMon = new Dictionary<int, string>();
            var danhSachCanhBao = new List<object>();

            foreach (var kvp in nhuCauBanh)
            {
                int maBanh = kvp.Key;
                var info = kvp.Value;
                int tonLo = tonKhoLoTheoBanh.TryGetValue(maBanh, out int slLo) ? slLo : 0;
                int tonSp = tonKhoSanPhamBanh.TryGetValue(maBanh, out int slSp) ? slSp : 0;
                int tonThucTe = Math.Min(tonSp, tonLo);

                if (tonThucTe < info.SoLuongCan)
                {
                    string thongBao = tonThucTe <= 0
                        ? $"Vị bánh \"{info.TenBanh}\" vừa hết hàng trong kho (cần {info.SoLuongCan} bánh)."
                        : $"Vị bánh \"{info.TenBanh}\" chỉ còn {tonThucTe} bánh còn hạn trong kho (đang chọn {info.SoLuongCan} bánh).";

                    danhSachCanhBao.Add(new
                    {
                        maBanh = maBanh,
                        tenBanh = info.TenBanh,
                        soLuongCan = info.SoLuongCan,
                        tonKhoThucTe = tonThucTe,
                        thongBao = thongBao
                    });

                    foreach (int maCtgh in info.DanhSachMaCtgh)
                    {
                        if (danhSachLoiTheoMon.ContainsKey(maCtgh))
                            danhSachLoiTheoMon[maCtgh] += " • " + thongBao;
                        else
                            danhSachLoiTheoMon[maCtgh] = thongBao;
                    }
                }
            }

            foreach (var kvpVo in nhuCauVoHop)
            {
                var infoVo = kvpVo.Value;
                if (infoVo.TonKhoVo < infoVo.SoLuongCan)
                {
                    string thongBao = $"Mẫu vỏ hộp \"{infoVo.TenVoHop}\" chỉ còn {infoVo.TonKhoVo} vỏ trong kho (cần {infoVo.SoLuongCan} hộp).";
                    danhSachCanhBao.Add(new
                    {
                        maBanh = 0,
                        tenBanh = infoVo.TenVoHop,
                        soLuongCan = infoVo.SoLuongCan,
                        tonKhoThucTe = infoVo.TonKhoVo,
                        thongBao = thongBao
                    });

                    foreach (int maCtgh in infoVo.DanhSachMaCtgh)
                    {
                        if (danhSachLoiTheoMon.ContainsKey(maCtgh))
                            danhSachLoiTheoMon[maCtgh] += " • " + thongBao;
                        else
                            danhSachLoiTheoMon[maCtgh] = thongBao;
                    }
                }
            }

            return Json(new
            {
                thanhCong = true,
                hopLe = danhSachCanhBao.Count == 0,
                danhSachLoiTheoMon = danhSachLoiTheoMon,
                danhSachCanhBao = danhSachCanhBao
            });
        }
    }
}

