# Technical Specification: TallyJ4 - Phase 4: Frontend Application

## 1. Overview

**Phase**: Phase 4 - Frontend Application  
**Objective**: Build a comprehensive Vue 3 SPA that provides election management, ballot entry, result viewing, and tally calculation interfaces.

**Complexity**: **Hard** - Complex UI/UX with multiple user roles, real-time data, forms, and state management.

---

## 2. Technical Context

### 2.1 Technology Stack

**Frontend**:
- Vue 3.5.22 (Composition API with `<script setup>`)
- TypeScript 5.9.3
- Vite (build tool - using rolldown-vite@7.1.14)
- Pinia 3.0.3 (state management)
- Vue Router 4.6.3
- Element Plus 2.11.5 (UI component library)
- Axios 1.13.2 (HTTP client)
- Vue I18n 10.0.8 (internationalization)
- @microsoft/signalr 9.0.6 (real-time updates - Phase 5)

**Backend API** (Already Complete):
- .NET 10.0 REST API
- 8 controllers with 40+ endpoints
- JWT bearer authentication
- Phase 3 tally algorithms implemented

### 2.2 Current Frontend State

**Implemented** ✅:
- Basic project structure
- Login/Register pages
- Auth service and Pinia auth store
- Router setup with 3 routes
- i18n (English, French)
- Element Plus integration
- Language selector component

**Project Structure**:
```
frontend/
├── src/
│   ├── assets/          # Static assets
│   ├── components/
│   │   └── common/      # LanguageSelector.vue
│   ├── locales/         # en.json, fr.json, index.ts
│   ├── pages/           # LoginPage.vue, RegisterPage.vue, HelloWorld.vue
│   ├── router/          # router.ts
│   ├── services/        # api.ts, authService.ts
│   ├── stores/          # authStore.ts
│   ├── App.vue
│   ├── main.ts
│   └── style.css
├── package.json
├── tsconfig.json
├── vite.config.ts
└── index.html
```

### 2.3 User Roles & Permissions

Based on reverse engineering documentation:

1. **Administrator** - Full access to election management
2. **Head Teller** - Create/manage elections, assign tellers
3. **Teller** - Enter ballots, view results
4. **Voter** - Vote online (Phase 5)
5. **Observer** - View results (read-only)

For Phase 4, focus on **Administrator** and **Teller** roles.

---

## 3. Implementation Approach

### 3.1 Application Structure

**Layout System**:
```
MainLayout (authenticated users)
├── AppHeader (logo, user menu, language selector)
├── AppSidebar (navigation menu)
└── RouterView (page content)

PublicLayout (unauthenticated)
├── PublicHeader (logo, language selector)
└── RouterView (login, register)
```

**Page Hierarchy**:
```
/ (redirect to /login or /dashboard)
/login                    - LoginPage
/register                 - RegisterPage
/dashboard                - Dashboard (election overview)
/elections                - ElectionListPage
/elections/create         - CreateElectionPage
/elections/:id            - ElectionDetailPage
/elections/:id/people     - PeopleManagementPage
/elections/:id/ballots    - BallotManagementPage
/elections/:id/results    - ResultsPage
/elections/:id/tally      - TallyCalculationPage
/profile                  - UserProfilePage
```

### 3.2 State Management Strategy

**Pinia Stores**:

1. **authStore** (existing) - User authentication state
   - currentUser
   - isAuthenticated
   - login(), logout(), register()

2. **electionStore** (new) - Elections data
   - elections: Election[]
   - currentElection: Election | null
   - fetchElections()
   - fetchElectionById(id)
   - createElection(dto)
   - updateElection(id, dto)
   - deleteElection(id)

3. **peopleStore** (new) - People/candidates data
   - people: Person[]
   - fetchPeople(electionId)
   - createPerson(dto)
   - updatePerson(id, dto)
   - deletePerson(id)
   - searchPeople(term)

4. **ballotStore** (new) - Ballots and votes
   - ballots: Ballot[]
   - votes: Vote[]
   - fetchBallots(electionId)
   - createBallot(dto)
   - createVote(ballotId, personId)

5. **resultStore** (new) - Election results
   - results: TallyResult | null
   - statistics: TallyStatistics | null
   - calculateTally(electionId, type)
   - fetchResults(electionId)
   - fetchStatistics(electionId)

### 3.3 API Service Layer

**Service Architecture**:
```
services/
├── api.ts                # Axios instance with auth interceptor
├── authService.ts        # Login, register, refresh token (existing)
├── electionService.ts    # Election CRUD operations
├── peopleService.ts      # People CRUD operations
├── ballotService.ts      # Ballot CRUD operations
├── voteService.ts        # Vote CRUD operations
├── resultService.ts      # Tally calculation, results retrieval
└── types/
    ├── Election.ts       # Election type definitions
    ├── Person.ts         # Person type definitions
    ├── Ballot.ts         # Ballot type definitions
    ├── Result.ts         # Result type definitions
    └── ApiResponse.ts    # Generic API response types
```

**Type Generation Option**:
- Use OpenAPI/Swagger endpoint to generate TypeScript types
- Package: `@hey-api/openapi-ts` (already in devDependencies)
- Command: `npm run gen` (already in package.json)

### 3.4 UI Component Library

**Element Plus Components to Use**:
- **Layout**: `el-container`, `el-header`, `el-aside`, `el-main`
- **Navigation**: `el-menu`, `el-menu-item`, `el-breadcrumb`
- **Forms**: `el-form`, `el-input`, `el-select`, `el-date-picker`, `el-switch`
- **Data Display**: `el-table`, `el-pagination`, `el-card`, `el-descriptions`
- **Feedback**: `el-message`, `el-dialog`, `el-loading`, `el-alert`
- **Buttons**: `el-button`, `el-button-group`, `el-dropdown`

**Custom Components**:
- `ElectionCard.vue` - Election summary card
- `PeopleTable.vue` - People data table with actions
- `BallotEntryForm.vue` - Ballot entry interface
- `ResultsTable.vue` - Results display with sections (Elected/Extra/Other)
- `TieIndicator.vue` - Visual indicator for tied candidates
- `StatisticsPanel.vue` - Election statistics summary

---

## 4. Implementation Tasks

### Phase 4.1: Project Structure & Routing
**Estimated Time**: 4 hours

**Tasks**:
1. Create layout components (MainLayout, PublicLayout)
2. Create AppHeader, AppSidebar, AppFooter components
3. Configure router with all routes and auth guards
4. Create placeholder pages for all routes
5. Add route transitions and loading states

**Files Created**:
```
src/
├── layouts/
│   ├── MainLayout.vue
│   └── PublicLayout.vue
├── components/
│   ├── AppHeader.vue
│   ├── AppSidebar.vue
│   └── AppFooter.vue
├── router/
│   ├── router.ts (update)
│   └── guards.ts (auth guards)
└── pages/
    ├── DashboardPage.vue
    ├── elections/
    │   ├── ElectionListPage.vue
    │   ├── CreateElectionPage.vue
    │   ├── ElectionDetailPage.vue
    │   ├── PeopleManagementPage.vue
    │   ├── BallotManagementPage.vue
    │   └── ResultsPage.vue
    └── ProfilePage.vue
```

**Verification**:
- All routes accessible
- Auth guard prevents access without login
- Navigation menu highlights active route
- Page transitions smooth

---

### Phase 4.2: API Service Layer
**Estimated Time**: 6 hours

**Tasks**:
1. Generate TypeScript types from OpenAPI spec (`npm run gen`)
2. Create service files for each domain (elections, people, ballots, results)
3. Implement CRUD operations for each service
4. Add error handling and response interceptors
5. Create TypeScript interfaces for DTOs

**Files Created**:
```
src/
├── services/
│   ├── electionService.ts
│   ├── peopleService.ts
│   ├── ballotService.ts
│   ├── voteService.ts
│   └── resultService.ts
└── types/
    ├── Election.ts
    ├── Person.ts
    ├── Ballot.ts
    ├── Vote.ts
    ├── Result.ts
    └── ApiResponse.ts
```

**Verification**:
- TypeScript types match backend DTOs
- API calls return properly typed data
- Error handling works correctly
- Auth token included in requests

---

### Phase 4.3: State Management (Pinia Stores)
**Estimated Time**: 6 hours

**Tasks**:
1. Create electionStore with CRUD actions
2. Create peopleStore with CRUD actions
3. Create ballotStore with CRUD actions
4. Create resultStore with tally actions
5. Add loading states and error handling
6. Implement data caching strategies

**Files Created**:
```
src/
└── stores/
    ├── electionStore.ts
    ├── peopleStore.ts
    ├── ballotStore.ts
    └── resultStore.ts
```

**Verification**:
- Stores fetch and cache data correctly
- State updates reactively in components
- Loading states work
- Error states handled gracefully

---

### Phase 4.4: Dashboard Page
**Estimated Time**: 4 hours

**Tasks**:
1. Create DashboardPage.vue with election overview
2. Display election cards with status (Setting Up, Voting, Tallying, Finalized)
3. Show quick statistics (total elections, active elections, voters, ballots)
4. Add "Create New Election" button
5. Add recent activity feed (optional)

**Components**:
- `DashboardPage.vue`
- `ElectionCard.vue`
- `QuickStats.vue`

**Verification**:
- Dashboard loads quickly
- Election cards link to detail pages
- Statistics accurate
- Responsive layout

---

### Phase 4.5: Election Management Pages
**Estimated Time**: 8 hours

**Tasks**:
1. Create ElectionListPage with table view and filters
2. Implement CreateElectionPage with multi-step form
3. Build ElectionDetailPage with tabs (Overview, People, Ballots, Results)
4. Add election edit functionality
5. Implement election deletion with confirmation

**Components**:
- `ElectionListPage.vue`
- `CreateElectionPage.vue`
- `ElectionDetailPage.vue`
- `ElectionForm.vue`
- `ElectionFilters.vue`

**Form Fields (CreateElectionPage)**:
- Name (required)
- Election Type (LSA, Unit, Convention, etc.)
- Date of Election
- Number to Elect
- Number Extra
- Voting Methods (In-Person, Online, etc.)
- Description

**Verification**:
- Create election works end-to-end
- Validation prevents invalid submissions
- Edit updates election correctly
- Delete removes election and related data

---

### Phase 4.6: People Management Page
**Estimated Time**: 8 hours

**Tasks**:
1. Create PeopleManagementPage with searchable table
2. Implement "Add Person" form
3. Add bulk import CSV functionality (UI only, backend already exists)
4. Implement person edit/delete
5. Add filters (Can Vote, Can Receive Votes, status)
6. Implement search by name

**Components**:
- `PeopleManagementPage.vue`
- `PeopleTable.vue`
- `PersonForm.vue`
- `ImportCsvDialog.vue`

**Table Columns**:
- Full Name
- Can Vote (checkbox/badge)
- Can Receive Votes (checkbox/badge)
- Email (optional)
- Phone (optional)
- Actions (Edit, Delete)

**Verification**:
- Add person creates record
- Edit updates person
- Delete removes person
- Search filters table
- CSV import works (if implemented)

---

### Phase 4.7: Ballot Management Page
**Estimated Time**: 6 hours

**Tasks**:
1. Create BallotManagementPage with ballot list
2. Implement "Create Ballot" form
3. Build ballot entry interface (select candidates)
4. Add vote management within ballot
5. Show ballot status (Ok, Spoiled, etc.)

**Components**:
- `BallotManagementPage.vue`
- `BallotTable.vue`
- `BallotEntryForm.vue`
- `VoteSelector.vue`

**Ballot Entry Flow**:
1. Create new ballot (generates ballot code)
2. Select location
3. Add votes (select people, up to NumberToElect)
4. Save ballot

**Verification**:
- Create ballot works
- Add votes to ballot
- Ballot validation (can't add more votes than allowed)
- Ballot status updates correctly

---

### Phase 4.8: Results & Tally Page
**Estimated Time**: 8 hours

**Tasks**:
1. Create ResultsPage with tally display
2. Implement "Calculate Tally" button
3. Display results in sections (Elected, Extra, Other)
4. Show tie indicators and tie-break requirements
5. Display election statistics
6. Add export functionality (print-friendly view)

**Components**:
- `ResultsPage.vue`
- `ResultsTable.vue`
- `TieIndicator.vue`
- `StatisticsPanel.vue`
- `TallyControls.vue`

**Results Display**:
- **Elected Section** (green background)
  - Rank, Name, Vote Count, Tie indicator
- **Extra Section** (yellow background)
  - Rank, Name, Vote Count, Tie indicator
- **Other Section** (gray background)
  - Rank, Name, Vote Count

**Statistics Panel**:
- Total Ballots
- Ballots Received
- Spoiled Ballots
- Total Votes
- Valid Votes
- Invalid Votes
- Number to Elect
- Number Extra

**Tie Visualization**:
- Tied candidates highlighted
- Tie-break required badge (red)
- Tie group number displayed

**Verification**:
- Calculate tally triggers backend API
- Results display correctly by section
- Ties highlighted properly
- Statistics accurate
- Export/print works

---

### Phase 4.9: UI Polish & Responsiveness
**Estimated Time**: 4 hours

**Tasks**:
1. Ensure all pages are responsive (mobile, tablet, desktop)
2. Add loading skeletons
3. Implement error boundaries
4. Add empty states (no elections, no people, etc.)
5. Improve form validation feedback
6. Add confirmation dialogs for destructive actions

**Verification**:
- App works on mobile devices
- Loading states clear
- Errors don't crash app
- Empty states informative

---

### Phase 4.10: Testing & Documentation
**Estimated Time**: 4 hours

**Tasks**:
1. Add Vitest or Jest for unit tests
2. Write component tests for key components
3. Test store actions
4. Create user guide/help documentation
5. Document component props and events

**Verification**:
- Tests pass
- Coverage >60%
- Documentation clear

---

## 5. Data Flow Examples

### Example 1: Create Election Flow

```
User fills CreateElectionPage form
    ↓
Click "Create Election"
    ↓
electionStore.createElection(dto)
    ↓
electionService.createElection(dto) → POST /api/elections
    ↓
Backend returns ElectionDto
    ↓
Store adds to elections array
    ↓
Router navigates to /elections/:id
    ↓
ElectionDetailPage displays new election
```

### Example 2: Calculate Tally Flow

```
User on ResultsPage clicks "Calculate Tally"
    ↓
resultStore.calculateTally(electionId, 'normal')
    ↓
resultService.calculateTally(electionId) → POST /api/results/election/{id}/calculate
    ↓
Backend runs tally algorithm (Phase 3)
    ↓
Returns TallyResultDto with results, statistics, ties
    ↓
Store updates results state
    ↓
ResultsTable re-renders with new data
    ↓
Ties and statistics displayed
```

---

## 6. Success Criteria

**Must Have** (Phase 4 Complete):
1. ✅ User can login/logout
2. ✅ Dashboard shows election overview
3. ✅ User can create new election
4. ✅ User can view election list
5. ✅ User can add/edit/delete people
6. ✅ User can create ballots and votes
7. ✅ User can calculate tally
8. ✅ User can view results with sections and ties
9. ✅ All pages responsive
10. ✅ Error handling works
11. ✅ Build succeeds with no errors

**Nice to Have** (Optional):
- ❌ CSV import for people
- ❌ Print/export results
- ❌ Advanced filtering and search
- ❌ Keyboard shortcuts
- ❌ Dark mode
- ❌ Unit tests (>60% coverage)

---

## 7. API Endpoints Used

**Authentication**:
- POST `/auth/login` - User login
- POST `/auth/register` - User registration
- POST `/auth/refresh` - Refresh token

**Elections**:
- GET `/api/elections?pageNumber=1&pageSize=10` - List elections
- GET `/api/elections/{guid}` - Get election by ID
- POST `/api/elections` - Create election
- PUT `/api/elections/{guid}` - Update election
- DELETE `/api/elections/{guid}` - Delete election

**People**:
- GET `/api/people/election/{electionGuid}` - Get people by election
- GET `/api/people/{guid}` - Get person by ID
- POST `/api/people` - Create person
- PUT `/api/people/{guid}` - Update person
- DELETE `/api/people/{guid}` - Delete person
- GET `/api/people/search?electionGuid={guid}&searchTerm={term}` - Search people

**Ballots**:
- GET `/api/ballots/election/{electionGuid}` - Get ballots by election
- POST `/api/ballots` - Create ballot
- PUT `/api/ballots/{guid}` - Update ballot
- DELETE `/api/ballots/{guid}` - Delete ballot

**Votes**:
- GET `/api/votes/ballot/{ballotGuid}` - Get votes by ballot
- POST `/api/votes` - Create vote
- DELETE `/api/votes/{guid}` - Delete vote

**Results**:
- POST `/api/results/election/{guid}/calculate?electionType=normal` - Calculate tally
- GET `/api/results/election/{guid}` - Get results
- GET `/api/results/election/{guid}/summary` - Get statistics
- GET `/api/results/election/{guid}/final` - Get final results (Elected+Extra only)

---

## 8. Development Workflow

### Initial Setup
```bash
cd frontend
npm install
npm run gen  # Generate TypeScript types from OpenAPI
npm run dev  # Start dev server
```

### Build & Deploy
```bash
npm run build    # Production build
npm run preview  # Preview production build
```

### Backend Integration
```bash
# Terminal 1: Backend
cd backend
dotnet run

# Terminal 2: Frontend
cd frontend
npm run dev
```

**CORS Configuration**:
Backend already configured to allow `http://localhost:5173` (Vite default port).

---

## 9. Risk Assessment

**Risk**: TypeScript type mismatches with backend DTOs  
**Mitigation**: Use OpenAPI code generation (`npm run gen`)

**Risk**: State management complexity  
**Mitigation**: Keep stores simple, one per domain, avoid over-engineering

**Risk**: Performance with large datasets  
**Mitigation**: Use pagination, virtual scrolling for tables, lazy loading

**Risk**: Browser compatibility  
**Mitigation**: Vite/Vue 3 targets modern browsers, test on Chrome, Firefox, Safari, Edge

**Risk**: Authentication token expiry  
**Mitigation**: Implement token refresh logic in axios interceptor

---

## 10. Phase 5 Preparation

Phase 4 sets the foundation for Phase 5: Real-time Features

**SignalR Integration Points** (Phase 5):
- Real-time tally updates during calculation
- Live ballot count updates
- Multi-teller collaboration notifications
- Result broadcasting to observers

**Frontend Changes Needed (Phase 5)**:
- Add SignalR hub connection in `main.ts`
- Create `signalrService.ts`
- Update stores to listen to SignalR events
- Add real-time indicators in UI

---

## 11. Conclusion

Phase 4 transforms the backend API into a fully functional election management application. The Vue 3 SPA provides:
- Intuitive election creation and management
- Efficient people and ballot data entry
- Powerful tally calculation with visual results
- Responsive, modern UI using Element Plus
- Type-safe TypeScript throughout

**Estimated Total Time**: 50-60 hours (1.5-2 weeks for 1 developer)

**Assessment Date**: January 21, 2026  
**Phase**: Phase 4 - Frontend Application  
**Status**: Ready to implement  
**Complexity**: Hard - Complex UI/UX, multiple user flows, state management
