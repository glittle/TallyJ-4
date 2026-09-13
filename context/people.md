# People records

## AgeGroup is not stored

**Status:** active  
**Evidence:** confirmed  
**Source:** maintainer decision after review of eligibility vs leftover v2/v3 metadata  
**Revisit when:** someone proposes restoring a demographic age field separate from eligibility

`Person.AgeGroup` (`A`/`Y`) was a v2/v3 column carried into v4. It did not drive voting or candidacy. Youth who can vote but cannot be elected use eligibility reason **V01** (“Youth aged 18/19/20”). Under-18 uses **X05**. The person form had both controls and they were never synced.

The column, person DTOs, form dropdown, and unused turnout-by-age breakdown were removed. Incoming v2 XML / JSON packages may still contain `AgeGroup`; it is ignored so old files keep importing.

**Rejected alternative:** keep the column for turnout reports. The only consumer grouped `HasOnlineBallot` and the UI never showed it.

**Rejected alternative:** derive V01 from Age Group = Youth. Adult youth (18–20) and under-18 are different eligibility rows; a two-value age flag cannot express that.

## Person eligibility is stored as a short code

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #263  
**Revisit when:** a new reason is added, or an incoming package format no longer carries GUIDs

`Person` stores `IneligibleReasonCode` (`X01`, `V04`, …). Empty/null means fully eligible. Votes already stored the same short code.

GUIDs stay on `IneligibleReasonEnum` only so old JSON packages and v2/v3 XML can still be imported. Incoming files may send a code or a GUID (including legacy v3 sub-reason GUIDs); only the code is persisted. New package export writes the code.

**Rejected alternative:** keep `IneligibleReasonGuid` on Person and derive the code at the API. Create/update, name import, and the person form all used the code already; the GUID column was leftover v3 storage.

**Rejected alternative:** store both columns. Two sources of truth would drift, and the index would still need the GUID.

## Cannot mark cannot-vote after a ballot is accepted

**Status:** active  
**Evidence:** confirmed (v3 `EditPerson.updateReasons`; issue #171)  
**Source:** TallyJ-3.0 `Site/Views/Setup/EditPerson.cshtml.js` (`updateReasons(!!VotingMethod)`); v4 write gate in `PeopleService.UpdatePersonAsync`  
**Revisit when:** pending online ballots (not yet accepted) should also lock eligibility, or paper “accepted” should mean a `Ballot` row instead of Front Desk `VotingMethod`

v3 disabled eligibility options with `CanVote === false` once the person had a voting method. The person-form tip said they cannot change to a non-voting option after voting. v4 only had the Finalized write lock (#308) — tellers could still set X/R reasons after check-in or Accept-all.

`HasAcceptedBallot` is Front Desk `VotingMethod` (the paper/mail/call-in record that a ballot was received) or an `OnlineVotingInfo` row with status `Processed` (Accept-all). `Person.HasOnlineBallot` is set on the first online write (Draft autosave or Submitted) and is not the accepted signal. Pending `Submitted` / `Processing` rows do not lock. Draft is also not accepted. V-group reasons (can vote, cannot receive) stay allowed so votes they already received can still be spoiled.

The API throws `people.cannotMarkCannotVoteAfterVoted` before copying fields. Person detail exposes the combined `HasAcceptedBallot` flag and the latest `OnlineBallotStatus` (Draft / Submitted / Processing / Processed) so tellers can see pending vs accepted online on the person form. The person form disables X/R from `HasAcceptedBallot`, not from `hasOnlineBallot`. Finalized still wins first. Front Desk shows the same per-person online status; the monitor stays counts-only.

**Rejected alternative:** UI-only disable, matching v3. Rejected — #171 asked to verify the status cannot be changed; a write gate matches other Front Desk locks.

**Rejected alternative:** treat `Person.HasOnlineBallot` as the online half of “accepted”. Rejected — that flag is set on the first online write (including Draft), not Accept-all. Accepted vs pending is `OnlineVotingInfo.Status` (`Processed` vs `Submitted` / `Processing`). Paper check-in still uses `VotingMethod`.

## Guest tellers add people only when Can Add People is on

**Status:** active  
**Evidence:** confirmed (v3 ExtraSetting GA / `Election.GuestTellersCanAddPeople`; issue #186)  
**Source:** TallyJ-3.0 `PeopleModel.SavePerson`, `BallotNormal.cshtml.js` `prepareReasons`, Setup “Can Add People?”  
**Revisit when:** a logged-in assistant role is added that is neither guest nor full teller

v3 had no special “Name not in the List” spoil GUID. The ballot spoiled-reason dropdown started with an optgroup **Name not in the List**: either “Add new name (including spoiled)” when `GuestTellersCanAddPeople` was on, or “(Ask head teller to add required name)” when it was off. Adding an eligible person is a valid vote. Known/full tellers could always add. Guests could add only when that election flag was on (default off).

v4 already had `BallotAddPersonPanel` (U01 / U02 / create person). The missing product pieces were the election switch (next to the access code, same as v3), the guest write gate on `PeopleService.CreatePersonAsync` (`people.guestCannotAddPeople`), and the v3 labels on ballot entry.

**Rejected alternative:** a dedicated spoil reason or vote type for “name not in the list”. Rejected — v3 treated it as adding a person, not as a reason GUID.

**Rejected alternative:** let guests add people whenever they can enter ballots. Rejected — v3 defaulted GA to false so a logged-in teller had to add new names.

## Confidential voters are ordinary people named Confidential X

**Status:** active  
**Evidence:** confirmed (issue #186; no Confidential feature in v3 code)  
**Source:** v3 convention (Add New Person + Front Desk + Analyze SaveManual Eligible Voters)  
**Revisit when:** a jurisdiction asks for a first-class anonymous-voter type

There is no Confidential person type. Tellers add `Confidential 1`, `Confidential 2`, and so on as normal eligible people, check them in, and if the listed eligible count should stay as it was, they override **Eligible Voters** on Analyze (`ResultType` `M`). See [election-analysis.md](election-analysis.md).

**Rejected alternative:** an Add Confidential button or auto-numbered Confidential N. Rejected — v3 never had that product.
