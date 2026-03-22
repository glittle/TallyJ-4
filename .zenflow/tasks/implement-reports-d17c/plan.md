# Auto

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Implementation
<!-- chat-id: e9c5475e-49f1-4c0f-b447-cd041d14eca6 -->

Replace all v4 reports with v3-equivalent reports. V3 had 21 report types across two categories (Ballot Reports and Voter Reports). V4's existing reports (chart-based, advanced filtering, comparison) are unrelated to v3 and will be removed.

**Architecture:**
- Backend: New `ReportsController` + `IReportService`/`ReportService` with one endpoint per report, returning typed DTOs
- Frontend: New `ReportsPage.vue` with sidebar chooser + report panel, individual report sub-components
- Print CSS for clean printing

**V3 Reports to implement:**

Ballot Reports:
1. Main (Election Summary) - elected candidates, statistics, spoiled info
2. VotesByNum - Tellers' Report by vote count
3. VotesByName - Tellers' Report alphabetical
4. Ballots - All ballots for review
5. BallotsOnline - Online ballots only (conditional)
6. BallotsImported - Imported ballots only (conditional)
7. BallotsTied - Ballots containing tied candidates
8. SpoiledVotes - Spoiled votes on valid ballots
9. BallotAlignment - Ballot alignment to results
10. BallotsSame - Duplicate ballots
11. BallotsSummary - Ballots summary with tellers

Voter Reports:
12. AllCanReceive - People eligible to receive votes
13. Voters - Participation (who voted and how)
14. Flags - Attendance checklists
15. VotersOnline - Online voters (conditional)
16. VotersByArea - Eligible/voted by area
17. VotersByLocation - Voting method by venue (conditional)
18. VotersByLocationArea - Attendance by venue (conditional)
19. ChangedPeople - Updated people records
20. AllNonEligible - People with eligibility status
21. VoterEmails - Email & phone list (conditional)

- [x] Investigation and planning
- [x] Backend: DTOs, service interface, service implementation, controller
- [x] Frontend: Remove old reports, create new ReportsPage + report components
- [x] Locale strings (English only), print styles
- [x] Build and verify

### [x] Step: Review Location detiails
<!-- chat-id: 3eb15a1d-1c19-41db-9a87-28726a2debfb -->

Ballot locations are mostly random, added by admins in an election. However, there are two special "locations" - "Online" and "Imported". Those are automatic locations enabled by settings in the Election setup page.  The current code relies on the visible name to identify these locations. In a multilingual project, that's not a good idea. We should add an enum for LocationType with Manual, Online, Imported as the possible answers.   If that sounds good, let's do that. I can add an EF migration after.
