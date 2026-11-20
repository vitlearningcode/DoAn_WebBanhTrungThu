using Microsoft.AspNetCore.Http;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuKiemKho : IDichVuCoSo
    {
        Task<KiemKhoAdminViewModel> XayDungTrangKiemKhoAsync(
            string? tuKhoa,
            string locTrangThai = "ALL");

        Task<object?> LayChiTietPhieuKiemJsonAsync(int maPkk);

        Task<(byte[] NoiDung, string ContentType, string TenFile)> XuatFileMauKiemKhoAsync(string dinhDang = "xlsx");

        Task<KetQuaDocExcelKiemKhoViewModel> DocFileExcelKiemKhoAsync(IFormFile? fileExcel);

        Task<(bool ThanhCong, string ThongBao)> ImportExcelTaoPhieuKiemKhoAsync(
            IFormFile? fileExcel,
            string? ghiChu,
            bool canBangNgay,
            string nguoiKiem,
            int? maNdLap);

        Task<(bool ThanhCong, string ThongBao)> LuuPhieuKiemKhoAsync(
            LuuPhieuKiemKhoRequest req,
            string nguoiKiem,
            int? maNdLap);

        Task<(bool ThanhCong, string ThongBao)> CanBangPhieuKiemKhoAsync(
            int maPkk,
            string nguoiThucHien,
            int? maNdLap);

        Task<(bool ThanhCong, string ThongBao)> XoaHoacHuyPhieuKiemKhoAsync(int maPkk);
    }
}
