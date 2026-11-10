import { useState } from 'react'
import { Icon } from './Icon.jsx'

const STARTER = [
  { text: 'Drag a widget by its header', done: true },
  { text: 'Drag this list into the other section', done: false },
  { text: 'Add a widget from the catalog', done: false },
  { text: 'Switch to dark mode', done: false },
]

/** A to-do list kept in the widget's settings, so it's saved with the board. */
export function Tasks({ options, setOption }) {
  const [draft, setDraft] = useState('')
  const tasks = Array.isArray(options.items) ? options.items : STARTER
  const visible = options.hideDone ? tasks.filter(task => !task.done) : tasks
  const left = tasks.filter(task => !task.done).length
  const save = items => setOption('items', items)

  const add = event => {
    event.preventDefault()
    const text = draft.trim()
    if (text) {
      save([...tasks, { text, done: false }])
      setDraft('')
    }
  }

  return (
    <div className="demo-tasks">
      <form className="demo-todo-add" onSubmit={add}>
        <input className="gk-input" placeholder="Add a task" aria-label="New task" maxLength={200}
          value={draft} onChange={event => setDraft(event.target.value)} />
        <button type="submit" className="gk-button gk-button-primary" disabled={!draft.trim()}>Add</button>
      </form>

      {visible.length === 0
        ? <p className="gk-muted demo-empty">{tasks.length === 0 ? 'Nothing to do.' : "Everything's done."}</p>
        : (
          <ul className="gk-list">
            {visible.map(task => (
              <li key={tasks.indexOf(task)} className={task.done ? 'demo-done' : undefined}>
                <label className="demo-check">
                  <input type="checkbox" checked={task.done}
                    onChange={() => save(tasks.map(t => (t === task ? { ...t, done: !t.done } : t)))} />
                  <span>{task.text}</span>
                </label>
                <button type="button" className="gk-icon-button" aria-label={`Delete ${task.text}`}
                  onClick={() => save(tasks.filter(t => t !== task))}>
                  <Icon name="trash" />
                </button>
              </li>
            ))}
          </ul>
        )}

      <div className="demo-row">
        <span className="gk-muted">{left} left</span>
        <button type="button" className="gk-button gk-button-small gk-button-ghost" disabled={!tasks.some(t => t.done)}
          onClick={() => save(tasks.filter(t => !t.done))}>
          Clear finished
        </button>
      </div>
    </div>
  )
}
