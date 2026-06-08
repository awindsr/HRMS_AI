# HRMS AI Agent — Write Tools Design (Day 6)

> Day 6 deliverable. The four state-changing tools introduced today — `createTask`, `assignTask`, `markAttendance`, `deleteTask` — with full input/output schemas, risk classification, and the new problems writes bring (idempotency, side effects, confirmation). Builds on the read/write framing from [Day 4 tool-design.md](../../Day%204/docs/tool-design.md).

---

## Table of Contents

1. [Read vs Write — Recap](#1-read-vs-write--recap)
2. [The Three New Problems Writes Bring](#2-the-three-new-problems-writes-bring)
3. [Day 6 Write-Tool Inventory](#3-day-6-write-tool-inventory)
4. [`createTask` — R2 Soft Write](#4-createtask--r2-soft-write)
5. [`assignTask` — R3 Hard Write](#5-assigntask--r3-hard-write)
6. [`markAttendance` — R2 / R3](#6-markattendance--r2--r3)
7. [`deleteTask` — R4 Destructive](#7-deletetask--r4-destructive)
8. [Schema Index](#8-schema-index)

---

## 1. Read vs Write — Recap

Day 5's three tools (`getEmployeeList`, `getEmployeeDetails`, `getTaskList`) were all **reads**: safe to call speculatively, idempotent, no confirmation. Day 6's tools **change state**. The contrast (from [Day 4 §2](../../Day%204/docs/tool-design.md#2-read-tools-vs-write-tools)):

| Property | Read tool (Day 5) | Write tool (Day 6) |
|---|---|---|
| Side effects | None | Creates / modifies / deletes records |
| Safe to call speculatively? | Yes | **No** — a stray call mutates real data |
| Idempotent? | Yes | **No** — two calls = two tasks, or a double-marked day |
| Confirmation required? | No | **Yes — always** (R2 and above) |
| Worst-case error | User sees an error message | Wrong data committed, someone notified, a record gone |

The single most important rule carried into Day 6:

> **A write tool must never fire as part of *understanding* the request. It fires only after the user has seen a plain-language summary and explicitly confirmed.** See [confirmation-flow.md](confirmation-flow.md).

---

## 2. The Three New Problems Writes Bring

### 2.1 Idempotency

Reads can be retried freely. Writes cannot. If the model calls `createTask` twice (a retry, a re-render, an ambiguous "yes"), the user gets **two** tasks. Defenses:

- **Confirmation gate** — one confirmation authorizes exactly one execution (Day 4 rule **CF-5**).
- **Client request ID** — the tool wrapper attaches a `clientRequestId`; the API treats a repeat as the same operation and returns the original result instead of creating a duplicate.
- **No auto-retry on writes** — unlike reads, a timed-out write is **not** silently retried (it may have succeeded). It is surfaced as `uncertain` and the user is asked to verify. See [§2.3](#23-side-effects--notifications).

### 2.2 Reversibility spectrum

Not all writes are equal. Risk tracks how hard the action is to undo:

```
  REVERSIBLE ◄──────────────────────────────────────────► IRREVERSIBLE

  createTask          markAttendance        assignTask          deleteTask
  (delete to undo)    (re-mark to fix,      (notifies people;   (record gone;
   R2                  feeds payroll)        re-assign to undo)   no undo)
                       R2 / R3               R3                   R4
```

### 2.3 Side effects & notifications

A write often does more than change a row. `assignTask` emails the new assignee; `markAttendance` feeds payroll. These downstream effects are why a tool can't just be "undone" by reversing the database write — the email is already sent. The confirmation summary must name the side effect (Day 4 **CF-1**): *"Priya will be notified."*

---

## 3. Day 6 Write-Tool Inventory

| # | Tool | Type | REST | Risk | Role | Confirmation |
|---|---|---|---|---|---|---|
| 1 | `createTask` | Create | `POST /api/v1/tasks` | R2 | EMPLOYEE+ (self) / HR for others | **Yes** |
| 2 | `assignTask` | Update | `PATCH /api/v1/tasks/{taskId}/assignment` | R3 | HR_MANAGER / lead | **Yes** |
| 3 | `markAttendance` | Create | `POST /api/v1/attendance` | R2 self / R3 others | EMPLOYEE (self, same-day) / HR (any, backdated) | **Yes** |
| 4 | `deleteTask` | Delete | `DELETE /api/v1/tasks/{taskId}` | R4 | HR_MANAGER / ADMIN | **Yes + double-confirm** |

Risk levels follow [Day 4 §5](../../Day%204/docs/tool-design.md#5-risk-classification-framework): R2 = soft write · R3 = hard write (downstream effects) · R4 = admin/destructive.

> These reuse the task model seeded in Day 5: `taskId` (e.g. `T-501`), `title`, `assigneeId` (e.g. `E1001`), `status`, `priority`, `dueDate`.

---

## 4. `createTask` — R2 Soft Write

**Purpose:** Create a new work task, optionally assigned to an employee.

**Risk:** R2 — creates a record. Reversible via `deleteTask`. If an `assigneeId` is supplied, a notification is sent (a real side effect), but the task itself can be removed.

**Confirmation required: Yes** — summarize title, assignee, priority, due date before calling.

**REST API:**
```
POST /api/v1/tasks
Body: { title, description, assigneeId, priority, dueDate, clientRequestId }
```

### Input Schema

```json
{
  "name": "createTask",
  "description": "Create a new work task in the HRMS task tracker. IMPORTANT: This is a write operation. Present a complete summary (title, who it is assigned to, priority, due date) and obtain explicit user confirmation before calling. Never call it speculatively or just to acknowledge a request.",
  "input_schema": {
    "type": "object",
    "properties": {
      "title": {
        "type": "string",
        "description": "Short, action-oriented task title.",
        "maxLength": 120
      },
      "description": {
        "type": "string",
        "description": "Optional longer detail about what the task involves.",
        "maxLength": 1000
      },
      "assigneeId": {
        "type": "string",
        "description": "Employee ID to assign the task to. Omit to leave the task unassigned. Assigning to someone other than the requester requires a lead/HR role.",
        "examples": ["E1001", "E1002"]
      },
      "priority": {
        "type": "string",
        "enum": ["low", "medium", "high"],
        "description": "Task priority.",
        "default": "medium"
      },
      "dueDate": {
        "type": "string",
        "format": "date",
        "description": "Due date in YYYY-MM-DD format. Must be today or later. Resolve relative dates ('next Friday') to absolute before calling."
      }
    },
    "required": ["title"]
  }
}
```

### Output Schema

```json
{
  "type": "object",
  "properties": {
    "taskId":     { "type": "string", "description": "ID of the created task, e.g. T-507." },
    "title":      { "type": "string" },
    "assigneeId": { "type": "string" },
    "status":     { "type": "string", "enum": ["open"], "description": "New tasks start as 'open'." },
    "priority":   { "type": "string" },
    "dueDate":    { "type": "string", "format": "date" },
    "createdAt":  { "type": "string", "format": "date-time" },
    "notificationSent": { "type": "boolean", "description": "True if an assignee was notified." }
  }
}
```

### Confirmation Flow

```
 1. User: "Create a high-priority task for Priya to review the function-calling loop, due Friday"
 2. Agent: "To confirm — I'll create this task:
             - Title:    Review the function-calling loop
             - Assignee: Priya Sharma (E1001)
             - Priority: High
             - Due:      Fri 12 Jun 2026
             Priya will be notified. Create it?"
 3. User: "Yes"
 4. Agent: calls createTask(...)
 5. Agent: "Done — created T-507 and assigned it to Priya. She's been notified."
```

---

## 5. `assignTask` — R3 Hard Write

**Purpose:** Assign or re-assign an existing task to an employee.

**Risk:** R3 — changes ownership of an existing record and notifies people. Re-assignment notifies both the previous and the new assignee. Reversible only by a second `assignTask`, after the notifications have already gone out — hence hard, not soft.

**Confirmation required: Yes** — show the task, the current assignee, and the new assignee.

**REST API:**
```
PATCH /api/v1/tasks/{taskId}/assignment
Body: { assigneeId, clientRequestId }
```

### Input Schema

```json
{
  "name": "assignTask",
  "description": "Assign or re-assign an existing task to an employee. This notifies the new assignee (and the previous one, on re-assignment) and changes who owns the work. IMPORTANT: This is a write operation. Before calling, show the task, its current assignee, and the new assignee, then wait for explicit confirmation. Requires a lead or HR_MANAGER role to assign work to other employees.",
  "input_schema": {
    "type": "object",
    "properties": {
      "taskId": {
        "type": "string",
        "description": "ID of the task to assign, e.g. T-501.",
        "examples": ["T-501", "T-504"]
      },
      "assigneeId": {
        "type": "string",
        "description": "Employee ID of the new assignee.",
        "examples": ["E1001", "E1002"]
      }
    },
    "required": ["taskId", "assigneeId"]
  }
}
```

### Output Schema

```json
{
  "type": "object",
  "properties": {
    "taskId":             { "type": "string" },
    "title":              { "type": "string" },
    "previousAssigneeId": { "type": "string", "description": "Null if the task was unassigned." },
    "newAssigneeId":      { "type": "string" },
    "updatedAt":          { "type": "string", "format": "date-time" },
    "notificationSent":   { "type": "boolean" }
  }
}
```

### Confirmation Flow

```
 1. HR: "Reassign T-504 to Arjun"
 2. Agent: fetches the task via getTaskList / task lookup
 3. Agent: "Confirm — reassigning:
             - Task:     T-504 'Fix attendance import' (currently blocked)
             - From:     Priya Sharma (E1001)
             - To:       Arjun Mehta (E1002)
             Both will be notified. Proceed?"
 4. HR: "Yes"
 5. Agent: calls assignTask({ taskId: "T-504", assigneeId: "E1002" })
 6. Agent: "Done — T-504 is now Arjun's. Priya and Arjun have been notified."
```

---

## 6. `markAttendance` — R2 / R3

**Purpose:** Record an attendance entry for an employee on a date (present / absent / WFH / leave, with optional check-in/out times).

**Risk: context-dependent.**
- **R2** when an employee marks their **own** attendance for **today** (a routine check-in).
- **R3** when HR marks/corrects **someone else's** record, or **backdates** an entry — because attendance feeds payroll and leave accounting, and a backdated change can alter pay.

See [attendance-tools.md](attendance-tools.md) for the full treatment, business rules, and the read/write pairing with `getAttendance`.

**Confirmation required: Yes** (both R2 and R3).

**REST API:**
```
POST /api/v1/attendance
Body: { employeeId, date, status, checkIn, checkOut, note, clientRequestId }
```

### Input Schema (summary — full version in attendance-tools.md)

```json
{
  "name": "markAttendance",
  "description": "Record an attendance entry for an employee for a specific date. IMPORTANT: This is a write operation that can affect payroll. Confirm the employee, date, and status before calling. Employees may only mark their own attendance for the current day; backdated or other-employee entries require HR_MANAGER role.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": { "type": "string", "description": "Employee whose attendance is recorded. From session for self-marking." },
      "date":       { "type": "string", "format": "date", "description": "Date of the entry (YYYY-MM-DD). Defaults to today." },
      "status":     { "type": "string", "enum": ["present", "absent", "wfh", "leave", "half_day"], "description": "Attendance status for the day." },
      "checkIn":    { "type": "string", "format": "time", "description": "Optional check-in time HH:MM (for present/wfh/half_day)." },
      "checkOut":   { "type": "string", "format": "time", "description": "Optional check-out time HH:MM." },
      "note":       { "type": "string", "description": "Optional reason/regularization note.", "maxLength": 200 }
    },
    "required": ["employeeId", "date", "status"]
  }
}
```

---

## 7. `deleteTask` — R4 Destructive

**Purpose:** Permanently remove a task from the tracker.

**Risk:** R4 — destructive and the highest risk in the system. Deletion removes a record; in a hard-delete model there is **no undo**. This is the canonical *delete risk* case — see [delete-risk-notes.md](delete-risk-notes.md) for the full guardrail set.

**Confirmation required: Yes + double-confirm** — an R4 action requires a second, explicit confirmation that names the exact record (Day 4 approval matrix: R4 = *never auto-execute, explicit approval + audit*).

**REST API:**
```
DELETE /api/v1/tasks/{taskId}
```

### Input Schema

```json
{
  "name": "deleteTask",
  "description": "Permanently delete a task. DESTRUCTIVE and IRREVERSIBLE. IMPORTANT: Requires HR_MANAGER or ADMIN role. You MUST (1) fetch and display the task's details, (2) state clearly that deletion cannot be undone, and (3) obtain an explicit second confirmation that names the task ID before calling. Never delete in bulk in a single call.",
  "input_schema": {
    "type": "object",
    "properties": {
      "taskId": {
        "type": "string",
        "description": "ID of the single task to delete, e.g. T-501.",
        "examples": ["T-501"]
      },
      "confirmationToken": {
        "type": "string",
        "description": "Echo of the task ID the user re-typed to confirm the deletion. Must equal taskId, proving an explicit second confirmation."
      }
    },
    "required": ["taskId", "confirmationToken"]
  }
}
```

### Output Schema

```json
{
  "type": "object",
  "properties": {
    "taskId":    { "type": "string" },
    "deleted":   { "type": "boolean" },
    "deletedAt": { "type": "string", "format": "date-time" },
    "deletedBy": { "type": "string", "description": "ID of the user who performed the deletion (for audit)." }
  }
}
```

### Confirmation Flow

```
 1. HR: "Delete task T-501"
 2. Agent: fetches T-501 details
 3. Agent: "⚠️ This will PERMANENTLY delete:
             - Task:     T-501 'Onboard new hire'
             - Assignee: Priya Sharma (E1001)
             - Status:   in progress
             This cannot be undone. To confirm, reply with the task ID 'T-501'."
 4. HR: "T-501"
 5. Agent: calls deleteTask({ taskId: "T-501", confirmationToken: "T-501" })
 6. Agent: "Deleted T-501. This action was logged."
```

---

## 8. Schema Index

| Tool | Type | Required input | Optional input | Returns | Risk | Confirm |
|---|---|---|---|---|---|---|
| `createTask` | Create | `title` | `description`, `assigneeId`, `priority`, `dueDate` | `taskId`, `status` | R2 | Yes |
| `assignTask` | Update | `taskId`, `assigneeId` | — | `previousAssigneeId`, `newAssigneeId`, `notificationSent` | R3 | Yes |
| `markAttendance` | Create | `employeeId`, `date`, `status` | `checkIn`, `checkOut`, `note` | `attendanceId`, `status`, `recordedBy` | R2/R3 | Yes |
| `deleteTask` | Delete | `taskId`, `confirmationToken` | — | `deleted`, `deletedAt`, `deletedBy` | R4 | Yes + double |

---

> Related docs: [confirmation-flow.md](confirmation-flow.md) · [attendance-tools.md](attendance-tools.md) · [delete-risk-notes.md](delete-risk-notes.md) · [Day 4 — tool-design.md](../../Day%204/docs/tool-design.md) · [Day 4 — tool-safety-rules.md](../../Day%204/docs/tool-safety-rules.md)
