// ─── api.js — HTTP-клиент, токены, refresh ────────────────────────────────────

const API_BASE = (() => {
    const { protocol, hostname, port } = window.location;
    const portSuffix = port ? `:${port}` : "";
    return `${protocol}//${hostname}${portSuffix}`;
})();

const ACCESS_KEY  = "pagely_access_token";
const REFRESH_KEY = "pagely_refresh_token";

function getAccessToken()  { return localStorage.getItem(ACCESS_KEY); }
function getRefreshToken() { return localStorage.getItem(REFRESH_KEY); }
function setTokens(a, r)   { localStorage.setItem(ACCESS_KEY, a); localStorage.setItem(REFRESH_KEY, r); }
function clearTokens()     { localStorage.removeItem(ACCESS_KEY); localStorage.removeItem(REFRESH_KEY); }

function unwrapResponse(payload) { return payload?.data ?? payload?.Data ?? payload; }

async function readJson(response) {
    const ct = response.headers.get("content-type") || "";
    return ct.includes("application/json") ? response.json() : null;
}

async function refreshSession() {
    const refreshToken = getRefreshToken();
    if (!refreshToken) throw new Error("Session expired.");
    const response = await fetch(`${API_BASE}/api/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken })
    });
    const payload = await readJson(response);
    if (!response.ok) { clearTokens(); window.location.href = "index.html"; throw new Error(payload?.error ?? "Session expired."); }
    const data = unwrapResponse(payload);
    setTokens(data.accessToken, data.refreshToken);
    return data;
}

async function apiRequest(path, options = {}, retry = true) {
    const headers = new Headers(options.headers || {});
    const accessToken = getAccessToken();
    if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
    if (options.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
    const response = await fetch(`${API_BASE}${path}`, { ...options, headers });
    if (response.status === 401 && retry) {
        try { await refreshSession(); } catch { clearTokens(); window.location.href = "index.html"; throw new Error("Session expired."); }
        return apiRequest(path, options, false);
    }
    const payload = await readJson(response);
    if (!response.ok) throw new Error(payload?.error ?? payload?.Error ?? "Request failed.");
    return payload;
}

// LogOut — отзываем refresh-токен на бэке (endpoint /api/auth/logout),
// затем чистим localStorage и уходим на index.html
async function logout() {
    try {
        const refreshToken = getRefreshToken();
        if (refreshToken) {
            await apiRequest("/api/auth/logout", {
                method: "POST",
                body: JSON.stringify({ refreshToken })
            });
        }
    } catch (_) {
        // даже если сервер вернул ошибку — всё равно чистим локальное хранилище
    } finally {
        clearTokens();
        window.location.href = "index.html";
    }
}

function requireAuth() {
    if (!getAccessToken() || !getRefreshToken()) { window.location.href = "index.html"; return false; }
    return true;
}
