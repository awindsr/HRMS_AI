`# HRMS AI Agent — Attendance Tools (Day 6)

> Day 6 deliverable. The attendance write tool `markAttendance` in full — self check-in vs HR regularization, why attendance is payroll-grade data, the schema, business rules, and how it pairs with the read tool `getAttendance` designed in [Day 4](../../Day%204/docs/hrms-api-tool-map.md#22-getattendance).

---

## Table of Contents

1. [The Attendance Read/Write Pair](#1-the-attendance-readwrite-pair)
2. [Why Attendance Is Payroll-Grade Data](#2-why-attendance-is-payroll-grade-data)
3. [Two Modes: Self Check-In vs HR Regularization](#3-two-modes-self-check-in-vs-hr-regularization)
4. [`markAttendance` — Full Schema](#4-markattendance--full-schema)
5. [Business Rules](#5-business-rules)
6. [Confirmation Examples](#6-confirmation-examples)
7. [Empty & Error Cases](#7-empty--error-cases)

---

## 1. The Attendance Read/Write Pair

Attendance has a clean read/write split. Day 4 designed the read; Day 6 adds the write:

| Tool | Type | Direction | Risk | Confirmation |
|---|---|---|---|---|
| `getAttendance` | Read | Fetch records for a date range | R1 | No |
| `markAttendance` | **Write** | Record an entry for a date | R2 (self) / R3 (others) | **Yes** |

A common pattern is **read-before-write**: when correcting a day, first `getAttendance` to show the current record, then `markAttendance` to change it — so the confirmation summary can show *before → after*.

```
 "I forgot to check in yesterday"
        │
        ▼
 getAttendance(E1002, 2026-06-07)  ──►  { status: "absent" }   (current)
        │
        ▼
 summarize: "Yesterday is currently 'absent'. Mark it 'present'?"
        │  user: yes
        ▼
 markAttendance(E1002, 2026-06-07, present)  ──►  updated
```

---

## 2. Why Attendance Is Payroll-Grade Data

Attendance is not a casual log — it flows into pay and leave accounting:

- **Days present/absent** can drive salary deductions and loss-of-pay calculations.
- **Leave** entries decrement leave balances.
- **Backdated changes** alter periods that may already be processed or close to payroll cut-off.

This is why `markAttendance` is **R3 (hard write)** the moment it touches someone else's record or a past date — the downstream effect is the same class as approving leave (`updateLeaveStatus`, R3 in [Day 4](../../Day%204/docs/tool-safety-rules.md#3-tool-risk-register)). A wrong mark isn't just a bad row; it can be a wrong paycheck.

---

## 3. Two Modes: Self Check-In vs HR Regularization

`markAttendance` serves two distinct use cases at two different risk levels:

| | Self check-in | HR regularization |
|---|---|---|
| **Who** | The authenticated employee | HR_MANAGER correcting any employee |
| **Whose record** | Own only | Any employee |
| **Date** | Today only | Any date (often backdated) |
| **Typical status** | `present`, `wfh` | `present`, `absent`, `leave`, `half_day` (correction) |
| **Risk** | **R2** | **R3** |
| **Extra controls** | Confirmation | Confirmation + role check + audit + payroll-impact note |

The mode is determined by **(employeeId == session user) AND (date == today)**. Anything else escalates to the HR path, which the binding layer enforces via the role check — the model cannot self-elevate (Day 4 **AC-2**).

---

## 4. `markAttendance` — Full Schema

**REST API:**
```
POST /api/v1/attendance
Body: { employeeId, date, status, checkIn, checkOut, note, clientRequestId }
```

### Input Schema

```json
{
  "name": "markAttendance",
  "description": "Record an attendance entry for an employee for a specific date. IMPORTANT: This is a write operation that can affect payroll and leave balances. Always confirm the employee, date, and status before calling. Employees may mark ONLY their own attendance for the CURRENT day; marking another employee or any past date requires HR_MANAGER role. For corrections, read the existing record first so the user can see the before/after.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "Employee whose attendance is being recorded. For self check-in, the authenticated user's own ID from session — never from the message.",
        "examples": ["E1001", "E1002"]
      },
      "date": {
        "type": "string",
        "format": "date",
        "description": "Date of the attendance entry, YYYY-MM-DD. Defaults to today. A past date is a regularization and requires HR role.",
        "examples": ["2026-06-08"]
      },
      "status": {
        "type": "string",
        "enum": ["present", "absent", "wfh", "leave", "half_day"],
        "description": "Attendance status for the day."
      },
      "checkIn": {
        "type": "string",
        "description": "Check-in time HH:MM (24h). Relevant for present/wfh/half_day.",
        "pattern": "^([01]\\d|2[0-3]):[0-5]\\d$",
        "examples": ["09:15"]
      },
      "checkOut": {
        "type": "string",
        "description": "Check-out time HH:MM (24h). Must be after checkIn.",
        "pattern": "^([01]\\d|2[0-3]):[0-5]\\d$",
        "examples": ["18:30"]
      },
      "note": {
        "type": "string",
        "description": "Optional reason/regularization note (e.g. 'forgot to check in', 'client site').",
        "maxLength": 200
      }
    },
    "required": ["employeeId", "date", "status"]
  }
}
```

### Output Schema

```json
{
  "type": "object",
  "properties": {
    "attendanceId": { "type": "string", "description": "ID of the attendance record, e.g. ATT-20260608-E1002." },
    "employeeId":   { "type": "string" },
    "date":         { "type": "string", "format": "date" },
    "status":       { "type": "string" },
    "checkIn":      { "type": "string" },
    "checkOut":     { "type": "string" },
    "recordedBy":   { "type": "string", "description": "User ID who recorded the entry (self or HR), for audit." },
    "recordedAt":   { "type": "string", "format": "date-time" },
    "wasOverwrite": { "type": "boolean", "description": "True if this replaced an existing entry for the date." }
  }
}
```

---

## 5. Business Rules

| ID | Rule |
|---|---|
| **AT-1** | An employee may mark only their **own** attendance, only for **today**. Any other employee or date requires HR_MANAGER (enforced in code, not just prompt). |
| **AT-2** | `checkOut` must be after `checkIn`; reject otherwise (Day 4 **IV-3** analogue). |
| **AT-3** | Marking a date that already has an entry is an **overwrite** — the summary must show the existing value and `wasOverwrite` is returned true. |
| **AT-4** | `status: "leave"` should reconcile with the leave system; if a marked leave has no matching approved leave request, flag it rather than silently accepting. |
| **AT-5** | Backdated entries (`date < today`) are regularizations: HR role, mandatory `note`, and the confirmation must state the payroll impact. |
| **AT-6** | Future-dated attendance is rejected — you cannot be present on a day that hasn't happened. |
| **AT-7** | Times are recorded in the employee's work location timezone; the tool wrapper normalizes, the model never invents a timezone. |

---

## 6. Confirmation Examples

### Self check-in (R2)

```
 1. User (E1002): "Check me in, I started at 9:15"
 2. Agent: "To confirm — marking your attendance for today:
             - Date:   Mon 8 Jun 2026
             - Status: Present
             - Check-in: 09:15
             Save it?"
 3. User: "Yes"
 4. Agent: calls markAttendance({ employeeId: "E1002", date: "2026-06-08",
            status: "present", checkIn: "09:15" })
 5. Agent: "Done — you're checked in for today at 09:15."
```

### HR regularization, backdated (R3)

```
 1. HR: "Mark Priya present for last Friday, she was at the client site"
 2. Agent: reads getAttendance(E1001, 2026-06-05) → currently 'absent'
 3. Agent: "Confirm this regularization:
             - Employee: Priya Sharma (E1001)
             - Date:     Fri 5 Jun 2026 (currently 'absent')
             - New:      Present — note 'client site'
             This is a backdated change and may affect payroll. Proceed?"
 4. HR: "Yes"
 5. Agent: calls markAttendance({ employeeId: "E1001", date: "2026-06-05",
            status: "present", note: "client site" })
 6. Agent: "Done — Priya's 5 Jun record is now 'present'. The change was logged."
```

---

## 7. Empty & Error Cases

Consistent with [response-quality-guidelines.md](response-quality-guidelines.md) and the Day 5 error taxonomy:

| Case | Behavior |
|---|---|
| `getAttendance` returns no records for the range | "I don't see any attendance recorded for that period yet." — never invent days. See [response-quality-guidelines.md §3](response-quality-guidelines.md#3-empty-result-handling). |
| Future date supplied | Refuse per **AT-6**; explain why. |
| Employee marks someone else | Role check fails → "You can only mark your own attendance; an HR manager can update others." (Day 4 **AC-6**). |
| `markAttendance` times out | Don't auto-retry; verify with a fresh `getAttendance` before telling the user it saved. |

---

> Related docs: [write-tools-design.md §6](write-tools-design.md#6-markattendance--r2--r3) · [confirmation-flow.md](confirmation-flow.md) · [response-quality-guidelines.md](response-quality-guidelines.md) · [Day 4 — getAttendance](../../Day%204/docs/hrms-api-tool-map.md#22-getattendance)
