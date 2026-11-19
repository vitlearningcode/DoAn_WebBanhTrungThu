using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;
using GioHang = WebBanhTrungThu.Models.Entities.GioHang;

namespace WebBanhTrungThu.Services.ThanhToan
{
    /// <summary>
    /// Dịch vụ chuyên trách tính tiền hàng từ CSDL (Anti-Tampering), tra bảng chiết khấu sỉ BangChietKhauSiB2b,
    /// kiểm tra 4 điều kiện hạn dùng của MaKhuyenMai và tính phí vận chuyển chuẩn xác.
    /// </summary>
    public class DichVuTinhGiaDonHang : DichVuCoSo, IDichVuTinhGiaDonHang
    {
        public DichVuTinhGiaDonHang(CSDLBanhTrungThuContext context) : base(context)
        {
        }

        /// <summary>
        /// Tính lại 100% đơn giá của từng dòng giỏ hàng trực tiếp từ SQL Server (Anti-Tampering).
        /// </summary>
        public decimal TinhDonGiaThucTeTuCsdl(ChiTietGioHang ct)
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
        public bool KiemTraVungHoTroHoaToc(string? tinhThanh, string? diaChiChiTiet)
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
        /// Tính toán 100% từ CSDL SQL Server: Tổng tiền hàng gốc, Tỷ lệ & số tiền chiết khấu sỉ B2B (BangChietKhauSiB2b),
        /// Kiểm tra điều kiện & hạn dùng mã khuyến mãi (MaKhuyenMai), và tính PhiVanChuyen chuẩn xác.
        /// </summary>
        public async Task<KetQuaTinhChiPhiDonHang> TinhToanChiPhiDonHangAsync(
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
        {
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

            if (luuCapNhatGiaGioHang && coCapNhatGia)
            {
                await _context.SaveChangesAsync();
            }

            var danhSachMon = gioHang.ChiTietGioHangs.OrderByDescending(c => c.NgayThem).ToList();
            decimal tongTienHang = danhSachMon.Sum(c => c.SoLuong * c.DonGiaTamTinh);

            int tongSoHop = danhSachMon
                .Where(c => c.LoaiSanPham == 2 || c.LoaiSanPham == 3)
                .Sum(c => c.SoLuong);

            // 1. Tra bảng chiết khấu sỉ B2B từ SQL Server
            var bangB2B = await _context.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
            var mocB2B = bangB2B.FirstOrDefault(b => tongSoHop >= b.SlhopToiThieu && tongSoHop <= b.SlhopToiDa)
                         ?? (tongSoHop > 0 && bangB2B.Any() && tongSoHop > bangB2B.Last().SlhopToiDa ? bangB2B.Last() : null);

            decimal phanTramCkB2B = mocB2B?.PhanTramChietKhau ?? 0m;
            decimal tienChietKhauB2B = phanTramCkB2B > 0 ? Math.Round(tongTienHang * (phanTramCkB2B / 100m), 0) : 0m;
            bool mienPhiInLogo = mocB2B?.MienPhiInLogo ?? false;
            decimal phiInLogoMacDinh = mocB2B?.PhiKhuonInLogo ?? (bangB2B.FirstOrDefault()?.PhiKhuonInLogo ?? 500000m);

            decimal phiInLogo = (laDonDoanhNghiepB2B && yeuCauInLogo && !mienPhiInLogo)
                ? phiInLogoMacDinh
                : 0m;

            // 2. Lấy thông tin mã gợi ý TRANGTRON2026 & Kiểm tra 4 điều kiện hạn dùng của MaKhuyenMai
            var kmGoiY = await _context.MaKhuyenMais.AsNoTracking().FirstOrDefaultAsync(k => k.CodeKm == "TRANGTRON2026");
            string thongTinVoucherGoiY = kmGoiY != null
                ? $"HSD: {kmGoiY.NgayKetThuc:dd/MM/yyyy} • Đơn từ {(kmGoiY.DonToiThieu / 1000m):N0}k"
                : "HSD: 31/12/2027 • Đơn từ 200k";

            decimal tienGiamVoucher = 0m;
            string? codeKmDangDung = gioHang.CodeKmdangApDung;
            if (!string.IsNullOrWhiteSpace(codeKmDangDung))
            {
                var km = await _context.MaKhuyenMais.FirstOrDefaultAsync(k => k.CodeKm == codeKmDangDung);
                DateTime bayGio = LayThoiGianHeThong();

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

                    if (capNhatLuotDungVoucher)
                    {
                        km.SldaDung += 1;
                    }
                }
                else
                {
                    codeKmDangDung = null;
                    gioHang.CodeKmdangApDung = null;
                    if (luuCapNhatGiaGioHang)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // 3. Tính ưu đãi Hạng VIP (không tụt hạng khi tiêu điểm) & Tiêu điểm tích lũy (1 điểm = 1.000đ, tối đa 50% tiền hàng sau giảm)
            decimal tienHangSauCkVaVoucher = Math.Max(0m, tongTienHang - tienChietKhauB2B - tienGiamVoucher);
            string hangTv = "Đồng";
            decimal phanTramGiamVip = 0m;
            decimal tongChiTieuTichLuy = 0m;
            int diemTichLuyHienCo = 0;

            if (maNd.HasValue)
            {
                var nd = await _context.NguoiDungs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.MaNd == maNd.Value);

                if (nd != null)
                {
                    diemTichLuyHienCo = Math.Max(0, nd.DiemTichLuy);
                    (hangTv, phanTramGiamVip, tongChiTieuTichLuy, _) = await DichVuHoiVien.TinhThongTinHangVipAsync(
                        _context,
                        nd.MaNd,
                        diemTichLuyHienCo);
                }
            }

            decimal tienGiamVip = phanTramGiamVip > 0m
                ? Math.Round(tienHangSauCkVaVoucher * (phanTramGiamVip / 100m), 0)
                : 0m;
            decimal tienHangSauVip = Math.Max(0m, tienHangSauCkVaVoucher - tienGiamVip);

            int diemToiDaTheoDon = (int)Math.Floor((tienHangSauVip * 0.5m) / 1000m);
            int diemTichLuySeDung = dungDiemTichLuy ? Math.Min(diemTichLuyHienCo, diemToiDaTheoDon) : 0;
            decimal tienGiamDiem = diemTichLuySeDung * 1000m;

            // 4. Tính phí vận chuyển (Nhận tại cửa hàng / Tiêu chuẩn Freeship 500k / Hỏa tốc 6h TP.HCM & An Giang)
            decimal tienHangSauGiam = Math.Max(0m, tienHangSauVip - tienGiamDiem);
            bool duMocFreeship500k = tienHangSauGiam >= 500000m;

            var cacDotGiao = danhSachDotGiao ?? new List<DotGiaoHangInput>();
            bool laNhanTaiCuaHang = string.Equals(hinhThucNhanHang, "NHAN_TAI_CUA_HANG", StringComparison.OrdinalIgnoreCase)
                || cacDotGiao.Any(d => !string.IsNullOrWhiteSpace(d.DiaChiGiaoChiTiet) &&
                    (d.DiaChiGiaoChiTiet.Contains("[Nhận tại cửa hàng]", StringComparison.OrdinalIgnoreCase) ||
                     d.DiaChiGiaoChiTiet.Contains("Nhận tại Cửa hàng", StringComparison.OrdinalIgnoreCase)));

            bool yeuCauHoaToc = !laNhanTaiCuaHang && (
                string.Equals(tocDoGiaoHang, "GIAO_HOA_TOC", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tocDoGiaoHang, "HOA_TOC_6H", StringComparison.OrdinalIgnoreCase));

            bool hopLeVungHoaToc = yeuCauHoaToc && cacDotGiao.Any() && cacDotGiao.All(d =>
                KiemTraVungHoTroHoaToc(d.TinhThanhPho, d.DiaChiGiaoChiTiet));

            decimal phiMoiDotGiao;
            if (laNhanTaiCuaHang)
            {
                phiMoiDotGiao = 0m;
            }
            else if (hopLeVungHoaToc)
            {
                phiMoiDotGiao = duMocFreeship500k ? 30000m : 60000m;
            }
            else
            {
                phiMoiDotGiao = duMocFreeship500k ? 0m : 30000m;
            }

            int soDiemNhan = laNhanTaiCuaHang ? 1 : Math.Max(1, cacDotGiao.Count);
            decimal phiVanChuyen = laNhanTaiCuaHang ? 0m : (soDiemNhan * phiMoiDotGiao);
            decimal tongThanhToanCuoi = Math.Max(0m, tienHangSauGiam + phiInLogo + phiVanChuyen);

            return new KetQuaTinhChiPhiDonHang
            {
                DanhSachMon = danhSachMon,
                TongSoHop = tongSoHop,
                TongTienHang = tongTienHang,
                PhanTramCkB2B = phanTramCkB2B,
                TienChietKhauB2B = tienChietKhauB2B,
                MienPhiInLogo = mienPhiInLogo,
                PhiInLogoMacDinh = phiInLogoMacDinh,
                PhiInLogo = phiInLogo,
                TienGiamVoucher = tienGiamVoucher,
                CodeKmDangDung = codeKmDangDung,
                ThongTinVoucherGoiY = thongTinVoucherGoiY,
                HangTv = hangTv,
                PhanTramGiamVip = phanTramGiamVip,
                TienGiamVip = tienGiamVip,
                TongChiTieuTichLuy = tongChiTieuTichLuy,
                DiemTichLuyHienCo = diemTichLuyHienCo,
                DiemTichLuySeDung = diemTichLuySeDung,
                TienGiamDiem = tienGiamDiem,
                TienHangSauGiam = tienHangSauGiam,
                LaNhanTaiCuaHang = laNhanTaiCuaHang,
                HopLeVungHoaToc = hopLeVungHoaToc,
                PhiMoiDotGiao = phiMoiDotGiao,
                PhiVanChuyen = phiVanChuyen,
                TongThanhToanCuoi = tongThanhToanCuoi
            };
        }
    }
}
