// =========================================================================
// MODULE THANH TOÁN: SỔ ĐỊA CHỈ ĐA ĐIỂM NHẬN QUÀ & THIỆP CHÚC MỪNG QR 3D
// =========================================================================
(function () {
    const cfg = window.CAU_HINH_THANH_TOAN || {};
    const NGAY_HEN_MAC_DINH = cfg.ngayHenMacDinh || "";
    const tongSoLuongSanPham = Number(cfg.tongSoLuongSanPham || 1);

    window.laTinhHoTroHoaToc = function (tinhThanh) {
        if (!tinhThanh) return false;
        const t = tinhThanh.toLowerCase();
        return t.includes("hồ chí minh") || t.includes("an giang") || t.includes("long xuyên");
    };

    window.kiemTraTinhThanhVaCapNhatHoaToc = function () {
        const tinhThanh = document.getElementById("ddlTinhThanh")?.value || "TP. Hồ Chí Minh";
        const dot0Tinh = document.getElementById("dot0_TinhThanhPho");
        if (dot0Tinh) dot0Tinh.value = tinhThanh;

        const radHoaToc = document.getElementById("radGiaoHoaToc");
        const radTieuChuan = document.getElementById("radGiaoTieuChuan");
        const lblDongHoaToc = document.getElementById("lblDongGiaoHoaToc");
        const thongBaoHoaToc = document.getElementById("thongBaoGioiHanHoaToc");

        const hoTro = laTinhHoTroHoaToc(tinhThanh);

        if (!hoTro) {
            if (radHoaToc) {
                if (radHoaToc.checked && radTieuChuan) {
                    radTieuChuan.checked = true;
                }
                radHoaToc.disabled = true;
            }
            if (lblDongHoaToc) {
                lblDongHoaToc.style.opacity = "0.55";
                lblDongHoaToc.style.cursor = "not-allowed";
            }
            if (thongBaoHoaToc) {
                thongBaoHoaToc.style.display = "block";
            }
        } else {
            if (radHoaToc) radHoaToc.disabled = false;
            if (lblDongHoaToc) {
                lblDongHoaToc.style.opacity = "1";
                lblDongHoaToc.style.cursor = "pointer";
            }
            if (thongBaoHoaToc) {
                thongBaoHoaToc.style.display = "none";
            }
        }

        if (typeof window.capNhatBangTinhThanhToan === "function") {
            window.capNhatBangTinhThanhToan();
        }
    };

    window.chuyenDoiHinhThucNhanHang = function () {
        const laNhanTaiCuaHang = document.getElementById("radNhanTaiCuaHang")?.checked === true;
        const khungDiaChiGiao = document.getElementById("khungDiaChiGiaoTanNoi");
        const khungCuaHang = document.getElementById("khungNhanTaiCuaHang");

        document.getElementById("lblChonGiaoTanNoi")?.classList.toggle("dang-chon", !laNhanTaiCuaHang);
        document.getElementById("lblChonNhanTaiCuaHang")?.classList.toggle("dang-chon", laNhanTaiCuaHang);

        if (khungDiaChiGiao) khungDiaChiGiao.style.display = laNhanTaiCuaHang ? "none" : "block";
        if (khungCuaHang) khungCuaHang.style.display = laNhanTaiCuaHang ? "block" : "none";

        capNhatGiaoDienNguoiKhacNhan();
        dongBoDiaChiGiaoChinh();
        if (typeof window.capNhatBangTinhThanhToan === "function") {
            window.capNhatBangTinhThanhToan();
        }
    };

    window.capNhatGiaoDienNguoiKhacNhan = function () {
        const laNhanTaiCuaHang = document.getElementById("radNhanTaiCuaHang")?.checked === true;
        const lblChk = document.getElementById("lblChkGoiNguoiKhacNhan");
        const container = document.getElementById("danhSachCacDotGiaoContainer");
        const btnThem = document.getElementById("btnThemDiemNhanQua");

        if (laNhanTaiCuaHang) {
            if (lblChk) lblChk.textContent = "Gọi người khác nhận hàng tại cửa hàng";
            if (container) {
                container.innerHTML = "";
                container.style.display = "none";
            }
            if (btnThem) btnThem.style.display = "none";
        } else if (tongSoLuongSanPham <= 1) {
            if (lblChk) lblChk.textContent = "Gọi người khác nhận hàng";
            if (container) {
                container.innerHTML = "";
                container.style.display = "none";
            }
            if (btnThem) btnThem.style.display = "none";
        } else {
            if (lblChk) lblChk.textContent = `Gọi người khác nhận hàng (Chia điểm nhận tối đa ${tongSoLuongSanPham} địa chỉ)`;
            if (container) {
                container.style.display = "block";
            }
            const soDiemHienTai = 1 + (container ? container.querySelectorAll(".item-dot-giao").length : 0);
            if (btnThem) {
                btnThem.style.display = soDiemHienTai >= tongSoLuongSanPham ? "none" : "inline-block";
            }
        }
    };

    window.dongBoDiaChiGiaoChinh = function () {
        const laNhanTaiCuaHang = document.getElementById("radNhanTaiCuaHang")?.checked === true;
        const inputDiaChiServer = document.getElementById("dot0_DiaChi");
        if (!inputDiaChiServer) return;

        if (laNhanTaiCuaHang) {
            const radCuaHang = document.querySelector('input[name="CuaHangNhan"]:checked');
            inputDiaChiServer.value = radCuaHang
                ? radCuaHang.value
                : "[Nhận tại cửa hàng] Nguyệt Cát – Quận 1, Nguyễn Huệ (128 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh - 0909 123 456)";
        } else {
            const soNha = document.getElementById("txtDiaChiSoNha")?.value.trim() || "";
            const phuongQuan = document.getElementById("txtPhuongXaQuanHuyen")?.value.trim() || "";
            const tinhThanh = document.getElementById("ddlTinhThanh")?.value.trim() || "";

            const cacPhan = [soNha, phuongQuan, tinhThanh].filter(p => p.length > 0);
            inputDiaChiServer.value = soNha.length > 0 ? cacPhan.join(", ") : "";
        }
    };

    window.chonTuDropdownSoDiaChi = function (selectEl) {
        const opt = selectEl.options[selectEl.selectedIndex];
        if (!opt || !opt.value) return;

        const soNhaEl = document.getElementById("txtDiaChiSoNha");
        const phuongQuanEl = document.getElementById("txtPhuongXaQuanHuyen");
        const tinhThanhEl = document.getElementById("ddlTinhThanh");

        if (soNhaEl) soNhaEl.value = opt.value;
        if (phuongQuanEl && opt.dataset.phuongquan) phuongQuanEl.value = opt.dataset.phuongquan;
        if (tinhThanhEl && opt.dataset.tinhthanh) {
            for (let i = 0; i < tinhThanhEl.options.length; i++) {
                if (tinhThanhEl.options[i].value.includes(opt.dataset.tinhthanh) || opt.dataset.tinhthanh.includes(tinhThanhEl.options[i].value)) {
                    tinhThanhEl.selectedIndex = i;
                    break;
                }
            }
        }

        if (opt.dataset.ten && document.getElementById("dot0_TenNguoiNhan")) {
            document.getElementById("dot0_TenNguoiNhan").value = opt.dataset.ten;
        }
        if (opt.dataset.sdt && document.getElementById("dot0_SdtNguoiNhan")) {
            document.getElementById("dot0_SdtNguoiNhan").value = opt.dataset.sdt;
        }

        dongBoDiaChiGiaoChinh();
        kiemTraTinhThanhVaCapNhatHoaToc();
        if (typeof hienThiThongBaoGioHang === "function") {
            hienThiThongBaoGioHang("Đã điền nhanh địa chỉ giao hàng đã lưu!", true);
        }
    };

    window.batTatKhungTuyChonMoRong = function (idCheckbox, idKhung) {
        const chk = document.getElementById(idCheckbox);
        const khung = document.getElementById(idKhung);
        if (chk && khung) {
            khung.style.display = chk.checked ? "block" : "none";
        }
    };

    window.xuLyBatTatThiepQrChinh = function (chk) {
        const khung = document.getElementById("khungChiTietThiepQr");
        const hidCoThiep = document.getElementById("hidCoThiep3D");
        const txtKinhGui = document.getElementById("txtKinhGuiTrenThiep_0");
        const txtNguoiGui = document.getElementById("txtNguoiGuiTrenThiep_0");
        const txtLoiChuc = document.getElementById("txtLoiChucThiep_0");

        const batThiep = Boolean(chk && chk.checked);
        if (khung) khung.style.display = batThiep ? "block" : "none";
        if (hidCoThiep) hidCoThiep.value = batThiep ? "true" : "false";

        if (txtKinhGui) txtKinhGui.required = batThiep;
        if (txtNguoiGui) {
            txtNguoiGui.required = batThiep;
            if (batThiep && !txtNguoiGui.value.trim()) {
                txtNguoiGui.value = document.getElementById("txtHoTenNguoiMua")?.value.trim() || "";
            }
        }
        if (txtLoiChuc) txtLoiChuc.required = batThiep;
        kiemTraTrungTenThiep3D();
    };

    window.kiemTraTrungTenThiep3D = function () {
        const chk = document.getElementById("chkThiepQr_0");
        const canhBao = document.getElementById("canhBaoTrungTenThiep3D");
        if (!chk || !chk.checked || !canhBao) {
            if (canhBao) canhBao.style.display = "none";
            return;
        }
        const kinhGui = (document.getElementById("txtKinhGuiTrenThiep_0")?.value || "").trim().toLowerCase();
        const nguoiGui = (document.getElementById("txtNguoiGuiTrenThiep_0")?.value || "").trim().toLowerCase();
        canhBao.style.display = (kinhGui.length > 0 && kinhGui === nguoiGui) ? "block" : "none";
    };

    window.xuLyBatTatNguoiKhacNhan = function () {
        batTatKhungTuyChonMoRong("chkGoiNguoiKhacNhan", "khungNguoiKhacNhanHang");
        capNhatGiaoDienNguoiKhacNhan();
        dongBoNguoiMuaSangNguoiNhan();
    };

    window.dongBoNguoiMuaSangNguoiNhan = function () {
        const chkNguoiKhac = document.getElementById("chkGoiNguoiKhacNhan");
        const hoTenMua = document.getElementById("txtHoTenNguoiMua")?.value.trim() || "";
        const sdtMua = document.getElementById("txtSdtNguoiMua")?.value.trim() || "";

        const elTenNhan0 = document.getElementById("dot0_TenNguoiNhan");
        const elSdtNhan0 = document.getElementById("dot0_SdtNguoiNhan");
        const elDiemTen0 = document.getElementById("diem0_TenNguoiNhan");
        const elDiemSdt0 = document.getElementById("diem0_SdtNguoiNhan");

        if (!chkNguoiKhac || !chkNguoiKhac.checked) {
            if (elTenNhan0) elTenNhan0.value = hoTenMua;
            if (elSdtNhan0) elSdtNhan0.value = sdtMua;
        }

        if (elDiemTen0) elDiemTen0.value = elTenNhan0?.value || hoTenMua;
        if (elDiemSdt0) elDiemSdt0.value = elSdtNhan0?.value || sdtMua;

        const txtNguoiGuiThiep = document.getElementById("txtNguoiGuiTrenThiep_0");
        if (txtNguoiGuiThiep && !txtNguoiGuiThiep.dataset.daSuaTay) {
            txtNguoiGuiThiep.value = hoTenMua;
        }
    };

    window.batTatKhungThiepQr = function (chk) {
        const khungThiep = chk.closest(".khung-thiep-qr-phu");
        if (!khungThiep) return;
        const vungLoiChuc = khungThiep.querySelector(".noi-dung-loi-chuc-qr");
        if (vungLoiChuc) {
            vungLoiChuc.style.display = chk.checked ? "block" : "none";
        }
    };

    window.themDotGiaoHangMoi = function () {
        const laNhanTaiCuaHang = document.getElementById("radNhanTaiCuaHang")?.checked === true;
        if (laNhanTaiCuaHang || tongSoLuongSanPham <= 1) {
            capNhatGiaoDienNguoiKhacNhan();
            return;
        }

        const container = document.getElementById("danhSachCacDotGiaoContainer");
        const soDiemPhuHienTai = container.querySelectorAll(".item-dot-giao").length;
        const tongSoDiemHienTai = 1 + soDiemPhuHienTai;
        if (tongSoDiemHienTai >= tongSoLuongSanPham) {
            capNhatGiaoDienNguoiKhacNhan();
            return;
        }

        const idx = soDiemPhuHienTai + 1;
        const tenNguoiMua = document.getElementById("txtHoTenNguoiMua")?.value.trim() || "";

        const div = document.createElement("div");
        div.className = "item-dot-giao pt-2 mt-2 border-top";
        div.dataset.index = idx;
        div.innerHTML = `
            <div class="d-flex justify-content-between align-items-center mb-2">
                <strong class="small" style="color: #111827;">
                    <span class="tieu-de-dot-giao">Điểm nhận quà #${idx + 1}</span>
                </strong>
                <button type="button" class="btn btn-sm btn-link text-danger p-0 text-decoration-none" onclick="xoaDotGiaoHang(this)">
                    Xóa
                </button>
            </div>

            <div class="row g-2">
                <div class="col-md-6">
                    <input type="text" name="DanhSachDotGiao[${idx}].TenNguoiNhan" required
                           class="input-phang-bitis" placeholder="Họ và tên người nhận *" />
                </div>
                <div class="col-md-6">
                    <input type="text" name="DanhSachDotGiao[${idx}].SdtNguoiNhan" required
                           class="input-phang-bitis" placeholder="Số điện thoại người nhận *" />
                </div>
                <div class="col-12">
                    <input type="text" name="DanhSachDotGiao[${idx}].DiaChiGiaoChiTiet" required
                           class="input-phang-bitis" placeholder="Địa chỉ nhận quà chi tiết *" />
                </div>
                <input type="hidden" name="DanhSachDotGiao[${idx}].NgayHenGiao" value="${NGAY_HEN_MAC_DINH}" />
                <input type="hidden" name="DanhSachDotGiao[${idx}].KhungGioGiao" value="Sáng (08:00 - 11:30)" />
            </div>

            <div class="khung-thiep-qr-phu mt-2">
                <div class="form-check m-0">
                    <input class="form-check-input" type="checkbox" name="DanhSachDotGiao[${idx}].CoKemThiepQr" value="true"
                           onchange="batTatKhungThiepQr(this)">
                    <label class="form-check-label small" style="color: #1F2937;">
                        Đính kèm Thiệp Chúc Tết Đoàn Viên QR 3D riêng (Miễn phí)
                    </label>
                </div>
                <div class="noi-dung-loi-chuc-qr mt-2" style="display: none;">
                    <div class="row g-2 mb-2">
                        <div class="col-md-6">
                            <input type="text" name="DanhSachDotGiao[${idx}].TenNguoiNhanThiep"
                                   class="input-phang-bitis" placeholder="Kính gửi *" />
                        </div>
                        <div class="col-md-6">
                            <input type="text" name="DanhSachDotGiao[${idx}].TenNguoiGuiThiep" value="${tenNguoiMua}"
                                   class="input-phang-bitis" placeholder="Người gửi *" />
                        </div>
                    </div>
                    <textarea name="DanhSachDotGiao[${idx}].LoiChucThiep" rows="2" class="form-control form-control-sm"
                              placeholder="Lời chúc riêng cho điểm nhận này...">Kính chúc Quý Đối tác &amp; Gia đình mùa Tết Trung Thu Đoàn Viên, Thịnh Vượng!</textarea>
                </div>
            </div>
        `;

        container.appendChild(div);
        danhSoLaiCacDotGiao();
        capNhatGiaoDienNguoiKhacNhan();
        if (typeof window.capNhatBangTinhThanhToan === "function") {
            window.capNhatBangTinhThanhToan();
        }
    };

    window.xoaDotGiaoHang = function (btn) {
        const item = btn.closest(".item-dot-giao");
        if (item) {
            item.remove();
            danhSoLaiCacDotGiao();
            capNhatGiaoDienNguoiKhacNhan();
            if (typeof window.capNhatBangTinhThanhToan === "function") {
                window.capNhatBangTinhThanhToan();
            }
        }
    };

    window.danhSoLaiCacDotGiao = function () {
        const items = document.querySelectorAll("#danhSachCacDotGiaoContainer .item-dot-giao");
        items.forEach((item, i) => {
            const index = i + 1;
            item.dataset.index = index;
            const titleEl = item.querySelector(".tieu-de-dot-giao");
            if (titleEl) {
                titleEl.textContent = `Điểm nhận quà #${index + 1}`;
            }
            item.querySelectorAll("input, select, textarea").forEach(input => {
                if (input.name && input.name.startsWith("DanhSachDotGiao[")) {
                    input.name = input.name.replace(/DanhSachDotGiao\[\d+\]/, `DanhSachDotGiao[${index}]`);
                }
            });
        });
    };
})();
