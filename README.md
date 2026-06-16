# HRMS AI — Voyon Attendance Assistant

An AI attendance assistant for the HRMS platform. A chat UI lets a signed-in user ask about team
attendance and log live check-ins/check-outs in natural language; an Azure AI Foundry agent drives
the conversation and calls tools that proxy the HRMS API under the **user's own** credentials.

| Part | Stack | What it is |
|---|---|---|
| `TeamAI/` | .NET 9 / ASP.NET Core | Backend: drives the Foundry agent, runs its function tools in-process, proxies HRMS, owns auth. |
| `voyon-chat/` | React + Vite | Frontend chat SPA. Holds no credentials. |
| `Day 1/` … `Day 8/` | Markdown | Daily build log / design docs. **[Day 8](Day%208/README.md)** covers the security posture. |

## Local development

```bash
# 1. Backend secrets — stored in .NET User Secrets, outside the repo (never committed)
cd TeamAI
dotnet user-secrets set "Agent:ConnectionString" "https://<resource>.services.ai.azure.com/api/projects/<project>"
dotnet user-secrets set "Agent:TenantId"        "<entra-tenant-guid>"
dotnet run        # complete the one-time Foundry browser sign-in (dev)

# 2. Frontend
cd ../voyon-chat
cp .env.example .env      # leave VITE_API_BASE empty for same-origin dev (Vite proxies /api)
npm install && npm run dev
```

Every variable each side needs is documented in [`TeamAI/.env.example`](TeamAI/.env.example) and
[`voyon-chat/.env.example`](voyon-chat/.env.example) — copy and fill them in.

## Security

Full detail and evidence in [Day 8](Day%208/README.md) ([checklist](Day%208/security-checklist.md) ·
[evidence](Day%208/evidence-authenticated-call.md)). Summary of the posture:

### How the HRMS JWT is handled
- On login, the backend exchanges HRMS credentials for a per-user **JWT** and stores it **inside an
  encrypted, `HttpOnly` session cookie** (`voyon.session`, token key `hrms_jwt`). See
  [`AuthController`](TeamAI/Controllers/AuthController.cs).
- **The token is never exposed to JavaScript** — no `localStorage`, no `Authorization` header in the
  SPA. The browser only sends the cookie (`withCredentials: true`). This defeats token theft via XSS.
- Every outbound HRMS call resolves the **current user's** token from the cookie via
  [`TokenManager`](TeamAI/Services/TokenManager.cs) — there is **no shared/admin service token**, and
  no fallback: an unauthenticated request throws. So an agent tool call only ever acts on data the
  signed-in user's JWT permits ([least privilege](Day%208/docs/least-privilege.md)).
- The session expires with the JWT (`exp`); the token is never logged.

### How secrets are configured
- **No secrets in source.** Committed config (`appsettings.json`) holds only non-secret settings
  (`Hrms:BaseUrl`, `DefaultTeamId`, agent name/version). Secret values are blank in committed config.
- **Local dev:** real values live in **.NET User Secrets** (`<UserSecretsId>` in `TeamAI.csproj`),
  stored in your user profile, outside the repo.
- **CI / production:** **environment variables** using the ASP.NET Core `__` → `:` convention
  (`Agent__ConnectionString` → `Agent:ConnectionString`), or **Azure Key Vault** (wired in
  [`Program.cs`](TeamAI/Program.cs), enabled in Production when `KeyVault:Uri` is set).
- `appsettings.Development.json` and `.env` are **gitignored**; `git log -S` confirms no secret ever
  entered history. See [secret management](Day%208/docs/secret-management.md).

### Tool-level authorization
- The chat endpoint (`POST /api/v1/chat`) is `[Authorize]` — unauthenticated calls get **401** and
  never reach a tool.
- The Foundry agent's tools run **in-process** during the chat request (request-scoped DI), so each
  HRMS call inherits the signed-in user's token. Writes require explicit user confirmation.
  See [tool-level authorization](Day%208/docs/tool-level-authorization.md).

### Setting up from scratch
1. Copy `TeamAI/.env.example` values into User Secrets (or env vars) — `Agent__ConnectionString`,
   `Agent__TenantId`.
2. Copy `voyon-chat/.env.example` → `.env` (leave `VITE_API_BASE` empty for same-origin dev).
3. Never commit `.env` or `appsettings.Development.json` — they're gitignored for this reason.
