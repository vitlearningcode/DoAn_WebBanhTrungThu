using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;

namespace WebBanhTrungThu.Services.CoSo
{
    /// <summary>
    /// Giao diện chuẩn cho tầng Dịch vụ cơ sở (Base Service Interface) trong kiến trúc Clean MVC 3 tầng.
    /// </summary>
    public interface IDichVuCoSo
    {
        DateTime LayThoiGianHeThong();
        DateOnly LayNgayHienTai();
        string ChuanHoaChuoi(string? duLieuTho, string giaTriMacDinh = "");
    }

    /// <summary>
    /// Lớp cơ sở trừu tượng (Abstract Base Service) cung cấp kết nối CSDL dùng chung,
    /// quản lý giao dịch ACID (Database Transaction) và các tiện ích chuẩn hóa dữ liệu Tiếng Việt.
    /// </summary>
    public abstract class DichVuCoSo : IDichVuCoSo
    {
        protected readonly CSDLBanhTrungThuContext _context;
        protected CSDLBanhTrungThuContext _db => _context;

        protected DichVuCoSo(CSDLBanhTrungThuContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy thời gian hệ thống hiện tại.
        /// </summary>
        public virtual DateTime LayThoiGianHeThong() => DateTime.Now;

        /// <summary>
        /// Lấy ngày hiện tại kiểu DateOnly phục vụ đối chiếu hạn sử dụng lô bánh FEFO.
        /// </summary>
        public virtual DateOnly LayNgayHienTai() => DateOnly.FromDateTime(DateTime.Today);

        /// <summary>
        /// Chuẩn hóa chuỗi đầu vào (Trim khoảng trắng thừa, trả về giá trị mặc định nếu rỗng).
        /// </summary>
        public virtual string ChuanHoaChuoi(string? duLieuTho, string giaTriMacDinh = "")
        {
            return string.IsNullOrWhiteSpace(duLieuTho) ? giaTriMacDinh : duLieuTho.Trim();
        }

        /// <summary>
        /// Thực thi một tác vụ bất đồng bộ an toàn bên trong giao dịch CSDL (BeginTransactionAsync).
        /// Tự động tham gia giao dịch hiện hành nếu đã mở, hoặc khởi tạo giao dịch mới và Commit/Rollback.
        /// </summary>
        protected async Task ThucThiTrongGiaoDichAsync(Func<Task> tacVu)
        {
            var giaoDichHienTai = _context.Database.CurrentTransaction;
            if (giaoDichHienTai != null)
            {
                await tacVu();
                return;
            }

            using var giaoDichMoi = await _context.Database.BeginTransactionAsync();
            try
            {
                await tacVu();
                await giaoDichMoi.CommitAsync();
            }
            catch
            {
                await giaoDichMoi.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Thực thi một tác vụ bất đồng bộ có trả về kết quả bên trong giao dịch CSDL (BeginTransactionAsync).
        /// </summary>
        protected async Task<TKetQua> ThucThiTrongGiaoDichAsync<TKetQua>(Func<Task<TKetQua>> tacVu)
        {
            var giaoDichHienTai = _context.Database.CurrentTransaction;
            if (giaoDichHienTai != null)
            {
                return await tacVu();
            }

            using var giaoDichMoi = await _context.Database.BeginTransactionAsync();
            try
            {
                TKetQua ketQua = await tacVu();
                await giaoDichMoi.CommitAsync();
                return ketQua;
            }
            catch
            {
                await giaoDichMoi.RollbackAsync();
                throw;
            }
        }
    }
}
