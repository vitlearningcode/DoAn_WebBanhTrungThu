(function () {
    let dsDongXuat = [];

    function formatSo(n) {
        return Math.round(Number(n) || 0).toLocaleString('en-US');
    }

    function escapeHtml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    window.locPhieuXuatClient = function () {
        const tk = (document.getElementById('txtTimPhieuXuat')?.value || '').trim().toLowerCase();
        const lx = document.querySelector('input[name="radPxLoai"]:checked')?.value || 'ALL';
        const tt = document.querySelector('input[name="radPxTrangThai"]:checked')?.value || 'ALL';

        document.querySelectorAll('#tbodyPhieuXuat tr.kv-master-row').forEach(tr => {
            let hien = true;
            if (tk && !(tr.getAttribute('data-search') || '').includes(tk)) hien = false;
            if (hien && lx !== 'ALL' && tr.getAttribute('data-loaiz') !== lx) hien = false;
            if (hien && tt !== 'ALL' && tr.getAttribute('data-trangthai') !== tt) hien = false;
            tr.style.display = hien ? '' : 'none';
            if (!hien) {
                tr.classList.remove('active-row');
                const detail = tr.nextElementSibling;
                if (detail && detail.classList.contains('kv-detail-row')) detail.classList.remove('open');
            }
        });
    };

    window.capNhatNhanLoaiXuat = function () {
        const loai = document.getElementById('px_LoaiXuat')?.value || 'XUAT_HUY_LOI';
        const lblDt = document.getElementById('px_LblDoiTuong');
        const inpDt = document.getElementById('px_DoiTuong');
        const lblGc = document.getElementById('px_LblGhiChu');
        const inpGc = document.getElementById('px_GhiChu');

        if (loai === 'TIEU_DUNG_NOI_BO') {
            lblDt.innerHTML = 'Người nhận / Đối tượng liên quan <span class="text-danger">*</span>';
            inpDt.placeholder = 'VD: Sếp mời nhân viên - Phòng Kinh Doanh';
            lblGc.innerHTML = 'Ghi chú chi tiết <span class="text-danger">*</span>';
            inpGc.required = true;
            inpGc.placeholder = 'Bắt buộc ghi rõ mục đích tiêu dùng nội bộ...';
        } else if (loai === 'TRA_NHA_SAN_XUAT') {
            lblDt.innerHTML = 'Nhà sản xuất / Hãng nhận trả <span class="text-danger">*</span>';
            inpDt.placeholder = 'VD: Trả hãng Kinh Đô (lô bánh móp hộp)';
            lblGc.innerHTML = 'Ghi chú chi tiết';
            inpGc.required = false;
            inpGc.placeholder = 'Biên bản bàn giao / lý do trả hãng...';
        } else {
            lblDt.innerHTML = 'Nhân viên / Nguyên nhân liên quan <span class="text-danger">*</span>';
            inpDt.placeholder = 'VD: Nhân viên A làm bể, hàng cận date quá hạn';
            lblGc.innerHTML = 'Ghi chú chi tiết';
            inpGc.required = false;
            inpGc.placeholder = 'Mô tả nguyên nhân hư hỏng, xuất hủy...';
        }
    };

    window.renderBangDongXuat = function () {
        const tbody = document.getElementById('px_TbodyDongXuat');
        if (!tbody) return;
        const dsSp = window.NGUYETCAT_DS_SP_XUAT || [];

        if (dsDongXuat.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Chưa có dòng sản phẩm nào. Hãy chọn sản phẩm phía trên.</td></tr>';
            capNhatTongVonXuat();
            return;
        }

        tbody.innerHTML = dsDongXuat.map((d, idx) => {
            const sp = dsSp.find(x => x.maBanh === d.maSp);
            const los = sp ? (sp.los || []) : [];
            const optsLo = [`<option value="">Tự động trừ FEFO (Tồn tổng: ${sp ? sp.tonKho : 0})</option>`]
                .concat(los.map(l => `<option value="${l.maLo}" ${Number(d.maLo) === l.maLo ? 'selected' : ''}>${escapeHtml(l.maLoCode)} | HSD ${escapeHtml(l.hanSuDung)} (Còn ${l.slConLai})</option>`))
                .join('');

            const loChon = d.maLo ? los.find(l => l.maLo === Number(d.maLo)) : null;
            const maxSl = loChon ? loChon.slConLai : (sp ? sp.tonKho : 1);
            const tt = (Number(d.soLuongXuat) || 0) * (Number(d.giaVon) || 0);

            return `<tr>
                <td>${idx + 1}</td>
                <td><div class="fw-semibold text-dark">${escapeHtml(d.tenBanh)}</div><small class="text-primary">${escapeHtml(d.maSku)}</small></td>
                <td><select class="form-select form-select-sm" onchange="chonLoDongXuat(${idx}, this.value)">${optsLo}</select></td>
                <td><input type="number" min="1" max="${Math.max(1, maxSl)}" value="${d.soLuongXuat}" class="form-control form-control-sm text-center fw-bold" onchange="capNhatSlDongXuat(${idx}, this.value)" /></td>
                <td class="text-end so-tien">${formatSo(d.giaVon)} đ</td>
                <td class="text-end fw-bold text-danger so-tien">${formatSo(tt)} đ</td>
                <td><input type="text" value="${escapeHtml(d.ghiChuDong || '')}" class="form-control form-control-sm" placeholder="Ghi chú dòng..." oninput="dsDongXuat[${idx}].ghiChuDong = this.value" /></td>
                <td class="text-center"><button type="button" class="btn btn-sm text-danger p-0" onclick="xoaDongXuat(${idx})"><i class="bi bi-trash"></i></button></td>
            </tr>`;
        }).join('');

        capNhatTongVonXuat();
    };

    window.chonLoDongXuat = function (idx, val) {
        if (!dsDongXuat[idx]) return;
        dsDongXuat[idx].maLo = val ? parseInt(val, 10) : null;
        renderBangDongXuat();
    };

    window.capNhatSlDongXuat = function (idx, val) {
        if (!dsDongXuat[idx]) return;
        dsDongXuat[idx].soLuongXuat = Math.max(1, parseInt(val, 10) || 1);
        renderBangDongXuat();
    };

    window.xoaDongXuat = function (idx) {
        dsDongXuat.splice(idx, 1);
        renderBangDongXuat();
    };

    function capNhatTongVonXuat() {
        const tong = dsDongXuat.reduce((acc, d) => acc + (Number(d.soLuongXuat) || 0) * (Number(d.giaVon) || 0), 0);
        const el = document.getElementById('px_LblTongVon');
        if (el) el.innerText = formatSo(tong) + ' đ';
    }

    window.themDongTuSelectXuat = function () {
        const sel = document.getElementById('px_ChonSpSelect');
        const maBanh = parseInt(sel?.value || '0', 10);
        if (!maBanh) return;
        const sp = (window.NGUYETCAT_DS_SP_XUAT || []).find(x => x.maBanh === maBanh);
        if (!sp) return;

        dsDongXuat.push({
            maSp: sp.maBanh,
            maSku: sp.maSku,
            tenBanh: sp.tenBanh,
            maLo: null,
            soLuongXuat: 1,
            giaVon: sp.giaVon,
            ghiChuDong: ''
        });
        sel.value = '';
        renderBangDongXuat();
    };

    window.moModalTaoPhieuXuat = function () {
        document.getElementById('px_LoaiXuat').value = 'XUAT_HUY_LOI';
        document.getElementById('px_DoiTuong').value = '';
        document.getElementById('px_GhiChu').value = '';
        capNhatNhanLoaiXuat();
        dsDongXuat = [];
        renderBangDongXuat();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoPhieuXuat')).show();
    };

    window.chuanBiSubmitPhieuXuat = function () {
        const loai = document.getElementById('px_LoaiXuat')?.value;
        const doiTuong = (document.getElementById('px_DoiTuong')?.value || '').trim();
        const ghiChu = (document.getElementById('px_GhiChu')?.value || '').trim();
        if (!doiTuong) {
            alert('Vui lòng nhập Người nhận / Đối tượng liên quan!');
            return false;
        }
        if (loai === 'TIEU_DUNG_NOI_BO' && !ghiChu) {
            alert('Phiếu xuất tiêu dùng nội bộ bắt buộc phải nhập Ghi chú chi tiết!');
            return false;
        }
        if (dsDongXuat.length === 0) {
            alert('Vui lòng thêm ít nhất 1 dòng sản phẩm cần xuất kho!');
            return false;
        }
        document.getElementById('px_ChiTietJson').value = JSON.stringify(dsDongXuat.map(d => ({
            maSp: d.maSp,
            maLo: d.maLo || null,
            soLuongXuat: d.soLuongXuat,
            ghiChuDong: d.ghiChuDong || ''
        })));
        return true;
    };
})();
