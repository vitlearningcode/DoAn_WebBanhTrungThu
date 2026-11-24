using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.TaiKhoan;
using GioHang = WebBanhTrungThu.Models.Entities.GioHang;

namespace WebBanhTrungThu.Services.ThanhToan
{
    /// <summary>
    /// Dịch vụ chuyên trách kiểm tra tồn kho thực tế, trừ kho theo lô LoSanXuatBanh (thuật toán FEFO)
    /// và Rollback hoàn kho khi đơn QR MoMo quá hạn 15 phút.
    /// </summary>
    public class DichVuTonKhoFefo : DichVuCoSo, IDichVuTonKhoFefo
    {
        public DichVuTonKhoFefo(CSDLBanhTrungThuContext context) : base(context)
        {
        }

        /// <summary>
        /// Kiểm tra chi tiết từng loại bánh lẻ, combo và bánh trong hộp tự phối đối chiếu với tổng tồn kho khả dụng
        /// (SlconLai > 0 && HanSuDung > DateTime.Today && TrangThaiLo == "DANG_BAN" / "XA_CAN_DATE") trong bảng LoSanXuatBanh.
        /// Trả về danh sách tên các bánh bị thiếu (nếu có).
        /// </summary>
        public async Task<KetQuaKiemTraTonKhoGioHang> KiemTraTonKhoGioHangAsync(Models.Entities.GioHang gioHang)
        {
            var ketQua = new KetQuaKiemTraTonKhoGioHang();
            DateOnly homNay = LayNgayHienTai();

            var nhuCauBanh = new Dictionary<int, (string TenBanh, int SoLuongCan, int TonKhoThucTe)>();
            var nhuCauVoHop = new Dictionary<int, (string TenVoHop, int SoLuongCan, int TonKhoThucTe)>();

            async Task<int> TinhTonKhoLoConHanAsync(SanPhamBanh banh)
            {
                var cacLo = banh.LoSanXuatBanhs;
                if (cacLo == null || !cacLo.Any())
                {
                    cacLo = await _context.LoSanXuatBanhs
                        .AsNoTracking()
                        .Where(l => l.MaBanh == banh.MaBanh)
                        .ToListAsync();
                }

                if (cacLo.Any())
                {
                    int tonLo = cacLo
                        .Where(l => l.HanSuDung > homNay && (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "XA_CAN_DATE") && l.SlconLai > 0)
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
                    int ton = await TinhTonKhoLoConHanAsync(b);
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
                            int ton = await TinhTonKhoLoConHanAsync(b);
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
                            int ton = await TinhTonKhoLoConHanAsync(b);
                            int slCu = nhuCauBanh.ContainsKey(b.MaBanh) ? nhuCauBanh[b.MaBanh].SoLuongCan : 0;
                            nhuCauBanh[b.MaBanh] = (b.TenBanh, slCu + ct.SoLuong, ton);
                        }
                    }
                }
            }

            foreach (var itemBanh in nhuCauBanh.Values)
            {
                if (itemBanh.TonKhoThucTe < itemBanh.SoLuongCan)
                {
                    ketQua.DanhSachTenBanhThieu.Add(itemBanh.TenBanh);
                    string msg = $"Rất tiếc! Sản phẩm {itemBanh.TenBanh} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho (cần {itemBanh.SoLuongCan}, kho còn {itemBanh.TonKhoThucTe}). Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn vị bánh khác.";
                    ketQua.DanhSachCanhBao.Add(msg);
                    ketQua.ThongBaoChiTiet ??= $"Rất tiếc! Sản phẩm {itemBanh.TenBanh} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho. Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn vị bánh khác.";
                }
            }

            foreach (var itemVo in nhuCauVoHop.Values)
            {
                if (itemVo.TonKhoThucTe < itemVo.SoLuongCan)
                {
                    ketQua.DanhSachTenBanhThieu.Add(itemVo.TenVoHop);
                    string msg = $"Rất tiếc! Mẫu vỏ hộp {itemVo.TenVoHop} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho (cần {itemVo.SoLuongCan}, kho còn {itemVo.TonKhoThucTe}). Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn mẫu hộp khác.";
                    ketQua.DanhSachCanhBao.Add(msg);
                    ketQua.ThongBaoChiTiet ??= $"Rất tiếc! Sản phẩm {itemVo.TenVoHop} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho. Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn vị bánh khác.";
                }
            }

            return ketQua;
        }

        /// <summary>
        /// Thực thi trừ kho theo nguyên tắc FEFO (lô hết hạn trước trừ trước) cho một mã bánh cụ thể.
        /// </summary>
        public async Task<List<NhatKyTruLoFefo>> TruTonKhoFefoAsync(
            int maBanh,
            int soLuongCanTru,
            List<NhatKyTruLoFefo>? danhSachLoDaTru = null)
        {
            danhSachLoDaTru ??= new List<NhatKyTruLoFefo>();
            if (soLuongCanTru <= 0) return danhSachLoDaTru;

            var banh = _context.SanPhamBanhs.Local.FirstOrDefault(b => b.MaBanh == maBanh)
                       ?? await _context.SanPhamBanhs
                           .Include(b => b.LoSanXuatBanhs)
                           .FirstOrDefaultAsync(b => b.MaBanh == maBanh);

            if (banh == null) return danhSachLoDaTru;

            if (banh.LoSanXuatBanhs == null || !banh.LoSanXuatBanhs.Any())
            {
                await _context.Entry(banh).Collection(b => b.LoSanXuatBanhs).LoadAsync();
            }

            DateOnly homNay = LayNgayHienTai();
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

            return danhSachLoDaTru;
        }

        /// <summary>
        /// Trừ tồn kho mẫu vỏ hộp quà và ghi nhận nhật ký phục vụ Rollback khi quá hạn thanh toán.
        /// </summary>
        public async Task<List<NhatKyTruVoHop>> TruTonKhoVoHopAsync(
            int maVh,
            int soLuongCanTru,
            List<NhatKyTruVoHop>? danhSachVoHopDaTru = null)
        {
            danhSachVoHopDaTru ??= new List<NhatKyTruVoHop>();
            if (soLuongCanTru <= 0) return danhSachVoHopDaTru;

            var voHop = _context.MauVoHops.Local.FirstOrDefault(v => v.MaVh == maVh)
                        ?? await _context.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == maVh);

            if (voHop == null) return danhSachVoHopDaTru;

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

            return danhSachVoHopDaTru;
        }

        private async Task HoanNhapLoFefoAsync(int maBanh, int soLuongHoan, int? maLoUuTien = null)
        {
            if (soLuongHoan <= 0) return;

            var banh = await _context.SanPhamBanhs.FirstOrDefaultAsync(b => b.MaBanh == maBanh);
            if (banh != null)
            {
                banh.TongTonKho += soLuongHoan;
            }

            LoSanXuatBanh? lo = null;
            if (maLoUuTien.HasValue)
            {
                lo = await _context.LoSanXuatBanhs.FirstOrDefaultAsync(l =>
                    l.MaLo == maLoUuTien.Value
                    && l.MaBanh == maBanh
                    && l.TrangThaiLo != "DA_HUY"
                    && l.TrangThaiLo != "HET_HAN");
            }

            lo ??= await _context.LoSanXuatBanhs
                .Where(l => l.MaBanh == maBanh
                         && l.TrangThaiLo != "DA_HUY"
                         && l.TrangThaiLo != "HET_HAN"
                         && (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "SAP_HET_HAN"))
                .OrderBy(l => l.TrangThaiLo == "DANG_BAN" ? 0 : 1)
                .ThenByDescending(l => l.HanSuDung)
                .FirstOrDefaultAsync();

            lo ??= await _context.LoSanXuatBanhs
                .Where(l => l.MaBanh == maBanh
                         && l.TrangThaiLo != "DA_HUY"
                         && l.TrangThaiLo != "HET_HAN")
                .OrderByDescending(l => l.HanSuDung)
                .FirstOrDefaultAsync();

            if (lo != null)
            {
                lo.SlconLai += soLuongHoan;
            }
        }

        /// <summary>
        /// Hoàn trả toàn bộ tồn kho bánh theo lô FEFO và vỏ hộp cho 3 loại sản phẩm (Bánh lẻ, Combo, Hộp tự chọn)
        /// vào DbContext hiện tại mà không tự mở Transaction hay gọi SaveChangesAsync.
        /// </summary>
        public async Task HoanTraTonKhoTheoDonAsync(DonHang donHang)
        {
            if (donHang.DaRollbackKho)
            {
                return;
            }

            var nhatKyDb = await _context.NhatKyTruKhoDonHangs
                .Where(n => n.MaDh == donHang.MaDh && !n.DaHoanKho)
                .ToListAsync();

            var nhatKyLoDb = nhatKyDb
                .Where(n => n.LoaiDoiTuong == "LO_BANH" && n.MaBanh.HasValue)
                .ToList();
            var nhatKyVoDb = nhatKyDb
                .Where(n => n.LoaiDoiTuong == "VO_HOP" && n.MaVh.HasValue)
                .ToList();

            if (nhatKyLoDb.Count > 0 || nhatKyVoDb.Count > 0)
            {
                foreach (var nkLo in nhatKyLoDb)
                {
                    await HoanNhapLoFefoAsync(nkLo.MaBanh!.Value, nkLo.SoLuongDaTru, nkLo.MaLo);
                    nkLo.DaHoanKho = true;
                }

                if (nhatKyVoDb.Count > 0)
                {
                    foreach (var nkVo in nhatKyVoDb)
                    {
                        var vo = await _context.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == nkVo.MaVh!.Value);
                        if (vo != null)
                        {
                            vo.SltonKhoVo += nkVo.SoLuongDaTru;
                        }
                        nkVo.DaHoanKho = true;
                    }
                }
                else
                {
                    var chiTietsVo = donHang.ChiTietDonHangs != null && donHang.ChiTietDonHangs.Any()
                        ? donHang.ChiTietDonHangs.ToList()
                        : await _context.ChiTietDonHangs.Where(c => c.MaDh == donHang.MaDh).ToListAsync();

                    foreach (var ct in chiTietsVo)
                    {
                        if (ct.LoaiSanPham == 2 && ct.MaCb.HasValue)
                        {
                            var maVh = await _context.ComboMauCoSans.AsNoTracking()
                                .Where(c => c.MaCb == ct.MaCb.Value)
                                .Select(c => (int?)c.MaVh)
                                .FirstOrDefaultAsync();
                            if (maVh.HasValue)
                            {
                                var vo = await _context.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == maVh.Value);
                                if (vo != null) vo.SltonKhoVo += ct.SoLuong;
                            }
                        }
                        else if (ct.LoaiSanPham == 3 && ct.MaHopTc.HasValue)
                        {
                            var maVh = await _context.HopQuaTuChons.AsNoTracking()
                                .Where(h => h.MaHopTc == ct.MaHopTc.Value)
                                .Select(h => (int?)h.MaVh)
                                .FirstOrDefaultAsync();
                            if (maVh.HasValue)
                            {
                                var vo = await _context.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == maVh.Value);
                                if (vo != null) vo.SltonKhoVo += ct.SoLuong;
                            }
                        }
                    }
                }

                donHang.DaRollbackKho = true;
                return;
            }

            var chiTiets = donHang.ChiTietDonHangs != null && donHang.ChiTietDonHangs.Any()
                ? donHang.ChiTietDonHangs.ToList()
                : await _context.ChiTietDonHangs.Where(c => c.MaDh == donHang.MaDh).ToListAsync();

            foreach (var ct in chiTiets)
            {
                if (ct.LoaiSanPham == 1 && ct.MaBanh.HasValue)
                {
                    await HoanNhapLoFefoAsync(ct.MaBanh.Value, ct.SoLuong);
                }
                else if (ct.LoaiSanPham == 2 && ct.MaCb.HasValue)
                {
                    var combo = await _context.ComboMauCoSans
                        .Include(c => c.ChiTietComboMaus)
                        .FirstOrDefaultAsync(c => c.MaCb == ct.MaCb.Value);

                    if (combo != null)
                    {
                        var vo = await _context.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == combo.MaVh);
                        if (vo != null)
                        {
                            vo.SltonKhoVo += ct.SoLuong;
                        }
                        foreach (var ctcb in combo.ChiTietComboMaus)
                        {
                            await HoanNhapLoFefoAsync(ctcb.MaBanh, ctcb.SoLuong * ct.SoLuong);
                        }
                    }
                }
                else if (ct.LoaiSanPham == 3 && ct.MaHopTc.HasValue)
                {
                    var hopTc = await _context.HopQuaTuChons
                        .Include(h => h.ChiTietHopTuChons)
                        .FirstOrDefaultAsync(h => h.MaHopTc == ct.MaHopTc.Value);

                    if (hopTc != null)
                    {
                        var vo = await _context.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == hopTc.MaVh);
                        if (vo != null)
                        {
                            vo.SltonKhoVo += ct.SoLuong;
                        }
                        foreach (var cth in hopTc.ChiTietHopTuChons)
                        {
                            await HoanNhapLoFefoAsync(cth.MaBanh, ct.SoLuong);
                        }
                    }
                }
            }

            donHang.DaRollbackKho = true;
        }

        /// <summary>
        /// Hoàn trả tồn kho về đúng từng lô sản xuất FEFO và mẫu vỏ hộp khi đơn QR MoMo quá hạn 15 phút,
        /// đảm bảo chạy an toàn bên trong Database Transaction (ThucThiTrongGiaoDichAsync / BeginTransactionAsync).
        /// </summary>
        public async Task HoanTraTonKhoRollbackAsync(DonHang donHang)
        {
            if (donHang.TrangThaiDh == "DA_HUY" && (donHang.TrangThaiTt == "HET_HAN_HUY" || donHang.TrangThaiTt == "HET_HAN_15P"))
            {
                return;
            }

            await ThucThiTrongGiaoDichAsync(async () =>
            {
                await HoanTraTonKhoTheoDonAsync(donHang);

                donHang.TrangThaiDh = "DA_HUY";
                donHang.TrangThaiTt = "HET_HAN_HUY";
                donHang.LyDoHuy = "Tự động hủy do quá thời hạn 15 phút chuyển khoản QR";
                donHang.DaRollbackKho = true;

                var cacDotGiao = donHang.DotGiaoHangDaDiems != null && donHang.DotGiaoHangDaDiems.Any()
                    ? donHang.DotGiaoHangDaDiems.ToList()
                    : await _context.DotGiaoHangDaDiems.Where(g => g.MaDh == donHang.MaDh).ToListAsync();

                foreach (var dot in cacDotGiao)
                {
                    dot.TrangThaiGiao = "DA_HUY";
                }

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
            });
        }
    }
}

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
}
