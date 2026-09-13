# Front Desk

## Ballot Not Received is a hide-received filter

**Status:** active  
**Evidence:** confirmed (v3 Front Desk `#ifNoBallot`)  
**Source:** TallyJ-3.0 `Site/Views/Before/FrontDesk.cshtml` + `.cshtml.less` (`.NoBallot .Voter:not(.VM-)`)  
**Revisit when:** election setup exposes checklist flags as first-class mailed-ballot tracking

v3’s **Ballot Not Received** checkbox hid everyone whose voting method was set (ballot received / recorded). Combined with a checklist/flag, that showed who was flagged (for example mailed a ballot) but still had no method.

v4 already had All / Not registered / Registered radios and method/flag filters. Those cover the same workflow when the default is Not registered, but they did not name the v3 control or apply it on the All list.

The checkbox is an extra filter on the current list: hide rows with a voting method. Flag filters still apply. Clearing filters turns it off.

## Front Desk method codes and mixed-method switch

**Status:** active  
**Evidence:** inferred (issue #194; Person.VotingMethod varchar(1); reports/analyzer already used P/M/D/O/K/I)  
**Revisit when:** election setup exposes VotingMethods as first-class checkboxes instead of a string

`Person.VotingMethod` is a single letter: **P** in person, **M** mailed, **D** dropped off, **C** called in, **O** online, **K** kiosk, **I** imported, **1/2/3** custom. Election setup may store the same letters concatenated (`PMD`) or comma-separated aliases (`IP,OL`). Front Desk buttons come from that string except **O** — Online is voter-initiated, so tellers cannot check anyone in as Online (that would count a Front Desk registration with no ballot). Empty setup defaults to P/M/D. The English word “In Person” is not code `I` — `I` is Imported.

Online ballots stay voter-initiated. Tellers do not create ballots at the Online location. Checking in with a method other than Online while the person has a **Draft** or **Submitted** online row discards that pending row so Accept-all cannot create a second ballot. **Processed** (and **Processing**) online already means they have an accepted/claimed online ballot — Front Desk refuses another method. Draft is not Accept-all pending and does not lock cannot-vote.

**Rejected alternative:** let tellers record `O` at Front Desk when election setup lists Online. Rejected — that marks the person as voted with no Ballot, breaks Front Desk vs ballots reconciliation, and contradicts voter-initiated online.

**Rejected alternative:** keep Front Desk writing `I` for in person. Rejected — reports and analysis already count `I` as Imported and `P` as In Person.

**Rejected alternative:** leave a Submitted row in place after an in-person/mail/kiosk check-in. Rejected — Analyze stays blocked while any live Submitted exists, and Accept-all could mint a second ballot.

**Reason:** mixed-method elections need the same letters everywhere, and a switch of method must not count two ballots.

#186 names this as “online then votes in person.” Draft or Submitted is withdrawn and the Front Desk method is recorded. Processing refuses (`frontDesk.errors.alreadyProcessingOnline`). Processed refuses (`frontDesk.errors.alreadyAcceptedOnline`). Covered in `Issue186OnlineThenInPersonTests`.

**Rejected alternative:** treat the Not registered radio as enough and skip the named checkbox. Rejected — #171 names the control, and operators combine it with flags on the full list.

## Roll Call and envelope pages are not v4 product pages

**Status:** active  
**Evidence:** inferred (v3 menu vs v4 routes; `docs/Hubs-v3-vs-v4.md`)  
**Source:** TallyJ-3.0 `Site/Views/menu.xml` (Roll Call / Sort Envelopes require `BallotProcess=Roll`); v4 has no `BallotProcess` setting  
**Revisit when:** a projector roll-call display or envelope-sort workflow is requested

v3 Roll Call and Sort Envelopes existed only for the Roll ballot process. Count Envelopes was `Ballots/Reconcile`. v4 has no Roll process, no Front Desk roll-call or envelope-count routes, and **RollCallHub is deferred**.

`GET .../frontdesk/rollCall` is a leftover that returns eligible voters plus check-in stats. Envelope numbers live on Front Desk (`ENABLE_ENVELOPE_NUMBERS` is still off). Envelope *counting* is the Analyze count-reconciliation report, not a Gathering page.

**Rejected alternative:** rebuild the v3 projector and envelope pages for #171. Rejected — they depended on a process v4 does not have; the remaining API is covered by tests, not a new UI.

## Front Desk checked-in count matches analysis voted

**Status:** active  
**Evidence:** inferred (issue #185 count-match leftover)  
**Source:** issue #185; [reports.md](reports.md)

Header stats and `IsCheckedIn` used `RegistrationTime` only. Accept-all does not set that field, so processed online voters were missing from Front Desk while analysis and reconciliation counted them.

**Chosen:** checked-in = `HasVotedForCounts` (recorded method or Processed online). Same rule as analysis `NumVoters` and VotersByArea `Voted`.

**Rejected alternative:** leave Front Desk on `RegistrationTime` and only document the difference. Rejected — #185 asked the three surfaces to match.

Unregister (and other desk-registration undo) stays on `RegistrationTime`. Accept-all does not create a Front Desk registration, so the overlay must not offer Unregister for Processed-only rows.

**Rejected alternative:** let Unregister clear Processed online. Rejected — `UnregisterVoterAsync` still requires `RegistrationTime`; online acceptance is not a desk check-in.

## Repeatable desk method while registration is open

**Status:** active  
**Evidence:** confirmed (Glen, issue #336, 2026-09-13)  
**Source:** issue #336 after #186 / #334 / #335  
**Revisit when:** tellers need in-place method buttons without Unregister

A pending online row (Draft / Submitted) can be superseded by a desk method. Check-in still withdraws that row (same #186 path). After that, the teller may change the desk method until registration is done.

**Chosen:** keep Unregister + re-check-in. The overlay already offers Unregister when `RegistrationTime` is set, hides method buttons while checked in, and shows them again after Unregister. A second check-in records the new method. No in-place “change method” API.

The withdrawn online row is deleted. Unregister clears only the desk fields; it does not recreate the row. Accept-all therefore has nothing to process for that person, including after a later Unregister or a second method. There is no separate process-one endpoint — Accept-all is the only accept path.

Processing / Processed stay refused. Unregister is still not offered without `RegistrationTime`.

**Rejected alternative:** add in-place method-change buttons on the checked-in overlay. Rejected — Unregister + re-check-in already updates the choice and is offered while registration is open.

**Rejected alternative:** invent a new “registration closed” flag or lock Front Desk when leaving Gathering. Rejected — see below.

## Registration-done is the Finalized write lock

**Status:** active  
**Evidence:** confirmed (existing `ElectionFinalizedWriteGuard`; Glen, issue #336)  
**Source:** [election-state.md](election-state.md); `FrontDeskService` check-in / Unregister / flags / envelope  

There is no separate “end registration” switch. Head tellers already lock people and ballot writes by advancing to **Finalized** (`ElectionFinalizedWriteGuard`). Check-in and Unregister already throw `elections.finalizedWriteBlocked`. The overlay uses that same stage: method, Unregister, and flag writes are not actionable while `electionStage === Finalized`. The notice reuses `elections.finalizedWriteBlocked` (same phrase as the API).

Leaving Finalized (confirmed FullTeller stage change) is what reopens those writes. Gathering / Processing / SettingUp do not lock Front Desk.

**Rejected alternative:** a second lock that closes Front Desk when the election leaves GatheringBallots. Rejected — #336 asked to reuse an existing gate; Finalized already is that lock.

**Reason:** one write lock for people/ballot mutations, including changing how someone registered.

## Accept-all vs Front Desk same-moment race

**Status:** active  
**Evidence:** confirmed (issue #336 second slice; SQLite `Issue336AcceptAllFrontDeskRaceTests`)  
**Source:** issue #336 leftover after #337  
**Revisit when:** Accept-all or check-in grows a third writer of `OnlineVotingInfo.Status`

Committed order already had one counted vote: check-in first withdraws Draft/Submitted and Accept-all skips; Accept-all first to **Processing** / **Processed** and Front Desk refuses (`alreadyProcessingOnline` / `alreadyAcceptedOnline`); pass 2 drops the row if a desk method is already recorded; withdraw + Unregister leaves Accept-all nothing.

The leftover hole was a Front Desk `DbContext` that had already tracked **Submitted**. EF identity resolution keeps that Status after another context claims **Processing** or writes **Processed**. Check-in then deleted the claimed/processed row and wrote a desk method — In Person plus an Online ballot.

**Chosen:** no new product lock. Check-in re-reads Status from the database (not the tracker) and withdraws only with `DELETE … WHERE Status IN (Draft, Submitted)`. 0 rows and the row is now Processing/Processed still throws the existing keys, so a desk method is not recorded on top of a newly Processed online ballot. Accept-all is unchanged.

**Rejected alternative:** a new election-scoped lock that serializes Accept-all with Front Desk. Rejected — #336 said not to invent a product lock; Status compare-and-swap on the existing withdraw/claim is enough.

**Rejected alternative:** tests-only and leave the stale-context write. Rejected — SQLite proved check-in would record a second counted method after Accept-all had already claimed or processed.

**Reason:** one counted vote per person. The desk method wins only while the online row is still Draft or Submitted.

## Front Desk SMS column is the phone P-row hint

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #254 leftover; [sms-eligibility.md](sms-eligibility.md) tenth slice  
**Revisit when:** Front Desk should open person detail for Set OK / Block

Eligible-voter rows include a compact `PhoneOnlineVoter` hint (never-seen / imported / OK / block reason) for people who have a phone. Lookup is P-row scoped; a non-P occupant’s status is not shown. The raw phone is not added to the Front Desk DTO. Set OK / Block stays on person detail — row-click is still check-in.

**Rejected alternative:** inline SMS edits on Front Desk. Person detail already has that action; Front Desk is check-in.
