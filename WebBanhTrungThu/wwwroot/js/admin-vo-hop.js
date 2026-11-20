(function () {
    window.locVoHopClient = function () {
        const tk = (document.getElementById('txtTimVoHop')?.value || '').trim().toLowerCase();
        const sucChua = parseInt(document.querySelector('input[name="radVhSucChua"]:checked')?.value || '0', 10);
        const ttTon = document.querySelector('input[name="radVhTon"]:checked')?.value || 'ALL';

        let soDongHien = 0;
        document.querySelectorAll('#tbodyVoHop tr.kv-master-row').forEach(tr => {
            let hien = true;
            if (tk && !(tr.getAttribute('data-search') || '').includes(tk)) hien = false;

            if (hien && sucChua > 0) {
                const sc = parseInt(tr.getAttribute('data-succhua') || '0', 10);
                if (sucChua >= 8) {
                    if (sc < 8) hien = false;
                } else if (sc !== sucChua) {
                    hien = false;
                }
            }

            if (hien && ttTon !== 'ALL') {
                const ton = parseInt(tr.getAttribute('data-ton') || '0', 10);
                const tt = tr.getAttribute('data-trangthai') || '';
                if (ttTon === 'SAP_HET' && ton > 20) hien = false;
                else if (ttTon === 'CON_HANG' && ton <= 20) hien = false;
                else if (ttTon === 'NGUNG_KD' && tt !== 'NGUNG_KD') hien = false;
            }

            tr.style.display = hien ? '' : 'none';
            if (hien) soDongHien++;
        });

        const rowTrong = document.getElementById('rowTrongVoHop');
        if (rowTrong) rowTrong.style.display = soDongHien === 0 ? '' : 'none';
    };

    window.moModalTaoVoHop = function () {
        document.getElementById('vh_MaVh').value = '0';
        document.getElementById('vh_ModalTitle').innerText = 'Thêm mẫu vỏ hộp mới';
        document.getElementById('vh_MaCodeVh').value = '';
        document.getElementById('vh_TenVoHop').value = '';
        document.getElementById('vh_SucChuaBanh').value = '4';
        document.getElementById('vh_KieuBoCucLuoi').value = 'grid-2x2';
        document.getElementById('vh_GiaVoHopRieng').value = '150000';
        document.getElementById('vh_SlVip').value = '0';
        document.getElementById('vh_MaAnhChinh').value = '';
        document.getElementById('vh_FileAnhTaiLen').value = '';
        document.getElementById('vh_UrlAnhNhapTay').value = '';
        document.getElementById('vh_PreviewImg').src = '/images/vohop/khay-4-o-rong.png';
        document.getElementById('vh_MoTaChatLieu').value = '';
        document.getElementById('vh_KemTraOlong').checked = false;
        document.getElementById('vh_TrangThai').checked = true;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalLuuVoHop')).show();
    };

    window.moModalSuaVoHop = function (btn) {
        const d = btn.dataset;
        document.getElementById('vh_MaVh').value = d.mavh || '0';
        document.getElementById('vh_ModalTitle').innerText = `Cập nhật mẫu vỏ hộp ${d.macode || ''}`;
        document.getElementById('vh_MaCodeVh').value = d.macode || '';
        document.getElementById('vh_TenVoHop').value = d.ten || '';
        document.getElementById('vh_SucChuaBanh').value = d.succhua || '4';
        document.getElementById('vh_KieuBoCucLuoi').value = d.luoi || 'grid-2x2';
        document.getElementById('vh_GiaVoHopRieng').value = d.gia || '0';
        document.getElementById('vh_SlVip').value = d.vip || '0';
        document.getElementById('vh_MaAnhChinh').value = d.maanh || '';
        document.getElementById('vh_FileAnhTaiLen').value = '';
        document.getElementById('vh_UrlAnhNhapTay').value = '';
        document.getElementById('vh_PreviewImg').src = d.urlanh || '/images/vohop/khay-4-o-rong.png';
        document.getElementById('vh_MoTaChatLieu').value = d.mota || '';
        document.getElementById('vh_KemTraOlong').checked = d.tra === 'true';
        document.getElementById('vh_TrangThai').checked = d.trangthai !== 'false';
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalLuuVoHop')).show();
    };

    window.moModalNhapXuatVoHop = function (maVh, maCode, tenVo, tonHienTai, loaiGiaoDich) {
        document.getElementById('nx_MaVh').value = maVh;
        document.getElementById('nx_TenVoHopDisplay').innerText = `[${maCode}] ${tenVo}`;
        document.getElementById('nx_TonHienTaiDisplay').innerText = Number(tonHienTai || 0).toLocaleString('vi-VN');
        document.getElementById('nx_TonHienTaiDisplay').dataset.ton = String(tonHienTai || 0);
        document.getElementById('nx_LoaiGiaoDich').value = loaiGiaoDich || 'NHAP_VO_HOP';
        document.getElementById('nx_SoLuong').value = loaiGiaoDich === 'XUAT_HUY_VO_HOP' ? '1' : '20';
        document.getElementById('nx_DonGiaNhap').value = '50000';
        document.getElementById('nx_GhiChu').value = '';

        capNhatCheDoNhapXuatVoHop();
        capNhatThanhTienVoHop();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalNhapXuatVoHop')).show();
    };

    window.capNhatCheDoNhapXuatVoHop = function () {
        const loai = document.getElementById('nx_LoaiGiaoDich')?.value || 'NHAP_VO_HOP';
        const laNhap = loai === 'NHAP_VO_HOP';
        const tonHienTai = parseInt(document.getElementById('nx_TonHienTaiDisplay')?.dataset.ton || '0', 10);
        const inpSl = document.getElementById('nx_SoLuong');
        const btnSubmit = document.getElementById('nx_BtnSubmit');
        const title = document.getElementById('nx_ModalTitle');

        document.querySelectorAll('#modalNhapXuatVoHop .nx-chi-nhap').forEach(el => {
            el.style.display = laNhap ? '' : 'none';
        });

        if (inpSl) {
            if (!laNhap && tonHienTai > 0) inpSl.max = String(tonHienTai);
            else inpSl.removeAttribute('max');
        }

        if (title) {
            title.innerText = laNhap ? 'Nhập thêm vỏ hộp vào kho' : 'Xuất hủy vỏ hộp hỏng / lỗi';
        }
        if (btnSubmit) {
            btnSubmit.className = laNhap ? 'btn-kv-green' : 'btn-kv-danger-outline';
            btnSubmit.innerHTML = laNhap
                ? '<i class="bi bi-check2-circle"></i> Xác nhận nhập kho'
                : '<i class="bi bi-exclamation-triangle"></i> Xác nhận xuất hủy';
        }
    };

    window.capNhatThanhTienVoHop = function () {
        const sl = Math.max(0, parseInt(document.getElementById('nx_SoLuong')?.value || '0', 10));
        const donGia = Math.max(0, parseFloat(document.getElementById('nx_DonGiaNhap')?.value || '0'));
        const thanhTien = Math.round(sl * donGia);
        const el = document.getElementById('nx_ThanhTienDisplay');
        if (el) el.innerText = thanhTien.toLocaleString('vi-VN') + ' đ';
    };
})();
