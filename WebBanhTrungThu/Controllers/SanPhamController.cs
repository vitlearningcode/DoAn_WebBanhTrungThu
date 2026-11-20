using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Controllers
{
    public class SanPhamController : ControllerKhachHangCoSo
    {
        private readonly IDichVuChiTietSanPham _dichVuChiTietSanPham;

        public SanPhamController(
            CSDLBanhTrungThuContext db,
            IDichVuChiTietSanPham dichVuChiTietSanPham) : base(db)
        {
            _dichVuChiTietSanPham = dichVuChiTietSanPham;
        }

        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            if (id <= 0) return RedirectToAction("Index", "Home");

            var vm = await _dichVuChiTietSanPham.LayChiTietSanPhamAsync(id);
            if (vm == null) return NotFound();

            return View(vm);
        }
    }
}
