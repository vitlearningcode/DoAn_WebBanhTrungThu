using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Lô sản xuất FEFO, Kiểm kho hạn sử dụng,
    /// Xóa/hủy lô hết hạn và Kích hoạt xả kho cận Date (-30%). Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuQuanTriKhoFefo : DichVuCoSo
    {
        public DichVuQuanTriKhoFefo(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        public async Task<KiemKhoLoAdminViewModel> XayDungDuLieuKiemKhoAsync(
            string? tuKhoa,
            string locTrangThai = "ALL",
            string locGiamGia = "ALL")
        {
            var dsLo = await _db.LoSanXuatBanhs
                .AsNoTracking()
                .Include(l => l.MaBanhNavigation)
                .OrderBy(l => l.HanSuDung)
                .ToListAsync();

            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .OrderBy(b => b.TenBanh)
                .ToListAsync();

            return new KiemKhoLoAdminViewModel
            {
                DanhSachLoBanh = dsLo,
                DanhSachBanh = dsBanh,
                TuKhoa = tuKhoa,
                LocTrangThai = locTrangThai,
                LocGiamGia = locGiamGia
            };
        }

        public async Task<(bool HopLe, string ThongBao)> ThemLoBanhMoiAsync(
            int maBanh,
            string? maLoCode,
            DateTime? ngaySanXuat,
            DateTime? hanSuDung,
            int slNhapBanDau = 50,
            decimal phanTramGiamCanDate = 0m)
        {
            var banh = await _db.SanPhamBanhs.FindAsync(maBanh);
            if (banh == null)
            {
                return (false, "Không tìm thấy sản phẩm bánh để nhập lô mới!");
            }

            string code = string.IsNullOrWhiteSpace(maLoCode)
                ? $"LO-{DateTime.Now:yyyyMMdd}-{banh.MaBanh:D2}-{Random.Shared.Next(10, 99)}"
                : maLoCode.Trim().ToUpperInvariant();

            if (await _db.LoSanXuatBanhs.AnyAsync(l => l.MaLoCode == code))
            {
                code += "-" + Random.Shared.Next(10, 99);
            }

            DateOnly nsx = ngaySanXuat.HasValue
                ? DateOnly.FromDateTime(ngaySanXuat.Value)
                : LayNgayHienTai();
            DateOnly hsd = hanSuDung.HasValue
                ? DateOnly.FromDateTime(hanSuDung.Value)
                : nsx.AddDays(35);
            if (hsd < nsx) hsd = nsx.AddDays(30);

            int slNhap = Math.Max(1, slNhapBanDau);
            decimal ptGiam = Math.Clamp(Math.Round(phanTramGiamCanDate, 0), 0m, 90m);
            string trangThai = ptGiam > 0
                ? "XA_CAN_DATE"
                : (hsd < LayNgayHienTai() ? "HET_HAN" : "DANG_BAN");

            var loMoi = new LoSanXuatBanh
            {
                MaBanh = banh.MaBanh,
                MaLoCode = code,
                NgaySanXuat = nsx,
                HanSuDung = hsd,
                SlnhapBanDau = slNhap,
                SlconLai = slNhap,
                PhanTramGiamCanDate = ptGiam,
                TrangThaiLo = trangThai
            };

            _db.LoSanXuatBanhs.Add(loMoi);
            banh.TongTonKho = Math.Max(0, banh.TongTonKho) + slNhap;
            await _db.SaveChangesAsync();

            return (true, $"Đã nhập lô mới [{code}] cho bánh '{banh.TenBanh}' (+{slNhap:N0} bánh vào tổng tồn kho)!");
        }

        public async Task<string?> CapNhatGiamGiaCanDateAsync(
            int maLo,
            decimal phanTramGiam,
            string? trangThaiLo,
            int? slConLai)
        {
            var lo = await _db.LoSanXuatBanhs
                .Include(l => l.MaBanhNavigation)
                .FirstOrDefaultAsync(l => l.MaLo == maLo);

            if (lo == null) return null;

            lo.PhanTramGiamCanDate = Math.Clamp(Math.Round(phanTramGiam, 0), 0m, 90m);
            if (slConLai.HasValue && slConLai.Value >= 0)
            {
                int chenhLech = slConLai.Value - lo.SlconLai;
                lo.SlconLai = slConLai.Value;
                if (lo.MaBanhNavigation != null)
                {
                    lo.MaBanhNavigation.TongTonKho = Math.Max(0, lo.MaBanhNavigation.TongTonKho + chenhLech);
                }
            }

            if (!string.IsNullOrWhiteSpace(trangThaiLo))
            {
                lo.TrangThaiLo = trangThaiLo;
            }
            else if (lo.PhanTramGiamCanDate > 0)
            {
                lo.TrangThaiLo = "XA_CAN_DATE";
            }
            else if (lo.HanSuDung < LayNgayHienTai())
            {
                lo.TrangThaiLo = "HET_HAN";
            }
            else
            {
                lo.TrangThaiLo = "DANG_BAN";
            }

            await _db.SaveChangesAsync();
            return $"Đã cập nhật lô {lo.MaLoCode} ({lo.MaBanhNavigation?.TenBanh}) -> Giảm cận Date: {lo.PhanTramGiamCanDate:N0}% ({lo.TrangThaiLo})!";
        }

        public async Task<string?> XoaLoBanhAsync(int maLo)
        {
            var lo = await _db.LoSanXuatBanhs
                .Include(l => l.MaBanhNavigation)
                .FirstOrDefaultAsync(l => l.MaLo == maLo);

            if (lo == null) return null;

            string code = lo.MaLoCode;
            string tenBanh = lo.MaBanhNavigation?.TenBanh ?? "";
            if (lo.MaBanhNavigation != null && lo.SlconLai > 0)
            {
                lo.MaBanhNavigation.TongTonKho = Math.Max(0, lo.MaBanhNavigation.TongTonKho - lo.SlconLai);
            }

            _db.LoSanXuatBanhs.Remove(lo);
            await _db.SaveChangesAsync();
            return $"Đã xóa lô [{code}] của sản phẩm '{tenBanh}' và đồng bộ lại tồn kho!";
        }

        public async Task<object> KichHoatXaKhoNhanhAsync(int maLo, int mucGiam = 30)
        {
            var lo = await _db.LoSanXuatBanhs
                .Include(l => l.MaBanhNavigation)
                .FirstOrDefaultAsync(l => l.MaLo == maLo);

            if (lo == null)
            {
                return new { thanhCong = false, thongBao = "Không tìm thấy lô sản xuất cần kích hoạt xả kho!" };
            }

            int phanTram = Math.Clamp(mucGiam, 1, 90);
            lo.PhanTramGiamCanDate = phanTram;
            lo.TrangThaiLo = "XA_CAN_DATE";
            await _db.SaveChangesAsync();

            decimal giaGoc = lo.MaBanhNavigation?.DonGiaLe ?? 0m;
            decimal giaMoi = Math.Round(giaGoc * (100m - phanTram) / 100m, 0);

            return new
            {
                thanhCong = true,
                thongBao = "Đã kích hoạt ưu đãi xả kho -30% cho lô cận date!",
                maLo = lo.MaLo,
                maLoCode = lo.MaLoCode,
                maBanh = lo.MaBanh,
                tenBanh = lo.MaBanhNavigation?.TenBanh ?? "",
                mucGiam = phanTram,
                giaGoc = Math.Round(giaGoc, 0),
                giaMoi
            };
        }
    }
}
