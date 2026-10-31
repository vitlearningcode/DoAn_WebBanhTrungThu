using System.Text.Json;
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

        public ThanhToanController(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        private async Task<GioHang?> LayGioHangDayDuAsync()
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            if (maNd.HasValue)
            {
                return await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaCbNavigation)
                            .ThenInclude(cb => cb!.MaVhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaCbNavigation)
                            .ThenInclude(cb => cb!.ChiTietComboMaus)
                                .ThenInclude(ctcb => ctcb.MaBanhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.MaVhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.ChiTietHopTuChons)
                                .ThenInclude(cth => cth.MaBanhNavigation)
                    .FirstOrDefaultAsync(g => g.MaNd == maNd.Value);
            }

            if (!string.IsNullOrWhiteSpace(maPhienKvl))
            {
                return await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaCbNavigation)
                            .ThenInclude(cb => cb!.MaVhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaCbNavigation)
                            .ThenInclude(cb => cb!.ChiTietComboMaus)
                                .ThenInclude(ctcb => ctcb.MaBanhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.MaVhNavigation)
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                            .ThenInclude(h => h!.ChiTietHopTuChons)
                                .ThenInclude(cth => cth.MaBanhNavigation)
                    .FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl);
            }

            return null;
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

            var danhSachMon = gioHang.ChiTietGioHangs.OrderByDescending(c => c.NgayThem).ToList();
            decimal tongTienHang = danhSachMon.Sum(c => c.SoLuong * c.DonGiaTamTinh);

            // Tính tổng số hộp quà (Combo mẫu LoaiSanPham == 2 hoặc Hộp tự chọn LoaiSanPham == 3)
            int tongSoHop = danhSachMon
                .Where(c => c.LoaiSanPham == 2 || c.LoaiSanPham == 3)
                .Sum(c => c.SoLuong);

            // Đối chiếu bảng chiết khấu sỉ B2B
            var bangB2B = await _db.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
            var mocB2B = bangB2B.FirstOrDefault(b => tongSoHop >= b.SlhopToiThieu && tongSoHop <= b.SlhopToiDa)
                         ?? (tongSoHop > 0 && bangB2B.Any() && tongSoHop > bangB2B.Last().SlhopToiDa ? bangB2B.Last() : null);

            decimal phanTramCkB2B = mocB2B?.PhanTramChietKhau ?? 0m;
            decimal tienChietKhauB2B = phanTramCkB2B > 0 ? Math.Round(tongTienHang * (phanTramCkB2B / 100m), 0) : 0m;
            bool mienPhiInLogo = mocB2B?.MienPhiInLogo ?? false;
            decimal phiInLogoMacDinh = mocB2B?.PhiKhuonInLogo ?? (bangB2B.FirstOrDefault()?.PhiKhuonInLogo ?? 500000m);

            // Tính lại giảm giá Voucher nếu có
            decimal tienGiamVoucher = 0m;
            string? codeKmDangDung = gioHang.CodeKmdangApDung;
            if (!string.IsNullOrWhiteSpace(codeKmDangDung))
            {
                var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k =>
                    k.CodeKm == codeKmDangDung &&
                    k.NgayKetThuc >= DateTime.Now &&
                    k.SldaDung < k.SltoiDa);

                if (km != null && (maNd.HasValue || km.ChoPhepKhachVangLai) && tongTienHang >= km.DonToiThieu)
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
                }
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
        // 2. XỬ LÝ ĐẶT HÀNG (TRANSACTION, ĐA ĐIỂM NHẬN, TOKEN QR 3D, TRỪ TỒN KHO)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(TaoDonHangRequest req)
        {
            var gioHang = await LayGioHangDayDuAsync();
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return RedirectToAction("Index", "Home");
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
                        CoKemThiepQr = true,
                        LoiChucThiep = "Kính chúc mùa Trăng Rằm Đoàn Viên & An Khang!",
                        MauGiaoDienThiep = "CO_PHONG_HOANG_GIA"
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

                // 2. Tính toán tài chính chuẩn xác
                var danhSachMon = gioHang.ChiTietGioHangs.ToList();
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

                decimal tienGiamVoucher = 0m;
                if (!string.IsNullOrWhiteSpace(gioHang.CodeKmdangApDung))
                {
                    var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k =>
                        k.CodeKm == gioHang.CodeKmdangApDung &&
                        k.NgayKetThuc >= DateTime.Now &&
                        k.SldaDung < k.SltoiDa);

                    if (km != null && (maNd.HasValue || km.ChoPhepKhachVangLai) && tongTienHang >= km.DonToiThieu)
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

                decimal tongPhiVanChuyen = req.DanhSachDotGiao.Count * 30000m;
                decimal tongThanhToanCuoi = Math.Max(0m, tongTienHang - tienChietKhauB2B - tienGiamVoucher + phiInLogo + tongPhiVanChuyen);

                // 3. Lưu bản ghi DonHang
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
                    PhuongThucTt = string.IsNullOrWhiteSpace(req.PhuongThucTt) ? "COD" : req.PhuongThucTt,
                    TrangThaiTt = req.PhuongThucTt == "COD" ? "CHUA_TT" : "CHO_XAC_MINH_CK",
                    TrangThaiDh = "CHO_XAC_NHAN",
                    NgayDat = DateTime.Now
                };

                _db.DonHangs.Add(donHang);
                await _db.SaveChangesAsync();

                // 4. Lưu danh sách DotGiaoHangDaDiems kèm MaTokenThiepQr
                var cacDotDaLuu = new List<DotGiaoHangDaDiem>();
                foreach (var dotInput in req.DanhSachDotGiao)
                {
                    string? tokenQr = null;
                    if (dotInput.CoKemThiepQr)
                    {
                        tokenQr = "QR-" + Guid.NewGuid().ToString("N").ToUpper()[..12];
                    }

                    var dotEntity = new DotGiaoHangDaDiem
                    {
                        MaDh = donHang.MaDh,
                        TenNguoiNhan = string.IsNullOrWhiteSpace(dotInput.TenNguoiNhan) ? donHang.HoTenNguoiMua : dotInput.TenNguoiNhan.Trim(),
                        SdtnguoiNhan = string.IsNullOrWhiteSpace(dotInput.SdtNguoiNhan) ? donHang.SdtnguoiMua : dotInput.SdtNguoiNhan.Trim(),
                        DiaChiGiaoChiTiet = string.IsNullOrWhiteSpace(dotInput.DiaChiGiaoChiTiet) ? "Địa chỉ nhận quà Trung Thu" : dotInput.DiaChiGiaoChiTiet.Trim(),
                        NgayHenGiao = dotInput.NgayHenGiao ?? DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                        KhungGioGiao = string.IsNullOrWhiteSpace(dotInput.KhungGioGiao) ? "Sáng (08:00 - 11:30)" : dotInput.KhungGioGiao,
                        PhiShipDotNay = 30000m,
                        TrangThaiGiao = "CHO_DONG_GOI",
                        CoKemThiepQr = dotInput.CoKemThiepQr,
                        MaTokenThiepQr = tokenQr,
                        LoiChucThiep = dotInput.CoKemThiepQr
                            ? (string.IsNullOrWhiteSpace(dotInput.LoiChucThiep)
                                ? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!"
                                : dotInput.LoiChucThiep.Trim())
                            : null,
                        MauGiaoDienThiep = string.IsNullOrWhiteSpace(dotInput.MauGiaoDienThiep) ? "CO_PHONG_HOANG_GIA" : dotInput.MauGiaoDienThiep,
                        SoLanMoThiep = 0
                    };

                    _db.DotGiaoHangDaDiems.Add(dotEntity);
                    cacDotDaLuu.Add(dotEntity);
                }
                await _db.SaveChangesAsync();

                int? maDotDauTien = cacDotDaLuu.FirstOrDefault()?.MaDotGh;

                // 5. Lưu ChiTietDonHangs & Trừ tồn kho SanPhamBanh, MauVoHop
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
                        ctGio.MaBanhNavigation.TongTonKho = Math.Max(0, ctGio.MaBanhNavigation.TongTonKho - ctGio.SoLuong);
                    }
                    else if (ctGio.LoaiSanPham == 2 && ctGio.MaCbNavigation != null)
                    {
                        tenLuuCung = ctGio.MaCbNavigation.TenCombo;
                        if (ctGio.MaCbNavigation.MaVhNavigation != null)
                        {
                            ctGio.MaCbNavigation.MaVhNavigation.SltonKhoVo = Math.Max(0, ctGio.MaCbNavigation.MaVhNavigation.SltonKhoVo - ctGio.SoLuong);
                        }
                        foreach (var ctCombo in ctGio.MaCbNavigation.ChiTietComboMaus)
                        {
                            if (ctCombo.MaBanhNavigation != null)
                            {
                                int slTru = ctCombo.SoLuong * ctGio.SoLuong;
                                ctCombo.MaBanhNavigation.TongTonKho = Math.Max(0, ctCombo.MaBanhNavigation.TongTonKho - slTru);
                            }
                        }
                    }
                    else if (ctGio.LoaiSanPham == 3 && ctGio.MaHopTcNavigation != null)
                    {
                        var hopTc = ctGio.MaHopTcNavigation;
                        tenLuuCung = $"{hopTc.TenHopKhachDat} ({hopTc.MaBamCauHinh})";

                        if (hopTc.MaVhNavigation != null)
                        {
                            hopTc.MaVhNavigation.SltonKhoVo = Math.Max(0, hopTc.MaVhNavigation.SltonKhoVo - ctGio.SoLuong);
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

                        cauHinhJson = JsonSerializer.Serialize(dsBanhTrongHop);

                        foreach (var ctHop in hopTc.ChiTietHopTuChons)
                        {
                            if (ctHop.MaBanhNavigation != null)
                            {
                                ctHop.MaBanhNavigation.TongTonKho = Math.Max(0, ctHop.MaBanhNavigation.TongTonKho - ctGio.SoLuong);
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

                // 6. Cộng điểm tích lũy cho Member (10.000đ = 1 điểm) & Nâng hạng tự động nếu đủ điều kiện
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

                // 7. Xóa các dòng trong ChiTietGioHangs sau khi đặt thành công
                _db.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = DateTime.Now;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("ThanhCong", new { maDon = donHang.MaTraCuuDh });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =========================================================================
        // 3. TRANG ĐẶT HÀNG THÀNH CÔNG & SINH MÃ QR 3D BẰNG QRCODER (BASE64 PNG)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ThanhCong(string maDon)
        {
            if (string.IsNullOrWhiteSpace(maDon))
            {
                return RedirectToAction("Index", "Home");
            }

            var donHang = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maDon.Trim());

            if (donHang == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var vm = new DonHangThanhCongViewModel
            {
                DonHang = donHang,
                DiemCongThem = donHang.MaNd.HasValue ? (int)(donHang.TongThanhToanCuoi / 10000m) : 0
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

            return View(vm);
        }
    }
}

