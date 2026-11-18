using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.Services
{
    public class KiotItemMeta
    {
        public string Key { get; set; } = string.Empty; // "BANH_1", "COMBO_1"
        public string MaVach { get; set; } = string.Empty;
        public string ThuongHieu { get; set; } = "Đồng Khánh";
        public string NhaCungCap { get; set; } = "Lò Bánh Đồng Khánh Chính Gốc";
        public string ViTriKho { get; set; } = "Kệ A1";
        public string DonViTinh { get; set; } = "Cái";
        public bool BanTrucTiep { get; set; } = true;
        public int? MaDmCombo { get; set; }
        public string? TenDmCombo { get; set; }
        public int? TonKhoCombo { get; set; }
        public int? TrongLuongComboGram { get; set; }
        public List<string> DanhSachAnh { get; set; } = new();
    }

    public class KiotMetadataStore
    {
        public List<string> DanhSachThuongHieu { get; set; } = new()
        {
            "Đồng Khánh",
            "Kinh Đô",
            "Như Lan",
            "Givral",
            "Brodard",
            "Nguyệt Cát"
        };

        public List<string> DanhSachNhaCungCap { get; set; } = new()
        {
            "Lò Bánh Đồng Khánh Chính Gốc",
            "NPP Bánh Kinh Đô Miền Nam",
            "NPP Như Lan Sài Gòn",
            "Bánh Trung Thu Givral Chính Hãng",
            "NPP Brodard Sài Gòn",
            "Xưởng Bánh Thủ Công Nguyệt Cát"
        };

        public List<string> DanhSachViTriKho { get; set; } = new()
        {
            "Kệ A1",
            "Kệ A2",
            "Kệ B1",
            "Kệ B2",
            "Tủ Mát C1"
        };

        public List<string> DanhSachDonViTinh { get; set; } = new()
        {
            "Cái",
            "Hộp",
            "Lốc 4 cái",
            "Thùng 20 hộp"
        };

        public Dictionary<string, KiotItemMeta> Items { get; set; } = new();
        public Dictionary<int, KiotDonHangMeta> DonHangs { get; set; } = new();
        public CauHinhThanhToanQr CauHinhQr { get; set; } = new();
        public List<PhieuThuChiKiot> DanhSachPhieuThuChi { get; set; } = new();
    }

    public class CauHinhThanhToanQr
    {
        public string SoTaiKhoanMoMo { get; set; } = "0814585526";
        public string SoDienThoaiHienThi { get; set; } = "0814 585 526";
        public string TenChuTaiKhoan { get; set; } = "LE MINH DUC";
        public string TenNganHangVi { get; set; } = "MOMO";
        public string MaBinNganHang { get; set; } = "970422";
        public string CuPhapChuyenKhoan { get; set; } = "NGUYETCAT {MaTraCuuDh}";
        public int ThoiGianChoPhut { get; set; } = 15;

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

    public class PhieuThuChiKiot
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

    public class KiotDonHangMeta
    {
        public int MaDh { get; set; }
        public string KenhBan { get; set; } = "WEB_ONLINE";
        public decimal? SoTienKhachDaTra { get; set; }
        public string? MaNganHangHoan { get; set; }
        public string? SoTaiKhoanHoan { get; set; }
        public string? TenChuTkHoan { get; set; }
        public string? LyDoHuyHoan { get; set; }
        public string? GhiChuDieuChinhAdmin { get; set; }
        public string? ThoiGianCapNhatCuoi { get; set; }
        public string? NguoiCapNhatCuoi { get; set; }
        public DateTime? HanThanhToan { get; set; }
        public bool DaRollbackKho { get; set; }
        public List<NhatKyTruLoFefo> DanhSachLoDaTru { get; set; } = new();
        public List<NhatKyTruVoHop> DanhSachVoHopDaTru { get; set; } = new();
        public Dictionary<string, ThongTinThiep3DMeta> Thiep3DTheoToken { get; set; } = new();

        [JsonIgnore]
        public bool DaHoanKhoDoHetHan
        {
            get => DaRollbackKho;
            set => DaRollbackKho = value;
        }
    }

    /// <summary>
    /// Dịch vụ quản lý dữ liệu Sổ quỹ (PhieuThuChi), Thiệp Chúc Mừng 3D (ThiepChucMung3D),
    /// Cấu hình QR MoMo (CauHinhHeThong) và Metadata KiotViet lưu trữ 100% trong SQL Server.
    /// </summary>
    public class DichVuKiotVietMetadata
    {
        public const string KHOA_CAU_HINH_QR_MOMO = "CAU_HINH_QR_MOMO";
        public const string KHOA_METADATA_KIOTVIET = "KIOT_METADATA_STORE";

        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly object _lock = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DichVuKiotVietMetadata(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public KiotMetadataStore DocDuLieu()
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                return DocDuLieuTuSqlInternal(db);
            }
        }

        public void LuuDuLieu(KiotMetadataStore store)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                LuuDuLieuVaoSqlInternal(db, store);
            }
        }

        private KiotMetadataStore DocDuLieuTuSqlInternal(CSDLBanhTrungThuContext db)
        {
            KiotMetadataStore store;

            // 1. Đọc Metadata KiotViet từ bảng vật lý dbo.CauHinhHeThong
            var rowStore = db.CauHinhHeThongs
                .AsNoTracking()
                .FirstOrDefault(c => c.KhoaCauHinh == KHOA_METADATA_KIOTVIET);

            if (rowStore != null && !string.IsNullOrWhiteSpace(rowStore.GiaTriJson))
            {
                try
                {
                    store = JsonSerializer.Deserialize<KiotMetadataStore>(rowStore.GiaTriJson, _jsonOptions)
                            ?? TaoDuLieuMacDinh();
                }
                catch
                {
                    store = TaoDuLieuMacDinh();
                }
            }
            else
            {
                store = TaoDuLieuMacDinh();
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = KHOA_METADATA_KIOTVIET,
                    GiaTriJson = JsonSerializer.Serialize(store, _jsonOptions),
                    MoTa = "Dữ liệu mở rộng Hàng hóa, Thương hiệu, Vị trí kho và Nhật ký FEFO KiotViet",
                    NgayCapNhat = DateTime.Now
                });
                db.SaveChanges();
            }

            // 2. Đọc cấu hình Tài khoản MoMo (0814585526 - LE MINH DUC) từ bảng dbo.CauHinhHeThong
            var rowQr = db.CauHinhHeThongs
                .AsNoTracking()
                .FirstOrDefault(c => c.KhoaCauHinh == KHOA_CAU_HINH_QR_MOMO);

            if (rowQr != null && !string.IsNullOrWhiteSpace(rowQr.GiaTriJson))
            {
                try
                {
                    store.CauHinhQr = JsonSerializer.Deserialize<CauHinhThanhToanQr>(rowQr.GiaTriJson, _jsonOptions)
                                      ?? new CauHinhThanhToanQr();
                }
                catch
                {
                    store.CauHinhQr = new CauHinhThanhToanQr();
                }
            }
            else
            {
                store.CauHinhQr ??= new CauHinhThanhToanQr();
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = KHOA_CAU_HINH_QR_MOMO,
                    GiaTriJson = JsonSerializer.Serialize(store.CauHinhQr, _jsonOptions),
                    MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                    NgayCapNhat = DateTime.Now
                });
                db.SaveChanges();
            }

            // 3. Đồng bộ danh sách Thiệp 3D từ bảng vật lý dbo.ThiepChucMung3D
            store.DonHangs ??= new Dictionary<int, KiotDonHangMeta>();
            var dsThiepDb = db.ThiepChucMung3ds.AsNoTracking().ToList();
            foreach (var thiep in dsThiepDb)
            {
                if (!store.DonHangs.TryGetValue(thiep.MaDonHang, out var dhMeta))
                {
                    dhMeta = new KiotDonHangMeta { MaDh = thiep.MaDonHang };
                    store.DonHangs[thiep.MaDonHang] = dhMeta;
                }
                dhMeta.Thiep3DTheoToken ??= new Dictionary<string, ThongTinThiep3DMeta>();
                dhMeta.Thiep3DTheoToken[thiep.MaTokenQr] = new ThongTinThiep3DMeta
                {
                    MaTokenThiepQr = thiep.MaTokenQr,
                    TenNguoiNhanThiep = thiep.TenNguoiNhan,
                    TenNguoiGuiThiep = thiep.TenNguoiGui,
                    LoiChucThiep = thiep.LoiChuc,
                    MauGiaoDienThiep = thiep.MauGiaoDien
                };
            }

            // 4. Đọc danh sách Phiếu Thu Chi từ bảng vật lý dbo.PhieuThuChi
            var dsPhieuDb = db.PhieuThuChis
                .AsNoTracking()
                .OrderByDescending(p => p.NgayLap)
                .ThenByDescending(p => p.MaPhieu)
                .ToList();

            var mapMaTraCuu = db.DonHangs
                .AsNoTracking()
                .Select(d => new { d.MaDh, d.MaTraCuuDh, d.SdtnguoiMua })
                .ToDictionary(d => d.MaDh);

            store.DanhSachPhieuThuChi = dsPhieuDb.Select(p =>
            {
                string maTraCuu = string.Empty;
                string sdt = string.Empty;
                if (p.MaDonHangLienKet.HasValue && mapMaTraCuu.TryGetValue(p.MaDonHangLienKet.Value, out var dhInfo))
                {
                    maTraCuu = dhInfo.MaTraCuuDh;
                    sdt = dhInfo.SdtnguoiMua ?? string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(p.GhiChu))
                {
                    var match = Regex.Match(p.GhiChu, @"(TT2026-\d{4}|POS26-\d{5})", RegexOptions.IgnoreCase);
                    if (match.Success) maTraCuu = match.Value.ToUpperInvariant();
                }

                return new PhieuThuChiKiot
                {
                    MaPhieu = string.IsNullOrWhiteSpace(p.MaPhieuHienThi) ? $"PTH-{p.MaPhieu:D6}" : p.MaPhieuHienThi,
                    MaDh = p.MaDonHangLienKet,
                    MaTraCuuDh = maTraCuu,
                    LoaiPhieu = p.LoaiPhieu,
                    PhuongThuc = p.PhuongThuc,
                    SoTien = p.SoTien,
                    NoiDung = p.GhiChu ?? p.HangMuc,
                    NguoiNopNhan = p.NguoiNopNhan,
                    SdtNguoiNopNhan = sdt,
                    ThoiGianTao = p.NgayLap
                };
            }).ToList();

            return store;
        }

        private void LuuDuLieuVaoSqlInternal(CSDLBanhTrungThuContext db, KiotMetadataStore store)
        {
            store.CauHinhQr ??= new CauHinhThanhToanQr();

            // 1. Lưu cấu hình QR MoMo vào bảng dbo.CauHinhHeThong
            string jsonQr = JsonSerializer.Serialize(store.CauHinhQr, _jsonOptions);
            var rowQr = db.CauHinhHeThongs.FirstOrDefault(c => c.KhoaCauHinh == KHOA_CAU_HINH_QR_MOMO);
            if (rowQr == null)
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = KHOA_CAU_HINH_QR_MOMO,
                    GiaTriJson = jsonQr,
                    MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                    NgayCapNhat = DateTime.Now
                });
            }
            else
            {
                rowQr.GiaTriJson = jsonQr;
                rowQr.NgayCapNhat = DateTime.Now;
            }

            // 2. Đồng bộ Thiep3DTheoToken vào bảng vật lý dbo.ThiepChucMung3D
            if (store.DonHangs != null)
            {
                var tokenDaCo = db.ThiepChucMung3ds
                    .ToDictionary(t => t.MaTokenQr, StringComparer.OrdinalIgnoreCase);

                foreach (var kvDh in store.DonHangs)
                {
                    int maDh = kvDh.Key;
                    var dhMeta = kvDh.Value;
                    if (dhMeta?.Thiep3DTheoToken == null) continue;

                    foreach (var kvThiep in dhMeta.Thiep3DTheoToken)
                    {
                        string token = (kvThiep.Key ?? string.Empty).Trim();
                        var info = kvThiep.Value;
                        if (string.IsNullOrWhiteSpace(token) || info == null) continue;

                        if (tokenDaCo.TryGetValue(token, out var thiepEntity))
                        {
                            thiepEntity.TenNguoiGui = string.IsNullOrWhiteSpace(info.TenNguoiGuiThiep) ? thiepEntity.TenNguoiGui : info.TenNguoiGuiThiep;
                            thiepEntity.TenNguoiNhan = string.IsNullOrWhiteSpace(info.TenNguoiNhanThiep) ? thiepEntity.TenNguoiNhan : info.TenNguoiNhanThiep;
                            if (!string.IsNullOrWhiteSpace(info.LoiChucThiep)) thiepEntity.LoiChuc = info.LoiChucThiep;
                            if (!string.IsNullOrWhiteSpace(info.MauGiaoDienThiep)) thiepEntity.MauGiaoDien = info.MauGiaoDienThiep;
                        }
                        else
                        {
                            int? maDotGiao = db.DotGiaoHangDaDiems
                                .AsNoTracking()
                                .Where(d => d.MaTokenThiepQr == token)
                                .Select(d => (int?)d.MaDotGh)
                                .FirstOrDefault();

                            var thiepMoi = new ThiepChucMung3d
                            {
                                MaTokenQr = token,
                                MaDonHang = maDh,
                                MaDotGiao = maDotGiao,
                                TenNguoiGui = string.IsNullOrWhiteSpace(info.TenNguoiGuiThiep) ? "Khách Hàng Nguyệt Cát" : info.TenNguoiGuiThiep,
                                TenNguoiNhan = string.IsNullOrWhiteSpace(info.TenNguoiNhanThiep) ? "Quý Khách & Gia Đình" : info.TenNguoiNhanThiep,
                                LoiChuc = string.IsNullOrWhiteSpace(info.LoiChucThiep)
                                    ? "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!"
                                    : info.LoiChucThiep,
                                MauGiaoDien = string.IsNullOrWhiteSpace(info.MauGiaoDienThiep) ? "CO_PHONG_HOANG_GIA" : info.MauGiaoDienThiep,
                                SoLanMoThiep = 0,
                                NgayTao = DateTime.Now
                            };
                            db.ThiepChucMung3ds.Add(thiepMoi);
                            tokenDaCo[token] = thiepMoi;
                        }
                    }
                }
            }

            // 3. Đồng bộ DanhSachPhieuThuChi vào bảng vật lý dbo.PhieuThuChi
            if (store.DanhSachPhieuThuChi != null && store.DanhSachPhieuThuChi.Count > 0)
            {
                var dsHienCo = db.PhieuThuChis.AsNoTracking().ToList();
                foreach (var p in store.DanhSachPhieuThuChi)
                {
                    string maHienThi = string.IsNullOrWhiteSpace(p.MaPhieu)
                        ? $"PTH-{DateTime.Now:yyMMddHHmmss}"
                        : p.MaPhieu.Trim();

                    bool daTonTai = dsHienCo.Any(x =>
                        string.Equals(x.MaPhieuHienThi, maHienThi, StringComparison.OrdinalIgnoreCase) ||
                        (p.MaDh.HasValue && x.MaDonHangLienKet == p.MaDh.Value && x.LoaiPhieu == p.LoaiPhieu));

                    if (!daTonTai)
                    {
                        db.PhieuThuChis.Add(new PhieuThuChi
                        {
                            MaPhieuHienThi = maHienThi,
                            LoaiPhieu = string.IsNullOrWhiteSpace(p.LoaiPhieu) ? "THU" : p.LoaiPhieu.Trim().ToUpperInvariant(),
                            HangMuc = p.LoaiPhieu == "CHI" ? "Chi hoàn tiền đơn hàng" : "Thu tiền bán hàng",
                            SoTien = p.SoTien,
                            PhuongThuc = string.IsNullOrWhiteSpace(p.PhuongThuc) ? "MOMO_QR" : p.PhuongThuc.Trim().ToUpperInvariant(),
                            MaDonHangLienKet = p.MaDh,
                            NguoiNopNhan = string.IsNullOrWhiteSpace(p.NguoiNopNhan) ? "Khách Hàng Nguyệt Cát" : p.NguoiNopNhan.Trim(),
                            GhiChu = string.IsNullOrWhiteSpace(p.NoiDung) ? $"Giao dịch đơn #{p.MaTraCuuDh}" : p.NoiDung.Trim(),
                            NgayLap = p.ThoiGianTao == default ? DateTime.Now : p.ThoiGianTao
                        });
                    }
                }
            }

            // 4. Lưu Metadata KiotViet vào bảng vật lý dbo.CauHinhHeThong
            string jsonStore = JsonSerializer.Serialize(store, _jsonOptions);
            var rowStore = db.CauHinhHeThongs.FirstOrDefault(c => c.KhoaCauHinh == KHOA_METADATA_KIOTVIET);
            if (rowStore == null)
            {
                db.CauHinhHeThongs.Add(new CauHinhHeThong
                {
                    KhoaCauHinh = KHOA_METADATA_KIOTVIET,
                    GiaTriJson = jsonStore,
                    MoTa = "Dữ liệu mở rộng Hàng hóa, Thương hiệu, Vị trí kho và Nhật ký FEFO KiotViet",
                    NgayCapNhat = DateTime.Now
                });
            }
            else
            {
                rowStore.GiaTriJson = jsonStore;
                rowStore.NgayCapNhat = DateTime.Now;
            }

            db.SaveChanges();
        }

        public static KiotMetadataStore TaoDuLieuMacDinh()
        {
            var store = new KiotMetadataStore();
            string[] thuongHieuMau = { "Đồng Khánh", "Kinh Đô", "Như Lan", "Brodard", "Givral", "Đồng Khánh", "Kinh Đô", "Như Lan" };
            string[] nccMau =
            {
                "Lò Bánh Đồng Khánh Chính Gốc",
                "NPP Bánh Kinh Đô Miền Nam",
                "NPP Như Lan Sài Gòn",
                "NPP Brodard Sài Gòn",
                "Bánh Trung Thu Givral Chính Hãng",
                "Lò Bánh Đồng Khánh Chính Gốc",
                "NPP Bánh Kinh Đô Miền Nam",
                "NPP Như Lan Sài Gòn"
            };
            string[] viTriMau = { "Kệ A1", "Kệ A2", "Kệ B1", "Kệ B2", "Tủ Mát C1", "Kệ A1", "Kệ B1", "Kệ A2" };

            for (int i = 1; i <= 15; i++)
            {
                int idx = (i - 1) % thuongHieuMau.Length;
                store.Items[$"BANH_{i}"] = new KiotItemMeta
                {
                    Key = $"BANH_{i}",
                    MaVach = $"8934680010{i:D3}",
                    ThuongHieu = thuongHieuMau[idx],
                    NhaCungCap = nccMau[idx],
                    ViTriKho = viTriMau[idx],
                    DonViTinh = "Cái",
                    BanTrucTiep = true
                };
            }

            string[] thuongHieuCombo = { "Đồng Khánh", "Kinh Đô", "Như Lan", "Nguyệt Cát" };
            string[] nccCombo =
            {
                "Lò Bánh Đồng Khánh Chính Gốc",
                "NPP Bánh Kinh Đô Miền Nam",
                "NPP Như Lan Sài Gòn",
                "Xưởng Bánh Thủ Công Nguyệt Cát"
            };

            for (int c = 1; c <= 10; c++)
            {
                int idx = (c - 1) % thuongHieuCombo.Length;
                store.Items[$"COMBO_{c}"] = new KiotItemMeta
                {
                    Key = $"COMBO_{c}",
                    MaVach = $"8934680990{c:D3}",
                    ThuongHieu = thuongHieuCombo[idx],
                    NhaCungCap = nccCombo[idx],
                    ViTriKho = "Kệ B1",
                    DonViTinh = "Hộp",
                    BanTrucTiep = true,
                    TenDmCombo = c == 1 ? "ĐỒNG GIÁ 20K" : "Combo Hộp Quà",
                    TonKhoCombo = 35 + (c * 5),
                    TrongLuongComboGram = 750
                };
            }

            return store;
        }

        public void GanMetadataChoBanh(SanPhamBanh banh, KiotMetadataStore store)
        {
            string key = $"BANH_{banh.MaBanh}";
            if (store.Items.TryGetValue(key, out var meta))
            {
                banh.MaVach = string.IsNullOrWhiteSpace(meta.MaVach) ? $"8934680010{banh.MaBanh:D3}" : meta.MaVach;
                banh.ThuongHieu = string.IsNullOrWhiteSpace(meta.ThuongHieu) ? "Đồng Khánh" : meta.ThuongHieu;
                banh.NhaCungCap = string.IsNullOrWhiteSpace(meta.NhaCungCap) ? "Lò Bánh Đồng Khánh Chính Gốc" : meta.NhaCungCap;
                banh.ViTriKho = string.IsNullOrWhiteSpace(meta.ViTriKho) ? "Kệ A1" : meta.ViTriKho;
                banh.DonViTinh = string.IsNullOrWhiteSpace(meta.DonViTinh) ? "Cái" : meta.DonViTinh;
                banh.BanTrucTiep = meta.BanTrucTiep;
            }
            else
            {
                banh.MaVach = $"8934680010{banh.MaBanh:D3}";
                banh.ThuongHieu = "Đồng Khánh";
                banh.NhaCungCap = "Lò Bánh Đồng Khánh Chính Gốc";
                banh.ViTriKho = "Kệ A1";
                banh.DonViTinh = "Cái";
                banh.BanTrucTiep = banh.TrangThaiBan;
            }
        }

        public void GanMetadataChoCombo(ComboMauCoSan combo, KiotMetadataStore store)
        {
            string key = $"COMBO_{combo.MaCb}";
            if (store.Items.TryGetValue(key, out var meta))
            {
                combo.MaVach = string.IsNullOrWhiteSpace(meta.MaVach) ? $"8934680990{combo.MaCb:D3}" : meta.MaVach;
                combo.ThuongHieu = string.IsNullOrWhiteSpace(meta.ThuongHieu) ? "Đồng Khánh" : meta.ThuongHieu;
                combo.NhaCungCap = string.IsNullOrWhiteSpace(meta.NhaCungCap) ? "Lò Bánh Đồng Khánh Chính Gốc" : meta.NhaCungCap;
                combo.ViTriKho = string.IsNullOrWhiteSpace(meta.ViTriKho) ? "Kệ B1" : meta.ViTriKho;
                combo.DonViTinh = string.IsNullOrWhiteSpace(meta.DonViTinh) ? "Hộp" : meta.DonViTinh;
                combo.BanTrucTiep = meta.BanTrucTiep;
                combo.MaDm = meta.MaDmCombo ?? 0;
                combo.TenDm = string.IsNullOrWhiteSpace(meta.TenDmCombo) ? "ĐỒNG GIÁ 20K" : meta.TenDmCombo!;
                combo.TongTonKho = meta.TonKhoCombo ?? 40;
                combo.TrongLuongGram = meta.TrongLuongComboGram ?? 750;
            }
            else
            {
                combo.MaVach = $"8934680990{combo.MaCb:D3}";
                combo.ThuongHieu = "Đồng Khánh";
                combo.NhaCungCap = "Lò Bánh Đồng Khánh Chính Gốc";
                combo.ViTriKho = "Kệ B1";
                combo.DonViTinh = "Hộp";
                combo.BanTrucTiep = combo.TrangThai;
                combo.TenDm = "ĐỒNG GIÁ 20K";
                combo.TongTonKho = 40;
                combo.TrongLuongGram = 750;
            }
        }

        public void GanMetadataChoDonHang(DonHang dh, KiotMetadataStore? store = null)
        {
            store ??= DocDuLieu();
            if (store.DonHangs != null && store.DonHangs.TryGetValue(dh.MaDh, out var meta))
            {
                dh.MaNganHangHoan = meta.MaNganHangHoan;
                dh.SoTaiKhoanHoan = meta.SoTaiKhoanHoan;
                dh.TenChuTkHoan = meta.TenChuTkHoan;
                dh.LyDoHuyHoan = meta.LyDoHuyHoan;
                dh.GhiChuDieuChinhAdmin = meta.GhiChuDieuChinhAdmin;
                dh.ThoiGianCapNhatCuoi = meta.ThoiGianCapNhatCuoi;
                dh.NguoiCapNhatCuoi = meta.NguoiCapNhatCuoi;
                dh.HanThanhToan = meta.HanThanhToan ?? dh.NgayDat.AddMinutes(15);

                if (dh.DotGiaoHangDaDiems != null && meta.Thiep3DTheoToken != null)
                {
                    foreach (var dot in dh.DotGiaoHangDaDiems)
                    {
                        if (!string.IsNullOrWhiteSpace(dot.MaTokenThiepQr) &&
                            meta.Thiep3DTheoToken.TryGetValue(dot.MaTokenThiepQr, out var thiepMeta))
                        {
                            dot.TenNguoiNhanThiep = thiepMeta.TenNguoiNhanThiep;
                            dot.TenNguoiGuiThiep = thiepMeta.TenNguoiGuiThiep;
                        }
                    }
                }
            }
            else
            {
                dh.HanThanhToan = dh.NgayDat.AddMinutes(15);
            }
        }

        public KiotDonHangMeta LayMetadataDonHang(int maDh)
        {
            var store = DocDuLieu();
            if (store.DonHangs != null && store.DonHangs.TryGetValue(maDh, out var meta))
            {
                return meta;
            }
            return new KiotDonHangMeta { MaDh = maDh };
        }

        public void CapNhatMetadataDonHang(int maDh, Action<KiotDonHangMeta> capNhat)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                var store = DocDuLieuTuSqlInternal(db);
                store.DonHangs ??= new Dictionary<int, KiotDonHangMeta>();
                if (!store.DonHangs.TryGetValue(maDh, out var meta))
                {
                    meta = new KiotDonHangMeta { MaDh = maDh };
                    store.DonHangs[maDh] = meta;
                }
                capNhat(meta);
                LuuDuLieuVaoSqlInternal(db, store);
            }
        }

        public CauHinhThanhToanQr LayCauHinhQr()
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();

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
                var tracked = db.CauHinhHeThongs.FirstOrDefault(c => c.KhoaCauHinh == KHOA_CAU_HINH_QR_MOMO);
                if (tracked == null)
                {
                    db.CauHinhHeThongs.Add(new CauHinhHeThong
                    {
                        KhoaCauHinh = KHOA_CAU_HINH_QR_MOMO,
                        GiaTriJson = JsonSerializer.Serialize(macDinh, _jsonOptions),
                        MoTa = "Cấu hình Tài khoản nhận tiền Ví MoMo / VietQR (0814585526 - LE MINH DUC)",
                        NgayCapNhat = DateTime.Now
                    });
                }
                else
                {
                    tracked.GiaTriJson = JsonSerializer.Serialize(macDinh, _jsonOptions);
                    tracked.NgayCapNhat = DateTime.Now;
                }
                db.SaveChanges();
                return macDinh;
            }
        }

        public CauHinhThanhToanQr LayCauHinhThanhToan() => LayCauHinhQr();

        public void CapNhatCauHinhQr(Action<CauHinhThanhToanQr> capNhat)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                var store = DocDuLieuTuSqlInternal(db);
                store.CauHinhQr ??= new CauHinhThanhToanQr();
                capNhat(store.CauHinhQr);
                LuuDuLieuVaoSqlInternal(db, store);
            }
        }

        public CauHinhThanhToanQr CapNhatCauHinhQr(string? soTaiKhoanMoMo, string? tenChuTaiKhoan, string? tenNganHangVi, string? maBinNganHang = null, string? cuPhap = null)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CSDLBanhTrungThuContext>();
                var store = DocDuLieuTuSqlInternal(db);
                store.CauHinhQr ??= new CauHinhThanhToanQr();

                string stkSach = (soTaiKhoanMoMo ?? "0814585526").Replace(" ", "").Trim();
                if (string.IsNullOrWhiteSpace(stkSach)) stkSach = "0814585526";

                string sdtHienThi = stkSach.Length == 10
                    ? $"{stkSach[..4]} {stkSach.Substring(4, 3)} {stkSach.Substring(7, 3)}"
                    : stkSach;

                store.CauHinhQr.SoTaiKhoanMoMo = stkSach;
                store.CauHinhQr.SoDienThoaiHienThi = sdtHienThi;
                store.CauHinhQr.TenChuTaiKhoan = string.IsNullOrWhiteSpace(tenChuTaiKhoan) ? "LE MINH DUC" : tenChuTaiKhoan.Trim().ToUpperInvariant();
                store.CauHinhQr.TenNganHangVi = string.IsNullOrWhiteSpace(tenNganHangVi) ? "MOMO" : tenNganHangVi.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(maBinNganHang))
                {
                    store.CauHinhQr.MaBinNganHang = maBinNganHang.Trim();
                }
                if (!string.IsNullOrWhiteSpace(cuPhap))
                {
                    store.CauHinhQr.CuPhapChuyenKhoan = cuPhap.Trim();
                }

                LuuDuLieuVaoSqlInternal(db, store);
                return store.CauHinhQr;
            }
        }

        public void GhiNhanPhieuThuChi(PhieuThuChiKiot phieu)
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

        public void GhiPhieuThuChi(PhieuThuChiKiot phieu) => GhiNhanPhieuThuChi(phieu);

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

                var store = DocDuLieuTuSqlInternal(db);
                if (store.DonHangs != null)
                {
                    foreach (var meta in store.DonHangs.Values)
                    {
                        if (meta.Thiep3DTheoToken != null && meta.Thiep3DTheoToken.TryGetValue(tokenChuan, out var thiep))
                        {
                            return thiep;
                        }
                    }
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
    }
}
