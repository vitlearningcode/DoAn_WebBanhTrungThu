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
                "DA_XAC_NHAN" => moi is "CHO_LAY_HANG" or "DA_HUY",
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
                return $"🔒 Đơn hàng #{donHang.MaTraCuuDh} đang ở trạng thái khóa cứng ({donHang.TrangThaiDh}), không thể xác nhận thanh toán!";
            }

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

            if (LaTrangThaiKhoaCung(donHang.TrangThaiDh))
            {
                return $"🔒 Đơn hàng #{donHang.MaTraCuuDh} đang ở trạng thái khóa cứng ({donHang.TrangThaiDh}). TUYỆT ĐỐI không cho phép chuyển ngược trạng thái!";
            }

            if (!string.IsNullOrWhiteSpace(trangThaiDh))
            {
                string trangThaiMoiChuan = trangThaiDh.Trim().ToUpperInvariant();
                if (!KiemTraChuyenTrangThaiHopLe(donHang.TrangThaiDh, trangThaiMoiChuan, laAdminOverride))
                {
                    return $"⚠️ Chuyển trạng thái không hợp lệ cho đơn #{donHang.MaTraCuuDh}: Không thể chuyển từ {donHang.TrangThaiDh} sang {trangThaiMoiChuan}!";
                }

                donHang.TrangThaiDh = trangThaiMoiChuan;
                foreach (var dot in donHang.DotGiaoHangDaDiems)
                {
                    dot.TrangThaiGiao = trangThaiMoiChuan;
                }

                bool daGiaoHoacHoanTat = trangThaiMoiChuan == "DA_GIAO" || trangThaiMoiChuan == "HOAN_TAT";
                if (daGiaoHoacHoanTat)
                {
                    if (string.Equals(donHang.PhuongThucTt, "COD", StringComparison.OrdinalIgnoreCase))
                    {
                        donHang.TrangThaiTt = "DA_THANH_TOAN";
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
                        donHang.TrangThaiTt = "DA_THANH_TOAN";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(trangThaiTt)
                && !(string.Equals(donHang.PhuongThucTt, "COD", StringComparison.OrdinalIgnoreCase)
                     && (donHang.TrangThaiDh == "DA_GIAO" || donHang.TrangThaiDh == "HOAN_TAT")))
            {
                donHang.TrangThaiTt = trangThaiTt.Trim().ToUpperInvariant();
            }

            await _db.SaveChangesAsync();

            if (LaTrangThaiKhoaCung(donHang.TrangThaiDh))
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
                lo = await _db.LoSanXuatBanhs.FirstOrDefaultAsync(l => l.MaLo == maLoUuTien.Value);
            }

            lo ??= await _db.LoSanXuatBanhs
                .Where(l => l.MaBanh == maBanh && l.TrangThaiLo != "HET_HAN")
                .OrderByDescending(l => l.HanSuDung)
                .FirstOrDefaultAsync();

            lo ??= await _db.LoSanXuatBanhs
                .Where(l => l.MaBanh == maBanh)
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
            foreach (var dot in donHang.DotGiaoHangDaDiems)
            {
                dot.TrangThaiGiao = trangThaiDich;
            }

            int soBanhHoanKho = 0;
            int soVoHopHoanKho = 0;
            var metaDon = _cauHinhHeThong.LayMetadataDonHang(donHang.MaDh);

            if (!laXuatHuyHangLoi)
            {
                if (metaDon.DanhSachLoDaTru != null && metaDon.DanhSachLoDaTru.Any())
                {
                    foreach (var nhatKyLo in metaDon.DanhSachLoDaTru)
                    {
                        await HoanNhapLoSanXuatAsync(nhatKyLo.MaBanh, nhatKyLo.SoLuongDaTru, nhatKyLo.MaLo);
                        soBanhHoanKho += nhatKyLo.SoLuongDaTru;
                    }

                    if (metaDon.DanhSachVoHopDaTru != null && metaDon.DanhSachVoHopDaTru.Any())
                    {
                        foreach (var nhatKyVo in metaDon.DanhSachVoHopDaTru)
                        {
                            var voHop = await _db.MauVoHops.FindAsync(nhatKyVo.MaVh);
                            if (voHop != null)
                            {
                                voHop.SltonKhoVo += nhatKyVo.SoLuongDaTru;
                                soVoHopHoanKho += nhatKyVo.SoLuongDaTru;
                            }
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

            await _db.SaveChangesAsync();

            await DichVuHoiVien.HoanDiemKhiHuyDonAsync(_db, _cauHinhHeThong, donHang);

            _cauHinhHeThong.CapNhatMetadataDonHang(donHang.MaDh, meta =>
            {
                meta.MaNganHangHoan = maNganHangHoan?.Trim();
                meta.SoTaiKhoanHoan = soTaiKhoanHoan?.Trim();
                meta.TenChuTkHoan = tenChuTkHoan?.Trim().ToUpperInvariant();
                meta.LyDoHuyHoan = lyDoLuu;
                if (laXuatHuyHangLoi)
                {
                    meta.GhiChuDieuChinhAdmin = $"[Xuất hủy hàng lỗi] {lyDoSach}";
                }
                meta.ThoiGianCapNhatCuoi = LayThoiGianHeThong().ToString("dd/MM/yyyy HH:mm");
                meta.NguoiCapNhatCuoi = nguoiThucHien;
            });

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
