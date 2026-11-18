// =========================================================================
// MODULE 1: BỘ ĐIỀU KHIỂN CUỘN FULLPAGE SLIDE DECK GPU (TRANSFORM: TRANSLATE3D)
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

function laCheDoFullpageDesktop() {
    return window.innerWidth >= 992 && document.getElementById("dai-truot-fullpage") !== null;
}

function layChieuCaoThanhMenu() {
    return document.querySelector("header, .navbar")?.offsetHeight || 68;
}

function capNhatChamTronActive(selector) {
    document.querySelectorAll(".side-dot-btn").forEach(btn => {
        btn.classList.toggle("active", btn.getAttribute("data-target") === selector);
    });
}

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

function khoiTaoBoMayChuyenCanhRoiRac() {
    const daiTruot = document.getElementById("dai-truot-fullpage");
    danhSachCacTang = DANH_SACH_PHAN_KHU
        .map(sel => document.querySelector(sel))
        .filter(el => el !== null);

    if (danhSachCacTang.length === 0) return;

    dongBoKhungFullpageTheoManHinh();

    const tangDau = danhSachCacTang[chiSoTangHienTai];
    if (tangDau) {
        capNhatChamTronActive("#" + tangDau.id);
        tangDau.querySelector(".section-reveal")?.classList.add("active");
    }

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

function chuyenDenTang(chiSoMoi) {
    const tongSoTang = danhSachCacTang.length;
    if (tongSoTang === 0) return;

    const chiSoHopLe = Math.max(0, Math.min(chiSoMoi, tongSoTang - 1));
    if (dangChuyenCanh === true || chiSoHopLe === chiSoTangHienTai) return;

    const phanTuDich = danhSachCacTang[chiSoHopLe];
    if (!phanTuDich) return;

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

    const daiTruot = document.getElementById("dai-truot-fullpage");
    if (daiTruot) {
        window.scrollTo(0, 0);
        daiTruot.style.transform = "translate3d(0, -" + (chiSoTangHienTai * 100) + "%, 0)";
    }

    if (phanTuDich.id) {
        capNhatChamTronActive("#" + phanTuDich.id);
    }
    phanTuDich.querySelector(".section-reveal")?.classList.add("active");

    if (boHenGioDuPhongChuyenCanh) {
        clearTimeout(boHenGioDuPhongChuyenCanh);
    }
    boHenGioDuPhongChuyenCanh = setTimeout(() => {
        hoanTatChuyenCanhGPU();
    }, 880);
}

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

function cuonDenPhanKhu(selector, e) {
    const mucTieu = document.querySelector(selector);
    if (!mucTieu) {
        return true;
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

function layDanhSachSectionTrangChu() {
    return DANH_SACH_PHAN_KHU
        .map(id => document.querySelector(id))
        .filter(el => el !== null);
}

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

    const btnAllLoc = document.querySelector('#nhomLocDinhDuong button[data-loc="ALL"]');
    if (typeof locSanPham === "function") locSanPham("ALL", btnAllLoc);

    let btnTh = null;
    document.querySelectorAll("#nhomLocThuongHieu .btn-thuong-hieu").forEach(b => {
        if ((b.getAttribute("data-th") || "").toLowerCase() === tenThuongHieu.toLowerCase()) {
            btnTh = b;
        }
    });
    if (typeof locTheoThuongHieu === "function") locTheoThuongHieu(tenThuongHieu, btnTh);

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

    const btnAllTh = document.querySelector('#nhomLocThuongHieu button[data-th="ALL"]');
    if (typeof locTheoThuongHieu === "function") locTheoThuongHieu("ALL", btnAllTh);

    const btnLoc = document.querySelector(`#nhomLocDinhDuong button[data-loc="${loaiLoc}"]`);
    if (typeof locSanPham === "function") locSanPham(loaiLoc, btnLoc);

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
    if (radioVh && typeof doiMauVoHop === "function") {
        radioVh.checked = true;
        doiMauVoHop(radioVh);
    }

    const chiSoTuongUng = danhSachCacTang.indexOf(tangHop);
    if (chiSoTuongUng !== -1) {
        chuyenDenTang(chiSoTuongUng);
    }
    return false;
}

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
