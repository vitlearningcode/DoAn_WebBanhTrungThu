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
let daKhoiTaoFullpage = false;

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

function datLaiViTriCuonKhungFullpage() {
    window.scrollTo(0, 0);
    if (document.documentElement) {
        document.documentElement.scrollTop = 0;
    }
    if (document.body) {
        document.body.scrollTop = 0;
    }
    const khung = document.getElementById("khung-fullpage-trang-chu");
    if (khung) {
        khung.scrollTop = 0;
        khung.scrollLeft = 0;
    }
    const daiTruot = document.getElementById("dai-truot-fullpage");
    if (daiTruot) {
        daiTruot.scrollTop = 0;
        daiTruot.scrollLeft = 0;
    }
}

function chuanHoaSelectorPhanKhu(hashStr) {
    let hashChuan = String(hashStr || "").trim().toLowerCase();
    if (!hashChuan) return "";
    if (!hashChuan.startsWith("#")) {
        hashChuan = "#" + hashChuan;
    }
    const banDo = {
        "#tang-doanh-nghiep": "#tang-b2b",
        "#chiet-khau-so-luong": "#tang-b2b",
        "#studio-phoi-hop": "#tang-tu-chon-hop",
        "#bo-suu-tap-banh": "#tang-bo-suu-tap"
    };
    return banDo[hashChuan] || hashChuan;
}

function timChiSoTangTheoHash(hashStr) {
    if (!hashStr || danhSachCacTang.length === 0) return -1;
    const hashChuan = chuanHoaSelectorPhanKhu(hashStr);
    return danhSachCacTang.findIndex(el => el && el.id && ("#" + el.id.toLowerCase()) === hashChuan);
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
        datLaiViTriCuonKhungFullpage();
        daiTruot.style.transform = "translate3d(0, -" + (chiSoTangHienTai * 100) + "%, 0)";
    } else {
        document.body.classList.remove("trang-chu-fullpage");
        daiTruot.style.transform = "none";
    }
}

function xuLyDieuHuongTheoHash() {
    if (danhSachCacTang.length === 0) return;
    const chiSoTang = timChiSoTangTheoHash(window.location.hash);
    if (chiSoTang < 0) return;

    datLaiViTriCuonKhungFullpage();
    diChuyenDenTang(chiSoTang, false);

    setTimeout(() => {
        datLaiViTriCuonKhungFullpage();
        diChuyenDenTang(chiSoTang, false);
    }, 50);

    requestAnimationFrame(() => datLaiViTriCuonKhungFullpage());
}

function khoiTaoBoMayChuyenCanhRoiRac() {
    if (daKhoiTaoFullpage) return;
    const daiTruot = document.getElementById("dai-truot-fullpage");
    danhSachCacTang = DANH_SACH_PHAN_KHU
        .map(sel => document.querySelector(sel))
        .filter(el => el !== null);

    if (danhSachCacTang.length === 0) return;
    daKhoiTaoFullpage = true;

    if ("scrollRestoration" in history) {
        history.scrollRestoration = "manual";
    }

    const khung = document.getElementById("khung-fullpage-trang-chu");
    if (khung) {
        khung.addEventListener("scroll", function () {
            if (laCheDoFullpageDesktop() && khung.scrollTop !== 0) {
                khung.scrollTop = 0;
            }
        });
    }

    dongBoKhungFullpageTheoManHinh();

    const tangDau = danhSachCacTang[chiSoTangHienTai];
    if (tangDau) {
        capNhatChamTronActive("#" + tangDau.id);
        tangDau.querySelector(".section-reveal")?.classList.add("active");
    }

    xuLyDieuHuongTheoHash();

    if (daiTruot) {
        daiTruot.addEventListener("transitionend", function (e) {
            if (e.target !== daiTruot || e.propertyName !== "transform") return;
            hoanTatChuyenCanhGPU();
        });
    }

    window.addEventListener("hashchange", xuLyDieuHuongTheoHash);
    window.addEventListener("load", () => {
        if (window.location.hash) {
            xuLyDieuHuongTheoHash();
        } else if (laCheDoFullpageDesktop()) {
            datLaiViTriCuonKhungFullpage();
        }
    });
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

function chuyenDenTang(chiSoMoi, coHieuUng = true) {
    const tongSoTang = danhSachCacTang.length;
    if (tongSoTang === 0) return;

    const chiSoHopLe = Math.max(0, Math.min(chiSoMoi, tongSoTang - 1));
    if (coHieuUng && (dangChuyenCanh === true || chiSoHopLe === chiSoTangHienTai)) return;

    const phanTuDich = danhSachCacTang[chiSoHopLe];
    if (!phanTuDich) return;

    if (!coHieuUng) {
        if (boHenGioDuPhongChuyenCanh) {
            clearTimeout(boHenGioDuPhongChuyenCanh);
            boHenGioDuPhongChuyenCanh = null;
        }
        dangChuyenCanh = false;
        khoaQuanTinhSauChuyen = false;
        chiSoTangHienTai = chiSoHopLe;

        if (laCheDoFullpageDesktop()) {
            const daiTruot = document.getElementById("dai-truot-fullpage");
            datLaiViTriCuonKhungFullpage();
            if (daiTruot) {
                daiTruot.style.transition = "none";
                daiTruot.style.transform = "translate3d(0, -" + (chiSoTangHienTai * 100) + "%, 0)";
                void daiTruot.offsetHeight;
                daiTruot.style.transition = "";
            }
        } else {
            const chieuCaoThanhMenu = layChieuCaoThanhMenu();
            window.scrollTo({
                top: Math.max(0, phanTuDich.offsetTop - chieuCaoThanhMenu),
                behavior: "auto"
            });
        }

        if (phanTuDich.id) {
            capNhatChamTronActive("#" + phanTuDich.id);
        }
        phanTuDich.querySelector(".section-reveal")?.classList.add("active");
        return;
    }

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
        datLaiViTriCuonKhungFullpage();
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

function diChuyenDenTang(chiSoMoi, coHieuUng = true) {
    chuyenDenTang(chiSoMoi, coHieuUng);
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
    const selectorChuan = chuanHoaSelectorPhanKhu(selector) || selector;
    const mucTieu = selectorChuan ? document.querySelector(selectorChuan) : null;
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

// =========================================================================
// ĐỒNG HỒ ĐẾM NGƯỢC TRUNG THU THỰC TẾ & HERO BANNER SLIDER LUCIAN #258
// =========================================================================
let boHenGioDemNguocTrungThu = null;

function tinhMocTrungThuKeTiep() {
    const bayGio = Date.now();
    const khungCd = document.getElementById("khungDemNguocHero");
    const mocSqlIso = khungCd ? khungCd.getAttribute("data-moc-dem-nguoc") : "";
    if (mocSqlIso) {
        const mocSqlDate = new Date(mocSqlIso);
        const mocSqlMs = mocSqlDate.getTime();
        const laCuoiNamDuongLich = !isNaN(mocSqlMs) && mocSqlDate.getMonth() === 11 && mocSqlDate.getDate() === 31;
        if (!isNaN(mocSqlMs) && mocSqlMs > bayGio && !laCuoiNamDuongLich) {
            return mocSqlMs;
        }
    }

    const cacMocTrungThu = [
        new Date(2026, 8, 25, 20, 0, 0).getTime(), // 25/09/2026 20:00:00 (15/08 Âm lịch)
        new Date(2027, 8, 15, 20, 0, 0).getTime(), // 15/09/2027 20:00:00 (15/08 Âm lịch)
        new Date(2028, 9, 3, 20, 0, 0).getTime(),  // 03/10/2028 20:00:00 (15/08 Âm lịch)
        new Date(2029, 8, 22, 20, 0, 0).getTime()  // 22/09/2029 20:00:00 (15/08 Âm lịch)
    ];

    const mocSapToi = cacMocTrungThu.find(moc => moc > bayGio);
    if (mocSapToi) return mocSapToi;

    return new Date(new Date().getFullYear() + 1, 8, 15, 20, 0, 0).getTime();
}

function capNhatDongHoDemNguocTrungThu() {
    const elNgay = document.getElementById("cd-ngay");
    const elGio = document.getElementById("cd-gio");
    const elPhut = document.getElementById("cd-phut");
    const elGiay = document.getElementById("cd-giay");
    if (!elNgay || !elGio || !elPhut || !elGiay) return false;

    const hienTai = new Date();

    // Cập nhật dòng ngày & giờ hiện tại trên thanh tiêu đề
    const elDongHoText = document.getElementById("ht-dong-ho-text");
    if (elDongHoText) {
        const dd = hienTai.getDate().toString().padStart(2, "0");
        const mm = (hienTai.getMonth() + 1).toString().padStart(2, "0");
        const yyyy = hienTai.getFullYear();
        const hh = hienTai.getHours().toString().padStart(2, "0");
        const mi = hienTai.getMinutes().toString().padStart(2, "0");
        const ss = hienTai.getSeconds().toString().padStart(2, "0");
        elDongHoText.textContent = `${dd}/${mm}/${yyyy} (${hh}:${mi}:${ss})`;
    }

    // 4 ô lớn: Ngày - Giờ - Phút - Giây đếm ngược đến Tết Trung Thu (15/08 Âm lịch)
    const mocDich = tinhMocTrungThuKeTiep();
    const tongGiay = Math.floor(Math.max(0, mocDich - hienTai.getTime()) / 1000);
    const soNgay = Math.floor(tongGiay / 86400);
    const soGio = Math.floor((tongGiay % 86400) / 3600);
    const soPhut = Math.floor((tongGiay % 3600) / 60);
    const soGiay = tongGiay % 60;

    elNgay.textContent = soNgay.toString().padStart(2, "0");
    elGio.textContent = soGio.toString().padStart(2, "0");
    elPhut.textContent = soPhut.toString().padStart(2, "0");
    elGiay.textContent = soGiay.toString().padStart(2, "0");
    return true;
}

function khoiTaoDongHoDemNguocTrungThu() {
    if (!capNhatDongHoDemNguocTrungThu()) return;
    if (boHenGioDemNguocTrungThu) {
        clearInterval(boHenGioDemNguocTrungThu);
    }
    boHenGioDemNguocTrungThu = setInterval(capNhatDongHoDemNguocTrungThu, 1000);
}

function khoiTaoBannerSliderHero258() {
    const root = document.getElementById("heroBannerSlider258");
    if (!root || root.dataset.initialized === "1") return;
    root.dataset.initialized = "1";

    let danhSachSlide = [];
    try {
        danhSachSlide = JSON.parse(root.dataset.slides || "[]");
    } catch {
        danhSachSlide = [];
    }
    if (!Array.isArray(danhSachSlide) || danhSachSlide.length === 0) return;

    const imgEl = document.getElementById("u258Image");
    const pgEl = document.getElementById("u258Progress");
    const cpEl = document.getElementById("u258Caption");
    const cpIdxEl = document.getElementById("u258CaptionIndex");
    const cpTitleEl = document.getElementById("u258CaptionTitle");
    const badgeEl = document.getElementById("heroSlideBadge258");
    const descEl = document.getElementById("u258SlideDesc");
    const dotsWrap = document.getElementById("u258Dots");
    const btnPrev = document.getElementById("btnPrevSlide258");
    const btnNext = document.getElementById("btnNextSlide258");
    if (!imgEl) return;

    const giamChuyenDong = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    let chiSoHienTai = 0;
    let timerTuDong = null;
    let timerBlurUp = null;
    let phienTai = 0;

    function capNhatDotActive(idx) {
        if (!dotsWrap) return;
        dotsWrap.querySelectorAll(".u258-dot").forEach((dot, i) => {
            dot.classList.toggle("active", i === idx);
            dot.setAttribute("aria-selected", i === idx ? "true" : "false");
        });
    }

    function chayHieuUngBlurUp(slide) {
        phienTai += 1;
        const phienHienTai = phienTai;

        if (timerBlurUp) {
            clearTimeout(timerBlurUp);
            timerBlurUp = null;
        }

        imgEl.classList.remove("is-loaded");
        if (cpEl) cpEl.classList.remove("is-loaded");
        if (pgEl) {
            pgEl.classList.remove("is-running", "is-done");
            void pgEl.offsetWidth;
            pgEl.classList.add("is-running");
        }

        if (badgeEl) badgeEl.textContent = slide.nhan || "";
        if (descEl) descEl.textContent = slide.moTa || "";
        if (cpIdxEl) cpIdxEl.textContent = `${String(chiSoHienTai + 1).padStart(2, "0")} / ${String(danhSachSlide.length).padStart(2, "0")}`;
        if (cpTitleEl) cpTitleEl.textContent = slide.tieuDe || "";

        imgEl.setAttribute("data-fit", slide.fit || "cover");
        imgEl.alt = slide.tieuDe || "Lễ hộp Trung Thu Nguyệt Cát";

        const hoanTatHienAnh = () => {
            if (phienHienTai !== phienTai) return;
            imgEl.classList.add("is-loaded");
            if (cpEl) cpEl.classList.add("is-loaded");
            if (pgEl) {
                pgEl.classList.remove("is-running");
                pgEl.classList.add("is-done");
            }
        };

        const treBlurMs = giamChuyenDong ? 20 : 280;
        let anhDaTai = false;
        let hetTreToiThieu = false;

        const kiemTraHien = () => {
            if (anhDaTai && hetTreToiThieu) {
                hoanTatHienAnh();
            }
        };

        imgEl.onload = () => {
            anhDaTai = true;
            kiemTraHien();
        };
        imgEl.onerror = () => {
            anhDaTai = true;
            kiemTraHien();
        };

        imgEl.src = slide.anh;
        if (imgEl.complete && imgEl.naturalWidth > 0) {
            anhDaTai = true;
        }

        timerBlurUp = setTimeout(() => {
            hetTreToiThieu = true;
            kiemTraHien();
        }, treBlurMs);
    }

    function hienSlide(chiSoMoi) {
        const tong = danhSachSlide.length;
        chiSoHienTai = ((chiSoMoi % tong) + tong) % tong;
        capNhatDotActive(chiSoHienTai);
        chayHieuUngBlurUp(danhSachSlide[chiSoHienTai]);
    }

    function datLaiAutoSlide() {
        if (timerTuDong) clearInterval(timerTuDong);
        timerTuDong = setInterval(() => {
            hienSlide(chiSoHienTai + 1);
        }, 4500);
    }

    if (btnPrev) {
        btnPrev.addEventListener("click", () => {
            hienSlide(chiSoHienTai - 1);
            datLaiAutoSlide();
        });
    }

    if (btnNext) {
        btnNext.addEventListener("click", () => {
            hienSlide(chiSoHienTai + 1);
            datLaiAutoSlide();
        });
    }

    if (dotsWrap) {
        dotsWrap.querySelectorAll(".u258-dot").forEach(dot => {
            dot.addEventListener("click", () => {
                const idx = parseInt(dot.getAttribute("data-index") || "0", 10);
                hienSlide(idx);
                datLaiAutoSlide();
            });
        });
    }

    root.addEventListener("mouseenter", () => {
        if (timerTuDong) {
            clearInterval(timerTuDong);
            timerTuDong = null;
        }
    });

    root.addEventListener("mouseleave", () => {
        datLaiAutoSlide();
    });

    hienSlide(0);
    datLaiAutoSlide();
}

document.addEventListener("DOMContentLoaded", function () {
    khoiTaoBoMayChuyenCanhRoiRac();
    khoiTaoDongHoDemNguocTrungThu();
    khoiTaoBannerSliderHero258();
});
