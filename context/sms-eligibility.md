# Paid SMS / voice / WhatsApp destination eligibility

## Status: active

## Evidence: confirmed

**Source:** issue #254 (maintainer); July 555-range send incident described there  
**Revisit when:** later #254 slices land (`OnlineVoter.SmsStatus`, Person UI, Twilio callback auto-learn), or NANP reserved-range rules change

## In-code gate before any paid provider

Paid verification (SMS / voice / WhatsApp) must not call Twilio or GreenAPI for reserved, fictional, or malformed destinations. The July incident was ~80 attempts to 555-style numbers that still incurred tiny Twilio charges — the existing “registered in an open election” check does not stop an election owner from loading those numbers and opening the window.

**This slice (issue #254, first cut):** a pure in-code helper (`PaidDestinationPhone`) rejects:

- malformed E.164 (and the same digit string without a leading `+`)
- NANP area code `555`
- NANP exchange `555` (including the `555-01xx` fictional block)

The check runs at the start of `RequestVerificationCodeAsync` for phone + paid delivery, and again inside `PaidVerificationSender` so a future caller cannot bypass it. Email, OAuth, and kiosk paths are unchanged. `WhenRegistered` is not written for a rejected number.

**Rejected alternative:** rely only on the open-election registration check. That stops outsiders requesting codes for arbitrary numbers; it does not stop fictional numbers that are already on a Person row.

**Rejected alternative:** wait for `OnlineVoter.SmsStatus` (later on the same issue). Status needs a row and a reason vocabulary. 555 / malformed must be blocked with no database work.

**Rejected alternative:** reject only `555-01xx` and allow other 555 NPA/NXX. The incident and the issue call out area code 555 and exchange 555, with `555-01xx` as the most important subset.

**Not in this slice:** `OnlineVoter.SmsStatus`, migrations, Person UI, Twilio status-callback learning, country allow-lists, spend caps.

## Related

- [auth.md](auth.md) — voter JWT claims (separate from paid-destination rules)
- `docs/VOTER_AUTHENTICATION_IMPLEMENTATION.md` — requestCode flow and SMS pumping
- Issue #254 remaining work; #255 for WhatsApp / GreenAPI status parity
