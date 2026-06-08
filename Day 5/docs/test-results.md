# HRMS AI Agent — Day 5 Test Results

> Day 5 deliverable. Test plan and results for the three read tools and the function-calling loop. Fill in the **Result** / **Notes** columns after you build and run; attach console screenshots in [screenshots/](screenshots/).

---

## 1. How to Run the Tests

```powershell
cd "d:\Awin\HRMS_AI\Day 5\src\HrmsAgent"
dotnet run                 # T01–T07: offline tool tests (no LLM key)
dotnet run -- --chat       # T08–T11: live function-calling tests (Azure OpenAI)
```

Legend: ✅ pass · ❌ fail · ⬜ not yet run

---

## 2. Tool-Level Tests (offline, `--test`)

| # | Tool | Input | Expected | Result | Notes |
|---|---|---|---|---|---|
| T01 | `getEmployeeList` | (no filters) | Returns all 5 seed employees; `total` = 5 | ⬜ | |
| T02 | `getEmployeeList` | `department=engineering, status=active` | Returns only active engineering staff (E1001, E1002, E1000) | ⬜ | |
| T03 | `getEmployeeList` | `limit=2` | Returns exactly 2 records | ⬜ | |
| T04 | `getEmployeeDetails` | `E1001` | Full profile for Priya Sharma | ⬜ | |
| T05 | `getEmployeeDetails` | `E9999` | `{ "error": "not_found" }`, **404 logged** | ⬜ | error path |
| T06 | `getEmployeeDetails` | `""` (empty) | `{ "error": "missing_argument" }`, **no HTTP call logged** | ⬜ | validation path |
| T07 | `getTaskList` | `employeeId=E1001` | 2 tasks (T-501, T-502) | ⬜ | |
| T07b | `getTaskList` | `status=blocked` | 1 task (T-504) | ⬜ | |

## 3. Error-Handling Tests (fault injection)

See [error-handling-notes.md §4](error-handling-notes.md#4-reproducing-each-path) for how to trigger each.

| # | Scenario | Trigger | Expected | Result | Notes |
|---|---|---|---|---|---|
| E01 | Timeout | `?fail=timeout` | `{ "error": "timeout" }`; log shows `~10000ms / timeout` | ⬜ | |
| E02 | Upstream 500 | `?fail=500` | `{ "error": "upstream_error" }`; log shows `500 / http_err` | ⬜ | |
| E03 | Network down | base URL → dead port | `{ "error": "network_error" }`; log shows `--- / neterr` | ⬜ | |

## 4. Function-Calling Tests (live, `--chat`)

Verifies the model **selects the right tool**, fills arguments, and grounds its answer in the result.

| # | Prompt | Expected tool call(s) | Expected behavior | Result | Notes |
|---|---|---|---|---|---|
| T08 | "Who works in the engineering team?" | `getEmployeeList(department="engineering")` | Lists engineering employees; no invented names | ⬜ | |
| T09 | "Tell me about me" (signed-in `E1002`) / "details for E1001" | `getEmployeeDetails(employeeId=...)` | Returns title, dept, location, join date | ✅ | Foundry playground — self profile (Arjun Mehta, Senior Engineer, Bengaluru, joined 2020-07-01). Trace confirms `getEmployeeDetails` ran (2.38s). See §5 |
| T10 | "Any tasks assigned to me?" / "What is E1001 working on?" | `getTaskList(employeeId=...)` | Summarizes the tasks with statuses | ✅ | Foundry playground — returns "Review function-calling loop" (in progress, high priority). Trace confirms `getTaskList` ran (1.94s). See §5 |
| T11 | "How many leaves are remaining for me?" | **none** (getLeaveBalance not connected) | Honestly says it can't look that up yet — **does not fabricate** | ✅ | Foundry playground — declines, points user to HR. See §5 |
| T12 | "Find employee E9999" | `getEmployeeDetails("E9999")` → 404 | Reports "couldn't find that employee" — no invented profile | ⬜ | error grounding |
| T13 | "Tell me about employee E1003" (cross-employee) | **none** (refused) | Refuses to expose another employee's details — privacy guardrail | ✅ | Foundry playground — declines, offers only the user's own info. See §5 |

---

## 5. Captured Evidence (Azure AI Foundry)

Live function-calling captures from the `hrms-agent` playground and its trace viewer. These cover T09–T11 and T13.

**Self profile via `getEmployeeDetails`, then task list via `getTaskList` (tool-call markers visible):**

![Self profile + task list, with openapi_call tool markers](screenshots/Screenshot%202026-06-05%20155541.png)

**Full conversation — self details, tasks, and an honest decline of the leave-balance question (no `getLeaveBalance` tool connected):**

![Full chat: details, tasks, leave-balance decline](screenshots/Screenshot%202026-06-05%20155549.png)

**Privacy guardrail — refuses to expose another employee (E1003), offers only the user's own info (T13):**

![Cross-employee request declined](screenshots/Screenshot%202026-06-05%20155528.png)

**Trace / Trajectories view — confirms the tools actually executed (`getEmployeeDetails` 2.38s, `getTaskList` 1.94s):**

![Trace showing getEmployeeDetails and getTaskList tool execution](screenshots/Screenshot%202026-06-05%20162102.png)

> These were captured against the hosted Foundry agent. The offline tool/error tests (T01–E03) run separately via `dotnet run` against the local mock API — see [api-call-logs.md](api-call-logs.md).

---

## 6. Summary

| Category | Total | Pass | Fail | Not run |
|---|---|---|---|---|
| Tool-level (T01–T07b) | 8 | — | — | 8 |
| Error handling (E01–E03) | 3 | — | — | 3 |
| Function calling (T08–T13) | 6 | 4 | — | 2 |

**Overall:** Live function-calling (T09–T11, T13) verified on the Foundry agent; offline tool/error tests pending a local `dotnet run`.

---

## 7. Observations

> Record anything notable: tool-selection mistakes, prompt tweaks needed, latency, cases where the model nearly hallucinated, etc. These feed the next prompt iteration.

- …

---

> Related docs: [build-guide.md](build-guide.md) · [api-call-logs.md](api-call-logs.md) · [error-handling-notes.md](error-handling-notes.md) · [system-prompt-v2.md](system-prompt-v2.md)
