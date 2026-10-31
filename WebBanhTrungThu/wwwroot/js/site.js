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

document.addEventListener("DOMContentLoaded", function () {
    renderKhayHop();
    khoiChayDongHoDemNguoc();
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

function xacNhanThemHopTuChon() {
    const tenHop = document.getElementById("txtTenHopTuDat").value;
    alert(`Đã ghi nhận "${tenHop}" với cấu hình ${document.getElementById("lblMaBamCauHinh").innerText}!\n(Sang Giai đoạn 3 chúng ta sẽ nối nút này lưu thẳng vào bảng HopQuaTuChon & ChiTietGioHang).`);
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