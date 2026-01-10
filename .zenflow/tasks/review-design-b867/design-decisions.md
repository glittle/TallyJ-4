# TallyJ4 Design Review - Final Decisions

**Date**: January 9, 2026  
**Participants**: Project Owner, AI Assistant  
**Purpose**: Review spec.md and finalize design decisions before implementation

---

## Design Changes from Initial Spec

### 1. Backend Architecture

**Decision**: Middle-ground approach with 3 projects

```
backend/
├── Domain/              # Entities, interfaces, enums
├── Application/         # Services, DTOs, business logic
└── Web/                # API controllers, SignalR hubs, EF, Program.cs
```

**Rationale**: 
- More organized than single project
- Simpler than full Clean Architecture
- Easy to navigate for solo developer with AI assistance

---

### 2. SignalR Hub Organization

**Decision**: Organize by user role (4 hubs instead of 5-7)

1. **PublicHub** - Non-authenticated pages (roll call display, public results)
2. **VoterHub** - Voter portal, verification codes, ballot submission
3. **TellerHub** - Ballot entry, front desk, tally operations, real-time collaboration
4. **AdminHub** - Election management, system monitoring, teller coordination

**Rationale**:
- Clearer separation by security context
- Simpler connection management
- Better aligns with authentication boundaries

---

### 3. Authentication

**Admin Authentication**:
- Google OAuth OR local username/password (user chooses at registration)
- Optional 2FA (TOTP)
- Password reset for local accounts only
- JWT tokens for session management

**Guest Teller Authentication**:
- Access code only (Election.ElectionPasscode)
- No user accounts
- JWT with ElectionGuid claim

**Voter Authentication**:
- Email/SMS one-time codes (6-digit, 15-minute expiration)
- WhatsApp deferred to v4.1+ (pending Meta approval)
- JWT with VoterId + ElectionGuid claims

**Constraint**: One auth type per browser session (no simultaneous roles)

**Changes from Spec**:
- ❌ Remove X.com OAuth (unstable API, defer to future)
- ❌ Remove WhatsApp for v4.0 (defer to v4.1)
- ✅ Keep Google OAuth only

---

### 4. CSS/Styling Approach

**Decision**: LESS with component-scoped classes (NOT Vue scoped styles)

**Pattern**:
```vue
<template>
  <div class="ballot-entry-page">
    <h1>Ballot Entry</h1>
    <div class="ballot-form">...</div>
  </div>
</template>

<style lang="less">
.ballot-entry-page {
  padding: 20px;
  
  h1 {
    font-size: 24px;
  }
  
  .ballot-form {
    background: white;
    // nested styles...
  }
}
</style>
```

**Global CSS**: In App.vue or separate global.less file

**Changes from Spec**:
- ✅ Confirmed: NO Tailwind
- ✅ Confirmed: Element Plus UI library
- ⚠️ Clarified: NOT using `<style scoped>`, instead top-level class + LESS nesting

---

### 5. Internationalization (i18n)

**Launch Languages**: English + French only

**Rationale**:
- Enforces discipline (all text in i18n files from day 1)
- French has real user demand
- Prevents "lazy English-only" development

**Post-Launch Languages**: Arabic, Korean, Spanish, Persian

**RTL Support**: Deferred to post-launch
- Verify Element Plus compatibility with postcss-loader
- May require alternative UI library if Element Plus RTL support is poor

**Changes from Spec**:
- ❌ Remove initial support for Spanish, Persian, Portuguese
- ✅ Focus on English + French
- ⚠️ RTL deferred (but verify feasibility)

---

### 6. Database Schema

**Primary Database**: SQL Server only (no PostgreSQL for v4.0)

**Key Decisions**:
- ✅ Use `IDENTITY` columns for primary keys (efficiency)
- ✅ Use `UNIQUEIDENTIFIER` (GUIDs) for foreign key relationships
- ✅ EF Core generates sequential GUIDs (`NEWSEQUENTIALID()` equivalent)
- ✅ Avoid stored procedures (all logic in C#)

**Election Table Cleanup**:
- **Important**: v3 Election table has "compressed" columns (workaround for non-code-first DB)
- **v4 Approach**: Use clean model properties, build fresh schema
- **Do NOT** carry forward unused columns from v3 database
- Reference v3 model classes for current properties needed, not v3 DB schema

---

### 7. SignalR Backplane

**Decision**: .NET native SignalR (no Redis)

**Rationale**:
- Simpler deployment for self-hosted users
- .NET SignalR fully supports multi-client coordination without Redis
- Reduces external dependencies

**Changes from Spec**:
- ❌ Remove Redis requirement
- ❌ Remove StackExchange.Redis package
- ✅ Use built-in ASP.NET Core SignalR

---

### 8. Features Deferred to v4.1+

**v4.0 Scope** (Feature Parity):
- Admin authentication (Google OAuth + local accounts + 2FA)
- Guest teller authentication (access codes)
- Voter authentication (email/SMS codes only)
- Election creation and setup
- Voter import (CSV)
- Front desk (voter check-in)
- Ballot entry (multi-teller)
- Tally algorithms (exact v3 match)
- Results display (tie detection)
- Online voting portal
- Reports (HTML formatted for print-to-PDF)
- Roll call display
- Real-time updates (SignalR)

**Deferred to v4.1+**:
- ❌ SMS cost estimation (Twilio Lookup API + caching)
- ❌ WhatsApp integration (pending Meta approval)
- ❌ X.com OAuth
- ❌ Linked elections (full automation)
- ❌ Additional languages (Spanish, Persian, Arabic, Korean)
- ❌ Dynamic text enlargement
- ❌ Programmatic PDF generation (QuestPDF)

**Simplified Tie-Break Workflow** (v4.0):
1. Admin sees "Tie detected" on results page
2. Admin clicks "Create Tie-Break Election"
   - System copies election, marks only tied candidates as eligible
   - Generates new election code
3. **SignalR Feature**: Admin clicks "Switch All Tellers" → broadcasts message to all connected tellers
   - Each teller's browser receives message, automatically switches to tie-break election
   - (This mechanism needs to be designed/implemented)
4. After tie-break completes, admin manually updates main election results

**Note**: Full linked election automation (parent/child relationships, automatic result updates) deferred to v4.1+

---

### 9. Reporting Approach

**Decision**: HTML formatted for browser print-to-PDF

**Approach**:
- Generate HTML reports with print-friendly CSS
- User prints via browser (Ctrl+P → Save as PDF)
- NO programmatic PDF generation (QuestPDF not needed)

**Timing**: Near end of development (after core features work)

**Changes from Spec**:
- ❌ Remove QuestPDF package
- ❌ Remove RazorLight (may use simple Razor views in Web project)
- ✅ Focus on HTML + print CSS

---

### 10. Deployment Model

**Primary Deployment**: File drop into folder, run directly

**Supported Platforms**:
- Windows: Drop files, run `TallyJ4.exe`
- Linux/macOS: Drop files, run `dotnet TallyJ4.dll`
- IIS: Configure as standard ASP.NET Core site
- nginx: Reverse proxy to Kestrel

**No Docker Required** (but can be added as optional later)

**Rationale**:
- Simplest for non-technical users
- No container orchestration complexity
- Standard ASP.NET Core self-contained deployment

---

## Technology Stack (Final)

### Backend
- .NET 10 (LTS, released November 2025)
- ASP.NET Core Web API
- Entity Framework Core 10 (SQL Server provider)
- ASP.NET Core Identity + JWT
- SignalR Core (no Redis backplane)
- Serilog (already configured)
- Google OAuth (`Microsoft.AspNetCore.Authentication.Google`)
- 2FA: Otp.NET + QRCoder
- CSV: CsvHelper
- Email: MailKit
- SMS: Twilio

### Frontend
- Vue 3 (Composition API, `<script setup>`)
- TypeScript (strict mode)
- Vite
- Element Plus UI library
- LESS (NOT scoped, top-level class pattern)
- Pinia state management
- @microsoft/signalr client
- vue-i18n (lazy loading, English + French)
- axios

### Database
- SQL Server Express (local development)
- SQL Server (production, any edition)
- IDENTITY columns for PKs, GUIDs for FKs

---

## Next Steps

### Phase 1: Foundation Setup (Week 1)

#### Task 1.1: Backend Restructure
- Create Domain, Application, Web projects
- Move existing models to Domain
- Set up project references
- Configure EF Core in Web project

#### Task 1.2: i18n Infrastructure
- **Frontend**: 
  - Install vue-i18n
  - Create `src/locales/en.json`, `src/locales/fr.json`
  - Configure lazy loading
  - Create LanguageSelector component
- **Backend**: 
  - Configure Microsoft.Extensions.Localization
  - Create resource files for API error messages

#### Task 1.3: Database Schema Finalization
- Review v3 Election model (clean properties, ignore compressed columns)
- Create/update all 16+ entities in Domain project
- Add new entities: TwoFactorToken (skip SmsCostCache for v4.1)
- Generate EF migrations
- Test against SQL Server Express

#### Task 1.4: Admin Authentication
- Google OAuth configuration
- Local account registration/login
- JWT token generation
- 2FA setup/verification (TOTP)
- Password reset flow (local accounts)
- AuthController implementation

### Phase 2: Core Election Features (Weeks 2-3)
- Election CRUD operations
- Guest teller authentication
- Basic election setup wizard
- SignalR hub infrastructure (4 hubs)

### Phase 3: Ballot Entry & Tally (Weeks 4-5)
- Port tally algorithm from v3
- Implement ballot entry UI
- Multi-teller coordination via TellerHub
- Tally computation and results display
- Tie detection

### Phase 4: Front Desk & Voters (Weeks 6-7)
- Voter import (CSV)
- Front desk check-in UI
- Voter matching logic (email/phone → Person)
- Roll call display

### Phase 5: Online Voting (Weeks 8-9)
- Voter authentication (email/SMS codes)
- Online voting portal
- Ballot submission API
- VoterHub real-time updates

### Phase 6: Reporting & Polish (Weeks 10-11)
- HTML report templates (print-to-PDF)
- UI/UX polish
- Comprehensive testing
- Documentation

### Phase 7: Deployment (Week 12)
- Self-contained deployment packages
- Installation guides
- IIS/nginx configuration docs
- Final testing on Windows/Linux/macOS

---

## Success Criteria

**Feature Parity Checklist** (must match v3):
- ✅ Admin authentication (Google OAuth, local accounts, 2FA)
- ✅ Guest teller authentication (access codes)
- ✅ Voter authentication (email/SMS codes)
- ✅ Election creation and setup
- ✅ Voter import (CSV)
- ✅ Front desk (voter check-in)
- ✅ Ballot entry (multi-teller)
- ✅ Tally computation (exact v3 algorithm)
- ✅ Results display (tie detection)
- ✅ Online voting portal
- ✅ Reports (teller report, results, audit log)
- ✅ Roll call display
- ✅ Real-time updates (SignalR)

**Technical Quality**:
- ✅ .NET 10
- ✅ TypeScript strict mode
- ✅ Tally algorithm tests pass 100%
- ✅ i18n: All text in en.json + fr.json
- ✅ CSS: Top-level class + LESS nesting (no scoped styles)
- ✅ SignalR: 4 hubs (PublicHub, VoterHub, TellerHub, AdminHub)

**Performance**:
- ✅ Tally computation < 5 seconds (500 ballots)
- ✅ Supports 30 concurrent elections
- ✅ Supports 200 concurrent tellers

---

## Risks & Mitigation

### Risk 1: Tally Algorithm Accuracy
**Mitigation**: Port line-by-line from v3, replicate all test cases, side-by-side verification

### Risk 2: Element Plus RTL Support
**Mitigation**: Verify postcss-loader compatibility early, have backup UI library option

### Risk 3: SignalR Coordination Without Redis
**Mitigation**: Test multi-teller scenarios early, verify .NET SignalR handles concurrent updates

### Risk 4: Timeline Flexibility
**Mitigation**: No hard deadline, phased delivery (v4.0 → v4.1 → v4.2...)

---

## Open Questions / Future Decisions

1. **Tie-Break Teller Switching**: Exact SignalR message format/flow needs to be designed
2. **Element Plus RTL**: Test compatibility before committing to post-launch RTL support
3. **WhatsApp Meta Approval**: Track Meta's business license requirements for future integration
4. **Report Templates**: Final design/layout TBD during Phase 6

---

## References

- Initial Spec: `.zenflow/tasks/review-design-b867/spec.md`
- v3 Reverse Engineering: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`
- v3 Source Code: (available for tally algorithm porting)
