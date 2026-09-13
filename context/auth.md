# Auth

## JWT user id claim lookup on .NET 10

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`)  
**Revisit when:** identity/JWT middleware or claim mapping changes, or .NET major upgrade changes claim defaults again

User IDs are stored in JWT `sub` claims. Code that reads the current user ID must check both:

```csharp
User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value
```

**Reason:** on .NET 10, claim type mapping for the subject claim is not always the same as older stacks; reading only `ClaimTypes.NameIdentifier` or only `"sub"` fails depending on token and middleware configuration.

**Rejected alternative:** assume a single claim type everywhere. That breaks in one of the two common configurations and produces hard-to-spot auth bugs (null user id, wrong scoping).

## Online-voter session is a distinct httpOnly cookie

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #250; CodeQL `js/clear-text-storage-of-sensitive-data` on `voter_token` / `voter_id` in localStorage; teller cookie pattern in `SecureCookieMiddleware`  
**Revisit when:** voter JWT lifetime or cookie attributes change, or teller/voter sessions must be mutually exclusive

Online voters use the same JWT claims as before (`voterType=online`, `voterId`, `voterIdType`). Only transport changed.

- **Cookie `voter_token`:** httpOnly, **always** Secure + SameSite=Strict + host-only (no Domain), Path=/ — the session JWT (24h). Never written to `localStorage` / `sessionStorage` and not returned in auth JSON.
- **Cookie `voter_session=1`:** same attributes except **not** httpOnly — a boolean flag so the SPA can detect a session and call `GET /api/online-voting/me` to restore `voterId`.
- **Cookie name is not `auth_token`.** Teller and voter JWTs can coexist in one browser. `OnMessageReceived` prefers `voter_token` on `/api/online-voting/*`, `/hubs/all-voters`, and `/hubs/voter-personal`; everywhere else it prefers `auth_token`. Bearer and hub `access_token` query still win when present (tests / tools).
- **Logout:** `POST /api/online-voting/logout` clears only voter cookies, using the same Secure/Strict/host-only attributes so the browser expires them. Teller logout still clears only teller cookies.
- **Dev / prod:** local Vite is HTTPS (`:8095`) and proxies `/api` and `/hubs` to HTTP `:5016`. The backend therefore sees `Request.IsHttps == false`. Voter cookies ignore that and stay Secure. UAT/prod are HTTPS.

**Rejected alternative:** copy the teller HTTP-dev exception (`Secure`/`SameSite`/`Domain` from `Request.IsHttps`). Rejected — Vite already presents HTTPS to the browser; issuing `Secure=false` voter cookies behind the proxy is not what operators want. Teller cookies keep that exception in this slice.

**Multi-tab:** cookies are shared across tabs on the same origin. Logging out in one tab clears cookies for all tabs; other tabs discover this on the next `/me` or `availableElections` call (401 → treat as logged out).

**Multi-device:** each device has its own cookie. A new login still notifies other sessions via VoterPersonal `updateVoter` (`login: true`). Logout on device A does not revoke device B’s JWT; B’s cookie lasts until expiry or its own logout.

**Rejected alternative:** reuse `auth_token` for voters. A teller who then votes (or the reverse) would overwrite the other session.

**Rejected alternative:** keep the JWT in the auth response body and only stop persisting it. XSS can still read the response; teller auth already omits tokens from the body.

**Rejected alternative:** encrypt the JWT in `localStorage`. Not a fix under XSS (issue #250 / #249).

## Proxy-aware auth rate limits (issue #192 leftover)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #192 remaining work; PR #328 review (prefer RemoteIp after UseForwardedHeaders; do not key leftmost XFF)  
**Revisit when:** ingress hop count changes (ForwardLimit), or rate limits move off the in-memory middleware

> Superseded 2026-09: keying on the **leftmost** X-Forwarded-For / Forwarded IP. Azure Front Door and App Service **append** the connecting socket IP. Leftmost is client-supplied; rotating it opened a new 5/min bucket.

> Superseded 2026-09: a parallel “infrastructure peer + rightmost public” reader that ignored `RemoteIpAddress` after `UseForwardedHeaders`, and proto-only forwarded headers. Rate-limit keys now follow the same hop walk the pipeline already applies.

Auth rate-limit keys use `Connection.RemoteIpAddress` **after** `UseForwardedHeaders`. That middleware applies `X-Forwarded-For` and `X-Forwarded-Proto` with `AuthForwardedHeaders.ForwardLimit` (2: Front Door + App Service). It walks from the right; entries left of that window are not applied. KnownProxies / KnownIPNetworks / KnownNetworks are cleared so the Azure hop is trusted (loopback-only defaults would leave RemoteIp as the platform).

`GetClientIpAddress` does not parse headers when RemoteIp is already set. Header parsing is only a fallback when remote is null (test host): the same right-to-left walk with the same forward limit. Leftmost is never chosen first.

The same in-memory middleware still owns the limits. Paths include the existing teller auth set plus anonymous voter `requestCode` / `verifyCode` and the cheap OAuth POSTs (`/api/auth/google/one-tap|facebook|kakao|telegram` and `/api/online-voting/*Auth`). 429 bodies return the i18n key `error.tooManyRequests` (English catalog only; other locales fall back). Verify failures keep `codeExpired`, `tooManyAttempts`, and `noCodeFound` (used-or-missing after the code is cleared). The SPA resolves those keys instead of a single generic verify message.

**Rejected alternative:** leftmost XFF as “original client.” Front Door’s docs say an existing XFF is appended; the left side is attacker-controlled.

**Rejected alternative:** parse XFF on every request and ignore RemoteIp. That duplicates and can disagree with `UseForwardedHeaders`.

**Rejected alternative:** keep keying on pre-forwarded `RemoteIpAddress` only. On Azure UAT that address is the platform hop, so one client locks everyone out or the limit never isolates a single attacker.

**Rejected alternative:** add a separate `alreadyUsed` key. After a successful verify the stored code is cleared; used and never-issued are the same row state (`noCodeFound`).

**Rejected alternative:** rebuild on ASP.NET `RateLimiter` or add a new auth flow. #192 said do not rebuild auth; this slice only fixes keying, coverage, and i18n bodies.
