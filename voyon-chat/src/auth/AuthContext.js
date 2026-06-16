import { createContext, useContext } from 'react'

/**
 * Auth state shared across the app. The HRMS token lives only in the backend's httpOnly cookie;
 * the context carries just the display profile, a status, and the sign-in / sign-out actions.
 * The provider lives in AuthProvider.jsx (kept separate so this file exports no components).
 */
export const AuthContext = createContext(null)

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
