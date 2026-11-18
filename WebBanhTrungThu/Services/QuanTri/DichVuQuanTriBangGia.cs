using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách nghiệp vụ Thiết lập bảng giá Đại lý Nguyệt Cát ERP:
    /// Cập nhật giá bán/giá vốn hàng loạt, quản lý giá vỏ hộp, quản lý bậc chiết khấu sỉ BangChietKhauSiB2b,
    /// bảng giá Combo lễ hộp và quản lý chương trình MaKhuyenMai. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuQuanTriBangGia : DichVuCoSo
    {
        public DichVuQuanTriBangGia(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        public async Task<ThietLapGiaAdminViewModel> XayDungThietLapGiaAsync()
        {
            return new ThietLapGiaAdminViewModel
            {
                DanhSachVoHop = await _db.MauVoHops.AsNoTracking().OrderBy(v => v.MaVh).ToListAsync(),
                DanhSachCombo = await _db.ComboMauCoSans
                    .AsNoTracking()
                    .Include(c => c.MaVhNavigation)
                    .Include(c => c.ChiTietComboMaus).ThenInclude(ct => ct.MaBanhNavigation)
                    .OrderBy(c => c.MaCb)
                    .ToListAsync(),
                BangChietKhauB2B = await _db.BangChietKhauSiB2bs.AsNoTracking().OrderBy(b => b.SlhopToiThieu).ToListAsync(),
                DanhSachBanh = await _db.SanPhamBanhs.AsNoTracking().OrderBy(b => b.TenBanh).ToListAsync()
            };
        }

        public async Task<string?> CapNhatGiaBanhNhanhAsync(CapNhatBanhNhanhRequest req)
        {
            var banh = await _db.SanPhamBanhs.Include(b => b.ThongSoDinhDuong).FirstOrDefaultAsync(b => b.MaBanh == req.MaBanh);
            if (banh == null) return null;

            if (!string.IsNullOrWhiteSpace(req.TenBanh)) banh.TenBanh = req.TenBanh.Trim();
            banh.DonGiaLe = Math.Round(req.DonGiaLe, 0);
            banh.GiaVon = Math.Round(req.GiaVon, 0);
            banh.TongTonKho = req.TongTonKho;

            if (banh.ThongSoDinhDuong != null)
            {
                banh.ThongSoDinhDuong.NangLuongCalo = req.NangLuongCalo;
                banh.ThongSoDinhDuong.LuongDuongGram = req.LuongDuongGram;
                banh.ThongSoDinhDuong.ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi;
                banh.ThongSoDinhDuong.CanhBaoDiUng = ChuanHoaChuoi(req.CanhBaoDiUng, "Không");
            }

            await _db.SaveChangesAsync();
            return $"Đã cập nhật nhanh bảng giá & thông tin cho [{banh.MaSku}] {banh.TenBanh}!";
        }

        public async Task<(bool HopLe, string ThongBao)> ThemVoHopMoiAsync(
            string? maCodeVh,
            string tenVoHop,
            int sucChuaBanh,
            string? kieuBoCucLuoi,
            decimal giaVoHopRieng,
            int slTonKhoVo,
            int slBanhThuongHangToiThieu,
            string? moTaChatLieu)
        {
            if (string.IsNullOrWhiteSpace(tenVoHop))
            {
                return (false, "Vui lòng nhập tên mẫu vỏ hộp!");
            }

            string code = string.IsNullOrWhiteSpace(maCodeVh) ? $"VH-{Random.Shared.Next(100, 999)}" : maCodeVh.Trim().ToUpperInvariant();
            if (await _db.MauVoHops.AnyAsync(v => v.MaCodeVh == code)) code += "-" + Random.Shared.Next(10, 99);

            var vh = new MauVoHop
            {
                MaCodeVh = code,
                TenVoHop = tenVoHop.Trim(),
                SucChuaBanh = Math.Clamp(sucChuaBanh, 1, 12),
                KieuBoCucLuoi = ChuanHoaChuoi(kieuBoCucLuoi, "LUOI_2X2"),
                GiaVoHopRieng = Math.Max(0m, Math.Round(giaVoHopRieng, 0)),
                SltonKhoVo = Math.Max(0, slTonKhoVo),
                SlbanhThuongHangToiThieu = Math.Max(0, slBanhThuongHangToiThieu),
                MoTaChatLieu = ChuanHoaChuoi(moTaChatLieu, "Gỗ sơn mài / Giấy mỹ thuật ép kim"),
                TrangThai = true
            };

            _db.MauVoHops.Add(vh);
            await _db.SaveChangesAsync();
            return (true, $"Đã thêm mẫu vỏ hộp mới: [{vh.MaCodeVh}] {vh.TenVoHop}!");
        }

        public async Task<string?> CapNhatGiaVoHopAsync(int maVh, decimal giaVoHopRieng, int slTonKhoVo, int slBanhVipToiThieu)
        {
            var vh = await _db.MauVoHops.FindAsync(maVh);
            if (vh == null) return null;

            vh.GiaVoHopRieng = Math.Round(giaVoHopRieng, 0);
            vh.SltonKhoVo = slTonKhoVo;
            vh.SlbanhThuongHangToiThieu = slBanhVipToiThieu;
            await _db.SaveChangesAsync();
            return $"Đã cập nhật giá & tồn kho cho vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop}!";
        }

        public async Task<string?> XoaVoHopAsync(int maVh)
        {
            var vh = await _db.MauVoHops.FindAsync(maVh);
            if (vh == null) return null;

            bool dangDung = await _db.ComboMauCoSans.AnyAsync(c => c.MaVh == maVh)
                            || await _db.HopQuaTuChons.AnyAsync(h => h.MaVh == maVh);
            if (dangDung)
            {
                vh.TrangThai = false;
                await _db.SaveChangesAsync();
                return $"Mẫu vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop} đã có đơn/combo liên kết -> Đã chuyển sang Ngừng sử dụng!";
            }

            _db.MauVoHops.Remove(vh);
            await _db.SaveChangesAsync();
            return $"Đã xóa mẫu vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop}!";
        }

        public async Task<(bool HopLe, string ThongBao)> ThemMocB2BMoiAsync(
            string tenMocB2b,
            int slHopToiThieu,
            int? slHopToiDa,
            decimal phanTramChietKhau,
            decimal phiKhuonInLogo,
            bool mienPhiInLogo)
        {
            if (string.IsNullOrWhiteSpace(tenMocB2b))
            {
                return (false, "Vui lòng nhập tên mốc chiết khấu B2B!");
            }

            var moc = new BangChietKhauSiB2b
            {
                TenMocB2b = tenMocB2b.Trim(),
                SlhopToiThieu = Math.Max(1, slHopToiThieu),
                SlhopToiDa = (slHopToiDa.HasValue && slHopToiDa.Value >= slHopToiThieu) ? slHopToiDa.Value : 9999,
                PhanTramChietKhau = Math.Clamp(Math.Round(phanTramChietKhau, 0), 0m, 80m),
                PhiKhuonInLogo = Math.Max(0m, Math.Round(phiKhuonInLogo, 0)),
                MienPhiInLogo = mienPhiInLogo,
                SoNgaySanXuatCongThem = 2
            };

            _db.BangChietKhauSiB2bs.Add(moc);
            await _db.SaveChangesAsync();
            return (true, $"Đã thêm mốc chiết khấu B2B mới: {moc.TenMocB2b} (-{moc.PhanTramChietKhau:N0}%)!");
        }

        public async Task<string?> CapNhatMocB2BAsync(
            int maMocCk,
            int? slHopToiThieu,
            int? slHopToiDa,
            decimal phanTramChietKhau,
            decimal phiKhuonInLogo,
            bool mienPhiInLogo)
        {
            var moc = await _db.BangChietKhauSiB2bs.FindAsync(maMocCk);
            if (moc == null) return null;

            if (slHopToiThieu.HasValue && slHopToiThieu.Value > 0) moc.SlhopToiThieu = slHopToiThieu.Value;
            if (slHopToiDa.HasValue && slHopToiDa.Value >= moc.SlhopToiThieu) moc.SlhopToiDa = slHopToiDa.Value;
            moc.PhanTramChietKhau = Math.Round(phanTramChietKhau, 0);
            moc.PhiKhuonInLogo = Math.Round(phiKhuonInLogo, 0);
            moc.MienPhiInLogo = mienPhiInLogo;
            await _db.SaveChangesAsync();
            return $"Đã cập nhật mốc chiết khấu B2B: {moc.TenMocB2b} (-{moc.PhanTramChietKhau:N0}%)!";
        }

        public async Task<string?> XoaMocB2BAsync(int maMocCk)
        {
            var moc = await _db.BangChietKhauSiB2bs.FindAsync(maMocCk);
            if (moc == null) return null;

            string ten = moc.TenMocB2b;
            _db.BangChietKhauSiB2bs.Remove(moc);
            await _db.SaveChangesAsync();
            return $"Đã xóa mốc chiết khấu B2B: {ten}!";
        }

        public async Task<(bool HopLe, string ThongBao)> ThemComboGiaMoiAsync(
            string? maCodeCb,
            string tenCombo,
            int maVh,
            decimal giaNiemYet,
            decimal giaUuDaiCombo,
            string? thongDiepYngia)
        {
            if (string.IsNullOrWhiteSpace(tenCombo))
            {
                return (false, "Vui lòng nhập tên Combo!");
            }

            string code = string.IsNullOrWhiteSpace(maCodeCb) ? $"CB-{Random.Shared.Next(100, 999)}" : maCodeCb.Trim().ToUpperInvariant();
            if (await _db.ComboMauCoSans.AnyAsync(c => c.MaCodeCb == code)) code += "-" + Random.Shared.Next(10, 99);

            int maVhHopLe = maVh > 0 ? maVh : (await _db.MauVoHops.Select(v => v.MaVh).FirstOrDefaultAsync());
            var cb = new ComboMauCoSan
            {
                MaCodeCb = code,
                TenCombo = tenCombo.Trim(),
                MaVh = maVhHopLe,
                GiaNiemYet = Math.Max(0m, Math.Round(giaNiemYet, 0)),
                GiaUuDaiCombo = Math.Max(0m, Math.Round(giaUuDaiCombo, 0)),
                ThongDiepYngia = ChuanHoaChuoi(thongDiepYngia, "Combo lễ hộp Trung Thu tuyển chọn"),
                TheDoiTuongPhuHop = "ONG_BA,SEP_VIP,BAN_BE",
                TrangThai = true
            };

            _db.ComboMauCoSans.Add(cb);
            await _db.SaveChangesAsync();
            return (true, $"Đã thêm Combo mới: [{cb.MaCodeCb}] {cb.TenCombo}!");
        }

        public async Task<string?> CapNhatGiaComboAsync(int maCb, decimal giaNiemYet, decimal giaUuDaiCombo)
        {
            var cb = await _db.ComboMauCoSans.FindAsync(maCb);
            if (cb == null) return null;

            cb.GiaNiemYet = Math.Max(0m, Math.Round(giaNiemYet, 0));
            cb.GiaUuDaiCombo = Math.Max(0m, Math.Round(giaUuDaiCombo, 0));
            await _db.SaveChangesAsync();
            return $"Đã cập nhật bảng giá cho Combo [{cb.MaCodeCb}] {cb.TenCombo}!";
        }

        public async Task<string?> XoaComboGiaAsync(int maCb)
        {
            var cb = await _db.ComboMauCoSans.Include(c => c.ChiTietComboMaus).FirstOrDefaultAsync(c => c.MaCb == maCb);
            if (cb == null) return null;

            bool coTrongGio = await _db.ChiTietGioHangs.AnyAsync(g => g.MaCb == maCb);
            if (coTrongGio)
            {
                cb.TrangThai = false;
                await _db.SaveChangesAsync();
                return $"Combo [{cb.MaCodeCb}] đang có trong giỏ hàng -> Đã chuyển sang Ngừng kinh doanh!";
            }

            if (cb.ChiTietComboMaus.Any()) _db.ChiTietComboMaus.RemoveRange(cb.ChiTietComboMaus);
            _db.ComboMauCoSans.Remove(cb);
            await _db.SaveChangesAsync();
            return $"Đã xóa Combo [{cb.MaCodeCb}] {cb.TenCombo}!";
        }

        public async Task<KhuyenMaiAdminViewModel> LayDanhSachKhuyenMaiAsync()
        {
            return new KhuyenMaiAdminViewModel
            {
                DanhSachKhuyenMai = await _db.MaKhuyenMais
                    .AsNoTracking()
                    .OrderByDescending(k => k.NgayKetThuc)
                    .ToListAsync()
            };
        }

        public async Task<string> ThemVoucherMoiAsync(ThemVoucherMoiRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CodeKm) || string.IsNullOrWhiteSpace(req.TenChuongTrinh))
            {
                return "Vui lòng nhập đầy đủ Mã CodeKM và Tên chương trình khuyến mãi!";
            }

            string codeChuan = req.CodeKm.Trim().ToUpperInvariant();
            if (await _db.MaKhuyenMais.AnyAsync(k => k.CodeKm == codeChuan))
            {
                return $"Mã khuyến mãi '{codeChuan}' đã tồn tại!";
            }

            var kmMoi = new MaKhuyenMai
            {
                CodeKm = codeChuan,
                TenChuongTrinh = req.TenChuongTrinh.Trim(),
                LoaiGiam = req.LoaiGiam,
                GiaTriGiam = req.GiaTriGiam,
                GiamToiDa = req.GiamToiDa,
                DonToiThieu = req.DonToiThieu,
                SltoiDa = req.SltoiDa > 0 ? req.SltoiDa : 100,
                SldaDung = 0,
                ChoPhepKhachVangLai = req.ChoPhepKhachVangLai,
                NgayBatDau = LayThoiGianHeThong(),
                NgayKetThuc = req.NgayKetThuc ?? LayThoiGianHeThong().AddDays(45)
            };

            _db.MaKhuyenMais.Add(kmMoi);
            await _db.SaveChangesAsync();

            return $"Đã tạo mã khuyến mãi mới [{kmMoi.CodeKm}] - {kmMoi.TenChuongTrinh}!";
        }

        public async Task<string?> CapNhatVoucherAsync(int maKm, int slToiDa, DateTime ngayKetThuc, bool choPhepKhachVangLai)
        {
            var km = await _db.MaKhuyenMais.FindAsync(maKm);
            if (km == null) return null;

            km.SltoiDa = slToiDa;
            km.NgayKetThuc = ngayKetThuc;
            km.ChoPhepKhachVangLai = choPhepKhachVangLai;
            await _db.SaveChangesAsync();

            return $"Đã cập nhật cấu hình Voucher [{km.CodeKm}]!";
        }
    }
}
