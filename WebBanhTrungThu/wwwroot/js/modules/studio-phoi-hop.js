// =========================================================================
// MODULE 2: STUDIO TỰ PHỐI LỄ HỘP QUÀ TẶNG 4/6 Ô & THANH ĐO DINH DƯỠNG GI
// =========================================================================
let cauHinhHop = {
    maVH: 1,
    tenVoHop: "Hộp Tứ Quý Đoàn Viên (4 Bánh)",
    sucChua: 4,
    giaVo: 85000,
    minVipRequired: 0,
    kieuLuoi: "grid-2x2",
    cacO: [null, null, null, null]
};

let viTriOVuaChon = -1;

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

    let mangMoi = new Array(cauHinhHop.sucChua).fill(null);
    for (let i = 0; i < Math.min(cauHinhHop.cacO.length, cauHinhHop.sucChua); i++) {
        mangMoi[i] = cauHinhHop.cacO[i];
    }
    cauHinhHop.cacO = mangMoi;
    renderKhayHop();
}

function layChiSoGiTheoMaBanh(maBanh, giTruyenVao) {
    if (typeof giTruyenVao === "number" && !isNaN(giTruyenVao) && giTruyenVao > 0) {
        return giTruyenVao;
    }
    const nutDom = document.querySelector(`.btn-chon-banh-khay[data-mabanh="${maBanh}"]`);
    if (nutDom && nutDom.dataset.gi) {
        const giDom = parseInt(nutDom.dataset.gi, 10);
        if (!isNaN(giDom) && giDom > 0) return giDom;
    }
    return 52;
}

function datBanhVaoOTrong(maBanh, tenBanh, donGia, calo, laVip, anhSlot, diUng, thuongHieu = "", chiSoGi = null) {
    const viTriTrong = cauHinhHop.cacO.findIndex(o => o === null);
    if (viTriTrong === -1) {
        hienThiThongBaoGioHang("Khay lễ hộp đã đầy! Bạn có thể chạm vào 1 ô trên khay để đổi vị bánh hoặc chọn mẫu hộp 6 bánh.", false);
        return;
    }

    const giChuan = layChiSoGiTheoMaBanh(maBanh, chiSoGi !== null ? parseInt(chiSoGi, 10) : null);
    viTriOVuaChon = viTriTrong;
    cauHinhHop.cacO[viTriTrong] = { maBanh, tenBanh, donGia, calo, laVip, anhSlot, diUng, thuongHieu, chiSoGi: giChuan };
    renderKhayHop();
}

function xoaBanhKhoiO(index) {
    viTriOVuaChon = -1;
    cauHinhHop.cacO[index] = null;
    renderKhayHop();
}

function lamTrongKhayHop() {
    viTriOVuaChon = -1;
    cauHinhHop.cacO = new Array(cauHinhHop.sucChua).fill(null);
    renderKhayHop();
}

function capNhatThanhDoChiSoGi(soBanhDaXep, tongChiSoGi) {
    const thanhMauGi = document.getElementById("thanh-mau-gi");
    const nhanXetGi = document.getElementById("nhan-xet-dinh-duong-gi");
    if (!thanhMauGi || !nhanXetGi) return;

    if (soBanhDaXep <= 0) {
        thanhMauGi.style.width = "0%";
        thanhMauGi.style.backgroundColor = "#10B981";
        nhanXetGi.innerText = "Chưa có bánh trong khay";
        nhanXetGi.style.color = "var(--mau-chu-nau-phu)";
        return;
    }

    const chiSoGiTrungBinh = Math.round(tongChiSoGi / soBanhDaXep);
    if (chiSoGiTrungBinh <= 35) {
        thanhMauGi.style.width = "35%";
        thanhMauGi.style.backgroundColor = "#10B981";
        nhanXetGi.innerText = "Thanh đạm • Chuẩn ăn kiêng Isomalt (GI thấp: " + chiSoGiTrungBinh + ")";
        nhanXetGi.style.color = "#059669";
    } else if (chiSoGiTrungBinh <= 55) {
        thanhMauGi.style.width = "68%";
        thanhMauGi.style.backgroundColor = "#D97706";
        nhanXetGi.innerText = "Cân bằng • Hòa quyện vị truyền thống (GI TB: " + chiSoGiTrungBinh + ")";
        nhanXetGi.style.color = "#B45309";
    } else {
        thanhMauGi.style.width = "95%";
        thanhMauGi.style.backgroundColor = "#B91C1C";
        nhanXetGi.innerText = "Giàu năng lượng • Đậm đà thượng hạng (GI TB: " + chiSoGiTrungBinh + ")";
        nhanXetGi.style.color = "#B91C1C";
    }
}

function capNhatGiaoDienKhayBanh() {
    renderKhayHop();
}

function renderKhayHop() {
    const luoi = document.getElementById("luoiOBanh");
    if (!luoi) return;
    luoi.innerHTML = "";

    let soBanhDaXep = 0;
    let tongCalo = 0;
    let tongChiSoGi = 0;
    let soBanhVip = 0;
    let tongTienBanh = 0;
    let tapHopDiUng = new Set();
    let maBanhArr = [];

    cauHinhHop.cacO.forEach((banh, idx) => {
        const div = document.createElement("div");
        div.className = banh ? "o-banh-slot da-co-banh" : "o-banh-slot";
        if (banh && idx === viTriOVuaChon) {
            div.classList.add("o-banh-vua-chon");
        }

        if (banh) {
            soBanhDaXep++;
            tongCalo += banh.calo;
            tongChiSoGi += layChiSoGiTheoMaBanh(banh.maBanh, banh.chiSoGi);
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

    viTriOVuaChon = -1;
    capNhatThanhDoChiSoGi(soBanhDaXep, tongChiSoGi);

    const giaVoGoc = Math.round(cauHinhHop.giaVo || 0);
    const tongGiaHop = soBanhDaXep === 0 ? 0 : (cauHinhHop.giaVo * 0.95) + tongTienBanh;
    const lblChuThichGia = document.getElementById("lblChuThichGiaHop");
    if (lblChuThichGia) {
        if (soBanhDaXep === 0) {
            lblChuThichGia.innerText = `(Vỏ hộp: ${giaVoGoc.toLocaleString("vi-VN")}đ • Chưa chọn bánh)`;
        } else {
            const giaVoUuDai = Math.round(cauHinhHop.giaVo * 0.95);
            lblChuThichGia.innerText = `(Đã gồm vỏ hộp ưu đãi ${giaVoUuDai.toLocaleString("vi-VN")}đ + ${soBanhDaXep} bánh)`;
        }
    }

    document.getElementById("lblSoODaXep").innerText = `${soBanhDaXep} / ${cauHinhHop.sucChua} bánh`;
    document.getElementById("lblTongCalo").innerText = `${tongCalo.toLocaleString("vi-VN")} Kcal`;
    document.getElementById("lblSoBanhVip").innerText = `${soBanhVip} bánh`;
    document.getElementById("lblCanhBaoDiUng").innerText = tapHopDiUng.size > 0 ? Array.from(tapHopDiUng).join("; ") : "An toàn";
    document.getElementById("lblMaBamCauHinh").innerText = `Loại hộp: ${cauHinhHop.sucChua} bánh • ${tongCalo.toLocaleString("vi-VN")} Kcal`;
    document.getElementById("lblTongGiaHop").innerText = `${Math.round(tongGiaHop).toLocaleString("vi-VN")} đ`;

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
        if (typeof chuyenDenTang === "function") chuyenDenTang(1);
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

        params.delete("chonBanh");
        const queryMoi = params.toString();
        const urlMoi = window.location.pathname + (queryMoi ? "?" + queryMoi : "") + window.location.hash;
        window.history.replaceState({}, document.title, urlMoi);
    }, 220);
}
