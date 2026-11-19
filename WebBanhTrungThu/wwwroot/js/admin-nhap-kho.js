(function () {
    let dsDongNhap = [];
    let cheDoHoanThanh = true;
    let trangThaiPhieuDangSua = 'PHIEU_TAM';

    function formatSo(n) {
        return Math.round(Number(n) || 0).toLocaleString('en-US');
    }

    function escapeHtml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function ngayHomNayStr(congNgay) {
        const d = new Date();
        d.setDate(d.getDate() + (congNgay || 0));
        return d.toISOString().slice(0, 10);
    }

    window.locPhieuNhapClient = function () {
        const tk = (document.getElementById('txtTimPhieuNhap')?.value || '').trim().toLowerCase();
        const tt = document.querySelector('input[name="radPnTrangThai"]:checked')?.value || 'ALL';
        const pt = document.querySelector('input[name="radPnThanhToan"]:checked')?.value || 'ALL';
        const ncc = document.getElementById('fltPnNhaCungCap')?.value || 'ALL';

        document.querySelectorAll('#tbodyPhieuNhap tr.kv-master-row').forEach(tr => {
            let hien = true;
            if (tk && !(tr.getAttribute('data-search') || '').includes(tk)) hien = false;
            if (hien && tt !== 'ALL' && tr.getAttribute('data-trangthai') !== tt) hien = false;
            if (hien && pt !== 'ALL' && tr.getAttribute('data-tt') !== pt) hien = false;
            if (hien && ncc !== 'ALL' && tr.getAttribute('data-ncc') !== ncc) hien = false;
            tr.style.display = hien ? '' : 'none';
            if (!hien) {
                tr.classList.remove('active-row');
                const detail = tr.nextElementSibling;
                if (detail && detail.classList.contains('kv-detail-row')) detail.classList.remove('open');
            }
        });
    };

    window.renderBangDongNhap = function () {
        const tbody = document.getElementById('pn_TbodyDongNhap');
        if (!tbody) return;
        const chiSuaInfo = trangThaiPhieuDangSua === 'DA_NHAP_KHO';

        if (dsDongNhap.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Chưa có dòng sản phẩm nào. Hãy chọn sản phẩm phía trên.</td></tr>';
            tinhTongPhieuNhap();
            return;
        }

        tbody.innerHTML = dsDongNhap.map((d, idx) => {
            const tt = Math.max(0, (Number(d.soLuong) || 0) * (Number(d.donGiaNhap) || 0));
            const disabledAttr = chiSuaInfo ? 'disabled' : '';
            return `<tr>
                <td>${idx + 1}</td>
                <td><div class="fw-semibold text-dark">${escapeHtml(d.tenBanh)}</div><small class="text-primary">${escapeHtml(d.maSku)}</small></td>
                <td><input type="number" min="1" value="${d.soLuong}" ${disabledAttr} class="form-control form-control-sm text-center fw-bold" onchange="capNhatDongNhap(${idx}, 'soLuong', this.value)" /></td>
                <td><input type="number" min="0" step="500" value="${Math.round(d.donGiaNhap)}" ${disabledAttr} class="form-control form-control-sm text-end fw-semibold" oninput="capNhatDongNhap(${idx}, 'donGiaNhap', this.value)" /></td>
                <td><input type="date" value="${d.ngaySanXuat}" ${disabledAttr} class="form-control form-control-sm" onchange="capNhatDongNhap(${idx}, 'ngaySanXuat', this.value)" /></td>
                <td><input type="date" value="${d.hanSuDung}" ${disabledAttr} class="form-control form-control-sm" onchange="capNhatDongNhap(${idx}, 'hanSuDung', this.value)" /></td>
                <td class="text-end fw-bold text-primary so-tien" id="pn_tt_${idx}">${formatSo(tt)} đ</td>
                <td class="text-center">${chiSuaInfo ? '' : `<button type="button" class="btn btn-sm text-danger p-0" onclick="xoaDongNhap(${idx})"><i class="bi bi-trash"></i></button>`}</td>
            </tr>`;
        }).join('');

        tinhTongPhieuNhap();
    };

    window.capNhatDongNhap = function (idx, field, val) {
        const d = dsDongNhap[idx];
        if (!d) return;
        if (field === 'soLuong') d.soLuong = Math.max(1, parseInt(val, 10) || 1);
        else if (field === 'donGiaNhap') d.donGiaNhap = Math.max(0, Math.round(Number(val) || 0));
        else d[field] = val;

        const ttEl = document.getElementById(`pn_tt_${idx}`);
        if (ttEl) ttEl.innerText = formatSo(d.soLuong * d.donGiaNhap) + ' đ';
        tinhTongPhieuNhap();
    };

    window.xoaDongNhap = function (idx) {
        dsDongNhap.splice(idx, 1);
        renderBangDongNhap();
    };

    window.tinhTongPhieuNhap = function () {
        const tongTienHang = dsDongNhap.reduce((acc, d) => acc + (Number(d.soLuong) || 0) * (Number(d.donGiaNhap) || 0), 0);
        const giamGia = Math.min(tongTienHang, Math.max(0, Math.round(Number(document.getElementById('pn_GiamGia')?.value) || 0)));
        const tongPhaiTra = Math.max(0, tongTienHang - giamGia);
        const elHang = document.getElementById('pn_LblTongTienHang');
        const elTra = document.getElementById('pn_LblTongPhaiTra');
        if (elHang) elHang.innerText = formatSo(tongTienHang) + ' đ';
        if (elTra) elTra.innerText = formatSo(tongPhaiTra) + ' đ';
    };

    window.themDongTuSelectNhap = function () {
        const sel = document.getElementById('pn_ChonSpSelect');
        const maBanh = parseInt(sel?.value || '0', 10);
        if (!maBanh) return;
        const ds = window.NGUYETCAT_DS_SP_NHAP || [];
        const sp = ds.find(x => x.maBanh === maBanh);
        if (!sp) return;

        dsDongNhap.push({
            maSp: sp.maBanh,
            maSku: sp.maSku,
            tenBanh: sp.tenBanh,
            soLuong: 20,
            donGiaNhap: Math.round(sp.giaVon || 30000),
            ngaySanXuat: ngayHomNayStr(0),
            hanSuDung: ngayHomNayStr(40)
        });
        sel.value = '';
        renderBangDongNhap();
    };

    function thietLapCheDoModalNhap(laDaNhap) {
        document.getElementById('pn_BoxChonSanPham').style.display = laDaNhap ? 'none' : 'block';
        document.getElementById('pn_GiamGia').readOnly = laDaNhap;
        document.getElementById('pn_WrapTraNgay').style.display = laDaNhap ? 'none' : 'block';
        document.getElementById('pn_WrapPhuongThuc').style.display = laDaNhap ? 'none' : 'block';
        document.getElementById('pn_BtnLuuTam').style.display = laDaNhap ? 'none' : 'inline-flex';
        document.getElementById('pn_BtnHoanThanh').innerHTML = laDaNhap
            ? '<i class="bi bi-check2-circle"></i> Lưu thông tin phiếu'
            : '<i class="bi bi-check2-circle"></i> Hoàn thành nhập kho';
    }

    window.moModalTaoPhieuNhap = function () {
        trangThaiPhieuDangSua = 'PHIEU_TAM';
        document.getElementById('pn_MaPn').value = '0';
        document.getElementById('pn_ModalTitle').innerText = 'Tạo phiếu nhập hàng (Đa sản phẩm)';
        document.getElementById('pn_NhaCungCap').value = 'Kinh Đô';
        document.getElementById('pn_GhiChu').value = '';
        document.getElementById('pn_GiamGia').value = '0';
        document.getElementById('pn_TraNgay').value = '0';
        dsDongNhap = [];
        thietLapCheDoModalNhap(false);
        renderBangDongNhap();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoSuaPhieuNhap')).show();
    };

    window.moModalSuaPhieuNhap = async function (maPn) {
        const resp = await fetch(`/Admin/NhapKhoAdmin/ChiTietJson?id=${maPn}`);
        const res = await resp.json();
        if (!res.thanhCong || !res.duLieu) {
            alert(res.thongBao || 'Không tải được dữ liệu phiếu nhập!');
            return;
        }
        const p = res.duLieu;
        trangThaiPhieuDangSua = p.trangThai || 'PHIEU_TAM';
        document.getElementById('pn_MaPn').value = p.maPn;
        document.getElementById('pn_ModalTitle').innerText = `Cập nhật phiếu nhập ${p.soPhieuNhap}`;
        document.getElementById('pn_NhaCungCap').value = p.nhaCungCap || '';
        document.getElementById('pn_GhiChu').value = p.ghiChu || '';
        document.getElementById('pn_GiamGia').value = Math.round(p.giamGia || 0);
        document.getElementById('pn_TraNgay').value = Math.round(p.daThanhToan || 0);
        dsDongNhap = (p.chiTiets || []).map(c => ({
            maSp: c.maSp,
            maSku: c.maSku,
            tenBanh: c.tenBanh,
            soLuong: c.soLuong,
            donGiaNhap: c.donGiaNhap,
            ngaySanXuat: c.ngaySanXuat,
            hanSuDung: c.hanSuDung
        }));
        thietLapCheDoModalNhap(trangThaiPhieuDangSua === 'DA_NHAP_KHO');
        renderBangDongNhap();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoSuaPhieuNhap')).show();
    };

    window.datCheDoLuuPhieuNhap = function (hoanThanh) {
        cheDoHoanThanh = hoanThanh;
        document.getElementById('pn_HoanThanhNhapKho').value = hoanThanh ? 'true' : 'false';
    };

    window.chuanBiSubmitPhieuNhap = function () {
        if (trangThaiPhieuDangSua !== 'DA_NHAP_KHO' && dsDongNhap.length === 0) {
            alert('Vui lòng thêm ít nhất 1 sản phẩm vào phiếu nhập!');
            return false;
        }
        document.getElementById('pn_ChiTietJson').value = JSON.stringify(dsDongNhap);
        return true;
    };

    window.moModalThanhToanTuButton = function (btn) {
        if (!btn) return;
        moModalThanhToanPhieuNhap(
            btn.getAttribute('data-mapn'),
            btn.getAttribute('data-sophieu') || '',
            btn.getAttribute('data-ncc') || '',
            btn.getAttribute('data-conno') || 0
        );
    };

    window.moModalThanhToanPhieuNhap = function (maPn, soPhieu, ncc, conNo) {
        document.getElementById('tt_MaPn').value = maPn;
        document.getElementById('tt_SoPhieu').innerText = soPhieu;
        document.getElementById('tt_NhaCungCap').innerText = ncc;
        document.getElementById('tt_ConNoLbl').innerText = formatSo(conNo) + ' đ';
        const inp = document.getElementById('tt_SoTienTra');
        inp.value = Math.round(conNo);
        inp.max = Math.round(conNo);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalThanhToanPhieuNhap')).show();
    };
})();
