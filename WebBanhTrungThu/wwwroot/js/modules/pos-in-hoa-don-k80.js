// =========================================================================
// MODULE POS: QUẢN LÝ CA THU NGÂN, XẢ KHO NHANH & IN HÓA ĐƠN NHIỆT K80
// =========================================================================
(function () {
    const cfg = window.POS_CATALOG || {};
    const DS_BANH_POS = Array.isArray(cfg.dsBanhPos) ? cfg.dsBanhPos : [];
    let dsBanhDangChonTrongHop = [];

    function dinhDangN0(n) {
        return Math.round(Number(n) || 0).toLocaleString('en-US');
    }

    window.moModalDongHopTuChonPos = function () {
        dsBanhDangChonTrongHop = [];
        document.getElementById('inpPosTenHopTuChon').value = '';
        renderSlotHopTuChonPos();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalDongHopPos')).show();
    };

    window.doiVoHopTuChonPos = function () {
        const sel = document.getElementById('selPosVoHopTuChon');
        const opt = sel.options[sel.selectedIndex];
        const sucChua = parseInt(opt.getAttribute('data-succhua') || '4');
        if (dsBanhDangChonTrongHop.length > sucChua) {
            dsBanhDangChonTrongHop = dsBanhDangChonTrongHop.slice(0, sucChua);
        }
        renderSlotHopTuChonPos();
    };

    window.themBanhVaoHopTuChonPos = function (maBanh) {
        const sel = document.getElementById('selPosVoHopTuChon');
        const opt = sel.options[sel.selectedIndex];
        const sucChua = parseInt(opt.getAttribute('data-succhua') || '4');

        if (dsBanhDangChonTrongHop.length >= sucChua) {
            alert(`Mẫu vỏ hộp này có sức chứa tối đa ${sucChua} bánh!`);
            return;
        }
        const banh = DS_BANH_POS.find(b => b.maBanh === maBanh);
        if (!banh) return;
        dsBanhDangChonTrongHop.push(banh);
        renderSlotHopTuChonPos();
    };

    window.xoaBanhKhoiSlotHopPos = function (idx) {
        dsBanhDangChonTrongHop.splice(idx, 1);
        renderSlotHopTuChonPos();
    };

    window.xoaHetBanhTrongHopPos = function () {
        dsBanhDangChonTrongHop = [];
        renderSlotHopTuChonPos();
    };

    function renderSlotHopTuChonPos() {
        const sel = document.getElementById('selPosVoHopTuChon');
        if (!sel) return;
        const opt = sel.options[sel.selectedIndex];
        const sucChua = parseInt(opt.getAttribute('data-succhua') || '4');
        const giaVo = Number(opt.getAttribute('data-giavo') || 0);

        document.getElementById('lblPosDemBanhHop').innerText = `${dsBanhDangChonTrongHop.length}/${sucChua}`;
        const listEl = document.getElementById('listSlotHopPos');
        listEl.innerHTML = '';

        let tongGiaHop = giaVo;
        for (let i = 0; i < sucChua; i++) {
            const b = dsBanhDangChonTrongHop[i];
            const div = document.createElement('div');
            div.className = 'p-2 rounded border d-flex justify-content-between align-items-center ' + (b ? 'bg-white' : 'bg-light text-muted');
            if (b) {
                tongGiaHop += b.giaBan;
                div.innerHTML = `
                    <span class="small"><strong>Ô ${i + 1}:</strong> ${b.ten} (${dinhDangN0(b.giaBan)} đ)</span>
                    <button type="button" class="btn btn-sm text-danger p-0" onclick="xoaBanhKhoiSlotHopPos(${i})"><i class="bi bi-x-circle"></i></button>
                `;
            } else {
                div.innerHTML = `<span class="small">Ô ${i + 1}: Chưa chọn bánh (Chạm bánh bên phải)</span>`;
            }
            listEl.appendChild(div);
        }

        document.getElementById('lblPosTongGiaHopTuChon').innerText = dinhDangN0(tongGiaHop) + ' đ';
    }

    window.xacNhanThemHopTuChonVaoPos = function () {
        const sel = document.getElementById('selPosVoHopTuChon');
        const opt = sel.options[sel.selectedIndex];
        const maVh = parseInt(sel.value);
        const tenVo = opt.getAttribute('data-ten') || 'Vỏ hộp tự chọn';
        const sucChua = parseInt(opt.getAttribute('data-succhua') || '4');
        const giaVo = Number(opt.getAttribute('data-giavo') || 0);

        if (dsBanhDangChonTrongHop.length < sucChua) {
            alert(`Vui lòng chọn đủ ${sucChua} bánh cho mẫu vỏ hộp "${tenVo}" (Hiện mới chọn ${dsBanhDangChonTrongHop.length}/${sucChua} bánh)!`);
            return;
        }

        const tenHopNhap = (document.getElementById('inpPosTenHopTuChon').value || '').trim();
        const tenHienThi = tenHopNhap || `Hộp Tự Chọn Tại Quầy (${tenVo})`;
        const donGiaHop = giaVo + dsBanhDangChonTrongHop.reduce((s, b) => s + b.giaBan, 0);
        const moTaBanh = dsBanhDangChonTrongHop.map((b, i) => `Ô ${i + 1}: ${b.ten}`).join(', ');

        if (typeof window.themMonHopTuChonVaoTabPos === 'function') {
            window.themMonHopTuChonVaoTabPos({
                loaiSanPham: 3,
                maVh: maVh,
                tenHopTuChon: tenHienThi,
                danhSachMaBanhTrongHop: dsBanhDangChonTrongHop.map(b => b.maBanh),
                maHang: `HOP-TC${maVh}`,
                tenHang: tenHienThi,
                chiTietPhu: `${tenVo} (${moTaBanh})`,
                donViTinh: 'Hộp',
                donGia: donGiaHop,
                soLuong: 1
            });
        }

        bootstrap.Modal.getInstance(document.getElementById('modalDongHopPos'))?.hide();
    };

    window.moModalThemKhachPos = function () {
        document.getElementById('inpPosNewCustName').value = '';
        document.getElementById('inpPosNewCustPhone').value = '';
        document.getElementById('inpPosNewCustEmail').value = '';
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalThemKhachPos')).show();
    };

    window.luuKhachHangNhanhPos = async function () {
        const hoTen = (document.getElementById('inpPosNewCustName').value || '').trim();
        const sdt = (document.getElementById('inpPosNewCustPhone').value || '').trim();
        const email = (document.getElementById('inpPosNewCustEmail').value || '').trim();

        if (!hoTen || !sdt) {
            alert('Vui lòng nhập đầy đủ Họ tên và Số điện thoại khách hàng!');
            return;
        }

        const fd = new FormData();
        fd.append('hoTen', hoTen);
        fd.append('sdt', sdt);
        fd.append('email', email);

        try {
            const resp = await fetch('/Admin/BanHangPos/ThemKhachHangNhanhPos', {
                method: 'POST',
                body: fd
            });
            const res = await resp.json();
            if (!res.thanhCong) {
                alert(res.thongBao || 'Không thể tạo khách hàng!');
                return;
            }
            bootstrap.Modal.getInstance(document.getElementById('modalThemKhachPos'))?.hide();
            if (typeof window.chonKhachHangVaoTabPos === 'function') {
                window.chonKhachHangVaoTabPos(res.khach);
            }
        } catch (e) {
            alert('Lỗi kết nối máy chủ!');
        }
    };

    window.thucHienThanhToanPos = async function () {
        if (typeof window.luuThongTinTabHienTai === 'function') {
            window.luuThongTinTabHienTai();
        }
        const tab = typeof window.layTabPosHienTai === 'function' ? window.layTabPosHienTai() : null;

        if (!tab || !tab.items || tab.items.length === 0) {
            alert('Hóa đơn chưa có sản phẩm nào. Vui lòng chọn sản phẩm trước khi thanh toán!');
            return;
        }

        if (tab.cheDoMua === 'GIAO_HANG_QR') {
            if (!tab.giaoHang.tenNguoiNhan.trim() || !tab.giaoHang.sdtNguoiNhan.trim() || !tab.giaoHang.diaChiGiao.trim()) {
                alert('Vui lòng nhập đầy đủ Họ tên, SĐT và Địa chỉ người nhận cho đơn đặt giao hàng!');
                return;
            }
        }

        const btn = document.getElementById('btnPosThanhToanF9');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> ĐANG XỬ LÝ THANH TOÁN...';

        const khachCanTraHienTai = typeof window.layKhachCanTraPosHienTai === 'function' ? window.layKhachCanTraPosHienTai() : 0;

        const payload = {
            maNd: tab.khachHang ? tab.khachHang.maNd : null,
            hoTenKhach: tab.khachHang ? tab.khachHang.hoTen : 'Khách lẻ tại quầy',
            sdtKhach: tab.khachHang ? tab.khachHang.sdt : '0900000000',
            emailKhach: tab.khachHang ? tab.khachHang.email : null,
            cheDoMua: tab.cheDoMua,
            laDonDoanhNghiepB2B: tab.yeuCauInLogo,
            yeuCauInLogo: tab.yeuCauInLogo,
            codeKm: tab.codeKm || null,
            phuongThucTt: tab.phuongThucTt || 'TIEN_MAT',
            tienKhachDua: Number(document.getElementById('inpPosTienKhachDua').value) || khachCanTraHienTai,
            tenNguoiNhan: tab.giaoHang.tenNguoiNhan,
            sdtNguoiNhan: tab.giaoHang.sdtNguoiNhan,
            diaChiGiao: tab.giaoHang.diaChiGiao,
            ngayHenGiao: tab.giaoHang.ngayHenGiao,
            khungGioGiao: tab.giaoHang.khungGioGiao,
            coKemThiepQr: tab.giaoHang.coKemThiepQr,
            loiChucThiep: tab.giaoHang.loiChucThiep,
            danhSachMon: tab.items.map(i => ({
                loaiSanPham: i.loaiSanPham,
                maBanh: i.maBanh || null,
                maCb: i.maCb || null,
                maVh: i.maVh || null,
                tenHopTuChon: i.tenHopTuChon || null,
                danhSachMaBanhTrongHop: i.danhSachMaBanhTrongHop || [],
                soLuong: i.soLuong
            }))
        };

        try {
            const resp = await fetch('/Admin/BanHangPos/ThanhToanTaiQuay', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const res = await resp.json();

            if (!res.thanhCong) {
                alert(res.thongBao || 'Thanh toán không thành công!');
                return;
            }

            hienThiPhieuInHoaDonK80(res, tab, payload);
            if (typeof window.dongTabHoaDon === 'function') {
                window.dongTabHoaDon(tab.id);
            }
        } catch (err) {
            alert('Lỗi kết nối khi thanh toán tại quầy!');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-printer-fill"></i> THANH TOÁN (F9)';
        }
    };

    function hienThiPhieuInHoaDonK80(res, tab, payload) {
        document.getElementById('rcpMaDon').innerText = res.maDon;
        document.getElementById('rcpNgayDat').innerText = res.ngayDat;
        document.getElementById('rcpKhachHang').innerText = `${res.hoTenKhach} - ${res.sdtKhach}${res.diemCong > 0 ? ` (+${res.diemCong} điểm)` : ''}`;
        document.getElementById('rcpHinhThuc').innerText = res.cheDoMua === 'GIAO_HANG_QR'
            ? 'Đặt giao hàng tận nơi & Thiệp QR 3D'
            : 'Khách nhận hàng ngay tại quầy';

        const rcpTbody = document.getElementById('rcpTbodyItems');
        rcpTbody.innerHTML = '';
        tab.items.forEach(item => {
            let dongConHtml = '';
            if (item.loaiSanPham === 3 && Array.isArray(item.danhSachMaBanhTrongHop) && item.danhSachMaBanhTrongHop.length > 0) {
                item.danhSachMaBanhTrongHop.forEach((mb, idxO) => {
                    const banhO = DS_BANH_POS.find(b => b.maBanh === mb);
                    const tenBanhO = banhO ? banhO.ten : `Bánh #${mb}`;
                    dongConHtml += `<div class="text-muted" style="font-size: 10.5px; padding-left: 8px;">- Ô ${idxO + 1}: ${tenBanhO}</div>`;
                });
            } else if (item.chiTietPhu) {
                dongConHtml = `<div class="text-muted" style="font-size: 10.5px; padding-left: 8px;">${item.chiTietPhu}</div>`;
            }

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>
                    <div class="fw-semibold">${item.tenHang}</div>
                    ${dongConHtml}
                </td>
                <td class="text-center">${item.soLuong}</td>
                <td class="text-end fw-semibold so-tien">${dinhDangN0(item.donGia * item.soLuong)} đ</td>
            `;
            rcpTbody.appendChild(tr);
        });

        document.getElementById('rcpTongTienHang').innerText = dinhDangN0(res.tongTienHang) + ' đ';

        const rowCk = document.getElementById('rcpRowCkB2B');
        if (res.tienChietKhauB2B > 0) {
            rowCk.style.display = 'flex';
            document.getElementById('rcpTienCkB2B').innerText = '-' + dinhDangN0(res.tienChietKhauB2B) + ' đ';
        } else {
            rowCk.style.display = 'none';
        }

        const rowVc = document.getElementById('rcpRowVoucher');
        if (res.tienGiamVoucher > 0) {
            rowVc.style.display = 'flex';
            document.getElementById('rcpTienVoucher').innerText = '-' + dinhDangN0(res.tienGiamVoucher) + ' đ';
        } else {
            rowVc.style.display = 'none';
        }

        const phuThu = (res.phiInLogo || 0) + (res.phiVanChuyen || 0);
        const rowShip = document.getElementById('rcpRowShip');
        if (phuThu > 0) {
            rowShip.style.display = 'flex';
            document.getElementById('rcpPhiPhuThu').innerText = '+' + dinhDangN0(phuThu) + ' đ';
        } else {
            rowShip.style.display = 'none';
        }

        document.getElementById('rcpTongThanhToan').innerText = dinhDangN0(res.tongThanhToan) + ' đ';
        const mapPtTen = {
            'TIEN_MAT': 'Tiền mặt',
            'CHUYEN_KHOAN': 'Chuyển khoản VietQR',
            'THE_NGAN_HANG': 'Quẹt thẻ ngân hàng'
        };
        document.getElementById('rcpPhuongThuc').innerText = mapPtTen[res.phuongThucTt] || res.phuongThucTt;

        const tienDua = Math.max(res.tongThanhToan, payload.tienKhachDua || res.tongThanhToan);
        const tienThua = Math.max(0, tienDua - res.tongThanhToan);
        document.getElementById('rcpTienKhachDua').innerText = dinhDangN0(tienDua) + ' đ';
        document.getElementById('rcpTienThua').innerText = dinhDangN0(tienThua) + ' đ';

        const urlQrMucTieu = res.maTokenQr
            ? `${window.location.origin}/Thiep3D/Xem/${res.maTokenQr}`
            : `${window.location.origin}/ThanhToan/ThanhCong?maDon=${encodeURIComponent(res.maDon)}`;
        const rcpQrImg = document.getElementById('rcpQrImgK80');
        if (rcpQrImg) {
            rcpQrImg.src = `https://api.qrserver.com/v1/create-qr-code/?size=110x110&data=${encodeURIComponent(urlQrMucTieu)}`;
        }

        const boxQr = document.getElementById('rcpBoxThiepQr');
        if (res.maTokenQr) {
            boxQr.style.display = 'block';
            document.getElementById('rcpMaTokenQr').innerText = res.maTokenQr;
            document.getElementById('rcpLinkThiepQr').href = `/Thiep3D/Xem/${res.maTokenQr}`;
        } else {
            boxQr.style.display = 'none';
        }

        document.getElementById('btnMoTrangInChuanKiot').href = `/Admin/DonHangAdmin/InHoaDon/${res.maDh}`;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalPhieuInPos')).show();
    }

    window.kichHoatXaKhoNhanhPos = async function (maLo, maBanh, nutBam) {
        if (!nutBam) return;
        nutBam.disabled = true;
        nutBam.innerText = 'Đang áp giá -30%...';

        try {
            const resp = await fetch(`/Admin/BanHangPos/KichHoatXaKhoNhanh?maLo=${maLo}&mucGiam=30`, {
                method: 'POST'
            });
            const data = await resp.json();
            if (data && data.thanhCong) {
                nutBam.classList.add('da-ap-gia-xa');
                nutBam.innerText = '✓ Đã áp giá xả kho -30%';

                if (typeof window.capNhatGiaBanhXaKhoTrongTabsPos === 'function') {
                    window.capNhatGiaBanhXaKhoTrongTabsPos(maBanh, data.mucGiam || 30, data.giaMoi);
                }
            } else {
                nutBam.disabled = false;
                nutBam.innerText = '⚡ Kích hoạt xả kho -30%';
                alert((data && data.thongBao) || 'Không thể kích hoạt xả kho!');
            }
        } catch (err) {
            nutBam.disabled = false;
            nutBam.innerText = '⚡ Kích hoạt xả kho -30%';
            alert('Lỗi kết nối máy chủ khi kích hoạt xả kho!');
        }
    };

    document.addEventListener('keydown', function (e) {
        if (e.key === 'F3') {
            e.preventDefault();
            document.getElementById('txtPosTimSanPham')?.focus();
        } else if (e.key === 'F4') {
            e.preventDefault();
            document.getElementById('txtPosTimKhach')?.focus();
        } else if (e.key === 'F6') {
            e.preventDefault();
            if (typeof window.moModalDongHopTuChonPos === 'function') window.moModalDongHopTuChonPos();
        } else if (e.key === 'F9') {
            e.preventDefault();
            window.thucHienThanhToanPos();
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        damBaoModalPhuPosTonTai();
    });

    function damBaoModalPhuPosTonTai() {
        if (!document.getElementById('modalThemKhachPos')) {
            document.body.insertAdjacentHTML('beforeend', `
<div class="modal fade" id="modalThemKhachPos" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-sm modal-dialog-centered">
    <div class="modal-content border-0 shadow">
      <div class="modal-header py-2 px-3 bg-light">
        <h6 class="modal-title fw-bold mb-0"><i class="bi bi-person-plus-fill text-primary me-1"></i> Mở thẻ thành viên tại quầy</h6>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body p-3">
        <div class="mb-2"><label class="form-label small fw-bold mb-1">Họ tên khách hàng <span class="text-danger">*</span></label><input type="text" id="inpPosNewCustName" class="form-control form-control-sm" placeholder="VD: Nguyễn Minh Anh" /></div>
        <div class="mb-2"><label class="form-label small fw-bold mb-1">Số điện thoại <span class="text-danger">*</span></label><input type="text" id="inpPosNewCustPhone" class="form-control form-control-sm" placeholder="VD: 0909123456" /></div>
        <div class="mb-1"><label class="form-label small fw-bold mb-1">Email (tùy chọn)</label><input type="email" id="inpPosNewCustEmail" class="form-control form-control-sm" placeholder="khachhang@gmail.com" /></div>
        <small class="text-success d-block mt-1">🎁 Tự động tặng <strong>+50 điểm</strong> mở thẻ mới (MK: <code>123456</code>).</small>
      </div>
      <div class="modal-footer py-2 px-3 bg-light">
        <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Bỏ qua</button>
        <button type="button" class="btn btn-primary btn-sm fw-bold" onclick="luuKhachHangNhanhPos()">Lưu &amp; Chọn khách</button>
      </div>
    </div>
  </div>
</div>`);
        }
        if (!document.getElementById('modalPhieuInPos')) {
            document.body.insertAdjacentHTML('beforeend', `
<div class="modal fade" id="modalPhieuInPos" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-dialog-centered" style="max-width: 390px;">
    <div class="modal-content border-0 shadow-lg">
      <div class="modal-header py-2 px-3" style="background: #00B63E; color: #fff;">
        <h6 class="modal-title fw-bold mb-0"><i class="bi bi-check-circle-fill me-1"></i> Hóa đơn nhiệt K80 - Nguyệt Cát ERP POS</h6>
        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body p-3" id="areaInHoaDonPos">
        <div class="hoa-don-nhiet-k80">
          <div class="text-center">
            <div class="fw-bold" style="font-size: 13.5px; letter-spacing: 0.2px;">NGUYỆT CÁT – TINH HOA TRĂNG RẰM</div>
            <div style="font-size: 11px;">128 Nguyễn Huệ, Quận 1, TP.HCM • Hotline: 1900 6886</div>
          </div>
          <div class="duong-ke-net-dut"></div>
          <div class="text-center">
            <div class="fw-bold" style="font-size: 13px;">HÓA ĐƠN BÁN HÀNG / PHIẾU GIAO QUÀ</div>
            <div style="font-size: 11px;">Số HĐ: <strong id="rcpMaDon">POS26-00000</strong> • <span id="rcpNgayDat">---</span></div>
          </div>
          <div class="mt-1" style="font-size: 11.5px;">
            <div>Khách hàng: <strong id="rcpKhachHang">Khách lẻ tại quầy</strong></div>
            <div>Hình thức: <strong id="rcpHinhThuc">Nhận hàng ngay tại quầy</strong></div>
          </div>
          <div class="duong-ke-net-dut"></div>
          <table>
            <thead>
              <tr>
                <th class="text-start">Tên món</th>
                <th class="text-center" style="width: 34px;">SL</th>
                <th class="text-end" style="width: 78px;">Thành tiền</th>
              </tr>
            </thead>
            <tbody id="rcpTbodyItems"></tbody>
          </table>
          <div class="mt-2" style="font-size: 11.5px;">
            <div class="d-flex justify-content-between"><span>Tổng tiền hàng:</span><strong class="so-tien" id="rcpTongTienHang">0 đ</strong></div>
            <div class="d-flex justify-content-between" id="rcpRowCkB2B" style="display:none;"><span>Chiết khấu sỉ B2B:</span><strong class="so-tien" id="rcpTienCkB2B">-0 đ</strong></div>
            <div class="d-flex justify-content-between" id="rcpRowVoucher" style="display:none;"><span>Chiết khấu / Voucher:</span><strong class="so-tien" id="rcpTienVoucher">-0 đ</strong></div>
            <div class="d-flex justify-content-between" id="rcpRowShip" style="display:none;"><span>Phí giao hàng:</span><strong class="so-tien" id="rcpPhiPhuThu">0 đ</strong></div>
            <div class="duong-ke-net-dut"></div>
            <div class="d-flex justify-content-between fw-bold" style="font-size: 14px;"><span>TỔNG THANH TOÁN:</span><span class="so-tien" id="rcpTongThanhToan">0 đ</span></div>
            <div class="d-flex justify-content-between mt-1" style="font-size: 11px;"><span>Khách thanh toán (<span id="rcpPhuongThuc">Tiền mặt</span>):</span><strong class="so-tien" id="rcpTienKhachDua">0 đ</strong></div>
            <div class="d-flex justify-content-between" style="font-size: 11px;"><span>Tiền thừa trả khách:</span><strong class="so-tien" id="rcpTienThua">0 đ</strong></div>
          </div>
          <div class="duong-ke-net-dut"></div>
          <div class="text-center mt-2">
            <div class="d-inline-block p-1 border rounded bg-white mb-1"><img id="rcpQrImgK80" src="" width="110" height="110" alt="Mã QR Hóa Đơn K80" /></div>
            <div id="rcpBoxThiepQr" class="mb-1" style="display: none; font-size: 11px;">
              <span class="fw-semibold">Token Thiệp 3D: <code id="rcpMaTokenQr">---</code></span>
              <a id="rcpLinkThiepQr" href="#" target="_blank" class="ms-1 text-decoration-none">[Mở Thiệp 3D]</a>
            </div>
            <div class="fst-italic" style="font-size: 10.5px; line-height: 1.35;">"Quét mã QR để mở Thiệp Chúc Trung Thu 3D &amp; Tra cứu hành trình đơn hàng. Cảm ơn Quý khách!"</div>
          </div>
        </div>
      </div>
      <div class="modal-footer py-2 px-3 bg-light justify-content-between">
        <a id="btnMoTrangInChuanKiot" href="#" target="_blank" class="btn btn-outline-primary btn-sm"><i class="bi bi-printer"></i> Mở trang in K80 riêng</a>
        <div class="d-flex gap-2">
          <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Đóng</button>
          <button type="button" class="btn btn-success btn-sm fw-bold px-3" onclick="window.print()">🖨️ In Hóa Đơn K80</button>
        </div>
      </div>
    </div>
  </div>
</div>`);
        }
    }
})();
