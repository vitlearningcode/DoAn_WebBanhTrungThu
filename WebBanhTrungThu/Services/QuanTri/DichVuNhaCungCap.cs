using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Đối tác Nhà cung cấp (dbo.NhaCungCap),
    /// mức chiết khấu nhập hàng mặc định và đối soát tổng nhập / công nợ từ dbo.PhieuNhapKho.
    /// </summary>
    public class DichVuNhaCungCap : DichVuCoSo, IDichVuNhaCungCap
    {
        public DichVuNhaCungCap(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        public async Task<NhaCungCapAdminViewModel> XayDungTrangNhaCungCapAsync(
            string? tuKhoa,
            string locLoaiNcc = "ALL",
            string locTrangThai = "ALL")
        {
            var dsNcc = await _db.NhaCungCaps
                .AsNoTracking()
                .OrderBy(n => n.MaCodeNcc)
                .ThenBy(n => n.MaNcc)
                .ToListAsync();

            var dsPhieuNhap = await _db.PhieuNhapKhos
                .AsNoTracking()
                .Where(p => p.TrangThai != "DA_HUY")
                .OrderByDescending(p => p.NgayNhap)
                .ThenByDescending(p => p.MaPn)
                .ToListAsync();

            var dsThuongHieuDb = await _db.ThuongHieus
                .AsNoTracking()
                .Where(t => t.DangHoatDong)
                .OrderBy(t => t.MaThuongHieu)
                .Select(t => t.TenThuongHieu)
                .ToListAsync();

            var dsThuongHieu = new List<string> { "Đa thương hiệu" };
            foreach (var th in dsThuongHieuDb.Concat(dsNcc.Select(x => x.ThuongHieuCungCap)))
            {
                string sach = ChuanHoaChuoi(th);
                if (!string.IsNullOrWhiteSpace(sach) && !dsThuongHieu.Contains(sach, StringComparer.OrdinalIgnoreCase))
                {
                    dsThuongHieu.Add(sach);
                }
            }

            var danhSachItem = dsNcc.Select(n =>
            {
                var phieuCuaNcc = dsPhieuNhap
                    .Where(p => p.MaNcc == n.MaNcc ||
                        (p.MaNcc == null && string.Equals(p.NhaCungCap, n.TenNcc, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var phieuDaNhap = phieuCuaNcc.Where(p => p.TrangThai == "DA_NHAP_KHO").ToList();

                return new NhaCungCapItemViewModel
                {
                    MaNcc = n.MaNcc,
                    MaCodeNcc = n.MaCodeNcc,
                    TenNcc = n.TenNcc,
                    LoaiNcc = n.LoaiNcc,
                    ThuongHieuCungCap = n.ThuongHieuCungCap,
                    PhanTramChietKhauMacDinh = Math.Round(n.PhanTramChietKhauMacDinh, 2),
                    SoDienThoai = n.SoDienThoai,
                    Email = n.Email,
                    DiaChi = n.DiaChi,
                    GhiChu = n.GhiChu,
                    DangHoatDong = n.DangHoatDong,
                    NgayTao = n.NgayTao,
                    SoPhieuNhap = phieuCuaNcc.Count,
                    TongGiaTriDaNhap = phieuDaNhap.Sum(p => p.TongPhaiTra),
                    TongDaThanhToan = phieuDaNhap.Sum(p => p.DaThanhToan),
                    DanhSachPhieuGanDay = phieuCuaNcc.Take(5).Select(p => new PhieuNhapTomTatNccViewModel
                    {
                        MaPn = p.MaPn,
                        SoPhieuNhap = p.SoPhieuNhap,
                        NgayNhap = p.NgayNhap,
                        TongPhaiTra = p.TongPhaiTra,
                        DaThanhToan = p.DaThanhToan,
                        TrangThai = p.TrangThai,
                        TrangThaiThanhToan = p.TrangThaiThanhToan
                    }).ToList()
                };
            }).ToList();

            string loaiChuan = ChuanHoaChuoi(locLoaiNcc, "ALL").ToUpperInvariant();
            string ttChuan = ChuanHoaChuoi(locTrangThai, "ALL").ToUpperInvariant();
            string tkChuan = ChuanHoaChuoi(tuKhoa);

            var ketQuaLoc = danhSachItem.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tkChuan))
            {
                ketQuaLoc = ketQuaLoc.Where(n =>
                    n.MaCodeNcc.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || n.TenNcc.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || n.ThuongHieuCungCap.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || (n.SoDienThoai ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || (n.Email ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase));
            }
            if (loaiChuan != "ALL")
            {
                ketQuaLoc = ketQuaLoc.Where(n => n.LoaiNcc == loaiChuan);
            }
            if (ttChuan == "ACTIVE")
            {
                ketQuaLoc = ketQuaLoc.Where(n => n.DangHoatDong);
            }
            else if (ttChuan == "INACTIVE")
            {
                ketQuaLoc = ketQuaLoc.Where(n => !n.DangHoatDong);
            }

            var nccHoatDong = danhSachItem.Where(n => n.DangHoatDong).ToList();

            return new NhaCungCapAdminViewModel
            {
                DanhSachNhaCungCap = ketQuaLoc.ToList(),
                DanhSachThuongHieu = dsThuongHieu,
                TuKhoa = tuKhoa,
                LocLoaiNcc = loaiChuan,
                LocTrangThai = ttChuan,
                TongSoNcc = danhSachItem.Count,
                SoHangChinhHang = danhSachItem.Count(n => n.LoaiNcc == "HANG_CHINH_HANG"),
                SoDaiLyDieuTiet = danhSachItem.Count(n => n.LoaiNcc == "DAI_LY_DIEU_TIET"),
                ChietKhauTrungBinh = nccHoatDong.Count > 0
                    ? Math.Round(nccHoatDong.Average(n => n.PhanTramChietKhauMacDinh), 1)
                    : 0m,
                TongGiaTriDaNhap = danhSachItem.Sum(n => n.TongGiaTriDaNhap),
                TongCongNoPhaiTra = danhSachItem.Sum(n => n.CongNoConPhaiTra)
            };
        }

        public async Task<List<NhaCungCapOptionViewModel>> LayDanhSachNccHoatDongAsync()
        {
            return await _db.NhaCungCaps
                .AsNoTracking()
                .Where(n => n.DangHoatDong)
                .OrderBy(n => n.MaCodeNcc)
                .ThenBy(n => n.TenNcc)
                .Select(n => new NhaCungCapOptionViewModel
                {
                    MaNcc = n.MaNcc,
                    MaCodeNcc = n.MaCodeNcc,
                    TenNcc = n.TenNcc,
                    LoaiNcc = n.LoaiNcc,
                    ThuongHieuCungCap = n.ThuongHieuCungCap,
                    PhanTramChietKhauMacDinh = n.PhanTramChietKhauMacDinh
                })
                .ToListAsync();
        }

        public async Task<object?> LayChiTietNhaCungCapJsonAsync(int maNcc)
        {
            var ncc = await _db.NhaCungCaps
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.MaNcc == maNcc);

            if (ncc == null) return null;

            return new
            {
                maNcc = ncc.MaNcc,
                maCodeNcc = ncc.MaCodeNcc,
                tenNcc = ncc.TenNcc,
                loaiNcc = ncc.LoaiNcc,
                thuongHieuCungCap = ncc.ThuongHieuCungCap,
                phanTramChietKhauMacDinh = Math.Round(ncc.PhanTramChietKhauMacDinh, 2),
                soDienThoai = ncc.SoDienThoai ?? string.Empty,
                email = ncc.Email ?? string.Empty,
                diaChi = ncc.DiaChi ?? string.Empty,
                ghiChu = ncc.GhiChu ?? string.Empty,
                dangHoatDong = ncc.DangHoatDong
            };
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuNhaCungCapAsync(LuuNhaCungCapRequest req)
        {
            string ten = ChuanHoaChuoi(req.TenNcc);
            if (string.IsNullOrWhiteSpace(ten))
            {
                return (false, "Vui lòng nhập Tên nhà cung cấp!");
            }

            if (req.PhanTramChietKhauMacDinh < 0m || req.PhanTramChietKhauMacDinh > 90m)
            {
                return (false, "Mức chiết khấu nhập mặc định phải nằm trong khoảng từ 0% đến 90%.");
            }

            string loaiChuan = string.Equals(req.LoaiNcc?.Trim(), "DAI_LY_DIEU_TIET", StringComparison.OrdinalIgnoreCase)
                ? "DAI_LY_DIEU_TIET"
                : "HANG_CHINH_HANG";
            string thuongHieu = ChuanHoaChuoi(req.ThuongHieuCungCap, "Đa thương hiệu");
            decimal ckChuan = Math.Round(req.PhanTramChietKhauMacDinh, 2);

            string maCode = ChuanHoaChuoi(req.MaCodeNcc).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(maCode))
            {
                maCode = await SinhMaCodeNccTuDongAsync();
            }
            if (maCode.Length > 30) maCode = maCode[..30];

            bool trungMa = await _db.NhaCungCaps
                .AsNoTracking()
                .AnyAsync(n => n.MaCodeNcc.ToUpper() == maCode && n.MaNcc != req.MaNcc);
            if (trungMa)
            {
                return (false, $"Mã nhà cung cấp [{maCode}] đã tồn tại trong hệ thống.");
            }

            if (req.MaNcc > 0)
            {
                var nccCu = await _db.NhaCungCaps.FirstOrDefaultAsync(n => n.MaNcc == req.MaNcc);
                if (nccCu == null)
                {
                    return (false, "Không tìm thấy nhà cung cấp cần cập nhật.");
                }

                nccCu.MaCodeNcc = maCode;
                nccCu.TenNcc = ten.Length > 200 ? ten[..200] : ten;
                nccCu.LoaiNcc = loaiChuan;
                nccCu.ThuongHieuCungCap = thuongHieu.Length > 100 ? thuongHieu[..100] : thuongHieu;
                nccCu.PhanTramChietKhauMacDinh = ckChuan;
                nccCu.SoDienThoai = ChuanHoaTruongTuyChon(req.SoDienThoai, 20);
                nccCu.Email = ChuanHoaTruongTuyChon(req.Email, 150);
                nccCu.DiaChi = ChuanHoaTruongTuyChon(req.DiaChi, 300);
                nccCu.GhiChu = ChuanHoaTruongTuyChon(req.GhiChu, 500);
                nccCu.DangHoatDong = req.DangHoatDong;

                await _db.SaveChangesAsync();
                return (true, $"Đã cập nhật nhà cung cấp [{nccCu.MaCodeNcc}] {nccCu.TenNcc} (Chiết khấu mặc định: {nccCu.PhanTramChietKhauMacDinh:0.##}%).");
            }

            var nccMoi = new NhaCungCap
            {
                MaCodeNcc = maCode,
                TenNcc = ten.Length > 200 ? ten[..200] : ten,
                LoaiNcc = loaiChuan,
                ThuongHieuCungCap = thuongHieu.Length > 100 ? thuongHieu[..100] : thuongHieu,
                PhanTramChietKhauMacDinh = ckChuan,
                SoDienThoai = ChuanHoaTruongTuyChon(req.SoDienThoai, 20),
                Email = ChuanHoaTruongTuyChon(req.Email, 150),
                DiaChi = ChuanHoaTruongTuyChon(req.DiaChi, 300),
                GhiChu = ChuanHoaTruongTuyChon(req.GhiChu, 500),
                DangHoatDong = req.DangHoatDong,
                NgayTao = LayThoiGianHeThong()
            };

            _db.NhaCungCaps.Add(nccMoi);
            await _db.SaveChangesAsync();

            return (true, $"Đã thêm mới nhà cung cấp [{nccMoi.MaCodeNcc}] {nccMoi.TenNcc} (CK: {nccMoi.PhanTramChietKhauMacDinh:0.##}%).");
        }

        public async Task<(bool ThanhCong, string ThongBao)> XoaHoacNgungNhaCungCapAsync(int maNcc)
        {
            var ncc = await _db.NhaCungCaps.FirstOrDefaultAsync(n => n.MaNcc == maNcc);
            if (ncc == null)
            {
                return (false, "Không tìm thấy nhà cung cấp.");
            }

            bool dangLienKet = await _db.PhieuNhapKhos
                .AsNoTracking()
                .AnyAsync(p => p.MaNcc == maNcc || p.NhaCungCap == ncc.TenNcc)
                || await _db.SanPhamBanhs.AsNoTracking().AnyAsync(b => b.MaNccMacDinh == maNcc)
                || await _db.PhieuKhoVoHops.AsNoTracking().AnyAsync(p => p.MaNcc == maNcc);

            if (dangLienKet)
            {
                if (!ncc.DangHoatDong)
                {
                    return (true, $"Nhà cung cấp [{ncc.MaCodeNcc}] đã được đánh dấu ngừng hợp tác trước đó.");
                }
                ncc.DangHoatDong = false;
                await _db.SaveChangesAsync();
                return (true, $"[{ncc.MaCodeNcc}] {ncc.TenNcc} đã phát sinh chứng từ/sản phẩm liên kết nên được chuyển sang trạng thái Ngừng hợp tác để bảo toàn lịch sử.");
            }

            string maTen = $"[{ncc.MaCodeNcc}] {ncc.TenNcc}";
            _db.NhaCungCaps.Remove(ncc);
            await _db.SaveChangesAsync();
            return (true, $"Đã xóa vĩnh viễn nhà cung cấp {maTen}.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> KhoiPhucNhaCungCapAsync(int maNcc)
        {
            var ncc = await _db.NhaCungCaps.FirstOrDefaultAsync(n => n.MaNcc == maNcc);
            if (ncc == null)
            {
                return (false, "Không tìm thấy nhà cung cấp.");
            }

            ncc.DangHoatDong = true;
            await _db.SaveChangesAsync();
            return (true, $"Đã kích hoạt lại hợp tác với nhà cung cấp [{ncc.MaCodeNcc}] {ncc.TenNcc}.");
        }

        private async Task<string> SinhMaCodeNccTuDongAsync()
        {
            var dsCode = await _db.NhaCungCaps
                .AsNoTracking()
                .Select(n => n.MaCodeNcc)
                .ToListAsync();

            int maxSo = 0;
            foreach (var code in dsCode)
            {
                if (code.StartsWith("NCC", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(code[3..], out int so) && so > maxSo)
                {
                    maxSo = so;
                }
            }

            return $"NCC{(maxSo + 1):D3}";
        }

        private static string? ChuanHoaTruongTuyChon(string? giaTri, int doDaiToiDa)
        {
            if (string.IsNullOrWhiteSpace(giaTri)) return null;
            string sach = giaTri.Trim();
            return sach.Length > doDaiToiDa ? sach[..doDaiToiDa] : sach;
        }
    }
}
