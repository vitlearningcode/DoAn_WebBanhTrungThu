using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách đọc/lưu các nhóm cấu hình hệ thống vào bảng dbo.CauHinhHeThong:
    /// Thanh toán &amp; QR MoMo, Thông tin Đại lý &amp; Chi nhánh, Vận chuyển &amp; Phí giao hàng, Hội viên VIP &amp; Điểm tích lũy.
    /// </summary>
    public class DichVuCaiDatHeThong : DichVuCoSo, IDichVuCaiDatHeThong
    {
        public const string KHOA_CUA_HANG = "CAU_HINH_CUA_HANG";
        public const string KHOA_VAN_CHUYEN = "CAU_HINH_VAN_CHUYEN";
        public const string KHOA_HOI_VIEN_VIP = "CAU_HINH_HOI_VIEN_VIP";

        private readonly IDichVuCauHinhHeThong _cauHinhHeThong;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DichVuCaiDatHeThong(
            CSDLBanhTrungThuContext db,
            IDichVuCauHinhHeThong cauHinhHeThong) : base(db)
        {
            _cauHinhHeThong = cauHinhHeThong;
        }

        public async Task<CaiDatHeThongViewModel> XayDungTrangCaiDatAsync(string? muc)
        {
            string mucChuan = ChuanHoaMucCaiDat(muc);
            var cauHinhQr = _cauHinhHeThong.LayCauHinhQr();
            var cuaHang = await DocHoacTaoMacDinhAsync(KHOA_CUA_HANG, "Cấu hình thông tin Đại lý và 2 chi nhánh Nguyệt Cát", () => new CauHinhCuaHang());
            var vanChuyen = await DocHoacTaoMacDinhAsync(KHOA_VAN_CHUYEN, "Cấu hình mốc Freeship và biểu phí giao hàng", () => new CauHinhVanChuyen());
            var hoiVienVip = await DocHoacTaoMacDinhAsync(KHOA_HOI_VIEN_VIP, "Cấu hình quy đổi điểm tích lũy và hạng hội viên VIP", () => new CauHinhHoiVienVip());

            var (anhQrMau, noiDungMau) = TaoAnhQrMoMoMau(cauHinhQr);

            var danhSachBanner = await _db.CauHinhLandingPages
                .AsNoTracking()
                .Include(c => c.MaAnhNenNavigation)
                .Include(c => c.MaAnhPhuParallaxNavigation)
                .OrderBy(c => c.ThuTuHienThi)
                .ThenBy(c => c.MaPhanKhu)
                .ToListAsync();

            var danhSachAnhBanner = await _db.KhoHinhAnhs
                .AsNoTracking()
                .OrderByDescending(h => h.MaHa)
                .ToListAsync();

            return new CaiDatHeThongViewModel
            {
                MucDangChon = mucChuan,
                CauHinhQr = cauHinhQr,
                AnhQrMauBase64 = anhQrMau,
                NoiDungCkMau = noiDungMau,
                CauHinhCuaHang = cuaHang,
                CauHinhVanChuyen = vanChuyen,
                CauHinhHoiVienVip = hoiVienVip,
                DanhSachBanner = danhSachBanner,
                DanhSachAnhBanner = danhSachAnhBanner
            };
        }

        public Task<(bool ThanhCong, string ThongBao)> LuuCauHinhMoMoAsync(CauHinhThanhToanQr model, int? maNdCapNhat = null)
        {
            string stk = Regex.Replace(model.SoTaiKhoanMoMo ?? string.Empty, @"\D", "");
            if (string.IsNullOrWhiteSpace(stk) || stk.Length < 8)
            {
                return Task.FromResult((false, "Vui lòng nhập số điện thoại hoặc số tài khoản MoMo hợp lệ (tối thiểu 8 chữ số)."));
            }

            string tenChuTk = BoDauTiengViet(ChuanHoaChuoi(model.TenChuTaiKhoan, "LE MINH DUC")).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(tenChuTk))
            {
                return Task.FromResult((false, "Vui lòng nhập tên chủ tài khoản nhận tiền."));
            }

            string sdtHienThi = stk.Length == 10
                ? $"{stk[..4]} {stk.Substring(4, 3)} {stk.Substring(7, 3)}"
                : stk;

            int thoiGianCho = Math.Clamp(model.ThoiGianChoPhut <= 0 ? 15 : model.ThoiGianChoPhut, 5, 60);
            string cuPhap = ChuanHoaChuoi(model.CuPhapChuyenKhoan, "NGUYETCAT {MaTraCuuDh}");
            string tenCong = ChuanHoaChuoi(model.TenNganHangVi, "MOMO").ToUpperInvariant();
            string maBin = ChuanHoaChuoi(model.MaBinNganHang, "970422");

            _cauHinhHeThong.CapNhatCauHinhQr(cfg =>
            {
                cfg.SoTaiKhoanMoMo = stk;
                cfg.SoDienThoaiHienThi = sdtHienThi;
                cfg.TenChuTaiKhoan = tenChuTk;
                cfg.TenNganHangVi = tenCong;
                cfg.MaBinNganHang = maBin;
                cfg.CuPhapChuyenKhoan = cuPhap;
                cfg.ThoiGianChoPhut = thoiGianCho;
                cfg.SoTaiKhoanNganHang = string.IsNullOrWhiteSpace(model.SoTaiKhoanNganHang) ? null : model.SoTaiKhoanNganHang.Trim();
                cfg.TenNganHangVietQr = string.IsNullOrWhiteSpace(model.TenNganHangVietQr) ? null : model.TenNganHangVietQr.Trim();
            }, maNdCapNhat);

            return Task.FromResult((true, $"Đã lưu cấu hình Thanh toán QR / MoMo ({sdtHienThi} - {tenChuTk}) vào SQL Server."));
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuCauHinhCuaHangAsync(CauHinhCuaHang model, int? maNdCapNhat = null)
        {
            if (string.IsNullOrWhiteSpace(model.TenDaiLy) || string.IsNullOrWhiteSpace(model.Hotline))
            {
                return (false, "Vui lòng nhập đầy đủ Tên đại lý và Hotline liên hệ.");
            }

            var chuan = new CauHinhCuaHang
            {
                TenDaiLy = model.TenDaiLy.Trim(),
                Hotline = model.Hotline.Trim(),
                EmailLienHe = ChuanHoaChuoi(model.EmailLienHe, "cskh@nguyetcat.vn"),
                GioMoCua = ChuanHoaChuoi(model.GioMoCua, "07:30 – 22:00"),
                TenChiNhanh1 = ChuanHoaChuoi(model.TenChiNhanh1, "Chi nhánh TP. Long Xuyên (An Giang)"),
                DiaChiChiNhanh1 = ChuanHoaChuoi(model.DiaChiChiNhanh1, "TP. Long Xuyên, An Giang"),
                SdtChiNhanh1 = ChuanHoaChuoi(model.SdtChiNhanh1, "0296 385 6886"),
                TenChiNhanh2 = ChuanHoaChuoi(model.TenChiNhanh2, "Chi nhánh Quận 1 (TP. Hồ Chí Minh)"),
                DiaChiChiNhanh2 = ChuanHoaChuoi(model.DiaChiChiNhanh2, "128 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh"),
                SdtChiNhanh2 = ChuanHoaChuoi(model.SdtChiNhanh2, "028 3829 6886")
            };

            await LuuJsonVaoSqlAsync(KHOA_CUA_HANG, "Cấu hình thông tin Đại lý và 2 chi nhánh Nguyệt Cát", chuan, maNdCapNhat);
            return (true, "Đã lưu thông tin Đại lý & 2 Chi nhánh vào SQL Server.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuCauHinhVanChuyenAsync(CauHinhVanChuyen model, int? maNdCapNhat = null)
        {
            if (model.MocFreeship < 0 || model.PhiGiaoTieuChuan < 0 || model.PhiGiaoHoaToc6h < 0 || model.PhiHoaTocSauFreeship < 0)
            {
                return (false, "Các mức phí vận chuyển và mốc miễn phí giao hàng không được nhỏ hơn 0 đ.");
            }

            var chuan = new CauHinhVanChuyen
            {
                MocFreeship = Math.Round(model.MocFreeship, 0),
                PhiGiaoTieuChuan = Math.Round(model.PhiGiaoTieuChuan, 0),
                PhiGiaoHoaToc6h = Math.Round(model.PhiGiaoHoaToc6h, 0),
                PhiHoaTocSauFreeship = Math.Round(model.PhiHoaTocSauFreeship, 0),
                KhuVucHoTroHoaToc = ChuanHoaChuoi(model.KhuVucHoTroHoaToc, "TP. Hồ Chí Minh, TP. Long Xuyên (An Giang)"),
                GhiChuGiaoHang = ChuanHoaChuoi(model.GhiChuGiaoHang, "Miễn phí giao hàng tiêu chuẩn toàn quốc cho đơn hàng đạt mốc quy định.")
            };

            await LuuJsonVaoSqlAsync(KHOA_VAN_CHUYEN, "Cấu hình mốc Freeship và biểu phí giao hàng", chuan, maNdCapNhat);
            return (true, "Đã lưu cấu hình Vận chuyển & Phí giao hàng vào SQL Server.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuCauHinhHoiVienVipAsync(CauHinhHoiVienVip model, int? maNdCapNhat = null)
        {
            if (model.SoTienMotDiem < 1000m || model.GiaTriQuyDoiMotDiem < 100m)
            {
                return (false, "Tỷ lệ quy đổi điểm thưởng phải hợp lệ (tối thiểu 1.000đ/điểm và 100đ giá trị quy đổi).");
            }
            if (model.TranTieuDiemPhanTram <= 0 || model.TranTieuDiemPhanTram > 100)
            {
                return (false, "Trần tiêu điểm tối đa phải nằm trong khoảng 1% – 100%.");
            }
            if (!(model.MocBacDiem > 0 && model.MocVangDiem > model.MocBacDiem && model.MocKimCuongDiem > model.MocVangDiem))
            {
                return (false, "Mốc điểm các hạng phải tăng dần: Bạc < Vàng < Kim Cương.");
            }

            var chuan = new CauHinhHoiVienVip
            {
                SoTienMotDiem = Math.Round(model.SoTienMotDiem, 0),
                GiaTriQuyDoiMotDiem = Math.Round(model.GiaTriQuyDoiMotDiem, 0),
                TranTieuDiemPhanTram = model.TranTieuDiemPhanTram,
                DiemTangDangKyMoi = Math.Max(0, model.DiemTangDangKyMoi),
                MocBacDiem = model.MocBacDiem,
                PhanTramGiamBac = Math.Clamp(model.PhanTramGiamBac, 0m, 50m),
                MocVangDiem = model.MocVangDiem,
                PhanTramGiamVang = Math.Clamp(model.PhanTramGiamVang, 0m, 50m),
                MocKimCuongDiem = model.MocKimCuongDiem,
                PhanTramGiamKimCuong = Math.Clamp(model.PhanTramGiamKimCuong, 0m, 50m)
            };

            await LuuJsonVaoSqlAsync(KHOA_HOI_VIEN_VIP, "Cấu hình quy đổi điểm tích lũy và hạng hội viên VIP", chuan, maNdCapNhat);
            return (true, "Đã lưu cấu hình Hội viên VIP & Điểm tích lũy vào SQL Server.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuCauHinhBannerAsync(LuuBannerLandingRequest req, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(req.TieuDeChinh))
            {
                return (false, "Vui lòng nhập tiêu đề chính cho phân khu Banner / Trang chủ.");
            }

            string maCodeChuan = (req.MaCodeKhuVuc ?? string.Empty).Trim().ToUpperInvariant();
            var phanKhu = req.MaPhanKhu > 0
                ? await _db.CauHinhLandingPages.FirstOrDefaultAsync(c => c.MaPhanKhu == req.MaPhanKhu)
                : null;

            if (phanKhu == null && !string.IsNullOrEmpty(maCodeChuan))
            {
                phanKhu = await _db.CauHinhLandingPages.FirstOrDefaultAsync(c => c.MaCodeKhuVuc == maCodeChuan);
            }

            if (phanKhu == null)
            {
                return (false, "Không tìm thấy phân khu Banner / Landing Page cần cập nhật trong CSDL.");
            }

            if (string.IsNullOrEmpty(maCodeChuan))
            {
                maCodeChuan = phanKhu.MaCodeKhuVuc;
            }
            else if (!Regex.IsMatch(maCodeChuan, @"^[A-Z0-9_]{3,50}$"))
            {
                return (false, "Mã định danh khu vực (MaCodeKhuVuc) không hợp lệ.");
            }
            else if (!string.Equals(phanKhu.MaCodeKhuVuc, maCodeChuan, StringComparison.OrdinalIgnoreCase))
            {
                bool trungMaCode = await _db.CauHinhLandingPages
                    .AsNoTracking()
                    .AnyAsync(c => c.MaPhanKhu != phanKhu.MaPhanKhu && c.MaCodeKhuVuc == maCodeChuan);
                if (trungMaCode)
                {
                    return (false, $"Mã khu vực [{maCodeChuan}] đã tồn tại ở một phân khu khác.");
                }
            }

            bool coTaiAnhMoi = req.TepAnhNenMoi != null && req.TepAnhNenMoi.Length > 0;
            string duoiFile = string.Empty;
            if (coTaiAnhMoi)
            {
                var duoiHopLe = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
                duoiFile = Path.GetExtension(req.TepAnhNenMoi!.FileName ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(duoiFile) || !duoiHopLe.Contains(duoiFile))
                {
                    return (false, "Định dạng ảnh tải lên không hợp lệ. Chỉ chấp nhận các tệp .jpg, .jpeg, .png hoặc .webp.");
                }

                if (req.TepAnhNenMoi.Length > 5 * 1024 * 1024)
                {
                    return (false, "Dung lượng ảnh vượt quá giới hạn tối đa 5MB.");
                }
            }

            await ThucThiTrongGiaoDichAsync(async () =>
            {
                int? maAnhNenChot = phanKhu.MaAnhNen;
                if (req.MaAnhNen.HasValue && req.MaAnhNen.Value > 0)
                {
                    if (await _db.KhoHinhAnhs.AsNoTracking().AnyAsync(h => h.MaHa == req.MaAnhNen.Value))
                    {
                        maAnhNenChot = req.MaAnhNen.Value;
                    }
                }
                else if (req.MaAnhNen.HasValue && req.MaAnhNen.Value <= 0)
                {
                    maAnhNenChot = null;
                }

                int? maAnhPhuChot = null;
                if (req.MaAnhPhuParallax.HasValue && req.MaAnhPhuParallax.Value > 0)
                {
                    if (await _db.KhoHinhAnhs.AsNoTracking().AnyAsync(h => h.MaHa == req.MaAnhPhuParallax.Value))
                    {
                        maAnhPhuChot = req.MaAnhPhuParallax.Value;
                    }
                }

                if (coTaiAnhMoi)
                {
                    string thuMucLanding = Path.Combine(webRootPath, "images", "landing");
                    if (!Directory.Exists(thuMucLanding))
                    {
                        Directory.CreateDirectory(thuMucLanding);
                    }

                    string tenFileAnToan = $"banner_{LayThoiGianHeThong():yyyyMMdd_HHmmss}{duoiFile}";
                    string duongDanVatLy = Path.Combine(thuMucLanding, tenFileAnToan);

                    using (var stream = new FileStream(duongDanVatLy, FileMode.Create))
                    {
                        await req.TepAnhNenMoi!.CopyToAsync(stream);
                    }

                    string tenTepGoc = Path.GetFileName(req.TepAnhNenMoi!.FileName ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(tenTepGoc))
                    {
                        tenTepGoc = tenFileAnToan;
                    }

                    var hinhMoi = new KhoHinhAnh
                    {
                        TenTepGoc = tenTepGoc,
                        TieuDeAlt = $"Banner {maCodeChuan} - {req.TieuDeChinh.Trim()}",
                        LoaiLuuTru = 1,
                        DuongDanCucBo = $"/images/landing/{tenFileAnToan}",
                        KichThuocPixel = "1920x1080",
                        NgayTaiLen = LayThoiGianHeThong()
                    };

                    _db.KhoHinhAnhs.Add(hinhMoi);
                    await _db.SaveChangesAsync();

                    _db.LienKetHinhAnhs.Add(new LienKetHinhAnh
                    {
                        MaHa = hinhMoi.MaHa,
                        LoaiDoiTuong = "BANNER",
                        MaDoiTuong = phanKhu.MaPhanKhu,
                        VaiTroAnh = "ANH_NEN",
                        ThuTuHienThi = 1
                    });

                    maAnhNenChot = hinhMoi.MaHa;
                }

                phanKhu.MaCodeKhuVuc = maCodeChuan;
                phanKhu.TieuDeChinh = req.TieuDeChinh.Trim();
                phanKhu.TieuDePhu = string.IsNullOrWhiteSpace(req.TieuDePhu) ? null : req.TieuDePhu.Trim();
                phanKhu.NoiDungMoTa = string.IsNullOrWhiteSpace(req.NoiDungMoTa) ? null : req.NoiDungMoTa.Trim();
                phanKhu.MaAnhNen = maAnhNenChot;
                phanKhu.MaAnhPhuParallax = maAnhPhuChot;
                phanKhu.ChuNutBamCta = string.IsNullOrWhiteSpace(req.ChuNutBamCta) ? null : req.ChuNutBamCta.Trim();
                phanKhu.DuongDanCta = string.IsNullOrWhiteSpace(req.DuongDanCta) ? null : req.DuongDanCta.Trim();
                phanKhu.MocThoiGianDemNguoc = req.MocThoiGianDemNguoc;
                phanKhu.ThuTuHienThi = req.ThuTuHienThi <= 0 ? phanKhu.ThuTuHienThi : req.ThuTuHienThi;
                phanKhu.DangKichHoat = req.DangKichHoat;

                await _db.SaveChangesAsync();
            });

            return (true, $"Đã cập nhật cấu hình phân khu [{maCodeChuan}] trực tiếp vào bảng dbo.CauHinhLandingPage.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> ChuyenTrangThaiBannerAsync(int maPhanKhu)
        {
            var phanKhu = await _db.CauHinhLandingPages.FirstOrDefaultAsync(c => c.MaPhanKhu == maPhanKhu);
            if (phanKhu == null)
            {
                return (false, "Không tìm thấy phân khu Banner / Trang chủ cần chuyển trạng thái.");
            }

            phanKhu.DangKichHoat = !phanKhu.DangKichHoat;
            await _db.SaveChangesAsync();

            string trangThaiText = phanKhu.DangKichHoat ? "Đang hiển thị" : "Tạm ẩn";
            return (true, $"Đã chuyển trạng thái phân khu [{phanKhu.MaCodeKhuVuc}] sang \"{trangThaiText}\".");
        }

        private async Task<T> DocHoacTaoMacDinhAsync<T>(string khoa, string moTa, Func<T> taoMacDinh) where T : class
        {
            var banGhi = await _db.CauHinhHeThongs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.KhoaCauHinh == khoa);

            if (banGhi != null && !string.IsNullOrWhiteSpace(banGhi.GiaTriJson))
            {
                try
                {
                    var ketQua = JsonSerializer.Deserialize<T>(banGhi.GiaTriJson, _jsonOptions);
                    if (ketQua != null) return ketQua;
                }
                catch
                {
                    // Dùng dữ liệu mặc định nếu JSON cũ không tương thích
                }
            }

            var macDinh = taoMacDinh();
            await LuuJsonVaoSqlAsync(khoa, moTa, macDinh);
            return macDinh;
        }

        private async Task LuuJsonVaoSqlAsync<T>(string khoa, string moTa, T duLieu, int? maNdCapNhat = null)
        {
            int? maNdHopLe = null;
            if (maNdCapNhat.HasValue && await _db.NguoiDungs.AsNoTracking().AnyAsync(u => u.MaNd == maNdCapNhat.Value))
            {
                maNdHopLe = maNdCapNhat.Value;
            }

            string json = JsonSerializer.Serialize(duLieu, _jsonOptions);
            var banGhi = await _db.CauHinhHeThongs.FirstOrDefaultAsync(c => c.KhoaCauHinh == khoa);
            if (banGhi == null)
            {
                _db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = khoa,
                    GiaTriJson = json,
                    MoTa = moTa,
                    NgayCapNhat = LayThoiGianHeThong(),
                    MaNdCapNhat = maNdHopLe
                });
            }
            else
            {
                banGhi.GiaTriJson = json;
                banGhi.MoTa = moTa;
                banGhi.NgayCapNhat = LayThoiGianHeThong();
                if (maNdHopLe.HasValue) banGhi.MaNdCapNhat = maNdHopLe.Value;
            }

            await _db.SaveChangesAsync();
        }

        private static string ChuanHoaMucCaiDat(string? muc)
        {
            return (muc ?? "momo").Trim().ToLowerInvariant() switch
            {
                "cuahang" => "cuahang",
                "vanchuyen" => "vanchuyen",
                "hoivien" => "hoivien",
                "banner" => "banner",
                _ => "momo"
            };
        }

        private static (string AnhQrBase64, string NoiDungCk) TaoAnhQrMoMoMau(CauHinhThanhToanQr cfg)
        {
            string sdtRaw = Regex.Replace(cfg.SoTaiKhoanMoMo ?? string.Empty, @"\D", "");
            if (string.IsNullOrWhiteSpace(sdtRaw)) sdtRaw = "0814585526";

            string tenChuTk = BoDauTiengViet(string.IsNullOrWhiteSpace(cfg.TenChuTaiKhoan) ? "LE MINH DUC" : cfg.TenChuTaiKhoan).ToUpperInvariant();
            string noiDung = BoDauTiengViet((cfg.CuPhapChuyenKhoan ?? "NGUYETCAT {MaTraCuuDh}")
                .Replace("{MaTraCuuDh}", "TT2026-8888", StringComparison.OrdinalIgnoreCase)).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(noiDung)) noiDung = "NGUYETCAT TT2026-8888";

            using var qrGenerator = new QRCodeGenerator();
            string payload = $"2|99|{sdtRaw}|{tenChuTk}||0|0|500000|{noiDung}|transfer_myqr";
            using QRCodeData qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.H);
            using var pngQr = new PngByteQRCode(qrData);
            string base64 = "data:image/png;base64," + Convert.ToBase64String(pngQr.GetGraphic(8, true));
            return (base64, noiDung);
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
    }
}
