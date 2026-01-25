# Technical Specification: TallyJ4 - Phase 4 Completion & System Integration

## 1. Overview

**Phase**: Phase 4 - Frontend Application Completion & System Integration  
**Objective**: Complete the Vue 3 frontend application and ensure full end-to-end integration with the backend API.

**Complexity**: **Medium** - Frontend foundation is substantial (~95% complete), requiring integration testing, bug fixes, and polish.

---

## 2. Technical Context

### 2.1 Technology Stack

**Backend** (Complete ✅):
- .NET 10.0 ASP.NET Core Web API
- Entity Framework Core 10.0
- SQL Server
- JWT Bearer authentication
- Phase 3 tally algorithms fully implemented

**Frontend** (95% Complete):
- Vue 3.5.22 (Composition API)
- TypeScript 5.9.3
- Vite 7.1.14 (rolldown-vite)
- Pinia 3.0.3 (state management)
- Vue Router 4.6.3
- Element Plus 2.11.5 (UI library)
- Axios 1.13.2
- Vue I18n 10.0.8

### 2.2 Current State Assessment

**Backend Status**:
- ✅ Build: Successful (0 errors, 2 NuGet warnings)
- ✅ Unit Tests: 26/26 passing
- ❌ Integration Tests: 15 failing (path issue - referencing old worktree `jan-20-6eb4`)
- ✅ API: 8 controllers, 40+ endpoints functional
- ✅ Tally Algorithms: Fully implemented and tested

**Frontend Status**:
- ✅ Build: Successful (chunk size warning only)
- ✅ Project Structure: Complete
- ✅ Routing: 10 routes with auth guards
- ✅ Layouts: MainLayout, PublicLayout
- ✅ Pages: 11 pages implemented
  - LoginPage, RegisterPage ✅
  - DashboardPage ✅
  - ElectionListPage, CreateElectionPage, ElectionDetailPage ✅
  - PeopleManagementPage ✅
  - BallotManagementPage ✅
  - ResultsPage, TallyCalculationPage ✅
  - ProfilePage ✅
- ✅ Components: 14+ components
  - AppHeader, AppSidebar ✅
  - LanguageSelector ✅
  - Ballot dialogs (BallotFormDialog, BallotVotesDialog, VoteFormDialog) ✅
  - People components (PersonFormDialog, PeopleTable) ✅
  - Results components (ResultsTable, TiesDisplay) ✅
- ✅ Stores: 5 Pinia stores
  - authStore, electionStore, peopleStore, ballotStore, resultStore ✅
- ✅ Services: 6 API services
  - api, authService, electionService, peopleService, ballotService, voteService, resultService ✅
- ✅ Types: TypeScript types for all entities ✅
- ✅ i18n: English translations comprehensive, French partial

**Known Issues**:
1. Integration tests reference wrong path (`jan-20-6eb4` instead of `contineu-b1db`)
2. Frontend not yet tested against live backend
3. End-to-end workflows not verified
4. Edge cases and error handling need validation
5. Vue I18n deprecated version (v10.0.8 - should upgrade to v11)
6. Missing some French translations

---

## 3. Implementation Approach

### 3.1 Fix Integration Test Path Issue

**Problem**: Integration tests reference hardcoded path `C:\Users\glenl\.zenflow\worktrees\jan-20-6eb4\backend\` which doesn't exist in current worktree.

**Root Cause**: Test infrastructure or Program.cs uses absolute path instead of relative path.

**Solution**: 
- Update `CustomWebApplicationFactory` or `IntegrationTestBase` to use correct content root path
- Use `Directory.GetCurrentDirectory()` or relative path resolution
- Ensure tests run from correct working directory

### 3.2 End-to-End Integration Testing

**Approach**:
1. Start backend API server
2. Start frontend dev server
3. Manually test critical user workflows:
   - User registration and login
   - Create election
   - Add people to election
   - Create ballots and votes
   - Calculate tally
   - View results
4. Verify API error handling in UI
5. Test authentication token refresh
6. Validate form validation works
7. Check responsive design on different screen sizes

### 3.3 Bug Fixes and Polish

**Tasks**:
- Fix any runtime errors discovered during testing
- Improve error messages and user feedback
- Add loading states where missing
- Ensure consistent styling
- Add proper empty states
- Validate all forms properly
- Handle API errors gracefully

### 3.4 Documentation Updates

**Tasks**:
- Update README with frontend setup instructions
- Document environment variables needed
- Create user guide (optional)
- Update API examples if needed

---

## 4. Source Code Structure

### 4.1 Files Requiring Updates

**Backend Tests**:
```
TallyJ4.Tests/
├── IntegrationTests/
│   ├── IntegrationTestBase.cs       [FIX PATH]
│   └── CustomWebApplicationFactory.cs  [FIX PATH or create if missing]
```

**Frontend** (minimal changes expected):
```
frontend/
├── src/
│   ├── locales/
│   │   └── fr.json                  [ADD MISSING TRANSLATIONS]
│   └── services/
│       └── api.ts                   [VERIFY ERROR HANDLING]
├── .env.development                 [VERIFY API URL CONFIG]
└── package.json                     [OPTIONAL: UPGRADE VUE-I18N]
```

### 4.2 Configuration Files

**Frontend Environment Variables**:
```
VITE_API_BASE_URL=http://localhost:5000
```

**Backend appsettings**:
- Already configured correctly
- No changes needed

---

## 5. Implementation Tasks

### Task 1: Fix Integration Test Path Issue
**Priority**: HIGH  
**Estimated Time**: 30 minutes

**Steps**:
1. Locate path reference in test infrastructure
2. Change to relative path or `Directory.GetCurrentDirectory()`
3. Run `dotnet test` to verify all tests pass

**Success Criteria**:
- All 41 tests pass (26 unit + 15 integration)
- No path-related errors

---

### Task 2: Start Backend and Frontend Servers
**Priority**: HIGH  
**Estimated Time**: 15 minutes

**Steps**:
1. Start backend: `cd backend && dotnet run`
2. Start frontend: `cd frontend && npm run dev`
3. Verify both servers running
4. Verify frontend can reach backend API

**Success Criteria**:
- Backend API running on `http://localhost:5000`
- Frontend dev server running on `http://localhost:5173`
- API accessible from frontend (CORS configured correctly)

---

### Task 3: End-to-End Testing - Authentication
**Priority**: HIGH  
**Estimated Time**: 30 minutes

**Test Cases**:
1. Register new user → success
2. Login with valid credentials → redirect to dashboard
3. Login with invalid credentials → error message
4. Logout → redirect to login
5. Access protected route without auth → redirect to login
6. Token refresh works after expiration

**Success Criteria**:
- All authentication flows work correctly
- Error messages clear and helpful
- No console errors

---

### Task 4: End-to-End Testing - Election Management
**Priority**: HIGH  
**Estimated Time**: 1 hour

**Test Cases**:
1. Dashboard loads with statistics
2. Create election with valid data → success
3. Create election with invalid data → validation errors
4. View election list with pagination
5. View election details
6. Edit election → changes saved
7. Delete election → confirmation dialog → deleted

**Success Criteria**:
- All CRUD operations work
- Form validation prevents invalid submissions
- API errors displayed to user
- Loading states shown during API calls

---

### Task 5: End-to-End Testing - People Management
**Priority**: HIGH  
**Estimated Time**: 45 minutes

**Test Cases**:
1. Navigate to people page for election
2. Add person with valid data → appears in table
3. Search people by name → filtered results
4. Edit person → changes saved
5. Delete person → removed from table
6. Table pagination works

**Success Criteria**:
- People CRUD operations work
- Search functionality works
- Table displays data correctly

---

### Task 6: End-to-End Testing - Ballot & Vote Entry
**Priority**: HIGH  
**Estimated Time**: 1 hour

**Test Cases**:
1. Navigate to ballots page
2. Create ballot → opens vote entry dialog
3. Add votes for ballot → vote count accurate
4. View votes for ballot → correct votes shown
5. Edit ballot → changes saved
6. Delete ballot → removed

**Success Criteria**:
- Ballot entry workflow intuitive
- Vote counting accurate
- Forms validate correctly

---

### Task 7: End-to-End Testing - Tally Calculation & Results
**Priority**: CRITICAL  
**Estimated Time**: 1 hour

**Test Cases**:
1. Navigate to tally calculation page
2. Select election type (Normal/SingleName)
3. Click calculate → tally runs successfully
4. View results preview → statistics accurate
5. Navigate to full results page
6. Verify sections (Elected/Extra/Other) correct
7. Verify tie indicators shown
8. Check vote counts match expected

**Success Criteria**:
- Tally calculation completes successfully
- Results display correctly
- Statistics accurate
- Ties detected and displayed
- Sections categorized properly

---

### Task 8: Bug Fixes and Polish
**Priority**: MEDIUM  
**Estimated Time**: 2 hours

**Tasks**:
- Fix any bugs discovered during testing
- Improve error handling and user feedback
- Add missing loading indicators
- Ensure consistent styling
- Test responsive design
- Add empty states where missing
- Improve form validation messages

**Success Criteria**:
- No critical bugs remain
- User experience smooth and intuitive
- Application looks professional

---

### Task 9: French Translation Completion (Optional)
**Priority**: LOW  
**Estimated Time**: 1 hour

**Tasks**:
1. Review `frontend/src/locales/en.json`
2. Add missing translations to `fr.json`
3. Test language switching

**Success Criteria**:
- French translations complete
- Language switching works smoothly
- No untranslated strings

---

### Task 10: Documentation Updates
**Priority**: LOW  
**Estimated Time**: 1 hour

**Tasks**:
1. Update README with frontend setup
2. Add environment variable documentation
3. Document deployment steps
4. Create troubleshooting section

**Success Criteria**:
- README clear and complete
- New developers can set up project easily

---

## 6. Test Scenarios

### Critical Path Testing

**Scenario 1: Complete Election Workflow**
1. Register user
2. Login
3. Create election "Test LSA 2026"
4. Add 15 people (9 eligible to receive votes)
5. Create 10 ballots with votes
6. Calculate tally
7. View results
8. Verify elected candidates correct

**Expected Outcome**: Complete workflow succeeds without errors

**Scenario 2: Error Handling**
1. Try to create election without name → validation error
2. Try to add invalid ballot → API error shown
3. Try to calculate tally with 0 ballots → appropriate message
4. Lose network connection → error handling works

**Expected Outcome**: All errors handled gracefully with clear messages

**Scenario 3: Multi-User Simulation**
1. Create election as admin
2. Assign teller role (future feature)
3. Teller enters ballots
4. Admin calculates tally
5. Both users view results

**Expected Outcome**: Data syncs correctly between users

---

## 7. Success Criteria

### Must Have (Phase 4 Complete):
1. ✅ Backend builds and runs (0 errors)
2. ✅ Frontend builds and runs (0 errors)
3. ❌ All backend tests pass (26 unit + 15 integration = 41 total)
4. ❌ End-to-end authentication works
5. ❌ Election CRUD operations work
6. ❌ People CRUD operations work
7. ❌ Ballot/Vote entry works
8. ❌ Tally calculation works end-to-end
9. ❌ Results display correctly
10. ❌ No critical bugs

### Nice to Have:
- French translations complete
- Responsive design optimized
- Performance optimization
- Advanced search features
- Batch operations
- CSV import UI complete

---

## 8. Risk Mitigation

**Risk**: API CORS issues prevent frontend from accessing backend  
**Mitigation**: Backend CORS already configured, verify during testing

**Risk**: Authentication token expiration causes errors  
**Mitigation**: Token refresh already implemented in authStore, test thoroughly

**Risk**: Large elections cause performance issues  
**Mitigation**: Backend tally algorithm already optimized, frontend pagination implemented

**Risk**: Integration tests continue to fail  
**Mitigation**: Path fix should resolve, if not, integration tests are supplementary to unit tests

---

## 9. Verification Steps

### Build Verification
```bash
# Backend
cd backend
dotnet build
# Expected: 0 errors, 2 warnings (NuGet only)

# Frontend
cd frontend
npm install
npm run build
# Expected: 0 errors, 1 warning (chunk size)
```

### Test Verification
```bash
cd TallyJ4.Tests
dotnet test
# Expected: 41/41 tests passing (after path fix)
```

### Runtime Verification
```bash
# Terminal 1: Backend
cd backend
dotnet run

# Terminal 2: Frontend
cd frontend
npm run dev

# Browser: http://localhost:5173
# Test: Register, login, create election, calculate tally
```

---

## 10. Notes

- **Phase 3 Complete**: Tally algorithms fully implemented and tested (28 unit tests passing)
- **Phase 4 Foundation**: ~95% of frontend code already implemented
- **Focus Area**: Integration testing and bug fixes, not new development
- **Timeline**: Phase 4 can be completed in 1-2 days of focused testing and fixes
- **Next Phase**: Phase 5 (SignalR real-time features) is planned but not started
