import { useCallback, useEffect, useState } from 'react'
import { AuthContext } from './AuthContext'
import { fetchSession, login as apiLogin, logout as apiLogout } from '../api/auth'

/**
 * Owns the authentication state and exposes sign-in / sign-out. status is:
 * 'loading' (checking the session) | 'authed' | 'anon'.
 */
export function AuthProvider({ children }) {
  const [status, setStatus] = useState('loading')
  const [user, setUser] = useState(null)

  // On mount, ask the backend whether the cookie still represents a valid session.
  useEffect(() => {
    let alive = true
    fetchSession()
      .then((profile) => {
        if (!alive) return
        if (profile) {
          setUser(profile)
          setStatus('authed')
        } else {
          setStatus('anon')
        }
      })
      .catch(() => alive && setStatus('anon'))
    return () => {
      alive = false
    }
  }, [])

  const login = useCallback(async (credentials) => {
    const profile = await apiLogin(credentials)
    setUser(profile)
    setStatus('authed')
    return profile
  }, [])

  const logout = useCallback(async () => {
    await apiLogout()
    setUser(null)
    setStatus('anon')
  }, [])

  return (
    <AuthContext.Provider value={{ status, user, login, logout }}>{children}</AuthContext.Provider>
  )
}
