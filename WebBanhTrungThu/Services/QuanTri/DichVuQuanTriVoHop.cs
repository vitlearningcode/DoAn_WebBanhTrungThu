using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services.CoSo;
using WebBanhTrungThu.Services.Interfaces;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Dịch vụ chuyên trách quản trị Mẫu vỏ hộp quà tặng và hạch toán Nhập kho / Xuất hủy Vỏ hộp (PhieuKhoVoHop &amp; PhieuThuChi).
    /// </summary>
    public class DichVuQuanTriVoHop : DichVuCoSo, IDichVuQuanTriVoHop
    {
        private readonly IWebHostEnvironment _env;

        public DichVuQuanTriVoHop(CSDLBanhTrungThuContext db, IWebHostEnvironment env) : base(db)
        {
            _env = env;
        }

        public async Task<VoHopAdminIndexViewModel> LayDuLieuTrangQuanTriAsync(
            string? tuKhoa,
            int? sucChua,
            string? trangThaiTon)
        {
            var dsVoHopDb = await _db.MauVoHops
                .AsNoTracking()
                .Include(v => v.MaAnhChinhNavigation)
                .Include(v => v.ComboMauCoSans)
                .Include(v => v.PhieuKhoVoHops)
                .OrderBy(v => v.MaVh)
                .ToListAsync();

            var lienKetAnhVo = await _db.LienKetHinhAnhs
                .AsNoTracking()
                .Include(l => l.MaHaNavigation)
                .Where(l => l.LoaiDoiTuong == "VOHOP")
                .OrderBy(l => l.ThuTuHienThi)
                .ThenBy(l => l.MaLk)
                .ToListAsync();

            var mapUrlLienKet = lienKetAnhVo
                .Where(l => l.MaHaNavigation != null && !string.IsNullOrWhiteSpace(l.MaHaNavigation.UrlHienThi))
                .GroupBy(l => l.MaDoiTuong)
                .ToDictionary(g => g.Key, g => g.First().MaHaNavigation.UrlHienThi);

            var dsItemDayDu = dsVoHopDb.Select(v =>
            {
                string urlAnh = v.MaAnhChinhNavigation != null && !string.IsNullOrWhiteSpace(v.MaAnhChinhNavigation.UrlHienThi)
                    ? v.MaAnhChinhNavigation.UrlHienThi
                    : (mapUrlLienKet.TryGetValue(v.MaVh, out var urlLk)
                        ? urlLk
                        : (v.SucChuaBanh >= 6 ? "/images/vohop/khay-6-o-rong.png" : "/images/vohop/khay-4-o-rong.png"));

                return new MauVoHopAdminItemViewModel
                {
                    MaVh = v.MaVh,
                    MaCodeVh = v.MaCodeVh,
                    TenVoHop = v.TenVoHop,
                    SucChuaBanh = v.SucChuaBanh,
                    GiaVoHopRieng = Math.Round(v.GiaVoHopRieng, 0),
                    SlbanhThuongHangToiThieu = v.SlbanhThuongHangToiThieu,
                    KieuBoCucLuoi = v.KieuBoCucLuoi,
                    KemTraOlong = v.KemTraOlong,
                    SltonKhoVo = v.SltonKhoVo,
                    MoTaChatLieu = v.MoTaChatLieu,
                    TrangThai = v.TrangThai,
                    MaAnhChinh = v.MaAnhChinh,
                    UrlAnh = urlAnh,
                    SoComboLienKet = v.ComboMauCoSans.Count,
                    SoGiaoDichKho = v.PhieuKhoVoHops.Count
                };
            }).ToList();

            decimal tongGiaTriNhap = await _db.PhieuKhoVoHops
                .AsNoTracking()
                .Where(p => p.LoaiGiaoDich == "NHAP_VO_HOP")
                .SumAsync(p => (decimal?)p.ThanhTien) ?? 0m;

            string ttChuan = ChuanHoaChuoi(trangThaiTon, "ALL").ToUpperInvariant();
            string tkChuan = ChuanHoaChuoi(tuKhoa);

            IEnumerable<MauVoHopAdminItemViewModel> truyVan = dsItemDayDu;
            if (!string.IsNullOrWhiteSpace(tkChuan))
            {
                truyVan = truyVan.Where(v =>
                    v.MaCodeVh.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || v.TenVoHop.Contains(tkChuan, StringComparison.OrdinalIgnoreCase)
                    || (v.MoTaChatLieu ?? string.Empty).Contains(tkChuan, StringComparison.OrdinalIgnoreCase));
            }
            if (sucChua.HasValue && sucChua.Value > 0)
            {
                truyVan = sucChua.Value >= 8
                    ? truyVan.Where(v => v.SucChuaBanh >= 8)
                    : truyVan.Where(v => v.SucChuaBanh == sucChua.Value);
            }
            if (ttChuan != "ALL")
            {
                truyVan = ttChuan switch
                {
                    "SAP_HET" => truyVan.Where(v => v.SltonKhoVo <= 20),
                    "HET_HANG" => truyVan.Where(v => v.SltonKhoVo <= 0),
                    "CON_HANG" => truyVan.Where(v => v.SltonKhoVo > 20),
                    "DANG_DUNG" => truyVan.Where(v => v.TrangThai),
                    "NGUNG_KD" => truyVan.Where(v => !v.TrangThai),
                    _ => truyVan
                };
            }

            var lichSuKho = await _db.PhieuKhoVoHops
                .AsNoTracking()
                .Include(p => p.MaVhNavigation)
                .Include(p => p.MaNccNavigation)
                .Include(p => p.MaPhieuChiNavigation)
                .OrderByDescending(p => p.NgayGiaoDich)
                .ThenByDescending(p => p.MaPvh)
                .Take(100)
                .ToListAsync();

            var dsNcc = await _db.NhaCungCaps
                .AsNoTracking()
                .Where(n => n.DangHoatDong)
                .OrderBy(n => n.TenNcc)
                .ToListAsync();

            var dsHinhAnh = await _db.KhoHinhAnhs
                .AsNoTracking()
                .OrderByDescending(h => h.MaHa)
                .Take(80)
                .ToListAsync();

            return new VoHopAdminIndexViewModel
            {
                TongMauVoHop = dsItemDayDu.Count,
                TongTonKhoVo = dsItemDayDu.Sum(v => v.SltonKhoVo),
                SoMauSapHet = dsItemDayDu.Count(v => v.SltonKhoVo <= 20),
                TongGiaTriNhapVoHop = Math.Round(tongGiaTriNhap, 0),
                TuKhoa = tuKhoa,
                SucChuaLoc = sucChua,
                TrangThaiTonLoc = ttChuan,
                DanhSachVoHop = truyVan.ToList(),
                LichSuKhoVoHop = lichSuKho,
                DanhSachNhaCungCap = dsNcc,
                DanhSachHinhAnh = dsHinhAnh
            };
        }

        public async Task<(bool ThanhCong, string ThongBao)> LuuMauVoHopAsync(LuuMauVoHopRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.TenVoHop))
            {
                return (false, "Vui lòng nhập Tên mẫu vỏ hộp.");
            }
            if (req.SucChuaBanh < 2)
            {
                return (false, "Sức chứa bánh của mẫu vỏ hộp phải từ 2 bánh trở lên.");
            }
            if (req.GiaVoHopRieng < 0m)
            {
                return (false, "Giá bán riêng vỏ hộp không được nhỏ hơn 0 đ.");
            }

            string maCodeChuan = ChuanHoaChuoi(req.MaCodeVh).ToUpperInvariant();
            if (maCodeChuan.Length > 30) maCodeChuan = maCodeChuan[..30];

            if (!string.IsNullOrWhiteSpace(maCodeChuan))
            {
                bool trungMa = await _db.MauVoHops
                    .AsNoTracking()
                    .AnyAsync(v => v.MaCodeVh == maCodeChuan && v.MaVh != req.MaVh);
                if (trungMa)
                {
                    return (false, $"Mã vỏ hộp \"{maCodeChuan}\" đã tồn tại. Vui lòng chọn mã khác.");
                }
            }

            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                MauVoHop? voHop = req.MaVh > 0
                    ? await _db.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == req.MaVh)
                    : null;

                if (req.MaVh > 0 && voHop == null)
                {
                    return (false, "Không tìm thấy mẫu vỏ hộp cần cập nhật.");
                }

                string boCuc = ChuanHoaChuoi(req.KieuBoCucLuoi, req.SucChuaBanh >= 6 ? "grid-3x2" : "grid-2x2");
                if (boCuc.Length > 50) boCuc = boCuc[..50];
                int slVipToiThieu = Math.Clamp(req.SlbanhThuongHangToiThieu, 0, req.SucChuaBanh);
                string? moTa = string.IsNullOrWhiteSpace(req.MoTaChatLieu) ? null : req.MoTaChatLieu.Trim();
                if (moTa != null && moTa.Length > 500) moTa = moTa[..500];

                bool laTaoMoi = voHop == null;
                if (voHop == null)
                {
                    if (string.IsNullOrWhiteSpace(maCodeChuan))
                    {
                        maCodeChuan = await SinhMaCodeVoHopTuDongAsync();
                    }

                    voHop = new MauVoHop
                    {
                        MaCodeVh = maCodeChuan,
                        TenVoHop = req.TenVoHop.Trim(),
                        SucChuaBanh = req.SucChuaBanh,
                        GiaVoHopRieng = Math.Round(req.GiaVoHopRieng, 0),
                        SlbanhThuongHangToiThieu = slVipToiThieu,
                        KieuBoCucLuoi = boCuc,
                        KemTraOlong = req.KemTraOlong,
                        SltonKhoVo = 0,
                        MoTaChatLieu = moTa,
                        TrangThai = req.TrangThai
                    };
                    _db.MauVoHops.Add(voHop);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(maCodeChuan))
                    {
                        voHop.MaCodeVh = maCodeChuan;
                    }
                    voHop.TenVoHop = req.TenVoHop.Trim();
                    voHop.SucChuaBanh = req.SucChuaBanh;
                    voHop.GiaVoHopRieng = Math.Round(req.GiaVoHopRieng, 0);
                    voHop.SlbanhThuongHangToiThieu = slVipToiThieu;
                    voHop.KieuBoCucLuoi = boCuc;
                    voHop.KemTraOlong = req.KemTraOlong;
                    voHop.MoTaChatLieu = moTa;
                    voHop.TrangThai = req.TrangThai;
                }

                int? maAnhChinhMoi = await XuLyVaDongBoAnhVoHopAsync(voHop, req);
                if (maAnhChinhMoi.HasValue)
                {
                    voHop.MaAnhChinh = maAnhChinhMoi.Value;
                }

                await _db.SaveChangesAsync();

                string hanhDong = laTaoMoi ? "Đã thêm mới" : "Đã cập nhật";
                return (true, $"{hanhDong} mẫu vỏ hộp [{voHop.MaCodeVh}] {voHop.TenVoHop}!");
            });
        }

        public async Task<(bool ThanhCong, string ThongBao)> ChuyenTrangThaiVoHopAsync(int maVh)
        {
            var voHop = await _db.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == maVh);
            if (voHop == null)
            {
                return (false, "Không tìm thấy mẫu vỏ hộp cần chuyển trạng thái.");
            }

            voHop.TrangThai = !voHop.TrangThai;
            await _db.SaveChangesAsync();

            string trangThaiMoi = voHop.TrangThai ? "Đang sử dụng" : "Ngừng sử dụng";
            return (true, $"Đã chuyển mẫu vỏ hộp [{voHop.MaCodeVh}] {voHop.TenVoHop} sang trạng thái {trangThaiMoi}.");
        }

        public async Task<(bool ThanhCong, string ThongBao)> ThucHienNhapXuatVoHopAsync(
            NhapXuatVoHopRequest req,
            int? maNdLap,
            string tenNguoiLap)
        {
            if (req.SoLuong <= 0)
            {
                return (false, "Số lượng nhập / xuất hủy vỏ hộp phải lớn hơn 0.");
            }

            string loaiGd = ChuanHoaChuoi(req.LoaiGiaoDich, "NHAP_VO_HOP").ToUpperInvariant();
            if (loaiGd != "NHAP_VO_HOP" && loaiGd != "XUAT_HUY_VO_HOP")
            {
                return (false, "Loại giao dịch kho vỏ hộp không hợp lệ.");
            }

            return await ThucThiTrongGiaoDichAsync<(bool, string)>(async () =>
            {
                var voHop = await _db.MauVoHops.FirstOrDefaultAsync(v => v.MaVh == req.MaVh);
                if (voHop == null)
                {
                    return (false, "Không tìm thấy mẫu vỏ hộp cần cập nhật tồn kho.");
                }

                DateTime bayGio = LayThoiGianHeThong();
                int? maNdHopLe = null;
                if (maNdLap.HasValue && await _db.NguoiDungs.AsNoTracking().AnyAsync(u => u.MaNd == maNdLap.Value))
                {
                    maNdHopLe = maNdLap.Value;
                }

                string nguoiLapChuan = ChuanHoaChuoi(tenNguoiLap, "Quản trị viên Nguyệt Cát");
                if (nguoiLapChuan.Length > 150) nguoiLapChuan = nguoiLapChuan[..150];
                string? ghiChuChuan = string.IsNullOrWhiteSpace(req.GhiChu) ? null : req.GhiChu.Trim();
                if (ghiChuChuan != null && ghiChuChuan.Length > 500) ghiChuChuan = ghiChuChuan[..500];

                if (loaiGd == "XUAT_HUY_VO_HOP")
                {
                    if (req.SoLuong > voHop.SltonKhoVo)
                    {
                        return (false, $"Số lượng xuất hủy ({req.SoLuong:N0}) vượt quá tồn kho hiện tại ({voHop.SltonKhoVo:N0}) của vỏ [{voHop.MaCodeVh}].");
                    }

                    voHop.SltonKhoVo -= req.SoLuong;
                    string soPhieuXuat = await SinhSoPhieuKhoVoHopAsync("PVH-XH", bayGio);

                    _db.PhieuKhoVoHops.Add(new PhieuKhoVoHop
                    {
                        SoPhieu = soPhieuXuat,
                        MaVh = voHop.MaVh,
                        LoaiGiaoDich = "XUAT_HUY_VO_HOP",
                        SoLuong = req.SoLuong,
                        DonGiaNhap = 0m,
                        ThanhTien = 0m,
                        MaNcc = null,
                        MaPhieuChi = null,
                        MaNdLap = maNdHopLe,
                        NguoiLap = nguoiLapChuan,
                        GhiChu = ghiChuChuan ?? "Xuất hủy vỏ hộp hư hỏng / lỗi quy cách",
                        NgayGiaoDich = bayGio
                    });

                    await _db.SaveChangesAsync();
                    return (true, $"Đã lập phiếu {soPhieuXuat}: xuất hủy {req.SoLuong:N0} vỏ [{voHop.MaCodeVh}] (tồn mới: {voHop.SltonKhoVo:N0}).");
                }

                if (req.DonGiaNhap < 0m)
                {
                    return (false, "Đơn giá nhập vỏ hộp không được nhỏ hơn 0 đ.");
                }

                decimal donGiaNhap = Math.Round(req.DonGiaNhap, 0);
                decimal thanhTien = req.SoLuong * donGiaNhap;

                NhaCungCap? nccDb = null;
                if (req.MaNcc.HasValue && req.MaNcc.Value > 0)
                {
                    nccDb = await _db.NhaCungCaps.AsNoTracking().FirstOrDefaultAsync(n => n.MaNcc == req.MaNcc.Value);
                }

                voHop.SltonKhoVo += req.SoLuong;
                string soPhieuNhap = await SinhSoPhieuKhoVoHopAsync("PVH-NH", bayGio);

                int? maPhieuChiMoi = null;
                string? maPhieuChiHienThi = null;
                if (req.ThanhToanNgay && thanhTien > 0m)
                {
                    maPhieuChiHienThi = await SinhMaPhieuChiVoHopAsync(bayGio);
                    string ptChuan = ChuanHoaChuoi(req.PhuongThucTt, "CHUYEN_KHOAN").ToUpperInvariant();
                    if (ptChuan != "TIEN_MAT" && ptChuan != "CHUYEN_KHOAN")
                    {
                        ptChuan = ptChuan.Contains("MAT") ? "TIEN_MAT" : "CHUYEN_KHOAN";
                    }

                    string doiTuongChi = nccDb?.TenNcc ?? "Nhà cung cấp vỏ hộp";
                    if (doiTuongChi.Length > 150) doiTuongChi = doiTuongChi[..150];

                    string ghiChuChi = $"Chi nhập {req.SoLuong:N0} vỏ hộp [{voHop.MaCodeVh}] {voHop.TenVoHop} (Phiếu {soPhieuNhap})";
                    if (ghiChuChi.Length > 500) ghiChuChi = ghiChuChi[..500];

                    var phieuChi = new PhieuThuChi
                    {
                        MaPhieuHienThi = maPhieuChiHienThi,
                        LoaiPhieu = "CHI",
                        HangMuc = "Chi nhập kho vỏ hộp quà tặng",
                        SoTien = thanhTien,
                        PhuongThuc = ptChuan,
                        MaDonHangLienKet = null,
                        MaPhieuNhapLienKet = null,
                        NguoiNopNhan = doiTuongChi,
                        GhiChu = ghiChuChi,
                        NgayLap = bayGio
                    };
                    _db.PhieuThuChis.Add(phieuChi);
                    await _db.SaveChangesAsync();
                    maPhieuChiMoi = phieuChi.MaPhieu;
                }

                _db.PhieuKhoVoHops.Add(new PhieuKhoVoHop
                {
                    SoPhieu = soPhieuNhap,
                    MaVh = voHop.MaVh,
                    LoaiGiaoDich = "NHAP_VO_HOP",
                    SoLuong = req.SoLuong,
                    DonGiaNhap = donGiaNhap,
                    ThanhTien = thanhTien,
                    MaNcc = nccDb?.MaNcc,
                    MaPhieuChi = maPhieuChiMoi,
                    MaNdLap = maNdHopLe,
                    NguoiLap = nguoiLapChuan,
                    GhiChu = ghiChuChuan ?? $"Nhập kho vỏ hộp [{voHop.MaCodeVh}]",
                    NgayGiaoDich = bayGio
                });

                await _db.SaveChangesAsync();

                string thongBaoChi = !string.IsNullOrWhiteSpace(maPhieuChiHienThi)
                    ? $" và đã tự động ghi Phiếu Chi [{maPhieuChiHienThi}] vào Sổ quỹ ({thanhTien:N0} đ)"
                    : string.Empty;

                return (true, $"Đã nhập +{req.SoLuong:N0} vỏ [{voHop.MaCodeVh}] (Phiếu {soPhieuNhap}, tồn mới: {voHop.SltonKhoVo:N0}){thongBaoChi}!");
            });
        }

        private async Task<int?> XuLyVaDongBoAnhVoHopAsync(MauVoHop voHop, LuuMauVoHopRequest req)
        {
            int? maHaChon = null;

            if (req.FileAnhTaiLen != null && req.FileAnhTaiLen.Length > 0)
            {
                string ext = Path.GetExtension(req.FileAnhTaiLen.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                string fileName = $"vohop-{LayThoiGianHeThong():yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}{ext}";
                string dirPath = Path.Combine(_env.WebRootPath, "images", "vohop");
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

                using (var stream = new FileStream(Path.Combine(dirPath, fileName), FileMode.Create))
                {
                    await req.FileAnhTaiLen.CopyToAsync(stream);
                }

                var anhMoi = new KhoHinhAnh
                {
                    TenTepGoc = req.FileAnhTaiLen.FileName,
                    TieuDeAlt = voHop.TenVoHop.Length > 250 ? voHop.TenVoHop[..250] : voHop.TenVoHop,
                    LoaiLuuTru = 1,
                    DuongDanCucBo = $"/images/vohop/{fileName}",
                    KichThuocPixel = "800x800",
                    NgayTaiLen = LayThoiGianHeThong()
                };
                _db.KhoHinhAnhs.Add(anhMoi);
                await _db.SaveChangesAsync();
                maHaChon = anhMoi.MaHa;
            }
            else if (!string.IsNullOrWhiteSpace(req.UrlAnhNhapTay))
            {
                string url = req.UrlAnhNhapTay.Trim();
                if (url.Length > 500) url = url[..500];

                var anhMoi = new KhoHinhAnh
                {
                    TenTepGoc = Path.GetFileName(url),
                    TieuDeAlt = voHop.TenVoHop.Length > 250 ? voHop.TenVoHop[..250] : voHop.TenVoHop,
                    LoaiLuuTru = 1,
                    DuongDanCucBo = url,
                    KichThuocPixel = "800x800",
                    NgayTaiLen = LayThoiGianHeThong()
                };
                _db.KhoHinhAnhs.Add(anhMoi);
                await _db.SaveChangesAsync();
                maHaChon = anhMoi.MaHa;
            }
            else if (req.MaAnhChinh.HasValue && req.MaAnhChinh.Value > 0)
            {
                bool tonTaiAnh = await _db.KhoHinhAnhs.AsNoTracking().AnyAsync(h => h.MaHa == req.MaAnhChinh.Value);
                if (tonTaiAnh) maHaChon = req.MaAnhChinh.Value;
            }

            if (!maHaChon.HasValue) return null;

            var lienKetKhay = await _db.LienKetHinhAnhs
                .FirstOrDefaultAsync(l => l.LoaiDoiTuong == "VOHOP"
                                       && l.MaDoiTuong == voHop.MaVh
                                       && l.VaiTroAnh == "KHAY_HOP_RONG");

            if (lienKetKhay != null)
            {
                lienKetKhay.MaHa = maHaChon.Value;
                lienKetKhay.ThuTuHienThi = 1;
            }
            else
            {
                _db.LienKetHinhAnhs.Add(new LienKetHinhAnh
                {
                    MaHa = maHaChon.Value,
                    LoaiDoiTuong = "VOHOP",
                    MaDoiTuong = voHop.MaVh,
                    VaiTroAnh = "KHAY_HOP_RONG",
                    ThuTuHienThi = 1
                });
            }

            return maHaChon.Value;
        }

        private async Task<string> SinhMaCodeVoHopTuDongAsync()
        {
            var dsCode = await _db.MauVoHops.AsNoTracking().Select(v => v.MaCodeVh).ToListAsync();
            int stt = 1;
            while (true)
            {
                string ungVien = $"VH-{stt:D2}";
                if (!dsCode.Any(c => string.Equals(c, ungVien, StringComparison.OrdinalIgnoreCase)))
                {
                    return ungVien;
                }
                stt++;
            }
        }

        private async Task<string> SinhSoPhieuKhoVoHopAsync(string tienToNgay, DateTime bayGio)
        {
            string tienTo = $"{tienToNgay}-{bayGio:yyyyMMdd}-";
            var dsCungNgay = await _db.PhieuKhoVoHops
                .AsNoTracking()
                .Where(p => p.SoPhieu.StartsWith(tienTo))
                .Select(p => p.SoPhieu)
                .ToListAsync();

            int maxStt = 0;
            foreach (var ma in dsCungNgay)
            {
                if (int.TryParse(ma.Substring(tienTo.Length), out int so) && so > maxStt)
                {
                    maxStt = so;
                }
            }
            return $"{tienTo}{(maxStt + 1):D3}";
        }

        private async Task<string> SinhMaPhieuChiVoHopAsync(DateTime bayGio)
        {
            string maPhieu = $"PCH-PVH-{bayGio:yyMMddHHmmss}";
            while (await _db.PhieuThuChis.AsNoTracking().AnyAsync(p => p.MaPhieuHienThi == maPhieu))
            {
                maPhieu = $"PCH-PVH-{bayGio:yyMMddHHmmss}-{Random.Shared.Next(10, 99)}";
                if (maPhieu.Length > 50) maPhieu = maPhieu[..50];
            }
            return maPhieu;
        }
    }
}
