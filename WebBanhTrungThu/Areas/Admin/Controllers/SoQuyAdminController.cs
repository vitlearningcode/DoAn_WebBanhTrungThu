using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN,GIAO_HANG")]
    public class SoQuyAdminController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuKiotVietMetadata _kiotMeta;

        public SoQuyAdminController(CSDLBanhTrungThuContext db, DichVuKiotVietMetadata kiotMeta)
        {
            _db = db;
            _kiotMeta = kiotMeta;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? locPhuongThuc)
        {
            var tatCaDon = await _db.DonHangs
                .AsNoTracking()
                .Include(d => d.ChiTietDonHangs)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            var store = _kiotMeta.DocDuLieu();
            foreach (var d in tatCaDon)
            {
                _kiotMeta.GanMetadataChoDonHang(d, store);
            }

            // 1. Các đơn hợp lệ (TrangThaiDh != "DA_HUY") dùng để ghi nhận Phiếu Thu
            var danhSachDonHieuLuc = tatCaDon
                .Where(d => d.TrangThaiDh != "DA_HUY")
                .ToList();

            // 2. Các đơn đã hủy và đã hoàn tiền ngân hàng cho khách (TrangThaiTt == "DA_HOAN_TIEN")
            var danhSachDonHoanTien = tatCaDon
                .Where(d => d.TrangThaiTt == "DA_HOAN_TIEN")
                .ToList();

            // Tổng thu tính theo các đơn hiệu lực (ưu tiên đơn DA_TT hoặc đơn đang vận hành)
            var donDaThu = danhSachDonHieuLuc.Where(d => d.TrangThaiTt == "DA_TT").ToList();
            var tapTinhThu = donDaThu.Any() ? donDaThu : danhSachDonHieuLuc;

            decimal tongThu = tapTinhThu.Sum(d => d.TongThanhToanCuoi);
            decimal thuCod = tapTinhThu.Where(d => d.PhuongThucTt == "COD").Sum(d => d.TongThanhToanCuoi);
            decimal thuVietQr = tapTinhThu.Where(d => d.PhuongThucTt == "VIETQR").Sum(d => d.TongThanhToanCuoi);
            decimal thuMoMo = tapTinhThu.Where(d => d.PhuongThucTt == "MOMO").Sum(d => d.TongThanhToanCuoi);
            decimal thuVnPay = tapTinhThu.Where(d => d.PhuongThucTt == "VNPAY").Sum(d => d.TongThanhToanCuoi);

            decimal tongHoanTien = danhSachDonHoanTien.Sum(d => d.TongThanhToanCuoi);

            // Ước tính tổng giá vốn (~ 54% doanh thu tiền hàng trước giảm của các đơn chưa hủy)
            decimal tongTienHang = danhSachDonHieuLuc.Sum(d => d.TongTienHang);
            decimal tongGiaVon = Math.Round(tongTienHang * 0.54m, 0);
            decimal loiNhuanGop = tongThu - tongGiaVon;

            var dsHienThi = danhSachDonHieuLuc;
            if (!string.IsNullOrWhiteSpace(locPhuongThuc) && locPhuongThuc != "ALL")
            {
                dsHienThi = danhSachDonHieuLuc.Where(d => d.PhuongThucTt == locPhuongThuc).ToList();
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
                TongTienHoanKhach = tongHoanTien,
                LoiNhuanGop = loiNhuanGop,
                DanhSachPhieuThuDonHang = dsHienThi,
                DanhSachPhieuHoanTien = danhSachDonHoanTien
            };

            return View(vm);
        }
    }
}
