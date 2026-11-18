using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebBanhTrungThu.Data;

namespace WebBanhTrungThu.Areas.Admin.Controllers.CoSo
{
    /// <summary>
    /// Lớp Controller cơ sở trừu tượng cho toàn bộ phân khu Quản trị Nguyệt Cát ERP ([Area("Admin")]),
    /// cung cấp sẵn kết nối CSDL, tiện ích kiểm tra phân quyền vai trò và phản hồi JSON chuẩn hóa.
    /// </summary>
    [Area("Admin")]
    public abstract class ControllerQuanTriCoSo : Controller
    {
        protected readonly CSDLBanhTrungThuContext _context;
        protected CSDLBanhTrungThuContext _db => _context;

        protected ControllerQuanTriCoSo(CSDLBanhTrungThuContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy mã vai trò hiện tại của nhân sự đăng nhập (QUAN_TRI, THU_NGAN, GIAO_HANG, NHAN_VIEN).
        /// </summary>
        protected string LayVaiTroNhanSuHienTai()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "QUAN_TRI";
        }

        /// <summary>
        /// Lấy họ tên nhân sự vận hành đang đăng nhập.
        /// </summary>
        protected string LayHoTenNhanSuHienTai()
        {
            return User.FindFirstValue(ClaimTypes.Name) ?? "Quản trị viên Nguyệt Cát";
        }

        protected string LayTenQuanTriVienDangNhap() => LayHoTenNhanSuHienTai();

        /// <summary>
        /// Kiểm tra xem nhân sự hiện tại có quyền Quản trị viên tối cao (Admin Override) hay không.
        /// </summary>
        protected bool LaQuanTriVienToiCao()
        {
            return User.IsInRole("QUAN_TRI");
        }

        /// <summary>
        /// Trả về phản hồi JSON thành công chuẩn hóa cho phân khu Admin.
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
        /// Trả về phản hồi JSON thất bại chuẩn hóa cho phân khu Admin.
        /// </summary>
        protected JsonResult JsonThatBai(string thongBao)
        {
            return Json(new { thanhCong = false, thongBao });
        }
    }
}
