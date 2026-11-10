// The page's own script: it saves the board on the server after each change, lists the
// dashboard's events and applies the theme picker.

const SAVE_DELAY_MS = 600;
const MAX_ENTRIES = 8;

const host = document.getElementById('dashboard');
const status = document.getElementById('save-status');
const log = document.getElementById('log');
const token = document.querySelector('meta[name="request-verification-token"]')?.content ?? '';

let pending = null;
let timer = 0;

document.querySelector('[data-autosubmit]')?.addEventListener('change', event => event.target.form.submit());

host.addEventListener('gk-changed', event => {
    const { kind, itemId, sectionId, layout, board } = event.detail;
    write(kind, [itemId, sectionId, layout].filter(Boolean).join(' · '));
    pending = board;
    clearTimeout(timer);
    timer = setTimeout(save, SAVE_DELAY_MS);
});

host.addEventListener('gk-layout', event => write('layout', `${event.detail.id}, ${event.detail.columns} columns`));

// A change made just before leaving would otherwise be lost with the timer.
window.addEventListener('pagehide', () => {
    if (pending) {
        clearTimeout(timer);
        save(true);
    }
});

async function save(leaving = false) {
    const board = pending;
    pending = null;
    if (!board) {
        return;
    }

    status.textContent = 'Saving…';
    try {
        const response = await fetch('api/board', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
            body: JSON.stringify(board),
            keepalive: leaving,
        });
        if (!response.ok) {
            throw new Error(`${response.status} ${response.statusText}`.trim());
        }

        status.textContent = `Saved on the server at ${new Date().toLocaleTimeString()}.`;
    } catch (error) {
        status.textContent = `The board couldn't be saved (${error.message}).`;
    }
}

function write(kind, detail) {
    const entry = document.createElement('li');
    const time = document.createElement('time');
    time.textContent = new Date().toLocaleTimeString();
    const name = document.createElement('code');
    name.textContent = kind;
    entry.append(time, ' ', name, ' ', detail);
    log.prepend(entry);
    while (log.children.length > MAX_ENTRIES) {
        log.lastElementChild.remove();
    }
}
