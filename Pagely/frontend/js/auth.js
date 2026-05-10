// ─── auth.js — страница авторизации ──────────────────────────────────────────

function initAuthPage() {
    if (getAccessToken() && getRefreshToken()) { window.location.href = "app.html"; return; }
    const status    = document.getElementById("auth-status");
    const loginForm = document.getElementById("login-form");
    const regForm   = document.getElementById("register-form");
    const tabs      = document.querySelectorAll("[data-auth-tab]");

    tabs.forEach(tab => {
        tab.addEventListener("click", () => {
            tabs.forEach(t => t.classList.remove("active"));
            tab.classList.add("active");
            const sel = tab.dataset.authTab;
            loginForm.classList.toggle("hidden", sel !== "login");
            regForm.classList.toggle("hidden", sel !== "register");
            status.textContent = "";
        });
    });

    loginForm.addEventListener("submit", async e => { e.preventDefault(); await submitAuthForm("/api/auth/login",    loginForm, status); });
    regForm.addEventListener("submit",   async e => { e.preventDefault(); await submitAuthForm("/api/auth/register", regForm,   status); });
}

async function submitAuthForm(endpoint, form, statusEl) {
    statusEl.textContent = "Подождите...";
    statusEl.style.color = "var(--text-secondary)";
    const payload = Object.fromEntries(new FormData(form).entries());
    try {
        const response = await fetch(`${API_BASE}${endpoint}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });
        const json = await readJson(response);
        if (!response.ok) throw new Error(json?.error ?? json?.Error ?? "Ошибка авторизации.");
        const data = unwrapResponse(json);
        setTokens(data.accessToken, data.refreshToken);
        window.location.href = "app.html";
    } catch (error) {
        statusEl.textContent = error.message;
        statusEl.style.color = "#b91c1c";
    }
}
