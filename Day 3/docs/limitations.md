# HRMS Assistant v1 — Limitations Note (No-Tool Agent)

> An honest write-up of what **HRMS Assistant v1** *cannot* do, *why*, and *how* each limitation gets resolved. v1 is a **no-tool agent**: a system prompt + a model, with no connection to any HR system.

This is the most important takeaway of Day 3. A no-tool agent is **a talker, not a doer.**

---

## Table of Contents

1. [The Core Limitation](#1-the-core-limitation)
2. [Limitations by Category](#2-limitations-by-category)
3. [The Hallucination Trap](#3-the-hallucination-trap)
4. [What v1 *Is* Good At](#4-what-v1-is-good-at)
5. [How Each Limitation Gets Fixed](#5-how-each-limitation-gets-fixed)
6. [One-Paragraph Summary](#6-one-paragraph-summary)

---

## 1. The Core Limitation

> **HRMS Assistant v1 has no tools, so it has no access to real HR data or the ability to perform any real action.**

The system prompt *tells* the agent to "use the available tools to fetch real data" — but no tools are attached. So every request that needs live, per-user data hits a wall:

```
  User: "How many leaves do I have?"
                │
                ▼
   ┌─────────────────────────────┐
   │  Agent (no tools)            │
   │  • Wants to call             │
   │    getLeaveBalance()         │
   │  • ...but it doesn't exist   │
   └─────────────┬───────────────┘
                 ▼
      Two possible outcomes:
      ✅ "I can't access live data" (honest)
      ❌ "You have 6 casual leaves" (HALLUCINATED)
```

The agent can reason and talk about HR, but it cannot *look anything up* or *change anything.*

In our actual run, v1 hit the **honest** branch — it asked for the leave type, then explained it has no access to real-time leave data and pointed the user to HR, instead of inventing a number:

![No-tool data gap — agent honestly defers instead of fabricating](screenshots/No-tool-limitation.png)

---

## 2. Limitations by Category

| # | Limitation | Why it happens | Impact |
|---|---|---|---|
| **L1** | **Cannot fetch personal data** (leave balance, attendance, salary, profile) | No `getLeaveBalance` / `getAttendance` / `getSalaryInfo` / `getEmployeeDetails` tool | Can't answer the most common employee questions with real values |
| **L2** | **Cannot perform write actions** (apply leave, approve/reject) | No `applyLeave` / `updateLeaveStatus` tool | Can confirm intent but cannot actually submit/change anything |
| **L3** | **Cannot ground policy answers** | No `getCompanyPolicy` tool | Policy replies are generic or risk being invented, not the company's real policy |
| **L4** | **No real authentication / role awareness** | No trusted identity context wired in | "Acts on behalf of the user" is aspirational; it can't truly scope data per user |
| **L5** | **No memory across sessions** | No persistent store/memory | Forgets everything between chats; no long-term context |
| **L6** | **Prone to hallucination under data pressure** | Model fills gaps when it has no real source | May state confident, fake numbers — the biggest risk (see §3) |
| **L7** | **No live validation** (dates, IDs, availability) | No backend to check against | Can't verify a date is valid, an ID exists, or leave is available |
| **L8** | **No logging / auditability of actions** | Nothing executes, nothing is recorded | Not production-safe for real workflows |

---

## 3. The Hallucination Trap

This deserves its own section because it's the **subtlest and most dangerous** limitation.

The v1 prompt contains a contradiction *in the absence of tools*:

- It says **"use the available tools to fetch real data"** — but there are none.
- It says **"never invent data"** — a guardrail.
- The model is also trained to **be helpful** — which pushes it to *answer anyway.*

When these collide, a no-tool model may **resolve the tension by inventing a plausible answer** ("You have 6 casual leaves left") that looks completely legitimate. In an HR setting that's not a harmless slip — it's **wrong information about someone's pay or time off.**

```
        "never invent data"   ⚔️   "be helpful"
                        \           /
                         \         /
                          ▼       ▼
                  no tool to resolve the conflict
                          │
                          ▼
                 risk: confident fabrication
```

**Mitigation in v1 (partial):** strong "never guess / say so clearly if you can't" wording helps, but does **not** fully eliminate the risk. **Real fix:** give it tools so "fetch real data" is actually executable (Day 4).

In our run the v1.1 wording held: the **policy** question (C03) is a good example — the agent gave only generic policy categories, said the policy tool was unavailable, and pointed to the official source instead of fabricating "this company's" WFH rules:

![C03 — generic WFH answer, no fabricated company policy](screenshots/WFH-Policy-response.png)

> 🔎 Track which conversations triggered this in [conversation-tests.md](conversation-tests.md) (watch C02, C03, C04, C07). So far C02, C03 and C07 all deferred honestly rather than hallucinating.

---

## 4. What v1 *Is* Good At

Limitations aside, v1 validates the **most important non-data behaviours** — and these carry forward unchanged once tools are added:

- ✅ **Scope discipline** — stays on HR topics, declines off-topic asks.
- ✅ **Tone & formatting** — concise, professional, readable.
- ✅ **Refusals** — privacy ("only your own data") and prompt-injection resistance.
- ✅ **Clarifying questions** — asks for missing details instead of assuming.
- ✅ **Confirmation language** — restates write actions before "doing" them.

> In other words: **the prompt/personality works.** What's missing is *hands*, not *judgment.*

---

## 5. How Each Limitation Gets Fixed

| Limitation | Fix | When |
|---|---|---|
| L1 — fetch personal data | Attach read tools (`getLeaveBalance`, `getAttendance`, `getSalaryInfo`, `getEmployeeDetails`) | **Day 4+** (see [../../Day 1/docs/api-tool-map.md](../../Day%201/docs/api-tool-map.md)) |
| L2 — write actions | Attach write tools (`applyLeave`, `updateLeaveStatus`) behind confirmation | Day 4+ |
| L3 — grounded policy | Attach `getCompanyPolicy` and/or RAG over the handbook | Day 4+ |
| L4 — auth & roles | Wire authenticated identity/role into the request context; enforce RBAC in code | Later |
| L5 — memory | Add conversation/session memory or a vector store | Later |
| L6 — hallucination | Tools remove the data-gap pressure; add grounding + evals | Day 4+ |
| L7 — validation | Validate inputs server-side against the live backend | Later |
| L8 — logging/audit | Log every tool call & decision; add monitoring | Later |

---

## 6. One-Paragraph Summary

> **HRMS Assistant v1 is a no-tool agent: it understands HR, holds its scope, refuses unsafe requests, and asks good clarifying questions — but it cannot fetch any real data (leave, attendance, salary, policy) or perform any real action (applying or approving leave), because no tools are connected.** Its biggest risk is **hallucinating plausible-but-fake data** when asked for information it has no way to retrieve. v1 successfully proves the *prompt and behaviour* are sound; the next step is attaching the tools from the [Day 1 API/tool map](../../Day%201/docs/api-tool-map.md) so the agent can finally **act on real data** instead of just talking about it.

---

> Related docs: [README.md](../README.md) · [foundry-setup-guide.md](foundry-setup-guide.md) · [system-prompt-used.md](system-prompt-used.md) · [conversation-tests.md](conversation-tests.md)
