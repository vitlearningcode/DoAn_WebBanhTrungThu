using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.GiaoDien
{
    /// <summary>
    /// Kết quả chấm điểm gợi ý bánh thông minh 30 giây cho Khách hàng.
    /// </summary>
    public class GoiYBanhKetQua
    {
        public int MaBanh { get; set; }
        public string TenBanh { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int Calo { get; set; }

        [JsonPropertyName("chiSoGI")]
        public int ChiSoGI { get; set; }

        public string AnhBanh { get; set; } = string.Empty;
        public double DoPhuHop { get; set; }
        public string LyDo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dịch vụ chuyên trách dữ liệu Trang chủ (Storefront Landing Page), thuật toán gợi ý bánh theo gu
    /// và tra cứu đơn hàng nhanh cho Khách vãng lai. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuTrangChu : DichVuCoSo, IDichVuTrangChu
    {
        private readonly IDichVuHinhAnh _dichVuHinhAnh;
        private readonly IDichVuCauHinhHeThong _kiotMeta;

        public DichVuTrangChu(
            CSDLBanhTrungThuContext db,
            IDichVuHinhAnh dichVuHinhAnh,
            IDichVuCauHinhHeThong kiotMeta) : base(db)
        {
            _dichVuHinhAnh = dichVuHinhAnh;
            _kiotMeta = kiotMeta;
        }

        public async Task<LandingPageViewModel> XayDungDuLieuTrangChuAsync(int? maND, string? maPhienKVL)
        {
            var vm = new LandingPageViewModel();

            // 1. Lấy từ điển ảnh đa nguồn (WebRoot, Ổ C:\, Google Drive)
            vm.TuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();

            // 2. Lấy cấu hình Landing Page
            var cauHinhs = await _db.CauHinhLandingPages
                .AsNoTracking()
                .Include(c => c.MaAnhNenNavigation)
                .Include(c => c.MaAnhPhuParallaxNavigation)
                .Where(c => c.DangKichHoat)
                .OrderBy(c => c.ThuTuHienThi)
                .ThenBy(c => c.MaPhanKhu)
                .ToListAsync();

            vm.DanhSachBanner = cauHinhs;
            vm.HeroBanner = cauHinhs.FirstOrDefault(c => c.MaCodeKhuVuc == "HERO_BANNER" || c.MaCodeKhuVuc == "TANG_1_HERO")
                ?? cauHinhs.FirstOrDefault(c => c.ThuTuHienThi == 1);
            vm.BoxBuilderIntro = cauHinhs.FirstOrDefault(c => c.MaCodeKhuVuc == "BOX_BUILDER_INTRO" || c.MaCodeKhuVuc == "TANG_2_STUDIO")
                ?? cauHinhs.FirstOrDefault(c => c.ThuTuHienThi == 2);
            vm.BoSuuTapIntro = cauHinhs.FirstOrDefault(c => c.MaCodeKhuVuc == "TANG_3_BOSUUTAP")
                ?? cauHinhs.FirstOrDefault(c => c.ThuTuHienThi == 3);
            vm.ChietKhauB2BIntro = cauHinhs.FirstOrDefault(c => c.MaCodeKhuVuc == "TANG_4_CHIETKHAU")
                ?? cauHinhs.FirstOrDefault(c => c.ThuTuHienThi == 4);
            vm.ThiepQrIntro = cauHinhs.FirstOrDefault(c => c.MaCodeKhuVuc == "TANG_5_THIEPQR")
                ?? cauHinhs.FirstOrDefault(c => c.ThuTuHienThi == 5);

            // 3. Lấy Danh mục, Bánh lẻ (kèm Thông số dinh dưỡng 1-1), Vỏ hộp & Combo mẫu
            vm.DanhMucs = await _db.DanhMucBanhs.Where(d => d.TrangThai).OrderBy(d => d.ThuTuSx).ToListAsync();

            vm.DanhSachBanh = await _db.SanPhamBanhs
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .Include(b => b.MaDmNavigation)
                .Include(b => b.MaThuongHieuNavigation)
                .Include(b => b.MaNccMacDinhNavigation)
                .Where(b => b.TrangThaiBan)
                .ToListAsync();

            vm.DanhSachVoHop = await _db.MauVoHops.Where(v => v.TrangThai).ToListAsync();

            vm.DanhSachCombo = await _db.ComboMauCoSans
                .Include(cb => cb.MaVhNavigation)
                .Include(cb => cb.MaThuongHieuNavigation)
                .Include(cb => cb.MaDmNavigation)
                .Include(cb => cb.ChiTietComboMaus)
                    .ThenInclude(ct => ct.MaBanhNavigation)
                .Where(cb => cb.TrangThai)
                .ToListAsync();

            // Lấy danh sách Thương hiệu trực tiếp từ dbo.ThuongHieu (SQL Server)
            vm.DanhSachThuongHieu = await _db.ThuongHieus
                .AsNoTracking()
                .Where(t => t.DangHoatDong)
                .OrderBy(t => t.MaThuongHieu)
                .Select(t => t.TenThuongHieu)
                .ToListAsync();
            if (vm.DanhSachThuongHieu.Count == 0)
            {
                vm.DanhSachThuongHieu = vm.DanhSachBanh
                    .Select(b => b.ThuongHieu)
                    .Where(th => !string.IsNullOrWhiteSpace(th))
                    .Distinct()
                    .ToList();
            }

            // 4. Lấy bảng chiết khấu B2B và Mã khuyến mãi
            var thoiDiemHienTai = LayThoiGianHeThong();
            vm.BangGiaSiB2B = await _db.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
            vm.DanhSachKhuyenMai = await _db.MaKhuyenMais
                .Where(k => k.TrangThaiKichHoat && k.NgayKetThuc >= thoiDiemHienTai)
                .ToListAsync();

            // 5. Xác định Phiên (Guest vs Member) và nạp Giỏ hàng hiện tại
            vm.DaDangNhap = maND.HasValue;
            vm.MaPhienHienTai = maND.HasValue ? $"MEMBER #{maND}" : (maPhienKVL ?? "GUEST");

            if (maND.HasValue)
            {
                var nd = await _db.NguoiDungs.FindAsync(maND.Value);
                vm.HoTenHienThi = nd?.HoTen ?? "Thành viên";
                vm.GioHangHienTai = await TaiGioHangTheoPhienAsync(maND.Value, null);
            }
            else if (!string.IsNullOrEmpty(maPhienKVL))
            {
                vm.GioHangHienTai = await TaiGioHangTheoPhienAsync(null, maPhienKVL);
            }

            if (vm.GioHangHienTai != null && vm.GioHangHienTai.ChiTietGioHangs.Any())
            {
                vm.TongSoLuongTrongGio = vm.GioHangHienTai.ChiTietGioHangs.Sum(c => c.SoLuong);
                vm.TongTamTinhGioHang = vm.GioHangHienTai.ChiTietGioHangs.Sum(c => c.SoLuong * c.DonGiaTamTinh);
            }

            return vm;
        }

        private async Task<WebBanhTrungThu.Models.Entities.GioHang?> TaiGioHangTheoPhienAsync(int? maND, string? maPhienKVL)
        {
            var query = _db.GioHangs
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation)
                    .ThenInclude(h => h!.ChiTietHopTuChons).ThenInclude(cth => cth.MaBanhNavigation);

            return maND.HasValue
                ? await query.FirstOrDefaultAsync(g => g.MaNd == maND.Value)
                : await query.FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKVL);
        }

        // =================================================================================
        // THUẬT TOÁN TRỢ LÝ CHỌN BÁNH 30 GIÂY (WEIGHTED SCORING MATRIX S(P))
        // =================================================================================
        public async Task<List<GoiYBanhKetQua>> GoiYBanhThongMinhAsync(string? doiTuong, string? sucKhoe, string? huongVi, decimal nganSachMax)
        {
            var dsBanh = await _db.SanPhamBanhs
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .Where(b => b.TrangThaiBan)
                .ToListAsync();

            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();
            var ketQuaChamDiem = new List<GoiYBanhKetQua>();

            foreach (var b in dsBanh)
            {
                double diemSucKhoe = 0; // W1 = 35
                double diemNganSach = 0; // W2 = 25
                double diemDoiTuong = 0; // W3 = 20
                double diemHuongVi = 0;  // W4 = 20
                double diemPhat = 0;     // P_health

                int gi = b.ThongSoDinhDuong?.ChiSoDuongHuyetGi ?? 55;
                var lyDoPhuHop = new List<string>();

                // 1. Chấm điểm Sức khỏe (W1 = 35)
                if (sucKhoe == "TIEU_DUONG")
                {
                    if (b.MucDoNgot == "ISOMALT_TIEUDUONG" && gi <= 45)
                    {
                        diemSucKhoe = 35;
                        lyDoPhuHop.Add($"Đường ăn kiêng Isomalt (Chỉ số GI thấp: {gi})");
                    }
                    else if (b.MucDoNgot == "IT_NGOT")
                    {
                        diemSucKhoe = 21;
                    }
                    else
                    {
                        diemPhat = 500;
                    }
                }
                else if (sucKhoe == "AN_CHAY")
                {
                    if (b.LaBanhChay)
                    {
                        diemSucKhoe = 35;
                        lyDoPhuHop.Add("Thuần chay 100% thực vật");
                    }
                    else
                    {
                        diemPhat = 500;
                    }
                }
                else
                {
                    diemSucKhoe = 30;
                }

                // 2. Chấm điểm Ngân sách (W2 = 25)
                decimal giaUocTinhHop = b.DonGiaLe * 4;
                if (giaUocTinhHop <= nganSachMax)
                {
                    diemNganSach = 25;
                    lyDoPhuHop.Add("Nằm gọn trong ngân sách dự kiến");
                }
                else
                {
                    diemNganSach = 15;
                }

                // 3. Chấm điểm Đối tượng tặng (W3 = 20)
                if (!string.IsNullOrEmpty(b.TheDoiTuongPhuHop) && b.TheDoiTuongPhuHop.Contains(doiTuong ?? ""))
                {
                    diemDoiTuong = 20;
                    lyDoPhuHop.Add("Thiết kế hương vị chuẩn gu người nhận");
                }
                else
                {
                    diemDoiTuong = 8;
                }

                // 4. Chấm điểm Gu Hương vị (W4 = 20)
                if (b.NhomHuongVi == huongVi)
                {
                    diemHuongVi = 20;
                    lyDoPhuHop.Add("Đúng dòng nhân yêu thích");
                }
                else
                {
                    diemHuongVi = 10;
                }

                double tongDiem = diemSucKhoe + diemNganSach + diemDoiTuong + diemHuongVi - diemPhat;
                if (tongDiem > 0)
                {
                    ketQuaChamDiem.Add(new GoiYBanhKetQua
                    {
                        MaBanh = b.MaBanh,
                        TenBanh = b.TenBanh,
                        DonGia = b.DonGiaThucTe,
                        Calo = b.ThongSoDinhDuong?.NangLuongCalo ?? 400,
                        ChiSoGI = gi,
                        AnhBanh = DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", b.MaBanh, "ANH_CHINH"),
                        DoPhuHop = Math.Min(99, Math.Round(tongDiem)),
                        LyDo = string.Join(" • ", lyDoPhuHop)
                    });
                }
            }

            return ketQuaChamDiem.OrderByDescending(x => x.DoPhuHop).Take(3).ToList();
        }

        // =================================================================================
        // TRA CỨU ĐƠN HÀNG NHANH CHO KHÁCH VÃNG LAI (GUEST TRACKING)
        // =================================================================================
        public async Task<object> TraCuuDonHangNhanhAsync(string? maTraCuu, string? sdt)
        {
            if (string.IsNullOrWhiteSpace(maTraCuu) || string.IsNullOrWhiteSpace(sdt))
            {
                return new { thanhCong = false, thongBao = "Vui lòng nhập đầy đủ Mã đơn và Số điện thoại!" };
            }

            string maChuan = ChuanHoaChuoi(maTraCuu);
            string sdtChuan = ChuanHoaChuoi(sdt);

            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maChuan && d.SdtnguoiMua == sdtChuan);

            if (donHang == null)
            {
                return new { thanhCong = false, thongBao = "Không tìm thấy đơn hàng khớp với thông tin trên!" };
            }

            return new
            {
                thanhCong = true,
                maDon = donHang.MaTraCuuDh,
                nguoiMua = donHang.HoTenNguoiMua,
                ngayDat = donHang.NgayDat.ToString("dd/MM/yyyy HH:mm"),
                tongTien = donHang.TongThanhToanCuoi.ToString("N0") + " đ",
                trangThai = donHang.TenTrangThaiDhHienThi,
                soDiemGiao = donHang.DotGiaoHangDaDiems.Count
            };
        }
    }
}
