# Day 4 — Tool Design & Safety

**Topic:** Tool architecture for AI agents — schemas, API mapping, and risk classification.

## What We Built

| Document | What it covers |
|---|---|
| [docs/tool-design.md](docs/tool-design.md) | Core concepts: read vs write tools, JSON schema anatomy, API-to-tool mapping, risk classification framework + schema examples |
| [docs/hrms-api-tool-map.md](docs/hrms-api-tool-map.md) | Complete tool map for all 9 HRMS tools — full input/output JSON schemas, REST endpoint mapping, risk level, confirmation requirements |
| [docs/tool-safety-rules.md](docs/tool-safety-rules.md) | Safety rulebook: confirmation protocol, access control matrix, input validation, output handling, enforcement layers |

## Key Concepts Covered

### Read vs Write
Every tool is either a **read** (fetches data, safe to call speculatively) or a **write** (modifies data, requires confirmation). The confirmation gate is the primary safety mechanism for write tools.

### Tool Schema Format
Tools are described in JSON Schema format. The `description` field is the most important part — the model reads it to decide *when* to use the tool, so it must describe the use-case, not just the implementation.

### API-to-Tool Mapping
HRMS tools wrap real REST endpoints but expose a simplified, model-friendly interface. The tool wrapper handles auth headers, normalizes field names, and validates output before returning it to the model.

### Risk Classification (R0–R4)
| Level | Label | Example |
|---|---|---|
| R0 | Safe read | `getCompanyPolicy` |
| R1 | Scoped read | `getLeaveBalance`, `getSalaryInfo` |
| R2 | Soft write | `applyLeave` |
| R3 | Hard write | `updateLeaveStatus` |
| R4 | Admin/destructive | (future: bulk deletions) |

### Enforcement Layers
Safety is never a single gate. The system uses 4 layers: system prompt → tool schema descriptions → application code (RBAC, validation, confirmation gate) → audit & monitoring.

## Tool Summary

| Tool | Type | Risk | Role | Confirmation |
|---|---|---|---|---|
| `getCompanyPolicy` | Read | R0 | Any | No |
| `getLeaveBalance` | Read | R1 | EMPLOYEE+ | No |
| `getAttendance` | Read | R1 | EMPLOYEE+ | No |
| `getSalaryInfo` | Read | R1 | EMPLOYEE (own) / HR+ | No |
| `getEmployeeDetails` | Read | R1 | EMPLOYEE (own) / HR+ | No |
| `applyLeave` | Write | R2 | EMPLOYEE | **Yes** |
| `getLeaveRequests` | Read | R1 | HR/ADMIN | No |
| `updateLeaveStatus` | Write | R3 | HR/ADMIN | **Yes** |
| `getEmployeeAnalytics` | Read | R1 | HR/ADMIN | No |

## Build Log

- Day 1: Requirements, user personas, tool catalogue, basic API map
- Day 2: Agent rules, confirmation policy, system prompt, threat model
- Day 3: Live agent on Azure AI Foundry, tested against real prompts
- **Day 4: Full JSON schemas, risk classification, safety rulebook ← today**
