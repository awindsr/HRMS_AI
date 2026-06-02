# AI Agent Learning & HRMS Agent Prototype

> A hands-on learning repository for understanding **AI Agents** from first principles, culminating in the design of a real-world **HRMS (Human Resource Management System) AI Agent**.

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [What is an AI Agent?](#2-what-is-an-ai-agent)
3. [AI Agent Building Blocks](#3-ai-agent-building-blocks)
4. [HRMS AI Agent Business Scenario](#4-hrms-ai-agent-business-scenario)
5. [Repository Structure](#5-repository-structure)
6. [Future Improvements](#6-future-improvements)

---

## 1. Introduction

### What is this repository about?

This repository is a **structured learning resource** for engineers who can already write code but are new to building **AI Agents**. It bridges the gap between theory ("what is an agent?") and practice ("how do I design one for a real business?").

The learning is anchored to a concrete, relatable use case: an **HRMS AI Agent** — an intelligent assistant that helps employees and HR teams handle everyday tasks such as checking leave balances, viewing attendance, and answering policy questions in natural language.

### Purpose of building an AI Agent

Traditional software requires users to navigate menus, fill forms, and learn an interface. An AI Agent flips this model: the user simply **states what they want**, and the agent figures out *how* to accomplish it by reasoning, calling tools, and composing a response.

We build an AI Agent to:

- **Reduce friction** — replace clicks and forms with plain language.
- **Automate repetitive workflows** — leave applications, status lookups, report generation.
- **Scale human support** — answer thousands of routine questions without growing the HR team.
- **Surface insight** — combine data from multiple systems into a single, conversational answer.

### Learning objectives

By working through this repository, you will be able to:

- [ ] Explain what an AI Agent is and how it differs from a chatbot or a RAG app.
- [ ] Describe the six core building blocks of an agent: **Prompt, Model, Tools, Memory, Evaluation, Logging**.
- [ ] Translate a business problem into an agent **requirements document**.
- [ ] Write effective **system and user prompts**.
- [ ] Design a **tool/API map** that an agent can reason over.
- [ ] Understand the end-to-end **request → reasoning → tool → response** workflow.

---

## 2. What is an AI Agent?

### Definition

> An **AI Agent** is a system that uses a Large Language Model (LLM) as a *reasoning engine* to decide which **actions** to take, executes those actions using **tools**, observes the results, and repeats this loop until it can produce a final answer for the user.

The key word is **agency**: unlike a system that only generates text, an agent can *act on the world* — query a database, call an API, send an email — and adapt its next step based on what it observes.

### How AI agents work

At its heart, an agent runs a loop, often called the **reasoning-action loop** (or *ReAct* loop):

```
        ┌─────────────────────────────────────────┐
        │                                          │
        ▼                                          │
  ┌───────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
  │  Observe  │──▶│  Reason  │──▶│   Act    │──▶│  Result  │
  │  (input)  │   │  (LLM    │   │  (call   │   │  (tool   │
  │           │   │  thinks) │   │  a tool) │   │  output) │
  └───────────┘   └──────────┘   └──────────┘   └──────────┘
        ▲                                          │
        └──────────── loop until done ─────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │ Final Answer  │
                    └───────────────┘
```

1. **Observe** — the agent receives a user request and any context (memory, prior steps).
2. **Reason** — the LLM decides: *Can I answer directly, or do I need a tool?*
3. **Act** — if a tool is needed, the agent calls it (e.g. `getLeaveBalance(empId)`).
4. **Result** — the tool returns data, which is fed back into the loop.
5. **Repeat** — the agent reasons again with the new information.
6. **Respond** — once it has enough information, it produces a human-readable answer.

### Chatbot vs. RAG vs. AI Agent

These three are often confused. The difference is in **what they can do** and **how much autonomy** they have.

| Capability | Traditional Chatbot | RAG Application | AI Agent |
|---|---|---|---|
| **Core idea** | Pattern/intent matching with scripted replies | Retrieve documents, then generate a grounded answer | Reason, plan, and take actions to achieve a goal |
| **Understands free language** | Limited (keywords/intents) | Yes | Yes |
| **Accesses external knowledge** | No (hardcoded) | Yes (vector search over docs) | Yes (tools + retrieval) |
| **Takes actions (writes data, calls APIs)** | Rarely / hardcoded | No (read-only) | **Yes** |
| **Multi-step reasoning** | No | No (single retrieve-then-answer) | **Yes (loops & plans)** |
| **Decides *which* operation to run** | No | No | **Yes (tool selection)** |
| **Example** | "Press 1 for leave, 2 for payroll" | "Summarize our leave policy from the handbook" | "Apply 2 days of casual leave for me next Monday" |

**In one line:**
- A **chatbot** *talks*.
- A **RAG app** *talks with knowledge*.
- An **AI Agent** *talks, knows, and acts*.

> 💡 An AI Agent often **uses RAG as one of its tools** — retrieval is a building block, not a competitor.

---

## 3. AI Agent Building Blocks

An AI Agent is assembled from six components. Think of them as the organs of the system.

```
┌────────────────────────────────────────────────────────────┐
│                         AI AGENT                             │
│                                                              │
│   ┌─────────┐   ┌────────┐   ┌────────┐                      │
│   │ PROMPT  │──▶│ MODEL  │──▶│ TOOLS  │                      │
│   │ (rules) │   │ (LLM)  │   │ (APIs) │                      │
│   └─────────┘   └────────┘   └────────┘                      │
│        ▲            ▲             │                          │
│        │            │             ▼                          │
│   ┌─────────┐   ┌────────────────────┐                       │
│   │ MEMORY  │   │ EVALUATION + LOGS  │                       │
│   └─────────┘   └────────────────────┘                       │
└────────────────────────────────────────────────────────────┘
```

### a) Prompt

The **prompt** is the set of instructions that shapes how the model behaves. It is the single most important lever you control.

- **Role of prompts** — they define the agent's *personality, scope, rules, and output format*. A well-crafted prompt is the difference between a helpful assistant and an unreliable one.
- **System prompt** — set once, behind the scenes. It establishes *who the agent is* and *what it may and may not do*. Example: *"You are an HRMS assistant. You may only answer HR-related questions. Never reveal another employee's salary."*
- **User prompt** — the actual question or request typed by the user. Example: *"How many casual leaves do I have left?"*
- **Why prompt engineering matters** — LLMs are extremely sensitive to wording. Clear instructions, examples, constraints, and a defined output format dramatically improve reliability and reduce hallucination.

> See [docs/prompt-examples.md](docs/prompt-examples.md) for concrete examples.

### b) Model

The **model** (LLM) is the agent's brain — the reasoning engine.

- **LLM's role inside an agent** — it interprets the user's intent, decides whether a tool is needed, picks *which* tool, fills in the arguments, and writes the final natural-language answer.
- **Reasoning and decision making** — modern LLMs can break a request into steps ("first find the employee, then fetch their leave balance, then format the answer"). This planning ability is what makes agents possible.

| Model concern | Why it matters |
|---|---|
| **Capability** | More capable models reason and plan better. |
| **Latency** | Users expect fast replies; pick a model sized to the task. |
| **Cost** | Larger models cost more per call; balance against accuracy. |
| **Context window** | Determines how much memory/history you can include. |

### c) Tools

**Tools** are the functions and APIs the agent can call to *do things* in the real world.

- **What are AI agent tools?** — well-defined functions exposed to the model, each with a name, a description, and an input schema. Example: `getEmployeeDetails(employeeId)`.
- **Function calling** — the model doesn't run code itself. Instead it outputs a structured request like `{ "tool": "getLeaveBalance", "args": { "employeeId": "E123" } }`. Your application executes the function and returns the result to the model.
- **API integrations** — tools are typically thin wrappers around your existing REST APIs, databases, or third-party services.
- **Examples for HRMS:**
  - `getEmployeeDetails()` — fetch a profile
  - `getAttendance()` — read attendance records
  - `getLeaveBalance()` — check remaining leave
  - `applyLeave()` — submit a leave request
  - `getCompanyPolicy()` — retrieve HR policy text

> See [docs/api-tool-map.md](docs/api-tool-map.md) for the full tool catalogue.

### d) Memory

Memory lets the agent stay coherent across a conversation and across sessions.

| Type | What it stores | Lifespan | Example |
|---|---|---|---|
| **Short-term memory** | The current conversation turns | Single session | Remembering you just asked about *casual* leave when you follow up with "and sick leave?" |
| **Long-term memory** | Durable facts and preferences | Across sessions | Remembering your employee ID, manager, or that you prefer answers in a specific format |
| **Conversation history** | The running transcript fed back into the model | Per session (windowed) | Lets the agent resolve "approve *it*" to the leave request mentioned two messages ago |

> Long-term memory is commonly backed by a **vector database** so the agent can semantically recall relevant past information.

### e) Evaluation

You cannot improve what you don't measure. **Evaluation** verifies the agent behaves correctly.

- **Why evaluation is needed** — LLMs are non-deterministic; the same question can yield different answers. Evaluation catches regressions and unsafe behavior before users do.
- **Accuracy checking** — compare the agent's answers and tool calls against a curated set of expected outputs ("golden" test cases).
- **Testing agent responses** — check for: *Did it pick the right tool? Did it pass the right arguments? Is the answer factually grounded? Did it respect access rules (e.g. not leak another person's data)?*

| Evaluation type | Question it answers |
|---|---|
| **Tool-selection accuracy** | Did the agent call the correct tool? |
| **Argument correctness** | Were the tool inputs right? |
| **Answer quality** | Is the response accurate, complete, and well-formatted? |
| **Safety / policy** | Did it refuse out-of-scope or unauthorized requests? |

### f) Logging & Monitoring

Logging makes the agent's behavior **observable** — essential for debugging and trust.

- **Tracking agent decisions** — record every step: the prompt, the model's reasoning, which tool was chosen, the arguments, the tool result, and the final answer.
- **Debugging** — when an answer is wrong, traces let you pinpoint *where* it went wrong (bad reasoning? wrong tool? bad data?).
- **Improving agent performance** — aggregated logs reveal patterns: common failure modes, slow tools, frequently asked questions worth optimizing.

| Log signal | Used for |
|---|---|
| **Traces** (step-by-step) | Debugging a single conversation |
| **Latency metrics** | Finding slow tools/models |
| **Error rates** | Reliability monitoring & alerting |
| **Usage analytics** | Product decisions & cost control |

---

## 4. HRMS AI Agent Business Scenario

To make the concepts concrete, this repository centers on a single scenario: an **HRMS AI Agent** that serves both employees and HR teams.

Imagine a conversational assistant living inside the company's HR portal or Slack. Instead of clicking through five screens to find a leave balance, an employee simply asks.

The HRMS AI Agent can:

- 🗣️ **Answer employee questions** — "What's our work-from-home policy?", "When is the next holiday?"
- 🌴 **Manage leave requests** — check balances, apply for leave, track approval status.
- 👤 **Fetch employee details** — pull up a profile, designation, department, or reporting manager.
- 🕒 **Provide attendance information** — "How many days was I present last month?", "Show my late check-ins."
- 🧑‍💼 **Assist HR teams** — look up employees, generate attendance summaries, support leave approvals, surface analytics.
- ⚙️ **Automate repetitive workflows** — turn multi-step manual processes into a single natural-language request.

**Example interaction:**

```
Employee: How many casual leaves do I have, and can I take Friday off?

Agent:    You currently have 6 casual leaves remaining.
          I can apply 1 casual leave for Friday, 6 June 2026 — shall I submit it?

Employee: Yes, please.

Agent:    ✅ Done. Your leave request for 6 June 2026 has been submitted
          and is pending approval from your manager (Priya Sharma).
```

> Full requirements are documented in [docs/agent-requirements.md](docs/agent-requirements.md).

---

## 5. Repository Structure

```
/
├── README.md                  # You are here — overview & concepts
└── docs/
    ├── agent-requirements.md   # Requirements spec for the HRMS AI Agent
    ├── prompt-examples.md      # System & user prompt examples
    └── api-tool-map.md         # Catalogue of tools/APIs the agent can call
```

| File | Purpose |
|---|---|
| [README.md](README.md) | Conceptual foundation: what agents are and how they're built. |
| [docs/agent-requirements.md](docs/agent-requirements.md) | The business & functional requirements for the HRMS agent. |
| [docs/prompt-examples.md](docs/prompt-examples.md) | Ready-to-use prompt templates and best practices. |
| [docs/api-tool-map.md](docs/api-tool-map.md) | The tools the agent reasons over, with I/O contracts. |

---

## 6. Future Improvements

This repository is a **design and learning prototype**. To evolve it into a production system, the following are planned:

- [ ] **Real LLM integration** — connect a live model (e.g. Claude) with function calling.
- [ ] **Vector database** — add semantic long-term memory and policy retrieval (RAG).
- [ ] **API tools** — implement the tools in [api-tool-map.md](docs/api-tool-map.md) against a real HRMS backend.
- [ ] **Authentication & authorization** — enforce role-based access (employee vs. HR vs. admin) and per-record data scoping.
- [ ] **Monitoring & observability** — add tracing, evaluation harnesses, latency/error dashboards, and alerting.

---

> 📚 **Start here:** read this README, then explore [docs/agent-requirements.md](docs/agent-requirements.md) → [docs/prompt-examples.md](docs/prompt-examples.md) → [docs/api-tool-map.md](docs/api-tool-map.md).
