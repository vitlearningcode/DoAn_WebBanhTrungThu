using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Services;

namespace WebBanhTrungThu.Controllers.CoSo
{
    /// <summary>
    /// Lớp Controller cơ sở trừu tượng cho toàn bộ phân hệ Khách hàng (Storefront),
    /// cung cấp sẵn kết nối CSDL, hàm trích xuất phiên đăng nhập / khách vãng lai và chuẩn hóa phản hồi JSON.
    /// </summary>
    public abstract class ControllerKhachHangCoSo : Controller
    {
        protected readonly CSDLBanhTrungThuContext _context;
        protected CSDLBanhTrungThuContext _db => _context;

        protected ControllerKhachHangCoSo(CSDLBanhTrungThuContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy mã người dùng đang đăng nhập từ Cookie Claims (trả về null nếu là khách vãng lai).
        /// </summary>
        protected int? LayMaNguoiDungDangNhap()
        {
            return ThongTinPhienHelper.LayMaNguoiDung(HttpContext);
        }

        /// <summary>
        /// Lấy mã phiên khách vãng lai (Guest Session Cookie).
        /// </summary>
        protected string? LayMaPhienKhachVangLai()
        {
            return ThongTinPhienHelper.LayMaPhienKVL(HttpContext);
        }

        /// <summary>
        /// Lấy bộ đôi thông tin phiên hiện tại (MaNd, MaPhienKvl) phục vụ truy vấn giỏ hàng và thanh toán.
        /// </summary>
        protected (int? MaNd, string? MaPhienKvl) LayThongTinPhienHienTai()
        {
            int? maNd = LayMaNguoiDungDangNhap();
            string? maPhienKvl = LayMaPhienKhachVangLai();
            return (maNd, maPhienKvl);
        }

        /// <summary>
        /// Trả về phản hồi JSON thành công chuẩn hóa của hệ thống.
        /// </summary>
        protected JsonResult JsonThanhCong(string thongBao, object? duLieu = null)
        {
            if (duLieu == null)
            {
                return Json(new { thanhCong = true, thongBao });
            }
            return Json(new { thanhCong = true, thongBao, duLieu });
        }

        /// <summary>
        /// Trả về phản hồi JSON thất bại kèm thông báo lỗi Tiếng Việt.
        /// </summary>
        protected JsonResult JsonThatBai(string thongBao, string? loaiLoi = null)
        {
            if (string.IsNullOrWhiteSpace(loaiLoi))
            {
                return Json(new { thanhCong = false, thongBao });
            }
            return Json(new { thanhCong = false, loaiLoi, thongBao });
        }
    }
}
