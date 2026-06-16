import { useAuth } from '../auth/AuthContext'

/**
 * Exposes the signed-in user's identity for the greeting. The profile is loaded once at sign-in
 * and shared via AuthContext, so this is just a thin selector — no extra fetch.
 */
export function useProfile() {
  const { user } = useAuth()
  const name = user?.name?.trim() || null
  const firstName = name ? name.split(/\s+/)[0] : null

  return { name, firstName, email: user?.email ?? null, employeeId: user?.employeeId ?? null }
}
