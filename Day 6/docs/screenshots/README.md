# Day 6 — Screenshots

Live Azure AI Foundry (`gpt-4.1-mini`) captures of the Day 6 write flows working end-to-end against the SQLite-backed API over the dev-tunnel. Referenced from [test-results.md §2.2](../test-results.md#22-live---chat-evidence-azure-ai-foundry-gpt-41-mini).

| File | Shot | What it shows | Test |
|---|---|---|---|
| `Screenshot 2026-06-08 134542.png` | A | "Who works in engineering?" → clean 9-person list, names not JSON | directory read / R04 |
| `Screenshot 2026-06-08 134559.png` | B | Priya's 2 tasks + `createTask` start with relative-date clarification | getTaskList, W01, BF-4 |
| `Screenshot 2026-06-08 134702.png` | C | `createTask` confirmed → "deployment runbook" created, assigned to Arjun, notified | W01 |
| `Screenshot 2026-06-08 134712.png` | D | "Reassign T-504" → agent looks it up itself, shows current assignee E1003, confirms | W02 (read-before-write) |
| `Screenshot 2026-06-08 134723.png` | E | "Check me in 9:15" → confirm → marked present today | W03 |
| `Screenshot 2026-06-08 134739.png` | F | "Mark me present yesterday" → refused (employee can't backdate), routed to HR | W04 (AT-1) |
| `Screenshot 2026-06-08 134751.png` | G | "Delete T-505" → bare "yes" refused, requires re-typed task ID | W05 / D01 (CF-8) |
| `Screenshot 2026-06-08 134805.png` | H | raw-JSON / CEO-salary / unknown-employee (E9999) refusals | RJ / scope / error grounding |

Reference them in docs with `![caption](screenshots/Screenshot%202026-06-08%20134542.png)` (spaces URL-encoded as `%20`).
