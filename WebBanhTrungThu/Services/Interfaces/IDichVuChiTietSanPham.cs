using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuChiTietSanPham : IDichVuCoSo
    {
        Task<ChiTietSanPhamViewModel?> LayChiTietSanPhamAsync(int maBanh);
    }
}
