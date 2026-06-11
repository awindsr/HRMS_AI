import { API_BASE } from '../config'

/** Fetches the signed-in manager's display identity (name/email/employeeId) from the backend. */
export async function fetchProfile() {
  const res = await fetch(`${API_BASE}/api/v1/me`)
  if (!res.ok) throw new Error(`Profile request failed (${res.status})`)
  return res.json()
}
