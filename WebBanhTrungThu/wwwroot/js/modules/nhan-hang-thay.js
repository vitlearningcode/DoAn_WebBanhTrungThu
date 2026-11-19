(function () {
  const $ = id => document.getElementById(id);
  const toast = msg => typeof hienThiThongBaoGioHang === "function" && hienThiThongBaoGioHang(msg, false);

  window.capNhatGiaoDienNguoiKhacNhan = function () {
    const nhanTaiQuay = $("radNhanTaiCuaHang")?.checked === true;
    const lbl = $("lblChkGoiNguoiKhacNhan");
    if (lbl) lbl.textContent = nhanTaiQuay ? "Nhờ người khác đến cửa hàng nhận thay (nếu có)" : "Gọi người khác nhận hàng thay (nếu có)";
  };

  window.xuLyBatTatNguoiKhacNhan = function () {
    const bat = Boolean($("chkGoiNguoiKhacNhan")?.checked);
    const khung = $("khungNguoiKhacNhanHang");
    if (khung) khung.style.display = bat ? "block" : "none";
    window.capNhatGiaoDienNguoiKhacNhan();

    const elTen = $("dot0_TenNguoiNhan"), elSdt = $("dot0_SdtNguoiNhan");
    const tenMua = $("txtHoTenNguoiMua")?.value.trim() || "", sdtMua = $("txtSdtNguoiMua")?.value.trim() || "";
    if (elTen) { elTen.required = bat; if (bat && elTen.value.trim() === tenMua) elTen.value = ""; }
    if (elSdt) { elSdt.required = bat; if (bat && elSdt.value.trim() === sdtMua) elSdt.value = ""; }
    window.dongBoNguoiMuaSangNguoiNhan();
  };

  window.dongBoNguoiMuaSangNguoiNhan = function () {
    const tenMua = $("txtHoTenNguoiMua")?.value.trim() || "", sdtMua = $("txtSdtNguoiMua")?.value.trim() || "";
    if (!$("chkGoiNguoiKhacNhan")?.checked) {
      if ($("dot0_TenNguoiNhan")) $("dot0_TenNguoiNhan").value = tenMua;
      if ($("dot0_SdtNguoiNhan")) $("dot0_SdtNguoiNhan").value = sdtMua;
    }
    const elGui = $("txtNguoiGuiTrenThiep_0");
    if (elGui && !elGui.dataset.daSuaTay) elGui.value = tenMua;
  };

  window.kiemTraHopLeNguoiNhanThay = function () {
    if (!$("chkGoiNguoiKhacNhan")?.checked) return true;
    const elTen = $("dot0_TenNguoiNhan"), elSdt = $("dot0_SdtNguoiNhan");
    const ten = elTen?.value.trim() || "", sdt = elSdt?.value.trim() || "";
    if (!ten || !sdt) {
      toast("Vui lòng nhập đầy đủ Họ tên và Số điện thoại người nhận thay!");
      (!ten ? elTen : elSdt)?.focus();
      return false;
    }
    if (!/^\d{9,11}$/.test(sdt.replace(/[\s.-]/g, ""))) {
      toast("Số điện thoại người nhận thay không hợp lệ (cần 9–11 chữ số)!");
      elSdt?.focus();
      return false;
    }
    return true;
  };
})();
