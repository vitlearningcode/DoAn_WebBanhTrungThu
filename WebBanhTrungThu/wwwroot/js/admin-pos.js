// =========================================================================
// KIOTVIET WEB POS - XỬ LÝ ĐA HÓA ĐƠN, ĐÓNG HỘP TỰ CHỌN, VIETQR & IN K80
// =========================================================================
(function () {
    const cfg = window.POS_CATALOG || {};
    const DS_BANH_POS = Array.isArray(cfg.dsBanhPos) ? cfg.dsBanhPos : [];
    const DS_COMBO_POS = Array.isArray(cfg.dsComboPos) ? cfg.dsComboPos : [];
    const DS_VOHOP_POS = Array.isArray(cfg.dsVoHopPos) ? cfg.dsVoHopPos : [];
    const BANG_B2B_POS = Array.isArray(cfg.bangB2B) ? cfg.bangB2B : [];
    const DS_VOUCHER_POS = Array.isArray(cfg.dsVoucher) ? cfg.dsVoucher : [];
    const NGAY_HEN_MAC_DINH_POS = cfg.ngayHenMacDinh || '';

    let boLocPillHienTai = 'ALL';
    let dsBanhDangChonTrongHop = [];

    const STORAGE_KEY_POS = 'NGUYETCAT_KIOTVIET_POS_TABS_V1';
    let danhSachTabs = [];
    let tabDangChonId = 1;
    let khachCanTraHienTai = 0;

    function taoTabMoiMacDinh(soThuTu) {
        return {
            id: Date.now() + Math.floor(Math.random() * 1000),
            tenTab: `Hóa đơn ${soThuTu}`,
            items: [],
            khachHang: null,
            cheDoMua: 'TAI_QUAY',
            yeuCauInLogo: false,
            codeKm: '',
            phuongThucTt: 'TIEN_MAT',
            tienKhachDua: 0,
            giaoHang: {
                tenNguoiNhan: '',
                sdtNguoiNhan: '',
                diaChiGiao: '',
                ngayHenGiao: NGAY_HEN_MAC_DINH_POS,
                khungGioGiao: 'Sáng (08:00 - 11:30)',
                coKemThiepQr: true,
                loiChucThiep: 'Kính chúc Quý khách & Gia đình mùa Trăng Rằm Đoàn Viên, Viên Mãn & An Khang!'
            }
        };
    }

    function khoiTaoMultiTabsPos() {
        try {
            const raw = sessionStorage.getItem(STORAGE_KEY_POS);
            if (raw) {
                const parsed = JSON.parse(raw);
                if (Array.isArray(parsed.tabs) && parsed.tabs.length > 0) {
                    danhSachTabs = parsed.tabs;
                    tabDangChonId = parsed.activeId || danhSachTabs[0].id;
                }
            }
        } catch (e) { }

        if (danhSachTabs.length === 0) {
            const t1 = taoTabMoiMacDinh(1);
            danhSachTabs = [t1];
            tabDangChonId = t1.id;
        }
        renderThanhMultiTabs();
        napDuLieuTabHienTaiLenUI();
    }

    function luuStoragePos() {
        try {
            sessionStorage.setItem(STORAGE_KEY_POS, JSON.stringify({
                activeId: tabDangChonId,
                tabs: danhSachTabs
            }));
        } catch (e) { }
    }

    function layTabHienTai() {
        let t = danhSachTabs.find(x => x.id === tabDangChonId);
        if (!t) {
            t = danhSachTabs[0];
            tabDangChonId = t.id;
        }
        return t;
    }

    function renderThanhMultiTabs() {
        const bar = document.getElementById('barMultiTabsHoaDon');
        if (!bar) return;
        bar.innerHTML = '';

        danhSachTabs.forEach((t) => {
            const btn = document.createElement('div');
            btn.className = 'pos-tab-item' + (t.id === tabDangChonId ? ' active' : '');
            const soMon = t.items.reduce((s, i) => s + i.soLuong, 0);
            btn.onclick = () => chuyenTabHoaDon(t.id);
            btn.innerHTML = `
                <span>🧾 ${t.tenTab}${soMon > 0 ? ` (${soMon})` : ''}</span>
                <span class="pos-tab-close" title="Đóng hóa đơn này" onclick="event.stopPropagation(); dongTabHoaDon(${t.id});">×</span>
            `;
            bar.appendChild(btn);
        });

        const btnAdd = document.createElement('button');
        btnAdd.type = 'button';
        btnAdd.className = 'pos-btn-add-tab';
        btnAdd.title = 'Thêm hóa đơn mới (Phục vụ nhiều khách cùng lúc)';
        btnAdd.innerHTML = '<i class="bi bi-plus-lg"></i>';
        btnAdd.onclick = themTabHoaDonMoi;
        bar.appendChild(btnAdd);
    }

    window.themTabHoaDonMoi = function () {
        luuThongTinTabHienTai();
        const soMoi = danhSachTabs.length + 1;
        const tabMoi = taoTabMoiMacDinh(soMoi);
        danhSachTabs.push(tabMoi);
        tabDangChonId = tabMoi.id;
        luuStoragePos();
        renderThanhMultiTabs();
        napDuLieuTabHienTaiLenUI();
    };

    window.chuyenTabHoaDon = function (id) {
        if (id === tabDangChonId) return;
        luuThongTinTabHienTai();
        tabDangChonId = id;
        luuStoragePos();
        renderThanhMultiTabs();
        napDuLieuTabHienTaiLenUI();
    };

    window.dongTabHoaDon = function (id) {
        if (danhSachTabs.length <= 1) {
            danhSachTabs[0] = taoTabMoiMacDinh(1);
            tabDangChonId = danhSachTabs[0].id;
        } else {
            const idx = danhSachTabs.findIndex(x => x.id === id);
            if (idx >= 0) {
                danhSachTabs.splice(idx, 1);
            }
            if (tabDangChonId === id) {
                tabDangChonId = danhSachTabs[Math.max(0, idx - 1)].id;
            }
        }
        luuStoragePos();
        renderThanhMultiTabs();
        napDuLieuTabHienTaiLenUI();
    };

    window.luuThongTinTabHienTai = function () {
        const tab = layTabHienTai();
        if (!tab) return;
        tab.yeuCauInLogo = document.getElementById('chkPosYeuCauInLogo')?.checked || false;
        tab.codeKm = document.getElementById('selPosVoucher')?.value || '';
        const radPt = document.querySelector('input[name="radPosPhuongThucTt"]:checked');
        if (radPt) tab.phuongThucTt = radPt.value;
        tab.tienKhachDua = Number(document.getElementById('inpPosTienKhachDua')?.value) || 0;

        tab.giaoHang = {
            tenNguoiNhan: document.getElementById('txtPosTenNguoiNhan')?.value || '',
            sdtNguoiNhan: document.getElementById('txtPosSdtNguoiNhan')?.value || '',
            diaChiGiao: document.getElementById('txtPosDiaChiGiao')?.value || '',
            ngayHenGiao: document.getElementById('txtPosNgayHenGiao')?.value || '',
            khungGioGiao: document.getElementById('selPosKhungGioGiao')?.value || 'Sáng (08:00 - 11:30)',
            coKemThiepQr: document.getElementById('chkPosKemThiepQr')?.checked !== false,
            loiChucThiep: document.getElementById('txtPosLoiChucThiep')?.value || ''
        };
        luuStoragePos();
    };

    function napDuLieuTabHienTaiLenUI() {
        const tab = layTabHienTai();
        if (!tab) return;

        hienThiKhachHangTrenUI(tab.khachHang);
        chuyenCheDoMuaPos(tab.cheDoMua, true);

        if (tab.giaoHang) {
            document.getElementById('txtPosTenNguoiNhan').value = tab.giaoHang.tenNguoiNhan || '';
            document.getElementById('txtPosSdtNguoiNhan').value = tab.giaoHang.sdtNguoiNhan || '';
            document.getElementById('txtPosDiaChiGiao').value = tab.giaoHang.diaChiGiao || '';
            if (tab.giaoHang.ngayHenGiao) document.getElementById('txtPosNgayHenGiao').value = tab.giaoHang.ngayHenGiao;
            document.getElementById('selPosKhungGioGiao').value = tab.giaoHang.khungGioGiao || 'Sáng (08:00 - 11:30)';
            document.getElementById('chkPosKemThiepQr').checked = tab.giaoHang.coKemThiepQr !== false;
            document.getElementById('txtPosLoiChucThiep').value = tab.giaoHang.loiChucThiep || '';
        }

        document.getElementById('chkPosYeuCauInLogo').checked = !!tab.yeuCauInLogo;
        document.getElementById('selPosVoucher').value = tab.codeKm || '';

        const rad = document.querySelector(`input[name="radPosPhuongThucTt"][value="${tab.phuongThucTt || 'TIEN_MAT'}"]`);
        if (rad) rad.checked = true;
        doiPhuongThucTtPos(tab.phuongThucTt || 'TIEN_MAT', true);

        renderBangGioHangPos();
    }

    function dinhDangN0(n) {
        return Math.round(Number(n) || 0).toLocaleString('en-US');
    }

    window.chonPillLocPos = function (btn, maLoc) {
        boLocPillHienTai = maLoc;
        document.querySelectorAll('#barLocPillPos .pos-filter-pill').forEach(b => b.classList.remove('active'));
        if (btn) btn.classList.add('active');
        locSanPhamPos();
    };

    window.locSanPhamPos = function () {
        const kw = (document.getElementById('txtPosTimSanPham')?.value || '').trim().toLowerCase();
        const grid = document.getElementById('gridPosProducts');
        if (!grid) return;
        grid.innerHTML = '';

        if (boLocPillHienTai !== 'COMBO') {
            DS_BANH_POS.forEach(b => {
                if (boLocPillHienTai.startsWith('TH_')) {
                    const thCanTim = boLocPillHienTai.substring(3);
                    if ((b.thuongHieu || '') !== thCanTim) return;
                } else if (boLocPillHienTai === 'ISOMALT') {
                    if (b.mucDoNgot !== 'ISOMALT_TIEUDUONG' && !b.laBanhChay) return;
                }

                if (kw !== '') {
                    const str = `${b.maSku} ${b.maVach} ${b.ten} ${b.thuongHieu}`.toLowerCase();
                    if (!str.includes(kw)) return;
                }

                const card = document.createElement('div');
                card.className = 'pos-prod-card' + (b.tonKho <= 0 ? ' out-of-stock' : '');
                card.onclick = () => themBanhLeVaoPos(b.maBanh);

                let badgeCanDate = '';
                let htmlGia = `<div class="pos-prod-price">${dinhDangN0(b.giaBan)} đ</div>`;
                if (b.dangXaCanDate && b.phanTramCanDate > 0) {
                    badgeCanDate = `<span class="pos-badge-candate">-${b.phanTramCanDate}% Cận Date</span>`;
                    htmlGia = `
                        <div class="pos-prod-price text-danger">
                            ${dinhDangN0(b.giaBan)} đ
                            <small class="text-muted text-decoration-line-through fw-normal" style="font-size: 11px;">${dinhDangN0(b.giaGoc)}</small>
                        </div>`;
                }

                card.innerHTML = `
                    <div>
                        <div class="pos-prod-img-wrap">
                            ${badgeCanDate}
                            <span class="pos-badge-stock">Tồn: ${b.tonKho}</span>
                            <img src="${b.anh}" alt="${b.ten}" onerror="window.kvImgFallback(this)" />
                        </div>
                        <div class="pos-prod-name">${b.ten}</div>
                        <div class="pos-prod-brand"><i class="bi bi-tag"></i> ${b.thuongHieu} • ${b.maSku}</div>
                    </div>
                    ${htmlGia}
                `;
                grid.appendChild(card);
            });
        }

        if (boLocPillHienTai === 'ALL' || boLocPillHienTai === 'COMBO' || boLocPillHienTai.startsWith('TH_')) {
            DS_COMBO_POS.forEach(c => {
                if (boLocPillHienTai.startsWith('TH_')) {
                    const thCanTim = boLocPillHienTai.substring(3);
                    if ((c.thuongHieu || '') !== thCanTim) return;
                }

                if (kw !== '') {
                    const str = `${c.maSku} ${c.maVach} ${c.ten} ${c.thuongHieu}`.toLowerCase();
                    if (!str.includes(kw)) return;
                }

                const card = document.createElement('div');
                card.className = 'pos-prod-card';
                card.onclick = () => themComboVaoPos(c.maCb);
                card.innerHTML = `
                    <div>
                        <div class="pos-prod-img-wrap">
                            <span class="pos-badge-candate" style="background:#005AC3;">Combo</span>
                            <span class="pos-badge-stock">Tồn: ${c.tonKho}</span>
                            <img src="${c.anh}" alt="${c.ten}" onerror="window.kvImgFallback(this)" />
                        </div>
                        <div class="pos-prod-name">${c.ten}</div>
                        <div class="pos-prod-brand"><i class="bi bi-box2-heart"></i> ${c.tenVoHop}</div>
                    </div>
                    <div class="pos-prod-price">${dinhDangN0(c.giaBan)} đ</div>
                `;
                grid.appendChild(card);
            });
        }
    };

    window.quetMaVachNhanhPos = function (giaTri) {
        const code = (giaTri || '').trim().toLowerCase();
        if (!code) return;

        const banh = DS_BANH_POS.find(b =>
            (b.maSku && b.maSku.toLowerCase() === code) ||
            (b.maVach && b.maVach.toLowerCase() === code));
        if (banh) {
            themBanhLeVaoPos(banh.maBanh);
            document.getElementById('txtPosTimSanPham').value = '';
            locSanPhamPos();
            return;
        }

        const combo = DS_COMBO_POS.find(c =>
            (c.maSku && c.maSku.toLowerCase() === code) ||
            (c.maVach && c.maVach.toLowerCase() === code));
        if (combo) {
            themComboVaoPos(combo.maCb);
            document.getElementById('txtPosTimSanPham').value = '';
            locSanPhamPos();
        }
    };

    window.themBanhLeVaoPos = function (maBanh) {
        const b = DS_BANH_POS.find(x => x.maBanh === maBanh);
        if (!b) return;
        if (b.tonKho <= 0) {
            alert(`Sản phẩm [${b.maSku}] ${b.ten} đã hết tồn kho!`);
            return;
        }

        const tab = layTabHienTai();
        const existing = tab.items.find(x => x.loaiSanPham === 1 && x.maBanh === maBanh);
        if (existing) {
            if (existing.soLuong + 1 > b.tonKho) {
                alert(`Tồn kho chỉ còn ${b.tonKho} bánh!`);
                return;
            }
            existing.soLuong += 1;
        } else {
            tab.items.push({
                loaiSanPham: 1,
                maBanh: b.maBanh,
                maHang: b.maSku,
                tenHang: b.ten,
                chiTietPhu: b.dangXaCanDate ? `Ưu đãi xả kho cận Date -${b.phanTramCanDate}%` : `Thương hiệu: ${b.thuongHieu}`,
                donViTinh: b.donViTinh,
                donGia: b.giaBan,
                soLuong: 1
            });
        }
        luuStoragePos();
        renderThanhMultiTabs();
        renderBangGioHangPos();
    };

    window.themComboVaoPos = function (maCb) {
        const c = DS_COMBO_POS.find(x => x.maCb === maCb);
        if (!c) return;

        const tab = layTabHienTai();
        const existing = tab.items.find(x => x.loaiSanPham === 2 && x.maCb === maCb);
        if (existing) {
            existing.soLuong += 1;
        } else {
            tab.items.push({
                loaiSanPham: 2,
                maCb: c.maCb,
                maHang: c.maSku,
                tenHang: c.ten,
                chiTietPhu: `${c.tenVoHop} (${c.chiTietBanh})`,
                donViTinh: 'Hộp',
                donGia: c.giaBan,
                soLuong: 1
            });
        }
        luuStoragePos();
        renderThanhMultiTabs();
        renderBangGioHangPos();
    };

    window.capNhatSoLuongMonPos = function (idx, deltaHoacGiaTri, laNhapTrucTiep) {
        const tab = layTabHienTai();
        const item = tab.items[idx];
        if (!item) return;

        let slMoi = laNhapTrucTiep ? (parseInt(deltaHoacGiaTri) || 1) : (item.soLuong + deltaHoacGiaTri);
        if (slMoi <= 0) {
            tab.items.splice(idx, 1);
        } else {
            item.soLuong = slMoi;
        }
        luuStoragePos();
        renderThanhMultiTabs();
        renderBangGioHangPos();
    };

    window.xoaMonKhoiPos = function (idx) {
        const tab = layTabHienTai();
        tab.items.splice(idx, 1);
        luuStoragePos();
        renderThanhMultiTabs();
        renderBangGioHangPos();
    };

    function renderBangGioHangPos() {
        const tab = layTabHienTai();
        const tbody = document.getElementById('tbodyPosCart');
        if (!tbody) return;
        tbody.innerHTML = '';

        if (!tab.items || tab.items.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center text-muted py-5">
                        <i class="bi bi-cart3 fs-3 d-block mb-1"></i>
                        Chưa có sản phẩm trong <strong>${tab.tenTab}</strong>. Hãy chạm chọn bánh ở lưới bên dưới hoặc bấm <strong>F6</strong> để đóng hộp quà tự chọn.
                    </td>
                </tr>`;
        } else {
            tab.items.forEach((item, idx) => {
                const thanhTien = item.donGia * item.soLuong;
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td class="text-center text-muted fw-semibold">${idx + 1}</td>
                    <td class="fw-bold text-primary">${item.maHang}</td>
                    <td>
                        <div class="fw-semibold text-dark">${item.tenHang}</div>
                        ${item.chiTietPhu ? `<div class="text-muted" style="font-size: 11.5px;">${item.chiTietPhu}</div>` : ''}
                    </td>
                    <td class="text-center">${item.donViTinh}</td>
                    <td class="text-center">
                        <div class="pos-qty-box">
                            <button type="button" class="pos-qty-btn" onclick="capNhatSoLuongMonPos(${idx}, -1, false)">-</button>
                            <input type="number" min="1" class="pos-qty-input" value="${item.soLuong}"
                                   onchange="capNhatSoLuongMonPos(${idx}, this.value, true)" />
                            <button type="button" class="pos-qty-btn" onclick="capNhatSoLuongMonPos(${idx}, 1, false)">+</button>
                        </div>
                    </td>
                    <td class="text-end fw-medium">${dinhDangN0(item.donGia)}</td>
                    <td class="text-end fw-bold text-primary">${dinhDangN0(thanhTien)}</td>
                    <td class="text-center">
                        <button type="button" class="btn btn-sm text-danger p-0" onclick="xoaMonKhoiPos(${idx})" title="Xóa dòng">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        }

        tinhToanHoaDonPos();
    }

    window.tinhToanHoaDonPos = function () {
        const tab = layTabHienTai();
        let tongTienHang = 0;
        let tongSoMon = 0;
        let tongSoHop = 0;

        tab.items.forEach(item => {
            tongTienHang += item.donGia * item.soLuong;
            tongSoMon += item.soLuong;
            if (item.loaiSanPham === 2 || item.loaiSanPham === 3) {
                tongSoHop += item.soLuong;
            }
        });

        let mocB2B = BANG_B2B_POS.find(b => tongSoHop >= b.tuHop && tongSoHop <= b.denHop);
        if (!mocB2B && tongSoHop > 0 && BANG_B2B_POS.length > 0 && tongSoHop > BANG_B2B_POS[BANG_B2B_POS.length - 1].denHop) {
            mocB2B = BANG_B2B_POS[BANG_B2B_POS.length - 1];
        }

        const phanTramCk = mocB2B ? mocB2B.phanTramCk : 0;
        const tienCkB2B = phanTramCk > 0 ? Math.round(tongTienHang * (phanTramCk / 100)) : 0;
        const mienPhiLogo = mocB2B ? mocB2B.mienPhiInLogo : false;
        const phiKhuonMacDinh = mocB2B ? mocB2B.phiKhuon : 500000;

        const yeuCauInLogo = document.getElementById('chkPosYeuCauInLogo')?.checked || false;
        const phiInLogo = (yeuCauInLogo && !mienPhiLogo) ? phiKhuonMacDinh : 0;

        document.getElementById('lblPosTenMocB2B').innerText = mocB2B
            ? `${mocB2B.tenMoc} (-${phanTramCk}%)`
            : 'Tự động áp dụng từ 10 hộp';
        document.getElementById('lblPosTienCkB2B').innerText = tienCkB2B > 0 ? `-${dinhDangN0(tienCkB2B)}` : '0';
        document.getElementById('lblPosPhiInLogo').innerText = yeuCauInLogo
            ? (mienPhiLogo ? 'Miễn phí 100%' : `+${dinhDangN0(phiInLogo)}`)
            : '0';

        const codeKm = document.getElementById('selPosVoucher')?.value || '';
        let tienVoucher = 0;
        if (codeKm) {
            const km = DS_VOUCHER_POS.find(k => k.codeKm === codeKm);
            if (km && tongTienHang >= km.donToiThieu) {
                if (km.loaiGiam === 'PHAN_TRAM') {
                    tienVoucher = Math.round(tongTienHang * (km.giaTriGiam / 100));
                    if (km.giamToiDa > 0 && tienVoucher > km.giamToiDa) tienVoucher = km.giamToiDa;
                } else {
                    tienVoucher = km.giaTriGiam;
                    if (km.giamToiDa > 0 && tienVoucher > km.giamToiDa) tienVoucher = km.giamToiDa;
                }
            }
        }
        document.getElementById('lblPosTienVoucher').innerText = tienVoucher > 0 ? `-${dinhDangN0(tienVoucher)}` : '0';

        const phiShip = (tab.cheDoMua === 'GIAO_HANG_QR' && tab.items.length > 0) ? 30000 : 0;
        document.getElementById('rowPosPhiShip').style.display = (tab.cheDoMua === 'GIAO_HANG_QR') ? 'flex' : 'none';
        document.getElementById('lblPosPhiShip').innerText = dinhDangN0(phiShip);

        khachCanTraHienTai = Math.max(0, tongTienHang - tienCkB2B - tienVoucher + phiInLogo + phiShip);

        document.getElementById('lblPosTongSoMon').innerText = `${tongSoMon} món`;
        document.getElementById('lblPosTongSoHop').innerText = `${tongSoHop} hộp`;
        document.getElementById('lblPosTongTienHang').innerText = dinhDangN0(tongTienHang);
        document.getElementById('lblPosKhachCanTra').innerText = dinhDangN0(khachCanTraHienTai) + ' đ';

        const inpDua = document.getElementById('inpPosTienKhachDua');
        if (inpDua && (!inpDua.dataset.userEdited || Number(inpDua.value) === 0)) {
            inpDua.value = khachCanTraHienTai;
        }
        tinhTienThuaPos();
        capNhatAnhVietQrPos();
        luuThongTinTabHienTai();
    };

    window.chonMenhGiaPos = function (menhGia) {
        const inp = document.getElementById('inpPosTienKhachDua');
        if (!inp) return;
        inp.dataset.userEdited = '1';
        inp.value = menhGia === 0 ? khachCanTraHienTai : menhGia;
        tinhTienThuaPos();
        luuThongTinTabHienTai();
    };

    window.tinhTienThuaPos = function () {
        const tienDua = Number(document.getElementById('inpPosTienKhachDua')?.value) || 0;
        const tienThua = Math.max(0, tienDua - khachCanTraHienTai);
        const lbl = document.getElementById('lblPosTienThua');
        if (lbl) lbl.innerText = dinhDangN0(tienThua) + ' đ';
    };

    window.doiPhuongThucTtPos = function (pt, boQuaLuu) {
        document.getElementById('boxPosTienMat').style.display = (pt === 'TIEN_MAT') ? 'block' : 'none';
        document.getElementById('boxPosVietQr').style.display = (pt === 'CHUYEN_KHOAN_QR') ? 'block' : 'none';
        capNhatAnhVietQrPos();
        if (!boQuaLuu) luuThongTinTabHienTai();
    };

    function capNhatAnhVietQrPos() {
        const img = document.getElementById('imgPosVietQrLive');
        if (!img) return;
        const amount = Math.max(0, Math.round(khachCanTraHienTai));
        delete img.dataset.fallbackApplied;
        img.src = `https://img.vietqr.io/image/MB-0814585526-compact2.png?amount=${amount}&addInfo=${encodeURIComponent('POS NGUYET CAT')}`;
    }

    window.chuyenCheDoMuaPos = function (cheDo, boQuaLuu) {
        const tab = layTabHienTai();
        tab.cheDoMua = cheDo;
        const isGiao = (cheDo === 'GIAO_HANG_QR');
        document.getElementById('btnModeTaiQuay')?.classList.toggle('active', !isGiao);
        document.getElementById('btnModeGiaoHangQr')?.classList.toggle('active', isGiao);
        const boxGiao = document.getElementById('boxGiaoHangQrPos');
        if (boxGiao) boxGiao.style.display = isGiao ? 'block' : 'none';
        if (!boQuaLuu) tinhToanHoaDonPos();
    };

    window.timKhachHangPosAjax = async function (tuKhoa) {
        const dd = document.getElementById('dropdownKhachPos');
        if (!dd) return;
        try {
            const resp = await fetch(`/Admin/BanHangPos/TimKhachHangPos?tuKhoa=${encodeURIComponent(tuKhoa || '')}`);
            const data = await resp.json();
            if (!data.thanhCong || !Array.isArray(data.danhSach) || data.danhSach.length === 0) {
                dd.innerHTML = `<div class="p-2 text-muted small">Không tìm thấy thành viên. Bấm <strong>[+ Thêm khách mới]</strong> để mở thẻ.</div>`;
                dd.style.display = 'block';
                return;
            }
            dd.innerHTML = '';
            data.danhSach.forEach(u => {
                const div = document.createElement('div');
                div.className = 'pos-cust-option';
                div.innerHTML = `
                    <div class="d-flex justify-content-between">
                        <strong>${u.hoTen}</strong>
                        <span class="badge bg-light text-primary border">${u.hangTv} • ${u.diemTichLuy} điểm</span>
                    </div>
                    <small class="text-muted">${u.sdt} • ${u.email}</small>
                `;
                div.onclick = () => chonKhachHangVaoTabPos(u);
                dd.appendChild(div);
            });
            dd.style.display = 'block';
        } catch (e) { }
    };

    window.chonKhachHangVaoTabPos = function (khach) {
        const tab = layTabHienTai();
        tab.khachHang = khach;
        document.getElementById('dropdownKhachPos').style.display = 'none';
        document.getElementById('txtPosTimKhach').value = '';
        hienThiKhachHangTrenUI(khach);
        luuStoragePos();
    };

    window.boChonKhachHangPos = function () {
        const tab = layTabHienTai();
        tab.khachHang = null;
        hienThiKhachHangTrenUI(null);
        luuStoragePos();
    };

    function hienThiKhachHangTrenUI(khach) {
        const boxChon = document.getElementById('boxThongTinKhachDaChon');
        const boxTim = document.getElementById('boxTimKhachPos');
        if (!boxChon || !boxTim) return;
        if (khach && khach.maNd) {
            boxTim.style.display = 'none';
            boxChon.style.display = 'block';
            document.getElementById('lblPosTenKhach').innerText = khach.hoTen;
            document.getElementById('lblPosSdtKhach').innerText = khach.sdt;
            document.getElementById('lblPosHangTv').innerText = khach.hangTv || 'Đồng';
            document.getElementById('lblPosDiemKhach').innerText = khach.diemTichLuy || 0;
        } else {
            boxTim.style.display = 'block';
            boxChon.style.display = 'none';
        }
    }

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
            chonKhachHangVaoTabPos(res.khach);
        } catch (e) {
            alert('Lỗi kết nối máy chủ!');
        }
    };

    document.addEventListener('click', function (e) {
        const box = document.getElementById('boxTimKhachPos');
        if (box && !box.contains(e.target)) {
            const dd = document.getElementById('dropdownKhachPos');
            if (dd) dd.style.display = 'none';
        }
    });

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

        const tab = layTabHienTai();
        tab.items.push({
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

        bootstrap.Modal.getInstance(document.getElementById('modalDongHopPos'))?.hide();
        luuStoragePos();
        renderThanhMultiTabs();
        renderBangGioHangPos();
    };

    window.thucHienThanhToanPos = async function () {
        luuThongTinTabHienTai();
        const tab = layTabHienTai();

        if (!tab.items || tab.items.length === 0) {
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
            dongTabHoaDon(tab.id);
        } catch (err) {
            alert('Lỗi kết nối khi thanh toán tại quầy!');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-printer-fill"></i> THANH TOÁN (F9)';
        }
    };

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

                const banhPos = DS_BANH_POS.find(b => b.maBanh === maBanh);
                if (banhPos) {
                    banhPos.phanTramCanDate = data.mucGiam || 30;
                    banhPos.dangXaCanDate = true;
                    banhPos.giaBan = data.giaMoi || Math.round(banhPos.giaGoc * 0.7);
                }

                if (Array.isArray(danhSachTabs)) {
                    danhSachTabs.forEach(t => {
                        if (Array.isArray(t.items)) {
                            t.items.forEach(it => {
                                if (it.loaiSanPham === 1 && it.maBanh === maBanh && banhPos) {
                                    it.donGia = banhPos.giaBan;
                                }
                            });
                        }
                    });
                }

                locSanPhamPos();
                renderBangGioHangPos();
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
            moModalDongHopTuChonPos();
        } else if (e.key === 'F9') {
            e.preventDefault();
            thucHienThanhToanPos();
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        damBaoModalPhuPosTonTai();
        locSanPhamPos();
        khoiTaoMultiTabsPos();
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
        <h6 class="modal-title fw-bold mb-0"><i class="bi bi-check-circle-fill me-1"></i> Hóa đơn nhiệt K80 - KiotViet POS</h6>
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
