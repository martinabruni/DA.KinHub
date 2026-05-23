const BOARD_STATUSES = [
  "backlog",
  "planned",
  "in-progress",
  "blocked",
  "implemented",
  "validated",
  "archived",
];

const TYPE_LABELS = {
  task: "Task",
  bug: "Bug",
  "change-request": "Change request",
  research: "Research",
};

const state = {
  currentView: "work-items",
  sidebarCollapsed: false,
  dbPath: "",
  workItems: [],
  activeWorkItemId: null,
  modalPayload: null,
  filters: {
    id: "",
    type: "",
    title: "",
    status: "",
  },
};

const appShell = document.querySelector("#app-shell");
const sidebarToggle = document.querySelector("#sidebar-toggle");
const navList = document.querySelector("#nav-list");
const dbPath = document.querySelector("#db-path");
const viewTitle = document.querySelector("#view-title");
const viewDescription = document.querySelector("#view-description");
const topbarStats = document.querySelector("#topbar-stats");
const workItemsCount = document.querySelector("#work-items-count");
const workItemsBody = document.querySelector("#work-items-body");
const workItemsEmpty = document.querySelector("#work-items-empty");
const boardColumns = document.querySelector("#board-columns");
const detailModal = document.querySelector("#detail-modal");
const detailModalBody = document.querySelector("#detail-modal-body");
const detailModalTitle = document.querySelector("#detail-modal-title");
const detailModalClose = document.querySelector("#detail-modal-close");

const filterInputs = {
  id: document.querySelector("#filter-id"),
  type: document.querySelector("#filter-type"),
  title: document.querySelector("#filter-title"),
  status: document.querySelector("#filter-status"),
};

function api(path) {
  return fetch(path).then((response) => {
    if (!response.ok) {
      throw new Error(`Request failed: ${response.status}`);
    }
    return response.json();
  });
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function renderInlineMarkdown(value) {
  let html = escapeHtml(value);
  html = html.replace(/`([^`]+)`/g, "<code>$1</code>");
  html = html.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");
  html = html.replace(/__([^_]+)__/g, "<strong>$1</strong>");
  html = html.replace(/\*([^*]+)\*/g, "<em>$1</em>");
  html = html.replace(
    /(https?:\/\/[^\s<]+)/g,
    '<a href="$1" target="_blank" rel="noreferrer">$1</a>'
  );
  return html;
}

function renderMarkdown(value) {
  const text = String(value ?? "").trim();
  if (!text) {
    return '<p class="markdown-empty">n/a</p>';
  }

  const blocks = [];
  const paragraph = [];
  const quoteLines = [];
  const listItems = [];
  const codeLines = [];
  let currentListType = null;
  let inCode = false;

  const flushParagraph = () => {
    if (!paragraph.length) {
      return;
    }
    blocks.push(`<p>${paragraph.map(renderInlineMarkdown).join("<br>")}</p>`);
    paragraph.length = 0;
  };

  const flushQuote = () => {
    if (!quoteLines.length) {
      return;
    }
    blocks.push(`<blockquote>${quoteLines.map(renderInlineMarkdown).join("<br>")}</blockquote>`);
    quoteLines.length = 0;
  };

  const flushList = () => {
    if (!listItems.length || !currentListType) {
      return;
    }
    const tag = currentListType === "ol" ? "ol" : "ul";
    blocks.push(
      `<${tag}>${listItems.map((item) => `<li>${renderInlineMarkdown(item)}</li>`).join("")}</${tag}>`
    );
    listItems.length = 0;
    currentListType = null;
  };

  const flushCode = () => {
    if (!codeLines.length) {
      return;
    }
    blocks.push(`<pre><code>${escapeHtml(codeLines.join("\n"))}</code></pre>`);
    codeLines.length = 0;
  };

  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.replace(/\s+$/, "");

    if (line.trim().startsWith("```")) {
      if (inCode) {
        flushCode();
      } else {
        flushParagraph();
        flushQuote();
        flushList();
      }
      inCode = !inCode;
      continue;
    }

    if (inCode) {
      codeLines.push(line);
      continue;
    }

    if (!line.trim()) {
      flushParagraph();
      flushQuote();
      flushList();
      continue;
    }

    const headingMatch = line.match(/^(#{1,6})\s+(.*)$/);
    if (headingMatch) {
      flushParagraph();
      flushQuote();
      flushList();
      const level = headingMatch[1].length;
      blocks.push(`<h${level}>${renderInlineMarkdown(headingMatch[2])}</h${level}>`);
      continue;
    }

    const quoteMatch = line.match(/^>\s?(.*)$/);
    if (quoteMatch) {
      flushParagraph();
      flushList();
      quoteLines.push(quoteMatch[1]);
      continue;
    }

    const unorderedMatch = line.match(/^\s*[-*+]\s+(.*)$/);
    if (unorderedMatch) {
      flushParagraph();
      flushQuote();
      if (currentListType && currentListType !== "ul") {
        flushList();
      }
      currentListType = "ul";
      listItems.push(unorderedMatch[1]);
      continue;
    }

    const orderedMatch = line.match(/^\s*\d+\.\s+(.*)$/);
    if (orderedMatch) {
      flushParagraph();
      flushQuote();
      if (currentListType && currentListType !== "ol") {
        flushList();
      }
      currentListType = "ol";
      listItems.push(orderedMatch[1]);
      continue;
    }

    flushQuote();
    if (currentListType) {
      flushList();
    }
    paragraph.push(line.trim());
  }

  flushParagraph();
  flushQuote();
  flushList();
  flushCode();

  return blocks.join("");
}

function renderBadge(value, kind = "default") {
  return `<span class="pill pill--${kind}">${escapeHtml(value || "n/a")}</span>`;
}

function workItemTypeClass(type) {
  return `type-${String(type || "default")}`;
}

function detailText(value) {
  const text = String(value ?? "").trim();
  return text || "n/a";
}

function renderTopbarStats(items) {
  const counts = items.reduce((map, item) => {
    map.status.set(item.status, (map.status.get(item.status) || 0) + 1);
    map.type.set(item.type, (map.type.get(item.type) || 0) + 1);
    return map;
  }, { status: new Map(), type: new Map() });

  const cards = [
    { label: "Work items", value: items.length },
    { label: "Statuses", value: counts.status.size },
    { label: "Types", value: counts.type.size },
  ];

  topbarStats.innerHTML = cards
    .map(
      (card) => `
        <span class="stat-chip">
          <span class="label">${escapeHtml(card.label)}</span>
          <strong>${escapeHtml(card.value)}</strong>
        </span>
      `
    )
    .join("");
}

function normalizeText(value) {
  return String(value ?? "").trim().toLowerCase();
}

function matchesFilters(item) {
  if (state.filters.id && !normalizeText(item.id).includes(state.filters.id)) {
    return false;
  }
  if (state.filters.type && item.type !== state.filters.type) {
    return false;
  }
  if (state.filters.title && !normalizeText(item.title).includes(state.filters.title)) {
    return false;
  }
  if (state.filters.status && item.status !== state.filters.status) {
    return false;
  }
  return true;
}

function filteredWorkItems() {
  return state.workItems.filter(matchesFilters);
}

function rowHtml(item) {
  return `
    <tr class="${state.activeWorkItemId === item.id ? "active" : ""}" data-work-item-id="${escapeHtml(item.id)}">
      <td><code>${escapeHtml(item.id)}</code></td>
      <td>${renderBadge(TYPE_LABELS[item.type] || item.type, workItemTypeClass(item.type))}</td>
      <td>
        <button type="button" class="link-button" data-open-work-item="${escapeHtml(item.id)}">${escapeHtml(item.title)}</button>
      </td>
      <td>${renderBadge(item.status, "status")}</td>
    </tr>
  `;
}

function renderWorkItemsTable() {
  const items = filteredWorkItems();
  workItemsCount.textContent = String(items.length);

  if (!items.length) {
    workItemsBody.innerHTML = "";
    workItemsEmpty.textContent = "No work items match the current filters.";
    workItemsEmpty.classList.remove("hidden");
    return;
  }

  workItemsEmpty.classList.add("hidden");
  workItemsBody.innerHTML = items.map(rowHtml).join("");
}

function renderBoard() {
  const items = filteredWorkItems();
  const grouped = Object.fromEntries(BOARD_STATUSES.map((status) => [status, []]));

  for (const item of items) {
    if (!grouped[item.status]) {
      grouped[item.status] = [];
    }
    grouped[item.status].push(item);
  }

  boardColumns.innerHTML = BOARD_STATUSES.map((status) => {
    const cards = (grouped[status] || [])
      .map(
        (item) => `
          <button type="button" class="board-card ${state.activeWorkItemId === item.id ? "active" : ""}" data-open-work-item="${escapeHtml(item.id)}">
            <div class="section-header">
              <code>${escapeHtml(item.id)}</code>
              ${renderBadge(TYPE_LABELS[item.type] || item.type, workItemTypeClass(item.type))}
            </div>
            <div class="work-item-title"><strong>${escapeHtml(item.title)}</strong></div>
            <div class="detail-line">${renderBadge(item.status, "status")}</div>
          </button>
        `
      )
      .join("");

    return `
      <article class="board-column panel">
        <div class="section-header">
          <h3>${escapeHtml(status)}</h3>
          <span class="pill">${escapeHtml((grouped[status] || []).length)}</span>
        </div>
        <div class="board-column-items">
          ${cards || '<div class="empty-state">No items.</div>'}
        </div>
      </article>
    `;
  }).join("");
}

function setView(view) {
  state.currentView = view;

  for (const button of navList.querySelectorAll("[data-view]")) {
    button.classList.toggle("active", button.dataset.view === view);
  }

  document.querySelector("#work-items-view").classList.toggle("hidden", view !== "work-items");
  document.querySelector("#board-view").classList.toggle("hidden", view !== "board");

  viewTitle.textContent = view === "board" ? "Board" : "Work items";
  viewDescription.textContent =
    view === "board"
      ? "Horizontal kanban grouped by tracker status."
      : "Table with per-column filters in the header.";
}

function setSidebarCollapsed(collapsed) {
  state.sidebarCollapsed = collapsed;
  appShell.classList.toggle("sidebar-collapsed", collapsed);
  sidebarToggle.innerHTML = collapsed ? "⟩" : "⟨";
  sidebarToggle.setAttribute("aria-label", collapsed ? "Expand sidebar" : "Collapse sidebar");
}

function closeModal() {
  detailModal.classList.add("hidden");
  detailModalBody.innerHTML = "";
  detailModalTitle.textContent = "Loading...";
  document.body.classList.remove("modal-open");
  state.modalPayload = null;
}

function renderPairs(entries) {
  return `
    <div class="detail-pairs">
      ${entries
        .map(
          ([label, value]) => `
            <div class="detail-pair">
              <span class="label">${escapeHtml(label)}</span>
              <div>${renderMarkdown(value)}</div>
            </div>
          `
        )
        .join("")}
    </div>
  `;
}

function renderAssociatedItems(items) {
  if (!items.length) {
    return '<div class="empty-state">No associated items.</div>';
  }

  return items
    .map(
      (item) => `
        <article class="associated-item">
          <div class="section-header">
            <strong>${escapeHtml(item.id)}</strong>
            <span class="pill muted">${escapeHtml(item.relation)}</span>
          </div>
          <div>${escapeHtml(item.title)}</div>
          <div class="section-header">
            ${renderBadge(TYPE_LABELS[item.type] || item.type, workItemTypeClass(item.type))}
            ${renderBadge(item.status, "status")}
          </div>
          ${item.notes ? `<div class="label">${escapeHtml(item.notes)}</div>` : ""}
        </article>
      `
    )
    .join("");
}

function renderDetailModal(payload) {
  const item = payload.item || {};
  const feature = payload.feature || null;
  const associatedItems = payload.associated_items || [];

  detailModalTitle.textContent = `${item.id || "Work item"} — ${item.title || "detail"}`;
  detailModalBody.innerHTML = `
    <div class="detail-grid">
      <section class="section-card">
        <h3>Work item</h3>
        ${renderPairs([
          ["Id", item.id],
          ["Title", item.title],
          ["Type", TYPE_LABELS[item.type] || item.type],
          ["Status", item.status],
          ["Feature", item.feature_id],
          ["Description", item.summary],
          ["Implementation notes", item.implementation_notes],
        ])}
      </section>
      <section class="section-card">
        <h3>Feature</h3>
        ${feature
          ? renderPairs([
              ["Id", feature.id],
              ["Title", feature.title],
              ["Description", feature.description],
            ])
          : '<div class="empty-state">No feature data.</div>'}
      </section>
    </div>
    <section class="section-card">
      <h3>Associated items</h3>
      <div class="detail-list">
        ${renderAssociatedItems(associatedItems)}
      </div>
    </section>
    <section class="section-card">
      <h3>Links</h3>
      <div class="detail-grid">
        <div class="detail-list">
          <h4>Outgoing</h4>
          ${
            (payload.outgoing_links || []).length
              ? payload.outgoing_links
                  .map(
                    (link) => `
                      <article class="link-card">
                        <strong>#${escapeHtml(link.id)}</strong>
                        <div>${escapeHtml(link.relation_type)} → ${escapeHtml(link.target_work_item_id)}</div>
                        <small>${escapeHtml(link.notes || "no notes")}</small>
                      </article>
                    `
                  )
                  .join("")
              : '<div class="empty-state">No outgoing links.</div>'
          }
        </div>
        <div class="detail-list">
          <h4>Incoming</h4>
          ${
            (payload.incoming_links || []).length
              ? payload.incoming_links
                  .map(
                    (link) => `
                      <article class="link-card">
                        <strong>#${escapeHtml(link.id)}</strong>
                        <div>${escapeHtml(link.source_work_item_id)} → ${escapeHtml(link.relation_type)}</div>
                        <small>${escapeHtml(link.notes || "no notes")}</small>
                      </article>
                    `
                  )
                  .join("")
              : '<div class="empty-state">No incoming links.</div>'
          }
        </div>
      </div>
    </section>
    <section class="section-card">
      <h3>History</h3>
      <div class="detail-list">
        ${
          (payload.history || []).length
            ? payload.history
                .map(
                  (entry) => `
                    <article class="history-item">
                      <div class="section-header">
                        <strong>${escapeHtml(entry.action)}</strong>
                        <span class="label">${escapeHtml(entry.created_at)}</span>
                      </div>
                      <div>${escapeHtml(entry.entity_type)} • ${escapeHtml(entry.entity_id)}</div>
                    </article>
                  `
                )
                .join("")
            : '<div class="empty-state">No history entries.</div>'
        }
      </div>
    </section>
  `;
}

function openModal(payload) {
  state.modalPayload = payload;
  renderDetailModal(payload);
  detailModal.classList.remove("hidden");
  document.body.classList.add("modal-open");
}

function collectAssociatedItems(payload) {
  const items = new Map();
  const push = (entry) => {
    if (!entry || !entry.id || entry.id === payload.item.id) {
      return;
    }
    const existing = items.get(entry.id);
    if (existing) {
      existing.relation = `${existing.relation}, ${entry.relation}`;
      return;
    }
    items.set(entry.id, { ...entry });
  };

  for (const item of payload.feature_items || []) {
    push({
      ...item,
      relation: "same feature",
    });
  }

  for (const link of payload.outgoing_links || []) {
    if (link.target_item) {
      push({
        ...link.target_item,
        relation: `outgoing: ${link.relation_type}`,
        notes: link.notes,
      });
    }
  }

  for (const link of payload.incoming_links || []) {
    if (link.source_item) {
      push({
        ...link.source_item,
        relation: `incoming: ${link.relation_type}`,
        notes: link.notes,
      });
    }
  }

  return [...items.values()].sort((left, right) => left.id.localeCompare(right.id));
}

async function loadWorkItem(workItemIdValue) {
  state.activeWorkItemId = workItemIdValue;

  try {
    const payload = await api(`/api/work-items/${encodeURIComponent(workItemIdValue)}`);
    payload.associated_items = collectAssociatedItems(payload);
    openModal(payload);
  } catch (error) {
    state.modalPayload = null;
    detailModalTitle.textContent = `Failed to load ${workItemIdValue}`;
    detailModalBody.innerHTML = `<div class="empty-state">Failed to load work item: ${escapeHtml(error.message)}</div>`;
    detailModal.classList.remove("hidden");
    document.body.classList.add("modal-open");
  }

  renderWorkItemsTable();
  renderBoard();
}

function bindTableFilters() {
  for (const [key, element] of Object.entries(filterInputs)) {
    element.addEventListener("input", () => {
      state.filters[key] = normalizeText(element.value);
      renderWorkItemsTable();
      renderBoard();
    });
  }
}

function bindNavigation() {
  for (const button of navList.querySelectorAll("[data-view]")) {
    button.addEventListener("click", () => setView(button.dataset.view));
  }
}

function bindClicks() {
  document.addEventListener("click", (event) => {
    const trigger = event.target.closest("[data-open-work-item]");
    if (trigger) {
      loadWorkItem(trigger.dataset.openWorkItem);
      return;
    }

    if (event.target === detailModal) {
      closeModal();
    }
  });
}

function bindKeyboard() {
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && !detailModal.classList.contains("hidden")) {
      closeModal();
    }
  });
}

async function loadData() {
  const payload = await api("/api/work-items");
  state.workItems = payload.work_items || [];
  state.dbPath = payload.db_path || "";
  dbPath.textContent = state.dbPath;
  renderTopbarStats(state.workItems);
  renderWorkItemsTable();
  renderBoard();
}

async function start() {
  bindTableFilters();
  bindNavigation();
  bindClicks();
  bindKeyboard();
  setSidebarCollapsed(false);
  setView("work-items");

  detailModalClose.addEventListener("click", closeModal);
  sidebarToggle.addEventListener("click", () => setSidebarCollapsed(!state.sidebarCollapsed));

  try {
    await loadData();
  } catch (error) {
    const message = `Failed to load tracker UI: ${error.message}`;
    workItemsEmpty.textContent = message;
    workItemsEmpty.classList.remove("hidden");
    boardColumns.innerHTML = `<div class="empty-state">${escapeHtml(message)}</div>`;
  }
}

start();
