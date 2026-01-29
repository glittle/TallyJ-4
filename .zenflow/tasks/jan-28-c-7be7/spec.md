# Technical Specification: TallyJ 4 Real-time Features Implementation

## Technical Context
- **Backend**: ASP.NET Core Web API (.NET 10.0) with SignalR hubs for real-time communication
- **Frontend**: Vue 3 + TypeScript with Microsoft SignalR client
- **Database**: SQL Server with Entity Framework Core
- **Current Status**: Backend SignalR infrastructure exists but frontend integration is incomplete

## Implementation Approach
Based on the review of previous tasks, focus on fixing critical SignalR integration issues and implementing missing real-time features. Follow established patterns from the codebase:
- Use existing Pinia stores for state management
- Leverage signalrService.ts for connection management
- Implement event-driven updates in stores
- Maintain consistent error handling and logging

## Source Code Structure Changes
### Frontend Changes
- **src/stores/resultStore.ts**: Fix event name casing for tally progress updates
- **src/stores/ballotStore.ts**: Add SignalR integration for ballot status updates
- **src/pages/results/TallyCalculationPage.vue**: Ensure proper SignalR initialization
- **src/pages/ballots/BallotEntryPage.vue**: Create new page with real-time ballot updates

### Backend Changes
- **Services/SignalRNotificationService.cs**: Update group naming to match frontend expectations
- **Hubs/AnalyzeHub.cs**: Ensure consistent event name casing

## Data Model / API / Interface Changes
- **SignalR Events**: Standardize event names between backend and frontend (lowercase)
- **Group Names**: Align group naming conventions (e.g., "Analyze{electionGuid}")
- **No database schema changes required**

## Verification Approach
- Unit tests for SignalR event handling
- Manual testing with multiple browser sessions
- Run `dotnet test` for backend tests
- Run `npm run test` for frontend tests
- Verify real-time updates work across clients</content>
</xai:function_call">### Step: Technical Specification

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

### [ ] Step: Implementation

Implement the task according to the technical specification and general engineering best practices.

1. Break the task into steps where possible.
2. Implement the required changes in the codebase.
3. Add and run relevant tests and linters.
4. Perform basic manual verification if applicable.
5. After completion, write a report to `{@artifacts_path}/report.md` describing:
   - What was implemented
   - How the solution was tested
   - The biggest issues or challenges encountered