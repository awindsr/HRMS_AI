// The glowing coral orb — the app's one piece of brand identity. Variants via className.
export default function Orb({ className = '' }) {
  return <span className={`orb ${className}`} aria-hidden="true" />
}
