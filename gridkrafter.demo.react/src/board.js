import { Clock } from './widgets/Clock.jsx'
import { SalesChart } from './widgets/SalesChart.jsx'
import { Stopwatch } from './widgets/Stopwatch.jsx'
import { Tasks } from './widgets/Tasks.jsx'

/** The React component for each kind in the catalog. */
export const widgets = {
  sales: SalesChart,
  tasks: Tasks,
  clock: Clock,
  stopwatch: Stopwatch,
}

/** The widgets people can add. They have no content of their own, so the dashboard leaves their bodies for React. */
export const catalog = [
  {
    kind: 'sales',
    name: 'Sales chart',
    description: 'Revenue and orders over a week, month or year. Refreshing it makes up new figures.',
    icon: 'chart',
    color: 'blue',
    w: 8,
    h: 5,
    minW: 4,
    minH: 4,
    refresh: 60,
    options: [
      { key: 'range', label: 'Range', type: 'select', default: 'week', choices: [
        { value: 'week', label: 'This week' },
        { value: 'month', label: 'This month' },
        { value: 'year', label: 'This year' },
      ] },
      { key: 'orders', label: 'Show orders', type: 'switch', default: true },
    ],
  },
  {
    kind: 'tasks',
    name: 'To-do list',
    description: 'Tasks saved with the board.',
    icon: 'list',
    color: 'orange',
    w: 5,
    h: 5,
    minW: 3,
    autoHeight: true,
    options: [{ key: 'hideDone', label: 'Hide finished tasks', type: 'switch', default: false }],
  },
  {
    kind: 'clock',
    name: 'Clock',
    description: 'The time and date where you are.',
    icon: 'clock',
    w: 4,
    h: 2,
    minW: 2,
    minH: 2,
    options: [
      { key: 'hour12', label: '12-hour clock', type: 'switch', default: false },
      { key: 'seconds', label: 'Show seconds', type: 'switch', default: true },
    ],
  },
  {
    kind: 'stopwatch',
    name: 'Stopwatch',
    description: 'Keeps running while you move it around.',
    icon: 'activity',
    color: 'teal',
    w: 4,
    h: 3,
    minW: 3,
    autoHeight: true,
  },
]

/** The board as the page defines it. Changes are kept in local storage and applied over it. */
export const board = {
  sections: [
    { id: 'overview', title: 'Overview', icon: 'home' },
    { id: 'workspace', title: 'Workspace', icon: 'layers' },
  ],
  items: [
    stat('revenue', 'Revenue', '$48,200', '+3.2%', 'green', 0),
    stat('visitors', 'Visitors', '12,840', '+8.1%', 'blue', 3),
    stat('orders', 'Orders', '1,204', '-2.4%', 'orange', 6),
    stat('uptime', 'Uptime', '99.98%', '+0.02%', 'violet', 9),
    { id: 'sales', kind: 'sales', section: 'overview', title: 'Sales', x: 0, y: 2, w: 8, h: 5 },
    { id: 'clock', kind: 'clock', section: 'overview', title: 'Clock', x: 8, y: 2, w: 4, h: 2 },
    { id: 'stopwatch', kind: 'stopwatch', section: 'overview', title: 'Stopwatch', x: 8, y: 4, w: 4, h: 3 },
    { id: 'tasks', kind: 'tasks', section: 'workspace', title: 'To-do list', x: 0, y: 0, w: 5, h: 5 },
    {
      id: 'about',
      section: 'workspace',
      title: 'How this page works',
      icon: 'info',
      x: 5,
      y: 0,
      w: 7,
      h: 4,
      autoHeight: true,
      html: `
        <p>The dashboard is <code>&lt;gk-dashboard&gt;</code>, Blazor running in WebAssembly, wrapped in a React component.</p>
        <ul class="gk-list">
          <li><span>Widgets without content of their own leave their bodies empty, and a <code>gk-mounts</code> event lists them.</span></li>
          <li><span>The wrapper portals a React component into each one, so the chart, clock, stopwatch and to-do list are plain React.</span></li>
          <li><span>A widget saves a setting by dispatching <code>gk-option</code>; it's then part of the board, like the to-do list's tasks.</span></li>
        </ul>
        <p>Everything is kept in this browser's local storage. Reset it from the toolbar in edit mode.</p>`,
    },
  ],
}

/** A headline figure whose body is an HTML string: the simplest kind of widget. */
function stat(id, label, value, change, color, x) {
  const tone = change.startsWith('-') ? 'demo-down' : 'demo-up'
  return {
    id,
    section: 'overview',
    title: label,
    color,
    chrome: 'plain',
    buttons: 'color, delete',
    x,
    y: 0,
    w: 3,
    h: 2,
    minW: 2,
    maxH: 3,
    html: `
      <div class="demo-stat">
        <span class="demo-stat-label">${label}</span>
        <strong class="demo-stat-value">${value}</strong>
        <span class="demo-stat-change ${tone}">${change} <span class="gk-muted">vs last week</span></span>
      </div>`,
  }
}
