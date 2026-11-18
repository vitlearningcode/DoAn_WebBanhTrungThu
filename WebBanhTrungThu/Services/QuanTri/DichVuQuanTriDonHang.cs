using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Đơn hàng Đại lý Nguyệt Cát ERP:
    /// Lọc danh sách đơn hàng, xác nhận chuyển khoản QR, cập nhật trạng thái điều phối giao hàng đa điểm,
    /// hủy đơn hoàn kho và lập phiếu chi hoàn tiền vào Sổ quỹ. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuQuanTriDonHang : DichVuCoSo
    {
        private readonly DichVuCauHinhHeThong _cauHinhHeThong;

        public DichVuQuanTriDonHang(CSDLBanhTrungThuContext db, DichVuCauHinhHeThong cauHinhHeThong) : base(db)
        {
            _cauHinhHeThong = cauHinhHeThong;
        }

        public async Task<List<DonHang>> LayDanhSachDonHangLocAsync(
            string? tuKhoa,
            string? locTrangThai,
            string? locLoaiDon,
            string? locPhuongThuc,
            string? locThoiGian)
        {
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
                if (locTrangThai == "CHO_LAY_HANG" || locTrangThai == "DANG_DONG_GOI_HOP")
                {
                    query = query.Where(d => d.TrangThaiDh == "CHO_LAY_HANG" || d.TrangThaiDh == "DANG_DONG_GOI_HOP");
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
            var store = _cauHinhHeThong.DocDuLieu();
            foreach (var dh in danhSach)
            {
                _cauHinhHeThong.GanMetadataChoDonHang(dh, store);
            }

            return danhSach;
        }

        public async Task<DonHangThanhCongViewModel?> XayDungChiTietDonHangQrAsync(int id, string scheme, string host, bool asNoTracking = false)
        {
            IQueryable<DonHang> query = _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs);

            if (asNoTracking) query = query.AsNoTracking();

            var donHang = await query.FirstOrDefaultAsync(d => d.MaDh == id);
            if (donHang == null) return null;

            _cauHinhHeThong.GanMetadataChoDonHang(donHang, _cauHinhHeThong.DocDuLieu());

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

        public async Task<string?> XacNhanChuyenKhoanQrAsync(int maDh, string nguoiCapNhat)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang == null || donHang.TrangThaiDh == "DA_HUY") return null;

            donHang.TrangThaiTt = "DA_TT";
            if (donHang.TrangThaiDh == "CHO_XAC_NHAN" || donHang.TrangThaiDh == "DANG_DONG_GOI_HOP")
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

            await DichVuHoiVien.CongDiemThuongDonHangAsync(_db, _cauHinhHeThong, donHang);

            _cauHinhHeThong.CapNhatMetadataDonHang(donHang.MaDh, meta =>
            {
                meta.ThoiGianCapNhatCuoi = LayThoiGianHeThong().ToString("dd/MM/yyyy HH:mm");
                meta.NguoiCapNhatCuoi = nguoiCapNhat;
            });

            _cauHinhHeThong.GhiNhanPhieuThuChi(new PhieuThuChiMoRong
            {
                MaDonHang = donHang.MaDh,
                MaTraCuuDh = donHang.MaTraCuuDh,
                LoaiPhieu = "THU",
                PhuongThuc = donHang.PhuongThucTt,
                SoTien = donHang.TongThanhToanCuoi,
                NguoiNopNhan = donHang.HoTenNguoiMua,
                NoiDung = $"Thu xác nhận chuyển khoản QR đơn #{donHang.MaTraCuuDh}",
                ThoiGian = LayThoiGianHeThong()
            });

            return $"⚡ Đã xác nhận nhận chuyển khoản QR cho đơn #{donHang.MaTraCuuDh} ({donHang.TongThanhToanCuoi:N0} đ) -> Tự động đẩy sang trạng thái Chờ lấy hàng (CHO_LAY_HANG)!";
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

            if (!string.IsNullOrWhiteSpace(trangThaiDh))
            {
                donHang.TrangThaiDh = trangThaiDh;
                foreach (var dot in donHang.DotGiaoHangDaDiems)
                {
                    dot.TrangThaiGiao = trangThaiDh;
                }

                if (trangThaiDh == "HOAN_TAT" && string.IsNullOrWhiteSpace(trangThaiTt) && donHang.TrangThaiTt == "CHUA_TT")
                {
                    donHang.TrangThaiTt = "DA_TT";
                }
            }

            if (!string.IsNullOrWhiteSpace(trangThaiTt))
            {
                donHang.TrangThaiTt = trangThaiTt;
            }

            await _db.SaveChangesAsync();

            if (string.Equals(donHang.TrangThaiDh, "DA_HUY", StringComparison.OrdinalIgnoreCase))
            {
                await DichVuHoiVien.HoanDiemKhiHuyDonAsync(_db, _cauHinhHeThong, donHang);
            }
            else if (string.Equals(donHang.TrangThaiDh, "DA_GIAO", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(donHang.TrangThaiDh, "HOAN_TAT", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(donHang.TrangThaiTt, "DA_TT", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(donHang.TrangThaiTt, "DA_THANH_TOAN", StringComparison.OrdinalIgnoreCase))
            {
                await DichVuHoiVien.CongDiemThuongDonHangAsync(_db, _cauHinhHeThong, donHang);
            }

            _cauHinhHeThong.CapNhatMetadataDonHang(donHang.MaDh, meta =>
            {
                meta.ThoiGianCapNhatCuoi = LayThoiGianHeThong().ToString("dd/MM/yyyy HH:mm");
                meta.NguoiCapNhatCuoi = nguoiCapNhat;
                if (!string.IsNullOrWhiteSpace(ghiChuDieuChinh))
                {
                    meta.GhiChuDieuChinhAdmin = ghiChuDieuChinh.Trim();
                }
            });

            string nhanCheDo = laAdminOverride ? "[Admin Override] " : "";
            return $"{nhanCheDo}Đã cập nhật hóa đơn #{donHang.MaTraCuuDh} -> Trạng thái đơn: {donHang.TrangThaiDh} | Thanh toán: {donHang.TrangThaiTt}!";
        }

        public async Task<string?> HuyDonVaHoanTienAsync(
            int maDh,
            string? maNganHangHoan,
            string? soTaiKhoanHoan,
            string? tenChuTkHoan,
            string? lyDoHuy,
            string nguoiThucHien)
        {
            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (donHang == null) return null;

            if (donHang.TrangThaiDh == "DA_HUY")
            {
                return $"Đơn hàng #{donHang.MaTraCuuDh} đã ở trạng thái Đã hủy trước đó.";
            }

            bool canHoanTien = donHang.TrangThaiTt == "DA_TT" || donHang.TrangThaiTt == "DA_THANH_TOAN";
            if (canHoanTien)
            {
                if (string.IsNullOrWhiteSpace(maNganHangHoan) || string.IsNullOrWhiteSpace(soTaiKhoanHoan) || string.IsNullOrWhiteSpace(tenChuTkHoan))
                {
                    return $"⚠️ Đơn hàng #{donHang.MaTraCuuDh} đã thanh toán ({donHang.TongThanhToanCuoi:N0} đ). Bắt buộc phải nhập Ngân hàng, Số tài khoản và Tên chủ tài khoản để hoàn tiền trước khi hủy!";
                }

                donHang.TrangThaiTt = "DA_HOAN_TIEN";
            }

            donHang.TrangThaiDh = "DA_HUY";
            foreach (var dot in donHang.DotGiaoHangDaDiems)
            {
                dot.TrangThaiGiao = "DA_HUY";
            }

            int soBanhHoanKho = 0;
            int soVoHopHoanKho = 0;

            foreach (var ct in donHang.ChiTietDonHangs)
            {
                if (ct.MaBanh.HasValue)
                {
                    var banh = await _db.SanPhamBanhs.FindAsync(ct.MaBanh.Value);
                    if (banh != null)
                    {
                        banh.TongTonKho += ct.SoLuong;
                        soBanhHoanKho += ct.SoLuong;
                    }
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
                            var banh = await _db.SanPhamBanhs.FindAsync(cth.MaBanh);
                            if (banh != null)
                            {
                                int slCong = ct.SoLuong;
                                banh.TongTonKho += slCong;
                                soBanhHoanKho += slCong;
                            }
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
                            var banh = await _db.SanPhamBanhs.FindAsync(ctc.MaBanh);
                            if (banh != null)
                            {
                                int slCong = ctc.SoLuong * ct.SoLuong;
                                banh.TongTonKho += slCong;
                                soBanhHoanKho += slCong;
                            }
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();

            await DichVuHoiVien.HoanDiemKhiHuyDonAsync(_db, _cauHinhHeThong, donHang);

            _cauHinhHeThong.CapNhatMetadataDonHang(donHang.MaDh, meta =>
            {
                meta.MaNganHangHoan = maNganHangHoan?.Trim();
                meta.SoTaiKhoanHoan = soTaiKhoanHoan?.Trim();
                meta.TenChuTkHoan = tenChuTkHoan?.Trim().ToUpperInvariant();
                meta.LyDoHuyHoan = ChuanHoaChuoi(lyDoHuy, "Khách yêu cầu hủy đơn");
                meta.ThoiGianCapNhatCuoi = LayThoiGianHeThong().ToString("dd/MM/yyyy HH:mm");
                meta.NguoiCapNhatCuoi = nguoiThucHien;
            });

            if (canHoanTien)
            {
                _cauHinhHeThong.GhiNhanPhieuThuChi(new PhieuThuChiMoRong
                {
                    MaPhieu = $"PCH-{LayThoiGianHeThong():yyMMddHHmmss}",
                    MaDonHang = donHang.MaDh,
                    MaTraCuuDh = donHang.MaTraCuuDh,
                    LoaiPhieu = "CHI",
                    PhuongThuc = "CHUYEN_KHOAN",
                    SoTien = donHang.TongThanhToanCuoi,
                    NguoiNopNhan = donHang.HoTenNguoiMua,
                    NoiDung = $"Chi hoàn tiền hủy đơn #{donHang.MaTraCuuDh} ({maNganHangHoan} - STK: {soTaiKhoanHoan})",
                    ThoiGian = LayThoiGianHeThong()
                });

                return $"🗑️ Đã hủy đơn #{donHang.MaTraCuuDh}, hoàn kho ({soBanhHoanKho} bánh, {soVoHopHoanKho} vỏ hộp) và ghi nhận Phiếu Chi Hoàn Tiền {donHang.TongThanhToanCuoi:N0} đ tới {maNganHangHoan} - STK: {soTaiKhoanHoan} ({tenChuTkHoan}) vào Sổ Quỹ!";
            }

            return $"🗑️ Đã hủy đơn #{donHang.MaTraCuuDh} (hủy {donHang.DotGiaoHangDaDiems.Count} phiếu giao, hoàn kho {soBanhHoanKho} bánh & {soVoHopHoanKho} vỏ hộp) thành công!";
        }
    }
}
