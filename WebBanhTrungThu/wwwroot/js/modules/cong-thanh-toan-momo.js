// =========================================================================
// MODULE CỔNG THANH TOÁN MOMO ĐỘC LẬP (ĐẾM NGƯỢC 15 PHÚT, POLLING, COPY, WEBHOOK)
// =========================================================================
(function () {
    const root = document.getElementById("congThanhToanMoMoRoot");
    if (!root) return;

    const MA_TRA_CUU_DON = root.dataset.maDonHang || "";
    const TONG_THANH_TOAN_DON = Number(root.dataset.tongThanhToan || 0);
    const NOI_DUNG_CK_CHUAN = root.dataset.noiDungCk || "";
    const HAN_THANH_TOAN_ISO = root.dataset.hanThanhToanIso || "";
    const URL_KIEM_TRA = root.dataset.urlKiemTra || "/ThanhToan/KiemTraTrangThaiDon";
    const URL_HUY_HET_HAN = root.dataset.urlHuyHetHan || "/ThanhToan/HuyDonHetHanVaHoanKho";
    const URL_WEBHOOK = root.dataset.urlWebhook || "/api/webhook/momo";
    const URL_THANH_CONG = root.dataset.urlThanhCong || `/ThanhToan/ThanhCong/${encodeURIComponent(MA_TRA_CUU_DON)}`;
    const URL_VIET_QR = root.dataset.urlVietqr || "";
    const QR_MOMO_BASE64 = document.getElementById("imgQrCongMoMoChinh")?.getAttribute("src") || "";

    const soGiayAttr = Number(root.dataset.soGiayConLai);
    const soGiayBanDau = !Number.isNaN(soGiayAttr)
        ? Math.max(0, soGiayAttr)
        : Math.max(0, Math.ceil((Date.parse(HAN_THANH_TOAN_ISO) - Date.now()) / 1000));
    const mocKetThucMs = Date.now() + (soGiayBanDau * 1000);

    let boDemGiayQr = null;
    let boPollingQr = null;
    let dangChoThanhToanQr = true;
    let daXuLyHetHan = false;

    function tinhSoGiayConLai() {
        return Math.max(0, Math.ceil((mocKetThucMs - Date.now()) / 1000));
    }

    function chuyenKieuQrCongThanhToan(loai) {
        const img = document.getElementById("imgQrCongMoMoChinh");
        const logoTam = document.getElementById("logoTamQrMoMo");
        const btnMomo = document.getElementById("btnTabQrMoMo");
        const btnVietQr = document.getElementById("btnTabVietQr");
        const lblHd = document.getElementById("lblHuongDanQuetQr");

        if (loai === "VIETQR" && URL_VIET_QR) {
            if (img) img.src = URL_VIET_QR;
            if (logoTam) logoTam.style.display = "none";
            if (btnMomo) btnMomo.classList.remove("active");
            if (btnVietQr) btnVietQr.classList.add("active");
            if (lblHd) {
                lblHd.innerHTML = `<i class="bi bi-qr-code-scan"></i><span>Mở ứng dụng Ngân hàng bất kỳ để quét mã VietQR chuyển khoản nhanh 24/7</span>`;
            }
        } else {
            if (img) img.src = QR_MOMO_BASE64;
            if (logoTam) logoTam.style.display = "flex";
            if (btnMomo) btnMomo.classList.add("active");
            if (btnVietQr) btnVietQr.classList.remove("active");
            if (lblHd) {
                lblHd.innerHTML = `<i class="bi bi-qr-code-scan"></i><span>Mở ứng dụng MoMo → Chọn Quét Mã để chuyển khoản tự động điền sẵn số tiền &amp; nội dung</span>`;
            }
        }
    }

    function danhDauDaChep(btn) {
        if (!btn) return;
        const textCu = btn.dataset.textCu || btn.innerText;
        btn.dataset.textCu = textCu;
        btn.innerText = "Đã chép";
        setTimeout(() => { btn.innerText = textCu; }, 1500);
    }

    function saoChepDuPhong(noiDung, btn) {
        const ta = document.createElement("textarea");
        ta.value = noiDung;
        ta.style.position = "fixed";
        ta.style.left = "-9999px";
        document.body.appendChild(ta);
        ta.select();
        try {
            document.execCommand("copy");
            danhDauDaChep(btn);
        } finally {
            document.body.removeChild(ta);
        }
    }

    function saoChepNoiDungMoMo(noiDung, btn) {
        const giaTri = String(noiDung ?? "");
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(giaTri).then(() => danhDauDaChep(btn)).catch(() => saoChepDuPhong(giaTri, btn));
        } else {
            saoChepDuPhong(giaTri, btn);
        }
    }

    function capNhatHienThiDongHo(tongGiay) {
        const g = Math.max(0, Math.floor(tongGiay));
        const elPhut = document.getElementById("phut-con-lai");
        const elGiay = document.getElementById("giay-con-lai");
        if (elPhut) elPhut.textContent = String(Math.floor(g / 60)).padStart(2, "0");
        if (elGiay) elGiay.textContent = String(g % 60).padStart(2, "0");
    }

    function chuyenSangTrangBienLaiThanhCong() {
        if (!dangChoThanhToanQr) return;
        dangChoThanhToanQr = false;
        if (boDemGiayQr) clearInterval(boDemGiayQr);
        if (boPollingQr) clearInterval(boPollingQr);
        window.location.href = URL_THANH_CONG;
    }

    async function xuLyHetHan15PhutVaHoanKho() {
        if (daXuLyHetHan || !dangChoThanhToanQr) return;
        daXuLyHetHan = true;
        dangChoThanhToanQr = false;

        if (boDemGiayQr) clearInterval(boDemGiayQr);
        if (boPollingQr) clearInterval(boPollingQr);

        try {
            const res = await fetch(`${URL_HUY_HET_HAN}?maTraCuu=${encodeURIComponent(MA_TRA_CUU_DON)}`, {
                method: "POST"
            });
            const data = await res.json();
            if (data && data.daThanhToan) {
                window.location.href = URL_THANH_CONG;
                return;
            }
        } catch (err) {
            console.warn("Lỗi gọi hoàn kho tự động:", err);
        }

        window.location.href = URL_THANH_CONG;
    }

    async function kiemTraTrangThaiDonDinhKy() {
        if (!dangChoThanhToanQr) return;
        try {
            const res = await fetch(`${URL_KIEM_TRA}?maTraCuu=${encodeURIComponent(MA_TRA_CUU_DON)}`);
            const data = await res.json();
            if (!data || !data.thanhCong) return;

            if (data.daThanhToan || data.trangThaiTt === "DA_THANH_TOAN") {
                chuyenSangTrangBienLaiThanhCong();
            } else if (data.daHetHan || data.soGiayConLai <= 0) {
                await xuLyHetHan15PhutVaHoanKho();
            }
        } catch (err) {
            // Bỏ qua lỗi mạng tạm thời trong lúc polling
        }
    }

    async function giaLapKhachQuetQrThanhCong() {
        const btn = document.getElementById("btnTestWebhookMoMo");
        const nhanMacDinh = "⚡ Giả lập Khách đã quét QR thành công (Test Webhook MoMo)";
        if (btn) {
            btn.disabled = true;
            btn.innerText = "Đang xác nhận giao dịch từ Webhook MoMo...";
        }

        try {
            const res = await fetch(URL_WEBHOOK, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    maDonHang: MA_TRA_CUU_DON,
                    content: NOI_DUNG_CK_CHUAN,
                    transferAmount: TONG_THANH_TOAN_DON
                })
            });
            const data = await res.json();
            if (data && data.thanhCong) {
                chuyenSangTrangBienLaiThanhCong();
            } else {
                if (btn) {
                    btn.disabled = false;
                    btn.innerText = nhanMacDinh;
                }
                alert(data?.thongBao || "Không thể xử lý Webhook MoMo.");
            }
        } catch (err) {
            if (btn) {
                btn.disabled = false;
                btn.innerText = nhanMacDinh;
            }
            alert("Lỗi kết nối tới Webhook MoMo!");
        }
    }

    window.chuyenKieuQrCongThanhToan = chuyenKieuQrCongThanhToan;
    window.saoChepNoiDungMoMo = saoChepNoiDungMoMo;
    window.giaLapKhachQuetQrThanhCong = giaLapKhachQuetQrThanhCong;

    document.addEventListener("DOMContentLoaded", function () {
        root.querySelectorAll("[data-copy]").forEach(btn => {
            btn.addEventListener("click", function () {
                saoChepNoiDungMoMo(btn.dataset.copy || "", btn);
            });
        });

        root.querySelectorAll("[data-qr-mode]").forEach(btn => {
            btn.addEventListener("click", function () {
                chuyenKieuQrCongThanhToan(btn.dataset.qrMode || "MOMO");
            });
        });

        document.getElementById("btnTestWebhookMoMo")?.addEventListener("click", giaLapKhachQuetQrThanhCong);

        const giayConLai = tinhSoGiayConLai();
        capNhatHienThiDongHo(giayConLai);
        if (giayConLai <= 0) {
            xuLyHetHan15PhutVaHoanKho();
            return;
        }

        boDemGiayQr = setInterval(() => {
            const conLai = tinhSoGiayConLai();
            capNhatHienThiDongHo(conLai);
            if (conLai <= 0) {
                xuLyHetHan15PhutVaHoanKho();
            }
        }, 1000);

        boPollingQr = setInterval(kiemTraTrangThaiDonDinhKy, 3000);
    });
})();
