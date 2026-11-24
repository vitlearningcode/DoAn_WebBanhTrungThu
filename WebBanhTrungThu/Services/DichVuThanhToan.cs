using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;
using EntGioHang = WebBanhTrungThu.Models.Entities.GioHang;

namespace WebBanhTrungThu.Services
{
    public class KetQuaXuLyDatHang
    {
        public bool ThanhCong { get; set; }
        public string? TenSanPhamHetHang { get; set; }
        public string? ThongBaoLoi { get; set; }
        public DonHang? DonHangDaTao { get; set; }
        public bool LaThanhToanQrMoMo { get; set; }
    }

    /// <summary>
    /// Dịch vụ điều phối quy trình Thanh toán, tạo DonHang trong ACID Transaction,
    /// ghi ThiepChucMung3D và dựng dữ liệu trang biên lai ThanhCong.
    /// </summary>
    public class DichVuThanhToan : DichVuCoSo, IDichVuThanhToan
    {
        private readonly IDichVuCauHinhHeThong _kiotMeta;
        private readonly IDichVuTinhGiaDonHang _dichVuTinhGia;
        private readonly IDichVuTonKhoFefo _dichVuTonKho;
        private readonly IDichVuKhuyenMaiGioHang _dichVuKhuyenMai;
        private readonly IDichVuCongThanhToanMoMo _dichVuCongMoMo;

        public DichVuThanhToan(
            CSDLBanhTrungThuContext context,
            IDichVuCauHinhHeThong kiotMeta,
            IDichVuTinhGiaDonHang dichVuTinhGia,
            IDichVuTonKhoFefo dichVuTonKho,
            IDichVuKhuyenMaiGioHang dichVuKhuyenMai,
            IDichVuCongThanhToanMoMo dichVuCongMoMo) : base(context)
        {
            _kiotMeta = kiotMeta;
            _dichVuTinhGia = dichVuTinhGia;
            _dichVuTonKho = dichVuTonKho;
            _dichVuKhuyenMai = dichVuKhuyenMai;
            _dichVuCongMoMo = dichVuCongMoMo;
        }

        public Task<KetQuaKiemTraTonKhoGioHang> KiemTraTonKhoGioHangAsync(EntGioHang gioHang)
            => _dichVuTonKho.KiemTraTonKhoGioHangAsync(gioHang);

        public async Task<EntGioHang?> LayGioHangDayDuAsync(int? maNd, string? maPhienKvl)
        {
            IQueryable<EntGioHang> truyVan = _context.GioHangs
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation).ThenInclude(cb => cb!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation).ThenInclude(cb => cb!.ChiTietComboMaus).ThenInclude(ctcb => ctcb.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.ChiTietHopTuChons).ThenInclude(cth => cth.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs);

            if (maNd.HasValue) return await truyVan.FirstOrDefaultAsync(g => g.MaNd == maNd.Value);
            if (!string.IsNullOrWhiteSpace(maPhienKvl)) return await truyVan.FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl);
            return null;
        }

        public async Task<(TrangThanhToanViewModel? ViewModel, string? TenBanhHetHang, string? ThongBaoHetHang)> XayDungDuLieuTrangThanhToanAsync(int? maNd, string? maPhienKvl)
        {
            var gioHang = await LayGioHangDayDuAsync(maNd, maPhienKvl);
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any()) return (null, null, null);

            await _dichVuKhuyenMai.DamBaoMaKhuyenMaiMauConHanAsync();
            var chiPhi = await _dichVuTinhGia.TinhToanChiPhiDonHangAsync(gioHang, maNd, luuCapNhatGiaGioHang: true);
            DateTime bayGio = LayThoiGianHeThong();
            var dsVoucher = await _context.MaKhuyenMais.AsNoTracking()
                .Where(k => k.NgayBatDau <= bayGio && k.NgayKetThuc >= bayGio && k.SldaDung < k.SltoiDa)
                .OrderBy(k => k.DonToiThieu).ThenByDescending(k => k.GiaTriGiam).Take(4).ToListAsync();
            var kqKho = await _dichVuTonKho.KiemTraTonKhoGioHangAsync(gioHang);

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
                DanhSachVoucherGoiY = dsVoucher,
                DanhSachCanhBaoTonKho = kqKho.DanhSachCanhBao,
                DiemTichLuy = chiPhi.DiemTichLuyHienCo,
                HangTv = chiPhi.HangTv,
                PhanTramGiamVip = chiPhi.PhanTramGiamVip,
                TienGiamVip = chiPhi.TienGiamVip,
                DaDangNhap = maNd.HasValue,
                MaPhienHienTai = maNd.HasValue ? $"MEMBER #{maNd.Value}" : (maPhienKvl ?? "GUEST")
            };

            if (maNd.HasValue)
            {
                var nd = await _context.NguoiDungs.AsNoTracking().Include(u => u.SoDiaChis).FirstOrDefaultAsync(u => u.MaNd == maNd.Value);
                if (nd != null)
                {
                    vm.HoTenNguoiMua = nd.HoTen;
                    vm.SdtNguoiMua = nd.Sdt;
                    vm.EmailNguoiMua = nd.Email;
                    vm.DanhSachSoDiaChi = nd.SoDiaChis.OrderByDescending(d => d.LaMacDinh).ToList();
                }
            }

            return (vm, kqKho.HopLe ? null : kqKho.TenSanPhamThieu, kqKho.HopLe ? null : kqKho.ThongBaoChiTiet);
        }

        public async Task GoMaKhuyenMaiGioHangAsync(int? maNd, string? maPhienKvl)
        {
            var gioHang = await LayGioHangDayDuAsync(maNd, maPhienKvl);
            if (gioHang != null && !string.IsNullOrWhiteSpace(gioHang.CodeKmdangApDung))
            {
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = LayThoiGianHeThong();
                await _context.SaveChangesAsync();
            }
        }

        public async Task<KetQuaXuLyDatHang> XuLyDatHangAsync(
            EntGioHang gioHang,
            TaoDonHangRequest req,
            int? maNd,
            string? maPhienKvl,
            string hinhThucNhanHang,
            string tocDoGiao)
        {
            bool laNhanTaiCuaHangReq = string.Equals(hinhThucNhanHang, "NHAN_TAI_CUA_HANG", StringComparison.OrdinalIgnoreCase);

            if (req.DanhSachDotGiao == null || !req.DanhSachDotGiao.Any())
            {
                if (!laNhanTaiCuaHangReq)
                    return new KetQuaXuLyDatHang { ThanhCong = false, ThongBaoLoi = "Vui lòng nhập đầy đủ thông tin và địa chỉ giao hàng!" };

                req.DanhSachDotGiao = new List<DotGiaoHangInput>
                {
                    new() { TenNguoiNhan = req.HoTenNguoiMua, SdtNguoiNhan = req.SdtNguoiMua, DiaChiGiaoChiTiet = "Nhận tại Cửa hàng Nguyệt Cát", NgayHenGiao = DateOnly.FromDateTime(DateTime.Now.AddDays(2)), KhungGioGiao = "Sáng (08:00 - 11:30)", CoKemThiepQr = req.CoThiep3D }
                };
            }
            else if (!laNhanTaiCuaHangReq && req.DanhSachDotGiao.Any(d => string.IsNullOrWhiteSpace(d.DiaChiGiaoChiTiet)))
            {
                return new KetQuaXuLyDatHang { ThanhCong = false, ThongBaoLoi = "Vui lòng nhập đầy đủ địa chỉ nhận hàng!" };
            }

            using var giaoDichDb = await _context.Database.BeginTransactionAsync();
            try
            {
                var ketQuaKho = await _dichVuTonKho.KiemTraTonKhoGioHangAsync(gioHang);
                if (!ketQuaKho.HopLe)
                {
                    await giaoDichDb.RollbackAsync();
                    return new KetQuaXuLyDatHang { ThanhCong = false, TenSanPhamHetHang = ketQuaKho.TenSanPhamThieu ?? "Bánh Trung Thu", ThongBaoLoi = ketQuaKho.ThongBaoChiTiet };
                }

                string maTraCuu;
                do { maTraCuu = "TT2026-" + Random.Shared.Next(1000, 9999); }
                while (await _context.DonHangs.AnyAsync(d => d.MaTraCuuDh == maTraCuu));

                var chiPhi = await _dichVuTinhGia.TinhToanChiPhiDonHangAsync(
                    gioHang, maNd, req.LaDonDoanhNghiepB2B, req.YeuCauInLogo, hinhThucNhanHang, tocDoGiao,
                    req.DanhSachDotGiao, capNhatLuotDungVoucher: true, luuCapNhatGiaGioHang: false,
                    dungDiemTichLuy: req.SuDungDiemTichLuy || req.DungDiemTichLuy);

                if (chiPhi.LaNhanTaiCuaHang && req.DanhSachDotGiao.Count > 1) req.DanhSachDotGiao = req.DanhSachDotGiao.Take(1).ToList();

                string ptRaw = ChuanHoaChuoi(req.PhuongThucTt, "COD").ToUpperInvariant();
                bool laQrMoMo = ptRaw is "MOMO_QR" or "VIETQR" or "MOMO";
                DateTime thoiDiemDat = LayThoiGianHeThong();

                var donHang = new DonHang
                {
                    MaTraCuuDh = maTraCuu, MaNd = maNd, MaPhienKvl = maPhienKvl,
                    HoTenNguoiMua = ChuanHoaChuoi(req.HoTenNguoiMua, "Khách Hàng Nguyệt Cát"),
                    SdtnguoiMua = ChuanHoaChuoi(req.SdtNguoiMua, "0909000000"),
                    EmailNguoiMua = string.IsNullOrWhiteSpace(req.EmailNguoiMua) ? null : req.EmailNguoiMua.Trim(),
                    LaDonDoanhNghiepB2b = req.LaDonDoanhNghiepB2B,
                    TenCongTy = req.LaDonDoanhNghiepB2B ? req.TenCongTy?.Trim() : null,
                    MaSoThue = req.LaDonDoanhNghiepB2B ? req.MaSoThue?.Trim() : null,
                    TongTienHang = chiPhi.TongTienHang, PhanTramCkB2b = chiPhi.PhanTramCkB2B, TienChietKhauB2b = chiPhi.TienChietKhauB2B,
                    TienGiamVoucher = chiPhi.TienGiamVoucher, PhiInLogo = chiPhi.PhiInLogo, TongPhiVanChuyen = chiPhi.PhiVanChuyen,
                    TongThanhToanCuoi = chiPhi.TongThanhToanCuoi,
                    TienGiamHangVip = Math.Round(chiPhi.TienGiamVip, 0), PhanTramGiamVip = (int)Math.Round(chiPhi.PhanTramGiamVip, 0),
                    SoDiemDaDung = chiPhi.DiemTichLuySeDung, TienGiamDiemVip = Math.Round(chiPhi.TienGiamDiem, 0),
                    HanThanhToanQr = laQrMoMo ? thoiDiemDat.AddMinutes(15) : null,
                    PhuongThucTt = laQrMoMo ? "MOMO_QR" : "COD",
                    TrangThaiTt = laQrMoMo ? "CHO_THANH_TOAN" : "CHUA_TT",
                    TrangThaiDh = "CHO_XAC_NHAN", NgayDat = thoiDiemDat,
                    KenhBan = "WEB_ONLINE", DaRollbackKho = false,
                    MaKm = chiPhi.MaKm, MaMocCk = chiPhi.MaMocCk
                };

                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync();

                var cacDotDaLuu = new List<DotGiaoHangDaDiem>();
                var mapThiepMeta = new Dictionary<string, ThongTinThiep3DMeta>(StringComparer.OrdinalIgnoreCase);

                foreach (var dotInput in req.DanhSachDotGiao)
                {
                    bool coThiep = req.CoThiep3D || dotInput.CoKemThiepQr;
                    string? tokenQr = coThiep ? "QR-" + Guid.NewGuid().ToString("N").ToUpperInvariant()[..12] : null;
                    string? loiChuc = coThiep ? ChuanHoaChuoi(dotInput.LoiChucThiep, "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!") : null;
                    string? mauGd = coThiep ? ChuanHoaChuoi(dotInput.MauGiaoDienThiep, "CO_PHONG_HOANG_GIA") : null;

                    if (coThiep && tokenQr != null)
                    {
                        mapThiepMeta[tokenQr] = new ThongTinThiep3DMeta
                        {
                            MaTokenThiepQr = tokenQr,
                            TenNguoiNhanThiep = !string.IsNullOrWhiteSpace(dotInput.TenNguoiNhanThiep) ? dotInput.TenNguoiNhanThiep.Trim() : ChuanHoaChuoi(dotInput.TenNguoiNhan, "Quý Khách & Gia Đình"),
                            TenNguoiGuiThiep = ChuanHoaChuoi(dotInput.TenNguoiGuiThiep, donHang.HoTenNguoiMua),
                            LoiChucThiep = loiChuc!,
                            MauGiaoDienThiep = mauGd!
                        };
                    }

                    var dotEntity = new DotGiaoHangDaDiem
                    {
                        MaDh = donHang.MaDh,
                        TenNguoiNhan = ChuanHoaChuoi(dotInput.TenNguoiNhan, donHang.HoTenNguoiMua),
                        SdtnguoiNhan = ChuanHoaChuoi(dotInput.SdtNguoiNhan, donHang.SdtnguoiMua),
                        DiaChiGiaoChiTiet = ChuanHoaChuoi(dotInput.DiaChiGiaoChiTiet, "Địa chỉ nhận quà Trung Thu"),
                        NgayHenGiao = (dotInput.NgayHenGiao.HasValue && dotInput.NgayHenGiao.Value >= LayNgayHienTai()) ? dotInput.NgayHenGiao.Value : LayNgayHienTai().AddDays(1),
                        KhungGioGiao = ChuanHoaChuoi(dotInput.KhungGioGiao, "Sáng (08:00 - 11:30)"),
                        PhiShipDotNay = chiPhi.LaNhanTaiCuaHang ? 0m : chiPhi.PhiMoiDotGiao,
                        TrangThaiGiao = "CHO_DONG_GOI", CoKemThiepQr = coThiep, MaTokenThiepQr = tokenQr,
                        LoiChucThiep = loiChuc, MauGiaoDienThiep = mauGd
                    };
                    _context.DotGiaoHangDaDiems.Add(dotEntity);
                    cacDotDaLuu.Add(dotEntity);
                }
                await _context.SaveChangesAsync();

                foreach (var dot in cacDotDaLuu.Where(d => d.CoKemThiepQr && !string.IsNullOrWhiteSpace(d.MaTokenThiepQr)))
                {
                    mapThiepMeta.TryGetValue(dot.MaTokenThiepQr!, out var tm);
                    _context.ThiepChucMung3ds.Add(new ThiepChucMung3d
                    {
                        MaTokenQr = dot.MaTokenThiepQr!, MaDonHang = donHang.MaDh, MaDotGiao = dot.MaDotGh,
                        TenNguoiGui = tm?.TenNguoiGuiThiep ?? donHang.HoTenNguoiMua,
                        TenNguoiNhan = tm?.TenNguoiNhanThiep ?? dot.TenNguoiNhan,
                        LoiChuc = dot.LoiChucThiep ?? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!",
                        MauGiaoDien = dot.MauGiaoDienThiep ?? "CO_PHONG_HOANG_GIA",
                        NgayTao = LayThoiGianHeThong()
                    });
                }

                var danhSachLoDaTru = new List<NhatKyTruLoFefo>();
                var danhSachVoHopDaTru = new List<NhatKyTruVoHop>();
                var danhSachMon = gioHang.ChiTietGioHangs.ToList();
                var jsonOpt = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

                for (int idx = 0; idx < danhSachMon.Count; idx++)
                {
                    var ctGio = danhSachMon[idx];
                    int? maDotGan = cacDotDaLuu.Any() ? cacDotDaLuu[idx % cacDotDaLuu.Count].MaDotGh : null;
                    string tenLuuCung = "Sản phẩm Nguyệt Cát";
                    string? cauHinhJson = null;

                    if (ctGio.LoaiSanPham == 1 && ctGio.MaBanhNavigation != null)
                    {
                        tenLuuCung = ctGio.MaBanhNavigation.TenBanh;
                        await _dichVuTonKho.TruTonKhoFefoAsync(ctGio.MaBanhNavigation.MaBanh, ctGio.SoLuong, danhSachLoDaTru);
                    }
                    else if (ctGio.LoaiSanPham == 2 && ctGio.MaCbNavigation != null)
                    {
                        tenLuuCung = ctGio.MaCbNavigation.TenCombo;
                        if (ctGio.MaCbNavigation.MaVhNavigation != null)
                            await _dichVuTonKho.TruTonKhoVoHopAsync(ctGio.MaCbNavigation.MaVhNavigation.MaVh, ctGio.SoLuong, danhSachVoHopDaTru);

                        var dsBanhCombo = ctGio.MaCbNavigation.ChiTietComboMaus
                            .OrderBy(c => c.ViTriO)
                            .Select(c => new
                            {
                                ViTriOTrongHop = c.ViTriO,
                                MaBanh = c.MaBanh,
                                TenBanh = c.MaBanhNavigation?.TenBanh ?? "Bánh Trung Thu",
                                DonGia = Math.Round(c.MaBanhNavigation?.DonGiaLe ?? 0m, 0)
                            })
                            .ToList();
                        if (dsBanhCombo.Count > 0)
                        {
                            cauHinhJson = JsonSerializer.Serialize(dsBanhCombo, jsonOpt);
                        }

                        foreach (var ctCombo in ctGio.MaCbNavigation.ChiTietComboMaus.Where(c => c.MaBanhNavigation != null))
                            await _dichVuTonKho.TruTonKhoFefoAsync(ctCombo.MaBanhNavigation!.MaBanh, ctCombo.SoLuong * ctGio.SoLuong, danhSachLoDaTru);
                    }
                    else if (ctGio.LoaiSanPham == 3 && ctGio.MaHopTcNavigation != null)
                    {
                        var hopTc = ctGio.MaHopTcNavigation;
                        tenLuuCung = $"{hopTc.TenHopKhachDat} ({hopTc.MaBamCauHinh})";
                        if (hopTc.MaVhNavigation != null)
                            await _dichVuTonKho.TruTonKhoVoHopAsync(hopTc.MaVhNavigation.MaVh, ctGio.SoLuong, danhSachVoHopDaTru);

                        cauHinhJson = JsonSerializer.Serialize(
                            hopTc.ChiTietHopTuChons.OrderBy(x => x.ViTriOtrongHop).Select(x => new { ViTriOTrongHop = x.ViTriOtrongHop, MaBanh = x.MaBanh, TenBanh = x.MaBanhNavigation?.TenBanh ?? "Bánh Trung Thu", DonGia = x.DonGiaBanhLucChon }),
                            jsonOpt);

                        foreach (var ctHop in hopTc.ChiTietHopTuChons.Where(c => c.MaBanhNavigation != null))
                            await _dichVuTonKho.TruTonKhoFefoAsync(ctHop.MaBanhNavigation!.MaBanh, ctGio.SoLuong, danhSachLoDaTru);
                    }

                    _context.ChiTietDonHangs.Add(new ChiTietDonHang
                    {
                        MaDh = donHang.MaDh, MaDotGh = maDotGan, LoaiSanPham = ctGio.LoaiSanPham,
                        MaBanh = ctGio.MaBanh, MaCb = ctGio.MaCb, MaHopTc = ctGio.MaHopTc,
                        TenSanPhamLuuCung = tenLuuCung, CauHinhHopJson = cauHinhJson,
                        SoLuong = ctGio.SoLuong, DonGiaLucMua = ctGio.DonGiaTamTinh, ThanhTien = ctGio.SoLuong * ctGio.DonGiaTamTinh
                    });
                }

                if (maNd.HasValue)
                {
                    var nd = await _context.NguoiDungs.FindAsync(maNd.Value);
                    if (nd != null)
                    {
                        if (chiPhi.DiemTichLuySeDung > 0) nd.DiemTichLuy = Math.Max(0, nd.DiemTichLuy - chiPhi.DiemTichLuySeDung);
                        var (hangVipMoi, _, _, _) = await DichVuHoiVien.TinhThongTinHangVipAsync(_context, nd.MaNd, nd.DiemTichLuy);
                        nd.HangTv = hangVipMoi;
                    }
                }

                foreach (var lo in danhSachLoDaTru)
                {
                    _context.NhatKyTruKhoDonHangs.Add(new NhatKyTruKhoDonHang
                    {
                        MaDh = donHang.MaDh,
                        LoaiDoiTuong = "LO_BANH",
                        MaLo = lo.MaLo > 0 ? lo.MaLo : null,
                        MaBanh = lo.MaBanh > 0 ? lo.MaBanh : null,
                        SoLuongDaTru = lo.SoLuongDaTru,
                        DaHoanKho = false,
                        NgayGhi = thoiDiemDat
                    });
                }

                foreach (var vo in danhSachVoHopDaTru)
                {
                    _context.NhatKyTruKhoDonHangs.Add(new NhatKyTruKhoDonHang
                    {
                        MaDh = donHang.MaDh,
                        LoaiDoiTuong = "VO_HOP",
                        MaVh = vo.MaVh > 0 ? vo.MaVh : null,
                        SoLuongDaTru = vo.SoLuongDaTru,
                        DaHoanKho = false,
                        NgayGhi = thoiDiemDat
                    });
                }

                _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = LayThoiGianHeThong();
                await _context.SaveChangesAsync();
                await giaoDichDb.CommitAsync();

                return new KetQuaXuLyDatHang { ThanhCong = true, DonHangDaTao = donHang, LaThanhToanQrMoMo = laQrMoMo };
            }
            catch { await giaoDichDb.RollbackAsync(); throw; }
        }

        public async Task<DonHangThanhCongViewModel?> XayDungDuLieuDonHangThanhCongAsync(string maTraCuu, string scheme, string host)
        {
            await _dichVuCongMoMo.KiemTraVaHuyDonQrQuaHanAsync();
            var donHang = await _context.DonHangs.Include(d => d.DotGiaoHangDaDiems).Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu);
            if (donHang == null) return null;

            foreach (var thiep in await _context.ThiepChucMung3ds.AsNoTracking().Where(t => t.MaDonHang == donHang.MaDh).ToListAsync())
            {
                var dot = donHang.DotGiaoHangDaDiems.FirstOrDefault(g => g.MaDotGh == thiep.MaDotGiao) ?? donHang.DotGiaoHangDaDiems.FirstOrDefault(g => g.MaTokenThiepQr == thiep.MaTokenQr);
                if (dot == null) continue;
                dot.CoKemThiepQr = true;
                dot.MaTokenThiepQr ??= thiep.MaTokenQr;
                dot.LoiChucThiep ??= thiep.LoiChuc;
                dot.MauGiaoDienThiep ??= thiep.MauGiaoDien;
                dot.TenNguoiNhanThiep ??= thiep.TenNguoiNhan;
                dot.TenNguoiGuiThiep ??= thiep.TenNguoiGui;
            }

            bool laDonQrMoMo = donHang.PhuongThucTt is "MOMO_QR" or "VIETQR" or "MOMO";
            bool laHuy = donHang.TrangThaiDh == "DA_HUY" || donHang.TrangThaiTt is "HET_HAN_HUY" or "HET_HAN_15P";
            DateTime hanTt = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);
            bool dangChoQr = !laHuy && laDonQrMoMo && donHang.TrangThaiTt is "CHO_THANH_TOAN" or "CHO_XAC_MINH_CK";
            int soGiayConLai = dangChoQr ? Math.Max(0, (int)Math.Ceiling((hanTt - LayThoiGianHeThong()).TotalSeconds)) : 0;

            var qr = _dichVuCongMoMo.TaoDuLieuQrMoMo(donHang, dangChoQr);
            var vm = new DonHangThanhCongViewModel
            {
                DonHang = donHang,
                DiemCongThem = (!laHuy && donHang.MaNd.HasValue) ? (int)(donHang.TongThanhToanCuoi / 10000m) : 0,
                CauHinhQr = qr.CauHinhQr, NoiDungChuyenKhoan = qr.NoiDungCk,
                AnhQrThanhToanBase64 = qr.AnhQrThanhToanBase64, UrlVietQr = qr.UrlVietQr,
                DangChoThanhToanQr = dangChoQr, HanThanhToan = hanTt, SoGiayConLai = soGiayConLai
            };

            using var qrGenerator = new QRCodeGenerator();
            foreach (var dot in donHang.DotGiaoHangDaDiems.Where(d => d.CoKemThiepQr && !string.IsNullOrWhiteSpace(d.MaTokenThiepQr)))
            {
                var metaThiep = _kiotMeta.LayThongTinThiepTheoToken(donHang.MaDh, dot.MaTokenThiepQr);
                if (metaThiep != null) { dot.TenNguoiNhanThiep = metaThiep.TenNguoiNhanThiep; dot.TenNguoiGuiThiep = metaThiep.TenNguoiGuiThiep; }
                string urlThiep = $"{scheme}://{host}/Thiep3D/Xem/{dot.MaTokenThiepQr}";
                using QRCodeData qrData = qrGenerator.CreateQrCode(urlThiep, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrData);
                vm.DanhSachDotGiaoQr.Add(new DotGiaoKemQrItem { DotGiao = dot, UrlThiep3D = urlThiep, AnhQrBase64 = "data:image/png;base64," + Convert.ToBase64String(qrCode.GetGraphic(10)) });
            }

            return vm;
        }
    }
}
