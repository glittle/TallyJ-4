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

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: c4bb534c-48ce-49f7-b24f-beb5e0bde697 -->

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

Save to `{@artifacts_path}/plan.md`. If the feature is trivial and doesn't warrant this breakdown, keep the Implementation step below as is.

---

### [x] Step: Initialize SignalR in Election Pages
<!-- chat-id: 627985fc-8521-4ae7-a459-45371f9c9648 -->

Add SignalR initialization to election detail pages to enable real-time election updates.

- Modify ElectionDetailPage.vue to call electionStore.initializeSignalR() on mount
- Add joinElection(electionGuid) when entering election
- Add leaveElection when leaving/unmounting
- Verify election status changes are reflected in real-time

### [x] Step: Add Real-time Tally Progress
<!-- chat-id: f63f56f1-b138-4135-b064-45fe5bbed849 -->

Integrate real-time progress display in tally calculation page.

- Modify TallyCalculationPage.vue to initialize resultStore SignalR
- Add joinTallySession before starting calculation
- Display progress bar using tallyProgress from store
- Show percentage and status messages during calculation
- Test with actual tally calculation

### [x] Step: Enable Real-time People Updates
<!-- chat-id: 62de9818-74b4-4768-b40e-4379169ca0aa -->

Add live updates for people management.

- Modify PeopleManagementPage.vue to initialize peopleStore SignalR
- Join election group for people updates
- Verify people list updates when persons are added/edited/deleted in other sessions
- Handle real-time search results if applicable

### [ ] Step: Add Ballot Status Updates
<!-- chat-id: dcfd8e0f-60ce-410e-bd95-6704bdbd5044 -->

Enable real-time ballot entry status updates.

- Modify BallotEntryPage.vue to initialize ballotStore SignalR
- Join election group for ballot updates
- Display live ballot counts and status changes
- Test ballot creation/updates across multiple clients

### [ ] Step: Test Real-time Features

Perform comprehensive testing of all real-time features.

- Test tally progress across multiple browser tabs
- Test people updates in real-time
- Test election status synchronization
- Test SignalR reconnection after network issues
- Document any issues found

### [ ] Step: Run Tests and Linting

Ensure code quality and run all tests.

- Run frontend linting: npm run lint
- Run frontend type checking: npm run typecheck
- Run backend tests: dotnet test
- Fix any issues found
- Write implementation report to report.md
