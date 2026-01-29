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
<!-- chat-id: 6247e0d6-dc7d-4510-8d00-898f5445251f -->

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

### [x] Step: Add export libraries to backend
<!-- chat-id: e01e1a12-c216-4cd8-a46a-7e5fe94c203e -->

Add ClosedXML and iTextSharp NuGet packages to TallyJ4.csproj for Excel and PDF generation.

### [x] Step: Create ExportRequest DTO
<!-- chat-id: 9944d2fb-a05f-4c52-ac84-137bdfd2f6e5 -->

Create Models/ExportRequest.cs with properties for format, electionId, and optional filters.

### [x] Step: Implement ReportExportService
<!-- chat-id: 6a955a04-21d8-4ecf-bf07-69125678d03c -->

Create Services/ReportExportService.cs with methods to generate PDF and Excel reports using election data.

### [x] Step: Add export API endpoint
<!-- chat-id: 20c560c5-38a2-451b-906b-e8e5a45e1610 -->

Add POST /api/reports/export/{electionId} endpoint to Controllers/ReportsController.cs that uses ReportExportService.

### [x] Step: Update frontend API
<!-- chat-id: 0d606a5b-82ef-46be-b2b6-ec700d9dd27b -->

Add export function to src/api/reports.ts to call the new backend endpoint.

### [x] Step: Update ReportingPage export function
<!-- chat-id: 2be652ee-0a13-49aa-8e33-0fbeaf7af56b -->

Modify exportReport function in src/pages/results/ReportingPage.vue to call the API and handle file download.

### [x] Step: Add unit tests
<!-- chat-id: 2b148eff-def8-4e26-b2bd-50ecbcf9c025 -->

Create unit tests for ReportExportService in TallyJ4.Tests.

### [ ] Step: Test and verify

Run tests, perform manual testing of export functionality, and write implementation report to report.md.
