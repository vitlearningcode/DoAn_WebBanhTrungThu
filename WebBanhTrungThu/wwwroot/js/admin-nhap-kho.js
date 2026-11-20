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

    function tron2So(n) {
        return Math.round((Number(n) || 0) * 100) / 100;
    }

    function layThongTinNccDangChon() {
        const sel = document.getElementById('pn_MaNcc');
        if (!sel || !sel.value) {
            return { maNcc: '', tenNcc: '', phanTramCk: 0, thuongHieu: '' };
        }
        const opt = sel.options[sel.selectedIndex];
        return {
            maNcc: sel.value,
            tenNcc: opt?.getAttribute('data-ten') || '',
            phanTramCk: tron2So(opt?.getAttribute('data-ck') || 0),
            thuongHieu: opt?.getAttribute('data-thuonghieu') || ''
        };
    }

    function capNhatHienThiCkNcc() {
        const info = layThongTinNccDangChon();
        const hidTen = document.getElementById('pn_NhaCungCap');
        const lblCk = document.getElementById('pn_LblCkNcc');
        const btnApDung = document.getElementById('pn_BtnApDungCkNcc');

        if (hidTen) hidTen.value = info.tenNcc;
        if (lblCk) lblCk.innerText = `${info.phanTramCk}%`;
        if (btnApDung) {
            const hienNut = trangThaiPhieuDangSua !== 'DA_NHAP_KHO' && Boolean(info.maNcc) && dsDongNhap.length > 0;
            btnApDung.classList.toggle('d-none', !hienNut);
        }
    }

    function tinhDonGiaTuChietKhau(giaNiemYet, ck, giaVonFallback) {
        const giaGoc = Math.round(Number(giaNiemYet) || 0);
        const ptCk = Math.max(0, Math.min(100, Number(ck) || 0));
        if (giaGoc > 0) {
            return Math.max(0, Math.round(giaGoc * (100 - ptCk) / 100));
        }
        return Math.max(0, Math.round(Number(giaVonFallback) || 0));
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
                if (detail && detail.classList.contains('kv-detail-row')) {
                    detail.classList.remove('open');
                }
            }
        });
    };

    window.locSanPhamTheoThuongHieuNhap = function (giaTriChonSan) {
        const selTh = document.getElementById('pn_LocThuongHieu');
        const selSp = document.getElementById('pn_ChonSpSelect');
        if (!selSp) return;

        const th = (selTh?.value || 'ALL').trim();
        const ds = window.NGUYETCAT_DS_SP_NHAP || [];
        const dsLoc = th === 'ALL'
            ? ds
            : ds.filter(sp => (sp.thuongHieu || '').toLowerCase() === th.toLowerCase());

        const giaTriHienTai = giaTriChonSan !== undefined ? String(giaTriChonSan) : selSp.value;
        const opts = ['<option value="">-- Chọn bánh trung thu --</option>'];
        dsLoc.forEach(sp => {
            opts.push(`<option value="${sp.maBanh}">[${escapeHtml(sp.maSku)}] ${escapeHtml(sp.tenBanh)} • Giá bán: ${formatSo(sp.donGiaLe)} đ • Tồn: ${formatSo(sp.tonKho)}</option>`);
        });
        selSp.innerHTML = opts.join('');

        if (giaTriHienTai && dsLoc.some(sp => String(sp.maBanh) === giaTriHienTai)) {
            selSp.value = giaTriHienTai;
        } else {
            selSp.value = '';
        }
    };

    window.khiDoiNhaCungCapNhap = function () {
        const info = layThongTinNccDangChon();
        capNhatHienThiCkNcc();

        const selTh = document.getElementById('pn_LocThuongHieu');
        if (selTh && info.thuongHieu && info.thuongHieu.toLowerCase() !== 'đa thương hiệu') {
            const optKhop = Array.from(selTh.options).find(o => o.value.toLowerCase() === info.thuongHieu.toLowerCase());
            if (optKhop) {
                selTh.value = optKhop.value;
                locSanPhamTheoThuongHieuNhap('');
            }
        } else if (selTh && !info.maNcc) {
            selTh.value = 'ALL';
            locSanPhamTheoThuongHieuNhap('');
        }

        if (trangThaiPhieuDangSua !== 'DA_NHAP_KHO' && dsDongNhap.length > 0 && info.maNcc) {
            let coCapNhat = false;
            dsDongNhap.forEach(d => {
                if (!d.daSuaTayCk) {
                    d.phanTramChietKhau = info.phanTramCk;
                    d.donGiaNhap = tinhDonGiaTuChietKhau(d.giaNiemYet, d.phanTramChietKhau, d.donGiaNhap);
                    coCapNhat = true;
                }
            });
            if (coCapNhat) renderBangDongNhap();
        }
    };

    window.apDungCkNccChoTatCaDong = function () {
        const info = layThongTinNccDangChon();
        if (!info.maNcc || dsDongNhap.length === 0) return;

        dsDongNhap.forEach(d => {
            d.phanTramChietKhau = info.phanTramCk;
            d.donGiaNhap = tinhDonGiaTuChietKhau(d.giaNiemYet, d.phanTramChietKhau, d.donGiaNhap);
            d.daSuaTayCk = false;
        });
        renderBangDongNhap();
    };

    function themDongSanPham(sp) {
        if (!sp) return;
        const infoNcc = layThongTinNccDangChon();
        const giaNiemYet = Math.max(0, Math.round(Number(sp.donGiaLe) || 0));
        const ckMacDinh = infoNcc.maNcc ? infoNcc.phanTramCk : 0;
        const donGiaNhap = tinhDonGiaTuChietKhau(giaNiemYet, ckMacDinh, sp.giaVon || 30000);

        dsDongNhap.push({
            maSp: sp.maBanh,
            maSku: sp.maSku,
            tenBanh: sp.tenBanh,
            giaNiemYet: giaNiemYet,
            phanTramChietKhau: ckMacDinh,
            donGiaNhap: donGiaNhap,
            soLuong: 20,
            ngaySanXuat: ngayHomNayStr(0),
            hanSuDung: ngayHomNayStr(40),
            daSuaTayCk: false
        });
        renderBangDongNhap();
    }

    window.themDongTuSelectNhap = function () {
        const sel = document.getElementById('pn_ChonSpSelect');
        const maBanh = parseInt(sel?.value || '0', 10);
        if (!maBanh) return;
        const ds = window.NGUYETCAT_DS_SP_NHAP || [];
        const sp = ds.find(x => x.maBanh === maBanh);
        if (!sp) return;

        themDongSanPham(sp);
        sel.value = '';
        const inpMa = document.getElementById('pn_NhapNhanhMaBanh');
        if (inpMa) inpMa.value = '';
    };

    function timSanPhamTheoMaNhanh(tuKhoa) {
        const q = (tuKhoa || '').trim().toLowerCase();
        if (!q) return null;
        const ds = window.NGUYETCAT_DS_SP_NHAP || [];
        const khopChinhXac = ds.find(x => (x.maSku || '').toLowerCase() === q);
        if (khopChinhXac) return khopChinhXac;

        const dsBatDau = ds.filter(x => (x.maSku || '').toLowerCase().startsWith(q));
        if (dsBatDau.length === 1) return dsBatDau[0];

        const dsChua = ds.filter(x =>
            (x.maSku || '').toLowerCase().includes(q) ||
            (x.tenBanh || '').toLowerCase().includes(q));
        return dsChua.length === 1 ? dsChua[0] : null;
    }

    function ganSuKienNhapNhanhMaBanh() {
        const inpMa = document.getElementById('pn_NhapNhanhMaBanh');
        if (!inpMa || inpMa.dataset.bound === '1') return;
        inpMa.dataset.bound = '1';

        inpMa.addEventListener('input', function () {
            const sp = timSanPhamTheoMaNhanh(this.value);
            if (!sp) return;
            const selTh = document.getElementById('pn_LocThuongHieu');
            if (selTh && selTh.value !== 'ALL' && (sp.thuongHieu || '').toLowerCase() !== selTh.value.toLowerCase()) {
                selTh.value = 'ALL';
            }
            locSanPhamTheoThuongHieuNhap(String(sp.maBanh));
        });

        inpMa.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter') return;
            e.preventDefault();
            const sp = timSanPhamTheoMaNhanh(this.value);
            if (sp) {
                themDongSanPham(sp);
                this.value = '';
                const selSp = document.getElementById('pn_ChonSpSelect');
                if (selSp) selSp.value = '';
                this.focus();
            } else if ((this.value || '').trim()) {
                const selSp = document.getElementById('pn_ChonSpSelect');
                if (selSp && selSp.value) {
                    themDongTuSelectNhap();
                    this.value = '';
                    this.focus();
                } else {
                    alert('Không tìm thấy sản phẩm khớp duy nhất với mã vừa nhập.');
                }
            }
        });
    }

    window.renderBangDongNhap = function () {
        const tbody = document.getElementById('pn_TbodyDongNhap');
        if (!tbody) return;
        const chiSuaInfo = trangThaiPhieuDangSua === 'DA_NHAP_KHO';

        if (dsDongNhap.length === 0) {
            tbody.innerHTML = '<tr><td colspan="10" class="text-center text-muted py-3">Chưa có dòng sản phẩm nào. Hãy chọn thương hiệu, nhập mã bánh hoặc chọn sản phẩm phía trên.</td></tr>';
            capNhatHienThiCkNcc();
            tinhTongPhieuNhap();
            return;
        }

        tbody.innerHTML = dsDongNhap.map((d, idx) => {
            const tt = Math.max(0, (Number(d.soLuong) || 0) * (Number(d.donGiaNhap) || 0));
            const disabledAttr = chiSuaInfo ? 'disabled' : '';
            return `<tr>
                <td>${idx + 1}</td>
                <td>
                    <div class="fw-semibold text-dark">${escapeHtml(d.tenBanh)}</div>
                    <small class="text-primary">${escapeHtml(d.maSku)}</small>
                </td>
                <td class="text-end text-muted so-tien">${formatSo(d.giaNiemYet)} đ</td>
                <td>
                    <input type="number" id="pn_ck_${idx}" min="0" max="100" step="0.01" value="${tron2So(d.phanTramChietKhau)}" ${disabledAttr}
                           class="form-control form-control-sm text-center fw-semibold text-success"
                           oninput="capNhatChietKhauDong(${idx}, this.value)" />
                </td>
                <td>
                    <input type="number" id="pn_dg_${idx}" min="0" step="1" value="${Math.round(d.donGiaNhap)}" ${disabledAttr}
                           class="form-control form-control-sm text-end fw-semibold"
                           oninput="capNhatDonGiaNhapDong(${idx}, this.value)" />
                </td>
                <td>
                    <input type="number" min="1" step="1" value="${d.soLuong}" ${disabledAttr}
                           class="form-control form-control-sm text-center fw-bold"
                           onchange="capNhatDongNhap(${idx}, 'soLuong', this.value)" />
                </td>
                <td><input type="date" value="${d.ngaySanXuat}" ${disabledAttr} class="form-control form-control-sm" onchange="capNhatDongNhap(${idx}, 'ngaySanXuat', this.value)" /></td>
                <td><input type="date" value="${d.hanSuDung}" ${disabledAttr} class="form-control form-control-sm" onchange="capNhatDongNhap(${idx}, 'hanSuDung', this.value)" /></td>
                <td class="text-end fw-bold text-primary so-tien" id="pn_tt_${idx}">${formatSo(tt)} đ</td>
                <td class="text-center">${chiSuaInfo ? '' : `<button type="button" class="btn btn-sm text-danger p-0" onclick="xoaDongNhap(${idx})" title="Xóa dòng"><i class="bi bi-trash"></i></button>`}</td>
            </tr>`;
        }).join('');

        capNhatHienThiCkNcc();
        tinhTongPhieuNhap();
    };

    window.capNhatChietKhauDong = function (idx, val) {
        const d = dsDongNhap[idx];
        if (!d) return;
        const ckRaw = Number(val);
        const ck = Number.isFinite(ckRaw) ? Math.max(0, Math.min(100, tron2So(ckRaw))) : 0;
        d.phanTramChietKhau = ck;
        d.daSuaTayCk = true;
        if (d.giaNiemYet > 0) {
            d.donGiaNhap = Math.max(0, Math.round(d.giaNiemYet * (100 - ck) / 100));
            const dgEl = document.getElementById(`pn_dg_${idx}`);
            if (dgEl) dgEl.value = String(d.donGiaNhap);
        }
        const ttEl = document.getElementById(`pn_tt_${idx}`);
        if (ttEl) ttEl.innerText = formatSo(d.soLuong * d.donGiaNhap) + ' đ';
        tinhTongPhieuNhap();
    };

    window.capNhatDonGiaNhapDong = function (idx, val) {
        const d = dsDongNhap[idx];
        if (!d) return;
        const dgRaw = Number(val);
        const donGia = Number.isFinite(dgRaw) ? Math.max(0, Math.round(dgRaw)) : 0;
        d.donGiaNhap = donGia;
        d.daSuaTayCk = true;

        if (d.giaNiemYet > 0) {
            const ckMoi = Math.max(0, Math.min(100, tron2So(((d.giaNiemYet - donGia) / d.giaNiemYet) * 100)));
            d.phanTramChietKhau = ckMoi;
            const ckEl = document.getElementById(`pn_ck_${idx}`);
            if (ckEl) ckEl.value = String(ckMoi);
        }

        const ttEl = document.getElementById(`pn_tt_${idx}`);
        if (ttEl) ttEl.innerText = formatSo(d.soLuong * d.donGiaNhap) + ' đ';
        tinhTongPhieuNhap();
    };

    window.capNhatDongNhap = function (idx, field, val) {
        const d = dsDongNhap[idx];
        if (!d) return;
        if (field === 'soLuong') d.soLuong = Math.max(1, parseInt(val, 10) || 1);
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

    function thietLapCheDoModalNhap(laDaNhap) {
        document.getElementById('pn_BoxChonSanPham').style.display = laDaNhap ? 'none' : 'block';
        document.getElementById('pn_GiamGia').readOnly = laDaNhap;
        document.getElementById('pn_WrapTraNgay').style.display = laDaNhap ? 'none' : 'block';
        document.getElementById('pn_WrapPhuongThuc').style.display = laDaNhap ? 'none' : 'block';
        document.getElementById('pn_BtnLuuTam').style.display = laDaNhap ? 'none' : 'inline-flex';
        document.getElementById('pn_BtnHoanThanh').innerHTML = laDaNhap
            ? '<i class="bi bi-check2-circle"></i> Lưu thông tin phiếu'
            : '<i class="bi bi-check2-circle"></i> Hoàn thành nhập kho';
        capNhatHienThiCkNcc();
    }

    function xoaOptionNccTam() {
        const selNcc = document.getElementById('pn_MaNcc');
        if (!selNcc) return;
        Array.from(selNcc.options)
            .filter(o => o.getAttribute('data-tam') === '1')
            .forEach(o => o.remove());
    }

    window.moModalTaoPhieuNhap = function () {
        trangThaiPhieuDangSua = 'PHIEU_TAM';
        xoaOptionNccTam();
        document.getElementById('pn_MaPn').value = '0';
        document.getElementById('pn_ModalTitle').innerText = 'Tạo phiếu nhập hàng (Đa sản phẩm)';
        document.getElementById('pn_MaNcc').value = '';
        document.getElementById('pn_NhaCungCap').value = '';
        document.getElementById('pn_GhiChu').value = '';
        document.getElementById('pn_GiamGia').value = '0';
        document.getElementById('pn_TraNgay').value = '0';

        const selTh = document.getElementById('pn_LocThuongHieu');
        if (selTh) selTh.value = 'ALL';
        const inpMa = document.getElementById('pn_NhapNhanhMaBanh');
        if (inpMa) inpMa.value = '';
        locSanPhamTheoThuongHieuNhap('');

        dsDongNhap = [];
        thietLapCheDoModalNhap(false);
        renderBangDongNhap();
        ganSuKienNhapNhanhMaBanh();
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
        xoaOptionNccTam();

        document.getElementById('pn_MaPn').value = p.maPn;
        document.getElementById('pn_ModalTitle').innerText = `Cập nhật phiếu nhập ${p.soPhieuNhap}`;

        const selNcc = document.getElementById('pn_MaNcc');
        let daChonNcc = false;
        if (p.maNcc && Array.from(selNcc.options).some(o => o.value === String(p.maNcc))) {
            selNcc.value = String(p.maNcc);
            daChonNcc = true;
        } else if (p.nhaCungCap) {
            const optByName = Array.from(selNcc.options).find(o =>
                (o.getAttribute('data-ten') || '').toLowerCase() === p.nhaCungCap.toLowerCase());
            if (optByName) {
                selNcc.value = optByName.value;
                daChonNcc = true;
            }
        }
        if (!daChonNcc && p.nhaCungCap) {
            const optTam = document.createElement('option');
            optTam.value = p.maNcc ? String(p.maNcc) : '0';
            optTam.textContent = p.nhaCungCap;
            optTam.setAttribute('data-ten', p.nhaCungCap);
            optTam.setAttribute('data-ck', '0');
            optTam.setAttribute('data-thuonghieu', 'Đa thương hiệu');
            optTam.setAttribute('data-tam', '1');
            selNcc.appendChild(optTam);
            selNcc.value = optTam.value;
        }

        document.getElementById('pn_NhaCungCap').value = p.nhaCungCap || '';
        document.getElementById('pn_GhiChu').value = p.ghiChu || '';
        document.getElementById('pn_GiamGia').value = Math.round(p.giamGia || 0);
        document.getElementById('pn_TraNgay').value = Math.round(p.daThanhToan || 0);

        const selTh = document.getElementById('pn_LocThuongHieu');
        if (selTh) selTh.value = 'ALL';
        locSanPhamTheoThuongHieuNhap('');

        dsDongNhap = (p.chiTiets || []).map(c => {
            const giaNiemYet = Math.round(c.giaNiemYet || 0);
            const donGia = Math.round(c.donGiaNhap || 0);
            let ck = tron2So(c.phanTramChietKhau || 0);
            if (ck === 0 && giaNiemYet > 0 && donGia > 0 && donGia < giaNiemYet) {
                ck = tron2So(((giaNiemYet - donGia) / giaNiemYet) * 100);
            }
            return {
                maSp: c.maSp,
                maSku: c.maSku,
                tenBanh: c.tenBanh,
                giaNiemYet: giaNiemYet,
                phanTramChietKhau: ck,
                donGiaNhap: donGia,
                soLuong: c.soLuong,
                ngaySanXuat: c.ngaySanXuat,
                hanSuDung: c.hanSuDung,
                daSuaTayCk: true
            };
        });

        thietLapCheDoModalNhap(trangThaiPhieuDangSua === 'DA_NHAP_KHO');
        renderBangDongNhap();
        ganSuKienNhapNhanhMaBanh();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTaoSuaPhieuNhap')).show();
    };

    window.datCheDoLuuPhieuNhap = function (hoanThanh) {
        cheDoHoanThanh = hoanThanh;
        document.getElementById('pn_HoanThanhNhapKho').value = hoanThanh ? 'true' : 'false';
    };

    window.chuanBiSubmitPhieuNhap = function () {
        const info = layThongTinNccDangChon();
        const tenNcc = info.tenNcc || (document.getElementById('pn_NhaCungCap')?.value || '').trim();
        if (!tenNcc) {
            alert('Vui lòng chọn Nhà cung cấp!');
            return false;
        }
        document.getElementById('pn_NhaCungCap').value = tenNcc;

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
