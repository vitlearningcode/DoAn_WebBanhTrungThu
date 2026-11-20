// =========================================================================
// MODULE CHI TIẾT SẢN PHẨM: ĐỔI ẢNH THUMBNAIL, CHỌN SỐ LƯỢNG & MUA NGAY
// =========================================================================
function doiAnhChinhChiTietSp(nutThumb, urlAnh) {
    const imgChinh = document.getElementById("imgSanPhamChiTietChinh");
    if (imgChinh && urlAnh) {
        delete imgChinh.dataset.fallbackApplied;
        imgChinh.src = urlAnh;
    }

    document.querySelectorAll(".sp-ct-thumb-btn").forEach(btn => {
        btn.classList.toggle("active", btn === nutThumb);
    });
}

function laySoLuongChiTietSp() {
    const input = document.getElementById("txtSoLuongChiTietSp");
    if (!input) return 1;

    const min = parseInt(input.min || "1", 10);
    const max = parseInt(input.max || "999", 10);
    let val = parseInt(input.value || "1", 10);
    if (isNaN(val) || val < min) val = min;
    if (!isNaN(max) && max > 0 && val > max) val = max;
    input.value = String(val);
    return val;
}

function thayDoiSoLuongChiTietSp(delta) {
    const input = document.getElementById("txtSoLuongChiTietSp");
    if (!input || input.disabled) return;

    const min = parseInt(input.min || "1", 10);
    const max = parseInt(input.max || "999", 10);
    let hienTai = parseInt(input.value || "1", 10);
    if (isNaN(hienTai)) hienTai = min;

    let moi = hienTai + delta;
    if (moi < min) moi = min;
    if (!isNaN(max) && max > 0 && moi > max) moi = max;
    input.value = String(moi);
}

async function muaNgaySanPhamChiTiet(maBanh, nutBam) {
    const soLuong = laySoLuongChiTietSp();
    const noiDungCu = nutBam ? nutBam.innerHTML : "";
    if (nutBam) {
        nutBam.disabled = true;
        nutBam.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span>Đang chuyển thanh toán...`;
    }

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
        if (data && data.thanhCong) {
            window.location.href = "/ThanhToan/Index";
            return;
        }
        if (typeof hienThiThongBaoGioHang === "function") {
            hienThiThongBaoGioHang((data && data.thongBao) || "Không thể đặt mua sản phẩm này!", false);
        }
    } catch (err) {
        if (typeof hienThiThongBaoGioHang === "function") {
            hienThiThongBaoGioHang("Lỗi kết nối máy chủ khi xử lý mua ngay!", false);
        }
    } finally {
        if (nutBam) {
            nutBam.disabled = false;
            nutBam.innerHTML = noiDungCu;
        }
    }
}
