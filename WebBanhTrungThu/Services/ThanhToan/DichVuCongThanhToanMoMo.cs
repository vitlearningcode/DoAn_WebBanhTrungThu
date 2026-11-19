using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.Services.TaiKhoan;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.ThanhToan
{
    public class DuLieuQrMoMo
    {
        public CauHinhThanhToanQr CauHinhQr { get; set; } = new();
        public string NoiDungCk { get; set; } = string.Empty;
        public string SdtMoMoRaw { get; set; } = string.Empty;
        public string SdtMoMoHienThi { get; set; } = string.Empty;
        public string TenChuTk { get; set; } = string.Empty;
        public string AnhQrThanhToanBase64 { get; set; } = string.Empty;
        public string UrlVietQr { get; set; } = string.Empty;
    }

    public class KetQuaHuyDonHetHan
    {
        public bool ThanhCong { get; set; }
        public bool DaThanhToan { get; set; }
        public string? TrangThaiTt { get; set; }
        public string? TrangThaiDh { get; set; }
        public string ThongBao { get; set; } = string.Empty;
    }

    public class KetQuaXuLyWebhookMoMo
    {
        public int MaTrangThaiHttp { get; set; } = 200;
        public bool ThanhCong { get; set; }
        public string? MaTraCuu { get; set; }
        public string? TrangThaiTt { get; set; }
        public string? TrangThaiDh { get; set; }
        public decimal SoTienDaNhan { get; set; }
        public string ThongBao { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dịch vụ chuyên trách Cổng thanh toán QR MoMo / VietQR: sinh mã QR chuyển khoản chuẩn MoMo,
    /// kiểm tra trạng thái đơn, xử lý Webhook tự động (cộng điểm VIP + ghi sổ quỹ THU)
    /// và hủy đơn quá hạn 15 phút hoàn kho FEFO + hoàn điểm VIP trong SQL Transaction.
    /// </summary>
    public class DichVuCongThanhToanMoMo : DichVuCoSo, IDichVuCongThanhToanMoMo
    {
        private readonly IDichVuCauHinhHeThong _kiotMeta;
        private readonly IDichVuTonKhoFefo _dichVuTonKho;

        public DichVuCongThanhToanMoMo(
            CSDLBanhTrungThuContext context,
            IDichVuCauHinhHeThong kiotMeta,
            IDichVuTonKhoFefo dichVuTonKho) : base(context)
        {
            _kiotMeta = kiotMeta;
            _dichVuTonKho = dichVuTonKho;
        }

        private static string BoDauTiengViet(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string formD = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (char ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            string khongDau = sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D');
            return Regex.Replace(khongDau, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Sinh dữ liệu chuyển khoản và ảnh QR MoMo chuẩn payload 2|99|... (cùng link VietQR nếu có cấu hình ngân hàng).
        /// </summary>
        public DuLieuQrMoMo TaoDuLieuQrMoMo(DonHang donHang, bool sinhAnhQr = true)
        {
            var cauHinhQr = _kiotMeta.LayCauHinhQr();
            string sdtMoMoRaw = Regex.Replace(cauHinhQr.SoTaiKhoanMoMo ?? string.Empty, @"\D", "");
            if (string.IsNullOrWhiteSpace(sdtMoMoRaw)) sdtMoMoRaw = "0814585526";

            string sdtMoMoHienThi = !string.IsNullOrWhiteSpace(cauHinhQr.SoDienThoaiHienThi)
                ? cauHinhQr.SoDienThoaiHienThi.Trim()
                : (sdtMoMoRaw.Length == 10
                    ? $"{sdtMoMoRaw[..4]} {sdtMoMoRaw.Substring(4, 3)} {sdtMoMoRaw.Substring(7, 3)}"
                    : sdtMoMoRaw);

            string tenChuTk = BoDauTiengViet(ChuanHoaChuoi(cauHinhQr.TenChuTaiKhoan, "LE MINH DUC")).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(tenChuTk)) tenChuTk = "LE MINH DUC";

            string noiDungCk = BoDauTiengViet((cauHinhQr.MauNoiDungCk ?? "NGUYETCAT {MaTraCuuDh}")
                .Replace("{MaTraCuuDh}", donHang.MaTraCuuDh, StringComparison.OrdinalIgnoreCase)).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(noiDungCk)) noiDungCk = $"NGUYETCAT {donHang.MaTraCuuDh}".ToUpperInvariant();

            if (!sinhAnhQr)
            {
                return new DuLieuQrMoMo
                {
                    CauHinhQr = cauHinhQr,
                    NoiDungCk = noiDungCk,
                    SdtMoMoRaw = sdtMoMoRaw,
                    SdtMoMoHienThi = sdtMoMoHienThi,
                    TenChuTk = tenChuTk
                };
            }

            long soTienNguyen = (long)Math.Round(donHang.TongThanhToanCuoi, 0);
            string urlVietQr = string.Empty;
            bool coCauHinhNganHang = !string.IsNullOrWhiteSpace(cauHinhQr.SoTaiKhoanNganHang)
                || (!string.Equals(cauHinhQr.TenNganHangVi, "MOMO", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(cauHinhQr.MaBinVietQr));

            if (coCauHinhNganHang)
            {
                string bin = ChuanHoaChuoi(cauHinhQr.MaBinVietQr, "970422");
                string stkNganHang = !string.IsNullOrWhiteSpace(cauHinhQr.SoTaiKhoanNganHang)
                    ? Regex.Replace(cauHinhQr.SoTaiKhoanNganHang, @"\D", "")
                    : sdtMoMoRaw;
                if (!string.IsNullOrWhiteSpace(stkNganHang))
                {
                    urlVietQr = $"https://img.vietqr.io/image/{bin}-{Uri.EscapeDataString(stkNganHang)}-compact2.png?amount={soTienNguyen}&addInfo={Uri.EscapeDataString(noiDungCk)}&accountName={Uri.EscapeDataString(tenChuTk)}";
                }
            }

            using var qrGenerator = new QRCodeGenerator();
            string payloadQrMoMo = $"2|99|{sdtMoMoRaw}|{tenChuTk}||0|0|{soTienNguyen}|{noiDungCk}|transfer_myqr";
            using QRCodeData dataQrTt = qrGenerator.CreateQrCode(payloadQrMoMo, QRCodeGenerator.ECCLevel.H);
            using var pngQrTt = new PngByteQRCode(dataQrTt);
            string qrThanhToanBase64 = "data:image/png;base64," + Convert.ToBase64String(pngQrTt.GetGraphic(10, true));

            return new DuLieuQrMoMo
            {
                CauHinhQr = cauHinhQr,
                NoiDungCk = noiDungCk,
                SdtMoMoRaw = sdtMoMoRaw,
                SdtMoMoHienThi = sdtMoMoHienThi,
                TenChuTk = tenChuTk,
                AnhQrThanhToanBase64 = qrThanhToanBase64,
                UrlVietQr = urlVietQr
            };
        }

        /// <summary>
        /// Kiểm tra và hủy hàng loạt đơn hàng QR quá hạn 15 phút trong SQL Transaction an toàn,
        /// đồng thời hoàn kho FEFO và hoàn điểm VIP đã sử dụng.
        /// </summary>
        public async Task<int> KiemTraVaHuyDonQrQuaHanAsync(int? maDonHang = null)
        {
            var bayGio = LayThoiGianHeThong();
            var truyVan = _context.DonHangs
                .AsSplitQuery()
                .Include(d => d.ChiTietDonHangs)
                .Include(d => d.DotGiaoHangDaDiems)
                .Where(d => (d.TrangThaiTt == "CHO_THANH_TOAN" || d.TrangThaiTt == "CHO_XAC_MINH_CK")
                         && d.TrangThaiDh != "DA_HUY"
                         && (d.PhuongThucTt == "MOMO_QR" || d.PhuongThucTt == "VIETQR" || d.PhuongThucTt == "MOMO"));

            if (maDonHang.HasValue)
            {
                truyVan = truyVan.Where(d => d.MaDh == maDonHang.Value);
            }

            var cacDonCho = await truyVan.ToListAsync();
            var cacDonQuaHan = cacDonCho
                .Where(d => bayGio >= (d.HanThanhToanQr ?? d.NgayDat.AddMinutes(15)))
                .ToList();

            if (cacDonQuaHan.Count == 0) return 0;

            int soDonDaHuy = 0;
            foreach (var donHang in cacDonQuaHan)
            {
                bool daHuyDonNay = false;

                await ThucThiTrongGiaoDichAsync(async () =>
                {
                    if ((donHang.TrangThaiTt != "CHO_THANH_TOAN" && donHang.TrangThaiTt != "CHO_XAC_MINH_CK")
                        || donHang.TrangThaiDh == "DA_HUY")
                    {
                        return;
                    }

                    donHang.TrangThaiDh = "DA_HUY";
                    donHang.TrangThaiTt = "HET_HAN_HUY";
                    donHang.LyDoHuy = "Tự động hủy do quá thời hạn 15 phút chuyển khoản QR";

                    var cacDotGiao = donHang.DotGiaoHangDaDiems.Any()
                        ? donHang.DotGiaoHangDaDiems.ToList()
                        : await _context.DotGiaoHangDaDiems.Where(g => g.MaDh == donHang.MaDh).ToListAsync();

                    foreach (var dot in cacDotGiao)
                    {
                        dot.TrangThaiGiao = "DA_HUY";
                    }

                    await _dichVuTonKho.HoanTraTonKhoTheoDonAsync(donHang);

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

                    daHuyDonNay = true;
                });

                // Cập nhật metadata sau khi Transaction chính đã Commit xong (tránh mở DbContext thứ hai khi Transaction đang khóa)
                if (daHuyDonNay)
                {
                    _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, m =>
                    {
                        m.DaHoanKhoDoHetHan = true;
                    });
                    soDonDaHuy++;
                }
            }

            return soDonDaHuy;
        }

        /// <summary>
        /// Tổng hợp dữ liệu ViewModel cho trang Cổng thanh toán MoMo độc lập.
        /// </summary>
        public async Task<(string TrangThaiDieuHuong, CongThanhToanMoMoViewModel? DuLieuView)> XayDungDuLieuCongMoMoAsync(string maTraCuu)
        {
            await KiemTraVaHuyDonQrQuaHanAsync();

            var donHang = await _context.DonHangs
                .AsSplitQuery()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu);

            if (donHang == null) return ("VE_TRANG_CHU", null);

            _kiotMeta.GanMetadataChoDonHang(donHang);

            if (donHang.TrangThaiTt == "DA_THANH_TOAN" || donHang.TrangThaiTt == "DA_TT")
            {
                return ("DA_THANH_TOAN", null);
            }

            if (donHang.TrangThaiTt == "HET_HAN_HUY" || donHang.TrangThaiTt == "HET_HAN_15P" || donHang.TrangThaiDh == "DA_HUY")
            {
                return ("DA_HUY_HET_HAN", null);
            }

            DateTime hanThanhToan = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);
            int soGiayConLai = Math.Max(0, (int)Math.Ceiling((hanThanhToan - LayThoiGianHeThong()).TotalSeconds));
            var duLieuQr = TaoDuLieuQrMoMo(donHang);

            return ("HIEN_CONG_MOMO", new CongThanhToanMoMoViewModel
            {
                DonHang = donHang,
                MaTraCuuDh = donHang.MaTraCuuDh,
                TongThanhToan = donHang.TongThanhToanCuoi,
                TongThanhToanCuoi = donHang.TongThanhToanCuoi,
                NoiDungChuyenKhoan = duLieuQr.NoiDungCk,
                SoDienThoaiMoMo = duLieuQr.SdtMoMoHienThi,
                SoTaiKhoanMoMo = duLieuQr.SdtMoMoRaw,
                TenChuTaiKhoan = duLieuQr.TenChuTk,
                AnhQrThanhToanBase64 = duLieuQr.AnhQrThanhToanBase64,
                UrlVietQr = duLieuQr.UrlVietQr,
                CauHinhQr = duLieuQr.CauHinhQr,
                HanThanhToan = hanThanhToan,
                SoGiayConLai = soGiayConLai
            });
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán thời gian thực của đơn hàng QR và số giây còn lại.
        /// </summary>
        public async Task<(DonHang? DonHang, int SoGiayConLai)> KiemTraVaCapNhatTrangThaiDonAsync(string maTraCuu)
        {
            await KiemTraVaHuyDonQrQuaHanAsync();

            var donHang = await _context.DonHangs.FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu.Trim());
            if (donHang == null) return (null, 0);

            _kiotMeta.GanMetadataChoDonHang(donHang);
            DateTime hanTt = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);

            int soGiayConLai = (donHang.TrangThaiDh == "DA_HUY" || donHang.TrangThaiTt == "HET_HAN_HUY" || donHang.TrangThaiTt == "HET_HAN_15P")
                ? 0
                : Math.Max(0, (int)Math.Ceiling((hanTt - LayThoiGianHeThong()).TotalSeconds));
            return (donHang, soGiayConLai);
        }

        /// <summary>
        /// Hủy một đơn hàng QR hết hạn theo mã tra cứu (từ yêu cầu đồng hồ đếm ngược phía client) và hoàn kho FEFO + điểm VIP.
        /// </summary>
        public async Task<KetQuaHuyDonHetHan> HuyDonHetHanTheoMaTraCuuAsync(string maTraCuu)
        {
            if (string.IsNullOrWhiteSpace(maTraCuu))
            {
                return new KetQuaHuyDonHetHan
                {
                    ThanhCong = false,
                    ThongBao = "Không tìm thấy mã đơn hàng."
                };
            }

            var donHang = await _context.DonHangs
                .AsSplitQuery()
                .Include(d => d.ChiTietDonHangs)
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu.Trim());

            if (donHang == null)
            {
                return new KetQuaHuyDonHetHan
                {
                    ThanhCong = false,
                    ThongBao = "Không tìm thấy đơn hàng."
                };
            }

            if (donHang.TrangThaiTt == "DA_THANH_TOAN" || donHang.TrangThaiTt == "DA_TT")
            {
                return new KetQuaHuyDonHetHan
                {
                    ThanhCong = false,
                    DaThanhToan = true,
                    TrangThaiTt = donHang.TrangThaiTt,
                    TrangThaiDh = donHang.TrangThaiDh,
                    ThongBao = "Đơn hàng đã được thanh toán thành công!"
                };
            }

            DateTime hanTt = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);
            if (LayThoiGianHeThong() < hanTt.AddSeconds(-2) && donHang.TrangThaiDh != "DA_HUY")
            {
                return new KetQuaHuyDonHetHan
                {
                    ThanhCong = false,
                    DaThanhToan = false,
                    TrangThaiTt = donHang.TrangThaiTt,
                    TrangThaiDh = donHang.TrangThaiDh,
                    ThongBao = "Đơn hàng vẫn đang trong thời hạn thanh toán 15 phút."
                };
            }

            await KiemTraVaHuyDonQrQuaHanAsync(donHang.MaDh);
            if (donHang.TrangThaiDh != "DA_HUY")
            {
                await _dichVuTonKho.HoanTraTonKhoRollbackAsync(donHang);
            }

            if (donHang.TrangThaiDh == "DA_HUY")
            {
                var cacDotChuaDongBo = await _context.DotGiaoHangDaDiems
                    .Where(g => g.MaDh == donHang.MaDh && g.TrangThaiGiao != "DA_HUY")
                    .ToListAsync();
                if (cacDotChuaDongBo.Count > 0)
                {
                    foreach (var dot in cacDotChuaDongBo)
                    {
                        dot.TrangThaiGiao = "DA_HUY";
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return new KetQuaHuyDonHetHan
            {
                ThanhCong = true,
                TrangThaiTt = donHang.TrangThaiTt,
                TrangThaiDh = donHang.TrangThaiDh,
                ThongBao = "Đơn hàng đã tự động hủy do hết hạn thanh toán 15 phút. Sản phẩm và điểm đã dùng đã được hoàn trả."
            };
        }

        /// <summary>
        /// Xử lý Webhook xác nhận thanh toán MoMo: đổi trạng thái, cộng điểm VIP có khóa chống cộng trùng DaCongDiemThuong và ghi PhieuThuChi loại THU.
        /// </summary>
        public async Task<KetQuaXuLyWebhookMoMo> XuLyWebhookMoMoAsync(MomoWebhookPayload payload)
        {
            string noiDung = payload.Content ?? payload.Description ?? payload.OrderInfo
                ?? payload.Data?.FirstOrDefault()?.Content ?? payload.Data?.FirstOrDefault()?.Description ?? string.Empty;

            decimal soTienNhan = payload.TransferAmount ?? payload.Amount ?? payload.SoTien
                ?? payload.Data?.FirstOrDefault()?.TransferAmount ?? payload.Data?.FirstOrDefault()?.Amount ?? 0m;

            string? maDonThang = payload.MaDonHang;
            if (string.IsNullOrWhiteSpace(maDonThang))
            {
                var khopMa = Regex.Match(noiDung, @"TT2026[- ]?\d{4}", RegexOptions.IgnoreCase);
                if (khopMa.Success)
                {
                    string chuoiTho = khopMa.Value.ToUpperInvariant().Replace(" ", "");
                    maDonThang = chuoiTho.Contains('-') ? chuoiTho : "TT2026-" + chuoiTho.Substring(6);
                }
            }

            if (string.IsNullOrWhiteSpace(maDonThang))
            {
                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 200,
                    ThanhCong = false,
                    ThongBao = "Không tìm thấy mã đơn hàng hợp lệ (TT2026-XXXX) trong nội dung chuyển khoản."
                };
            }

            var donHang = await _context.DonHangs.FirstOrDefaultAsync(d => d.MaTraCuuDh == maDonThang.Trim());
            if (donHang == null)
            {
                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 404,
                    ThanhCong = false,
                    ThongBao = $"Không tìm thấy đơn hàng #{maDonThang} trong CSDL."
                };
            }

            _kiotMeta.GanMetadataChoDonHang(donHang);
            DateTime hanTt = donHang.HanThanhToanQr ?? donHang.NgayDat.AddMinutes(15);

            if (donHang.TrangThaiTt == "HET_HAN_HUY"
                || donHang.TrangThaiTt == "HET_HAN_15P"
                || donHang.TrangThaiDh == "DA_HUY"
                || LayThoiGianHeThong() >= hanTt)
            {
                if (donHang.TrangThaiDh != "DA_HUY")
                {
                    await KiemTraVaHuyDonQrQuaHanAsync(donHang.MaDh);
                }

                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 200,
                    ThanhCong = false,
                    ThongBao = $"Đơn hàng #{donHang.MaTraCuuDh} đã quá hạn thanh toán 15 phút hoặc đã hủy."
                };
            }

            if (soTienNhan < Math.Round(donHang.TongThanhToanCuoi, 0))
            {
                return new KetQuaXuLyWebhookMoMo
                {
                    MaTrangThaiHttp = 200,
                    ThanhCong = false,
                    ThongBao = $"Số tiền chuyển khoản ({soTienNhan:N0} đ) nhỏ hơn tổng thanh toán của đơn ({donHang.TongThanhToanCuoi:N0} đ)."
                };
            }

            bool vuaXacNhanThanhToan = false;
            await ThucThiTrongGiaoDichAsync(async () =>
            {
                if (donHang.TrangThaiTt == "DA_THANH_TOAN" || donHang.TrangThaiTt == "DA_TT")
                {
                    return;
                }

                donHang.TrangThaiTt = "DA_THANH_TOAN";
                donHang.TrangThaiDh = "DA_XAC_NHAN";
                await _context.SaveChangesAsync();

                await DichVuHoiVien.CongDiemThuongDonHangAsync(_context, donHang);
                vuaXacNhanThanhToan = true;
            });

            if (vuaXacNhanThanhToan)
            {
                _kiotMeta.CapNhatMetadataDonHang(donHang.MaDh, m => m.SoTienKhachDaTra = soTienNhan);
                _kiotMeta.GhiNhanPhieuThuChi(new PhieuThuChiMoRong
                {
                    MaDonHang = donHang.MaDh,
                    MaTraCuuDh = donHang.MaTraCuuDh,
                    LoaiPhieu = "THU",
                    PhuongThuc = "MOMO_QR",
                    SoTien = soTienNhan,
                    NguoiNopNhan = donHang.HoTenNguoiMua,
                    NoiDung = $"Thu tự động Webhook MoMo: {(string.IsNullOrWhiteSpace(noiDung) ? "NGUYETCAT " + donHang.MaTraCuuDh : noiDung.Trim())}",
                    ThoiGian = LayThoiGianHeThong()
                });
            }

            return new KetQuaXuLyWebhookMoMo
            {
                MaTrangThaiHttp = 200,
                ThanhCong = true,
                MaTraCuu = donHang.MaTraCuuDh,
                TrangThaiTt = donHang.TrangThaiTt,
                TrangThaiDh = donHang.TrangThaiDh,
                SoTienDaNhan = soTienNhan,
                ThongBao = $"Đã xác nhận thanh toán tự động thành công cho đơn #{donHang.MaTraCuuDh}!"
            };
        }
    }
}
