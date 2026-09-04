# Ballot Validation & Pre-Finalization Integrity

## Status: active
## Evidence: confirmed (issues #189 / #190)

Validation must catch problems *before* analysis or finalization. Catching errors only at report time is too late.

### Key constraint
Once an election moves past certain states, correcting bad ballot data becomes expensive or impossible without breaking the audit trail.

### Design posture
Prefer explicit failure and clear recovery paths over silent best-effort acceptance of questionable ballots.

### Related
- Front Desk flows
- Online ballot acceptance
- Election analysis engine

## Count reconciliation report (issue #190)

**Status:** active  
**Evidence:** inferred (issue #190 remaining items + current v4 data model; v3 Reconcile page is not in this repo)

Tellers need **which rows** do not reconcile before Analyze and Finalize — not only the existing finalization summary blockers (missing analysis, outstanding/review ballots, unresolved ties). Those gates stay. This report is a live count check, not a second readiness system: `ElectionCountReconciliation` is called from the same Analyze/Finalize path (`TallyService.Calculate*` and `ElectionStageFinalizationReadiness`).

### What v4 can identify as rows

- **Pending online:** each `OnlineVotingInfo` in `Submitted` or `Processing`, named from `Person`. Processed rows are not pending (acceptance is not reversible; do not re-link accepted ballots).
- **Duplicate envelope number:** two or more people share `EnvNum`.
- **Duplicate voting path:** a paper/imported Front Desk method (`P`/`M`/`D`/`C`/`I`/`1`/`2`/`3`) plus a pending or Processed online row for the same person.

### Front Desk vs ballot count

Paper and accepted-online ballots have no `PersonGuid`. v4 cannot name which paper voter lacks a ballot. When residual counts differ, the report emits one `FrontDeskVsBallots` row with the two totals.

Front Desk side (accounted): people with a voting method, or a **Processed** online row. v4 submit/accept does not set `VotingMethod`, so Processed is the person-level record that an accepted online vote exists. Pending online people are excluded from that count (they have no `Ballot` yet) and appear as their own rows.

Ballot side: every entered ballot, **including spoiled** (`StatusCode != Ok`). Analysis already uses `BallotsReceived + SpoiledBallots` vs envelope totals for `UseOnReports`. Excluding spoiled from this check would invent a false Front Desk mismatch.

### What does not apply as a count-reconciliation row

- **`BallotStatus.Dup`:** duplicate names on one ballot. That is ballot quality; those ballots are spoiled (`Status != Ok`) and already visible on the ballot list. Not a voter-vs-ballot identity match.
- **Spoiled ballots as their own mismatch type:** spoiled exclusion is a **rule** (include them in the entered-ballot total), not a list of “wrong” ballots. A spoiled ballot that matches a Front Desk registration is reconciled.
- **Reconnecting accepted online ballots to `OnlineVotingInfo`:** `BallotGuid` is wiped on Accept-all. The report must not invent that link.

**Rejected alternative:** a parallel “readiness v2” API unused by Analyze/Finalize. Rejected — tellers would see a report that does not actually gate those actions.

**Rejected alternative:** treat pending online as Processed for the Front Desk count. Rejected — pending rows are not ballots until Accept-all.

**Reason:** issue #190 asked for a teller-visible row-level report that blocks Analyze/Finalize when counts do not reconcile, using the existing readiness path.
