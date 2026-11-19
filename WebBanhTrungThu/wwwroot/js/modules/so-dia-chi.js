// =========================================================================
// MODULE SỔ ĐỊA CHỈ HỘI VIÊN (THÊM / CHỈNH SỬA QUA MODAL & XÁC NHẬN XÓA)
// =========================================================================
(function () {
    function chuanBiModal() {
        const modalEl = document.getElementById("modalSuaDiaChi");
        if (!modalEl) return null;
        if (modalEl.parentElement !== document.body) {
            document.body.appendChild(modalEl);
        }
        return modalEl;
    }

    function thietLapTruongForm(cheDo) {
        const form = document.getElementById("formDiaChiHoSo");
        const tieuDe = document.getElementById("tieuDeModalSuaDiaChi");
        const nutLuu = document.getElementById("btnLuuDiaChiModal");
        const hidMaDc = document.getElementById("suaMaDiaChi");
        const elNhan = document.getElementById("suaNhanGoiNho");
        const elTen = document.getElementById("suaNguoiNhan");
        const elSdt = document.getElementById("suaSdtNhan");
        const elDiaChi = document.getElementById("suaDiaChiChiTiet");

        const laThemMoi = cheDo === "THEM";
        if (form) {
            form.action = laThemMoi
                ? (form.dataset.urlThem || "/TaiKhoan/ThemDiaChiMoi")
                : (form.dataset.urlSua || "/TaiKhoan/SuaDiaChi");
        }
        if (tieuDe) {
            tieuDe.textContent = laThemMoi ? "Thêm địa chỉ nhận quà mới" : "Chỉnh sửa địa chỉ nhận quà";
        }
        if (nutLuu) {
            nutLuu.textContent = laThemMoi ? "Lưu vào sổ địa chỉ" : "Lưu thay đổi";
        }
        if (hidMaDc) {
            hidMaDc.disabled = laThemMoi;
            if (laThemMoi) hidMaDc.value = "0";
        }
        if (elNhan) elNhan.name = laThemMoi ? "nhanDiaChi" : "nhanGoiNho";
        if (elTen) elTen.name = laThemMoi ? "tenNguoiNhan" : "nguoiNhan";
        if (elSdt) elSdt.name = laThemMoi ? "sdtNguoiNhan" : "sdtNhan";
        if (elDiaChi) elDiaChi.name = laThemMoi ? "diaChiCuThe" : "diaChiChiTiet";
    }

    window.moModalThemDiaChi = function () {
        const modalEl = chuanBiModal();
        if (!modalEl) return;

        thietLapTruongForm("THEM");
        document.getElementById("suaNhanGoiNho").value = "";
        document.getElementById("suaNguoiNhan").value = "";
        document.getElementById("suaSdtNhan").value = "";
        document.getElementById("suaTinhThanh").value = "TP. Hồ Chí Minh";
        document.getElementById("suaQuanHuyen").value = "";
        document.getElementById("suaPhuongXa").value = "";
        document.getElementById("suaDiaChiChiTiet").value = "";
        document.getElementById("suaLaMacDinh").checked = false;

        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    };

    window.moModalSuaDiaChi = function (btn) {
        if (!btn) return;
        const modalEl = chuanBiModal();
        if (!modalEl) return;

        thietLapTruongForm("SUA");
        document.getElementById("suaMaDiaChi").value = btn.dataset.madc || "0";
        document.getElementById("suaNhanGoiNho").value = btn.dataset.nhan || "";
        document.getElementById("suaNguoiNhan").value = btn.dataset.ten || "";
        document.getElementById("suaSdtNhan").value = btn.dataset.sdt || "";
        document.getElementById("suaTinhThanh").value = btn.dataset.tinhthanh || "";
        document.getElementById("suaQuanHuyen").value = btn.dataset.quanhuyen || "";
        document.getElementById("suaPhuongXa").value = btn.dataset.phuongxa || "";
        document.getElementById("suaDiaChiChiTiet").value = btn.dataset.diachi || "";
        document.getElementById("suaLaMacDinh").checked = btn.dataset.macdinh === "true";

        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    };

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll('[data-mo-modal-dia-chi="them"]').forEach(btn => {
            btn.addEventListener("click", window.moModalThemDiaChi);
        });

        document.querySelectorAll('[data-mo-modal-dia-chi="sua"]').forEach(btn => {
            btn.addEventListener("click", function () {
                window.moModalSuaDiaChi(btn);
            });
        });

        document.querySelectorAll("form.js-form-xoa-dia-chi").forEach(form => {
            form.addEventListener("submit", function (e) {
                const thongBao = form.dataset.xacNhanXoa || "Xóa địa chỉ này khỏi Sổ địa chỉ?";
                if (!window.confirm(thongBao)) {
                    e.preventDefault();
                }
            });
        });
    });

    window.moModalKhachHuyDon = function (maDh, maTraCuu) {
        const modalEl = document.getElementById("modalKhachHuyDonCod");
        if (!modalEl || typeof bootstrap === "undefined") return;
        if (modalEl.parentElement !== document.body) {
            document.body.appendChild(modalEl);
        }

        const inpMaDh = document.getElementById("inpKhachHuyMaDh");
        const lblMa = document.getElementById("lblKhachHuyMaTraCuu");
        const sel = document.getElementById("selLyDoHuyKhach");
        const txtKhac = document.getElementById("txtLyDoHuyKhacKhach");

        if (inpMaDh) inpMaDh.value = String(maDh || 0);
        if (lblMa) lblMa.textContent = "#" + (maTraCuu || "");
        if (sel) sel.selectedIndex = 0;
        if (txtKhac) txtKhac.value = "";
        window.dongBoLyDoHuyKhach();

        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    };

    window.dongBoLyDoHuyKhach = function () {
        const sel = document.getElementById("selLyDoHuyKhach");
        const boxKhac = document.getElementById("khungNhapLyDoKhacKhach");
        const txtKhac = document.getElementById("txtLyDoHuyKhacKhach");
        if (!sel || !boxKhac) return;

        const laLyDoKhac = sel.value === "KHAC";
        boxKhac.style.display = laLyDoKhac ? "block" : "none";
        if (txtKhac) {
            txtKhac.required = laLyDoKhac;
        }
    };

    window.chuanBiGuiLyDoHuyDon = function () {
        const sel = document.getElementById("selLyDoHuyKhach");
        const txtKhac = document.getElementById("txtLyDoHuyKhacKhach");
        const inpCuoi = document.getElementById("inpKhachHuyLyDoCuoi");
        if (!inpCuoi) return true;

        let lyDo = sel ? sel.value : "Thay đổi nhu cầu mua hàng";
        if (lyDo === "KHAC") {
            lyDo = (txtKhac ? txtKhac.value : "").trim();
            if (!lyDo) {
                if (txtKhac) txtKhac.focus();
                return false;
            }
        }
        inpCuoi.value = lyDo;
        return true;
    };
})();
