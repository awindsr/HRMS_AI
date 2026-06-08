# Day 6 — Write Tools, Confirmation Flow & Business-Friendly Responses

**Topic:** Going from read-only to **state-changing** tools — task creation, task assignment, attendance marking, and deletion — gated behind an explicit confirmation flow, and making the agent's replies sound like a helpful HR colleague instead of a JSON dump.

Days 1–4 produced requirements, rules, prompts, and schemas; Day 5 built the first three **read** tools and the live function-calling loop. Day 6 designs the first **write** tools and the safety + response-quality machinery they require. This day is **design/spec** — schemas, rules, and flows — not yet wired into code.

## What This Day Covers

| Topic | Where |
|---|---|
| What write tools are & why they're different | [write-tools-design.md §1–2](docs/write-tools-design.md#1-read-vs-write-recap) |
| Task creation (`createTask`) | [write-tools-design.md §4](docs/write-tools-design.md#4-createtask--r2-soft-write) |
| Task assignment (`assignTask`) | [write-tools-design.md §5](docs/write-tools-design.md#5-assigntask--r3-hard-write) |
| Attendance marking (`markAttendance`) | [attendance-tools.md](docs/attendance-tools.md) |
| Confirmation flow (the gate before every write) | [confirmation-flow.md](docs/confirmation-flow.md) |
| Delete risk (`deleteTask`, R4) | [delete-risk-notes.md](docs/delete-risk-notes.md) |
| Business-friendly responses | [response-quality-guidelines.md §2](docs/response-quality-guidelines.md#2-business-friendly-responses) |
| Empty result handling | [response-quality-guidelines.md §3](docs/response-quality-guidelines.md#3-empty-result-handling) |
| Raw JSON avoidance | [response-quality-guidelines.md §4](docs/response-quality-guidelines.md#4-raw-json-avoidance) |

## Documents

| Document | What it covers |
|---|---|
| [docs/write-tools-design.md](docs/write-tools-design.md) | **Start here.** What changes when a tool writes; the four Day 6 write tools (`createTask`, `assignTask`, `markAttendance`, `deleteTask`), full input/output schemas, risk levels, and the idempotency problem |
| [docs/confirmation-flow.md](docs/confirmation-flow.md) | The COLLECT → SUMMARIZE → GATE → EXECUTE → REPORT protocol, the `confirmed` flag, good vs bad summaries, and worked conversation flows for each write tool |
| [docs/attendance-tools.md](docs/attendance-tools.md) | `markAttendance` design — self check-in vs HR regularization, why attendance is payroll-grade data, schema, business rules, and the read/write attendance pair |
| [docs/delete-risk-notes.md](docs/delete-risk-notes.md) | Why `deleteTask` is R4, the destructive-action checklist, soft-delete vs hard-delete, and the extra guardrails deletions require |
| [docs/response-quality-guidelines.md](docs/response-quality-guidelines.md) | Turning tool output into clear answers: business-friendly tone, empty-result handling, and never leaking raw JSON / internal IDs |
| [docs/system-prompt-v3.md](docs/system-prompt-v3.md) | System prompt v2 → v3: adds the write tools, the confirmation policy in force, and the response-quality rules |
| [docs/test-results.md](docs/test-results.md) | Test plan + results template: confirmation gating, idempotency, empty results, delete guardrails, response quality |
| [docs/screenshots/](docs/screenshots/) | Where to drop confirmation-flow / write-tool screenshots |

## The Four Write Tools (new in Day 6)

| Tool | REST endpoint | Type | Risk | Confirmation |
|---|---|---|---|---|
| `createTask` | `POST /api/v1/tasks` | Create | R2 | **Yes** |
| `assignTask` | `PATCH /api/v1/tasks/{taskId}/assignment` | Update | R3 | **Yes** |
| `markAttendance` | `POST /api/v1/attendance` | Create | R2 (self) / R3 (others) | **Yes** |
| `deleteTask` | `DELETE /api/v1/tasks/{taskId}` | Delete | R4 | **Yes + double-confirm** |

These extend the task domain built in Day 5 (`getTaskList`, sample tasks `T-501`/`T-502`/`T-504`) and the attendance design from [Day 4](../Day%204/docs/hrms-api-tool-map.md#22-getattendance).

## Required Outputs — Checklist

- [ ] **Write-tool schemas** — `createTask`, `assignTask`, `markAttendance`, `deleteTask` with input/output schemas & risk ([write-tools-design.md](docs/write-tools-design.md))
- [ ] **Confirmation flow** — the gate documented with worked examples per tool ([confirmation-flow.md](docs/confirmation-flow.md))
- [ ] **Response-quality rules** — business-friendly tone, empty-result handling, raw-JSON avoidance ([response-quality-guidelines.md](docs/response-quality-guidelines.md))
- [ ] **Delete-risk notes** — why deletion is the highest risk and how it's guarded ([delete-risk-notes.md](docs/delete-risk-notes.md))
- [ ] **Updated system prompt** — [system-prompt-v3.md](docs/system-prompt-v3.md), saved as `system-prompt.txt` for `--chat`
- [ ] **Test plan / results** — fill [test-results.md](docs/test-results.md), add screenshots to [screenshots/](docs/screenshots/)

## Build Log

- Day 1: Requirements, user personas, tool catalogue, basic API map
- Day 2: Agent rules, confirmation policy, system prompt, threat model
- Day 3: Live agent on Azure AI Foundry, tested against real prompts
- Day 4: Full JSON schemas, risk classification, safety rulebook
- Day 5: First real **read** tools — function calling, API wrapper, error handling, logging
- **Day 6: First **write** tools — confirmation flow, attendance marking, delete risk, business-friendly responses ← today**

---

> Related docs: [Day 4 — tool-safety-rules.md](../Day%204/docs/tool-safety-rules.md) · [Day 4 — hrms-api-tool-map.md](../Day%204/docs/hrms-api-tool-map.md) · [Day 5 — system-prompt-v2.md](../Day%205/docs/system-prompt-v2.md)
