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
        sentRows: new Map(),
        rowsToSend: null,
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
    for (const entry of entries) {
        const item = state.observed.get(entry.target);
        if (item) {
            items.add(item);
        }
    }

    measure(state, items);
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
        const grid = gridAt(state, drag.clientY) ?? drag.grid;
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
