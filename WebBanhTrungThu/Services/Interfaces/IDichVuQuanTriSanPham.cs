using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuQuanTriSanPham : IDichVuCoSo
    {
        Task<SanPhamAdminViewModel> XayDungDanhMucHangHoaAsync(
            string? tuKhoa, int? maDm, string? tenDm, string? locDacTinh, string? locTonKho,
            string? duKienHetHang, string? thoiGianTao, string? nhaCungCap, string? thuongHieu,
            string? viTriKho, string? loaiHang, string? banTrucTiep);
        Task<string> LuuHangHoaKiotAsync(LuuHangHoaKiotRequest req);
        Task<string?> TaoNhomHangNhanhAsync(string tenNhomHang, string? moTa);
        string? TaoThuongHieuNhanh(string tenThuongHieu, string? tenNhaCungCap);
        string? TaoViTriNhanh(string tenViTri);
        string? TaoNhaCungCapNhanh(string tenNhaCungCap);
        Task<object> TaoDanhMucNhanhAjaxAsync(string loaiDanhMuc, string tenMoi, string? moTaPhu);
        Task<string?> XoaHangHoaAsync(int idGoc, bool laCombo);
        Task<(byte[] NoiDung, string ContentType, string TenFile)> XuatExcelHangHoaAsync(string dinhDang = "xlsx");
        Task<(byte[] NoiDung, string ContentType, string TenFile)> TaoFileMauImportHangHoaAsync(string dinhDang = "xlsx");
        Task<KetQuaImportExcelSanPham> NhapExcelHangHoaAsync(IFormFile? fileExcel);
    }
}
