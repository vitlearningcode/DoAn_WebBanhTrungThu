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
    khoiTaoTrinhLatTangBanhXe();
    khoiTaoLienKetNeoMuot();
});

// =========================================================================
// 1A. ĐIỀU HƯỚNG PHÂN KHU MƯỢT MÀ (SAO.AGU.EDU.VN) & MENU DROPDOWN (BUM97RACING.VN)
// =========================================================================
// Danh sách 5 phân khu chính có chấm điều hướng bên phải
const DANH_SACH_PHAN_KHU = [
    "#tang-hero",
    "#tang-tu-chon-hop",
    "#tang-bo-suu-tap",
    "#tang-b2b",
    "#tang-thiep-qr"
];

// Danh sách các tầng mục tiêu cho thao tác lật phân khu bằng bánh xe chuột
const DANH_SACH_TANG_LAT_TRANG = [
    "tang-hero",
    "tang-tu-chon-hop",
    "tang-bo-suu-tap",
    "tang-b2b",
    "tang-thiep-qr",
    "footer"
];

const THOI_GIAN_NGHI_SAU_CUON = 150; // Thời gian hạ nhiệt sau khi hoàn tất cuộn (150ms cooldown)
let dangCuonChuyenTang = false;
let thoiGianKetThucCuon = 0;
let maKhungHinhCuon = 0;

// Hàm nội suy mượt mà bậc ba: easeInOutCubic(t)
function hamEmDiuBacBa(t) {
    return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
}

// Kiểm tra xem người dùng có bật chế độ giảm chuyển động trong hệ điều hành không
function kiemTraGiamChuyenDong() {
    return !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
}

// Lấy chiều cao thực tế của thanh menu cố định phía trên
function layChieuCaoThanhMenu() {
    const thanhMenu = document.querySelector(".navbar-kinh-mo");
    return thanhMenu ? thanhMenu.offsetHeight : 72;
}

// Lấy giới hạn cuộn tối đa của tài liệu
function layGioiHanCuonToiDa() {
    return Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
}

// Hàm cuộn trang mượt mà 60fps bằng requestAnimationFrame (sử dụng easeInOutCubic)
function cuonMuotDenViTri(viTriDichY, thoiGianChay = 700) {
    const viTriBatDau = window.scrollY;
    const viTriToiDa = layGioiHanCuonToiDa();
    const viTriDich = Math.min(Math.max(0, Math.round(viTriDichY)), viTriToiDa);
    const quangDuong = viTriDich - viTriBatDau;

    if (maKhungHinhCuon) {
        cancelAnimationFrame(maKhungHinhCuon);
        maKhungHinhCuon = 0;
    }

    if (Math.abs(quangDuong) < 2 || thoiGianChay <= 0 || kiemTraGiamChuyenDong()) {
        window.scrollTo(0, viTriDich);
        dangCuonChuyenTang = false;
        thoiGianKetThucCuon = Date.now();
        return;
    }

    dangCuonChuyenTang = true;
    const thoiDiemBatDau = performance.now();

    function buocKhungHinh(thoiDiemHienTai) {
        const thoiGianTroiQua = thoiDiemHienTai - thoiDiemBatDau;
        const tienDo = Math.min(1, thoiGianTroiQua / thoiGianChay);
        const heSoEmDiu = hamEmDiuBacBa(tienDo);

        window.scrollTo(0, viTriBatDau + quangDuong * heSoEmDiu);

        if (tienDo < 1) {
            maKhungHinhCuon = requestAnimationFrame(buocKhungHinh);
        } else {
            maKhungHinhCuon = 0;
            thoiGianKetThucCuon = Date.now();
            setTimeout(() => {
                if (!maKhungHinhCuon) {
                    dangCuonChuyenTang = false;
                }
            }, THOI_GIAN_NGHI_SAU_CUON);
        }
    }

    maKhungHinhCuon = requestAnimationFrame(buocKhungHinh);
}

// Cuộn trang mượt mà đến vị trí đỉnh của một phần tử cụ thể (có trừ khoảng đệm menu sticky)
function cuonMuotDenPhanTu(phanTu, thoiGianChay = 700) {
    if (!phanTu) return;
    const chieuCaoHeader = layChieuCaoThanhMenu();
    const viTriDich = phanTu.getBoundingClientRect().top + window.scrollY - chieuCaoHeader;
    cuonMuotDenViTri(viTriDich, thoiGianChay);

    if (phanTu.id) {
        capNhatChamTronActive("#" + phanTu.id);
    }
    const khoiHienDan = phanTu.querySelector(".section-reveal");
    if (khoiHienDan) {
        khoiHienDan.classList.add("active");
    }
}

// Hàm kích hoạt điều hướng phân khu từ các nút chấm tròn bên phải hoặc thẻ liên kết
function cuonDenPhanKhu(selector, e) {
    const mucTieu = document.querySelector(selector);
    if (!mucTieu) {
        return true; // Cho phép điều hướng href mặc định nếu đang ở trang con khác
    }
    if (e && typeof e.preventDefault === "function") {
        e.preventDefault();
    }

    cuonMuotDenPhanTu(mucTieu);
    return false;
}

// Cập nhật trạng thái sáng chấm tròn dọc bên phải (.side-dot-btn.active)
function capNhatChamTronActive(selector) {
    document.querySelectorAll(".side-dot-btn").forEach(btn => {
        btn.classList.toggle("active", btn.getAttribute("data-target") === selector);
    });
}

// Lấy danh sách các phần tử tầng trang chủ hiện có trong DOM
function layDanhSachSectionTrangChu() {
    return DANH_SACH_PHAN_KHU
        .map(id => document.querySelector(id))
        .filter(el => el !== null);
}

// Lấy danh sách các tầng mục tiêu để cuộn lật phân khu theo bánh xe chuột
function layDanhSachTangLatTrang() {
    return DANH_SACH_TANG_LAT_TRANG
        .map(id => id === "footer" ? (document.getElementById("footer") || document.querySelector("footer")) : document.getElementById(id))
        .filter(el => el !== null);
}

// Khởi tạo IntersectionObserver: chỉ bật sáng chấm tròn active và trượt nhẹ .section-reveal.active
function khoiTaoSectionScrollObserver() {
    const cacSection = layDanhSachSectionTrangChu();
    if (cacSection.length === 0 || !("IntersectionObserver" in window)) {
        cacSection.forEach(sec => sec.querySelector(".section-reveal")?.classList.add("active"));
        return;
    }

    // 1. Theo dõi tầng đang xuất hiện ở vùng trung tâm tầm mắt để đổi chấm active
    const boQuanSatChamTron = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                capNhatChamTronActive("#" + entry.target.id);
            }
        });
    }, {
        root: null,
        rootMargin: "-40% 0px -40% 0px",
        threshold: 0
    });

    // 2. Theo dõi tầng bắt đầu lọt vào khung nhìn để kích hoạt hiệu ứng hiện dần
    const boQuanSatHienDan = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.querySelector(".section-reveal")?.classList.add("active");
            }
        });
    }, {
        root: null,
        rootMargin: "0px 0px -10% 0px",
        threshold: 0
    });

    cacSection.forEach(sec => {
        boQuanSatChamTron.observe(sec);
        boQuanSatHienDan.observe(sec);
    });
}

// Kiểm tra con trỏ chuột có đang nằm trong một hộp cuộn nội bộ (VD: danh sách bánh) chưa chạm mép
function kiemTraHopCuonNoiBo(phanTuDich, huongXuong) {
    let nut = phanTuDich instanceof Element ? phanTuDich : null;
    while (nut && nut !== document.body && nut !== document.documentElement) {
        const phongCach = window.getComputedStyle(nut);
        const kieuTranY = phongCach.overflowY;
        const laHopCuon = (kieuTranY === "auto" || kieuTranY === "scroll" || nut.classList.contains("overflow-auto"));

        if (laHopCuon && nut.scrollHeight > nut.clientHeight + 1) {
            if (huongXuong && Math.ceil(nut.scrollTop + nut.clientHeight) < nut.scrollHeight - 1) {
                return true; // Còn cuộn xuống tiếp được bên trong hộp
            }
            if (!huongXuong && nut.scrollTop > 1) {
                return true; // Còn cuộn lên tiếp được bên trong hộp
            }
        }
        nut = nut.parentElement;
    }
    return false;
}

// Khởi tạo trình lắng nghe sự kiện lăn chuột phân khu mượt mà (style sao.agu.edu.vn)
function khoiTaoTrinhLatTangBanhXe() {
    const cacTang = layDanhSachTangLatTrang();
    if (cacTang.length < 2 || kiemTraGiamChuyenDong()) return;
    window.addEventListener("wheel", xuLyLanChuotPhanKhu, { passive: false });
}

// Xử lý sự kiện lăn bánh xe chuột phân trang thông minh
function xuLyLanChuotPhanKhu(e) {
    // 1. Kiểm tra các điều kiện bỏ qua (Bypass conditions)
    if (e.ctrlKey) return; // Người dùng đang nhấn phím Ctrl để zoom trình duyệt
    if (Math.abs(e.deltaY) < 1 || Math.abs(e.deltaY) < Math.abs(e.deltaX)) return; // Bỏ qua lăn ngang
    if (document.querySelector(".modal.show, .offcanvas.show")) return; // Đang mở Modal hoặc Drawer

    const huongXuong = e.deltaY > 0;
    if (kiemTraHopCuonNoiBo(e.target, huongXuong)) return; // Con trỏ chuột nằm trong danh sách nội bộ chưa chạm mép

    // 2. Chặn các thao tác cuộn đè trong khi đang chạy hiệu ứng hoặc trong khoảng hồi phục (150ms cooldown)
    const bayGio = Date.now();
    if (dangCuonChuyenTang || (bayGio - thoiGianKetThucCuon < THOI_GIAN_NGHI_SAU_CUON)) {
        e.preventDefault();
        return;
    }

    const cacTang = layDanhSachTangLatTrang();
    const chieuCaoHeader = layChieuCaoThanhMenu();
    const chieuCaoManHinh = window.innerHeight;
    const viTriCuonHienTai = window.scrollY;
    const duongMoc = chieuCaoHeader + 2;

    // Tìm tầng hiện tại đang hiển thị dưới thanh header
    let chiSoTang = cacTang.findIndex(tang => {
        const r = tang.getBoundingClientRect();
        return r.top <= duongMoc && r.bottom > duongMoc;
    });

    if (chiSoTang === -1) {
        // Dự phòng: chọn tầng có diện tích bao phủ lớn nhất trong màn hình
        let maxDienTich = -1;
        cacTang.forEach((tang, idx) => {
            const r = tang.getBoundingClientRect();
            const topNhin = Math.max(chieuCaoHeader, r.top);
            const botNhin = Math.min(chieuCaoManHinh, r.bottom);
            const chieuCao = botNhin - topNhin;
            if (chieuCao > maxDienTich) {
                maxDienTich = chieuCao;
                chiSoTang = idx;
            }
        });
    }

    if (chiSoTang === -1) return;

    const tangHienTai = cacTang[chiSoTang];
    const khungTang = tangHienTai.getBoundingClientRect();
    const buocCuonTrongTang = Math.round((chieuCaoManHinh - chieuCaoHeader) * 0.75);
    let viTriDich = viTriCuonHienTai;

    if (huongXuong) {
        // Cuộn XUỐNG: Nếu tầng hiện tại cao hơn viewport và đáy tầng vẫn nằm dưới đáy viewport -> cuộn tiếp bên trong tầng
        const dayTangToaDo = khungTang.bottom + viTriCuonHienTai;
        if ((khungTang.bottom - khungTang.top > chieuCaoManHinh - chieuCaoHeader) && (khungTang.bottom > chieuCaoManHinh + 5)) {
            viTriDich = Math.min(viTriCuonHienTai + buocCuonTrongTang, dayTangToaDo - chieuCaoManHinh);
        } else if (chiSoTang < cacTang.length - 1) {
            // Đã thấy đáy tầng hiện tại -> lướt mượt xuống đỉnh tầng kế tiếp
            const tangKeTiep = cacTang[chiSoTang + 1];
            viTriDich = tangKeTiep.getBoundingClientRect().top + viTriCuonHienTai - chieuCaoHeader;
        } else {
            return; // Đã ở tầng cuối cùng
        }
    } else {
        // Cuộn LÊN: Nếu đỉnh tầng hiện tại đang nằm trên sticky header -> cuộn lên đỉnh tầng hiện tại trước
        const dinhTangToaDo = khungTang.top + viTriCuonHienTai - chieuCaoHeader;
        if (khungTang.top < chieuCaoHeader - 5) {
            viTriDich = Math.max(dinhTangToaDo, viTriCuonHienTai - buocCuonTrongTang);
        } else if (chiSoTang > 0) {
            // Đang ở sát đỉnh tầng hiện tại -> lướt mượt về tầng phía trước
            const tangTruoc = cacTang[chiSoTang - 1];
            const khungTangTruoc = tangTruoc.getBoundingClientRect();
            const chieuCaoTangTruoc = khungTangTruoc.bottom - khungTangTruoc.top;

            // Nếu tầng trước cao hơn màn hình thì cuộn đến đáy của tầng trước để người dùng không bỏ lỡ nội dung
            if (chieuCaoTangTruoc > (chieuCaoManHinh - chieuCaoHeader + 5)) {
                viTriDich = khungTangTruoc.bottom + viTriCuonHienTai - chieuCaoManHinh;
            } else {
                viTriDich = khungTangTruoc.top + viTriCuonHienTai - chieuCaoHeader;
            }
        } else {
            return; // Đã ở tầng đầu tiên
        }
    }

    const viTriToiDa = layGioiHanCuonToiDa();
    const viTriDichChuan = Math.min(Math.max(0, viTriDich), viTriToiDa);

    if (Math.abs(viTriDichChuan - viTriCuonHienTai) < 2) return;

    e.preventDefault();
    cuonMuotDenViTri(viTriDichChuan, 700);
}

// Bắt sự kiện click vào tất cả các liên kết neo #tang-... để trượt mượt mà
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
        cuonMuotDenPhanTu(mucTieu);
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

    cuonMuotDenPhanTu(tangBst);
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

    cuonMuotDenPhanTu(tangBst);
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

    cuonMuotDenPhanTu(tangHop);
    return false;
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
