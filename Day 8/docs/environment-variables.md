# Environment Variables

A learning note on configuration via environment variables: why they exist, the principles behind them, and the conventions different stacks use. Voyon appears at the end as one example.

## 1. The problem they solve

The same application code usually runs in several places — a developer laptop, CI, staging, production. Each needs *different* configuration (database URLs, endpoints, feature flags) and some of it is *secret*. If you hardcode those values in source you get two failures at once:

1. The build is tied to one environment — you can't promote the same artifact.
2. Secrets end up in version control, where they're effectively public forever.

The fix, captured by the **Twelve-Factor App** methodology (factor III, "Config"), is:

> **Store config in the environment, not in code.** Strict separation of config from code; config varies between deploys, code does not.

Environment variables are the lowest-common-denominator way to inject that config at run time: every OS, language, and container runtime can read them.

## 2. Config vs secrets — a key distinction

Not all configuration is sensitive. Conflating the two causes pain in both directions:

| | Example | Where it can live |
|---|---|---|
| **Config** (non-secret) | API base URL, log level, region, feature flag | Committed files are fine |
| **Secret** | API key, DB password, private key, token | Never in source — env vars / secret manager |

A useful test: *"If this value appeared in a public repo, would I need to rotate it?"* If yes, it's a secret. Environment variables handle both classes, but secrets often graduate to a dedicated secret store (see [secret management](secret-management.md)).

## 3. How config layering works

Most frameworks build configuration from **multiple sources, with a defined precedence** — later sources override earlier ones. A typical chain:

```
built-in defaults
   └─ committed config file        (appsettings.json, config.yaml)
        └─ environment-specific file (per-stage overrides)
             └─ local secret store    (dev only)
                  └─ environment variables   ← usually highest, wins
```

This is what lets a committed file hold a **blank or placeholder** secret while the real value arrives from an env var at deploy time. The code reads one merged view and never knows which layer supplied a given key.

## 4. Naming conventions across stacks

Environment variable names are flat strings, but config is often hierarchical. Each ecosystem bridges that gap differently:

| Stack | Convention | Example |
|---|---|---|
| **.NET** | `__` (double underscore) maps to the `:` section separator | `Agent__ConnectionString` → `Agent:ConnectionString` |
| **Node / 12-factor** | Flat `UPPER_SNAKE_CASE`, read from `process.env` | `DATABASE_URL` |
| **Vite / CRA front-ends** | Prefix gate (`VITE_`, `REACT_APP_`) decides what's exposed to client code | `VITE_API_BASE` |
| **Spring Boot** | Relaxed binding; `.`/`-` → `_`, uppercased | `SERVER_PORT` → `server.port` |
| **Docker / k8s** | Inject via `-e`, `env:`, or `envFrom` a Secret/ConfigMap | — |

### The front-end caveat
Anything bundled into a browser app is **downloadable by the user**, so a "client environment variable" is *not secret*. Tools like Vite enforce this with a prefix: only `VITE_`-prefixed variables are exposed to client code, precisely so you don't accidentally ship a server secret to the browser. **Never put a secret in a front-end env var.**

## 5. The `.env` file and the `.env.example` contract

Locally, developers often keep variables in a **`.env` file** loaded at startup. Two rules make this safe and friendly:

1. **`.env` is gitignored** — it holds real local values and must never be committed.
2. **`.env.example` is committed** — same keys, **no values**, with a comment per variable. It's the *onboarding contract*: a new developer copies it to `.env` and fills in the blanks. It documents exactly what the app needs without leaking anything.

## 6. Common anti-patterns

- Committing a real `.env`.
- Putting secrets in front-end (`VITE_`/`REACT_APP_`) variables.
- No `.env.example`, so onboarding means reading the source to discover required keys.
- Baking config into the build instead of reading it at run time (can't promote one artifact).
- Giant undocumented variables with no defaults or validation.

## 7. In this project (Voyon)

- **.NET backend** uses the `__`→`:` convention: `Agent__ConnectionString` binds to `AgentOptions.ConnectionString` via `AddOptions().Bind(GetSection("Agent"))` in [`Program.cs`](../../TeamAI/Program.cs).
- Precedence is `appsettings.json` < `appsettings.Development.json` < User Secrets < env vars, which is why the committed dev file can hold `ConnectionString: ""` while the real value comes from User Secrets — *proven* at startup (`Chat relay ready ... v16` only logs when the value resolved; see [evidence](../evidence-authenticated-call.md)).
- **Vite frontend** exposes only `VITE_API_BASE` (`import.meta.env`), a non-secret origin — by design no secret can reach client code.
- Both ship a committed `.env.example` ([backend](../../TeamAI/.env.example), [frontend](../../voyon-chat/.env.example)); the real `.env` / dev config are gitignored.

See [secret management](secret-management.md) for where the real values ultimately live.
