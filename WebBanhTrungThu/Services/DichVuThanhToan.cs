using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;
using GioHang = WebBanhTrungThu.Models.Entities.GioHang;

namespace WebBanhTrungThu.Services
{
    public class KetQuaKiemTraTonKhoGioHang
    {
        public bool HopLe => DanhSachTenBanhThieu.Count == 0 && DanhSachCanhBao.Count == 0;
        public string? TenSanPhamThieu => DanhSachTenBanhThieu.FirstOrDefault();
        public string? ThongBaoChiTiet { get; set; }
        public List<string> DanhSachTenBanhThieu { get; set; } = new();
        public List<string> DanhSachCanhBao { get; set; } = new();
    }

    public class KetQuaTinhChiPhiDonHang
    {
        public List<ChiTietGioHang> DanhSachMon { get; set; } = new();
        public int TongSoHop { get; set; }
        public decimal TongTienHang { get; set; }
        public decimal PhanTramCkB2B { get; set; }
        public decimal TienChietKhauB2B { get; set; }
        public bool MienPhiInLogo { get; set; }
        public decimal PhiInLogoMacDinh { get; set; }
        public decimal PhiInLogo { get; set; }
        public decimal TienGiamVoucher { get; set; }
        public string? CodeKmDangDung { get; set; }
        public string ThongTinVoucherGoiY { get; set; } = string.Empty;
        public string HangTv { get; set; } = "Đồng";
        public decimal PhanTramGiamVip { get; set; }
        public decimal TienGiamVip { get; set; }
        public decimal TongChiTieuTichLuy { get; set; }
        public int DiemTichLuyHienCo { get; set; }
        public int DiemTichLuySeDung { get; set; }
        public decimal TienGiamDiem { get; set; }
        public decimal TienHangSauGiam { get; set; }
        public bool LaNhanTaiCuaHang { get; set; }
        public bool HopLeVungHoaToc { get; set; }
        public decimal PhiMoiDotGiao { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal TongThanhToanCuoi { get; set; }
    }

    public class KetQuaXuLyDatHang
    {
        public bool ThanhCong { get; set; }
        public string? TenSanPhamHetHang { get; set; }
        public string? ThongBaoLoi { get; set; }
        public DonHang? DonHangDaTao { get; set; }
        public bool LaThanhToanQrMoMo { get; set; }
    }

    public class KetQuaXuLyWebhookMoMo
    {
        public int MaTrangThaiHttp { get; set; } = 200;
        public bool ThanhCong { get; set; }
        public string? MaTraCuu { get; set; }
        public string? TrangThaiTt { get; set; }
        public string? TrangThaiDh { get; set; }
        public decimal SoTienDaNhan { get; set; }
        public string ThongBao { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dịch vụ Facade điều phối 3 dịch vụ chuyên biệt (DichVuBaoMatCaptcha, DichVuTinhGiaDonHang, DichVuTonKhoFefo),
    /// lưu DonHang, tạo ThiepChucMung3D (chỉ khi CoThiep3D == true) và xử lý Webhook /api/webhook/momo.
    /// </summary>
    public class DichVuThanhToan : DichVuCoSo, IDichVuThanhToan
    {
        private readonly IDichVuCauHinhHeThong _kiotMeta;
        private readonly IDichVuBaoMatCaptcha _dichVuCaptcha;
        private readonly IDichVuTinhGiaDonHang _dichVuTinhGia;
        private readonly IDichVuTonKhoFefo _dichVuTonKho;

        public DichVuThanhToan(
            CSDLBanhTrungThuContext context,
            IDichVuCauHinhHeThong kiotMeta,
            IDichVuBaoMatCaptcha dichVuCaptcha,
            IDichVuTinhGiaDonHang dichVuTinhGia,
            IDichVuTonKhoFefo dichVuTonKho) : base(context)
        {
            _kiotMeta = kiotMeta;
            _dichVuCaptcha = dichVuCaptcha;
            _dichVuTinhGia = dichVuTinhGia;
            _dichVuTonKho = dichVuTonKho;
        }

        // =========================================================================
        // 1. ỦY QUYỀN SANG 3 SERVICE CON CHUYÊN BIỆT (CAPTCHA, TÍNH GIÁ, KHO FEFO)
        // =========================================================================
        public string TaoChuoiSvgCaptchaThuan(out string maCaptcha) => _dichVuCaptcha.TaoChuoiSvgCaptchaThuan(out maCaptcha);

        public string TaoAnhSvgCaptcha(out string maCaptcha) => _dichVuCaptcha.TaoAnhSvgCaptcha(out maCaptcha);

        public Task<KetQuaKiemTraTonKhoGioHang> KiemTraTonKhoGioHangAsync(Models.Entities.GioHang gioHang)
            => _dichVuTonKho.KiemTraTonKhoGioHangAsync(gioHang);

        public Task<KetQuaTinhChiPhiDonHang> TinhToanChiPhiDonHangAsync(
            Models.Entities.GioHang gioHang,
            int? maNd,
            bool laDonDoanhNghiepB2B = false,
            bool yeuCauInLogo = false,
            string? hinhThucNhanHang = null,
            string? tocDoGiaoHang = null,
            List<DotGiaoHangInput>? danhSachDotGiao = null,
            bool capNhatLuotDungVoucher = false,
            bool luuCapNhatGiaGioHang = false,
            bool dungDiemTichLuy = false)
            => _dichVuTinhGia.TinhToanChiPhiDonHangAsync(
                gioHang, maNd, laDonDoanhNghiepB2B, yeuCauInLogo,
                hinhThucNhanHang, tocDoGiaoHang, danhSachDotGiao,
                capNhatLuotDungVoucher, luuCapNhatGiaGioHang, dungDiemTichLuy);

        public Task<List<NhatKyTruLoFefo>> TruTonKhoFefoAsync(int maBanh, int soLuongCanTru, List<NhatKyTruLoFefo>? danhSachLoDaTru = null)
            => _dichVuTonKho.TruTonKhoFefoAsync(maBanh, soLuongCanTru, danhSachLoDaTru);

        public Task HoanTraTonKhoRollbackAsync(DonHang donHang)
            => _dichVuTonKho.HoanTraTonKhoRollbackAsync(donHang);

        public async Task<Models.Entities.GioHang?> LayGioHangDayDuAsync(int? maNd, string? maPhienKvl)
        {
            IQueryable<Models.Entities.GioHang> truyVan = _context.GioHangs
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation).ThenInclude(cb => cb!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation).ThenInclude(cb => cb!.ChiTietComboMaus).ThenInclude(ctcb => ctcb.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.ChiTietHopTuChons).ThenInclude(cth => cth.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs);

            if (maNd.HasValue) return await truyVan.FirstOrDefaultAsync(g => g.MaNd == maNd.Value);
            if (!string.IsNullOrWhiteSpace(maPhienKvl)) return await truyVan.FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl);
            return null;
        }

        // =========================================================================
        // 2. XỬ LÝ ĐẶT HÀNG TRỌN GÓI TRONG GIAO DỊCH ACID
        // =========================================================================
        public async Task<KetQuaXuLyDatHang> XuLyDatHangAsync(
            Models.Entities.GioHang gioHang,
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
                {
                    return new KetQuaXuLyDatHang
                    {
                        ThanhCong = false,
                        ThongBaoLoi = "Vui lòng nhập đầy đủ thông tin và địa chỉ giao hàng!"
                    };
                }

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
            else if (!laNhanTaiCuaHangReq && req.DanhSachDotGiao.Any(d => string.IsNullOrWhiteSpace(d.DiaChiGiaoChiTiet)))
            {
                return new KetQuaXuLyDatHang
                {
                    ThanhCong = false,
                    ThongBaoLoi = "Vui lòng nhập đầy đủ địa chỉ nhận hàng!"
                };
            }

            using var giaoDichDb = await _context.Database.BeginTransactionAsync();
            try
            {
                var ketQuaKho = await _dichVuTonKho.KiemTraTonKhoGioHangAsync(gioHang);
                if (!ketQuaKho.HopLe)
                {
                    await giaoDichDb.RollbackAsync();
                    return new KetQuaXuLyDatHang
                    {
                        ThanhCong = false,
                        TenSanPhamHetHang = ketQuaKho.TenSanPhamThieu ?? "Bánh Trung Thu",
                        ThongBaoLoi = ketQuaKho.ThongBaoChiTiet
                    };
                }

                string maTraCuu;
                do
                {
                    maTraCuu = "TT2026-" + Random.Shared.Next(1000, 9999);
                } while (await _context.DonHangs.AnyAsync(d => d.MaTraCuuDh == maTraCuu));

                var chiPhi = await _dichVuTinhGia.TinhToanChiPhiDonHangAsync(
                    gioHang, maNd, req.LaDonDoanhNghiepB2B, req.YeuCauInLogo,
                    hinhThucNhanHang, tocDoGiao, req.DanhSachDotGiao,
                    capNhatLuotDungVoucher: true, luuCapNhatGiaGioHang: false,
                    dungDiemTichLuy: req.SuDungDiemTichLuy || req.DungDiemTichLuy);

                if (chiPhi.LaNhanTaiCuaHang && req.DanhSachDotGiao.Count > 1)
                {
                    req.DanhSachDotGiao = req.DanhSachDotGiao.Take(1).ToList();
                }

                string phuongThucRaw = ChuanHoaChuoi(req.PhuongThucTt, "COD").ToUpperInvariant();
                bool laThanhToanQrMoMo = phuongThucRaw == "MOMO_QR" || phuongThucRaw == "VIETQR" || phuongThucRaw == "MOMO";
                string phuongThucTt = laThanhToanQrMoMo ? "MOMO_QR" : "COD";
                DateTime thoiDiemDat = LayThoiGianHeThong();

                var donHang = new DonHang
                {
                    MaTraCuuDh = maTraCuu,
                    MaNd = maNd,
                    MaPhienKvl = maPhienKvl,
                    HoTenNguoiMua = ChuanHoaChuoi(req.HoTenNguoiMua, "Khách Hàng Nguyệt Cát"),
                    SdtnguoiMua = ChuanHoaChuoi(req.SdtNguoiMua, "0909000000"),
                    EmailNguoiMua = string.IsNullOrWhiteSpace(req.EmailNguoiMua) ? null : req.EmailNguoiMua.Trim(),
                    LaDonDoanhNghiepB2b = req.LaDonDoanhNghiepB2B,
                    TenCongTy = req.LaDonDoanhNghiepB2B ? req.TenCongTy?.Trim() : null,
                    MaSoThue = req.LaDonDoanhNghiepB2B ? req.MaSoThue?.Trim() : null,
                    TongTienHang = chiPhi.TongTienHang,
                    PhanTramCkB2b = chiPhi.PhanTramCkB2B,
                    TienChietKhauB2b = chiPhi.TienChietKhauB2B,
                    TienGiamVoucher = chiPhi.TienGiamVoucher,
                    PhiInLogo = chiPhi.PhiInLogo,
                    TongPhiVanChuyen = chiPhi.PhiVanChuyen,
                    TongThanhToanCuoi = chiPhi.TongThanhToanCuoi,
                    TienGiamHangVip = Math.Round(chiPhi.TienGiamVip, 0),
                    PhanTramGiamVip = (int)Math.Round(chiPhi.PhanTramGiamVip, 0),
                    SoDiemDaDung = chiPhi.DiemTichLuySeDung,
                    TienGiamDiemVip = Math.Round(chiPhi.TienGiamDiem, 0),
                    HanThanhToanQr = laThanhToanQrMoMo ? thoiDiemDat.AddMinutes(15) : null,
                    DaCongDiemThuong = false,
                    DaHoanDiemKhiHuy = false,
                    LyDoHuy = null,
                    PhuongThucTt = phuongThucTt,
                    TrangThaiTt = laThanhToanQrMoMo ? "CHO_THANH_TOAN" : "CHUA_TT",
                    TrangThaiDh = "CHO_XAC_NHAN",
                    NgayDat = thoiDiemDat
                };

                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync();

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
                        loiChucLuu = ChuanHoaChuoi(dotInput.LoiChucThiep, "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!");
                        mauGiaoDienLuu = ChuanHoaChuoi(dotInput.MauGiaoDienThiep, "CO_PHONG_HOANG_GIA");

                        string kinhGuiTrenThiep = !string.IsNullOrWhiteSpace(dotInput.TenNguoiNhanThiep)
                            ? dotInput.TenNguoiNhanThiep.Trim()
                            : ChuanHoaChuoi(dotInput.TenNguoiNhan, "Quý Khách & Gia Đình");

                        string nguoiGuiTrenThiep = ChuanHoaChuoi(dotInput.TenNguoiGuiThiep, donHang.HoTenNguoiMua);

                        mapThiepMeta[tokenQr] = new ThongTinThiep3DMeta
                        {
                            MaTokenThiepQr = tokenQr,
                            TenNguoiNhanThiep = kinhGuiTrenThiep,
                            TenNguoiGuiThiep = nguoiGuiTrenThiep,
                            LoiChucThiep = loiChucLuu,
                            MauGiaoDienThiep = mauGiaoDienLuu
                        };
                    }

                    bool dotNhanTaiCuaHang = chiPhi.LaNhanTaiCuaHang;

                    var dotEntity = new DotGiaoHangDaDiem
                    {
                        MaDh = donHang.MaDh,
                        TenNguoiNhan = ChuanHoaChuoi(dotInput.TenNguoiNhan, donHang.HoTenNguoiMua),
                        SdtnguoiNhan = ChuanHoaChuoi(dotInput.SdtNguoiNhan, donHang.SdtnguoiMua),
                        DiaChiGiaoChiTiet = ChuanHoaChuoi(dotInput.DiaChiGiaoChiTiet, "Địa chỉ nhận quà Trung Thu"),
                        NgayHenGiao = (dotInput.NgayHenGiao.HasValue && dotInput.NgayHenGiao.Value >= LayNgayHienTai())
                            ? dotInput.NgayHenGiao.Value
                            : LayNgayHienTai().AddDays(1),
                        KhungGioGiao = ChuanHoaChuoi(dotInput.KhungGioGiao, "Sáng (08:00 - 11:30)"),
                        PhiShipDotNay = dotNhanTaiCuaHang ? 0m : chiPhi.PhiMoiDotGiao,
                        TrangThaiGiao = "CHO_DONG_GOI",
                        CoKemThiepQr = coTaoThiep3D,
                        MaTokenThiepQr = tokenQr,
                        LoiChucThiep = loiChucLuu,
                        MauGiaoDienThiep = mauGiaoDienLuu,
                        SoLanMoThiep = 0
                    };

                    _context.DotGiaoHangDaDiems.Add(dotEntity);
                    cacDotDaLuu.Add(dotEntity);
                }
                await _context.SaveChangesAsync();

                foreach (var dotDaLuu in cacDotDaLuu.Where(d => d.CoKemThiepQr && !string.IsNullOrWhiteSpace(d.MaTokenThiepQr)))
                {
                    mapThiepMeta.TryGetValue(dotDaLuu.MaTokenThiepQr!, out var thiepMeta);
                    _context.ThiepChucMung3ds.Add(new ThiepChucMung3d
                    {
                        MaTokenQr = dotDaLuu.MaTokenThiepQr!,
                        MaDonHang = donHang.MaDh,
                        MaDotGiao = dotDaLuu.MaDotGh,
                        TenNguoiGui = thiepMeta?.TenNguoiGuiThiep ?? donHang.HoTenNguoiMua,
                        TenNguoiNhan = thiepMeta?.TenNguoiNhanThiep ?? dotDaLuu.TenNguoiNhan,
                        LoiChuc = dotDaLuu.LoiChucThiep ?? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!",
                        MauGiaoDien = dotDaLuu.MauGiaoDienThiep ?? "CO_PHONG_HOANG_GIA",
                        SoLanMoThiep = 0,
                        NgayTao = LayThoiGianHeThong()
                    });
                }

                int? maDotDauTien = cacDotDaLuu.FirstOrDefault()?.MaDotGh;
                var danhSachLoDaTru = new List<NhatKyTruLoFefo>();
                var danhSachVoHopDaTru = new List<NhatKyTruVoHop>();
                var danhSachMon = gioHang.ChiTietGioHangs.ToList();

                for (int idx = 0; idx < danhSachMon.Count; idx++)
                {
                    var ctGio = danhSachMon[idx];
                    int? maDotGan = cacDotDaLuu.Any() ? cacDotDaLuu[idx % cacDotDaLuu.Count].MaDotGh : maDotDauTien;
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
                        {
                            await _dichVuTonKho.TruTonKhoVoHopAsync(ctGio.MaCbNavigation.MaVhNavigation.MaVh, ctGio.SoLuong, danhSachVoHopDaTru);
                        }
                        foreach (var ctCombo in ctGio.MaCbNavigation.ChiTietComboMaus.Where(c => c.MaBanhNavigation != null))
                        {
                            await _dichVuTonKho.TruTonKhoFefoAsync(ctCombo.MaBanhNavigation!.MaBanh, ctCombo.SoLuong * ctGio.SoLuong, danhSachLoDaTru);
                        }
                    }
                    else if (ctGio.LoaiSanPham == 3 && ctGio.MaHopTcNavigation != null)
                    {
                        var hopTc = ctGio.MaHopTcNavigation;
                        tenLuuCung = $"{hopTc.TenHopKhachDat} ({hopTc.MaBamCauHinh})";
                        if (hopTc.MaVhNavigation != null)
                        {
                            await _dichVuTonKho.TruTonKhoVoHopAsync(hopTc.MaVhNavigation.MaVh, ctGio.SoLuong, danhSachVoHopDaTru);
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

                        foreach (var ctHop in hopTc.ChiTietHopTuChons.Where(c => c.MaBanhNavigation != null))
                        {
                            await _dichVuTonKho.TruTonKhoFefoAsync(ctHop.MaBanhNavigation!.MaBanh, ctGio.SoLuong, danhSachLoDaTru);
                        }
                    }

                    _context.ChiTietDonHangs.Add(new ChiTietDonHang
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
                    });
                }

                if (maNd.HasValue)
                {
                    var nd = await _context.NguoiDungs.FindAsync(maNd.Value);
                    if (nd != null)
                    {
                        if (chiPhi.DiemTichLuySeDung > 0)
                        {
                            nd.DiemTichLuy = Math.Max(0, nd.DiemTichLuy - chiPhi.DiemTichLuySeDung);
                        }

                        var (hangVipMoi, _, _, _) = await DichVuHoiVien.TinhThongTinHangVipAsync(_context, nd.MaNd, nd.DiemTichLuy);
                        nd.HangTv = hangVipMoi;
                    }
                }

                _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = LayThoiGianHeThong();

                await _context.SaveChangesAsync();
                await giaoDichDb.CommitAsync();

                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, meta =>
                {
                    meta.KenhBan = "WEB_ONLINE";
                    meta.DaHoanKhoDoHetHan = false;
                    meta.DanhSachLoDaTru = danhSachLoDaTru;
                    meta.DanhSachVoHopDaTru = danhSachVoHopDaTru;
                    meta.Thiep3DTheoToken = mapThiepMeta;
                });

                return new KetQuaXuLyDatHang
                {
                    ThanhCong = true,
                    DonHangDaTao = donHang,
                    LaThanhToanQrMoMo = laThanhToanQrMoMo
                };
            }
            catch
            {
                await giaoDichDb.RollbackAsync();
                throw;
            }
        }

        // =========================================================================
        // 3. TỰ ĐỘNG HỦY ĐƠN QR QUÁ HẠN 15 PHÚT, CỔNG MOMO, TRANG THÀNH CÔNG & WEBHOOK
        // =========================================================================
        public async Task<int> KiemTraVaHuyDonQrQuaHanAsync(int? maDonHang = null)
        {
            var bayGio = LayThoiGianHeThong();
            var truyVan = _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .Include(d => d.DotGiaoHangDaDiems)
                .Where(d => (d.TrangThaiTt == "CHO_THANH_TOAN" || d.TrangThaiTt == "CHO_XAC_MINH_CK")
                         && d.TrangThaiDh != "DA_HUY"
                         && (d.PhuongThucTt == "MOMO_QR" || d.PhuongThucTt == "VIETQR" || d.PhuongThucTt == "MOMO"));

            if (maDonHang.HasValue)
            {
                truyVan = truyVan.Where(d => d.MaDh == maDonHang.Value);
            }

            var cacDonCho = await truyVan.ToListAsync();
            var cacDonQuaHan = cacDonCho
                .Where(d => bayGio >= (d.HanThanhToanQr ?? d.NgayDat.AddMinutes(15)))
                .ToList();

            if (cacDonQuaHan.Count == 0) return 0;

            int soDonDaHuy = 0;
            foreach (var donHang in cacDonQuaHan)
            {
                bool daHuyDonNay = false;

                await ThucThiTrongGiaoDichAsync(async () =>
                {
                    if ((donHang.TrangThaiTt != "CHO_THANH_TOAN" && donHang.TrangThaiTt != "CHO_XAC_MINH_CK")
                        || donHang.TrangThaiDh == "DA_HUY")
                    {
                        return;
                    }

                    donHang.TrangThaiDh = "DA_HUY";
                    donHang.TrangThaiTt = "HET_HAN_HUY";
                    donHang.LyDoHuy = "Tự động hủy do quá thời hạn 15 phút chuyển khoản QR";

                    foreach (var dot in donHang.DotGiaoHangDaDiems)
                    {
                        dot.TrangThaiGiao = "DA_HUY";
                    }

                    await _dichVuTonKho.HoanTraTonKhoTheoDonAsync(donHang);

                    NguoiDung? ndHoan = null;
                    if (donHang.MaNd.HasValue && donHang.SoDiemDaDung > 0 && !donHang.DaHoanDiemKhiHuy)
                    {
                        ndHoan = await _context.NguoiDungs.FindAsync(donHang.MaNd.Value);
                        if (ndHoan != null)
                        {
                            ndHoan.DiemTichLuy += donHang.SoDiemDaDung;
                        }
                        donHang.DaHoanDiemKhiHuy = true;
                    }

                    await _context.SaveChangesAsync();

                    if (ndHoan != null)
                    {
                        var (hangVipMoi, _, _, _) = await DichVuHoiVien.TinhThongTinHangVipAsync(_context, ndHoan.MaNd, ndHoan.DiemTichLuy);
                        ndHoan.HangTv = hangVipMoi;
                        await _context.SaveChangesAsync();
                    }

                    daHuyDonNay = true;
                });

                if (daHuyDonNay)
                {
                    _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, m =>
                    {
                        m.DaHoanKhoDoHetHan = true;
                    });
                    soDonDaHuy++;
                }
            }

            return soDonDaHuy;
        }

        public async Task<(string TrangThaiDieuHuong, CongThanhToanMoMoViewModel? DuLieuView)> XayDungDuLieuCongMoMoAsync(string maTraCuu)
        {
            await KiemTraVaHuyDonQrQuaHanAsync();

            var donHang = await _context.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu);

            if (donHang == null) return ("VE_TRANG_CHU", null);

            _kiotMeta.GanMetadataChoDonHang(donHang);

            if (donHang.TrangThaiTt == "DA_THANH_TOAN" || donHang.TrangThaiTt == "DA_TT")
            {
                return ("DA_THANH_TOAN", null);
            }

            if (donHang.TrangThaiTt == "HET_HAN_HUY" || donHang.TrangThaiTt == "HET_HAN_15P" || donHang.TrangThaiDh == "DA_HUY")
            {
                return ("DA_HUY_HET_HAN", null);
            }

            DateTime hanThanhToan = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);
            int soGiayConLai = Math.Max(0, (int)Math.Ceiling((hanThanhToan - LayThoiGianHeThong()).TotalSeconds));
            var (cauHinhQr, noiDungCk, sdtMoMoRaw, sdtMoMoHienThi, tenChuTk, qrThanhToanBase64, urlVietQr) = TaoMaQrThanhToanMoMo(donHang);

            return ("HIEN_CONG_MOMO", new CongThanhToanMoMoViewModel
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
            });
        }

        public async Task<DonHangThanhCongViewModel?> XayDungDuLieuDonHangThanhCongAsync(string maTraCuu, string scheme, string host)
        {
            await KiemTraVaHuyDonQrQuaHanAsync();

            var donHang = await _context.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu);

            if (donHang == null) return null;

            var danhSachThiep = await _context.ThiepChucMung3ds
                .AsNoTracking()
                .Where(t => t.MaDonHang == donHang.MaDh)
                .ToListAsync();

            foreach (var thiep in danhSachThiep)
            {
                var dot = donHang.DotGiaoHangDaDiems.FirstOrDefault(g => g.MaDotGh == thiep.MaDotGiao)
                    ?? donHang.DotGiaoHangDaDiems.FirstOrDefault(g => g.MaTokenThiepQr == thiep.MaTokenQr);
                if (dot == null) continue;

                if (!dot.CoKemThiepQr) dot.CoKemThiepQr = true;
                if (string.IsNullOrWhiteSpace(dot.MaTokenThiepQr)) dot.MaTokenThiepQr = thiep.MaTokenQr;
                if (string.IsNullOrWhiteSpace(dot.LoiChucThiep)) dot.LoiChucThiep = thiep.LoiChuc;
                if (string.IsNullOrWhiteSpace(dot.MauGiaoDienThiep)) dot.MauGiaoDienThiep = thiep.MauGiaoDien;
                if (string.IsNullOrWhiteSpace(dot.TenNguoiNhanThiep)) dot.TenNguoiNhanThiep = thiep.TenNguoiNhan;
                if (string.IsNullOrWhiteSpace(dot.TenNguoiGuiThiep)) dot.TenNguoiGuiThiep = thiep.TenNguoiGui;
            }

            _kiotMeta.GanMetadataChoDonHang(donHang);

            bool laDonQrMoMo = donHang.PhuongThucTt == "MOMO_QR" || donHang.PhuongThucTt == "VIETQR" || donHang.PhuongThucTt == "MOMO";
            bool laDaHuyHoacHetHan = donHang.TrangThaiDh == "DA_HUY"
                || donHang.TrangThaiTt == "HET_HAN_HUY"
                || donHang.TrangThaiTt == "HET_HAN_15P";
            DateTime hanThanhToan = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);

            bool dangChoThanhToanQr = !laDaHuyHoacHetHan
                && laDonQrMoMo
                && (donHang.TrangThaiTt == "CHO_THANH_TOAN" || donHang.TrangThaiTt == "CHO_XAC_MINH_CK");
            int soGiayConLai = dangChoThanhToanQr ? Math.Max(0, (int)Math.Ceiling((hanThanhToan - LayThoiGianHeThong()).TotalSeconds)) : 0;

            var (cauHinhQr, noiDungCk, _, _, _, qrThanhToanBase64, urlVietQr) = TaoMaQrThanhToanMoMo(donHang, dangChoThanhToanQr);

            var vm = new DonHangThanhCongViewModel
            {
                DonHang = donHang,
                DiemCongThem = (!laDaHuyHoacHetHan && donHang.MaNd.HasValue) ? (int)(donHang.TongThanhToanCuoi / 10000m) : 0,
                CauHinhQr = cauHinhQr,
                NoiDungChuyenKhoan = noiDungCk,
                AnhQrThanhToanBase64 = qrThanhToanBase64,
                UrlVietQr = urlVietQr,
                DangChoThanhToanQr = dangChoThanhToanQr,
                HanThanhToan = hanThanhToan,
                SoGiayConLai = soGiayConLai
            };

            using var qrGenerator = new QRCodeGenerator();
            foreach (var dot in donHang.DotGiaoHangDaDiems.Where(d => d.CoKemThiepQr && !string.IsNullOrWhiteSpace(d.MaTokenThiepQr)))
            {
                var metaThiep = _kiotMeta.LayThongTinThiepTheoToken(donHang.MaDh, dot.MaTokenThiepQr);
                if (metaThiep != null)
                {
                    dot.TenNguoiNhanThiep = metaThiep.TenNguoiNhanThiep;
                    dot.TenNguoiGuiThiep = metaThiep.TenNguoiGuiThiep;
                }

                string urlThiep = $"{scheme}://{host}/Thiep3D/Xem/{dot.MaTokenThiepQr}";
                using QRCodeData qrCodeData = qrGenerator.CreateQrCode(urlThiep, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);

                vm.DanhSachDotGiaoQr.Add(new DotGiaoKemQrItem
                {
                    DotGiao = dot,
                    UrlThiep3D = urlThiep,
                    AnhQrBase64 = "data:image/png;base64," + Convert.ToBase64String(qrCode.GetGraphic(10))
                });
            }

            return vm;
        }

        private static string BoDauTiengViet(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string formD = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (char ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            string khongDau = sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D');
            return Regex.Replace(khongDau, @"\s+", " ").Trim();
        }

        private (CauHinhThanhToanQr CauHinhQr, string NoiDungCk, string SdtMoMoRaw, string SdtMoMoHienThi, string TenChuTk, string QrBase64, string UrlVietQr) TaoMaQrThanhToanMoMo(DonHang donHang, bool sinhAnhQr = true)
        {
            var cauHinhQr = _kiotMeta.LayCauHinhQr();
            string sdtMoMoRaw = Regex.Replace(cauHinhQr.SoTaiKhoanMoMo ?? string.Empty, @"\D", "");
            if (string.IsNullOrWhiteSpace(sdtMoMoRaw)) sdtMoMoRaw = "0814585526";

            string sdtMoMoHienThi = !string.IsNullOrWhiteSpace(cauHinhQr.SoDienThoaiHienThi)
                ? cauHinhQr.SoDienThoaiHienThi.Trim()
                : (sdtMoMoRaw.Length == 10
                    ? $"{sdtMoMoRaw[..4]} {sdtMoMoRaw.Substring(4, 3)} {sdtMoMoRaw.Substring(7, 3)}"
                    : sdtMoMoRaw);

            string tenChuTk = BoDauTiengViet(ChuanHoaChuoi(cauHinhQr.TenChuTaiKhoan, "LE MINH DUC")).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(tenChuTk)) tenChuTk = "LE MINH DUC";

            string noiDungCk = BoDauTiengViet((cauHinhQr.MauNoiDungCk ?? "NGUYETCAT {MaTraCuuDh}")
                .Replace("{MaTraCuuDh}", donHang.MaTraCuuDh, StringComparison.OrdinalIgnoreCase)).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(noiDungCk)) noiDungCk = $"NGUYETCAT {donHang.MaTraCuuDh}".ToUpperInvariant();

            if (!sinhAnhQr)
            {
                return (cauHinhQr, noiDungCk, sdtMoMoRaw, sdtMoMoHienThi, tenChuTk, string.Empty, string.Empty);
            }

            long soTienNguyen = (long)Math.Round(donHang.TongThanhToanCuoi, 0);
            string urlVietQr = string.Empty;
            bool coCauHinhNganHang = !string.IsNullOrWhiteSpace(cauHinhQr.SoTaiKhoanNganHang)
                || (!string.Equals(cauHinhQr.TenNganHangVi, "MOMO", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(cauHinhQr.MaBinVietQr));

            if (coCauHinhNganHang)
            {
                string bin = ChuanHoaChuoi(cauHinhQr.MaBinVietQr, "970422");
                string stkNganHang = !string.IsNullOrWhiteSpace(cauHinhQr.SoTaiKhoanNganHang)
                    ? Regex.Replace(cauHinhQr.SoTaiKhoanNganHang, @"\D", "")
                    : sdtMoMoRaw;
                if (!string.IsNullOrWhiteSpace(stkNganHang))
                {
                    urlVietQr = $"https://img.vietqr.io/image/{bin}-{Uri.EscapeDataString(stkNganHang)}-compact2.png?amount={soTienNguyen}&addInfo={Uri.EscapeDataString(noiDungCk)}&accountName={Uri.EscapeDataString(tenChuTk)}";
                }
            }

            using var qrGenerator = new QRCodeGenerator();
            string payloadQrMoMo = $"2|99|{sdtMoMoRaw}|{tenChuTk}||0|0|{soTienNguyen}|{noiDungCk}|transfer_myqr";
            using QRCodeData dataQrTt = qrGenerator.CreateQrCode(payloadQrMoMo, QRCodeGenerator.ECCLevel.H);
            using var pngQrTt = new PngByteQRCode(dataQrTt);
            string qrThanhToanBase64 = "data:image/png;base64," + Convert.ToBase64String(pngQrTt.GetGraphic(10, true));

            return (cauHinhQr, noiDungCk, sdtMoMoRaw, sdtMoMoHienThi, tenChuTk, qrThanhToanBase64, urlVietQr);
        }

        public async Task<(DonHang? DonHang, int SoGiayConLai)> KiemTraVaCapNhatTrangThaiDonAsync(string maTraCuu)
        {
            await KiemTraVaHuyDonQrQuaHanAsync();

            var donHang = await _context.DonHangs.FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu.Trim());
            if (donHang == null) return (null, 0);

            _kiotMeta.GanMetadataChoDonHang(donHang);
            DateTime hanTt = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);

            int soGiayConLai = (donHang.TrangThaiDh == "DA_HUY" || donHang.TrangThaiTt == "HET_HAN_HUY" || donHang.TrangThaiTt == "HET_HAN_15P")
                ? 0
                : Math.Max(0, (int)Math.Ceiling((hanTt - LayThoiGianHeThong()).TotalSeconds));
            return (donHang, soGiayConLai);
        }

        public async Task<KetQuaXuLyWebhookMoMo> XuLyWebhookMoMoAsync(MomoWebhookPayload payload)
        {
            string noiDung = payload.Content ?? payload.Description ?? payload.OrderInfo
                ?? payload.Data?.FirstOrDefault()?.Content ?? payload.Data?.FirstOrDefault()?.Description ?? string.Empty;

            decimal soTienNhan = payload.TransferAmount ?? payload.Amount ?? payload.SoTien
                ?? payload.Data?.FirstOrDefault()?.TransferAmount ?? payload.Data?.FirstOrDefault()?.Amount ?? 0m;

            string? maDonThang = payload.MaDonHang;
            if (string.IsNullOrWhiteSpace(maDonThang))
            {
                var khopMa = Regex.Match(noiDung, @"TT2026[- ]?\d{4}", RegexOptions.IgnoreCase);
                if (khopMa.Success)
                {
                    string chuoiTho = khopMa.Value.ToUpperInvariant().Replace(" ", "");
                    maDonThang = chuoiTho.Contains('-') ? chuoiTho : "TT2026-" + chuoiTho.Substring(6);
                }
            }

            if (string.IsNullOrWhiteSpace(maDonThang))
            {
                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 200,
                    ThanhCong = false,
                    ThongBao = "Không tìm thấy mã đơn hàng hợp lệ (TT2026-XXXX) trong nội dung chuyển khoản."
                };
            }

            var donHang = await _context.DonHangs.FirstOrDefaultAsync(d => d.MaTraCuuDh == maDonThang.Trim());
            if (donHang == null)
            {
                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 404,
                    ThanhCong = false,
                    ThongBao = $"Không tìm thấy đơn hàng #{maDonThang} trong CSDL."
                };
            }

            _kiotMeta.GanMetadataChoDonHang(donHang);
            DateTime hanTt = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);

            if (donHang.TrangThaiTt == "HET_HAN_HUY"
                || donHang.TrangThaiTt == "HET_HAN_15P"
                || donHang.TrangThaiDh == "DA_HUY"
                || LayThoiGianHeThong() >= hanTt)
            {
                if (donHang.TrangThaiDh != "DA_HUY")
                {
                    await KiemTraVaHuyDonQrQuaHanAsync(donHang.MaDh);
                }

                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 200,
                    ThanhCong = false,
                    ThongBao = $"Đơn hàng #{donHang.MaTraCuuDh} đã quá hạn thanh toán 15 phút hoặc đã hủy."
                };
            }

            if (soTienNhan < Math.Round(donHang.TongThanhToanCuoi, 0))
            {
                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 200,
                    ThanhCong = false,
                    ThongBao = $"Số tiền chuyển khoản ({soTienNhan:N0} đ) nhỏ hơn tổng thanh toán của đơn ({donHang.TongThanhToanCuoi:N0} đ)."
                };
            }

            if (donHang.TrangThaiTt != "DA_THANH_TOAN" && donHang.TrangThaiTt != "DA_TT")
            {
                donHang.TrangThaiTt = "DA_THANH_TOAN";
                donHang.TrangThaiDh = "DA_XAC_NHAN";
                await _context.SaveChangesAsync();

                await DichVuHoiVien.CongDiemThuongDonHangAsync(_context, donHang);

                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, m => m.SoTienKhachDaTra = soTienNhan);
                _kiotMeta.GhiNhanPhieuThuChi(new PhieuThuChiMoRong
                {
                    MaDonHang = donHang.MaDh,
                    MaTraCuuDh = donHang.MaTraCuuDh,
                    LoaiPhieu = "THU",
                    PhuongThuc = "MOMO_QR",
                    SoTien = soTienNhan,
                    NguoiNopNhan = donHang.HoTenNguoiMua,
                    NoiDung = $"Thu tự động Webhook MoMo: {(string.IsNullOrWhiteSpace(noiDung) ? "NGUYETCAT " + donHang.MaTraCuuDh : noiDung.Trim())}",
                    ThoiGian = LayThoiGianHeThong()
                });
            }

            return new KetQuaXuLyWebhookMoMo
            {
                MaTrangThaiHttp = 200,
                ThanhCong = true,
                MaTraCuu = donHang.MaTraCuuDh,
                TrangThaiTt = donHang.TrangThaiTt,
                TrangThaiDh = donHang.TrangThaiDh,
                SoTienDaNhan = soTienNhan,
                ThongBao = $"Đã xác nhận thanh toán tự động thành công cho đơn #{donHang.MaTraCuuDh}!"
            };
        }
    }
}
