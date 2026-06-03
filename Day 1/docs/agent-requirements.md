# HRMS AI Agent — Requirements Document

> A detailed requirements specification for an AI Agent that assists employees and HR teams within a Human Resource Management System (HRMS).

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Problem Statement](#2-problem-statement)
3. [Business Goals](#3-business-goals)
4. [User Personas](#4-user-personas)
5. [Functional Requirements](#5-functional-requirements)
6. [Non-Functional Requirements](#6-non-functional-requirements)
7. [AI Agent Workflow](#7-ai-agent-workflow)
8. [Limitations and Future Scope](#8-limitations-and-future-scope)

---

## 1. Project Overview

The **HRMS AI Agent** is a conversational assistant that sits on top of an organization's existing HR systems. It allows employees and HR staff to interact with HR data and processes using **natural language** instead of navigating menus, forms, and dashboards.

The agent uses a Large Language Model (LLM) as a reasoning engine to interpret requests, select the appropriate **tools** (APIs into the HRMS), execute them, and return clear, human-readable answers.

| Attribute | Value |
|---|---|
| **Product name** | HRMS AI Agent |
| **Primary users** | Employees, HR Managers, Admins |
| **Interface** | Conversational (chat / portal / Slack) |
| **Core technology** | LLM + tool calling (function calling) |
| **Status** | Design / prototype |

---

## 2. Problem Statement

HR operations are dominated by **repetitive, low-complexity requests** that nonetheless consume significant time:

- Employees repeatedly ask the same questions ("How many leaves do I have?", "What's the WFH policy?").
- Finding information requires navigating multiple screens or raising tickets to HR.
- HR teams spend hours on routine lookups and approvals instead of strategic work.
- Information is scattered across attendance, payroll, leave, and policy systems.

The result is **slow response times, frustrated employees, and overloaded HR staff**.

> **Problem:** Routine HR interactions are inefficient, fragmented, and labor-intensive — for both employees seeking answers and HR teams providing them.

---

## 3. Business Goals

| # | Goal | Success Indicator |
|---|---|---|
| G1 | **Reduce HR ticket volume** | Fewer routine queries reach HR staff |
| G2 | **Faster self-service** | Employees get answers in seconds, not hours |
| G3 | **Automate repetitive workflows** | Leave applications & lookups handled end-to-end by the agent |
| G4 | **Improve employee experience** | Higher satisfaction with HR interactions |
| G5 | **Free HR for strategic work** | Less time on routine lookups, more on people/strategy |
| G6 | **Centralize HR access** | One conversational entry point across all HR data |

---

## 4. User Personas

### 👤 Persona 1 — Employee

| Field | Detail |
|---|---|
| **Role** | General staff member |
| **Goals** | Quickly check leave balance, apply for leave, view attendance/salary, understand policies |
| **Pain points** | Doesn't know which screen to use; waits on HR for simple answers |
| **Access level** | **Own data only** — cannot view other employees' records |
| **Example request** | *"Apply 2 days of sick leave for next week."* |

### 🧑‍💼 Persona 2 — HR Manager

| Field | Detail |
|---|---|
| **Role** | HR operations / people manager |
| **Goals** | Look up any employee, review attendance summaries, support leave approvals, analyze trends |
| **Pain points** | Manual report generation; data spread across systems |
| **Access level** | **Team / department data** — broad read access, approval actions |
| **Example request** | *"Show me last month's attendance summary for the engineering team."* |

### 🛠️ Persona 3 — Admin

| Field | Detail |
|---|---|
| **Role** | System administrator |
| **Goals** | Configure the agent, manage access roles, monitor usage, manage policies/data |
| **Pain points** | Needs control and visibility over what the agent can do and access |
| **Access level** | **Full** — configuration, all data, monitoring |
| **Example request** | *"Update the casual leave policy text and review this week's agent usage logs."* |

---

## 5. Functional Requirements

### 5.1 Employee Agent Features

| ID | Feature | Description | Tool(s) used |
|---|---|---|---|
| FE-1 | **Check leave balance** | Report remaining leave by type (casual, sick, earned) | `getLeaveBalance()` |
| FE-2 | **Apply leave** | Submit a leave request with dates, type, and reason | `applyLeave()` |
| FE-3 | **View attendance** | Show presence, absences, late check-ins for a period | `getAttendance()` |
| FE-4 | **View salary information** | Show salary/payslip details for the employee | `getSalaryInfo()` |
| FE-5 | **Ask company policy questions** | Answer policy questions (leave, WFH, holidays, benefits) | `getCompanyPolicy()` |

**Acceptance criteria (examples):**
- The agent **must only** return the requesting employee's own data (FE-1 to FE-4).
- Before submitting a leave request (FE-2), the agent **must confirm** the dates and type with the user.
- Policy answers (FE-5) **must be grounded** in the official policy source, not invented.

### 5.2 HR Agent Features

| ID | Feature | Description | Tool(s) used |
|---|---|---|---|
| FH-1 | **Employee lookup** | Fetch any employee's profile/details | `getEmployeeDetails()` |
| FH-2 | **Attendance summary** | Aggregate attendance for a person, team, or period | `getAttendance()` |
| FH-3 | **Leave approval support** | List pending requests and assist approval/rejection | `getLeaveRequests()`, `updateLeaveStatus()` |
| FH-4 | **Employee analytics** | Surface trends (absenteeism, leave patterns, headcount) | `getEmployeeAnalytics()` |

**Acceptance criteria (examples):**
- HR features (FH-1 to FH-4) **must verify** the requester holds an HR/Admin role before executing.
- Approval actions (FH-3) **must be logged** with the actor, timestamp, and decision.

---

## 6. Non-Functional Requirements

| Category | Requirement |
|---|---|
| **🔒 Security** | All tool calls run over encrypted (HTTPS/TLS) channels. Sensitive data (salary, PII) is never logged in plaintext. Inputs are validated to prevent injection. |
| **🔑 Authentication** | Every request is tied to an authenticated user identity. The agent acts *on behalf of* that user and inherits their permissions. |
| **🛡️ Privacy** | Strict data scoping: employees see only their own data. PII is minimized in prompts and logs. Access follows the principle of least privilege. |
| **⚡ Performance** | Typical responses returned within a few seconds. Tool calls are timed out and retried gracefully on failure. |
| **📈 Scalability** | The system handles many concurrent users; tools are stateless where possible and horizontally scalable. |
| **🧭 Reliability** | Graceful degradation: if a tool fails, the agent explains the issue rather than hallucinating an answer. |
| **🔍 Observability** | All agent decisions and tool calls are traced and monitored (see README §3f). |

---

## 7. AI Agent Workflow

Every request flows through the same pipeline. The LLM is the decision-maker at the center; tools are how it acts.

```
            ┌─────────────────────┐
            │    User Request     │   "How many leaves do I have?"
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │  Prompt Processing  │   Combine system prompt + history + request
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │    LLM Reasoning     │   "I need the user's leave balance."
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │   Tool Selection    │   Choose: getLeaveBalance(employeeId)
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │    API Execution    │   Call HRMS API → { casual: 6, sick: 3 }
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │ Response Generation │   "You have 6 casual and 3 sick leaves left."
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │   Answer to User    │
            └─────────────────────┘
```

### Step-by-step

1. **User Request** — the user submits a natural-language message.
2. **Prompt Processing** — the system prompt, conversation history, available tool definitions, and user identity are assembled into the model input.
3. **LLM Reasoning** — the model interprets intent and decides whether a tool is required.
4. **Tool Selection** — if needed, the model emits a structured function call with arguments.
5. **API Execution** — the application runs the tool (after permission checks) and returns the result to the model.
6. **Response Generation** — the model converts the raw result into a clear, human-readable answer.
7. *(Loop)* — for multi-step tasks, steps 3–5 repeat until the agent has everything it needs.

---

## 8. Limitations and Future Scope

### Current Limitations

- **Prototype scope** — this is a design specification; tools are not yet implemented against a live backend.
- **No real LLM wired in yet** — reasoning behavior is described, not deployed.
- **Static policy knowledge** — policy answers depend on the source provided; no semantic retrieval yet.
- **Non-determinism** — LLM outputs can vary; rigorous evaluation is required before production use.
- **Limited action set** — only the tools in [api-tool-map.md](api-tool-map.md) are supported.

### Future Scope

- [ ] **Live LLM + function calling** integration.
- [ ] **Vector database / RAG** for grounded, up-to-date policy answers and long-term memory.
- [ ] **Role-based access control (RBAC)** enforced at the tool layer.
- [ ] **Write-back workflows** — full leave approval, attendance correction, profile updates.
- [ ] **Proactive notifications** — reminders for pending approvals, low leave balances.
- [ ] **Multi-channel deployment** — web portal, Slack, MS Teams, mobile.
- [ ] **Evaluation harness & monitoring dashboards** for accuracy, safety, and performance.

---

> Related docs: [README.md](../README.md) · [prompt-examples.md](prompt-examples.md) · [api-tool-map.md](api-tool-map.md)
