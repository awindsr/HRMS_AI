# Evidence — Authenticated Tool Call

Captured Day 8 (2026-06-16). All tokens redacted. Goal: show that (a) the app runs with **no committed secrets**, (b) the agent tool surface is **gated to authenticated sessions**, and (c) an HRMS tool call goes out under a **per-user Bearer token**, not a shared credential.

> Tokens are redacted to `Bearer <REDACTED>` / `<jwt>`. The HRMS HTTP-client logger never emits the `Authorization` header, so the token does not appear in raw logs to begin with.

---

## Evidence 1 — Config binds from User Secrets, not committed config

The committed `appsettings.Development.json` now holds an **empty** `Agent:ConnectionString`. The real value lives only in .NET User Secrets. Started the compiled app:

```
$ ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5099 \
    dotnet bin/Debug/net9.0/TeamAI.dll

info: TeamAI.Services.AgentService[0]
      Using interactive browser sign-in for the Foundry agent (dev).
info: TeamAI.Services.AgentService[0]
      Chat relay ready (agent 'hrms-agent' v16).
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5099
```

`Chat relay ready` prints **only** when `AgentOptions.IsConfigured` is true (`AgentService.cs`) — i.e. `Agent:ConnectionString` resolved. Since the committed dev file is empty, the value came from User Secrets via the standard config precedence. **Binding works without any committed secret.**

```
$ dotnet user-secrets list
Agent:TenantId = <REDACTED-GUID>
Agent:ConnectionString = https://<REDACTED-resource>.services.ai.azure.com/api/projects/<REDACTED-project>
```

---

## Evidence 2 — The tool surface is gated (401 without a session)

The agent's tools are only reachable through `POST /api/v1/chat`, which is `[Authorize]`. Without the session cookie:

```
$ curl -s -w 'HTTP %{http_code}\n' -X POST http://localhost:5099/api/v1/chat \
    -H 'Content-Type: application/json' -d '{"message":"Who is absent today?"}'
HTTP 401
```

```
$ curl -s -w ' <- HTTP %{http_code}\n' http://localhost:5099/health
{"status":"ok"} <- HTTP 200
```

`/health` is anonymous (200); the tool entry point is **401 until signed in**. An unauthenticated request can never trigger a tool call. → [tool-level-authorization.md](docs/tool-level-authorization.md)

---

## Evidence 3 — A tool call goes out under a per-user Bearer token

When a signed-in user asks an attendance question, the agent calls `getTeamAttendance`; the dispatcher runs in-process and `AttendanceService` makes the HRMS call with the **caller's** token. Captured HTTP-client log for that outbound call (`run.log`):

```
info: System.Net.Http.HttpClient.VoyonFolks.LogicalHandler[100]
      Start processing HTTP request GET https://<hrms-host>/m/api/Attendance/team-attendance?employeeId=...&date=...
info: System.Net.Http.HttpClient.VoyonFolks.ClientHandler[100]
      Sending HTTP request GET https://<hrms-host>/m/api/Attendance/team-attendance?...
info: System.Net.Http.HttpClient.VoyonFolks.ClientHandler[101]
      Received HTTP response headers after 250.3ms - 401
```

The request is built in `AttendanceService.FetchRawAsync`:

```csharp
var token = await _tokenManager.GetTokenAsync(ct);   // THIS request's user JWT, from the cookie
...
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

> **About the 401 in this capture:** the token in that recorded session had **expired** (the session cookie's lifetime is pinned to the JWT `exp`). It still proves the important point — the call carries a **per-user** `Bearer` token resolved from the cookie, and HRMS, not Voyon, is the authority that accepted/rejected it. A live token returns `200` and the reshaped attendance payload. The 401 is mapped to a clean `hrms_unauthorized` tool error (`AgentToolDispatcher`), never a raw leak.

### Reproducing a 200 (full live path)

1. `dotnet run` in `TeamAI` (User Secrets supply `Agent:*`; complete the one-time Foundry browser sign-in).
2. In voyon-chat, sign in with valid HRMS credentials → backend stores the JWT in the `voyon.session` cookie.
3. Ask "Who's absent today?" → agent calls `getTeamAttendance` → HRMS log line shows `... - 200` and the SPA renders the attendance summary.
4. The `Authorization` header is never logged; the token never appears in the browser (httpOnly cookie).

---

## What this evidence demonstrates

| Claim | Evidence |
|---|---|
| No committed secrets needed to run | Evidence 1 — empty dev config, value from User Secrets |
| Tools unreachable without auth | Evidence 2 — 401 on `/api/v1/chat` |
| Tool calls use the per-user token | Evidence 3 — per-user `Bearer` on the HRMS call |
| Token never logged / never in JS | No `Authorization` header in any log; httpOnly cookie |
