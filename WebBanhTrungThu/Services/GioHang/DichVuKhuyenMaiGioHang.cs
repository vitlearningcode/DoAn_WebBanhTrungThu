using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.GioHang
{
    /// <summary>
    /// Dịch vụ chuyên trách kiểm tra điều kiện mã giảm giá, tính chiết khấu sỉ B2B
    /// và tổng kết tiền giỏ hàng chuẩn xác từ CSDL. Kế thừa từ DichVuCoSo.
    /// </summary>
    public class DichVuKhuyenMaiGioHang : DichVuCoSo
    {
        public DichVuKhuyenMaiGioHang(CSDLBanhTrungThuContext db) : base(db)
        {
        }

        /// <summary>
        /// Tính lại đơn giá thực tế của từng dòng giỏ hàng trực tiếp từ CSDL (Anti-Tampering).
        /// </summary>
        public decimal TinhDonGiaThucTeTuCsdl(ChiTietGioHang ct)
        {
            if (ct.LoaiSanPham == 1 && ct.MaBanhNavigation != null)
            {
                return Math.Round(ct.MaBanhNavigation.DonGiaThucTe, 0);
            }
            if (ct.LoaiSanPham == 2 && ct.MaCbNavigation != null)
            {
                return Math.Round(ct.MaCbNavigation.GiaUuDaiCombo, 0);
            }
            if (ct.LoaiSanPham == 3 && ct.MaHopTcNavigation != null)
            {
                decimal giaVoHop = ct.MaHopTcNavigation.MaVhNavigation?.GiaVoHopRieng ?? 85000m;
                decimal tongGiaBanh = ct.MaHopTcNavigation.ChiTietHopTuChons.Sum(x =>
                    x.MaBanhNavigation != null ? x.MaBanhNavigation.DonGiaThucTe : x.DonGiaBanhLucChon);
                return Math.Round((giaVoHop * 0.95m) + tongGiaBanh, 0);
            }
            return Math.Round(ct.DonGiaTamTinh, 0);
        }

        /// <summary>
        /// Đảm bảo mã khuyến mãi mẫu TRANGTRON2026 luôn có hiệu lực đến năm 2027.
        /// </summary>
        public async Task DamBaoMaKhuyenMaiMauConHanAsync()
        {
            var mocHetHan = new DateTime(2027, 12, 31, 23, 59, 59);
            var kmTrangTron = await _db.MaKhuyenMais.FirstOrDefaultAsync(k => k.CodeKm == "TRANGTRON2026");
            if (kmTrangTron == null)
            {
                _db.MaKhuyenMais.Add(new MaKhuyenMai
                {
                    CodeKm = "TRANGTRON2026",
                    TenChuongTrinh = "Ưu Đãi Đêm Trăng Đoàn Viên (-10%)",
                    LoaiGiam = "PHAN_TRAM",
                    GiaTriGiam = 10m,
                    GiamToiDa = 150000m,
                    DonToiThieu = 200000m,
                    SltoiDa = 500,
                    SldaDung = 0,
                    ChoPhepKhachVangLai = true,
                    NgayBatDau = new DateTime(2026, 1, 1),
                    NgayKetThuc = mocHetHan
                });
                await _db.SaveChangesAsync();
            }
            else if (kmTrangTron.NgayKetThuc < mocHetHan || kmTrangTron.NgayBatDau > LayThoiGianHeThong())
            {
                if (kmTrangTron.NgayBatDau > LayThoiGianHeThong()) kmTrangTron.NgayBatDau = new DateTime(2026, 1, 1);
                if (kmTrangTron.NgayKetThuc < mocHetHan) kmTrangTron.NgayKetThuc = mocHetHan;
                if (kmTrangTron.SltoiDa <= kmTrangTron.SldaDung) kmTrangTron.SltoiDa = kmTrangTron.SldaDung + 100;
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Tính số tiền giảm giá của mã khuyến mãi dựa trên tổng tạm tính.
        /// </summary>
        public decimal TinhTienGiamVoucher(MaKhuyenMai km, decimal tongTamTinh)
        {
            if (tongTamTinh < km.DonToiThieu) return 0m;

            decimal tienGiam = km.LoaiGiam == "PHAN_TRAM"
                ? Math.Round(tongTamTinh * (km.GiaTriGiam / 100m), 0)
                : km.GiaTriGiam;

            if (km.GiamToiDa > 0 && tienGiam > km.GiamToiDa)
            {
                tienGiam = km.GiamToiDa;
            }

            return Math.Min(tienGiam, tongTamTinh);
        }

        /// <summary>
        /// Kiểm tra 4 điều kiện mã giảm giá và áp dụng vào giỏ hàng hiện tại.
        /// </summary>
        public async Task<object> ApDungVoucherChoGioHangAsync(
            Models.Entities.GioHang gioHang,
            int? maNd,
            ApDungVoucherRequest? req,
            string? codeKm)
        {
            await DamBaoMaKhuyenMaiMauConHanAsync();

            string maNhap = (req?.CodeKm ?? codeKm ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(maNhap))
            {
                gioHang.CodeKmdangApDung = null;
                gioHang.NgayCapNhat = LayThoiGianHeThong();
                await _db.SaveChangesAsync();
                return new { thanhCong = false, thongBao = "Vui lòng nhập mã khuyến mãi hợp lệ!" };
            }

            if (!gioHang.ChiTietGioHangs.Any())
            {
                return new { thanhCong = false, thongBao = "Giỏ quà đang trống, vui lòng chọn bánh trước khi áp mã!" };
            }

            var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k => k.CodeKm == maNhap);
            if (km == null)
            {
                return new { thanhCong = false, thongBao = $"Mã giảm giá '{maNhap}' không tồn tại trong hệ thống!" };
            }

            if (!km.TrangThaiKichHoat)
            {
                return new { thanhCong = false, thongBao = $"Mã ưu đãi '{maNhap}' hiện đang tạm ngưng kích hoạt!" };
            }

            DateTime bayGio = LayThoiGianHeThong();
            if (bayGio < km.NgayBatDau)
            {
                return new { thanhCong = false, thongBao = $"Mã ưu đãi '{maNhap}' chỉ có hiệu lực từ ngày {km.NgayBatDau:dd/MM/yyyy}!" };
            }
            if (bayGio > km.NgayKetThuc)
            {
                return new { thanhCong = false, thongBao = $"Mã ưu đãi đã hết hạn vào ngày {km.NgayKetThuc:dd/MM/yyyy}" };
            }

            if (km.SoLuotDaDung >= km.SoLuotToiDa)
            {
                return new { thanhCong = false, thongBao = $"Mã giảm giá '{maNhap}' đã hết lượt sử dụng ({km.SoLuotDaDung}/{km.SoLuotToiDa})!" };
            }

            if (!maNd.HasValue && !km.ChoPhepKhachVangLai)
            {
                return new
                {
                    thanhCong = false,
                    thongBao = $"Mã '{maNhap}' chỉ dành riêng cho Thành Viên. Vui lòng Đăng nhập để sử dụng!"
                };
            }

            foreach (var ct in gioHang.ChiTietGioHangs)
            {
                ct.DonGiaTamTinh = TinhDonGiaThucTeTuCsdl(ct);
            }
            decimal tongTamTinh = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong * ct.DonGiaTamTinh);

            if (tongTamTinh < km.GiaTriDonToiThieu)
            {
                return new
                {
                    thanhCong = false,
                    thongBao = $"Mã '{maNhap}' yêu cầu đơn hàng tối thiểu {km.GiaTriDonToiThieu:N0} đ (Hiện tại: {tongTamTinh:N0} đ)!"
                };
            }

            decimal tienGiam = TinhTienGiamVoucher(km, tongTamTinh);
            gioHang.CodeKmdangApDung = km.CodeKm;
            gioHang.NgayCapNhat = LayThoiGianHeThong();
            await _db.SaveChangesAsync();

            return new
            {
                thanhCong = true,
                thongBao = $"Áp dụng mã '{km.CodeKm}' ({km.TenChuongTrinh} • HSD: {km.NgayKetThuc:dd/MM/yyyy}) thành công! Giảm {tienGiam:N0} đ.",
                codeKmDangDung = km.CodeKm,
                hanSuDungVoucher = km.NgayKetThuc.ToString("dd/MM/yyyy"),
                tienGiamVoucher = tienGiam,
                tongThanhToanSauGiam = Math.Max(0, tongTamTinh - tienGiam)
            };
        }

        /// <summary>
        /// Tổng kết thông tin giỏ hàng ra đối tượng JSON đồng bộ cho Offcanvas & Badge.
        /// </summary>
        public async Task<object> TongKetThongTinGioHangJsonAsync(Models.Entities.GioHang? gioHang, int? maNd)
        {
            await DamBaoMaKhuyenMaiMauConHanAsync();

            var kmGoiY = await _db.MaKhuyenMais.AsNoTracking().FirstOrDefaultAsync(k => k.CodeKm == "TRANGTRON2026");
            string thongTinVoucherGoiY = kmGoiY != null
                ? $"HSD: {kmGoiY.NgayKetThuc:dd/MM/yyyy} • Đơn từ {(kmGoiY.DonToiThieu / 1000m):N0}k"
                : "HSD: 31/12/2027 • Đơn từ 200k";

            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return new
                {
                    thanhCong = true,
                    tongSoLuong = 0,
                    tongTamTinh = 0m,
                    codeKmDangDung = (string?)null,
                    thongTinVoucherGoiY,
                    tienGiamVoucher = 0m,
                    tongThanhToanSauGiam = 0m,
                    danhSachMon = new List<object>()
                };
            }

            var danhSachMon = gioHang.ChiTietGioHangs
                .OrderByDescending(ct => ct.NgayThem)
                .Select(ct =>
                {
                    string tenMon = "Sản phẩm Trung Thu";
                    string? maBamCauHinh = null;
                    int tongCalo = 0;
                    var danhSachViBanh = new List<string>();
                    decimal donGiaCsdl = TinhDonGiaThucTeTuCsdl(ct);

                    if (ct.LoaiSanPham == 1 && ct.MaBanhNavigation != null)
                    {
                        tenMon = ct.MaBanhNavigation.TenBanh;
                        tongCalo = ct.MaBanhNavigation.ThongSoDinhDuong?.NangLuongCalo ?? 400;
                    }
                    else if (ct.LoaiSanPham == 2 && ct.MaCbNavigation != null)
                    {
                        tenMon = ct.MaCbNavigation.TenCombo;
                    }
                    else if (ct.LoaiSanPham == 3 && ct.MaHopTcNavigation != null)
                    {
                        tenMon = ct.MaHopTcNavigation.TenHopKhachDat;
                        maBamCauHinh = ct.MaHopTcNavigation.MaBamCauHinh;
                        tongCalo = ct.MaHopTcNavigation.TongCaloHop;
                        danhSachViBanh = ct.MaHopTcNavigation.ChiTietHopTuChons
                            .OrderBy(x => x.ViTriOtrongHop)
                            .Select(x => $"Ô {x.ViTriOtrongHop}: {x.MaBanhNavigation?.TenBanh ?? "Bánh"}")
                            .ToList();
                    }

                    return new
                    {
                        maCtgh = ct.MaCtgh,
                        loaiSanPham = ct.LoaiSanPham,
                        tenMon,
                        maBamCauHinh,
                        tongCalo,
                        danhSachViBanh,
                        soLuong = ct.SoLuong,
                        donGia = donGiaCsdl,
                        thanhTien = ct.SoLuong * donGiaCsdl
                    };
                })
                .ToList();

            int tongSoLuong = danhSachMon.Sum(x => x.soLuong);
            decimal tongTamTinh = danhSachMon.Sum(x => x.thanhTien);

            string? codeKmDangDung = gioHang.CodeKmdangApDung;
            decimal tienGiamVoucher = 0m;

            if (!string.IsNullOrWhiteSpace(codeKmDangDung))
            {
                var km = await _db.MaKhuyenMais
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.CodeKm == codeKmDangDung && k.NgayBatDau <= DateTime.Now && k.NgayKetThuc >= DateTime.Now && k.SldaDung < k.SltoiDa);

                if (km != null && km.TrangThaiKichHoat && (maNd.HasValue || km.ChoPhepKhachVangLai) && tongTamTinh >= km.DonToiThieu)
                {
                    tienGiamVoucher = TinhTienGiamVoucher(km, tongTamTinh);
                }
                else
                {
                    codeKmDangDung = null;
                }
            }

            return new
            {
                thanhCong = true,
                tongSoLuong,
                tongTamTinh,
                codeKmDangDung,
                thongTinVoucherGoiY,
                tienGiamVoucher,
                tongThanhToanSauGiam = Math.Max(0m, tongTamTinh - tienGiamVoucher),
                danhSachMon
            };
        }

        /// <summary>
        /// Kiểm tra chi tiết tồn kho thực tế của các món trong giỏ hàng để cảnh báo trên ngăn kéo giỏ quà.
        /// </summary>
        public async Task<object> KiemTraTonKhoGioHangChiTietAsync(Models.Entities.GioHang? gioHang)
        {
            if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
            {
                return new
                {
                    thanhCong = true,
                    hopLe = true,
                    danhSachLoiTheoMon = new Dictionary<int, string>(),
                    danhSachCanhBao = new List<object>()
                };
            }

            var homNay = LayNgayHienTai();
            var nhuCauBanh = new Dictionary<int, (string TenBanh, int SoLuongCan, List<int> DanhSachMaCtgh)>();
            var nhuCauVoHop = new Dictionary<int, (string TenVoHop, int SoLuongCan, int TonKhoVo, List<int> DanhSachMaCtgh)>();

            foreach (var ct in gioHang.ChiTietGioHangs)
            {
                if (ct.LoaiSanPham == 1 && ct.MaBanhNavigation != null)
                {
                    int maBanh = ct.MaBanhNavigation.MaBanh;
                    if (!nhuCauBanh.TryGetValue(maBanh, out var item)) item = (ct.MaBanhNavigation.TenBanh, 0, new List<int>());
                    item.SoLuongCan += ct.SoLuong;
                    if (!item.DanhSachMaCtgh.Contains(ct.MaCtgh)) item.DanhSachMaCtgh.Add(ct.MaCtgh);
                    nhuCauBanh[maBanh] = item;
                }
                else if (ct.LoaiSanPham == 2 && ct.MaCbNavigation != null)
                {
                    if (ct.MaCbNavigation.MaVhNavigation != null)
                    {
                        var vh = ct.MaCbNavigation.MaVhNavigation;
                        if (!nhuCauVoHop.TryGetValue(vh.MaVh, out var itemVo)) itemVo = (vh.TenVoHop, 0, vh.SltonKhoVo, new List<int>());
                        itemVo.SoLuongCan += ct.SoLuong;
                        if (!itemVo.DanhSachMaCtgh.Contains(ct.MaCtgh)) itemVo.DanhSachMaCtgh.Add(ct.MaCtgh);
                        nhuCauVoHop[vh.MaVh] = itemVo;
                    }
                    foreach (var ctCb in ct.MaCbNavigation.ChiTietComboMaus.Where(c => c.MaBanhNavigation != null))
                    {
                        int maBanh = ctCb.MaBanhNavigation!.MaBanh;
                        if (!nhuCauBanh.TryGetValue(maBanh, out var item)) item = (ctCb.MaBanhNavigation.TenBanh, 0, new List<int>());
                        item.SoLuongCan += ctCb.SoLuong * ct.SoLuong;
                        if (!item.DanhSachMaCtgh.Contains(ct.MaCtgh)) item.DanhSachMaCtgh.Add(ct.MaCtgh);
                        nhuCauBanh[maBanh] = item;
                    }
                }
                else if (ct.LoaiSanPham == 3 && ct.MaHopTcNavigation != null)
                {
                    if (ct.MaHopTcNavigation.MaVhNavigation != null)
                    {
                        var vh = ct.MaHopTcNavigation.MaVhNavigation;
                        if (!nhuCauVoHop.TryGetValue(vh.MaVh, out var itemVo)) itemVo = (vh.TenVoHop, 0, vh.SltonKhoVo, new List<int>());
                        itemVo.SoLuongCan += ct.SoLuong;
                        if (!itemVo.DanhSachMaCtgh.Contains(ct.MaCtgh)) itemVo.DanhSachMaCtgh.Add(ct.MaCtgh);
                        nhuCauVoHop[vh.MaVh] = itemVo;
                    }
                    foreach (var ctHop in ct.MaHopTcNavigation.ChiTietHopTuChons.Where(c => c.MaBanhNavigation != null))
                    {
                        int maBanh = ctHop.MaBanhNavigation!.MaBanh;
                        if (!nhuCauBanh.TryGetValue(maBanh, out var item)) item = (ctHop.MaBanhNavigation.TenBanh, 0, new List<int>());
                        item.SoLuongCan += ct.SoLuong;
                        if (!item.DanhSachMaCtgh.Contains(ct.MaCtgh)) item.DanhSachMaCtgh.Add(ct.MaCtgh);
                        nhuCauBanh[maBanh] = item;
                    }
                }
            }

            var danhSachMaBanhCanKiemTra = nhuCauBanh.Keys.ToList();
            var tonKhoLoTheoBanh = await _db.LoSanXuatBanhs
                .AsNoTracking()
                .Where(l => danhSachMaBanhCanKiemTra.Contains(l.MaBanh) &&
                            l.HanSuDung > homNay &&
                            (l.TrangThaiLo == "DANG_BAN" || l.TrangThaiLo == "XA_CAN_DATE") &&
                            l.SlconLai > 0)
                .GroupBy(l => l.MaBanh)
                .Select(g => new { MaBanh = g.Key, TongTonConHan = g.Sum(x => x.SlconLai) })
                .ToDictionaryAsync(x => x.MaBanh, x => x.TongTonConHan);

            var tonKhoSanPhamBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Where(b => danhSachMaBanhCanKiemTra.Contains(b.MaBanh))
                .ToDictionaryAsync(b => b.MaBanh, b => b.TrangThaiBan ? b.TongTonKho : 0);

            var danhSachLoiTheoMon = new Dictionary<int, string>();
            var danhSachCanhBao = new List<object>();

            foreach (var kvp in nhuCauBanh)
            {
                int maBanh = kvp.Key;
                var info = kvp.Value;
                int tonLo = tonKhoLoTheoBanh.TryGetValue(maBanh, out int slLo) ? slLo : 0;
                int tonSp = tonKhoSanPhamBanh.TryGetValue(maBanh, out int slSp) ? slSp : 0;
                int tonThucTe = Math.Min(tonSp, tonLo);

                if (tonThucTe < info.SoLuongCan)
                {
                    string thongBao = tonThucTe <= 0
                        ? $"Vị bánh \"{info.TenBanh}\" vừa hết hàng trong kho (cần {info.SoLuongCan} bánh)."
                        : $"Vị bánh \"{info.TenBanh}\" chỉ còn {tonThucTe} bánh còn hạn trong kho (đang chọn {info.SoLuongCan} bánh).";

                    danhSachCanhBao.Add(new { maBanh, tenBanh = info.TenBanh, soLuongCan = info.SoLuongCan, tonKhoThucTe = tonThucTe, thongBao });

                    foreach (int maCtgh in info.DanhSachMaCtgh)
                    {
                        danhSachLoiTheoMon[maCtgh] = danhSachLoiTheoMon.TryGetValue(maCtgh, out string? cu) ? cu + " • " + thongBao : thongBao;
                    }
                }
            }

            foreach (var kvpVo in nhuCauVoHop)
            {
                var infoVo = kvpVo.Value;
                if (infoVo.TonKhoVo < infoVo.SoLuongCan)
                {
                    string thongBao = $"Mẫu vỏ hộp \"{infoVo.TenVoHop}\" chỉ còn {infoVo.TonKhoVo} vỏ trong kho (cần {infoVo.SoLuongCan} hộp).";
                    danhSachCanhBao.Add(new { maBanh = 0, tenBanh = infoVo.TenVoHop, soLuongCan = infoVo.SoLuongCan, tonKhoThucTe = infoVo.TonKhoVo, thongBao });

                    foreach (int maCtgh in infoVo.DanhSachMaCtgh)
                    {
                        danhSachLoiTheoMon[maCtgh] = danhSachLoiTheoMon.TryGetValue(maCtgh, out string? cu) ? cu + " • " + thongBao : thongBao;
                    }
                }
            }

            return new
            {
                thanhCong = true,
                hopLe = danhSachCanhBao.Count == 0,
                danhSachLoiTheoMon,
                danhSachCanhBao
            };
        }
    }
}
