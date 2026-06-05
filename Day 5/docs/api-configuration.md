# HRMS AI Agent — API Configuration (Day 5)

> Day 5 deliverable. How the agent is configured: where settings live, the precedence order, the keys it reads, and how to supply secrets without committing them.

---

## 1. Configuration Philosophy

Nothing environment-specific is hard-coded. Two distinct connections are configured separately:

| Connection | What it is | Configured by |
|---|---|---|
| **HRMS API** | The REST service the tools call (mock on localhost in Day 5; a real HRMS later) | `Hrms:*` keys |
| **Azure OpenAI** | The LLM that does function calling | `AzureOpenAI:*` keys |

Swapping the mock API for a real HRMS, or pointing at a different model deployment, is a **config change only** — no code edits.

---

## 2. Sources & Precedence

The app loads configuration in this order; later sources override earlier ones (see `Program.cs`):

```
 appsettings.json   ─┐
                     ├──►  merged configuration  (env vars win on conflict)
 Environment vars   ─┘
```

For local development you may also layer in **user-secrets** (see §5), which the .NET configuration system reads ahead of `appsettings.json` when enabled.

---

## 3. The Keys

From [`appsettings.json`](build-guide.md#step-2--configuration-appsettingsjson):

| Key | Purpose | Default | Sensitive |
|---|---|---|---|
| `Hrms:ApiBaseUrl` | Base URL of the HRMS REST API | `http://localhost:5099` | No |
| `Hrms:ApiKey` | Auth key sent as `X-Api-Key` header by the wrapper | `local-dev-key` | Yes (in prod) |
| `Hrms:HttpTimeoutSeconds` | Per-request timeout for the `HttpClient` | `10` | No |
| `AzureOpenAI:Endpoint` | Azure OpenAI resource endpoint URL | _(blank)_ | No |
| `AzureOpenAI:ApiKey` | Azure OpenAI API key | _(blank)_ | **Yes** |
| `AzureOpenAI:Deployment` | Deployment name of the chat model | `gpt-4.1-mini` | No |
| `MockApi:Port` | Port the in-process mock API binds to | `5099` | No |
| `MockApi:SimulateLatencyMs` | Artificial per-request delay (for realistic timing/logs) | `0` | No |

The HRMS API key never appears in a tool schema — it is attached inside the `HrmsApiClient` wrapper (Day 4 "hide internals" mapping principle).

---

## 4. Overriding via Environment Variables

.NET maps nested config keys to env vars using a **double underscore** (`__`) separator. This is the recommended way to pass secrets:

**PowerShell (current session):**

```powershell
$env:AzureOpenAI__Endpoint   = "https://<your-resource>.openai.azure.com/"
$env:AzureOpenAI__ApiKey     = "<your-azure-openai-key>"
$env:AzureOpenAI__Deployment = "gpt-4.1-mini"
# optional: point the tools at a different HRMS API
$env:Hrms__ApiBaseUrl        = "http://localhost:5099"
dotnet run -- --chat
```

| Config key | Environment variable |
|---|---|
| `AzureOpenAI:Endpoint` | `AzureOpenAI__Endpoint` |
| `AzureOpenAI:ApiKey` | `AzureOpenAI__ApiKey` |
| `AzureOpenAI:Deployment` | `AzureOpenAI__Deployment` |
| `Hrms:ApiBaseUrl` | `Hrms__ApiBaseUrl` |
| `Hrms:ApiKey` | `Hrms__ApiKey` |
| `Hrms:HttpTimeoutSeconds` | `Hrms__HttpTimeoutSeconds` |

---

## 5. Secrets via `dotnet user-secrets` (recommended for local dev)

Keeps keys out of the repo and out of your shell history:

```powershell
cd "d:\Awin\HRMS_AI\Day 5\src\HrmsAgent"
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey"   "<your-azure-openai-key>"
```

User-secrets are stored outside the project tree (under your user profile), so they are never committed. To read them, add `.AddUserSecrets<Program>()` to the `ConfigurationBuilder` in `Program.cs` (after `AddJsonFile`).

> **Rule:** the only secret values that ever live in `appsettings.json` are placeholders. Real keys come from env vars or user-secrets. Add `appsettings.*.local.json` and any `.env` files to `.gitignore`.

---

## 6. Timeouts, Retries & Resilience

| Concern | Day 5 handling | Production upgrade |
|---|---|---|
| **Timeout** | `HttpClient.Timeout` = `Hrms:HttpTimeoutSeconds`. A slow/dead API aborts and returns a clean `timeout` error. | Per-endpoint timeouts. |
| **Retries** | None (kept simple). A failed read returns an error the model reports honestly. | Add `Microsoft.Extensions.Http.Resilience` / Polly with exponential backoff for **idempotent reads only**. |
| **Connection reuse** | A single long-lived `HttpClient` (correct pattern — avoids socket exhaustion). | `IHttpClientFactory` + typed clients via DI. |
| **Circuit breaking** | None. | Polly circuit breaker so repeated upstream failures fail fast. |

Retries are safe to add here precisely because all three tools are **read-only / idempotent** ([Day 4 read-vs-write](../../Day%204/docs/tool-design.md#2-read-tools-vs-write-tools)). Never blindly retry write tools.

---

## 7. Azure OpenAI Setup Checklist

1. Create (or reuse) an Azure OpenAI / AI Foundry resource.
2. Deploy a **tool-capable** chat model — e.g. `gpt-4.1-mini` (the Day 3 model).
3. Copy the **Endpoint** and a **Key** from the resource's *Keys and Endpoint* page.
4. Note the **deployment name** (not the model name — they can differ).
5. Supply all three via env vars or user-secrets (§4–§5).

---

> Related docs: [build-guide.md](build-guide.md) · [error-handling-notes.md](error-handling-notes.md) · [Day 3 — foundry-setup-guide.md](../../Day%203/docs/foundry-setup-guide.md)
