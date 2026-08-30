# Paid SMS / voice / WhatsApp destination eligibility

## Status: active

## Evidence: confirmed

**Source:** issue #254 (maintainer); July 555-range send incident described there  
**Revisit when:** SuperAdmin/teller manual SmsStatus, Front Desk / list columns, or NANP reserved-range rules change

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
**Revisit when:** email/kiosk rows need the same ensure, or SuperAdmin/teller manual SmsStatus lands

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
**Revisit when:** Front Desk / people list columns, SuperAdmin/teller manual SmsStatus, or recent SmsLog land

People Management person detail (`GetPersonDetails` / `PersonDetailDto.PhoneOnlineVoter`) shows the global phone OnlineVoter SMS/auth fields. Lookup is both `VoterId == Person.Phone` and `VoterIdType == "P"` (`OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync`). `IX_OnlineVoter_Id` is unique on `VoterId` alone, but paid-send and this UI are type-scoped to `"P"`. A non-P row occupying that `VoterId` is treated as no phone row (never seen); that row’s `SmsStatus` is not shown as the phone’s.

No phone (null/whitespace) → `PhoneOnlineVoter` is null and the UI hides the block. Phone with no matching P row → `HasPhoneRow` false (never seen). P row with `WhenRegistered` null → imported-only / not yet used for auth. `WhenRegistered` and `WhenLastLogin` are the stored values from that P row. `SmsStatus` is unchecked (null) / `OK` / blocked + the reason string.

`VoterId` is the Person phone string as stored, not a normalized E.164. Logs still must not include raw phone/PII. Email / kiosk / Telegram UI is unchanged.

**Rejected alternative:** look up by `VoterId` only. That would surface a non-P occupant’s `SmsStatus` and dates as if they belonged to the phone.

**Rejected alternative:** Front Desk / people list columns in this slice. Optional later; person detail is the required surface.

**Not in this slice:** Front Desk / list columns, SuperAdmin/teller manual set of `SmsStatus`, recent `SmsLog` rows, WhatsAppStatus / GreenAPI `checkWhatsapp` (#255).

## Twilio status-callback auto-learn (fifth slice)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #254 (maintainer); this slice’s callback rules  
**Revisit when:** SuperAdmin/teller manual SmsStatus, setting OK from delivered, send-side SmsLog insert, or WhatsApp / GreenAPI #255

v3 already had one Twilio status callback: `PublicController.SmsStatus` → `TwilioHelper.LogSmsStatus` (update the existing `SmsLog` row by SID). v4 had the `SmsLog` table but no callback. This slice ports that **single** path to `POST /api/Public/smsStatus` and hooks auto-learn there. There is no second callback endpoint.

**SmsLog:** if a row exists for the SID (`MessageSid` or `SmsSid`), update `LastStatus`, `ErrorCode`, `LastDate`, and `Phone` (v3 `LogSmsStatus`). Do not insert a log row from a callback. v4 `PaidVerificationSender` still does not write `SmsLog` on send; auto-learn therefore does **not** require a log row — it uses Twilio `To`.

**Auto-learn write rule:**

```text
if MessageStatus is undelivered or failed
   AND ErrorCode is in the lasting-unusable allow-list
   AND an OnlineVoter row exists with VoterId matching To (see below) AND VoterIdType == "P"
   AND current SmsStatus is null or "OK"
→ SmsStatus = "twilio-{code}"
```

Allow-list (destination lastingly unusable): `30003` unreachable, `30005` unknown destination, `30006` landline/unreachable, `30004` filtered, `21211` invalid To, `21614` not a mobile. Mapped reason is always `twilio-{code}` (fits `varchar(50)`). Unlisted or missing `ErrorCode` on a terminal failure does **not** write (transient failures must not permanently block). `queued` / `sending` / `sent` / `delivered` never write. This slice does **not** set `SmsStatus` to `"OK"` from a delivered callback.

**Lookup:** Twilio `To` may be E.164 with `+` while `OnlineVoter.VoterId` is the Person phone as stored (with or without `+`). Auth still compares `Person.Phone == dto.VoterId` exactly. The callback tries the trimmed `To`, then the +/- variant. Lookup includes `VoterIdType == "P"` — not `VoterId` alone. A non-P occupant of a candidate `VoterId` is skipped (no convert, no wipe). If no matching P row exists, skip; do not create an `OnlineVoter` from a callback. Do not rewrite Person phones.

An existing block reason is left alone. A later selected hard failure may overwrite `"OK"`.

Logs: method + status/code only. No raw phone or other PII.

**Rejected alternative:** require an `SmsLog` row before learning. That would make auto-learn a no-op until send-side log inserts land.

**Rejected alternative:** a new dedicated auto-learn endpoint. The issue says to hook the existing status-callback / SmsLog path.

**Rejected alternative:** write `undeliverable` (or set `"OK"` on delivered) in this slice. The selected-code vocabulary is `twilio-{code}`; OK-from-delivered is a later choice.

**Not in this slice:** PaidDestinationPhone, the pre-send SmsStatus gate, `EnsureOnlineVoterForPhoneAsync`, Person UI, SuperAdmin set, SignalR #229, WhatsApp/GreenAPI #255, send-side SmsLog insert.

## Related

- [auth.md](auth.md) — voter JWT claims (separate from paid-destination rules)
- `docs/VOTER_AUTHENTICATION_IMPLEMENTATION.md` — requestCode flow and SMS pumping
- Issue #254 remaining work; #255 for WhatsApp / GreenAPI status parity
