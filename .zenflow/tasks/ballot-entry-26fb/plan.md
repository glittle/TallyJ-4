# Full SDD workflow

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Workflow Steps

### [x] Step: Requirements
<!-- chat-id: 5f6f7406-449a-405e-bda2-352c893b3db8 -->

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `{@artifacts_path}/requirements.md`.

### [x] Step: Technical Specification
<!-- chat-id: 735202d9-b22a-4c75-82a4-0245880a9b53 -->

Create a technical specification based on the PRD in `{@artifacts_path}/requirements.md`.

1. Review existing codebase architecture and identify reusable components
2. Define the implementation approach

Save to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach referencing existing code patterns
- Source code structure changes
- Data model / API / interface changes
- Delivery phases (incremental, testable milestones)
- Verification approach using project lint/test commands

### [x] Step: Planning
<!-- chat-id: 6c16d8f6-c5f8-425f-92d0-52aa0aab2a0f -->

Create a detailed implementation plan based on `{@artifacts_path}/spec.md`.

1. Break down the work into concrete tasks
2. Each task should reference relevant contracts and include verification steps
3. Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint). Avoid steps that are too granular (single function) or too broad (entire feature).

Important: unit tests must be part of each implementation task, not separate tasks. Each task should implement the code and its tests together, if relevant.

If the feature is trivial and doesn't warrant full specification, update this workflow to remove unnecessary steps and explain the reasoning to the user.

Save to `{@artifacts_path}/plan.md`.

---

## Implementation Tasks

### [x] Step: Backend API - Add CombinedSoundCodes to PersonDto
<!-- chat-id: 6ca9a1a5-ce29-4101-8e23-1abb7a8c9600 -->

**Files to modify**:
- `backend/DTOs/People/PersonDto.cs` - Add `CombinedSoundCodes` property
- `backend/Mappings/PersonProfile.cs` - Add AutoMapper mapping for CombinedSoundCodes

**Description**:
Add the `CombinedSoundCodes` field to PersonDto so that phonetic search codes are available to the frontend. Update AutoMapper configuration to map this field from Person entity.

**Verification**:
- [ ] Backend builds without errors: `cd backend && dotnet build`
- [ ] PersonDto includes `CombinedSoundCodes` property with XML documentation
- [ ] AutoMapper profile includes mapping for this field
- [ ] Existing tests still pass: `dotnet test`

---

### [ ] Step: Backend API - Create GetCandidates Endpoint

**Files to create/modify**:
- `backend/Services/IPeopleService.cs` - Add `GetCandidatesAsync` method signature
- `backend/Services/PeopleService.cs` - Implement `GetCandidatesAsync` method
- `backend/Controllers/PeopleController.cs` - Add `GetCandidates` endpoint

**Description**:
Create a new API endpoint `/api/people/election/{electionGuid}/candidates` that returns all people where `CanReceiveVotes = true` for client-side caching. The method should:
- Filter by electionGuid and CanReceiveVotes = true
- Include CombinedSoundCodes in response
- Order by LastName, then FirstName
- Return complete list (no pagination)

**Unit Tests** (`TallyJ4.Tests/UnitTests/Services/PeopleServiceTests.cs`):
- [ ] Test `GetCandidatesAsync_ReturnsOnlyEligiblePeople`
- [ ] Test `GetCandidatesAsync_IncludesSoundCodes`
- [ ] Test `GetCandidatesAsync_OrdersByLastNameFirstName`

**Verification**:
- [ ] Backend builds: `dotnet build`
- [ ] Unit tests pass: `dotnet test --filter "FullyQualifiedName~PeopleService"`
- [ ] API tested with Swagger at `/api/people/election/{guid}/candidates`
- [ ] Response includes all expected PersonDto fields including CombinedSoundCodes
- [ ] Performance < 2s for 1000 people (test with sample data)

---

### [ ] Step: Frontend API - Regenerate Client & Update Types

**Files to modify**:
- `frontend/src/types/Person.ts` - Add `combinedSoundCodes` property and `SearchablePersonDto` interface
- Regenerate API client after backend changes

**Description**:
Regenerate the frontend API client to include the new `/candidates` endpoint. Update TypeScript interfaces to include:
- `PersonDto.combinedSoundCodes?: string`
- New `SearchablePersonDto` interface with `_searchText` and `_soundexCodes` fields for client-side search optimization

**Verification**:
- [ ] Run API regeneration: `cd frontend && npm run gen`
- [ ] TypeScript compiles: `npm run type-check`
- [ ] No type errors in console
- [ ] PersonDto includes combinedSoundCodes property

---

### [ ] Step: Frontend Utils - Implement Search Strategies

**Files to create**:
- `frontend/src/utils/searchStrategies.ts` - Individual search strategy implementations
- `frontend/src/utils/__tests__/searchStrategies.spec.ts` - Unit tests for strategies

**Description**:
Implement search strategy functions:
1. **exactMatch** (weight: 100): Full name matches exactly (case-insensitive)
2. **prefixMatch** (weight: 90): Search term is prefix of name
3. **wordBoundaryMatch** (weight: 85): Each word starts name parts
4. **substringMatch** (weight: 80): Search appears anywhere
5. **otherNamesMatch** (weight: 70): Match OtherNames/OtherLastNames
6. **phoneticMatch** (weight: 60-75): Soundex codes similarity
7. **fuzzyMatch** (weight: 50): Levenshtein distance ≤ 2

Include helper functions:
- `calculateLevenshteinDistance(a: string, b: string): number`
- `compareSoundexCodes(codes1: string[], codes2: string[]): number`
- `normalizeSearchText(text: string): string`

**Unit Tests**:
- [ ] Test each strategy with various inputs
- [ ] Test edge cases (empty strings, special characters, unicode)
- [ ] Test normalization function
- [ ] Test Levenshtein distance calculation
- [ ] Test Soundex comparison

**Verification**:
- [ ] All tests pass: `npm run test searchStrategies`
- [ ] Type check passes: `npm run type-check`
- [ ] Coverage > 90% for searchStrategies.ts
- [ ] Performance: Each strategy executes in < 5ms for single comparison

---

### [ ] Step: Frontend Composable - Create usePersonSearch

**Files to create**:
- `frontend/src/composables/usePersonSearch.ts` - Multi-strategy search composable
- `frontend/src/composables/__tests__/usePersonSearch.spec.ts` - Unit tests

**Description**:
Create a composable that:
- Accepts a reactive search query and list of SearchablePersonDto
- Applies all 7 search strategies in parallel
- Ranks results by weight (highest first)
- Breaks ties by LastName, FirstName alphabetically
- Debounces search (150ms) using @vueuse/core
- Activates phonetic/fuzzy only after 3+ characters
- Caps results at 20
- Returns reactive search results

**Unit Tests**:
- [ ] Test exact match returns highest weight
- [ ] Test prefix match finds partial names
- [ ] Test phonetic match finds similar-sounding names
- [ ] Test fuzzy match finds typos (Levenshtein ≤ 2)
- [ ] Test ranking orders by weight correctly
- [ ] Test tie-breaking by name
- [ ] Test debouncing delays search
- [ ] Test minimum 3 chars for phonetic/fuzzy
- [ ] Test result cap at 20
- [ ] Test performance < 50ms for 1000 people

**Verification**:
- [ ] Tests pass: `npm run test usePersonSearch`
- [ ] Type check passes
- [ ] Performance benchmark met (< 50ms for 1000 people)
- [ ] Coverage > 90%

---

### [ ] Step: Frontend Store - Add Candidate Cache to PeopleStore

**Files to modify**:
- `frontend/src/stores/peopleStore.ts` - Add candidate cache state and actions
- `frontend/src/stores/__tests__/peopleStore.spec.ts` - Add/update tests

**Description**:
Extend peopleStore with:
- `candidateCache: Ref<SearchablePersonDto[]>` - cached candidates
- `isCacheInitialized: Ref<boolean>` - initialization state
- `initializeCandidateCache(electionGuid: string)` - fetch and enrich candidates
- `enrichPersonForSearch(person: PersonDto): SearchablePersonDto` - add search metadata
- Update SignalR `handlePersonUpdated` to refresh cache entries
- Update SignalR `handlePersonAdded` to add to cache if eligible
- Update SignalR `handlePersonDeleted` to remove from cache

**Tests**:
- [ ] Test cache initialization fetches candidates
- [ ] Test enrichPersonForSearch adds _searchText and _soundexCodes
- [ ] Test SignalR update refreshes cache entry
- [ ] Test SignalR add appends to cache if CanReceiveVotes=true
- [ ] Test SignalR delete removes from cache

**Verification**:
- [ ] Tests pass: `npm run test peopleStore`
- [ ] Type check passes
- [ ] No console errors during cache initialization
- [ ] Cache loads in < 2s for 100+ people (manual test)

---

### [ ] Step: Frontend Component - Create VoteEntryRow

**Files to create**:
- `frontend/src/components/ballots/VoteEntryRow.vue` - Single vote input row
- `frontend/src/components/ballots/__tests__/VoteEntryRow.spec.ts` - Component tests

**Description**:
Create a component with:
- Props: `positionOnBallot`, `modelValue`, `candidates`, `duplicatePersonGuids`
- Emits: `update:modelValue`, `vote-selected`, `vote-cleared`
- Search input with autocomplete dropdown (Element Plus `el-autocomplete` or custom)
- Keyboard navigation:
  - Down arrow: highlight next result
  - Up arrow: highlight previous result
  - Enter: select highlighted result, emit vote-selected
  - Escape: clear search, close dropdown
- Display selected person name after selection
- Clear button (X) to remove vote
- Warning indicator if duplicate (prop-based)
- ARIA attributes for accessibility

**Component Tests**:
- [ ] Test keyboard Down arrow highlights next result
- [ ] Test keyboard Up arrow highlights previous result
- [ ] Test Enter key selects highlighted result
- [ ] Test Escape clears search
- [ ] Test search shows filtered results
- [ ] Test clear button removes selection
- [ ] Test duplicate indicator shows when prop is true
- [ ] Test ARIA labels are present

**Verification**:
- [ ] Tests pass: `npm run test VoteEntryRow`
- [ ] Type check passes
- [ ] Visual test: component renders correctly
- [ ] Keyboard navigation works (manual test)
- [ ] Screen reader announces selections (manual test)

---

### [ ] Step: Frontend Component - Create InlineBallotEntry

**Files to create**:
- `frontend/src/components/ballots/InlineBallotEntry.vue` - Container for vote entry rows
- `frontend/src/components/ballots/__tests__/InlineBallotEntry.spec.ts` - Component tests

**Description**:
Create a container component with:
- Props: `electionGuid`, `ballot`, `maxVotes`
- Emits: `vote-added`, `vote-removed`, `ballot-saved`
- Displays `maxVotes` number of VoteEntryRow components
- Manages focus between rows (Tab, Enter moves to next)
- Detects duplicates (same personGuid selected twice)
- Shows duplicate warning toast (Element Plus `ElMessage`)
- "Save Ballot" button (if not auto-saving)
- "Clear All" button
- Status indicator: "X of Y votes entered"
- Initialize candidate cache on mount via peopleStore

**Component Tests**:
- [ ] Test renders correct number of rows based on maxVotes
- [ ] Test Tab key moves focus to next row
- [ ] Test Enter on vote selection moves to next row
- [ ] Test duplicate detection shows warning
- [ ] Test Clear All resets all votes
- [ ] Test status indicator updates on vote changes
- [ ] Test cache initialization on mount

**Verification**:
- [ ] Tests pass: `npm run test InlineBallotEntry`
- [ ] Type check passes
- [ ] Visual test: layout matches design
- [ ] Focus management works (manual test)
- [ ] Duplicate warning appears (manual test)

---

### [ ] Step: Frontend Page - Integrate InlineBallotEntry into BallotEntryPage

**Files to modify**:
- `frontend/src/pages/ballots/BallotEntryPage.vue` - Replace VoteFormDialog with InlineBallotEntry
- `frontend/src/pages/ballots/__tests__/BallotEntryPage.spec.ts` - Update tests

**Description**:
Update BallotEntryPage to:
- Remove VoteFormDialog import and usage
- Add InlineBallotEntry component
- Pass required props: electionGuid, ballot, maxVotes (from election.numberToElect)
- Handle `vote-added` event: create vote via ballot store, show success toast
- Handle `vote-removed` event: delete vote via ballot store
- Handle SignalR person updates: show toast if name changed
- Initialize candidate cache on page mount

**Tests**:
- [ ] Test InlineBallotEntry is rendered
- [ ] Test vote-added handler calls ballot store
- [ ] Test vote-removed handler calls ballot store
- [ ] Test person update toast appears

**Verification**:
- [ ] Tests pass: `npm run test BallotEntryPage`
- [ ] Type check passes
- [ ] Manual test: Navigate to ballot entry page
- [ ] Manual test: Enter full ballot using keyboard only
- [ ] Manual test: Verify votes are saved to backend
- [ ] No console errors

---

### [ ] Step: Integration Testing & Real-time Sync Verification

**Description**:
Perform end-to-end integration testing to verify:
1. Ballot entry works from start to finish
2. SignalR person updates appear within 1s
3. Multiple users can work simultaneously without conflicts
4. Search performance meets targets

**Test Scenarios** (manual):
- [ ] **Rapid Entry Test**: Enter 9-vote ballot using only keyboard in < 60s
- [ ] **Phonetic Search Test**: Search "Macfarland" finds "McFarland"
- [ ] **Real-time Update Test**: Open 2 tabs, update person in Tab 1, see change in Tab 2 within 1s
- [ ] **Duplicate Warning Test**: Select same person twice, see warning toast
- [ ] **Large Dataset Test**: Load 100+ candidates, verify search < 50ms (use console.time)
- [ ] **Accessibility Test**: Navigate entire ballot entry with Tab/Enter/Arrow keys only

**Verification**:
- [ ] All manual scenarios pass
- [ ] Backend tests pass: `cd backend && dotnet test`
- [ ] Frontend tests pass: `cd frontend && npm run test`
- [ ] Type check passes: `npm run type-check`
- [ ] Build succeeds: `npm run build`
- [ ] No console errors or warnings
- [ ] Performance benchmarks met (< 50ms search, < 2s initial load)

---

### [ ] Step: Polish & Optimization

**Description**:
Final enhancements for production readiness:
1. Add loading states (skeleton screens during cache initialization)
2. Add empty state messaging ("No matches found - check spelling or add new person")
3. Add "Add New Person" quick action button (if no results)
4. Add error boundaries for graceful error handling
5. Optimize search performance (memoization if needed)
6. Visual polish (match Element Plus design system)
7. Accessibility audit (WCAG 2.1 AA)

**Files to modify**:
- `frontend/src/components/ballots/VoteEntryRow.vue` - Add loading/empty states
- `frontend/src/components/ballots/InlineBallotEntry.vue` - Add error boundaries
- `frontend/src/composables/usePersonSearch.ts` - Add memoization if needed

**Tasks**:
- [ ] Add loading skeleton to InlineBallotEntry while cache initializes
- [ ] Add empty state message in VoteEntryRow dropdown
- [ ] Add "Add New Person" button in empty state (optional MVP feature)
- [ ] Add error boundary for cache initialization failures
- [ ] Run accessibility audit (axe DevTools or Lighthouse)
- [ ] Fix any accessibility issues found
- [ ] Performance test with 1000 person dataset
- [ ] Visual review: consistent spacing, colors, typography

**Verification**:
- [ ] Loading states render correctly
- [ ] Empty states show helpful messages
- [ ] Errors are caught and displayed gracefully
- [ ] Accessibility score 100% (Lighthouse)
- [ ] Performance benchmarks met (< 50ms search, < 2s load)
- [ ] Visual design consistent with app
- [ ] All tests still pass
- [ ] Build succeeds with no warnings

---

## Final Verification

Before marking feature complete, verify all acceptance criteria from spec.md Section 6.4:

**Functional**:
- [ ] Local candidate cache loads on page mount
- [ ] Search finds exact matches
- [ ] Search finds phonetic matches (Soundex)
- [ ] Search finds fuzzy matches (Levenshtein ≤ 2)
- [ ] Results ranked by relevance
- [ ] Keyboard navigation works (Up/Down/Enter/Escape)
- [ ] Enter adds vote and moves to next line
- [ ] SignalR updates cache within 1s
- [ ] Duplicate votes show warning
- [ ] Multiple users can work concurrently

**Non-Functional**:
- [ ] Search response < 50ms
- [ ] API response < 2s (1000 people)
- [ ] Memory usage < 10MB
- [ ] WCAG 2.1 AA compliant
- [ ] Works in Chrome 90+, Firefox 88+, Safari 14+, Edge 90+

**Code Quality**:
- [ ] Unit tests pass (backend & frontend)
- [ ] Integration tests pass
- [ ] Type check passes: `npm run type-check`
- [ ] Build passes: `npm run build`
- [ ] No build warnings
- [ ] Code follows existing conventions
- [ ] AutoMapper mappings complete
- [ ] SignalR handlers tested
