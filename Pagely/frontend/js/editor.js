// ─── editor.js — редактор заметки, черновик, emoji-пикер ─────────────────────

function bindEditorEvents() {
    const pageTitle   = document.getElementById("page-title");
    const pageContent = document.getElementById("page-content");
    const coverInput  = document.getElementById("cover-color-input");
    const emojiButton = document.getElementById("emoji-button");
    const pageIconBtn = document.getElementById("page-icon-button");
    const statusInput = document.getElementById("page-status");
    const dateInput   = document.getElementById("page-date");
    const deleteBtn   = document.getElementById("delete-page-button");

    if (!pageTitle || !pageContent) return;

    pageTitle.addEventListener("input", () => {
        if (state.suppressEditorEvents || !state.currentPage) return;
        state.currentPage.title = getEditorTitle();
        scheduleSave();
    });

    pageContent.addEventListener("input", () => {
        if (state.suppressEditorEvents || !state.currentPage) return;
        state.currentPage.content = pageContent.innerHTML;
        scheduleSave();
    });

    if (coverInput) coverInput.addEventListener("input", e => {
        if (!state.currentPage) return;
        state.currentPage.coverColor = e.target.value;
        document.getElementById("cover-strip").style.background = e.target.value;
        scheduleSave(250);
    });

    if (statusInput) statusInput.addEventListener("change", () => {
        if (!state.currentPage) return;
        state.currentPage.status = statusInput.value;
        scheduleSave(250);
    });

    if (dateInput) dateInput.addEventListener("change", () => {
        if (!state.currentPage) return;
        state.currentPage.scheduledFor = dateInput.value || null;
        scheduleSave(250);
    });

    // Удаление — исправлено: ловим ошибки внутри deletePage
    if (deleteBtn) deleteBtn.addEventListener("click", async () => {
        if (!state.currentPage || !confirm("Удалить эту заметку?")) return;
        await deletePage(state.currentPage.id);
    });

    if (pageIconBtn && emojiButton) {
        pageIconBtn.addEventListener("click", toggleEmojiPicker);
        emojiButton.addEventListener("click", toggleEmojiPicker);
        buildEmojiPicker();
    }

    document.querySelectorAll("[data-command]").forEach(btn => {
        btn.addEventListener("click", () => {
            pageContent.focus();
            document.execCommand(btn.dataset.command, false, null);
            if (state.currentPage) { state.currentPage.content = pageContent.innerHTML; scheduleSave(250); }
        });
    });

    document.addEventListener("click", e => {
        if (!e.target.closest("#emoji-picker") && !e.target.closest("#emoji-button") && !e.target.closest("#page-icon-button"))
            hideEmojiPicker();
    });
    document.addEventListener("keydown", e => { if (e.key === "Escape") hideEmojiPicker(); });
}

// ─── Editor open / save ───────────────────────────────────────────────────────
async function openPage(id) {
    if (state.currentPage && state.currentPage.id !== id) await flushPendingSave();
    const response = await apiRequest(`/api/pages/${id}`, { method: "GET" });
    state.currentPage = unwrapResponse(response);
    renderEditor(state.currentPage);
    highlightCurrentPage();
}

function renderEditor(page) {
    state.suppressEditorEvents = true;
    document.getElementById("page-title").textContent        = page.title   || "";
    document.getElementById("page-content").innerHTML        = page.content || "";
    document.getElementById("page-icon-button").textContent  = page.icon    || "🗂️";
    document.getElementById("emoji-button").textContent      = page.icon    || "🗂️";
    document.getElementById("cover-color-input").value       = normalizeColor(page.coverColor);
    document.getElementById("cover-strip").style.background  = page.coverColor || "linear-gradient(135deg,#f5f5f5,#e9e9e9)";
    document.getElementById("save-status").textContent       = "Готово";
    const si = document.getElementById("page-status");
    if (si) si.value = normalizeStatus(page.status);
    const di = document.getElementById("page-date");
    if (di) di.value = toDateInputValue(page.scheduledFor);
    state.suppressEditorEvents = false;
}

function getEditorTitle() { return document.getElementById("page-title").textContent.trim(); }

function scheduleSave(delay = 1000) {
    if (!state.currentPage) return;
    document.getElementById("save-status").textContent = "Сохранение...";
    clearTimeout(state.saveTimer);
    state.saveTimer = setTimeout(() => saveCurrentPage(), delay);
}

async function flushPendingSave() {
    if (state.saveTimer) { clearTimeout(state.saveTimer); state.saveTimer = null; }
    if (state.currentPage) await saveCurrentPage();
}

async function saveCurrentPage() {
    if (!state.currentPage) return;
    const body = {
        title: getEditorTitle(),
        content: document.getElementById("page-content").innerHTML,
        icon: state.currentPage.icon,
        coverColor: state.currentPage.coverColor,
        status: getCurrentStatus(),
        scheduledFor: getCurrentScheduledFor()
    };
    const response = await apiRequest(`/api/pages/${state.currentPage.id}`, { method: "PUT", body: JSON.stringify(body) });
    state.currentPage = unwrapResponse(response);
    document.getElementById("save-status").textContent = "Сохранено";
    await loadPages();
}

// ─── Draft panel ──────────────────────────────────────────────────────────────
function bindDraftPanel() {
    const draftToggle  = document.getElementById("draft-toggle");
    const draftPanel   = document.getElementById("draft-panel");
    if (!draftToggle || !draftPanel) return;

    const draftTitle   = document.getElementById("draft-title");
    const draftContent = document.getElementById("draft-content");
    const draftStatus  = document.getElementById("draft-status");
    const draftDate    = document.getElementById("draft-date");
    const draftPublish = document.getElementById("draft-publish");
    const draftClear   = document.getElementById("draft-clear");

    try {
        const saved = JSON.parse(sessionStorage.getItem("pagely_draft") || "null");
        if (saved) {
            if (draftTitle)   draftTitle.value   = saved.title   || "";
            if (draftContent) draftContent.value = saved.content || "";
            if (draftStatus)  draftStatus.value  = saved.status  || "Todo";
            if (draftDate)    draftDate.value     = saved.scheduledFor || "";
        }
    } catch {}

    draftToggle.addEventListener("click", () => {
        draftPanel.classList.toggle("hidden");
        draftToggle.classList.toggle("active");
    });

    function saveDraftLocally() {
        sessionStorage.setItem("pagely_draft", JSON.stringify({
            title:        draftTitle?.value || "",
            content:      draftContent?.value || "",
            status:       draftStatus?.value || "Todo",
            scheduledFor: draftDate?.value || ""
        }));
    }

    if (draftTitle)   draftTitle.addEventListener("input",  saveDraftLocally);
    if (draftContent) draftContent.addEventListener("input", saveDraftLocally);
    if (draftStatus)  draftStatus.addEventListener("change", saveDraftLocally);
    if (draftDate)    draftDate.addEventListener("change",   saveDraftLocally);

    if (draftClear) {
        draftClear.addEventListener("click", () => {
            if (draftTitle)   draftTitle.value   = "";
            if (draftContent) draftContent.value = "";
            if (draftStatus)  draftStatus.value  = "Todo";
            if (draftDate)    draftDate.value     = "";
            sessionStorage.removeItem("pagely_draft");
        });
    }

    if (draftPublish) {
        draftPublish.addEventListener("click", async () => {
            const title = draftTitle?.value.trim();
            if (!title) { alert("Введите заголовок черновика."); return; }
            try {
                draftPublish.disabled     = true;
                draftPublish.textContent  = "Публикация...";
                const resp = await apiRequest("/api/pages", {
                    method: "POST",
                    body: JSON.stringify({
                        title,
                        content:      draftContent?.value || "",
                        icon:         "📝",
                        coverColor:   null,
                        status:       draftStatus?.value || "Todo",
                        scheduledFor: draftDate?.value || null,
                        parentId:     null
                    })
                });
                const created = unwrapResponse(resp);
                if (draftTitle)   draftTitle.value   = "";
                if (draftContent) draftContent.value = "";
                if (draftStatus)  draftStatus.value  = "Todo";
                if (draftDate)    draftDate.value     = "";
                sessionStorage.removeItem("pagely_draft");
                await loadPages();
                await openPage(created.id);
                draftPanel.classList.add("hidden");
                draftToggle.classList.remove("active");
            } catch (err) { alert(err.message); }
            finally { draftPublish.disabled = false; draftPublish.textContent = "Опубликовать"; }
        });
    }
}

// ─── Emoji picker ─────────────────────────────────────────────────────────────
function toggleEmojiPicker() { document.getElementById("emoji-picker").classList.toggle("hidden"); }
function hideEmojiPicker()   { document.getElementById("emoji-picker").classList.add("hidden"); }
function buildEmojiPicker() {
    const picker = document.getElementById("emoji-picker");
    picker.innerHTML = "";
    EMOJIS.forEach(emoji => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.textContent = emoji;
        btn.addEventListener("click", () => {
            hideEmojiPicker();
            if (!state.currentPage) return;
            state.currentPage.icon = emoji;
            document.getElementById("page-icon-button").textContent = emoji;
            document.getElementById("emoji-button").textContent = emoji;
            scheduleSave(250);
        });
        picker.appendChild(btn);
    });
}
