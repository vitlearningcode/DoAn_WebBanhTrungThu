using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;

namespace WebBanhTrungThu.Data
{
    /// <summary>
    /// Phần Seed dữ liệu mặc định (Nhà cung cấp, Thương hiệu, chuẩn hóa thuộc tính Bánh/Combo, Vai trò &amp; Hồ sơ nhân viên).
    /// </summary>
    public static partial class KhoiTaoCsdl
    {
        private static async Task SeedNhaCungCapMacDinhAsync(CSDLBanhTrungThuContext db)
        {
            if (!await db.NhaCungCaps.AnyAsync())
            {
                DateTime bayGio = DateTime.Now;
                db.NhaCungCaps.AddRange(
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC001",
                        TenNcc = "Nhà phân phối Kinh Đô Chính Hãng",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Kinh Đô",
                        PhanTramChietKhauMacDinh = 28m,
                        SoDienThoai = "02838270838",
                        Email = "phanphoi.kinhdo@mondelez.vn",
                        DiaChi = "Số 138 Hai Bà Trưng, Quận 1, TP. Hồ Chí Minh",
                        GhiChu = "Hãng / NPP Cấp 1 chính thức các dòng Kinh Đô Trăng Vàng & Truyền Thống",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC002",
                        TenNcc = "Lò Bánh Đồng Khánh Chính Gốc",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Đồng Khánh",
                        PhanTramChietKhauMacDinh = 26m,
                        SoDienThoai = "02838552418",
                        Email = "kinhdoanh@banhdongkhanh.vn",
                        DiaChi = "Số 62 Trần Hưng Đạo, Quận 5, TP. Hồ Chí Minh",
                        GhiChu = "Lò bánh gia truyền Đồng Khánh Bông Lúa Vàng",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC003",
                        TenNcc = "Bánh Trung Thu Như Lan TP.HCM",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Như Lan",
                        PhanTramChietKhauMacDinh = 25m,
                        SoDienThoai = "02838292970",
                        Email = "daily@nhulanbakery.vn",
                        DiaChi = "Số 50 Hàm Nghi, Quận 1, TP. Hồ Chí Minh",
                        GhiChu = "Nguồn hàng chính gốc Như Lan TP.HCM, chiết khấu theo lô mùa vụ",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC004",
                        TenNcc = "Brodard Bakery Saigon",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Brodard",
                        PhanTramChietKhauMacDinh = 24m,
                        SoDienThoai = "02838223966",
                        Email = "b2c-wholesale@brodardbakery.vn",
                        DiaChi = "Số 11 Nguyễn Thiệp, Quận 1, TP. Hồ Chí Minh",
                        GhiChu = "Dòng bánh Pháp thượng hạng Brodard, giao lạnh bảo quản tiêu chuẩn",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC005",
                        TenNcc = "Givral Bakery Chính Hãng",
                        LoaiNcc = "HANG_CHINH_HANG",
                        ThuongHieuCungCap = "Givral",
                        PhanTramChietKhauMacDinh = 25m,
                        SoDienThoai = "02838684377",
                        Email = "nguonhang@givralbakery.vn",
                        DiaChi = "Lô II-1B, KCN Tân Bình, Quận Tân Phú, TP. Hồ Chí Minh",
                        GhiChu = "Hãng bánh tươi Givral ít ngọt & dòng ăn kiêng Isomalt",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    },
                    new NhaCungCap
                    {
                        MaCodeNcc = "NCC006",
                        TenNcc = "Đại Lý Điều Tiết Bánh Miền Tây (An Giang)",
                        LoaiNcc = "DAI_LY_DIEU_TIET",
                        ThuongHieuCungCap = "Đa thương hiệu",
                        PhanTramChietKhauMacDinh = 22m,
                        SoDienThoai = "02963841225",
                        Email = "dieutiet.mientay@gmail.com",
                        DiaChi = "Số 215 Trần Hưng Đạo, TP. Long Xuyên, An Giang",
                        GhiChu = "Đại lý điều tiết nguồn hàng đa thương hiệu bổ sung gấp trong mùa cao điểm",
                        DangHoatDong = true,
                        NgayTao = bayGio
                    }
                );
                await db.SaveChangesAsync();
            }

            // Liên kết MaNcc cho các phiếu nhập cũ chưa có khóa ngoại MaNcc (không ghi đè chuỗi NhaCungCap cũ)
            var dsPhieuChuaGan = await db.PhieuNhapKhos
                .Where(p => p.MaNcc == null)
                .ToListAsync();

            if (dsPhieuChuaGan.Count > 0)
            {
                var dsNcc = await db.NhaCungCaps.AsNoTracking().ToListAsync();
                bool coCapNhatPn = false;

                foreach (var pn in dsPhieuChuaGan)
                {
                    string tenCu = (pn.NhaCungCap ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(tenCu)) continue;

                    var khop = dsNcc.FirstOrDefault(n =>
                        string.Equals(n.TenNcc, tenCu, StringComparison.OrdinalIgnoreCase))
                        ?? dsNcc.FirstOrDefault(n =>
                            !string.Equals(n.ThuongHieuCungCap, "Đa thương hiệu", StringComparison.OrdinalIgnoreCase) &&
                            (string.Equals(n.ThuongHieuCungCap, tenCu, StringComparison.OrdinalIgnoreCase) ||
                             tenCu.Contains(n.ThuongHieuCungCap, StringComparison.OrdinalIgnoreCase)));

                    if (khop != null)
                    {
                        pn.MaNcc = khop.MaNcc;
                        coCapNhatPn = true;
                    }
                }

                if (coCapNhatPn)
                {
                    await db.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedThuongHieuVaChuanHoaHangHoaAsync(CSDLBanhTrungThuContext db)
        {
            var danhSachMacDinh = new (string MaCode, string Ten, string XuatXu, string MoTa)[]
            {
                ("KINH_DO", "Kinh Đô", "TP. Hồ Chí Minh", "Thương hiệu bánh Trung Thu Kinh Đô Trăng Vàng & Truyền Thống"),
                ("DONG_KHANH", "Đồng Khánh", "TP. Hồ Chí Minh", "Lò bánh Trung Thu gia truyền Đồng Khánh Bông Lúa Vàng"),
                ("NHU_LAN", "Như Lan", "TP. Hồ Chí Minh", "Thương hiệu bánh Trung Thu Như Lan Sài Gòn"),
                ("BRODARD", "Brodard", "TP. Hồ Chí Minh", "Bánh Trung Thu Pháp thượng hạng Brodard Bakery"),
                ("GIVRAL", "Givral", "TP. Hồ Chí Minh", "Bánh tươi Givral Bakery ít ngọt & đường ăn kiêng Isomalt"),
                ("NGUYET_CAT", "Nguyệt Cát", "TP. Long Xuyên, An Giang", "Thương hiệu lễ hộp tuyển chọn Nguyệt Cát")
            };

            var dsThuongHieuDb = await db.ThuongHieus.ToListAsync();
            bool coThemTh = false;
            foreach (var item in danhSachMacDinh)
            {
                if (!dsThuongHieuDb.Any(t =>
                    string.Equals(t.MaCodeThuongHieu, item.MaCode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t.TenThuongHieu, item.Ten, StringComparison.OrdinalIgnoreCase)))
                {
                    var thMoi = new ThuongHieu
                    {
                        MaCodeThuongHieu = item.MaCode,
                        TenThuongHieu = item.Ten,
                        XuatXu = item.XuatXu,
                        MoTa = item.MoTa,
                        DangHoatDong = true
                    };
                    db.ThuongHieus.Add(thMoi);
                    dsThuongHieuDb.Add(thMoi);
                    coThemTh = true;
                }
            }
            if (coThemTh)
            {
                await db.SaveChangesAsync();
            }

            var dsNcc = await db.NhaCungCaps.AsNoTracking().ToListAsync();
            var dsDanhMuc = await db.DanhMucBanhs.AsNoTracking().OrderBy(d => d.ThuTuSx).ThenBy(d => d.MaDm).ToListAsync();
            int? maDmComboMacDinh = dsDanhMuc
                .FirstOrDefault(d => !string.Equals(d.DuongDanSlug, "dong-gia-20k", StringComparison.OrdinalIgnoreCase))?.MaDm
                ?? dsDanhMuc.FirstOrDefault()?.MaDm;

            string[] thuongHieuBanhMau = { "Đồng Khánh", "Kinh Đô", "Như Lan", "Brodard", "Givral", "Đồng Khánh", "Kinh Đô", "Như Lan" };
            string[] viTriBanhMau = { "Kệ A1", "Kệ A2", "Kệ B1", "Kệ B2", "Tủ Mát C1", "Kệ A1", "Kệ B1", "Kệ A2" };

            int? TimMaNccTheoThuongHieu(string tenTh)
            {
                var khop = dsNcc.FirstOrDefault(n =>
                    string.Equals(n.ThuongHieuCungCap, tenTh, StringComparison.OrdinalIgnoreCase)
                    || n.TenNcc.Contains(tenTh, StringComparison.OrdinalIgnoreCase));
                return khop?.MaNcc ?? dsNcc.FirstOrDefault()?.MaNcc;
            }

            var dsBanh = await db.SanPhamBanhs.OrderBy(b => b.MaBanh).ToListAsync();
            bool coSuaBanh = false;
            for (int i = 0; i < dsBanh.Count; i++)
            {
                var b = dsBanh[i];
                int idx = i % thuongHieuBanhMau.Length;
                string tenThMacDinh = thuongHieuBanhMau[idx];
                var thDb = dsThuongHieuDb.FirstOrDefault(t => string.Equals(t.TenThuongHieu, tenThMacDinh, StringComparison.OrdinalIgnoreCase))
                    ?? dsThuongHieuDb.FirstOrDefault();

                if (!b.MaThuongHieu.HasValue && thDb != null)
                {
                    b.MaThuongHieu = thDb.MaThuongHieu;
                    coSuaBanh = true;
                }
                if (!b.MaNccMacDinh.HasValue)
                {
                    string tenThChuan = dsThuongHieuDb.FirstOrDefault(t => t.MaThuongHieu == b.MaThuongHieu)?.TenThuongHieu ?? tenThMacDinh;
                    b.MaNccMacDinh = TimMaNccTheoThuongHieu(tenThChuan);
                    if (b.MaNccMacDinh.HasValue) coSuaBanh = true;
                }
                if (string.IsNullOrWhiteSpace(b.MaVach))
                {
                    b.MaVach = $"8934680010{b.MaBanh:D3}";
                    coSuaBanh = true;
                }
                if (string.IsNullOrWhiteSpace(b.ViTriKho))
                {
                    b.ViTriKho = viTriBanhMau[idx];
                    coSuaBanh = true;
                }
                if (string.IsNullOrWhiteSpace(b.DonViTinh))
                {
                    b.DonViTinh = "Cái";
                    coSuaBanh = true;
                }
            }
            if (coSuaBanh)
            {
                await db.SaveChangesAsync();
            }

            string[] thuongHieuComboMau = { "Đồng Khánh", "Kinh Đô", "Như Lan", "Nguyệt Cát" };
            var dsCombo = await db.ComboMauCoSans.OrderBy(c => c.MaCb).ToListAsync();
            bool coSuaCombo = false;
            for (int i = 0; i < dsCombo.Count; i++)
            {
                var cb = dsCombo[i];
                string tenThMacDinh = thuongHieuComboMau[i % thuongHieuComboMau.Length];
                var thDb = dsThuongHieuDb.FirstOrDefault(t => string.Equals(t.TenThuongHieu, tenThMacDinh, StringComparison.OrdinalIgnoreCase))
                    ?? dsThuongHieuDb.FirstOrDefault();

                if (!cb.MaThuongHieu.HasValue && thDb != null)
                {
                    cb.MaThuongHieu = thDb.MaThuongHieu;
                    coSuaCombo = true;
                }
                if (!cb.MaDm.HasValue && maDmComboMacDinh.HasValue)
                {
                    cb.MaDm = maDmComboMacDinh.Value;
                    coSuaCombo = true;
                }
                if (string.IsNullOrWhiteSpace(cb.MaVach))
                {
                    cb.MaVach = $"8934680990{cb.MaCb:D3}";
                    coSuaCombo = true;
                }
                if (string.IsNullOrWhiteSpace(cb.ViTriKho))
                {
                    cb.ViTriKho = "Kệ B1";
                    coSuaCombo = true;
                }
                if (string.IsNullOrWhiteSpace(cb.DonViTinh))
                {
                    cb.DonViTinh = "Hộp";
                    coSuaCombo = true;
                }
                if (cb.TongTonKho <= 0)
                {
                    cb.TongTonKho = 35 + ((i + 1) * 5);
                    coSuaCombo = true;
                }
                if (cb.TrongLuongGram <= 0)
                {
                    cb.TrongLuongGram = 750;
                    coSuaCombo = true;
                }
            }
            if (coSuaCombo)
            {
                await db.SaveChangesAsync();
            }
        }

        private static async Task SeedHoSoNhanVienVaDongBoVaiTroAsync(CSDLBanhTrungThuContext db)
        {
            var vaiTroBatBuoc = new (string MaVt, string TenVt, string MoTa)[]
            {
                ("ADMIN", "Chủ cửa hàng", "Toàn quyền quản trị cửa hàng, kho bãi, nhân sự và sổ quỹ Nguyệt Cát ERP"),
                ("NHAN_VIEN", "Nhân viên Bán hàng", "Nhân viên bán hàng tại quầy POS và xử lý đơn hàng trực tuyến"),
                ("KHACH_TV", "Khách thành viên", "Khách hàng thành viên tích điểm")
            };

            bool coThayDoiVt = false;
            foreach (var vt in vaiTroBatBuoc)
            {
                var vtDb = await db.VaiTros.FirstOrDefaultAsync(v => v.MaVt == vt.MaVt);
                if (vtDb == null)
                {
                    db.VaiTros.Add(new VaiTro
                    {
                        MaVt = vt.MaVt,
                        TenVt = vt.TenVt,
                        MoTa = vt.MoTa
                    });
                    coThayDoiVt = true;
                }
                else if (vt.MaVt == "NHAN_VIEN" && vtDb.TenVt != vt.TenVt)
                {
                    vtDb.TenVt = vt.TenVt;
                    coThayDoiVt = true;
                }
            }
            if (coThayDoiVt)
            {
                await db.SaveChangesAsync();
            }

            string mkSha = ThongTinPhienHelper.BamMatKhauSHA256("123456");
            var taiKhoanNoiBoMau = new (string Email, string Sdt, string HoTen, string MaVt, string HangTv)[]
            {
                ("admin@nguyetcat.vn", "0814585526", "Lê Minh Đức (Chủ cửa hàng)", "ADMIN", "Kim Cương"),
                ("thungan@nguyetcat.vn", "0908111222", "Trần Thu Ngân (Nhân viên Bán hàng)", "NHAN_VIEN", "Vàng"),
                ("giaohang@nguyetcat.vn", "0908333444", "Nguyễn Văn Giao (Nhân viên Bán hàng)", "NHAN_VIEN", "Bạc")
            };

            bool coThayDoiNd = false;
            foreach (var tk in taiKhoanNoiBoMau)
            {
                var tonTai = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == tk.Email || u.Sdt == tk.Sdt);
                if (tonTai == null)
                {
                    db.NguoiDungs.Add(new NguoiDung
                    {
                        MaVt = tk.MaVt,
                        HoTen = tk.HoTen,
                        Sdt = tk.Sdt,
                        Email = tk.Email,
                        MatKhauHash = mkSha,
                        DiemTichLuy = 500,
                        HangTv = tk.HangTv,
                        TrangThai = true,
                        NgayTao = DateTime.Now
                    });
                    coThayDoiNd = true;
                }
            }
            if (coThayDoiNd)
            {
                await db.SaveChangesAsync();
            }

            // Đồng bộ vai trò nội bộ hiện hữu về đúng 2 vai trò: ADMIN (Chủ cửa hàng) & NHAN_VIEN (Nhân viên Bán hàng)
            var cacVaiTroCuCapDuoi = new[] { "THU_NGAN", "GIAO_HANG", "NV_BAN_HANG" };
            var tatCaNoiBo = await db.NguoiDungs
                .Where(u => u.MaVt == "ADMIN"
                         || u.MaVt == "QUAN_TRI"
                         || u.MaVt == "NHAN_VIEN"
                         || cacVaiTroCuCapDuoi.Contains(u.MaVt)
                         || u.Email == "admin@nguyetcat.vn"
                         || u.Email == "thungan@nguyetcat.vn"
                         || u.Email == "giaohang@nguyetcat.vn")
                .OrderBy(u => u.MaNd)
                .ToListAsync();

            bool coDongBoVt = false;
            foreach (var nd in tatCaNoiBo)
            {
                if (string.Equals(nd.Email, "admin@nguyetcat.vn", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(nd.MaVt, "QUAN_TRI", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(nd.MaVt, "ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    if (nd.MaVt != "ADMIN")
                    {
                        nd.MaVt = "ADMIN";
                        coDongBoVt = true;
                    }
                }
                else if (nd.MaVt != "NHAN_VIEN")
                {
                    nd.MaVt = "NHAN_VIEN";
                    coDongBoVt = true;
                }
            }
            if (coDongBoVt)
            {
                await db.SaveChangesAsync();
            }

            // Seed bản ghi dbo.HoSoNhanVien cho các tài khoản nội bộ chưa có hồ sơ
            var dsMaNdDaCoHoSo = await db.HoSoNhanViens
                .AsNoTracking()
                .Select(h => h.MaNd)
                .ToListAsync();
            var setMaNdDaCo = dsMaNdDaCoHoSo.ToHashSet();

            var dsCodeDaDung = await db.HoSoNhanViens
                .AsNoTracking()
                .Select(h => h.MaCodeNv)
                .ToListAsync();
            var setCodeDaDung = new HashSet<string>(dsCodeDaDung, StringComparer.OrdinalIgnoreCase);

            int soThuTuKeTiep = 1;
            string SinhMaNvKeTiep()
            {
                while (true)
                {
                    string ungVien = $"NV{soThuTuKeTiep:D3}";
                    soThuTuKeTiep++;
                    if (!setCodeDaDung.Contains(ungVien))
                    {
                        setCodeDaDung.Add(ungVien);
                        return ungVien;
                    }
                }
            }

            bool coThemHoSo = false;
            foreach (var nd in tatCaNoiBo)
            {
                if (setMaNdDaCo.Contains(nd.MaNd)) continue;

                string emailLower = (nd.Email ?? string.Empty).Trim().ToLowerInvariant();
                string maCodeUutien;
                string chucVu;
                string caLamViec;
                string ghiChu;

                if (emailLower == "admin@nguyetcat.vn" || nd.MaVt == "ADMIN")
                {
                    maCodeUutien = "NV001";
                    chucVu = "Chủ cửa hàng";
                    caLamViec = "Toàn thời gian";
                    ghiChu = "Chủ cửa hàng quản trị toàn hệ thống Nguyệt Cát ERP";
                }
                else if (emailLower == "thungan@nguyetcat.vn")
                {
                    maCodeUutien = "NV002";
                    chucVu = "Nhân viên Bán hàng";
                    caLamViec = "Ca sáng (07:30 - 14:30)";
                    ghiChu = "Nhân viên bán hàng tại quầy POS & xử lý đơn trực tuyến";
                }
                else if (emailLower == "giaohang@nguyetcat.vn")
                {
                    maCodeUutien = "NV003";
                    chucVu = "Nhân viên Bán hàng";
                    caLamViec = "Ca chiều (14:00 - 21:00)";
                    ghiChu = "Nhân viên bán hàng & điều phối giao nhận đơn hàng";
                }
                else
                {
                    maCodeUutien = string.Empty;
                    chucVu = "Nhân viên Bán hàng";
                    caLamViec = "Toàn thời gian";
                    ghiChu = "Nhân sự nội bộ Nguyệt Cát";
                }

                string maCodeChot = !string.IsNullOrEmpty(maCodeUutien) && !setCodeDaDung.Contains(maCodeUutien)
                    ? maCodeUutien
                    : SinhMaNvKeTiep();
                setCodeDaDung.Add(maCodeChot);

                db.HoSoNhanViens.Add(new HoSoNhanVien
                {
                    MaNd = nd.MaNd,
                    MaCodeNv = maCodeChot,
                    ChucVu = chucVu,
                    ChiNhanh = "TP. Long Xuyên",
                    CaLamViec = caLamViec,
                    NgayVaoLam = DateOnly.FromDateTime(nd.NgayTao == default ? DateTime.Today : nd.NgayTao),
                    TrangThaiLamViec = nd.TrangThai ? "DANG_LAM_VIEC" : "TAM_KHOA",
                    GhiChu = ghiChu
                });
                setMaNdDaCo.Add(nd.MaNd);
                coThemHoSo = true;
            }

            if (coThemHoSo)
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
