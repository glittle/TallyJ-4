# Design Review Report - TallyJ4

**Date**: January 9, 2026  
**Task**: Review Design (review-design-b867)  
**Status**: ✅ Complete

---

## What Was Accomplished

### 1. Comprehensive Spec Review

Reviewed the complete 1,927-line technical specification covering:
- Technology stack (.NET 10, Vue 3, SQL Server)
- Architecture approach (simplified Clean Architecture)
- 16+ database entities
- 3 authentication systems
- 10 SignalR hubs (v3) → proposed 5-7 hubs
- Internationalization (5 languages)
- New features (SMS cost estimation, WhatsApp, linked elections)
- 12-week phased delivery plan

### 2. Structured Design Interview

Conducted 26-question interview across key design areas:
- **Architecture & Complexity**: Hub consolidation, project structure
- **Authentication**: 3 auth systems, OAuth providers
- **New Features vs Parity**: SMS cost estimation, WhatsApp, linked elections
- **Technology Choices**: Element Plus, LESS vs Tailwind, .NET 10
- **Database & Persistence**: Redis, SQL Server portability
- **Migration Strategy**: Big bang vs incremental, tally algorithm verification
- **Internationalization**: Language scope, RTL support
- **Timeline**: 12-week reality check, AI assistance assumptions
- **Reporting**: Template-based approach
- **Deployment**: Self-hosting complexity

### 3. Key Design Decisions

#### Backend Architecture
- **Changed**: Single project → 3 projects (Domain, Application, Web)
- **Rationale**: Better organization while staying simpler than full Clean Architecture

#### SignalR Hubs
- **Changed**: 5-7 feature-based hubs → 4 role-based hubs
- **New Structure**: PublicHub, VoterHub, TellerHub, AdminHub
- **Rationale**: Better security boundaries, clearer separation by user role

#### Authentication
- **Changed**: Removed X.com OAuth (unstable API)
- **Kept**: Google OAuth + local accounts, 2FA, guest teller codes, voter email/SMS codes
- **Deferred**: WhatsApp to v4.1 (pending Meta approval)

#### CSS/Styling
- **Clarified**: LESS with top-level class pattern, NOT Vue scoped styles
- **Pattern**: `<div class="component-name">` + nested LESS rules
- **Confirmed**: Element Plus UI library, NO Tailwind

#### Internationalization
- **Changed**: 5 languages → 2 languages (English + French)
- **Rationale**: Enforces i18n discipline without overcommitting
- **Deferred**: RTL support (verify Element Plus compatibility first)

#### Database
- **Changed**: SQL Server portability goal → SQL Server only
- **Allowed**: IDENTITY columns (efficiency)
- **Key**: Use GUIDs for FKs (EF sequential), avoid v3 "compressed columns" pattern

#### SignalR Infrastructure
- **Changed**: Redis backplane → .NET native SignalR
- **Rationale**: Simpler deployment, fewer dependencies

#### Features Deferred to v4.1+
- SMS cost estimation
- WhatsApp integration
- X.com OAuth
- Linked elections (full automation)
- Additional languages (Spanish, Persian, Arabic, Korean)
- Programmatic PDF generation

#### Reporting
- **Clarified**: HTML formatted for browser print-to-PDF (NOT QuestPDF)
- **Timing**: Near end of development

#### Deployment
- **Clarified**: File drop + run directly (no Docker requirement)
- **Platforms**: Windows, Linux, macOS, IIS, nginx

### 4. Documentation Created

Created `design-decisions.md` (comprehensive reference) covering:
- All design changes from initial spec
- Final technology stack
- Phase 1 next steps (detailed tasks)
- Success criteria
- Risks & mitigation
- Open questions

### 5. Election Table Schema Clarification

**Important Discovery**: v3 Election table uses "compressed columns" workaround due to non-code-first constraints.

**v4 Approach**: 
- Use clean model properties from v3 model classes
- Build fresh schema (EF Code First)
- Do NOT carry forward unused v3 database columns
- This was a critical clarification that would have caused confusion

---

## How the Solution Was Validated

### Interview Process
- Asked 26 structured questions across 10 design areas
- Received clear, decisive answers on all points
- Identified scope reductions (deferred features) to focus on feature parity
- Clarified ambiguities (CSS pattern, reporting approach, deployment model)

### Design Consistency Check
- Verified alignment between design decisions and technical feasibility
- Confirmed Element Plus experience (used in previous projects)
- Validated .NET 10 availability (LTS released November 2025)
- Confirmed access to v3 source code (critical for tally algorithm porting)

### Scope Validation
- Confirmed flexible timeline (no hard deadline)
- Identified MVP scope (feature parity) vs nice-to-have features
- Deferred 6 features to v4.1+ to focus on core functionality
- Validated that tie-break workflow can be simplified (manual steps + SignalR helper)

---

## Biggest Issues or Challenges Encountered

### 1. Feature Creep in Initial Spec
**Issue**: Initial spec included many new features (SMS cost estimation, WhatsApp, linked elections, 5 languages) alongside feature parity.

**Resolution**: Clearly separated v4.0 scope (parity) from v4.1+ features (enhancements). This reduces risk and ensures v4.0 can be solid foundation.

### 2. SignalR Hub Organization Ambiguity
**Issue**: Original spec proposed 5-7 hubs organized by feature (FrontDeskHub, BallotHub, etc.), but this didn't align well with authentication boundaries.

**Resolution**: Reorganized to 4 role-based hubs (PublicHub, VoterHub, TellerHub, AdminHub). This is cleaner and maps to security contexts.

### 3. CSS Pattern Confusion
**Issue**: Spec didn't specify whether to use Vue scoped styles or global LESS. This is critical for consistent styling approach.

**Resolution**: Clarified pattern: top-level class per component + nested LESS rules. NO scoped styles. This gives flexibility while maintaining organization.

### 4. Database Schema Evolution
**Issue**: v3 Election table has "compressed columns" (workaround for non-code-first DB). Spec didn't clarify whether to replicate this pattern.

**Resolution**: Use clean model properties, build fresh schema. Do NOT carry forward legacy workarounds. This was critical clarification that could have caused significant confusion during implementation.

### 5. RTL Support Uncertainty
**Issue**: Initial spec included Persian language (RTL), but Element Plus RTL support is unclear.

**Resolution**: Defer RTL languages to post-launch, but verify Element Plus + postcss-loader compatibility early. Keep option to switch UI library if needed.

### 6. Reporting Approach Misunderstanding
**Issue**: Spec mentioned QuestPDF (programmatic PDF generation), but this wasn't aligned with project owner's vision.

**Resolution**: Clarified approach: HTML formatted for browser print-to-PDF. This is simpler and leverages browser's native capabilities.

---

## Recommendations for Phase 1

### Immediate Next Steps (Week 1)

1. **Backend Restructure** (Day 1-2)
   - Create Domain, Application, Web projects
   - Move existing EF models to Domain
   - Set up project references

2. **i18n Infrastructure** (Day 3)
   - Frontend: vue-i18n with lazy loading (en.json, fr.json)
   - Backend: Microsoft.Extensions.Localization
   - Create LanguageSelector component

3. **Database Schema** (Day 4-5)
   - Review v3 model classes for Election and other entities
   - Create clean schema in Domain (no compressed columns)
   - Generate EF migrations
   - Seed test data

4. **Admin Authentication** (Day 6-7)
   - Google OAuth setup
   - Local account registration/login
   - JWT token generation
   - AuthController implementation

### Risk Mitigation Priorities

1. **Tally Algorithm**: Start porting test cases from v3 ASAP (even before implementation)
2. **Element Plus RTL**: Test postcss-loader compatibility in Week 2
3. **SignalR Coordination**: Build simple multi-teller test in Week 3
4. **i18n Discipline**: Enforce en.json + fr.json from day 1 (no hardcoded strings)

### Success Metrics for Phase 1

By end of Week 1:
- ✅ Backend: 3 projects (Domain, Application, Web)
- ✅ Frontend: vue-i18n working, language switcher functional
- ✅ Database: Clean schema with 16+ entities, migrations successful
- ✅ Auth: Can register admin account (local or Google)
- ✅ All text in en.json + fr.json (zero hardcoded strings)

---

## Conclusion

The design review successfully:
- Identified ambiguities in the initial spec
- Made clear decisions on all design questions
- Reduced scope to focus on feature parity (defer enhancements to v4.1+)
- Clarified critical technical patterns (CSS, i18n, database schema)
- Created actionable Phase 1 tasks

The project is now ready to begin implementation with a clear, validated design.

**Status**: ✅ Ready to proceed with Phase 1 (Backend Restructure → i18n → Database → Auth)

---

## Appendix: Question Summary

**21 questions answered** (Q1-Q21 from initial interview, Q22-Q26 from follow-up):

- **Architecture**: Middle-ground (Domain/Application/Web), 4 role-based hubs
- **Auth**: Google OAuth + local accounts, no X.com, defer WhatsApp
- **CSS**: LESS with top-level class pattern, Element Plus, NO Tailwind
- **i18n**: English + French (defer RTL)
- **Database**: SQL Server only, IDENTITY + GUIDs, clean schema
- **Infrastructure**: .NET native SignalR (no Redis)
- **Scope**: Feature parity for v4.0, defer enhancements to v4.1+
- **Deployment**: File drop + run (no Docker requirement)
- **Reporting**: HTML + print CSS (no QuestPDF)
- **Timeline**: Flexible (no hard deadline)

All design decisions documented in `design-decisions.md` for reference.
