using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;

namespace WebBanhTrungThu.Services
{
    public class DichVuGioHang
    {
        private readonly CSDLBanhTrungThuContext _db;

        public DichVuGioHang(CSDLBanhTrungThuContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Thuật toán hợp nhất giỏ hàng từ Phiên Khách Vãng Lai (Guest) vào Tài Khoản Thành Viên (Member)
        /// </summary>
        public async Task GopGioHangAsync(string? maPhienKVL, int maND)
        {
            if (string.IsNullOrWhiteSpace(maPhienKVL)) return;

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy giỏ hàng của Guest (kèm thông tin Hộp tự chọn qua MaHopTcNavigation)
                var gioHangGuest = await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                    .FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKVL);

                if (gioHangGuest == null || !gioHangGuest.ChiTietGioHangs.Any())
                    return;

                // 2. Lấy giỏ hàng của Member
                var gioHangMember = await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                        .ThenInclude(ct => ct.MaHopTcNavigation)
                    .FirstOrDefaultAsync(g => g.MaNd == maND);

                if (gioHangMember == null)
                {
                    // Chuyển thẳng giỏ của Guest thành giỏ của Member
                    gioHangGuest.MaNd = maND;
                    gioHangGuest.MaPhienKvl = null;
                    gioHangGuest.NgayCapNhat = DateTime.Now;

                    var danhSachHopGuest = await _db.HopQuaTuChons
                        .Where(h => h.MaPhienKvl == maPhienKVL)
                        .ToListAsync();

                    foreach (var hop in danhSachHopGuest)
                    {
                        hop.MaNd = maND;
                        hop.MaPhienKvl = null;
                    }
                }
                else
                {
                    // 3. Hợp nhất từng món từ giỏ Guest sang giỏ Member
                    foreach (var monGuest in gioHangGuest.ChiTietGioHangs.ToList())
                    {
                        ChiTietGioHang? monTrungTrongMember = null;

                        if (monGuest.LoaiSanPham == 1) // Bánh lẻ
                        {
                            monTrungTrongMember = gioHangMember.ChiTietGioHangs
                                .FirstOrDefault(m => m.LoaiSanPham == 1 && m.MaBanh == monGuest.MaBanh);
                        }
                        else if (monGuest.LoaiSanPham == 2) // Combo mẫu
                        {
                            monTrungTrongMember = gioHangMember.ChiTietGioHangs
                                .FirstOrDefault(m => m.LoaiSanPham == 2 && m.MaCb == monGuest.MaCb);
                        }
                        else if (monGuest.LoaiSanPham == 3 && monGuest.MaHopTcNavigation != null) // Hộp tự chọn
                        {
                            string maBamGuest = monGuest.MaHopTcNavigation.MaBamCauHinh;
                            monTrungTrongMember = gioHangMember.ChiTietGioHangs
                                .FirstOrDefault(m => m.LoaiSanPham == 3
                                                  && m.MaHopTcNavigation != null
                                                  && m.MaHopTcNavigation.MaBamCauHinh == maBamGuest);
                        }

                        if (monTrungTrongMember != null)
                        {
                            monTrungTrongMember.SoLuong += monGuest.SoLuong;
                            _db.ChiTietGioHangs.Remove(monGuest);
                        }
                        else
                        {
                            monGuest.MaGh = gioHangMember.MaGh;
                            if (monGuest.LoaiSanPham == 3 && monGuest.MaHopTcNavigation != null)
                            {
                                monGuest.MaHopTcNavigation.MaNd = maND;
                                monGuest.MaHopTcNavigation.MaPhienKvl = null;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(gioHangGuest.CodeKmdangApDung))
                    {
                        gioHangMember.CodeKmdangApDung = gioHangGuest.CodeKmdangApDung;
                    }
                    gioHangMember.NgayCapNhat = DateTime.Now;

                    _db.GioHangs.Remove(gioHangGuest);
                }

                // 4. Đánh dấu phiên Guest đã gộp thành công
                var phienKVL = await _db.PhienKhachVangLais.FindAsync(maPhienKVL);
                if (phienKVL != null)
                {
                    phienKVL.DaGopGioHang = true;
                    phienKVL.MaNdDaGop = maND;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}