import { useEffect, useState } from 'react'
import { GridKrafter } from './GridKrafter.jsx'
import { board, catalog, widgets } from './board.js'

const THEME_KEY = 'gridkrafter-react-demo:theme'
const MAX_ENTRIES = 8
const THEMES = [
  { value: 'auto', label: 'Auto' },
  { value: 'light', label: 'Light' },
  { value: 'dark', label: 'Dark' },
]

const timeOf = date => date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit', second: '2-digit' })

export default function App() {
  const [theme, setTheme] = useState(() => localStorage.getItem(THEME_KEY) ?? 'auto')
  const [entries, setEntries] = useState([])

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    localStorage.setItem(THEME_KEY, theme)
  }, [theme])

  const log = (name, text) =>
    setEntries(list => [{ id: crypto.randomUUID(), at: new Date(), name, text }, ...list].slice(0, MAX_ENTRIES))

  return (
    <>
      <header className="demo-nav">
        <a className="demo-brand" href="/">
          <img src="/img/Logo.png" alt="GridKrafter" />
          <span>React demo</span>
        </a>
        <label className="demo-theme">
          Theme
          <select value={theme} onChange={event => setTheme(event.target.value)}>
            {THEMES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
          </select>
        </label>
      </header>

      <main className="demo-main">
        <div className="demo-intro">
          <h1>Dashboard</h1>
          <p>
            The widgets below are React components living inside GridKrafter&apos;s grid. Drag them by their header,
            switch on edit mode to resize them or add more, and refresh the chart for new figures.
          </p>
        </div>

        <GridKrafter
          board={board}
          catalog={catalog}
          widgets={widgets}
          theme={theme}
          storageKey="react-demo"
          chrome="card"
          onChanged={change => log(change.kind, [change.itemId ?? change.sectionId, change.layout].filter(Boolean).join(' · '))}
          onLayout={layout => log('layout', `${layout.id}, ${layout.columns} columns`)}
          onEditMode={editing => log('edit mode', editing ? 'on' : 'off')}
        />

        <section className="demo-log" aria-labelledby="log-title">
          <h2 id="log-title">Events</h2>
          <p className="demo-muted">Move, resize, collapse or edit a widget and it shows up here.</p>
          <ol aria-live="polite">
            {entries.map(entry => (
              <li key={entry.id}>
                <time>{timeOf(entry.at)}</time> <code>{entry.name}</code> {entry.text}
              </li>
            ))}
          </ol>
        </section>
      </main>
    </>
  )
}
