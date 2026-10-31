using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    public class HomeController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuHinhAnh _dichVuHinhAnh;

        public HomeController(CSDLBanhTrungThuContext db, DichVuHinhAnh dichVuHinhAnh)
        {
            _db = db;
            _dichVuHinhAnh = dichVuHinhAnh;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new LandingPageViewModel();

            // 1. Lấy từ điển ảnh đa nguồn (WebRoot, Ổ C:\, Google Drive)
            vm.TuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();

            // 2. Lấy cấu hình Landing Page
            var cauHinhs = await _db.CauHinhLandingPages
                .Include(c => c.MaAnhNenNavigation)
                .Where(c => c.DangKichHoat)
                .ToListAsync();

            vm.HeroBanner = cauHinhs.FirstOrDefault(c => c.MaCodeKhuVuc == "HERO_BANNER");
            vm.BoxBuilderIntro = cauHinhs.FirstOrDefault(c => c.MaCodeKhuVuc == "BOX_BUILDER_INTRO");

            // 3. Lấy Danh mục, Bánh lẻ (kèm Thông số dinh dưỡng 1-1), Vỏ hộp & Combo mẫu
            vm.DanhMucs = await _db.DanhMucBanhs.Where(d => d.TrangThai).OrderBy(d => d.ThuTuSx).ToListAsync();

            vm.DanhSachBanh = await _db.SanPhamBanhs
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.MaDmNavigation)
                .Where(b => b.TrangThaiBan)
                .ToListAsync();

            vm.DanhSachVoHop = await _db.MauVoHops.Where(v => v.TrangThai).ToListAsync();

            vm.DanhSachCombo = await _db.ComboMauCoSans
                .Include(cb => cb.MaVhNavigation)
                .Include(cb => cb.ChiTietComboMaus)
                    .ThenInclude(ct => ct.MaBanhNavigation)
                .Where(cb => cb.TrangThai)
                .ToListAsync();

            // 4. Lấy bảng chiết khấu B2B và Mã khuyến mãi
            vm.BangGiaSiB2B = await _db.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
            vm.DanhSachKhuyenMai = await _db.MaKhuyenMais
                .Where(k => k.NgayKetThuc >= DateTime.Now)
                .ToListAsync();

            // 5. Xác định Phiên (Guest vs Member) và nạp Giỏ hàng hiện tại
            int? maND = ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
            string? maPhienKVL = ThongTinPhienHelper.LayMaPhienKVL(HttpContext);

            vm.DaDangNhap = maND.HasValue;
            vm.MaPhienHienTai = maND.HasValue ? $"MEMBER #{maND}" : (maPhienKVL ?? "GUEST");

            if (maND.HasValue)
            {
                var nd = await _db.NguoiDungs.FindAsync(maND.Value);
                vm.HoTenHienThi = nd?.HoTen ?? "Thành viên";
                vm.GioHangHienTai = await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation)
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation)
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation)
                    .FirstOrDefaultAsync(g => g.MaNd == maND.Value);
            }
            else if (!string.IsNullOrEmpty(maPhienKVL))
            {
                vm.GioHangHienTai = await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation)
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation)
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation)
                    .FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKVL);
            }

            if (vm.GioHangHienTai != null && vm.GioHangHienTai.ChiTietGioHangs.Any())
            {
                vm.TongSoLuongTrongGio = vm.GioHangHienTai.ChiTietGioHangs.Sum(c => c.SoLuong);
                vm.TongTamTinhGioHang = vm.GioHangHienTai.ChiTietGioHangs.Sum(c => c.SoLuong * c.DonGiaTamTinh);
            }

            return View(vm);
        }

        // =================================================================================
        // API 1: THUẬT TOÁN TRỢ LÝ CHỌN BÁNH 30 GIÂY (WEIGHTED SCORING MATRIX S(P))
        // =================================================================================
        [HttpGet]
        public async Task<IActionResult> GoiYBanhThongMinh(string doiTuong, string sucKhoe, string huongVi, decimal nganSachMax)
        {
            var dsBanh = await _db.SanPhamBanhs.Include(b => b.ThongSoDinhDuong).Where(b => b.TrangThaiBan).ToListAsync();
            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();

            var ketQuaChamDiem = new List<object>();

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
                        diemPhat = 500; // Loại trừ bánh nhiều đường cho người tiểu đường
                    }
                }
                else if (sucKhoe == "AN_CHAY")
                {
                    if (b.LaBanhChay)
                    {
                        diemSucKhoe = 35;
                        lyDoPhuHop.Add("Thuần chay 100% thực vật");
                    }
                    else diemPhat = 500;
                }
                else
                {
                    diemSucKhoe = 30;
                }

                // 2. Chấm điểm Ngân sách (W2 = 25) - Tính theo giá quy đổi 1 hộp 4 bánh (~ 4 * giá lẻ)
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
                if (!string.IsNullOrEmpty(b.TheDoiTuongPhuHop) && b.TheDoiTuongPhuHop.Contains(doiTuong))
                {
                    diemDoiTuong = 20;
                    lyDoPhuHop.Add("Thiết kế hương vị chuẩn gu người nhận");
                }
                else diemDoiTuong = 8;

                // 4. Chấm điểm Gu Hương vị (W4 = 20)
                if (b.NhomHuongVi == huongVi)
                {
                    diemHuongVi = 20;
                    lyDoPhuHop.Add("Đúng dòng nhân yêu thích");
                }
                else diemHuongVi = 10;

                double tongDiem = diemSucKhoe + diemNganSach + diemDoiTuong + diemHuongVi - diemPhat;
                if (tongDiem > 0)
                {
                    ketQuaChamDiem.Add(new
                    {
                        maBanh = b.MaBanh,
                        tenBanh = b.TenBanh,
                        donGia = b.DonGiaLe,
                        calo = b.ThongSoDinhDuong?.NangLuongCalo ?? 400,
                        chiSoGI = gi,
                        anhBanh = DichVuHinhAnh.LayUrl(tuDienAnh, "BANH", b.MaBanh, "TACH_NEN_SLOT"),
                        doPhuHop = Math.Min(99, Math.Round(tongDiem)),
                        lyDo = string.Join(" • ", lyDoPhuHop)
                    });
                }
            }

            return Json(ketQuaChamDiem.OrderByDescending(x => ((dynamic)x).doPhuHop).Take(3));
        }

        // =================================================================================
        // API 2: TRA CỨU ĐƠN HÀNG NHANH CHO KHÁCH VÃNG LAI (GUEST TRACKING)
        // =================================================================================
        [HttpGet]
        public async Task<IActionResult> TraCuuDonHangNhanh(string maTraCuu, string sdt)
        {
            if (string.IsNullOrWhiteSpace(maTraCuu) || string.IsNullOrWhiteSpace(sdt))
                return Json(new { thanhCong = false, thongBao = "Vui lòng nhập đầy đủ Mã đơn và Số điện thoại!" });

            var donHang = await _db.DonHangs
                .Include(d => d.DotGiaoHangDaDiems)
                .FirstOrDefaultAsync(d => d.MaTraCuuDh == maTraCuu.Trim() && d.SdtnguoiMua == sdt.Trim());

            if (donHang == null)
                return Json(new { thanhCong = false, thongBao = "Không tìm thấy đơn hàng khớp với thông tin trên!" });

            return Json(new
            {
                thanhCong = true,
                maDon = donHang.MaTraCuuDh,
                nguoiMua = donHang.HoTenNguoiMua,
                ngayDat = donHang.NgayDat.ToString("dd/MM/yyyy HH:mm"),
                tongTien = donHang.TongThanhToanCuoi.ToString("N0") + " đ",
                trangThai = donHang.TrangThaiDh,
                soDiemGiao = donHang.DotGiaoHangDaDiems.Count
            });
        }
    }
}