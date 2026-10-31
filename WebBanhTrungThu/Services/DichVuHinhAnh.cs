using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.Services
{
    public class DichVuHinhAnh
    {
        private readonly CSDLBanhTrungThuContext _db;

        public DichVuHinhAnh(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lấy toàn bộ từ điển ảnh của danh sách Bánh/Vỏ hộp trong 1 lần truy vấn duy nhất (tránh lỗi N+1 Query)
        /// </summary>
        public async Task<Dictionary<string, string>> LayTuDienUrlAnhAsync()
        {
            var danhSachLK = await _db.LienKetHinhAnhs
                .Include(lk => lk.MaHaNavigation)
                .OrderBy(lk => lk.ThuTuHienThi)
                .ToListAsync();

            var ketQua = new Dictionary<string, string>();

            foreach (var lk in danhSachLK)
            {
                // Khóa tra cứu dạng: "BANH_1_TACH_NEN_SLOT" hoặc "VOHOP_1_KHAY_HOP_RONG"
                string key = $"{lk.LoaiDoiTuong}_{lk.MaDoiTuong}_{lk.VaiTroAnh}";
                if (!ketQua.ContainsKey(key) && lk.MaHaNavigation != null)
                {
                    ketQua[key] = lk.MaHaNavigation.UrlHienThi;
                }
            }

            return ketQua;
        }

        /// <summary>
        /// Hàm lấy nhanh URL ảnh từ Từ điển đã nạp (có sẵn ảnh Fallback nếu thiếu)
        /// </summary>
        public static string LayUrl(Dictionary<string, string>? tuDien, string loaiDoiTuong, int maDoiTuong, string vaiTroAnh)
        {
            if (tuDien != null)
            {
                string key = $"{loaiDoiTuong}_{maDoiTuong}_{vaiTroAnh}";
                if (tuDien.TryGetValue(key, out string? url) && !string.IsNullOrEmpty(url))
                {
                    return url;
                }

                // Nếu tìm ANH_CHINH chưa có thì lấy tạm ảnh TACH_NEN_SLOT của bánh đó
                string fallbackKey = $"{loaiDoiTuong}_{maDoiTuong}_TACH_NEN_SLOT";
                if (tuDien.TryGetValue(fallbackKey, out string? fallbackUrl))
                {
                    return fallbackUrl;
                }
            }

            return loaiDoiTuong == "VOHOP"
                ? "/images/default/vohop-mac-dinh.png"
                : "/images/default/banh-mac-dinh.png";
        }
    }
}