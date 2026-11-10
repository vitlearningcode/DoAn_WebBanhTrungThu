using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanhTrungThu.Data;
using WebBanhTrungThu.Models.Entities;
using WebBanhTrungThu.Services;
using WebBanhTrungThu.ViewModels;

namespace WebBanhTrungThu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QUAN_TRI,NHAN_VIEN,THU_NGAN")]
    public class BanHangPosController : Controller
    {
        private readonly CSDLBanhTrungThuContext _db;
        private readonly DichVuHinhAnh _dichVuHinhAnh;
        private readonly DichVuKiotVietMetadata _kiotMeta;

        public BanHangPosController(
            CSDLBanhTrungThuContext db,
            DichVuHinhAnh dichVuHinhAnh,
            DichVuKiotVietMetadata kiotMeta)
        {
            _db = db;
            _dichVuHinhAnh = dichVuHinhAnh;
            _kiotMeta = kiotMeta;
        }

        // =====================================================================
        // 1. MÀN HÌNH CHÍNH KIOTVIET WEB POS (/Admin/BanHangPos/Index)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var store = _kiotMeta.DocDuLieu();
            var tuDienAnh = await _dichVuHinhAnh.LayTuDienUrlAnhAsync();

            var dsBanh = await _db.SanPhamBanhs
                .AsNoTracking()
                .Include(b => b.ThongSoDinhDuong)
                .Include(b => b.LoSanXuatBanhs)
                .Include(b => b.MaDmNavigation)
                .Where(b => b.TrangThaiBan)
                .OrderBy(b => b.MaBanh)
                .ToListAsync();

            foreach (var b in dsBanh)
            {
                _kiotMeta.GanMetadataChoBanh(b, store);
                string keyMeta = $"BANH_{b.MaBanh}";
                if (store.Items.TryGetValue(keyMeta, out var metaItem) && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                {
                    tuDienAnh[$"BANH_{b.MaBanh}_ANH_CHINH"] = metaItem.DanhSachAnh[0];
                }
            }

            var dsCombo = await _db.ComboMauCoSans
                .AsNoTracking()
                .Include(c => c.MaVhNavigation)
                .Include(c => c.ChiTietComboMaus)
                    .ThenInclude(ct => ct.MaBanhNavigation)
                .Where(c => c.TrangThai)
                .OrderBy(c => c.MaCb)
                .ToListAsync();

            foreach (var cb in dsCombo)
            {
                _kiotMeta.GanMetadataChoCombo(cb, store);
                string keyMeta = $"COMBO_{cb.MaCb}";
                if (store.Items.TryGetValue(keyMeta, out var metaItem) && metaItem.DanhSachAnh.Count > 0 && !string.IsNullOrWhiteSpace(metaItem.DanhSachAnh[0]))
                {
                    tuDienAnh[$"COMBO_{cb.MaCb}_ANH_CHINH"] = metaItem.DanhSachAnh[0];
                }
            }

            var dsVoHop = await _db.MauVoHops
                .AsNoTracking()
                .Where(v => v.TrangThai)
                .OrderBy(v => v.MaVh)
                .ToListAsync();

            var dsDanhMuc = await _db.DanhMucBanhs
                .AsNoTracking()
                .Where(d => d.TrangThai)
                .OrderBy(d => d.ThuTuSx)
                .ToListAsync();

            var bangB2B = await _db.BangChietKhauSiB2bs
                .AsNoTracking()
                .OrderBy(b => b.SlhopToiThieu)
                .ToListAsync();

            var dsKhuyenMai = await _db.MaKhuyenMais
                .AsNoTracking()
                .Where(k => k.NgayKetThuc >= DateTime.Now && k.SldaDung < k.SltoiDa)
                .OrderBy(k => k.CodeKm)
                .ToListAsync();

            string tenThuNgan = User.Identity?.Name ?? "Thu ngân Nguyệt Cát";
            string vaiTro = User.FindFirst(ClaimTypes.Role)?.Value ?? "THU_NGAN";

            var vm = new BanHangPosViewModel
            {
                DanhSachBanh = dsBanh,
                DanhSachCombo = dsCombo,
                DanhSachVoHop = dsVoHop,
                DanhSachDanhMuc = dsDanhMuc,
                BangChietKhauB2B = bangB2B,
                DanhSachKhuyenMai = dsKhuyenMai,
                TuDienAnh = tuDienAnh,
                TenThuNgan = tenThuNgan,
                VaiTroThuNgan = vaiTro
            };

            return View(vm);
        }

        // =====================================================================
        // 2. API TÌM NHANH KHÁCH HÀNG THÀNH VIÊN TẠI QUẦY (F4)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> TimKhachHangPos(string? tuKhoa)
        {
            string kw = (tuKhoa ?? string.Empty).Trim();
            var query = _db.NguoiDungs
                .AsNoTracking()
                .Where(u => u.TrangThai);

            if (!string.IsNullOrWhiteSpace(kw))
            {
                query = query.Where(u => u.Sdt.Contains(kw) || u.HoTen.Contains(kw) || u.Email.Contains(kw));
            }

            var ds = await query
                .OrderByDescending(u => u.DiemTichLuy)
                .Take(10)
                .Select(u => new
                {
                    maNd = u.MaNd,
                    hoTen = u.HoTen,
                    sdt = u.Sdt,
                    email = u.Email,
                    hangTv = u.HangTv,
                    diemTichLuy = u.DiemTichLuy
                })
                .ToListAsync();

            return Json(new { thanhCong = true, danhSach = ds });
        }

        // =====================================================================
        // 3. API THÊM NHANH KHÁCH HÀNG THÀNH VIÊN MỚI TẠI QUẦY (+50 ĐIỂM MỞ THẺ)
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> ThemKhachHangNhanhPos(string hoTen, string sdt, string? email)
        {
            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(sdt))
            {
                return Json(new { thanhCong = false, thongBao = "Vui lòng nhập đầy đủ Họ tên và Số điện thoại khách hàng!" });
            }

            string sdtChuan = sdt.Trim();
            var daTonTai = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.Sdt == sdtChuan);
            if (daTonTai != null)
            {
                return Json(new
                {
                    thanhCong = true,
                    thongBao = $"Số điện thoại {sdtChuan} đã là thành viên ({daTonTai.HoTen}) - Đã chọn vào hóa đơn!",
                    khach = new
                    {
                        maNd = daTonTai.MaNd,
                        hoTen = daTonTai.HoTen,
                        sdt = daTonTai.Sdt,
                        email = daTonTai.Email,
                        hangTv = daTonTai.HangTv,
                        diemTichLuy = daTonTai.DiemTichLuy
                    }
                });
            }

            string emailChuan = string.IsNullOrWhiteSpace(email)
                ? $"khachpos.{sdtChuan}.{Random.Shared.Next(10, 99)}@nguyetcat.vn"
                : email.Trim();

            if (await _db.NguoiDungs.AnyAsync(u => u.Email == emailChuan))
            {
                emailChuan = $"khachpos.{sdtChuan}.{DateTime.Now:HHmmss}@nguyetcat.vn";
            }

            // Đảm bảo vai trò KHACH_HANG tồn tại
            if (!await _db.VaiTros.AnyAsync(v => v.MaVt == "KHACH_HANG"))
            {
                _db.VaiTros.Add(new VaiTro
                {
                    MaVt = "KHACH_HANG",
                    TenVt = "Khách hàng thành viên",
                    MoTa = "Tích điểm và hưởng đặc quyền thành viên Nguyệt Cát"
                });
                await _db.SaveChangesAsync();
            }

            var khachMoi = new NguoiDung
            {
                MaVt = "KHACH_HANG",
                HoTen = hoTen.Trim(),
                Sdt = sdtChuan,
                Email = emailChuan,
                MatKhauHash = ThongTinPhienHelper.BamMatKhauSHA256("123456"),
                DiemTichLuy = 50, // Tặng 50 điểm mở thẻ tại quầy
                HangTv = "Đồng",
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _db.NguoiDungs.Add(khachMoi);
            await _db.SaveChangesAsync();

            return Json(new
            {
                thanhCong = true,
                thongBao = $"Đã tạo thẻ thành viên cho '{khachMoi.HoTen}' (Tặng +50 điểm mở thẻ, MK mặc định: 123456)!",
                khach = new
                {
                    maNd = khachMoi.MaNd,   
                    hoTen = khachMoi.HoTen,
                    sdt = khachMoi.Sdt,
                    email = khachMoi.Email,
                    hangTv = khachMoi.HangTv,
                    diemTichLuy = khachMoi.DiemTichLuy
                }
            });
        }

        // =====================================================================
        // 4. API THANH TOÁN TẠI QUẦY (CHỐT ĐƠN POS TRONG TRANSACTION)
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> ThanhToanTaiQuay([FromBody] PosCheckoutRequest? req)
        {
            if (req == null || req.DanhSachMon == null || !req.DanhSachMon.Any())
            {
                return Json(new { thanhCong = false, thongBao = "Hóa đơn hiện chưa có sản phẩm nào. Vui lòng chọn bánh hoặc hộp quà!" });
            }

            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Nạp dữ liệu Bánh, Combo, Vỏ hộp có tracking để kiểm tra và trừ tồn kho
                var dictBanh = await _db.SanPhamBanhs
                    .Include(b => b.LoSanXuatBanhs)
                    .Include(b => b.ThongSoDinhDuong)
                    .ToDictionaryAsync(b => b.MaBanh);

                var dictCombo = await _db.ComboMauCoSans
                    .Include(c => c.MaVhNavigation)
                    .Include(c => c.ChiTietComboMaus)
                        .ThenInclude(ct => ct.MaBanhNavigation)
                    .ToDictionaryAsync(c => c.MaCb);

                var dictVoHop = await _db.MauVoHops.ToDictionaryAsync(v => v.MaVh);

                // Tổng hợp nhu cầu tồn kho để kiểm tra chặt chẽ trước khi xuất
                var nhuCauBanh = new Dictionary<int, int>();
                var nhuCauVoHop = new Dictionary<int, int>();

                decimal tongTienHang = 0m;
                int tongSoHop = 0;

                foreach (var item in req.DanhSachMon)
                {
                    int sl = Math.Max(1, item.SoLuong);
                    if (item.LoaiSanPham == 1)
                    {
                        if (!item.MaBanh.HasValue || !dictBanh.TryGetValue(item.MaBanh.Value, out var banh))
                        {
                            return Json(new { thanhCong = false, thongBao = "Có sản phẩm bánh lẻ không tồn tại trong hệ thống!" });
                        }
                        nhuCauBanh[banh.MaBanh] = nhuCauBanh.GetValueOrDefault(banh.MaBanh) + sl;
                        tongTienHang += Math.Round(banh.DonGiaThucTe, 0) * sl;
                    }
                    else if (item.LoaiSanPham == 2)
                    {
                        if (!item.MaCb.HasValue || !dictCombo.TryGetValue(item.MaCb.Value, out var combo))
                        {
                            return Json(new { thanhCong = false, thongBao = "Có bộ Combo không tồn tại trong hệ thống!" });
                        }
                        nhuCauVoHop[combo.MaVh] = nhuCauVoHop.GetValueOrDefault(combo.MaVh) + sl;
                        foreach (var ct in combo.ChiTietComboMaus)
                        {
                            nhuCauBanh[ct.MaBanh] = nhuCauBanh.GetValueOrDefault(ct.MaBanh) + (ct.SoLuong * sl);
                        }
                        tongTienHang += Math.Round(combo.GiaUuDaiCombo, 0) * sl;
                        tongSoHop += sl;
                    }
                    else if (item.LoaiSanPham == 3)
                    {
                        int maVh = item.MaVh ?? dictVoHop.Keys.FirstOrDefault();
                        if (!dictVoHop.TryGetValue(maVh, out var voHop))
                        {
                            return Json(new { thanhCong = false, thongBao = "Mẫu vỏ hộp tự chọn không hợp lệ!" });
                        }
                        if (item.DanhSachMaBanhTrongHop == null || item.DanhSachMaBanhTrongHop.Count == 0)
                        {
                            return Json(new { thanhCong = false, thongBao = $"Hộp tự chọn '{voHop.TenVoHop}' chưa có bánh thành phần bên trong!" });
                        }

                        nhuCauVoHop[voHop.MaVh] = nhuCauVoHop.GetValueOrDefault(voHop.MaVh) + sl;
                        decimal giaMotHop = Math.Round(voHop.GiaVoHopRieng, 0);
                        foreach (var mb in item.DanhSachMaBanhTrongHop)
                        {
                            if (!dictBanh.TryGetValue(mb, out var bHop))
                            {
                                return Json(new { thanhCong = false, thongBao = "Bánh trong hộp tự chọn không tồn tại!" });
                            }
                            nhuCauBanh[bHop.MaBanh] = nhuCauBanh.GetValueOrDefault(bHop.MaBanh) + sl;
                            giaMotHop += Math.Round(bHop.DonGiaThucTe, 0);
                        }
                        tongTienHang += giaMotHop * sl;
                        tongSoHop += sl;
                    }
                }

                // Kiểm tra tồn kho Bánh lẻ & Hạn sử dụng lô
                foreach (var kv in nhuCauBanh)
                {
                    var banh = dictBanh[kv.Key];
                    if (banh.TongTonKho < kv.Value)
                    {
                        return Json(new
                        {
                            thanhCong = false,
                            thongBao = $"Sản phẩm [{banh.MaSku}] {banh.TenBanh} chỉ còn tồn {banh.TongTonKho} bánh (cần xuất {kv.Value} bánh)!"
                        });
                    }

                    bool coLoConHan = !banh.LoSanXuatBanhs.Any()
                        || banh.LoSanXuatBanhs.Any(l => l.HanSuDung >= homNay && l.TrangThaiLo != "HET_HAN" && l.SlconLai > 0);
                    if (!coLoConHan)
                    {
                        return Json(new
                        {
                            thanhCong = false,
                            thongBao = $"Sản phẩm [{banh.MaSku}] {banh.TenBanh} hiện chỉ còn lô đã hết hạn sử dụng, không thể xuất bán!"
                        });
                    }
                }

                // Kiểm tra tồn kho Vỏ hộp
                foreach (var kv in nhuCauVoHop)
                {
                    if (dictVoHop.TryGetValue(kv.Key, out var vh) && vh.SltonKhoVo < kv.Value)
                    {
                        return Json(new
                        {
                            thanhCong = false,
                            thongBao = $"Mẫu vỏ hộp [{vh.MaCodeVh}] {vh.TenVoHop} chỉ còn tồn {vh.SltonKhoVo} vỏ (cần {kv.Value} vỏ)!"
                        });
                    }
                }

                // Tính chiết khấu sỉ B2B (nếu tổng số hộp >= 10 hoặc đạt mốc trong BangChietKhauSiB2bs)
                var bangB2B = await _db.BangChietKhauSiB2bs.OrderBy(b => b.SlhopToiThieu).ToListAsync();
                var mocB2B = bangB2B.FirstOrDefault(b => tongSoHop >= b.SlhopToiThieu && tongSoHop <= b.SlhopToiDa)
                             ?? (tongSoHop > 0 && bangB2B.Any() && tongSoHop > bangB2B.Last().SlhopToiDa ? bangB2B.Last() : null);

                decimal phanTramCkB2B = mocB2B?.PhanTramChietKhau ?? 0m;
                decimal tienChietKhauB2B = phanTramCkB2B > 0 ? Math.Round(tongTienHang * (phanTramCkB2B / 100m), 0) : 0m;
                bool mienPhiInLogo = mocB2B?.MienPhiInLogo ?? false;
                decimal phiKhuonMacDinh = mocB2B?.PhiKhuonInLogo ?? (bangB2B.FirstOrDefault()?.PhiKhuonInLogo ?? 500000m);
                decimal phiInLogo = (req.YeuCauInLogo && !mienPhiInLogo) ? phiKhuonMacDinh : 0m;

                // Tính giảm giá Voucher (nếu có)
                decimal tienGiamVoucher = 0m;
                if (!string.IsNullOrWhiteSpace(req.CodeKm))
                {
                    string code = req.CodeKm.Trim().ToUpperInvariant();
                    var km = await _db.MaKhuyenMais.FirstOrDefaultAsync(k =>
                        k.CodeKm == code &&
                        k.NgayKetThuc >= DateTime.Now &&
                        k.SldaDung < k.SltoiDa);

                    if (km != null && tongTienHang >= km.DonToiThieu)
                    {
                        if (km.LoaiGiam == "PHAN_TRAM")
                        {
                            tienGiamVoucher = Math.Round(tongTienHang * (km.GiaTriGiam / 100m), 0);
                            if (km.GiamToiDa > 0 && tienGiamVoucher > km.GiamToiDa)
                                tienGiamVoucher = km.GiamToiDa;
                        }
                        else
                        {
                            tienGiamVoucher = km.GiaTriGiam;
                            if (km.GiamToiDa > 0 && tienGiamVoucher > km.GiamToiDa)
                                tienGiamVoucher = km.GiamToiDa;
                        }
                        km.SldaDung += 1;
                    }
                }

                bool laGiaoHang = string.Equals(req.CheDoMua, "GIAO_HANG_QR", StringComparison.OrdinalIgnoreCase);
                decimal phiVanChuyen = laGiaoHang ? 30000m : 0m;
                decimal tongThanhToanCuoi = Math.Max(0m, tongTienHang - tienChietKhauB2B - tienGiamVoucher + phiInLogo + phiVanChuyen);

                // Tạo mã đơn hàng tại quầy: POS26-XXXXX
                string maTraCuu;
                do
                {
                    maTraCuu = "POS26-" + Random.Shared.Next(10000, 99999);
                } while (await _db.DonHangs.AnyAsync(d => d.MaTraCuuDh == maTraCuu));

                // Xác định thông tin khách hàng
                NguoiDung? khachMember = null;
                if (req.MaNd.HasValue && req.MaNd.Value > 0)
                {
                    khachMember = await _db.NguoiDungs.FindAsync(req.MaNd.Value);
                }

                string hoTenMua = khachMember?.HoTen
                    ?? (!string.IsNullOrWhiteSpace(req.HoTenKhach) ? req.HoTenKhach.Trim() : "Khách lẻ tại quầy");
                string sdtMua = khachMember?.Sdt
                    ?? (!string.IsNullOrWhiteSpace(req.SdtKhach) ? req.SdtKhach.Trim() : "0900000000");
                string? emailMua = khachMember?.Email
                    ?? (!string.IsNullOrWhiteSpace(req.EmailKhach) ? req.EmailKhach.Trim() : null);

                var donHang = new DonHang
                {
                    MaTraCuuDh = maTraCuu,
                    MaNd = khachMember?.MaNd,
                    MaPhienKvl = null,
                    HoTenNguoiMua = hoTenMua,
                    SdtnguoiMua = sdtMua,
                    EmailNguoiMua = emailMua,
                    LaDonDoanhNghiepB2b = req.LaDonDoanhNghiepB2B || tongSoHop >= 10,
                    TenCongTy = !string.IsNullOrWhiteSpace(req.TenCongTy) ? req.TenCongTy.Trim() : null,
                    MaSoThue = !string.IsNullOrWhiteSpace(req.MaSoThue) ? req.MaSoThue.Trim() : null,
                    TongTienHang = tongTienHang,
                    PhanTramCkB2b = phanTramCkB2B,
                    TienChietKhauB2b = tienChietKhauB2B,
                    TienGiamVoucher = tienGiamVoucher,
                    PhiInLogo = phiInLogo,
                    TongPhiVanChuyen = phiVanChuyen,
                    TongThanhToanCuoi = tongThanhToanCuoi,
                    PhuongThucTt = string.IsNullOrWhiteSpace(req.PhuongThucTt) ? "TIEN_MAT" : req.PhuongThucTt,
                    TrangThaiTt = "DA_TT",
                    TrangThaiDh = laGiaoHang ? "CHO_LAY_HANG" : "HOAN_TAT",
                    NgayDat = DateTime.Now
                };

                _db.DonHangs.Add(donHang);
                await _db.SaveChangesAsync();

                // Nếu khách yêu cầu Giao hàng về nhà / Biếu tặng kèm Thiệp QR 3D
                int? maDotGh = null;
                string? maTokenQr = null;
                if (laGiaoHang)
                {
                    if (req.CoKemThiepQr)
                    {
                        maTokenQr = "QR-" + Guid.NewGuid().ToString("N").ToUpper()[..12];
                    }

                    DateOnly ngayGiao = homNay.AddDays(1);
                    if (!string.IsNullOrWhiteSpace(req.NgayHenGiao) && DateOnly.TryParse(req.NgayHenGiao, out var parsedDate) && parsedDate >= homNay)
                    {
                        ngayGiao = parsedDate;
                    }

                    var dotGiao = new DotGiaoHangDaDiem
                    {
                        MaDh = donHang.MaDh,
                        TenNguoiNhan = string.IsNullOrWhiteSpace(req.TenNguoiNhan) ? hoTenMua : req.TenNguoiNhan.Trim(),
                        SdtnguoiNhan = string.IsNullOrWhiteSpace(req.SdtNguoiNhan) ? sdtMua : req.SdtNguoiNhan.Trim(),
                        DiaChiGiaoChiTiet = string.IsNullOrWhiteSpace(req.DiaChiGiao) ? "Giao tận nơi theo yêu cầu tại quầy POS" : req.DiaChiGiao.Trim(),
                        NgayHenGiao = ngayGiao,
                        KhungGioGiao = string.IsNullOrWhiteSpace(req.KhungGioGiao) ? "Sáng (08:00 - 11:30)" : req.KhungGioGiao,
                        PhiShipDotNay = phiVanChuyen,
                        TrangThaiGiao = "CHO_DONG_GOI",
                        CoKemThiepQr = req.CoKemThiepQr,
                        MaTokenThiepQr = maTokenQr,
                        LoiChucThiep = req.CoKemThiepQr
                            ? (string.IsNullOrWhiteSpace(req.LoiChucThiep)
                                ? "Kính chúc Quý khách & Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!"
                                : req.LoiChucThiep.Trim())
                            : null,
                        MauGiaoDienThiep = string.IsNullOrWhiteSpace(req.MauGiaoDienThiep) ? "CO_PHONG_HOANG_GIA" : req.MauGiaoDienThiep,
                        SoLanMoThiep = 0
                    };

                    _db.DotGiaoHangDaDiems.Add(dotGiao);
                    await _db.SaveChangesAsync();
                    maDotGh = dotGiao.MaDotGh;
                }

                // Hàm phụ trừ tồn kho Bánh lẻ và trừ SlconLai của Lô theo chuẩn FEFO
                void TruTonKhoBanhVaLoFefo(SanPhamBanh banh, int soLuongCanTru)
                {
                    banh.TongTonKho = Math.Max(0, banh.TongTonKho - soLuongCanTru);
                    int conPhaiTru = soLuongCanTru;
                    var cacLoKhaDung = banh.LoSanXuatBanhs
                        .Where(l => l.SlconLai > 0 && l.TrangThaiLo != "HET_HAN")
                        .OrderBy(l => l.HanSuDung)
                        .ToList();

                    foreach (var lo in cacLoKhaDung)
                    {
                        if (conPhaiTru <= 0) break;
                        int truLoNay = Math.Min(lo.SlconLai, conPhaiTru);
                        lo.SlconLai -= truLoNay;
                        conPhaiTru -= truLoNay;
                    }
                }

                // Lưu ChiTietDonHangs & Trừ tồn kho thực tế
                foreach (var item in req.DanhSachMon)
                {
                    int sl = Math.Max(1, item.SoLuong);

                    if (item.LoaiSanPham == 1 && item.MaBanh.HasValue && dictBanh.TryGetValue(item.MaBanh.Value, out var banh))
                    {
                        decimal donGia = Math.Round(banh.DonGiaThucTe, 0);
                        TruTonKhoBanhVaLoFefo(banh, sl);

                        _db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDh = donHang.MaDh,
                            MaDotGh = maDotGh,
                            LoaiSanPham = 1,
                            MaBanh = banh.MaBanh,
                            TenSanPhamLuuCung = banh.TenBanh,
                            SoLuong = sl,
                            DonGiaLucMua = donGia,
                            ThanhTien = donGia * sl
                        });
                    }
                    else if (item.LoaiSanPham == 2 && item.MaCb.HasValue && dictCombo.TryGetValue(item.MaCb.Value, out var combo))
                    {
                        decimal donGia = Math.Round(combo.GiaUuDaiCombo, 0);
                        if (dictVoHop.TryGetValue(combo.MaVh, out var vhCombo))
                        {
                            vhCombo.SltonKhoVo = Math.Max(0, vhCombo.SltonKhoVo - sl);
                        }
                        foreach (var ct in combo.ChiTietComboMaus)
                        {
                            if (dictBanh.TryGetValue(ct.MaBanh, out var bThanhPhan))
                            {
                                TruTonKhoBanhVaLoFefo(bThanhPhan, ct.SoLuong * sl);
                            }
                        }

                        var dsBanhCombo = combo.ChiTietComboMaus
                            .OrderBy(x => x.ViTriO)
                            .Select(x => new
                            {
                                ViTriOTrongHop = x.ViTriO,
                                MaBanh = x.MaBanh,
                                TenBanh = x.MaBanhNavigation?.TenBanh ?? "Bánh Trung Thu",
                                DonGia = Math.Round(x.MaBanhNavigation?.DonGiaLe ?? 0m, 0)
                            })
                            .ToList();

                        _db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDh = donHang.MaDh,
                            MaDotGh = maDotGh,
                            LoaiSanPham = 2,
                            MaCb = combo.MaCb,
                            TenSanPhamLuuCung = combo.TenCombo,
                            CauHinhHopJson = JsonSerializer.Serialize(dsBanhCombo, jsonOptions),
                            SoLuong = sl,
                            DonGiaLucMua = donGia,
                            ThanhTien = donGia * sl
                        });
                    }
                    else if (item.LoaiSanPham == 3)
                    {
                        int maVh = item.MaVh ?? dictVoHop.Keys.First();
                        var voHop = dictVoHop[maVh];
                        voHop.SltonKhoVo = Math.Max(0, voHop.SltonKhoVo - sl);

                        int tongCalo = 0;
                        decimal tongGiaMotHop = Math.Round(voHop.GiaVoHopRieng, 0);
                        var dsBanhTrongHopJson = new List<object>();

                        foreach (var mb in item.DanhSachMaBanhTrongHop)
                        {
                            var bHop = dictBanh[mb];
                            tongCalo += bHop.ThongSoDinhDuong?.NangLuongCalo ?? 380;
                            tongGiaMotHop += Math.Round(bHop.DonGiaThucTe, 0);
                        }

                        string maBam = $"POS-VH{voHop.MaVh}-" + string.Join("-", item.DanhSachMaBanhTrongHop);
                        string tenHop = string.IsNullOrWhiteSpace(item.TenHopTuChon)
                            ? $"Hộp Tự Chọn Tại Quầy ({voHop.TenVoHop})"
                            : item.TenHopTuChon.Trim();

                        var hopTc = new HopQuaTuChon
                        {
                            MaVh = voHop.MaVh,
                            MaNd = khachMember?.MaNd,
                            MaPhienKvl = null,
                            TenHopKhachDat = tenHop,
                            MaBamCauHinh = maBam.Length > 140 ? maBam[..140] : maBam,
                            TongCaloHop = tongCalo,
                            TongGiaMotHop = tongGiaMotHop,
                            DaLuuMauYeuThich = false,
                            NgayTao = DateTime.Now
                        };
                        _db.HopQuaTuChons.Add(hopTc);
                        await _db.SaveChangesAsync();

                        for (int viTri = 0; viTri < item.DanhSachMaBanhTrongHop.Count; viTri++)
                        {
                            int mb = item.DanhSachMaBanhTrongHop[viTri];
                            var bHop = dictBanh[mb];
                            decimal giaBanh = Math.Round(bHop.DonGiaThucTe, 0);

                            _db.ChiTietHopTuChons.Add(new ChiTietHopTuChon
                            {
                                MaHopTc = hopTc.MaHopTc,
                                MaBanh = bHop.MaBanh,
                                ViTriOtrongHop = viTri + 1,
                                DonGiaBanhLucChon = giaBanh
                            });

                            dsBanhTrongHopJson.Add(new
                            {
                                ViTriOTrongHop = viTri + 1,
                                MaBanh = bHop.MaBanh,
                                TenBanh = bHop.TenBanh,
                                DonGia = giaBanh
                            });

                            TruTonKhoBanhVaLoFefo(bHop, sl);
                        }

                        _db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDh = donHang.MaDh,
                            MaDotGh = maDotGh,
                            LoaiSanPham = 3,
                            MaHopTc = hopTc.MaHopTc,
                            TenSanPhamLuuCung = $"{tenHop} ({voHop.TenVoHop})",
                            CauHinhHopJson = JsonSerializer.Serialize(dsBanhTrongHopJson, jsonOptions),
                            SoLuong = sl,
                            DonGiaLucMua = tongGiaMotHop,
                            ThanhTien = tongGiaMotHop * sl
                        });
                    }
                }

                // Cộng điểm tích lũy cho thành viên (+1 điểm cho mỗi 10,000đ)
                int diemCong = 0;
                if (khachMember != null)
                {
                    diemCong = (int)(tongThanhToanCuoi / 10000m);
                    khachMember.DiemTichLuy += diemCong;
                    if (khachMember.DiemTichLuy >= 1500) khachMember.HangTv = "Kim Cương";
                    else if (khachMember.DiemTichLuy >= 500) khachMember.HangTv = "Vàng";
                    else if (khachMember.DiemTichLuy >= 200) khachMember.HangTv = "Bạc";
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    thanhCong = true,
                    maDon = donHang.MaTraCuuDh,
                    maDh = donHang.MaDh,
                    ngayDat = donHang.NgayDat.ToString("dd/MM/yyyy HH:mm"),
                    hoTenKhach = donHang.HoTenNguoiMua,
                    sdtKhach = donHang.SdtnguoiMua,
                    tongTienHang = donHang.TongTienHang,
                    tienChietKhauB2B = donHang.TienChietKhauB2b,
                    tienGiamVoucher = donHang.TienGiamVoucher,
                    phiInLogo = donHang.PhiInLogo,
                    phiVanChuyen = donHang.TongPhiVanChuyen,
                    tongThanhToan = donHang.TongThanhToanCuoi,
                    phuongThucTt = donHang.PhuongThucTt,
                    cheDoMua = laGiaoHang ? "GIAO_HANG_QR" : "TAI_QUAY",
                    maTokenQr = maTokenQr,
                    diemCong = diemCong
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new
                {
                    thanhCong = false,
                    thongBao = $"Lỗi xử lý thanh toán POS: {ex.Message}"
                });
            }
        }

        // =====================================================================
        // 5. API KÍCH HOẠT XẢ KHO NHANH -30% CHO LÔ CẬN DATE (NHÁNH 5.0.2)
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> KichHoatXaKhoNhanh(int maLo, int mucGiam = 30)
        {
            var lo = await _db.LoSanXuatBanhs
                .Include(l => l.MaBanhNavigation)
                .FirstOrDefaultAsync(l => l.MaLo == maLo);

            if (lo == null)
            {
                return Json(new { thanhCong = false, thongBao = "Không tìm thấy lô sản xuất cần kích hoạt xả kho!" });
            }

            int phanTram = Math.Clamp(mucGiam, 1, 90);
            lo.PhanTramGiamCanDate = phanTram;
            lo.TrangThaiLo = "XA_CAN_DATE";
            await _db.SaveChangesAsync();

            decimal giaGoc = lo.MaBanhNavigation?.DonGiaLe ?? 0m;
            decimal giaMoi = Math.Round(giaGoc * (100m - phanTram) / 100m, 0);

            return Json(new
            {
                thanhCong = true,
                thongBao = "Đã kích hoạt ưu đãi xả kho -30% cho lô cận date!",
                maLo = lo.MaLo,
                maLoCode = lo.MaLoCode,
                maBanh = lo.MaBanh,
                tenBanh = lo.MaBanhNavigation?.TenBanh ?? "",
                mucGiam = phanTram,
                giaGoc = Math.Round(giaGoc, 0),
                giaMoi = giaMoi
            });
        }
    }
}

