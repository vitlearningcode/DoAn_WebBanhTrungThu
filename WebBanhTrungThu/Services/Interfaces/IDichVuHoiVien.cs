using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.Interfaces
{
    public interface IDichVuHoiVien : IDichVuCoSo
    {
        Task<(bool HopLe, string ThongBao, NguoiDung? NguoiDung)> XacThucDangNhapAsync(string? tenDangNhap, string? matKhau);
        Task<NguoiDung?> LayTaiKhoanChuyenVaiTroDemoAsync(string maVt);
        Task<(bool HopLe, string ThongBao, NguoiDung? NguoiDung)> DangKyHoiVienMoiAsync(
            string? hoTen,
            string? sdt,
            string? email,
            string? matKhau);
        Task<HoSoThanhVienViewModel?> XayDungHoSoThanhVienAsync(int maNd, string? tab);
        Task<string> ThemDiaChiMoiAsync(
            int maNd,
            string? tenNguoiNhan,
            string? sdtNguoiNhan,
            string? tinhThanh,
            string? quanHuyen,
            string? phuongXa,
            string? diaChiCuThe,
            string? nhanDiaChi,
            bool laMacDinh);
        Task<(bool ThanhCong, string ThongBao)> CapNhatDiaChiAsync(
            int maNd,
            int maDiaChi,
            string? nhanGoiNho,
            string? nguoiNhan,
            string? sdtNhan,
            string? diaChiChiTiet,
            bool laMacDinh,
            string? tinhThanh = null,
            string? quanHuyen = null,
            string? phuongXa = null);
        Task<(bool ThanhCong, string ThongBao)> DatDiaChiMacDinhAsync(int maNd, int maDiaChi);
        Task<string?> XoaDiaChiAsync(int maNd, int maDc);
        Task<string> DatLaiHopTuChonVaoGioAsync(int maNd, int maHopTc);
        Task DamBaoVaiTroVaTaiKhoanDemoAsync();
    }
}
