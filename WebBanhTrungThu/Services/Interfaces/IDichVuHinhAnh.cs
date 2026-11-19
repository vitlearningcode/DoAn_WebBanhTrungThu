using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuHinhAnh : IDichVuCoSo
    {
        Task<KhoAnhAdminViewModel> LayDuLieuKhoAnhAdminAsync();
        Task<string> ThemAnhDaNguonAsync(ThemAnhDaNguonRequest req, string webRootPath);
        Task<Dictionary<string, string>> LayTuDienUrlAnhAsync();
    }
}
