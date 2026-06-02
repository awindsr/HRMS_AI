# HRMS AI Agent — API & Tool Map

> The catalogue of **tools** (functions/APIs) the HRMS AI Agent can call, their input/output contracts, how the agent decides which tool to use, and security considerations.

A **tool** is a function exposed to the LLM. The model never runs code itself — it emits a structured request (`tool name` + `arguments`), the application executes it, and the result is returned to the model. See [agent-requirements.md](agent-requirements.md#7-ai-agent-workflow) for the full workflow.

---

## Table of Contents

1. [Tool Catalogue (summary table)](#1-tool-catalogue)
2. [Tool Reference](#2-tool-reference)
3. [How Agents Decide Which Tool to Use](#3-how-agents-decide-which-tool-to-use)
4. [Tool Execution Flow](#4-tool-execution-flow)
5. [Error Handling](#5-error-handling)
6. [API Security Considerations](#6-api-security-considerations)

---

## 1. Tool Catalogue

| Tool Name | Purpose | Input | Output | Example |
|---|---|---|---|---|
| `getEmployeeDetails()` | Fetch an employee profile | `employeeId` | Employee data (name, role, dept, manager) | `getEmployeeDetails("E123")` |
| `getAttendance()` | Retrieve attendance records | `employeeId`, `startDate`, `endDate` | List of attendance entries | `getAttendance("E123", "2026-05-01", "2026-05-31")` |
| `getLeaveBalance()` | Check available leaves | `employeeId` | Leave balances by type | `getLeaveBalance("E123")` |
| `applyLeave()` | Submit a leave request | `employeeId`, `type`, `startDate`, `endDate`, `reason` | Request ID + status | `applyLeave("E123", "casual", "2026-06-05", "2026-06-05", "Personal")` |
| `getCompanyPolicy()` | Retrieve HR policy information | `policyName` | Policy text | `getCompanyPolicy("work_from_home")` |
| `getSalaryInfo()` | Fetch salary / payslip details | `employeeId`, `month?` | Salary breakdown | `getSalaryInfo("E123", "2026-05")` |
| `getLeaveRequests()` *(HR)* | List leave requests | `status?`, `team?` | List of requests | `getLeaveRequests("pending", "engineering")` |
| `updateLeaveStatus()` *(HR)* | Approve/reject a request | `requestId`, `status` | Updated request | `updateLeaveStatus("LR-4581", "approved")` |
| `getEmployeeAnalytics()` *(HR)* | Surface workforce trends | `metric`, `period`, `scope?` | Aggregated analytics | `getEmployeeAnalytics("leave_usage", "Q2-2026")` |

> Tools marked *(HR)* require an `HR_MANAGER` or `ADMIN` role.

---

## 2. Tool Reference

### 2.1 `getEmployeeDetails()`

**Purpose:** Fetch an employee's profile.

| | |
|---|---|
| **Input** | `employeeId: string` |
| **Output** | Employee data object |
| **Access** | Employee (own only) · HR · Admin |

```json
// Input
{ "employeeId": "E123" }

// Output
{
  "employeeId": "E123",
  "name": "Asha Menon",
  "designation": "Software Engineer",
  "department": "Engineering",
  "manager": "Priya Sharma",
  "dateOfJoining": "2023-04-10"
}
```

---

### 2.2 `getAttendance()`

**Purpose:** Retrieve attendance records for a date range.

| | |
|---|---|
| **Input** | `employeeId: string`, `startDate: date`, `endDate: date` |
| **Output** | List of daily attendance entries |
| **Access** | Employee (own only) · HR · Admin |

```json
// Input
{ "employeeId": "E123", "startDate": "2026-05-01", "endDate": "2026-05-31" }

// Output
{
  "employeeId": "E123",
  "summary": { "present": 20, "absent": 1, "leave": 1, "lateCheckIns": 2 },
  "entries": [
    { "date": "2026-05-01", "status": "present", "checkIn": "09:05" },
    { "date": "2026-05-02", "status": "present", "checkIn": "09:45", "late": true }
  ]
}
```

---

### 2.3 `getLeaveBalance()`

**Purpose:** Check available leaves by type.

| | |
|---|---|
| **Input** | `employeeId: string` |
| **Output** | Remaining leave per category |
| **Access** | Employee (own only) · HR · Admin |

```json
// Input
{ "employeeId": "E123" }

// Output
{ "casual": 6, "sick": 3, "earned": 12 }
```

---

### 2.4 `applyLeave()`

**Purpose:** Submit a leave request. *(Data-changing — requires user confirmation first.)*

| | |
|---|---|
| **Input** | `employeeId`, `type`, `startDate`, `endDate`, `reason` |
| **Output** | Request ID + status |
| **Access** | Employee (own only) |

```json
// Input
{
  "employeeId": "E123",
  "type": "casual",
  "startDate": "2026-06-05",
  "endDate": "2026-06-05",
  "reason": "Personal"
}

// Output
{ "requestId": "LR-4602", "status": "pending", "approver": "Priya Sharma" }
```

---

### 2.5 `getCompanyPolicy()`

**Purpose:** Retrieve official HR policy text so the agent can answer policy questions grounded in source.

| | |
|---|---|
| **Input** | `policyName: string` (e.g. `"work_from_home"`, `"leave"`, `"resignation"`) |
| **Output** | Policy title + text |
| **Access** | All roles |

```json
// Input
{ "policyName": "work_from_home" }

// Output
{
  "policyName": "work_from_home",
  "title": "Work From Home Policy",
  "text": "Employees may work from home up to 2 days per week with manager approval..."
}
```

---

## 3. How Agents Decide Which Tool to Use

The LLM selects a tool by matching the **user's intent** against each tool's **name and description**. This is why clear, descriptive tool definitions matter as much as good prompts.

The decision process:

1. **Interpret intent** — what is the user actually asking for?
2. **Check if a tool is needed** — can this be answered from context, or is fresh data required?
3. **Match to a tool** — find the tool whose description best fits the intent.
4. **Check authorization** — does the user's role permit this tool?
5. **Extract / request arguments** — pull arguments from the conversation; ask the user for anything missing.
6. **Emit the tool call** — produce the structured function call.

| User says... | Intent | Tool chosen |
|---|---|---|
| "How many leaves do I have?" | Check leave balance | `getLeaveBalance()` |
| "Apply leave for Friday" | Create a leave request | `applyLeave()` |
| "Was I late last week?" | Inspect attendance | `getAttendance()` |
| "What's the WFH policy?" | Look up policy | `getCompanyPolicy()` |
| "Who's my manager?" | Profile lookup | `getEmployeeDetails()` |

> 💡 If no tool matches and the question is in scope, the agent answers conversationally. If the question is out of scope, it politely declines.

---

## 4. Tool Execution Flow

```
  ┌──────────────┐
  │ LLM emits    │   { "tool": "getLeaveBalance",
  │ tool call    │     "args": { "employeeId": "E123" } }
  └──────┬───────┘
         ▼
  ┌──────────────┐
  │ Validate     │   • Is the tool name known?
  │ request      │   • Are required args present & well-typed?
  └──────┬───────┘
         ▼
  ┌──────────────┐
  │ Authorize    │   • Does the user's role permit this tool?
  │              │   • Is the data scoped to the user (own data)?
  └──────┬───────┘
         ▼
  ┌──────────────┐
  │ Execute API  │   Call the underlying HRMS service (HTTPS)
  └──────┬───────┘
         ▼
  ┌──────────────┐
  │ Return       │   Pass the result (or error) back to the LLM
  │ result       │
  └──────┬───────┘
         ▼
  ┌──────────────┐
  │ LLM composes │   Human-readable answer for the user
  │ final answer │
  └──────────────┘
```

---

## 5. Error Handling

Tools fail — networks time out, records are missing, permissions are denied. The agent must handle this **gracefully and honestly** (never fabricate data to cover a failure).

| Error type | Cause | Agent behavior |
|---|---|---|
| **Validation error** | Missing/invalid arguments | Ask the user for the missing information. |
| **Not found** | No matching record | Tell the user no data was found; suggest a correction. |
| **Unauthorized** | Role/scope not permitted | Politely refuse and explain the access limit. |
| **Timeout / unavailable** | Backend service down or slow | Apologize, state the system is temporarily unavailable, suggest retrying. |
| **Unexpected error** | Anything else | Fail safe: report a generic error, log details for debugging, do **not** guess. |

**Example — graceful failure:**

```text
Tool result:
{ "error": "SERVICE_UNAVAILABLE" }

Response (to user):
"I'm having trouble reaching the leave system right now.
 Please try again in a few minutes."
```

**Principles**

- Every error is **logged** with context (tool, args, error code) for debugging — never with raw PII in plaintext.
- The agent **never invents** a result to mask a failure.
- Transient errors (timeouts) may be **retried** with backoff before surfacing to the user.

---

## 6. API Security Considerations

| Concern | Practice |
|---|---|
| **Authentication** | Every tool call carries the authenticated user's identity/token. The agent acts on behalf of that user only. |
| **Authorization (RBAC)** | Authorization is enforced **server-side** at the tool layer — never trusted to the LLM alone. The model's role check is a first line, not the only one. |
| **Data scoping** | Employee requests are scoped to their own `employeeId`. The backend rejects cross-employee access even if the model requests it. |
| **Transport security** | All API traffic uses HTTPS/TLS. No sensitive data over plaintext channels. |
| **Input validation** | Tool arguments are validated and sanitized to prevent injection or malformed requests. |
| **Least privilege** | Each tool exposes the minimum data and capability needed. Write tools (`applyLeave`, `updateLeaveStatus`) are tightly restricted. |
| **PII minimization** | Only necessary data is injected into prompts and logs; sensitive fields (salary, IDs) are masked or excluded from logs. |
| **Audit logging** | All actions — especially data-changing ones — are logged with actor, timestamp, and outcome for accountability. |
| **Confirmation for writes** | Data-changing tools require explicit user confirmation before execution (see [prompt-examples.md](prompt-examples.md#example-b--apply-leave-confirmation-before-action)). |

> ⚠️ **Golden rule:** Treat the LLM as **untrusted input**. Always re-validate authorization and arguments on the server side before any tool actually runs.

---

> Related docs: [README.md](../README.md) · [agent-requirements.md](agent-requirements.md) · [prompt-examples.md](prompt-examples.md)
