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

### [ ] Step: Add export libraries to backend

Add ClosedXML and iTextSharp NuGet packages to TallyJ4.csproj for Excel and PDF generation.

### [ ] Step: Create ExportRequest DTO

Create Models/ExportRequest.cs with properties for format, electionId, and optional filters.

### [ ] Step: Implement ReportExportService

Create Services/ReportExportService.cs with methods to generate PDF and Excel reports using election data.

### [ ] Step: Add export API endpoint

Add POST /api/reports/export/{electionId} endpoint to Controllers/ReportsController.cs that uses ReportExportService.

### [ ] Step: Update frontend API

Add export function to src/api/reports.ts to call the new backend endpoint.

### [ ] Step: Update ReportingPage export function

Modify exportReport function in src/pages/results/ReportingPage.vue to call the API and handle file download.

### [ ] Step: Add unit tests

Create unit tests for ReportExportService in TallyJ4.Tests.

### [ ] Step: Test and verify

Run tests, perform manual testing of export functionality, and write implementation report to report.md.
