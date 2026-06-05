# HRMS AI Agent — Error Handling Notes (Day 5)

> Day 5 deliverable. The error taxonomy for the read tools, where each error is caught, what the model sees, and how to reproduce every path.

---

## 1. Principle: Errors Are Data, Not Crashes

A tool must **never** throw an unhandled exception into the agent loop, and must **never** hand the model a raw stack trace. Every failure is converted into a small, clean JSON object the model can read and explain to the user honestly — directly enforcing the Day 2/3 grounding rule: *if a tool fails, say so plainly; never invent a fallback answer*.

```
 HTTP / network failure
        │
        ▼
 HrmsApiClient  ── catches & classifies ──►  ApiResult<T>.Fail(code, message)
        │
        ▼
 HrmsTools      ── serializes ──►  { "error": "<code>", "message": "<text>" }
        │
        ▼
 Agent loop     ── feeds JSON back to model ──►  model explains the failure to the user
```

---

## 2. Error Taxonomy

All errors are caught in `HrmsApiClient.GetAsync<T>` (Step 6 of the [build guide](build-guide.md#step-6--the-api-wrapper-hrmsapiclient)) and mapped to a stable `error` code:

| `error` code | Trigger | Caught via | What the model is told |
|---|---|---|---|
| `not_found` | API returns HTTP 404 (e.g. unknown employee ID) | `StatusCode == NotFound` | "The requested record does not exist." |
| `upstream_error` | API returns any other non-2xx (500, 403, …) | `!IsSuccessStatusCode` | "HRMS API returned HTTP `<code>`." |
| `empty_response` | 2xx but body deserialized to `null` | null check after deserialize | "The HRMS API returned no usable data." |
| `timeout` | No response within `Hrms:HttpTimeoutSeconds` | `TaskCanceledException` | "The HRMS API did not respond in time." |
| `network_error` | Connection refused, DNS failure, socket error | `HttpRequestException` | "Could not reach the HRMS API." |
| `unknown_error` | Any other exception (last-resort guard) | `catch (Exception)` | "An unexpected error occurred." |
| `missing_argument` | Required tool arg absent/blank (e.g. empty `employeeId`) | validated in `HrmsTools` before the call | "`employeeId` is required." |

Two layers: **transport/HTTP errors** are classified in the wrapper; **argument validation** happens in the tool before any HTTP call is made.

---

## 3. Why Catch Each One Separately

| Exception | Why it needs its own branch |
|---|---|
| `TaskCanceledException` | `HttpClient` raises this on timeout. Without a dedicated catch it looks like an arbitrary cancellation; we surface it as an actionable "try again". |
| `HttpRequestException` | Means we never got an HTTP response at all (vs. a 4xx/5xx, which *is* a response). Different user message: "can't reach" vs. "server said no". |
| Non-2xx status | Not an exception in .NET — `GetAsync` succeeds. We must inspect `IsSuccessStatusCode` explicitly or silently treat an error page as data. |
| `null` after deserialize | A 204/empty/garbage body deserializes to `null`. Guarding here prevents a `NullReferenceException` downstream. |
| Catch-all `Exception` | Defense in depth: a JSON parse error or unforeseen issue still yields a clean error code instead of crashing the loop. |

---

## 4. Reproducing Each Path

The mock API supports fault injection so every branch is testable.

| Path | How to trigger |
|---|---|
| `not_found` | `getEmployeeDetails("E9999")` — the test runner already does this. |
| `missing_argument` | `getEmployeeDetails("")` — also in the test runner. |
| `timeout` | Add `?fail=timeout` to the employee-list request (mock sleeps 30s > 10s client timeout). |
| `upstream_error` | Add `?fail=500` to the employee-list request. |
| `network_error` | Stop/skip `MockHrmsApi.Start`, or set `Hrms:ApiBaseUrl` to an unused port. |
| `empty_response` | Point a tool at an endpoint that returns `204 No Content`. |

Each reproduced error also produces a distinct line in `logs/api-calls.log` (outcome column: `notfound`, `timeout`, `http_err`, `neterr`, …) — see [api-call-logs.md](api-call-logs.md).

---

## 5. What the User Ultimately Sees

The model receives the structured error and turns it into plain language — never a code or stack trace (Day 4 output rule OH-1/OH-3):

```
 tool result:  { "error": "not_found", "message": "The requested record does not exist." }

 assistant:    "I couldn't find an employee with ID E9999. Could you double-check the ID?"
```

```
 tool result:  { "error": "timeout", "message": "The HRMS API did not respond in time." }

 assistant:    "The HR system isn't responding right now, so I can't pull that up.
                Please try again in a moment."
```

---

## 6. Limitations (Day 5)

- **No retries** — a transient blip surfaces as an error. Acceptable for reads; revisit with Polly ([api-configuration.md §6](api-configuration.md#6-timeouts-retries--resilience)).
- **No partial-result handling** — list endpoints either return fully or error; there's no streaming/pagination cursor yet.
- **Mock-only auth** — the `X-Api-Key` header is sent but the mock doesn't validate it. A real HRMS would return `401/403`, which already map to `upstream_error`.

---

> Related docs: [build-guide.md](build-guide.md) · [api-configuration.md](api-configuration.md) · [api-call-logs.md](api-call-logs.md) · [Day 4 — tool-safety-rules.md §7](../../Day%204/docs/tool-safety-rules.md#7-output-handling-rules)
