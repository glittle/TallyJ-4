# Technical Specification: TallyJ 4 Completion Plan

## 1. Technical Context

### 1.1 Backend Stack
- **Language**: C# (.NET 10.0)
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 10.0.1
- **Database**: SQL Server
- **Authentication**: ASP.NET Identity + JWT Bearer tokens
- **Real-time**: SignalR
- **Logging**: Serilog
- **Validation**: FluentValidation 11.11.0
- **Mapping**: AutoMapper 12.0.1
- **API Documentation**: Swagger/OpenAPI (Swashbuckle 9.0.6)
- **Testing**: xUnit 2.9.3 + Moq 4.20.70

### 1.2 Frontend Stack
- **Language**: TypeScript 5.9.3
- **Framework**: Vue 3.5.22 (Composition API)
- **UI Library**: Element Plus 2.11.5
- **State Management**: Pinia 3.0.3
- **Routing**: Vue Router 4.6.3
- **HTTP Client**: Axios 1.13.2
- **Real-time**: @microsoft/signalr 9.0.6
- **i18n**: Vue I18n 11.0.0
- **Build Tool**: Vite 7.1.14 (rolldown-vite)
- **Testing**: Vitest 4.0.18 + @vue/test-utils 2.4.6
- **Charts**: Chart.js 4.4.8 + vue-chartjs 5.3.1

### 1.3 Current Architecture

**Backend Structure**:
- 12 Controllers (Account, Auth, Ballots, Dashboard, Elections, Import, People, Public, Reports, Results, Setup, Votes)
- 5 SignalR Hubs (Main, Analyze, BallotImport, FrontDesk, Public)
- 16 Domain Entities + Identity tables
- Service layer with interface-based design
- Authorization middleware for election access
- Global exception handling
- Database migrations and seeding

**Frontend Structure**:
- 15+ page components organized by feature
- Pinia stores (auth, election, people, ballot, result, import, theme)
- Reusable component library
- SignalR service for real-time communication
- API service layer (manual types currently)
- MainLayout and PublicLayout
- Route guards for authentication
- English/French localization

## 2. Implementation Approach

### 2.1 Phase Breakdown

This specification defines a **7-phase approach** to complete TallyJ 4:

**Phase A: Complete Documentation** (1-2 weeks)
- Systematic v3 site walkthrough and documentation
- Feature matrix creation
- Gap analysis

**Phase B: Fix Critical Issues** (1 week)
- Fix failing integration tests
- Resolve SignalR group name mismatch
- Fix OpenAPI generation
- Stabilize foundation

**Phase C: Core Missing Features** (3-4 weeks)
- Location Management
- Teller Assignment
- Advanced Election Configuration
- Online Voting Portal
- Front Desk Registration
- Import Ballots
- Audit Logs UI
- Public Display

**Phase D: UI/UX Professional Polish** (2-3 weeks)
- Design system creation
- Page redesign
- Responsive design
- Visual enhancements

**Phase E: Testing & QA** (1-2 weeks)
- Test coverage expansion
- Accessibility audit
- Performance optimization
- Security audit

**Phase F: Advanced Reporting** (1-2 weeks)
- Enhanced visualizations
- Historical comparisons
- Statistical analysis

**Phase G: Deployment** (1 week)
- Production deployment
- Documentation
- User training materials

### 2.2 Technology Decisions

**Design System Approach**:
- Use Element Plus as foundation
- Create consistent design tokens (colors, spacing, typography)
- Build custom theme on top of Element Plus
- Document component usage patterns
- Create reusable composition functions for common UI patterns

**API Type Safety**:
- Fix OpenAPI spec generation (currently failing due to size)
- Use @hey-api/openapi-ts for type generation
- Replace manual types in `frontend/src/types/` with generated types
- Maintain backwards compatibility during migration

**State Management Strategy**:
- Continue using Pinia for client state
- Implement optimistic updates with rollback for better UX
- Use SignalR for real-time synchronization
- Add offline support with LocalForage for critical workflows

**Testing Strategy**:
- Backend: xUnit for unit tests, integration tests with in-memory DB
- Frontend: Vitest for unit/integration tests, component tests with @vue/test-utils
- Target >80% code coverage
- E2E tests for critical workflows (authentication, ballot entry, tally calculation)

## 3. Source Code Structure Changes

### 3.1 Backend Changes

**New Controllers**:
```
backend/Controllers/
  LocationsController.cs         # Location management
  TellersController.cs           # Teller assignment
  AuditLogsController.cs         # Audit log display
  OnlineVotingController.cs      # Voter-facing online portal
  FrontDeskController.cs         # Front desk registration
```

**Enhanced Controllers**:
```
backend/Controllers/
  ElectionsController.cs         # Add advanced configuration endpoints
  ImportController.cs            # Add ballot import functionality
  PublicController.cs            # Add public display endpoints
```

**New Services**:
```
backend/Services/
  LocationService.cs / ILocationService.cs
  TellerService.cs / ITellerService.cs
  AuditLogService.cs / IAuditLogService.cs
  OnlineVotingService.cs / IOnlineVotingService.cs
  FrontDeskService.cs / IFrontDeskService.cs
```

**New Entities** (from v3 schema):
```
backend/TallyJ4.Domain/Entities/
  Location.cs                    # Voting locations
  Computer.cs                    # Computer registration
  Teller.cs                      # Teller assignments
  AuditLog.cs                    # Audit trail
  Message.cs                     # Messages/notifications
  ImportFile.cs                  # Ballot import tracking
```

**New Validators**:
```
backend/Validators/
  CreateLocationDtoValidator.cs
  CreateTellerDtoValidator.cs
  OnlineVoteBatchDtoValidator.cs
  FrontDeskRegistrationDtoValidator.cs
```

### 3.2 Frontend Changes

**New Pages**:
```
frontend/src/pages/
  locations/
    LocationsListPage.vue        # Manage voting locations
    LocationDetailPage.vue       # Edit location
  tellers/
    TellersListPage.vue          # Manage tellers
    TellerAssignmentPage.vue     # Assign tellers to elections
  frontdesk/
    FrontDeskPage.vue            # Front desk registration workflow
  online/
    OnlineVotingPortalPage.vue   # Voter-facing portal
    OnlineVotingBallotPage.vue   # Online ballot entry
  admin/
    AuditLogsPage.vue            # View audit logs
  public/
    PublicDisplayPage.vue        # Public results display
```

**Enhanced Pages**:
```
frontend/src/pages/elections/
  ElectionFormPage.vue           # Add advanced configuration fields
  
frontend/src/pages/ballots/
  BallotImportPage.vue           # Bulk import functionality
```

**New Stores**:
```
frontend/src/stores/
  locationStore.ts               # Location state management
  tellerStore.ts                 # Teller state management
  frontDeskStore.ts              # Front desk state management
  auditLogStore.ts               # Audit log state management
  onlineVotingStore.ts           # Online voting state management
```

**New Components**:
```
frontend/src/components/
  locations/
    LocationForm.vue
    LocationList.vue
    ComputerRegistration.vue
  tellers/
    TellerForm.vue
    TellerAssignmentTable.vue
  frontdesk/
    VoterCheckIn.vue
    RollCall.vue
  common/
    SkeletonLoader.vue           # Loading states
    EmptyState.vue               # Empty list states
    ConfirmDialog.vue            # Confirmation dialogs
    Toast.vue                    # Toast notifications
```

**Design System**:
```
frontend/src/
  design-system/
    tokens/
      colors.ts                  # Color palette
      spacing.ts                 # Spacing system
      typography.ts              # Typography system
    components/
      Button.vue                 # Standardized button
      Card.vue                   # Standardized card
      Table.vue                  # Standardized table
      Form.vue                   # Standardized form
    composables/
      useDesignTokens.ts         # Access design tokens
      useResponsive.ts           # Responsive breakpoints
```

## 4. Data Model Changes

### 4.1 New Database Tables

**Location Table**:
```csharp
public class Location
{
    public int C_RowId { get; set; }
    public Guid LocationGuid { get; set; }
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public Election Election { get; set; }
    public ICollection<Person> People { get; set; }
    public ICollection<Computer> Computers { get; set; }
}
```

**Computer Table**:
```csharp
public class Computer
{
    public int C_RowId { get; set; }
    public Guid ComputerGuid { get; set; }
    public Guid LocationGuid { get; set; }
    public string ComputerInternalCode { get; set; }
    public string BrowserInfo { get; set; }
    public string IpAddress { get; set; }
    public DateTime? LastActivity { get; set; }
    
    // Navigation
    public Location Location { get; set; }
}
```

**Teller Table**:
```csharp
public class Teller
{
    public int C_RowId { get; set; }
    public Guid TellerGuid { get; set; }
    public Guid ElectionGuid { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Role { get; set; } // "Teller", "HeadTeller", "Observer"
    public bool CanEnterBallots { get; set; }
    public bool CanEditElection { get; set; }
    public bool CanViewResults { get; set; }
    public DateTime? AssignedAt { get; set; }
    
    // Navigation
    public Election Election { get; set; }
    public AppUser User { get; set; }
}
```

**AuditLog Table**:
```csharp
public class AuditLog
{
    public int C_RowId { get; set; }
    public Guid AuditLogGuid { get; set; }
    public Guid? ElectionGuid { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; } // "Create", "Update", "Delete", "View", "Login"
    public string EntityType { get; set; } // "Election", "Person", "Ballot", "Vote"
    public string EntityId { get; set; }
    public string Details { get; set; } // JSON
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
    
    // Navigation
    public Election Election { get; set; }
    public AppUser User { get; set; }
}
```

**ImportFile Table**:
```csharp
public class ImportFile
{
    public int C_RowId { get; set; }
    public Guid ImportFileGuid { get; set; }
    public Guid ElectionGuid { get; set; }
    public string FileName { get; set; }
    public string FileType { get; set; } // "CSV", "Excel", "JSON"
    public int RecordCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string ErrorDetails { get; set; } // JSON
    public DateTime ImportedAt { get; set; }
    public string ImportedBy { get; set; }
    
    // Navigation
    public Election Election { get; set; }
}
```

### 4.2 Enhanced Existing Tables

**Election Table Updates**:
- All v3 fields are already present in v4 schema
- Need to expose all fields in UI (many are in DB but not in creation/edit forms)

**Person Table Updates**:
- `VotingLocationGuid` - already exists, need to connect to Location entity
- `Teller1`, `Teller2` - already exists, need UI support

## 5. API Changes

### 5.1 New Endpoints

**Location Management**:
```
GET    /api/elections/{electionId}/locations
POST   /api/elections/{electionId}/locations
GET    /api/elections/{electionId}/locations/{locationId}
PUT    /api/elections/{electionId}/locations/{locationId}
DELETE /api/elections/{electionId}/locations/{locationId}
POST   /api/elections/{electionId}/locations/{locationId}/computers
```

**Teller Management**:
```
GET    /api/elections/{electionId}/tellers
POST   /api/elections/{electionId}/tellers
PUT    /api/elections/{electionId}/tellers/{tellerId}
DELETE /api/elections/{electionId}/tellers/{tellerId}
```

**Front Desk Registration**:
```
POST   /api/elections/{electionId}/frontdesk/register
POST   /api/elections/{electionId}/frontdesk/checkin
GET    /api/elections/{electionId}/frontdesk/rollcall
```

**Online Voting Portal**:
```
GET    /api/online-voting/{electionCode}/info
POST   /api/online-voting/{electionCode}/authenticate
POST   /api/online-voting/{electionCode}/vote
GET    /api/online-voting/{electionCode}/status
```

**Audit Logs**:
```
GET    /api/audit-logs
GET    /api/elections/{electionId}/audit-logs
```

**Ballot Import**:
```
POST   /api/elections/{electionId}/import/ballots
GET    /api/elections/{electionId}/import/history
```

**Public Display**:
```
GET    /api/public/{electionCode}/results
GET    /api/public/{electionCode}/status
```

### 5.2 Enhanced Endpoints

**Election Configuration**:
```
PUT    /api/elections/{electionId}/configuration
```
- Expand to support all v3 election settings
- Add validation for all configuration fields

## 6. Real-time (SignalR) Changes

### 6.1 Fix Existing Issues

**Group Name Standardization**:
```csharp
// Current inconsistency:
// AnalyzeHub sends to group: "Election:{electionGuid}"
// Frontend listens to: "election-{electionId}"

// Solution: Standardize all hubs to use "election-{electionGuid}"
```

### 6.2 New Hub Methods

**FrontDeskHub**:
```csharp
public class FrontDeskHub : Hub
{
    Task RegisterVoter(string electionId, PersonDto person);
    Task CheckInVoter(string electionId, string personId);
    Task UpdateRollCall(string electionId);
    // Broadcasts: VoterRegistered, VoterCheckedIn, RollCallUpdated
}
```

**OnlineVotingHub** (new):
```csharp
public class OnlineVotingHub : Hub
{
    Task SubmitVote(string electionCode, VoteDto vote);
    Task GetVotingStatus(string electionCode);
    // Broadcasts: VoteSubmitted, VotingClosed
}
```

## 7. UI/UX Design System Specification

### 7.1 Design Tokens

**Color Palette**:
```typescript
export const colors = {
  primary: {
    50: '#E3F2FD',
    100: '#BBDEFB',
    500: '#2196F3',  // Main primary
    700: '#1976D2',
    900: '#0D47A1'
  },
  success: '#4CAF50',
  warning: '#FF9800',
  danger: '#F44336',
  info: '#00BCD4',
  neutral: {
    50: '#FAFAFA',
    100: '#F5F5F5',
    200: '#EEEEEE',
    500: '#9E9E9E',
    700: '#616161',
    900: '#212121'
  }
}
```

**Typography**:
```typescript
export const typography = {
  fontFamily: {
    base: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    mono: 'Consolas, Monaco, "Courier New", monospace'
  },
  fontSize: {
    xs: '0.75rem',    // 12px
    sm: '0.875rem',   // 14px
    base: '1rem',     // 16px
    lg: '1.125rem',   // 18px
    xl: '1.25rem',    // 20px
    '2xl': '1.5rem',  // 24px
    '3xl': '1.875rem' // 30px
  },
  fontWeight: {
    normal: 400,
    medium: 500,
    semibold: 600,
    bold: 700
  }
}
```

**Spacing**:
```typescript
export const spacing = {
  xs: '0.25rem',   // 4px
  sm: '0.5rem',    // 8px
  md: '1rem',      // 16px
  lg: '1.5rem',    // 24px
  xl: '2rem',      // 32px
  '2xl': '3rem',   // 48px
  '3xl': '4rem'    // 64px
}
```

### 7.2 Component Standards

**Page Layout**:
```vue
<template>
  <div class="page-container">
    <PageHeader :title="title" :breadcrumbs="breadcrumbs" />
    <div class="page-content">
      <slot />
    </div>
  </div>
</template>
```

**Loading States**:
- Use skeleton loaders for content
- Show progress bars for long operations
- Display spinners for short waits
- Disable buttons during actions

**Error Handling**:
- Toast notifications for non-critical errors
- Error states with retry buttons for critical errors
- Form validation errors inline
- Global error boundary for crashes

**Responsive Breakpoints**:
```typescript
export const breakpoints = {
  mobile: 640,   // 0-640px
  tablet: 768,   // 641-768px
  desktop: 1024, // 769-1024px
  wide: 1280     // 1025px+
}
```

## 8. Verification & Testing Approach

### 8.1 Automated Testing

**Backend Tests**:
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageThreshold=80
```

**Frontend Tests**:
```bash
# Run all tests
npm run test

# Run with coverage
npm run test:coverage

# Target: >80% coverage
```

### 8.2 Manual Testing Checklist

**Phase B Verification**:
- [ ] All 49 integration tests passing
- [ ] SignalR tally progress events working
- [ ] OpenAPI spec generating correctly
- [ ] Type-safe API client generated

**Phase C Verification**:
- [ ] Can create/edit/delete locations
- [ ] Can assign tellers to elections
- [ ] All election configuration fields accessible
- [ ] Online voting portal functional
- [ ] Front desk registration workflow complete
- [ ] Ballot import working
- [ ] Audit logs displaying correctly
- [ ] Public display mode working

**Phase D Verification**:
- [ ] All pages follow design system
- [ ] Responsive on mobile, tablet, desktop
- [ ] Loading states present everywhere
- [ ] Error handling user-friendly
- [ ] Consistent spacing and typography
- [ ] Animations smooth and purposeful

**Phase E Verification**:
- [ ] Test coverage >80%
- [ ] Lighthouse score >90
- [ ] WCAG 2.1 AA compliant
- [ ] No security vulnerabilities
- [ ] Bundle size <1MB gzipped

### 8.3 Lint and Type Check Commands

**Backend**:
```bash
# Build (includes type checking)
dotnet build

# Restore packages
dotnet restore

# No built-in linter, but can add:
# dotnet tool install -g dotnet-format
# dotnet format
```

**Frontend**:
```bash
# Type check
npx vue-tsc --noEmit

# Build (includes type checking)
npm run build

# No ESLint configured currently
# Should add: "lint": "eslint . --ext .vue,.js,.jsx,.cjs,.mjs,.ts,.tsx,.cts,.mts --fix"
```

## 9. Deployment Phases

### 9.1 Phase Deliverables

**Phase A Deliverables**:
- Complete feature matrix (v3 vs v4)
- Updated requirements.md with screenshots
- Prioritized backlog
- Decision on design approach

**Phase B Deliverables**:
- All tests passing (49/49)
- SignalR working correctly
- Generated API types available
- Stable foundation

**Phase C Deliverables**:
- 8 new features implemented
- Feature parity with v3 achieved
- All Priority 1 features tested

**Phase D Deliverables**:
- Design system documented
- All pages redesigned
- Responsive design complete
- Professional visual quality

**Phase E Deliverables**:
- >80% test coverage
- Accessibility compliant
- Performance optimized
- Production-ready quality

**Phase F Deliverables**:
- Advanced reporting features
- Data visualization
- Historical comparisons

**Phase G Deliverables**:
- Production deployment
- User documentation
- Admin guide
- Training materials

### 9.2 Success Criteria

**Technical Quality**:
- ✅ Zero critical bugs
- ✅ All tests passing
- ✅ >80% code coverage
- ✅ Type-safe API integration
- ✅ Real-time features working

**User Experience**:
- ✅ Professional, modern UI
- ✅ Intuitive navigation
- ✅ Fast performance (<2s page loads)
- ✅ Mobile-friendly
- ✅ Helpful error messages

**Feature Completeness**:
- ✅ All v3 features implemented
- ✅ No functional regressions
- ✅ New features documented
- ✅ User workflows tested

**Production Readiness**:
- ✅ Security hardened
- ✅ Monitoring configured
- ✅ Documentation complete
- ✅ Deployment automated

## 10. Risk Mitigation

### 10.1 Technical Risks

**Risk: OpenAPI Generation Fails**
- Mitigation: Split spec into multiple files if needed
- Fallback: Continue with manual types, fix later
- Likelihood: Low (solvable)

**Risk: Performance Issues with Large Elections**
- Mitigation: Implement pagination, lazy loading, virtualization
- Testing: Load test with 10,000+ people
- Likelihood: Medium

**Risk: SignalR Connection Stability**
- Mitigation: Implement reconnection logic, offline support
- Testing: Test with network interruptions
- Likelihood: Medium

### 10.2 Schedule Risks

**Risk: Feature Discovery During Phase A Takes Longer**
- Mitigation: Time-box to 2 weeks max
- Adjust Phase C scope if needed
- Likelihood: Medium

**Risk: UI/UX Polish Takes Longer Than Expected**
- Mitigation: Prioritize high-value pages
- Accept incremental improvement
- Likelihood: High

**Risk: Test Coverage Goal Not Met**
- Mitigation: Focus on critical paths first
- Accept 70% coverage if time-constrained
- Likelihood: Medium

## 11. Next Steps

### 11.1 Immediate Actions (After Approval)

1. **Mark Technical Specification Complete** ✓
2. **Create Implementation Plan** (Phase-by-phase breakdown)
3. **Get User Approval** for Phase A (v3 review)
4. **Begin Phase B** (fix critical issues) in parallel with Phase A decision

### 11.2 Phase A Setup (If Approved)

1. Set up screen recording/screenshot tool
2. Create feature matrix template
3. Access v3 production site
4. Document systematically (page by page)
5. Update requirements.md with findings

### 11.3 Phase B Setup (Start Immediately)

1. Review failing test output
2. Identify SignalR group name mismatches
3. Debug OpenAPI generation issue
4. Prioritize fixes by impact
5. Create task breakdown for Phase B

## 12. Conclusion

This technical specification provides a **comprehensive roadmap** to complete TallyJ 4 over **10-15 weeks**. The approach is:

✅ **Systematic**: Clear phases with defined deliverables
✅ **Risk-Aware**: Identifies and mitigates key risks
✅ **Quality-Focused**: Testing and polish built into timeline
✅ **User-Centric**: Professional UX is core requirement
✅ **Achievable**: Based on realistic effort estimates

**Key Success Factors**:
1. Complete v3 documentation (Phase A) before major feature work
2. Fix critical issues (Phase B) to provide stable foundation
3. Systematic UI/UX improvement (Phase D) for professional quality
4. Comprehensive testing (Phase E) for production readiness

**Total Estimated Timeline**: 10-15 weeks to production-ready TallyJ 4

**Primary Decision Point**: Approve Phase A (v3 comprehensive review) - **STRONGLY RECOMMENDED**
