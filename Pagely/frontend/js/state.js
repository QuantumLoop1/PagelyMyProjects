// ─── state.js — глобальное состояние и константы ──────────────────────────────

const EMOJIS = ["😀","🎯","🚀","✅","📌","📝","📅","📚","💡","🎨","⚡","🧩","📈","🌿","🗂️","🔔"];

const state = {
    pages: [],
    flatPages: [],
    currentPage: null,
    expanded: new Set(),
    saveTimer: null,
    suppressEditorEvents: false,
    draggedPageId: null,
    dashboardDraggedPageId: null,
    dashboardDropStatus: null,
    dashboardSuppressClick: false,
    calendarMonth: new Date()
};
