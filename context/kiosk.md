# Kiosk voting

## Status: active

## Evidence: confirmed (TallyJ-3.0 `VoterCodeHelper.GenerateKioskCode` / `EditPerson` copy; issue #182)

Kiosk is the path for voters without email or phone. A teller enables it on Setup, mints a short code from the person record, and the voter types that code on a shared browser.

## Login window is 15 minutes and teller-renewed

**Status:** active  
**Evidence:** confirmed (v3 Setup: “Make or renew a Kiosk Code” / “Code will expire after 15 minutes.”; issue #182)

`POST /api/People/{guid}/generateKioskCode` mints a code if the person has none, or keeps the same `Person.KioskCode` and refreshes `OnlineVoter.VerifyCodeDate`. Direct kiosk login (`voterId === verifyCode`, including `K_` prefix) succeeds only while that stamp is less than 15 minutes old. Opening person details does not mint a code and does not start the clock.

v3’s login helper used a 10-minute constant while the UI said 15. v4 follows the UI and #182 (15 minutes).

**Rejected alternative:** mint on `GetPersonDetails` (the previous v4 behavior). Viewing a record would start a 15-minute window the teller did not intend to share.

**Rejected alternative:** treat a matching `Person.KioskCode` as a permanent login. Anyone who copied the letters could vote later from another machine.

**Rejected alternative:** generate a new letter-code on every renew. v3 reused the same code and only refreshed the window so the teller can read the same letters to the voter.

## Shared kiosk browser must not keep the previous voter

**Status:** active  
**Evidence:** confirmed (issue #182; v3 cookie was non-persistent)

Kiosk login uses the same httpOnly `voter_token` cookie as other online voters. After a non-draft kiosk submit the login window is closed (`VerifyCode` / `VerifyCodeDate` cleared) so the same code cannot open a **new** session. `Person.KioskCode` stays so the current JWT can still find the row (status / a same-request write). The ballot page then logs out and the confirmation page is a handoff (`?kiosk=1`) with no “back to my elections.”

**Rejected alternative:** set `Person.KioskCode` to empty on submit (v3 used that plus `VotingMethod`). v4 looks up the person by kiosk code for submit/status; clearing it would break an in-flight JWT. Ending the login window plus logout is what isolates the next voter.

**Rejected alternative:** set `VotingMethod = K` on kiosk submit (v3). v4 `HasAcceptedBallot` treats any `VotingMethod` as accepted, which would lock eligibility before Accept-all.

Generate/renew is refused after Finalized, after a Front Desk `VotingMethod`, after a consumed empty code, or after a Processed online ballot.

Kiosk `OnlineVoter` reads and stamps require `VoterIdType == C` (same as phone lookups require `P`). A matching `VoterId` on an email or phone row is not a kiosk session and must not be retagged.

v4 submit does not empty `Person.KioskCode`; empty is the v3 used-code sentinel only.

## Login window is election-scoped

**Status:** active  
**Evidence:** inferred (`IX_PersonKioskCode` is `(ElectionGuid, KioskCode)`; `OnlineVoter.VoterId` is globally unique)

`Person.KioskCode` letters may repeat across elections. The 15-minute window lives on an `OnlineVoter` row keyed `CODE.{electionGuid:N}` (type C). Mint/renew/submit in election A must not stamp or clear election B. Auth of the typed letters binds the single open window; if two open elections both have a live window for those letters, login is refused rather than picking `FirstOrDefault`.

**Rejected alternative:** keep one global `OnlineVoter` per letter-code. Two open elections with `SMART` would share and close each other’s window.

**Rejected alternative:** enforce global uniqueness of live letters at mint time. That would change the teller-facing code when another election already used it, and the person index is already per-election.

## Setup toggle writes `K` on `VotingMethods`

**Status:** active  
**Evidence:** inferred (v3 `VotingMethodsContains(Kiosk)`; v4 already gated person-form kiosk UI on that string)

The Setup switch adds or removes `K` (or the `KI` alias) without replacing the other tokens. Turning kiosk on also sets `UseOnlineVoting`, because kiosk auth uses the same open-window rule as email/phone.

## Related

- Auth (voter cookie)
- Online ballots
- People records
