# HRMS Assistant v1 — Microsoft AI Foundry Setup Guide

> A step-by-step guide to building **HRMS Assistant Agent v1** in **Microsoft AI Foundry** (`ai.azure.com`) — from creating the project, to deploying a model, to assembling a **no-tool** agent and testing its instructions.

> ℹ️ **UI note:** Microsoft AI Foundry evolves quickly and labels/screens may shift. The *concepts* and *order of operations* below are stable; if a button name differs, look for the nearest equivalent. Steps reflect the portal as of **June 2026**.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Create an AI Foundry Project](#2-create-an-ai-foundry-project)
3. [Model Selection](#3-model-selection)
4. [Deploy the Model](#4-deploy-the-model)
5. [Create the Agent (Agent Playground)](#5-create-the-agent-agent-playground)
6. [Add the System Prompt (Instructions)](#6-add-the-system-prompt-instructions)
7. [Instruction Testing Loop](#7-instruction-testing-loop)
8. [Capture the Screenshot](#8-capture-the-screenshot)
9. [Setup Checklist](#9-setup-checklist)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Prerequisites

| Requirement | Notes |
|---|---|
| **Azure account** | An active Azure subscription with permission to create resources. |
| **Access to AI Foundry** | Sign in at [https://ai.azure.com](https://ai.azure.com). |
| **Model access** | Quota/access for an Azure OpenAI model (e.g. GPT-4o). Some models require an access request. |
| **Role** | `Owner` or `Contributor` on the subscription/resource group is simplest for first-time setup. |
| **The system prompt** | From [system-prompt-used.md](system-prompt-used.md) — have it ready to paste. |

> 💡 If you don't have a subscription, an Azure free trial typically includes credits sufficient for this exercise. Costs here are small (a handful of short chats).

---

## 2. Create an AI Foundry Project

1. Go to **[https://ai.azure.com](https://ai.azure.com)** and sign in.
2. Click **+ Create project** (or **New project**).
3. Give it a name, e.g. **`hrms-agent-lab`**.
4. When prompted, let Foundry **create a new hub / resource** (or pick an existing one). This provisions the backing Azure resources (the AI Foundry resource, storage, etc.) under a resource group.
5. Choose a **region** close to you that supports your target model.
6. Click **Create** and wait for provisioning to finish.

```
 ai.azure.com
   └─ Create project ──► name: hrms-agent-lab
          └─ Hub/resource: (new) ──► Region: (e.g. East US)
                 └─ Create ──► ✅ Project ready
```

> ✅ **Done when:** you land on the project's **Overview** page.

---

## 3. Model Selection

Before deploying, decide *which* model the agent will think with. For an instruction-heavy, safety-sensitive assistant, prioritize **instruction-following and reliable refusals** over raw creativity.

| Candidate | Why pick it | Why not |
|---|---|---|
| **GPT-4o** ✅ *(recommended for v1)* | Strong, consistent instruction-following; handles scope/refusal rules well; good default. | Costs more than mini. |
| **GPT-4o-mini** | Cheapest + fastest; fine for most HR Q&A; great for high volume. | Slightly less reliable on adversarial/edge refusals. |
| **o-series (reasoning)** | Best for complex multi-step reasoning/analytics. | Slower + pricier; unnecessary for a no-tool Q&A agent. |

**Decision for v1:** **GPT-4o** — we want the most dependable adherence to the Day 2 safety rules while we validate the prompt. (Swap to `mini` later if cost/latency matters and tests still pass.)

> Record your actual choice in the [setup checklist](#9-setup-checklist).

---

## 4. Deploy the Model

1. In your project, open **Models + endpoints** (or **Deployments**) → **+ Deploy model** → **Deploy base model**.
2. In the **model catalog**, search for and select **`gpt-4o`** (or your chosen model).
3. Click **Confirm / Deploy**.
4. Give the deployment a name (e.g. **`gpt-4o`**) and accept defaults for now.
5. Wait until status shows **Succeeded**.

> ✅ **Done when:** the deployment appears with status *Succeeded* and is selectable in playgrounds/agents.

---

## 5. Create the Agent (Agent Playground)

1. In the left nav, open **Agents** (Azure AI Agent Service) → **+ New agent** (or **Create**).
2. Set:
   - **Agent name:** `HRMS Assistant`
   - **Deployment / model:** the `gpt-4o` deployment from step 4.
3. **Tools / Knowledge / Actions:** leave **empty** — this is intentional for v1.
   - Do **not** add Code Interpreter, File Search, Functions, or any action.
4. Open the agent's **playground / Try in playground** to get a chat pane.

```
 Agents ──► New agent
   ├─ Name:        HRMS Assistant
   ├─ Model:       gpt-4o
   ├─ Instructions: (next step)
   └─ Tools:       ⛔ none  ← the whole point of v1
```

> 🧪 **Alternative:** if your Foundry tenant doesn't have the Agents service enabled, use the **Chat playground** instead and paste the system prompt into the **System message / Setup** box. Functionally identical for this exercise (still a no-tool agent).

---

## 6. Add the System Prompt (Instructions)

1. In the agent's **Instructions** field (or the Chat playground's **System message** box), paste the **entire** system prompt from [system-prompt-used.md](system-prompt-used.md).
2. Leave temperature/other params at defaults (a low–moderate temperature is fine; lower = more consistent refusals).
3. **Save** the agent.

> ⚠️ Paste the prompt **verbatim**. The instructions *are* the product in v1 — every behaviour you test traces back to this text.

---

## 7. Instruction Testing Loop

Now validate behaviour against the Day 2 design using a tight loop:

```
   ┌──────────────┐
   │ Edit         │
   │ instructions │◀───────────────┐
   └──────┬───────┘                │
          ▼                        │
   ┌──────────────┐        ┌───────┴────────┐
   │ Run a test   │───────▶│ Compare to     │
   │ conversation │        │ EXPECTED       │
   └──────────────┘        │ behaviour      │
                           └────────────────┘
```

1. Open [conversation-tests.md](conversation-tests.md).
2. Send each test message in the playground.
3. Record the **actual** response next to the **expected** one.
4. If behaviour is wrong, tweak the instructions and re-run **just that case** (then re-run the full set before finishing — regression check).
5. Note anything the agent **can't** do because it has no tools → that feeds [limitations.md](limitations.md).

> 🎯 **Goal for v1:** the agent should nail **scope, tone, refusals, and confirmation language**. It will **not** be able to return real data — that's expected and documented, not a bug.

---

## 8. Capture the Screenshot

For the required deliverable:

1. Run a representative conversation (e.g. a greeting + a leave-balance question that exposes the no-tool limitation).
2. Capture the agent playground showing **(a)** the agent name, **(b)** the model, and **(c)** a sample exchange.
3. Save the image(s) into **[screenshots/](screenshots/)**.
4. Reference it from your notes.

**Captured for this build** (agent `test-data`, model `gpt-4.1-mini`):

![AI Foundry playground overview](screenshots/AI%20Foundry.png)

| Screenshot | Shows |
|---|---|
| [AI Foundry.png](screenshots/AI%20Foundry.png) | Playground overview — agent, `gpt-4.1-mini` model, Instructions loaded, no tools. |
| [chat.png](screenshots/chat.png) | C01 capabilities reply (scope & tone). |
| [No-tool-limitation.png](screenshots/No-tool-limitation.png) | C02 no-tool data gap — honest deferral, no invented number. |

> 🖼️ See [screenshots/README.md](screenshots/README.md) for the full gallery and descriptions.

---

## 9. Setup Checklist

Fill this in as you go (your record of what was actually built):

| Item | Value (fill in) |
|---|---|
| Foundry project name | `__________` |
| Region | `__________` |
| Model selected | `gpt-4.1-mini` (Global Standard) |
| Deployment name | `gpt-4.1-mini` |
| Agent name | `test-data` |
| Tools attached | **None (intentional)** |
| System prompt source | [system-prompt-used.md](system-prompt-used.md) |
| Screenshot saved | `screenshots/AI Foundry.png`, `screenshots/chat.png`, `screenshots/No-tool-limitation.png` |
| Date built | `2026-06-04` |

---

## 10. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Model not in catalog / "no quota" | Region or access restriction | Try another region; request model access; or use `gpt-4o-mini`. |
| Agent ignores some rules | Instructions truncated or temperature too high | Re-paste the full prompt; lower temperature. |
| "Agents" not visible | Agent Service not enabled in tenant | Use the **Chat playground** with the prompt as the system message. |
| Agent invents leave/salary numbers | **Expected** for a no-tool agent | Don't "fix" with prompt hacks — record it in [limitations.md](limitations.md); real fix is tools (Day 4). |
| Deployment "Succeeded" but chat errors | Propagation delay / wrong deployment selected | Wait a minute; confirm the agent points at the right deployment. |

---

> Related docs: [README.md](../README.md) · [system-prompt-used.md](system-prompt-used.md) · [conversation-tests.md](conversation-tests.md) · [limitations.md](limitations.md)
