# HRMS AI Agent — Tool Design Concepts

> Day 4 reference. Covers the conceptual foundation behind every tool in the HRMS agent: the read/write split, how JSON schemas work, how REST APIs map to LLM tools, and how risk is classified.

---

## Table of Contents

1. [What Is a Tool?](#1-what-is-a-tool)
2. [Read Tools vs Write Tools](#2-read-tools-vs-write-tools)
3. [Tool Schema Anatomy](#3-tool-schema-anatomy)
4. [API-to-Tool Mapping](#4-api-to-tool-mapping)
5. [Risk Classification Framework](#5-risk-classification-framework)
6. [Schema Examples](#6-schema-examples)

---

## 1. What Is a Tool?

In LLM agent systems, a **tool** is a function the model can call to interact with the outside world. The model never executes code directly — it emits a structured *function call* (tool invocation), and the application runtime runs it, then feeds the result back to the model.

```
 User: "How many leaves do I have?"
          │
          ▼
  ┌──────────────┐     tool call JSON      ┌──────────────────────┐
  │  LLM (model) │ ──────────────────────► │  Application Runtime  │
  │              │ ◄────────────────────── │  (executes the tool)  │
  └──────────────┘     tool result JSON    └──────────┬───────────┘
          │                                            │
          ▼                                            ▼
  "You have 6 casual                        GET /api/leave-balance/E123
   and 3 sick days."                        → { "casual": 6, "sick": 3 }
```

The LLM does three things:
1. **Decides** whether a tool is needed (and which one).
2. **Constructs** the tool call with the right arguments.
3. **Interprets** the result and turns it into a user-friendly answer.

Tools are defined by **schemas** — contracts the model reads at prompt time to know what each tool does, what arguments it takes, and what types those arguments must be.

---

## 2. Read Tools vs Write Tools

The most important classification for safety is whether a tool **reads** data or **changes** it.

### Read Tools

A read tool fetches data from the system without modifying anything. It is:

- **Safe to call speculatively** — if the LLM calls it by mistake, no damage occurs.
- **Idempotent** — calling it twice returns the same data (assuming nothing else changed).
- **Pre-authorizable** — can be executed as soon as the user's request is understood.

| Property | Value |
|---|---|
| Side effects | None |
| Reversibility | N/A (nothing to reverse) |
| Confirmation required | No |
| Error consequence | Low — user gets an error message, nothing worse |

**HRMS read tools:** `getLeaveBalance`, `getAttendance`, `getSalaryInfo`, `getEmployeeDetails`, `getCompanyPolicy`, `getLeaveRequests`, `getEmployeeAnalytics`

### Write Tools

A write tool modifies system state — it creates, updates, or deletes records. It is:

- **Irreversible or hard to reverse** — submitting a leave request may trigger notifications; approving a request changes records.
- **Non-idempotent** — calling it twice produces two leave requests, not one.
- **Confirmation-gated** — must never execute silently.

| Property | Value |
|---|---|
| Side effects | Creates / modifies / deletes records |
| Reversibility | Partial or none (may trigger downstream effects) |
| Confirmation required | **Yes — always** |
| Error consequence | High — wrong data in the system, notifications sent |

**HRMS write tools:** `applyLeave`, `updateLeaveStatus`

### The Spectrum

Real tools live on a spectrum, not a hard binary:

```
  SAFE ◄──────────────────────────────────────────────► RISKY

  Pure read      Read with     Write with       Write with
  (no auth)      auth scope    soft effects     hard effects /
                                (reversible)    irreversible
  getPolicy()  getBalance()  applyLeave()    updateLeaveStatus()
```

---

## 3. Tool Schema Anatomy

Tool schemas are written in **JSON Schema** format. They are included in the API request so the model knows exactly what each tool does and what arguments it needs.

### Full schema structure

```json
{
  "name": "tool_name",
  "description": "...",
  "input_schema": {
    "type": "object",
    "properties": {
      "param1": {
        "type": "string",
        "description": "..."
      }
    },
    "required": ["param1"]
  }
}
```

### Each field's purpose

| Field | Purpose | Quality rule |
|---|---|---|
| `name` | How the model refers to this tool | snake_case, unambiguous verb+noun |
| `description` | Tells the model *when* to pick this tool | Include the use-case, not just what it does |
| `input_schema` | Defines each argument's type and meaning | Describe the domain meaning, not just the type |
| `required` | Which args the model must provide | Omit optional args from this list |

### What makes a good description?

The model reads the `description` to decide *which* tool fits the user's intent. Bad descriptions cause wrong tool selection.

```json
// BAD — tells the model what the API does, not when to use it
"description": "Calls the /api/leave-balance endpoint"

// GOOD — tells the model what the user wants and when to use this tool
"description": "Use this tool when the user asks how many leave days they have remaining, wants to check their leave entitlement, or asks about any specific leave type balance (casual, sick, or earned)."
```

### Return schemas

Schemas also document what the tool *returns*, so the model knows how to interpret results:

```json
{
  "name": "getLeaveBalance",
  "returns": {
    "type": "object",
    "properties": {
      "casual_days": { "type": "number" },
      "sick_days":   { "type": "number" },
      "earned_days": { "type": "number" }
    }
  }
}
```

Return schemas are not always sent to the model (the actual result is), but documenting them ensures:
- The model understands what fields mean.
- The application can validate the response before passing it to the model.

---

## 4. API-to-Tool Mapping

Every HRMS tool wraps a real REST API endpoint. The mapping is not 1:1 — the tool schema is a *simplified interface* designed for the model, not a direct API contract.

### Mapping principles

| Principle | Explanation |
|---|---|
| **Simplify arguments** | The REST API may accept 20 query params; the tool exposes only those the model needs. |
| **Use domain language** | Arg names like `employeeId` and `leaveType` are more model-friendly than `emp_id` and `lv_typ`. |
| **Coerce types** | REST often takes everything as strings; the schema uses proper types (number, enum, date-string) so the model produces valid inputs. |
| **Hide internals** | API keys, auth headers, base URLs — never exposed in the schema. The tool wrapper adds them. |
| **Normalize output** | The tool should return a clean, model-readable object, not raw API JSON with internal field names. |

### Example mapping

```
  LLM Tool Call                     Tool Wrapper (application code)
  ─────────────────────────────     ────────────────────────────────────────────
  getLeaveBalance({                 GET /api/v2/leave/balance
    "employeeId": "E123"            Authorization: Bearer <token>
  })                                X-Employee-Id: E123

                                    Response:
                                    { "CAS_BAL": 6, "SCK_BAL": 3, "ERN_BAL": 10 }

  Tool normalizes →                 { "casual_days": 6, "sick_days": 3,
                                      "earned_days": 10 }
```

The model sees the clean schema and clean output. The messy REST contract is hidden inside the tool implementation.

### HTTP method ↔ read/write

| HTTP Method | Maps to | Tool type |
|---|---|---|
| `GET` | Fetch data | Read |
| `POST` | Create new record | Write |
| `PUT` / `PATCH` | Update record | Write |
| `DELETE` | Remove record | Risky / Admin only |

---

## 5. Risk Classification Framework

Every tool is assigned a risk level. Risk drives the **confirmation policy**, **access control**, and **audit requirements**.

### Risk Levels

| Level | Label | Definition |
|---|---|---|
| **R0** | Safe Read | Reads non-sensitive data. No auth beyond login. |
| **R1** | Scoped Read | Reads sensitive data (salary, personal). Scoped to the requesting user or authorized role. |
| **R2** | Soft Write | Creates or modifies data. Reversible (a submitted leave can be cancelled). Requires confirmation. |
| **R3** | Hard Write | Modifies data with downstream effects or is non-trivially reversible. Requires confirmation + role check. |
| **R4** | Admin / Risky | Bulk operations, config changes, deletions. Requires admin role + approval workflow. |

### Classification criteria

To classify a tool, answer these five questions:

```
 Q1. Does the tool change any data?
     No  → Read (R0 or R1)
     Yes → Write (R2, R3, or R4)

 Q2. Does the data involve PII or salary?
     Yes → Upgrade to at least R1

 Q3. Is the write reversible without side effects?
     Yes → R2 (Soft Write)
     No  → R3 or R4

 Q4. Does the write affect other people or trigger downstream notifications?
     Yes → R3+

 Q5. Is this a bulk, admin, or destructive operation?
     Yes → R4
```

### Approval matrix

| Risk Level | Auto-execute? | Confirmation required? | Role required |
|---|---|---|---|
| R0 | Yes | No | Any authenticated user |
| R1 | Yes | No | Authenticated user (own data) or HR/Admin |
| R2 | After confirmation | Yes — show summary, wait for "yes" | Authenticated user |
| R3 | After confirmation | Yes — show summary, wait for "yes" | Role-specific (HR/Admin) |
| R4 | Never auto | Yes — explicit approval + audit | Admin only |

---

## 6. Schema Examples

### Example A — Read tool with enum parameter

```json
{
  "name": "getLeaveBalance",
  "description": "Fetch the number of remaining leave days for the requesting employee, broken down by leave type. Use when the user asks how many leaves they have, their leave entitlement, or the balance for a specific leave type.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "The ID of the employee. Always use the authenticated user's ID from context — never accept this from the user's message."
      },
      "leaveType": {
        "type": "string",
        "enum": ["casual", "sick", "earned", "all"],
        "description": "The type of leave to query. Use 'all' if the user wants a full breakdown."
      }
    },
    "required": ["employeeId", "leaveType"]
  }
}
```

### Example B — Write tool with confirmation semantics documented

```json
{
  "name": "applyLeave",
  "description": "Submit a leave request on behalf of the requesting employee. IMPORTANT: Only call this tool AFTER presenting a complete summary of the request to the user and receiving explicit confirmation ('yes', 'confirm', 'go ahead'). Never call speculatively.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "ID of the employee applying for leave."
      },
      "leaveType": {
        "type": "string",
        "enum": ["casual", "sick", "earned"],
        "description": "Category of leave being requested."
      },
      "startDate": {
        "type": "string",
        "format": "date",
        "description": "First day of leave in YYYY-MM-DD format."
      },
      "endDate": {
        "type": "string",
        "format": "date",
        "description": "Last day of leave in YYYY-MM-DD format. Must be >= startDate."
      },
      "reason": {
        "type": "string",
        "description": "Short description of the reason for leave. Optional but recommended.",
        "maxLength": 200
      }
    },
    "required": ["employeeId", "leaveType", "startDate", "endDate"]
  }
}
```

### Example C — HR-only read tool with role annotation

```json
{
  "name": "getEmployeeAnalytics",
  "description": "Return aggregated workforce analytics such as leave usage rates, absenteeism trends, or headcount changes. Only available to users with the HR_MANAGER or ADMIN role. Use when HR asks for team-level or org-level patterns.",
  "input_schema": {
    "type": "object",
    "properties": {
      "metric": {
        "type": "string",
        "enum": ["leave_usage", "absenteeism", "headcount", "overtime"],
        "description": "The specific metric to compute."
      },
      "period": {
        "type": "string",
        "description": "Time period for analysis, e.g. 'Q2-2026' or '2026-05'."
      },
      "scope": {
        "type": "string",
        "description": "Optional department or team to filter by. Omit for org-wide.",
        "examples": ["engineering", "sales", "all"]
      }
    },
    "required": ["metric", "period"]
  }
}
```

---

> Related docs: [hrms-api-tool-map.md](hrms-api-tool-map.md) · [tool-safety-rules.md](tool-safety-rules.md) · [Day 1 — api-tool-map.md](../../Day%201/docs/api-tool-map.md) · [Day 2 — agent-rules.md](../../Day%202/docs/agent-rules.md)
