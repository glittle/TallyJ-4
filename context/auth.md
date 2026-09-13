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
**Source:** issue #192 remaining work; PR #328 review — leftmost XFF is client-spoofable; Azure Front Door docs (append socket IP)  
**Revisit when:** ingress changes (no longer App Service / Front Door), venue halls regularly exceed the 60/min IP ceiling, or rate limits move off the in-memory middleware

> Superseded 2026-09: keying on the **leftmost** X-Forwarded-For / Forwarded IP. Azure Front Door and App Service **append** the connecting socket IP. Leftmost is whatever the client sent (`fake` or `fake, real`), so a new leftmost address opened a new 5/min bucket and bypassed the limit.

> Superseded 2026-09: keying on `RemoteIpAddress` after `UseForwardedHeaders` applies XFF with ForwardLimit 2. Tried after the rightmost-public design and reverted — trust-all + a hop limit can still rewrite RemoteIp from a client-controlled chain.

Auth rate-limit keys use the IP the **trusted ingress** saw, not a client-supplied leftmost address.

- **No proxy hop** (public `RemoteIpAddress`): key is that address. XFF / Forwarded are ignored so a direct client cannot pick a bucket.
- **Infrastructure peer** (`RemoteIpAddress` is null, loopback, or private — TestServer, App Service ARR, Docker): parse XFF (else RFC 7239 `Forwarded`) and take the **rightmost public** IP. That is the socket address the platform appended. Private / loopback / link-local / CGNAT (100.64/10) suffix hops are skipped. Two connecting clients behind one proxy therefore get two buckets; changing only the leftmost XFF stays in the same bucket.

`UseForwardedHeaders` is proto-only (`X-Forwarded-Proto`) with KnownProxies / KnownIPNetworks / KnownNetworks cleared so Azure TLS termination still sets `Request.IsHttps`. It does **not** apply `X-Forwarded-For`: trust-all + ForwardLimit would rewrite `RemoteIpAddress` from the client-controlled chain and make spoofing easier. Rate-limit keying reads the headers itself under the infrastructure-peer check above.

The same in-memory middleware still owns the limits. Teller `/api/auth/login` (and register / 2FA / password / teller OAuth) stay **tight per trusted-ingress IP** (5/min login). Anonymous voter `requestCode` / `verifyCode` do **not** use that 5/min IP bucket: elections often share one venue WiFi / community NAT, so a public `RemoteIp` is the whole hall. Those routes use a **5/min per VoterId** bucket (JSON body peek capped at 16 KiB even when ContentLength is missing / chunked; a fitting body is replaced with a MemoryStream at position 0 so model binding still works) plus a **60/min per trusted-ingress IP** venue ceiling. Missing/unreadable `voterId` is treated as one identifier scoped to that IP (`missing:{ip}`), not as a free pass. Voter OAuth (`/api/online-voting/*Auth`) has no VoterId in the body — venue IP ceiling only. Teller OAuth stays on the tight IP table.

429 bodies return the i18n key `error.tooManyRequests` (same pattern as voter verify keys). That string lives only in `frontend/src/locales/en/errors.json`; other locales are not given English placeholders (missing keys fall back to English). Verify failures keep `codeExpired`, `tooManyAttempts`, and `noCodeFound` (used-or-missing after the code is cleared). The SPA resolves those keys instead of a single generic verify message. `VerifyAttempts` on `OnlineVoter` still locks five failed codes for that row; the middleware 429 is a cheap pre-service cap.

**Rejected alternative:** leftmost XFF as “original client.” Front Door’s own docs say an existing XFF is appended; the left side is attacker-controlled.

**Rejected alternative:** `UseForwardedHeaders` for XFF with KnownProxies cleared and ForwardLimit 2. That trust-all walk can set `RemoteIpAddress` to a spoofed entry, after which a “use RemoteIp” key is also spoofable.

**Rejected alternative:** keep keying on `RemoteIpAddress` only. On Azure UAT that address is the platform hop, so one client locks everyone out or the limit never isolates a single attacker.

**Rejected alternative:** add a separate `alreadyUsed` key. After a successful verify the stored code is cleared; used and never-issued are the same row state (`noCodeFound`).

**Rejected alternative:** split venue clients by trusting leftmost XFF. That reopens the spoof bypass. Two clients Azure actually distinguishes (two public RemoteIps, or two rightmost-public hops behind an infrastructure peer) stay two IP buckets; people behind one real public NAT share the venue ceiling and are separated by VoterId.

**Rejected alternative:** keep 5/min per IP on `requestCode` / `verifyCode`. A hall on one public address locks after a few voters — the opposite of the Front Door “one proxy hop” problem.

**Rejected alternative:** endpoint filter or service-level identifier limit as the primary mechanism. An endpoint filter sees the bound DTO but would split IP vs identifier across two pipeline stages. Service-level already has `VerifyAttempts`; it runs after routing/DB work and still needs an IP ceiling in middleware. Reading a capped body prefix in the existing middleware keeps both buckets in one place.

**Rejected alternative:** rebuild on ASP.NET `RateLimiter` or add a new auth flow. #192 said do not rebuild auth; this slice only fixes keying, coverage, and i18n bodies.
