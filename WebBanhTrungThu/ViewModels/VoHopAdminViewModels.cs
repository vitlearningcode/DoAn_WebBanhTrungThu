using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.ViewModels
{
    public class MauVoHopAdminItemViewModel
    {
        public int MaVh { get; set; }
        public string MaCodeVh { get; set; } = string.Empty;
        public string TenVoHop { get; set; } = string.Empty;
        public int SucChuaBanh { get; set; }
        public decimal GiaVoHopRieng { get; set; }
        public int SlbanhThuongHangToiThieu { get; set; }
        public string KieuBoCucLuoi { get; set; } = "grid-2x2";
        public bool KemTraOlong { get; set; }
        public int SltonKhoVo { get; set; }
        public string? MoTaChatLieu { get; set; }
        public bool TrangThai { get; set; }
        public int? MaAnhChinh { get; set; }
        public string UrlAnh { get; set; } = "/images/default/vohop-mac-dinh.png";
        public int SoComboLienKet { get; set; }
        public int SoGiaoDichKho { get; set; }
    }

    public class VoHopAdminIndexViewModel
    {
        public int TongMauVoHop { get; set; }
        public int TongTonKhoVo { get; set; }
        public int SoMauSapHet { get; set; }
        public decimal TongGiaTriNhapVoHop { get; set; }

        public string? TuKhoa { get; set; }
        public int? SucChuaLoc { get; set; }
        public string TrangThaiTonLoc { get; set; } = "ALL";

        public List<MauVoHopAdminItemViewModel> DanhSachVoHop { get; set; } = new();
        public List<PhieuKhoVoHop> LichSuKhoVoHop { get; set; } = new();
        public List<NhaCungCap> DanhSachNhaCungCap { get; set; } = new();
        public List<KhoHinhAnh> DanhSachHinhAnh { get; set; } = new();
    }

    public class LuuMauVoHopRequest
    {
        public int MaVh { get; set; }
        public string? MaCodeVh { get; set; }
        public string TenVoHop { get; set; } = string.Empty;
        public int SucChuaBanh { get; set; } = 4;
        public decimal GiaVoHopRieng { get; set; }
        public int SlbanhThuongHangToiThieu { get; set; }
        public string KieuBoCucLuoi { get; set; } = "grid-2x2";
        public bool KemTraOlong { get; set; }
        public string? MoTaChatLieu { get; set; }
        public int? MaAnhChinh { get; set; }
        public IFormFile? FileAnhTaiLen { get; set; }
        public string? UrlAnhNhapTay { get; set; }
        public bool TrangThai { get; set; } = true;
    }

    public class NhapXuatVoHopRequest
    {
        public int MaVh { get; set; }
        public string LoaiGiaoDich { get; set; } = "NHAP_VO_HOP";
        public int SoLuong { get; set; }
        public decimal DonGiaNhap { get; set; }
        public int? MaNcc { get; set; }
        public bool ThanhToanNgay { get; set; }
        public string PhuongThucTt { get; set; } = "CHUYEN_KHOAN";
        public string? GhiChu { get; set; }
    }
}
