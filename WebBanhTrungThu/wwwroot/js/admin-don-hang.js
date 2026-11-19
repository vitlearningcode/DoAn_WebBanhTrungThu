// =========================================================================
// NGUYETCAT_ERP ADMIN HÓA ĐƠN - XỬ LÝ LỌC BẢNG, ĐIỀU PHỐI TRẠNG THÁI, HOÀN TIỀN VIETQR & IN HÓA ĐƠN
// =========================================================================
(function () {
    let _huyDonDangDaTt = false;
    let _huyDonSoTien = 0;
    let _huyDonMaTraCuu = '';
    let _huyDonTenKhach = '';

    window.chuyenTabChiTietDon = function (maDh, tabIdx) {
        const p1 = document.getElementById('paneTabDh1_' + maDh);
        const p2 = document.getElementById('paneTabDh2_' + maDh);
        const b1 = document.getElementById('btnTabDh1_' + maDh);
        const b2 = document.getElementById('btnTabDh2_' + maDh);
        if (!p1 || !p2) return;

        p1.style.display = (tabIdx === 1) ? 'block' : 'none';
        p2.style.display = (tabIdx === 2) ? 'block' : 'none';
        if (b1) b1.classList.toggle('active', tabIdx === 1);
        if (b2) b2.classList.toggle('active', tabIdx === 2);
    };

    let _huyDonSoMon = 0;

    window.moModalCapNhatTrangThai = function (maDh, maTraCuu, hoTen, tongTien, phuongThucTt, trangThaiDh, trangThaiTt, soDotGiao) {
        document.getElementById('lblMdCnMaDon').textContent = '#' + maTraCuu;
        document.getElementById('lblMdCnKhachHang').textContent = hoTen;
        document.getElementById('lblMdCnSoDotGiao').textContent = soDotGiao;
        document.getElementById('lblMdCnPhuongThuc').textContent = phuongThucTt + ' (' + trangThaiTt + ')';
        document.getElementById('lblMdCnTongTien').textContent = Number(tongTien).toLocaleString('vi-VN') + ' đ';

        document.getElementById('inpQrMaDh').value = maDh;
        document.getElementById('inpNextStepMaDh').value = maDh;
        document.getElementById('inpOverrideMaDh').value = maDh;

        const st = (trangThaiDh === 'DANG_DONG_GOI_HOP') ? 'CHO_LAY_HANG' : trangThaiDh;
        const biKhoaCung = (st === 'DA_HUY' || st === 'HOAN_TIEN_TRA_HANG');
        const thuTu = ['CHO_XAC_NHAN', 'CHO_LAY_HANG', 'DANG_GIAO', 'HOAN_TAT'];
        const stStepper = (st === 'DA_GIAO') ? 'HOAN_TAT' : st;
        const idxHienTai = thuTu.indexOf(stStepper);

        thuTu.forEach((stepKey, idx) => {
            const el = document.getElementById('stepItem_' + stepKey);
            if (!el) return;
            el.classList.remove('done', 'current');
            if (biKhoaCung) return;
            if (idx < idxHienTai || stStepper === 'HOAN_TAT') {
                el.classList.add('done');
            } else if (idx === idxHienTai) {
                el.classList.add('current');
            }
        });

        const boxKhoaCung = document.getElementById('boxTrangThaiKhoaCung');
        const lblKhoaCung = document.getElementById('lblTrangThaiKhoaCung');
        if (boxKhoaCung) {
            boxKhoaCung.style.display = biKhoaCung ? 'block' : 'none';
            if (lblKhoaCung) lblKhoaCung.textContent = st;
        }

        const boxQr = document.getElementById('boxXacNhanQrThuNgan');
        if (boxQr) {
            boxQr.style.display = (trangThaiTt === 'CHUA_TT' && !biKhoaCung) ? 'block' : 'none';
        }

        ['CHO_XAC_NHAN', 'CHO_LAY_HANG', 'DANG_GIAO', 'HOAN_TAT'].forEach(k => {
            const wrap = document.getElementById('btnWrapStep_' + k);
            if (wrap) wrap.style.display = (!biKhoaCung && k === stStepper) ? 'block' : 'none';
        });

        const formOverride = document.getElementById('formAdminOverrideTrangThai');
        if (formOverride) {
            const laQuyenAdmin = formOverride.getAttribute('data-role-admin') === '1';
            formOverride.querySelectorAll('select, input[type="text"], button[type="submit"]').forEach(el => {
                el.disabled = biKhoaCung || !laQuyenAdmin;
            });
        }

        const selDh = document.getElementById('selOverrideTrangThaiDh');
        if (selDh) {
            const daGiao = (st === 'HOAN_TAT' || st === 'DA_GIAO');
            Array.from(selDh.options).forEach(opt => {
                if (daGiao && opt.value !== 'HOAN_TAT') {
                    opt.disabled = true;
                } else {
                    opt.disabled = false;
                }
            });
            if (thuTu.includes(stStepper)) selDh.value = stStepper;
        }
        const selTt = document.getElementById('selOverrideTrangThaiTt');
        if (selTt) selTt.value = trangThaiTt;

        const modalEl = document.getElementById('modalCapNhatTrangThaiDon');
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    };

    window.capNhatNhanXuLyKhoHuy = function () {
        const laXuatHuy = Boolean(document.getElementById('radXuatHuyHangLoi')?.checked);
        const lblKho = document.getElementById('lblHuyTrangThaiKhoPreview');
        if (!lblKho) return;
        if (laXuatHuy) {
            lblKho.className = 'text-danger';
            lblKho.innerHTML = 'Xuất hủy hàng lỗi / móp hộp (Không hoàn tồn kho)';
        } else {
            lblKho.className = 'text-success';
            lblKho.innerHTML = '+<span id="lblHuySoMon">' + _huyDonSoMon + '</span> món hoàn nhập Lô đang bán';
        }
    };

    window.moModalHuyDon = function (maDh, maTraCuu, hoTen, sdt, tongTien, phuongThucTt, trangThaiTt, soDotGiao, soThiepQr, tongSoMon, trangThaiDh) {
        _huyDonDangDaTt = (trangThaiTt === 'DA_TT' || trangThaiTt === 'DA_THANH_TOAN');
        _huyDonSoTien = Math.round(Number(tongTien) || 0);
        _huyDonMaTraCuu = maTraCuu;
        _huyDonTenKhach = hoTen;
        _huyDonSoMon = tongSoMon || 0;

        const laTraHangSauGiao = (trangThaiDh === 'HOAN_TAT' || trangThaiDh === 'DA_GIAO');
        document.getElementById('inpHuyMaDh').value = maDh;
        document.getElementById('lblMdHuyMaDon').textContent = '#' + maTraCuu;
        document.getElementById('lblHuySoDotGiao').textContent = soDotGiao;
        document.getElementById('lblHuySoThiepQr').textContent = soThiepQr;
        const lblSoMonEl = document.getElementById('lblHuySoMon');
        if (lblSoMonEl) lblSoMonEl.textContent = _huyDonSoMon;

        const radHoanNhap = document.getElementById('radHoanNhapLo');
        if (radHoanNhap) radHoanNhap.checked = true;
        capNhatNhanXuLyKhoHuy();

        const txtLyDo = document.getElementById('txtLyDoHuyDon');
        if (txtLyDo) {
            txtLyDo.value = laTraHangSauGiao
                ? 'Khách trả hàng sau giao - đối soát hoàn tiền'
                : 'Khách yêu cầu hủy đơn & hoàn nhập lô';
        }

        document.getElementById('lblHuyTrangThaiTt').textContent =
            _huyDonDangDaTt
                ? ('ĐÃ THANH TOÁN (' + _huyDonSoTien.toLocaleString('vi-VN') + ' đ) ➔ Bắt buộc Hoàn tiền')
                : ('CHƯA THANH TOÁN (' + phuongThucTt + ') ➔ Hủy trực tiếp');

        const boxHoanTien = document.getElementById('boxHoanTienVietQr');
        const btnSubmit = document.getElementById('btnSubmitXacNhanHuy');

        if (_huyDonDangDaTt) {
            boxHoanTien.style.display = 'block';
            document.getElementById('lblHuySoTienCanHoan').textContent = _huyDonSoTien.toLocaleString('vi-VN');
            document.getElementById('lblHuyThongTinKhach').textContent = hoTen + ' - ' + sdt;
            document.getElementById('lblSoTienQrPreview').textContent = 'Số tiền hoàn: ' + _huyDonSoTien.toLocaleString('vi-VN') + ' đ';
            btnSubmit.innerHTML = laTraHangSauGiao
                ? '💸 Xác nhận Đổi trả / Hoàn tiền &amp; Ghi Sổ Quỹ'
                : '💸 Xác nhận Đã Hoàn Tiền &amp; Hủy Đơn Hàng';

            document.getElementById('txtSoTaiKhoanHoan').value = '';
            document.getElementById('txtTenChuTkHoan').value = boDauTiengViet(hoTen).toUpperCase();
            capNhatAnhVietQrHoanTien();
        } else {
            boxHoanTien.style.display = 'none';
            btnSubmit.innerHTML = '🗑️ Xác nhận Hủy Đơn Hàng &amp; Xử lý kho';
        }

        const modalEl = document.getElementById('modalHuyDonVaHoanTien');
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    };

    function boDauTiengViet(str) {
        if (!str) return '';
        return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D');
    }

    window.dienNhanhStkKhachDemo = function () {
        document.getElementById('selMaNganHangHoan').value = 'VCB';
        document.getElementById('txtSoTaiKhoanHoan').value = '1029384756';
        if (!document.getElementById('txtTenChuTkHoan').value) {
            document.getElementById('txtTenChuTkHoan').value = boDauTiengViet(_huyDonTenKhach || 'NGUYEN MINH CHAU').toUpperCase();
        }
        capNhatAnhVietQrHoanTien();
    };

    window.capNhatAnhVietQrHoanTien = function () {
        const maNganHang = document.getElementById('selMaNganHangHoan')?.value || 'VCB';
        const soTaiKhoan = (document.getElementById('txtSoTaiKhoanHoan')?.value || '').trim();
        const tenChuTK = (document.getElementById('txtTenChuTkHoan')?.value || '').trim().toUpperCase();
        const soTienHoan = _huyDonSoTien;
        const maDon = _huyDonMaTraCuu;

        const imgQr = document.getElementById('imgVietQrHoanTien');
        const placeholder = document.getElementById('placeholderVietQrHoan');

        if (!soTaiKhoan) {
            if (imgQr) imgQr.style.display = 'none';
            if (placeholder) placeholder.style.display = 'block';
            return;
        }

        const qrUrl = `https://img.vietqr.io/image/${maNganHang}-${soTaiKhoan}-compact2.png?amount=${soTienHoan}&addInfo=${encodeURIComponent('HOAN TIEN DON ' + maDon)}&accountName=${encodeURIComponent(tenChuTK)}`;
        if (imgQr) {
            delete imgQr.dataset.fallbackApplied;
            imgQr.src = qrUrl;
            imgQr.style.display = 'inline-block';
        }
        if (placeholder) {
            placeholder.style.display = 'none';
        }
    };

    window.kiemTraTruocKhiSubmitHuy = function () {
        if (_huyDonDangDaTt) {
            const stk = (document.getElementById('txtSoTaiKhoanHoan')?.value || '').trim();
            const tenTk = (document.getElementById('txtTenChuTkHoan')?.value || '').trim();
            if (!stk || !tenTk) {
                alert('⚠️ Đơn hàng này khách đã thanh toán! Vui lòng nhập đầy đủ Số tài khoản và Tên chủ tài khoản ngân hàng của khách để hoàn tiền trước khi xác nhận.');
                return false;
            }
        }

        const txtLyDo = document.getElementById('txtLyDoHuyDon');
        const laXuatHuy = Boolean(document.getElementById('radXuatHuyHangLoi')?.checked);
        if (txtLyDo) {
            let lyDoSach = txtLyDo.value
                .replace(/\[XUAT_HUY_LOI\]/gi, '')
                .replace(/\[HOAN_NHAP_LO\]/gi, '')
                .trim();
            if (laXuatHuy) {
                if (!lyDoSach) lyDoSach = 'Xuất hủy hàng lỗi / móp hộp';
                txtLyDo.value = '[XUAT_HUY_LOI] ' + lyDoSach;
            } else {
                txtLyDo.value = lyDoSach || 'Khách yêu cầu hủy đơn / hoàn nhập lô';
            }
        }
        return true;
    };

    window.locHoaDonClient = function () {
        const tuKhoa = (document.getElementById('fltDhTuKhoaTop')?.value || '').trim().toLowerCase();
        const thoiGian = document.querySelector('input[name="fltDhThoiGian"]:checked')?.value || 'ALL';
        const trangThai = document.getElementById('fltDhTrangThai')?.value || 'ALL';
        const loaiDon = document.getElementById('fltDhLoaiDon')?.value || 'ALL';
        const phuongThuc = document.getElementById('fltDhPhuongThuc')?.value || 'ALL';

        const rows = document.querySelectorAll('#tbodyDanhSachHoaDon tr.kv-master-row');
        rows.forEach(tr => {
            let hien = true;

            if (tuKhoa !== '') {
                const s = tr.getAttribute('data-search') || '';
                if (!s.includes(tuKhoa)) hien = false;
            }

            if (hien && thoiGian !== 'ALL') {
                if (thoiGian === 'HOM_NAY' && tr.getAttribute('data-homnay') !== '1') hien = false;
                else if (thoiGian === 'THANG_NAY' && tr.getAttribute('data-thangnay') !== '1') hien = false;
            }

            if (hien && trangThai !== 'ALL') {
                const stRow = tr.getAttribute('data-trangthai');
                if (trangThai === 'GIAO_HANG') {
                    if (stRow !== 'CHO_LAY_HANG' && stRow !== 'DANG_GIAO') hien = false;
                } else if (stRow !== trangThai) {
                    hien = false;
                }
            }

            if (hien && loaiDon !== 'ALL') {
                if (loaiDon === 'B2B' && tr.getAttribute('data-b2b') !== 'B2B') hien = false;
                else if (loaiDon === 'B2C' && tr.getAttribute('data-b2b') !== 'B2C') hien = false;
                else if (loaiDon === 'GUEST' && tr.getAttribute('data-khach') !== 'GUEST') hien = false;
                else if (loaiDon === 'MEMBER' && tr.getAttribute('data-khach') !== 'MEMBER') hien = false;
            }

            if (hien && phuongThuc !== 'ALL') {
                if (tr.getAttribute('data-phuongthuc') !== phuongThuc) hien = false;
            }

            tr.style.display = hien ? '' : 'none';
            if (!hien) {
                tr.classList.remove('active-row');
                const detailId = tr.getAttribute('data-detail-id');
                if (detailId) {
                    const dRow = document.getElementById(detailId);
                    if (dRow) dRow.classList.remove('open');
                }
            }
        });
    };
})();
