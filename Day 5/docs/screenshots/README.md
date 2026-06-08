# Day 5 — Screenshots

Captured evidence of the live agent's function-calling behavior, taken from the **Azure AI Foundry** `hrms-agent` playground and its trace viewer. Referenced from [test-results.md §5](../test-results.md#5-captured-evidence-azure-ai-foundry) and [api-call-logs.md §5](../api-call-logs.md#5-trace-evidence-tool-execution).

| File | What it shows |
|---|---|
| `Screenshot 2026-06-05 155528.png` | Playground: agent **refuses cross-employee access** ("tell me about employee E1003" → declined) then returns the signed-in user's own profile via `getEmployeeDetails` |
| `Screenshot 2026-06-05 155541.png` | Playground: self profile + "Any tasks assigned to me?" → `getTaskList`, with the `openapi_call` / tool-call markers visible |
| `Screenshot 2026-06-05 155549.png` | Playground: full conversation — self details, task list, and **honest decline** of "how many leaves are remaining for me" (no `getLeaveBalance` tool connected) |
| `Screenshot 2026-06-05 162102.png` | Trace / Trajectories view: `execute_tool ... getEmployeeDetails` (2.38s) and `execute_tool ... getTaskList` (1.94s) — proof the tools really executed |

Reference them in docs with `![caption](screenshots/Screenshot%202026-06-05%20155528.png)` (spaces URL-encoded as `%20`).
