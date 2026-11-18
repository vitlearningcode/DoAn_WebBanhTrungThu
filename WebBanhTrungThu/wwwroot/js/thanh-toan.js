// =========================================================================
// XỬ LÝ TƯƠNG TÁC TRANG THANH TOÁN CHUẨN BITI'S (CAPTCHA, TỒN KHO, ĐA ĐIỂM, VOUCHER)
// =========================================================================
(function () {
    const cfg = window.CAU_HINH_THANH_TOAN || {};
    const TONG_TIEN_HANG = Number(cfg.tongTienHang || 0);
    const TIEN_CK_B2B = Number(cfg.tienCkB2B || 0);
    let TIEN_GIAM_VOUCHER = Number(cfg.tienGiamVoucher || 0);
    const MIEN_PHI_LOGO = Boolean(cfg.mienPhiLogo);
    const PHI_LOGO_MAC_DINH = Number(cfg.phiLogoMacDinh || 500000);
    const NGAY_HEN_MAC_DINH = cfg.ngayHenMacDinh || "";
    const tongSoLuongSanPham = Number(cfg.tongSoLuongSanPham || 1);
    const MOC_FREESHIP_TIEU_CHUAN = 500000;

    let daXacThucCaptchaHopLe = false;
    let dangGuiDonHang = false;

    document.addEventListener("DOMContentLoaded", function () {
        damBaoModalCanhBaoTonTai();
        capNhatGiaoDienNguoiKhacNhan();
        dongBoNguoiMuaSangNguoiNhan();
        dongBoDiaChiGiaoChinh();
        kiemTraTinhThanhVaCapNhatHoaToc();
        capNhatBangTinhThanhToan();
        taiMaCaptchaMoi(false);

        if (cfg.tenBanhHetHangBanDau) {
            hienThiModalHetHang(cfg.tenBanhHetHangBanDau, cfg.thongBaoHetHangBanDau);
        }
    });

    function damBaoModalCanhBaoTonTai() {
        const khungCh = document.getElementById("khungNhanTaiCuaHang");
        if (khungCh && khungCh.children.length === 0) {
            khungCh.innerHTML = `
<label class="the-cua-hang-nhan"><div class="d-flex align-items-start gap-2"><input type="radio" name="CuaHangNhan" class="form-check-input mt-1" checked value="[Nhận tại cửa hàng] Nguyệt Cát – Quận 1, Nguyễn Huệ (128 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh - 0909 123 456)" onchange="dongBoDiaChiGiaoChinh()" /><div><strong class="d-block text-dark small">Nguyệt Cát – Quận 1, Nguyễn Huệ</strong><small class="text-muted d-block">128 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh</small></div></div><span class="small fw-semibold text-success text-nowrap">Miễn phí • 0đ</span></label>
<label class="the-cua-hang-nhan"><div class="d-flex align-items-start gap-2"><input type="radio" name="CuaHangNhan" class="form-check-input mt-1" value="[Nhận tại cửa hàng] Nguyệt Cát – Quận 3, Pasteur (215 Pasteur, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh - 0909 654 321)" onchange="dongBoDiaChiGiaoChinh()" /><div><strong class="d-block text-dark small">Nguyệt Cát – Quận 3, Pasteur</strong><small class="text-muted d-block">215 Pasteur, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh</small></div></div><span class="small fw-semibold text-success text-nowrap">Miễn phí • 0đ</span></label>
<label class="the-cua-hang-nhan"><div class="d-flex align-items-start gap-2"><input type="radio" name="CuaHangNhan" class="form-check-input mt-1" value="[Nhận tại cửa hàng] Nguyệt Cát – Long Xuyên, An Giang (45 Trần Hưng Đạo, Phường Mỹ Xuyên, TP. Long Xuyên, An Giang - 0909 888 999)" onchange="dongBoDiaChiGiaoChinh()" /><div><strong class="d-block text-dark small">Nguyệt Cát – Long Xuyên, An Giang</strong><small class="text-muted d-block">45 Trần Hưng Đạo, Phường Mỹ Xuyên, TP. Long Xuyên, An Giang</small></div></div><span class="small fw-semibold text-success text-nowrap">Miễn phí • 0đ</span></label>`;
        }
        if (!document.getElementById("modalThongBaoHetHang")) {
            document.body.insertAdjacentHTML("beforeend", `
<div class="modal fade" id="modalThongBaoHetHang" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content border-0 shadow-lg" style="border-radius: 16px; overflow: hidden;">
      <div class="modal-header text-white border-0 py-3" style="background: linear-gradient(135deg, #7A1215, #4A080A);">
        <h5 class="modal-title fw-bold d-flex align-items-center gap-2"><i class="bi bi-exclamation-octagon-fill text-warning"></i>Thông Báo Tình Trạng Tồn Kho Nguyệt Cát</h5>
        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Đóng"></button>
      </div>
      <div class="modal-body p-4" style="background: #FFFDF9;">
        <p id="noiDungModalHetHang" class="mb-3 fw-medium" style="color: #1F2937; line-height: 1.6; font-size: 0.96rem;"></p>
        <div class="p-3 rounded-3 small" style="background: #FEF2F2; color: #991B1B; border: 1px solid #FECACA;"><i class="bi bi-shield-check me-1"></i>Hệ thống kiểm tra tồn kho thực tế theo lô sản xuất (FEFO) để đảm bảo mọi lễ hộp giao đến tay Quý khách đều mới nhất.</div>
      </div>
      <div class="modal-footer border-0 bg-light px-4 py-3 d-flex justify-content-between">
        <button type="button" class="btn btn-outline-secondary rounded-pill px-3" data-bs-dismiss="modal">Đóng lại</button>
        <button type="button" class="btn text-white rounded-pill px-4 fw-bold" style="background: #7A1215;" onclick="bootstrap.Modal.getInstance(document.getElementById('modalThongBaoHetHang'))?.hide(); moNganKeoGioHang();"><i class="bi bi-bag-check me-1"></i>Điều chỉnh Giỏ quà ngay</button>
      </div>
    </div>
  </div>
</div>`);
        }
        if (!document.getElementById("modalCanhBaoMang")) {
            document.body.insertAdjacentHTML("beforeend", `
<div class="modal fade" id="modalCanhBaoMang" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content border-0 shadow-lg" style="border-radius: 16px; overflow: hidden;">
      <div class="modal-header text-white border-0 py-3" style="background: #92400E;">
        <h5 class="modal-title fw-bold d-flex align-items-center gap-2"><i class="bi bi-wifi-off"></i>Cảnh Báo Kết Nối Mạng</h5>
        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Đóng"></button>
      </div>
      <div class="modal-body p-4">
        <p id="noiDungModalCanhBaoMang" class="mb-0 fw-medium" style="color: #1F2937; line-height: 1.6;">Kết nối Internet bị gián đoạn! Quý khách vui lòng kiểm tra lại đường truyền mạng và tiến hành xác thực thanh toán lại.</p>
      </div>
      <div class="modal-footer border-0 bg-light px-4 py-2">
        <button type="button" class="btn btn-dark rounded-pill px-4" data-bs-dismiss="modal">Đã hiểu &amp; Thử lại</button>
      </div>
    </div>
  </div>
</div>`);
        }
    }

    window.hienThiModalHetHang = function (tenBanh, thongBaoTuyChinh) {
        const noiDungEl = document.getElementById("noiDungModalHetHang");
        if (noiDungEl) {
            noiDungEl.textContent = thongBaoTuyChinh
                || `Rất tiếc! Sản phẩm ${tenBanh || "Bánh Trung Thu"} trong giỏ quà của Quý khách vừa hết hàng hoặc không còn đủ số lượng tại kho. Nguyệt Cát chân thành xin lỗi Quý khách vì sự bất tiện này. Vui lòng điều chỉnh lại số lượng hoặc chọn vị bánh khác.`;
        }
        const modalEl = document.getElementById("modalThongBaoHetHang");
        if (modalEl && window.bootstrap) {
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }
    };

    window.hienThiModalCanhBaoMang = function () {
        const modalEl = document.getElementById("modalCanhBaoMang");
        if (modalEl && window.bootstrap) {
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }
    };

    window.datTrangThaiKhoaNutDatHang = function (moKhoa) {
        daXacThucCaptchaHopLe = Boolean(moKhoa);
        const btn = document.getElementById("btnHoanTatDatHang");
        if (btn) {
            btn.disabled = !daXacThucCaptchaHopLe;
        }
    };

    window.taiMaCaptchaMoi = async function (xoaOInput = true) {
        datTrangThaiKhoaNutDatHang(false);
        const inputCaptcha = document.getElementById("txtMaCaptchaNhap");
        const lbl = document.getElementById("lblTrangThaiCaptchaKho");

        if (xoaOInput && inputCaptcha) {
            inputCaptcha.value = "";
            if (lbl) {
                lbl.style.color = "#6B7280";
                lbl.textContent = "Nhập đủ 5 ký tự trong hình để hệ thống tự động xác thực & kiểm tra tồn kho.";
            }
        }

        try {
            const res = await fetch("/ThanhToan/LayMaCaptchaMoi");
            const data = await res.json();
            if (data && data.thanhCong) {
                const img = document.getElementById("imgCaptchaThanhToan");
                if (img) img.src = data.anhCaptchaSvg;
            }
        } catch (e) {
            // Bỏ qua nếu đang tải trang
        }
    };

    window.xuLyGoMaCaptchaTuDong = async function (inputEl) {
        if (!inputEl) return;
        const giaTri = inputEl.value.replace(/[^a-zA-Z0-9]/g, "").toUpperCase();
        if (inputEl.value !== giaTri) {
            inputEl.value = giaTri;
        }

        const lbl = document.getElementById("lblTrangThaiCaptchaKho");
        if (giaTri.length < 5) {
            datTrangThaiKhoaNutDatHang(false);
            if (lbl) {
                lbl.style.color = "#6B7280";
                lbl.textContent = "Nhập đủ 5 ký tự trong hình để hệ thống tự động xác thực & kiểm tra tồn kho.";
            }
            return;
        }

        if (giaTri.length === 5) {
            if (lbl) {
                lbl.style.color = "#B45309";
                lbl.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span>Đang xác thực mã & kiểm tra tồn kho thực tế...`;
            }
            await goiApiXacThucCaptchaVaKho();
        }
    };

    window.goiApiXacThucCaptchaVaKho = async function () {
        const inputEl = document.getElementById("txtMaCaptchaNhap");
        const maCaptcha = inputEl?.value.trim() || "";
        const lbl = document.getElementById("lblTrangThaiCaptchaKho");
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 8000);

        try {
            const res = await fetch("/ThanhToan/XacThucCaptchaVaKiemTraKho", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    maCaptcha: maCaptcha,
                    maCaptchaNhap: maCaptcha
                }),
                signal: controller.signal
            });
            clearTimeout(timeoutId);

            if (!res.ok) {
                throw new Error("Network response was not ok");
            }

            const data = await res.json();
            if (data.thanhCong) {
                datTrangThaiKhoaNutDatHang(true);
                if (lbl) {
                    lbl.style.color = "#059669";
                    lbl.className = "d-block mt-1 fw-semibold";
                    lbl.textContent = "✓ Đã xác thực & kiểm tra còn hàng";
                }
                return true;
            }

            datTrangThaiKhoaNutDatHang(false);

            if (data.loaiLoi === "CAPTCHA") {
                if (lbl) {
                    lbl.style.color = "#DC2626";
                    lbl.className = "d-block mt-1 fw-medium";
                    lbl.textContent = data.thongBao || "Mã xác thực Captcha không chính xác. Vui lòng nhập lại mã hiển thị trong hình!";
                }
                if (data.anhCaptchaSvg) {
                    const img = document.getElementById("imgCaptchaThanhToan");
                    if (img) img.src = data.anhCaptchaSvg;
                    if (inputEl) {
                        inputEl.value = "";
                        inputEl.focus();
                    }
                } else {
                    await taiMaCaptchaMoi(false);
                    if (inputEl) {
                        inputEl.value = "";
                        inputEl.focus();
                    }
                }
                return false;
            }

            if (lbl) {
                lbl.style.color = "#DC2626";
                lbl.className = "d-block mt-1 fw-medium";
                lbl.textContent = `Sản phẩm ${data.tenBanh || "trong giỏ"} vừa hết hàng hoặc không đủ tồn kho!`;
            }
            hienThiModalHetHang(data.tenBanh, data.thongBao);
            return false;
        } catch (err) {
            clearTimeout(timeoutId);
            datTrangThaiKhoaNutDatHang(false);
            if (lbl) {
                lbl.style.color = "#DC2626";
                lbl.className = "d-block mt-1 fw-medium";
                lbl.textContent = "Kết nối Internet bị gián đoạn!";
            }
            hienThiModalCanhBaoMang();
            return false;
        }
    };

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

        capNhatBangTinhThanhToan();
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
        capNhatBangTinhThanhToan();
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

    window.capNhatTrangThaiPhuongThucTt = function () {
        document.querySelectorAll("#nhomPhuongThucThanhToan .dong-phuong-thuc-tt-bitis").forEach(the => {
            const radio = the.querySelector('input[type="radio"]');
            the.classList.toggle("dang-chon", Boolean(radio && radio.checked));
        });
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
        capNhatBangTinhThanhToan();
    };

    window.xoaDotGiaoHang = function (btn) {
        const item = btn.closest(".item-dot-giao");
        if (item) {
            item.remove();
            danhSoLaiCacDotGiao();
            capNhatGiaoDienNguoiKhacNhan();
            capNhatBangTinhThanhToan();
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

    window.chonNhanhMaCheckout = function (maCode) {
        const txt = document.getElementById("txtMaGiamGiaCheckout");
        if (txt) {
            txt.value = maCode;
            apDungMaGiamGiaTrangThanhToan();
        }
    };

    window.apDungMaGiamGiaTrangThanhToan = async function () {
        const txt = document.getElementById("txtMaGiamGiaCheckout");
        const lblKetQua = document.getElementById("lblKetQuaVoucherCheckout");
        const codeKm = txt ? txt.value.trim() : "";

        try {
            const res = await fetch(`/GioHang/ApDungVoucher?codeKm=${encodeURIComponent(codeKm)}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ codeKm: codeKm })
            });

            const data = await res.json();
            if (data.thanhCong) {
                const resGio = await fetch("/GioHang/LayThongTinGioHangJson");
                const dataGio = await resGio.json();
                if (dataGio && dataGio.thanhCong) {
                    TIEN_GIAM_VOUCHER = Math.round(dataGio.tienGiamVoucher || 0);
                    const dongVoucher = document.getElementById("dongGiamGiaVoucherCheckout");
                    const lblTienGiam = document.getElementById("lblTienGiamVoucherCheckout");
                    if (dongVoucher) {
                        dongVoucher.style.setProperty("display", TIEN_GIAM_VOUCHER > 0 ? "flex" : "none", "important");
                    }
                    if (lblTienGiam) {
                        lblTienGiam.innerText = `-${TIEN_GIAM_VOUCHER.toLocaleString("vi-VN")} đ`;
                    }
                    if (lblKetQua) {
                        lblKetQua.className = "d-block mb-2 text-success fw-medium";
                        lblKetQua.innerHTML = dataGio.codeKmDangDung
                            ? `✓ Đang áp dụng mã <strong>${dataGio.codeKmDangDung}</strong>`
                            : "";
                    }
                    capNhatBangTinhThanhToan();
                }
                if (typeof hienThiThongBaoGioHang === "function") {
                    hienThiThongBaoGioHang(data.thongBao, true);
                }
            } else {
                if (lblKetQua) {
                    lblKetQua.className = "d-block mb-2 text-danger fw-medium";
                    lblKetQua.textContent = data.thongBao;
                }
                if (typeof hienThiThongBaoGioHang === "function") {
                    hienThiThongBaoGioHang(data.thongBao, false);
                }
            }
        } catch (err) {
            if (typeof hienThiThongBaoGioHang === "function") {
                hienThiThongBaoGioHang("Lỗi kết nối khi áp dụng mã giảm giá!", false);
            }
        }
    };

    window.capNhatBangTinhThanhToan = function () {
        const chkB2B = document.getElementById("chkDonB2B");
        const khungB2B = document.getElementById("khungThongTinB2B");
        const chkLogo = document.getElementById("chkYeuCauInLogo");

        if (chkB2B && khungB2B) {
            khungB2B.style.display = chkB2B.checked ? "block" : "none";
            if (!chkB2B.checked && chkLogo) {
                chkLogo.checked = false;
            }
        }

        let phiLogo = 0;
        if (chkB2B && chkB2B.checked && chkLogo && chkLogo.checked && !MIEN_PHI_LOGO) {
            phiLogo = PHI_LOGO_MAC_DINH;
        }

        const dongLogo = document.getElementById("dongPhiLogoB2BCheckout");
        if (dongLogo) {
            dongLogo.style.setProperty("display", phiLogo > 0 ? "flex" : "none", "important");
        }
        const lblLogo = document.getElementById("lblPhiInLogoCheckout");
        if (lblLogo) {
            lblLogo.innerText = `${phiLogo.toLocaleString("vi-VN")} đ`;
        }

        const laNhanTaiCuaHang = document.getElementById("radNhanTaiCuaHang")?.checked === true;
        const soDiemPhu = laNhanTaiCuaHang ? 0 : document.querySelectorAll("#danhSachCacDotGiaoContainer .item-dot-giao").length;
        const soDiemGiao = 1 + soDiemPhu;

        const tienHangSauGiam = Math.max(0, TONG_TIEN_HANG - TIEN_CK_B2B - TIEN_GIAM_VOUCHER);
        const duMocFreeship = tienHangSauGiam >= MOC_FREESHIP_TIEU_CHUAN;

        const phiTieuChuan = duMocFreeship ? 0 : (30000 * soDiemGiao);
        const phiHoaToc = (duMocFreeship ? 30000 : 60000) * soDiemGiao;

        const lblGiaShipTieuChuan = document.getElementById("lblGiaShipTieuChuan");
        if (lblGiaShipTieuChuan) {
            lblGiaShipTieuChuan.innerText = "30.000đ";
        }

        const lblGiaShipHoaToc = document.getElementById("lblGiaShipHoaToc");
        if (lblGiaShipHoaToc) {
            lblGiaShipHoaToc.innerText = duMocFreeship ? "30.000đ" : "60.000đ";
        }

        const radHoaToc = document.getElementById("radGiaoHoaToc");
        const laGiaoHoaToc = !laNhanTaiCuaHang && Boolean(radHoaToc && radHoaToc.checked && !radHoaToc.disabled);

        document.getElementById("lblDongGiaoTieuChuan")?.classList.toggle("dang-chon", !laGiaoHoaToc);
        document.getElementById("lblDongGiaoHoaToc")?.classList.toggle("dang-chon", laGiaoHoaToc);

        const hidTocDo = document.getElementById("hidTocDoGiaoHang");
        if (hidTocDo) {
            hidTocDo.value = laNhanTaiCuaHang ? "NHAN_TAI_CUA_HANG" : (laGiaoHoaToc ? "GIAO_HOA_TOC" : "GIAO_TIEU_CHUAN");
        }

        let tongPhiShip = 0;
        if (laNhanTaiCuaHang) {
            tongPhiShip = 0;
        } else if (laGiaoHoaToc) {
            tongPhiShip = phiHoaToc;
        } else {
            tongPhiShip = phiTieuChuan;
        }

        const tongCuoi = Math.max(0, tienHangSauGiam + phiLogo + tongPhiShip);

        const lblSoDiem = document.getElementById("lblSoDiemGiaoCheckout");
        if (lblSoDiem) lblSoDiem.innerText = soDiemGiao;

        const lblPhiShip = document.getElementById("lblPhiShipCheckout");
        if (lblPhiShip) {
            lblPhiShip.innerText = tongPhiShip === 0 ? "0 đ" : `${tongPhiShip.toLocaleString("vi-VN")} đ`;
        }

        const lblTongCotPhai = document.getElementById("lblTongThanhToanCuoiCheckout");
        if (lblTongCotPhai) {
            lblTongCotPhai.innerText = `${tongCuoi.toLocaleString("vi-VN")} đ`;
        }
    };

    window.chuanBiTruocKhiGuiDon = async function (e) {
        if (dangGuiDonHang) return true;
        if (e) e.preventDefault();

        dongBoNguoiMuaSangNguoiNhan();
        dongBoDiaChiGiaoChinh();

        const hoTen = document.getElementById("txtHoTenNguoiMua")?.value.trim() || "";
        const sdt = document.getElementById("txtSdtNguoiMua")?.value.trim() || "";
        const diaChi = document.getElementById("dot0_DiaChi")?.value.trim() || "";
        const maCaptcha = document.getElementById("txtMaCaptchaNhap")?.value.trim() || "";

        if (!hoTen || !sdt) {
            if (typeof hienThiThongBaoGioHang === "function") {
                hienThiThongBaoGioHang("Vui lòng nhập đầy đủ Họ và tên, Số điện thoại!", false);
            }
            document.getElementById("txtHoTenNguoiMua")?.focus();
            return false;
        }

        if (!diaChi) {
            if (typeof hienThiThongBaoGioHang === "function") {
                hienThiThongBaoGioHang("Vui lòng nhập Địa chỉ giao hàng (Số nhà, tên đường) hoặc chọn Cửa hàng nhận!", false);
            }
            document.getElementById("txtDiaChiSoNha")?.focus();
            return false;
        }

        const chkThiep0 = document.getElementById("chkThiepQr_0");
        if (chkThiep0 && chkThiep0.checked) {
            const kinhGui = document.getElementById("txtKinhGuiTrenThiep_0")?.value.trim() || "";
            const nguoiGui = document.getElementById("txtNguoiGuiTrenThiep_0")?.value.trim() || "";
            const loiChuc = document.getElementById("txtLoiChucThiep_0")?.value.trim() || "";

            if (!kinhGui || !nguoiGui || !loiChuc) {
                if (typeof hienThiThongBaoGioHang === "function") {
                    hienThiThongBaoGioHang("Vui lòng nhập đầy đủ Kính gửi, Người gửi và Lời chúc trên thiệp 3D!", false);
                }
                return false;
            }
        }

        if (maCaptcha.length < 5) {
            const lbl = document.getElementById("lblTrangThaiCaptchaKho");
            if (lbl) {
                lbl.style.color = "#DC2626";
                lbl.textContent = "Mã xác thực Captcha không chính xác. Vui lòng nhập lại mã hiển thị trong hình!";
            }
            document.getElementById("txtMaCaptchaNhap")?.focus();
            return false;
        }

        if (!daXacThucCaptchaHopLe) {
            const hopLeKho = await goiApiXacThucCaptchaVaKho();
            if (!hopLeKho) {
                return false;
            }
        }

        dangGuiDonHang = true;
        document.getElementById("formDatHangTrungThu").submit();
        return true;
    };
})();
