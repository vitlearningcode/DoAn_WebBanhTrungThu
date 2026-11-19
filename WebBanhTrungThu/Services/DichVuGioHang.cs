using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Middlewares;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services
{
    /// <summary>
    /// Dịch vụ Facade (< 18KB) điều phối thêm/sửa/xóa món trong giỏ, gọi DichVuHopQuaTuChon và DichVuKhuyenMaiGioHang,
    /// xử lý GopGioHangAsync và DatLaiDonHangAsync. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuGioHang : DichVuCoSo, IDichVuGioHang
    {
        private readonly IDichVuHopQuaTuChon _dichVuHopTuChon;
        private readonly IDichVuKhuyenMaiGioHang _dichVuKhuyenMai;

        public DichVuGioHang(
            CSDLBanhTrungThuContext db,
            IDichVuHopQuaTuChon dichVuHopTuChon,
            IDichVuKhuyenMaiGioHang dichVuKhuyenMai) : base(db)
        {
            _dichVuHopTuChon = dichVuHopTuChon;
            _dichVuKhuyenMai = dichVuKhuyenMai;
        }

        public async Task<Models.Entities.GioHang> LayHoacTaoGioHangAsync(HttpContext httpContext)
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(httpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(httpContext);

            IQueryable<Models.Entities.GioHang> truyVanDayDu = _db.GioHangs
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation).ThenInclude(cb => cb!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation).ThenInclude(cb => cb!.ChiTietComboMaus).ThenInclude(ctcb => ctcb.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.ChiTietHopTuChons).ThenInclude(cth => cth.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs);

            Models.Entities.GioHang? gioHang;

            if (maNd.HasValue)
            {
                gioHang = await truyVanDayDu.FirstOrDefaultAsync(g => g.MaNd == maNd.Value);
                if (gioHang == null)
                {
                    gioHang = new Models.Entities.GioHang
                    {
                        MaNd = maNd.Value,
                        MaPhienKvl = null,
                        LaCheDoMuaSiB2b = false,
                        YeuCauInLogoB2b = false,
                        NgayCapNhat = LayThoiGianHeThong()
                    };
                    _db.GioHangs.Add(gioHang);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(maPhienKvl))
                {
                    maPhienKvl = "KVL-" + Guid.NewGuid().ToString("N").ToUpperInvariant()[..20];
                    _db.PhienKhachVangLais.Add(new PhienKhachVangLai
                    {
                        MaPhienKvl = maPhienKvl,
                        DiaChiIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        ThongTinTrinhDuyet = "Browser Session",
                        DaGopGioHang = false,
                        NgayTao = LayThoiGianHeThong(),
                        NgayHetHan = LayThoiGianHeThong().AddDays(14)
                    });
                    await _db.SaveChangesAsync();

                    httpContext.Response.Cookies.Append(PhienKhachVangLaiMiddleware.TenCookieKVL, maPhienKvl, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = httpContext.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true,
                        Expires = DateTimeOffset.UtcNow.AddDays(14)
                    });
                    httpContext.Items["MaPhienKVL"] = maPhienKvl;
                }

                gioHang = await truyVanDayDu.FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl);
                if (gioHang == null)
                {
                    gioHang = new Models.Entities.GioHang
                    {
                        MaNd = null,
                        MaPhienKvl = maPhienKvl,
                        LaCheDoMuaSiB2b = false,
                        YeuCauInLogoB2b = false,
                        NgayCapNhat = LayThoiGianHeThong()
                    };
                    _db.GioHangs.Add(gioHang);
                    await _db.SaveChangesAsync();
                }
            }

            return gioHang;
        }

        public async Task<object> ThemHopTuChonAsync(HttpContext httpContext, LuuHopTuChonRequest? req)
        {
            var gioHang = await LayHoacTaoGioHangAsync(httpContext);
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(httpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(httpContext);
            return await _dichVuHopTuChon.ThemHoacCapNhatHopTuChonAsync(gioHang, maNd, maPhienKvl, req);
        }

        public async Task<object> ThemBanhHoacComboAsync(HttpContext httpContext, ThemMonLeRequest? req)
        {
            if (req == null || req.MaSanPham <= 0)
            {
                return new { thanhCong = false, thongBao = "Thông tin sản phẩm không hợp lệ!" };
            }

            int soLuongThem = req.SoLuong > 0 ? req.SoLuong : 1;
            var gioHang = await LayHoacTaoGioHangAsync(httpContext);
            string tenHienThi;

            if (req.LoaiSanPham == 1)
            {
                var banh = await _db.SanPhamBanhs.Include(b => b.LoSanXuatBanhs).FirstOrDefaultAsync(b => b.MaBanh == req.MaSanPham && b.TrangThaiBan);
                if (banh == null)
                {
                    return new { thanhCong = false, thongBao = "Sản phẩm bánh không tồn tại hoặc đã ngừng bán!" };
                }

                tenHienThi = banh.TenBanh;
                var dongHienTai = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.LoaiSanPham == 1 && ct.MaBanh == banh.MaBanh);

                if (dongHienTai != null)
                {
                    dongHienTai.SoLuong += soLuongThem;
                    dongHienTai.DonGiaTamTinh = banh.DonGiaThucTe;
                }
                else
                {
                    _db.ChiTietGioHangs.Add(new ChiTietGioHang
                    {
                        MaGh = gioHang.MaGh,
                        LoaiSanPham = 1,
                        MaBanh = banh.MaBanh,
                        SoLuong = soLuongThem,
                        DonGiaTamTinh = banh.DonGiaThucTe,
                        NgayThem = LayThoiGianHeThong()
                    });
                }
            }
            else if (req.LoaiSanPham == 2)
            {
                var combo = await _db.ComboMauCoSans.FirstOrDefaultAsync(c => c.MaCb == req.MaSanPham && c.TrangThai);
                if (combo == null)
                {
                    return new { thanhCong = false, thongBao = "Combo lễ hộp không tồn tại hoặc đã ngừng bán!" };
                }

                tenHienThi = combo.TenCombo;
                var dongHienTai = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.LoaiSanPham == 2 && ct.MaCb == combo.MaCb);

                if (dongHienTai != null)
                {
                    dongHienTai.SoLuong += soLuongThem;
                    dongHienTai.DonGiaTamTinh = combo.GiaUuDaiCombo;
                }
                else
                {
                    _db.ChiTietGioHangs.Add(new ChiTietGioHang
                    {
                        MaGh = gioHang.MaGh,
                        LoaiSanPham = 2,
                        MaCb = combo.MaCb,
                        SoLuong = soLuongThem,
                        DonGiaTamTinh = combo.GiaUuDaiCombo,
                        NgayThem = LayThoiGianHeThong()
                    });
                }
            }
            else
            {
                return new { thanhCong = false, thongBao = "Loại sản phẩm không được hỗ trợ!" };
            }

            gioHang.NgayCapNhat = LayThoiGianHeThong();
            await _db.SaveChangesAsync();

            return new
            {
                thanhCong = true,
                thongBao = $"Đã thêm {soLuongThem} x \"{tenHienThi}\" vào Giỏ Quà!"
            };
        }

        public async Task<object> CapNhatSoLuongAsync(HttpContext httpContext, CapNhatSoLuongGioRequest? req)
        {
            if (req == null || req.MaCtgh <= 0)
            {
                return new { thanhCong = false, thongBao = "Dữ liệu cập nhật không hợp lệ!" };
            }

            var gioHang = await LayHoacTaoGioHangAsync(httpContext);
            var dongGio = await _db.ChiTietGioHangs
                .FirstOrDefaultAsync(ct => ct.MaCtgh == req.MaCtgh && ct.MaGh == gioHang.MaGh);

            if (dongGio == null)
            {
                return new { thanhCong = false, thongBao = "Không tìm thấy sản phẩm trong giỏ hàng của bạn!" };
            }

            if (req.SoLuongMoi <= 0)
            {
                await _dichVuHopTuChon.XoaDongChiTietVaHopMoCoiAsync(dongGio);
                gioHang.NgayCapNhat = LayThoiGianHeThong();
                await _db.SaveChangesAsync();
                return new { thanhCong = true, thongBao = "Đã xóa sản phẩm khỏi Giỏ Quà!" };
            }

            dongGio.SoLuong = req.SoLuongMoi;
            gioHang.NgayCapNhat = LayThoiGianHeThong();
            await _db.SaveChangesAsync();

            return new { thanhCong = true, thongBao = "Đã cập nhật số lượng!" };
        }

        public async Task<object> XoaMonAsync(HttpContext httpContext, CapNhatSoLuongGioRequest? req)
        {
            if (req == null || req.MaCtgh <= 0)
            {
                return new { thanhCong = false, thongBao = "Mã chi tiết giỏ hàng không hợp lệ!" };
            }

            var gioHang = await LayHoacTaoGioHangAsync(httpContext);
            var dongGio = await _db.ChiTietGioHangs
                .FirstOrDefaultAsync(ct => ct.MaCtgh == req.MaCtgh && ct.MaGh == gioHang.MaGh);

            if (dongGio == null)
            {
                return new { thanhCong = false, thongBao = "Món quà này không tồn tại trong giỏ!" };
            }

            await _dichVuHopTuChon.XoaDongChiTietVaHopMoCoiAsync(dongGio);
            gioHang.NgayCapNhat = LayThoiGianHeThong();
            await _db.SaveChangesAsync();

            return new { thanhCong = true, thongBao = "Đã xóa món quà khỏi giỏ hàng!" };
        }

        public async Task<object> ApDungVoucherAsync(HttpContext httpContext, ApDungVoucherRequest? req, string? codeKm)
        {
            var gioHang = await LayHoacTaoGioHangAsync(httpContext);
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(httpContext);
            return await _dichVuKhuyenMai.ApDungVoucherChoGioHangAsync(gioHang, maNd, req, codeKm);
        }

        public Task GopGioHangAsync(string? maPhienKVL, int maND)
            => _dichVuHopTuChon.GopGioHangTuKhachVangLaiAsync(maPhienKVL, maND);

        public async Task<object> DatLaiDonHangAsync(HttpContext httpContext, int maDonHang)
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(httpContext);
            var gioHang = await LayHoacTaoGioHangAsync(httpContext);
            return await _dichVuHopTuChon.SaoChepDonCuVaoGioHangAsync(gioHang, maDonHang, maNd);
        }

        public async Task<object> LayThongTinGioHangJsonAsync(HttpContext httpContext)
        {
            int? maNd = ThongTinPhienHelper.LayMaNguoiDung(httpContext);
            string? maPhienKvl = ThongTinPhienHelper.LayMaPhienKVL(httpContext);

            IQueryable<Models.Entities.GioHang> truyVan = _db.GioHangs
                .AsNoTracking()
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation).ThenInclude(b => b!.ThongSoDinhDuong)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaCbNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.MaVhNavigation)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation).ThenInclude(h => h!.ChiTietHopTuChons).ThenInclude(cth => cth.MaBanhNavigation).ThenInclude(b => b!.LoSanXuatBanhs);

            Models.Entities.GioHang? gioHang = maNd.HasValue
                ? await truyVan.FirstOrDefaultAsync(g => g.MaNd == maNd.Value)
                : (!string.IsNullOrEmpty(maPhienKvl) ? await truyVan.FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKvl) : null);

            return await _dichVuKhuyenMai.TongKetThongTinGioHangJsonAsync(gioHang, maNd);
        }

        public async Task<object> KiemTraTonKhoThucTeAsync(HttpContext httpContext)
        {
            var gioHang = await LayHoacTaoGioHangAsync(httpContext);
            return await _dichVuKhuyenMai.KiemTraTonKhoGioHangChiTietAsync(gioHang);
        }
    }
}
