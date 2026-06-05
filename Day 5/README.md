# Day 5 — Function Calling & Read-Only Tools

**Topic:** Turning the designed tools into running code — function calling, API wrappers, configuration, and error handling.

This is the first **build** day. Days 1–4 produced requirements, rules, prompts, and schemas; Day 5 implements the first three tools for real and wires them into a live LLM function-calling loop. **You build it manually** by following the build guide — nothing here is auto-generated.

## What This Day Covers

| Topic | Where |
|---|---|
| Function calling (LLM ↔ tools loop) | [build-guide.md Step 9](docs/build-guide.md#step-9--the-llm-agent-loop-azure-openai) |
| API wrappers | [build-guide.md Step 6](docs/build-guide.md#step-6--the-api-wrapper-hrmsapiclient) |
| Read-only tools | [build-guide.md Step 7–8](docs/build-guide.md#step-7--the-three-read-tools) |
| API configuration | [api-configuration.md](docs/api-configuration.md) |
| Error handling | [error-handling-notes.md](docs/error-handling-notes.md) |

## Documents

| Document | What it covers |
|---|---|
| [docs/build-guide.md](docs/build-guide.md) | **Start here.** Step-by-step C#/.NET build: project, mock HRMS API, API wrapper, the 3 tools, schemas, the Azure OpenAI function-calling loop, run modes |
| [docs/models-schemas.md](docs/models-schemas.md) | Data model schemas: `Employee`, `EmployeeTask`, response envelopes, enums — field tables, JSON Schema, examples |
| [docs/api-configuration.md](docs/api-configuration.md) | Config sources & precedence, the config keys, env-var / user-secrets overrides, timeout & retry strategy |
| [docs/error-handling-notes.md](docs/error-handling-notes.md) | Error taxonomy, where each failure is caught, what the model sees, how to reproduce every path |
| [docs/system-prompt-v2.md](docs/system-prompt-v2.md) | Updated system prompt (v1.1 → v2) with the three connected tools |
| [docs/api-call-logs.md](docs/api-call-logs.md) | Log format + a representative captured run (replace with your own) |
| [docs/test-results.md](docs/test-results.md) | Test plan + results template (tool, error, and function-calling tests) |
| [docs/screenshots/](docs/screenshots/) | Where to drop console/chat screenshots |

## The Three Tools (all read-only)

| Tool | REST endpoint | Risk | New in Day 5? |
|---|---|---|---|
| `getEmployeeList` | `GET /api/v1/employees` | R1 | ✅ new |
| `getEmployeeDetails` | `GET /api/v1/employees/{id}` | R1 | designed Day 4, **built today** |
| `getTaskList` | `GET /api/v1/tasks` | R1 | ✅ new |

## Required Outputs — Checklist

- [ ] **≥ 3 working read tools** — `getEmployeeList`, `getEmployeeDetails`, `getTaskList` (build via the guide, verify with `dotnet run`)
- [ ] **API logs** — capture `logs/api-calls.log`, paste a slice into [api-call-logs.md](docs/api-call-logs.md)
- [ ] **Screenshots / test results** — fill [test-results.md](docs/test-results.md), add images to [screenshots/](docs/screenshots/)
- [ ] **Error-handling notes** — review & confirm [error-handling-notes.md](docs/error-handling-notes.md) against your run
- [ ] **Updated system prompt** — [system-prompt-v2.md](docs/system-prompt-v2.md), saved as `system-prompt.txt` for `--chat`

## Quick Start

```powershell
# follow docs/build-guide.md to create the project, then:
cd "d:\Awin\HRMS_AI\Day 5\src\HrmsAgent"
dotnet run                 # offline test runner — all 3 tools + error paths, writes API logs
dotnet run -- --chat       # live LLM function calling (needs Azure OpenAI creds)
```

## Build Log

- Day 1: Requirements, user personas, tool catalogue, basic API map
- Day 2: Agent rules, confirmation policy, system prompt, threat model
- Day 3: Live agent on Azure AI Foundry, tested against real prompts
- Day 4: Full JSON schemas, risk classification, safety rulebook
- **Day 5: First real tools — function calling, API wrapper, error handling, logging ← today**
