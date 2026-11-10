import { useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'

const OPTION_EVENT = 'gk-option'

/**
 * React wrapper for <gk-dashboard>. index.html loads the runtime that defines the element.
 *
 * Widgets whose kind is a key of `widgets` are React components, rendered into the bodies the
 * dashboard leaves empty. Each gets { id, kind, title, options, setOption, refreshes }: options are
 * its settings over the catalog's defaults, setOption(key, value) saves one with the board, and
 * refreshes counts how often it's been refreshed.
 *
 * board, catalog and layouts are objects; pass the same ones on every render.
 */
export function GridKrafter({
  board,
  catalog = [],
  layouts,
  widgets = {},
  theme = 'light',
  language,
  storageKey,
  chrome,
  buttons,
  editMode = false,
  readOnly = false,
  showToolbar = true,
  displayMode = 'tabs',
  onReady,
  onChanged,
  onLayout,
  onEditMode,
}) {
  const ref = useRef(null)
  const handlers = useRef({})
  const hosts = useRef(new Map())
  const [mounts, setMounts] = useState([])
  const [items, setItems] = useState(() => new Map())
  const [refreshes, setRefreshes] = useState({})

  const boardJson = useMemo(() => toJson(board), [board])
  const catalogJson = useMemo(() => toJson(catalog), [catalog])
  const layoutsJson = useMemo(() => toJson(layouts), [layouts])
  const defaults = useMemo(() => defaultsOf(catalog), [catalog])

  useEffect(() => {
    handlers.current = { onReady, onChanged, onLayout, onEditMode }
  })

  useEffect(() => {
    const element = ref.current
    const track = current => current && setItems(new Map(current.items.map(item => [item.id, item])))
    const listeners = {
      'gk-ready': event => {
        track(event.detail.board)
        handlers.current.onReady?.(event.detail)
      },
      'gk-changed': event => {
        track(event.detail.board)
        handlers.current.onChanged?.(event.detail)
      },
      'gk-layout': event => handlers.current.onLayout?.(event.detail),
      'gk-edit-mode': event => handlers.current.onEditMode?.(event.detail.editing),
      'gk-refresh': event => {
        const { id } = event.detail
        setRefreshes(counts => ({ ...counts, [id]: (counts[id] ?? 0) + 1 }))
      },
      'gk-mounts': event => setMounts(place(element, hosts.current, event.detail.mounts)),
    }

    for (const [name, listener] of Object.entries(listeners)) {
      element.addEventListener(name, listener)
    }

    return () => {
      for (const [name, listener] of Object.entries(listeners)) {
        element.removeEventListener(name, listener)
      }
    }
  }, [])

  return (
    <>
      <gk-dashboard
        ref={ref}
        board={boardJson}
        catalog={catalogJson}
        layouts={layoutsJson}
        theme={theme}
        language={language}
        storage-key={storageKey}
        chrome={chrome}
        buttons={buttons}
        edit-mode={String(editMode)}
        read-only={String(readOnly)}
        show-toolbar={String(showToolbar)}
        display-mode={displayMode}
      />
      {mounts.map(({ id, kind, host }) => {
        const Widget = widgets[kind]
        const item = items.get(id)
        const setOption = (key, value) =>
          host.dispatchEvent(new CustomEvent(OPTION_EVENT, { bubbles: true, detail: { key, value } }))

        return Widget && createPortal(
          <Widget
            id={id}
            kind={kind}
            title={item?.title ?? ''}
            options={{ ...defaults[kind], ...item?.options }}
            setOption={setOption}
            refreshes={refreshes[id] ?? 0}
          />,
          host,
          id,
        )
      })}
    </>
  )
}

function toJson(value) {
  return value === undefined ? undefined : JSON.stringify(value)
}

function defaultsOf(catalog) {
  return Object.fromEntries(catalog.map(definition => [
    definition.kind,
    Object.fromEntries((definition.options ?? []).map(option => [option.key, option.default])),
  ]))
}

// A widget dragged to another section gets a new, empty body. Each widget renders into a host of its
// own that moves into whichever body it has now, so its React state comes along.
function place(element, hosts, mounts) {
  const placed = []
  for (const mount of mounts) {
    const body = element.querySelector(`[data-gk-mount="${CSS.escape(mount.id)}"]`)
    if (!body) {
      continue
    }

    let host = hosts.get(mount.id)
    if (!host) {
      host = document.createElement('div')
      host.className = 'demo-react-host'
      hosts.set(mount.id, host)
    }

    if (host.parentNode !== body) {
      body.replaceChildren(host)
    }

    placed.push({ ...mount, host })
  }

  for (const id of [...hosts.keys()]) {
    if (!placed.some(mount => mount.id === id)) {
      hosts.delete(id)
    }
  }

  return placed
}
