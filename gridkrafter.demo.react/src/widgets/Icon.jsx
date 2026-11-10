const PATHS = {
  trash: 'M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3',
  play: 'M8 5v14l11-7z',
  pause: 'M8 5v14M16 5v14',
  flag: 'M5 21V4M5 4h11l-2 4 2 4H5',
  reset: 'M4 12a8 8 0 1 0 2.3-5.7L4 8.6M4 4v4.6h4.6',
}

/** A stroked icon in the style of the dashboard's own. */
export function Icon({ name }) {
  return (
    <svg className="gk-icon" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor"
      strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d={PATHS[name]} />
    </svg>
  )
}
