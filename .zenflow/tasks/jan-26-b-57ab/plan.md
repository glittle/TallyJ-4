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
<!-- chat-id: e9aaa9d3-cda5-477f-9912-47a8a4080166 -->

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

### [x] Step: Fix OpenAPI Generation
<!-- chat-id: 6eee7fe1-6e5c-4edc-a40c-2527d6e22957 -->
Resolve the OpenAPI spec download and type generation issue to ensure type safety across the frontend.

**Tasks**:
- Fix the OpenAPI spec download (currently truncated at 100KB)
- Generate complete API client types
- Update service layer to use generated types
- Verify type compatibility with existing code

**Verification**: `npm run gen` succeeds and generates complete type definitions

---

### [ ] Step: Implement Real-time Features
Connect SignalR hubs to provide live updates for election results, ballot status, and collaborative features.

**Tasks**:
- Integrate SignalR events into Pinia stores
- Add real-time result updates in ResultsPage
- Implement live ballot status changes
- Add real-time notifications for election events
- Connect monitoring dashboard to live data

**Verification**: Real-time updates work when backend sends SignalR events

---

### [ ] Step: Enhance Error Handling and UX
Add comprehensive error handling, loading states, and user feedback throughout the application.

**Tasks**:
- Add error boundaries and global error handling
- Implement loading skeletons and states
- Add toast notifications for actions
- Improve form validation messages
- Add confirmation dialogs for destructive actions

**Verification**: All user actions provide appropriate feedback and error messages

---

### [ ] Step: Performance Optimization
Optimize the application for production use with code splitting, lazy loading, and bundle size reduction.

**Tasks**:
- Implement route-based code splitting
- Add lazy loading for heavy components
- Optimize bundle size (currently 1.2MB)
- Implement caching strategies
- Add service worker for offline support

**Verification**: Bundle size under 1MB, Lighthouse score >90

---

### [ ] Step: Add Testing Infrastructure
Implement comprehensive testing to ensure code quality and prevent regressions.

**Tasks**:
- Add unit tests for components using Vitest
- Implement integration tests for stores
- Add E2E test scripts for critical flows
- Configure test coverage reporting
- Add CI/CD test pipeline

**Verification**: `npm run test` passes with >80% coverage

---

### [ ] Step: Final Polish and Deployment
Complete UI/UX polish, accessibility improvements, and prepare for production deployment.

**Tasks**:
- Accessibility audit and fixes
- Cross-browser testing
- Mobile responsiveness improvements
- Documentation updates
- Production build optimization

**Verification**: Application works flawlessly in production environment
