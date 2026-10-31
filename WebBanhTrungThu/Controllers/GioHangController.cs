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
                        .ThenInclude(ct => ct.MaHopTcNavigation)
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
            decimal tongDonGiaCacBanh = req.DanhSachMaBanh.Sum(id => tuDienBanh[id].DonGiaLe);
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
                        DonGiaBanhLucChon = tuDienBanh[maBanhO].DonGiaLe
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
                var banh = await _db.SanPhamBanhs.FirstOrDefaultAsync(b => b.MaBanh == req.MaSanPham && b.TrangThaiBan);
                if (banh == null)
                {
                    return Json(new { thanhCong = false, thongBao = "Sản phẩm bánh không tồn tại hoặc đã ngừng bán!" });
                }

                tenHienThi = banh.TenBanh;
                var dongHienTai = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.LoaiSanPham == 1 && ct.MaBanh == banh.MaBanh);

                if (dongHienTai != null)
                {
                    dongHienTai.SoLuong += soLuongThem;
                    dongHienTai.DonGiaTamTinh = banh.DonGiaLe;
                }
                else
                {
                    _db.ChiTietGioHangs.Add(new ChiTietGioHang
                    {
                        MaGh = gioHang.MaGh,
                        LoaiSanPham = 1,
                        MaBanh = banh.MaBanh,
                        SoLuong = soLuongThem,
                        DonGiaTamTinh = banh.DonGiaLe,
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

            if (km.NgayKetThuc < DateTime.Now)
            {
                return Json(new { thanhCong = false, thongBao = $"Mã giảm giá '{maNhap}' đã hết hạn sử dụng!" });
            }

            if (km.SldaDung >= km.SltoiDa)
            {
                return Json(new { thanhCong = false, thongBao = $"Mã giảm giá '{maNhap}' đã hết lượt sử dụng!" });
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

            decimal tongTamTinh = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong * ct.DonGiaTamTinh);
            if (tongTamTinh < km.DonToiThieu)
            {
                return Json(new
                {
                    thanhCong = false,
                    thongBao = $"Mã '{maNhap}' yêu cầu đơn hàng tối thiểu {km.DonToiThieu:N0} đ (Hiện tại: {tongTamTinh:N0} đ)!"
                });
            }

            decimal tienGiam = TinhTienGiamVoucher(km, tongTamTinh);
            gioHang.CodeKmdangApDung = km.CodeKm;
            gioHang.NgayCapNhat = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                thanhCong = true,
                thongBao = $"Áp dụng mã '{km.CodeKm}' ({km.TenChuongTrinh}) thành công! Giảm {tienGiam:N0} đ.",
                codeKmDangDung = km.CodeKm,
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
        [HttpGet]
        public async Task<IActionResult> LayThongTinGioHangJson()
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            GioHang? gioHang = null;

            if (maNd.HasValue)
            {
                gioHang = await _db.GioHangs
                    .AsNoTracking()
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                            .ThenInclude(b => b!.ThongSoDinhDuong)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaCbNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.ChiTietHopTuChons)
                                .ThenInclude(cth => cth.MaBanhNavigation)
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
                        .ThenInclude(ct => ct.MaCbNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.ChiTietHopTuChons)
                                .ThenInclude(cth => cth.MaBanhNavigation)
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
                        donGia = ct.DonGiaTamTinh,
                        thanhTien = ct.SoLuong * ct.DonGiaTamTinh
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
                    .FirstOrDefaultAsync(k => k.CodeKm == codeKmDangDung && k.NgayKetThuc >= DateTime.Now && k.SldaDung < k.SltoiDa);

                if (km != null && (maNd.HasValue || km.ChoPhepKhachVangLai) && tongTamTinh >= km.DonToiThieu)
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
                tienGiamVoucher = tienGiamVoucher,
                tongThanhToanSauGiam = tongThanhToanSauGiam,
                danhSachMon = danhSachMon
            });
        }
    }
}

