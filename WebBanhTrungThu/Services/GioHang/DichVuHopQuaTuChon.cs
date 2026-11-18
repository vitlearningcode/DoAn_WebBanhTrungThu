using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.GioHang
{
    /// <summary>
    /// Dịch vụ chuyên trách tạo/cập nhật hộp quà tự phối, tính mã băm cấu hình MaBamCauHinh
    /// và sao chép hộp quà khi đặt lại đơn hàng. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuHopQuaTuChon : DichVuCoSo
    {
        public DichVuHopQuaTuChon(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        /// <summary>
        /// Tính mã băm cấu hình chuẩn cho hộp quà tự phối: "VH" + MaVh + "_B" + string.Join("-B", DanhSachMaBanh).
        /// </summary>
        public string TinhMaBamCauHinh(int maVh, IEnumerable<int> danhSachMaBanh)
        {
            return "VH" + maVh + "_B" + string.Join("-B", danhSachMaBanh);
        }

        /// <summary>
        /// Thêm hoặc cập nhật hộp quà tự chọn vào giỏ hàng hiện tại.
        /// </summary>
        public async Task<object> ThemHoacCapNhatHopTuChonAsync(
            Models.Entities.GioHang gioHang,
            int? maNd,
            string? maPhienKvl,
            LuuHopTuChonRequest? req)
        {
            if (req == null || req.DanhSachMaBanh == null || !req.DanhSachMaBanh.Any())
            {
                return new { thanhCong = false, thongBao = "Dữ liệu cấu hình hộp quà không hợp lệ!" };
            }

            var voHop = await _db.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == req.MaVh && v.TrangThai);
            if (voHop == null)
            {
                return new { thanhCong = false, thongBao = "Mẫu vỏ hộp không tồn tại hoặc đã ngừng kinh doanh!" };
            }

            if (req.DanhSachMaBanh.Count != voHop.SucChuaBanh)
            {
                return new
                {
                    thanhCong = false,
                    thongBao = $"Vỏ hộp '{voHop.TenVoHop}' yêu cầu xếp đủ {voHop.SucChuaBanh} bánh (hiện mới có {req.DanhSachMaBanh.Count} bánh)!"
                };
            }

            var maBanhPhanBiet = req.DanhSachMaBanh.Distinct().ToList();
            var tuDienBanh = await _db.SanPhamBanhs
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .Where(b => maBanhPhanBiet.Contains(b.MaBanh) && b.TrangThaiBan)
                .ToDictionaryAsync(b => b.MaBanh);

            if (req.DanhSachMaBanh.Any(id => !tuDienBanh.ContainsKey(id)))
            {
                return new { thanhCong = false, thongBao = "Có vị bánh trong khay không tồn tại hoặc đã ngừng bán!" };
            }

            int soBanhVip = req.DanhSachMaBanh.Count(id => tuDienBanh[id].LaBanhThuongHang);
            if (soBanhVip < voHop.SlbanhThuongHangToiThieu)
            {
                return new
                {
                    thanhCong = false,
                    thongBao = $"Mẫu vỏ '{voHop.TenVoHop}' yêu cầu tối thiểu {voHop.SlbanhThuongHangToiThieu} bánh Thượng Hạng (VIP)!"
                };
            }

            int tongCaloHop = req.DanhSachMaBanh.Sum(id => tuDienBanh[id].ThongSoDinhDuong?.NangLuongCalo ?? 400);
            decimal tongDonGiaCacBanh = req.DanhSachMaBanh.Sum(id => tuDienBanh[id].DonGiaThucTe);
            decimal tongGiaMotHop = Math.Round((voHop.GiaVoHopRieng * 0.95m) + tongDonGiaCacBanh, 0);

            string maBamCauHinh = TinhMaBamCauHinh(req.MaVh, req.DanhSachMaBanh);
            int soLuongThem = req.SoLuongHop > 0 ? req.SoLuongHop : 1;
            string tenHop = ChuanHoaChuoi(req.TenHopKhachDat, "Hộp Quà Đoàn Viên Tự Phối");

            var dongGioDaCo = gioHang.ChiTietGioHangs.FirstOrDefault(ct =>
                ct.LoaiSanPham == 3 &&
                ct.MaHopTcNavigation != null &&
                ct.MaHopTcNavigation.MaBamCauHinh == maBamCauHinh);

            if (dongGioDaCo != null)
            {
                dongGioDaCo.SoLuong += soLuongThem;
                dongGioDaCo.DonGiaTamTinh = tongGiaMotHop;
                if (dongGioDaCo.MaHopTcNavigation != null && !string.IsNullOrWhiteSpace(req.TenHopKhachDat))
                {
                    dongGioDaCo.MaHopTcNavigation.TenHopKhachDat = tenHop;
                }
            }
            else
            {
                var hopMoi = new HopQuaTuChon
                {
                    MaVh = voHop.MaVh,
                    MaNd = maNd,
                    MaPhienKvl = maNd.HasValue ? null : maPhienKvl,
                    TenHopKhachDat = tenHop,
                    MaBamCauHinh = maBamCauHinh,
                    TongCaloHop = tongCaloHop,
                    TongGiaMotHop = tongGiaMotHop,
                    DaLuuMauYeuThich = false,
                    NgayTao = LayThoiGianHeThong()
                };

                _db.HopQuaTuChons.Add(hopMoi);
                await _db.SaveChangesAsync();

                for (int i = 0; i < req.DanhSachMaBanh.Count; i++)
                {
                    int maBanhO = req.DanhSachMaBanh[i];
                    _db.ChiTietHopTuChons.Add(new ChiTietHopTuChon
                    {
                        MaHopTc = hopMoi.MaHopTc,
                        MaBanh = maBanhO,
                        ViTriOtrongHop = i + 1,
                        DonGiaBanhLucChon = tuDienBanh[maBanhO].DonGiaThucTe
                    });
                }

                _db.ChiTietGioHangs.Add(new ChiTietGioHang
                {
                    MaGh = gioHang.MaGh,
                    LoaiSanPham = 3,
                    MaHopTc = hopMoi.MaHopTc,
                    SoLuong = soLuongThem,
                    DonGiaTamTinh = tongGiaMotHop,
                    NgayThem = LayThoiGianHeThong()
                });
            }

            gioHang.NgayCapNhat = LayThoiGianHeThong();
            await _db.SaveChangesAsync();

            return new
            {
                thanhCong = true,
                thongBao = $"Đã lưu \"{tenHop}\" ({maBamCauHinh}) vào Giỏ Quà thành công!",
                maBamCauHinh,
                donGiaHop = tongGiaMotHop
            };
        }

        /// <summary>
        /// Sao chép bản ghi HopQuaTuChon cùng danh sách ChiTietHopTuChon khi khách đặt lại đơn hàng cũ.
        /// </summary>
        public async Task<HopQuaTuChon?> SaoChepHopQuaTuChonAsync(int maHopTcGoc, int? maNd, string? maPhienKvl)
        {
            var hopCu = await _db.HopQuaTuChons
                .AsNoTracking()
                .Include(h => h.ChiTietHopTuChons)
                .FirstOrDefaultAsync(h => h.MaHopTc == maHopTcGoc);

            if (hopCu == null) return null;

            var hopMoi = new HopQuaTuChon
            {
                MaVh = hopCu.MaVh,
                MaNd = maNd,
                MaPhienKvl = maNd.HasValue ? null : maPhienKvl,
                TenHopKhachDat = hopCu.TenHopKhachDat,
                MaBamCauHinh = hopCu.MaBamCauHinh,
                TongCaloHop = hopCu.TongCaloHop,
                TongGiaMotHop = hopCu.TongGiaMotHop,
                DaLuuMauYeuThich = false,
                NgayTao = LayThoiGianHeThong()
            };

            _db.HopQuaTuChons.Add(hopMoi);
            await _db.SaveChangesAsync();

            foreach (var oBanhCu in hopCu.ChiTietHopTuChons.OrderBy(o => o.ViTriOtrongHop))
            {
                _db.ChiTietHopTuChons.Add(new ChiTietHopTuChon
                {
                    MaHopTc = hopMoi.MaHopTc,
                    MaBanh = oBanhCu.MaBanh,
                    ViTriOtrongHop = oBanhCu.ViTriOtrongHop,
                    DonGiaBanhLucChon = oBanhCu.DonGiaBanhLucChon
                });
            }

            return hopMoi;
        }

        /// <summary>
        /// Dọn dẹp hộp quà tự chọn mồ côi khi xóa dòng chi tiết giỏ hàng.
        /// </summary>
        public async Task XoaDongChiTietVaHopMoCoiAsync(ChiTietGioHang dongGio)
        {
            int? maHopTcCanXoa = dongGio.LoaiSanPham == 3 ? dongGio.MaHopTc : null;
            int maCtghHienTai = dongGio.MaCtgh;

            _db.ChiTietGioHangs.Remove(dongGio);

            if (maHopTcCanXoa.HasValue)
            {
                bool daGanDonHang = await _db.ChiTietDonHangs.AnyAsync(d => d.MaHopTc == maHopTcCanXoa.Value);
                bool conTrongGioKhac = await _db.ChiTietGioHangs.AnyAsync(c => c.MaHopTc == maHopTcCanXoa.Value && c.MaCtgh != maCtghHienTai);

                if (!daGanDonHang && !conTrongGioKhac)
                {
                    var hopTuChon = await _db.HopQuaTuChons
                        .Include(h => h.ChiTietHopTuChons)
                        .FirstOrDefaultAsync(h => h.MaHopTc == maHopTcCanXoa.Value);

                    if (hopTuChon != null)
                    {
                        _db.ChiTietHopTuChons.RemoveRange(hopTuChon.ChiTietHopTuChons);
                        _db.HopQuaTuChons.Remove(hopTuChon);
                    }
                }
            }
        }

        /// <summary>
        /// Gộp giỏ hàng khách vãng lai (bao gồm hộp tự chọn trùng mã băm MaBamCauHinh) vào tài khoản thành viên.
        /// </summary>
        public async Task GopGioHangTuKhachVangLaiAsync(string? maPhienKVL, int maND)
        {
            if (string.IsNullOrWhiteSpace(maPhienKVL)) return;

            await ThucThiTrongGiaoDichAsync(async () =>
            {
                var gioHangGuest = await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation)
                    .FirstOrDefaultAsync(g => g.MaPhienKvl == maPhienKVL);

                if (gioHangGuest == null || !gioHangGuest.ChiTietGioHangs.Any())
                    return;

                var gioHangMember = await _db.GioHangs
                    .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.MaHopTcNavigation)
                    .FirstOrDefaultAsync(g => g.MaNd == maND);

                if (gioHangMember == null)
                {
                    gioHangGuest.MaNd = maND;
                    gioHangGuest.MaPhienKvl = null;
                    gioHangGuest.NgayCapNhat = LayThoiGianHeThong();

                    var danhSachHopGuest = await _db.HopQuaTuChons.Where(h => h.MaPhienKvl == maPhienKVL).ToListAsync();
                    foreach (var hop in danhSachHopGuest)
                    {
                        hop.MaNd = maND;
                        hop.MaPhienKvl = null;
                    }
                }
                else
                {
                    foreach (var monGuest in gioHangGuest.ChiTietGioHangs.ToList())
                    {
                        ChiTietGioHang? monTrungTrongMember = null;

                        if (monGuest.LoaiSanPham == 1)
                        {
                            monTrungTrongMember = gioHangMember.ChiTietGioHangs
                                .FirstOrDefault(m => m.LoaiSanPham == 1 && m.MaBanh == monGuest.MaBanh);
                        }
                        else if (monGuest.LoaiSanPham == 2)
                        {
                            monTrungTrongMember = gioHangMember.ChiTietGioHangs
                                .FirstOrDefault(m => m.LoaiSanPham == 2 && m.MaCb == monGuest.MaCb);
                        }
                        else if (monGuest.LoaiSanPham == 3 && monGuest.MaHopTcNavigation != null)
                        {
                            string maBamGuest = monGuest.MaHopTcNavigation.MaBamCauHinh;
                            monTrungTrongMember = gioHangMember.ChiTietGioHangs
                                .FirstOrDefault(m => m.LoaiSanPham == 3 && m.MaHopTcNavigation != null && m.MaHopTcNavigation.MaBamCauHinh == maBamGuest);
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
                    gioHangMember.NgayCapNhat = LayThoiGianHeThong();

                    var danhSachHopConLai = await _db.HopQuaTuChons.Where(h => h.MaPhienKvl == maPhienKVL).ToListAsync();
                    foreach (var hop in danhSachHopConLai)
                    {
                        hop.MaNd = maND;
                        hop.MaPhienKvl = null;
                    }

                    _db.GioHangs.Remove(gioHangGuest);
                }

                var phienKVL = await _db.PhienKhachVangLais.FindAsync(maPhienKVL);
                if (phienKVL != null)
                {
                    phienKVL.DaGopGioHang = true;
                    phienKVL.MaNdDaGop = maND;
                }

                await _db.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Sao chép toàn bộ sản phẩm (bánh lẻ, combo và hộp tự chọn) từ đơn hàng cũ vào giỏ hàng hiện tại.
        /// </summary>
        public async Task<object> SaoChepDonCuVaoGioHangAsync(Models.Entities.GioHang gioHang, int maDonHang, int? maNd)
        {
            if (maDonHang <= 0)
            {
                return new { thanhCong = false, thongBao = "Mã đơn hàng không hợp lệ!" };
            }

            var chiTietDonCu = await _db.ChiTietDonHangs
                .AsNoTracking()
                .Where(ct => ct.MaDh == maDonHang)
                .ToListAsync();

            if (!chiTietDonCu.Any())
            {
                return new { thanhCong = false, thongBao = "Không tìm thấy chi tiết sản phẩm của đơn hàng cũ!" };
            }

            string? maPhienKvl = maNd.HasValue ? null : gioHang.MaPhienKvl;

            foreach (var ct in chiTietDonCu)
            {
                int soLuongThem = Math.Max(1, ct.SoLuong);

                if (ct.LoaiSanPham == 1 && ct.MaBanh.HasValue)
                {
                    var banh = await _db.SanPhamBanhs.AsNoTracking().Include(b => b.LoSanXuatBanhs).FirstOrDefaultAsync(b => b.MaBanh == ct.MaBanh.Value);
                    decimal donGiaHienTai = banh != null ? Math.Round(banh.DonGiaThucTe, 0) : ct.DonGiaLucMua;
                    var dongGio = gioHang.ChiTietGioHangs.FirstOrDefault(x => x.LoaiSanPham == 1 && x.MaBanh == ct.MaBanh.Value);

                    if (dongGio != null)
                    {
                        dongGio.SoLuong += soLuongThem;
                        dongGio.DonGiaTamTinh = donGiaHienTai;
                        dongGio.NgayThem = LayThoiGianHeThong();
                    }
                    else
                    {
                        _db.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaGh = gioHang.MaGh,
                            LoaiSanPham = 1,
                            MaBanh = ct.MaBanh.Value,
                            SoLuong = soLuongThem,
                            DonGiaTamTinh = donGiaHienTai,
                            NgayThem = LayThoiGianHeThong()
                        });
                    }
                }
                else if (ct.LoaiSanPham == 2 && ct.MaCb.HasValue)
                {
                    var combo = await _db.ComboMauCoSans.AsNoTracking().FirstOrDefaultAsync(c => c.MaCb == ct.MaCb.Value);
                    decimal donGiaCombo = combo != null ? combo.GiaUuDaiCombo : ct.DonGiaLucMua;
                    var dongGio = gioHang.ChiTietGioHangs.FirstOrDefault(x => x.LoaiSanPham == 2 && x.MaCb == ct.MaCb.Value);

                    if (dongGio != null)
                    {
                        dongGio.SoLuong += soLuongThem;
                        dongGio.DonGiaTamTinh = donGiaCombo;
                        dongGio.NgayThem = LayThoiGianHeThong();
                    }
                    else
                    {
                        _db.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaGh = gioHang.MaGh,
                            LoaiSanPham = 2,
                            MaCb = ct.MaCb.Value,
                            SoLuong = soLuongThem,
                            DonGiaTamTinh = donGiaCombo,
                            NgayThem = LayThoiGianHeThong()
                        });
                    }
                }
                else if (ct.LoaiSanPham == 3 && ct.MaHopTc.HasValue)
                {
                    var hopMoi = await SaoChepHopQuaTuChonAsync(ct.MaHopTc.Value, maNd, maPhienKvl);
                    if (hopMoi != null)
                    {
                        _db.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaGh = gioHang.MaGh,
                            LoaiSanPham = 3,
                            MaHopTc = hopMoi.MaHopTc,
                            SoLuong = soLuongThem,
                            DonGiaTamTinh = hopMoi.TongGiaMotHop,
                            NgayThem = LayThoiGianHeThong()
                        });
                    }
                }
            }

            gioHang.NgayCapNhat = LayThoiGianHeThong();
            await _db.SaveChangesAsync();

            return new
            {
                thanhCong = true,
                thongBao = "Đã thêm toàn bộ sản phẩm của đơn cũ vào giỏ quà!"
            };
        }
    }
}
