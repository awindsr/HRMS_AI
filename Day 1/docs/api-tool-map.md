# HRMS AI Agent — API & Tool Map

> The catalogue of **tools** (functions/APIs) the HRMS AI Agent can call, their input/output contracts, how the agent decides which tool to use, and security considerations.

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

... (file continues unchanged)
