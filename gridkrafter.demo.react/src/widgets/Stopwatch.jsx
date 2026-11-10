import { useEffect, useState } from 'react'
import { Icon } from './Icon.jsx'

const MAX_LAPS = 5

/**
 * A stopwatch whose state is ordinary React state, not a setting: it keeps running while the
 * widget is dragged, collapsed or moved to another section, and starts over when the page reloads.
 */
export function Stopwatch() {
  const [startedAt, setStartedAt] = useState(null)
  const [banked, setBanked] = useState(0)
  const [laps, setLaps] = useState([])
  const [, setTick] = useState(0)

  useEffect(() => {
    if (startedAt === null) {
      return undefined
    }

    const timer = setInterval(() => setTick(tick => tick + 1), 100)
    return () => clearInterval(timer)
  }, [startedAt])

  const elapsed = banked + (startedAt === null ? 0 : Date.now() - startedAt)
  const running = startedAt !== null

  const toggle = () => {
    if (running) {
      setBanked(elapsed)
      setStartedAt(null)
    } else {
      setStartedAt(Date.now())
    }
  }

  const reset = () => {
    setStartedAt(null)
    setBanked(0)
    setLaps([])
  }

  return (
    <div className="demo-stopwatch">
      <time className="demo-clock-time">{format(elapsed)}</time>
      <div className="demo-stopwatch-buttons">
        <button type="button" className="gk-button gk-button-primary" onClick={toggle}>
          <Icon name={running ? 'pause' : 'play'} />
          {running ? 'Pause' : elapsed ? 'Resume' : 'Start'}
        </button>
        <button type="button" className="gk-button" disabled={!running}
          onClick={() => setLaps(list => [elapsed, ...list].slice(0, MAX_LAPS))}>
          <Icon name="flag" />
          Lap
        </button>
        <button type="button" className="gk-button gk-button-ghost" disabled={!elapsed} onClick={reset} aria-label="Reset">
          <Icon name="reset" />
        </button>
      </div>
      {laps.length > 0 && (
        <ol className="gk-list demo-laps">
          {laps.map((lap, i) => (
            <li key={laps.length - i}>
              <span className="gk-muted">Lap {laps.length - i}</span>
              <span className="demo-number">{format(lap)}</span>
            </li>
          ))}
        </ol>
      )}
    </div>
  )
}

function format(ms) {
  const minutes = Math.floor(ms / 60_000)
  const seconds = Math.floor(ms / 1000) % 60
  const tenths = Math.floor(ms / 100) % 10
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}.${tenths}`
}
