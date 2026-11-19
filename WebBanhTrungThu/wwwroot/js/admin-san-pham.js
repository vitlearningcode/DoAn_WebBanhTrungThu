// =========================================================================
// NGUYETCAT_ERP ADMIN HÀNG HÓA - XỬ LÝ BỘ LỌC TỨC THÌ, MODAL CRUD BÁNH LẺ / COMBO & LÔ FEFO
// =========================================================================
(function () {
    let mangHangThanhPhan = [];

    window.chonBanTrucTiep = function (btn, val) {
        const flt = document.getElementById('fltBanTrucTiep');
        if (flt) flt.value = val;
        document.querySelectorAll('#pillGroupBanTrucTiep .kv-pill-btn').forEach(b => b.classList.remove('active'));
        if (btn) btn.classList.add('active');
        locBangHangHoaClient();
    };

    window.locBangHangHoaClient = function () {
        const tuKhoa = (document.getElementById('txtTimKiemHangHoa')?.value || '').trim().toLowerCase();
        const maDm = document.getElementById('fltMaDm')?.value || 'ALL';
        const tonKho = document.getElementById('fltTonKho')?.value || 'ALL';
        const ncc = document.getElementById('fltNhaCungCap')?.value || 'ALL';
        const thuongHieu = document.getElementById('fltThuongHieu')?.value || 'ALL';
        const viTri = document.getElementById('fltViTriKho')?.value || 'ALL';
        const loaiHang = document.getElementById('fltLoaiHang')?.value || 'ALL';
        const banTrucTiep = document.getElementById('fltBanTrucTiep')?.value || 'ALL';

        const masterRows = document.querySelectorAll('#tbodyDanhSachHangHoa tr.kv-master-row');
        masterRows.forEach(tr => {
            let hien = true;

            if (tuKhoa !== '') {
                const searchStr = tr.getAttribute('data-search') || '';
                if (!searchStr.includes(tuKhoa)) hien = false;
            }

            if (hien && maDm !== 'ALL' && maDm !== '') {
                if (tr.getAttribute('data-madm') !== maDm) hien = false;
            }

            if (hien && tonKho !== 'ALL') {
                const slTon = parseInt(tr.getAttribute('data-tonkho') || '0', 10);
                if (tonKho === 'DUOI_DINH_MUC' && !(slTon > 0 && slTon <= 30)) hien = false;
                else if (tonKho === 'VUOT_DINH_MUC' && !(slTon > 100)) hien = false;
                else if (tonKho === 'CON_HANG' && !(slTon > 0)) hien = false;
                else if (tonKho === 'HET_HANG' && !(slTon <= 0)) hien = false;
            }

            if (hien && ncc !== 'ALL') {
                if (tr.getAttribute('data-ncc') !== ncc) hien = false;
            }

            if (hien && thuongHieu !== 'ALL') {
                if (tr.getAttribute('data-thuonghieu') !== thuongHieu) hien = false;
            }

            if (hien && viTri !== 'ALL') {
                if (tr.getAttribute('data-vitri') !== viTri) hien = false;
            }

            if (hien && loaiHang !== 'ALL') {
                if (tr.getAttribute('data-loaihang') !== loaiHang) hien = false;
            }

            if (hien && banTrucTiep !== 'ALL') {
                if (tr.getAttribute('data-bantructiep') !== banTrucTiep) hien = false;
            }

            tr.style.display = hien ? '' : 'none';
            if (!hien) {
                tr.classList.remove('active-row');
                const detailId = tr.getAttribute('data-detail-id');
                if (detailId) {
                    const detailRow = document.getElementById(detailId);
                    if (detailRow) detailRow.classList.remove('open');
                }
            }
        });
    };

    window.chuyenTabModalKiot = function (tab) {
        const isInfo = (tab === 'THONG_TIN');
        document.getElementById('paneTabThongTin').style.display = isInfo ? 'block' : 'none';
        document.getElementById('paneTabMoTa').style.display = isInfo ? 'none' : 'block';
        document.getElementById('btnTabThongTin').classList.toggle('active', isInfo);
        document.getElementById('btnTabMoTa').classList.toggle('active', !isInfo);
    };

    window.toggleAccordionBody = function (id) {
        const el = document.getElementById(id);
        if (!el) return;
        el.style.display = (el.style.display === 'none') ? 'block' : 'none';
    };

    function dinhDangSoN0(n) {
        const num = Math.round(Number(n) || 0);
        return num.toLocaleString('en-US');
    }

    window.renderBangHangThanhPhan = function () {
        const tbody = document.getElementById('tbodyHangThanhPhan');
        if (!tbody) return;
        tbody.innerHTML = '';
        let tongVon = 0;
        let tongBan = 0;

        if (mangHangThanhPhan.length === 0) {
            tbody.innerHTML = `<tr><td colspan="9" class="text-center text-muted py-3">Chưa có hàng thành phần. Hãy chọn bánh ở ô tìm kiếm phía trên.</td></tr>`;
        } else {
            mangHangThanhPhan.forEach((tp, idx) => {
                const sl = Math.max(1, parseInt(tp.soLuong) || 1);
                const gv = Math.round(Number(tp.giaVon) || 0);
                const gb = Math.round(Number(tp.giaBan) || 0);
                const tVon = gv * sl;
                const tBan = gb * sl;
                tongVon += tVon;
                tongBan += tBan;

                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td>${idx + 1}</td>
                    <td class="text-primary fw-medium">${tp.maHang}</td>
                    <td class="fw-medium">${tp.tenHang}</td>
                    <td class="text-center">
                        <input type="number" min="1" value="${sl}" class="form-control form-control-sm text-center mx-auto" style="width: 72px;"
                               onchange="capNhatSlThanhPhan(${idx}, this.value)" />
                    </td>
                    <td class="text-end">${dinhDangSoN0(gv)}</td>
                    <td class="text-end fw-medium">${dinhDangSoN0(tVon)}</td>
                    <td class="text-end">${dinhDangSoN0(gb)}</td>
                    <td class="text-end fw-medium text-primary">${dinhDangSoN0(tBan)}</td>
                    <td class="text-center">
                        <button type="button" class="btn btn-sm text-danger p-0" onclick="xoaHangThanhPhan(${idx})">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        }

        document.getElementById('lblTongGiaVonThanhPhan').innerText = dinhDangSoN0(tongVon);
        document.getElementById('lblTongGiaBanThanhPhan').innerText = dinhDangSoN0(tongBan);
        document.getElementById('kv_HangThanhPhanJson').value = JSON.stringify(mangHangThanhPhan);

        if (document.getElementById('kv_LaCombo').value === 'true' && tongVon > 0) {
            document.getElementById('kv_GiaVon').value = Math.round(tongVon);
            if (!document.getElementById('kv_GiaBan').value || Number(document.getElementById('kv_GiaBan').value) === 0) {
                document.getElementById('kv_GiaBan').value = Math.round(tongBan);
            }
        }
    };

    window.capNhatSlThanhPhan = function (idx, val) {
        if (mangHangThanhPhan[idx]) {
            mangHangThanhPhan[idx].soLuong = Math.max(1, parseInt(val) || 1);
            renderBangHangThanhPhan();
        }
    };

    window.xoaHangThanhPhan = function (idx) {
        mangHangThanhPhan.splice(idx, 1);
        renderBangHangThanhPhan();
    };

    window.themHangThanhPhanTuSelect = function (sel) {
        const maBanh = parseInt(sel.value);
        if (!maBanh) return;
        const opt = sel.options[sel.selectedIndex];
        const existing = mangHangThanhPhan.find(x => x.maBanh === maBanh);
        if (existing) {
            existing.soLuong += 1;
        } else {
            mangHangThanhPhan.push({
                maBanh: maBanh,
                maHang: opt.getAttribute('data-mahang'),
                tenHang: opt.getAttribute('data-tenhang'),
                soLuong: 1,
                giaVon: Math.round(Number(opt.getAttribute('data-giavon')) || 0),
                giaBan: Math.round(Number(opt.getAttribute('data-giaban')) || 0)
            });
        }
        sel.value = '';
        renderBangHangThanhPhan();
    };

    function ganAnhModalAnToan(urlAnh) {
        const imgMain = document.getElementById('kv_PreviewAnhChinh');
        const safeUrl = urlAnh || window.KV_FALLBACK_SVG;
        if (imgMain) {
            delete imgMain.dataset.fallbackApplied;
            imgMain.src = safeUrl;
        }
    }

    function cauHinhHienThiTheoLoaiHang(laCombo) {
        const boxCombo = document.getElementById('boxAccordionHangThanhPhan');
        const boxBanh = document.getElementById('boxAccordionDacTinhBanh');
        if (boxCombo) boxCombo.style.display = laCombo ? 'block' : 'none';
        if (boxBanh) boxBanh.style.display = laCombo ? 'none' : 'block';
    }

    window.moModalTaoMoi = function (laCombo) {
        chuyenTabModalKiot('THONG_TIN');
        document.getElementById('kv_IdGoc').value = '0';
        document.getElementById('kv_LaCombo').value = laCombo ? 'true' : 'false';
        document.getElementById('kv_ModalTitle').innerText = laCombo ? 'Thêm combo - đóng gói' : 'Thêm hàng hóa';

        cauHinhHienThiTheoLoaiHang(!!laCombo);

        document.getElementById('kv_MaHang').value = '';
        document.getElementById('kv_MaVach').value = '';
        document.getElementById('kv_TenHang').value = '';
        document.getElementById('kv_TenHang').placeholder = laCombo
            ? 'Nhập tên bộ Combo - đóng gói...'
            : 'Nhập tên bánh trung thu...';

        document.getElementById('kv_GiaBan').value = '';
        document.getElementById('kv_GiaVon').value = '';
        document.getElementById('kv_TonKho').value = '';
        document.getElementById('kv_TrongLuong').value = 150;
        document.getElementById('kv_DonViTrongLuong').value = 'g';
        document.getElementById('kv_DonViTinh').value = laCombo ? 'Hộp' : 'Cái';
        document.getElementById('kv_BanTrucTiep').checked = true;

        document.getElementById('kv_SoTrungMuoi').value = '1';
        document.getElementById('kv_MucDoNgot').value = 'BINH_THUONG';
        document.getElementById('kv_NangLuongCalo').value = 380;
        document.getElementById('kv_LuongDuongGram').value = 14;
        document.getElementById('kv_ChiSoDuongHuyetGi').value = 52;
        document.getElementById('kv_CanhBaoDiUng').value = 'Không';
        document.getElementById('kv_LaBanhChay').checked = false;
        document.getElementById('kv_LaBanhThuongHang').checked = false;
        document.getElementById('kv_MoTaNgan').value = '';
        document.getElementById('kv_CauChuyenBanh').value = '';

        const fileInput = document.getElementById('kv_FileAnhTaiLen');
        if (fileInput) fileInput.value = '';
        document.getElementById('kv_UrlAnhNhapTay').value = '';
        ganAnhModalAnToan('/images/default/banh-mac-dinh.png');

        mangHangThanhPhan = [];
        renderBangHangThanhPhan();

        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('modalHangHoaKiot'));
        modal.show();
    };

    window.moModalSuaTuButton = function (btn) {
        const raw = btn.getAttribute('data-item');
        if (!raw) return;
        const item = JSON.parse(raw);

        chuyenTabModalKiot('THONG_TIN');
        document.getElementById('kv_IdGoc').value = item.idGoc;
        document.getElementById('kv_LaCombo').value = item.laCombo ? 'true' : 'false';
        document.getElementById('kv_ModalTitle').innerText = item.laCombo ? 'Sửa combo - đóng gói' : 'Sửa hàng hóa';

        cauHinhHienThiTheoLoaiHang(!!item.laCombo);

        document.getElementById('kv_MaHang').value = item.maHang || '';
        document.getElementById('kv_MaVach').value = item.maVach || '';
        document.getElementById('kv_TenHang').value = item.tenHang || '';

        const selDm = document.getElementById('kv_MaDm');
        if (item.maDm && selDm) selDm.value = item.maDm;

        chonHoacThemOptionSelect('kv_ThuongHieu', item.thuongHieu || 'Đồng Khánh');
        chonHoacThemOptionSelect('kv_ViTriKho', item.viTriKho || 'Kệ A1');
        chonHoacThemOptionSelect('kv_DonViTinh', item.donViTinh || (item.laCombo ? 'Hộp' : 'Cái'));
        chonHoacThemOptionSelect('kv_NhaCungCap', item.nhaCungCap || 'Lò Bánh Đồng Khánh Chính Gốc');

        document.getElementById('kv_GiaBan').value = Math.round(Number(item.giaBan) || 0);
        document.getElementById('kv_GiaVon').value = Math.round(Number(item.giaVon) || 0);
        document.getElementById('kv_TonKho').value = Math.round(Number(item.tonKho) || 0);
        document.getElementById('kv_TrongLuong').value = Math.round(Number(item.trongLuongGram) || 150);
        document.getElementById('kv_DonViTrongLuong').value = 'g';
        document.getElementById('kv_BanTrucTiep').checked = !!item.banTrucTiep;

        document.getElementById('kv_MoTaNgan').value = item.moTaNgan || '';
        document.getElementById('kv_CauChuyenBanh').value = item.cauChuyenBanh || '';
        document.getElementById('kv_SoTrungMuoi').value = (item.soTrungMuoi !== undefined && item.soTrungMuoi !== null) ? String(item.soTrungMuoi) : '1';
        document.getElementById('kv_NangLuongCalo').value = item.nangLuongCalo || 380;
        document.getElementById('kv_LuongDuongGram').value = Math.round(Number(item.luongDuongGram) || 14);
        document.getElementById('kv_ChiSoDuongHuyetGi').value = item.chiSoDuongHuyetGi || 52;
        document.getElementById('kv_CanhBaoDiUng').value = item.canhBaoDiUng || 'Không';
        document.getElementById('kv_MucDoNgot').value = item.mucDoNgot || 'BINH_THUONG';
        document.getElementById('kv_LaBanhChay').checked = !!item.laBanhChay;
        document.getElementById('kv_LaBanhThuongHang').checked = !!item.laBanhThuongHang;

        if (item.maVhCombo) {
            document.getElementById('kv_MaVhCombo').value = item.maVhCombo;
        }

        const fileInput = document.getElementById('kv_FileAnhTaiLen');
        if (fileInput) fileInput.value = '';
        ganAnhModalAnToan(item.urlAnhChinh || '/images/default/banh-mac-dinh.png');
        document.getElementById('kv_UrlAnhNhapTay').value = '';

        mangHangThanhPhan = Array.isArray(item.hangThanhPhans)
            ? item.hangThanhPhans.map(x => ({ ...x }))
            : [];
        renderBangHangThanhPhan();

        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('modalHangHoaKiot'));
        modal.show();
    };

    function chonHoacThemOptionSelect(selectId, val, textHienThi) {
        if (!val) return;
        const sel = document.getElementById(selectId);
        if (!sel) return;
        let found = false;
        for (let i = 0; i < sel.options.length; i++) {
            if (sel.options[i].value === String(val)) {
                sel.selectedIndex = i;
                found = true;
                break;
            }
        }
        if (!found) {
            const opt = document.createElement('option');
            opt.value = val;
            opt.text = textHienThi || val;
            opt.selected = true;
            sel.appendChild(opt);
        }
    }

    window.moPopupTaoMoiDanhMuc = function (loai) {
        document.getElementById('hidPopupDmLoai').value = loai;
        document.getElementById('inpPopupDmTenMoi').value = '';
        document.getElementById('inpPopupDmMoTaPhu').value = '';
        const errEl = document.getElementById('msgPopupDmError');
        if (errEl) { errEl.style.display = 'none'; errEl.innerText = ''; }

        const titleEl = document.getElementById('lblPopupDmTitle');
        const lbl1 = document.getElementById('lblPopupDmField1');
        const inp1 = document.getElementById('inpPopupDmTenMoi');
        const wrap2 = document.getElementById('wrapPopupDmField2');
        const lbl2 = document.getElementById('lblPopupDmField2');
        const inp2 = document.getElementById('inpPopupDmMoTaPhu');

        if (loai === 'NHOM_HANG') {
            titleEl.innerText = 'Thêm nhóm hàng mới';
            lbl1.innerText = 'Tên nhóm hàng';
            inp1.placeholder = 'VD: ĐỒNG GIÁ 20K, Bánh Chay Cao Cấp...';
            wrap2.style.display = 'block';
            lbl2.innerText = 'Mô tả nhóm hàng';
            inp2.placeholder = 'Mô tả ngắn cho nhóm hàng...';
        } else if (loai === 'THUONG_HIEU') {
            titleEl.innerText = 'Thêm thương hiệu phân phối';
            lbl1.innerText = 'Tên thương hiệu';
            inp1.placeholder = 'VD: Kinh Đô, Đồng Khánh, Như Lan, Givral...';
            wrap2.style.display = 'block';
            lbl2.innerText = 'Nhà cung cấp đi kèm (tùy chọn)';
            inp2.placeholder = 'VD: NPP Kinh Đô Miền Nam...';
        } else if (loai === 'NHA_CUNG_CAP') {
            titleEl.innerText = 'Thêm nhà cung cấp / lò bánh';
            lbl1.innerText = 'Tên nhà cung cấp';
            inp1.placeholder = 'VD: Lò Bánh Đồng Khánh Chính Gốc...';
            wrap2.style.display = 'none';
        } else if (loai === 'VI_TRI') {
            titleEl.innerText = 'Thêm vị trí lưu kho';
            lbl1.innerText = 'Tên vị trí kệ / tủ';
            inp1.placeholder = 'VD: Kệ A3, Tủ Mát C2, Kho Lạnh B1...';
            wrap2.style.display = 'none';
        }

        const m = bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoDanhMucCaNhanHoa'));
        m.show();
        setTimeout(() => inp1.focus(), 200);
    };

    window.luuDanhMucNhanhAjax = async function () {
        const loai = document.getElementById('hidPopupDmLoai').value;
        const tenMoi = (document.getElementById('inpPopupDmTenMoi').value || '').trim();
        const moTaPhu = (document.getElementById('inpPopupDmMoTaPhu').value || '').trim();
        const errEl = document.getElementById('msgPopupDmError');

        if (!tenMoi) {
            errEl.innerText = 'Vui lòng nhập tên hợp lệ!';
            errEl.style.display = 'block';
            return;
        }

        const tokenInput = document.querySelector('#formLuuHangHoaKiot input[name="__RequestVerificationToken"]');
        const formData = new FormData();
        formData.append('loaiDanhMuc', loai);
        formData.append('tenMoi', tenMoi);
        formData.append('moTaPhu', moTaPhu);
        if (tokenInput) {
            formData.append('__RequestVerificationToken', tokenInput.value);
        }

        try {
            const resp = await fetch('/Admin/SanPhamAdmin/TaoDanhMucNhanhAjax', {
                method: 'POST',
                body: formData
            });
            const data = await resp.json();
            if (!data.ok) {
                errEl.innerText = data.message || 'Không thể lưu danh mục!';
                errEl.style.display = 'block';
                return;
            }

            if (data.loai === 'NHOM_HANG') {
                chonHoacThemOptionSelect('kv_MaDm', data.value, data.text);
                chonHoacThemOptionSelect('fltMaDm', data.value, data.text);
            } else if (data.loai === 'THUONG_HIEU') {
                chonHoacThemOptionSelect('kv_ThuongHieu', data.value, data.text);
                chonHoacThemOptionSelect('fltThuongHieu', data.value, data.text);
                if (data.extraNcc) {
                    chonHoacThemOptionSelect('kv_NhaCungCap', data.extraNcc, data.extraNcc);
                    chonHoacThemOptionSelect('fltNhaCungCap', data.extraNcc, data.extraNcc);
                }
            } else if (data.loai === 'NHA_CUNG_CAP') {
                chonHoacThemOptionSelect('kv_NhaCungCap', data.value, data.text);
                chonHoacThemOptionSelect('fltNhaCungCap', data.value, data.text);
            } else if (data.loai === 'VI_TRI') {
                chonHoacThemOptionSelect('kv_ViTriKho', data.value, data.text);
                chonHoacThemOptionSelect('fltViTriKho', data.value, data.text);
            }

            const m = bootstrap.Modal.getInstance(document.getElementById('modalTaoDanhMucCaNhanHoa'));
            if (m) m.hide();
        } catch (err) {
            errEl.innerText = 'Lỗi kết nối máy chủ khi tạo danh mục!';
            errEl.style.display = 'block';
        }
    };

    window.xemTruocAnhKiot = function (input) {
        if (input.files && input.files[0]) {
            const reader = new FileReader();
            reader.onload = function (e) {
                ganAnhModalAnToan(e.target.result);
            };
            reader.readAsDataURL(input.files[0]);
        }
    };
})();
