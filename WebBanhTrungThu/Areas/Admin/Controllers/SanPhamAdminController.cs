using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Areas.Admin.Controllers.CoSo;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản trị Hàng hóa, Kiểm kho lô FEFO và Thiết lập bảng giá Đại lý Nguyệt Cát ERP,
    /// kế thừa từ ControllerQuanTriCoSo và ủy quyền nghiệp vụ sang DichVuQuanTriSanPham, DichVuQuanTriKhoFefo & DichVuQuanTriBangGia.
    /// </summary>
    [Authorize(Policy = "ChinhSachQuanTriHeThong")]
    public class SanPhamAdminController : ControllerQuanTriCoSo
    {
        private readonly IDichVuQuanTriSanPham _dichVuSanPham;
        private readonly IDichVuQuanTriKhoFefo _dichVuKhoFefo;
        private readonly IDichVuQuanTriBangGia _dichVuBangGia;

        public SanPhamAdminController(
            CSDLBanhTrungThuContext db,
            IDichVuQuanTriSanPham dichVuSanPham,
            IDichVuQuanTriKhoFefo dichVuKhoFefo,
            IDichVuQuanTriBangGia dichVuBangGia) : base(db)
        {
            _dichVuSanPham = dichVuSanPham;
            _dichVuKhoFefo = dichVuKhoFefo;
            _dichVuBangGia = dichVuBangGia;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tuKhoa,
            int? maDm,
            string? tenDm,
            string? locDacTinh,
            string? locTonKho,
            string? duKienHetHang,
            string? thoiGianTao,
            string? nhaCungCap,
            string? thuongHieu,
            string? viTriKho,
            string? loaiHang,
            string? banTrucTiep)
        {
            var vm = await _dichVuSanPham.XayDungDanhMucHangHoaAsync(
                tuKhoa, maDm, tenDm, locDacTinh, locTonKho,
                duKienHetHang, thoiGianTao, nhaCungCap,
                thuongHieu, viTriKho, loaiHang, banTrucTiep);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuHangHoaKiot(LuuHangHoaKiotRequest req)
        {
            TempData["ThongBaoAdmin"] = await _dichVuSanPham.LuuHangHoaKiotAsync(req);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoNhomHangNhanh(string tenNhomHang, string? moTa)
        {
            string? thongBao = await _dichVuSanPham.TaoNhomHangNhanhAsync(tenNhomHang, moTa);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoThuongHieuNhanh(string tenThuongHieu, string? tenNhaCungCap)
        {
            string? thongBao = _dichVuSanPham.TaoThuongHieuNhanh(tenThuongHieu, tenNhaCungCap);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoViTriNhanh(string tenViTri)
        {
            string? thongBao = _dichVuSanPham.TaoViTriNhanh(tenViTri);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoNhaCungCapNhanh(string tenNhaCungCap)
        {
            string? thongBao = _dichVuSanPham.TaoNhaCungCapNhanh(tenNhaCungCap);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoDanhMucNhanhAjax(string loaiDanhMuc, string tenMoi, string? moTaPhu)
        {
            var ketQua = await _dichVuSanPham.TaoDanhMucNhanhAjaxAsync(loaiDanhMuc, tenMoi, moTaPhu);
            return Json(ketQua);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaHangHoa(int idGoc, bool laCombo)
        {
            string? thongBao = await _dichVuSanPham.XoaHangHoaAsync(idGoc, laCombo);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatBanhNhanh(CapNhatBanhNhanhRequest req)
        {
            string? thongBao = await _dichVuBangGia.CapNhatGiaBanhNhanhAsync(req);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemBanhMoi(ThemBanhMoiAdminRequest req)
        {
            return await LuuHangHoaKiot(new LuuHangHoaKiotRequest
            {
                IdGoc = 0,
                LaCombo = false,
                MaHang = req.MaSku,
                TenHang = req.TenBanh,
                MaDm = req.MaDm,
                GiaBan = req.DonGiaLe,
                GiaVon = req.GiaVon,
                TonKho = req.TongTonKho,
                TrongLuong = req.TrongLuongGram,
                DonViTrongLuong = "g",
                MoTaNgan = req.MoTaNgan,
                NangLuongCalo = req.NangLuongCalo,
                LuongDuongGram = req.LuongDuongGram,
                ChiSoDuongHuyetGi = req.ChiSoDuongHuyetGi,
                CanhBaoDiUng = req.CanhBaoDiUng,
                MucDoNgot = req.MucDoNgot,
                LaBanhChay = req.LaBanhChay,
                LaBanhThuongHang = req.LaBanhThuongHang,
                BanTrucTiep = true
            });
        }

        [HttpGet]
        public IActionResult KiemKho()
        {
            return RedirectToAction("Index", "KiemKhoAdmin", new { area = "Admin" });
        }

        [HttpGet]
        public async Task<IActionResult> XuatExcel(string dinhDang = "xlsx")
        {
            var (noiDung, contentType, tenFile) = await _dichVuSanPham.XuatExcelHangHoaAsync(dinhDang);
            return File(noiDung, contentType, tenFile);
        }

        [HttpGet]
        public async Task<IActionResult> TaiFileMauExcel(string dinhDang = "xlsx")
        {
            var (noiDung, contentType, tenFile) = await _dichVuSanPham.TaoFileMauImportHangHoaAsync(dinhDang);
            return File(noiDung, contentType, tenFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NhapExcel(IFormFile? fileExcel)
        {
            var kq = await _dichVuSanPham.NhapExcelHangHoaAsync(fileExcel);
            TempData[kq.ThanhCong ? "ThongBaoAdmin" : "LoiAdmin"] = kq.ThongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemLoBanhMoi(
            int maBanh,
            string? maLoCode,
            DateTime? ngaySanXuat,
            DateTime? hanSuDung,
            int slNhapBanDau = 50,
            decimal phanTramGiamCanDate = 0m,
            string? nguonHangPhanPhoi = null,
            decimal donGiaNhapSi = 0m,
            bool tuDongGhiPhieuChi = false,
            string? returnUrl = null)
        {
            var (hopLe, thongBao) = await _dichVuKhoFefo.ThemLoBanhMoiAsync(
                maBanh,
                maLoCode,
                ngaySanXuat,
                hanSuDung,
                slNhapBanDau,
                phanTramGiamCanDate,
                nguonHangPhanPhoi,
                donGiaNhapSi,
                tuDongGhiPhieuChi);

            TempData[hopLe ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;

            if (hopLe && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(KiemKho));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatGiamGiaCanDate(int maLo, decimal phanTramGiam, string? trangThaiLo, int? slConLai, string? returnUrl)
        {
            string? thongBao = await _dichVuKhoFefo.CapNhatGiamGiaCanDateAsync(maLo, phanTramGiam, trangThaiLo, slConLai);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaLoBanh(int maLo, string? returnUrl = null)
        {
            string? thongBao = await _dichVuKhoFefo.XoaLoBanhAsync(maLo);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(KiemKho));
        }

        [HttpPost]
        public async Task<IActionResult> KichHoatXaKhoNhanh(int maLo, int mucGiam = 30)
        {
            var ketQua = await _dichVuKhoFefo.KichHoatXaKhoNhanhAsync(maLo, mucGiam);
            return Json(ketQua);
        }

        [HttpGet]
        public async Task<IActionResult> ThietLapGia()
        {
            var vm = await _dichVuBangGia.XayDungThietLapGiaAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemVoHopMoi(
            string? maCodeVh,
            string tenVoHop,
            int sucChuaBanh = 4,
            string? kieuBoCucLuoi = "LUOI_2X2",
            decimal giaVoHopRieng = 150000m,
            int slTonKhoVo = 50,
            int slBanhThuongHangToiThieu = 0,
            string? moTaChatLieu = null)
        {
            var (hopLe, thongBao) = await _dichVuBangGia.ThemVoHopMoiAsync(
                maCodeVh, tenVoHop, sucChuaBanh, kieuBoCucLuoi,
                giaVoHopRieng, slTonKhoVo, slBanhThuongHangToiThieu, moTaChatLieu);

            TempData[hopLe ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatGiaVoHop(int maVh, decimal giaVoHopRieng, int slTonKhoVo, int slBanhVipToiThieu)
        {
            string? thongBao = await _dichVuBangGia.CapNhatGiaVoHopAsync(maVh, giaVoHopRieng, slTonKhoVo, slBanhVipToiThieu);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaVoHop(int maVh)
        {
            string? thongBao = await _dichVuBangGia.XoaVoHopAsync(maVh);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemMocB2BMoi(
            string tenMocB2b,
            int slHopToiThieu = 10,
            int? slHopToiDa = 50,
            decimal phanTramChietKhau = 10m,
            decimal phiKhuonInLogo = 500000m,
            bool mienPhiInLogo = false)
        {
            var (hopLe, thongBao) = await _dichVuBangGia.ThemMocB2BMoiAsync(
                tenMocB2b, slHopToiThieu, slHopToiDa, phanTramChietKhau, phiKhuonInLogo, mienPhiInLogo);

            TempData[hopLe ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatMocB2B(
            int maMocCk,
            int? slHopToiThieu,
            int? slHopToiDa,
            decimal phanTramChietKhau,
            decimal phiKhuonInLogo,
            bool mienPhiInLogo)
        {
            string? thongBao = await _dichVuBangGia.CapNhatMocB2BAsync(
                maMocCk, slHopToiThieu, slHopToiDa, phanTramChietKhau, phiKhuonInLogo, mienPhiInLogo);

            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaMocB2B(int maMocCk)
        {
            string? thongBao = await _dichVuBangGia.XoaMocB2BAsync(maMocCk);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemComboGiaMoi(
            string? maCodeCb,
            string tenCombo,
            int maVh,
            decimal giaNiemYet,
            decimal giaUuDaiCombo,
            string? thongDiepYngia)
        {
            var (hopLe, thongBao) = await _dichVuBangGia.ThemComboGiaMoiAsync(
                maCodeCb, tenCombo, maVh, giaNiemYet, giaUuDaiCombo, thongDiepYngia);

            TempData[hopLe ? "ThongBaoAdmin" : "LoiAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatGiaCombo(int maCb, decimal giaNiemYet, decimal giaUuDaiCombo)
        {
            string? thongBao = await _dichVuBangGia.CapNhatGiaComboAsync(maCb, giaNiemYet, giaUuDaiCombo);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaComboGia(int maCb)
        {
            string? thongBao = await _dichVuBangGia.XoaComboGiaAsync(maCb);
            if (!string.IsNullOrWhiteSpace(thongBao)) TempData["ThongBaoAdmin"] = thongBao;
            return RedirectToAction(nameof(ThietLapGia));
        }
    }
}
