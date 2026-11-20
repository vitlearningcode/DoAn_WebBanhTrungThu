using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuBanHangPos : IDichVuCoSo
    {
        Task<BanHangPosViewModel> XayDungDuLieuManHinhPosAsync(string tenThuNgan, string vaiTro);
        Task<object> TraCuuHoiVienPosAsync(string? sdt);
        Task<object> TimKhachHangPosAsync(string? tuKhoa);
        Task<object> ThemKhachHangNhanhPosAsync(string hoTen, string sdt, string? email);
        Task<object> ThanhToanTaiQuayAsync(PosCheckoutRequest? req, bool dungDiemTichLuy = false, int? maNdLap = null);
    }
}
