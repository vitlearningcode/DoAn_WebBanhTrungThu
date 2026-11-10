using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;

namespace WebBanhTrungThu.Controllers
{
    public class Thiep3DController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuKiotVietMetadata _kiotMeta;

        public Thiep3DController(CSDLBanhTrungThuContext db, DichVuKiotVietMetadata kiotMeta)
        {
            _db = db;
            _kiotMeta = kiotMeta;
        }

        [HttpGet]
        public async Task<IActionResult> Xem(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Home");
            }

            string tokenChuan = id.Trim();
            var dotGiao = await _db.DotGiaoHangDaDiems
                .Include(d => d.MaDhNavigation)
                .FirstOrDefaultAsync(d => d.MaTokenThiepQr == tokenChuan && d.CoKemThiepQr);

            if (dotGiao != null)
            {
                var metaThiep = _kiotMeta.LayThongTinThiepTheoToken(dotGiao.MaDh, dotGiao.MaTokenThiepQr);
                if (metaThiep != null)
                {
                    dotGiao.TenNguoiNhanThiep = metaThiep.TenNguoiNhanThiep;
                    dotGiao.TenNguoiGuiThiep = metaThiep.TenNguoiGuiThiep;
                }

                dotGiao.SoLanMoThiep += 1;
                await _db.SaveChangesAsync();
                return View(dotGiao);
            }

            // Dự phòng hiển thị mẫu nếu truy cập bằng mã Token thử nghiệm chưa có trong DB
            var dotMau = new DotGiaoHangDaDiem
            {
                MaTokenThiepQr = tokenChuan,
                TenNguoiNhan = "Quý Khách & Gia Đình",
                TenNguoiNhanThiep = "Quý Khách & Gia Đình",
                TenNguoiGuiThiep = "Con Lê Minh Đức",
                SdtnguoiNhan = "0909888999",
                DiaChiGiaoChiTiet = "Khu Biệt Thự Vinhomes Central Park, TP.HCM",
                NgayHenGiao = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                KhungGioGiao = "Tối Đoàn Viên (18:00 - 20:30)",
                CoKemThiepQr = true,
                LoiChucThiep = "Kính chúc Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & Vạn Sự An Khang!",
                MauGiaoDienThiep = "CO_PHONG_HOANG_GIA",
                SoLanMoThiep = 1,
                MaDhNavigation = new DonHang
                {
                    MaTraCuuDh = "TT2026-DEMO",
                    HoTenNguoiMua = "Lê Minh Đức"
                }
            };

            return View(dotMau);
        }
    }
}
