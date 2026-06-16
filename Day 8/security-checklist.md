# Security Checklist — Day 8

Posture review for Voyon (TeamAI backend + voyon-chat frontend). ✅ = in place, ⚠️ = acceptable gap / note, ❌ = action needed (none open at sign-off).

## JWT / token handling
- ✅ **Per-user HRMS JWT, not a service token** — resolved per request in [`TokenManager`](../TeamAI/Services/TokenManager.cs); no shared/admin fallback (it throws if unauthenticated).
- ✅ **Token stored httpOnly + encrypted** — kept inside the `voyon.session` auth cookie via `StoreTokens("hrms_jwt", ...)`; never in `localStorage` or JS.
- ✅ **Token never sent to the browser** — SPA only sends the cookie (`withCredentials: true`); holds no token.
- ✅ **Token never logged** — `TokenManager`/`AttendanceService` don't log it; the HTTP-client logger omits the `Authorization` header.
- ✅ **Session lifetime = token lifetime** — cookie `ExpiresUtc` from JWT `exp`; `AllowRefresh`/`SlidingExpiration` false, so no stale credential lingers.
- ✅ **Rejected token handled cleanly** — HRMS 401 → `HrmsUnauthorizedException` → `hrms_unauthorized` tool error; no raw upstream leak.

## Environment variables & config binding
- ✅ **`__` → `:` binding verified** — `Agent__ConnectionString` binds to `AgentOptions.ConnectionString` via `AddOptions().Bind(GetSection("Agent"))` in [`Program.cs`](../TeamAI/Program.cs).
- ✅ **App runs from User Secrets with empty committed config** — startup logged `Chat relay ready ... v16` with `appsettings.Development.json` `ConnectionString:""`. See [evidence](evidence-authenticated-call.md).
- ✅ **`.env.example` onboarding contract** — committed for both [TeamAI](../TeamAI/.env.example) and [voyon-chat](../voyon-chat/.env.example); no real values.
- ✅ **Frontend exposes only `VITE_` non-secrets** — `VITE_API_BASE` only; no secret reachable from client code by design.

## Secret management
- ✅ **No hardcoded secrets in committed source** — `Agent:ConnectionString`/`TenantId` emptied in dev config; real values in User Secrets (dev) / env vars / Key Vault (prod).
- ✅ **Secrets never in git history** — `git log -S` for the tenant guid and the resource name returns no commits.
- ✅ **`appsettings.Development.json` + `.env` gitignored** — added to [TeamAI/.gitignore](../TeamAI/.gitignore) and [voyon-chat/.gitignore](../voyon-chat/.gitignore).
- ✅ **`voyon-chat/.env` untracked** — was tracked; `git rm --cached` applied (held only non-secret `VITE_API_BASE`, no rotation needed).
- ✅ **Stale `Hrms:Token` removed** — a dead, real HRMS JWT lingered in *local* User Secrets (never in git, not referenced by code); removed via `dotnet user-secrets remove`.
- ✅ **Production secret store wired** — Key Vault via `DefaultAzureCredential`, gated to Production + `KeyVault:Uri` set.
- ⚠️ **Legacy `Day 5 - Project/.../appsettings.json` holds `ApiKey: "local-dev-key"`** — placeholder, not a real secret; out of scope for the live app. Left as-is.

## Least privilege
- ✅ **HRMS is the authorization authority** — the per-user JWT (`role`/`AccessLevels`) scopes every call; Voyon adds no privilege of its own.
- ✅ **No ambient/admin credential in the call path** — verified by absence of a service-token branch in `TokenManager`.
- ✅ **Foundry uses managed identity in prod** — `DefaultAzureCredential`; `InteractiveLogin` is dev-only.
- ✅ **CORS closed by default** — `AllowCredentials()` only when origins are explicitly configured.
- ✅ **Minimal tool surface** — only `getTeamAttendance` + `logAttendance` declared.
- ⚠️ **`team_id` is a client-supplied tool arg** — not pre-validated against team ownership; safe because HRMS still scopes by the caller's token. Note if HRMS authz is ever loosened.
- ⚠️ **No Voyon-side role checks** — all authz delegated to HRMS; a future HRMS-ungated action would need its own check.

## Tool-level authorization
- ✅ **Chat surface gated** — `[Authorize]` on [`ChatController`](../TeamAI/Controllers/ChatController.cs); unauthenticated `POST /api/v1/chat` → **401** (captured).
- ✅ **Tools run in-process, request-scoped** — `AgentService` runs the function-call loop; `AgentToolDispatcher` is `Scoped`, so tools inherit the signed-in user's `HttpContext`/token.
- ✅ **Tool calls carry the caller's Bearer token** — HRMS HTTP log shows the per-user `Bearer` call (token redacted). See [evidence](evidence-authenticated-call.md).
- ✅ **Model-supplied ids re-validated** — `LogAttendanceAsync` re-resolves the employee id from data the user's token can see.
- ✅ **Writes require confirmation** — enforced in the agent instructions + `logAttendance` description.
- ✅ **Tool-hop cap** — `MaxToolHops = 6` prevents runaway tool loops.

## Sign-off
No open ❌ items. Two ⚠️ least-privilege notes and one ⚠️ legacy-file note are documented and accepted for the current scope (HRMS remains the enforcement point). Detail in [docs/](docs/).
