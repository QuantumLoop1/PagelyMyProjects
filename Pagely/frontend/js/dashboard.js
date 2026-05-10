// ─── dashboard.js — канбан-доска ──────────────────────────────────────────────

function renderDashboardBoard() {
    const board = document.getElementById("dashboard-board");
    if (!board) return;
    board.innerHTML = "";
    const statuses = ["Todo", "Doing", "Done"];
    const statusIcons = { Todo: "📋", Doing: "⚡", Done: "✅" };
    const statusColors = {
        Todo:  { bg: "rgba(255,163,51,0.08)",  badge: "rgba(255,163,51,0.22)",  text: "#c46600" },
        Doing: { bg: "rgba(31,122,236,0.06)",  badge: "rgba(31,122,236,0.18)",  text: "#1f7aec" },
        Done:  { bg: "rgba(46,160,67,0.06)",   badge: "rgba(46,160,67,0.18)",   text: "#2ea043" }
    };

    statuses.forEach(status => {
        const col = document.createElement("div");
        col.className = "board-column";
        col.dataset.status = status;
        const pages = state.flatPages.filter(p => normalizeStatus(p.status) === status);
        const colors = statusColors[status];
        col.style.background = colors.bg;

        const header = document.createElement("div");
        header.className = "board-column-header";
        const left = document.createElement("div");
        left.className = "board-column-header-left";
        const icon = document.createElement("span");
        icon.textContent = statusIcons[status];
        const titleEl = document.createElement("span");
        titleEl.className = "board-column-title";
        titleEl.textContent = status;
        const badge = document.createElement("span");
        badge.className = "board-column-count";
        badge.textContent = pages.length;
        badge.style.background = colors.badge;
        badge.style.color = colors.text;
        left.append(icon, titleEl, badge);

        const addBtn = document.createElement("button");
        addBtn.className = "board-add-btn";
        addBtn.type = "button";
        addBtn.title = "Добавить заметку";
        addBtn.textContent = "+";
        addBtn.addEventListener("click", () => openNewPageModal(null, status));
        header.append(left, addBtn);
        col.appendChild(header);

        const list = document.createElement("div");
        list.className = "board-column-list";

        list.addEventListener("dragover", event => {
            if (!state.dashboardDraggedPageId) return;
            event.preventDefault();
            event.dataTransfer.dropEffect = "move";
            col.classList.add("board-column-drop-target");
        });
        list.addEventListener("dragleave", event => {
            if (!list.contains(event.relatedTarget)) col.classList.remove("board-column-drop-target");
        });
        list.addEventListener("drop", async event => {
            event.preventDefault();
            col.classList.remove("board-column-drop-target");
            if (!state.dashboardDraggedPageId) return;
            await moveDashboardCardToStatus(state.dashboardDraggedPageId, status, col);
        });

        if (!pages.length) {
            const empty = document.createElement("div");
            empty.className = "board-empty";
            empty.textContent = "Нет заметок";
            list.appendChild(empty);
        } else {
            pages.forEach(page => {
                const card = document.createElement("div");
                card.className = "board-card";
                card.draggable = true;
                card.dataset.id = page.id;
                card.dataset.status = status;
                card.style.cursor = "pointer";
                card.addEventListener("click", () => {
                    if (state.dashboardSuppressClick) return;
                    window.location.href = `app.html?pageId=${page.id}`;
                });
                card.addEventListener("dragstart", event => {
                    state.dashboardDraggedPageId = page.id;
                    state.dashboardDropStatus    = status;
                    state.dashboardSuppressClick = true;
                    event.dataTransfer.effectAllowed = "move";
                    event.dataTransfer.setData("text/plain", String(page.id));
                    card.classList.add("board-card-dragging");
                });
                card.addEventListener("dragend", () => {
                    card.classList.remove("board-card-dragging");
                    document.querySelectorAll(".board-column-drop-target").forEach(el => el.classList.remove("board-column-drop-target"));
                    state.dashboardDraggedPageId = null;
                    state.dashboardDropStatus    = null;
                    setTimeout(() => { state.dashboardSuppressClick = false; }, 80);
                });
                const cardIcon = document.createElement("span");
                cardIcon.className = "board-card-icon";
                cardIcon.textContent = page.icon ?? "🗂️";
                const cardBody = document.createElement("div");
                cardBody.className = "board-card-body";
                const cardTitle = document.createElement("div");
                cardTitle.className = "board-card-title";
                cardTitle.textContent = page.title ?? "Без названия";
                const cardMeta = document.createElement("div");
                cardMeta.className = "board-card-meta";
                const scheduled = page.scheduledFor;
                cardMeta.textContent = scheduled ? "📅 " + formatPageDate(scheduled) : "Без даты";
                cardBody.append(cardTitle, cardMeta);
                card.append(cardIcon, cardBody);
                list.appendChild(card);
            });
        }
        col.appendChild(list);
        board.appendChild(col);
    });
}

async function moveDashboardCardToStatus(pageId, nextStatus, targetColumnEl) {
    const page = state.flatPages.find(p => p.id === pageId);
    if (!page) return;

    const currentStatus = normalizeStatus(page.status);
    if (currentStatus === nextStatus) {
        if (targetColumnEl) {
            targetColumnEl.classList.add("board-column-drop-success");
            setTimeout(() => targetColumnEl.classList.remove("board-column-drop-success"), 420);
        }
        return;
    }

    const previousStatus = currentStatus;
    page.status = nextStatus;
    page.status = nextStatus;
    renderDashboardBoard();

    const movedCard = document.querySelector(`.board-card[data-id="${CSS.escape(String(pageId))}"]`);
    if (movedCard) {
        movedCard.classList.add("board-card-placed");
        setTimeout(() => movedCard.classList.remove("board-card-placed"), 560);
    }

    try {
        await apiRequest(`/api/pages/${pageId}`, {
            method: "PUT",
            body: JSON.stringify({
                title:        page.title        ?? "",
                content:      page.content      ?? "",
                icon:         page.icon         ?? "🗂️",
                coverColor:   page.coverColor   ?? null,
                status:       nextStatus,
                scheduledFor: page.scheduledFor ?? null
            })
        });
        await loadPages();
        renderDashboardBoard();
    } catch (error) {
        page.status = previousStatus;
        page.status = previousStatus;
        renderDashboardBoard();
        showBoardError(error);
    }
}

function showBoardError(error) {
    const board = document.getElementById("dashboard-board");
    if (board) board.innerHTML = `<div class="empty-state">${error.message || "Не удалось загрузить."}</div>`;
}
