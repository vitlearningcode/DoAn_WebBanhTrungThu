using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN")]
    public class SoQuyAdminController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;

        public SoQuyAdminController(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? locPhuongThuc)
        {
            var danhSachDon = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.ChiTietDonHangs)
                .Where(d => d.TrangThaiDh != "DA_HUY")
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            decimal tongThu = danhSachDon.Sum(d => d.TongThanhToanCuoi);
            decimal thuCod = danhSachDon.Where(d => d.PhuongThucTt == "COD").Sum(d => d.TongThanhToanCuoi);
            decimal thuVietQr = danhSachDon.Where(d => d.PhuongThucTt == "VIETQR").Sum(d => d.TongThanhToanCuoi);
            decimal thuMoMo = danhSachDon.Where(d => d.PhuongThucTt == "MOMO").Sum(d => d.TongThanhToanCuoi);
            decimal thuVnPay = danhSachDon.Where(d => d.PhuongThucTt == "VNPAY").Sum(d => d.TongThanhToanCuoi);

            // Ước tính tổng giá vốn (~ 54% doanh thu tiền hàng trước giảm)
            decimal tongTienHang = danhSachDon.Sum(d => d.TongTienHang);
            decimal tongGiaVon = Math.Round(tongTienHang * 0.54m, 0);
            decimal loiNhuanGop = tongThu - tongGiaVon;

            var dsHienThi = danhSachDon;
            if (!string.IsNullOrWhiteSpace(locPhuongThuc) && locPhuongThuc != "ALL")
            {
                dsHienThi = danhSachDon.Where(d => d.PhuongThucTt == locPhuongThuc).ToList();
            }

            ViewBag.LocPhuongThuc = locPhuongThuc ?? "ALL";

            var vm = new SoQuyAdminViewModel
            {
                QuyDauKy = 25000000m,
                TongThuTrongKy = tongThu,
                ThuTienMatCod = thuCod,
                ThuChuyenKhoanVietQr = thuVietQr,
                ThuViMoMo = thuMoMo,
                ThuVnPay = thuVnPay,
                TongGiaVonUocTinh = tongGiaVon,
                LoiNhuanGop = loiNhuanGop,
                DanhSachPhieuThuDonHang = dsHienThi
            };

            return View(vm);
        }
    }
}

