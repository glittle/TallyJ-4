# Paid SMS / voice / WhatsApp destination eligibility

## Status: active

## Evidence: confirmed

**Source:** issue #254 (maintainer); July 555-range send incident described there  
**Revisit when:** NANP reserved-range rules change

## In-code gate before any paid provider

Paid verification (SMS / voice / WhatsApp) must not call Twilio or GreenAPI for reserved, fictional, or malformed destinations. The July incident was ~80 attempts to 555-style numbers that still incurred tiny Twilio charges — the existing “registered in an open election” check does not stop an election owner from loading those numbers and opening the window.

**First slice (issue #254):** a pure in-code helper (`PaidDestinationPhone`) rejects:

- malformed numbers that are neither true E.164 (`+` then 8–15 digits) nor the same digit string without a leading `+`
- NANP area code `555`
- NANP exchange `555` (including the `555-01xx` fictional block)

The check runs at the start of `RequestVerificationCodeAsync` for phone + paid delivery, and again inside `PaidVerificationSender` so a future caller cannot bypass it. Email, OAuth, and kiosk paths are unchanged. `WhenRegistered` is not written for a rejected number.

**Rejected alternative:** rely only on the open-election registration check. That stops outsiders requesting codes for arbitrary numbers; it does not stop fictional numbers that are already on a Person row.

**Rejected alternative:** wait for `OnlineVoter.SmsStatus` (this file’s next section). Status needs a row and a reason vocabulary. 555 / malformed must be blocked with no database work.

**Rejected alternative:** reject only `555-01xx` and allow other 555 NPA/NXX. The incident and the issue call out area code 555 and exchange 555, with `555-01xx` as the most important subset.

## Durable `OnlineVoter.SmsStatus` (second slice)

`OnlineVoter` is the global phone-status store (key remains `VoterId`, E.164 when `VoterIdType` is `"P"`). Person is per-election; the same phone can appear in many elections, so eligibility cannot live only on Person.

Field: `string? SmsStatus`, max 50, non-unicode (`varchar(50)`).

**Send rule (phone `VoterIdType` `"P"` + paid channel only):**

```text
if SmsStatus is not null AND SmsStatus != "OK" → do not send
```

| `SmsStatus` | Meaning | Send SMS/voice/WhatsApp? |
|-------------|---------|---------------------------|
| `null` | Not yet checked | Yes (current behaviour) |
| `"OK"` | Checked, valid | Yes |
| anything else | Blocked; value is the reason | **No** |

Initial reason vocabulary (not a closed enum — short phrases/codes): `555-range`, `undeliverable`, `landline`, `premium`, `admin`, `twilio-{code}` (auto-learned from selected Twilio errors; see fifth slice), plus in-code `malformed-e164`.

Pre-Twilio gate order in `RequestVerificationCodeAsync` (issue #254):

1. `PaidDestinationPhone` (in-code; no DB)
2. `OnlineVoter.SmsStatus` when `VoterIdType` is `"P"` and the channel is paid, a phone row already exists (`VoterId` + `VoterIdType == "P"`), and status is not null / not `"OK"` (do not create a row just to store status). Email / kiosk identifiers skip this gate.
3. Existing open-election registration check (unchanged)
4. Then the provider

Skip logs method + status only (no raw phone or email). Voter-facing message reuses `voting.auth.requestCode.invalidPhone`. Email / OAuth / kiosk unchanged. `WhenRegistered` semantics unchanged.

**Rejected alternative (this slice):** rename the column to `Status` so email/kiosk rows could share it later. The issue field spec and this slice’s contract are `SmsStatus`; a generic rename can be a later migration if other identifier types need a status.

**Rejected alternative:** persist the in-code `PaidDestinationPhone` reason onto an existing `OnlineVoter` row when the format gate rejects. Useful only when a row already exists; creating a row here would pull in `EnsureOnlineVoterForPhoneAsync` (later slice). Not done.

**Not in this slice (at the time):** Person UI / Front Desk display, `EnsureOnlineVoterForPhoneAsync` on Person create/import/update, Twilio status-callback auto-learn (now landed; see below), WhatsAppStatus / GreenAPI `checkWhatsapp` (#255).

## Ensure phone `OnlineVoter` on Person write (third slice)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #254 (maintainer)  
**Revisit when:** email/kiosk rows need the same ensure

`OnlineVoterPhoneHelper.EnsureOnlineVoterForPhoneAsync` (and the batch `EnsureOnlineVotersForPhonesAsync` for import) inserts a `VoterIdType = "P"` row keyed by `VoterId` = the phone as stored on `Person`. Callers: `PeopleService` create/update, `PeopleImportService` load batches, and `DbSeeder` so SeedOnStartup phones are usable locally.

`WhenRegistered`, `WhenLastLogin`, and `SmsStatus` stay null on insert. An existing row is left untouched (no duplicate, no field wipe). Whitespace-only / missing phone does nothing. Email / kiosk / Telegram rows are not created here.

`WhenRegistered` is stamped in `RequestVerificationCodeAsync` only after the voter passes the open-election registration check, and only when it is still null. That is the first successful code request — the same moment a brand-new row used to get `WhenRegistered`. Person write must not set it.

`VoterId` is the Person phone string as stored. `PaidDestinationPhone` does not return a normalized E.164 form, and auth still matches `Person.Phone == dto.VoterId`. This helper does not rewrite Person phones.

**Rejected alternative:** also hook JSON / v2 election-package person import. Those copy an existing election; the issue names Person create / people import / update. Package load can call the same helper later if Person UI needs rows for packaged phones.

**Rejected alternative:** put the helper on `OnlineVotingService`. People write paths should not take a voting-auth dependency just to insert a global phone row.

**Rejected alternative:** set `WhenRegistered` when the Person phone is saved. That would mark import/edit as registration and break “never seen vs imported-only” for the Person UI slice.

**Not in this slice (at the time):** Person UI / Front Desk SMS-status display, Twilio status-callback auto-learn (now landed; see below), WhatsAppStatus / GreenAPI `checkWhatsapp` (#255).

## Person detail phone OnlineVoter status (fourth slice)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #254 (maintainer); this slice’s lookup rule  
**Revisit when:** Front Desk should show WhatsApp status (not in #255 first slice)

People Management person detail (`GetPersonDetails` / `PersonDetailDto.PhoneOnlineVoter`) shows the global phone OnlineVoter SMS/WhatsApp/auth fields. Lookup is both `VoterId == Person.Phone` and `VoterIdType == "P"` (`OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync`). `IX_OnlineVoter_Id` is unique on `VoterId` alone, but paid-send and this UI are type-scoped to `"P"`. A non-P row occupying that `VoterId` is treated as no phone row (never seen); that row’s `SmsStatus` / `WhatsAppStatus` are not shown as the phone’s.

No phone (null/whitespace) → `PhoneOnlineVoter` is null and the UI hides the block. Phone with no matching P row → `HasPhoneRow` false (never seen). P row with `WhenRegistered` null → imported-only / not yet used for auth. `WhenRegistered` and `WhenLastLogin` are the stored values from that P row. `SmsStatus` is unchecked (null) / `OK` / blocked + the reason string. `WhatsAppStatus` is unchecked (null) / `OK` / the stored reason (`no-wa`, `check-failed`, …) — first slice of #255.

`VoterId` is the Person phone string as stored, not a normalized E.164. Logs still must not include raw phone/PII. Email / kiosk / Telegram UI is unchanged.

**Rejected alternative:** look up by `VoterId` only. That would surface a non-P occupant’s `SmsStatus` and dates as if they belonged to the phone.

**Rejected alternative:** Front Desk / people list columns in this slice. Optional later; person detail is the required surface.

**Not in this slice (at the time):** Recent `SmsLog` on person detail is the sixth slice. Manual SmsStatus is the eighth slice. Delivered-callback OK is the ninth slice. Front Desk / list columns are the tenth slice. WhatsAppStatus display landed in the #255 first slice below.

## Twilio status-callback auto-learn (fifth slice)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #254 (maintainer); this slice’s callback rules  
**Revisit when:** WhatsApp / GreenAPI #255

v3 already had one Twilio status callback: `PublicController.SmsStatus` → `TwilioHelper.LogSmsStatus` (update the existing `SmsLog` row by SID). v4 had the `SmsLog` table but no callback. This slice ports that **single** path to `POST /api/Public/smsStatus` and hooks auto-learn there. There is no second callback endpoint.

**SmsLog:** if a row exists for the SID (`MessageSid`, `SmsSid`, or voice `CallSid`), update `LastStatus`, `ErrorCode`, `LastDate`, and `Phone` (v3 `LogSmsStatus`). Do not insert a log row from a callback. Send-side insert is the seventh slice; auto-learn still does **not** require a log row — it uses Twilio `To`.

**Auto-learn write rule:**

```text
if MessageStatus is undelivered or failed
   AND ErrorCode is in the lasting-unusable allow-list
   AND an OnlineVoter row exists with VoterId matching To (see below) AND VoterIdType == "P"
   AND current SmsStatus is null or "OK"
→ SmsStatus = "twilio-{code}"
```

Allow-list (destination lastingly unusable): `30003` unreachable, `30005` unknown destination, `30006` landline/unreachable, `30004` filtered, `21211` invalid To, `21614` not a mobile. Mapped reason is always `twilio-{code}` (fits `varchar(50)`). Unlisted or missing `ErrorCode` on a terminal failure does **not** write (transient failures must not permanently block). `queued` / `sending` / `sent` never write a block. This slice did **not** set `SmsStatus` to `"OK"` from a delivered callback.

> Superseded 2026-09: delivered/completed on an existing SmsLog SID now sets `"OK"` — see ninth slice. Failure auto-learn is unchanged.

**Lookup:** Twilio `To` may be E.164 with `+` while `OnlineVoter.VoterId` is the Person phone as stored (with or without `+`). Auth still compares `Person.Phone == dto.VoterId` exactly. The callback tries the trimmed `To`, then the +/- variant. The EF query is `VoterId == key AND VoterIdType == "P"` — same as paid-send and `FindTrackedPhoneOnlineVoterAsync` (tracked, not the AsNoTracking helper). A non-P occupant of a candidate `VoterId` is not returned (no convert, no wipe). If no matching P row exists, skip; do not create an `OnlineVoter` from a callback. Do not rewrite Person phones.

**Signature:** `AllowAnonymous` is required so Twilio can POST, but the handler can stamp a lasting block. Validate `X-Twilio-Signature` (HMAC-SHA1 of the request URL + sorted POST params) against `Twilio:AuthToken` before any SmsLog / SmsStatus write. Missing token (including `<placeholder>`), missing signature, or mismatch → 403, no leak of whether a voter row exists. v3 did not validate; auto-learn makes an unsigned callback too dangerous to leave open.

An existing block reason is left alone. A later selected hard failure may overwrite `"OK"`.

Logs: method + status/code only. No raw phone or other PII.

**Rejected alternative:** require an `SmsLog` row before learning. That would make auto-learn a no-op until send-side log inserts land.

**Rejected alternative:** a new dedicated auto-learn endpoint. The issue says to hook the existing status-callback / SmsLog path.

**Rejected alternative:** write `undeliverable` (or set `"OK"` on delivered) in this slice. The selected-code vocabulary is `twilio-{code}`; OK-from-delivered is a later choice.

**Not in this slice:** PaidDestinationPhone, the pre-send SmsStatus gate, `EnsureOnlineVoterForPhoneAsync`, Person UI, SuperAdmin set, SignalR #229, WhatsApp/GreenAPI #255. Send-side SmsLog insert is the seventh slice.

## Person detail recent SmsLog (sixth slice)

**Status:** active  
**Evidence:** confirmed (surface); inferred (lookup / limit details)  
**Source:** issue #254 Person UI “optional recent SmsLog”; existing +/- phone keys from the fifth slice  
**Revisit when:** Front Desk should show recent SmsLog (it does not; person detail does)

Person detail (`PersonPhoneOnlineVoterDto.RecentSmsLogs`) shows up to five newest `SmsLog` rows for that Person phone. Lookup is the stored phone plus the +/- E.164 variant (`TwilioSmsStatusHelper.VoterIdLookupKeys` / `SmsLogPhoneHelper.FindRecentForPhoneAsync`). Not election-scoped and not by `PersonGuid` — verification SMS often has neither. Logs are about the phone, so they are attached even when there is no P row (never seen). No phone → `PhoneOnlineVoter` stays null (no log block).

DTO fields: `SentDate`, `LastDate`, `LastStatus`, `ErrorCode`. No phone and no SID (person detail already has the phone; logs must not add extra identifiers). Newest first (`SentDate`, then `RowId`). Empty list when none match; the UI hides the section. Status text is the stored `LastStatus`, or “Sent” when that is null. Rows come from the seventh-slice send insert (and later callback updates of that SID).

**Rejected alternative:** election- or PersonGuid-scoped logs. Request-code SMS is pre-election; those columns are often null.

**Rejected alternative:** require a P row before showing logs. A never-seen phone can still have delivery history; the OnlineVoter auth block and SmsLog are separate.

**Rejected alternative:** include SID in the teller DTO. Not needed to see delivery history; keep the payload to status / times / error code.

**Rejected alternative:** send-side `SmsLog` insert in this slice. That is the seventh slice.

**Not in this slice:** send-side SmsLog insert (seventh), StatusCallback URL on send, WhatsApp/GreenAPI #255, SignalR #229. Manual SmsStatus is the eighth slice. Delivered-callback OK is the ninth slice. Front Desk / list columns are the tenth slice.

## Send-side SmsLog insert + StatusCallback (seventh slice)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #254 leftover after #325; v3 `TwilioHelper.SendSmsAsync` / `SendVoice` insert + `twilio-CallbackUrl`  
**Revisit when:** WhatsApp / GreenAPI #255

On a successful paid SMS / voice / WhatsApp send, persist an `SmsLog` so person-detail recent logs (#325 / sixth slice) have real rows. Fields: SID, phone as sent (the request-code `VoterId`, not a rewritten E.164), `SentDate` / `LastDate` UTC now, `LastStatus` from the provider JSON (`status` / GreenAPI `idMessage` send uses `"submitted"`). `ElectionGuid` and `PersonGuid` stay null — `requestCode` is pre-election; person detail already looks up by phone (+/- variant).

Do **not** insert when the destination is rejected, the provider is not configured (dev skip still returns success), the HTTP send fails, or the provider body has no SID. A failed log insert must not fail the voter send (code already went out). Logs still must not include raw phone or other PII.

Twilio SMS and voice set `StatusCallback` to the existing `POST /api/Public/smsStatus`. URL order (v3 `twilio-CallbackUrl`): `Twilio:StatusCallbackUrl` if it is a usable absolute http(s) URL (origin-only values get the path appended), else `ClientEnv:apiUrl` + `/api/Public/smsStatus`, else `ClientEnv:frontendUrl` + that path (same-host production). Omit the form field when none resolve — the log row is still written. GreenAPI WhatsApp has no Twilio callback.

The callback still never inserts (fifth slice). Failure auto-learn still uses Twilio `To` and does not require a log row. Delivered OK (ninth slice) does require that SID. Voice callbacks may send `CallSid` / `CallStatus`; those are the same update-by-SID path, not a second endpoint.

**Rejected alternative:** insert a log row from the callback if send forgot one. That inverts v3 `LogSmsStatus` and would create rows for SIDs we did not send.

**Rejected alternative:** require `ElectionGuid` / `PersonGuid` on verification SMS. `requestCode` is not election-scoped; #325 already looks up by phone for that reason.

**Rejected alternative:** a new callback URL or endpoint. Wire the existing public SmsStatus path.

**Not in this slice:** setting `SmsStatus` to `"OK"` from delivered (ninth slice), WhatsApp / GreenAPI product work (#255), SignalR #229, rate limits #192. Manual SmsStatus is the eighth slice. Front Desk / list columns are the tenth slice.

## Teller manual SmsStatus (eighth slice)

**Status:** active  
**Evidence:** confirmed (privilege model from existing People routes); inferred (Ensure-if-missing on set)  
**Source:** issue #254 leftover after #327; PeopleController `[Authorize]` (same as UpdatePerson)  
**Revisit when:** WhatsApp / GreenAPI #255

Person detail can set `OnlineVoter.SmsStatus` on the phone P row (`VoterId == Person.Phone` and `VoterIdType == "P"`). Values are `"OK"` (unblock) or a short block reason (max 50 after trim). Null/unchecked is not set here. Lowercase `ok` is stored as `"OK"` so it cannot accidentally become a block reason (`AllowsPaidSend` is exact `"OK"`).

Privilege matches other People writes: `[Authorize]` only. SuperAdmin is the dashboard email-list policy, not a people-edit role — no new role and no SuperAdmin-only gate. Guest-teller JWT can hit the same People APIs as UpdatePerson if they have a token; People Management is the teller UI.

Uses the **stored** `Person.Phone`, not unsaved form edits. If there is no P row, `EnsureOnlineVoterForPhoneAsync` creates one (same helper as Person write) then sets status. A non-P occupant of that `VoterId` is not converted; the set fails. `WhenRegistered` / `WhenLastLogin` stay as they are. Paid-send still uses `if SmsStatus is not null AND SmsStatus != "OK" → do not send`.

A later delivered/completed Twilio callback on an existing SmsLog SID overwrites a teller block to `"OK"` (ninth slice) — the phone just worked. Failure auto-learn still leaves a teller block alone.

Not election-scoped and not under `ElectionFinalizedWriteGuard`: this is global paid-channel eligibility for the phone, not an election-person write.

Logs: method + status only. No raw phone or other PII.

**Rejected alternative:** SuperAdmin-only (`[Authorize(Policy = "SuperAdmin")]`). SuperAdmin is not who edits people today; tellers already update Person rows.

**Rejected alternative:** put `SmsStatus` on `UpdatePersonDto`. It lives on the global OnlineVoter row; a dedicated action keeps person save (per-election fields) separate from a global eligibility stamp and always uses the stored phone.

**Rejected alternative:** require an existing P row and refuse never-seen phones. Person write already ensures a row; ensuring here covers older data so a teller can still block or unblock from person detail.

**Not in this slice:** WhatsApp / GreenAPI #255, SignalR #229. Setting `"OK"` from a delivered Twilio callback is the ninth slice. Front Desk / list columns are the tenth slice.

## Set `SmsStatus` OK from delivered callback (ninth slice)

**Status:** active  
**Evidence:** confirmed (leftover after #327; Twilio success statuses)  
**Source:** issue #254 leftover after send-side SmsLog + StatusCallback; this slice’s SID + overwrite rules  
**Revisit when:** WhatsApp / GreenAPI #255

A successful Twilio StatusCallback now sets the matching phone P row to `"OK"` so a never-seen or previously blocked phone is not stuck until a teller clicks Set OK.

**Write rule:**

```text
if MessageStatus / CallStatus is delivered (SMS) or completed (voice)
   AND an SmsLog row already exists for the SID
   AND an OnlineVoter row exists with VoterId matching To (+/- variant) AND VoterIdType == "P"
→ SmsStatus = "OK"
```

SMS success is `delivered` (handset received the message). Voice success is `completed` (call finished). `sent` / `queued` / `sending` / `accepted` are not success — the carrier accepted the request, not the destination. Voice `busy` / `no-answer` / `canceled` are not lasting-unusable and do not write a block (fifth-slice failure allow-list unchanged).

The SID must already exist in `SmsLog` (send-side insert from the seventh slice). Granting eligibility is not the same as learning a block: failure auto-learn still uses Twilio `To` without a log row; delivered OK does not. Unknown SID still does not insert a log or an OnlineVoter. A non-P occupant of that `VoterId` is not converted.

**Manual / existing block:** there was no written product rule that a teller block must stay sticky after a later successful delivery. Delivered success overwrites any current reason (`admin`, `landline`, `twilio-{code}`, …) to `"OK"` — the phone just worked. Teller manual set (eighth slice) is unchanged. Failure auto-learn still leaves an existing block alone (`CanLearnFromCallback`). A later selected hard failure may overwrite `"OK"` again.

Logs: method + status only. No raw phone or other PII. Same signature gate as the fifth slice.

**Rejected alternative:** set OK from `To` without an SmsLog SID (same as failure auto-learn). Blocking a destination from a signed failure is defensive; marking a phone OK should only follow a message we sent.

**Rejected alternative:** keep a manual block sticky when delivered later succeeds. No such rule was written. The phone working is stronger evidence than the earlier reason.

**Rejected alternative:** treat SMS `sent` as success. Twilio `sent` is carrier handoff, not handset delivery.

**Not in this slice:** Front Desk / list columns (tenth slice), #255 WhatsApp, #229 SignalR, #192 UAT SMS. Teller manual set is the eighth slice.

## Front Desk / people list SMS hint (tenth slice)

**Status:** active  
**Evidence:** confirmed (P-row contract from person detail); inferred (compact list vocabulary)  
**Source:** issue #254 leftover after #331; person-detail lookup rule  
**Revisit when:** Front Desk should open person detail for Set OK / Block, or WhatsApp / GreenAPI #255

People list (`PersonListDto.PhoneOnlineVoter`) and Front Desk (`FrontDeskVoterDto.PhoneOnlineVoter`) show a compact phone SMS hint for people who have a phone. Same P-row contract as person detail: `VoterId == Person.Phone` and `VoterIdType == "P"`. A non-P occupant of that `VoterId` is never seen (`HasPhoneRow` false; that row’s `SmsStatus` is not shown). No phone → the property is null and the cell is empty / a dash.

The list DTO is slim (`PersonPhoneSmsHintDto`: `HasPhoneRow`, `WhenRegistered`, `SmsStatus`). It does not include last-login or recent SmsLog — those stay on person detail. Create/update `PersonDto` carries the same slim hint so the people-list cache is not wiped on save. Front Desk does not add the raw phone to the voter row.

Compact cell (SMS first): blocked reason / OK / never-seen / imported (`HasPhoneRow` and `WhenRegistered` null) / unchecked (registered, `SmsStatus` still null). Set OK / Block stays on person detail; the list has no new edit surface.

Batch lookup is `FindPhoneOnlineVotersAsync` (P rows only) so Front Desk / people list do not N+1.

**Rejected alternative:** look up by `VoterId` only. Same reason as person detail — a non-P occupant’s status is not the phone’s.

**Rejected alternative:** embed full `PersonPhoneOnlineVoterDto` (last-login + SmsLog) on every list row. The list only needs the compact hint; logs stay on person detail.

**Rejected alternative:** inline Set OK / Block on Front Desk or the people list. Person detail already has that teller action; Front Desk row-click is check-in, not people edit.

**Rejected alternative:** stack this leftover on the delivered-callback OK work (#332). The list reads status already on main after #331; #332 (ninth slice) can write OK from a delivered callback independently.

**Not in this slice:** delivered-callback OK is the ninth slice (#332). WhatsApp / GreenAPI #255 first slice is below (no Front Desk WhatsApp column). SignalR #229, rate limits #192. #254 stays open.

## Durable `OnlineVoter.WhatsAppStatus` (first slice of #255)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #255 (maintainer); provider comment that v4 stays on GreenAPI  
**Revisit when:** head-teller notify / abort queue, or Front Desk WhatsApp column

WhatsApp stays one voter-facing channel. Backend stays on **GreenAPI** (`checkWhatsapp` + existing `sendMessage`). Meta Cloud API and Twilio WhatsApp are out. Presence is stored on **OnlineVoter**, not Person: `WhatsAppStatus` `string?` max 50, non-unicode (`varchar(50)`).

| `WhatsAppStatus` | Meaning | Send WhatsApp? |
|------------------|---------|----------------|
| `null` | Not yet checked | Yes |
| `"OK"` | Has WhatsApp | Yes |
| anything else | No / blocked / error (`no-wa`, `check-failed`, …) | **No** |

Global by phone. Identifier gate is `VoterId == Person.Phone` and `VoterIdType == "P"`. A non-P occupant of that `VoterId` is not converted and that row’s `WhatsAppStatus` is not shown or written.

`SmsStatus` (#254) stays a separate field: a number can be fine for SMS and not on WhatsApp, or the reverse. The existing `SmsStatus` paid-channel gate still applies to WhatsApp send too.

`CheckWhatsAppAsync` (GreenAPI HTTP) is mocked in tests — CI must not depend on a live GreenAPI account. Config keys already exist: `GreenApi:IdInstance`, `ApiToken`, `BaseUrl`. When those are missing or placeholders, do not persist a lasting status (local/dev skip).

Person write still uses `EnsureOnlineVoterForPhoneAsync`. On Person phone **change**, clear `WhatsAppStatus` on the **new** number’s P row (or leave null) so it is re-checked. Do not copy the old number’s status onto a different number. The old number’s row is left as-is.

Person detail shows unchecked / OK / reason from the P-row lookup, same pattern as SMS status. This slice has no Front Desk column, no bulk notify, and no abort queue.

**Rejected alternative:** `Person.HasWhatsApp` (v3 packed `HasWA`). Rejected — presence is global by phone, same as `SmsStatus`.

**Rejected alternative:** Meta Cloud API or Twilio WhatsApp as the v4 provider. Rejected — business verification never landed; this issue stays GreenAPI-shaped.

**Not in this slice (first slice):** bulk `CheckMultipleWhatsAppAsync`, head-teller notify queue, abort token, Front Desk / people-list WhatsApp column, SignalR #229, #254 SMS leftovers.

## Bulk check selected WhatsApp (second slice of #255)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #255 leftover after #339; this slice’s check-selected contract  
**Revisit when:** head-teller notify queue / abort, or a people-list WhatsApp column

`CheckMultipleWhatsAppAsync` checks a **selected** list of person GUIDs in **one election**. Same teller `[Authorize]` as other People writes. Same P-row gate as one-phone check: persist only when `VoterId == Person.Phone` and `VoterIdType == "P"`. A non-P occupant is skipped, not converted. No phone is skipped. A GUID that is not in that election is ignored (`skipped-other-election`). GreenAPI is still `IGreenApiWhatsAppClient.CheckWhatsAppAsync` (production POSTs; tests mock). When GreenAPI is not configured, nothing is persisted.

The request is bounded (`MaxSelectedPeople` = 100) so one call cannot check an entire imported roll. Provider calls are sequential with a 200–400 ms pause between them so a selected list of ~20 does not burst GreenAPI. If the request abort token fires, remaining checks stop and the response includes what finished plus `cancelled` for the rest. This is **not** the head-teller notify send queue.

The People list (same `PeopleTable`, not a second grid) has row selection and **Check WhatsApp** for the current selection. Per-row / summary outcomes: OK / no-wa / failed / skipped. No Front Desk WhatsApp column, no notify “Has WhatsApp” filter, no notify send/abort queue.

Selected people are checked even if they already have a status — the teller chose this set. v3 skipped already-checked numbers; that is left for a later filter if notify needs “unchecked only.”

**Rejected alternative:** skip already-checked numbers (v3 `CheckMultipleWhatsAppAsync`). Rejected for this slice — “check selected” means the current selection, including a re-check.

**Rejected alternative:** a second people grid or notify page for the action. Rejected — the existing People list already had leftover bulk-action strings; selection belongs there.

**Rejected alternative:** a dedicated abort-queue endpoint (v3 notify abort). Rejected for this slice — cancel is the HTTP request token only.

**Not in this slice:** head-teller notify send + jitter + abort queue, Front Desk / people-list WhatsApp column, notify “Has WhatsApp” filter, SignalR #229, #254 SMS leftovers. #255 stays open.

## Related

- [auth.md](auth.md) — voter JWT claims (separate from paid-destination rules)
- `docs/VOTER_AUTHENTICATION_IMPLEMENTATION.md` — requestCode flow and SMS pumping
- Issue #254 remaining work; #255 for WhatsApp / GreenAPI status parity
