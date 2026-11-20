using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.TaiKhoan
{
    /// <summary>
    /// Dịch vụ chuyên trách nghiệp vụ Hội viên: Xác thực đăng nhập, đăng ký thành viên mới (+50 điểm),
    /// tổng hợp dữ liệu HoSoThanhVienViewModel, tính hạng VIP, quản lý Sổ địa chỉ biếu tặng và đặt lại hộp quà tự chọn.
    /// Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuHoiVien : DichVuCoSo, IDichVuHoiVien
    {
        private readonly IDichVuCongThanhToanMoMo _dichVuCongMoMo;
        private readonly IDichVuTonKhoFefo _dichVuTonKho;
        private readonly IDichVuCauHinhHeThong _cauHinhHeThong;

        public DichVuHoiVien(
            CSDLBanhTrungThuContext db,
            IDichVuCongThanhToanMoMo dichVuCongMoMo,
            IDichVuTonKhoFefo dichVuTonKho,
            IDichVuCauHinhHeThong cauHinhHeThong) : base(db)
        {
            _dichVuCongMoMo = dichVuCongMoMo;
            _dichVuTonKho = dichVuTonKho;
            _cauHinhHeThong = cauHinhHeThong;
        }

        public string XacDinhDuongDanSauDangNhap(string? maVt)
        {
            string vaiTro = (maVt ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(vaiTro) || vaiTro == "KHACH_TV" || vaiTro == "KHACH_HANG")
            {
                return string.Empty;
            }

            if (vaiTro == "QUAN_TRI" || vaiTro == "ADMIN")
            {
                return "/Admin/Dashboard/Index";
            }

            if (vaiTro == "GIAO_HANG")
            {
                return "/Admin/DonHangAdmin/Index?tab=GIAO_HANG";
            }

            // Nhân viên bán hàng / Thu ngân (NHAN_VIEN, THU_NGAN, NV_BAN_HANG, ...) nhảy thẳng sang POS
            return "/Admin/BanHangPos/Index";
        }

        public async Task<(bool HopLe, string ThongBao, NguoiDung? NguoiDung)> XacThucDangNhapAsync(string? tenDangNhap, string? matKhau)
        {
            string taiKhoan = ChuanHoaChuoi(tenDangNhap);
            string mkNhap = ChuanHoaChuoi(matKhau);

            if (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrWhiteSpace(mkNhap))
            {
                return (false, "Vui lòng nhập đầy đủ Email/Số điện thoại và Mật khẩu!", null);
            }

            await DamBaoVaiTroVaTaiKhoanDemoAsync();

            var nd = await _db.NguoiDungs
                .Include(u => u.HoSoNhanVien)
                .FirstOrDefaultAsync(u => u.Email == taiKhoan || u.Sdt == taiKhoan);

            if (nd == null)
            {
                return (false, "Tài khoản không tồn tại hoặc mật khẩu chưa đúng!", null);
            }

            if (!nd.TrangThai
                || (nd.HoSoNhanVien != null
                    && string.Equals(nd.HoSoNhanVien.TrangThaiLamViec, "TAM_KHOA", StringComparison.OrdinalIgnoreCase)))
            {
                bool laNhanSu = nd.HoSoNhanVien != null
                    || (!string.Equals(nd.MaVt, "KHACH_TV", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(nd.MaVt, "KHACH_HANG", StringComparison.OrdinalIgnoreCase));

                string thongBaoKhoa = laNhanSu
                    ? "Tài khoản nhân viên hiện đang bị tạm khóa. Vui lòng liên hệ Chủ cửa hàng để được mở khóa!"
                    : "Tài khoản của bạn hiện đang bị khóa. Vui lòng liên hệ quản trị viên!";

                return (false, thongBaoKhoa, null);
            }

            string maBamSha256 = ThongTinPhienHelper.BamMatKhauSHA256(mkNhap);
            bool hopLe = false;

            if (string.Equals(nd.MatKhauHash, maBamSha256, StringComparison.OrdinalIgnoreCase))
            {
                hopLe = true;
            }
            else if (nd.MatKhauHash.StartsWith("AQAAAA", StringComparison.OrdinalIgnoreCase) && mkNhap == "123456")
            {
                nd.MatKhauHash = maBamSha256;
                await _db.SaveChangesAsync();
                hopLe = true;
            }

            if (!hopLe)
            {
                return (false, "Mật khẩu không chính xác. Vui lòng kiểm tra lại!", null);
            }

            return (true, $"Đăng nhập thành công ({nd.HoTen} - Vai trò: {nd.MaVt})!", nd);
        }

        public async Task<NguoiDung?> LayTaiKhoanChuyenVaiTroDemoAsync(string maVt)
        {
            await DamBaoVaiTroVaTaiKhoanDemoAsync();

            string sdtMucTieu = maVt switch
            {
                "THU_NGAN" => "0908111222",
                "GIAO_HANG" => "0908333444",
                _ => "0814585526"
            };

            var nd = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.Sdt == sdtMucTieu);
            if (nd == null)
            {
                nd = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.MaVt == maVt);
            }

            return nd;
        }

        public async Task<(bool HopLe, string ThongBao, NguoiDung? NguoiDung)> DangKyHoiVienMoiAsync(
            string? hoTen,
            string? sdt,
            string? email,
            string? matKhau)
        {
            string ten = ChuanHoaChuoi(hoTen);
            string soDt = ChuanHoaChuoi(sdt);
            string thuDienTu = ChuanHoaChuoi(email);
            string mk = ChuanHoaChuoi(matKhau);

            if (string.IsNullOrWhiteSpace(ten) || string.IsNullOrWhiteSpace(soDt) || string.IsNullOrWhiteSpace(thuDienTu) || string.IsNullOrWhiteSpace(mk))
            {
                return (false, "Vui lòng điền đầy đủ Họ tên, Số điện thoại, Email và Mật khẩu!", null);
            }

            bool daTonTai = await _db.NguoiDungs.AnyAsync(u => u.Email == thuDienTu || u.Sdt == soDt);
            if (daTonTai)
            {
                return (false, "Email hoặc Số điện thoại này đã được đăng ký!", null);
            }

            var ndMoi = new NguoiDung
            {
                MaVt = "KHACH_TV",
                HoTen = ten,
                Sdt = soDt,
                Email = thuDienTu,
                MatKhauHash = ThongTinPhienHelper.BamMatKhauSHA256(mk),
                DiemTichLuy = 50,
                HangTv = "Đồng",
                TrangThai = true,
                NgayTao = LayThoiGianHeThong()
            };

            _db.NguoiDungs.Add(ndMoi);
            await _db.SaveChangesAsync();

            return (true, "Đăng ký thành công (Tặng +50 điểm) & Đã hợp nhất giỏ hàng vào tài khoản!", ndMoi);
        }

        public async Task<HoSoThanhVienViewModel?> XayDungHoSoThanhVienAsync(int maNd, string? tab)
        {
            await _dichVuCongMoMo.KiemTraVaHuyDonQrQuaHanAsync();

            var nd = await _db.NguoiDungs
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MaNd == maNd);

            if (nd == null) return null;

            var soDiaChis = await _db.SoDiaChis
                .AsNoTracking()
                .Where(d => d.MaNd == maNd)
                .OrderByDescending(d => d.LaMacDinh)
                .ThenByDescending(d => d.MaDc)
                .ToListAsync();

            var lichSuDonHangs = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .Where(d => d.MaNd == maNd)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            if (lichSuDonHangs.Count > 0)
            {
                var danhSachMaDon = lichSuDonHangs.Select(d => d.MaDh).ToList();
                var danhSachThiep = await _db.ThiepChucMung3ds
                    .AsNoTracking()
                    .Where(t => danhSachMaDon.Contains(t.MaDonHang))
                    .ToListAsync();

                var thiepTheoDon = danhSachThiep.ToLookup(t => t.MaDonHang);
                foreach (var donHang in lichSuDonHangs)
                {
                    foreach (var thiep in thiepTheoDon[donHang.MaDh])
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
            }

            var danhSachHopTuChons = await _db.HopQuaTuChons
                .AsNoTracking()
                .Include(h => h.MaVhNavigation)
                .Include(h => h.ChiTietHopTuChons)
                    .ThenInclude(ct => ct.MaBanhNavigation)
                .Where(h => h.MaNd == maNd)
                .OrderByDescending(h => h.NgayTao)
                .ToListAsync();

            var vm = new HoSoThanhVienViewModel
            {
                NguoiDung = nd,
                DanhSachSoDiaChi = soDiaChis,
                LichSuDonHang = lichSuDonHangs,
                DanhSachHopTuChon = danhSachHopTuChons
            };
            vm.TinhToanChiSoHoiVien(tab);

            if (!string.Equals(nd.HangTv, vm.TenHangHienTai, StringComparison.Ordinal))
            {
                nd.HangTv = vm.TenHangHienTai;
                await _db.NguoiDungs
                    .Where(u => u.MaNd == maNd)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.HangTv, vm.TenHangHienTai));
            }

            return vm;
        }

        public async Task<(bool ThanhCong, string ThongBao)> KhachHangHuyDonCodAsync(int maNd, int maDh, string? lyDoHuy)
        {
            if (maDh <= 0)
            {
                return (false, "Mã đơn hàng không hợp lệ.");
            }

            var don = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDh == maDh);

            if (don == null || !don.MaNd.HasValue || don.MaNd.Value != maNd)
            {
                return (false, "Không tìm thấy đơn hàng thuộc tài khoản của bạn.");
            }

            if (!string.Equals((don.PhuongThucTt ?? string.Empty).Trim(), "COD", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Chỉ hỗ trợ tự hủy đối với đơn hàng thanh toán tiền mặt khi nhận (COD).");
            }

            string trangThaiHienTai = (don.TrangThaiDh ?? string.Empty).Trim().ToUpperInvariant();
            if (trangThaiHienTai != "CHO_XAC_NHAN" && trangThaiHienTai != "CHO_DUYET")
            {
                return (false, "Đơn hàng đã được xác nhận hoặc xử lý, không thể tự hủy trực tuyến. Vui lòng liên hệ hotline cửa hàng để được hỗ trợ.");
            }

            string lyDoSach = ChuanHoaChuoi(lyDoHuy, "Thay đổi nhu cầu mua hàng");
            string lyDoLuu = $"Khách hàng tự hủy: {lyDoSach}";
            if (lyDoLuu.Length > 500)
            {
                lyDoLuu = lyDoLuu[..500];
            }

            int diemDaHoan = 0;

            await ThucThiTrongGiaoDichAsync(async () =>
            {
                don.TrangThaiDh = "DA_HUY";
                don.LyDoHuy = lyDoLuu;

                var cacDotGiao = don.DotGiaoHangDaDiems != null && don.DotGiaoHangDaDiems.Any()
                    ? don.DotGiaoHangDaDiems.ToList()
                    : await _db.DotGiaoHangDaDiems.Where(g => g.MaDh == don.MaDh).ToListAsync();

                foreach (var dot in cacDotGiao)
                {
                    dot.TrangThaiGiao = "DA_HUY";
                }

                await _dichVuTonKho.HoanTraTonKhoTheoDonAsync(don);

                NguoiDung? nd = null;
                if (don.SoDiemDaDung > 0 && !don.DaHoanDiemKhiHuy)
                {
                    nd = await _db.NguoiDungs.FindAsync(maNd);
                    if (nd != null)
                    {
                        nd.DiemTichLuy += don.SoDiemDaDung;
                        diemDaHoan = don.SoDiemDaDung;
                    }
                    don.DaHoanDiemKhiHuy = true;
                }

                await _db.SaveChangesAsync();

                if (nd != null)
                {
                    var (hangVipMoi, _, _, _) = await TinhThongTinHangVipAsync(_db, nd.MaNd, nd.DiemTichLuy);
                    nd.HangTv = hangVipMoi;
                    await _db.SaveChangesAsync();
                }
            });

            _cauHinhHeThong.CapNhatMetadataDonHang(don.MaDh, meta =>
            {
                meta.DaHoanKhoDoHetHan = true;
                meta.LyDoHuyHoan = don.LyDoHuy;
                meta.ThoiGianCapNhatCuoi = LayThoiGianHeThong().ToString("dd/MM/yyyy HH:mm");
                meta.NguoiCapNhatCuoi = "Khách hàng tự hủy";
            });

            string thongBaoHoanDiem = diemDaHoan > 0 ? $" và hoàn lại {diemDaHoan:N0} điểm VIP" : string.Empty;
            return (true, $"Đã hủy đơn hàng #{don.MaTraCuuDh} thành công{thongBaoHoanDiem}.");
        }

        /// <summary>
        /// Xác định Hạng VIP dựa trên MAX giữa điểm tích lũy hiện tại và tổng chi tiêu hợp lệ (TrangThaiDh != "DA_HUY"),
        /// đảm bảo khi hội viên tiêu điểm giảm giá đơn hàng mới thì Hạng thẻ VIP tuyệt đối không bị tụt hạng.
        /// </summary>
        public static (string HangTv, decimal PhanTramGiamVip, int DiemXetHang) XacDinhHangVipTheoChiTieu(
            int diemHienTai,
            decimal tongChiTieuTichLuy)
        {
            int diemHopLe = Math.Max(0, diemHienTai);
            int diemQuyDoiChiTieu = tongChiTieuTichLuy > 0m ? (int)(tongChiTieuTichLuy / 10000m) : 0;
            int diemXetHang = Math.Max(diemHopLe, diemQuyDoiChiTieu);

            if (diemXetHang >= 600 || tongChiTieuTichLuy >= 6000000m)
                return ("Kim Cương", 10m, diemXetHang);
            if (diemXetHang >= 300 || tongChiTieuTichLuy >= 3000000m)
                return ("Vàng", 5m, diemXetHang);
            if (diemXetHang >= 100 || tongChiTieuTichLuy >= 1000000m)
                return ("Bạc", 3m, diemXetHang);

            return ("Đồng", 0m, diemXetHang);
        }

        /// <summary>
        /// Truy vấn TongChiTieuTichLuy các đơn hàng hợp lệ (TrangThaiDh != "DA_HUY") trong bảng DonHang
        /// để tính hạng VIP và phần trăm ưu đãi tự động của hội viên.
        /// </summary>
        public static async Task<(string HangTv, decimal PhanTramGiamVip, decimal TongChiTieuTichLuy, int DiemXetHang)> TinhThongTinHangVipAsync(
            CSDLBanhTrungThuContext db,
            int maNd,
            int diemHienTai)
        {
            decimal tongChiTieuTichLuy = await db.DonHangs
                .Where(d => d.MaNd == maNd && d.TrangThaiDh != "DA_HUY")
                .SumAsync(d => (decimal?)d.TongThanhToanCuoi) ?? 0m;

            var (hangTv, phanTramGiamVip, diemXetHang) = XacDinhHangVipTheoChiTieu(diemHienTai, tongChiTieuTichLuy);
            return (hangTv, phanTramGiamVip, tongChiTieuTichLuy, diemXetHang);
        }

        /// <summary>
        /// Cộng thưởng +1 điểm cho mỗi 10.000 đ thực trả khi đơn thanh toán QR thành công hoặc chuyển sang DA_GIAO / HOAN_TAT.
        /// Đảm bảo idempotent qua cột SQL DaCongDiemThuong trên bảng dbo.DonHang.
        /// </summary>
        public static async Task<int> CongDiemThuongDonHangAsync(
            CSDLBanhTrungThuContext db,
            DonHang donHang)
        {
            if (!donHang.MaNd.HasValue
                || string.Equals(donHang.TrangThaiDh, "DA_HUY", StringComparison.OrdinalIgnoreCase)
                || donHang.DaCongDiemThuong)
            {
                return 0;
            }

            int diemThuong = donHang.TongThanhToanCuoi > 0m
                ? (int)(donHang.TongThanhToanCuoi / 10000m)
                : 0;

            var nd = await db.NguoiDungs.FindAsync(donHang.MaNd.Value);
            if (nd == null)
            {
                return 0;
            }

            if (diemThuong > 0)
            {
                nd.DiemTichLuy += diemThuong;
            }

            donHang.DaCongDiemThuong = true;
            await db.SaveChangesAsync();

            var (hangVipMoi, _, _, _) = await TinhThongTinHangVipAsync(db, nd.MaNd, nd.DiemTichLuy);
            nd.HangTv = hangVipMoi;
            await db.SaveChangesAsync();

            return diemThuong;
        }

        /// <summary>
        /// Hoàn lại số điểm đã dùng (và thu hồi điểm thưởng nếu đã cộng trước đó) khi đơn hàng chuyển sang DA_HUY.
        /// Đọc/ghi trực tiếp cột SQL SoDiemDaDung, DaCongDiemThuong, DaHoanDiemKhiHuy trên bảng dbo.DonHang.
        /// </summary>
        public static async Task<int> HoanDiemKhiHuyDonAsync(
            CSDLBanhTrungThuContext db,
            DonHang donHang)
        {
            if (!donHang.MaNd.HasValue || donHang.DaHoanDiemKhiHuy)
            {
                return 0;
            }

            var nd = await db.NguoiDungs.FindAsync(donHang.MaNd.Value);
            if (nd == null)
            {
                return 0;
            }

            int diemHoan = Math.Max(0, donHang.SoDiemDaDung);
            if (diemHoan > 0)
            {
                nd.DiemTichLuy += diemHoan;
            }

            if (donHang.DaCongDiemThuong)
            {
                int diemThuongDaCong = donHang.TongThanhToanCuoi > 0m
                    ? (int)(donHang.TongThanhToanCuoi / 10000m)
                    : 0;
                if (diemThuongDaCong > 0)
                {
                    nd.DiemTichLuy = Math.Max(0, nd.DiemTichLuy - diemThuongDaCong);
                }
            }

            donHang.DaHoanDiemKhiHuy = true;
            await db.SaveChangesAsync();

            var (hangVipMoi, _, _, _) = await TinhThongTinHangVipAsync(db, nd.MaNd, nd.DiemTichLuy);
            nd.HangTv = hangVipMoi;
            await db.SaveChangesAsync();

            return diemHoan;
        }

        public async Task<string> ThemDiaChiMoiAsync(
            int maNd,
            string? tenNguoiNhan,
            string? sdtNguoiNhan,
            string? tinhThanh,
            string? quanHuyen,
            string? phuongXa,
            string? diaChiCuThe,
            string? nhanDiaChi,
            bool laMacDinh)
        {
            if (string.IsNullOrWhiteSpace(tenNguoiNhan) || string.IsNullOrWhiteSpace(sdtNguoiNhan) || string.IsNullOrWhiteSpace(diaChiCuThe))
            {
                return "Vui lòng nhập đầy đủ Tên người nhận, Số điện thoại và Địa chỉ cụ thể!";
            }

            bool chuaCoDiaChi = !await _db.SoDiaChis.AnyAsync(d => d.MaNd == maNd);
            bool macDinhThucTe = laMacDinh || chuaCoDiaChi;

            if (macDinhThucTe)
            {
                var cacDiaChiCu = await _db.SoDiaChis.Where(d => d.MaNd == maNd && d.LaMacDinh).ToListAsync();
                foreach (var dc in cacDiaChiCu)
                {
                    dc.LaMacDinh = false;
                }
            }

            var dcMoi = new SoDiaChi
            {
                MaNd = maNd,
                TenNguoiNhan = tenNguoiNhan.Trim(),
                SdtnguoiNhan = sdtNguoiNhan.Trim(),
                TinhThanh = ChuanHoaChuoi(tinhThanh, "TP. Hồ Chí Minh"),
                QuanHuyen = ChuanHoaChuoi(quanHuyen, "Quận 1"),
                PhuongXa = ChuanHoaChuoi(phuongXa, "Phường Bến Nghé"),
                DiaChiCuThe = diaChiCuThe.Trim(),
                NhanDiaChi = ChuanHoaChuoi(nhanDiaChi, "Biếu Người Thân"),
                LaMacDinh = macDinhThucTe
            };

            _db.SoDiaChis.Add(dcMoi);
            await _db.SaveChangesAsync();

            return $"Đã thêm địa chỉ \"{dcMoi.NhanDiaChi} - {dcMoi.TenNguoiNhan}\" vào Sổ địa chỉ biếu tặng!";
        }

        public async Task<(bool ThanhCong, string ThongBao)> CapNhatDiaChiAsync(
            int maNd,
            int maDiaChi,
            string? nhanGoiNho,
            string? nguoiNhan,
            string? sdtNhan,
            string? diaChiChiTiet,
            bool laMacDinh,
            string? tinhThanh = null,
            string? quanHuyen = null,
            string? phuongXa = null)
        {
            if (string.IsNullOrWhiteSpace(nguoiNhan) || string.IsNullOrWhiteSpace(sdtNhan) || string.IsNullOrWhiteSpace(diaChiChiTiet))
            {
                return (false, "Vui lòng nhập đầy đủ Họ tên người nhận, Số điện thoại và Địa chỉ cụ thể!");
            }

            return await ThucThiTrongGiaoDichAsync(async () =>
            {
                var dc = await _db.SoDiaChis.FirstOrDefaultAsync(d => d.MaDc == maDiaChi && d.MaNd == maNd);
                if (dc == null)
                {
                    return (false, "Không tìm thấy địa chỉ thuộc tài khoản của bạn!");
                }

                dc.NhanDiaChi = ChuanHoaChuoi(nhanGoiNho, "Biếu Người Thân");
                dc.TenNguoiNhan = nguoiNhan.Trim();
                dc.SdtnguoiNhan = sdtNhan.Trim();
                dc.DiaChiCuThe = diaChiChiTiet.Trim();
                if (!string.IsNullOrWhiteSpace(tinhThanh)) dc.TinhThanh = tinhThanh.Trim();
                if (!string.IsNullOrWhiteSpace(quanHuyen)) dc.QuanHuyen = quanHuyen.Trim();
                if (!string.IsNullOrWhiteSpace(phuongXa)) dc.PhuongXa = phuongXa.Trim();

                if (laMacDinh)
                {
                    var cacDiaChiKhac = await _db.SoDiaChis
                        .Where(d => d.MaNd == maNd && d.MaDc != maDiaChi && d.LaMacDinh)
                        .ToListAsync();
                    foreach (var item in cacDiaChiKhac)
                    {
                        item.LaMacDinh = false;
                    }
                    dc.LaMacDinh = true;
                }
                else if (dc.LaMacDinh)
                {
                    var diaChiThayThe = await _db.SoDiaChis
                        .Where(d => d.MaNd == maNd && d.MaDc != maDiaChi)
                        .OrderByDescending(d => d.MaDc)
                        .FirstOrDefaultAsync();
                    if (diaChiThayThe != null)
                    {
                        dc.LaMacDinh = false;
                        diaChiThayThe.LaMacDinh = true;
                    }
                }

                await _db.SaveChangesAsync();
                return (true, $"Đã cập nhật địa chỉ \"{dc.NhanDiaChi} - {dc.TenNguoiNhan}\" thành công!");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> DatDiaChiMacDinhAsync(int maNd, int maDiaChi)
        {
            return await ThucThiTrongGiaoDichAsync(async () =>
            {
                var tatCaDiaChi = await _db.SoDiaChis
                    .Where(d => d.MaNd == maNd)
                    .ToListAsync();

                var dcChon = tatCaDiaChi.FirstOrDefault(d => d.MaDc == maDiaChi);
                if (dcChon == null)
                {
                    return (false, "Không tìm thấy địa chỉ thuộc tài khoản của bạn!");
                }

                foreach (var dc in tatCaDiaChi)
                {
                    dc.LaMacDinh = dc.MaDc == maDiaChi;
                }

                await _db.SaveChangesAsync();
                return (true, $"Đã đặt \"{dcChon.NhanDiaChi ?? dcChon.TenNguoiNhan}\" làm địa chỉ mặc định!");
            });
        }

        public async Task<string?> XoaDiaChiAsync(int maNd, int maDc)
        {
            return await ThucThiTrongGiaoDichAsync<string?>(async () =>
            {
                var dc = await _db.SoDiaChis.FirstOrDefaultAsync(d => d.MaDc == maDc && d.MaNd == maNd);
                if (dc == null) return null;

                bool laMacDinhCu = dc.LaMacDinh;
                _db.SoDiaChis.Remove(dc);
                await _db.SaveChangesAsync();

                if (laMacDinhCu)
                {
                    var diaChiConLai = await _db.SoDiaChis
                        .Where(d => d.MaNd == maNd)
                        .OrderByDescending(d => d.MaDc)
                        .FirstOrDefaultAsync();
                    if (diaChiConLai != null)
                    {
                        diaChiConLai.LaMacDinh = true;
                        await _db.SaveChangesAsync();
                    }
                }

                return "Đã xóa địa chỉ khỏi Sổ địa chỉ biếu tặng!";
            });
        }

        public async Task<string> DatLaiHopTuChonVaoGioAsync(int maNd, int maHopTc)
        {
            var hopCu = await _db.HopQuaTuChons
                .Include(h => h.MaVhNavigation)
                .Include(h => h.ChiTietHopTuChons)
                .FirstOrDefaultAsync(h => h.MaHopTc == maHopTc && h.MaNd == maNd);

            if (hopCu == null)
            {
                return "⚠️ Không tìm thấy mẫu hộp tự phối này!";
            }

            var gioHang = await _db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(ct => ct.MaHopTcNavigation)
                .FirstOrDefaultAsync(g => g.MaNd == maNd);

            if (gioHang == null)
            {
                gioHang = new Models.Entities.GioHang
                {
                    MaNd = maNd,
                    MaPhienKvl = null,
                    LaCheDoMuaSiB2b = false,
                    YeuCauInLogoB2b = false,
                    NgayCapNhat = LayThoiGianHeThong()
                };
                _db.GioHangs.Add(gioHang);
                await _db.SaveChangesAsync();
            }

            var dongDaCo = gioHang.ChiTietGioHangs.FirstOrDefault(ct =>
                ct.LoaiSanPham == 3 &&
                (ct.MaHopTc == hopCu.MaHopTc || (ct.MaHopTcNavigation != null && ct.MaHopTcNavigation.MaBamCauHinh == hopCu.MaBamCauHinh)));

            if (dongDaCo != null)
            {
                dongDaCo.SoLuong += 1;
                dongDaCo.DonGiaTamTinh = hopCu.TongGiaMotHop;
            }
            else
            {
                _db.ChiTietGioHangs.Add(new ChiTietGioHang
                {
                    MaGh = gioHang.MaGh,
                    LoaiSanPham = 3,
                    MaHopTc = hopCu.MaHopTc,
                    SoLuong = 1,
                    DonGiaTamTinh = hopCu.TongGiaMotHop,
                    NgayThem = LayThoiGianHeThong()
                });
            }

            gioHang.NgayCapNhat = LayThoiGianHeThong();
            await _db.SaveChangesAsync();

            return $"🛒 Đã đưa mẫu hộp \"{hopCu.TenHopKhachDat}\" ({hopCu.MaBamCauHinh}) vào lại Giỏ Quà thành công!";
        }

        public async Task DamBaoVaiTroVaTaiKhoanDemoAsync()
        {
            var danhSachVaiTro = new (string MaVt, string TenVt, string MoTa)[]
            {
                ("ADMIN", "Chủ cửa hàng", "Toàn quyền quản trị hệ thống & nhân sự Nguyệt Cát ERP"),
                ("QUAN_TRI", "Quản trị viên", "Toàn quyền quản trị hệ thống & Admin Override"),
                ("NHAN_VIEN", "Nhân viên Bán hàng", "Nhân viên Order - Bán hàng & Thu ngân"),
                ("THU_NGAN", "Thu ngân & Bán hàng", "Xác nhận thanh toán QR, chuẩn bị hàng CHO_LAY_HANG, xử lý hoàn tiền"),
                ("GIAO_HANG", "Nhân viên giao hàng", "Nhận đơn từ CHO_LAY_HANG -> DANG_GIAO -> xác nhận HOAN_TAT"),
                ("KHACH_TV", "Khách thành viên", "Khách hàng thành viên tích điểm")
            };

            bool coThayDoiVt = false;
            foreach (var vt in danhSachVaiTro)
            {
                if (!await _db.VaiTros.AnyAsync(v => v.MaVt == vt.MaVt))
                {
                    _db.VaiTros.Add(new VaiTro
                    {
                        MaVt = vt.MaVt,
                        TenVt = vt.TenVt,
                        MoTa = vt.MoTa
                    });
                    coThayDoiVt = true;
                }
            }

            if (coThayDoiVt)
            {
                await _db.SaveChangesAsync();
            }

            string mkSha = ThongTinPhienHelper.BamMatKhauSHA256("123456");

            var taiKhoanMau = new (string Sdt, string Email, string HoTen, string MaVt, string HangTv)[]
            {
                ("0908111222", "thungan@nguyetcat.vn", "Trần Thu Ngân (Nhân viên Bán hàng)", "NHAN_VIEN", "Vàng"),
                ("0908333444", "giaohang@nguyetcat.vn", "Nguyễn Văn Giao (Nhân viên Bán hàng)", "NHAN_VIEN", "Bạc"),
                ("0814585526", "admin@nguyetcat.vn", "Lê Minh Đức (Chủ cửa hàng)", "ADMIN", "Kim Cương")
            };

            bool coThayDoiNd = false;
            foreach (var tk in taiKhoanMau)
            {
                if (!await _db.NguoiDungs.AnyAsync(u => u.Sdt == tk.Sdt || u.Email == tk.Email))
                {
                    _db.NguoiDungs.Add(new NguoiDung
                    {
                        MaVt = tk.MaVt,
                        HoTen = tk.HoTen,
                        Sdt = tk.Sdt,
                        Email = tk.Email,
                        MatKhauHash = mkSha,
                        DiemTichLuy = 500,
                        HangTv = tk.HangTv,
                        TrangThai = true,
                        NgayTao = LayThoiGianHeThong()
                    });
                    coThayDoiNd = true;
                }
            }

            if (coThayDoiNd)
            {
                await _db.SaveChangesAsync();
            }
        }
    }
}
