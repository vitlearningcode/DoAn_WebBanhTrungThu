// =========================================================================
// XỬ LÝ TƯƠNG TÁC TRANG THANH TOÁN CHUẨN BITI'S (CAPTCHA, TỒN KHO, VOUCHER & TỔNG KẾT GIÁ)
// =========================================================================
(function () {
    const cfg = window.CAU_HINH_THANH_TOAN || {};
    const TONG_TIEN_HANG = Number(cfg.tongTienHang || 0);
    const TIEN_CK_B2B = Number(cfg.tienCkB2B || 0);
    let TIEN_GIAM_VOUCHER = Number(cfg.tienGiamVoucher || 0);
    const PHAN_TRAM_GIAM_VIP = Number(cfg.phanTramGiamVip || 0);
    const DIEM_TICH_LUY = Math.max(0, Math.floor(Number(cfg.diemTichLuy || 0)));
    const MIEN_PHI_LOGO = Boolean(cfg.mienPhiLogo);
    const PHI_LOGO_MAC_DINH = Number(cfg.phiLogoMacDinh || 500000);
    const MOC_FREESHIP_TIEU_CHUAN = 500000;

    let daXacThucCaptchaHopLe = false;
    let dangGuiDonHang = false;

    document.addEventListener("DOMContentLoaded", function () {
        damBaoModalCanhBaoTonTai();
        const urlParams = new URLSearchParams(window.location.search);
        const laCheDoB2B = urlParams.get("cheDoB2B") === "true" || Number(cfg.tongSoHop || 0) >= 10;
        const chkB2B = document.getElementById("chkDonB2B");
        if (chkB2B && laCheDoB2B) {
            chkB2B.checked = true;
        }
        if (typeof window.capNhatGiaoDienNguoiKhacNhan === "function") window.capNhatGiaoDienNguoiKhacNhan();
        if (typeof window.dongBoNguoiMuaSangNguoiNhan === "function") window.dongBoNguoiMuaSangNguoiNhan();
        if (typeof window.dongBoDiaChiGiaoChinh === "function") window.dongBoDiaChiGiaoChinh();
        if (typeof window.kiemTraTinhThanhVaCapNhatHoaToc === "function") window.kiemTraTinhThanhVaCapNhatHoaToc();
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

    window.capNhatTrangThaiPhuongThucTt = function () {
        document.querySelectorAll("#nhomPhuongThucThanhToan .dong-phuong-thuc-tt-bitis").forEach(the => {
            const radio = the.querySelector('input[type="radio"]');
            the.classList.toggle("dang-chon", Boolean(radio && radio.checked));
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
        const soDiemGiao = 1;

        const tienHangSauCkVaVoucher = Math.max(0, TONG_TIEN_HANG - TIEN_CK_B2B - TIEN_GIAM_VOUCHER);
        const tienGiamVip = PHAN_TRAM_GIAM_VIP > 0
            ? Math.round(tienHangSauCkVaVoucher * (PHAN_TRAM_GIAM_VIP / 100))
            : 0;

        const dongVip = document.getElementById("dongGiamGiaHangVipCheckout");
        const lblVip = document.getElementById("lblTienGiamHangVipCheckout");
        if (dongVip) {
            dongVip.style.setProperty("display", tienGiamVip > 0 ? "flex" : "none", "important");
        }
        if (lblVip) {
            lblVip.innerText = `-${tienGiamVip.toLocaleString("vi-VN")} đ`;
        }

        const tienHangSauVip = Math.max(0, tienHangSauCkVaVoucher - tienGiamVip);
        const diemToiDaTheoDon = Math.floor((tienHangSauVip * 0.5) / 1000);
        const diemKhaDung = Math.min(DIEM_TICH_LUY, diemToiDaTheoDon);

        const chkDungDiem = document.getElementById("chkDungDiemTichLuy");
        const dangBatDungDiem = Boolean(chkDungDiem && chkDungDiem.checked);
        const diemDaDung = dangBatDungDiem ? diemKhaDung : 0;
        const tienGiamDiem = diemDaDung * 1000;

        const lblGhiChuDiem = document.getElementById("lblGhiChuDiemTichLuyCheckout");
        if (lblGhiChuDiem) {
            if (dangBatDungDiem && diemKhaDung === 0) {
                lblGhiChuDiem.style.setProperty("display", "block", "important");
                lblGhiChuDiem.textContent = "Đơn hàng cần tối thiểu 2.000 đ sau ưu đãi để áp dụng tiêu điểm (tối đa 50% giá trị).";
            } else if (dangBatDungDiem && diemKhaDung < DIEM_TICH_LUY) {
                lblGhiChuDiem.style.setProperty("display", "block", "important");
                lblGhiChuDiem.textContent = `Áp dụng tối đa 50% tiền hàng sau giảm: dùng ${diemKhaDung.toLocaleString("vi-VN")} điểm (-${tienGiamDiem.toLocaleString("vi-VN")} đ).`;
            } else {
                lblGhiChuDiem.style.setProperty("display", "none", "important");
            }
        }

        const dongDiem = document.getElementById("dongGiamGiaDiemCheckout");
        const lblSoDiem = document.getElementById("lblSoDiemSuDungCheckout");
        const lblTienGiamDiem = document.getElementById("lblTienGiamDiemCheckout");
        if (dongDiem) {
            dongDiem.style.setProperty("display", tienGiamDiem > 0 ? "flex" : "none", "important");
        }
        if (lblSoDiem) {
            lblSoDiem.innerText = diemDaDung.toLocaleString("vi-VN");
        }
        if (lblTienGiamDiem) {
            lblTienGiamDiem.innerText = `-${tienGiamDiem.toLocaleString("vi-VN")} đ`;
        }

        const tienHangSauGiam = Math.max(0, tienHangSauVip - tienGiamDiem);
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

        if (typeof window.dongBoNguoiMuaSangNguoiNhan === "function") window.dongBoNguoiMuaSangNguoiNhan();
        if (typeof window.dongBoDiaChiGiaoChinh === "function") window.dongBoDiaChiGiaoChinh();

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

        const chkNguoiKhac = document.getElementById("chkGoiNguoiKhacNhan");
        if (chkNguoiKhac && chkNguoiKhac.checked) {
            const tenNhanThay = document.getElementById("dot0_TenNguoiNhan")?.value.trim() || "";
            const sdtNhanThay = document.getElementById("dot0_SdtNguoiNhan")?.value.trim() || "";
            if (!tenNhanThay || !sdtNhanThay) {
                if (typeof hienThiThongBaoGioHang === "function") {
                    hienThiThongBaoGioHang("Vui lòng nhập đầy đủ Họ tên và Số điện thoại người nhận thay!", false);
                }
                if (!tenNhanThay) document.getElementById("dot0_TenNguoiNhan")?.focus();
                else document.getElementById("dot0_SdtNguoiNhan")?.focus();
                return false;
            }
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

        const chkB2B = document.getElementById("chkDonB2B");
        const txtTenCongTy = document.getElementById("txtTenCongTyB2B");
        const txtMaSoThue = document.getElementById("txtMaSoThueB2B");
        const txtEmailVat = document.getElementById("txtEmailNhanVatB2B");
        const chkEpKimLogo = document.getElementById("chkYeuCauInLogo");
        const txtNoiDungLogo = document.getElementById("txtNoiDungEpKimLogo");

        if (chkB2B && chkB2B.checked) {
            const tenCongTyGoc = (txtTenCongTy?.value || "").split(" | VAT:")[0].split(" | Ép kim Logo:")[0].trim();
            const maSoThueVal = txtMaSoThue?.value.trim() || "";
            if (!tenCongTyGoc || !maSoThueVal) {
                if (typeof hienThiThongBaoGioHang === "function") {
                    hienThiThongBaoGioHang("Vui lòng nhập đầy đủ Tên doanh nghiệp / Cơ quan và Mã số thuế để xuất hóa đơn VAT!", false);
                }
                if (!tenCongTyGoc) txtTenCongTy?.focus();
                else txtMaSoThue?.focus();
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

        if (chkB2B && chkB2B.checked && txtTenCongTy) {
            const tenCongTyGoc = txtTenCongTy.value.split(" | VAT:")[0].split(" | Ép kim Logo:")[0].trim();
            const emailVatVal = txtEmailVat?.value.trim() || "";
            const noiDungLogoVal = txtNoiDungLogo?.value.trim() || "";
            const coEpKim = Boolean(chkEpKimLogo && chkEpKimLogo.checked) || noiDungLogoVal.length > 0;

            const txtEmailNguoiMua = document.getElementById("txtEmailNguoiMua");
            if (txtEmailNguoiMua && !txtEmailNguoiMua.value.trim() && emailVatVal) {
                txtEmailNguoiMua.value = emailVatVal;
            }

            let chuoiLuuB2B = tenCongTyGoc;
            if (emailVatVal) {
                chuoiLuuB2B += ` | VAT: ${emailVatVal}`;
            }
            if (coEpKim) {
                chuoiLuuB2B += ` | Ép kim Logo: ${noiDungLogoVal || "Có yêu cầu ép kim Logo doanh nghiệp lên vỏ hộp"}`;
            }
            txtTenCongTy.value = chuoiLuuB2B.slice(0, 248);
        }

        dangGuiDonHang = true;
        document.getElementById("formDatHangTrungThu").submit();
        return true;
    };
})();
