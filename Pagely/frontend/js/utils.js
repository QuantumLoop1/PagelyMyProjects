// ─── utils.js — утилиты ───────────────────────────────────────────────────────

function formatPageDate(value) {
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? "—" : d.toLocaleDateString("ru-RU", { month: "short", day: "numeric" });
}

function toDateInputValue(value) {
    if (!value) return "";
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? "" : d.toISOString().split("T")[0];
}

function getCurrentStatus() {
    const si = document.getElementById("page-status");
    return si ? si.value : normalizeStatus(state.currentPage?.status);
}

function getCurrentScheduledFor() {
    const di = document.getElementById("page-date");
    return di ? (di.value || null) : (state.currentPage?.scheduledFor ?? null);
}

function normalizeStatus(status) {
    if (!status) return "Todo";
    const s = String(status).trim();
    return s ? `${s[0].toUpperCase()}${s.slice(1).toLowerCase()}` : "Todo";
}

function normalizeColor(color) { return color || "#f5f5f5"; }

function getPageIdFromQuery() {
    const id = new URLSearchParams(window.location.search).get("pageId");
    return id && id.trim().length > 0 ? id : null;
}

function findPageById(pages, id) {
    for (const p of pages) {
        if (p.id === id) return p;
        const c = findPageById(p.children || [], id);
        if (c) return c;
    }
    return null;
}

function findFirstPage(pages) { return pages.length ? pages[0] : null; }

function flattenPages(pages, result = []) {
    for (const page of pages) { result.push(page); if (page.children?.length) flattenPages(page.children, result); }
    return result;
}

function toUtcDateKey(date) {
    return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, "0")}-${String(date.getUTCDate()).padStart(2, "0")}`;
}
