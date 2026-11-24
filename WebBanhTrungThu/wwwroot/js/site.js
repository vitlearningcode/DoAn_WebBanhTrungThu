// =========================================================================
// SITE.JS: NHÂN KHỞI TẠO HỆ THỐNG, FALLBACK ẢNH & THÔNG BÁO TOAST DÙNG CHUNG
// Các tính năng chuyên biệt đã được phân rã vào wwwroot/js/modules/:
// - cuon-fullpage.js
// - studio-phoi-hop.js
// - gio-hang-ajax.js
// - tu-van-va-tien-ich.js
// =========================================================================

const INLINE_FALLBACK_SVG = "data:image/svg+xml;utf8," + encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="120" height="120" viewBox="0 0 120 120"><rect width="120" height="120" rx="12" fill="#FDF8EE"/><circle cx="60" cy="54" r="24" fill="#C89B3C" opacity="0.28"/><text x="60" y="96" font-family="sans-serif" font-size="11" fill="#6E5041" text-anchor="middle">Bánh Trung Thu</text></svg>');

window.addEventListener("error", function (e) {
    if (e.target && e.target.tagName === "IMG") {
        const img = e.target;
        if (img.id === "imgCaptchaThanhToan" || img.id === "imgCaptchaDangKy") return;
        if (img.dataset.fallbackApplied) {
            if (!img.src.startsWith("data:image/svg+xml")) {
                img.src = INLINE_FALLBACK_SVG;
            }
            return;
        }
        img.dataset.fallbackApplied = "true";
        img.src = "/images/default/banh-mac-dinh.png";
    }
}, true);

function hienThiThongBaoGioHang(noiDung, thanhCong = true) {
    let container = document.getElementById("khungThongBaoToast");
    if (!container) {
        container = document.createElement("div");
        container.id = "khungThongBaoToast";
        container.className = "toast-container position-fixed bottom-0 start-0 p-0";
        container.style.zIndex = "1090";
        container.style.bottom = "20px";
        container.style.left = "20px";
        document.body.appendChild(container);
    }

    const toastEl = document.createElement("div");
    toastEl.className = "toast align-items-center show mb-2";
    toastEl.setAttribute("role", "alert");
    toastEl.style.background = thanhCong ? "linear-gradient(135deg, #6E0D10 0%, #4A0709 100%)" : "#FFFFFF";
    toastEl.style.color = thanhCong ? "#FAF0CA" : "#2C1810";
    toastEl.style.border = thanhCong ? "1.5px solid #D4AF37" : "1.5px solid #DC3545";
    toastEl.style.boxShadow = "0 10px 28px rgba(74, 7, 9, 0.28)";
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body fw-medium">
                <i class="bi ${thanhCong ? "bi-check-circle-fill chu-vang-kim" : "bi-exclamation-triangle-fill text-danger"} me-2"></i>
                <span>${noiDung}</span>
            </div>
            <button type="button" class="btn-close ${thanhCong ? "btn-close-white" : ""} me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    container.appendChild(toastEl);
    setTimeout(() => {
        toastEl.classList.remove("show");
        setTimeout(() => toastEl.remove(), 300);
    }, 3500);
}

document.addEventListener("DOMContentLoaded", function () {
    if (typeof chuanHoaKhungVoucherOffcanvas === "function") chuanHoaKhungVoucherOffcanvas();
    if (typeof renderKhayHop === "function") renderKhayHop();
    if (typeof khoiChayDongHoDemNguoc === "function") khoiChayDongHoDemNguoc();
    if (typeof taiLaiGioHangOffcanvas === "function") taiLaiGioHangOffcanvas();
    if (typeof khoiTaoSectionScrollObserver === "function") khoiTaoSectionScrollObserver();
    if (typeof khoiTaoBoMayChuyenCanhRoiRac === "function") khoiTaoBoMayChuyenCanhRoiRac();
    if (typeof khoiTaoLienKetNeoMuot === "function") khoiTaoLienKetNeoMuot();
    if (typeof khoiTaoTuDongThuGonNutTuVan === "function") khoiTaoTuDongThuGonNutTuVan();
    if (typeof xuLyTuDongXepBanhTuUrl === "function") xuLyTuDongXepBanhTuUrl();
    if (typeof khoiTaoLichSuTraCuuDonNhanh === "function") khoiTaoLichSuTraCuuDonNhanh();
    if (typeof khoiTaoSuKienKiemTraKhoOffcanvas === "function") khoiTaoSuKienKiemTraKhoOffcanvas();
});
