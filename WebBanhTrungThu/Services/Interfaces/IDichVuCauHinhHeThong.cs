using WebBanhTrungThu.Services.CoSo;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuCauHinhHeThong : IDichVuCoSo
    {
        CauHinhThanhToanQr LayCauHinhQr();
        CauHinhThanhToanQr LayCauHinhThanhToan();
        void CapNhatCauHinhQr(Action<CauHinhThanhToanQr> capNhat, int? maNdCapNhat = null);
        CauHinhThanhToanQr CapNhatCauHinhQr(string? soTaiKhoanMoMo, string? tenChuTaiKhoan, string? tenNganHangVi, string? maBinNganHang = null, string? cuPhap = null);
        void GhiNhanPhieuThuChi(PhieuThuChiMoRong phieu);
        void GhiPhieuThuChi(PhieuThuChiMoRong phieu);
        ThongTinThiep3DMeta? LayThongTinThiepTheoToken(string? maToken);
        ThongTinThiep3DMeta? LayThongTinThiepTheoToken(int maDh, string? maToken);
    }
}
