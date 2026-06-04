# Day 3 — Building HRMS Assistant v1 in Microsoft AI Foundry

> Day 3 of the **HRMS AI Agent** learning track. Today we leave the design docs behind and **actually build** a running agent — *HRMS Assistant v1* — inside **Microsoft AI Foundry**, then test it and document what it can (and cannot) do yet.

Days 1–2 were paper: *what an agent is* and *how to make it safe*. Day 3 is the first time the agent is **real and clickable**. Crucially, this version has **no tools** — it is the system prompt + a model, nothing else. That constraint is the whole lesson.

---

## Table of Contents

1. [Goals for Today](#1-goals-for-today)
2. [Learning Topics](#2-learning-topics)
   - [a) Microsoft AI Foundry project setup](#a-microsoft-ai-foundry-project-setup)
   - [b) Model selection](#b-model-selection)
   - [c) Agent playground](#c-agent-playground)
   - [d) Instruction testing](#d-instruction-testing)
   - [e) No-tool agent limitations](#e-no-tool-agent-limitations)
3. [Practical Work](#3-practical-work)
4. [Required Output](#4-required-output)
5. [The Big Picture: Where v1 Sits](#5-the-big-picture-where-v1-sits)
6. [Repository Structure](#6-repository-structure)

---

## 1. Goals for Today

By the end of Day 3 you should be able to:

- [ ] Create a **Microsoft AI Foundry** project from scratch.
- [ ] **Deploy and select** an appropriate model for an instruction-following assistant.
- [ ] Build an agent in the **Agent playground** using only **instructions** (system prompt).
- [ ] **Iterate** on instructions by testing real conversations.
- [ ] Clearly articulate the **limitations of a no-tool agent** — and why tools are Day 4's job.

> 🧠 **Mental model:** A no-tool agent is a *very well-briefed new hire on their first morning* — they know the policies and the tone, but they have **no login to any system**. They can talk about leave; they cannot look up *your* balance.

---

## 2. Learning Topics

### a) Microsoft AI Foundry project setup

**Microsoft AI Foundry** (`ai.azure.com`, formerly *Azure AI Studio*) is Microsoft's platform for building, testing, and deploying AI applications and agents. The hierarchy you'll work in:

```
┌─────────────────────────────────────────────────────────┐
│ Azure Subscription                                       │
│   └── Resource Group                                     │
│         └── AI Foundry resource / Hub                    │
│               └── Project           ← you build here     │
│                     ├── Model deployments (e.g. gpt-4o)  │
│                     ├── Agents      ← HRMS Assistant v1  │
│                     └── Playgrounds, eval, logs          │
└─────────────────────────────────────────────────────────┘
```

The step-by-step is in [docs/foundry-setup-guide.md](docs/foundry-setup-guide.md).

---

### b) Model selection

The agent's "brain" is the deployed model. Picking one is a trade-off between **capability, cost, and latency**.

| Model (example) | Strength | Best for | Watch-outs |
|---|---|---|---|
| **GPT-4o** | Strong reasoning + instruction following | The default for a rules-heavy agent like HRMS | Higher cost/latency than mini |
| **GPT-4o-mini** | Fast & cheap, good instruction following | High-volume, cost-sensitive deployments | Slightly weaker on tricky reasoning/refusals |
| **Reasoning models (o-series)** | Deep multi-step reasoning | Complex planning/analytics | Overkill + slower for simple Q&A |

> For HRMS Assistant v1 we want **reliable instruction-following and safe refusals**, so a **GPT-4o** (or **GPT-4o-mini** if cost matters) deployment is the sensible default. Rationale captured in [docs/foundry-setup-guide.md §3](docs/foundry-setup-guide.md#3-model-selection).

---

### c) Agent playground

The **Agent playground** is where you assemble and chat with the agent without writing any code. You set:

- **Name** — `HRMS Assistant`
- **Model deployment** — the one from step (b)
- **Instructions** — the **system prompt** (this is the heart of v1)
- **Tools** — *none yet* (intentionally empty for v1)

Then you talk to it in a chat pane and watch how it behaves.

---

### d) Instruction testing

The playground is a **tight feedback loop**: change the instructions → run a conversation → observe → refine.

```
   ┌──────────────┐
   │  Edit system │
   │  instructions│◀────────────┐
   └──────┬───────┘             │
          ▼                     │
   ┌──────────────┐      ┌──────┴───────┐
   │ Run a test   │─────▶│  Observe &   │
   │ conversation │      │  compare to  │
   └──────────────┘      │  expected    │
                         └──────────────┘
```

This is how we validate the system prompt from Day 2 against real model behaviour. Our 10 test conversations live in [docs/conversation-tests.md](docs/conversation-tests.md).

---

### e) No-tool agent limitations

This is the **key insight of Day 3.** With no tools, the agent **cannot touch real HR data.** It can only use what's in the prompt and its training. So:

| The agent CAN | The agent CANNOT (yet) |
|---|---|
| Explain HR concepts and processes generally | Look up *your actual* leave balance |
| Hold to its scope, tone, and refusal rules | Submit a real leave request |
| Ask clarifying questions | Fetch a real payslip or attendance record |
| Describe *how* it would help | Ground answers in live, per-user data |

⚠️ **The danger:** a no-tool agent asked for real data will either (a) correctly say it can't, or (b) **hallucinate** a plausible-sounding but fake number. Documenting where it does (b) is half the point of today.

> Full analysis: [docs/limitations.md](docs/limitations.md).

---

## 3. Practical Work

| # | Task | Output |
|---|---|---|
| 1 | Create **HRMS Assistant Agent v1** in AI Foundry (no tools) | [docs/foundry-setup-guide.md](docs/foundry-setup-guide.md) + screenshot |
| 2 | Run **10 test conversations** | [docs/conversation-tests.md](docs/conversation-tests.md) |
| 3 | Capture **expected vs. actual** results | [docs/conversation-tests.md](docs/conversation-tests.md) |
| 4 | Write a **limitations note** | [docs/limitations.md](docs/limitations.md) |

---

## 4. Required Output

- ✅ **Screenshot of agent** — see [docs/screenshots/](docs/screenshots/). Three captures: the [Foundry overview](docs/screenshots/AI%20Foundry.png), the [capabilities reply](docs/screenshots/chat.png) (C01), and the [no-tool limitation](docs/screenshots/No-tool-limitation.png) (C02).

![HRMS Assistant v1 in the AI Foundry playground](docs/screenshots/AI%20Foundry.png)
- ✅ **System prompt used** — [docs/system-prompt-used.md](docs/system-prompt-used.md).
- ✅ **10 conversation tests** — [docs/conversation-tests.md](docs/conversation-tests.md).
- ✅ **Limitations note** — [docs/limitations.md](docs/limitations.md).

---

## 5. The Big Picture: Where v1 Sits

```
 Day 1        Day 2             Day 3            Day 4+ (next)
 ─────        ─────             ─────            ────────────
 Concepts  →  Prompt & safety → Running agent  →  Add TOOLS
 (theory)     (design)          (no tools)        (real data)

                                 ▲ you are here
```

Day 3 proves the **prompt and personality work**. It also makes the **gap obvious**: without tools the agent is a talker, not a doer. That gap is exactly what motivates wiring in the tools from [Day 1's api-tool-map](../Day%201/docs/api-tool-map.md) next.

---

## 6. Repository Structure

```
Day 3/
├── README.md                       # You are here — Day 3 overview & guide
└── docs/
    ├── foundry-setup-guide.md      # Step-by-step: project, model, agent, testing
    ├── system-prompt-used.md       # The exact instructions given to v1
    ├── conversation-tests.md       # 10 conversations: expected vs. actual
    ├── limitations.md              # No-tool agent limitations note
    └── screenshots/                # Drop agent screenshot(s) here
        └── README.md               # What to capture
```

| File | Purpose |
|---|---|
| [README.md](README.md) | Conceptual + practical overview of Day 3. |
| [docs/foundry-setup-guide.md](docs/foundry-setup-guide.md) | The build guide: setup → model → agent → instruction testing. |
| [docs/system-prompt-used.md](docs/system-prompt-used.md) | The verbatim system prompt deployed to v1. |
| [docs/conversation-tests.md](docs/conversation-tests.md) | 10 test conversations with expected vs. actual results. |
| [docs/limitations.md](docs/limitations.md) | Honest write-up of what v1 can't do yet, and why. |

---

> 📚 **Recommended order:** this README → [foundry-setup-guide.md](docs/foundry-setup-guide.md) → [system-prompt-used.md](docs/system-prompt-used.md) → [conversation-tests.md](docs/conversation-tests.md) → [limitations.md](docs/limitations.md).
>
> ⬅️ Prior context: [Day 1](../Day%201/README.md) (concepts) · [Day 2](../Day%202/README.md) (prompt & safety).
