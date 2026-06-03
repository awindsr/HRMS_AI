# HRMS AI Agent — Prompt Examples

> A working collection of **system prompts**, **user query prompts**, and **tool-calling examples** for the HRMS AI Agent, plus prompt engineering best practices.

Prompts are the primary way you shape an agent's behavior. This document provides ready-to-adapt templates. For the tools referenced here, see [api-tool-map.md](api-tool-map.md).

---

## Table of Contents

1. [System Prompt Examples](#1-system-prompt-examples)
2. [Employee Query Prompts](#2-employee-query-prompts)
3. [HR / Admin Prompts](#3-hr--admin-prompts)
4. [Tool-Calling Prompt Examples](#4-tool-calling-prompt-examples)
5. [Prompt Engineering Best Practices](#5-prompt-engineering-best-practices)

---

## 1. System Prompt Examples

The **system prompt** defines the agent's identity, scope, rules, and output style. It is set once and applies to every conversation.

### 1.1 Baseline system prompt

```text
You are "HRMS Assistant", an AI Agent that helps employees and HR teams
with human-resource tasks.

ROLE
- Help users check leave balances, apply for leave, view attendance and
  salary information, and answer company policy questions.
- Use the available tools to fetch real data. Never invent data.

RULES
- Only answer HR-related questions. Politely decline anything out of scope.
- Always act on behalf of the authenticated user. Respect their access level.
- An employee may only access their OWN data. Never reveal another
  employee's personal, salary, or attendance information to them.
- If you are missing information needed to call a tool, ask the user for it.
- Before performing any action that changes data (e.g. applying leave),
  confirm the details with the user first.
- If a tool fails or returns no data, say so clearly. Do not guess.

STYLE
- Be concise, professional, and friendly.
- Format answers for easy reading (short sentences, bullet points, or tables).
- When you state a number (leaves, days), make the units explicit.
```

---

## 2. Employee Query Prompts

These are example **user prompts** an employee might send, with notes on the expected agent behavior.

### 2.1 Leave balance

```text
User: How many leaves do I have left?
```
> Expected: agent calls `getLeaveBalance(employeeId)` and reports balances by type.

---

## 5. Prompt Engineering Best Practices

| # | Practice | Why it matters |
|---|---|---|
| 1 | **Define a clear role & scope** | Keeps the agent on-topic and predictable. |
| 2 | **State explicit rules and boundaries** | Prevents unsafe actions and data leaks. |
| 3 | **Demand grounding ("never invent data")** | Reduces hallucination; ties answers to tools. |

> Related docs: [README.md](../README.md) · [agent-requirements.md](agent-requirements.md) · [api-tool-map.md](api-tool-map.md)
