// =========================================================================
// MODULE 4: TƯ VẤN CHỌN VỊ BÁNH, BỘ LỌC BST, CHIẾT KHẤU SỈ B2B, TRA CỨU & TÀI KHOẢN
// =========================================================================
let boLocDinhDuongHienTai = "ALL";
let boLocThuongHieuHienTai = "ALL";
let danhSachBanhGoiYHienTai = [];

function moModalTuVanQua() {
    const modalEl = document.getElementById("modalTroLyChonBanh");
    if (modalEl && typeof bootstrap !== "undefined") {
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    }
}

function dongModalTuVanQua() {
    const modalEl = document.getElementById("modalTroLyChonBanh");
    if (modalEl && typeof bootstrap !== "undefined") {
        const modalInstance = bootstrap.Modal.getInstance(modalEl) || bootstrap.Modal.getOrCreateInstance(modalEl);
        modalInstance.hide();
    }
}

function khoiTaoTuDongThuGonNutTuVan() {
    const nutTuVan = document.getElementById("nutNoiTuVanQua") || document.querySelector(".floating-quiz-btn");
    if (!nutTuVan) return;
    setTimeout(() => {
        nutTuVan.classList.add("thu-gon-nut-tu-van");
    }, 10000);
}

// 1. BỘ LỌC BÁNH THEO HƯƠNG VỊ DINH DƯỠNG & THƯƠNG HIỆU (TẦNG 3)
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

// 2. BẢNG ƯỚC TÍNH CHIẾT KHẤU SỈ B2B TRỰC TIẾP & ĐẶT HÀNG SỈ (TẦNG 4)
function tinhGiaSiB2B(soHopNhap, tuOInputSo) {
    let soHop = parseInt(soHopNhap, 10);
    if (isNaN(soHop) || soHop < 1) soHop = 10;

    const slider = document.getElementById("sliderSoHopB2B");
    const txtSoHop = document.getElementById("txtSoHopB2BDuKien");
    if (slider) {
        slider.value = Math.min(200, Math.max(10, soHop));
    }
    if (txtSoHop && !tuOInputSo) {
        txtSoHop.value = soHop;
    }

    const lblSoHop = document.getElementById("lblSoHopB2B");
    if (lblSoHop) lblSoHop.innerText = `${soHop} Hộp`;

    let phanTram = 0;
    let mienPhiLogo = soHop >= 20;
    let phiKhuon = 400000;

    const bangB2B = Array.isArray(window.BANG_CHIET_KHAU_B2B) ? window.BANG_CHIET_KHAU_B2B : [];
    if (bangB2B.length > 0) {
        let moc = bangB2B.find(m => soHop >= m.tuHop && soHop <= m.denHop);
        if (!moc && soHop > bangB2B[bangB2B.length - 1].denHop) {
            moc = bangB2B[bangB2B.length - 1];
        }
        if (moc) {
            phanTram = Number(moc.phanTramCk || 0);
            mienPhiLogo = Boolean(moc.mienPhiInLogo);
            phiKhuon = Number(moc.phiKhuon || 400000);
        }
    } else {
        if (soHop >= 100) { phanTram = 22; mienPhiLogo = true; }
        else if (soHop >= 30) { phanTram = 15; mienPhiLogo = true; }
        else if (soHop >= 10) { phanTram = 8; }
    }

    const selCombo = document.getElementById("selComboDatHangB2B");
    const optCombo = selCombo && selCombo.selectedIndex >= 0 ? selCombo.options[selCombo.selectedIndex] : null;
    const giaTrungBinh1Hop = Number(optCombo?.dataset?.gia || 600000);
    const tienTietKiem = Math.round(soHop * giaTrungBinh1Hop * (phanTram / 100));

    const lblPhanTram = document.getElementById("lblPhanTramB2B");
    const lblLogo = document.getElementById("lblLogoB2B");
    const lblTietKiem = document.getElementById("lblTietKiemB2B");

    if (lblPhanTram) lblPhanTram.innerText = `${phanTram}%`;
    if (lblLogo) {
        lblLogo.innerText = mienPhiLogo ? "MIỄN PHÍ" : `+${phiKhuon.toLocaleString("vi-VN")}đ`;
        lblLogo.className = mienPhiLogo ? "fs-6 text-success" : "fs-6 chu-nau-tram";
    }
    if (lblTietKiem) lblTietKiem.innerText = `${tienTietKiem.toLocaleString("vi-VN")}đ`;
}

async function datHangSiB2BNgay() {
    const txtSoHop = document.getElementById("txtSoHopB2BDuKien");
    const slider = document.getElementById("sliderSoHopB2B");
    const soHop = parseInt(txtSoHop?.value || slider?.value || "35", 10);

    if (isNaN(soHop) || soHop < 10) {
        if (typeof hienThiThongBaoGioHang === "function") {
            hienThiThongBaoGioHang("Đơn sỉ Doanh nghiệp (B2B) áp dụng từ tối thiểu 10 hộp trở lên!", false);
        } else {
            alert("Đơn sỉ Doanh nghiệp (B2B) áp dụng từ tối thiểu 10 hộp trở lên!");
        }
        txtSoHop?.focus();
        return;
    }

    const selCombo = document.getElementById("selComboDatHangB2B");
    const optCombo = selCombo && selCombo.selectedIndex >= 0 ? selCombo.options[selCombo.selectedIndex] : null;
    const maSanPham = parseInt(selCombo?.value || "0", 10);
    const loaiSanPham = parseInt(optCombo?.dataset?.loai || "2", 10);

    if (!maSanPham || maSanPham <= 0) {
        window.location.href = "/ThanhToan/Index?cheDoB2B=true";
        return;
    }

    const btn = document.getElementById("btnDatHangSiB2B");
    const noiDungCu = btn ? btn.innerHTML : "";
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span>Đang đưa ${soHop} hộp vào giỏ sỉ...`;
    }

    try {
        const res = await fetch("/GioHang/ThemBanhHoacCombo", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                loaiSanPham: loaiSanPham,
                maSanPham: maSanPham,
                soLuong: soHop
            })
        });
        const data = await res.json();
        if (data && data.thanhCong) {
            window.location.href = "/ThanhToan/Index?cheDoB2B=true";
            return;
        }
        if (typeof hienThiThongBaoGioHang === "function") {
            hienThiThongBaoGioHang(data?.thongBao || "Không thể thêm đơn sỉ vào giỏ hàng!", false);
        } else {
            alert(data?.thongBao || "Không thể thêm đơn sỉ vào giỏ hàng!");
        }
    } catch (err) {
        if (typeof hienThiThongBaoGioHang === "function") {
            hienThiThongBaoGioHang("Lỗi kết nối máy chủ khi tạo giỏ hàng sỉ B2B!", false);
        } else {
            alert("Lỗi kết nối máy chủ khi tạo giỏ hàng sỉ B2B!");
        }
    } finally {
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = noiDungCu;
        }
    }
}

// 3. NGHỆ NHÂN TƯ VẤN CHỌN QUÀ HỢP Ý (WEIGHTED SCORING MATRIX)
function chuanHoaThongTinBanhGoiY(item) {
    let maBanh = parseInt(item.maBanh || item.MaBanh || item.id || 0);
    let tenBanh = item.tenBanh || "";
    let donGia = parseFloat(item.donGia || 0);
    let calo = parseInt(item.calo || 400);
    let chiSoGi = parseInt(item.chiSoGI || item.chiSoGi || 52);
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
        chiSoGi = parseInt(nutDom.dataset.gi || chiSoGi);
        laVip = nutDom.dataset.lavip === "true";
        anhSlot = nutDom.dataset.anhslot || anhSlot;
        diUng = nutDom.dataset.diung || diUng;
        thuongHieu = nutDom.dataset.thuonghieu || thuongHieu;
    }

    return { maBanh, tenBanh, donGia, calo, chiSoGi, laVip, anhSlot, diUng, thuongHieu };
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

function xepBanhGoiYVaoHop(chiSoGoiY) {
    const item = danhSachBanhGoiYHienTai[chiSoGoiY];
    if (!item) return;

    const banh = chuanHoaThongTinBanhGoiY(item);
    const tangHop = document.getElementById("tang-tu-chon-hop");
    const luoiOBanh = document.getElementById("luoiOBanh");

    if (tangHop && luoiOBanh) {
        dongModalTuVanQua();
        setTimeout(() => {
            if (typeof chuyenDenTang === "function") chuyenDenTang(1);
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

function xepCa3ViGoiYVaoHop() {
    if (!danhSachBanhGoiYHienTai || danhSachBanhGoiYHienTai.length === 0) return;

    const danhSachBanh = danhSachBanhGoiYHienTai.map(item => chuanHoaThongTinBanhGoiY(item));
    const tangHop = document.getElementById("tang-tu-chon-hop");
    const luoiOBanh = document.getElementById("luoiOBanh");

    if (tangHop && luoiOBanh) {
        dongModalTuVanQua();
        setTimeout(() => {
            if (typeof chuyenDenTang === "function") chuyenDenTang(1);
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

// 4. TRA CỨU ĐƠN HÀNG NHANH CHO KHÁCH VÃNG LAI (GUEST TRACKING)
function khoiTaoLichSuTraCuuDonNhanh() {
    hienThiDanhSachDonVuaDatNhanh();
    const modalTraCuu = document.getElementById("modalTraCuuDon");
    if (modalTraCuu) {
        modalTraCuu.addEventListener("show.bs.modal", function () {
            hienThiDanhSachDonVuaDatNhanh();
        });
    }
}

function hienThiDanhSachDonVuaDatNhanh() {
    const khungNhanh = document.getElementById("danh-sach-don-vua-dat-nhanh");
    if (!khungNhanh) return;

    let danhSachDon = [];
    try {
        const chuoiLuu = localStorage.getItem("nguyetCat_DonGanDay");
        if (chuoiLuu) {
            const parsed = JSON.parse(chuoiLuu);
            if (Array.isArray(parsed)) {
                danhSachDon = parsed.filter(d => d && d.maTraCuuDh && d.sdtNguoiMua).slice(0, 5);
            }
        }
    } catch (err) {
        danhSachDon = [];
    }

    if (danhSachDon.length === 0) {
        khungNhanh.innerHTML = "";
        khungNhanh.classList.add("d-none");
        return;
    }

    khungNhanh.classList.remove("d-none");
    let nutChipsHtml = "";
    danhSachDon.forEach(don => {
        const maDonAnToan = String(don.maTraCuuDh).replace(/'/g, "");
        const sdtAnToan = String(don.sdtNguoiMua).replace(/'/g, "");
        const soTienText = typeof don.tongThanhToan === "number"
            ? `${Math.round(don.tongThanhToan).toLocaleString("vi-VN")}đ`
            : String(don.tongThanhToan || "");

        nutChipsHtml += `
            <button type="button" class="chip-don-vua-dat"
                    onclick="chonDonVuaDatNhanh('${maDonAnToan}', '${sdtAnToan}')">
                <span class="fw-bold">#${maDonAnToan}</span>
                ${soTienText ? `<span class="opacity-75">• ${soTienText}</span>` : ""}
            </button>
        `;
    });

    khungNhanh.innerHTML = `
        <div class="chu-nau-phu mb-1" style="font-size: 0.76rem; font-weight: 600;">
            Đơn vừa đặt trên thiết bị của bạn (chạm để tra cứu ngay):
        </div>
        <div class="d-flex flex-wrap gap-1">
            ${nutChipsHtml}
        </div>
    `;
}

function chonDonVuaDatNhanh(maTraCuu, sdtNguoiMua) {
    const oMa = document.getElementById("txtMaTraCuu");
    const oSdt = document.getElementById("txtSdtTraCuu");
    if (oMa) oMa.value = maTraCuu;
    if (oSdt) oSdt.value = sdtNguoiMua;
    traCuuDonHangGuest();
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

// 5. ĐĂNG NHẬP, ĐĂNG KÝ HỘI VIÊN & CHUYỂN TAB HỒ SƠ
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

function chuyenTabHoSo(nutTab) {
    if (!nutTab) return;
    const boChon = nutTab.getAttribute("data-muc-tieu");
    const paneDich = boChon ? document.querySelector(boChon) : null;
    if (!paneDich || nutTab.classList.contains("active")) return;

    document.querySelectorAll(".nut-tab-ho-so").forEach(btn => {
        const laNutDich = btn === nutTab;
        btn.classList.toggle("active", laNutDich);
        btn.setAttribute("aria-selected", laNutDich ? "true" : "false");
    });

    document.querySelectorAll(".pane-ho-so").forEach(pane => pane.classList.remove("dang-hien"));
    void paneDich.offsetWidth;
    paneDich.classList.add("dang-hien");

    const maTab = nutTab.getAttribute("data-tab-ho-so");
    if (maTab && window.history && typeof window.history.replaceState === "function") {
        const url = new URL(window.location.href);
        url.searchParams.set("tab", maTab);
        window.history.replaceState({}, document.title, url.pathname + url.search + url.hash);
    }

    if (window.innerWidth < 992) {
        const chieuCaoMenu = typeof layChieuCaoThanhMenu === "function" ? layChieuCaoThanhMenu() : 68;
        window.scrollTo({ top: Math.max(0, paneDich.getBoundingClientRect().top + window.scrollY - chieuCaoMenu - 12), behavior: "smooth" });
    }
}

document.addEventListener("keydown", function (e) {
    const nutDangFocus = document.activeElement;
    if (!nutDangFocus || !nutDangFocus.classList || !nutDangFocus.classList.contains("nut-tab-ho-so")) return;
    if (e.key !== "ArrowDown" && e.key !== "ArrowUp") return;

    const danhSachNut = Array.from(document.querySelectorAll(".nut-tab-ho-so"));
    const viTri = danhSachNut.indexOf(nutDangFocus);
    if (viTri === -1) return;
    e.preventDefault();

    const viTriMoi = e.key === "ArrowDown"
        ? (viTri + 1) % danhSachNut.length
        : (viTri - 1 + danhSachNut.length) % danhSachNut.length;
    danhSachNut[viTriMoi].focus();
    chuyenTabHoSo(danhSachNut[viTriMoi]);
});

function khoiChayDongHoDemNguoc() {
    if (typeof khoiTaoDongHoDemNguocTrungThu === "function") {
        khoiTaoDongHoDemNguocTrungThu();
        return;
    }

    const elNgay = document.getElementById("cd-ngay");
    const elGio = document.getElementById("cd-gio");
    const elPhut = document.getElementById("cd-phut");
    const elGiay = document.getElementById("cd-giay");
    if (!elNgay || !elGio || !elPhut || !elGiay) return;

    const capNhat = () => {
        const bayGio = Date.now();
        const cacMoc = [
            new Date(2026, 8, 25, 20, 0, 0).getTime(),
            new Date(2027, 8, 15, 20, 0, 0).getTime(),
            new Date(2028, 9, 3, 20, 0, 0).getTime()
        ];
        const mocDich = cacMoc.find(m => m > bayGio) || new Date(new Date().getFullYear() + 1, 8, 15, 20, 0, 0).getTime();
        const tongGiay = Math.floor(Math.max(0, mocDich - bayGio) / 1000);
        elNgay.textContent = Math.floor(tongGiay / 86400).toString().padStart(2, "0");
        elGio.textContent = Math.floor((tongGiay % 86400) / 3600).toString().padStart(2, "0");
        elPhut.textContent = Math.floor((tongGiay % 3600) / 60).toString().padStart(2, "0");
        elGiay.textContent = (tongGiay % 60).toString().padStart(2, "0");
    };

    capNhat();
    setInterval(capNhat, 1000);
}

function chuanHoaChuoiCaptchaNhap(chuoi) {
    if (!chuoi) return "";
    return String(chuoi).replace(/[^a-zA-Z0-9]/g, "").toUpperCase().slice(0, 5);
}
