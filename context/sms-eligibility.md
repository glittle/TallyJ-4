# Paid SMS / voice / WhatsApp destination eligibility

## Status: active

## Evidence: confirmed

**Source:** issue #254 (maintainer); July 555-range send incident described there  
**Revisit when:** later #254 slices land (Person UI, EnsureOnlineVoterForPhoneAsync, Twilio callback auto-learn), or NANP reserved-range rules change

## In-code gate before any paid provider

Paid verification (SMS / voice / WhatsApp) must not call Twilio or GreenAPI for reserved, fictional, or malformed destinations. The July incident was ~80 attempts to 555-style numbers that still incurred tiny Twilio charges — the existing “registered in an open election” check does not stop an election owner from loading those numbers and opening the window.

**First slice (issue #254):** a pure in-code helper (`PaidDestinationPhone`) rejects:

- malformed E.164 (and the same digit string without a leading `+`)
- NANP area code `555`
- NANP exchange `555` (including the `555-01xx` fictional block)

The check runs at the start of `RequestVerificationCodeAsync` for phone + paid delivery, and again inside `PaidVerificationSender` so a future caller cannot bypass it. Email, OAuth, and kiosk paths are unchanged. `WhenRegistered` is not written for a rejected number.

**Rejected alternative:** rely only on the open-election registration check. That stops outsiders requesting codes for arbitrary numbers; it does not stop fictional numbers that are already on a Person row.

**Rejected alternative:** wait for `OnlineVoter.SmsStatus` (this file’s next section). Status needs a row and a reason vocabulary. 555 / malformed must be blocked with no database work.

**Rejected alternative:** reject only `555-01xx` and allow other 555 NPA/NXX. The incident and the issue call out area code 555 and exchange 555, with `555-01xx` as the most important subset.

## Durable `OnlineVoter.SmsStatus` (second slice)

`OnlineVoter` is the global phone-status store (key remains `VoterId`, E.164 when `VoterIdType` is `"P"`). Person is per-election; the same phone can appear in many elections, so eligibility cannot live only on Person.

Field: `string? SmsStatus`, max 50, non-unicode (`varchar(50)`).

**Send rule (paid channels only):**

```text
if SmsStatus is not null AND SmsStatus != "OK" → do not send
```

| `SmsStatus` | Meaning | Send SMS/voice/WhatsApp? |
|-------------|---------|---------------------------|
| `null` | Not yet checked | Yes (current behaviour) |
| `"OK"` | Checked, valid | Yes |
| anything else | Blocked; value is the reason | **No** |

Initial reason vocabulary (not a closed enum — short phrases/codes): `555-range`, `undeliverable`, `landline`, `premium`, `admin`, `twilio-30003`, plus in-code `malformed-e164`.

Pre-Twilio gate order in `RequestVerificationCodeAsync` (issue #254):

1. `PaidDestinationPhone` (in-code; no DB)
2. `OnlineVoter.SmsStatus` when a row already exists and status is not null / not `"OK"` (paid channels only; do not create a row just to store status)
3. Existing open-election registration check (unchanged)
4. Then the provider

Skip logs method + status only (no raw phone or email). Voter-facing message reuses `voting.auth.requestCode.invalidPhone`. Email / OAuth / kiosk unchanged. `WhenRegistered` semantics unchanged.

**Rejected alternative (this slice):** rename the column to `Status` so email/kiosk rows could share it later. The issue field spec and this slice’s contract are `SmsStatus`; a generic rename can be a later migration if other identifier types need a status.

**Rejected alternative:** persist the in-code `PaidDestinationPhone` reason onto an existing `OnlineVoter` row when the format gate rejects. Useful only when a row already exists; creating a row here would pull in `EnsureOnlineVoterForPhoneAsync` (later slice). Not done.

**Not in this slice:** Person UI / Front Desk display, `EnsureOnlineVoterForPhoneAsync` on Person create/import/update, Twilio status-callback auto-learn, WhatsAppStatus / GreenAPI `checkWhatsapp` (#255).

## Related

- [auth.md](auth.md) — voter JWT claims (separate from paid-destination rules)
- `docs/VOTER_AUTHENTICATION_IMPLEMENTATION.md` — requestCode flow and SMS pumping
- Issue #254 remaining work; #255 for WhatsApp / GreenAPI status parity
