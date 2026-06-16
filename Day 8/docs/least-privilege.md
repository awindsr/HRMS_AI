# Least Privilege

A learning note on the Principle of Least Privilege (PoLP): one of the oldest and most reliable ideas in security. General concept first; Voyon as a worked example at the end.

## 1. The principle

> Every actor — a user, a process, a service, a credential — should have the **minimum** permissions required to do its job, and no more.

Coined by Saltzer and Schroeder (1975) and echoed in NIST and OWASP guidance ever since. The point isn't to prevent the *intended* action; it's to bound the *unintended* one. When (not if) something goes wrong — a bug, a stolen credential, a prompt injection, a compromised dependency — least privilege limits the **blast radius**.

Compare:

- An app that connects to its database as `db_admin` vs as a role that can only `SELECT/INSERT` on three tables. A SQL-injection bug in the first can drop every table; in the second it can't.
- A CI token with `repo:write` to one repository vs an org-wide admin PAT. If the first leaks, you lose one repo; if the second leaks, you lose everything.

## 2. Where the principle applies

PoLP is not one control; it's a lens you apply everywhere:

| Layer | Least-privilege question |
|---|---|
| **Users / roles** | Does this person have only the access their job needs? (RBAC/ABAC) |
| **Service identity** | Does this service account have only the cloud roles it uses? (managed identity, scoped IAM) |
| **Database** | Does the app's DB user have table/column-level grants, not `superuser`? |
| **Tokens / scopes** | Is the token scoped to the exact APIs/operations needed? (OAuth scopes) |
| **Network** | Can this component reach only the hosts it must? (security groups, egress rules) |
| **Files / processes** | Does it run as a non-root user with only the needed file permissions? |

## 3. Patterns that implement it

- **Role-Based Access Control (RBAC)** — assign permissions to roles, roles to users.
- **Scoped tokens** — OAuth scopes / fine-grained PATs that grant a subset of operations.
- **Per-user / per-request credentials** instead of one shared "service account" — so each action is authorized *and audited* as the real actor.
- **Just-in-time / time-boxed access** — elevate only when needed, expire automatically.
- **Managed identities** — let the platform issue short-lived, narrowly-scoped credentials so there's no long-lived key to leak.
- **Deny by default** — start with nothing and add only what's required (e.g. CORS, firewall rules).

## 4. The "confused deputy" — why per-user beats shared

A classic failure mode: a privileged program is tricked by a less-privileged caller into misusing its authority. If a backend calls a downstream API with **one shared, broadly-scoped credential**, then *any* bug or injection in the request-handling path can make it fetch or change data the calling user was never allowed to touch — the backend is a "confused deputy."

The fix is to **carry the caller's own identity all the way down**. When each downstream call uses the *user's* credential/token, the downstream system enforces that user's permissions, and no amount of trickery in the middle tier can exceed them. This is especially important for **AI agents**, where the "request-handling path" includes a model that can be steered by prompt injection. See [tool-level authorization](tool-level-authorization.md).

## 5. Common anti-patterns

- One admin/service account reused for everything ("it was easier").
- Granting `*` / wildcard IAM or DB superuser "to avoid permission errors."
- Long-lived broad tokens instead of short scoped ones.
- Adding privileges to fix a bug instead of fixing the access design.
- A middle tier that acts on a shared credential rather than the end user's identity.

## 6. In this project (Voyon)

- **HRMS is the authority.** The per-user JWT encodes the user's `role`/`AccessLevels`; Voyon forwards it and **adds no privilege of its own** — so an agent acting for Alice can only touch what Alice's account permits.
- **No shared/admin credential** in the call path: [`TokenManager`](../../TeamAI/Services/TokenManager.cs) resolves the signed-in user's token per request and throws if there isn't one (no service-token fallback) — this is the confused-deputy fix.
- **Managed identity in prod** for the Foundry connection (`DefaultAzureCredential`), not a long-lived key.
- **Deny by default**: CORS grants credentials only to explicitly configured origins; the agent's tool surface is just two tools.
- **Flagged gaps** (noted, not over-built): `team_id` is a client-supplied arg (safe only because HRMS still scopes by the caller's token), and Voyon adds no role checks of its own. See [security-checklist.md](../security-checklist.md).

Related: [tool-level authorization](tool-level-authorization.md), [JWT / API token usage](jwt-api-token-usage.md).
