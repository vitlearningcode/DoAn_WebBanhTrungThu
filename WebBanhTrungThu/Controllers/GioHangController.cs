using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Controllers
{
    /// <summary>
    /// Controller điều phối các API Giỏ hàng AJAX, kế thừa từ ControllerKhachHangCoSo
    /// và ủy quyền toàn bộ xử lý nghiệp vụ sang DichVuGioHang.
    /// </summary>
    public class GioHangController : ControllerKhachHangCoSo
    {
        private readonly DichVuGioHang _dichVuGioHang;

        public GioHangController(CSDLBanhTrungThuContext db, DichVuGioHang dichVuGioHang) : base(db)
        {
            _dichVuGioHang = dichVuGioHang;
        }

        [HttpPost]
        public async Task<IActionResult> ThemHopTuChon([FromBody] LuuHopTuChonRequest? req)
        {
            var ketQua = await _dichVuGioHang.ThemHopTuChonAsync(HttpContext, req);
            return Json(ketQua);
        }

        [HttpPost]
        public async Task<IActionResult> ThemBanhHoacCombo([FromBody] ThemMonLeRequest? req)
        {
            var ketQua = await _dichVuGioHang.ThemBanhHoacComboAsync(HttpContext, req);
            return Json(ketQua);
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatSoLuong([FromBody] CapNhatSoLuongGioRequest? req)
        {
            var ketQua = await _dichVuGioHang.CapNhatSoLuongAsync(HttpContext, req);
            return Json(ketQua);
        }

        [HttpPost]
        public async Task<IActionResult> XoaMon([FromBody] CapNhatSoLuongGioRequest? req)
        {
            var ketQua = await _dichVuGioHang.XoaMonAsync(HttpContext, req);
            return Json(ketQua);
        }

        [HttpPost]
        public async Task<IActionResult> ApDungVoucher([FromBody] ApDungVoucherRequest? req, [FromQuery] string? codeKm)
        {
            var ketQua = await _dichVuGioHang.ApDungVoucherAsync(HttpContext, req, codeKm);
            return Json(ketQua);
        }

        [HttpPost]
        public async Task<IActionResult> DatLaiDonHang([FromBody] int maDonHang)
        {
            var ketQua = await _dichVuGioHang.DatLaiDonHangAsync(HttpContext, maDonHang);
            return Json(ketQua);
        }

        [HttpGet]
        public async Task<IActionResult> LayThongTinGioHangJson()
        {
            var ketQua = await _dichVuGioHang.LayThongTinGioHangJsonAsync(HttpContext);
            return Json(ketQua);
        }

        [HttpGet]
        public async Task<IActionResult> KiemTraTonKhoThucTe()
        {
            var ketQua = await _dichVuGioHang.KiemTraTonKhoThucTeAsync(HttpContext);
            return Json(ketQua);
        }
    }
}
