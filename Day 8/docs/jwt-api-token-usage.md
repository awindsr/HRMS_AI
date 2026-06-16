# JWT / API Token Usage

A learning note on bearer tokens and JSON Web Tokens (JWTs): what they are, how they're meant to be handled, and the mistakes that turn them into breaches. The Voyon project appears at the end as one worked example.

## 1. What an API token is

An **API token** is a string a client presents to prove "I'm allowed to make this call." The server trusts the token instead of re-checking a username and password on every request. The most common form on the web today is the **bearer token**, sent in an HTTP header:

```
Authorization: Bearer <token>
```

"Bearer" is literal: *whoever bears (holds) the token can use it.* There is no second factor at request time — so a leaked token is a leaked identity until it expires or is revoked. Treat tokens like passwords, not like usernames.

## 2. Opaque tokens vs JWTs

There are two broad shapes:

| | Opaque token | JWT (JSON Web Token) |
|---|---|---|
| Looks like | A random string (`a1b2c3...`) | Three base64url parts: `header.payload.signature` |
| Meaning | None to the client; the server looks it up | Self-describing — the payload holds **claims** |
| Validation | Server-side store / introspection call | Verify the signature locally; no lookup needed |
| Revocation | Easy (delete the server record) | Hard (valid until `exp`) — needs a denylist to revoke early |

A **JWT** carries a JSON payload of *claims* — facts the issuer asserts about the subject. Standard (registered) claims include:

| Claim | Meaning |
|---|---|
| `sub` | Subject — who the token is about |
| `iss` | Issuer — who minted it |
| `aud` | Audience — who it's intended for |
| `exp` | Expiry (epoch seconds) — when it stops being valid |
| `nbf` / `iat` | Not-before / issued-at |

Plus any custom claims the issuer adds (roles, tenant, email, permission levels).

> **Critical misconception:** a JWT is **signed, not encrypted.** Anyone holding it can base64-decode the payload and read every claim. The signature only proves the payload wasn't *tampered with* — it does not hide anything. Never put a secret in a JWT payload, and never assume the claims are private.

## 3. How tokens should be handled

These rules are general, not project-specific:

1. **Transport over TLS only.** A bearer token in cleartext HTTP is sniffable. Always HTTPS.
2. **Header, never the URL.** Tokens in query strings end up in server logs, browser history, and `Referer` headers. Use the `Authorization` header.
3. **Never log the token.** Logging frameworks love to dump headers; scrub or allowlist so the credential never lands in a log file. Log a token *id* or a hash if you need correlation.
4. **Short lifetimes + refresh.** A short `exp` limits the blast radius of a leak. Pair a short-lived access token with a longer-lived refresh token when you need long sessions.
5. **Validate fully on the server.** Check the signature, `exp`/`nbf`, `iss`, and `aud`. Reject `alg: none`. Don't trust claims you didn't verify.
6. **Store safely on the client.** This is where most front-end breaches happen — see below.

## 4. Where to store a token in a browser (the hard one)

| Location | XSS-safe? | CSRF-safe? | Notes |
|---|---|---|---|
| `localStorage` / `sessionStorage` | ❌ No | ✅ Yes | Any injected script can read it. Popular but risky. |
| Normal JS-readable cookie | ❌ No | ❌ No | Worst of both. |
| **`HttpOnly` cookie** | ✅ Yes | ⚠️ Needs `SameSite`/CSRF token | JS can't read it; defeats token theft via XSS. |
| In-memory variable | ✅ Mostly | ✅ Yes | Lost on refresh; needs silent re-auth. |

The modern default for browser apps is an **`HttpOnly`, `Secure`, `SameSite` cookie**: JavaScript cannot read it (so an XSS payload can't exfiltrate it), and the browser attaches it automatically. The trade-off is CSRF, which `SameSite=Lax/Strict` and/or an anti-CSRF token address.

## 5. Common anti-patterns

| Anti-pattern | Why it bites |
|---|---|
| Token in `localStorage` | One XSS bug = stolen session for every user hit |
| Logging the `Authorization` header | A live credential sitting in plaintext logs |
| Long-lived tokens with no revocation | A leak stays exploitable for weeks |
| Trusting JWT claims without verifying the signature | Forged tokens; privilege escalation |
| One shared token for all users | No per-user authorization; over-privilege |
| Putting secrets in the JWT payload | It's readable by anyone holding the token |

## 6. In this project (Voyon)

Voyon applies the above to the HRMS bearer token:

- HRMS issues a **JWT** at login; Voyon reads non-secret claims (`email`, `EmployeeId`, `CompanyId`, `exp`) for display and to build write payloads, and forwards the raw token as `Authorization: Bearer ...`.
- The token is stored in an **`HttpOnly`, encrypted session cookie** — never in `localStorage` and never exposed to JS (the SPA only sends the cookie). → defeats XSS token theft.
- It is resolved per request in [`TokenManager`](../../TeamAI/Services/TokenManager.cs) and attached in [`AttendanceService`](../../TeamAI/Services/AttendanceService.cs); it is never logged.
- The session **expires with the JWT's `exp`** (no silent refresh), so a stale credential never lingers.

See [tool-level authorization](tool-level-authorization.md) for how each tool call carries the right user's token, and [secret management](secret-management.md) for non-token secrets.
