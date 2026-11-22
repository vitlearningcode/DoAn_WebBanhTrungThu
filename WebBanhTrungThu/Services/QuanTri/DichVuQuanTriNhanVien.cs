using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Nhân sự nội bộ (dbo.HoSoNhanVien &amp; dbo.NguoiDung)
    /// theo mô hình phân quyền 2 vai trò: ADMIN (Chủ cửa hàng) &amp; NHAN_VIEN (Nhân viên Bán hàng),
    /// đồng thời tổng hợp đơn hàng và doanh số POS trực tiếp từ dbo.DonHang.MaNdLap.
    /// </summary>
    public class DichVuQuanTriNhanVien : DichVuCoSo, IDichVuQuanTriNhanVien
    {
        private static readonly PasswordHasher<NguoiDung> _passwordHasher = new();
        private static readonly string[] _chiNhanhMacDinh =
        {
            "TP. Long Xuyên",
            "TP. Hồ Chí Minh"
        };

        private static readonly string[] _caLamViecMacDinh =
        {
            "Toàn thời gian",
            "Ca sáng (07:30 - 14:30)",
            "Ca chiều (14:00 - 21:00)"
        };

        public DichVuQuanTriNhanVien(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        public async Task<NhanVienAdminViewModel> XayDungTrangNhanVienAsync(
            string? tuKhoa,
            string locVaiTro = "ALL",
            string locChiNhanh = "ALL",
            string locCaLamViec = "ALL",
            string locTrangThai = "ALL",
            int? maNdDangNhap = null)
        {
            var dsHoSo = await _db.HoSoNhanViens
                .AsNoTracking()
                .Include(h => h.MaNdNavigation)
                .OrderBy(h => h.MaCodeNv)
                .ThenBy(h => h.MaNv)
                .ToListAsync();

            var dsMaNd = dsHoSo.Select(h => h.MaNd).Distinct().ToList();
            var dsDonLap = dsMaNd.Count > 0
                ? await _db.DonHangs
                    .AsNoTracking()
                    .Where(d => d.MaNdLap.HasValue && dsMaNd.Contains(d.MaNdLap.Value))
                    .OrderByDescending(d => d.NgayDat)
                    .ToListAsync()
                : new List<DonHang>();

            var donTheoNhanVien = dsDonLap.ToLookup(d => d.MaNdLap!.Value);

            var tatCaNhanVien = dsHoSo.Select(h =>
            {
                var nd = h.MaNdNavigation;
                string maVtChuan = ChuanHoaMaVaiTroNoiBo(nd?.MaVt);
                string trangThaiLvChuan = string.Equals(h.TrangThaiLamViec, "TAM_KHOA", StringComparison.OrdinalIgnoreCase)
                    ? "TAM_KHOA"
                    : "DANG_LAM_VIEC";
                bool taiKhoanMo = nd?.TrangThai ?? false;
                var cacDon = donTheoNhanVien[h.MaNd].ToList();
                var donHopLe = cacDon
                    .Where(d => !string.Equals(d.TrangThaiDh, "DA_HUY", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(d.TrangThaiDh, "HOAN_TIEN_TRA_HANG", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return new NhanVienItemViewModel
                {
                    MaNv = h.MaNv,
                    MaNd = h.MaNd,
                    MaCodeNv = h.MaCodeNv,
                    HoTen = nd?.HoTen ?? $"Nhân sự #{h.MaNd}",
                    Sdt = nd?.Sdt ?? string.Empty,
                    Email = nd?.Email ?? string.Empty,
                    MaVt = maVtChuan,
                    ChucVu = maVtChuan == "ADMIN" ? "Chủ cửa hàng" : "Nhân viên Bán hàng",
                    ChiNhanh = string.IsNullOrWhiteSpace(h.ChiNhanh) ? "TP. Long Xuyên" : h.ChiNhanh,
                    CaLamViec = string.IsNullOrWhiteSpace(h.CaLamViec) ? "Toàn thời gian" : h.CaLamViec,
                    NgayVaoLam = h.NgayVaoLam,
                    TrangThaiLamViec = trangThaiLvChuan,
                    TrangThaiTaiKhoan = taiKhoanMo,
                    GhiChu = h.GhiChu,
                    NgayTao = nd?.NgayTao ?? DateTime.Now,
                    TongDonDaLap = cacDon.Count,
                    SoDonHopLe = donHopLe.Count,
                    TongDoanhSo = Math.Round(donHopLe.Sum(d => d.TongThanhToanCuoi), 0),
                    DanhSachDonGanDay = cacDon.Take(50).Select(d => new DonHangNhanVienTomTatViewModel
                    {
                        MaDh = d.MaDh,
                        MaTraCuuDh = d.MaTraCuuDh,
                        HoTenNguoiMua = d.HoTenNguoiMua,
                        SdtNguoiMua = d.SdtnguoiMua,
                        NgayDat = d.NgayDat,
                        TongThanhToanCuoi = Math.Round(d.TongThanhToanCuoi, 0),
                        PhuongThucTt = d.PhuongThucTt,
                        TrangThaiDh = d.TrangThaiDh,
                        TrangThaiTt = d.TrangThaiTt,
                        TenTrangThaiDhHienThi = d.TenTrangThaiDhHienThi
                    }).ToList()
                };
            }).ToList();

            var dsChiNhanh = GopDanhSachGiaTri(_chiNhanhMacDinh, tatCaNhanVien.Select(x => x.ChiNhanh));
            var dsCaLamViec = GopDanhSachGiaTri(_caLamViecMacDinh, tatCaNhanVien.Select(x => x.CaLamViec));

            string vtLoc = ChuanHoaChuoi(locVaiTro, "ALL").ToUpperInvariant();
            string cnLoc = ChuanHoaChuoi(locChiNhanh, "ALL");
            string caLoc = ChuanHoaChuoi(locCaLamViec, "ALL");
            string ttLoc = ChuanHoaChuoi(locTrangThai, "ALL").ToUpperInvariant();
            string tkLoc = ChuanHoaChuoi(tuKhoa);

            var ketQuaLoc = tatCaNhanVien.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tkLoc))
            {
                ketQuaLoc = ketQuaLoc.Where(n =>
                    n.MaCodeNv.Contains(tkLoc, StringComparison.OrdinalIgnoreCase)
                    || n.HoTen.Contains(tkLoc, StringComparison.OrdinalIgnoreCase)
                    || n.Sdt.Contains(tkLoc, StringComparison.OrdinalIgnoreCase)
                    || n.Email.Contains(tkLoc, StringComparison.OrdinalIgnoreCase)
                    || n.ChiNhanh.Contains(tkLoc, StringComparison.OrdinalIgnoreCase));
            }
            if (vtLoc != "ALL")
            {
                ketQuaLoc = ketQuaLoc.Where(n => n.MaVt == vtLoc);
            }
            if (!string.Equals(cnLoc, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                ketQuaLoc = ketQuaLoc.Where(n => string.Equals(n.ChiNhanh, cnLoc, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.Equals(caLoc, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                ketQuaLoc = ketQuaLoc.Where(n => string.Equals(n.CaLamViec, caLoc, StringComparison.OrdinalIgnoreCase));
            }
            if (ttLoc == "DANG_LAM_VIEC")
            {
                ketQuaLoc = ketQuaLoc.Where(n => n.DangLamViec);
            }
            else if (ttLoc == "TAM_KHOA")
            {
                ketQuaLoc = ketQuaLoc.Where(n => !n.DangLamViec);
            }

            return new NhanVienAdminViewModel
            {
                DanhSachNhanVien = ketQuaLoc.ToList(),
                DanhSachChiNhanh = dsChiNhanh,
                DanhSachCaLamViec = dsCaLamViec,
                TuKhoa = tuKhoa,
                LocVaiTro = vtLoc,
                LocChiNhanh = cnLoc,
                LocCaLamViec = caLoc,
                LocTrangThai = ttLoc,
                MaNdDangNhap = maNdDangNhap,
                TongNhanSu = tatCaNhanVien.Count,
                SoDangLamViec = tatCaNhanVien.Count(n => n.DangLamViec),
                SoNhanVienBanHang = tatCaNhanVien.Count(n => n.MaVt == "NHAN_VIEN"),
                SoDangTamKhoa = tatCaNhanVien.Count(n => !n.DangLamViec)
            };
        }

        public async Task<object?> LayChiTietNhanVienJsonAsync(int maNd)
        {
            var hs = await _db.HoSoNhanViens
                .AsNoTracking()
                .Include(h => h.MaNdNavigation)
                .FirstOrDefaultAsync(h => h.MaNd == maNd);

            if (hs == null || hs.MaNdNavigation == null) return null;

            string maVtChuan = ChuanHoaMaVaiTroNoiBo(hs.MaNdNavigation.MaVt);
            string trangThaiLv = (hs.MaNdNavigation.TrangThai && !string.Equals(hs.TrangThaiLamViec, "TAM_KHOA", StringComparison.OrdinalIgnoreCase))
                ? "DANG_LAM_VIEC"
                : "TAM_KHOA";

            return new
            {
                maNv = hs.MaNv,
                maNd = hs.MaNd,
                maCodeNv = hs.MaCodeNv,
                hoTen = hs.MaNdNavigation.HoTen,
                sdt = hs.MaNdNavigation.Sdt,
                email = hs.MaNdNavigation.Email,
                maVt = maVtChuan,
                chucVu = maVtChuan == "ADMIN" ? "Chủ cửa hàng" : "Nhân viên Bán hàng",
                chiNhanh = hs.ChiNhanh,
                caLamViec = hs.CaLamViec,
                ngayVaoLam = hs.NgayVaoLam.ToString("yyyy-MM-dd"),
                trangThaiLamViec = trangThaiLv,
                ghiChu = hs.GhiChu ?? string.Empty
            };
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuNhanVienAsync(LuuNhanVienRequest req, int? maNdDangNhap)
        {
            string hoTen = ChuanHoaChuoi(req.HoTen);
            string sdt = ChuanHoaChuoi(req.Sdt);
            string email = ChuanHoaChuoi(req.Email);

            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(sdt) || string.IsNullOrWhiteSpace(email))
            {
                return (false, "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Email nhân viên!");
            }
            if (!email.Contains('@') || email.Length > 150)
            {
                return (false, "Email nhân viên không đúng định dạng hoặc vượt quá 150 ký tự!");
            }
            if (sdt.Length > 15)
            {
                return (false, "Số điện thoại không được vượt quá 15 ký tự!");
            }

            string maVtChuan = ChuanHoaMaVaiTroNoiBo(req.MaVt);
            string chucVuChuan = maVtChuan == "ADMIN" ? "Chủ cửa hàng" : "Nhân viên Bán hàng";
            string chiNhanh = ChuanHoaChuoi(req.ChiNhanh, "TP. Long Xuyên");
            if (chiNhanh.Length > 150) chiNhanh = chiNhanh[..150];
            string caLamViec = ChuanHoaChuoi(req.CaLamViec, "Toàn thời gian");
            if (caLamViec.Length > 100) caLamViec = caLamViec[..100];
            string trangThaiLv = string.Equals(req.TrangThaiLamViec?.Trim(), "TAM_KHOA", StringComparison.OrdinalIgnoreCase)
                ? "TAM_KHOA"
                : "DANG_LAM_VIEC";
            bool trangThaiNd = trangThaiLv == "DANG_LAM_VIEC";
            DateOnly ngayVaoLam = req.NgayVaoLam ?? LayNgayHienTai();
            string? ghiChu = string.IsNullOrWhiteSpace(req.GhiChu) ? null : req.GhiChu.Trim();
            if (ghiChu != null && ghiChu.Length > 500) ghiChu = ghiChu[..500];

            await DamBaoHaiVaiTroNoiBoAsync();

            if (req.MaNv > 0 || req.MaNd > 0)
            {
                var hs = await _db.HoSoNhanViens
                    .Include(h => h.MaNdNavigation)
                    .FirstOrDefaultAsync(h => (req.MaNv > 0 && h.MaNv == req.MaNv) || (req.MaNd > 0 && h.MaNd == req.MaNd));

                if (hs == null || hs.MaNdNavigation == null)
                {
                    return (false, "Không tìm thấy hồ sơ nhân viên cần cập nhật!");
                }

                if (maNdDangNhap.HasValue && hs.MaNd == maNdDangNhap.Value)
                {
                    if (!trangThaiNd)
                    {
                        return (false, "Không thể tự khóa tài khoản Chủ cửa hàng đang đăng nhập!");
                    }
                    if (ChuanHoaMaVaiTroNoiBo(hs.MaNdNavigation.MaVt) == "ADMIN" && maVtChuan != "ADMIN")
                    {
                        return (false, "Không thể tự hạ quyền tài khoản Chủ cửa hàng đang đăng nhập!");
                    }
                }

                string maCode = ChuanHoaChuoi(req.MaCodeNv, hs.MaCodeNv).ToUpperInvariant();
                if (maCode.Length > 20) maCode = maCode[..20];

                string emailLower = email.ToLowerInvariant();
                bool trungEmail = await _db.NguoiDungs.AsNoTracking()
                    .AnyAsync(u => u.MaNd != hs.MaNd && u.Email.ToLower() == emailLower);
                if (trungEmail)
                {
                    return (false, $"Email '{email}' đã được sử dụng bởi tài khoản khác!");
                }

                bool trungSdt = await _db.NguoiDungs.AsNoTracking()
                    .AnyAsync(u => u.MaNd != hs.MaNd && u.Sdt == sdt);
                if (trungSdt)
                {
                    return (false, $"Số điện thoại '{sdt}' đã được sử dụng bởi tài khoản khác!");
                }

                bool trungCode = await _db.HoSoNhanViens.AsNoTracking()
                    .AnyAsync(h => h.MaNv != hs.MaNv && h.MaCodeNv.ToUpper() == maCode);
                if (trungCode)
                {
                    return (false, $"Mã nhân viên [{maCode}] đã tồn tại trong hệ thống!");
                }

                return await ThucThiTrongGiaoDichAsync(async () =>
                {
                    hs.MaNdNavigation.HoTen = hoTen.Length > 150 ? hoTen[..150] : hoTen;
                    hs.MaNdNavigation.Sdt = sdt;
                    hs.MaNdNavigation.Email = email;
                    hs.MaNdNavigation.MaVt = maVtChuan;
                    hs.MaNdNavigation.TrangThai = trangThaiNd;

                    hs.MaCodeNv = maCode;
                    hs.ChucVu = chucVuChuan;
                    hs.ChiNhanh = chiNhanh;
                    hs.CaLamViec = caLamViec;
                    hs.NgayVaoLam = ngayVaoLam;
                    hs.TrangThaiLamViec = trangThaiLv;
                    hs.GhiChu = ghiChu;

                    await _db.SaveChangesAsync();
                    return (true, $"Đã cập nhật hồ sơ nhân viên [{hs.MaCodeNv}] {hs.MaNdNavigation.HoTen} ({chucVuChuan}).");
                });
            }

            string mkKhoiTao = ChuanHoaChuoi(req.MatKhauKhoiTao);
            if (mkKhoiTao.Length < 6)
            {
                return (false, "Mật khẩu khởi tạo cho nhân viên mới phải có ít nhất 6 ký tự!");
            }

            string emailMoiLower = email.ToLowerInvariant();
            if (await _db.NguoiDungs.AsNoTracking().AnyAsync(u => u.Email.ToLower() == emailMoiLower))
            {
                return (false, $"Email '{email}' đã tồn tại trong hệ thống!");
            }
            if (await _db.NguoiDungs.AsNoTracking().AnyAsync(u => u.Sdt == sdt))
            {
                return (false, $"Số điện thoại '{sdt}' đã tồn tại trong hệ thống!");
            }

            string maCodeMoi = ChuanHoaChuoi(req.MaCodeNv).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(maCodeMoi))
            {
                maCodeMoi = await SinhMaCodeNvTuDongAsync();
            }
            if (maCodeMoi.Length > 20) maCodeMoi = maCodeMoi[..20];

            if (await _db.HoSoNhanViens.AsNoTracking().AnyAsync(h => h.MaCodeNv.ToUpper() == maCodeMoi))
            {
                return (false, $"Mã nhân viên [{maCodeMoi}] đã tồn tại trong hệ thống!");
            }

            return await ThucThiTrongGiaoDichAsync(async () =>
            {
                var ndMoi = new NguoiDung
                {
                    MaVt = maVtChuan,
                    HoTen = hoTen.Length > 150 ? hoTen[..150] : hoTen,
                    Sdt = sdt,
                    Email = email,
                    DiemTichLuy = 0,
                    HangTv = maVtChuan == "ADMIN" ? "Kim Cương" : "Đồng",
                    TrangThai = trangThaiNd,
                    NgayTao = LayThoiGianHeThong()
                };
                ndMoi.MatKhauHash = _passwordHasher.HashPassword(ndMoi, mkKhoiTao);

                _db.NguoiDungs.Add(ndMoi);
                await _db.SaveChangesAsync();

                var hsMoi = new HoSoNhanVien
                {
                    MaNd = ndMoi.MaNd,
                    MaCodeNv = maCodeMoi,
                    ChucVu = chucVuChuan,
                    ChiNhanh = chiNhanh,
                    CaLamViec = caLamViec,
                    NgayVaoLam = ngayVaoLam,
                    TrangThaiLamViec = trangThaiLv,
                    GhiChu = ghiChu
                };

                _db.HoSoNhanViens.Add(hsMoi);
                await _db.SaveChangesAsync();

                return (true, $"Đã thêm mới nhân viên [{hsMoi.MaCodeNv}] {ndMoi.HoTen} ({chucVuChuan})!");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> DatLaiMatKhauAsync(DatLaiMatKhauNvRequest req)
        {
            if (req.MaNd <= 0)
            {
                return (false, "Mã tài khoản nhân viên không hợp lệ!");
            }

            string mkMoi = ChuanHoaChuoi(req.MatKhauMoi);
            if (mkMoi.Length < 6)
            {
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự!");
            }

            string xacNhan = ChuanHoaChuoi(req.XacNhanMatKhau);
            if (!string.IsNullOrEmpty(xacNhan) && !string.Equals(mkMoi, xacNhan, StringComparison.Ordinal))
            {
                return (false, "Mật khẩu xác nhận không khớp với mật khẩu mới!");
            }

            var hs = await _db.HoSoNhanViens
                .Include(h => h.MaNdNavigation)
                .FirstOrDefaultAsync(h => h.MaNd == req.MaNd);

            if (hs == null || hs.MaNdNavigation == null)
            {
                return (false, "Không tìm thấy hồ sơ nhân viên cần đặt lại mật khẩu!");
            }

            hs.MaNdNavigation.MatKhauHash = _passwordHasher.HashPassword(hs.MaNdNavigation, mkMoi);
            await _db.SaveChangesAsync();

            return (true, $"Đã đặt lại mật khẩu cho nhân viên [{hs.MaCodeNv}] {hs.MaNdNavigation.HoTen} thành công.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> ChuyenTrangThaiKhoaAsync(int maNdMucTieu, int? maNdDangNhap)
        {
            var hs = await _db.HoSoNhanViens
                .Include(h => h.MaNdNavigation)
                .FirstOrDefaultAsync(h => h.MaNd == maNdMucTieu);

            if (hs == null || hs.MaNdNavigation == null)
            {
                return (false, "Không tìm thấy hồ sơ nhân viên!");
            }

            bool dangLamViec = hs.MaNdNavigation.TrangThai
                && !string.Equals(hs.TrangThaiLamViec, "TAM_KHOA", StringComparison.OrdinalIgnoreCase);

            if (dangLamViec && maNdDangNhap.HasValue && maNdMucTieu == maNdDangNhap.Value)
            {
                return (false, "Không thể tự khóa tài khoản Chủ cửa hàng đang đăng nhập!");
            }

            return await ThucThiTrongGiaoDichAsync(async () =>
            {
                if (dangLamViec)
                {
                    hs.TrangThaiLamViec = "TAM_KHOA";
                    hs.MaNdNavigation.TrangThai = false;
                }
                else
                {
                    hs.TrangThaiLamViec = "DANG_LAM_VIEC";
                    hs.MaNdNavigation.TrangThai = true;
                }

                await _db.SaveChangesAsync();

                string hanhDong = dangLamViec ? "tạm khóa" : "mở khóa";
                return (true, $"Đã {hanhDong} tài khoản nhân viên [{hs.MaCodeNv}] {hs.MaNdNavigation.HoTen}.");
            });
        }

        private async Task DamBaoHaiVaiTroNoiBoAsync()
        {
            var vaiTros = new (string MaVt, string TenVt, string MoTa)[]
            {
                ("ADMIN", "Chủ cửa hàng", "Toàn quyền quản trị cửa hàng, kho bãi, nhân sự và sổ quỹ Nguyệt Cát ERP"),
                ("NHAN_VIEN", "Nhân viên Bán hàng", "Nhân viên bán hàng tại quầy POS và xử lý đơn hàng trực tuyến")
            };

            bool coThem = false;
            foreach (var vt in vaiTros)
            {
                if (!await _db.VaiTros.AnyAsync(v => v.MaVt == vt.MaVt))
                {
                    _db.VaiTros.Add(new VaiTro
                    {
                        MaVt = vt.MaVt,
                        TenVt = vt.TenVt,
                        MoTa = vt.MoTa
                    });
                    coThem = true;
                }
            }
            if (coThem)
            {
                await _db.SaveChangesAsync();
            }
        }

        private async Task<string> SinhMaCodeNvTuDongAsync()
        {
            var dsCode = await _db.HoSoNhanViens
                .AsNoTracking()
                .Select(h => h.MaCodeNv)
                .ToListAsync();

            int maxSo = 0;
            foreach (var code in dsCode)
            {
                if (code.StartsWith("NV", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(code[2..], out int so)
                    && so > maxSo)
                {
                    maxSo = so;
                }
            }

            return $"NV{(maxSo + 1):D3}";
        }

        private static string ChuanHoaMaVaiTroNoiBo(string? maVt)
        {
            string vt = (maVt ?? string.Empty).Trim().ToUpperInvariant();
            return (vt == "ADMIN" || vt == "QUAN_TRI") ? "ADMIN" : "NHAN_VIEN";
        }

        private static List<string> GopDanhSachGiaTri(IEnumerable<string> macDinh, IEnumerable<string> tuDb)
        {
            var ketQua = new List<string>();
            foreach (var muc in macDinh.Concat(tuDb))
            {
                string sach = (muc ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(sach) && !ketQua.Contains(sach, StringComparer.OrdinalIgnoreCase))
                {
                    ketQua.Add(sach);
                }
            }
            return ketQua;
        }
    }
}
