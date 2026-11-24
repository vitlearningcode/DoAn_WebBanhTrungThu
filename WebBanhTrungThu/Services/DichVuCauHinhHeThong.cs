using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Services
{
    public class CauHinhThanhToanQr
    {
        public string SoTaiKhoanMoMo { get; set; } = "0814585526";
        public string SoDienThoaiHienThi { get; set; } = "0814 585 526";
        public string TenChuTaiKhoan { get; set; } = "LE MINH DUC";
        public string TenNganHangVi { get; set; } = "MOMO";
        public string MaBinNganHang { get; set; } = "970422";
        public string CuPhapChuyenKhoan { get; set; } = "NGUYETCAT {MaTraCuuDh}";
        public int ThoiGianChoPhut { get; set; } = 15;
        public string? SoTaiKhoanNganHang { get; set; }
        public string? TenNganHangVietQr { get; set; }

        [JsonIgnore]
        public string MaNganHang
        {
            get => TenNganHangVi;
            set => TenNganHangVi = value;
        }

        [JsonIgnore]
        public string MaBinVietQr
        {
            get => MaBinNganHang;
            set => MaBinNganHang = value;
        }

        [JsonIgnore]
        public string MauNoiDungCk
        {
            get => CuPhapChuyenKhoan;
            set => CuPhapChuyenKhoan = value;
        }
    }

    public class PhieuThuChiMoRong
    {
        public string MaPhieu { get; set; } = string.Empty;
        public int? MaDh { get; set; }
        public string MaTraCuuDh { get; set; } = string.Empty;
        public string LoaiPhieu { get; set; } = "THU";
        public string PhuongThuc { get; set; } = "MOMO_QR";
        public decimal SoTien { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string NguoiNopNhan { get; set; } = string.Empty;
        public string SdtNguoiNopNhan { get; set; } = string.Empty;
        public DateTime ThoiGianTao { get; set; } = DateTime.Now;

        [JsonIgnore]
        public int? MaDonHang
        {
            get => MaDh;
            set => MaDh = value;
        }

        [JsonIgnore]
        public DateTime ThoiGian
        {
            get => ThoiGianTao;
            set => ThoiGianTao = value;
        }
    }

    public class NhatKyTruLoFefo
    {
        public int MaLo { get; set; }
        public int MaBanh { get; set; }
        public int SoLuongDaTru { get; set; }
    }

    public class NhatKyTruVoHop
    {
        public int MaVh { get; set; }
        public int SoLuongDaTru { get; set; }
    }

    public class ThongTinThiep3DMeta
    {
        public string MaTokenThiepQr { get; set; } = string.Empty;
        public string TenNguoiNhanThiep { get; set; } = string.Empty;
        public string TenNguoiGuiThiep { get; set; } = string.Empty;
        public string? LoiChucThiep { get; set; }
        public string? MauGiaoDienThiep { get; set; }
    }

    /// <summary>
    /// Dịch vụ SQL-First quản lý cấu hình QR MoMo (CAU_HINH_QR_MOMO trong dbo.CauHinhHeThong),
    /// ghi nhận phiếu thu/chi (dbo.PhieuThuChi) và truy xuất thông tin Thiệp chúc mừng 3D (dbo.ThiepChucMung3D).
    /// </summary>
    public class DichVuCauHinhHeThong : DichVuCoSo, IDichVuCauHinhHeThong
    {
        public const string KHOA_CAU_HINH_QR_MOMO = "CAU_HINH_QR_MOMO";

        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly object _lock = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DichVuCauHinhHeThong(CSDLBanhTrungThuContext context, IServiceScopeFactory scopeFactory) : base(context)
        {
            _scopeFactory = scopeFactory;
        }

        public CauHinhThanhToanQr LayCauHinhQr()
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                return DocHoacKhoiTaoCauHinhQrInternal(db);
            }
        }

        public CauHinhThanhToanQr LayCauHinhThanhToan() => LayCauHinhQr();

        public void CapNhatCauHinhQr(Action<CauHinhThanhToanQr> capNhat, int? maNdCapNhat = null)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                var cfg = DocHoacKhoiTaoCauHinhQrInternal(db);
                capNhat(cfg);
                LuuCauHinhQrInternal(db, cfg, maNdCapNhat);
                db.SaveChanges();
            }
        }

        public CauHinhThanhToanQr CapNhatCauHinhQr(string? soTaiKhoanMoMo, string? tenChuTaiKhoan, string? tenNganHangVi, string? maBinNganHang = null, string? cuPhap = null)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                var cfg = DocHoacKhoiTaoCauHinhQrInternal(db);

                string stkSach = (soTaiKhoanMoMo ?? "0814585526").Replace(" ", "").Trim();
                if (string.IsNullOrWhiteSpace(stkSach)) stkSach = "0814585526";

                string sdtHienThi = stkSach.Length == 10
                    ? $"{stkSach[..4]} {stkSach.Substring(4, 3)} {stkSach.Substring(7, 3)}"
                    : stkSach;

                cfg.SoTaiKhoanMoMo = stkSach;
                cfg.SoDienThoaiHienThi = sdtHienThi;
                cfg.TenChuTaiKhoan = string.IsNullOrWhiteSpace(tenChuTaiKhoan) ? "LE MINH DUC" : tenChuTaiKhoan.Trim().ToUpperInvariant();
                cfg.TenNganHangVi = string.IsNullOrWhiteSpace(tenNganHangVi) ? "MOMO" : tenNganHangVi.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(maBinNganHang))
                {
                    cfg.MaBinNganHang = maBinNganHang.Trim();
                }
                if (!string.IsNullOrWhiteSpace(cuPhap))
                {
                    cfg.CuPhapChuyenKhoan = cuPhap.Trim();
                }

                LuuCauHinhQrInternal(db, cfg);
                db.SaveChanges();
                return cfg;
            }
        }

        public void GhiNhanPhieuThuChi(PhieuThuChiMoRong phieu)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                string loai = string.IsNullOrWhiteSpace(phieu.LoaiPhieu) ? "THU" : phieu.LoaiPhieu.Trim().ToUpperInvariant();
                if (phieu.MaDh.HasValue && db.PhieuThuChis.Any(p => p.MaDonHangLienKet == phieu.MaDh.Value && p.LoaiPhieu == loai))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(phieu.MaPhieu))
                {
                    string tienTo = loai == "CHI" ? "PCH" : "PTH";
                    phieu.MaPhieu = $"{tienTo}-{DateTime.Now:yyMMddHHmmss}";
                }

                db.PhieuThuChis.Add(new PhieuThuChi
                {
                    MaPhieuHienThi = phieu.MaPhieu,
                    LoaiPhieu = loai,
                    HangMuc = loai == "CHI" ? "Chi hoàn tiền đơn hàng" : "Thu tiền bán hàng",
                    SoTien = phieu.SoTien,
                    PhuongThuc = string.IsNullOrWhiteSpace(phieu.PhuongThuc) ? "MOMO_QR" : phieu.PhuongThuc.Trim().ToUpperInvariant(),
                    MaDonHangLienKet = phieu.MaDh,
                    NguoiNopNhan = string.IsNullOrWhiteSpace(phieu.NguoiNopNhan) ? "Khách Hàng Nguyệt Cát" : phieu.NguoiNopNhan.Trim(),
                    GhiChu = string.IsNullOrWhiteSpace(phieu.NoiDung) ? $"Giao dịch đơn #{phieu.MaTraCuuDh}" : phieu.NoiDung.Trim(),
                    NgayLap = phieu.ThoiGianTao == default ? DateTime.Now : phieu.ThoiGianTao
                });

                db.SaveChanges();
            }
        }

        public void GhiPhieuThuChi(PhieuThuChiMoRong phieu) => GhiNhanPhieuThuChi(phieu);

        public ThongTinThiep3DMeta? LayThongTinThiepTheoToken(string? maToken)
        {
            if (string.IsNullOrWhiteSpace(maToken)) return null;
            string tokenChuan = maToken.Trim();

            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                var thiepDb = db.ThiepChucMung3ds
                    .AsNoTracking()
                    .FirstOrDefault(t => t.MaTokenQr == tokenChuan);

                if (thiepDb != null)
                {
                    return new ThongTinThiep3DMeta
                    {
                        MaTokenThiepQr = thiepDb.MaTokenQr,
                        TenNguoiNhanThiep = thiepDb.TenNguoiNhan,
                        TenNguoiGuiThiep = thiepDb.TenNguoiGui,
                        LoiChucThiep = thiepDb.LoiChuc,
                        MauGiaoDienThiep = thiepDb.MauGiaoDien
                    };
                }

                var dotDb = db.DotGiaoHangDaDiems
                    .AsNoTracking()
                    .Include(d => d.MaDhNavigation)
                    .FirstOrDefault(d => d.MaTokenThiepQr == tokenChuan && d.CoKemThiepQr);

                if (dotDb != null)
                {
                    return new ThongTinThiep3DMeta
                    {
                        MaTokenThiepQr = tokenChuan,
                        TenNguoiNhanThiep = dotDb.TenNguoiNhan,
                        TenNguoiGuiThiep = dotDb.MaDhNavigation?.HoTenNguoiMua ?? "Khách Hàng Nguyệt Cát",
                        LoiChucThiep = dotDb.LoiChucThiep,
                        MauGiaoDienThiep = dotDb.MauGiaoDienThiep
                    };
                }

                return null;
            }
        }

        public ThongTinThiep3DMeta? LayThongTinThiepTheoToken(int maDh, string? maToken)
        {
            if (string.IsNullOrWhiteSpace(maToken)) return null;
            string tokenChuan = maToken.Trim();

            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

                var thiepDb = db.ThiepChucMung3ds
                    .AsNoTracking()
                    .FirstOrDefault(t => t.MaTokenQr == tokenChuan && (t.MaDonHang == maDh || maDh <= 0));

                if (thiepDb != null)
                {
                    return new ThongTinThiep3DMeta
                    {
                        MaTokenThiepQr = thiepDb.MaTokenQr,
                        TenNguoiNhanThiep = thiepDb.TenNguoiNhan,
                        TenNguoiGuiThiep = thiepDb.TenNguoiGui,
                        LoiChucThiep = thiepDb.LoiChuc,
                        MauGiaoDienThiep = thiepDb.MauGiaoDien
                    };
                }
            }

            return LayThongTinThiepTheoToken(maToken);
        }

        private CauHinhThanhToanQr DocHoacKhoiTaoCauHinhQrInternal(CSDLBanhTrungThuContext db)
        {
            var rowQr = db.CauHinhHeThongs
                .AsNoTracking()
                .FirstOrDefault(c => c.KhoaCauHinh == KHOA_CAU_HINH_QR_MOMO);

            if (rowQr != null && !string.IsNullOrWhiteSpace(rowQr.GiaTriJson))
            {
                try
                {
                    var cfg = JsonSerializer.Deserialize<CauHinhThanhToanQr>(rowQr.GiaTriJson, _jsonOptions);
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.SoTaiKhoanMoMo))
                    {
                        return cfg;
                    }
                }
                catch
                {
                    // Khởi tạo lại mặc định bên dưới
                }
            }

            var macDinh = new CauHinhThanhToanQr();
            LuuCauHinhQrInternal(db, macDinh);
            db.SaveChanges();
            return macDinh;
        }

        private void LuuCauHinhQrInternal(CSDLBanhTrungThuContext db, CauHinhThanhToanQr cfg, int? maNdCapNhat = null)
        {
            int? maNdHopLe = null;
            if (maNdCapNhat.HasValue && db.NguoiDungs.AsNoTracking().Any(u => u.MaNd == maNdCapNhat.Value))
            {
                maNdHopLe = maNdCapNhat.Value;
            }

            string jsonQr = JsonSerializer.Serialize(cfg, _jsonOptions);
            var rowQr = db.CauHinhHeThongs.FirstOrDefault(c => c.KhoaCauHinh == KHOA_CAU_HINH_QR_MOMO);
            if (rowQr == null)
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = KHOA_CAU_HINH_QR_MOMO,
                    GiaTriJson = jsonQr,
                    MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                    NgayCapNhat = DateTime.Now,
                    MaNdCapNhat = maNdHopLe
                });
            }
            else
            {
                rowQr.GiaTriJson = jsonQr;
                rowQr.NgayCapNhat = DateTime.Now;
                if (maNdHopLe.HasValue) rowQr.MaNdCapNhat = maNdHopLe.Value;
            }
        }
    }
}
