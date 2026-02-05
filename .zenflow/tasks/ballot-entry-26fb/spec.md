# Technical Specification: Enhanced Ballot Entry with Intelligent Name Search

## 1. Technical Context

### 1.1 Technology Stack

**Backend:**
- Language: C# .NET 10
- Framework: ASP.NET Core Web API
- ORM: Entity Framework Core 10
- Real-time: SignalR
- Mapping: AutoMapper 12.0.1
- Database: SQL Server

**Frontend:**
- Language: TypeScript 5.9.3
- Framework: Vue 3.5.22 (Composition API)
- Build Tool: Vite 7.1.14
- UI Library: Element Plus 2.11.5
- State Management: Pinia 3.0.3
- HTTP Client: Axios 1.13.2
- Real-time: @microsoft/signalr 9.0.6
- Utilities: @vueuse/core 14.0.0

### 1.2 Existing Components

**Backend:**
- [`backend/TallyJ4.Domain/Entities/Person.cs`](../../../backend/TallyJ4.Domain/Entities/Person.cs:49) - Person entity with `CombinedSoundCodes` field
- [`backend/Controllers/PeopleController.cs`](../../../backend/Controllers/PeopleController.cs) - REST API endpoints for people
- [`backend/Services/PeopleService.cs`](../../../backend/Services/PeopleService.cs) - Business logic for people operations
- [`backend/Hubs/MainHub.cs`](../../../backend/Hubs/MainHub.cs) - SignalR hub for election groups
- `backend/Hubs/FrontDeskHub.cs` - SignalR hub for person updates

**Frontend:**
- [`frontend/src/pages/ballots/BallotEntryPage.vue`](../../../frontend/src/pages/ballots/BallotEntryPage.vue) - Current ballot entry page
- [`frontend/src/components/ballots/VoteFormDialog.vue`](../../../frontend/src/components/ballots/VoteFormDialog.vue) - Current vote entry dialog
- [`frontend/src/stores/peopleStore.ts`](../../../frontend/src/stores/peopleStore.ts) - Pinia store with SignalR support
- [`frontend/src/types/Person.ts`](../../../frontend/src/types/Person.ts) - TypeScript interfaces

## 2. Implementation Approach

### 2.1 Architecture Overview

The solution uses a **client-side search architecture** with server-side data synchronization:

1. **Initial Load**: Fetch all eligible candidates once on page mount
2. **Local Cache**: Store candidates in Pinia store with search metadata
3. **Search Engine**: Client-side multi-strategy search with ranking
4. **Real-time Sync**: SignalR updates keep cache current
5. **Inline Entry**: Replace dialog with inline vote entry rows

### 2.2 Search Algorithm Strategy

**Multi-Strategy Weighted Search:**

```typescript
interface SearchStrategy {
  name: string;
  weight: number;
  matcher: (query: string, person: PersonDto) => boolean;
}
```

**Strategies** (executed in parallel):
1. **Exact Match** (100): Full name matches exactly (case-insensitive)
2. **Prefix Match** (90): Search term is prefix of any name field
3. **Word Boundary** (85): Each word starts name parts ("J S" → "John Smith")
4. **Substring Match** (80): Search appears anywhere in name
5. **Other Names** (70): Match OtherNames/OtherLastNames fields
6. **Phonetic Match** (60-75): CombinedSoundCodes similarity
7. **Fuzzy Match** (50): Levenshtein distance ≤ 2

**Optimization:**
- Phonetic/fuzzy only activate after 3+ characters
- Debounce search input (150ms)
- Results capped at 20
- Pre-compute search metadata on cache load

### 2.3 Data Flow

```
┌─────────────────┐
│ BallotEntryPage │
│   (mounted)     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────┐
│  PeopleStore    │◄─────┤ PeopleService│
│  - Initialize   │      │ GET /people  │
│  - Load cache   │      │ ?canReceive  │
│  - Join SignalR │      │ Votes=true   │
└────────┬────────┘      └──────────────┘
         │
         ▼
┌─────────────────┐
│ VoteEntryRow    │─────► Local Search
│ (search input)  │       (composable)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Autocomplete    │
│ Dropdown        │
│ (results)       │
└────────┬────────┘
         │ Enter
         ▼
┌─────────────────┐      ┌──────────────┐
│ Create Vote     │─────►│ VoteService  │
│                 │      │ POST /votes  │
└─────────────────┘      └──────────────┘

         ┌──────────────┐
SignalR ─┤ PersonUpdate │─► Update Cache
         └──────────────┘
```

## 3. Source Code Structure Changes

### 3.1 Backend Changes

#### 3.1.1 New/Modified Files

**1. `backend/DTOs/People/PersonDto.cs`**
```csharp
public class PersonDto
{
    // ... existing fields ...
    
    /// <summary>
    /// Combined phonetic sound codes for name matching (Soundex).
    /// </summary>
    public string? CombinedSoundCodes { get; set; }
}
```

**2. `backend/Mappings/PersonProfile.cs`**
```csharp
// Add mapping for CombinedSoundCodes field
CreateMap<Person, PersonDto>()
    .ForMember(dest => dest.CombinedSoundCodes, 
               opt => opt.MapFrom(src => src.CombinedSoundCodes));
```

**3. `backend/Controllers/PeopleController.cs`**
```csharp
// Modify existing endpoint to support fetching all without pagination
[HttpGet("election/{electionGuid}/candidates")]
public async Task<ActionResult<ApiResponse<List<PersonDto>>>> GetCandidates(
    Guid electionGuid)
{
    var results = await _peopleService.GetCandidatesAsync(electionGuid);
    return Ok(ApiResponse<List<PersonDto>>.SuccessResponse(results));
}
```

**4. `backend/Services/IPeopleService.cs` & `PeopleService.cs`**
```csharp
// New method signature
Task<List<PersonDto>> GetCandidatesAsync(Guid electionGuid);

// Implementation in PeopleService.cs
public async Task<List<PersonDto>> GetCandidatesAsync(Guid electionGuid)
{
    var people = await _context.People
        .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
        .OrderBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .ToListAsync();

    return _mapper.Map<List<PersonDto>>(people);
}
```

**5. `backend/Hubs/FrontDeskHub.cs`** (verify existing implementation)
- Ensure `PersonUpdate` events include electionGuid, personGuid, action

### 3.2 Frontend Changes

#### 3.2.1 New Files

**1. `frontend/src/composables/usePersonSearch.ts`**
- Multi-strategy search engine
- Search result ranking
- Performance optimization (memoization, debouncing)
- TypeScript interface for search results

**2. `frontend/src/utils/searchStrategies.ts`**
- Individual search strategy implementations
- Phonetic matching (Soundex comparison)
- Fuzzy matching (Levenshtein distance)
- Weight calculation helpers

**3. `frontend/src/components/ballots/VoteEntryRow.vue`**
- Single vote input row with autocomplete
- Keyboard navigation (Up/Down/Enter/Escape)
- Selected person display
- Clear button
- ARIA accessibility attributes

**4. `frontend/src/components/ballots/InlineBallotEntry.vue`**
- Container for multiple VoteEntryRow components
- Vote position management
- Duplicate detection
- Keyboard focus management
- "Add New Person" quick action

#### 3.2.2 Modified Files

**1. `frontend/src/types/Person.ts`**
```typescript
export interface PersonDto {
  // ... existing fields ...
  combinedSoundCodes?: string;
}

export interface SearchablePersonDto extends PersonDto {
  _searchText: string;        // Pre-computed search text
  _soundexCodes: string[];    // Parsed soundex codes
}
```

**2. `frontend/src/stores/peopleStore.ts`**
```typescript
// Add candidate cache
const candidateCache = ref<SearchablePersonDto[]>([]);

// Add cache initialization
async function initializeCandidateCache(electionGuid: string) {
  const candidates = await peopleService.getCandidates(electionGuid);
  candidateCache.value = candidates.map(enrichPersonForSearch);
}

// Helper to enrich person with search metadata
function enrichPersonForSearch(person: PersonDto): SearchablePersonDto {
  return {
    ...person,
    _searchText: [
      person.fullName,
      person.firstName,
      person.lastName,
      person.otherNames,
      person.otherLastNames,
      person.area
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase(),
    _soundexCodes: person.combinedSoundCodes?.split(',') || []
  };
}

// Update SignalR handlers to refresh cache
function handlePersonUpdated(data: PersonUpdateEvent) {
  const index = candidateCache.value.findIndex(p => p.personGuid === data.personGuid);
  if (index !== -1) {
    // Re-fetch and re-enrich
    fetchPersonById(data.personGuid).then(person => {
      if (person.canReceiveVotes) {
        candidateCache.value[index] = enrichPersonForSearch(person);
      } else {
        candidateCache.value.splice(index, 1);
      }
    });
  }
}
```

**3. `frontend/src/services/peopleService.ts`**
```typescript
async getCandidates(electionGuid: string): Promise<PersonDto[]> {
  const response = await getApiPeopleElectionByElectionGuidCandidates({ 
    path: { electionGuid } 
  });
  return response.data as PersonDto[];
}
```

**4. `frontend/src/pages/ballots/BallotEntryPage.vue`**
```vue
<!-- Replace VoteFormDialog with InlineBallotEntry -->
<template>
  <div class="ballot-entry-page">
    <el-card>
      <!-- ... ballot info ... -->
      
      <InlineBallotEntry
        :election-guid="electionGuid"
        :ballot="ballot"
        :max-votes="maxVotes"
        @vote-added="handleVoteAdded"
        @vote-removed="handleVoteRemoved"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">
// Remove VoteFormDialog, import InlineBallotEntry
const maxVotes = computed(() => election.value?.numberToElect || 9);
</script>
```

**5. `frontend/src/types/Vote.ts`** (if needed)
```typescript
export interface VoteEntryState {
  positionOnBallot: number;
  personGuid: string | null;
  personName: string | null;
  isDuplicate: boolean;
  isValid: boolean;
}
```

## 4. Data Model / API Changes

### 4.1 Backend API Endpoints

#### New Endpoint

**GET** `/api/people/election/{electionGuid}/candidates`

**Description**: Fetch all eligible candidates (CanReceiveVotes=true) for client-side caching

**Query Parameters**: None

**Response**: `ApiResponse<List<PersonDto>>`
```json
{
  "success": true,
  "data": [
    {
      "personGuid": "...",
      "firstName": "John",
      "lastName": "Smith",
      "fullName": "Smith, John",
      "area": "District 1",
      "combinedSoundCodes": "S530,J500",
      "canReceiveVotes": true,
      "voteCount": 0
    }
  ]
}
```

**Performance**: Should return < 2s for 1000 people

#### Modified Endpoint

**GET** `/api/people/election/{electionGuid}` - No changes to signature, just ensure `CombinedSoundCodes` is included in response

### 4.2 SignalR Events

#### Existing Events (verify implementation)

**Event**: `PersonUpdate` (via FrontDeskHub)

**Payload**:
```typescript
{
  electionGuid: string;
  personGuid: string;
  action: 'added' | 'updated' | 'deleted';
  firstName?: string;
  lastName?: string;
  updatedAt: string;
}
```

**Client Handler**: Update `candidateCache` in peopleStore

### 4.3 Database Schema

**No schema changes required** - `Person.CombinedSoundCodes` already exists

**Potential improvement** (out of scope for MVP):
- Ensure CombinedSoundCodes is populated for all existing people
- Add database index on CombinedSoundCodes for future server-side phonetic search

## 5. Delivery Phases

### Phase 1: Backend API Enhancement (MVP Foundation)
**Goal**: Expose necessary data for client-side search

**Tasks**:
1. Add `CombinedSoundCodes` to PersonDto
2. Update AutoMapper profile
3. Create `GetCandidates` endpoint in PeopleController
4. Implement `GetCandidatesAsync` in PeopleService
5. Test endpoint with Swagger/Postman

**Verification**:
- API returns all candidates with soundcodes in < 2s for 1000 people
- Response includes all required fields per PersonDto

**Deliverable**: Working `/candidates` endpoint

---

### Phase 2: Frontend Search Engine (Core Logic)
**Goal**: Implement client-side search with multiple strategies

**Tasks**:
1. Create search strategy implementations (exact, prefix, substring, phonetic, fuzzy)
2. Create `usePersonSearch` composable with ranking algorithm
3. Update PersonDto TypeScript interface with `combinedSoundCodes`
4. Add candidate cache to peopleStore
5. Write unit tests for search strategies (Vitest)

**Verification**:
- Search executes in < 50ms for 1000 people
- All 7 strategies produce correct results
- Results are properly ranked by weight
- Unit tests pass (>90% coverage for search logic)

**Deliverable**: `usePersonSearch` composable with tests

---

### Phase 3: Vote Entry UI Components (User Interaction)
**Goal**: Build keyboard-optimized inline entry interface

**Tasks**:
1. Create `VoteEntryRow.vue` with autocomplete dropdown
2. Implement keyboard navigation (Up/Down/Enter/Escape)
3. Add ARIA accessibility attributes
4. Create `InlineBallotEntry.vue` container
5. Handle focus management between rows
6. Add duplicate detection logic
7. Style components to match Element Plus design system

**Verification**:
- All keyboard controls work as specified
- Tab order is logical
- Screen reader can navigate all elements
- Duplicate votes show warning
- Visual design consistent with app

**Deliverable**: Reusable vote entry components

---

### Phase 4: Integration & Real-time Sync (Complete Feature)
**Goal**: Integrate components and enable SignalR updates

**Tasks**:
1. Update `BallotEntryPage.vue` to use `InlineBallotEntry`
2. Initialize candidate cache on page mount
3. Connect SignalR handlers to update cache
4. Handle vote creation/deletion via ballot store
5. Add toast notifications for person updates
6. Test concurrent user scenarios (multi-tab/multi-user)

**Verification**:
- Ballot entry works end-to-end
- Person updates appear within 1 second
- Multiple users can enter ballots simultaneously
- No race conditions or cache staleness
- Integration tests pass

**Deliverable**: Fully functional ballot entry with real-time sync

---

### Phase 5: Polish & Optimization (Production Ready)
**Goal**: Enhance UX and performance for production

**Tasks**:
1. Add loading states and skeleton screens
2. Implement debouncing for search input
3. Add "Add New Person" quick action
4. Optimize search performance (memoization, indexing)
5. Add error boundaries and fallback UI
6. Responsive design review (if needed)
7. Accessibility audit (WCAG 2.1 AA)
8. Performance testing (1000+ person dataset)

**Verification**:
- Search response < 50ms sustained
- All error scenarios handled gracefully
- Accessibility checklist 100% complete
- Performance budget met
- User testing feedback positive

**Deliverable**: Production-ready feature

---

## 6. Verification Approach

### 6.1 Testing Strategy

#### Backend Tests (xUnit)

**Unit Tests** (`TallyJ4.Tests/UnitTests/Services/PeopleServiceTests.cs`):
- `GetCandidatesAsync_ReturnsOnlyEligiblePeople`
- `GetCandidatesAsync_IncludesSoundCodes`
- `GetCandidatesAsync_OrdersByLastNameFirstName`

**Integration Tests** (`TallyJ4.Tests/IntegrationTests/PeopleControllerTests.cs`):
- `GetCandidates_ReturnsSuccessResponse`
- `GetCandidates_FiltersIneligibleCandidates`

**Run Command**: 
```bash
dotnet test --filter "FullyQualifiedName~PeopleService"
```

#### Frontend Tests (Vitest)

**Unit Tests** (`frontend/src/composables/__tests__/usePersonSearch.spec.ts`):
- `exactMatch_FindsFullNameMatch`
- `prefixMatch_FindsPartialNameMatch`
- `phoneticMatch_FindsSimilarSoundingNames`
- `fuzzyMatch_FindsTypos`
- `ranking_OrdersByWeight`
- `performance_SearchesUnder50ms`

**Component Tests** (`frontend/src/components/ballots/__tests__/VoteEntryRow.spec.ts`):
- `keyboardNavigation_DownArrowSelectsNext`
- `keyboardNavigation_EnterSelectsHighlighted`
- `search_ShowsResults`
- `duplicate_ShowsWarning`

**Run Command**:
```bash
npm run test
```

#### E2E Testing Scenarios (Manual/Automated)

1. **Rapid Entry Test**: Enter 9-vote ballot using only keyboard in < 60s
2. **Phonetic Search Test**: Search "Macfarland" finds "McFarland"
3. **Real-time Update Test**: Open 2 tabs, update person in Tab 1, see change in Tab 2 within 1s
4. **Duplicate Warning Test**: Select same person twice, see warning
5. **Large Dataset Test**: Load 1000 candidates, search performance < 50ms
6. **Accessibility Test**: Navigate with Tab/Enter/Arrow keys only

### 6.2 Lint & Type Check

**Backend**:
```bash
cd backend
dotnet format --verify-no-changes
dotnet build
```

**Frontend**:
```bash
cd frontend
npm run type-check
# Note: No explicit lint command in package.json; add if needed
```

### 6.3 Performance Benchmarks

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| API response time (1000 people) | < 2s | Browser DevTools Network tab |
| Search execution time | < 50ms | `console.time()` in composable |
| Cache memory usage | < 10MB | Chrome DevTools Memory profiler |
| Initial page load | < 3s | Lighthouse performance score |
| SignalR update latency | < 1s | Timestamp comparison (server vs client) |

### 6.4 Acceptance Criteria Checklist

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
- [ ] Type check passes
- [ ] No build warnings
- [ ] Code follows existing conventions
- [ ] AutoMapper mappings complete
- [ ] SignalR handlers tested

## 7. Risk Mitigation

| Risk | Mitigation Strategy |
|------|-------------------|
| **Large datasets (5000+ people) slow down browser** | Implement virtual scrolling in results dropdown using Element Plus `el-virtual-list` if needed; benchmark with 5000 person test dataset |
| **Phonetic matching produces too many false positives** | Weight phonetic matches lower (60-75); require 3+ characters before activating; allow users to see match type indicator |
| **SignalR disconnection causes cache staleness** | Detect disconnection using SignalR connection state; show warning banner "Connection lost - data may be outdated"; add manual refresh button |
| **CombinedSoundCodes not populated for existing data** | Verify data in production; if missing, create migration script to populate soundcodes using Soundex algorithm in SQL Server |
| **Browser compatibility issues (Safari)** | Use ES2020+ features supported by all target browsers; test in BrowserStack; polyfill if needed (unlikely with Vite transpilation) |

## 8. Out of Scope

The following are **explicitly excluded** from this implementation:

- Mobile/tablet responsive optimization (desktop-only for v4.0)
- Voice input for name search
- OCR/ballot scanning
- Machine learning name matching
- Multi-language support beyond English
- Undo/redo functionality
- Offline mode with service workers
- Export ballot entry logs
- Advanced duplicate detection (same person different spelling)

## 9. Open Questions & Decisions

| Question | Decision | Rationale |
|----------|----------|-----------|
| Use existing CombinedSoundCodes or compute client-side? | Use existing (Phase 1); enhance later if needed | Leverage existing data; avoid adding client-side dependencies |
| Dialog vs inline entry? | **Inline entry** (matches TallyJ v3 UX) | Faster workflow, keyboard-optimized, better UX per requirements |
| Auto-save votes or manual save? | **Auto-save after each vote** | Data safety; prevents loss if browser crashes |
| Block duplicates or warn only? | **Warn only** | Tellers may need to enter ballots as written for review process |
| Show Area in search results? | **Yes, show Area in secondary text** | Most relevant disambiguating info per requirements |
| Cap results at 20 or unlimited? | **Cap at 20** | Balance between usefulness and UI clutter |
| Fuzzy match library or custom? | **Custom Levenshtein implementation** | Avoid new dependencies; simple algorithm, small code footprint |

## 10. Dependencies & Prerequisites

### 10.1 Required Before Starting

- [ ] Verify `Person.CombinedSoundCodes` is populated in database (check sample data)
- [ ] Confirm SignalR `PersonUpdate` events are firing correctly (check logs)
- [ ] Ensure test environment has 100+ people to test search performance

### 10.2 External Libraries (No New Dependencies)

All required functionality can be implemented using existing dependencies:
- String matching: Custom implementations
- Debouncing: @vueuse/core `useDebounceFn`
- Autocomplete: Element Plus `el-autocomplete` (or custom)
- Keyboard navigation: Vue event handlers

### 10.3 Backend Code Generation

After backend changes, regenerate frontend API client:
```bash
cd frontend
npm run gen
```

## 11. Definition of Done

A feature is complete when:

1. **Code Complete**:
   - All tasks in all phases implemented
   - Code follows existing conventions (Vue Composition API, TypeScript strict mode)
   - AutoMapper profiles updated
   - No TODO/FIXME comments

2. **Tests Passing**:
   - Backend: `dotnet test` passes all tests
   - Frontend: `npm run test` passes all tests
   - Type check: `npm run type-check` passes
   - No console errors/warnings in browser

3. **Documentation**:
   - Code comments for complex logic (search ranking, keyboard navigation)
   - TypeScript interfaces fully documented
   - API endpoints documented (XML comments)

4. **Acceptance Criteria Met**:
   - All checkboxes in Section 6.4 checked
   - User stories (PRD) can be executed successfully
   - Performance benchmarks met

5. **Code Review**:
   - Pull request approved
   - No unresolved review comments

6. **Deployment Ready**:
   - Feature flag implemented (optional)
   - Production deployment plan documented
   - Rollback plan documented

---

**Next Step**: Create detailed implementation plan breaking down phases into concrete tasks.
