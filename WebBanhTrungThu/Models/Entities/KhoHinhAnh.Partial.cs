using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace WebBanhTrungThu.Models.Entities
{
    public partial class KhoHinhAnh
    {
        /// <summary>
        /// Thuộc tính tự động trả về đường dẫn ảnh hiển thị chuẩn xác cho thẻ <img src="@anh.UrlHienThi" />
        /// Hỗ trợ cả 3 nguồn: 1 = WebRoot (/images/...), 2 = Ổ đĩa máy (/kho-anh-may/...), 3 = Google Drive
        /// </summary>
        [NotMapped]
        public string UrlHienThi
        {
            get
            {
                // Nguồn 3: Google Drive (Ưu tiên render qua CDN lh3.googleusercontent.com tốc độ cao)
                if (LoaiLuuTru == 3)
                {
                    string? driveId = LayMaFileDriveTuDong();
                    bool laIdGiaLap = string.IsNullOrEmpty(driveId)
                                      || driveId.Contains("1AbCdEfGhIjKlMnOpQrStUvWxYz123456", StringComparison.OrdinalIgnoreCase);

                    if (!laIdGiaLap)
                    {
                        return $"https://lh3.googleusercontent.com/d/{driveId}";
                    }

                    // Nếu mã Drive là dữ liệu mẫu giả lập hoặc không hợp lệ, ưu tiên trả về DuongDanCucBo
                    if (!string.IsNullOrWhiteSpace(DuongDanCucBo))
                    {
                        return DuongDanCucBo;
                    }

                    return "/images/banh/fallback-lava.png";
                }

                // Nguồn 1 (wwwroot) hoặc Nguồn 2 (Ổ cứng đã map qua /kho-anh-may)
                if (!string.IsNullOrWhiteSpace(DuongDanCucBo))
                {
                    return DuongDanCucBo;
                }

                // Dự phòng nếu nhập link ảnh trực tiếp vào DuongDanDamMay
                if (!string.IsNullOrWhiteSpace(DuongDanDamMay))
                {
                    return DuongDanDamMay;
                }

                // Fallback về ảnh mặc định nếu sản phẩm chưa có ảnh
                return "/images/default/banh-mac-dinh.png";
            }
        }

        private string? LayMaFileDriveTuDong()
        {
            if (!string.IsNullOrWhiteSpace(MaFileDrive))
                return MaFileDrive.Trim();

            if (string.IsNullOrWhiteSpace(DuongDanDamMay))
                return null;

            // Tự động bóc tách ID từ link chia sẻ Google Drive bất kỳ
            var match = Regex.Match(DuongDanDamMay, @"(?:\/d\/|id=)([a-zA-Z0-9_-]{20,})");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}