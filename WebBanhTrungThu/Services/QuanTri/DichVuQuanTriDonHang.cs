using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Đơn hàng Đại lý Nguyệt Cát ERP:
    /// Lọc danh sách đơn hàng, xác nhận chuyển khoản QR, cập nhật trạng thái điều phối giao hàng đa điểm,
    /// hủy đơn hoàn kho và lập phiếu chi hoàn tiền vào Sổ quỹ. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuQuanTriDonHang : DichVuCoSo, IDichVuQuanTriDonHang
    {
        private readonly IDichVuCauHinhHeThong _cauHinhHeThong;
        private readonly IDichVuCongThanhToanMoMo _dichVuCongMoMo;

        public DichVuQuanTriDonHang(
            CSDLBanhTrungThuContext db,
            IDichVuCauHinhHeThong cauHinhHeThong,
            IDichVuCongThanhToanMoMo dichVuCongMoMo) : base(db)
        {
            _cauHinhHeThong = cauHinhHeThong;
            _dichVuCongMoMo = dichVuCongMoMo;
        }

        public async Task<List<DonHang>> LayDanhSachDonHangLocAsync(
            string? tuKhoa,
            string? locTrangThai,
            string? locLoaiDon,
            string? locPhuongThuc,
            string? locThoiGian)
        {
            await _dichVuCongMoMo.KiemTraVaHuyDonQrQuaHanAsync();

            var query = _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                query = query.Where(d =>
                    d.MaTraCuuDh.Contains(tk) ||
                    d.HoTenNguoiMua.Contains(tk) ||
                    d.SdtnguoiMua.Contains(tk) ||
                    d.DotGiaoHangDaDiems.Any(g => g.TenNguoiNhan.Contains(tk) || g.SdtnguoiNhan.Contains(tk)));
            }

            if (!string.IsNullOrWhiteSpace(locTrangThai) && locTrangThai != "ALL")
            {
                if (locTrangThai == "GIAO_HANG")
                {
                    query = query.Where(d => d.TrangThaiDh == "CHO_LAY_HANG" || d.TrangThaiDh == "DANG_DONG_GOI_HOP" || d.TrangThaiDh == "DA_XAC_NHAN" || d.TrangThaiDh == "DANG_GIAO");
                }
                else if (locTrangThai == "CHO_LAY_HANG" || locTrangThai == "DANG_DONG_GOI_HOP")
                {
                    query = query.Where(d => d.TrangThaiDh == "CHO_LAY_HANG" || d.TrangThaiDh == "DANG_DONG_GOI_HOP" || d.TrangThaiDh == "DA_XAC_NHAN");
                }
                else if (locTrangThai == "CHO_XAC_NHAN" || locTrangThai == "CHO_DUYET")
                {
                    query = query.Where(d => d.TrangThaiDh == "CHO_XAC_NHAN" || d.TrangThaiDh == "CHO_DUYET");
                }
                else
                {
                    query = query.Where(d => d.TrangThaiDh == locTrangThai);
                }
            }

            if (locLoaiDon == "B2B") query = query.Where(d => d.LaDonDoanhNghiepB2b);
            else if (locLoaiDon == "B2C") query = query.Where(d => !d.LaDonDoanhNghiepB2b);
            else if (locLoaiDon == "GUEST") query = query.Where(d => !d.MaNd.HasValue);
            else if (locLoaiDon == "MEMBER") query = query.Where(d => d.MaNd.HasValue);

            if (!string.IsNullOrWhiteSpace(locPhuongThuc) && locPhuongThuc != "ALL")
            {
                query = query.Where(d => d.PhuongThucTt == locPhuongThuc);
            }

            if (locThoiGian == "HOM_NAY")
            {
                DateTime homNay = DateTime.Today;
                query = query.Where(d => d.NgayDat >= homNay);
            }
            else if (locThoiGian == "THANG_NAY")
            {
                DateTime dauThang = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                query = query.Where(d => d.NgayDat >= dauThang);
            }

            var danhSach = await query.OrderByDescending(d => d.NgayDat).ToListAsync();
            if (danhSach.Count > 0)
            {
                var danhSachMaDon = danhSach.Select(d => d.MaDh).ToList();
                var danhSachThiep = await _db.ThiepChucMung3ds
                    .AsNoTracking()
                    .Where(t => danhSachMaDon.Contains(t.MaDonHang))
                    .ToListAsync();

                var thiepTheoDon = danhSachThiep.ToLookup(t => t.MaDonHang);
                foreach (var dh in danhSach)
                {
                    GanThiep3DChoDonHangTrongBoNho(dh, thiepTheoDon[dh.MaDh]);
                }
            }

            return danhSach;
        }

        private static void GanThiep3DChoDonHangTrongBoNho(DonHang donHang, IEnumerable<ThiepChucMung3d> danhSachThiep)
        {
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
        }

        public async Task<DonHangThanhCongViewModel?> XayDungChiTietDonHangQrAsync(int id, string scheme, string host, bool asNoTracking = false)
        {
            await _dichVuCongMoMo.KiemTraVaHuyDonQrQuaHanAsync(id);

            IQueryable<DonHang> query = _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs);

            if (asNoTracking) query = query.AsNoTracking();

            var donHang = await query.FirstOrDefaultAsync(d => d.MaDh == id);
            if (donHang == null) return null;

            var danhSachThiep = await _db.ThiepChucMung3ds
                .AsNoTracking()
                .Where(t => t.MaDonHang == donHang.MaDh)
                .ToListAsync();
            GanThiep3DChoDonHangTrongBoNho(donHang, danhSachThiep);

            var vm = new DonHangThanhCongViewModel
            {
                DonHang = donHang
            };

            using var qrGenerator = new QRCodeGenerator();
            foreach (var dot in donHang.DotGiaoHangDaDiems)
            {
                string urlThiep = string.Empty;
                string qrBase64 = string.Empty;

                if (dot.CoKemThiepQr && !string.IsNullOrWhiteSpace(dot.MaTokenThiepQr))
                {
                    urlThiep = $"{scheme}://{host}/Thiep3D/Xem/{dot.MaTokenThiepQr}";
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

            return vm;
        }

        private static bool LaTrangThaiKhoaCung(string? trangThaiDh) =>
            string.Equals(trangThaiDh, "DA_HUY", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trangThaiDh, "HOAN_TIEN_TRA_HANG", StringComparison.OrdinalIgnoreCase);

        private static string ChuanHoaTrangThaiDon(string? trangThaiDh) =>
            string.Equals(trangThaiDh, "DANG_DONG_GOI_HOP", StringComparison.OrdinalIgnoreCase)
                ? "CHO_LAY_HANG"
                : (trangThaiDh ?? string.Empty).Trim().ToUpperInvariant();

        private static bool KiemTraChuyenTrangThaiHopLe(string trangThaiHienTai, string trangThaiMoi, bool laAdminOverride)
        {
            string hienTai = ChuanHoaTrangThaiDon(trangThaiHienTai);
            string moi = ChuanHoaTrangThaiDon(trangThaiMoi);

            if (LaTrangThaiKhoaCung(hienTai))
            {
                return false;
            }

            if (hienTai == moi)
            {
                return true;
            }

            if ((hienTai == "HOAN_TAT" || hienTai == "DA_GIAO")
                && (moi == "CHO_XAC_NHAN" || moi == "DA_XAC_NHAN" || moi == "CHO_LAY_HANG" || moi == "DANG_GIAO"))
            {
                return false;
            }

            if (laAdminOverride)
            {
                return true;
            }

            return hienTai switch
            {
                "CHO_XAC_NHAN" => moi is "DA_XAC_NHAN" or "CHO_LAY_HANG" or "DA_HUY",
                "DA_XAC_NHAN" => moi is "CHO_LAY_HANG" or "DANG_GIAO" or "DA_HUY",
                "CHO_LAY_HANG" => moi is "DANG_GIAO" or "DA_HUY",
                "DANG_GIAO" => moi is "DA_GIAO" or "HOAN_TAT" or "DA_HUY" or "HOAN_TIEN_TRA_HANG",
                "DA_GIAO" => moi is "HOAN_TAT" or "HOAN_TIEN_TRA_HANG",
                "HOAN_TAT" => moi is "HOAN_TIEN_TRA_HANG",
                _ => false
            };
        }

        public async Task<string?> XacNhanChuyenKhoanQrAsync(int maDh, string nguoiCapNhat)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang == null) return null;
            if (LaTrangThaiKhoaCung(donHang.TrangThaiDh))
            {
                return $"Đơn hàng #{donHang.MaTraCuuDh} đang ở trạng thái khóa cứng ({donHang.TrangThaiDh}), không thể xác nhận thanh toán!";
            }

            string pt = (donHang.PhuongThucTt ?? string.Empty).Trim().ToUpperInvariant();
            bool laDonQrChuyenKhoan = (pt.Contains("QR") || pt.Contains("MOMO") || pt.Contains("CHUYEN_KHOAN"))
                                      && pt != "COD" && pt != "TIEN_MAT";
            if (!laDonQrChuyenKhoan)
            {
                return $"Đơn hàng COD/Tiền mặt (#{donHang.MaTraCuuDh}) không sử dụng thao tác xác nhận chuyển khoản QR.";
            }

            donHang.TrangThaiTt = "DA_TT";
            donHang.SoTienKhachDaTra ??= donHang.TongThanhToanCuoi;
            donHang.ThoiGianCapNhatCuoi = LayThoiGianHeThong();
            donHang.NguoiCapNhatCuoi = nguoiCapNhat;
            if (donHang.TrangThaiDh == "CHO_XAC_NHAN" || donHang.TrangThaiDh == "DANG_DONG_GOI_HOP" || donHang.TrangThaiDh == "DA_XAC_NHAN")
            {
                donHang.TrangThaiDh = "CHO_LAY_HANG";
            }

            foreach (var dot in donHang.DotGiaoHangDaDiems)
            {
                if (dot.TrangThaiGiao != "HOAN_TAT" && dot.TrangThaiGiao != "DA_HUY")
                {
                    dot.TrangThaiGiao = "CHO_LAY_HANG";
                }
            }

            await _db.SaveChangesAsync();

            await DichVuHoiVien.CongDiemThuongDonHangAsync(_db, donHang);

            _cauHinhHeThong.GhiNhanPhieuThuChi(new PhieuThuChiMoRong
            {
                MaDonHang = donHang.MaDh,
                MaTraCuuDh = donHang.MaTraCuuDh,
                LoaiPhieu = "THU",
                PhuongThuc = donHang.PhuongThucTt ?? "CHUYEN_KHOAN",
                SoTien = donHang.TongThanhToanCuoi,
                NguoiNopNhan = donHang.HoTenNguoiMua,
                NoiDung = $"Thu xác nhận chuyển khoản QR đơn #{donHang.MaTraCuuDh}",
                ThoiGian = LayThoiGianHeThong()
            });

            return $"Đã xác nhận nhận chuyển khoản QR cho đơn #{donHang.MaTraCuuDh} ({donHang.TongThanhToanCuoi:N0} đ) và chuyển đơn sang trạng thái Chờ lấy hàng.";
        }

        public async Task<string?> CapNhatTrangThaiDonHangAsync(
            int maDh,
            string? trangThaiDh,
            string? trangThaiTt,
            bool laAdminOverride,
            string? ghiChuDieuChinh,
            string nguoiCapNhat)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang == null) return null;

            if (LaTrangThaiKhoaCung(donHang.TrangThaiDh))
            {
                return $"Đơn hàng #{donHang.MaTraCuuDh} đã ở trạng thái khóa hoàn tất ({donHang.TenTrangThaiDhHienThi}), không thể chuyển ngược trạng thái!";
            }

            if (!string.IsNullOrWhiteSpace(trangThaiDh))
            {
                string trangThaiMoiChuan = trangThaiDh.Trim().ToUpperInvariant();
                if (!KiemTraChuyenTrangThaiHopLe(donHang.TrangThaiDh, trangThaiMoiChuan, laAdminOverride))
                {
                    return $"Chuyển trạng thái không hợp lệ cho đơn #{donHang.MaTraCuuDh}: không thể chuyển từ \"{donHang.TenTrangThaiDhHienThi}\" sang trạng thái mới đã chọn!";
                }

                donHang.TrangThaiDh = trangThaiMoiChuan;
                var cacDotGiaoCapNhat = donHang.DotGiaoHangDaDiems.Any()
                    ? donHang.DotGiaoHangDaDiems.ToList()
                    : await _db.DotGiaoHangDaDiems.Where(g => g.MaDh == donHang.MaDh).ToListAsync();
                foreach (var dot in cacDotGiaoCapNhat)
                {
                    dot.TrangThaiGiao = trangThaiMoiChuan == "DA_HUY" ? "DA_HUY" : trangThaiMoiChuan;
                }

                bool daGiaoHoacHoanTat = trangThaiMoiChuan == "DA_GIAO" || trangThaiMoiChuan == "HOAN_TAT";
                if (daGiaoHoacHoanTat)
                {
                    if (string.Equals(donHang.PhuongThucTt, "COD", StringComparison.OrdinalIgnoreCase))
                    {
                        donHang.TrangThaiTt = "DA_TT";
                        donHang.SoTienKhachDaTra = donHang.TongThanhToanCuoi;
                        bool daCoPhieuThu = await _db.PhieuThuChis.AnyAsync(p =>
                            p.MaDonHangLienKet == donHang.MaDh && p.LoaiPhieu == "THU");

                        if (!daCoPhieuThu)
                        {
                            var bayGio = LayThoiGianHeThong();
                            _db.PhieuThuChis.Add(new PhieuThuChi
                            {
                                MaPhieuHienThi = $"PTH-{bayGio:yyMMddHHmmss}",
                                LoaiPhieu = "THU",
                                HangMuc = "Thu tiền đơn hàng COD",
                                SoTien = donHang.TongThanhToanCuoi,
                                PhuongThuc = "COD",
                                MaDonHangLienKet = donHang.MaDh,
                                NguoiNopNhan = donHang.HoTenNguoiMua,
                                GhiChu = $"Thu tiền đơn hàng COD #{donHang.MaTraCuuDh}",
                                NgayLap = bayGio
                            });
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(trangThaiTt) && donHang.TrangThaiTt == "CHUA_TT")
                    {
                        donHang.TrangThaiTt = "DA_TT";
                        donHang.SoTienKhachDaTra ??= donHang.TongThanhToanCuoi;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(trangThaiTt)
                && !(string.Equals(donHang.PhuongThucTt, "COD", StringComparison.OrdinalIgnoreCase)
                     && (donHang.TrangThaiDh == "DA_GIAO" || donHang.TrangThaiDh == "HOAN_TAT")))
            {
                donHang.TrangThaiTt = trangThaiTt.Trim().ToUpperInvariant();
            }

            if (LaTrangThaiKhoaCung(donHang.TrangThaiDh) && !string.IsNullOrWhiteSpace(ghiChuDieuChinh) && string.IsNullOrWhiteSpace(donHang.LyDoHuy))
            {
                donHang.LyDoHuy = ghiChuDieuChinh.Trim();
            }

            donHang.ThoiGianCapNhatCuoi = LayThoiGianHeThong();
            donHang.NguoiCapNhatCuoi = nguoiCapNhat;
            if (!string.IsNullOrWhiteSpace(ghiChuDieuChinh))
            {
                donHang.GhiChuDieuChinhAdmin = ghiChuDieuChinh.Trim();
            }

            await _db.SaveChangesAsync();

            if (LaTrangThaiKhoaCung(donHang.TrangThaiDh))
            {
                await DichVuHoiVien.HoanDiemKhiHuyDonAsync(_db, donHang);
            }
            else if (string.Equals(donHang.TrangThaiDh, "DA_GIAO", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(donHang.TrangThaiDh, "HOAN_TAT", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(donHang.TrangThaiTt, "DA_TT", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(donHang.TrangThaiTt, "DA_THANH_TOAN", StringComparison.OrdinalIgnoreCase))
            {
                await DichVuHoiVien.CongDiemThuongDonHangAsync(_db, donHang);
            }

            return $"Đã cập nhật đơn hàng #{donHang.MaTraCuuDh} sang trạng thái \"{donHang.TenTrangThaiDhHienThi}\" ({donHang.TenTrangThaiTtHienThi}).";
        }

        private async Task HoanNhapLoSanXuatAsync(int maBanh, int soLuongHoan, int? maLoUuTien = null)
        {
            if (soLuongHoan <= 0) return;

            var banh = await _db.SanPhamBanhs.FindAsync(maBanh);
            if (banh != null)
            {
                banh.TongTonKho += soLuongHoan;
            }

            LoSanXuatBanh? lo = null;
            if (maLoUuTien.HasValue)
            {
                lo = await _db.LoSanXuatBanhs.FirstOrDefaultAsync(l =>
                    l.MaLo == maLoUuTien.Value
                    && l.MaBanh == maBanh
                    && l.TrangThaiLo != "DA_HUY"
                    && l.TrangThaiLo != "HET_HAN");
            }

            lo ??= await _db.LoSanXuatBanhs
                .Where(l => l.MaBanh == maBanh
                         && l.TrangThaiLo != "DA_HUY"
                         && l.TrangThaiLo != "HET_HAN"
                         && (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "SAP_HET_HAN"))
                .OrderBy(l => l.TrangThaiLo == "DANG_BAN" ? 0 : 1)
                .ThenByDescending(l => l.HanSuDung)
                .FirstOrDefaultAsync();

            lo ??= await _db.LoSanXuatBanhs
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

        public async Task<string?> HuyDonVaHoanTienAsync(
            int maDh,
            string? maNganHangHoan,
            string? soTaiKhoanHoan,
            string? tenChuTkHoan,
            string? lyDoHuy,
            string nguoiThucHien,
            bool? hoanNhapKho = null)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang == null) return null;

            if (LaTrangThaiKhoaCung(donHang.TrangThaiDh))
            {
                return $"🔒 Đơn hàng #{donHang.MaTraCuuDh} đã ở trạng thái khóa cứng ({donHang.TrangThaiDh}) trước đó.";
            }

            bool laXuatHuyHangLoi = (hoanNhapKho == false)
                || (!string.IsNullOrWhiteSpace(lyDoHuy) && lyDoHuy.Contains("[XUAT_HUY_LOI]", StringComparison.OrdinalIgnoreCase));

            string lyDoSach = (lyDoHuy ?? string.Empty)
                .Replace("[XUAT_HUY_LOI]", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("[HOAN_NHAP_LO]", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (string.IsNullOrWhiteSpace(lyDoSach))
            {
                lyDoSach = laXuatHuyHangLoi
                    ? "Xuất hủy hàng lỗi / móp hộp"
                    : "Khách yêu cầu hủy đơn / trả hàng hoàn nhập lô";
            }

            string lyDoLuu = laXuatHuyHangLoi
                ? $"{lyDoSach} [Xuất hủy hàng lỗi / móp hộp - Không hoàn nhập tồn kho bán]"
                : $"{lyDoSach} [Hoàn nhập lại vào Lô đang bán]";

            bool laTraHangSauGiao = string.Equals(donHang.TrangThaiDh, "HOAN_TAT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(donHang.TrangThaiDh, "DA_GIAO", StringComparison.OrdinalIgnoreCase);

            bool canHoanTien = donHang.TrangThaiTt == "DA_TT" || donHang.TrangThaiTt == "DA_THANH_TOAN";
            if (canHoanTien)
            {
                if (string.IsNullOrWhiteSpace(maNganHangHoan) || string.IsNullOrWhiteSpace(soTaiKhoanHoan) || string.IsNullOrWhiteSpace(tenChuTkHoan))
                {
                    return $"⚠️ Đơn hàng #{donHang.MaTraCuuDh} đã thanh toán ({donHang.TongThanhToanCuoi:N0} đ). Bắt buộc phải nhập Ngân hàng, Số tài khoản và Tên chủ tài khoản để hoàn tiền trước khi xác nhận!";
                }

                donHang.TrangThaiTt = "DA_HOAN_TIEN";
            }

            string trangThaiDich = laTraHangSauGiao ? "HOAN_TIEN_TRA_HANG" : "DA_HUY";
            donHang.TrangThaiDh = trangThaiDich;
            donHang.LyDoHuy = lyDoLuu;
            var cacDotGiaoHuy = donHang.DotGiaoHangDaDiems.Any()
                ? donHang.DotGiaoHangDaDiems.ToList()
                : await _db.DotGiaoHangDaDiems.Where(g => g.MaDh == donHang.MaDh).ToListAsync();
            foreach (var dot in cacDotGiaoHuy)
            {
                dot.TrangThaiGiao = trangThaiDich == "DA_HUY" ? "DA_HUY" : trangThaiDich;
            }

            int soBanhHoanKho = 0;
            int soVoHopHoanKho = 0;

            if (!laXuatHuyHangLoi && !donHang.DaRollbackKho)
            {
                var nhatKyDb = await _db.NhatKyTruKhoDonHangs
                    .Where(n => n.MaDh == donHang.MaDh && !n.DaHoanKho)
                    .ToListAsync();

                var nhatKyLoDb = nhatKyDb.Where(n => n.LoaiDoiTuong == "LO_BANH" && n.MaBanh.HasValue).ToList();
                var nhatKyVoDb = nhatKyDb.Where(n => n.LoaiDoiTuong == "VO_HOP" && n.MaVh.HasValue).ToList();

                if (nhatKyLoDb.Count > 0 || nhatKyVoDb.Count > 0)
                {
                    foreach (var nkLo in nhatKyLoDb)
                    {
                        await HoanNhapLoSanXuatAsync(nkLo.MaBanh!.Value, nkLo.SoLuongDaTru, nkLo.MaLo);
                        soBanhHoanKho += nkLo.SoLuongDaTru;
                        nkLo.DaHoanKho = true;
                    }

                    if (nhatKyVoDb.Count > 0)
                    {
                        foreach (var nkVo in nhatKyVoDb)
                        {
                            var voHop = await _db.MauVoHops.FindAsync(nkVo.MaVh!.Value);
                            if (voHop != null)
                            {
                                voHop.SltonKhoVo += nkVo.SoLuongDaTru;
                                soVoHopHoanKho += nkVo.SoLuongDaTru;
                            }
                            nkVo.DaHoanKho = true;
                        }
                    }
                    else
                    {
                        foreach (var ct in donHang.ChiTietDonHangs)
                        {
                            if (ct.MaHopTc.HasValue)
                            {
                                var hopTc = await _db.HopQuaTuChons.AsNoTracking().FirstOrDefaultAsync(h => h.MaHopTc == ct.MaHopTc.Value);
                                if (hopTc != null)
                                {
                                    var voHop = await _db.MauVoHops.FindAsync(hopTc.MaVh);
                                    if (voHop != null) { voHop.SltonKhoVo += ct.SoLuong; soVoHopHoanKho += ct.SoLuong; }
                                }
                            }
                            else if (ct.MaCb.HasValue)
                            {
                                var combo = await _db.ComboMauCoSans.AsNoTracking().FirstOrDefaultAsync(c => c.MaCb == ct.MaCb.Value);
                                if (combo != null)
                                {
                                    var voHop = await _db.MauVoHops.FindAsync(combo.MaVh);
                                    if (voHop != null) { voHop.SltonKhoVo += ct.SoLuong; soVoHopHoanKho += ct.SoLuong; }
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var ct in donHang.ChiTietDonHangs)
                    {
                        if (ct.MaBanh.HasValue)
                        {
                            await HoanNhapLoSanXuatAsync(ct.MaBanh.Value, ct.SoLuong);
                            soBanhHoanKho += ct.SoLuong;
                        }
                        else if (ct.MaHopTc.HasValue)
                        {
                            var hopTc = await _db.HopQuaTuChons
                                .Include(h => h.ChiTietHopTuChons)
                                .FirstOrDefaultAsync(h => h.MaHopTc == ct.MaHopTc.Value);

                            if (hopTc != null)
                            {
                                var voHop = await _db.MauVoHops.FindAsync(hopTc.MaVh);
                                if (voHop != null)
                                {
                                    voHop.SltonKhoVo += ct.SoLuong;
                                    soVoHopHoanKho += ct.SoLuong;
                                }

                                foreach (var cth in hopTc.ChiTietHopTuChons)
                                {
                                    await HoanNhapLoSanXuatAsync(cth.MaBanh, ct.SoLuong);
                                    soBanhHoanKho += ct.SoLuong;
                                }
                            }
                        }
                        else if (ct.MaCb.HasValue)
                        {
                            var combo = await _db.ComboMauCoSans
                                .Include(c => c.ChiTietComboMaus)
                                .FirstOrDefaultAsync(c => c.MaCb == ct.MaCb.Value);

                            if (combo != null)
                            {
                                var voHop = await _db.MauVoHops.FindAsync(combo.MaVh);
                                if (voHop != null)
                                {
                                    voHop.SltonKhoVo += ct.SoLuong;
                                    soVoHopHoanKho += ct.SoLuong;
                                }

                                foreach (var ctc in combo.ChiTietComboMaus)
                                {
                                    int slCong = ctc.SoLuong * ct.SoLuong;
                                    await HoanNhapLoSanXuatAsync(ctc.MaBanh, slCong);
                                    soBanhHoanKho += slCong;
                                }
                            }
                        }
                    }
                }

                donHang.DaRollbackKho = true;
            }

            if (canHoanTien)
            {
                bool daCoPhieuChi = await _db.PhieuThuChis.AnyAsync(p =>
                    p.MaDonHangLienKet == donHang.MaDh && p.LoaiPhieu == "CHI");

                if (!daCoPhieuChi)
                {
                    var bayGio = LayThoiGianHeThong();
                    _db.PhieuThuChis.Add(new PhieuThuChi
                    {
                        MaPhieuHienThi = $"PCH-{bayGio:yyMMddHHmmss}",
                        LoaiPhieu = "CHI",
                        HangMuc = "Chi hoàn tiền trả hàng",
                        SoTien = donHang.TongThanhToanCuoi,
                        PhuongThuc = "CHUYEN_KHOAN",
                        MaDonHangLienKet = donHang.MaDh,
                        NguoiNopNhan = donHang.HoTenNguoiMua,
                        GhiChu = $"Chi hoàn tiền trả hàng đơn #{donHang.MaTraCuuDh} ({maNganHangHoan} - STK: {soTaiKhoanHoan}) | {lyDoLuu}",
                        NgayLap = bayGio
                    });
                }
            }

            donHang.MaNganHangHoan = maNganHangHoan?.Trim();
            donHang.SoTaiKhoanHoan = soTaiKhoanHoan?.Trim();
            donHang.TenChuTkHoan = tenChuTkHoan?.Trim().ToUpperInvariant();
            if (laXuatHuyHangLoi)
            {
                donHang.GhiChuDieuChinhAdmin = $"[Xuất hủy hàng lỗi] {lyDoSach}";
            }
            donHang.ThoiGianCapNhatCuoi = LayThoiGianHeThong();
            donHang.NguoiCapNhatCuoi = nguoiThucHien;

            await _db.SaveChangesAsync();

            await DichVuHoiVien.HoanDiemKhiHuyDonAsync(_db, donHang);

            string moTaKho = laXuatHuyHangLoi
                ? "Xuất hủy hàng lỗi / móp hộp (Không hoàn nhập tồn kho bán)"
                : $"Hoàn nhập lô FEFO ({soBanhHoanKho} bánh, {soVoHopHoanKho} vỏ hộp)";

            if (canHoanTien)
            {
                return $"💸 Đã xử lý {trangThaiDich} đơn #{donHang.MaTraCuuDh}, {moTaKho}, thu hồi/hoàn điểm VIP và ghi nhận Phiếu Chi 'Chi hoàn tiền trả hàng' {donHang.TongThanhToanCuoi:N0} đ tới {maNganHangHoan} - STK: {soTaiKhoanHoan} ({tenChuTkHoan}) vào Sổ Quỹ!";
            }

            return $"🗑️ Đã chuyển đơn #{donHang.MaTraCuuDh} sang {trangThaiDich} ({donHang.DotGiaoHangDaDiems.Count} phiếu giao, {moTaKho}) và hoàn lại điểm VIP (nếu có) thành công!";
        }
    }
}
