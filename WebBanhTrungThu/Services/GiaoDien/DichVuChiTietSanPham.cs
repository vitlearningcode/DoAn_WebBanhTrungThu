using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.GiaoDien
{
    /// <summary>
    /// Dịch vụ cung cấp dữ liệu trang Chi tiết sản phẩm (ảnh đa nguồn, giá cận date,
    /// tồn kho &amp; hạn sử dụng lô FEFO gần nhất, bảng dinh dưỡng thật và 4 sản phẩm liên quan).
    /// </summary>
    public class DichVuChiTietSanPham : DichVuCoSo, IDichVuChiTietSanPham
    {
        private readonly IDichVuHinhAnh _dichVuHinhAnh;

        public DichVuChiTietSanPham(
            CSDLBanhTrungThuContext context,
            IDichVuHinhAnh dichVuHinhAnh) : base(context)
        {
            _dichVuHinhAnh = dichVuHinhAnh;
        }

        public async Task<ChiTietSanPhamViewModel?> LayChiTietSanPhamAsync(int maBanh)
        {
            var banh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.MaNccMacDinhNavigation)
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .FirstOrDefaultAsync(b => b.MaBanh == maBanh && b.TrangThaiBan);

            if (banh == null) return null;

            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();

            var danhSachAnh = await TongHopDanhSachAnhBanhAsync(banh.MaBanh, tuDienAnh);
            string anhChinh = danhSachAnh.FirstOrDefault() ?? "/images/default/banh-mac-dinh.png";

            DateOnly homNay = LayNgayHienTai();
            var cacLoConHan = (banh.LoSanXuatBanhs ?? Enumerable.Empty<LoSanXuatBanh>())
                .Where(l => l.HanSuDung > homNay
                    && (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "XA_CAN_DATE" || l.TrangThaiLo == "CAN_DATE")
                    && l.SlconLai > 0)
                .OrderBy(l => l.HanSuDung)
                .ThenBy(l => l.MaLo)
                .ToList();

            var loGanNhat = cacLoConHan.FirstOrDefault();
            int tonKhoKhaDung = cacLoConHan.Any()
                ? Math.Min(Math.Max(0, banh.TongTonKho), cacLoConHan.Sum(l => l.SlconLai))
                : Math.Max(0, banh.TongTonKho);

            int? soNgayConLai = loGanNhat != null
                ? Math.Max(0, loGanNhat.HanSuDung.DayNumber - homNay.DayNumber)
                : null;

            decimal phanTramGiam = banh.PhanTramGiamCanDateHieuLuc;
            bool dangGiamCanDate = phanTramGiam > 0m;
            decimal giaBanThucTe = banh.DonGiaThucTe;

            var dsLienQuan = await LaySanPhamLienQuanAsync(banh, tuDienAnh);

            return new ChiTietSanPhamViewModel
            {
                SanPham = banh,
                TenDanhMuc = banh.MaDmNavigation?.TenDm ?? "Bánh Trung Thu",
                ThuongHieu = banh.ThuongHieu,
                NhaCungCap = banh.NhaCungCap,
                AnhChinh = anhChinh,
                DanhSachAnh = danhSachAnh,
                GiaGoc = Math.Round(banh.DonGiaLe, 0),
                GiaBanThucTe = Math.Round(giaBanThucTe, 0),
                PhanTramGiamCanDate = phanTramGiam,
                DangGiamCanDate = dangGiamCanDate,
                TonKhoKhaDung = tonKhoKhaDung,
                ConHang = tonKhoKhaDung > 0,
                LoFefoGanNhat = loGanNhat,
                SoNgayConLaiHsd = soNgayConLai,
                ThongSoDinhDuong = banh.ThongSoDinhDuong,
                NhanMucDoNgot = XacDinhNhanMucDoNgot(banh.MucDoNgot),
                GoiYThuongTra = XacDinhGoiYThuongTra(banh),
                HuongDanBaoQuan = "Bảo quản nơi khô ráo, thoáng mát (tránh ánh nắng trực tiếp và nhiệt độ cao). Sau khi mở bao bì kín khí, nên thưởng thức trong vòng 2–3 ngày hoặc giữ ngăn mát tủ lạnh.",
                SanPhamLienQuan = dsLienQuan
            };
        }

        private async Task<List<string>> TongHopDanhSachAnhBanhAsync(
            int maBanh,
            Dictionary<string, string> tuDienAnh)
        {
            var ketQua = new List<string>();
            void ThemNeuMoi(string? url)
            {
                if (string.IsNullOrWhiteSpace(url)) return;
                string urlSach = url.Trim();
                if (!ketQua.Contains(urlSach, StringComparer.OrdinalIgnoreCase))
                {
                    ketQua.Add(urlSach);
                }
            }

            var lienKetAnhs = await _db.LienKetHinhAnhs
                .AsNoTracking()
                .Include(l => l.MaHaNavigation)
                .Where(l => l.LoaiDoiTuong == "BANH" && l.MaDoiTuong == maBanh)
                .OrderBy(l => l.ThuTuHienThi)
                .ToListAsync();

            foreach (var lk in lienKetAnhs)
            {
                ThemNeuMoi(lk.MaHaNavigation?.UrlHienThi);
            }

            ThemNeuMoi(DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", maBanh, "ANH_CHINH"));
            ThemNeuMoi(DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", maBanh, "TACH_NEN_SLOT"));

            if (ketQua.Count == 0)
            {
                ketQua.Add("/images/default/banh-mac-dinh.png");
            }

            return ketQua;
        }

        private async Task<List<SanPhamLienQuanItemViewModel>> LaySanPhamLienQuanAsync(
            SanPhamBanh banhGoc,
            Dictionary<string, string> tuDienAnh)
        {
            var dsUngVien = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.MaNccMacDinhNavigation)
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .Where(b => b.TrangThaiBan && b.MaBanh != banhGoc.MaBanh)
                .ToListAsync();

            var dsChon = dsUngVien
                .OrderByDescending(sp => sp.MaDm == banhGoc.MaDm ? 2 : 0)
                .ThenByDescending(sp => string.Equals(sp.ThuongHieu, banhGoc.ThuongHieu, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenByDescending(sp => sp.TongTonKho > 0 ? 1 : 0)
                .ThenBy(sp => sp.MaBanh)
                .Take(4)
                .ToList();

            return dsChon.Select(sp =>
            {
                string anh = DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", sp.MaBanh, "ANH_CHINH");
                if (anh == "/images/default/banh-mac-dinh.png")
                {
                    anh = DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", sp.MaBanh, "TACH_NEN_SLOT");
                }

                return new SanPhamLienQuanItemViewModel
                {
                    MaBanh = sp.MaBanh,
                    TenBanh = sp.TenBanh,
                    ThuongHieu = sp.ThuongHieu,
                    TenDanhMuc = sp.MaDmNavigation?.TenDm ?? "Bánh Trung Thu",
                    AnhDaiDien = anh,
                    GiaGoc = Math.Round(sp.DonGiaLe, 0),
                    GiaBanThucTe = Math.Round(sp.DonGiaThucTe, 0),
                    DangGiamCanDate = sp.DangXaCanDate,
                    PhanTramGiamCanDate = sp.PhanTramGiamCanDateHieuLuc,
                    NangLuongCalo = sp.ThongSoDinhDuong?.NangLuongCalo ?? 400,
                    ChiSoGi = sp.ThongSoDinhDuong?.ChiSoDuongHuyetGi ?? 55,
                    LaBanhThuongHang = sp.LaBanhThuongHang,
                    LaBanhChay = sp.LaBanhChay,
                    LaIsomalt = string.Equals(sp.MucDoNgot, "ISOMALT_TIEUDUONG", StringComparison.OrdinalIgnoreCase),
                    TrongLuongGram = sp.TrongLuongGram
                };
            }).ToList();
        }

        private static string XacDinhNhanMucDoNgot(string? mucDoNgot)
        {
            return (mucDoNgot ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "ISOMALT_TIEUDUONG" => "Đường ăn kiêng Isomalt",
                "IT_NGOT" => "Ít ngọt thanh đạm",
                _ => "Ngọt hài hòa truyền thống"
            };
        }

        private static string XacDinhGoiYThuongTra(SanPhamBanh banh)
        {
            if (banh.LaBanhChay || string.Equals(banh.MucDoNgot, "ISOMALT_TIEUDUONG", StringComparison.OrdinalIgnoreCase))
            {
                return "Thích hợp dùng kèm trà xanh Thái Nguyên, trà sen Tây Hồ hoặc trà hoa cúc ấm (75–80°C) để tôn vị thanh khiết.";
            }
            if (string.Equals(banh.NhomHuongVi, "LAVA_CHAY", StringComparison.OrdinalIgnoreCase))
            {
                return "Nên dùng cùng trà Ô Long rang nhẹ hoặc trà Bá Tước ấm để cân bằng vị béo mịn của nhân kim sa.";
            }
            return "Thưởng thức trọn vị hơn khi đi kèm ấm trà Ô Long Bảo Lộc hoặc trà Shan Tuyết cổ thụ pha ấm.";
        }
    }
}
