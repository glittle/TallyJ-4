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

### [x] Step: Create ElectionAccessHandler
<!-- chat-id: b29726a4-1c27-4363-9098-71030843c07d -->
Create the ElectionAccessHandler authorization handler and ElectionAccessRequirement.
- Implement IAuthorizationHandler to check user access to elections
- Create ElectionAccessRequirement class
- Register handler as scoped service in Program.cs

### [x] Step: Update Controllers for Authorization
<!-- chat-id: 66dcccb1-f3bd-4246-8603-2fb2fe72022f -->
Modify ElectionsController to use the new authorization requirement.
- Add [Authorize(Policy = "ElectionAccess")] to relevant endpoints
- Ensure election GUID is available in route parameters

### [x] Step: Implement Test Database Seeding
<!-- chat-id: 129a4eeb-b5b3-4af4-8760-ef46a0f55bd4 -->
Create proper database seeding for integration tests.
- Modify CustomWebApplicationFactory to seed test data
- Ensure users and elections are created with proper relationships

### [x] Step: Switch to SQL Server LocalDB
<!-- chat-id: 04103e0e-99b6-4908-95a4-e84c192cb237 -->
Replace InMemory database with SQL Server LocalDB for integration tests.
- Update CustomWebApplicationFactory to use LocalDB
- Add Microsoft.EntityFrameworkCore.SqlServer package to tests
- Configure unique database names

### [ ] Step: Add Migration Testing
Create tests to verify database migrations work correctly.
- Add MigrationTests.cs with tests for migration application
- Verify seeded data is present after migrations

### [ ] Step: Run Tests and Verification
Execute all tests and verification steps.
- Run unit tests for ElectionAccessHandler
- Run integration tests with LocalDB
- Run migration tests
- Run lint and typecheck commands
- Write implementation report to report.md
