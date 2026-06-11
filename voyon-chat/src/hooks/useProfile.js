import { useEffect, useState } from 'react'
import { fetchProfile } from '../api/profile'

/**
 * Loads the manager's identity once on mount. Returns the full name and a friendly first name
 * for the greeting. Fails silently — the UI just falls back to a name-less greeting.
 */
export function useProfile() {
  const [profile, setProfile] = useState(null)

  useEffect(() => {
    let alive = true
    fetchProfile()
      .then((p) => alive && setProfile(p))
      .catch(() => {})
    return () => {
      alive = false
    }
  }, [])

  const name = profile?.name?.trim() || null
  const firstName = name ? name.split(/\s+/)[0] : null

  return { name, firstName, email: profile?.email ?? null, employeeId: profile?.employeeId ?? null }
}
