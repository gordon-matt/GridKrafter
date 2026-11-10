import { useEffect, useState } from 'react'

/** The time and date in the browser, in its own language. */
export function Clock({ options }) {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(timer)
  }, [])

  const time = now.toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
    second: options.seconds ? '2-digit' : undefined,
    hour12: Boolean(options.hour12),
  })

  return (
    <div className="demo-clock">
      <time className="demo-clock-time" dateTime={now.toISOString()}>{time}</time>
      <span className="gk-muted">
        {now.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
      </span>
    </div>
  )
}
