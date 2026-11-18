using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.TaiKhoan;
using GioHang = WebBanhTrungThu.Models.Entities.GioHang;

namespace WebBanhTrungThu.Services.ThanhToan
{
    /// <summary>
    /// Dịch vụ chuyên trách kiểm tra tồn kho thực tế, trừ kho theo lô LoSanXuatBanh (thuật toán FEFO)
    /// và Rollback hoàn kho khi đơn QR MoMo quá hạn 15 phút.
    /// </summary>
    public class DichVuTonKhoFefo : DichVuCoSo
    {
        private readonly DichVuCauHinhHeThong _kiotMeta;

        public DichVuTonKhoFefo(CSDLBanhTrungThuContext context, DichVuCauHinhHeThong kiotMeta) : base(context)
        {
            _kiotMeta = kiotMeta;
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

        /// <summary>
        /// Hoàn trả tồn kho về đúng từng lô sản xuất FEFO và mẫu vỏ hộp khi đơn QR MoMo quá hạn 15 phút,
        /// đảm bảo chạy an toàn bên trong Database Transaction (ThucThiTrongGiaoDichAsync / BeginTransactionAsync).
        /// </summary>
        public async Task HoanTraTonKhoRollbackAsync(DonHang donHang)
        {
            var meta = _kiotMeta.LayMetadataDonHang(donHang.MaDh);
            if (meta.DaHoanKhoDoHetHan && donHang.TrangThaiTt == "HET_HAN_15P")
            {
                return;
            }

            await ThucThiTrongGiaoDichAsync(async () =>
            {
                if (!meta.DaHoanKhoDoHetHan)
                {
                    if (meta.DanhSachLoDaTru != null && meta.DanhSachLoDaTru.Any())
                    {
                        foreach (var nhatKyLo in meta.DanhSachLoDaTru)
                        {
                            var lo = await _context.LoSanXuatBanhs.FirstOrDefaultAsync(l => l.MaLo == nhatKyLo.MaLo);
                            if (lo != null)
                            {
                                lo.SlconLai += nhatKyLo.SoLuongDaTru;
                            }

                            var banh = await _context.SanPhamBanhs.FirstOrDefaultAsync(b => b.MaBanh == nhatKyLo.MaBanh);
                            if (banh != null)
                            {
                                banh.TongTonKho += nhatKyLo.SoLuongDaTru;
                            }
                        }
                    }
                    else
                    {
                        var chiTiets = await _context.ChiTietDonHangs
                            .Where(c => c.MaDh == donHang.MaDh)
                            .ToListAsync();

                        foreach (var ct in chiTiets)
                        {
                            if (ct.LoaiSanPham == 1 && ct.MaBanh.HasValue)
                            {
                                var banh = await _context.SanPhamBanhs
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

                    if (meta.DanhSachVoHopDaTru != null && meta.DanhSachVoHopDaTru.Any())
                    {
                        foreach (var nhatKyVo in meta.DanhSachVoHopDaTru)
                        {
                            var vo = await _context.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == nhatKyVo.MaVh);
                            if (vo != null)
                            {
                                vo.SltonKhoVo += nhatKyVo.SoLuongDaTru;
                            }
                        }
                    }
                }

                donHang.TrangThaiDh = "DA_HUY";
                donHang.TrangThaiTt = "HET_HAN_15P";
                await _context.SaveChangesAsync();
            });

            _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, m =>
            {
                m.DaHoanKhoDoHetHan = true;
            });

            await DichVuHoiVien.HoanDiemKhiHuyDonAsync(_context, _kiotMeta, donHang);
        }
    }
}
