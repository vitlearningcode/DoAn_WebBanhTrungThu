// =========================================================================
// 1. BIẾN TRẠNG THÁI CHO TRÌNH DỰNG HỘP QUÀ TỰ CHỌN (TẦNG 3)
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

// Xử lý toàn cục khi ảnh bị lỗi tải (ví dụ link Google Drive giả lập bị lỗi) -> tự động thay bằng ảnh fallback-lava.png
window.addEventListener('error', function (e) {
    if (e.target && e.target.tagName === 'IMG') {
        if (!e.target.src.includes('/images/banh/fallback-lava.png')) {
            e.target.src = '/images/banh/fallback-lava.png';
        }
    }
}, true);

document.addEventListener("DOMContentLoaded", function () {
    renderKhayHop();
    khoiChayDongHoDemNguoc();
    taiLaiGioHangOffcanvas();
});

function doiMauVoHop(radio) {
    cauHinhHop.maVH = parseInt(radio.value);
    cauHinhHop.tenVoHop = radio.dataset.ten;
    cauHinhHop.sucChua = parseInt(radio.dataset.succhua);
    cauHinhHop.giaVo = parseFloat(radio.dataset.giavo);
    cauHinhHop.minVipRequired = parseInt(radio.dataset.minvip);
    cauHinhHop.kieuLuoi = radio.dataset.luoi;

    const khungKhay = document.getElementById("khungKhayHop");
    khungKhay.style.backgroundImage = `linear-gradient(rgba(11,19,43,0.65), rgba(11,19,43,0.65)), url('${radio.dataset.anhkhay}')`;

    const luoi = document.getElementById("luoiOBanh");
    luoi.className = cauHinhHop.kieuLuoi === "grid-3x2" ? "luoi-hop-3x2 w-100" : "luoi-hop-2x2 w-100";

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
        alert("Khay hộp đã đầy! Bạn có thể bấm vào 1 ô bất kỳ trên khay để đổi bánh khác hoặc chọn vỏ hộp 6 bánh.");
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
            div.title = "Bấm để bỏ bánh này ra khỏi ô";
            div.innerHTML = `
                <img src="${banh.anhSlot}" alt="${banh.tenBanh}" />
                <span class="badge bg-dark border border-warning text-warning mt-1 text-truncate" style="max-width:95%; font-size:0.68rem;">
                    Ô ${idx + 1}: ${banh.tenBanh}
                </span>
            `;
        } else {
            maBanhArr.push("0");
            div.innerHTML = `
                <i class="bi bi-plus-circle-dotted fs-3 text-warning opacity-50"></i>
                <small class="text-secondary mt-1">Ô số ${idx + 1}</small>
            `;
        }
        luoi.appendChild(div);
    });

    // Tính giá theo công thức Giai đoạn 1: Vỏ hộp giảm 5% khi tự phối + Tổng giá bánh lẻ
    let tongGiaHop = (cauHinhHop.giaVo * 0.95) + tongTienBanh;
    let maBam = `VH${cauHinhHop.maVH}_${maBanhArr.join("-")}`;

    document.getElementById("lblSoODaXep").innerText = `${soBanhDaXep} / ${cauHinhHop.sucChua} bánh`;
    document.getElementById("lblTongCalo").innerText = `${tongCalo.toLocaleString("vi-VN")} Kcal`;
    document.getElementById("lblSoBanhVip").innerText = `${soBanhVip} bánh`;
    document.getElementById("lblCanhBaoDiUng").innerText = tapHopDiUng.size > 0 ? Array.from(tapHopDiUng).join("; ") : "An toàn";
    document.getElementById("lblMaBamCauHinh").innerText = `Mã cấu hình: #${maBam}`;
    document.getElementById("lblTongGiaHop").innerText = `${Math.round(tongGiaHop).toLocaleString("vi-VN")} đ`;

    // Kiểm tra điều kiện hợp lệ (IsValidBox)
    const btnThem = document.getElementById("btnThemHopVaoGio");
    if (soBanhDaXep < cauHinhHop.sucChua) {
        btnThem.disabled = true;
        btnThem.innerText = `🔒 Hãy chọn thêm ${cauHinhHop.sucChua - soBanhDaXep} bánh nữa để đóng hộp`;
    } else if (soBanhVip < cauHinhHop.minVipRequired) {
        btnThem.disabled = true;
        btnThem.innerText = `⚠️ Vỏ hộp VIP này yêu cầu tối thiểu ${cauHinhHop.minVipRequired} bánh Thượng Hạng`;
    } else {
        btnThem.disabled = false;
        btnThem.innerText = `🎁 Thêm Hộp Tự Phối Này Vào Giỏ Quà (${Math.round(tongGiaHop).toLocaleString("vi-VN")} đ)`;
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
        btnThem.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu cấu hình hộp quà...`;
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
            hienThiThongBaoGioHang(data.thongBao || "Không thể thêm hộp quà tự chọn!", false);
        }
    } catch (err) {
        hienThiThongBaoGioHang("Lỗi kết nối máy chủ khi lưu hộp quà tự chọn!", false);
    } finally {
        if (btnThem) {
            btnThem.disabled = false;
            btnThem.innerHTML = noiDungCu;
            renderKhayHop();
        }
    }
}

// =========================================================================
// 1B. CÁC HÀM AJAX GIỎ HÀNG KÉP (GUEST & MEMBER) & OFFCANVAS REALTIME
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
        hienThiThongBaoGioHang("Lỗi kết nối khi thêm bánh lẻ vào giỏ hàng!", false);
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
        hienThiThongBaoGioHang("Lỗi kết nối khi xóa món khỏi giỏ hàng!", false);
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
            khungDanhSach.innerHTML = `<div class="text-center text-secondary py-5">Giỏ quà đang trống. Hãy tự phối hộp bánh ở Tầng 3!</div>`;
            return;
        }

        let html = "";
        data.danhSachMon.forEach(item => {
            let thongTinPhu = "";
            if (item.loaiSanPham === 3) {
                thongTinPhu += `<small class="text-info d-block mt-1">Mã cấu hình: ${item.maBamCauHinh} (${item.tongCalo.toLocaleString("vi-VN")} Kcal)</small>`;
                if (item.danhSachViBanh && item.danhSachViBanh.length > 0) {
                    thongTinPhu += `<div class="small text-secondary mt-1" style="font-size: 0.75rem;">${item.danhSachViBanh.join(" • ")}</div>`;
                }
            } else if (item.loaiSanPham === 1 && item.tongCalo > 0) {
                thongTinPhu += `<small class="text-secondary d-block mt-1">Bánh lẻ • ${item.tongCalo} Kcal / bánh</small>`;
            }

            html += `
                <div class="p-3 mb-2 rounded card-hoang-gia">
                    <div class="d-flex justify-content-between align-items-start gap-2">
                        <strong class="chu-vang-kim">${item.tenMon}</strong>
                        <button type="button" class="btn btn-sm btn-outline-danger py-0 px-2"
                                onclick="xoaMonKhoiGio(${item.maCtgh})" title="Xóa món này">
                            🗑️
                        </button>
                    </div>
                    ${thongTinPhu}
                    <div class="d-flex justify-content-between align-items-center mt-2 pt-2 border-top border-secondary">
                        <div class="btn-group btn-group-sm" role="group">
                            <button type="button" class="btn btn-outline-warning px-2 py-0"
                                    onclick="capNhatSoLuongMon(${item.maCtgh}, ${item.soLuong - 1})">
                                -
                            </button>
                            <span class="btn btn-dark border-warning text-light px-3 py-0 disabled">${item.soLuong}</span>
                            <button type="button" class="btn btn-outline-warning px-2 py-0"
                                    onclick="capNhatSoLuongMon(${item.maCtgh}, ${item.soLuong + 1})">
                                +
                            </button>
                        </div>
                        <div class="fw-bold text-warning">${Math.round(item.thanhTien).toLocaleString("vi-VN")} đ</div>
                    </div>
                </div>
            `;
        });

        khungDanhSach.innerHTML = html;
    } catch (err) {
        console.error("Không thể đồng bộ giỏ hàng:", err);
    }
}

function moNganKeoGioHang() {
    const elOffcanvas = document.getElementById("nganKeoGioHang");
    if (elOffcanvas && typeof bootstrap !== "undefined") {
        const bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(elOffcanvas);
        bsOffcanvas.show();
    }
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
        hienThiThongBaoGioHang("Lỗi kết nối máy chủ khi áp dụng mã giảm giá!", false);
    }
}

// =========================================================================
// 1C. ĐĂNG NHẬP, ĐĂNG KÝ COOKIE AUTH & TỰ ĐỘNG HỢP NHẤT GIỎ HÀNG GUEST -> MEMBER
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
            hienThiThongBaoGioHang(`${data.thongBao} Xin chào ${data.hoTen} (Hạng ${data.hangTv} • ${data.diem} điểm)!`, true);
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
        container.style.marginTop = "68px";
        document.body.appendChild(container);
    }

    const toastEl = document.createElement("div");
    toastEl.className = `toast align-items-center text-light bg-dark border ${thanhCong ? "border-warning" : "border-danger"} show mb-2`;
    toastEl.setAttribute("role", "alert");
    toastEl.style.boxShadow = "0 8px 24px rgba(0,0,0,0.65)";
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="bi ${thanhCong ? "bi-check-circle-fill chu-vang-kim" : "bi-exclamation-triangle-fill text-danger"} me-2"></i>
                <span>${noiDung}</span>
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    container.appendChild(toastEl);
    setTimeout(() => {
        toastEl.classList.remove("show");
        setTimeout(() => toastEl.remove(), 300);
    }, 3500);
}

// =========================================================================
// 2. BỘ LỌC BÁNH THEO CHỈ SỐ DINH DƯỠNG (TẦNG 4)
// =========================================================================
function locSanPham(loaiLoc, btn) {
    document.querySelectorAll("#tang-bo-suu-tap .btn-vien-vang").forEach(b => b.classList.remove("active"));
    btn.classList.add("active");

    document.querySelectorAll(".item-banh-card").forEach(card => {
        if (loaiLoc === "ALL") {
            card.classList.remove("d-none");
        } else if (loaiLoc === "ISOMALT") {
            card.classList.toggle("d-none", card.dataset.isomalt !== "1");
        } else if (loaiLoc === "VIP") {
            card.classList.toggle("d-none", card.dataset.vip !== "1");
        } else if (loaiLoc === "LAVA") {
            card.classList.toggle("d-none", card.dataset.lava !== "1");
        } else if (loaiLoc === "CHAY") {
            card.classList.toggle("d-none", card.dataset.chay !== "1");
        }
    });
}

// =========================================================================
// 3. MÁY TÍNH CHIẾT KHẤU SỈ B2B TRỰC TIẾP (TẦNG 5)
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
// 4. GỌI API TRỢ LÝ CHỌN QUÀ 30 GIÂY & TRA CỨU ĐƠN HÀNG GUEST
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
                <div class="p-3 rounded bg-dark border border-warning h-100 text-center">
                    <span class="badge bg-success mb-2">Độ tương thích: ${item.doPhuHop}%</span>
                    <img src="${item.anhBanh}" width="90" height="90" class="d-block mx-auto my-2 rounded-circle border border-warning" />
                    <h6 class="chu-vang-kim fw-bold">${item.tenBanh}</h6>
                    <div class="small text-info mb-1">${item.calo} Kcal • Chỉ số GI: ${item.chiSoGI}</div>
                    <p class="small text-secondary mb-2" style="font-size:0.75rem;">${item.lyDo}</p>
                    <strong class="d-block text-light">${item.donGia.toLocaleString("vi-VN")} đ / bánh</strong>
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
        box.innerHTML = `<span class="text-danger"><i class="bi bi-exclamation-triangle me-1"></i>${data.thongBao}</span>`;
    } else {
        box.innerHTML = `
            <div class="small">
                <strong class="chu-vang-kim d-block mb-1">Đơn hàng: ${data.maDon} (${data.trangThai})</strong>
                <div>Người đặt: ${data.nguoiMua} • Ngày đặt: ${data.ngayDat}</div>
                <div>Tổng thanh toán: <strong class="text-warning">${data.tongTien}</strong> (${data.soDiemGiao} điểm giao)</div>
            </div>
        `;
    }
}

function khoiChayDongHoDemNguoc() {
    let giay = 30;
    setInterval(() => {
        giay = giay > 0 ? giay - 1 : 59;
        const el = document.getElementById("cd-giay");
        if (el) el.innerText = giay.toString().padStart(2, '0');
    }, 1000);
}