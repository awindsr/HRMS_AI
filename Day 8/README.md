# Day 8 — Security Hardening: Secrets, Env Vars & Tool-Level Authorization

**Topic:** Getting every secret out of source, proving nothing sensitive is in Git, confirming each agent tool call runs under the **signed-in user's** HRMS token, and documenting the security posture.

By Day 7 the app worked end-to-end: cookie-based login, a per-user HRMS JWT, and an in-process agent tool loop. Day 8 doesn't add features — it **audits and hardens** what exists: move secrets to configuration, lock down `.gitignore`, write the onboarding contract (`.env.example`), and capture evidence that an authenticated tool call uses the right token.

## What This Day Covers

| Learning topic | Where |
|---|---|
| JWT / API token usage | [docs/jwt-api-token-usage.md](docs/jwt-api-token-usage.md) |
| Environment variables | [docs/environment-variables.md](docs/environment-variables.md) |
| Secret management | [docs/secret-management.md](docs/secret-management.md) |
| Least privilege | [docs/least-privilege.md](docs/least-privilege.md) |
| Tool-level authorization | [docs/tool-level-authorization.md](docs/tool-level-authorization.md) |

## Documents

| Document | What it covers |
|---|---|
| [security-checklist.md](security-checklist.md) | **The summary.** Every control as ✅ / ⚠️, grouped by JWT handling, env vars, secret management, least privilege, tool-level authz. |
| [evidence-authenticated-call.md](evidence-authenticated-call.md) | Captured proof (tokens redacted): config binds from User Secrets, `/api/v1/chat` is 401 without a session, and the HRMS call goes out under the per-user `Bearer` token. |
| [docs/jwt-api-token-usage.md](docs/jwt-api-token-usage.md) | The per-user token's lifecycle: login → encrypted cookie → `TokenManager` → `Bearer` header; the rules that keep it out of JS and logs. |
| [docs/environment-variables.md](docs/environment-variables.md) | The `__` → `:` binding convention, config precedence, Vite `VITE_` rules, and the `.env.example` contract. |
| [docs/secret-management.md](docs/secret-management.md) | The secret/config split, storage tiers (User Secrets → env → Key Vault), the Day 8 audit findings, and leak remediation. |
| [docs/least-privilege.md](docs/least-privilege.md) | Why HRMS is the authority, how per-user tokens prevent over-privilege, and the flagged gaps. |
| [docs/tool-level-authorization.md](docs/tool-level-authorization.md) | Tracing one tool call end-to-end: `[Authorize]` gate → in-process dispatch → per-user token → HRMS. |

## What Changed (code/config)

| Change | File | Why |
|---|---|---|
| Emptied real `Agent:ConnectionString` + `TenantId`, added pointer comment | `TeamAI/appsettings.Development.json` | Secrets out of source; real values moved to User Secrets. |
| Ignore `appsettings.Development.json`, `appsettings.*.Local.json`, `.env` | `TeamAI/.gitignore` | Prevent future secret commits. |
| Ignore `.env` / `.env.*` (keep `.env.example`) | `voyon-chat/.gitignore` | Same, for the frontend. |
| New onboarding contract | `TeamAI/.env.example`, `voyon-chat/.env.example` | Every required variable, documented, no real values. |
| Removed stale `Hrms:Token` from User Secrets | — | A dead real JWT no longer referenced by any code. |

> **No code logic changed.** The cookie-based auth design is untouched — this day only relocates secrets and documents the posture.

## Quick start (local dev)

```bash
# Backend secrets (real values, stored outside the repo)
cd TeamAI
dotnet user-secrets set "Agent:ConnectionString" "https://<resource>.services.ai.azure.com/api/projects/<project>"
dotnet user-secrets set "Agent:TenantId"        "<entra-tenant-guid>"
dotnet run

# Frontend
cd ../voyon-chat
cp .env.example .env   # leave VITE_API_BASE empty for same-origin dev
npm install && npm run dev
```

See the repository-root [README "Security" section](../README.md#security) for the full setup.
