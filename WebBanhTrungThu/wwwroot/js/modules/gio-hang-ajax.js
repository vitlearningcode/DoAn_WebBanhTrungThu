// =========================================================================
// MODULE 3: GIỎ HÀNG AJAX, BẢNG 2 CỘT (Ô 1..6 | TÊN BÁNH), TỒN KHO & VOUCHER
// =========================================================================
let gioHangDuTonKhoOffcanvas = true;

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
        <div class="hang-nhan-ma-uu-dai" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 6px; margin-bottom: 6px;">
            <label for="txtMaVoucherGioHang" class="small fw-bold chu-do-son-mai mb-0 text-nowrap">
                <i class="bi bi-ticket-perforated-fill me-1"></i>Mã ưu đãi:
            </label>
            <div class="d-inline-flex align-items-center gap-1 flex-wrap">
                <button type="button" class="btn btn-sm btn-vien-vang rounded-pill py-0 px-2 text-nowrap" style="font-size: 0.72rem;"
                        onclick="chonNhanhVoucher('TRANGTRON2026')">
                    Áp mã TRANGTRON2026 (-10%)
                </button>
                <span id="lblHsdVoucherGioHang" class="badge rounded-pill px-2 py-0" style="background: #FEF3C7; color: #92400E; font-size: 0.65rem; font-weight: 600;">
                    HSD: 31/12/2027 • Đơn từ 200k
                </span>
            </div>
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
        const lblHsdVoucher = document.getElementById("lblHsdVoucherGioHang");
        if (lblHsdVoucher && data.thongTinVoucherGoiY) {
            lblHsdVoucher.innerText = data.thongTinVoucherGoiY;
        }
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
                <div class="p-3 mb-2 rounded card-hoang-gia" data-ma-ctgh="${item.maCtgh}">
                    <div class="d-flex justify-content-between align-items-start gap-2">
                        <strong class="chu-do-son-mai">${item.tenMon}</strong>
                        <button type="button" class="btn btn-sm btn-outline-danger py-0 px-2"
                                onclick="xoaMonKhoiGio(${item.maCtgh})" title="Xóa món này">
                            🗑️
                        </button>
                    </div>
                    ${thongTinPhu}
                    <div class="canh-bao-ton-kho-mon text-danger small fw-semibold mt-1 d-none" style="font-size: 0.75rem;"></div>
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
        await kiemTraTonKhoThucTeOffcanvas();
    } catch (err) {
        console.error("Không thể đồng bộ giỏ quà:", err);
    }
}

function khoiTaoSuKienKiemTraKhoOffcanvas() {
    const elOffcanvas = document.getElementById("nganKeoGioHang");
    if (!elOffcanvas) return;
    elOffcanvas.addEventListener("show.bs.offcanvas", function () {
        kiemTraTonKhoThucTeOffcanvas();
    });
}

async function kiemTraTonKhoThucTeOffcanvas() {
    try {
        const res = await fetch("/GioHang/KiemTraTonKhoThucTe");
        const data = await res.json();
        if (!data || !data.thanhCong) return true;

        gioHangDuTonKhoOffcanvas = Boolean(data.hopLe);

        document.querySelectorAll("#danhSachMonTrongGio [data-ma-ctgh]").forEach(card => {
            const boxLoi = card.querySelector(".canh-bao-ton-kho-mon");
            if (boxLoi) {
                boxLoi.innerHTML = "";
                boxLoi.classList.add("d-none");
            }
            card.style.borderColor = "";
        });

        if (data.danhSachLoiTheoMon && typeof data.danhSachLoiTheoMon === "object") {
            Object.entries(data.danhSachLoiTheoMon).forEach(([maCtgh, thongBaoLoi]) => {
                const card = document.querySelector(`#danhSachMonTrongGio [data-ma-ctgh="${maCtgh}"]`);
                if (card && thongBaoLoi) {
                    card.style.borderColor = "#DC2626";
                    const boxLoi = card.querySelector(".canh-bao-ton-kho-mon");
                    if (boxLoi) {
                        boxLoi.innerHTML = `<i class="bi bi-exclamation-triangle-fill me-1"></i>${thongBaoLoi}`;
                        boxLoi.classList.remove("d-none");
                    }
                }
            });
        }

        const hopCanhBaoChung = document.getElementById("canhBaoTonKhoOffcanvas");
        const btnThanhToan = document.getElementById("btnTienHanhThanhToanOffcanvas");

        if (!gioHangDuTonKhoOffcanvas) {
            if (hopCanhBaoChung) {
                hopCanhBaoChung.innerHTML = `<i class="bi bi-exclamation-octagon-fill me-1"></i>Một số sản phẩm trong giỏ vượt quá tồn kho thực tế còn hạn. Vui lòng giảm số lượng trước khi thanh toán!`;
                hopCanhBaoChung.classList.remove("d-none");
            }
            if (btnThanhToan) {
                btnThanhToan.classList.add("disabled");
                btnThanhToan.style.pointerEvents = "none";
                btnThanhToan.style.opacity = "0.55";
            }
        } else {
            if (hopCanhBaoChung) {
                hopCanhBaoChung.innerHTML = "";
                hopCanhBaoChung.classList.add("d-none");
            }
            if (btnThanhToan) {
                btnThanhToan.classList.remove("disabled");
                btnThanhToan.style.pointerEvents = "auto";
                btnThanhToan.style.opacity = "1";
            }
        }

        return gioHangDuTonKhoOffcanvas;
    } catch (err) {
        return true;
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
    if (!gioHangDuTonKhoOffcanvas) {
        if (e) e.preventDefault();
        hienThiThongBaoGioHang("Có sản phẩm trong giỏ vượt quá tồn kho thực tế! Vui lòng giảm số lượng trước khi sang trang Thanh toán.", false);
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

async function datLaiDonHangCu(maDonHang, nutBam = null) {
    const noiDungCu = nutBam ? nutBam.innerHTML : "";
    if (nutBam) {
        nutBam.disabled = true;
        nutBam.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span>Đang thêm...`;
    }

    try {
        const res = await fetch("/GioHang/DatLaiDonHang", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(parseInt(maDonHang, 10))
        });
        const data = await res.json();
        if (data && data.thanhCong) {
            hienThiThongBaoGioHang(data.thongBao || "Đã thêm toàn bộ sản phẩm của đơn cũ vào giỏ quà!", true);
            await taiLaiGioHangOffcanvas();
            moNganKeoGioHang();
        } else {
            hienThiThongBaoGioHang((data && data.thongBao) || "Không thể đặt lại đơn hàng này!", false);
        }
    } catch (err) {
        hienThiThongBaoGioHang("Lỗi kết nối máy chủ khi đặt lại đơn hàng!", false);
    } finally {
        if (nutBam) {
            nutBam.disabled = false;
            nutBam.innerHTML = noiDungCu;
        }
    }
}
