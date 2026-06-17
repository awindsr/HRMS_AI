// App-wide configuration and static content. No secrets here — the backend owns all tokens.

// Same-origin in dev (Vite proxies /api → backend). Set VITE_API_BASE for a separate-origin deploy.
export const API_BASE = import.meta.env.VITE_API_BASE ?? ''

// The greeting name comes from /api/v1/me (the signed-in user's HRMS profile) — see useProfile.

export const SUGGESTIONS = [
  'Was I present today?',
  "Show this month's attendance",
  'When did I check in today?',
  'Log my check-in',
]
