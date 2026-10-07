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

## IdP-first teller signup (issue #347)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #347; product decision 2026-09-16 (Glen)  
**Revisit when:** another teller IdP is added, or open register is reconsidered

New teller/admin accounts are created through Google (teller external auth: `google/login`, `google/one-tap`). Open anonymous `POST /api/auth/registerAccount` is disabled and returns the i18n key `auth.errors.openRegisterDisabled`. Existing local email/password **login** is unchanged.

The SPA `/register` page without an invite query explains Google-first signup and sends the user to Google or `/login`. Login copy steers new tellers to Google.

**Rejected alternative:** keep v3-style open email/password register and add captcha. Live v3 already sees spam Admin Registered accounts; captcha is not the strategy.

> Superseded 2026-09: “build invite-only email signup later.” That leftover is now the SuperAdmin one-time invite path below.

**Rejected alternative:** remove `POST /api/auth/registerAccount` entirely. Leftover clients still get a clear i18n error instead of a silent 404. The endpoint is still rate-limited.

Testing/Development accept the same `dev-google:{email}` credential as voter Google tests so teller create/login can be mocked without calling Google.

## SuperAdmin one-time invite for local email/password signup (issue #347 leftover)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #347 leftover after #348; product rule for communities that cannot use Google  
**Revisit when:** invites should be bound to a pre-filled email, Head Teller should issue them, or captcha is added on this path only

Communities that cannot use Google get a **one-time invite link**, not open register. SuperAdmin (`POST /api/superadmin/account-invites`) issues the link. The raw token is returned once and stored only as SHA-256. Anonymous `GET /api/auth/account-invite` peeks it; `POST /api/auth/registerWithInvite` consumes it and calls existing `LocalAuthService.RegisterAsync` (same Identity `AppUser` store, same verification email). After use or expiry (7 days) the invite is dead. Open `registerAccount` stays disabled.

`/register?invite=` shows the email/password form only when peek says the token is still usable. SuperAdmin Users has “Issue email/password invite.”

**Rejected alternative:** let Head Teller issue invites. `HeadTellerAccess` is election-route-scoped (user + election GUID). Creating a site-wide login account is not an election privilege, so SuperAdmin is the issuer.

**Rejected alternative:** a second user table or a parallel register service. Invite redeem must go through `RegisterAsync` so password rules, unique email, and verification stay one path.

**Rejected alternative:** bind the invite to a SuperAdmin-chosen email in this slice. The product asked for a one-time link that creates one account; the invitee picks the address. Binding can be added later if leaked-link squat becomes a problem.

**Rejected alternative:** captcha on this path. Optional later hardening only; not the strategy and not in this slice.

**Rejected alternative:** skip email verification on invite create so login works immediately. Invite only authorizes *creating* the row; proving inbox ownership stays the existing local-account pattern. Tests confirm the email, then password login succeeds.

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

The same in-memory middleware still owns the limits. Account `POST /api/auth/login` and guest teller `POST /api/auth/teller-login` count **failed attempts only** (`LoginIpMaxRequests` and `TellerLoginIpMaxRequests`, 20 per minute per trusted-ingress IP). A successful login does not consume the bucket. The middleware still returns 429 `error.tooManyRequests` up front when that bucket is already full, and it records the timestamp only after the endpoint runs and only for a counted failure: bad credentials or an invalid two-factor code on login, or an unknown election / wrong passcode on teller-login. Not-open, no-main-teller, and locked teller replies do not count, and neither do account-locked or unverified-email replies. An invalid two-factor code on `POST /api/auth/login` does count. That failure also calls `AccessFailedAsync`, so the account locks after `MaxFailedAccessAttempts` (5), the same as a bad password. The attempt that crosses that lock returns the account-locked reply and does not also consume the IP bucket. A correct password with no code still clears the access-failed count; that request is the two-factor prompt, not a guess. A supplied code is checked without clearing the count first, or a wrong code would start from zero every time. Venues often share one public IP behind NAT, so counting every request, including a successful teller join, would 429 the next person in the hall. The failure is recorded after the endpoint returns, so parallel failures from one IP can all pass the check before any is recorded and a burst can go past 20. The per-IP cap is approximate under concurrency. The per-election lockout (10 consecutive wrong passcodes) and the account lockout are the real guards; see the guest teller login lockout section. Other teller routes (the disabled register endpoint, 2FA, password reset, teller OAuth) stay **tight per trusted-ingress IP** and still count every request. Anonymous voter `requestCode` / `verifyCode` do **not** use that login IP bucket: elections often share one venue WiFi / community NAT, so a public `RemoteIp` is the whole hall. Those routes use a **5/min per VoterId** bucket (JSON body peek capped at 16 KiB even when ContentLength is missing / chunked; a fitting body is replaced with a MemoryStream at position 0 so model binding still works) plus a **60/min per trusted-ingress IP** venue ceiling. Oversized / peek-overflow bodies are **413** (`error.payloadTooLarge`) and do not continue to model binding — they must not be demoted to `missing:{ip}`, or a padded body with a real voterId would opt out of the cross-IP identifier ceiling. `missing:{ip}` is only for a genuinely empty, malformed, or absent `voterId`. Voter OAuth (`/api/online-voting/*Auth`) has no VoterId in the body — venue IP ceiling only. Teller OAuth stays on the tight IP table.

429 bodies return the i18n key `error.tooManyRequests` (same pattern as voter verify keys). That string lives only in `frontend/src/locales/en/errors.json`; other locales are not given English placeholders (missing keys fall back to English). The SPA resolves those keys instead of a single generic verify message. `VerifyAttempts` on `OnlineVoter` still locks five failed codes for that row; the middleware 429 is a cheap pre-service cap. The fifth mismatch returns `tooManyAttempts` on that call so the row lock is reachable under the 5/min per-VoterId middleware ceiling (a sixth HTTP call would otherwise be 429 first).

**Rejected alternative:** leftmost XFF as “original client.” Front Door’s own docs say an existing XFF is appended; the left side is attacker-controlled.

**Rejected alternative:** `UseForwardedHeaders` for XFF with KnownProxies cleared and ForwardLimit 2. That trust-all walk can set `RemoteIpAddress` to a spoofed entry, after which a “use RemoteIp” key is also spoofable.

**Rejected alternative:** keep keying on `RemoteIpAddress` only. On Azure UAT that address is the platform hop, so one client locks everyone out or the limit never isolates a single attacker.

> Superseded 2026-09: treat used and never-issued as one `noCodeFound` key. A consumed OTP still leaves `VerifyCodeDate` set after `VerifyCode` is cleared, so tellers can tell “already used” from “never issued.” See the verify-error keys section below.

**Rejected alternative:** split venue clients by trusting leftmost XFF. That reopens the spoof bypass. Two clients Azure actually distinguishes (two public RemoteIps, or two rightmost-public hops behind an infrastructure peer) stay two IP buckets; people behind one real public NAT share the venue ceiling and are separated by VoterId.

**Rejected alternative:** keep 5/min per IP on `requestCode` / `verifyCode`. A hall on one public address locks after a few voters — the opposite of the Front Door “one proxy hop” problem.

**Rejected alternative:** treat a too-large peek as a missing voterId (`missing:{ip}`). The request would still bind and send SMS/email for the real voterId, so padding past 16 KiB evaded the global per-identifier cap (N IPs × 5/min).

**Rejected alternative:** endpoint filter or service-level identifier limit as the primary mechanism. An endpoint filter sees the bound DTO but would split IP vs identifier across two pipeline stages. Service-level already has `VerifyAttempts`; it runs after routing/DB work and still needs an IP ceiling in middleware. Reading a capped body prefix in the existing middleware keeps both buckets in one place.

**Rejected alternative:** rebuild on ASP.NET `RateLimiter` or add a new auth flow. #192 said do not rebuild auth; this slice only fixes keying, coverage, and i18n bodies.

> Superseded 2026-10: count every `POST /api/auth/login` and `POST /api/auth/teller-login` request, including successes, at 5 per minute per trusted-ingress IP. See the guest teller login lockout section.

## Distinct voter verify error keys (issue #192 leftover)

**Status:** active  
**Evidence:** inferred  
**Source:** issue #192 remaining checklist; existing `{ error }` 400 bodies and `messageKey` i18n keys; `OnlineVoter.VerifyCodeDate` is not cleared on success  
**Revisit when:** used OTPs must be remembered across a later `requestCode`, or mismatch remaining-attempts should move off the 400 body

`POST /api/online-voting/verifyCode` keeps the existing `{ error }` body. The keys are stable phrase keys, not English and not a second error protocol.

- `voting.auth.verify.codeExpired` — live row, code older than 15 minutes
- `voting.auth.verify.alreadyUsed` — `VerifyCode` empty and `VerifyCodeDate` set (consumed OTP; date is the issue time of the code that was used)
- `voting.auth.verify.noCodeFound` — `VerifyCode` empty and `VerifyCodeDate` null (never issued)
- `voting.auth.verify.tooManyAttempts` — five failed mismatches on that row (`VerifyAttempts`)
- `voting.auth.verify.invalidCode` — mismatch with attempts left; 400 also includes `attempts` (remaining). The service still encodes `invalidCode:N` internally; the controller splits that into the stable key plus `attempts` so the SPA can interpolate `{attempts}`
- `error.tooManyRequests` — middleware 429, not a verify-row lock

The voter login page runs those keys through `resolveUserFacingApiError` (and understands a leftover `invalidCode:N` string). It does not collapse them into `voting.auth.verify.failed`.

**Rejected alternative:** keep used and never-issued as one `noCodeFound`. The issue asked for an “already used” key. Distinguishing them does not need a new column: success clears `VerifyCode` and leaves `VerifyCodeDate`.

**Rejected alternative:** keep `voting.auth.verify.invalidCode:4` as the public `error` string. That is not a stable i18n key; `te()` misses it and the UI shows the raw suffix. Remaining count stays a sibling field.

**Rejected alternative:** store a used-code hash so a later `requestCode` can still say “already used” for the old code. After a new code is issued the old one is simply a mismatch; only a replay with no new request is `alreadyUsed`.

## Online ballot identity is the voter session (issue #371 slice 0)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #371; product rule that a pending online ballot stays editable until a teller Accept-all (#188)  
**Revisit when:** voter JWTs are pinned to one election, or `submitBallot` / `voteStatus` drop the client voter id from the contract

`POST /api/online-voting/{electionGuid}/submitBallot` and `GET /api/online-voting/{electionGuid}/{voterId}/voteStatus` require policy `OnlineVoter`. The person is the `voterId` claim on the httpOnly `voter_token` (or a Bearer voter JWT in tests). A body or route id that is present and not exactly that claim is 403. Unknown election and “not on this election’s list” are the same 403 body (`error: forbidden`) — no phrase key and no id. No voter session is 401. A teller JWT is authenticated but fails `OnlineVoter` (403); it is not a voter session.

List membership is a `Person` row on that election (email, phone, or kiosk code). A missing person does not create an `OnlineVotingInfo` under a new guid. Email and phone sessions are not pinned to one election: one login may submit on every open election that lists that address (`availableElections`). A kiosk id already embeds its election; using it on another election is the same 403. Window, Finalized, and “already processed” still apply only after the person is on the list. Vote status for that person stays readable when the window is closed. Accept-all stays a teller action and does not take a voter id.

**Rejected alternative:** ignore a mismatched client id and silently write the session’s ballot. A wrong id must fail, so a buggy client cannot look like success while the request named someone else.

**Rejected alternative:** remove `voterId` from the route and submit DTO in this slice. The server never uses a non-matching value, and the SPA already sends the session id. Dropping it is a client regen with no extra safety.

**Rejected alternative:** put `electionGuid` on the voter JWT. Email and phone voters pick among every open election that lists them. Kiosk scope stays inside the voter id.

## Guest teller login lockout (issue #371 slice 1)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #371 slice 1; PR #374 review  
**Revisit when:** shared passcodes are stored hashed, or lockout should notify the owner by email

Guest tellers sign in with `POST /api/auth/teller-login` and the election's shared passcode. That path had no rate limit and no lockout. Account login for owners and admins is not covered by this lockout row and still works while guest teller login is locked.

- **Per trusted-ingress IP:** 20 failed attempts per minute, in `RateLimitingMiddleware` (`TellerLoginIpMaxRequests`). Successes do not consume the bucket. An unknown election or a wrong passcode counts; not-open, no-main-teller, and locked replies do not. The middleware records the failure after the action and still rejects with 429 `error.tooManyRequests` before the action when the bucket is already full; that 429 is not a passcode failure. Many tellers at one venue share a public IP behind NAT, so the cap counts failures only. The per-election lockout is the guessing guard. This cap is a code constant, like the other teller routes, not a config value. Account login uses the same failure-only shape at `LoginIpMaxRequests` (20 per minute). A bad password or an invalid two-factor code counts. The per-IP cap is approximate under concurrency, because the failure is recorded after the endpoint returns; the per-election lockout and the account lockout are the real guards.
- **Per election, across IPs:** table `TellerLoginLockouts` (migration `AddTellerLoginLockouts`), one row keyed by `ElectionGuid`. After `TellerLoginProtection:MaxConsecutiveFailures` wrong passcodes in a row (default 10), guest teller login stays locked until `LockedUntil` (`CooldownMinutes`, default 15). A successful login sets the count back to 0 and clears `LockedUntil`. When `LockedUntil` is already in the past, the next wrong passcode starts the count at 1. Only the statement that crosses the threshold writes one `TellerLoginLocked` audit row (details include the UTC end time and the failure count). Later attempts during the lock do not write another lockout row.
- **Atomic count on SQL Server:** `RecordPasscodeFailureAsync` is one `UPDATE ... OUTPUT inserted.ConsecutiveFailures, inserted.LockedUntil, deleted.LockedUntil`. `ConsecutiveFailures` becomes 1 when `LockedUntil` is already in the past, otherwise it increments. `LockedUntil` stays put while that lock is still in the future, and is set only when the new count reaches the threshold and no lock is active. A missing row is inserted, and a duplicate-key error is retried. `LockoutStarted` is true only for the update that crossed the threshold. A failure never writes `LockedUntil = NULL` over a lock that is still active.
- **SQLite tests cannot show that race.** The test host has no `OUTPUT deleted.*`. Its `UPDATE` only matches a row whose lock is missing or already expired, and `RETURNING` is the values that statement wrote. A parallel test still asserts one `TellerLoginLocked` row and a count no higher than the failures that were processed. It does not prove the SQL Server statement is free of lost updates.
- **Who is locked:** only the shared-passcode path. While locked, every teller-login attempt for that election returns 400 `auth.tellerJoin.locked`, including a correct passcode. `GET` election sets `tellerLoginLockedUntil` when the lock is still in the future. The guest-teller share control shows `elections.tellerLoginLocked`.
- **Check order:** load the election; a missing election returns `auth.tellerJoin.invalidElection` (the compare against a placeholder still runs). `IsGuestTellerAccessOpen` and `HasActiveMainTeller` run next, before the passcode compare and before the lockout counter. A closed election returns `auth.tellerJoin.notOpen`. No connected main teller returns `auth.tellerJoin.noMainTeller`. Those two are not passcode failures. A wrong passcode on an open election returns the same `auth.tellerJoin.invalidElection` body as a missing election. `TellerJoinPage` shows the server text, so those replies are phrase keys.
- **Owner remedy:** `GET /api/public/elections` lists the GUIDs of open elections, so a caller who is not a guest teller can keep posting wrong passcodes and hold the lock. Changing the passcode to a different value in `UpdateElectionAsync` resets that election's lockout row. `TellerLoginUnlocked` is written only when that row had a failure count or a lock. `POST /api/elections/{guid}/teller-login-unlock` (`FullTellerAccess`: election Owner or Admin, or a global Admin) clears the same row and writes the audit even when nothing was locked, and the share drawer has an Unlock button beside the lockout alert. Unlock alone does not stop the next burst; changing the passcode does, because the old code no longer matches.
- **Compare:** `CryptographicOperations.FixedTimeEquals` on UTF-8 bytes. Unequal lengths still run that compare (the stored bytes against themselves) and then return false. An empty stored passcode does not match.
- **Minimum length:** `TellerLoginProtection:MinimumPasscodeLength` (default 6) applies when an owner creates or changes the passcode. Empty clears it. Saving the same stored value is allowed when that value is already shorter. Login still accepts a stored short passcode. The validation message is `elections.form.electionPasscodeMinLength`. The update check reads the stored passcode with a synchronous query: ASP.NET automatic validation cannot run `MustAsync`. The SPA constant and that English sentence are fixed at 6; the client is not sent `MinimumPasscodeLength`. JSON import and Duplicate clear a passcode shorter than that length, leave it blank, and return `elections.form.electionPasscodeNotCopied`. The dashboard shows that key as a warning toast after the success message. The package-import log sends the same key; the loader dialog translates a line when `te(line.message)` is true, and leaves ordinary English loader lines as they are. v2 package import (`TallyJv2ElectionImportService`) is unchanged and can still store a short passcode.

**Rejected alternative:** count every login and teller-login request, including successes, at 5 per minute per IP. A venue behind one NAT then 429s the next teller who has the right passcode. The per-election row, not that IP bucket, is what stops passcode guessing.

> Superseded 2026-10: an invalid two-factor code on account login does not consume the per-IP bucket. `VerifyAsync` has no attempt counter, and a correct password cleared the access-failed count before the code was checked, so guessing a TOTP on `POST /api/auth/login` was unlimited per IP and per user. TOTP accepts any of 11 codes in a ±5 step window, which made that route easier than `/api/auth/verify2fa` (still 10 requests a minute, every request).

**Rejected alternative:** leave a wrong TOTP off the IP bucket and off `AccessFailedAsync`, because the password was already correct. The login body would stay an easier guess than `verify2fa`.

**Rejected alternative:** an in-memory per-election bucket in `RateLimitStore` (the issue's first sketch was a fail window inside the existing limiter). That count resets on restart and is not shared by a second instance, and a per-IP bucket does not stop an attacker who rotates addresses.

**Rejected alternative:** rebuild this limit on ASP.NET `RateLimiter`. The IP cap stays on the middleware that already keys teller routes; the lockout needs a row that outlives the process.

**Rejected alternative:** enforce the minimum length at login. Elections that already stored a shorter passcode would be unable to sign tellers in.

**Rejected alternative:** leave the failure count at the threshold after the cooldown, so the next wrong passcode locks again immediately. The cooldown is the penalty; the next run of consecutive failures starts at 1.

**Rejected alternative:** `SELECT ... WITH (UPDLOCK, HOLDLOCK)` or a `rowversion` retry around the read-increment-save. One `UPDATE ... OUTPUT` is the increment and the lock decision, so two requests cannot both observe the old count.

**Rejected alternative:** compare the passcode before the open and main-teller checks, so a closed election can return "not open" only when the code matches. That reply is different from a wrong code, so the passcode can be guessed while the election is closed.

**Rejected alternative:** send `MinimumPasscodeLength` to the client. The create and edit forms and `elections.form.electionPasscodeMinLength` stay fixed at the default of 6.

## Pre-auth voter-code delivery channel (issue #229)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #229 security notes; v3 VoterCodeHub used a short client key; 2026-09-20 product decision to ship live status  
**Revisit when:** channel tokens must survive a multi-instance farm, or join should be one-shot with no reconnect

Live delivery status for SMS/voice/email login uses an **anonymous** SignalR hub (`/hubs/voter-code`). Isolation is the unguessability of a **server-issued** token, not JWT (the voter is not authenticated yet).

- Token is 256 bits of CSPRNG, returned once on `requestCode`, stored only as SHA-256, TTL 10 minutes.
- One live joiner; reconnect after disconnect is allowed until TTL.
- Hub payloads are status + i18n key only. The one-time code is never sent on the hub (dev echo remains HTTP-only in Development/Testing).
- No extra PII: the verify step already shows the contact the voter typed; status strings do not repeat phone/email.
- Rate limits and SMS-pumping gates are unchanged and run **before** a channel exists.

**Rejected alternative:** reuse AllVoters / VoterPersonal (need an online-voter JWT). Rejected — this is the login request, before verify.

**Rejected alternative:** client-generated channel key (v3). Rejected as too weak.

**Why 10 minutes, not 15:** the OTP window is 15 minutes; the status channel only needs to cover send + Twilio callbacks + a reconnect. A leaked token should die before the code does.

See `context/realtime.md` for hub groups, replay-on-join, and provider wiring.
