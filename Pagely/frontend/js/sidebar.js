// ─── sidebar.js — сайдбар, дерево страниц, контекстное меню ──────────────────

function bindSidebarEvents() {
    const toggle = document.getElementById("sidebar-toggle");
    if (toggle) toggle.addEventListener("click", () => document.getElementById("sidebar").classList.toggle("collapsed"));

    const tree = document.getElementById("pages-tree");
    if (tree) {
        tree.addEventListener("dragover", e => e.preventDefault());
        tree.addEventListener("drop", async e => {
            e.preventDefault();
            if (!state.draggedPageId) return;
            await movePage(state.draggedPageId, null, null);
            state.draggedPageId = null;
        });
    }

    // Кнопка Выйти — общая для всех страниц приложения
    const logoutBtn = document.getElementById("logout-button");
    if (logoutBtn) logoutBtn.addEventListener("click", logout);

    const newPageBtn = document.getElementById("new-page-btn");
    if (newPageBtn) newPageBtn.addEventListener("click", () => openNewPageModal(null));

    document.addEventListener("click", e => { if (!e.target.closest(".context-menu")) hideContextMenu(); });
    document.addEventListener("keydown", e => { if (e.key === "Escape") { hideContextMenu(); hideNewPageModal(); } });
}

// ─── Sidebar rendering ────────────────────────────────────────────────────────
function normalizeExpandedState(pages) {
    for (const page of pages) { if (page.children?.length) { state.expanded.add(page.id); normalizeExpandedState(page.children); } }
}

function renderSidebar() {
    const tree = document.getElementById("pages-tree");
    if (!tree) return;
    tree.innerHTML = "";
    renderPageBranch(tree, state.pages, 0);
    highlightCurrentPage();
}

function renderPageBranch(container, pages, depth) {
    pages.forEach(page => {
        const node = document.createElement("li");
        node.className = "tree-node";
        const item = document.createElement("div");
        item.className = "tree-item";
        item.dataset.id = page.id;
        item.style.paddingLeft = `${10 + depth * 8}px`;
        item.draggable = true;
        if (state.currentPage?.id === page.id) item.classList.add("active");

        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = "tree-toggle";
        const hasChildren = (page.children || []).length > 0;
        toggle.textContent = hasChildren ? (state.expanded.has(page.id) ? "▾" : "▸") : "";
        toggle.disabled = !hasChildren;
        toggle.addEventListener("click", e => {
            e.stopPropagation();
            if (!hasChildren) return;
            if (state.expanded.has(page.id)) state.expanded.delete(page.id); else state.expanded.add(page.id);
            renderSidebar();
        });

        const icon = document.createElement("span");
        icon.className = "tree-icon";
        icon.textContent = page.icon || "🗂️";

        const title = document.createElement("span");
        title.className = "tree-title";
        title.textContent = page.title || "Без названия";

        item.addEventListener("click", () => {
            if (document.body.dataset.page === "app") openPage(page.id);
            else window.location.href = `app.html?pageId=${page.id}`;
        });
        item.addEventListener("contextmenu", e => showContextMenu(e, page.id));
        item.addEventListener("dragstart",   e => handleDragStart(e, page.id));
        item.addEventListener("dragend",     () => handleDragEnd(item));
        item.addEventListener("dragover",    e => handleDragOver(e, item));
        item.addEventListener("dragleave",   () => handleDragLeave(item));
        item.addEventListener("drop",        async e => handleDrop(e, page.id, item));

        item.append(toggle, icon, title);
        node.appendChild(item);

        if (hasChildren && state.expanded.has(page.id)) {
            const children = document.createElement("ul");
            children.className = "tree-children";
            renderPageBranch(children, page.children, depth + 1);
            node.appendChild(children);
        }
        container.appendChild(node);
    });
}

function highlightCurrentPage() {
    document.querySelectorAll(".tree-item").forEach(item =>
        item.classList.toggle("active", item.dataset.id === state.currentPage?.id)
    );
}

// ─── Context menu ─────────────────────────────────────────────────────────────
function showContextMenu(event, pageId) {
    event.preventDefault();
    const menu = document.getElementById("context-menu");
    menu.innerHTML = "";
    [
        { label: "✏️ Переименовать", action: "rename" },
        { label: "➕ Дочерняя",       action: "add-child" },
        { label: "🗑️ Удалить",        action: "delete" }
    ].forEach(entry => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.textContent = entry.label;
        btn.addEventListener("click", async () => { hideContextMenu(); await handleContextAction(entry.action, pageId); });
        menu.appendChild(btn);
    });
    menu.style.left = `${event.clientX}px`;
    menu.style.top  = `${event.clientY}px`;
    menu.classList.remove("hidden");
}

function hideContextMenu() { const m = document.getElementById("context-menu"); if (m) m.classList.add("hidden"); }

async function handleContextAction(action, pageId) {
    await flushPendingSave();
    const page = findPageById(state.pages, pageId);
    if (!page) return;
    if (action === "rename") {
        const title = window.prompt("Переименовать страницу", page.title || "Без названия");
        if (title === null) return;
        await apiRequest(`/api/pages/${pageId}`, { method: "PUT", body: JSON.stringify({ title }) });
        await loadPages();
        if (state.currentPage?.id === pageId) await openPage(pageId);
    } else if (action === "add-child") {
        openNewPageModal(pageId, null);
    } else if (action === "delete") {
        if (!confirm("Удалить страницу и всех потомков?")) return;
        await deletePage(pageId);
    }
}

// ─── Drag & drop ──────────────────────────────────────────────────────────────
function handleDragStart(event, pageId) { state.draggedPageId = pageId; event.dataTransfer.effectAllowed = "move"; event.dataTransfer.setData("text/plain", pageId); event.currentTarget.classList.add("dragging"); }
function handleDragEnd(item)            { item.classList.remove("dragging"); state.draggedPageId = null; document.querySelectorAll(".tree-item.drag-over").forEach(el => el.classList.remove("drag-over")); }
function handleDragOver(event, item)    { event.preventDefault(); if (!state.draggedPageId || item.dataset.id === state.draggedPageId) return; item.classList.add("drag-over"); }
function handleDragLeave(item)          { item.classList.remove("drag-over"); }
async function handleDrop(event, targetPageId, item) {
    event.preventDefault(); event.stopPropagation();
    item.classList.remove("drag-over");
    if (!state.draggedPageId || state.draggedPageId === targetPageId) return;
    await movePage(state.draggedPageId, targetPageId, 0);
    state.expanded.add(targetPageId);
    state.draggedPageId = null;
}
