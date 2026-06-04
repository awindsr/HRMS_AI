# HRMS AI Agent — Full Tool Map with Schemas

> Day 4 deliverable. Complete input/output schemas for every HRMS tool, mapped to their REST API endpoints, with risk classification and access control annotations.

---

## Table of Contents

1. [Tool Inventory](#1-tool-inventory)
2. [Employee Tools](#2-employee-tools)
   - [getLeaveBalance](#21-getleavebalance)
   - [getAttendance](#22-getattendance)
   - [getSalaryInfo](#23-getsalaryinfo)
   - [getEmployeeDetails](#24-getemployeedetails)
   - [getCompanyPolicy](#25-getcompanypolicy)
   - [applyLeave](#26-applyleave)
3. [HR Tools](#3-hr-tools)
   - [getLeaveRequests](#31-getleaverequests)
   - [updateLeaveStatus](#32-updateleavestatus)
   - [getEmployeeAnalytics](#33-getemployeeanalytics)
4. [Schema Index](#4-schema-index)

---

## 1. Tool Inventory

| # | Tool | Type | Risk | Role Required | Confirmation |
|---|---|---|---|---|---|
| 1 | `getLeaveBalance` | Read | R1 | EMPLOYEE+ | No |
| 2 | `getAttendance` | Read | R1 | EMPLOYEE+ | No |
| 3 | `getSalaryInfo` | Read | R1 | EMPLOYEE (own) / HR+ | No |
| 4 | `getEmployeeDetails` | Read | R1 | EMPLOYEE (own) / HR+ | No |
| 5 | `getCompanyPolicy` | Read | R0 | Any authenticated | No |
| 6 | `applyLeave` | Write | R2 | EMPLOYEE | **Yes** |
| 7 | `getLeaveRequests` | Read | R1 | HR_MANAGER / ADMIN | No |
| 8 | `updateLeaveStatus` | Write | R3 | HR_MANAGER / ADMIN | **Yes** |
| 9 | `getEmployeeAnalytics` | Read | R1 | HR_MANAGER / ADMIN | No |

**Risk levels:** R0 = safe read · R1 = scoped read (sensitive data) · R2 = soft write · R3 = hard write

---

## 2. Employee Tools

### 2.1 `getLeaveBalance`

**Purpose:** Fetch how many leave days of each type an employee has remaining.

**Risk:** R1 — sensitive personal data, scoped to requesting employee.

**REST API:**
```
GET /api/v1/leave/balance/{employeeId}
```

#### Input Schema

```json
{
  "name": "getLeaveBalance",
  "description": "Fetch the number of remaining leave days for the requesting employee, broken down by leave type (casual, sick, earned). Use when the user asks how many leaves they have, their leave entitlement, or the balance for a specific type. Never call for another employee unless the requester has HR/Admin role.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "Employee ID from the authenticated session. Always taken from context — never from user input.",
        "examples": ["E123", "EMP-4501"]
      },
      "leaveType": {
        "type": "string",
        "enum": ["casual", "sick", "earned", "all"],
        "description": "Which leave type to query. Use 'all' when the user wants a full breakdown.",
        "default": "all"
      }
    },
    "required": ["employeeId", "leaveType"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "employeeId": { "type": "string" },
    "asOf": {
      "type": "string",
      "format": "date",
      "description": "Date the balance was calculated."
    },
    "balances": {
      "type": "object",
      "properties": {
        "casual": { "type": "number", "description": "Casual leave days remaining." },
        "sick":   { "type": "number", "description": "Sick leave days remaining." },
        "earned": { "type": "number", "description": "Earned/privilege leave days remaining." }
      }
    }
  }
}
```

#### Example

```
Input:  { "employeeId": "E123", "leaveType": "all" }
Output: {
  "employeeId": "E123",
  "asOf": "2026-06-04",
  "balances": { "casual": 6, "sick": 3, "earned": 10 }
}
```

---

### 2.2 `getAttendance`

**Purpose:** Retrieve an employee's attendance record for a date range — check-in/out times, absences, and late arrivals.

**Risk:** R1 — personal attendance data, scoped to requesting employee.

**REST API:**
```
GET /api/v1/attendance/{employeeId}?from={startDate}&to={endDate}
```

#### Input Schema

```json
{
  "name": "getAttendance",
  "description": "Retrieve attendance records (check-in/out times, present/absent status, late arrivals) for the requesting employee over a given date range. Use when the user asks about their attendance, punctuality, absences, or working days. HR users may query any employee.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "Employee ID from authenticated session."
      },
      "startDate": {
        "type": "string",
        "format": "date",
        "description": "Start of the date range in YYYY-MM-DD format."
      },
      "endDate": {
        "type": "string",
        "format": "date",
        "description": "End of the date range in YYYY-MM-DD format. Must be >= startDate. Defaults to today if omitted."
      }
    },
    "required": ["employeeId", "startDate"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "employeeId": { "type": "string" },
    "period": {
      "type": "object",
      "properties": {
        "from": { "type": "string", "format": "date" },
        "to":   { "type": "string", "format": "date" }
      }
    },
    "summary": {
      "type": "object",
      "properties": {
        "totalWorkingDays": { "type": "number" },
        "presentDays":      { "type": "number" },
        "absentDays":       { "type": "number" },
        "lateDays":         { "type": "number" }
      }
    },
    "records": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "date":     { "type": "string", "format": "date" },
          "status":   { "type": "string", "enum": ["present", "absent", "leave", "holiday"] },
          "checkIn":  { "type": "string", "format": "time", "description": "HH:MM:SS" },
          "checkOut": { "type": "string", "format": "time" },
          "late":     { "type": "boolean" }
        }
      }
    }
  }
}
```

---

### 2.3 `getSalaryInfo`

**Purpose:** Fetch payslip and salary breakdown for an employee.

**Risk:** R1 — highly sensitive financial PII. Scoped to the requesting employee; only HR/Admin can query others.

**REST API:**
```
GET /api/v1/payroll/{employeeId}/payslip?month={month}
```

#### Input Schema

```json
{
  "name": "getSalaryInfo",
  "description": "Fetch salary details and payslip breakdown (gross, deductions, net pay) for the requesting employee for a given month. Use when the user asks about their salary, pay, payslip, CTC, deductions, or take-home amount. This tool returns sensitive financial data — only serve the authenticated employee's own data unless the requester has HR/Admin role.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "Employee ID from authenticated session."
      },
      "month": {
        "type": "string",
        "description": "Month and year in YYYY-MM format. Omit to return the latest payslip.",
        "pattern": "^\\d{4}-(0[1-9]|1[0-2])$",
        "examples": ["2026-05", "2026-04"]
      }
    },
    "required": ["employeeId"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "employeeId":  { "type": "string" },
    "month":       { "type": "string" },
    "grossSalary": { "type": "number", "description": "Total earnings before deductions (INR)." },
    "deductions": {
      "type": "object",
      "properties": {
        "pf":           { "type": "number" },
        "tax":          { "type": "number" },
        "professionalTax": { "type": "number" },
        "other":        { "type": "number" }
      }
    },
    "netSalary": { "type": "number", "description": "Take-home pay after all deductions (INR)." },
    "payslipUrl": { "type": "string", "format": "uri", "description": "Link to the downloadable PDF payslip." }
  }
}
```

---

### 2.4 `getEmployeeDetails`

**Purpose:** Fetch an employee's profile — name, role, department, manager, contact.

**Risk:** R1 — personal data. Employees see their own profile; HR/Admin may query any employee.

**REST API:**
```
GET /api/v1/employees/{employeeId}
```

#### Input Schema

```json
{
  "name": "getEmployeeDetails",
  "description": "Fetch profile information for an employee: name, job title, department, manager, and contact details. Use when the user asks about their own profile, or when HR/Admin needs to look up any employee. Employees may only view their own profile.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "Employee ID to look up. For employees, always their own ID. For HR/Admin, can be any valid employee ID."
      }
    },
    "required": ["employeeId"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "employeeId":  { "type": "string" },
    "fullName":    { "type": "string" },
    "email":       { "type": "string", "format": "email" },
    "phone":       { "type": "string" },
    "jobTitle":    { "type": "string" },
    "department":  { "type": "string" },
    "location":    { "type": "string" },
    "managerId":   { "type": "string" },
    "managerName": { "type": "string" },
    "joinDate":    { "type": "string", "format": "date" },
    "status":      { "type": "string", "enum": ["active", "on_leave", "inactive"] }
  }
}
```

---

### 2.5 `getCompanyPolicy`

**Purpose:** Retrieve the text of an official company HR policy.

**Risk:** R0 — non-sensitive, non-personal. Policies are org-wide and not confidential.

**REST API:**
```
GET /api/v1/policies/{policySlug}
```

#### Input Schema

```json
{
  "name": "getCompanyPolicy",
  "description": "Retrieve the official text of a company HR policy. Use when the user asks about rules, entitlements, procedures, or company guidelines — for example, WFH policy, leave rules, holiday list, or expense policies. Always ground policy answers in the output of this tool, never invent or paraphrase from memory.",
  "input_schema": {
    "type": "object",
    "properties": {
      "policySlug": {
        "type": "string",
        "description": "Identifier of the policy to retrieve.",
        "enum": [
          "work_from_home",
          "leave_entitlement",
          "holiday_calendar",
          "expense_reimbursement",
          "code_of_conduct",
          "maternity_paternity_leave",
          "anti_harassment"
        ]
      }
    },
    "required": ["policySlug"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "policySlug":   { "type": "string" },
    "policyTitle":  { "type": "string" },
    "lastUpdated":  { "type": "string", "format": "date" },
    "content":      { "type": "string", "description": "Full policy text in Markdown." },
    "version":      { "type": "string" }
  }
}
```

---

### 2.6 `applyLeave`

**Purpose:** Submit a leave request on behalf of an employee.

**Risk:** R2 — soft write. Creates a record; triggers a notification to the manager. Reversible (request can be cancelled) but with side effects.

**Confirmation required: Yes** — must show full summary and receive explicit user agreement before calling.

**REST API:**
```
POST /api/v1/leave/requests
Body: { employeeId, leaveType, startDate, endDate, reason }
```

#### Input Schema

```json
{
  "name": "applyLeave",
  "description": "Submit a leave request for the requesting employee. IMPORTANT: This is a write operation. You MUST present a complete summary of the request (employee, type, dates, reason) and receive explicit confirmation from the user before calling this tool. Do not call it speculatively or as part of understanding the request.",
  "input_schema": {
    "type": "object",
    "properties": {
      "employeeId": {
        "type": "string",
        "description": "Employee ID from authenticated session. Always the requesting user's own ID."
      },
      "leaveType": {
        "type": "string",
        "enum": ["casual", "sick", "earned"],
        "description": "Category of leave. Must be confirmed with the user if ambiguous."
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
        "description": "Short reason for leave. Optional but helpful for the manager.",
        "maxLength": 200
      }
    },
    "required": ["employeeId", "leaveType", "startDate", "endDate"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "requestId":   { "type": "string", "description": "Unique ID for the leave request, e.g. LR-4581." },
    "status":      { "type": "string", "enum": ["pending", "submitted", "failed"] },
    "submittedAt": { "type": "string", "format": "date-time" },
    "leaveType":   { "type": "string" },
    "startDate":   { "type": "string", "format": "date" },
    "endDate":     { "type": "string", "format": "date" },
    "totalDays":   { "type": "number" },
    "message":     { "type": "string", "description": "Human-readable status message." }
  }
}
```

#### Confirmation Flow

```
 1. User: "Apply 2 days casual leave next Monday and Tuesday"
 2. Agent: "To confirm — I'll submit a CASUAL leave request for:
             - Dates: Mon 8 Jun 2026 – Tue 9 Jun 2026 (2 days)
             - Reason: not specified
             Shall I go ahead?"
 3. User: "Yes"
 4. Agent: calls applyLeave(...)
 5. Agent: "Done! Your request LR-4581 has been submitted and
             is pending manager approval."
```

---

## 3. HR Tools

> All tools in this section require `HR_MANAGER` or `ADMIN` role. Role is verified in code at the tool layer, not just in the prompt.

### 3.1 `getLeaveRequests`

**Purpose:** List leave requests with optional filters by status or team.

**Risk:** R1 — scoped sensitive data (multiple employees' leave records).

**REST API:**
```
GET /api/v1/leave/requests?status={status}&team={team}&page={page}
```

#### Input Schema

```json
{
  "name": "getLeaveRequests",
  "description": "List leave requests for a team or org. Supports filtering by approval status and team. Use when HR asks to review pending leave requests, see who is on leave, or manage approvals. Requires HR_MANAGER or ADMIN role.",
  "input_schema": {
    "type": "object",
    "properties": {
      "status": {
        "type": "string",
        "enum": ["pending", "approved", "rejected", "cancelled", "all"],
        "description": "Filter by approval status. Use 'all' for no filter.",
        "default": "pending"
      },
      "team": {
        "type": "string",
        "description": "Department or team name to filter by. Omit for org-wide results.",
        "examples": ["engineering", "sales", "operations"]
      },
      "fromDate": {
        "type": "string",
        "format": "date",
        "description": "Only return requests where the leave starts on or after this date."
      },
      "toDate": {
        "type": "string",
        "format": "date",
        "description": "Only return requests where the leave starts on or before this date."
      },
      "limit": {
        "type": "number",
        "description": "Maximum number of records to return. Default 20, max 100.",
        "default": 20,
        "maximum": 100
      }
    },
    "required": []
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "total":   { "type": "number" },
    "requests": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "requestId":    { "type": "string" },
          "employeeId":   { "type": "string" },
          "employeeName": { "type": "string" },
          "leaveType":    { "type": "string" },
          "startDate":    { "type": "string", "format": "date" },
          "endDate":      { "type": "string", "format": "date" },
          "totalDays":    { "type": "number" },
          "status":       { "type": "string", "enum": ["pending", "approved", "rejected", "cancelled"] },
          "reason":       { "type": "string" },
          "submittedAt":  { "type": "string", "format": "date-time" }
        }
      }
    }
  }
}
```

---

### 3.2 `updateLeaveStatus`

**Purpose:** Approve or reject a leave request.

**Risk:** R3 — hard write. Updates a record and triggers notifications to the employee. Non-trivially reversible (employee has been notified).

**Confirmation required: Yes** — must show request details and action, wait for explicit HR confirmation.

**REST API:**
```
PATCH /api/v1/leave/requests/{requestId}
Body: { status, comment }
```

#### Input Schema

```json
{
  "name": "updateLeaveStatus",
  "description": "Approve or reject a leave request by its ID. This action notifies the employee and updates the HR record — it is not easily undone. IMPORTANT: This is a write operation. Before calling, display the request details (employee name, dates, type) and the intended action (approve/reject), then wait for explicit HR confirmation. Requires HR_MANAGER or ADMIN role.",
  "input_schema": {
    "type": "object",
    "properties": {
      "requestId": {
        "type": "string",
        "description": "The leave request ID, e.g. LR-4581."
      },
      "status": {
        "type": "string",
        "enum": ["approved", "rejected"],
        "description": "The new status to set. Only 'approved' or 'rejected' are valid — use 'cancelled' flow for withdrawals."
      },
      "comment": {
        "type": "string",
        "description": "Optional message to include in the notification to the employee.",
        "maxLength": 300
      }
    },
    "required": ["requestId", "status"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "requestId":    { "type": "string" },
    "employeeId":   { "type": "string" },
    "employeeName": { "type": "string" },
    "previousStatus": { "type": "string" },
    "newStatus":    { "type": "string" },
    "updatedBy":    { "type": "string", "description": "HR user ID who made the change." },
    "updatedAt":    { "type": "string", "format": "date-time" },
    "notificationSent": { "type": "boolean" }
  }
}
```

#### Confirmation Flow

```
 1. HR: "Approve LR-4581"
 2. Agent: fetches request details via getLeaveRequests
 3. Agent: "Confirm — you're approving:
             - Request: LR-4581
             - Employee: Priya Sharma
             - Leave: 2 days CASUAL (8–9 Jun 2026)
             Priya will be notified. Approve?"
 4. HR: "Yes"
 5. Agent: calls updateLeaveStatus({ requestId: "LR-4581", status: "approved" })
 6. Agent: "Done — LR-4581 approved. Priya has been notified."
```

---

### 3.3 `getEmployeeAnalytics`

**Purpose:** Return aggregated workforce analytics and trend data.

**Risk:** R1 — aggregate data, not individual PII, but still restricted to HR/Admin to prevent reverse-engineering of individual records.

**REST API:**
```
GET /api/v1/analytics/workforce?metric={metric}&period={period}&scope={scope}
```

#### Input Schema

```json
{
  "name": "getEmployeeAnalytics",
  "description": "Return aggregated workforce analytics: leave usage rates, absenteeism trends, headcount changes, or overtime statistics. Use when HR/Admin asks for team-level or org-level patterns and summaries. Requires HR_MANAGER or ADMIN role. Do not use for individual employee lookups — use getAttendance or getLeaveBalance for that.",
  "input_schema": {
    "type": "object",
    "properties": {
      "metric": {
        "type": "string",
        "enum": ["leave_usage", "absenteeism", "headcount", "overtime", "leave_type_split"],
        "description": "The metric to compute."
      },
      "period": {
        "type": "string",
        "description": "Time period. Accepts quarter (Q2-2026), month (2026-05), or year (2026).",
        "examples": ["Q2-2026", "2026-05", "2026"]
      },
      "scope": {
        "type": "string",
        "description": "Optional department to scope the metric. Omit for org-wide.",
        "examples": ["engineering", "sales", "all"]
      },
      "groupBy": {
        "type": "string",
        "enum": ["department", "month", "leave_type"],
        "description": "Optional dimension to break down results by."
      }
    },
    "required": ["metric", "period"]
  }
}
```

#### Output Schema

```json
{
  "type": "object",
  "properties": {
    "metric":  { "type": "string" },
    "period":  { "type": "string" },
    "scope":   { "type": "string" },
    "summary": {
      "type": "object",
      "description": "Top-level aggregate value for the metric.",
      "properties": {
        "value":    { "type": "number" },
        "unit":     { "type": "string", "examples": ["days", "percentage", "count"] },
        "baseline": { "type": "number", "description": "Prior period value for comparison." },
        "trend":    { "type": "string", "enum": ["up", "down", "flat"] }
      }
    },
    "breakdown": {
      "type": "array",
      "description": "Per-group data if groupBy was specified.",
      "items": {
        "type": "object",
        "properties": {
          "group": { "type": "string" },
          "value": { "type": "number" }
        }
      }
    }
  }
}
```

---

## 4. Schema Index

Quick-reference table of all tool names, their JSON Schema keys, and where to find them in this document.

| Tool | Input Required | Input Optional | Returns |
|---|---|---|---|
| `getLeaveBalance` | `employeeId`, `leaveType` | — | `balances` (casual/sick/earned) |
| `getAttendance` | `employeeId`, `startDate` | `endDate` | `summary` + `records[]` |
| `getSalaryInfo` | `employeeId` | `month` | `grossSalary`, `deductions`, `netSalary` |
| `getEmployeeDetails` | `employeeId` | — | profile fields |
| `getCompanyPolicy` | `policySlug` | — | `content` (Markdown text) |
| `applyLeave` | `employeeId`, `leaveType`, `startDate`, `endDate` | `reason` | `requestId`, `status` |
| `getLeaveRequests` | — | `status`, `team`, `fromDate`, `toDate`, `limit` | `requests[]` |
| `updateLeaveStatus` | `requestId`, `status` | `comment` | updated request + `notificationSent` |
| `getEmployeeAnalytics` | `metric`, `period` | `scope`, `groupBy` | `summary` + `breakdown[]` |

---

> Related docs: [tool-design.md](tool-design.md) · [tool-safety-rules.md](tool-safety-rules.md) · [Day 2 — unsafe-actions.md](../../Day%202/docs/unsafe-actions.md)
