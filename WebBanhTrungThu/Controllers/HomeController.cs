using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Controllers
{
    public class HomeController : ControllerKhachHangCoSo
    {
        private readonly IDichVuTrangChu _dichVuTrangChu;

        public HomeController(CSDLBanhTrungThuContext db, IDichVuTrangChu dichVuTrangChu) : base(db)
        {
            _dichVuTrangChu = dichVuTrangChu;
        }

        public async Task<IActionResult> Index()
        {
            var (maND, maPhienKVL) = LayThongTinPhienHienTai();
            var vm = await _dichVuTrangChu.XayDungDuLieuTrangChuAsync(maND, maPhienKVL);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GoiYBanhThongMinh(string doiTuong, string sucKhoe, string huongVi, decimal nganSachMax)
        {
            var ketQua = await _dichVuTrangChu.GoiYBanhThongMinhAsync(doiTuong, sucKhoe, huongVi, nganSachMax);
            return Json(ketQua);
        }

        [HttpGet]
        public async Task<IActionResult> TraCuuDonHangNhanh(string maTraCuu, string sdt)
        {
            var ketQua = await _dichVuTrangChu.TraCuuDonHangNhanhAsync(maTraCuu, sdt);
            return Json(ketQua);
        }
    }
}
