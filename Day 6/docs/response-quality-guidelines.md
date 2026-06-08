# HRMS AI Agent — Response Quality Guidelines (Day 6)

> Day 6 deliverable. How the agent turns tool output into answers a non-technical HR user actually wants to read: business-friendly tone, honest empty-result handling, and never leaking raw JSON or internal IDs. Extends the Day 4 output rules **OH-1…OH-6** and the Day 5 prompt's output-style section.

---

## Table of Contents

1. [The Principle: The User Never Sees the Plumbing](#1-the-principle-the-user-never-sees-the-plumbing)
2. [Business-Friendly Responses](#2-business-friendly-responses)
3. [Empty Result Handling](#3-empty-result-handling)
4. [Raw JSON Avoidance](#4-raw-json-avoidance)
5. [Formatting Tool Output by Shape](#5-formatting-tool-output-by-shape)
6. [Quick Do / Don't Reference](#6-quick-do--dont-reference)

---

## 1. The Principle: The User Never Sees the Plumbing

A tool returns a machine object. The user asked a human question. The agent's job is translation:

```
 tool result (machine)                      user-facing answer (human)
 ────────────────────────────────────       ─────────────────────────────────
 {                                           "Priya has 2 open tasks:
   "total": 2,                                 • Review the function-calling loop
   "tasks": [                                    (high priority, due Fri)
     { "taskId": "T-501",                      • Fix the attendance import
       "title": "Review the                      (blocked)
        function-calling loop",               Want details on either?"
       "status": "in_progress",
       "priority": "high",
       "dueDate": "2026-06-12" },
     { "taskId": "T-504",
       "title": "Fix the attendance import",
       "status": "blocked" }
   ]
 }
```

The machine object is never shown. The answer leads with the point, uses names not IDs, and offers a next step.

---

## 2. Business-Friendly Responses

The agent speaks as a competent, friendly HR colleague — not a database, not a developer console.

### Tone rules

| ID | Rule | Example |
|---|---|---|
| **BF-1** | **Lead with the answer**, then the supporting detail. | "You have 6 casual days left." *then* the breakdown — not the reverse. |
| **BF-2** | Use **plain business language**, not system jargon. | "task tracker" not "task entity"; "assigned to" not "assigneeId set to". |
| **BF-3** | Refer to people by **name**, IDs only when needed for disambiguation. | "Priya Sharma" not "E1001"; "Priya (E1001)" only if two Priyas. |
| **BF-4** | **Humanize dates and durations.** | "Friday 12 Jun" not "2026-06-12"; "3 days" not "72 hours". |
| **BF-5** | Keep it **concise** — short sentences, bullets, small tables. No walls of text. |
| **BF-6** | Match the **register to the action.** Routine reads are light; writes and especially deletes are precise and careful. |
| **BF-7** | When declining or hitting an error, **explain briefly and offer a path forward** — never a bare "no" or a raw error. |

### Same fact, three registers

```
 Read (light):    "All set — Priya has 2 open tasks right now."

 Write (precise): "Done — created T-507 and assigned it to Priya. She's been notified."

 Delete (careful):"Deleted T-501. That can't be undone, and the action was logged."
```

### Confirmations are business-friendly too

The confirmation summary ([confirmation-flow.md §5](confirmation-flow.md#5-writing-a-good-summary)) follows the same rules: names not IDs, humanized dates, the consequence stated plainly. A good summary *is* a business-friendly response.

---

## 3. Empty Result Handling

A tool returning **zero results is a valid, successful answer** — not an error, and never an excuse to invent data. This directly enforces Day 4 **OH-4** (*if a tool returns empty results, say so; do not invent records*) and the Day 5 grounding rule.

### The empty-result rules

| ID | Rule |
|---|---|
| **ER-1** | An empty list (`total: 0`, `[]`) means *nothing matched* — state that plainly. Never fabricate a plausible-looking record to fill the gap. |
| **ER-2** | Distinguish **"none found"** from **"couldn't look it up."** Empty success ≠ error. "No tasks are assigned to you" is different from "I couldn't reach the task system." |
| **ER-3** | Give the empty result **context**: what was searched, and a constructive next step. |
| **ER-4** | If the filter may be the reason for emptiness, **say so** and offer to widen it. |

### Examples

```
 tool result: { "total": 0, "tasks": [] }   (getTaskList for E1002)

 GOOD:  "You don't have any tasks assigned right now. Want me to create one,
         or check a teammate's list?"

 BAD:   "You have a task 'Review Q2 report' due next week."   ← fabricated, violates ER-1
 BAD:   "Error: no tasks."                                    ← it's not an error (ER-2)
```

```
 tool result: { "total": 0 }   (getLeaveRequests, status=pending, team=sales)

 GOOD:  "No pending leave requests for the Sales team. (I filtered to pending —
         I can include approved/rejected if you'd like the full picture.)"   ← ER-4
```

### Empty vs error — decision

```
 Did the tool succeed (2xx) but return no rows?
 ├── YES → empty result → "none found" message (ER-1..ER-4)
 └── NO  → it's an error → honest error message (Day 5 error taxonomy)
```

---

## 4. Raw JSON Avoidance

The model receives JSON; the user must never be handed it back. This is Day 4 **OH-1** made concrete.

### The rules

| ID | Rule |
|---|---|
| **RJ-1** | **Never paste raw JSON, payloads, or object dumps** into the reply. Translate to prose, bullets, or a small table. |
| **RJ-2** | **Never expose internal/system identifiers** unless the user needs them to act. Hide `attendanceId`, `clientRequestId`, `managerId`, URLs, `recordedBy`. Show human-facing IDs (`T-501`, an employee ID on request) only when useful. |
| **RJ-3** | **Never show field names** (`assigneeId`, `dueDate`, `wasOverwrite`) — use their meaning ("assigned to", "due", "this replaced an existing entry"). |
| **RJ-4** | **Never surface stack traces or error codes.** Map them to plain language (Day 5 [error-handling-notes §5](../../Day%205/docs/error-handling-notes.md#5-what-the-user-ultimately-sees)). |
| **RJ-5** | **Exception — only when explicitly asked.** If a technical user says "show me the raw response / the JSON / the task ID", provide it. The default is human; raw is opt-in. |

### Example

```
 tool result:
 { "taskId": "T-507", "title": "Review the function-calling loop",
   "assigneeId": "E1001", "status": "open", "dueDate": "2026-06-12",
   "createdAt": "2026-06-08T10:22:04Z", "notificationSent": true,
   "clientRequestId": "req_9f2a..." }

 GOOD:  "Created the task 'Review the function-calling loop' and assigned it to
         Priya — it's open, due Friday 12 Jun, and she's been notified."

 BAD:   "Here's the result: { \"taskId\": \"T-507\", \"assigneeId\": \"E1001\", ... }"
                                                              ← RJ-1, RJ-2, RJ-3
```

(If the user then says "what's the task ID?" → "It's T-507." — that's RJ-5.)

---

## 5. Formatting Tool Output by Shape

Pick the format from the data's shape:

| Output shape | Best format | Notes |
|---|---|---|
| Single value (a balance, a count) | One sentence, lead with it | "You have 6 casual days left." |
| Single record (a profile, a task) | Short labelled lines or a sentence | Humanize fields; drop internal ones |
| Small list (≤ ~7 items) | Bullets | Name + the one or two fields that matter |
| Large list | Count + top items + offer to filter | "12 pending requests — here are the 5 oldest. Filter by team?" |
| Before/after (a correction) | "currently X → will be Y" | Used in write confirmations |
| Empty | "None found" + next step | [§3](#3-empty-result-handling) |
| Error | Plain-language cause + retry/alternative | [§4](#4-raw-json-avoidance) RJ-4 |

---

## 6. Quick Do / Don't Reference

| Do | Don't |
|---|---|
| Lead with the answer | Bury it after detail |
| "Priya Sharma" | "E1001" (unless needed) |
| "due Friday 12 Jun" | "dueDate: 2026-06-12" |
| "No tasks assigned right now." | Invent a task to fill the silence |
| "I couldn't reach the task system — try again shortly." | "Error: 500" / a stack trace |
| Bullets / small tables | A pasted JSON blob |
| Offer a next step | Dead-end the conversation |
| Give raw JSON **only when asked** | Dump objects by default |

These rules feed the **§8 Output Style** block of [system-prompt-v3.md](system-prompt-v3.md) and are tested in [test-results.md §5](test-results.md#5-response-quality-tests).

---

> Related docs: [system-prompt-v3.md](system-prompt-v3.md) · [confirmation-flow.md](confirmation-flow.md) · [Day 4 — tool-safety-rules.md §7](../../Day%204/docs/tool-safety-rules.md#7-output-handling-rules) · [Day 5 — error-handling-notes.md](../../Day%205/docs/error-handling-notes.md)
