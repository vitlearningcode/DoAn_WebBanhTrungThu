// =========================================================================
// LUCIAN UX/UI PROMPTS #264, #403, #404, #063: EMAIL TYPO, SIGN-IN & SIGN-UP UX
// MIT License (c) 2026 Seokwoooo · https://lucianlabs.dev/uxui-prompt/
// =========================================================================
const TEN_MIEN_CHIP_U264 = ["gmail.com", "icloud.com", "outlook.com", "yahoo.com", "agu.edu.vn"];
const TEN_MIEN_CHUAN_U264 = ["gmail.com", "icloud.com", "outlook.com", "yahoo.com", "agu.edu.vn", "hotmail.com", "live.com"];

function escapeHtmlU264(str) {
    return String(str || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

function tinhKhoangCachDamerauLevenshtein(a, b) {
    const s = String(a || "").toLowerCase();
    const t = String(b || "").toLowerCase();
    const m = s.length;
    const n = t.length;
    if (m === 0) return n;
    if (n === 0) return m;

    const dp = Array.from({ length: m + 1 }, () => new Array(n + 1).fill(0));
    for (let i = 0; i <= m; i++) dp[i][0] = i;
    for (let j = 0; j <= n; j++) dp[0][j] = j;

    for (let i = 1; i <= m; i++) {
        for (let j = 1; j <= n; j++) {
            const cost = s[i - 1] === t[j - 1] ? 0 : 1;
            dp[i][j] = Math.min(
                dp[i - 1][j] + 1,
                dp[i][j - 1] + 1,
                dp[i - 1][j - 1] + cost
            );
            if (i > 1 && j > 1 && s[i - 1] === t[j - 2] && s[i - 2] === t[j - 1]) {
                dp[i][j] = Math.min(dp[i][j], dp[i - 2][j - 2] + cost);
            }
        }
    }
    return dp[m][n];
}

function timGoiYLoiChinhTaTenMien(domainRaw, dangBlur = false) {
    const domain = String(domainRaw || "").trim().toLowerCase();
    if (!domain || TEN_MIEN_CHUAN_U264.includes(domain)) return null;

    const coDauCham = domain.includes(".");
    if (!coDauCham && (!dangBlur || domain.length < 4)) return null;

    if (coDauCham) {
        const phanSauCham = domain.split(".").pop() || "";
        if (!dangBlur && phanSauCham.length < 2) return null;
    }

    let tenMienTotNhat = null;
    let khoangCachMin = Infinity;

    for (const chuan of TEN_MIEN_CHUAN_U264) {
        const mucTieu = coDauCham ? chuan : chuan.split(".")[0];
        const kc = tinhKhoangCachDamerauLevenshtein(domain, mucTieu);
        if (kc < khoangCachMin) {
            khoangCachMin = kc;
            tenMienTotNhat = chuan;
        }
    }

    if (tenMienTotNhat && khoangCachMin > 0 && khoangCachMin <= 2) {
        return {
            goiY: tenMienTotNhat,
            sai: domain
        };
    }
    return null;
}

function khoiTaoEmailInputU264(container) {
    if (!container || container.dataset.u264Init === "1") return;
    const input = container.querySelector(".u264-in");
    if (!input) return;

    const laStep264 = container.classList.contains("u264-step");
    if (laStep264) {
        khoiTaoEmailStep264(container, input);
        return;
    }

    const chipsBox = container.querySelector(".u264-chips");
    const hintSlot = container.querySelector(".u264-hint-slot");
    if (!chipsBox || !hintSlot) return;

    container.dataset.u264Init = "1";

    function renderChips(localPart, domainPrefix) {
        const prefix = String(domainPrefix || "").toLowerCase();
        const khop = TEN_MIEN_CHIP_U264.filter(d => prefix === "" || d.startsWith(prefix));
        const danhSach = khop.length > 0 ? khop : TEN_MIEN_CHIP_U264;

        chipsBox.innerHTML = danhSach.map((dom, idx) =>
            `<button type="button" class="u264-chip" data-domain="${dom}" style="--chip-i:${idx};">@${dom}</button>`
        ).join("");
        chipsBox.classList.add("is-open");

        chipsBox.querySelectorAll(".u264-chip").forEach(chip => {
            chip.addEventListener("mousedown", function (e) {
                e.preventDefault();
            });
            chip.addEventListener("click", function () {
                const domChon = this.getAttribute("data-domain");
                if (!domChon) return;
                input.value = `${localPart}@${domChon}`;
                chipsBox.classList.remove("is-open");
                chipsBox.innerHTML = "";
                hintSlot.innerHTML = "";
                danhGiaEmail(false);
                input.dispatchEvent(new Event("input", { bubbles: true }));
                input.dispatchEvent(new Event("change", { bubbles: true }));
                input.focus();
            });
        });
    }

    function anChips() {
        chipsBox.classList.remove("is-open");
    }

    function renderGoiYSuaLoi(localPart, ketQua) {
        if (!ketQua) {
            hintSlot.innerHTML = "";
            return;
        }
        const emailDung = `${localPart}@${ketQua.goiY}`;
        hintSlot.innerHTML = `
            <button type="button" class="u264-typo-btn" data-fix="${escapeHtmlU264(emailDung)}">
                <span>Ý bạn là <strong>${escapeHtmlU264(localPart)}@<u class="u264-typo-ul">${escapeHtmlU264(ketQua.goiY)}</u></strong>?</span>
                <span class="u264-typo-old">(<u class="u264-typo-bad">${escapeHtmlU264(ketQua.sai)}</u>)</span>
            </button>
        `;
        const btnFix = hintSlot.querySelector(".u264-typo-btn");
        if (btnFix) {
            btnFix.addEventListener("click", function () {
                const emailSua = this.getAttribute("data-fix") || "";
                input.value = emailSua;
                hintSlot.innerHTML = "";
                anChips();
                container.classList.add("is-valid");
                input.dispatchEvent(new Event("input", { bubbles: true }));
                input.dispatchEvent(new Event("change", { bubbles: true }));
                input.focus();
            });
        }
    }

    function danhGiaEmail(dangBlur = false) {
        const giaTri = input.value.trim();
        const atIndex = giaTri.indexOf("@");
        const coNhieuAt = atIndex !== -1 && giaTri.indexOf("@", atIndex + 1) !== -1;

        if (!giaTri || atIndex <= 0 || coNhieuAt) {
            anChips();
            hintSlot.innerHTML = "";
            container.classList.remove("is-valid");
            return;
        }

        const localPart = giaTri.slice(0, atIndex);
        const domainPart = giaTri.slice(atIndex + 1).trim();
        const domainLower = domainPart.toLowerCase();

        if (!dangBlur && !domainLower.includes(".") && !TEN_MIEN_CHUAN_U264.includes(domainLower)) {
            renderChips(localPart, domainLower);
        } else {
            anChips();
        }

        const ketQuaLoi = timGoiYLoiChinhTaTenMien(domainLower, dangBlur);
        if (ketQuaLoi) {
            container.classList.remove("is-valid");
            renderGoiYSuaLoi(localPart, ketQuaLoi);
            return;
        }

        hintSlot.innerHTML = "";
        const hopLeCoBan = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(giaTri);
        container.classList.toggle("is-valid", hopLeCoBan);
    }

    input.addEventListener("input", () => danhGiaEmail(false));
    input.addEventListener("focus", () => {
        const val = input.value.trim();
        const atIndex = val.indexOf("@");
        if (atIndex > 0 && !val.slice(atIndex + 1).includes(".")) {
            danhGiaEmail(false);
        }
    });
    input.addEventListener("blur", () => {
        setTimeout(() => {
            anChips();
            danhGiaEmail(true);
        }, 120);
    });

    if (input.value && input.value.trim() !== "") {
        danhGiaEmail(true);
    }
}

function khoiTaoEmailStep264(root, inp) {
    const dms = root.querySelector(".u264-dms");
    const fx = root.querySelector(".u264-fx");
    const sg = root.querySelector(".u264-sg");
    if (!dms || !fx || !sg) return;

    root.dataset.u264Init = "1";

    dms.innerHTML = "";
    TEN_MIEN_CHIP_U264.forEach((d, idx) => {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "u264-dm";
        b.dataset.domain = d;
        b.textContent = "@" + d;
        b.style.transitionDelay = (idx * 0.04) + "s";
        b.addEventListener("mousedown", (e) => e.preventDefault());
        b.addEventListener("click", () => {
            const localPart = inp.value.split("@")[0];
            inp.value = localPart + "@" + d;
            inp.classList.remove("is-fix");
            void inp.offsetWidth;
            inp.classList.add("is-fix");
            inp.focus({ preventScroll: true });
            check(false);
            inp.dispatchEvent(new Event("input", { bubbles: true }));
        });
        dms.appendChild(b);
    });

    let fixTo = "";

    function capNhatChipsHienThi(dom) {
        const chips = Array.from(dms.children);
        let coKhop = false;
        chips.forEach(ch => {
            const d = ch.dataset.domain || "";
            const match = !dom || d.startsWith(dom);
            if (match) coKhop = true;
            ch.hidden = !match;
        });
        if (!coKhop) {
            chips.forEach(ch => { ch.hidden = false; });
        }
    }

    function check(dangBlur = false) {
        const v = inp.value.trim();
        const at = v.indexOf("@");
        const coNhieuAt = at !== -1 && v.indexOf("@", at + 1) !== -1;
        const dom = at >= 0 ? v.slice(at + 1).toLowerCase() : "";

        const moChips = !dangBlur && at > 0 && !coNhieuAt && !dom.includes(".") && !TEN_MIEN_CHUAN_U264.includes(dom);
        if (moChips) {
            capNhatChipsHienThi(dom);
        }
        root.classList.toggle("is-at", moChips);

        const ketQuaLoi = (at > 0 && !coNhieuAt) ? timGoiYLoiChinhTaTenMien(dom, dangBlur) : null;
        fixTo = ketQuaLoi ? (v.slice(0, at + 1) + ketQuaLoi.goiY) : "";
        root.classList.toggle("is-typo", !!ketQuaLoi);
        fx.tabIndex = ketQuaLoi ? 0 : -1;

        if (ketQuaLoi) {
            const d = ketQuaLoi.goiY;
            let k = 0;
            while (k < d.length && k < dom.length && d[k] === dom[k]) k++;
            let e = 0;
            while (e < d.length - k && e < dom.length - k && d[d.length - 1 - e] === dom[dom.length - 1 - e]) e++;
            if (k >= d.length - e && k > 0) {
                k = Math.max(0, d.length - e - 1);
            }
            sg.innerHTML = "";
            sg.appendChild(document.createTextNode(v.slice(0, at + 1) + d.slice(0, k)));
            const m = document.createElement("mark");
            m.textContent = d.slice(k, d.length - e) || d;
            sg.appendChild(m);
            sg.appendChild(document.createTextNode(d.slice(d.length - e)));
        }

        const ok = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/i.test(v) && !ketQuaLoi;
        root.classList.toggle("is-ok", ok);

        if (typeof window.capNhatChieuCaoDangKyU063 === "function") {
            window.capNhatChieuCaoDangKyU063();
        }
    }

    fx.addEventListener("mousedown", (e) => e.preventDefault());
    fx.addEventListener("click", () => {
        if (!fixTo) return;
        inp.value = fixTo;
        inp.classList.remove("is-fix");
        void inp.offsetWidth;
        inp.classList.add("is-fix");
        check(false);
        inp.dispatchEvent(new Event("input", { bubbles: true }));
        inp.focus({ preventScroll: true });
    });

    inp.addEventListener("input", () => check(false));
    inp.addEventListener("focus", () => check(false));
    inp.addEventListener("blur", () => {
        setTimeout(() => check(true), 120);
    });

    if (inp.value.trim()) check(true);
}

// =========================================================================
// #403 EMAIL / ACCOUNT SIGN-IN FORM
// =========================================================================
function khoiTaoDangNhapU403() {
    const root = document.querySelector("[data-u403]");
    if (!root || root.dataset.u403Init === "1") return;
    root.dataset.u403Init = "1";

    const form = root.querySelector(".u403-form");
    const fs = Array.from(root.querySelectorAll(".u403-f"));
    const ins = Array.from(root.querySelectorAll(".u403-in"));
    const em = ins[0];
    const pw = ins[1];
    const eye = root.querySelector(".u403-eye");
    const go = root.querySelector(".u403-go");
    const msg = root.querySelector(".u403-msg");
    const live = root.querySelector(".u403-live");
    if (!form || !em || !pw || !eye || !go || !msg) return;

    function syncFloatingLabels() {
        fs.forEach((f, k) => {
            f.classList.toggle("is-up", f.classList.contains("is-f") || !!(ins[k] && ins[k].value));
        });
    }

    window.dongBoTrangThaiU403 = function () {
        fs.forEach(f => f.classList.remove("is-err", "is-shake"));
        ins.forEach(i => {
            i.removeAttribute("aria-invalid");
            i.removeAttribute("aria-describedby");
        });
        msg.classList.remove("is-on");
        syncFloatingLabels();
    };

    window.datKetQuaDangNhapU403 = function (trangThai, thongBao, chiSoTruongLoi = 1) {
        if (trangThai === "busy") {
            fs.forEach(f => f.classList.remove("is-f", "is-err", "is-shake"));
            msg.classList.remove("is-on");
            syncFloatingLabels();
            go.classList.remove("is-ok");
            go.classList.add("is-busy");
            go.setAttribute("aria-busy", "true");
            if (live) live.textContent = thongBao || "Đang kiểm tra thông tin đăng nhập";
            return;
        }

        go.classList.remove("is-busy");
        go.removeAttribute("aria-busy");

        if (trangThai === "ok") {
            go.classList.add("is-ok");
            if (live) live.textContent = thongBao || "Đăng nhập thành công";
            return;
        }

        if (trangThai === "err") {
            const idx = chiSoTruongLoi === 0 ? 0 : 1;
            const f = fs[idx];
            const inp = ins[idx];
            if (f && inp) {
                f.classList.remove("is-shake");
                void f.offsetWidth;
                f.classList.add("is-err", "is-shake");
                inp.setAttribute("aria-invalid", "true");
                inp.setAttribute("aria-describedby", msg.id || "thongBaoLoiDangNhap");
                inp.focus({ preventScroll: true });
            }
            msg.textContent = thongBao || "Thông tin đăng nhập không chính xác";
            msg.classList.add("is-on");
            if (live) live.textContent = msg.textContent;
        }
    };

    root.addEventListener("pointerdown", (e) => {
        fs.forEach(f => f.classList.toggle("is-f", f.contains(e.target) && !eye.contains(e.target)));
        syncFloatingLabels();
    });

    ins.forEach((inp, k) => {
        inp.addEventListener("focus", () => {
            fs[k].classList.add("is-f");
            syncFloatingLabels();
        });
        inp.addEventListener("blur", () => {
            fs[k].classList.remove("is-f");
            syncFloatingLabels();
        });
        inp.addEventListener("input", () => {
            if (fs[k].classList.contains("is-err")) {
                fs[k].classList.remove("is-err", "is-shake");
                msg.classList.remove("is-on");
                inp.removeAttribute("aria-invalid");
                inp.removeAttribute("aria-describedby");
            }
            syncFloatingLabels();
        });
    });

    eye.addEventListener("click", () => {
        const show = eye.getAttribute("aria-pressed") !== "true";
        eye.setAttribute("aria-pressed", String(show));
        eye.setAttribute("aria-label", show ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        pw.type = show ? "text" : "password";
    });

    form.addEventListener("submit", (e) => {
        e.preventDefault();
        if (typeof window.thucHienDangNhapAjax === "function") {
            window.thucHienDangNhapAjax();
        }
    });

    syncFloatingLabels();
}

// =========================================================================
// #404 SIGN-UP PASSWORD & CAPTCHA CHỐNG ROBOT
// =========================================================================
window.captchaDangKyHopLe = false;

window.taiMaCaptchaDangKyMoi = function (xoaOInput = true) {
    window.captchaDangKyHopLe = false;
    const inputCaptcha = document.getElementById("txtMaCaptchaDangKy");
    const lbl = document.getElementById("lblTrangThaiCaptchaDangKy");

    if (xoaOInput && inputCaptcha) {
        inputCaptcha.value = "";
        inputCaptcha.classList.remove("is-shake");
    }
    if (lbl) {
        lbl.className = "u063-captcha-status";
        lbl.textContent = "Nhập đủ 5 ký tự trong hình để xác thực chống robot.";
    }

    const img = document.getElementById("imgCaptchaDangKy");
    if (img) {
        delete img.dataset.fallbackApplied;
        img.src = "/TaiKhoan/AnhCaptcha?t=" + Date.now();
    }
    if (typeof window.capNhatChieuCaoDangKyU063 === "function") {
        window.capNhatChieuCaoDangKyU063();
    }
};

window.xuLyGoMaCaptchaDangKy = async function (inputEl) {
    if (!inputEl) return;
    const giaTri = inputEl.value.replace(/[^a-zA-Z0-9]/g, "").toUpperCase();
    if (inputEl.value !== giaTri) {
        inputEl.value = giaTri;
    }

    const lbl = document.getElementById("lblTrangThaiCaptchaDangKy");
    if (giaTri.length < 5) {
        window.captchaDangKyHopLe = false;
        if (lbl) {
            lbl.className = "u063-captcha-status";
            lbl.textContent = "Nhập đủ 5 ký tự trong hình để xác thực chống robot.";
        }
        return;
    }

    if (lbl) {
        lbl.className = "u063-captcha-status";
        lbl.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span>Đang xác thực mã chống robot...`;
    }

    try {
        const res = await fetch("/TaiKhoan/XacThucCaptchaDangKy", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ maCaptcha: giaTri, maCaptchaNhap: giaTri })
        });
        const data = await res.json();

        if (data.thanhCong) {
            window.captchaDangKyHopLe = true;
            if (lbl) {
                lbl.className = "u063-captcha-status is-ok";
                lbl.textContent = data.thongBao || "✓ Đã xác thực chống robot";
            }
        } else {
            window.captchaDangKyHopLe = false;
            if (lbl) {
                lbl.className = "u063-captcha-status is-err";
                lbl.textContent = data.thongBao || "Mã Captcha không chính xác. Vui lòng nhập lại!";
            }
            if (data.anhCaptchaSvg) {
                const img = document.getElementById("imgCaptchaDangKy");
                if (img) img.src = data.anhCaptchaSvg;
            } else {
                window.taiMaCaptchaDangKyMoi(false);
            }
            inputEl.value = "";
            inputEl.classList.remove("is-shake");
            void inputEl.offsetWidth;
            inputEl.classList.add("is-shake");
            inputEl.focus({ preventScroll: true });
        }
    } catch (err) {
        window.captchaDangKyHopLe = false;
        if (lbl) {
            lbl.className = "u063-captcha-status is-err";
            lbl.textContent = "Không thể kết nối máy chủ để xác thực Captcha.";
        }
    }

    if (typeof window.capNhatChieuCaoDangKyU063 === "function") {
        window.capNhatChieuCaoDangKyU063();
    }
};

function khoiTaoMatKhauDangKyU404() {
    const root = document.querySelector("[data-u404]");
    if (!root || root.dataset.u404Init === "1") return;
    root.dataset.u404Init = "1";

    const LV = ["", "Yếu", "Khá", "Mạnh", "Rất mạnh"];
    const f = root.querySelector(".u404-f");
    const inp = root.querySelector(".u404-in");
    const eye = root.querySelector(".u404-eye");
    const bars = Array.from(root.querySelectorAll(".u404-bars i"));
    const meter = root.querySelector(".u404-bars");
    const lvl = root.querySelector(".u404-st em");
    const rules = Array.from(root.querySelectorAll(".u404-rules li"));
    const go = root.querySelector(".u404-go");
    const live = root.querySelector(".u404-live");
    if (!f || !inp || !eye || !meter || !lvl || !go) return;

    let ready = false;
    inp.setAttribute("aria-describedby", rules.map(r => r.id).filter(Boolean).join(" "));

    const CHECK = [
        (v) => v.length >= 8,
        (v) => /\d/.test(v),
        (v) => /[^A-Za-z0-9\s]/.test(v)
    ];

    function up() {
        f.classList.toggle("is-up", f.classList.contains("is-f") || !!inp.value);
    }

    function score(v) {
        if (!v) return 0;
        const c1 = v.length >= 8 ? 1 : 0;
        const c2 = /\d/.test(v) ? 1 : 0;
        const c3 = /[^A-Za-z0-9\s]/.test(v) ? 1 : 0;
        const c4 = (v.length >= 12 || (/[a-z]/.test(v) && /[A-Z]/.test(v))) ? 1 : 0;
        return Math.max(1, c1 + c2 + c3 + c4);
    }

    function sync() {
        const v = inp.value;
        const lv = score(v);
        let all = true;

        rules.forEach((r, k) => {
            const ok = CHECK[k](v);
            if (ok !== r.classList.contains("is-ok")) {
                r.classList.toggle("is-ok", ok);
            }
            all = all && ok;
        });

        root.setAttribute("data-lv", String(lv));
        bars.forEach((b, k) => b.classList.toggle("is-on", k < lv));
        meter.setAttribute("aria-valuenow", String(lv));
        meter.setAttribute("aria-valuetext", LV[lv] || "");
        lvl.textContent = LV[lv];

        if (all !== ready) {
            ready = all;
            go.classList.toggle("is-awake", all);
            go.setAttribute("aria-disabled", String(!all));
            if (all && live) {
                live.textContent = "Mật khẩu đã đạt chuẩn bảo mật";
            }
        }
        up();
    }

    root.addEventListener("pointerdown", (e) => {
        f.classList.toggle("is-f", f.contains(e.target) && !eye.contains(e.target));
        up();
    });
    inp.addEventListener("focus", () => {
        f.classList.add("is-f");
        up();
    });
    inp.addEventListener("blur", () => {
        f.classList.remove("is-f");
        up();
    });
    eye.addEventListener("click", () => {
        const s = eye.getAttribute("aria-pressed") !== "true";
        eye.setAttribute("aria-pressed", String(s));
        eye.setAttribute("aria-label", s ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        inp.type = s ? "text" : "password";
    });
    inp.addEventListener("input", sync);

    window.dongBoTrangThaiU404 = sync;
    sync();
}

// =========================================================================
// #063 ONE-QUESTION-AT-A-TIME SIGN-UP FLOW
// =========================================================================
function khoiTaoDangKyU063() {
    const root = document.querySelector("[data-u063]");
    if (!root || root.dataset.u063Init === "1") return;
    root.dataset.u063Init = "1";

    const qs = Array.from(root.querySelectorAll(".u063-q"));
    const bars = Array.from(root.querySelectorAll(".u063-bar i"));
    const chips = root.querySelector(".u063-chips");
    const stage = root.querySelector(".u063-stage");
    const form = root.querySelector("#formDangKyU063");
    const next = root.querySelector("#btnU063Next");
    const done = root.querySelector(".u063-done");
    const whoEl = root.querySelector(".u063-who");
    if (!qs.length || !chips || !stage || !form || !next || !done) return;

    let cur = 0;
    const vals = {};
    const stepMeta = [
        { key: "name", label: "Sửa họ tên" },
        { key: "phone", label: "Sửa số điện thoại" },
        { key: "mail", label: "Sửa email" }
    ];

    qs.forEach((q, i) => {
        const inp = q.querySelector("input[data-k]");
        const msg = q.querySelector(":scope > .u063-msg");
        if (inp && msg && msg.id && !inp.getAttribute("aria-describedby")) {
            inp.setAttribute("aria-describedby", msg.id);
        }
        q.inert = i !== 0;
    });

    function capNhatChieuCaoStage() {
        if (root.classList.contains("is-done")) {
            stage.style.minHeight = "260px";
            return;
        }
        const qHienTai = qs[cur];
        if (!qHienTai) return;
        const rectH = qHienTai.getBoundingClientRect ? qHienTai.getBoundingClientRect().height : 0;
        const h = Math.ceil(Math.max(qHienTai.scrollHeight || 0, qHienTai.offsetHeight || 0, rectH || 0));
        if (h > 0) {
            stage.style.minHeight = Math.max(205, h + 28) + "px";
        }
    }

    window.capNhatChieuCaoDangKyU063 = capNhatChieuCaoStage;

    if (typeof ResizeObserver !== "undefined") {
        const ro = new ResizeObserver(() => capNhatChieuCaoStage());
        qs.forEach(q => ro.observe(q));
    }

    function laSdtHopLe(raw) {
        const clean = raw.replace(/[\s.\-()]/g, "");
        return /^(0|\+84)[0-9]{8,10}$/.test(clean) || /^[0-9]{9,12}$/.test(clean);
    }

    function check(i) {
        const inp = qs[i]?.querySelector("input[data-k]");
        const v = inp ? inp.value.trim() : "";
        if (i === 0 && v.length < 2) return "Vui lòng nhập họ tên từ 2 ký tự trở lên";
        if (i === 1 && !laSdtHopLe(v)) return "Số điện thoại chưa hợp lệ · VD: 0909123456";
        if (i === 2 && !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(v)) return "Email chưa đúng định dạng · VD: ten@gmail.com";
        return "";
    }

    function show(i) {
        const rm = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        qs.forEach((q, k) => {
            q.classList.toggle("out", k < i);
            q.classList.toggle("wait", k > i);
            q.inert = k !== i;
        });
        cur = i;
        bars.forEach((b, k) => b.classList.toggle("on", k < i));
        next.hidden = i >= 3;
        capNhatChieuCaoStage();
        setTimeout(capNhatChieuCaoStage, 40);

        if (i < 4) {
            setTimeout(() => {
                if (root.isConnected && cur === i) {
                    const targetInp = qs[i]?.querySelector("input[data-k]");
                    if (targetInp) targetInp.focus({ preventScroll: true });
                }
            }, rm ? 30 : 350);
        }
    }

    function chipsRender() {
        chips.innerHTML = "";
        stepMeta.forEach((meta, i) => {
            if (!(meta.key in vals) || i >= cur) return;
            const c = document.createElement("button");
            c.type = "button";
            c.className = "u063-chip";
            c.style.animationDelay = (i === cur - 1 ? 0 : -1) + "s";
            c.innerHTML = '<i aria-hidden="true"><svg viewBox="0 0 10 10"><path d="M2 5.2l2 2 4-4.4"/></svg></i><span></span>';
            c.querySelector("span").textContent = vals[meta.key];
            c.setAttribute("aria-label", meta.label);
            c.addEventListener("click", () => {
                root.classList.remove("is-done");
                done.setAttribute("aria-hidden", "true");
                show(i);
                chipsRender();
            });
            chips.appendChild(c);
        });
        capNhatChieuCaoStage();
    }

    function advance() {
        if (cur >= 3) return;
        const q = qs[cur];
        const msg = q.querySelector(":scope > .u063-msg");
        const inp = q.querySelector("input[data-k]");
        const bad = check(cur);

        if (bad) {
            q.classList.remove("bad");
            void q.offsetWidth;
            q.classList.add("bad");
            if (msg) msg.textContent = bad;
            if (inp) {
                inp.setAttribute("aria-invalid", "true");
                inp.focus({ preventScroll: true });
            }
            capNhatChieuCaoStage();
            return;
        }

        q.classList.remove("bad");
        if (inp) {
            inp.removeAttribute("aria-invalid");
            vals[inp.getAttribute("data-k")] = inp.value.trim();
        }
        if (msg) msg.textContent = msg.getAttribute("data-hint") || "";

        show(cur + 1);
        chipsRender();
    }

    window.hoanTatDangKyU063 = function (hoTen, email) {
        const goBtn = root.querySelector("#btnSubmitDangKy");
        if (goBtn) {
            goBtn.classList.add("is-ok");
        }
        bars.forEach(b => b.classList.add("on"));
        cur = 4;
        qs.forEach(q => {
            q.classList.add("out");
            q.classList.remove("wait");
            q.inert = true;
        });
        next.hidden = true;
        root.classList.add("is-done");
        done.setAttribute("aria-hidden", "false");
        if (whoEl) {
            whoEl.textContent = `Chào mừng ${hoTen || "Hội viên mới"}! Đã cộng +50 điểm thưởng cho ${email || "tài khoản của bạn"}.`;
        }
        capNhatChieuCaoStage();
    };

    next.addEventListener("click", advance);
    form.addEventListener("submit", (e) => {
        e.preventDefault();
        if (cur < 3) {
            advance();
        } else if (typeof window.thucHienDangKyAjax === "function") {
            window.thucHienDangKyAjax();
        }
    });

    qs.forEach(q => {
        const inp = q.querySelector("input[data-k]");
        const msg = q.querySelector(":scope > .u063-msg");
        if (!inp) return;
        inp.addEventListener("input", () => {
            if (q.classList.contains("bad")) {
                q.classList.remove("bad");
                inp.removeAttribute("aria-invalid");
                if (msg) msg.textContent = msg.getAttribute("data-hint") || "";
                capNhatChieuCaoStage();
            }
        });
    });

    const tabDangKy = document.getElementById("btn-tab-dangky");
    if (tabDangKy) {
        tabDangKy.addEventListener("shown.bs.tab", () => {
            capNhatChieuCaoStage();
            const imgCap = document.getElementById("imgCaptchaDangKy");
            if (imgCap && (!imgCap.getAttribute("src") || !imgCap.complete || imgCap.naturalWidth === 0)) {
                window.taiMaCaptchaDangKyMoi(false);
            }
            const targetInp = qs[cur]?.querySelector("input[data-k]");
            if (targetInp && cur < 4) {
                setTimeout(() => targetInp.focus({ preventScroll: true }), 80);
            }
        });
    }
}

window.chuyenDoiThemeModalTaiKhoan = function () {
    const html = document.documentElement;
    const modal = document.getElementById("modalTaiKhoan");
    const btn = document.getElementById("btnToggleThemeTaiKhoan");
    const dangToi = html.getAttribute("data-theme") === "dark";
    const themeMoi = dangToi ? "light" : "dark";

    html.setAttribute("data-theme", themeMoi);
    if (modal) {
        modal.setAttribute("data-theme", themeMoi);
    }
    if (btn) {
        btn.setAttribute("aria-pressed", String(!dangToi));
        btn.innerHTML = !dangToi
            ? '<i class="bi bi-sun-fill" aria-hidden="true"></i>'
            : '<i class="bi bi-moon-stars-fill" aria-hidden="true"></i>';
    }
};

function khoiTaoTatCaUxAuthVaEmail() {
    document.querySelectorAll("[data-u264]").forEach(khoiTaoEmailInputU264);
    khoiTaoDangNhapU403();
    khoiTaoMatKhauDangKyU404();
    khoiTaoDangKyU063();
}

document.addEventListener("DOMContentLoaded", function () {
    khoiTaoTatCaUxAuthVaEmail();
    const modalTk = document.getElementById("modalTaiKhoan");
    if (modalTk) {
        modalTk.addEventListener("shown.bs.modal", function () {
            khoiTaoTatCaUxAuthVaEmail();
            if (typeof window.dongBoTrangThaiU403 === "function") {
                window.dongBoTrangThaiU403();
            }
            if (typeof window.capNhatChieuCaoDangKyU063 === "function") {
                window.capNhatChieuCaoDangKyU063();
            }
        });
    }
});
