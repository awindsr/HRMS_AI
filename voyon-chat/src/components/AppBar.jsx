import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'

/** Top-right session bar: shows the signed-in user and a sign-out button. */
export default function AppBar() {
  const { user, logout } = useAuth()
  const [busy, setBusy] = useState(false)

  const signOut = async () => {
    setBusy(true)
    await logout()
  }

  const name = user?.name?.trim() || 'Signed in'

  return (
    <header className="appbar">
      <span className="appbar__user" title={user?.email || undefined}>
        {name}
      </span>
      <button className="appbar__logout" type="button" onClick={signOut} disabled={busy}>
        {busy ? 'Signing out…' : 'Sign out'}
      </button>
    </header>
  )
}
