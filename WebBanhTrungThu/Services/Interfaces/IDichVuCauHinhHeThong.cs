using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuCauHinhHeThong : IDichVuCoSo
    {
        DuLieuMoRongHeThongRoot DocDuLieu();
        void LuuDuLieu(DuLieuMoRongHeThongRoot store);
        void GanMetadataChoBanh(SanPhamBanh banh, DuLieuMoRongHeThongRoot store);
        void GanMetadataChoCombo(ComboMauCoSan combo, DuLieuMoRongHeThongRoot store);
        void GanMetadataChoDonHang(DonHang dh, DuLieuMoRongHeThongRoot? store = null);
        ThongTinDonHangMoRong LayMetadataDonHang(int maDh);
        void CapNhatMetadataDonHang(int maDh, Action<ThongTinDonHangMoRong> capNhat);
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
