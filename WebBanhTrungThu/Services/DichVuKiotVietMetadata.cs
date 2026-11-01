using System.Text.Encodings.Web;
using System.Text.Json;
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
    }

    public class KiotDonHangMeta
    {
        public int MaDh { get; set; }
        public string? MaNganHangHoan { get; set; }
        public string? SoTaiKhoanHoan { get; set; }
        public string? TenChuTkHoan { get; set; }
        public string? LyDoHuyHoan { get; set; }
        public string? GhiChuDieuChinhAdmin { get; set; }
        public string? ThoiGianCapNhatCuoi { get; set; }
        public string? NguoiCapNhatCuoi { get; set; }
    }

    public class DichVuKiotVietMetadata
    {
        private readonly string _filePath;
        private static readonly object _lock = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DichVuKiotVietMetadata(IWebHostEnvironment env)
        {
            string dataDir = Path.Combine(env.WebRootPath, "data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            _filePath = Path.Combine(dataDir, "kiotviet_metadata.json");
        }

        public KiotMetadataStore DocDuLieu()
        {
            lock (_lock)
            {
                if (!File.Exists(_filePath))
                {
                    var macDinh = TaoDuLieuMacDinh();
                    LuuDuLieuInternal(macDinh);
                    return macDinh;
                }

                try
                {
                    string json = File.ReadAllText(_filePath);
                    var store = JsonSerializer.Deserialize<KiotMetadataStore>(json, _jsonOptions);
                    if (store == null)
                    {
                        store = TaoDuLieuMacDinh();
                        LuuDuLieuInternal(store);
                    }
                    return store;
                }
                catch
                {
                    var macDinh = TaoDuLieuMacDinh();
                    LuuDuLieuInternal(macDinh);
                    return macDinh;
                }
            }
        }

        public void LuuDuLieu(KiotMetadataStore store)
        {
            lock (_lock)
            {
                LuuDuLieuInternal(store);
            }
        }

        private void LuuDuLieuInternal(KiotMetadataStore store)
        {
            string json = JsonSerializer.Serialize(store, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }

        private KiotMetadataStore TaoDuLieuMacDinh()
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

        public void GanMetadataChoDonHang(DonHang dh, KiotMetadataStore store)
        {
            if (store.DonHangs != null && store.DonHangs.TryGetValue(dh.MaDh, out var meta))
            {
                dh.MaNganHangHoan = meta.MaNganHangHoan;
                dh.SoTaiKhoanHoan = meta.SoTaiKhoanHoan;
                dh.TenChuTkHoan = meta.TenChuTkHoan;
                dh.LyDoHuyHoan = meta.LyDoHuyHoan;
                dh.GhiChuDieuChinhAdmin = meta.GhiChuDieuChinhAdmin;
                dh.ThoiGianCapNhatCuoi = meta.ThoiGianCapNhatCuoi;
                dh.NguoiCapNhatCuoi = meta.NguoiCapNhatCuoi;
            }
        }

        public void CapNhatMetadataDonHang(int maDh, Action<KiotDonHangMeta> capNhat)
        {
            var store = DocDuLieu();
            store.DonHangs ??= new Dictionary<int, KiotDonHangMeta>();
            if (!store.DonHangs.TryGetValue(maDh, out var meta))
            {
                meta = new KiotDonHangMeta { MaDh = maDh };
                store.DonHangs[maDh] = meta;
            }
            capNhat(meta);
            LuuDuLieu(store);
        }
    }
}

