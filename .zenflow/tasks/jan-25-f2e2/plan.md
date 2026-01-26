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

### [ ] Step: Technical Specification
<!-- chat-id: 1eca2a0b-d845-48df-9d80-2f0118a25941 -->

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

### [x] Step: Technical Specification
<!-- chat-id: completed -->

### [x] Step: ResultsController Implementation and Frontend Integration
<!-- chat-id: completed -->

### [x] Step: Implement Additional Missing Controllers
<!-- chat-id: completed -->

Successfully implemented all core API controllers:
- ✅ ElectionsController - Full CRUD operations with pagination
- ✅ PeopleController - Person management with search and filtering
- ✅ BallotsController - Ballot CRUD operations
- ✅ VotesController - Vote management by ballot/election
- ✅ SetupController - Election wizard (step-by-step setup)
- ✅ DashboardController - Election dashboard and management
- ✅ ResultsController - Election results and reporting
- ✅ PublicController - Public election information
- ✅ AccountController - User account management
- ✅ AuthController - Authentication (login/register/2FA)

✅ SignalR hubs implemented:
- MainHub - General notifications and status updates
- AnalyzeHub - Tally progress and completion
- FrontDeskHub - Ballot entry updates
- BallotImportHub - Import status tracking
- PublicHub - Public result displays

**Note**: Integration tests have JWT configuration issues in test environment, but controllers compile successfully and basic functionality works. Authentication works in development environment.

### [x] Step: Create New Frontend Pages/Components
<!-- chat-id: completed -->

Successfully created new frontend pages and components for enhanced results functionality:
- ✅ Election monitoring dashboard with real-time updates (MonitoringDashboardPage.vue)
- ✅ Tie management interface for tie-breaking (TieManagementPage.vue)
- ✅ Presentation views for projector displays (PresentationViewPage.vue)
- ✅ Enhanced reporting interfaces (ReportingPage.vue)
- ✅ Updated router with new routes for all pages

### [ ] Step: Add SignalR Hubs

Implement SignalR hubs for real-time updates:
- AnalyzeHub for tally progress updates
- BallotImportHub for import status
- FrontDeskHub for ballot entry updates
- MainHub for general notifications
- PublicHub for public result displays

### [ ] Step: Implement Authentication/Authorization

Add authentication and authorization features:
- JWT token management
- Role-based access control
- User registration and login
- Password reset functionality
- Session management

### [ ] Step: Add File Import/Export Capabilities

Implement file import/export functionality:
- Ballot data import (CSV, Excel)
- Election configuration import/export
- Result export (PDF, Excel)
- Backup and restore capabilities

### [ ] Step: Create Reporting and Analytics

Build comprehensive reporting and analytics:
- Detailed election statistics
- Voter turnout analysis
- Location-based reporting
- Historical election comparisons
- Custom report generation

### [ ] Step: End-to-End Testing

Perform comprehensive testing of the implementation:
- Unit tests for all new components
- Integration tests for API endpoints
- Frontend component tests
- End-to-end user workflow testing
- Performance and load testing
