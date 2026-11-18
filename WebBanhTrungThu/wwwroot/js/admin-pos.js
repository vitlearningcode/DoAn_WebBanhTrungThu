// =========================================================================
// KIOTVIET WEB POS - QUẢN LÝ ĐA HÓA ĐƠN (MULTI-TABS), GIỎ HÀNG & ĐÓNG HỘP TỰ CHỌN
// =========================================================================
(function () {
    const cfg = window.POS_CATALOG || {};
    const DS_BANH_POS = Array.isArray(cfg.dsBanhPos) ? cfg.dsBanhPos : [];
    const DS_COMBO_POS = Array.isArray(cfg.dsComboPos) ? cfg.dsComboPos : [];
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

    window.layTabPosHienTai = layTabHienTai;
    window.layKhachCanTraPosHienTai = function () {
        return khachCanTraHienTai;
    };

    window.capNhatGiaBanhXaKhoTrongTabsPos = function (maBanh, mucGiam, giaMoi) {
        const banhPos = DS_BANH_POS.find(b => b.maBanh === maBanh);
        if (banhPos) {
            banhPos.phanTramCanDate = mucGiam || 30;
            banhPos.dangXaCanDate = true;
            banhPos.giaBan = giaMoi || Math.round(banhPos.giaGoc * 0.7);
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
    };

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

    document.addEventListener('click', function (e) {
        const box = document.getElementById('boxTimKhachPos');
        if (box && !box.contains(e.target)) {
            const dd = document.getElementById('dropdownKhachPos');
            if (dd) dd.style.display = 'none';
        }
    });

    window.themMonHopTuChonVaoTabPos = function (itemHop) {
        const tab = layTabHienTai();
        tab.items.push(itemHop);
        luuStoragePos();
        renderThanhMultiTabs();
        renderBangGioHangPos();
    };

    document.addEventListener('DOMContentLoaded', function () {
        locSanPhamPos();
        khoiTaoMultiTabsPos();
    });
})();
