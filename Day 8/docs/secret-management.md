# Secret Management

A learning note on handling secrets — passwords, API keys, tokens, certificates — across their whole lifecycle. General principles first; Voyon's specifics at the end.

## 1. What counts as a secret

A **secret** is any value whose disclosure lets someone impersonate a system or access data they shouldn't: API keys, database credentials, signing/private keys, OAuth client secrets, bearer tokens, encryption keys, webhook signing secrets.

The litmus test from [environment variables](environment-variables.md): *if it leaked into a public repo, would you have to rotate it?* If yes, it's a secret and must never be committed.

## 2. The lifecycle (where secrets go wrong)

Secrets fail at every stage, so manage all of them:

```
generate → distribute → store → use → rotate → revoke
```

- **Generate** with enough entropy (don't hand-pick "passw0rd").
- **Distribute** without emailing/Slacking them in plaintext.
- **Store** outside source control, encrypted at rest.
- **Use** without logging or printing them.
- **Rotate** on a schedule and on staff changes.
- **Revoke** immediately on suspected leak.

The most common real-world failure is the simplest: a secret committed to git. Because git keeps full history, *deleting it in a later commit does not remove it* — it's still in the history and on every clone.

## 3. Storage tiers (least → most secure)

```
Hardcoded in source        ✗ never — public to everyone with repo access
Committed config file      only placeholders / non-secret config
Gitignored local file      real values on one machine, never pushed (.env)
Local dev secret store     OS-backed, outside the repo (e.g. .NET User Secrets, direnv)
Environment variables      CI / container / platform runtime config
Dedicated secret manager   the production answer — see below
```

### Dedicated secret managers
For production, purpose-built systems beat raw env vars: **HashiCorp Vault, AWS Secrets Manager, Azure Key Vault, GCP Secret Manager, Kubernetes Secrets (sealed).** They add encryption at rest, fine-grained access policies, audit logs, and **automatic rotation**. Apps fetch secrets at startup using a *machine identity* (e.g. a cloud managed identity), so there's no "first secret" to bootstrap by hand.

## 4. Detection and prevention

Defence in depth, because humans slip:

- **`.gitignore`** the files that hold secrets (`.env`, environment-specific config).
- **Pre-commit hooks / scanners** — `git-secrets`, `gitleaks`, `truffleHog`, GitHub push protection — block or flag a secret before it lands.
- **CI secret scanning** as a backstop.
- **Code review** that asks "is anything here sensitive?"

## 5. Remediation when a secret leaks

There are **two independent steps**, and people skip the important one:

1. **Rotate / revoke first.** The moment a secret hits a shared place it's compromised. Invalidate it at the source (regenerate the key, revoke the token). *This is the only step that actually closes the hole* — scrubbing history does nothing if the old value still works.
2. **Then scrub.** `git rm --cached` + `.gitignore` for files that shouldn't be tracked; for a secret already in history, rewrite it out (`git filter-repo`, BFG) and force-push — but only *after* rotating.

Untracking ≠ removing from history. Assume any committed secret is burned.

## 6. Common anti-patterns

- Hardcoded keys "just for now."
- A real `.env` committed because it "worked locally."
- Scrubbing history but not rotating — the leaked value still works.
- One shared admin credential reused everywhere (no least privilege, no per-user audit).
- Secrets pasted into chat, tickets, or screenshots.
- Logging request/response bodies or headers that contain secrets.

## 7. In this project (Voyon)

Voyon's Day 8 audit applied these principles:

| Concern | Outcome |
|---|---|
| Real Foundry endpoint + tenant id in committed dev config | Moved to **.NET User Secrets** (dev) / env vars / **Key Vault** (prod); committed values blanked. |
| Were they ever committed? | `git log -S` → **never in history** (no rotation needed). |
| `voyon-chat/.env` was tracked | Untracked (`git rm --cached`); held only non-secret `VITE_API_BASE`. |
| Stale real HRMS JWT in local User Secrets (`Hrms:Token`) | Dead config, unused by code → removed via `dotnet user-secrets remove`. |
| Production store | Azure Key Vault via `DefaultAzureCredential`, gated on `KeyVault:Uri` ([`Program.cs`](../../TeamAI/Program.cs)). |

The per-user HRMS token is itself a secret; its handling is covered in [JWT / API token usage](jwt-api-token-usage.md). Binding mechanics are in [environment variables](environment-variables.md).
