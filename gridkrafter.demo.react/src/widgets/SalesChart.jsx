import { useMemo } from 'react'

const RANGES = [
  { value: 'week', label: 'Week', scale: 6_000, labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'] },
  { value: 'month', label: 'Month', scale: 32_000, labels: ['Wk 1', 'Wk 2', 'Wk 3', 'Wk 4'] },
  { value: 'year', label: 'Year', scale: 120_000, labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'] },
]

const money = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
const count = new Intl.NumberFormat()

/** Made-up revenue and orders. Refreshing the widget makes up new ones; the range is a setting. */
export function SalesChart({ options, setOption, refreshes }) {
  const range = RANGES.find(r => r.value === options.range) ?? RANGES[0]
  const points = useMemo(() => generate(range, refreshes), [range, refreshes])
  const maxRevenue = Math.max(1, ...points.map(p => p.revenue))
  const maxOrders = Math.max(1, ...points.map(p => p.orders))
  const line = points.map((p, i) => `${i * 10 + 5},${100 - (p.orders / maxOrders) * 80 - 6}`).join(' ')

  return (
    <div className="demo-chart">
      <div className="demo-row">
        <div className="demo-chart-totals">
          <div>
            <span className="gk-muted">Revenue</span>
            <strong>{money.format(points.reduce((sum, p) => sum + p.revenue, 0))}</strong>
          </div>
          {options.orders && (
            <div>
              <span className="gk-muted">Orders</span>
              <strong>{count.format(points.reduce((sum, p) => sum + p.orders, 0))}</strong>
            </div>
          )}
        </div>
        <div className="gk-tabs" role="tablist" aria-label="Range">
          {RANGES.map(r => (
            <button key={r.value} type="button" role="tab" aria-selected={r === range}
              onClick={() => setOption('range', r.value)}>
              {r.label}
            </button>
          ))}
        </div>
      </div>
      <div className="demo-chart-plot">
        <svg viewBox={`0 0 ${points.length * 10} 100`} preserveAspectRatio="none" role="img" aria-label={`Revenue by ${range.value}`}>
          {points.map((p, i) => {
            const height = (p.revenue / maxRevenue) * 90
            return (
              <rect key={p.label} x={i * 10 + 1.5} y={100 - height} width="7" height={height} rx="0.8">
                <title>{`${p.label}: ${money.format(p.revenue)}`}</title>
              </rect>
            )
          })}
          {options.orders && <polyline className="demo-chart-line" points={line} fill="none" vectorEffect="non-scaling-stroke" />}
        </svg>
      </div>
      <div className="demo-chart-axis" style={{ '--demo-bars': points.length }}>
        {points.map(p => <span key={p.label}>{p.label}</span>)}
      </div>
    </div>
  )
}

function generate(range, seed) {
  const random = mulberry32([...range.value].reduce((hash, c) => hash * 31 + c.charCodeAt(0), seed + 1) >>> 0)
  return range.labels.map(label => {
    const revenue = Math.round(range.scale * (0.45 + random() * 0.55))
    return { label, revenue, orders: Math.round(revenue / (55 + random() * 30)) }
  })
}

function mulberry32(seed) {
  return () => {
    seed = (seed + 0x6d2b79f5) >>> 0
    let t = seed
    t = Math.imul(t ^ (t >>> 15), t | 1)
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61)
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}
