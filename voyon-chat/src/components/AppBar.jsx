import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { useProfile } from '../hooks/useProfile'

/** Derives up-to-two-letter initials from a display name, for the avatar fallback. */
function initialsOf(name) {
  if (!name) return '·'
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const last = parts.length > 1 ? parts[parts.length - 1][0] : ''
  return (first + last).toUpperCase() || '·'
}

/** Top-right session bar: an avatar that opens a profile card with details + sign-out. */
export default function AppBar() {
  const { logout } = useAuth()
  const { name, designation, department, businessUnit, company, email, employeeId, photoUrl } = useProfile()
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [imgOk, setImgOk] = useState(true)
  const ref = useRef(null)

  // Close the menu on outside-click or Escape.
  useEffect(() => {
    if (!open) return
    const onClick = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false) }
    const onKey = (e) => { if (e.key === 'Escape') setOpen(false) }
    document.addEventListener('mousedown', onClick)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onClick)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  const signOut = async () => {
    setBusy(true)
    await logout()
  }

  const displayName = name || 'Signed in'
  const initials = initialsOf(name)
  const showPhoto = photoUrl && imgOk
  const subtitle = designation || department || null

  const detailRows = [
    ['Designation', designation],
    ['Department', department],
    ['Business unit', businessUnit],
    ['Company', company],
    ['Email', email],
    ['Employee ID', employeeId],
  ].filter(([, v]) => v)

  const Avatar = ({ size }) => (
    <span className={`avatar avatar--${size}`} aria-hidden="true">
      {showPhoto ? (
        <img src={photoUrl} alt="" onError={() => setImgOk(false)} />
      ) : (
        <span className="avatar__initials">{initials}</span>
      )}
    </span>
  )

  return (
    <header className="appbar" ref={ref}>
      <button
        type="button"
        className="appbar__trigger"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        title={displayName}
      >
        <Avatar size="sm" />
        <span className="appbar__name">{displayName}</span>
        <svg className={`appbar__caret ${open ? 'is-open' : ''}`} width="14" height="14" viewBox="0 0 24 24" aria-hidden="true">
          <path d="M6 9l6 6 6-6" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>

      {open && (
        <div className="profile-card" role="menu">
          <div className="profile-card__head">
            <Avatar size="lg" />
            <div className="profile-card__id">
              <span className="profile-card__name">{displayName}</span>
              {subtitle && <span className="profile-card__sub">{subtitle}</span>}
            </div>
          </div>

          {detailRows.length > 0 && (
            <dl className="profile-card__details">
              {detailRows.map(([label, value]) => (
                <div className="profile-card__row" key={label}>
                  <dt>{label}</dt>
                  <dd>{value}</dd>
                </div>
              ))}
            </dl>
          )}

          <button className="profile-card__logout" type="button" onClick={signOut} disabled={busy}>
            {busy ? 'Signing out…' : 'Sign out'}
          </button>
        </div>
      )}
    </header>
  )
}
