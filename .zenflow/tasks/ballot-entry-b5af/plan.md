# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

If you are blocked and need user clarification, mark the current step with `[!]` in plan.md before stopping.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: 92a09eab-71fd-44b8-966a-50652db3e9c5 -->

Assess the task's difficulty, as underestimating it leads to poor outcomes.
- easy: Straightforward implementation, trivial bug fix or feature
- medium: Moderate complexity, some edge cases or caveats to consider
- hard: Complex logic, many caveats, architectural considerations, or high-risk changes

Create a technical specification for the task that is appropriate for the complexity level:
- Review the existing codebase architecture and identify reusable components.
- Define the implementation approach based on established patterns in the project.
- Identify all source code files that will be created or modified.
- Define any necessary data model, API, or interface changes.
- Describe verification steps using the project's test and lint commands.

Save the output to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach
- Source code structure changes
- Data model / API / interface changes
- Verification approach

If the task is complex enough, create a detailed implementation plan based on `{@artifacts_path}/spec.md`:
- Break down the work into concrete tasks (incrementable, testable milestones)
- Each task should reference relevant contracts and include verification steps
- Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint, write tests for a module). Avoid steps that are too granular (single function).

Important: unit tests must be part of each implementation task, not separate tasks. Each task should implement the code and its tests together, if relevant.

Save to `{@artifacts_path}/plan.md`. If the feature is trivial and doesn't warrant this breakdown, keep the Implementation step below as is.

---

### [x] Step: Backend – Spoiled Votes + All-People Endpoint
<!-- chat-id: 198c2c0b-5ecc-4b7e-94c1-e34f16f7f585 -->

Backend changes for ineligible-person handling and the new ballot-entry people endpoint.

- Add `PersonVoteCountUpdateDto` SignalR DTO (`backend/DTOs/SignalR/PersonVoteCountUpdateDto.cs`)
- Add `SendPersonVoteCountUpdateAsync` to `ISignalRNotificationService` and implement in `SignalRNotificationService`
- Add `GetAllForBallotEntryAsync` to `IPeopleService` + implement in `PeopleService` (all people, live vote count from Vote table)
- Add `GET {electionGuid}/getAllForBallotEntry` endpoint to `PeopleController`
- Modify `VoteService.CreateVoteAsync`: remove `CanReceiveVotes` exception; if ineligible, set statusCode from `IneligibleReasonCode`; inject `ISignalRNotificationService` and broadcast live vote count after create/delete
- Write xUnit tests: spoiled vote creation, live vote count query, SignalR broadcast on create/delete
- Run `dotnet build && dotnet test`

---

### [ ] Step: Frontend – People Store + Search Sorting
<!-- chat-id: 3b474de5-aac8-4612-9ec9-020f22322c2f -->

Update the people store and search logic so the candidate list includes all people and sorts by live vote count.

- Add `getAllForBallotEntry` method to `peopleService.ts`
- Update `peopleStore.initializeCandidateCache` to call `getAllForBallotEntry` instead of `getCandidates`
- Add `PersonVoteCountUpdateEvent` to `frontend/src/types/SignalREvents.ts`
- Add `handlePersonVoteCountUpdated` to `peopleStore`; wire it up in `initializeSignalR` to listen for `PersonVoteCountUpdated` hub event
- Update sort comparator in `usePersonSearch.ts`: primary `voteCount` desc, secondary relevance weight desc
- Write/update Vitest tests for `usePersonSearch` sort order and `peopleStore` vote count handler
- Run `npx vue-tsc --noEmit && npm run test`

---

### [ ] Step: Frontend – VoteEntryRow UX + Ineligible Handling

Improve VoteEntryRow to display vote counts, handle ineligible persons, and verify keyboard navigation.

- Update autocomplete item template in `VoteEntryRow.vue` to show `voteCount` badge
- Style ineligible persons differently (e.g., warning colour, eligibility code shown)
- On person selection: if `canReceiveVotes !== true`, set `statusCode = person.ineligibleReasonCode` in emitted VoteDto
- Verify arrow-key / Enter keyboard navigation works end-to-end with `el-autocomplete`
- Update `InlineBallotEntry.vue` to pass statusCode through the emitted vote
- Update `BallotEntryPage.vue` to pass statusCode to `CreateVoteDto`; show distinct success message for spoiled votes
- Add i18n keys (`ballots.voteSpoiledSuccess`, `ballots.ineligible`) to `en/ballots.json` and `fr/ballots.json`
- Run `npx vue-tsc --noEmit && npm run test`

---

### [ ] Step: Verification + Report

End-to-end verification and documentation.

- Manual smoke test: ballot entry page loads all people, ineligible person adds a spoiled vote, second browser sees updated vote count via SignalR
- Run full test suite: `dotnet test` + `npm run test`
- Write `{@artifacts_path}/report.md` describing implementation, testing, and challenges
