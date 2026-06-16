import { API_BASE } from '../config'

// All auth calls rely on the httpOnly session cookie, so every request must include credentials.
const withCreds = { credentials: 'include' }

/**
 * Checks the current session by reading the user's profile from /me.
 * @returns the profile when signed in, or null on 401 (not signed in).
 */
export async function fetchSession() {
  const res = await fetch(`${API_BASE}/api/v1/me`, withCreds)
  if (res.status === 401) return null
  if (!res.ok) throw new Error(`Session check failed (${res.status})`)
  return res.json()
}

/** Signs in with HRMS credentials. Resolves to the profile, or throws an Error with a friendly message. */
export async function login({ username, password }) {
  const res = await fetch(`${API_BASE}/api/v1/auth/login`, {
    ...withCreds,
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    // offset = client tz offset in minutes (HRMS uses it only for the TimezoneOffset claim).
    body: JSON.stringify({ username, password, offset: -new Date().getTimezoneOffset() }),
  })

  if (!res.ok) {
    const data = await res.json().catch(() => null)
    throw new Error(friendlyError(res.status, data?.error))
  }
  return res.json()
}

/** Clears the session cookie. Best-effort; never throws. */
export async function logout() {
  try {
    await fetch(`${API_BASE}/api/v1/auth/logout`, { ...withCreds, method: 'POST' })
  } catch {
    /* ignore — the UI signs out locally regardless */
  }
}

// Maps the backend's { code, message } error to a user-facing message, per the HRMS error contract.
function friendlyError(status, error) {
  switch (error?.code) {
    case 'invalid_request':
      return 'Please enter both your username and password.'
    case 'invalid_grant':
      return 'Incorrect username or password.'
    case 'user_locked':
      return 'Your account is locked. Please contact your HR administrator.'
    case 'login_denied':
      return 'Sign-in is not permitted for this account.'
    case 'invalid_password_policy':
      return 'Your password has expired or must be changed. Reset it in HRMS, then try again.'
    case 'rate_limited':
      return 'Too many attempts. Please wait a few minutes and try again.'
    default:
      return error?.message || (status >= 500
        ? 'The sign-in service is unavailable right now. Please try again shortly.'
        : 'Sign-in failed. Please try again.')
  }
}
