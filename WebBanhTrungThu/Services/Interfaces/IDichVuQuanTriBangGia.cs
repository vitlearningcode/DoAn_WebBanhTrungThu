using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuQuanTriBangGia : IDichVuCoSo
    {
        Task<ThietLapGiaAdminViewModel> XayDungThietLapGiaAsync();
        Task<string?> CapNhatGiaBanhNhanhAsync(CapNhatBanhNhanhRequest req);
        Task<(bool HopLe, string ThongBao)> ThemVoHopMoiAsync(
            string? maCodeVh,
            string tenVoHop,
            int sucChuaBanh,
            string? kieuBoCucLuoi,
            decimal giaVoHopRieng,
            int slTonKhoVo,
            int slBanhThuongHangToiThieu,
            string? moTaChatLieu);
        Task<string?> CapNhatGiaVoHopAsync(int maVh, decimal giaVoHopRieng, int slTonKhoVo, int slBanhVipToiThieu);
        Task<string?> XoaVoHopAsync(int maVh);
        Task<(bool HopLe, string ThongBao)> ThemMocB2BMoiAsync(
            string tenMocB2b,
            int slHopToiThieu,
            int? slHopToiDa,
            decimal phanTramChietKhau,
            decimal phiKhuonInLogo,
            bool mienPhiInLogo);
        Task<string?> CapNhatMocB2BAsync(
            int maMocCk,
            int? slHopToiThieu,
            int? slHopToiDa,
            decimal phanTramChietKhau,
            decimal phiKhuonInLogo,
            bool mienPhiInLogo);
        Task<string?> XoaMocB2BAsync(int maMocCk);
        Task<(bool HopLe, string ThongBao)> ThemComboGiaMoiAsync(
            string? maCodeCb,
            string tenCombo,
            int maVh,
            decimal giaNiemYet,
            decimal giaUuDaiCombo,
            string? thongDiepYngia);
        Task<string?> CapNhatGiaComboAsync(int maCb, decimal giaNiemYet, decimal giaUuDaiCombo);
        Task<string?> XoaComboGiaAsync(int maCb);
        Task<KhuyenMaiAdminViewModel> LayDanhSachKhuyenMaiAsync();
        Task<string> ThemVoucherMoiAsync(ThemVoucherMoiRequest req);
        Task<string?> CapNhatVoucherAsync(int maKm, int slToiDa, DateTime ngayKetThuc, bool choPhepKhachVangLai);
    }
}
