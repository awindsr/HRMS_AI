# HRMS AI Agent — API Call Logs (Day 5)

> Day 5 deliverable. The log format the `ApiLogger` emits, plus a representative captured run. Replace the sample slice below with your own `logs/api-calls.log` after you build and run.

---

## 1. Log Format

Every outbound HRMS API call writes one line to **console** (prefixed `[API]`) and to **`logs/api-calls.log`**. Fields are pipe-delimited:

```
<timestamp> | <method> | <status> | <duration> | <outcome> | <url> [| ERROR: <detail>]
```

| Field | Meaning |
|---|---|
| `timestamp` | `yyyy-MM-dd HH:mm:ss.fff`, local time |
| `method` | HTTP verb (`GET` for all Day 5 read tools) |
| `status` | HTTP status code, or `---` if no response was received (timeout/network) |
| `duration` | Wall-clock time for the call, in ms |
| `outcome` | `ok` · `notfound` · `http_err` · `timeout` · `neterr` · `empty` · `error` |
| `url` | Full request URL incl. query string |
| `ERROR: …` | Present only on failures; the exception/cause detail |

The `outcome` column maps 1:1 to the error taxonomy in [error-handling-notes.md §2](error-handling-notes.md#2-error-taxonomy).

---

## 2. Sample Captured Log (test-runner run)

> Illustrative output from `dotnet run` (the offline test runner). Timings are machine-dependent. **Regenerate this from your own run before submitting.**

```text
2026-06-05 10:14:02.118 | GET  | 200 |    14ms | ok      | http://localhost:5099/api/v1/employees
2026-06-05 10:14:02.140 | GET  | 200 |     6ms | ok      | http://localhost:5099/api/v1/employees?department=engineering&status=active
2026-06-05 10:14:02.151 | GET  | 200 |     5ms | ok      | http://localhost:5099/api/v1/employees/E1001
2026-06-05 10:14:02.162 | GET  | 404 |     4ms | notfound| http://localhost:5099/api/v1/employees/E9999
2026-06-05 10:14:02.171 | GET  | 200 |     7ms | ok      | http://localhost:5099/api/v1/tasks?employeeId=E1001
2026-06-05 10:14:02.180 | GET  | 200 |     5ms | ok      | http://localhost:5099/api/v1/tasks?status=blocked
```

Note: `getEmployeeDetails("")` (the missing-argument case) produces **no** log line — it is rejected by argument validation in `HrmsTools` *before* any HTTP call is made. That is the intended behavior.

---

## 3. Sample Error Paths

Captured after forcing the fault-injection cases from [error-handling-notes.md §4](error-handling-notes.md#4-reproducing-each-path):

```text
2026-06-05 10:18:44.502 | GET  | --- | 10003ms | timeout | http://localhost:5099/api/v1/employees?fail=timeout | ERROR: request timed out
2026-06-05 10:19:07.231 | GET  | 500 |     9ms | http_err| http://localhost:5099/api/v1/employees?fail=500 | ERROR: HTTP 500
2026-06-05 10:20:15.880 | GET  | --- |     2ms | neterr  | http://localhost:5099/api/v1/employees | ERROR: Connection refused (localhost:5099)
```

- The `timeout` row shows ~10 000 ms — the client `HttpTimeoutSeconds` (10) firing before the mock's 30 s sleep.
- The `neterr` row was captured by pointing `Hrms:ApiBaseUrl` at a port with no listener.

---

## 4. Reading the Log for the Submission

For the Day 5 writeup, point to:
- **Happy paths** (§2) — proves all three tools make real HTTP calls and succeed.
- **At least one error path** (§3) — proves the error handling is exercised, not just written.
- The fact that the **missing-argument** case produces no HTTP call — proves validation happens before the network.

---

> Related docs: [build-guide.md](build-guide.md) · [error-handling-notes.md](error-handling-notes.md) · [test-results.md](test-results.md)
