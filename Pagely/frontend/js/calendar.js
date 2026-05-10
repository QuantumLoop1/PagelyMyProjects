// ─── calendar.js — календарь (только просмотр заметки, без редактирования) ───

function bindCalendarEvents() {
    const prevBtn  = document.getElementById("calendar-prev");
    const nextBtn  = document.getElementById("calendar-next");
    const todayBtn = document.getElementById("calendar-today");
    if (prevBtn)  prevBtn.addEventListener("click",  () => { state.calendarMonth = new Date(state.calendarMonth.getFullYear(), state.calendarMonth.getMonth() - 1, 1); renderCalendarGrid(); });
    if (nextBtn)  nextBtn.addEventListener("click",  () => { state.calendarMonth = new Date(state.calendarMonth.getFullYear(), state.calendarMonth.getMonth() + 1, 1); renderCalendarGrid(); });
    if (todayBtn) todayBtn.addEventListener("click", () => { state.calendarMonth = new Date(); renderCalendarGrid(); });

    // Закрытие pop-up по клику вне его
    document.addEventListener("click", e => {
        if (!e.target.closest(".calendar-task-chip") && !e.target.closest(".task-expanded")) {
            document.querySelectorAll(".task-expanded").forEach(el => el.remove());
        }
    });
    document.addEventListener("keydown", e => {
        if (e.key === "Escape") document.querySelectorAll(".task-expanded").forEach(el => el.remove());
    });
}

function renderCalendarGrid() {
    const grid       = document.getElementById("calendar-grid");
    const monthLabel = document.getElementById("calendar-month");
    if (!grid || !monthLabel) return;
    grid.innerHTML = "";

    const year  = state.calendarMonth.getFullYear();
    const month = state.calendarMonth.getMonth();
    const today = new Date();

    monthLabel.textContent = new Date(year, month, 1).toLocaleDateString("ru-RU", { month: "long", year: "numeric" });

    ["Пн","Вт","Ср","Чт","Пт","Сб","Вс"].forEach(d => {
        const h = document.createElement("div");
        h.className = "calendar-day-header";
        h.textContent = d;
        grid.appendChild(h);
    });

    const firstOfMonth = new Date(year, month, 1);
    let startDow = firstOfMonth.getDay();
    startDow = startDow === 0 ? 6 : startDow - 1;
    const daysInMonth     = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();
    const pagesByDate     = groupPagesByDate();

    for (let i = 0; i < startDow; i++) {
        grid.appendChild(buildCalendarCell(daysInPrevMonth - startDow + i + 1, true, [], false));
    }
    for (let day = 1; day <= daysInMonth; day++) {
        const key     = toUtcDateKey(new Date(Date.UTC(year, month, day)));
        const isToday = today.getFullYear() === year && today.getMonth() === month && today.getDate() === day;
        grid.appendChild(buildCalendarCell(day, false, pagesByDate.get(key) || [], isToday));
    }
    const remaining = (7 - ((startDow + daysInMonth) % 7)) % 7;
    for (let i = 1; i <= remaining; i++) grid.appendChild(buildCalendarCell(i, true, [], false));
}

function buildCalendarCell(dayNumber, isMuted, pages, isToday) {
    const cell = document.createElement("div");
    cell.className = "calendar-day-cell";
    if (isMuted)  cell.classList.add("calendar-day-muted");
    if (isToday)  cell.classList.add("calendar-day-today");

    const number = document.createElement("div");
    number.className = "calendar-day-number";
    number.textContent = dayNumber;
    cell.appendChild(number);

    if (pages.length) {
        const list = document.createElement("div");
        list.className = "calendar-day-tasks";

        pages.forEach(page => {
            const chip = document.createElement("div");
            const st   = normalizeStatus(page.status);
            chip.className = `calendar-task-chip status-chip-${st.toLowerCase()}`;
            chip.textContent = page.title ?? "Без названия";
            chip.title       = page.title ?? "";
            chip.style.cursor = "pointer";

            // ── ТОЛЬКО РАЗВЕРНУТЬ (read-only pop-up) ──────────────────────────
            chip.addEventListener("click", e => {
                e.stopPropagation();

                // Закрываем все уже открытые pop-up'ы
                document.querySelectorAll(".task-expanded").forEach(el => el.remove());

                const div = document.createElement("div");
                div.className = "task-expanded";

                const rawContent = page.content ?? "";
                // Показываем HTML-контент безопасно через innerText/textContent — только чистый текст
                const contentText = rawContent.replace(/<[^>]*>/g, " ").trim() || "Нет описания";

                div.innerHTML = `
                    <div class="task-expanded-header">
                        <span class="task-expanded-icon">${page.icon ?? "🗂️"}</span>
                        <span class="task-expanded-title">${page.title ?? "Без названия"}</span>
                        <button class="task-expanded-close" title="Закрыть">✕</button>
                    </div>
                    <div class="task-expanded-meta">
                        <span class="status-chip-${st.toLowerCase()} legend-chip">${st}</span>
                        ${(page.scheduledFor) ? `<span class="task-expanded-date">📅 ${formatPageDate(page.scheduledFor)}</span>` : ""}
                    </div>
                    <div class="task-expanded-content">${contentText}</div>
                    <a class="task-expanded-link" href="app.html?pageId=${page.id}">Открыть →</a>
                `;

                div.querySelector(".task-expanded-close").addEventListener("click", e2 => {
                    e2.stopPropagation();
                    div.remove();
                });

                chip.parentElement.appendChild(div);
            });

            list.appendChild(chip);
        });
        cell.appendChild(list);
    }
    return cell;
}

function groupPagesByDate() {
    const map = new Map();
    state.flatPages.forEach(page => {
        const scheduled = page.scheduledFor;
        if (!scheduled) return;
        const date = new Date(scheduled);
        if (Number.isNaN(date.getTime())) return;
        const key = toUtcDateKey(date);
        if (!map.has(key)) map.set(key, []);
        map.get(key).push(page);
    });
    return map;
}

function showCalendarError(error) {
    const grid = document.getElementById("calendar-grid");
    if (grid) grid.innerHTML = `<div class="empty-state">${error.message || "Не удалось загрузить."}</div>`;
}
