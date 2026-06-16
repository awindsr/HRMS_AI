import { useState } from 'react'
import GlowOrb from './GlowOrb'
import { useAuth } from '../auth/AuthContext'

/** Sign-in screen: brand orb, HRMS username/password, and inline error handling. */
export default function LoginView() {
  const { login } = useAuth()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)

  const submit = async (e) => {
    e.preventDefault()
    if (busy || !username.trim() || !password) return
    setBusy(true)
    setError(null)
    try {
      await login({ username: username.trim(), password })
    } catch (err) {
      setError(err.message || 'Sign-in failed. Please try again.')
      setBusy(false)
    }
  }

  return (
    <main className="login">
      <div className="login__orb">
        <GlowOrb hue={0} hoverIntensity={0.4} backgroundColor="#fbfcff" />
      </div>
      <h1 className="login__title">Voyon Attendance</h1>
      <p className="login__subtitle">Sign in with your HRMS account to continue.</p>

      <form className="login__form" onSubmit={submit}>
        <label className="field">
          <span className="field__label">Username</span>
          <input
            className="field__input"
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
            autoFocus
            disabled={busy}
          />
        </label>

        <label className="field">
          <span className="field__label">Password</span>
          <input
            className="field__input"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            disabled={busy}
          />
        </label>

        {error && (
          <p className="login__error" role="alert">
            {error}
          </p>
        )}

        <button className="login__submit" type="submit" disabled={busy || !username.trim() || !password}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </main>
  )
}
