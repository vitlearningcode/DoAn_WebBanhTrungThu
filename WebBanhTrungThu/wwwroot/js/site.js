// =========================================================================
// 1. BIẾN TRẠNG THÁI CHO KHUNG TỰ PHỐI LỄ HỘP QUÀ TẶNG (TẦNG 2)
// =========================================================================
let cauHinhHop = {
    maVH: 1,
    tenVoHop: "Hộp Tứ Quý Đoàn Viên (4 Bánh)",
    sucChua: 4,
    giaVo: 85000,
    minVipRequired: 0,
    kieuLuoi: "grid-2x2",
    cacO: [null, null, null, null] // Chứa object bánh tại từng vị trí ô
};

// Xử lý toàn cục khi ảnh bị lỗi tải -> chặn đứng vòng lặp vô tận bằng dataset.fallbackApplied và SVG nội tuyến
const INLINE_FALLBACK_SVG = "data:image/svg+xml;utf8," + encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="120" height="120" viewBox="0 0 120 120"><rect width="120" height="120" rx="12" fill="#FDF8EE"/><circle cx="60" cy="54" r="24" fill="#C89B3C" opacity="0.28"/><text x="60" y="96" font-family="sans-serif" font-size="11" fill="#6E5041" text-anchor="middle">Bánh Trung Thu</text></svg>');

window.addEventListener('error', function (e) {
    if (e.target && e.target.tagName === 'IMG') {
        const img = e.target;
        if (img.dataset.fallbackApplied) {
            if (!img.src.startsWith('data:image/svg+xml')) {
                img.src = INLINE_FALLBACK_SVG;
            }
            return;
        }
        img.dataset.fallbackApplied = "true";
        img.src = '/images/default/banh-mac-dinh.png';
    }
}, true);

document.addEventListener("DOMContentLoaded", function () {
    chuanHoaKhungVoucherOffcanvas();
    renderKhayHop();
    khoiChayDongHoDemNguoc();
    taiLaiGioHangOffcanvas();
    khoiTaoSectionScrollObserver();
    khoiTaoBoMayChuyenCanhRoiRac();
    khoiTaoLienKetNeoMuot();
    khoiTaoTuDongThuGonNutTuVan();
    xuLyTuDongXepBanhTuUrl();
});

// =========================================================================
// 1A. BỘ ĐIỀU KHIỂN FULLPAGE SLIDE DECK GPU (TRANSFORM: TRANSLATE3D - SAO.AGU.EDU.VN)
// =========================================================================
const DANH_SACH_PHAN_KHU = [
    "#tang-hero",
    "#tang-tu-chon-hop",
    "#tang-bo-suu-tap",
    "#tang-b2b",
    "#tang-thiep-qr"
];

let danhSachCacTang = [];
let chiSoTangHienTai = 0;
let dangChuyenCanh = false;
let khoaQuanTinhSauChuyen = false;
let boHenGioDuPhongChuyenCanh = null;

// Kiểm tra trang hiện tại có đang ở chế độ Fullpage Slide Deck trên Desktop (>= 992px) không
function laCheDoFullpageDesktop() {
    return window.innerWidth >= 992 && document.getElementById("dai-truot-fullpage") !== null;
}

// Lấy chiều cao thực tế của thanh menu cố định phía trên
function layChieuCaoThanhMenu() {
    return document.querySelector('header, .navbar')?.offsetHeight || 68;
}

// Cập nhật trạng thái sáng chấm tròn dọc bên phải (.side-dot-btn.active)
function capNhatChamTronActive(selector) {
    document.querySelectorAll(".side-dot-btn").forEach(btn => {
        btn.classList.toggle("active", btn.getAttribute("data-target") === selector);
    });
}

// Đồng bộ class body.trang-chu-fullpage và vị trí translate3d khi tải trang hoặc đổi kích thước màn hình
function dongBoKhungFullpageTheoManHinh() {
    const daiTruot = document.getElementById("dai-truot-fullpage");
    if (!daiTruot) return;

    const footerNgoai = document.querySelector("body > footer");
    if (footerNgoai) {
        footerNgoai.style.display = "none";
    }

    if (window.innerWidth >= 992) {
        document.body.classList.add("trang-chu-fullpage");
        window.scrollTo(0, 0);
        daiTruot.style.transform = "translate3d(0, -" + (chiSoTangHienTai * 100) + "%, 0)";
    } else {
        document.body.classList.remove("trang-chu-fullpage");
        daiTruot.style.transform = "none";
    }
}

// Khởi tạo danh sách 5 Slide và bộ điều khiển chuyển cảnh GPU
function khoiTaoBoMayChuyenCanhRoiRac() {
    const daiTruot = document.getElementById("dai-truot-fullpage");
    danhSachCacTang = DANH_SACH_PHAN_KHU
        .map(sel => document.querySelector(sel))
        .filter(el => el !== null);

    if (danhSachCacTang.length === 0) return;

    dongBoKhungFullpageTheoManHinh();

    // Bật sẵn hiệu ứng .section-reveal.active cho tầng đầu tiên
    const tangDau = danhSachCacTang[chiSoTangHienTai];
    if (tangDau) {
        capNhatChamTronActive("#" + tangDau.id);
        tangDau.querySelector(".section-reveal")?.classList.add("active");
    }

    // Nếu URL có hash (#tang-...) khi vừa mở trang chủ từ trang khác sang
    if (window.location.hash) {
        const mucTieuHash = document.querySelector(window.location.hash);
        const idxHash = danhSachCacTang.indexOf(mucTieuHash);
        if (idxHash > 0) {
            setTimeout(() => {
                window.scrollTo(0, 0);
                chuyenDenTang(idxHash);
            }, 60);
        }
    }

    if (daiTruot) {
        daiTruot.addEventListener("transitionend", function (e) {
            if (e.target !== daiTruot || e.propertyName !== "transform") return;
            hoanTatChuyenCanhGPU();
        });
    }

    window.addEventListener("wheel", xuLyLanChuotChuyenCanh, { passive: false });
    window.addEventListener("keydown", xuLyPhimMuiTenChuyenCanh);
    window.addEventListener("resize", dongBoKhungFullpageTheoManHinh);
}

// Mở khóa sau khi #dai-truot-fullpage đã trượt xong và bật khóa quán tính ngắn 70ms
function hoanTatChuyenCanhGPU() {
    if (!dangChuyenCanh) return;
    if (boHenGioDuPhongChuyenCanh) {
        clearTimeout(boHenGioDuPhongChuyenCanh);
        boHenGioDuPhongChuyenCanh = null;
    }

    dangChuyenCanh = false;
    khoaQuanTinhSauChuyen = true;
    setTimeout(() => {
        khoaQuanTinhSauChuyen = false;
    }, 70);
}

// Hàm chuyển đến tầng chỉ định bằng GPU transform: translate3d
function chuyenDenTang(chiSoMoi) {
    const tongSoTang = danhSachCacTang.length;
    if (tongSoTang === 0) return;

    const chiSoHopLe = Math.max(0, Math.min(chiSoMoi, tongSoTang - 1));
    if (dangChuyenCanh === true || chiSoHopLe === chiSoTangHienTai) return;

    const phanTuDich = danhSachCacTang[chiSoHopLe];
    if (!phanTuDich) return;

    // Trên Mobile (< 992px): cuộn mượt truyền thống
    if (!laCheDoFullpageDesktop()) {
        chiSoTangHienTai = chiSoHopLe;
        capNhatChamTronActive("#" + phanTuDich.id);
        phanTuDich.querySelector(".section-reveal")?.classList.add("active");
        const chieuCaoThanhMenu = layChieuCaoThanhMenu();
        window.scrollTo({
            top: Math.max(0, phanTuDich.offsetTop - chieuCaoThanhMenu),
            behavior: "smooth"
        });
        return;
    }

    dangChuyenCanh = true;
    chiSoTangHienTai = chiSoHopLe;

    // Dịch chuyển #dai-truot-fullpage bằng GPU
    const daiTruot = document.getElementById("dai-truot-fullpage");
    if (daiTruot) {
        window.scrollTo(0, 0);
        daiTruot.style.transform = "translate3d(0, -" + (chiSoTangHienTai * 100) + "%, 0)";
    }

    // Cập nhật ngay chấm tròn .active bên phải màn hình và hiệu ứng .section-reveal.active
    if (phanTuDich.id) {
        capNhatChamTronActive("#" + phanTuDich.id);
    }
    phanTuDich.querySelector(".section-reveal")?.classList.add("active");

    // Bộ hẹn giờ dự phòng 880ms phòng trường hợp trình duyệt bỏ qua sự kiện transitionend
    if (boHenGioDuPhongChuyenCanh) {
        clearTimeout(boHenGioDuPhongChuyenCanh);
    }
    boHenGioDuPhongChuyenCanh = setTimeout(() => {
        hoanTatChuyenCanhGPU();
    }, 880);
}

// Kiểm tra con trỏ chuột có đang nằm trong khung danh sách chọn vị bánh nhỏ ở Cột 3 Tầng 2 (.overflow-auto) và chưa chạm đỉnh/đáy không
function conKhoangCuonTrongKhungCon(phanTuMucTieu, deltaY) {
    let el = phanTuMucTieu instanceof Element ? phanTuMucTieu.closest(".overflow-auto, .luoi-san-pham-cuon") : null;
    while (el) {
        if (el.scrollHeight > el.clientHeight + 1) {
            if (deltaY < 0 && el.scrollTop > 0) {
                return true;
            }
            if (deltaY > 0 && el.scrollTop + el.clientHeight < el.scrollHeight - 2) {
                return true;
            }
        }
        el = el.parentElement ? el.parentElement.closest(".overflow-auto, .luoi-san-pham-cuon") : null;
    }
    return false;
}

// Sự kiện wheel ({ passive: false }) điều khiển chuyển cảnh rời rạc từng Slide
function xuLyLanChuotChuyenCanh(e) {
    if (!laCheDoFullpageDesktop() || danhSachCacTang.length === 0) return;

    if (e.ctrlKey || document.querySelector(".modal.show, .offcanvas.show")) return;

    if (conKhoangCuonTrongKhungCon(e.target, e.deltaY)) return;

    e.preventDefault();

    if (dangChuyenCanh === true || khoaQuanTinhSauChuyen === true || Math.abs(e.deltaY) < 4) return;

    if (e.deltaY > 0) {
        chuyenDenTang(chiSoTangHienTai + 1);
    } else if (e.deltaY < 0) {
        chuyenDenTang(chiSoTangHienTai - 1);
    }
}

// Hỗ trợ phím mũi tên và PageUp / PageDown để chuyển tầng mượt mà
function xuLyPhimMuiTenChuyenCanh(e) {
    if (!laCheDoFullpageDesktop() || danhSachCacTang.length === 0) return;
    if (document.querySelector(".modal.show, .offcanvas.show")) return;

    const phanTuDangFocus = document.activeElement;
    if (phanTuDangFocus && ["INPUT", "TEXTAREA", "SELECT"].includes(phanTuDangFocus.tagName)) return;

    if (e.key === "ArrowDown" || e.key === "PageDown") {
        e.preventDefault();
        if (!dangChuyenCanh && !khoaQuanTinhSauChuyen) {
            chuyenDenTang(chiSoTangHienTai + 1);
        }
    } else if (e.key === "ArrowUp" || e.key === "PageUp") {
        e.preventDefault();
        if (!dangChuyenCanh && !khoaQuanTinhSauChuyen) {
            chuyenDenTang(chiSoTangHienTai - 1);
        }
    }
}

// Tìm chỉ số tầng theo phần tử và gọi trực tiếp chuyenDenTang(chiSoTuongUng)
function diChuyenDenPhanTuTang(mucTieu) {
    if (!mucTieu) return;
    const chiSo = danhSachCacTang.indexOf(mucTieu);
    if (chiSo !== -1) {
        chuyenDenTang(chiSo);
    } else if (!laCheDoFullpageDesktop()) {
        const chieuCaoThanhMenu = layChieuCaoThanhMenu();
        const viTriDichY = Math.max(0, mucTieu.offsetTop - chieuCaoThanhMenu);
        window.scrollTo({ top: viTriDichY, behavior: "smooth" });
    }
}

// Hàm kích hoạt điều hướng phân khu từ các nút chấm tròn bên phải hoặc nút bấm cuộn
function cuonDenPhanKhu(selector, e) {
    const mucTieu = document.querySelector(selector);
    if (!mucTieu) {
        return true; // Cho phép điều hướng href mặc định nếu đang ở trang con khác
    }
    if (e && typeof e.preventDefault === "function") {
        e.preventDefault();
    }

    const chiSoTuongUng = danhSachCacTang.indexOf(mucTieu);
    if (chiSoTuongUng !== -1) {
        chuyenDenTang(chiSoTuongUng);
    } else {
        diChuyenDenPhanTuTang(mucTieu);
    }
    return false;
}

// Lấy danh sách các phần tử tầng trang chủ hiện có trong DOM
function layDanhSachSectionTrangChu() {
    return DANH_SACH_PHAN_KHU
        .map(id => document.querySelector(id))
        .filter(el => el !== null);
}

// Khởi tạo IntersectionObserver hỗ trợ hiển thị .section-reveal trên thiết bị di động (< 992px)
function khoiTaoSectionScrollObserver() {
    const cacSection = layDanhSachSectionTrangChu();
    if (cacSection.length === 0 || !("IntersectionObserver" in window)) {
        cacSection.forEach(sec => sec.querySelector(".section-reveal")?.classList.add("active"));
        return;
    }

    const boQuanSatHienDan = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.querySelector(".section-reveal")?.classList.add("active");
                if (!laCheDoFullpageDesktop() && !dangChuyenCanh) {
                    capNhatChamTronActive("#" + entry.target.id);
                    const idx = danhSachCacTang.indexOf(entry.target);
                    if (idx !== -1) {
                        chiSoTangHienTai = idx;
                    }
                }
            }
        });
    }, {
        root: null,
        rootMargin: "0px 0px -10% 0px",
        threshold: 0.15
    });

    cacSection.forEach(sec => {
        boQuanSatHienDan.observe(sec);
    });
}

// Bắt sự kiện click vào tất cả các liên kết neo #tang-... trên Menu Bar và trang chủ để gọi chuyenDenTang(chiSoTuongUng)
function khoiTaoLienKetNeoMuot() {
    document.addEventListener("click", function (e) {
        if (e.defaultPrevented || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey) return;

        const lienKet = e.target instanceof Element ? e.target.closest('a[href*="#tang-"]') : null;
        if (!lienKet) return;

        const duongDan = new URL(lienKet.href, window.location.href);
        if (duongDan.pathname !== window.location.pathname) return;

        const mucTieu = document.getElementById(duongDan.hash.slice(1));
        if (!mucTieu) return;

        e.preventDefault();
        const chiSoTuongUng = danhSachCacTang.indexOf(mucTieu);
        if (chiSoTuongUng !== -1) {
            chuyenDenTang(chiSoTuongUng);
        } else {
            diChuyenDenPhanTuTang(mucTieu);
        }
    });
}

function dieuHuongMenuThuongHieu(tenThuongHieu, e) {
    const tangBst = document.getElementById("tang-bo-suu-tap");
    if (!tangBst) return true;
    if (e && typeof e.preventDefault === "function") e.preventDefault();

    // Đặt lại bộ lọc hương vị về Tất cả để hiển thị đầy đủ bánh của thương hiệu
    const btnAllLoc = document.querySelector('#nhomLocDinhDuong button[data-loc="ALL"]');
    locSanPham("ALL", btnAllLoc);

    // Tìm nút thương hiệu tương ứng
    let btnTh = null;
    document.querySelectorAll("#nhomLocThuongHieu .btn-thuong-hieu").forEach(b => {
        if ((b.getAttribute("data-th") || "").toLowerCase() === tenThuongHieu.toLowerCase()) {
            btnTh = b;
        }
    });
    locTheoThuongHieu(tenThuongHieu, btnTh);

    const chiSoTuongUng = danhSachCacTang.indexOf(tangBst);
    if (chiSoTuongUng !== -1) {
        chuyenDenTang(chiSoTuongUng);
    }
    return false;
}

function dieuHuongMenuBoSuuTap(loaiLoc, e) {
    const tangBst = document.getElementById("tang-bo-suu-tap");
    if (!tangBst) return true;
    if (e && typeof e.preventDefault === "function") e.preventDefault();

    // Đặt lại thương hiệu về Tất cả
    const btnAllTh = document.querySelector('#nhomLocThuongHieu button[data-th="ALL"]');
    locTheoThuongHieu("ALL", btnAllTh);

    const btnLoc = document.querySelector(`#nhomLocDinhDuong button[data-loc="${loaiLoc}"]`);
    locSanPham(loaiLoc, btnLoc);

    const chiSoTuongUng = danhSachCacTang.indexOf(tangBst);
    if (chiSoTuongUng !== -1) {
        chuyenDenTang(chiSoTuongUng);
    }
    return false;
}

function dieuHuongMenuVoHop(maVh, e) {
    const tangHop = document.getElementById("tang-tu-chon-hop");
    if (!tangHop) return true;
    if (e && typeof e.preventDefault === "function") e.preventDefault();

    const radioVh = document.getElementById(`vh_${maVh}`);
    if (radioVh) {
        radioVh.checked = true;
        doiMauVoHop(radioVh);
    }

    const chiSoTuongUng = danhSachCacTang.indexOf(tangHop);
    if (chiSoTuongUng !== -1) {
        chuyenDenTang(chiSoTuongUng);
    }
    return false;
}

// Hàm trượt ngang Carousel Bộ Sưu Tập Bánh ở Tầng 3 khi bấm nút [‹] hoặc [›]
function truotBstBanh(huong) {
    const luoi = document.getElementById("luoiSanPhamBanh");
    if (!luoi) return;
    const theDauTien = luoi.querySelector(".item-banh-card:not(.d-none)");
    const buocTruot = theDauTien ? (theDauTien.offsetWidth + 16) * 2 : luoi.clientWidth * 0.75;
    luoi.scrollBy({
        left: huong * buocTruot,
        behavior: "smooth"
    });
}

function moModalTuVanQua() {
    const modalEl = document.getElementById("modalTroLyChonBanh");
    if (modalEl && typeof bootstrap !== "undefined") {
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    }
}

// Tự động thu gọn nút nổi Tư Vấn Chọn Hộp Quà thành hình tròn 52x52px sau 10 giây
function khoiTaoTuDongThuGonNutTuVan() {
    const nutTuVan = document.getElementById("nutNoiTuVanQua") || document.querySelector(".floating-quiz-btn");
    if (!nutTuVan) return;
    setTimeout(() => {
        nutTuVan.classList.add("thu-gon-nut-tu-van");
    }, 10000);
}

// =========================================================================
// 1B. TƯƠNG TÁC KHAY HỘP TỰ PHỐI
// =========================================================================
function doiMauVoHop(radio) {
    cauHinhHop.maVH = parseInt(radio.value);
    cauHinhHop.tenVoHop = radio.dataset.ten;
    cauHinhHop.sucChua = parseInt(radio.dataset.succhua);
    cauHinhHop.giaVo = parseFloat(radio.dataset.giavo);
    cauHinhHop.minVipRequired = parseInt(radio.dataset.minvip);
    cauHinhHop.kieuLuoi = radio.dataset.luoi;

    const luoi = document.getElementById("luoiOBanh");
    if (luoi) {
        luoi.className = cauHinhHop.kieuLuoi === "grid-3x2" ? "luoi-hop-3x2 w-100 h-100" : "luoi-hop-2x2 w-100 h-100";
    }

    // Điều chỉnh mảng các ô theo sức chứa mới
    let mangMoi = new Array(cauHinhHop.sucChua).fill(null);
    for (let i = 0; i < Math.min(cauHinhHop.cacO.length, cauHinhHop.sucChua); i++) {
        mangMoi[i] = cauHinhHop.cacO[i];
    }
    cauHinhHop.cacO = mangMoi;
    renderKhayHop();
}

function datBanhVaoOTrong(maBanh, tenBanh, donGia, calo, laVip, anhSlot, diUng, thuongHieu = "") {
    const viTriTrong = cauHinhHop.cacO.findIndex(o => o === null);
    if (viTriTrong === -1) {
        hienThiThongBaoGioHang("Khay lễ hộp đã đầy! Bạn có thể chạm vào 1 ô trên khay để đổi vị bánh hoặc chọn mẫu hộp 6 bánh.", false);
        return;
    }

    cauHinhHop.cacO[viTriTrong] = { maBanh, tenBanh, donGia, calo, laVip, anhSlot, diUng, thuongHieu };
    renderKhayHop();
}

function xoaBanhKhoiO(index) {
    cauHinhHop.cacO[index] = null;
    renderKhayHop();
}

function lamTrongKhayHop() {
    cauHinhHop.cacO = new Array(cauHinhHop.sucChua).fill(null);
    renderKhayHop();
}

function renderKhayHop() {
    const luoi = document.getElementById("luoiOBanh");
    if (!luoi) return;
    luoi.innerHTML = "";

    let soBanhDaXep = 0;
    let tongCalo = 0;
    let soBanhVip = 0;
    let tongTienBanh = 0;
    let tapHopDiUng = new Set();
    let maBanhArr = [];

    cauHinhHop.cacO.forEach((banh, idx) => {
        const div = document.createElement("div");
        div.className = banh ? "o-banh-slot da-co-banh" : "o-banh-slot";

        if (banh) {
            soBanhDaXep++;
            tongCalo += banh.calo;
            tongTienBanh += banh.donGia;
            if (banh.laVip) soBanhVip++;
            if (banh.diUng && banh.diUng !== "Không") tapHopDiUng.add(banh.diUng);
            maBanhArr.push("B" + banh.maBanh);

            const nhanPhu = (banh.thuongHieu ? banh.thuongHieu + " • " : "") + banh.donGia.toLocaleString("vi-VN") + "đ";
            div.onclick = () => xoaBanhKhoiO(idx);
            div.title = "Chạm để gỡ hoặc đổi vị bánh khác cho ô này";
            div.innerHTML = `
                <button type="button" class="btn-xoa-banh-o" aria-label="Gỡ bánh khỏi ô ${idx + 1}" title="Gỡ bánh">×</button>
                <img src="${banh.anhSlot}" alt="${banh.tenBanh}" />
                <div class="ten-banh-trong-o" title="${banh.tenBanh}">${banh.tenBanh}</div>
                <div class="nhan-phu-trong-o">${nhanPhu}</div>
            `;
        } else {
            maBanhArr.push("0");
            div.innerHTML = `
                <span class="fs-4 fw-bold chu-do-son-mai opacity-75 lh-1">+</span>
                <small class="chu-nau-tram fw-semibold mt-1" style="font-size: 0.78rem;">Ô bánh số ${idx + 1}</small>
                <span class="chu-nau-phu" style="font-size: 0.68rem;">Chọn vị bánh ở cột phải</span>
            `;
        }
        luoi.appendChild(div);
    });

    // Tính giá: Vỏ hộp giảm 5% khi tự phối + Tổng giá bánh lẻ
    let tongGiaHop = (cauHinhHop.giaVo * 0.95) + tongTienBanh;
    let maBam = `VH${cauHinhHop.maVH}_${maBanhArr.join("-")}`;

    document.getElementById("lblSoODaXep").innerText = `${soBanhDaXep} / ${cauHinhHop.sucChua} bánh`;
    document.getElementById("lblTongCalo").innerText = `${tongCalo.toLocaleString("vi-VN")} Kcal`;
    document.getElementById("lblSoBanhVip").innerText = `${soBanhVip} bánh`;
    document.getElementById("lblCanhBaoDiUng").innerText = tapHopDiUng.size > 0 ? Array.from(tapHopDiUng).join("; ") : "An toàn";
    document.getElementById("lblMaBamCauHinh").innerText = `Loại hộp: ${cauHinhHop.sucChua} bánh • ${tongCalo.toLocaleString("vi-VN")} Kcal`;
    document.getElementById("lblTongGiaHop").innerText = `${Math.round(tongGiaHop).toLocaleString("vi-VN")} đ`;

    // Kiểm tra điều kiện hợp lệ
    const btnThem = document.getElementById("btnThemHopVaoGio");
    if (soBanhDaXep < cauHinhHop.sucChua) {
        btnThem.disabled = true;
        btnThem.innerText = `🔒 Mời chọn thêm ${cauHinhHop.sucChua - soBanhDaXep} bánh nữa để hoàn tất lễ hộp`;
    } else if (soBanhVip < cauHinhHop.minVipRequired) {
        btnThem.disabled = true;
        btnThem.innerText = `⚠️ Mẫu hộp này cần tối thiểu ${cauHinhHop.minVipRequired} bánh Thượng Hạng`;
    } else {
        btnThem.disabled = false;
        btnThem.innerText = `🎁 Thêm Lễ Hộp Tự Phối Vào Giỏ Quà (${Math.round(tongGiaHop).toLocaleString("vi-VN")} đ)`;
    }
}

async function xacNhanThemHopTuChon() {
    const tenHopInput = document.getElementById("txtTenHopTuDat");
    const tenHop = tenHopInput && tenHopInput.value.trim() !== ""
        ? tenHopInput.value.trim()
        : "Hộp Quà Đoàn Viên Tự Phối";

    const danhSachMaBanh = cauHinhHop.cacO
        .filter(o => o !== null)
        .map(o => o.maBanh);

    if (danhSachMaBanh.length < cauHinhHop.sucChua) {
        hienThiThongBaoGioHang("Vui lòng xếp đủ bánh vào các ô trống trên khay trước khi đóng hộp!", false);
        return;
    }

    const btnThem = document.getElementById("btnThemHopVaoGio");
    const noiDungCu = btnThem ? btnThem.innerHTML : "";
    if (btnThem) {
        btnThem.disabled = true;
        btnThem.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu lễ hộp quà tặng...`;
    }

    try {
        const res = await fetch("/GioHang/ThemHopTuChon", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                maVh: cauHinhHop.maVH,
                tenHopKhachDat: tenHop,
                danhSachMaBanh: danhSachMaBanh,
                soLuongHop: 1
            })
        });

        const data = await res.json();
        if (data.thanhCong) {
            hienThiThongBaoGioHang(data.thongBao, true);
            await taiLaiGioHangOffcanvas();
            moNganKeoGioHang();
        } else {
            hienThiThongBaoGioHang(data.thongBao || "Không thể thêm lễ hộp tự chọn!", false);
        }
    } catch (err) {
        hienThiThongBaoGioHang("Lỗi kết nối máy chủ khi lưu lễ hộp tự chọn!", false);
    } finally {
        if (btnThem) {
            btnThem.disabled = false;
            btnThem.innerHTML = noiDungCu;
            renderKhayHop();
        }
    }
}

// =========================================================================
// 1C. CÁC HÀM AJAX GIỎ QUÀ & ĐỒNG BỘ NGĂN KÉO GIỎ HÀNG
// =========================================================================
async function themBanhLeVaoGio(maBanh, soLuong = 1, nutBam = null) {
    try {
        const res = await fetch("/GioHang/ThemBanhHoacCombo", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                loaiSanPham: 1,
                maSanPham: maBanh,
                soLuong: soLuong
            })
        });

        const data = await res.json();
        if (data.thanhCong) {
            if (nutBam instanceof HTMLElement) {
                const nhanGoc = nutBam.dataset.nhanGoc || nutBam.innerHTML;
                nutBam.dataset.nhanGoc = nhanGoc;
                nutBam.innerText = "✓ Đã thêm vào giỏ";
                nutBam.classList.add("da-them-gio");
                setTimeout(() => {
                    nutBam.innerHTML = nhanGoc;
                    nutBam.classList.remove("da-them-gio");
                }, 1200);
            }
            hienThiThongBaoGioHang(data.thongBao, true);
            await taiLaiGioHangOffcanvas();
            if (!document.querySelector(".modal.show")) {
                moNganKeoGioHang();
            }
        } else {
            hienThiThongBaoGioHang(data.thongBao || "Không thể thêm bánh vào giỏ!", false);
        }
    } catch (err) {
        hienThiThongBaoGioHang("Lỗi kết nối khi thêm bánh lẻ vào giỏ quà!", false);
    }
}

async function capNhatSoLuongMon(maCtgh, soLuongMoi) {
    try {
        const res = await fetch("/GioHang/CapNhatSoLuong", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                maCtgh: maCtgh,
                soLuongMoi: soLuongMoi
            })
        });

        const data = await res.json();
        if (data.thanhCong) {
            await taiLaiGioHangOffcanvas();
        } else {
            hienThiThongBaoGioHang(data.thongBao || "Không thể cập nhật số lượng!", false);
        }
    } catch (err) {
        hienThiThongBaoGioHang("Lỗi kết nối khi cập nhật số lượng!", false);
    }
}

async function xoaMonKhoiGio(maCtgh) {
    try {
        const res = await fetch("/GioHang/XoaMon", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                maCtgh: maCtgh,
                soLuongMoi: 0
            })
        });

        const data = await res.json();
        if (data.thanhCong) {
            hienThiThongBaoGioHang(data.thongBao, true);
            await taiLaiGioHangOffcanvas();
        } else {
            hienThiThongBaoGioHang(data.thongBao || "Không thể xóa món quà!", false);
        }
    } catch (err) {
        hienThiThongBaoGioHang("Lỗi kết nối khi xóa món khỏi giỏ quà!", false);
    }
}

// Chuẩn hóa khung Mã ưu đãi dưới đáy Ngăn kéo Giỏ quà để nhãn "Mã ưu đãi:" và nút "Áp mã TRANGTRON2026 (-10%)" nằm gọn trên 1 hàng ngang
function chuanHoaKhungVoucherOffcanvas() {
    const txtVoucher = document.getElementById("txtMaVoucherGioHang");
    if (!txtVoucher) return;

    const khungVoucher = txtVoucher.closest(".mb-3") || txtVoucher.parentElement?.parentElement;
    if (!khungVoucher || khungVoucher.dataset.daChuanHoaVoucher === "true") return;

    const giaTriHienTai = txtVoucher.value || "";
    const lblThongBaoCu = document.getElementById("lblThongBaoVoucher");
    const htmlThongBaoCu = lblThongBaoCu ? lblThongBaoCu.innerHTML : "";
    const classThongBaoCu = lblThongBaoCu ? lblThongBaoCu.className : "d-block mt-1";

    khungVoucher.dataset.daChuanHoaVoucher = "true";
    khungVoucher.innerHTML = `
        <div class="hang-nhan-ma-uu-dai" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: nowrap; white-space: nowrap; gap: 8px; margin-bottom: 6px;">
            <label for="txtMaVoucherGioHang" class="small fw-bold chu-do-son-mai mb-0 text-nowrap" style="white-space: nowrap;">
                <i class="bi bi-ticket-perforated-fill me-1"></i>Mã ưu đãi:
            </label>
            <button type="button" class="btn btn-sm btn-vien-vang rounded-pill py-0 px-2 text-nowrap" style="font-size: 0.72rem; white-space: nowrap;"
                    onclick="chonNhanhVoucher('TRANGTRON2026')">
                Áp mã TRANGTRON2026 (-10%)
            </button>
        </div>
        <div class="nhom-nhap-ma-uu-dai" style="display: flex; align-items: stretch; gap: 6px;">
            <input type="text" id="txtMaVoucherGioHang" class="form-control form-control-sm"
                   value="${giaTriHienTai}" placeholder="Nhập mã ưu đãi..." />
            <button type="button" class="btn btn-sm btn-do-son-mai px-3 text-nowrap" onclick="apDungVoucherGioHang()">
                Áp Dụng
            </button>
        </div>
        <small id="lblThongBaoVoucher" class="${classThongBaoCu}" style="font-size: 0.75rem;">${htmlThongBaoCu}</small>
    `;
}

// Tách danh sách ô bánh thành bảng 2 cột trật tự (.bang-thanh-phan-hop-2-cot: Ô 1..6 | Tên bánh)
function taoBangThanhPhanHop2Cot(danhSachViBanh) {
    if (!danhSachViBanh) return "";

    let mangO = [];
    if (Array.isArray(danhSachViBanh)) {
        danhSachViBanh.forEach(chuoi => {
            if (typeof chuoi === "string" && chuoi.includes("•")) {
                chuoi.split("•").forEach(phan => {
                    if (phan.trim()) mangO.push(phan.trim());
                });
            } else if (typeof chuoi === "string" && chuoi.trim()) {
                mangO.push(chuoi.trim());
            }
        });
    } else if (typeof danhSachViBanh === "string") {
        danhSachViBanh.split("•").forEach(phan => {
            if (phan.trim()) mangO.push(phan.trim());
        });
    }

    if (mangO.length === 0) return "";

    let dongHtml = "";
    mangO.forEach((muc, idx) => {
        let nhanO = `Ô ${idx + 1}`;
        let tenBanh = muc;
        const khop = muc.match(/^Ô\s*(?:số\s*)?(\d+)\s*:\s*(.+)$/i);
        if (khop) {
            nhanO = `Ô ${khop[1]}`;
            tenBanh = khop[2].trim();
        }
        dongHtml += `
            <div class="dong-thanh-phan-hop" style="display: flex; align-items: baseline; padding: 4px 8px; border-bottom: 1px dashed #EAE0D5; font-size: 0.78rem;">
                <span class="cot-o-banh" style="font-weight: 700; color: #7A1215; flex: 0 0 58px; width: 58px;">${nhanO}</span>
                <span class="cot-ten-banh" style="flex: 1; color: #2C1810; font-weight: 500;">${tenBanh}</span>
            </div>
        `;
    });

    return `<div class="bang-thanh-phan-hop-2-cot">${dongHtml}</div>`;
}

async function taiLaiGioHangOffcanvas() {
    try {
        chuanHoaKhungVoucherOffcanvas();
        const res = await fetch("/GioHang/LayThongTinGioHangJson");
        const data = await res.json();
        if (!data || !data.thanhCong) return;

        const badge = document.getElementById("badgeSoLuongGio");
        if (badge) {
            badge.innerText = data.tongSoLuong;
        }

        const lblTamTinh = document.getElementById("lblTamTinhOffcanvas");
        if (lblTamTinh) {
            lblTamTinh.innerText = `${Math.round(data.tongTamTinh).toLocaleString("vi-VN")} đ`;
        }

        const lblTienGiam = document.getElementById("lblTienGiamVoucherOffcanvas");
        if (lblTienGiam) {
            const tienGiam = Math.round(data.tienGiamVoucher || 0);
            lblTienGiam.innerText = `-${tienGiam.toLocaleString("vi-VN")} đ`;
        }

        const lblSauGiam = document.getElementById("lblTongThanhToanSauGiam");
        if (lblSauGiam) {
            const sauGiam = Math.round(data.tongThanhToanSauGiam !== undefined ? data.tongThanhToanSauGiam : data.tongTamTinh);
            lblSauGiam.innerText = `${sauGiam.toLocaleString("vi-VN")} đ`;
        }

        const txtVoucher = document.getElementById("txtMaVoucherGioHang");
        const lblThongBaoVoucher = document.getElementById("lblThongBaoVoucher");
        if (data.codeKmDangDung) {
            if (txtVoucher) txtVoucher.value = data.codeKmDangDung;
            if (lblThongBaoVoucher) {
                lblThongBaoVoucher.className = "d-block mt-1 text-success";
                lblThongBaoVoucher.innerHTML = `<i class="bi bi-check-circle-fill me-1"></i>Đang áp mã <strong>${data.codeKmDangDung}</strong> (-${Math.round(data.tienGiamVoucher).toLocaleString("vi-VN")} đ)`;
            }
        } else if (lblThongBaoVoucher) {
            lblThongBaoVoucher.innerHTML = "";
        }

        const khungDanhSach = document.getElementById("danhSachMonTrongGio");
        if (!khungDanhSach) return;

        if (!data.danhSachMon || data.danhSachMon.length === 0) {
            khungDanhSach.innerHTML = `<div class="text-center chu-nau-phu py-5">Giỏ quà đang trống. Mời bạn chọn bánh hoặc tự phối lễ hộp!</div>`;
            return;
        }

        let html = "";
        data.danhSachMon.forEach(item => {
            let thongTinPhu = "";
            if (item.loaiSanPham === 3) {
                const soBanhTrongHop = (item.danhSachViBanh && item.danhSachViBanh.length > 0)
                    ? item.danhSachViBanh.length
                    : (item.maBamCauHinh && item.maBamCauHinh.includes("_")
                        ? item.maBamCauHinh.split("_")[1].split("-").length
                        : 4);
                const caloText = (item.tongCalo || 0).toLocaleString("vi-VN");
                thongTinPhu += `<small class="chu-nau-phu d-block mt-1">Loại hộp: ${soBanhTrongHop} bánh • ${caloText} Kcal</small>`;
                if (item.danhSachViBanh && item.danhSachViBanh.length > 0) {
                    thongTinPhu += taoBangThanhPhanHop2Cot(item.danhSachViBanh);
                }
            } else if (item.loaiSanPham === 2) {
                if (item.danhSachViBanh && item.danhSachViBanh.length > 0) {
                    thongTinPhu += taoBangThanhPhanHop2Cot(item.danhSachViBanh);
                }
            } else if (item.loaiSanPham === 1 && item.tongCalo > 0) {
                thongTinPhu += `<small class="chu-nau-phu d-block mt-1">Bánh lẻ • ${item.tongCalo} Kcal / bánh</small>`;
            }

            html += `
                <div class="p-3 mb-2 rounded card-hoang-gia">
                    <div class="d-flex justify-content-between align-items-start gap-2">
                        <strong class="chu-do-son-mai">${item.tenMon}</strong>
                        <button type="button" class="btn btn-sm btn-outline-danger py-0 px-2"
                                onclick="xoaMonKhoiGio(${item.maCtgh})" title="Xóa món này">
                            🗑️
                        </button>
                    </div>
                    ${thongTinPhu}
                    <div class="d-flex justify-content-between align-items-center mt-2 pt-2 border-top" style="border-color: var(--mau-vien-vang-dong) !important;">
                        <div class="btn-group btn-group-sm" role="group">
                            <button type="button" class="btn btn-vien-vang px-2 py-0"
                                     onclick="capNhatSoLuongMon(${item.maCtgh}, ${item.soLuong - 1})">
                                -
                            </button>
                            <span class="btn px-3 py-0 disabled fw-bold" style="background: var(--mau-nen-ngoc-trai); color: var(--mau-chu-nau-tram); border-color: var(--mau-vang-dong-co);">${item.soLuong}</span>
                            <button type="button" class="btn btn-vien-vang px-2 py-0"
                                     onclick="capNhatSoLuongMon(${item.maCtgh}, ${item.soLuong + 1})">
                                +
                            </button>
                        </div>
                        <div class="fw-bold chu-do-son-mai">${Math.round(item.thanhTien).toLocaleString("vi-VN")} đ</div>
                    </div>
                </div>
            `;
        });

        khungDanhSach.innerHTML = html;
    } catch (err) {
        console.error("Không thể đồng bộ giỏ quà:", err);
    }
}

function moNganKeoGioHang() {
    const elOffcanvas = document.getElementById("nganKeoGioHang");
    if (elOffcanvas && typeof bootstrap !== "undefined") {
        const bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(elOffcanvas);
        bsOffcanvas.show();
    }
}

function chuyenDenTrangThanhToan(e) {
    const badge = document.getElementById("badgeSoLuongGio");
    const soLuong = badge ? parseInt(badge.innerText || "0") : 0;
    if (soLuong <= 0) {
        if (e) e.preventDefault();
        hienThiThongBaoGioHang("Giỏ quà đang trống! Hãy chọn bánh lẻ hoặc tự phối lễ hộp trước khi đặt quà.", false);
        return false;
    }
    window.location.href = "/ThanhToan/Index";
    return true;
}

function chonNhanhVoucher(maCode) {
    const txtVoucher = document.getElementById("txtMaVoucherGioHang");
    if (txtVoucher) {
        txtVoucher.value = maCode;
        apDungVoucherGioHang();
    }
}

async function apDungVoucherGioHang() {
    const txtVoucher = document.getElementById("txtMaVoucherGioHang");
    const lblThongBaoVoucher = document.getElementById("lblThongBaoVoucher");
    const codeKm = txtVoucher ? txtVoucher.value.trim() : "";

    try {
        const res = await fetch(`/GioHang/ApDungVoucher?codeKm=${encodeURIComponent(codeKm)}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ codeKm: codeKm })
        });

        const data = await res.json();
        if (data.thanhCong) {
            hienThiThongBaoGioHang(data.thongBao, true);
            await taiLaiGioHangOffcanvas();
        } else {
            if (lblThongBaoVoucher) {
                lblThongBaoVoucher.className = "d-block mt-1 text-danger";
                lblThongBaoVoucher.innerHTML = `<i class="bi bi-exclamation-circle-fill me-1"></i>${data.thongBao}`;
            }
            hienThiThongBaoGioHang(data.thongBao, false);
            await taiLaiGioHangOffcanvas();
        }
    } catch (err) {
        hienThiThongBaoGioHang("Lỗi kết nối máy chủ khi áp dụng mã ưu đãi!", false);
    }
}

// =========================================================================
// 1D. ĐĂNG NHẬP, ĐĂNG KÝ HỘI VIÊN & ĐỒNG BỘ GIỎ QUÀ
// =========================================================================
function dienNhanhTaiKhoanDemo(taiKhoan, matKhau) {
    const txtTk = document.getElementById("txtDangNhapTaiKhoan");
    const txtMk = document.getElementById("txtDangNhapMatKhau");
    if (txtTk) txtTk.value = taiKhoan;
    if (txtMk) txtMk.value = matKhau;
    thucHienDangNhapAjax();
}

async function thucHienDangNhapAjax() {
    const taiKhoan = document.getElementById("txtDangNhapTaiKhoan")?.value.trim() || "";
    const matKhau = document.getElementById("txtDangNhapMatKhau")?.value.trim() || "";
    const boxLoi = document.getElementById("thongBaoLoiDangNhap");
    const btn = document.getElementById("btnSubmitDangNhap");

    if (boxLoi) boxLoi.classList.add("d-none");
    if (btn) btn.disabled = true;

    try {
        const res = await fetch("/TaiKhoan/DangNhapAjax", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                tenDangNhap: taiKhoan,
                matKhau: matKhau
            })
        });

        const data = await res.json();
        if (data.thanhCong) {
            hienThiThongBaoGioHang(`${data.thongBao} Kính chào ${data.hoTen} (Hạng ${data.hangTv} • ${data.diem} điểm)!`, true);
            setTimeout(() => {
                window.location.reload();
            }, 900);
        } else {
            if (boxLoi) {
                boxLoi.innerText = data.thongBao || "Đăng nhập không thành công!";
                boxLoi.classList.remove("d-none");
            }
        }
    } catch (err) {
        if (boxLoi) {
            boxLoi.innerText = "Lỗi kết nối máy chủ khi đăng nhập!";
            boxLoi.classList.remove("d-none");
        }
    } finally {
        if (btn) btn.disabled = false;
    }
}

async function thucHienDangKyAjax() {
    const hoTen = document.getElementById("txtDangKyHoTen")?.value.trim() || "";
    const sdt = document.getElementById("txtDangKySdt")?.value.trim() || "";
    const email = document.getElementById("txtDangKyEmail")?.value.trim() || "";
    const matKhau = document.getElementById("txtDangKyMatKhau")?.value.trim() || "";
    const boxLoi = document.getElementById("thongBaoLoiDangKy");
    const btn = document.getElementById("btnSubmitDangKy");

    if (boxLoi) boxLoi.classList.add("d-none");
    if (btn) btn.disabled = true;

    try {
        const res = await fetch("/TaiKhoan/DangKyAjax", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                hoTen: hoTen,
                sdt: sdt,
                email: email,
                matKhau: matKhau
            })
        });

        const data = await res.json();
        if (data.thanhCong) {
            hienThiThongBaoGioHang(data.thongBao, true);
            setTimeout(() => {
                window.location.reload();
            }, 900);
        } else {
            if (boxLoi) {
                boxLoi.innerText = data.thongBao || "Đăng ký không thành công!";
                boxLoi.classList.remove("d-none");
            }
        }
    } catch (err) {
        if (boxLoi) {
            boxLoi.innerText = "Lỗi kết nối máy chủ khi đăng ký tài khoản!";
            boxLoi.classList.remove("d-none");
        }
    } finally {
        if (btn) btn.disabled = false;
    }
}

function hienThiThongBaoGioHang(noiDung, thanhCong = true) {
    let container = document.getElementById("khungThongBaoToast");
    if (!container) {
        container = document.createElement("div");
        container.id = "khungThongBaoToast";
        container.className = "toast-container position-fixed top-0 end-0 p-3";
        container.style.zIndex = "1090";
        container.style.marginTop = "76px";
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

// =========================================================================
// 2. BỘ LỌC BÁNH THEO HƯƠNG VỊ DINH DƯỠNG & THƯƠNG HIỆU (TẦNG 3)
// =========================================================================
let boLocDinhDuongHienTai = "ALL";
let boLocThuongHieuHienTai = "ALL";

function apDungBoLocTang4() {
    document.querySelectorAll(".item-banh-card").forEach(card => {
        let khopDinhDuong = true;
        if (boLocDinhDuongHienTai === "ISOMALT") khopDinhDuong = card.dataset.isomalt === "1";
        else if (boLocDinhDuongHienTai === "VIP") khopDinhDuong = card.dataset.vip === "1";
        else if (boLocDinhDuongHienTai === "LAVA") khopDinhDuong = card.dataset.lava === "1";
        else if (boLocDinhDuongHienTai === "CHAY") khopDinhDuong = card.dataset.chay === "1";

        let khopThuongHieu = true;
        if (boLocThuongHieuHienTai !== "ALL") {
            khopThuongHieu = (card.dataset.thuonghieu || "").toLowerCase() === boLocThuongHieuHienTai.toLowerCase();
        }

        card.classList.toggle("d-none", !(khopDinhDuong && khopThuongHieu));
    });

    const luoi = document.getElementById("luoiSanPhamBanh");
    if (luoi) {
        luoi.scrollTo({ left: 0, behavior: "smooth" });
    }
}

function locSanPham(loaiLoc, btn) {
    boLocDinhDuongHienTai = loaiLoc;
    document.querySelectorAll("#nhomLocDinhDuong .btn-vien-vang").forEach(b => b.classList.remove("active"));
    if (btn) btn.classList.add("active");
    apDungBoLocTang4();
}

function locTheoThuongHieu(thuongHieu, btn) {
    boLocThuongHieuHienTai = thuongHieu;
    document.querySelectorAll("#nhomLocThuongHieu .btn-thuong-hieu").forEach(b => {
        b.classList.remove("btn-do-son-mai");
        b.classList.add("btn-vien-vang");
    });
    if (btn) {
        btn.classList.remove("btn-vien-vang");
        btn.classList.add("btn-do-son-mai");
    }
    apDungBoLocTang4();
}

// =========================================================================
// 3. BẢNG ƯỚC TÍNH CHIẾT KHẤU SỈ B2B TRỰC TIẾP (TẦNG 4)
// =========================================================================
function tinhGiaSiB2B(soHop) {
    soHop = parseInt(soHop);
    document.getElementById("lblSoHopB2B").innerText = `${soHop} Hộp`;

    let phanTram = 8;
    let mienPhiLogo = false;
    if (soHop >= 100) { phanTram = 22; mienPhiLogo = true; }
    else if (soHop >= 30) { phanTram = 15; mienPhiLogo = true; }

    let giaTrungBinh1Hop = 600000;
    let tienTietKiem = Math.round(soHop * giaTrungBinh1Hop * (phanTram / 100));

    document.getElementById("lblPhanTramB2B").innerText = `${phanTram}%`;
    document.getElementById("lblLogoB2B").innerText = mienPhiLogo ? "MIỄN PHÍ" : "+400.000đ";
    document.getElementById("lblTietKiemB2B").innerText = `${tienTietKiem.toLocaleString("vi-VN")}đ`;
}

// =========================================================================
// 4. NGHỆ NHÂN TƯ VẤN CHỌN QUÀ HỢP Ý & TRA CỨU ĐƠN HÀNG
// =========================================================================
let danhSachBanhGoiYHienTai = [];

// Đóng Modal Tư Vấn Chọn Quà khi người dùng chuyển sang thao tác xếp hộp
function dongModalTuVanQua() {
    const modalEl = document.getElementById("modalTroLyChonBanh");
    if (modalEl && typeof bootstrap !== "undefined") {
        const modalInstance = bootstrap.Modal.getInstance(modalEl) || bootstrap.Modal.getOrCreateInstance(modalEl);
        modalInstance.hide();
    }
}

// Chuẩn hóa thông tin bánh từ kết quả API Gợi Ý kết hợp dữ liệu thực tế trên DOM (nếu đang ở Trang chủ)
function chuanHoaThongTinBanhGoiY(item) {
    let maBanh = parseInt(item.maBanh || item.MaBanh || item.id || 0);
    let tenBanh = item.tenBanh || "";
    let donGia = parseFloat(item.donGia || 0);
    let calo = parseInt(item.calo || 400);
    let laVip = Boolean(item.laVip || item.laBanhThuongHang);
    let anhSlot = item.anhBanh || "/images/default/banh-mac-dinh.png";
    let diUng = item.diUng || "Không";
    let thuongHieu = item.thuongHieu || "";

    let nutDom = null;
    if (maBanh > 0) {
        nutDom = document.querySelector(`.btn-chon-banh-khay[data-mabanh="${maBanh}"]`);
    }
    if (!nutDom && tenBanh) {
        const tenChuan = tenBanh.trim().toLowerCase();
        document.querySelectorAll(".btn-chon-banh-khay").forEach(btn => {
            if ((btn.dataset.tenbanh || "").trim().toLowerCase() === tenChuan) {
                nutDom = btn;
            }
        });
    }

    if (nutDom) {
        maBanh = parseInt(nutDom.dataset.mabanh || maBanh);
        tenBanh = nutDom.dataset.tenbanh || tenBanh;
        donGia = parseFloat(nutDom.dataset.dongia || donGia);
        calo = parseInt(nutDom.dataset.calo || calo);
        laVip = nutDom.dataset.lavip === "true";
        anhSlot = nutDom.dataset.anhslot || anhSlot;
        diUng = nutDom.dataset.diung || diUng;
        thuongHieu = nutDom.dataset.thuonghieu || thuongHieu;
    }

    return { maBanh, tenBanh, donGia, calo, laVip, anhSlot, diUng, thuongHieu };
}

async function chayThuatToanGoiY() {
    const dt = document.getElementById("quizDoiTuong").value;
    const sk = document.getElementById("quizSucKhoe").value;
    const hv = document.getElementById("quizHuongVi").value;
    const ns = document.getElementById("quizNganSach").value;

    const res = await fetch(`/Home/GoiYBanhThongMinh?doiTuong=${dt}&sucKhoe=${sk}&huongVi=${hv}&nganSachMax=${ns}`);
    const data = await res.json();
    danhSachBanhGoiYHienTai = Array.isArray(data) ? data : [];

    const container = document.getElementById("ketQuaQuizContainer");
    container.innerHTML = "";

    danhSachBanhGoiYHienTai.forEach((item, idx) => {
        const banh = chuanHoaThongTinBanhGoiY(item);
        container.innerHTML += `
            <div class="col-md-4">
                <div class="p-3 rounded card-hoang-gia h-100 text-center d-flex flex-column">
                    <span class="badge badge-thuong-hang mb-2 align-self-center">Độ hợp vị: ${item.doPhuHop}%</span>
                    <img src="${item.anhBanh}" width="90" height="90" class="d-block mx-auto my-2 rounded-circle p-1 bg-white" style="border: 1.5px solid var(--mau-vang-dong-co);" />
                    <h6 class="chu-do-son-mai fw-bold">${item.tenBanh}</h6>
                    <div class="small text-success fw-medium mb-1">${item.calo} Kcal • Chỉ số GI: ${item.chiSoGI}</div>
                    <p class="small chu-nau-phu mb-2" style="font-size:0.75rem;">${item.lyDo}</p>
                    <strong class="d-block chu-nau-tram">${item.donGia.toLocaleString("vi-VN")} đ / bánh</strong>
                    <div class="cum-nut-goi-y-banh" style="display: flex; gap: 6px; margin-top: auto; padding-top: 10px; width: 100%;">
                        <button type="button" class="btn-goi-y-mua-le" style="flex: 1; font-size: 0.78rem; padding: 6px 8px; border-radius: 6px;"
                                onclick="themBanhLeVaoGio(${banh.maBanh}, 1, this)">
                            + Mua lẻ
                        </button>
                        <button type="button" class="btn-goi-y-xep-hop" style="flex: 1; font-size: 0.78rem; padding: 6px 8px; border-radius: 6px;"
                                onclick="xepBanhGoiYVaoHop(${idx})">
                            Xếp vào hộp
                        </button>
                    </div>
                </div>
            </div>
        `;
    });

    if (danhSachBanhGoiYHienTai.length > 0) {
        container.innerHTML += `
            <div class="col-12 text-center mt-2 pt-1">
                <button type="button" class="btn btn-xep-ca-3-goi-y" onclick="xepCa3ViGoiYVaoHop()">
                    🎁 Xếp Cả 3 Vị Gợi Ý Vào Khay Hộp Đoàn Viên
                </button>
            </div>
        `;
    }
}

// Xếp 1 chiếc bánh từ kết quả gợi ý vào ô trống đầu tiên trong khay hộp
function xepBanhGoiYVaoHop(chiSoGoiY) {
    const item = danhSachBanhGoiYHienTai[chiSoGoiY];
    if (!item) return;

    const banh = chuanHoaThongTinBanhGoiY(item);
    const tangHop = document.getElementById("tang-tu-chon-hop");
    const luoiOBanh = document.getElementById("luoiOBanh");

    if (tangHop && luoiOBanh) {
        dongModalTuVanQua();
        setTimeout(() => {
            chuyenDenTang(1);
            const conOTrong = cauHinhHop.cacO.some(o => o === null);
            datBanhVaoOTrong(banh.maBanh, banh.tenBanh, banh.donGia, banh.calo, banh.laVip, banh.anhSlot, banh.diUng, banh.thuongHieu);
            if (conOTrong) {
                hienThiThongBaoGioHang(`Đã xếp "${banh.tenBanh}" vào khay lễ hộp!`, true);
            }
        }, 180);
    } else {
        const thamSoBanh = banh.maBanh > 0 ? banh.maBanh : banh.tenBanh;
        window.location.href = `/?chonBanh=${encodeURIComponent(thamSoBanh)}#tang-tu-chon-hop`;
    }
}

// Xếp cả 3 vị bánh gợi ý vào khay lễ hộp Đoàn Viên
function xepCa3ViGoiYVaoHop() {
    if (!danhSachBanhGoiYHienTai || danhSachBanhGoiYHienTai.length === 0) return;

    const danhSachBanh = danhSachBanhGoiYHienTai.map(item => chuanHoaThongTinBanhGoiY(item));
    const tangHop = document.getElementById("tang-tu-chon-hop");
    const luoiOBanh = document.getElementById("luoiOBanh");

    if (tangHop && luoiOBanh) {
        dongModalTuVanQua();
        setTimeout(() => {
            chuyenDenTang(1);
            const soOTrong = cauHinhHop.cacO.filter(o => o === null).length;
            if (soOTrong < danhSachBanh.length) {
                cauHinhHop.cacO = new Array(cauHinhHop.sucChua).fill(null);
            }
            danhSachBanh.forEach(banh => {
                datBanhVaoOTrong(banh.maBanh, banh.tenBanh, banh.donGia, banh.calo, banh.laVip, banh.anhSlot, banh.diUng, banh.thuongHieu);
            });
            hienThiThongBaoGioHang("Đã xếp cả 3 vị bánh gợi ý vào khay lễ hộp Đoàn Viên!", true);
        }, 180);
    } else {
        const chuoiMaBanh = danhSachBanh.map(b => b.maBanh > 0 ? b.maBanh : b.tenBanh).join(",");
        window.location.href = `/?chonBanh=${encodeURIComponent(chuoiMaBanh)}#tang-tu-chon-hop`;
    }
}

// Tự động nhận mã bánh từ URL (/?chonBanh=...#tang-tu-chon-hop) khi điều hướng từ trang khác về Trang chủ
function xuLyTuDongXepBanhTuUrl() {
    if (!window.location.search) return;
    const params = new URLSearchParams(window.location.search);
    const chonBanhParam = params.get("chonBanh");
    if (!chonBanhParam) return;

    const tangHop = document.getElementById("tang-tu-chon-hop");
    const luoiOBanh = document.getElementById("luoiOBanh");
    if (!tangHop || !luoiOBanh) return;

    const danhSachKhoa = chonBanhParam.split(",").map(s => s.trim()).filter(Boolean);
    if (danhSachKhoa.length === 0) return;

    setTimeout(() => {
        chuyenDenTang(1);
        danhSachKhoa.forEach(khoa => {
            let nutDom = document.querySelector(`.btn-chon-banh-khay[data-mabanh="${khoa}"]`);
            if (!nutDom) {
                const tenChuan = khoa.toLowerCase();
                document.querySelectorAll(".btn-chon-banh-khay").forEach(btn => {
                    if ((btn.dataset.tenbanh || "").trim().toLowerCase() === tenChuan) {
                        nutDom = btn;
                    }
                });
            }
            if (nutDom) {
                datBanhVaoOTrong(
                    parseInt(nutDom.dataset.mabanh),
                    nutDom.dataset.tenbanh,
                    parseFloat(nutDom.dataset.dongia),
                    parseInt(nutDom.dataset.calo),
                    nutDom.dataset.lavip === "true",
                    nutDom.dataset.anhslot,
                    nutDom.dataset.diung,
                    nutDom.dataset.thuonghieu
                );
            }
        });
        hienThiThongBaoGioHang("Đã tự động xếp bánh gợi ý vào khay lễ hộp!", true);

        // Xóa tham số chonBanh trên thanh địa chỉ để tránh lặp lại khi tải lại trang
        params.delete("chonBanh");
        const queryMoi = params.toString();
        const urlMoi = window.location.pathname + (queryMoi ? "?" + queryMoi : "") + window.location.hash;
        window.history.replaceState({}, document.title, urlMoi);
    }, 220);
}

async function traCuuDonHangGuest() {
    const ma = document.getElementById("txtMaTraCuu").value;
    const sdt = document.getElementById("txtSdtTraCuu").value;
    const box = document.getElementById("ketQuaTraCuuBox");

    const res = await fetch(`/Home/TraCuuDonHangNhanh?maTraCuu=${encodeURIComponent(ma)}&sdt=${encodeURIComponent(sdt)}`);
    const data = await res.json();

    box.classList.remove("d-none");
    if (!data.thanhCong) {
        box.innerHTML = `<span class="text-danger fw-medium"><i class="bi bi-exclamation-triangle me-1"></i>${data.thongBao}</span>`;
    } else {
        box.innerHTML = `
            <div class="small chu-nau-tram">
                <strong class="chu-do-son-mai d-block mb-1">Đơn hàng: ${data.maDon} (${data.trangThai})</strong>
                <div>Người đặt: ${data.nguoiMua} • Ngày đặt: ${data.ngayDat}</div>
                <div>Tổng thanh toán: <strong class="chu-do-son-mai">${data.tongTien}</strong> (${data.soDiemGiao} điểm giao)</div>
                <a href="/ThanhToan/ThanhCong?maDon=${encodeURIComponent(data.maDon)}" class="btn btn-sm btn-do-son-mai rounded-pill mt-2 w-100">
                    <i class="bi bi-qr-code-scan me-1"></i>Xem Chi Tiết Đơn &amp; Mã QR Thiệp 3D
                </a>
            </div>
        `;
    }
}

function khoiChayDongHoDemNguoc() {
    const el = document.getElementById("cd-giay");
    if (!el) return;
    let giay = 30;
    setInterval(() => {
        giay = giay > 0 ? giay - 1 : 59;
        el.innerText = giay.toString().padStart(2, '0');
    }, 1000);
}
