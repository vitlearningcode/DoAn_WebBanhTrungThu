using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller điều phối màn hình Bán hàng tại quầy Nguyệt Cát ERP Web POS,
    /// kế thừa từ ControllerQuanTriCoSo và ủy quyền nghiệp vụ sang DichVuBanHangPos & DichVuQuanTriKhoFefo.
    /// </summary>
    [Authorize(Roles = "QUAN_TRI,ADMIN,NHAN_VIEN,THU_NGAN,NV_BAN_HANG")]
    public class BanHangPosController : ControllerQuanTriCoSo
    {
        private readonly IDichVuBanHangPos _dichVuPos;
        private readonly IDichVuQuanTriKhoFefo _dichVuKhoFefo;

        public BanHangPosController(
            CSDLBanhTrungThuContext db,
            IDichVuBanHangPos dichVuPos,
            IDichVuQuanTriKhoFefo dichVuKhoFefo) : base(db)
        {
            _dichVuPos = dichVuPos;
            _dichVuKhoFefo = dichVuKhoFefo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string tenThuNgan = LayHoTenNhanSuHienTai();
            string vaiTro = LayVaiTroNhanSuHienTai();
            var vm = await _dichVuPos.XayDungDuLieuManHinhPosAsync(tenThuNgan, vaiTro);
            return View(vm);
        }

        public class PosCheckoutDayDuRequest : PosCheckoutRequest
        {
            public bool DungDiemTichLuy { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> TraCuuHoiVienPos(string? sdt)
        {
            var ketQua = await _dichVuPos.TraCuuHoiVienPosAsync(sdt);
            return Json(ketQua);
        }

        [HttpGet]
        public async Task<IActionResult> TimKhachHangPos(string? tuKhoa)
        {
            var ketQua = await _dichVuPos.TimKhachHangPosAsync(tuKhoa);
            return Json(ketQua);
        }

        [HttpPost]
        public async Task<IActionResult> ThemKhachHangNhanhPos(string hoTen, string sdt, string? email)
        {
            var ketQua = await _dichVuPos.ThemKhachHangNhanhPosAsync(hoTen, sdt, email);
            return Json(ketQua);
        }

        [HttpPost]
        public async Task<IActionResult> ThanhToanTaiQuay([FromBody] PosCheckoutDayDuRequest? req, [FromQuery] bool dungDiemTichLuy = false)
        {
            bool suDungDiem = (req?.DungDiemTichLuy ?? false) || dungDiemTichLuy;
            int? maNdLap = LayMaNhanSuHienTai();
            var ketQua = await _dichVuPos.ThanhToanTaiQuayAsync(req, suDungDiem, maNdLap);
            return Json(ketQua);
        }

        [HttpPost]
        [Authorize(Roles = "QUAN_TRI,ADMIN")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KichHoatXaKhoNhanh(int maLo, int mucGiam = 30)
        {
            var ketQua = await _dichVuKhoFefo.KichHoatXaKhoNhanhAsync(maLo, mucGiam);
            return Json(ketQua);
        }
    }
}
