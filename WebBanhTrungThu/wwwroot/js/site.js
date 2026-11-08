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
    renderKhayHop();
    khoiChayDongHoDemNguoc();
    taiLaiGioHangOffcanvas();
    khoiTaoSectionScrollObserver();
    khoiTaoBoMayChuyenCanhRoiRac();
    khoiTaoLienKetNeoMuot();
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

    const khungKhay = document.getElementById("khungKhayHop");
    if (khungKhay) {
        khungKhay.style.backgroundImage = `linear-gradient(rgba(74, 7, 9, 0.32), rgba(74, 7, 9, 0.32)), url('${radio.dataset.anhkhay}')`;
    }

    const luoi = document.getElementById("luoiOBanh");
    if (luoi) {
        luoi.className = cauHinhHop.kieuLuoi === "grid-3x2" ? "luoi-hop-3x2 w-100" : "luoi-hop-2x2 w-100";
    }

    // Điều chỉnh mảng các ô theo sức chứa mới
    let mangMoi = new Array(cauHinhHop.sucChua).fill(null);
    for (let i = 0; i < Math.min(cauHinhHop.cacO.length, cauHinhHop.sucChua); i++) {
        mangMoi[i] = cauHinhHop.cacO[i];
    }
    cauHinhHop.cacO = mangMoi;
    renderKhayHop();
}

function datBanhVaoOTrong(maBanh, tenBanh, donGia, calo, laVip, anhSlot, diUng) {
    const viTriTrong = cauHinhHop.cacO.findIndex(o => o === null);
    if (viTriTrong === -1) {
        hienThiThongBaoGioHang("Khay lễ hộp đã đầy! Bạn có thể chạm vào 1 ô trên khay để đổi vị bánh hoặc chọn mẫu hộp 6 bánh.", false);
        return;
    }

    cauHinhHop.cacO[viTriTrong] = { maBanh, tenBanh, donGia, calo, laVip, anhSlot, diUng };
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
        div.className = "o-banh-slot p-2";

        if (banh) {
            soBanhDaXep++;
            tongCalo += banh.calo;
            tongTienBanh += banh.donGia;
            if (banh.laVip) soBanhVip++;
            if (banh.diUng && banh.diUng !== "Không") tapHopDiUng.add(banh.diUng);
            maBanhArr.push("B" + banh.maBanh);

            div.onclick = () => xoaBanhKhoiO(idx);
            div.title = "Chạm để đổi bánh khác cho ô này";
            div.innerHTML = `
                <img src="${banh.anhSlot}" alt="${banh.tenBanh}" />
                <span class="badge badge-thuong-hang mt-1 text-truncate" style="max-width:95%; font-size:0.68rem;">
                    Ô ${idx + 1}: ${banh.tenBanh}
                </span>
            `;
        } else {
            maBanhArr.push("0");
            div.innerHTML = `
                <i class="bi bi-plus-circle-dotted fs-3 chu-do-son-mai opacity-75"></i>
                <small class="chu-nau-phu fw-medium mt-1">Ô bánh số ${idx + 1}</small>
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
    document.getElementById("lblMaBamCauHinh").innerText = `Ký hiệu hộp lễ: ${maBam}`;
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
async function themBanhLeVaoGio(maBanh, soLuong = 1) {
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
            hienThiThongBaoGioHang(data.thongBao, true);
            await taiLaiGioHangOffcanvas();
            moNganKeoGioHang();
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

async function taiLaiGioHangOffcanvas() {
    try {
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
                thongTinPhu += `<small class="chu-nau-phu d-block mt-1">Ký hiệu hộp lễ: ${item.maBamCauHinh} (${item.tongCalo.toLocaleString("vi-VN")} Kcal)</small>`;
                if (item.danhSachViBanh && item.danhSachViBanh.length > 0) {
                    thongTinPhu += `<div class="small chu-nau-phu mt-1" style="font-size: 0.75rem;">${item.danhSachViBanh.join(" • ")}</div>`;
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
async function chayThuatToanGoiY() {
    const dt = document.getElementById("quizDoiTuong").value;
    const sk = document.getElementById("quizSucKhoe").value;
    const hv = document.getElementById("quizHuongVi").value;
    const ns = document.getElementById("quizNganSach").value;

    const res = await fetch(`/Home/GoiYBanhThongMinh?doiTuong=${dt}&sucKhoe=${sk}&huongVi=${hv}&nganSachMax=${ns}`);
    const data = await res.json();

    const container = document.getElementById("ketQuaQuizContainer");
    container.innerHTML = "";

    data.forEach(item => {
        container.innerHTML += `
            <div class="col-md-4">
                <div class="p-3 rounded card-hoang-gia h-100 text-center">
                    <span class="badge badge-thuong-hang mb-2">Độ hợp vị: ${item.doPhuHop}%</span>
                    <img src="${item.anhBanh}" width="90" height="90" class="d-block mx-auto my-2 rounded-circle p-1 bg-white" style="border: 1.5px solid var(--mau-vang-dong-co);" />
                    <h6 class="chu-do-son-mai fw-bold">${item.tenBanh}</h6>
                    <div class="small text-success fw-medium mb-1">${item.calo} Kcal • Chỉ số GI: ${item.chiSoGI}</div>
                    <p class="small chu-nau-phu mb-2" style="font-size:0.75rem;">${item.lyDo}</p>
                    <strong class="d-block chu-nau-tram">${item.donGia.toLocaleString("vi-VN")} đ / bánh</strong>
                </div>
            </div>
        `;
    });
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
