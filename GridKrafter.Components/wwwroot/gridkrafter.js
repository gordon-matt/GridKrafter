// GridKrafter's browser side. It reports pointer positions in grid cells, widths and content
// heights to .NET and animates what Blazor renders; every decision about the layout is made in .NET.

const states = new WeakMap();
const INTERACTIVE = 'button, a[href], input, select, textarea, label, summary, [contenteditable=""], [contenteditable="true"], [data-gk-nodrag]';
const THRESHOLD = 4;
const SCROLL_EDGE = 56;
const SCROLL_SPEED = 20;
const SETTLE_MS = 200;
const STORAGE_PREFIX = 'gridkrafter:';
const OPTION_EVENT = 'gk-option';
let scrollLocks = 0;

const reducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true;

export function attach(root, dotnet, optionsJson) {
    detach(root);
    const state = {
        root,
        dotnet,
        options: parseOptions(optionsJson),
        layoutId: null,
        drag: null,
        pending: null,
        inflight: null,
        observed: new Map(),
        editObserved: new Map(),
        sentRows: new Map(),
        sentEditRows: new Map(),
        rowsToSend: null,
        editRowsToSend: null,
        cleanup: [],
    };
    states.set(root, state);

    const onPointerDown = event => pointerDown(state, event);
    const onKeyDown = event => keyDown(state, event);
    // A native drag (of selected text, an image or a link in the header) would swallow the pointer events.
    const onDragStart = event => state.drag && event.preventDefault();
    const onOption = event => setOption(state, event);
    root.addEventListener('pointerdown', onPointerDown);
    root.addEventListener('keydown', onKeyDown);
    root.addEventListener('dragstart', onDragStart);
    root.addEventListener(OPTION_EVENT, onOption);
    state.cleanup.push(() => {
        root.removeEventListener('pointerdown', onPointerDown);
        root.removeEventListener('keydown', onKeyDown);
        root.removeEventListener('dragstart', onDragStart);
        root.removeEventListener(OPTION_EVENT, onOption);
    });

    state.sizer = new ResizeObserver(() => reportWidth(state));
    state.sizer.observe(root);
    state.heights = new ResizeObserver(entries => measured(state, entries));

    const width = widthOf(state);
    state.layoutId = layoutFor(state, width);
    hydrate(state);
    observeHeights(state);
    observeEdits(state);
    return JSON.stringify({ width, offset: -new Date().getTimezoneOffset() });
}

export function afterRender(root, optionsJson) {
    const state = states.get(root);
    if (!state) {
        return;
    }

    if (optionsJson) {
        state.options = parseOptions(optionsJson);
        reportWidth(state);
    }

    hydrate(state);
    observeHeights(state);
    observeEdits(state);
    fitHeaders(state.root);
}

export function detach(root) {
    const state = states.get(root);
    if (!state) {
        return;
    }

    if (state.drag) {
        endListeners(state);
        clearVisuals(state.drag);
        document.documentElement.removeAttribute('data-gk-dragging');
    }

    state.sizer?.disconnect();
    state.heights?.disconnect();
    state.cleanup.forEach(dispose => dispose());
    states.delete(root);
}

export async function reload(root, itemId) {
    const state = states.get(root);
    const mount = state && findMount(root, itemId);
    return mount ? await fill(state, mount) : null;
}

export function scrollIntoView(root, itemId) {
    const item = root.querySelector(`[data-gk-item="${CSS.escape(itemId)}"]`);
    if (!item) {
        return;
    }

    item.scrollIntoView({ block: 'nearest', behavior: reducedMotion() ? 'auto' : 'smooth' });
    item.classList.remove('gk-item-flash');
    void item.offsetWidth;
    item.classList.add('gk-item-flash');
    setTimeout(() => item.classList.remove('gk-item-flash'), 1200);
}

export function lockScroll(locked) {
    scrollLocks = Math.max(0, scrollLocks + (locked ? 1 : -1));
    document.documentElement.classList.toggle('gk-noscroll', scrollLocks > 0);
}

export function timeOffset() {
    return -new Date().getTimezoneOffset();
}

export function load(key) {
    try {
        return localStorage.getItem(STORAGE_PREFIX + key);
    } catch {
        return null;
    }
}

export function save(key, json) {
    try {
        localStorage.setItem(STORAGE_PREFIX + key, json);
    } catch {
        // Storage is full or blocked; the dashboard still works for this visit.
    }
}

export function remove(key) {
    try {
        localStorage.removeItem(STORAGE_PREFIX + key);
    } catch {
        // Nothing to forget.
    }
}

export function focus(element) {
    element?.focus?.();
}

export function dispatch(element, eventName, detailJson) {
    element?.dispatchEvent(new CustomEvent(eventName, {
        bubbles: true,
        composed: true,
        detail: detailJson ? JSON.parse(detailJson) : null,
    }));
}

// ---------------------------------------------------------------- size

function parseOptions(json) {
    const options = json ? JSON.parse(json) : {};
    return {
        row: options.row ?? 60,
        gap: options.gap ?? 12,
        layouts: options.layouts ?? [],
        loadFailed: options.loadFailed ?? 'This content couldn\'t be loaded ({0}).',
    };
}

function widthOf(state) {
    const sections = state.root.querySelector(':scope > .gk-sections');
    return (sections ?? state.root).getBoundingClientRect().width;
}

function layoutFor(state, width) {
    const layouts = state.options.layouts;
    if (!layouts.length) {
        return null;
    }

    return (layouts.find(l => width >= l.minWidth) ?? layouts[layouts.length - 1]).id;
}

function reportWidth(state) {
    const width = widthOf(state);
    if (width <= 0) {
        return;
    }

    const id = layoutFor(state, width);
    if (id === state.layoutId) {
        return;
    }

    state.layoutId = id;
    if (state.drag) {
        abort(state);
    }

    state.dotnet.invokeMethodAsync('OnWidth', width).catch(() => { });
}

// ---------------------------------------------------------------- content

// How content that isn't a Blazor component (a React widget, a partial view's script) saves its settings.
function setOption(state, event) {
    const item = event.target.closest?.('[data-gk-item]');
    const key = event.detail?.key;
    if (!item || ownerOf(item) !== state.root || typeof key !== 'string' || !key) {
        return;
    }

    event.stopPropagation();
    const value = JSON.stringify(event.detail.value ?? null);
    state.dotnet.invokeMethodAsync('OnOption', item.dataset.gkItem, key, value).catch(() => { });
}

function findMount(root, itemId) {
    return root.querySelector(`[data-gk-mount="${CSS.escape(itemId)}"]`);
}

function hydrate(state) {
    for (const mount of state.root.querySelectorAll('[data-gk-mount][data-gk-url], [data-gk-mount][data-gk-template]')) {
        if (ownerOf(mount) === state.root && mount.dataset.gkLoaded !== sourceKey(mount)) {
            fill(state, mount);
        }
    }
}

function sourceKey(mount) {
    return mount.dataset.gkUrl ? `url:${mount.dataset.gkUrl}` : `template:${mount.dataset.gkTemplate}`;
}

async function fill(state, mount) {
    const key = sourceKey(mount);
    mount.dataset.gkLoaded = key;
    const id = mount.dataset.gkMount;

    if (!mount.dataset.gkUrl) {
        const template = safeQuery(mount.dataset.gkTemplate);
        if (template instanceof HTMLTemplateElement) {
            mount.replaceChildren(template.content.cloneNode(true));
            announce(mount, id);
            return null;
        }

        const message = `${mount.dataset.gkTemplate} not found`;
        mount.replaceChildren(failure(state, message));
        return message;
    }

    mount.setAttribute('aria-busy', 'true');
    try {
        const response = await fetch(mount.dataset.gkUrl, {
            headers: { 'X-Requested-With': 'GridKrafter' },
            credentials: 'same-origin',
        });
        if (!response.ok) {
            throw new Error(`${response.status} ${response.statusText}`.trim());
        }

        const html = await response.text();
        if (mount.dataset.gkLoaded !== key || !mount.isConnected) {
            return null;
        }

        mount.innerHTML = html;
        if (new URL(response.url, location.href).origin === location.origin) {
            runScripts(mount);
        }

        announce(mount, id);
        return null;
    } catch (error) {
        const message = error?.message || String(error);
        if (mount.dataset.gkLoaded === key) {
            mount.replaceChildren(failure(state, message));
        }

        return message;
    } finally {
        mount.removeAttribute('aria-busy');
    }
}

function safeQuery(selector) {
    try {
        return selector ? document.querySelector(selector) : null;
    } catch {
        return null;
    }
}

function failure(state, message) {
    const p = document.createElement('p');
    p.className = 'gk-load-error';
    p.setAttribute('role', 'alert');
    p.textContent = state.options.loadFailed.replace('{0}', message);
    return p;
}

// Partial views often carry an inline script for their chart or table; markup set through
// innerHTML never runs them, so they're recreated, as jQuery's load() used to do.
function runScripts(container) {
    for (const old of container.querySelectorAll('script')) {
        const script = document.createElement('script');
        for (const { name, value } of old.attributes) {
            script.setAttribute(name, value);
        }

        script.textContent = old.textContent;
        old.replaceWith(script);
    }
}

function announce(mount, id) {
    mount.dispatchEvent(new CustomEvent('gk-loaded', { bubbles: true, composed: true, detail: { id } }));
}

// ---------------------------------------------------------------- auto height

// The header, edit box and footer count as much as the content: a footer that renders late, a header
// that wraps or an edit box that opens all change the height a widget needs. They're added up rather
// than taken from what's left beside the body, because a widget shorter than its chrome squeezes the body to nothing.
const CHROME_PARTS = ':scope > .gk-widget-header, :scope > .gk-widget-editbox, :scope > .gk-widget-footer';
const MEASURED_PARTS = `${CHROME_PARTS}, :scope > .gk-widget-body > .gk-widget-flow`;

function widgetOf(item) {
    return item.querySelector(':scope > .gk-item-inner > .gk-widget');
}

function observeHeights(state) {
    const current = new Map();
    for (const item of state.root.querySelectorAll('[data-gk-item][data-gk-autoheight]')) {
        if (ownerOf(item) !== state.root) {
            continue;
        }

        for (const part of widgetOf(item)?.querySelectorAll(MEASURED_PARTS) ?? []) {
            current.set(part, item);
            if (!state.observed.has(part)) {
                state.heights.observe(part, { box: 'border-box' });
            }
        }
    }

    const changed = new Set();
    for (const [part, item] of state.observed) {
        if (!current.has(part)) {
            state.heights.unobserve(part);
            changed.add(item);
        }
    }

    state.observed = current;
    if (changed.size > 0) {
        measure(state, changed);
    }
}

function measured(state, entries) {
    const items = new Set();
    const edits = new Set();
    for (const entry of entries) {
        const item = state.observed.get(entry.target);
        if (item) {
            items.add(item);
        }

        const editing = state.editObserved.get(entry.target);
        if (editing) {
            edits.add(editing);
        }
    }

    if (items.size > 0) {
        measure(state, items);
    }

    if (edits.size > 0) {
        measureEdits(state, edits);
    }
}

function measure(state, items) {
    for (const item of items) {
        const widget = widgetOf(item);
        const body = widget?.querySelector(':scope > .gk-widget-body');
        const flow = body?.querySelector(':scope > .gk-widget-flow');
        if (!item.isConnected || !item.hasAttribute('data-gk-autoheight') || !flow || flow.offsetHeight === 0) {
            continue;
        }

        const frame = getComputedStyle(widget);
        const style = getComputedStyle(body);
        let chrome = parseFloat(frame.borderTopWidth) + parseFloat(frame.borderBottomWidth);
        for (const part of widget.querySelectorAll(CHROME_PARTS)) {
            chrome += part.offsetHeight;
        }

        const padding = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom);
        const natural = chrome + padding + flow.offsetHeight;
        const { row, gap } = state.options;
        const rows = Math.max(1, Math.ceil((natural + gap) / (row + gap)));
        const id = item.dataset.gkItem;
        if (state.sentRows.get(id) !== rows) {
            state.sentRows.set(id, rows);
            (state.rowsToSend ??= {})[id] = rows;
        }
    }

    if (state.rowsToSend && !state.rowsScheduled) {
        state.rowsScheduled = true;
        requestAnimationFrame(() => {
            state.rowsScheduled = false;
            const rows = state.rowsToSend;
            state.rowsToSend = null;
            if (rows) {
                state.dotnet.invokeMethodAsync('OnMeasured', JSON.stringify(rows)).catch(() => { });
            }
        });
    }
}

// A fixed-height widget can't scroll its edit panel. The panel's own height, in rows, is added to the
// cell until the panel closes. Auto-height widgets already count the panel with the rest of their chrome.
function observeEdits(state) {
    const current = new Map();
    const gone = [];
    for (const item of state.root.querySelectorAll('[data-gk-item][data-gk-editgrow]')) {
        if (ownerOf(item) !== state.root) {
            continue;
        }

        const box = widgetOf(item)?.querySelector(':scope > .gk-widget-editbox');
        if (!box) {
            continue;
        }

        current.set(box, item);
        if (!state.editObserved.has(box)) {
            state.heights.observe(box, { box: 'border-box' });
        }
    }

    for (const [box, item] of state.editObserved) {
        if (!current.has(box)) {
            state.heights.unobserve(box);
            gone.push(item);
        }
    }

    state.editObserved = current;
    for (const item of gone) {
        queueEditRows(state, item.dataset.gkItem, 0);
    }

    measureEdits(state, new Set(current.values()));
}

function measureEdits(state, items) {
    const pitch = state.options.row + state.options.gap;
    for (const item of items) {
        const box = widgetOf(item)?.querySelector(':scope > .gk-widget-editbox');
        const height = box ? Math.max(box.offsetHeight, box.scrollHeight) : 0;
        const extra = height > 0 ? Math.ceil(height / pitch) : 0;
        queueEditRows(state, item.dataset.gkItem, extra);
    }

    if (state.editRowsToSend && !state.editScheduled) {
        state.editScheduled = true;
        requestAnimationFrame(() => {
            state.editScheduled = false;
            const rows = state.editRowsToSend;
            state.editRowsToSend = null;
            if (rows) {
                state.dotnet.invokeMethodAsync('OnEditGrown', JSON.stringify(rows)).catch(() => { });
            }
        });
    }
}

function queueEditRows(state, id, extra) {
    if (!id || state.sentEditRows.get(id) === extra) {
        return;
    }

    state.sentEditRows.set(id, extra);
    (state.editRowsToSend ??= {})[id] = extra;
}

// ---------------------------------------------------------------- pointer

function ownerOf(element) {
    return element.closest('.gk-dashboard');
}

function pointerDown(state, event) {
    if (event.button !== 0 || !event.isPrimary || state.drag?.started) {
        return;
    }

    // A press that never moved far enough may never have seen its pointerup; it mustn't block this one.
    if (state.drag) {
        endListeners(state);
        state.drag = null;
    }

    const item = event.target.closest('[data-gk-item]');
    if (!item || ownerOf(item) !== state.root) {
        return;
    }

    const edge = event.target.closest('[data-gk-resize]');
    if (edge) {
        event.preventDefault();
        begin(state, event, item, 'resize', edge.dataset.gkResize);
        return;
    }

    const handle = event.target.closest('[data-gk-handle]');
    if (!handle || handle.closest('[data-gk-item]') !== item) {
        return;
    }

    const grip = event.target.closest('[data-gk-grip]');
    if (!grip && event.target.closest(INTERACTIVE)) {
        return;
    }

    begin(state, event, item, 'move', null);
}

function begin(state, event, item, mode, edge) {
    const inner = item.querySelector(':scope > .gk-item-inner');
    if (!inner) {
        return;
    }

    if (openHeaders.size > 0) {
        closeMenus();
    }

    const drag = {
        mode,
        edge,
        item,
        inner,
        id: item.dataset.gkItem,
        pointerId: event.pointerId,
        startX: event.clientX,
        startY: event.clientY,
        clientX: event.clientX,
        clientY: event.clientY,
        scrollX: window.scrollX,
        scrollY: window.scrollY,
        rect: inner.getBoundingClientRect(),
        cell: cellOf(item),
        grid: item.closest('[data-gk-grid]'),
        started: false,
        accepted: null,
    };
    state.drag = drag;

    drag.onMove = e => pointerMove(state, e);
    drag.onUp = e => pointerUp(state, e, true);
    drag.onCancel = e => pointerUp(state, e, false);
    drag.onKey = e => {
        if (e.key === 'Escape') {
            e.preventDefault();
            finish(state, false);
        }
    };
    drag.onScroll = () => drag.started && update(state);
    window.addEventListener('pointermove', drag.onMove, { passive: false });
    window.addEventListener('pointerup', drag.onUp);
    window.addEventListener('pointercancel', drag.onCancel);
    window.addEventListener('keydown', drag.onKey, true);
    window.addEventListener('scroll', drag.onScroll, { passive: true });
}

function endListeners(state) {
    const drag = state.drag;
    if (!drag) {
        return;
    }

    window.removeEventListener('pointermove', drag.onMove);
    window.removeEventListener('pointerup', drag.onUp);
    window.removeEventListener('pointercancel', drag.onCancel);
    window.removeEventListener('keydown', drag.onKey, true);
    window.removeEventListener('scroll', drag.onScroll);
    cancelAnimationFrame(drag.scrollFrame);
    try {
        state.root.releasePointerCapture?.(drag.pointerId);
    } catch {
        // The pointer was already released.
    }
}

function pointerMove(state, event) {
    const drag = state.drag;
    if (!drag || event.pointerId !== drag.pointerId) {
        return;
    }

    drag.clientX = event.clientX;
    drag.clientY = event.clientY;
    if (!drag.started) {
        if (Math.hypot(drag.clientX - drag.startX, drag.clientY - drag.startY) < THRESHOLD) {
            return;
        }

        start(state);
    }

    event.preventDefault();
    update(state);
}

function pointerUp(state, event, commit) {
    if (state.drag && event.pointerId === state.drag.pointerId) {
        finish(state, commit);
    }
}

function start(state) {
    const drag = state.drag;
    drag.started = true;
    document.documentElement.setAttribute('data-gk-dragging', drag.mode);
    window.getSelection?.()?.removeAllRanges();
    drag.item.classList.add('gk-item-lifting');
    try {
        state.root.setPointerCapture?.(drag.pointerId);
    } catch {
        // Capture is a nicety; window listeners still see the pointer.
    }

    drag.accepted = state.dotnet
        .invokeMethodAsync(drag.mode === 'move' ? 'OnDragStart' : 'OnResizeStart', drag.id)
        .then(ok => {
            if (!ok && state.drag === drag) {
                abort(state);
            }

            return ok;
        }, () => {
            if (state.drag === drag) {
                abort(state);
            }

            return false;
        });
    autoScroll(state);
}

function update(state) {
    const drag = state.drag;
    if (!drag?.started) {
        return;
    }

    const { gap, row } = state.options;
    const rtl = getComputedStyle(state.root).direction === 'rtl';
    const dx = drag.clientX - drag.startX;
    const dy = drag.clientY - drag.startY;
    const scrollDX = window.scrollX - drag.scrollX;
    const scrollDY = window.scrollY - drag.scrollY;

    if (drag.mode === 'move') {
        drag.inner.style.transform = `translate(${dx + scrollDX}px, ${dy + scrollDY}px)`;
        // The lifted widget sits under the pointer, so hit-testing can't see the tab beneath it.
        const tabId = tabAt(state, drag.clientX, drag.clientY);
        if (tabId) {
            revealSection(state, tabId);
        }

        const grid = (tabId && gridById(state, tabId)) || gridAt(state, drag.clientY) || drag.grid;
        if (!grid) {
            return;
        }

        const bounds = grid.getBoundingClientRect();
        const columns = columnsOf(grid);
        const colPitch = (bounds.width + gap) / columns;
        const left = drag.rect.left + dx;
        const right = drag.rect.right + dx;
        const top = drag.rect.top + dy - bounds.top;
        const x = Math.max(0, Math.round((rtl ? bounds.right - right : left - bounds.left) / colPitch));
        const y = Math.max(0, Math.round(top / (row + gap)));
        send(state, 'OnDragMove', grid.dataset.gkGrid, x, y);
        return;
    }

    const grid = drag.grid;
    const bounds = grid.getBoundingClientRect();
    const columns = columnsOf(grid);
    const colPitch = (bounds.width + gap) / columns;
    const rowPitch = row + gap;
    const inline = (dx + scrollDX) * (rtl ? -1 : 1);
    const block = dy + scrollDY;
    const edge = drag.edge;
    const minWidth = colPitch - gap;
    const minHeight = row;

    let width = drag.rect.width;
    let height = drag.rect.height;
    if (edge.includes('e')) {
        width = Math.max(minWidth, width + inline);
    }

    if (edge.includes('w')) {
        const maxGrow = drag.cell.x * colPitch;
        width = Math.max(minWidth, Math.min(width - inline, drag.rect.width + maxGrow));
    }

    if (edge.includes('s')) {
        height = Math.max(minHeight, height + block);
    }

    drag.inner.style.width = `${width}px`;
    drag.inner.style.height = `${height}px`;
    const shift = edge.includes('w') ? (drag.rect.width - width) * (rtl ? -1 : 1) : 0;
    drag.inner.style.transform = shift ? `translateX(${shift}px)` : '';

    const w = Math.max(1, Math.round((width + gap) / colPitch));
    const h = Math.max(1, Math.round((height + gap) / rowPitch));
    const x = edge.includes('w') ? Math.max(0, drag.cell.x + drag.cell.w - w) : drag.cell.x;
    send(state, 'OnResizeMove', x, drag.cell.y, edge.includes('w') ? drag.cell.x + drag.cell.w - x : w, h);
}

function tabAt(state, x, y) {
    for (const tab of state.root.querySelectorAll('[data-gk-section-tab]')) {
        const bounds = tab.getBoundingClientRect();
        if (x >= bounds.left && x <= bounds.right && y >= bounds.top && y <= bounds.bottom) {
            return tab.dataset.gkSectionTab;
        }
    }

    return null;
}

function gridById(state, sectionId) {
    const grid = state.root.querySelector(`[data-gk-grid="${CSS.escape(sectionId)}"]`);
    return grid && ownerOf(grid) === state.root ? grid : null;
}

// A tab's grid is hidden until .NET shows it. Unhide it now so this pointer move can land in it.
function revealSection(state, sectionId) {
    const grid = gridById(state, sectionId);
    const section = grid?.closest('[data-gk-section]');
    if (!section) {
        return;
    }

    section.hidden = false;
    if (grid) {
        grid.hidden = false;
    }
}

function gridAt(state, clientY) {
    let best = null;
    let bestDistance = Infinity;
    for (const grid of state.root.querySelectorAll('[data-gk-grid]')) {
        if (grid.hidden || ownerOf(grid) !== state.root) {
            continue;
        }

        const bounds = grid.getBoundingClientRect();
        const distance = clientY < bounds.top ? bounds.top - clientY : clientY > bounds.bottom ? clientY - bounds.bottom : 0;
        if (distance < bestDistance) {
            best = grid;
            bestDistance = distance;
        }
    }

    return best;
}

function columnsOf(grid) {
    return Math.max(1, parseInt(getComputedStyle(grid).getPropertyValue('--gk-columns'), 10) || 12);
}

function cellOf(item) {
    const read = name => parseInt(item.style.getPropertyValue(name), 10) || 0;
    return { x: read('--gk-x'), y: read('--gk-y'), w: Math.max(1, read('--gk-w')), h: Math.max(1, read('--gk-h')) };
}

function autoScroll(state) {
    const drag = state.drag;
    if (!drag) {
        return;
    }

    const step = () => {
        if (state.drag !== drag) {
            return;
        }

        const y = drag.clientY;
        let delta = 0;
        if (y < SCROLL_EDGE) {
            delta = -SCROLL_SPEED * (1 - y / SCROLL_EDGE);
        } else if (y > window.innerHeight - SCROLL_EDGE) {
            delta = SCROLL_SPEED * (1 - (window.innerHeight - y) / SCROLL_EDGE);
        }

        if (delta) {
            window.scrollBy(0, delta);
        }

        drag.scrollFrame = requestAnimationFrame(step);
    };
    drag.scrollFrame = requestAnimationFrame(step);
}

// Only the latest position matters, so while .NET is busy with one, newer ones replace each other.
function send(state, method, ...args) {
    const key = `${method}:${args.join(',')}`;
    if (key === state.lastSent) {
        return;
    }

    state.lastSent = key;
    state.pending = { method, args };
    if (!state.inflight) {
        state.inflight = flush(state);
    }
}

async function flush(state) {
    while (state.pending) {
        const { method, args } = state.pending;
        state.pending = null;
        try {
            await state.dotnet.invokeMethodAsync(method, ...args);
        } catch {
            // The circuit may be reconnecting; the next position will try again.
        }
    }

    state.inflight = null;
}

async function finish(state, commit) {
    const drag = state.drag;
    if (!drag) {
        return;
    }

    endListeners(state);
    state.drag = null;
    state.lastSent = null;
    if (!drag.started) {
        return;
    }

    const ok = await drag.accepted;
    if (!ok) {
        clearVisuals(drag);
        return;
    }

    if (state.inflight) {
        await state.inflight;
    }

    settle(state, drag, commit);
}

function abort(state) {
    const drag = state.drag;
    if (!drag) {
        return;
    }

    endListeners(state);
    state.drag = null;
    state.lastSent = null;
    state.pending = null;
    if (drag.started) {
        state.dotnet.invokeMethodAsync(drag.mode === 'move' ? 'OnDragEnd' : 'OnResizeEnd', false).catch(() => { });
    }

    clearVisuals(drag);
}

function clearVisuals(drag) {
    drag.inner.style.transform = '';
    drag.inner.style.width = '';
    drag.inner.style.height = '';
    drag.item.classList.remove('gk-item-lifting');
    drag.item.removeAttribute('data-gk-settling');
    document.documentElement.removeAttribute('data-gk-dragging');
}

// The widget is let go wherever the pointer left it, then glides into the cell .NET chose. The
// glide starts the moment Blazor moves the item, so there's no frame where it jumps.
function settle(state, drag, commit) {
    const { item, inner } = drag;
    const released = inner.getBoundingClientRect();
    item.setAttribute('data-gk-settling', '');
    let played = false;

    const play = () => {
        if (played) {
            return;
        }

        played = true;
        observer.disconnect();
        document.documentElement.removeAttribute('data-gk-dragging');
        item.classList.remove('gk-item-lifting');
        inner.style.transition = 'none';
        inner.style.transform = '';
        inner.style.width = '';
        inner.style.height = '';
        const target = inner.getBoundingClientRect();

        if (reducedMotion() || !item.isConnected) {
            inner.style.transition = '';
            item.removeAttribute('data-gk-settling');
            return;
        }

        inner.style.transform = `translate(${released.left - target.left}px, ${released.top - target.top}px)`;
        if (drag.mode === 'resize') {
            inner.style.width = `${released.width}px`;
            inner.style.height = `${released.height}px`;
        }

        void inner.offsetWidth;
        inner.style.transition = `transform ${SETTLE_MS}ms ease, width ${SETTLE_MS}ms ease, height ${SETTLE_MS}ms ease`;
        inner.style.transform = '';
        if (drag.mode === 'resize') {
            inner.style.width = `${target.width}px`;
            inner.style.height = `${target.height}px`;
        }

        setTimeout(() => {
            inner.style.transition = '';
            inner.style.width = '';
            inner.style.height = '';
            item.removeAttribute('data-gk-settling');
        }, SETTLE_MS + 40);
    };

    const observer = new MutationObserver(play);
    observer.observe(item, { attributes: true, attributeFilter: ['style', 'class'] });
    state.dotnet
        .invokeMethodAsync(drag.mode === 'move' ? 'OnDragEnd' : 'OnResizeEnd', commit)
        .catch(() => { })
        .finally(() => requestAnimationFrame(() => requestAnimationFrame(play)));
}

// ---------------------------------------------------------------- keyboard

function keyDown(state, event) {
    const grip = event.target.closest?.('[data-gk-grip]');
    if (!grip || ownerOf(grip) !== state.root || state.drag) {
        return;
    }

    const keys = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
    const step = keys[event.key];
    if (!step) {
        return;
    }

    event.preventDefault();
    const rtl = getComputedStyle(state.root).direction === 'rtl';
    const dx = rtl ? -step[0] : step[0];
    state.dotnet.invokeMethodAsync('OnNudge', grip.dataset.gkGrip, dx, step[1], event.shiftKey)
        .then(() => grip.isConnected && grip.closest('[data-gk-item]')?.scrollIntoView({ block: 'nearest' }))
        .catch(() => { });
}

// ---------------------------------------------------------------- header overflow

// Buttons stay on one line. Once the title has shrunk as far as it can and the header still overflows,
// the ones that don't fit move into the more menu. Wrapping them onto a second line is what this avoids.
const openHeaders = new Set();
const watchedHeaders = new WeakMap();
let menusReady = false;

export function fitHeader(root) {
    ensureMenus();
    if (!root || typeof root.querySelectorAll !== 'function') {
        return;
    }

    const headers = root.matches?.('.gk-widget-header, .gk-widget-float')
        ? [root]
        : [...root.querySelectorAll(':scope > .gk-widget-header, :scope > .gk-widget-float')];
    for (const header of headers) {
        watchHeader(header);
        layoutHeader(header);
    }
}

function fitHeaders(root) {
    ensureMenus();
    if (!root?.querySelectorAll) {
        return;
    }

    for (const header of root.querySelectorAll('.gk-widget-header, .gk-widget-float')) {
        if (ownerOf(header) !== root) {
            continue;
        }

        watchHeader(header);
        layoutHeader(header);
    }
}

function ensureMenus() {
    if (menusReady) {
        return;
    }

    menusReady = true;
    document.addEventListener('click', event => {
        const more = event.target.closest?.('[data-gk-more]');
        if (more) {
            event.stopPropagation();
            toggleMenu(more.closest('.gk-widget-header, .gk-widget-float'));
            return;
        }

        if (openHeaders.size === 0) {
            return;
        }

        // The colour palette is anchored to its button, so the menu has to stay open behind it.
        const item = event.target.closest?.('[data-gk-overflow="open"]');
        if (item && event.target.closest('[aria-haspopup="true"], .gk-popover')) {
            return;
        }

        closeMenus();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && openHeaders.size > 0) {
            event.preventDefault();
            event.stopPropagation();
            closeMenus();
        }
    }, true);
    window.addEventListener('scroll', () => {
        for (const header of openHeaders) {
            placeMenu(header);
        }
    }, true);
    window.addEventListener('resize', () => {
        for (const header of document.querySelectorAll('.gk-widget-header, .gk-widget-float')) {
            layoutHeader(header);
        }
    });
}

function watchHeader(header) {
    if (watchedHeaders.has(header)) {
        return;
    }

    const observer = new ResizeObserver(() => layoutHeader(header));
    // The floating bar's own width changes as buttons are parked, so watch the widget instead or it loops.
    const target = header.classList.contains('gk-widget-float') ? header.parentElement : header;
    if (target) {
        observer.observe(target);
    }

    watchedHeaders.set(header, observer);
}

function headerItems(header) {
    const toolbar = header.querySelector(':scope > .gk-widget-toolbar');
    const controls = header.querySelector(':scope > .gk-widget-controls');
    const toolbarItems = toolbar && getComputedStyle(toolbar).display !== 'none' ? [...toolbar.children] : [];
    const controlItems = [...(controls?.children ?? [])].filter(el => !el.classList.contains('gk-control-more'));
    return [...toolbarItems, ...controlItems];
}

function headerOverflows(header) {
    if (header.classList.contains('gk-widget-float')) {
        const widget = header.parentElement;
        return !!widget && header.getBoundingClientRect().width > widget.clientWidth - 12;
    }

    return header.scrollWidth > header.clientWidth + 1;
}

function layoutHeader(header) {
    if (!header?.isConnected || header.dataset.gkFitting === '1') {
        return;
    }

    header.dataset.gkFitting = '1';
    try {
        const items = headerItems(header);
        for (const item of items) {
            resetItem(item);
        }

        const more = header.querySelector('.gk-control-more');
        if (more) {
            delete more.dataset.gkShow;
        }

        let overflow = false;
        if (items.length > 0 && headerOverflows(header)) {
            if (more) {
                more.dataset.gkShow = 'true';
            }

            for (let i = items.length - 1; i >= 0 && headerOverflows(header); i--) {
                parkItem(items[i]);
            }

            overflow = items.some(item => item.dataset.gkOverflow === '1');
            if (!overflow) {
                openHeaders.delete(header);
                if (more) {
                    delete more.dataset.gkShow;
                }
            }
        } else {
            openHeaders.delete(header);
        }

        if (openHeaders.has(header) && overflow) {
            placeMenu(header);
        } else {
            openHeaders.delete(header);
            clearMenuChrome(header);
        }
    } finally {
        delete header.dataset.gkFitting;
    }
}

function resetItem(item) {
    delete item.dataset.gkOverflow;
    item.inert = false;
    const style = item.style;
    style.position = '';
    style.visibility = '';
    style.pointerEvents = '';
    style.clipPath = '';
    style.width = '';
    style.height = '';
    style.overflow = '';
    style.inset = '';
    style.top = '';
    style.left = '';
    style.right = '';
    style.zIndex = '';
    style.minWidth = '';
    style.display = '';
    style.alignItems = '';
    style.justifyContent = '';
    style.boxSizing = '';
    style.margin = '';
}

function parkItem(item) {
    item.dataset.gkOverflow = '1';
    item.inert = true;
    const style = item.style;
    style.position = 'absolute';
    style.insetInlineStart = '0';
    style.top = '0';
    style.clipPath = 'inset(50%)';
    style.width = '1px';
    style.height = '1px';
    style.overflow = 'hidden';
    style.pointerEvents = 'none';
    style.margin = '0';
}

function placeMenu(header) {
    const more = header.querySelector('.gk-control-more');
    const button = header.querySelector('[data-gk-more]');
    const menu = header.querySelector('.gk-control-menu');
    const items = headerItems(header).filter(item => item.dataset.gkOverflow === '1' || item.dataset.gkOverflow === 'open');
    if (!more || !button || !menu || items.length === 0) {
        openHeaders.delete(header);
        clearMenuChrome(header);
        return;
    }

    const anchor = button.getBoundingClientRect();
    const rtl = getComputedStyle(header).direction === 'rtl';
    const entries = items.map(item => {
        item.inert = false;
        item.dataset.gkOverflow = 'open';
        const style = item.style;
        style.position = 'fixed';
        style.visibility = 'hidden';
        style.pointerEvents = 'none';
        style.clipPath = 'none';
        style.overflow = 'visible';
        style.inset = 'auto';
        style.zIndex = '41';
        style.width = 'auto';
        style.height = 'auto';
        style.margin = '0';
        style.boxSizing = 'border-box';
        if (item.matches('button')) {
            style.display = 'flex';
            style.alignItems = 'center';
            style.justifyContent = 'center';
        }

        return { item, width: item.offsetWidth, height: Math.max(item.offsetHeight, 28) };
    });
    const width = Math.max(36, ...entries.map(entry => entry.width));
    const height = 8 + entries.reduce((sum, entry) => sum + entry.height, 0);
    let top = anchor.bottom;
    if (top + height > window.innerHeight - 8 && anchor.top - height >= 8) {
        top = anchor.top - height;
    }

    const side = rtl ? anchor.left : window.innerWidth - anchor.right;
    menu.dataset.gkShow = 'true';
    menu.style.top = `${top}px`;
    menu.style.width = `${width}px`;
    menu.style.height = `${height}px`;
    menu.style.left = rtl ? `${side}px` : 'auto';
    menu.style.right = rtl ? 'auto' : `${side}px`;
    let y = top + 4;
    for (const entry of entries) {
        const style = entry.item.style;
        style.top = `${y}px`;
        style.minWidth = `${width}px`;
        style.height = `${entry.height}px`;
        style.visibility = 'visible';
        style.pointerEvents = 'auto';
        style.left = rtl ? `${side}px` : 'auto';
        style.right = rtl ? 'auto' : `${side}px`;
        y += entry.height;
    }

    more.dataset.gkShow = 'true';
    button.setAttribute('aria-expanded', 'true');
    const widget = header.closest('.gk-widget');
    const host = header.closest('.gk-item') ?? widget;
    // The shade has to sit under this widget and over everything else, including while one is full screen.
    const level = widget?.classList.contains('gk-widget-fullscreen') ? 2000 : 40;
    if (host) {
        host.dataset.gkRaised = '1';
        host.style.zIndex = String(level);
    }

    const shade = ensureLayer();
    shade.style.zIndex = String(level - 1);
    shade.hidden = false;
}

function clearMenuChrome(header) {
    const more = header.querySelector('.gk-control-more');
    const button = header.querySelector('[data-gk-more]');
    const menu = header.querySelector('.gk-control-menu');
    if (menu) {
        delete menu.dataset.gkShow;
    }

    if (button) {
        button.setAttribute('aria-expanded', 'false');
    }

    const host = header.closest('.gk-item') ?? header.closest('.gk-widget');
    if (host?.dataset.gkRaised === '1') {
        delete host.dataset.gkRaised;
        host.style.zIndex = '';
    }

    if (openHeaders.size === 0) {
        const shade = document.getElementById('gk-control-layer');
        if (shade) {
            shade.hidden = true;
        }
    }
}

function toggleMenu(header) {
    if (!header) {
        return;
    }

    const willOpen = !openHeaders.has(header);
    closeMenus();
    if (!willOpen) {
        return;
    }

    openHeaders.add(header);
    layoutHeader(header);
}

function closeMenus() {
    const headers = [...openHeaders];
    openHeaders.clear();
    for (const header of headers) {
        layoutHeader(header);
    }

    const shade = document.getElementById('gk-control-layer');
    if (shade) {
        shade.hidden = true;
    }
}

function ensureLayer() {
    let shade = document.getElementById('gk-control-layer');
    if (!shade) {
        shade = document.createElement('div');
        shade.id = 'gk-control-layer';
        shade.className = 'gk-menu-layer';
        shade.hidden = true;
        shade.style.zIndex = '30';
        document.body.appendChild(shade);
        shade.addEventListener('pointerdown', event => {
            event.preventDefault();
            event.stopPropagation();
            closeMenus();
        });
    }

    return shade;
}
