# Teller reports

## VotersByArea 18+ / 18–21 use V01, not AgeGroup

**Status:** active  
**Evidence:** inferred (issue #185 names the columns; AgeGroup was removed — see [people.md](people.md))  
**Source:** issue #185 remaining checklist; TallyJ-3.0 `Site/Reports/VotersByArea.cshtml`  
**Revisit when:** a demographic age field is restored separately from eligibility

v3’s **Eligible and Voted by Area** table labeled the CanVote total **Adults**. It never had 18+ / 18–21 headers. AgeGroup (`A`/`Y`) is gone in v4. The only stored signal for “youth who can vote” is eligibility **V01** (“Youth aged 18/19/20”).

**Chosen:** keep the existing `VotersByArea` report. **18+** is every `CanVote` person (same population as v3 Adults / `TotalEligible`). **18–21** is the V01 subset. **Imported** stays a method column, shown when the election lists imported (`I` / `IM`) or any row has imported votes (`ShowImported`).

**Rejected alternative:** restore AgeGroup for these columns. Rejected — [people.md](people.md) already dropped that field; V01 and X05 are different rows.

**Rejected alternative:** treat 18+ as 21+ only (`CanReceiveVotes`). Rejected — the name would not match “18+”, and v3 Adults included V01 youth.

**Rejected alternative:** invent golden v3 totals for the report. Rejected — #168 comparison is parked; there are no known-good v3 packages here.

## One-click download is the existing report list as CSV zip

**Status:** active  
**Evidence:** inferred (issue #185 “if missing”; v3 Reports page is print + per-report CSV only)  
**Source:** issue #185; TallyJ-3.0 `Site/Views/After/Reports.cshtml`  
**Revisit when:** tellers need a single PDF of every report

v3 had no “download all”. v4 already had print and a separate advanced export (`ReportExportService`) that is not this teller family.

**Chosen:** `GET /api/Reports/{guid}/DownloadAll` returns a zip of one CSV per `GetAvailableReports` code. Same names and payloads as the on-screen reports, including Custom1/2/3 columns when those method names are set (same `v-if` rule as the Vue tables).

**Rejected alternative:** a new report family or the advanced PDF/Excel export. Rejected — #185 asked to test download-all if missing, not invent another catalog.

## Vote counts show `/ tie-break` only when a count was entered

**Status:** active  
**Evidence:** inferred (follows #198 unset vs explicit 0; #322 persist/analyze already shipped)  
**Source:** issue #198; [election-analysis.md](election-analysis.md)

Main and Votes-by-* reports used to append `" / " + TieBreakCount` whenever `TieBreakRequired` was true. After required ties stopped filling 0, unset became a dangling slash (C#) or the literal `"null"` (Vue). Download-all CSV already used empty for null and `"0"` for 0.

**Chosen:** format the suffix only when a count is present, including explicit 0. `HasTies` on the main report is `TieBreakRequired` on the elected/extra rows, not whether the display string contains `/`.

**Rejected alternative:** keep string-concatenating the nullable count. Rejected — it hid the unset vs 0 distinction that #198 asked for.

## Front Desk / ballots / analysis share one voted rule

**Status:** active  
**Evidence:** inferred (issue #185 “verify counts match”; Accept-all does not set `VotingMethod` or `RegistrationTime`)  
**Source:** issue #185; [ballot-validation.md](ballot-validation.md); [online-ballots.md](online-ballots.md)

Analysis `NumVoters`, VotersByArea `Voted`, and count reconciliation already use `VotingMethodCodes.HasVotedForCounts` (method **or** Processed online). Front Desk stats and `IsCheckedIn` used `RegistrationTime` only, so accepted online voters disappeared from the Front Desk total.

**Chosen:** Front Desk checked-in / registered uses the same `HasVotedForCounts` rule. Pending Submitted is not checked in.

**Rejected alternative:** set `RegistrationTime` / `VotingMethod=O` on Accept-all so the old Front Desk query would match. Rejected — online stays voter-initiated; Processed is the accepted record.
