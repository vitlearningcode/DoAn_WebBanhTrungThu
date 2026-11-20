(function () {
    window.locNhanVienClient = function () {
        const tk = (document.getElementById('txtTimNhanVien')?.value || '').trim().toLowerCase();
        const vt = document.querySelector('input[name="radNvVaiTro"]:checked')?.value || 'ALL';
        const cn = document.getElementById('fltNvChiNhanh')?.value || 'ALL';
        const ca = document.getElementById('fltNvCaLamViec')?.value || 'ALL';
        const tt = document.querySelector('input[name="radNvTrangThai"]:checked')?.value || 'ALL';

        let soHien = 0;
        document.querySelectorAll('#tbodyNhanVien tr.kv-master-row').forEach(tr => {
            let hien = true;
            if (tk && !(tr.getAttribute('data-search') || '').includes(tk)) hien = false;
            if (hien && vt !== 'ALL' && tr.getAttribute('data-vaitro') !== vt) hien = false;
            if (hien && cn !== 'ALL' && tr.getAttribute('data-chinhanh') !== cn) hien = false;
            if (hien && ca !== 'ALL' && tr.getAttribute('data-ca') !== ca) hien = false;
            if (hien && tt !== 'ALL' && tr.getAttribute('data-trangthai') !== tt) hien = false;

            tr.style.display = hien ? '' : 'none';
            if (hien) {
                soHien++;
            } else {
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

        const emptyRow = document.getElementById('nv_EmptyFilterRow');
        if (emptyRow) {
            emptyRow.classList.toggle('d-none', soHien > 0);
        }
    };

    function chonGiaTriSelectHoacThem(selectId, giaTri) {
        const sel = document.getElementById(selectId);
        if (!sel || !giaTri) return;
        const tonTai = Array.from(sel.options).some(opt => opt.value === giaTri);
        if (!tonTai) {
            const opt = document.createElement('option');
            opt.value = giaTri;
            opt.textContent = giaTri;
            sel.appendChild(opt);
        }
        sel.value = giaTri;
    }

    window.moModalTaoNhanVien = function () {
        document.getElementById('nv_MaNv').value = '0';
        document.getElementById('nv_MaNd').value = '0';
        document.getElementById('nv_ModalTitle').innerText = 'Thêm nhân viên mới';
        document.getElementById('nv_MaCodeNv').value = '';
        document.getElementById('nv_HoTen').value = '';
        document.getElementById('nv_Sdt').value = '';
        document.getElementById('nv_Email').value = '';
        document.getElementById('nv_MaVt').value = 'NHAN_VIEN';
        document.getElementById('nv_ChiNhanh').selectedIndex = 0;
        document.getElementById('nv_CaLamViec').selectedIndex = 0;
        document.getElementById('nv_NgayVaoLam').value = new Date().toISOString().slice(0, 10);
        document.getElementById('nv_TrangThaiLamViec').value = 'DANG_LAM_VIEC';
        document.getElementById('nv_GhiChu').value = '';

        const wrapMk = document.getElementById('nv_WrapMatKhau');
        const inpMk = document.getElementById('nv_MatKhauKhoiTao');
        if (wrapMk) wrapMk.classList.remove('d-none');
        if (inpMk) {
            inpMk.value = '';
            inpMk.required = true;
        }

        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalLuuNhanVien')).show();
    };

    window.moModalSuaNhanVien = async function (maNd) {
        try {
            const resp = await fetch(`/Admin/NhanVienAdmin/ChiTietJson?id=${maNd}`);
            const res = await resp.json();
            if (!res.thanhCong || !res.duLieu) {
                alert(res.thongBao || 'Không tải được hồ sơ nhân viên.');
                return;
            }
            const d = res.duLieu;
            document.getElementById('nv_MaNv').value = d.maNv || '0';
            document.getElementById('nv_MaNd').value = d.maNd || '0';
            document.getElementById('nv_ModalTitle').innerText = `Cập nhật nhân viên ${d.maCodeNv || ''}`;
            document.getElementById('nv_MaCodeNv').value = d.maCodeNv || '';
            document.getElementById('nv_HoTen').value = d.hoTen || '';
            document.getElementById('nv_Sdt').value = d.sdt || '';
            document.getElementById('nv_Email').value = d.email || '';
            document.getElementById('nv_MaVt').value = d.maVt === 'ADMIN' ? 'ADMIN' : 'NHAN_VIEN';
            chonGiaTriSelectHoacThem('nv_ChiNhanh', d.chiNhanh || 'TP. Long Xuyên');
            chonGiaTriSelectHoacThem('nv_CaLamViec', d.caLamViec || 'Toàn thời gian');
            document.getElementById('nv_NgayVaoLam').value = d.ngayVaoLam || '';
            document.getElementById('nv_TrangThaiLamViec').value = d.trangThaiLamViec === 'TAM_KHOA' ? 'TAM_KHOA' : 'DANG_LAM_VIEC';
            document.getElementById('nv_GhiChu').value = d.ghiChu || '';

            const wrapMk = document.getElementById('nv_WrapMatKhau');
            const inpMk = document.getElementById('nv_MatKhauKhoiTao');
            if (wrapMk) wrapMk.classList.add('d-none');
            if (inpMk) {
                inpMk.value = '';
                inpMk.required = false;
            }

            bootstrap.Modal.getOrCreateInstance(document.getElementById('modalLuuNhanVien')).show();
        } catch (err) {
            alert('Lỗi kết nối khi tải hồ sơ nhân viên.');
        }
    };

    window.moModalDatLaiMatKhauNv = function (btn) {
        const maNd = btn.dataset.mand || '0';
        const maCode = btn.dataset.macode || '';
        const hoTen = btn.dataset.hoten || '';
        document.getElementById('nvMk_MaNd').value = maNd;
        document.getElementById('nvMk_ThongTinNhanVien').innerText = `${maCode} • ${hoTen}`;
        document.getElementById('nvMk_MatKhauMoi').value = '';
        document.getElementById('nvMk_XacNhanMatKhau').value = '';
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalDatLaiMatKhauNv')).show();
    };

    window.kiemTraDatLaiMatKhauNv = function () {
        const mk = (document.getElementById('nvMk_MatKhauMoi')?.value || '').trim();
        const xn = (document.getElementById('nvMk_XacNhanMatKhau')?.value || '').trim();
        if (mk.length < 6) {
            alert('Mật khẩu mới phải có ít nhất 6 ký tự!');
            return false;
        }
        if (mk !== xn) {
            alert('Xác nhận mật khẩu mới chưa khớp!');
            return false;
        }
        return true;
    };

    window.xacNhanChuyenTrangThaiKhoaNv = function (form) {
        const maCode = form.dataset.macode || '';
        const hoTen = form.dataset.hoten || '';
        const dangLam = form.dataset.danglam === '1';
        const thongDiep = dangLam
            ? `Xác nhận tạm khóa tài khoản [${maCode}] ${hoTen}? Nhân viên này sẽ không thể đăng nhập.`
            : `Xác nhận mở khóa tài khoản [${maCode}] ${hoTen}?`;
        return confirm(thongDiep);
    };
})();
