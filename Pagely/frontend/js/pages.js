// ─── pages.js — CRUD заметок, модальное окно, загрузка данных ────────────────

async function loadPages() {
    const response = await apiRequest("/api/pages", { method: "GET" });
    state.pages    = unwrapResponse(response) || [];
    state.flatPages = flattenPages(state.pages);
    if (state.expanded.size === 0) normalizeExpandedState(state.pages);
    renderSidebar();
}

// ─── New page modal ───────────────────────────────────────────────────────────
function bindNewPageModal() {
    const modal     = document.getElementById("new-page-modal");
    const form      = document.getElementById("new-page-form");
    const cancelBtn = document.getElementById("new-page-cancel");
    if (cancelBtn) cancelBtn.addEventListener("click", hideNewPageModal);
    if (modal) modal.addEventListener("click", e => { if (e.target === modal) hideNewPageModal(); });
    if (form) {
        form.addEventListener("submit", async e => {
            e.preventDefault();
            const title    = document.getElementById("new-page-title").value.trim();
            const date     = document.getElementById("new-page-date").value;
            const status   = document.getElementById("new-page-status").value;
            const parentId = form.dataset.parentId || null;
            if (!title) return;
            try {
                const page = await createPage(parentId, title, status, date);
                await loadPages();
                hideNewPageModal();
                const curPage = document.body.dataset.page;
                if (curPage === "app")       { await openPage(page.id); }
                else if (curPage === "dashboard") { renderDashboardBoard(); }
                else { window.location.href = `app.html?pageId=${page.id}`; }
            } catch (err) { alert(err.message); }
        });
    }
}

function openNewPageModal(parentId, preStatus) {
    const modal = document.getElementById("new-page-modal");
    const form  = document.getElementById("new-page-form");
    if (!modal) return;
    if (form) form.dataset.parentId = parentId || "";
    const titleInput  = document.getElementById("new-page-title");
    const dateInput   = document.getElementById("new-page-date");
    const statusInput = document.getElementById("new-page-status");
    if (titleInput)  titleInput.value  = "";
    if (dateInput)   dateInput.value   = new Date().toISOString().split("T")[0];
    if (statusInput) statusInput.value = preStatus || "Todo";
    modal.classList.remove("hidden");
    if (titleInput) titleInput.focus();
}

function hideNewPageModal() {
    const modal = document.getElementById("new-page-modal");
    if (modal) modal.classList.add("hidden");
}

// ─── Page CRUD ────────────────────────────────────────────────────────────────
async function createPage(parentId, title, status = "Todo", scheduledFor = null) {
    const response = await apiRequest("/api/pages", {
        method: "POST",
        body: JSON.stringify({ parentId, title, content: "", icon: "🗂️", coverColor: null, status, scheduledFor: scheduledFor || null })
    });
    return unwrapResponse(response);
}

async function movePage(pageId, parentId, order) {
    await apiRequest(`/api/pages/${pageId}/move`, { method: "PATCH", body: JSON.stringify({ parentId, order }) });
    await loadPages();
}

/**
 * Удаление страницы.
 * Проблема в оригинале: после DELETE редактор не сбрасывался корректно,
 * и не было try/catch — ошибка API «зависала» без уведомления.
 */
async function deletePage(id) {
    try {
        await apiRequest(`/api/pages/${id}`, { method: "DELETE" });
    } catch (err) {
        alert("Не удалось удалить заметку: " + err.message);
        return;
    }

    state.currentPage = null;

    // Сброс редактора (только если он есть на этой странице)
    const titleEl = document.getElementById("page-title");
    if (titleEl) titleEl.textContent = "";
    const contentEl = document.getElementById("page-content");
    if (contentEl) contentEl.innerHTML = "";
    const iconBtn = document.getElementById("page-icon-button");
    if (iconBtn) iconBtn.textContent = "🗂️";
    const emojiBtn = document.getElementById("emoji-button");
    if (emojiBtn) emojiBtn.textContent = "🗂️";
    const cover = document.getElementById("cover-strip");
    if (cover) cover.style.background = "linear-gradient(135deg,#f5f5f5,#e9e9e9)";
    const si = document.getElementById("page-status");
    if (si) si.value = "Todo";
    const di = document.getElementById("page-date");
    if (di) di.value = "";

    await loadPages();

    // Открываем первую доступную страницу
    const first = findFirstPage(state.pages);
    if (first) await openPage(first.id);
}
