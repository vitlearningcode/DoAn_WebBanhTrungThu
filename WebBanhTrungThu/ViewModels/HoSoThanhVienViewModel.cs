using System.Text.Json;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    /// <summary>
    /// Mô hình hiển thị từng ô bánh bên trong hộp quà đã đặt trên trang Hồ sơ thành viên.
    /// </summary>
    public class ThanhPhanOTrongHopViewModel
    {
        public int ViTriOTrongHop { get; set; }
        public string TenBanh { get; set; } = "Bánh Trung Thu";
    }

    /// <summary>
    /// ViewModel chuẩn hóa cho trang Hồ sơ thành viên (/TaiKhoan/HoSo),
    /// tính toán sẵn toàn bộ chỉ số hạng VIP, tổng chi tiêu tích lũy và các hàm ánh xạ nhãn trạng thái Tiếng Việt.
    /// </summary>
    public class HoSoThanhVienViewModel
    {
        public NguoiDung NguoiDung { get; set; } = null!;
        public List<SoDiaChi> DanhSachSoDiaChi { get; set; } = new();
        public List<DonHang> LichSuDonHang { get; set; } = new();
        public List<HopQuaTuChon> DanhSachHopTuChon { get; set; } = new();

        public string TabMacDinh { get; set; } = "DON_HANG";
        public string ChuCaiDauTen { get; set; } = "N";

        // Các thuộc tính thống kê & tiến trình hạng VIP tính sẵn cho View
        public string TenHangHienTai { get; set; } = "Đồng";
        public string TenHangTiepTheo { get; set; } = "Hạng Bạc (100 điểm - Ưu đãi giảm 3%)";
        public int DiemCanThangHang { get; set; }
        public int PhanTramTienTrinhVip { get; set; }
        public bool DaDatDinhVip { get; set; }

        public int TongSoDonHang { get; set; }
        public int TongSoDiaChi { get; set; }
        public int TongSoHopTuChon { get; set; }
        public decimal TongChiTieuTichLuy { get; set; }

        public decimal GiaTriQuyDoiDiem => (NguoiDung?.DiemTichLuy ?? 0) * 1000m;

        /// <summary>
        /// Tính toán sẵn toàn bộ các chỉ số thống kê hội viên, tiến trình VIP và tab mặc định trước khi truyền sang View.
        /// </summary>
        public void TinhToanChiSoHoiVien(string? tabDangChon = null)
        {
            string tabChuan = (tabDangChon ?? TabMacDinh ?? "DON_HANG").Trim().ToUpperInvariant();
            if (tabChuan != "DON_HANG" && tabChuan != "DIA_CHI" && tabChuan != "HOP_TU_CHON")
            {
                tabChuan = "DON_HANG";
            }
            TabMacDinh = tabChuan;

            string hoTen = (NguoiDung?.HoTen ?? string.Empty).Trim();
            string tenGoi = hoTen.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "N";
            ChuCaiDauTen = tenGoi.Length > 0 ? tenGoi.Substring(0, 1).ToUpperInvariant() : "N";

            TongSoDonHang = LichSuDonHang?.Count ?? 0;
            TongSoDiaChi = DanhSachSoDiaChi?.Count ?? 0;
            TongSoHopTuChon = DanhSachHopTuChon?.Count ?? 0;

            TongChiTieuTichLuy = (LichSuDonHang ?? Enumerable.Empty<DonHang>())
                .Where(d => !string.Equals(d.TrangThaiDh, "DA_HUY", StringComparison.OrdinalIgnoreCase))
                .Sum(d => d.TongThanhToanCuoi);

            var (hangVipChuan, _, diemXetHang) = Services.TaiKhoan.DichVuHoiVien.XacDinhHangVipTheoChiTieu(
                NguoiDung?.DiemTichLuy ?? 0,
                TongChiTieuTichLuy);
            TenHangHienTai = hangVipChuan;

            if (diemXetHang < 100)
            {
                TenHangTiepTheo = "Hạng Bạc (100 điểm - Ưu đãi giảm 3%)";
                DiemCanThangHang = 100 - diemXetHang;
                PhanTramTienTrinhVip = Math.Clamp((int)Math.Round(diemXetHang * 100.0 / 100.0), 4, 99);
                DaDatDinhVip = false;
            }
            else if (diemXetHang < 300)
            {
                TenHangTiepTheo = "Hạng Vàng (300 điểm - Ưu đãi giảm 5%)";
                DiemCanThangHang = 300 - diemXetHang;
                PhanTramTienTrinhVip = Math.Clamp((int)Math.Round((diemXetHang - 100) * 100.0 / 200.0), 6, 99);
                DaDatDinhVip = false;
            }
            else if (diemXetHang < 600)
            {
                TenHangTiepTheo = "Hạng Kim Cương (600 điểm - Ưu đãi giảm 10% + Quà VIP)";
                DiemCanThangHang = 600 - diemXetHang;
                PhanTramTienTrinhVip = Math.Clamp((int)Math.Round((diemXetHang - 300) * 100.0 / 300.0), 6, 99);
                DaDatDinhVip = false;
            }
            else
            {
                TenHangTiepTheo = "Hạng Kim Cương Hoàng Gia (100%)";
                DiemCanThangHang = 0;
                PhanTramTienTrinhVip = 100;
                DaDatDinhVip = true;
            }
        }

        /// <summary>
        /// Ánh xạ mã trạng thái đơn hàng hoặc đợt giao sang nhãn Tiếng Việt lịch sự kèm lớp CSS huy hiệu.
        /// </summary>
        public (string TenHienThi, string LopHuyHieu) AnhXaTrangThaiDonHang(string? maTrangThai)
        {
            switch ((maTrangThai ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "CHO_XAC_NHAN":
                case "CHO_DUYET":
                    return ("Chờ xác nhận", "huy-hieu-cho-xac-nhan");
                case "DA_XAC_NHAN":
                case "CHO_DONG_GOI":
                case "DANG_DONG_GOI_HOP":
                    return ("Đang chuẩn bị lễ hộp", "huy-hieu-dang-chuan-bi");
                case "CHO_LAY_HANG":
                    return ("Chờ bàn giao vận chuyển", "huy-hieu-cho-ban-giao");
                case "DANG_GIAO":
                case "DANG_VAN_CHUYEN":
                    return ("Đang giao quà", "huy-hieu-dang-giao");
                case "DA_GIAO":
                case "DA_GIAO_THANH_CONG":
                case "HOAN_TAT":
                    return ("Giao thành công", "huy-hieu-giao-thanh-cong");
                case "DA_HUY":
                    return ("Đã hủy", "huy-hieu-da-huy");
                default:
                    return ("Đang xử lý", "huy-hieu-cho-xac-nhan");
            }
        }

        public string LayNhanTrangThaiDonHang(string? maTrangThai) => AnhXaTrangThaiDonHang(maTrangThai).TenHienThi;

        public string LayLopHuyHieuTrangThai(string? maTrangThai) => AnhXaTrangThaiDonHang(maTrangThai).LopHuyHieu;

        /// <summary>
        /// Hiển thị nhãn hình thức thanh toán Tiếng Việt thân thiện.
        /// </summary>
        public string AnhXaHinhThucThanhToan(string? maPhuongThuc)
        {
            return (maPhuongThuc ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "COD" => "Thanh toán khi nhận (COD)",
                "TIEN_MAT" => "Tiền mặt tại quầy",
                "MOMO_QR" or "MOMO" => "Ví MoMo QR",
                "VI_DIEN_TU" or "THE_NGAN_HANG" => "Thẻ / Ví điện tử",
                _ => "Chuyển khoản QR"
            };
        }

        /// <summary>
        /// Xác định bước tiến trình 4 bước của đơn hàng (0 = Đã hủy, 1..4 = Các bước hoàn thành).
        /// </summary>
        public int XacDinhBuocTienTrinhDon(string? maTrangThai)
        {
            switch ((maTrangThai ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "DA_HUY":
                    return 0;
                case "CHO_XAC_NHAN":
                case "CHO_DUYET":
                    return 1;
                case "DA_XAC_NHAN":
                case "CHO_DONG_GOI":
                case "DANG_DONG_GOI_HOP":
                case "CHO_LAY_HANG":
                    return 2;
                case "DANG_GIAO":
                    return 3;
                case "DA_GIAO":
                case "HOAN_TAT":
                    return 4;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Phân tích chuỗi JSON cấu hình hộp quà tự phối ra danh sách từng ô bánh đã sắp xếp theo vị trí ô.
        /// </summary>
        public List<ThanhPhanOTrongHopViewModel> PhanTichThanhPhanHop(string? cauHinhHopJson)
        {
            var danhSachO = new List<ThanhPhanOTrongHopViewModel>();
            if (string.IsNullOrWhiteSpace(cauHinhHopJson))
            {
                return danhSachO;
            }

            try
            {
                using var taiLieuJson = JsonDocument.Parse(cauHinhHopJson);
                if (taiLieuJson.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return danhSachO;
                }

                foreach (var phanTu in taiLieuJson.RootElement.EnumerateArray())
                {
                    int viTri = 1;
                    if (phanTu.TryGetProperty("ViTriOTrongHop", out var propViTri) && propViTri.TryGetInt32(out int vt))
                    {
                        viTri = vt;
                    }

                    string tenBanh = "Bánh Trung Thu";
                    if (phanTu.TryGetProperty("TenBanh", out var propTen) && propTen.ValueKind == JsonValueKind.String)
                    {
                        tenBanh = propTen.GetString() ?? "Bánh Trung Thu";
                    }

                    danhSachO.Add(new ThanhPhanOTrongHopViewModel
                    {
                        ViTriOTrongHop = viTri,
                        TenBanh = tenBanh
                    });
                }
            }
            catch
            {
                return new List<ThanhPhanOTrongHopViewModel>();
            }

            return danhSachO.OrderBy(o => o.ViTriOTrongHop).ToList();
        }
    }
}
