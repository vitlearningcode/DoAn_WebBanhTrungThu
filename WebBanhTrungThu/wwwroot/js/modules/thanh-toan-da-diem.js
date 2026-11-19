// =========================================================================
// MODULE THANH TOÁN: ĐỊA CHỈ GIAO HÀNG, NHẬN HÀNG THAY & THIỆP CHÚC MỪNG QR 3D
// =========================================================================
(function () {
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
        const khungCuaHang = document.getElementById("khung-chon-cua-hang") || document.getElementById("khungNhanTaiCuaHang");

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

        if (lblChk) {
            lblChk.textContent = laNhanTaiCuaHang
                ? "Nhờ người khác đến cửa hàng nhận thay (nếu có)"
                : "Gọi người khác nhận hàng thay (nếu có)";
        }
    };

    window.dongBoDiaChiGiaoChinh = function () {
        const laNhanTaiCuaHang = document.getElementById("radNhanTaiCuaHang")?.checked === true;
        const inputDiaChiServer = document.getElementById("dot0_DiaChi");
        const dot0Tinh = document.getElementById("dot0_TinhThanhPho");
        if (!inputDiaChiServer) return;

        document.querySelectorAll("#khung-chon-cua-hang .the-cua-hang-nhan, #khungNhanTaiCuaHang .the-cua-hang-nhan").forEach(the => {
            const rad = the.querySelector('input[type="radio"]');
            the.classList.toggle("dang-chon", Boolean(rad && rad.checked));
        });

        if (laNhanTaiCuaHang) {
            const radCuaHang = document.querySelector('input[name="CuaHangNhan"]:checked');
            inputDiaChiServer.value = radCuaHang
                ? radCuaHang.value
                : "[Nhận tại cửa hàng] Nguyệt Cát – Chi nhánh TP. Long Xuyên (Số 18 Đường Hà Hoàng Hổ, Phường Mỹ Xuyên, TP. Long Xuyên, An Giang)";
            if (dot0Tinh) {
                dot0Tinh.value = radCuaHang?.dataset?.tinhthanh || "An Giang";
            }
        } else {
            const soNha = document.getElementById("txtDiaChiSoNha")?.value.trim() || "";
            const phuongQuan = document.getElementById("txtPhuongXaQuanHuyen")?.value.trim() || "";
            const tinhThanh = document.getElementById("ddlTinhThanh")?.value.trim() || "";

            const cacPhan = [soNha, phuongQuan, tinhThanh].filter(p => p.length > 0);
            inputDiaChiServer.value = soNha.length > 0 ? cacPhan.join(", ") : "";
            if (dot0Tinh && tinhThanh) {
                dot0Tinh.value = tinhThanh;
            }
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
        const chk = document.getElementById("chkGoiNguoiKhacNhan");
        const bat = Boolean(chk && chk.checked);
        batTatKhungTuyChonMoRong("chkGoiNguoiKhacNhan", "khungNguoiKhacNhanHang");
        capNhatGiaoDienNguoiKhacNhan();

        const elTenNhan0 = document.getElementById("dot0_TenNguoiNhan");
        const elSdtNhan0 = document.getElementById("dot0_SdtNguoiNhan");
        const hoTenMua = document.getElementById("txtHoTenNguoiMua")?.value.trim() || "";
        const sdtMua = document.getElementById("txtSdtNguoiMua")?.value.trim() || "";

        if (elTenNhan0) {
            elTenNhan0.required = bat;
            if (bat && elTenNhan0.value.trim() === hoTenMua) {
                elTenNhan0.value = "";
            }
        }
        if (elSdtNhan0) {
            elSdtNhan0.required = bat;
            if (bat && elSdtNhan0.value.trim() === sdtMua) {
                elSdtNhan0.value = "";
            }
        }

        dongBoNguoiMuaSangNguoiNhan();
    };

    window.dongBoNguoiMuaSangNguoiNhan = function () {
        const chkNguoiKhac = document.getElementById("chkGoiNguoiKhacNhan");
        const hoTenMua = document.getElementById("txtHoTenNguoiMua")?.value.trim() || "";
        const sdtMua = document.getElementById("txtSdtNguoiMua")?.value.trim() || "";

        const elTenNhan0 = document.getElementById("dot0_TenNguoiNhan");
        const elSdtNhan0 = document.getElementById("dot0_SdtNguoiNhan");

        if (!chkNguoiKhac || !chkNguoiKhac.checked) {
            if (elTenNhan0) elTenNhan0.value = hoTenMua;
            if (elSdtNhan0) elSdtNhan0.value = sdtMua;
        }

        const txtNguoiGuiThiep = document.getElementById("txtNguoiGuiTrenThiep_0");
        if (txtNguoiGuiThiep && !txtNguoiGuiThiep.dataset.daSuaTay) {
            txtNguoiGuiThiep.value = hoTenMua;
        }
    };
})();
