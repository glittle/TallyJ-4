# Technical Specifications: TallyJ 4 System Components

## Frontend Application Specification

### Technical Context

- **Language**: TypeScript
- **Framework**: Vue 3 with Composition API
- **UI Library**: Element Plus
- **State Management**: Pinia
- **Routing**: Vue Router
- **HTTP Client**: Axios with interceptors
- **Real-time**: SignalR
- **Build Tool**: Vite
- **Internationalization**: Vue I18n
- **Backend API**: RESTful API with JWT authentication

### Existing Codebase Architecture

The frontend already has a comprehensive structure:

- **Pages**: 15+ page components for elections, people, ballots, results, auth
- **Components**: Reusable UI components organized by feature
- **Stores**: Pinia stores for state management (auth, election, people, ballot, result)
- **Services**: API service layer with manual type definitions
- **Layouts**: Main and public layouts with navigation
- **Routing**: Complete route configuration with guards
- **Internationalization**: English and French locales, with plans for many other languages to be added
- **SignalR**: Real-time communication setup with multiple hubs

### Current Implementation Status

✅ **Completed Features**:

- Authentication (login/register/2FA)
- Dashboard with statistics
- Election CRUD operations
- People management with search/filtering
- Ballot management
- Results display and calculation
- User profile management
- Responsive design with Element Plus
- Internationalization support
- SignalR infrastructure

❌ **Known Issues**:

- OpenAPI type generation failing (large spec truncated)
- Manual API types may not match backend
- Real-time features not fully integrated into UI
- Some pages may have incomplete functionality
- Missing error handling in some components
- No end-to-end testing

### Implementation Approach

1. **Fix OpenAPI Generation**: Resolve the spec download/generation issue to ensure type safety
2. **Complete Real-time Integration**: Connect SignalR events to UI updates
3. **Enhance Error Handling**: Add comprehensive error states and user feedback
4. **Add Missing Features**: Implement any incomplete CRUD operations or UI flows
5. **Performance Optimization**: Code splitting, lazy loading, and bundle optimization
6. **Testing**: Add component and integration tests
7. **Polish**: UI/UX improvements, accessibility, and responsive design

### Source Code Structure Changes

- **New Files**: Generated API client types, additional test files
- **Modified Files**: Fix OpenAPI config, enhance existing components with real-time features
- **Configuration**: Update build scripts, add environment variables

### Data Model / API / Interface Changes

- **API Client**: Generate from OpenAPI spec instead of manual types
- **Real-time Events**: Define SignalR event handlers for live updates
- **Error Responses**: Standardize error handling across components

### Delivery Phases

1. **Fix OpenAPI Generation**
   - Resolve spec download issue (size limit)
   - Generate type-safe API client
   - Update services to use generated client

2. **Real-time Features Implementation**
   - Connect election result updates
   - Live ballot status changes
   - Real-time notifications

3. **UI/UX Enhancements**
   - Loading states and skeletons
   - Error boundaries and messages
   - Confirmation dialogs
   - Toast notifications

4. **Performance and Optimization**
   - Lazy load routes
   - Optimize bundle size
   - Image optimization
   - Caching strategies

5. **Testing and Quality**
   - Unit tests for components
   - Integration tests for stores
   - E2E test scripts

### Verification Approach

- **Build**: `npm run build` succeeds without TypeScript errors
- **Lint**: Add ESLint configuration and verify code quality
- **Tests**: `npm run test` passes all unit tests
- **E2E**: Manual testing of all user flows
- **Performance**: Bundle size under 1MB, fast loading times
- **Real-time**: SignalR connections work for live updates

---

## Real-time Features Specification

### Technical Context

- **Frontend**: Vue 3 + TypeScript + Pinia stores
- **Backend**: .NET 10.0 ASP.NET Core with SignalR hubs
- **Real-time Protocol**: SignalR with automatic reconnection
- **Authentication**: JWT tokens for hub connections

### Implementation Approach

#### Current State Analysis

- SignalR infrastructure is fully implemented in backend (5 hubs: Main, Analyze, BallotImport, FrontDesk, Public)
- Frontend has SignalR service layer and composables
- Stores have SignalR initialization methods but are not called in pages
- Real-time events are defined but not integrated into UI components

#### Integration Strategy

1. **Page-level SignalR Initialization**: Initialize SignalR connections when entering election-specific pages
2. **Group Management**: Join/leave election groups for targeted broadcasts
3. **Real-time UI Updates**: Display progress bars, live data updates, and notifications
4. **Connection Lifecycle**: Handle connection states and cleanup on page navigation

### Source Code Structure Changes

#### Modified Files

- `frontend/src/pages/elections/ElectionDetailPage.vue` - Initialize election SignalR
- `frontend/src/pages/results/TallyCalculationPage.vue` - Add real-time progress display
- `frontend/src/pages/people/PeopleManagementPage.vue` - Real-time people updates
- `frontend/src/pages/ballots/BallotEntryPage.vue` - Live ballot status updates
- `frontend/src/stores/electionStore.ts` - Join election groups
- `frontend/src/stores/resultStore.ts` - Tally progress integration
- `frontend/src/stores/peopleStore.ts` - People updates integration

#### New Components (if needed)

- `ProgressDisplay.vue` - Reusable progress bar component
- `RealtimeIndicator.vue` - Connection status indicator

### Data Model / API / Interface Changes

- No changes required - SignalR events and DTOs already defined
- Existing interfaces: `TallyProgressEvent`, `ElectionUpdateEvent`, `PersonUpdateEvent`

### Verification Approach

#### Testing Strategy

1. **Unit Tests**: SignalR service connection/disconnection logic
2. **Integration Tests**: Hub method invocations and event handling
3. **Manual Testing**:
   - Open multiple browser tabs
   - Start tally calculation in one tab
   - Verify progress updates in real-time across tabs
   - Test people updates, election status changes
   - Test connection recovery after network interruption

#### Test Commands

```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend
npm run test:unit

# E2E tests (if implemented)
npm run test:e2e
```

#### Success Criteria

- Real-time tally progress display during calculation
- Live updates when people are added/modified/deleted
- Election status changes reflected immediately across clients
- Automatic reconnection after network issues
- No performance degradation with SignalR enabled

---

## Combined Verification Approach

### Build & Quality Verification

- **Frontend Build**: `npm run build` succeeds without TypeScript errors
- **Backend Build**: `dotnet build` succeeds without compilation errors
- **Bundle Size**: Under 1MB for production build
- **Performance**: Fast loading times, Lighthouse score >90

### Testing Verification

- **Unit Tests**: `npm run test:unit` and `dotnet test` pass
- **Integration Tests**: SignalR event handling works correctly
- **E2E Tests**: Critical user flows work end-to-end
- **Manual Testing**: Real-time features work across multiple clients

### Real-time Features Verification

- **Connection Management**: Automatic reconnection after network issues
- **Group Broadcasting**: Targeted updates to election participants
- **UI Responsiveness**: Immediate updates without page refresh
- **Performance**: No degradation with SignalR enabled
- **Cross-tab Sync**: Changes reflected across multiple browser tabs

### Error Handling Verification

- **Network Issues**: Graceful handling of connection failures
- **Authentication**: Proper JWT token handling for hub connections
- **Validation**: Comprehensive error states and user feedback
- **Recovery**: Automatic reconnection and state synchronization

### Accessibility & UX Verification

- **WCAG Compliance**: Accessibility standards met
- **Responsive Design**: Works on mobile and desktop
- **Internationalization**: English and French locales functional
- **Loading States**: Proper feedback during async operations
- **Error Messages**: Clear, actionable error communication
