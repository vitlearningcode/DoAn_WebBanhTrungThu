(function () {
    let dsDongKiem = [];
    let canBangNgay = false;

    function formatSo(n) {
        return Math.round(Number(n) || 0).toLocaleString('en-US');
    }

    function escapeHtml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    window.locPhieuKiemClient = function () {
        const tk = (document.getElementById('txtTimPhieuKiem')?.value || '').trim().toLowerCase();
        const tt = document.querySelector('input[name="radKkTrangThai"]:checked')?.value || 'ALL';

        document.querySelectorAll('#tbodyPhieuKiemKho tr.kv-master-row').forEach(tr => {
            let hien = true;
            if (tk && !(tr.getAttribute('data-search') || '').includes(tk)) hien = false;
            if (hien && tt !== 'ALL' && tr.getAttribute('data-trangthai') !== tt) hien = false;
            tr.style.display = hien ? '' : 'none';
            if (!hien) {
                tr.classList.remove('active-row');
                const detail = tr.nextElementSibling;
                if (detail && detail.classList.contains('kv-detail-row')) detail.classList.remove('open');
            }
        });
    };

    window.renderBangDongKiemKho = function () {
        const tbody = document.getElementById('kk_TbodyDongKiem');
        if (!tbody) return;

        if (dsDongKiem.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Chưa có sản phẩm nào trong phiếu kiểm. Chọn sản phẩm hoặc nạp từ Excel.</td></tr>';
            capNhatTongLechKiemKho();
            return;
        }

        tbody.innerHTML = dsDongKiem.map((d, idx) => {
            const lech = (Number(d.tonThucTe) || 0) - (Number(d.tonHeThong) || 0);
            const giaTriLech = lech * (Number(d.giaVon) || 0);
            const mauClass = lech < 0 ? 'text-danger fw-bold' : (lech > 0 ? 'text-success fw-bold' : 'text-muted');
            const prefix = lech > 0 ? '+' : '';

            return `<tr>
                <td>${idx + 1}</td>
                <td><div class="fw-semibold text-dark">${escapeHtml(d.tenBanh)}</div><small class="text-primary">${escapeHtml(d.maSku)}</small></td>
                <td class="text-end fw-medium so-tien">${formatSo(d.tonHeThong)}</td>
                <td><input type="number" min="0" value="${d.tonThucTe}" class="form-control form-control-sm text-center fw-bold" oninput="capNhatTonThucTeKiem(${idx}, this.value)" /></td>
                <td class="text-end so-tien ${mauClass}" id="kk_lech_${idx}">${prefix}${formatSo(lech)}</td>
                <td class="text-end so-tien ${mauClass}" id="kk_gtlech_${idx}">${prefix}${formatSo(giaTriLech)} đ</td>
                <td><input type="text" list="dlLyDoLechKiemKho" value="${escapeHtml(d.lyDoLech || '')}" class="form-control form-control-sm" placeholder="VD: Nhân viên lấy nhầm sản phẩm..." oninput="dsDongKiem[${idx}].lyDoLech = this.value" /></td>
                <td class="text-center"><button type="button" class="btn btn-sm text-danger p-0" onclick="xoaDongKiemKho(${idx})"><i class="bi bi-trash"></i></button></td>
            </tr>`;
        }).join('');

        capNhatTongLechKiemKho();
    };

    window.capNhatTonThucTeKiem = function (idx, val) {
        const d = dsDongKiem[idx];
        if (!d) return;
        d.tonThucTe = Math.max(0, parseInt(val, 10) || 0);
        const lech = d.tonThucTe - (Number(d.tonHeThong) || 0);
        const giaTriLech = lech * (Number(d.giaVon) || 0);
        const prefix = lech > 0 ? '+' : '';
        const cls = lech < 0 ? 'text-end so-tien text-danger fw-bold' : (lech > 0 ? 'text-end so-tien text-success fw-bold' : 'text-end so-tien text-muted');

        const elL = document.getElementById(`kk_lech_${idx}`);
        const elGt = document.getElementById(`kk_gtlech_${idx}`);
        if (elL) {
            elL.className = cls;
            elL.innerText = prefix + formatSo(lech);
        }
        if (elGt) {
            elGt.className = cls;
            elGt.innerText = prefix + formatSo(giaTriLech) + ' đ';
        }
        capNhatTongLechKiemKho();
    };

    function capNhatTongLechKiemKho() {
        let tang = 0, giam = 0, giaTri = 0;
        dsDongKiem.forEach(d => {
            const lech = (Number(d.tonThucTe) || 0) - (Number(d.tonHeThong) || 0);
            if (lech > 0) tang += lech;
            else if (lech < 0) giam += Math.abs(lech);
            giaTri += lech * (Number(d.giaVon) || 0);
        });
        document.getElementById('kk_LblLechTang').innerText = '+' + formatSo(tang);
        document.getElementById('kk_LblLechGiam').innerText = '-' + formatSo(giam);
        const lblGt = document.getElementById('kk_LblTongGiaTriLech');
        lblGt.innerText = (giaTri > 0 ? '+' : '') + formatSo(giaTri) + ' đ';
        lblGt.className = giaTri < 0 ? 'fs-6 so-tien text-danger fw-bold' : (giaTri > 0 ? 'fs-6 so-tien text-success fw-bold' : 'fs-6 so-tien text-dark fw-bold');
    }

    window.xoaDongKiemKho = function (idx) {
        dsDongKiem.splice(idx, 1);
        renderBangDongKiemKho();
    };

    window.themDongTuSelectKiemKho = function () {
        const sel = document.getElementById('kk_ChonSpSelect');
        const maBanh = parseInt(sel?.value || '0', 10);
        if (!maBanh) return;
        if (dsDongKiem.some(x => x.maSp === maBanh)) {
            sel.value = '';
            return;
        }
        const sp = (window.NGUYETCAT_DS_SP_KIEM || []).find(x => x.maBanh === maBanh);
        if (!sp) return;
        dsDongKiem.push({
            maSp: sp.maBanh,
            maSku: sp.maSku,
            tenBanh: sp.tenBanh,
            tonHeThong: sp.tonHeThong,
            tonThucTe: sp.tonHeThong,
            giaVon: sp.giaVon,
            lyDoLech: ''
        });
        sel.value = '';
        renderBangDongKiemKho();
    };

    window.napTatCaSanPhamVaoBangKiem = function () {
        const dsSp = window.NGUYETCAT_DS_SP_KIEM || [];
        dsSp.forEach(sp => {
            if (!dsDongKiem.some(x => x.maSp === sp.maBanh)) {
                dsDongKiem.push({
                    maSp: sp.maBanh,
                    maSku: sp.maSku,
                    tenBanh: sp.tenBanh,
                    tonHeThong: sp.tonHeThong,
                    tonThucTe: sp.tonHeThong,
                    giaVon: sp.giaVon,
                    lyDoLech: ''
                });
            }
        });
        renderBangDongKiemKho();
    };

    window.moModalTaoPhieuKiemKho = function () {
        document.getElementById('kk_MaPkk').value = '0';
        document.getElementById('kk_ModalTitle').innerText = 'Tạo phiếu kiểm kho đối soát thực tế';
        document.getElementById('kk_GhiChu').value = '';
        dsDongKiem = [];
        renderBangDongKiemKho();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoPhieuKiemKho')).show();
    };

    window.moModalSuaPhieuKiemKho = async function (maPkk) {
        const resp = await fetch(`/Admin/KiemKhoAdmin/ChiTietJson?id=${maPkk}`);
        const res = await resp.json();
        if (!res.thanhCong || !res.duLieu) {
            alert(res.thongBao || 'Không tải được phiếu kiểm kho!');
            return;
        }
        const p = res.duLieu;
        document.getElementById('kk_MaPkk').value = p.maPkk;
        document.getElementById('kk_ModalTitle').innerText = `Cập nhật phiếu kiểm kho ${p.soPhieuKiem}`;
        document.getElementById('kk_GhiChu').value = p.ghiChu || '';
        dsDongKiem = (p.chiTiets || []).map(c => ({
            maSp: c.maSp,
            maSku: c.maSku,
            tenBanh: c.tenBanh,
            tonHeThong: c.tonHeThong,
            tonThucTe: c.tonThucTe,
            giaVon: c.giaVon,
            lyDoLech: c.lyDoLech || ''
        }));
        renderBangDongKiemKho();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoPhieuKiemKho')).show();
    };

    window.napExcelVaoModalKiemKho = async function () {
        const fileInp = document.getElementById('kk_FileImportExcel');
        const msgEl = document.getElementById('kk_ImportMsg');
        if (!fileInp || !fileInp.files || fileInp.files.length === 0) {
            msgEl.innerText = 'Vui lòng chọn file Excel (.xlsx) hoặc CSV (.csv) trước.';
            msgEl.style.display = 'block';
            return;
        }
        msgEl.style.display = 'none';

        const token = document.querySelector('#frmImportKiemKho input[name="__RequestVerificationToken"]')?.value || '';
        const fd = new FormData();
        fd.append('fileExcel', fileInp.files[0]);
        fd.append('__RequestVerificationToken', token);

        const resp = await fetch('/Admin/KiemKhoAdmin/DocFileExcel', { method: 'POST', body: fd });
        const data = await resp.json();
        if (!data.thanhCong) {
            msgEl.innerText = data.thongBao || 'Không đọc được file Excel kiểm kho.';
            msgEl.style.display = 'block';
            return;
        }

        bootstrap.Modal.getInstance(document.getElementById('modalImportKiemKho'))?.hide();
        document.getElementById('kk_MaPkk').value = '0';
        document.getElementById('kk_ModalTitle').innerText = 'Đối soát dữ liệu kiểm kho từ Excel';
        document.getElementById('kk_GhiChu').value = `Đối soát từ tệp ${fileInp.files[0].name}`;
        dsDongKiem = (data.danhSachDong || []).map(c => ({
            maSp: c.maSp,
            maSku: c.maSku,
            tenBanh: c.tenBanh,
            tonHeThong: c.tonHeThong,
            tonThucTe: c.tonThucTe,
            giaVon: c.giaVon,
            lyDoLech: c.lyDoLech || ''
        }));
        renderBangDongKiemKho();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoPhieuKiemKho')).show();
    };

    window.datCheDoCanBangKiemKho = function (cb) {
        canBangNgay = cb;
        document.getElementById('kk_CanBangNgay').value = cb ? 'true' : 'false';
    };

    window.chuanBiSubmitPhieuKiemKho = function () {
        if (dsDongKiem.length === 0) {
            alert('Vui lòng thêm ít nhất 1 sản phẩm vào phiếu kiểm kho!');
            return false;
        }
        if (canBangNgay) {
            const xacNhan = confirm('Xác nhận hoàn thành kiểm kho và tự động cân bằng tồn kho theo số lượng thực tế?');
            if (!xacNhan) return false;
        }
        document.getElementById('kk_ChiTietJson').value = JSON.stringify(dsDongKiem);
        return true;
    };
})();
