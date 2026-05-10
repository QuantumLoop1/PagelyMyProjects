// ─── main.js — точка входа, роутер страниц ───────────────────────────────────

document.addEventListener("DOMContentLoaded", () => {
    const page = document.body.dataset.page;
    if (page === "auth")      initAuthPage();
    if (page === "app")       initAppPage();
    if (page === "dashboard") initDashboardPage();
    if (page === "calendar")  initCalendarPage();
});

async function initAppPage() {
    if (!requireAuth()) return;
    bindSidebarEvents();
    bindEditorEvents();
    bindNewPageModal();
    bindDraftPanel();
    try {
        await loadPages();
        const requestedId = getPageIdFromQuery();
        if (requestedId) { await openPage(requestedId); }
        else if (!state.currentPage && state.pages.length > 0) {
            const first = findFirstPage(state.pages);
            if (first) await openPage(first.id);
        }
    } catch (err) {
        const status = document.getElementById("save-status");
        if (status) status.textContent = err.message;
    }
}

async function initDashboardPage() {
    if (!requireAuth()) return;
    bindSidebarEvents();
    bindNewPageModal();
    try {
        await loadPages();
        renderDashboardBoard();
    } catch (err) { showBoardError(err); }
}

async function initCalendarPage() {
    if (!requireAuth()) return;
    bindSidebarEvents();
    bindCalendarEvents();
    try {
        await loadPages();
        renderCalendarGrid();
    } catch (err) { showCalendarError(err); }
}
