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
<!-- chat-id: 21aaaa3f-7765-49aa-95ae-956bc0cf6db9 -->

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

### [x] Step: Fix Integration Test Path Issue
<!-- chat-id: db867bf9-c79e-4fb3-97e9-e4ec2df59c98 -->

**Priority**: HIGH  
**Estimated Time**: 30 minutes

Fix the hardcoded path in integration tests that references the old worktree `jan-20-6eb4`.

**Tasks**:
1. ✅ Locate path reference in `IntegrationTestBase.cs` or `CustomWebApplicationFactory`
2. ✅ Change to use relative path or `Directory.GetCurrentDirectory()`
3. ✅ Run `dotnet test` to verify all 41 tests pass

**Verification**: All tests pass without path errors

**Result**: ✅ **ALL 41 TESTS PASSING** 

**Fixes applied:**
1. **Database Configuration** - Modified `Program.cs` to skip DbContext registration in Testing environment  
2. **InMemory Database Setup** - Configured `CustomWebApplicationFactory.cs` to use InMemory database for tests
3. **Transaction Support** - Added `InMemoryEventId.TransactionIgnoredWarning` to ignore transaction warnings in InMemory database
4. **Validation Fixes** - Updated test DTOs to use valid ElectionType values ("STV" instead of "Standard") and TallyStatus values ("Processing" instead of "InProgress")
5. **Test Data Fixes** - Added `CanReceiveVotes = true` to Person entities in test data so votes are counted as valid
6. **Response Type Fixes** - Corrected test deserialization types:
   - `GetElections` returns `PaginatedResponse<ElectionSummaryDto>` (not wrapped in ApiResponse)
   - `GetSummary` returns `TallyStatisticsDto` (not TallyResultDto)

**Final Status**: 41/41 tests passing (0 failures)

---

### [ ] Step: End-to-End Integration Testing
<!-- chat-id: bfc8a8ef-46bb-4204-b8a0-64d3b80bbf2e -->

**Priority**: HIGH  
**Estimated Time**: 4-5 hours

Test the complete application stack with backend and frontend running together.

**Sub-tasks**:
1. Start backend API server (`cd backend && dotnet run`)
2. Start frontend dev server (`cd frontend && npm run dev`)
3. Test authentication workflows (register, login, logout, token refresh)
4. Test election management (create, list, view, edit, delete)
5. Test people management (add, search, edit, delete)
6. Test ballot & vote entry workflows
7. Test tally calculation end-to-end
8. Test results display (sections, ties, statistics)

**Verification**: All critical workflows complete successfully without errors

---

### [ ] Step: Bug Fixes and Polish

**Priority**: MEDIUM  
**Estimated Time**: 2-3 hours

Address any issues discovered during integration testing and improve UX.

**Tasks**:
1. Fix runtime errors and bugs
2. Improve error handling and user feedback
3. Add missing loading indicators
4. Ensure consistent styling
5. Test responsive design
6. Validate all forms properly

**Verification**: Application runs smoothly with no critical bugs

---

### [ ] Step: Documentation and Final Report

**Priority**: LOW  
**Estimated Time**: 1 hour

Update documentation and create completion report.

**Tasks**:
1. Update README with frontend setup instructions
2. Document environment variables
3. Write completion report to `{@artifacts_path}/report.md` describing:
   - What was implemented
   - How the solution was tested
   - Issues encountered and resolved
   - Current system status

**Verification**: Documentation is clear and complete
