(function () {
    window.locNhaCungCapClient = function () {
        const tk = (document.getElementById('txtTimNhaCungCap')?.value || '').trim().toLowerCase();
        const loai = document.querySelector('input[name="radNccLoai"]:checked')?.value || 'ALL';
        const tt = document.querySelector('input[name="radNccTrangThai"]:checked')?.value || 'ALL';

        document.querySelectorAll('#tbodyNhaCungCap tr.kv-master-row').forEach(tr => {
            let hien = true;
            if (tk && !(tr.getAttribute('data-search') || '').includes(tk)) hien = false;
            if (hien && loai !== 'ALL' && tr.getAttribute('data-loai') !== loai) hien = false;
            if (hien && tt !== 'ALL' && tr.getAttribute('data-trangthai') !== tt) hien = false;

            tr.style.display = hien ? '' : 'none';
            if (!hien) {
                tr.classList.remove('active-row');
                const detailId = tr.getAttribute('data-detail-id');
                if (detailId) {
                    const detail = document.getElementById(detailId);
                    if (detail) {
                        detail.classList.remove('open');
                        detail.classList.add('d-none');
                    }
                }
            }
        });
    };

    window.moModalTaoNhaCungCap = function () {
        document.getElementById('ncc_MaNcc').value = '0';
        document.getElementById('ncc_ModalTitle').innerText = 'Thêm nhà cung cấp mới';
        document.getElementById('ncc_MaCodeNcc').value = '';
        document.getElementById('ncc_TenNcc').value = '';
        document.getElementById('ncc_LoaiNcc').value = 'HANG_CHINH_HANG';
        document.getElementById('ncc_ThuongHieu').value = 'Đa thương hiệu';
        document.getElementById('ncc_PhanTramCk').value = '25';
        document.getElementById('ncc_SoDienThoai').value = '';
        document.getElementById('ncc_Email').value = '';
        document.getElementById('ncc_DiaChi').value = '';
        document.getElementById('ncc_GhiChu').value = '';
        document.getElementById('ncc_DangHoatDong').checked = true;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalLuuNhaCungCap')).show();
    };

    window.moModalSuaNhaCungCap = async function (maNcc) {
        const resp = await fetch(`/Admin/NhaCungCapAdmin/ChiTietJson?id=${maNcc}`);
        const res = await resp.json();
        if (!res.thanhCong || !res.duLieu) {
            alert(res.thongBao || 'Không tải được dữ liệu nhà cung cấp.');
            return;
        }
        const d = res.duLieu;
        document.getElementById('ncc_MaNcc').value = d.maNcc;
        document.getElementById('ncc_ModalTitle').innerText = `Cập nhật nhà cung cấp ${d.maCodeNcc}`;
        document.getElementById('ncc_MaCodeNcc').value = d.maCodeNcc || '';
        document.getElementById('ncc_TenNcc').value = d.tenNcc || '';
        document.getElementById('ncc_LoaiNcc').value = d.loaiNcc || 'HANG_CHINH_HANG';
        document.getElementById('ncc_ThuongHieu').value = d.thuongHieuCungCap || 'Đa thương hiệu';
        document.getElementById('ncc_PhanTramCk').value = d.phanTramChietKhauMacDinh ?? 0;
        document.getElementById('ncc_SoDienThoai').value = d.soDienThoai || '';
        document.getElementById('ncc_Email').value = d.email || '';
        document.getElementById('ncc_DiaChi').value = d.diaChi || '';
        document.getElementById('ncc_GhiChu').value = d.ghiChu || '';
        document.getElementById('ncc_DangHoatDong').checked = d.dangHoatDong !== false;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalLuuNhaCungCap')).show();
    };
})();
