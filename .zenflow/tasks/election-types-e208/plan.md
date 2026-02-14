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
<!-- chat-id: 46757cab-9682-47a8-885e-8d92ebeabd7f -->

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

### [x] Step: Create enum classes and update backend validators/services
<!-- chat-id: 1e118351-12ea-4e05-9566-87eda87f438d -->

1. Create `backend/TallyJ4.Domain/Enumerations/ElectionTypeEnum.cs` with codes: LSA, LSA1, LSA2, NSA, Con, Reg, Oth
2. Create `backend/TallyJ4.Domain/Enumerations/ElectionModeEnum.cs` with codes: N, T, B
3. Update `CreateElectionDtoValidator.cs` — replace `{"STV","Cond","Multi"}` with `ElectionTypeEnum.AllCodes` and `{"N","I"}` with `ElectionModeEnum.AllCodes`
4. Update `UpdateElectionDtoValidator.cs` — same changes
5. Update `ElectionStep2DtoValidator.cs` — same changes
6. Update `SetupService.cs` — use enum constants for defaults
7. Update `DbSeeder.cs` — fix `"Conv"` → `"Con"`, `"I"` → `"N"`, `"D"` → `"N"`
8. Fix DTO doc comments in ElectionDto, CreateElectionDto, UpdateElectionDto, ElectionStep2Dto
9. Run `dotnet build` to verify compilation

### [x] Step: Update frontend components with correct election types/modes
<!-- chat-id: 639f34c2-23ec-4cfb-94ec-eb19f2d8a5c4 -->

1. Update `ElectionFormTabs.vue` — replace type options (STV/Cond/Multi → LSA/LSA1/LSA2/NSA/Con/Reg/Oth) and mode options (Normal/International → Normal/Tie-Break/By-election)
2. Update `ElectionListPage.vue` — replace type filter dropdown options
3. Update `CreateElectionPage.vue` — change default `electionType: 'STV'` → `'LSA'`
4. Run `npx vue-tsc --noEmit` to verify

### [ ] Step: Fix all tests and verify

1. Fix `ElectionServiceTests.cs` — change `"Standard"` → `"LSA"` for all ElectionType values
2. Fix `TallyServiceTests.cs` — change `"SingleName"` → valid type code
3. Fix `IntegrationTestBase.cs` — change `"STV"` → `"LSA"`, `"FPTP"` → `"NSA"`
4. Fix `ElectionsControllerTests.cs` — change `"STV"` → `"LSA"`
5. Fix `ResultsControllerTests.cs` — change `"STV"` → `"LSA"`
6. Fix `electionStore.test.ts` — change `"Normal"` → valid codes
7. Run `dotnet test` for backend tests
8. Run `npm run test` for frontend tests
9. Write report to `{@artifacts_path}/report.md`
