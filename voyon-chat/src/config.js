// App-wide configuration and static content. No secrets here — the backend owns all tokens.

// Same-origin in dev (Vite proxies /api → backend). Set VITE_API_BASE for a separate-origin deploy.
export const API_BASE = import.meta.env.VITE_API_BASE ?? ''

// The greeting name comes from /api/v1/me (decoded from the HRMS token) — see useProfile.

export const SUGGESTIONS = [
  "Who's absent today?",
  "Show today's attendance report",
  "Who's on leave today?",
  'Log a check-in',
]
