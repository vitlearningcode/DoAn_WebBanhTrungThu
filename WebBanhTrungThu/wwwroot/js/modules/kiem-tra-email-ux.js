// =========================================================================
// LUCIAN UX/UI PROMPT #264: EMAIL INPUT THAT CATCHES TYPOS (.u264)
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
    const chipsBox = container.querySelector(".u264-chips");
    const hintSlot = container.querySelector(".u264-hint-slot");
    if (!input || !chipsBox || !hintSlot) return;

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

function khoiTaoTatCaEmailU264() {
    document.querySelectorAll("[data-u264]").forEach(khoiTaoEmailInputU264);
}

document.addEventListener("DOMContentLoaded", function () {
    khoiTaoTatCaEmailU264();
    const modalTk = document.getElementById("modalTaiKhoan");
    if (modalTk) {
        modalTk.addEventListener("shown.bs.modal", khoiTaoTatCaEmailU264);
    }
});
