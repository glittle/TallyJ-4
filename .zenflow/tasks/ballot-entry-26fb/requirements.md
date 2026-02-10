# Product Requirements Document: Enhanced Ballot Entry with Intelligent Name Search

## Overview

This feature enhances the ballot entry workflow to enable fast, accurate, and intuitive vote entry through an intelligent name search system with keyboard-optimized navigation. The system will support comprehensive search capabilities including exact and phonetic matching, with real-time synchronization of name list changes across all users.

## Background

### Current State

The current ballot entry system ([./frontend/src/pages/ballots/BallotEntryPage.vue](./frontend/src/pages/ballots/BallotEntryPage.vue)) uses:
- A modal dialog ([./frontend/src/components/ballots/VoteFormDialog.vue](./frontend/src/components/ballots/VoteFormDialog.vue)) to add individual votes
- Element Plus `el-select` component with remote search
- Server-side search via `PeopleService.SearchPeopleAsync` that searches FirstName, LastName, FullName, Email, and BahaiId fields
- SignalR notifications for person updates (add/update/delete)

### Problems with Current Approach

1. **Performance**: Every search query triggers a server request, causing latency
2. **Limited Search**: Only supports basic substring matching, no phonetic/fuzzy matching
3. **Poor UX for Rapid Entry**: Dialog-based entry interrupts workflow, requires mouse interaction
4. **Missing Phonetic Search**: Database has `CombinedSoundCodes` field (Soundex) but it's unused
5. **No Result Ranking**: Results are sorted alphabetically, not by relevance

### TallyJ v3 Pattern

Based on [v3 UI patterns documentation](../jan-31-review-f-cad9/v3_ui_patterns.md#8-ballot-entry---name-search-sub-feature):
- Large text input with real-time type-ahead
- Dropdown list of matching names (Last, First format)
- Keyboard navigation: Up/Down arrows + Enter to select
- Hover highlighting
- Votes displayed in order as they're added
- Ability to remove votes

## Goals

### Primary Goals

1. **Speed**: Enable tellers to enter ballots quickly with minimal latency
2. **Accuracy**: Comprehensive search that finds correct person even with misspellings or name variations
3. **Efficiency**: Keyboard-only workflow for experienced users
4. **Consistency**: Real-time synchronization when person data changes

### Success Metrics

- Ballot entry time reduced by 50%
- Phonetic search finds intended person on first try 90%+ of the time
- Zero server requests during name search (after initial load)
- 100% keyboard navigability

## Requirements

### Functional Requirements

#### FR-1: Local Name Cache

**Priority**: MUST HAVE

The browser must maintain a local cache of all eligible candidates for the current election.

**Acceptance Criteria**:
- On page load, fetch complete list of people where `CanReceiveVotes = true` for the election
- Cache stored in memory (Pinia store)
- Cache includes all searchable fields: FirstName, LastName, FullName, OtherNames, OtherLastNames, Area, BahaiId, Email
- Cache includes phonetic codes (CombinedSoundCodes or client-computed equivalents)
- Maximum load time: 2 seconds for elections with up to 1000 people

#### FR-2: Comprehensive Search Algorithm

**Priority**: MUST HAVE

Search must find matches using multiple strategies ranked by confidence.

**Search Strategies** (in order of priority):

1. **Exact Match**: Full name matches exactly (case-insensitive)
   - Weight: 100
   - Example: "John Smith" matches "John Smith"

2. **Prefix Match**: Search term is prefix of name
   - Weight: 90
   - Example: "Joh" matches "John Smith"

3. **Substring Match**: Search term appears anywhere in name
   - Weight: 80
   - Example: "Smi" matches "John Smith"

4. **Word Boundary Match**: Each word in search matches start of name parts
   - Weight: 85
   - Example: "J S" matches "John Smith"

5. **Phonetic Match**: Soundex/Metaphone codes match
   - Weight: 60-75 (based on how many codes match)
   - Example: "Jon Smyth" matches "John Smith"

6. **Other Names Match**: Match against OtherNames, OtherLastNames fields
   - Weight: 70
   - Example: "Bob" matches person with FirstName="Robert", OtherNames="Bob"

7. **Fuzzy Match**: Levenshtein distance ≤ 2
   - Weight: 50
   - Example: "Jhon" matches "John"

**Acceptance Criteria**:
- All strategies applied in parallel
- Results sorted by highest weight first
- Ties broken alphabetically by LastName, FirstName
- Search executes in < 50ms for 1000 person database
- Minimum 3 characters before phonetic/fuzzy matching activates
- Maximum 20 results displayed

#### FR-3: Real-Time Search UI

**Priority**: MUST HAVE

Provide an autocomplete-style search interface that updates as user types.

**UI Components**:
- Text input field (large, prominent)
- Dropdown results list (appears below input when typing)
- Each result shows:
  - **Primary**: Full name (Last, First format per v3 convention)
  - **Secondary** (optional): Area, other distinguishing info in smaller/lighter text
- Visual indicator for match type (e.g., icon or badge for phonetic matches)
- Loading state while initial data loads
- Empty state: "No matches found - check spelling or add new person"

**Acceptance Criteria**:
- Search triggers on every keystroke (debounced 150ms)
- Results appear within 100ms of keystroke
- Clear visual hierarchy: exact matches stand out from fuzzy matches
- Accessible ARIA labels for screen readers

#### FR-4: Keyboard Navigation

**Priority**: MUST HAVE

Support complete keyboard-only workflow for rapid entry.

**Keyboard Controls**:

| Key | Action |
|-----|--------|
| **Type** | Filter results in dropdown |
| **↓ (Down Arrow)** | Highlight next result in list |
| **↑ (Up Arrow)** | Highlight previous result in list |
| **Enter** | Select highlighted result, add vote, move to next vote line |
| **Escape** | Clear search, close dropdown |
| **Tab** | Move to next form field (if applicable) |

**Navigation Behavior**:
- Dropdown auto-opens when input receives focus (if has value)
- First result auto-highlighted by default
- Circular navigation: Down on last item → first item
- Visual indicator (background color, border) for highlighted item
- After Enter pressed:
  1. Add vote to ballot at next position
  2. Clear search input
  3. Move focus to next vote line (or same input if more votes needed)

**Acceptance Criteria**:
- All interactions achievable without mouse
- Highlight always visible (e.g., blue background)
- Enter immediately adds vote (no confirmation dialog)
- Tab order logical and documented

#### FR-5: Inline Vote Entry

**Priority**: SHOULD HAVE (decision pending clarification)

Display multiple vote input rows simultaneously for rapid sequential entry.

**UI Layout**:
```
Ballot #A123 - Main Location
Teller: Jane Doe

Vote  Name
---   ---------------------------------
1.    [Search input.....................] [X]
2.    [Search input.....................] [X]
3.    [Search input.....................] [X]
...
9.    [Search input.....................] [X]

[Save Ballot] [Clear All]
```

**Features**:
- Number of rows = Election.NumberToElect (typically 9)
- Each row has independent search input
- [X] button to clear individual vote
- Shows selected person name after selection
- Can edit/replace selected vote by clicking back into field

**Acceptance Criteria**:
- All vote lines visible without scrolling (up to 15)
- Tab key moves between vote lines in order
- Duplicate detection: warn if same person selected twice
- Partially complete ballots can be saved
- Status indicator: "3 of 9 votes entered"

**Alternative** (if inline rejected): Keep dialog-based entry but improve keyboard flow

#### FR-6: Real-Time Person Updates via SignalR

**Priority**: MUST HAVE

Automatically update cached person list when changes occur in other sessions.

**SignalR Events to Handle**:

| Event | Action |
|-------|--------|
| `PersonUpdate` (action: "added") | Add new person to cache if CanReceiveVotes=true |
| `PersonUpdate` (action: "updated") | Update person in cache (name changes, eligibility changes) |
| `PersonUpdate` (action: "deleted") | Remove person from cache |

**Acceptance Criteria**:
- Cache updates are silent (no UI notification)
- If user is actively searching, results refresh automatically
- If person being voted for is updated mid-entry, show toast: "Name updated to [new name]"
- If person being voted for is deleted/made ineligible mid-entry, show warning: "[Name] is no longer eligible"
- Updates apply within 1 second of server event
- No full page reload required

#### FR-7: Duplicate Vote Warning

**Priority**: SHOULD HAVE (decision pending clarification)

Warn user if attempting to add same person multiple times to one ballot.

**Behavior**:
- When selecting a person already on current ballot, show warning:
  - **Toast notification**: "⚠️ [Name] already selected at position X"
  - **Visual indicator**: Highlight duplicate vote row
- User can proceed anyway (some elections allow duplicates during entry for later review)
- Duplicate status shown in ballot status: "Ballot A123 - Contains Duplicates"

**Acceptance Criteria**:
- Check runs on every vote selection
- Warning appears immediately
- User can still save ballot with duplicates
- Duplicate check is case-insensitive and uses PersonGuid

### Non-Functional Requirements

#### NFR-1: Performance

- Initial person list load: < 2s for 1000 people
- Search response time: < 50ms
- UI responsiveness: 60fps during typing/navigation
- Memory usage: < 10MB for person cache

#### NFR-2: Accessibility

- WCAG 2.1 AA compliance
- Keyboard navigation fully functional
- Screen reader support for all actions
- High contrast mode compatible

#### NFR-3: Browser Compatibility

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

#### NFR-4: Data Integrity

- Vote entries validated before save
- PersonGuid must exist and be eligible
- Duplicate prevention at database level (optional)
- Optimistic locking for concurrent ballot edits

## User Stories

### US-1: Rapid Ballot Entry (Primary)

**As a** teller entering paper ballots  
**I want to** quickly search and select candidate names using keyboard only  
**So that** I can process ballots efficiently during time-sensitive vote counting

**Scenario**: Entering a 9-vote ballot
1. Teller opens ballot entry page
2. Types first 3 letters of first candidate
3. Presses Down arrow to highlight match (if not already first)
4. Presses Enter to select
5. Input clears, focus moves to next vote line
6. Repeats steps 2-5 for remaining 8 votes
7. Saves ballot (or auto-saves after each vote)

**Expected**: Complete ballot entry in < 60 seconds

### US-2: Finding Similar Names

**As a** teller  
**I want to** find the correct person even when spelling is uncertain  
**So that** votes are attributed to the right candidate

**Scenario**: Handwriting is unclear
1. Ballot shows name that looks like "Macfarland" or "McFarland"
2. Teller types "macfar"
3. Results show both "MacFarland, John" and "McFarland, Jane"
4. Results also show phonetically similar: "McFarlane, Bob" (lower in list)
5. Teller selects correct match based on other context (e.g., Area)

### US-3: Handling Name Updates

**As a** teller  
**I want to** see updated candidate names without refreshing page  
**So that** I always have current information

**Scenario**: Concurrent name correction
1. Teller A is entering ballots
2. Teller B corrects a name spelling: "Smyth" → "Smith"
3. Teller A's cached list updates silently via SignalR
4. When Teller A searches "smith", updated name appears
5. If Teller A already selected old name, they see: "Name updated to Smith, John"

### US-4: Adding New Person Mid-Entry

**As a** teller  
**I want to** add a new candidate when they're not in the list  
**So that** I don't lose ballot context

**Scenario**: Write-in candidate
1. Teller searches for "Alice Wonderland"
2. No matches found
3. UI shows: "No matches - [Add New Person]" button
4. Clicks button → opens create person dialog (or inline form)
5. Adds person → new person immediately available in search
6. Selects newly added person for current vote

## Open Questions

1. **Entry Mode**: Should ballot entry be inline (multiple vote rows) or dialog-based (one vote at a time)?
   - **Recommendation**: Inline for speed, matches v3 pattern

2. **Auto-Save**: Should votes auto-save individually or require manual "Save Ballot" action?
   - **Recommendation**: Auto-save after each vote for data safety

3. **Duplicate Handling**: Warn only, or block duplicate votes entirely?
   - **Recommendation**: Warn but allow (tellers may need to enter as-written for review)

4. **Search Result Details**: Show Area, Email, BahaiId in results?
   - **Recommendation**: Show Area only (most relevant for disambiguation)

5. **Phonetic Algorithm**: Use existing CombinedSoundCodes (Soundex) or implement client-side algorithm (Metaphone3)?
   - **Recommendation**: Use existing CombinedSoundCodes initially, can enhance later

6. **Maximum Results**: Cap at 10, 20, or unlimited?
   - **Recommendation**: 20 results (balances usefulness with scrolling)

## Out of Scope

- Optical character recognition (OCR) for ballot scanning
- Voice input for name search
- Mobile/tablet optimized view (desktop only for v4.0)
- Machine learning-based name matching
- Multi-language name search (English only)
- Undo/redo for vote entry

## Dependencies

### Backend
- Existing `PeopleService.GetPeopleByElectionAsync` (may need to fetch all at once)
- Existing `MainHub.PersonUpdate` SignalR event
- Existing `Vote` creation endpoints ([./backend/Controllers/VotesController.cs](./backend/Controllers/VotesController.cs))

### Frontend
- Element Plus autocomplete or custom component
- Pinia store for person cache
- SignalR client connection (already implemented in ballot store)
- String matching library (e.g., `fuse.js` for fuzzy search, or custom implementation)

### Database
- `Person.CombinedSoundCodes` field (exists per [reference.md](../reference.md))
- May need to ensure soundcodes are populated for all people

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Large person lists (5000+) slow down browser | High | Medium | Implement pagination or virtual scrolling in results |
| Phonetic matching produces too many false positives | Medium | Medium | Tune weights, allow user to toggle phonetic search off |
| SignalR disconnection causes cache staleness | High | Low | Detect disconnection, show warning, offer manual refresh |
| Keyboard shortcuts conflict with browser defaults | Low | Medium | Use non-conflicting keys, document clearly |
| Duplicate votes cause ballot rejection | Medium | Medium | Clear warnings, allow but flag for review |

## Success Criteria

### MVP (Minimum Viable Product)
- [ ] Local person cache with < 2s load time
- [ ] Real-time search with exact, prefix, and substring matching
- [ ] Keyboard navigation (Up/Down/Enter)
- [ ] SignalR updates to person cache
- [ ] Basic inline vote entry (or improved dialog)

### Complete Feature
- [ ] All search strategies implemented (including phonetic)
- [ ] Result ranking by confidence
- [ ] Duplicate vote warnings
- [ ] Add new person from search interface
- [ ] Performance benchmarks met (< 50ms search)

### Future Enhancements
- [ ] Batch ballot entry (import from CSV)
- [ ] Voice input integration
- [ ] Machine learning for improved matching
- [ ] Multi-language support
- [ ] Mobile-optimized interface

## References

- [TallyJ v3 UI Patterns - Ballot Entry](../jan-31-review-f-cad9/v3_ui_patterns.md#8-ballot-entry---name-search-sub-feature)
- [Current Ballot Entry Page](./frontend/src/pages/ballots/BallotEntryPage.vue)
- [Current Vote Form Dialog](./frontend/src/components/ballots/VoteFormDialog.vue)
- [People Service Backend](./backend/Services/PeopleService.cs)
- [Person Entity Reference](../reference.md)
- [SignalR Person Update DTO](./backend/DTOs/SignalR/PersonUpdateDto.cs)
