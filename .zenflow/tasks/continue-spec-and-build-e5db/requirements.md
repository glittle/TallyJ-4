# Requirements: Continue TallyJ Reverse Engineering Documentation

## Objective

Complete the remaining reverse engineering documentation tasks (Tasks 6-11) for the TallyJ election system migration from ASP.NET Framework 4.8 to .NET Core 8 + Vue 3.

## Current Status

**Completed Documentation** (Tasks 1-5):
- ✅ All 12 controller API endpoints documented
- ✅ Authorization rules and security model documented  
- ✅ Database entities (16 core entities) fully documented
- ✅ Authentication systems (3 independent systems) fully documented
- ✅ SignalR hubs (10 hubs) fully documented
- ✅ Tally algorithms and business logic fully documented
- ✅ UI/UX screenshots (26 screenshots) analyzed

**Documentation Location**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`

**Total Completed**: ~55,000 lines of comprehensive documentation

## Remaining Tasks (6-11)

### Task 6: Document Configuration Settings
**Priority**: Medium  
**Estimated Effort**: 2 hours

**Source Files**:
- `C:\Dev\TallyJ\v3\Site\Web.config` (primary configuration)
- `C:\Dev\TallyJ\v3\Site\AppDataSample\AppSettings.config` (external app settings)
- Additional Web.*.config transform files for different environments

**Required Documentation**:
1. **Connection Strings**
   - MainConnection3 (SQL Server connection configured at IIS/machine level)
   - Connection string format and requirements

2. **AppSettings**
   - Environment (Dev/Prod)
   - HostSite URL
   - HostSupportsOnlineElections flag
   - UseProductionFiles flag
   - iftttKey (IFTTT logging integration)
   - LOGENTRIES_ACCOUNT_KEY, LOGENTRIES_TOKEN, LOGENTRIES_LOCATION
   - OAuth keys (Facebook, Google - referenced from external file)
   - webpages:Version, ClientValidationEnabled, UnobtrusiveJavaScriptEnabled

3. **System.Web Configuration**
   - compilation (debug, targetFramework)
   - customErrors mode
   - httpRuntime (maxRequestLength, targetFramework)
   - authentication mode (Windows/Forms)
   - session state configuration (StateServer at localhost:42424, 6-hour timeout)
   - machineKey settings
   - Profile and Membership providers

4. **Unity DI Configuration**
   - Type registrations from `<unity>` section
   - Lifetime management

5. **Entity Framework Configuration**
   - Provider configuration
   - Migration settings

6. **SMTP Configuration**
   - Email server settings
   - Credentials
   - From address

7. **OWIN Configuration**
   - AutomaticAppStartup
   - Cookie authentication settings
   - OAuth middleware configuration

8. **Session State**
   - StateServer mode
   - Server address (localhost:42424)
   - Timeout (6 hours / 360 minutes)
   - cookieless setting

9. **.NET Core Migration Mapping**
   - Web.config → appsettings.json structure
   - Environment-specific configuration (appsettings.Development.json, appsettings.Production.json)
   - User secrets for sensitive data
   - Connection string configuration
   - Dependency injection registration (Unity → built-in DI)
   - Session configuration (StateServer → Redis/SQL Server distributed cache)

**Output**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/configuration/settings.md`

---

### Task 7: Document External Integrations
**Priority**: Medium  
**Estimated Effort**: 4-6 hours

**Required Documentation**:

#### 7.1 OAuth Authentication Providers
- Google OAuth 2.0 (sign in with Google)
- Facebook OAuth (sign in with Facebook)
- Configuration requirements (client ID, client secret)
- Callback URLs
- Scopes requested
- User claims mapping
- .NET Core migration (IdentityServer/external authentication)

#### 7.2 Twilio SMS Integration
- Phone number verification for voters
- 6-digit verification code delivery
- API configuration (account SID, auth token, from phone number)
- SMS templates
- Rate limiting and retry logic
- Error handling
- Cost considerations
- .NET Core migration (Twilio SDK)

#### 7.3 Email Service (SMTP)
- Email server configuration
- Authentication method
- Election notifications (invitations, results)
- Voter verification codes (as alternative to SMS)
- Email templates
- .NET Core migration (MailKit or SendGrid)

#### 7.4 Logging Services
- **LogEntries** integration (cloud logging)
  - Account key, token, location configuration
  - Log levels and categories
  - .NET Core migration (Serilog with LogEntries sink)

- **IFTTT** integration (high-level activity logging)
  - iftttKey configuration
  - Events logged
  - .NET Core migration (HTTP webhook calls)

#### 7.5 Other Potential Integrations
- Highcharts (reporting/charting library - client-side)
- CKEditor (rich text editing - client-side)
- Azure hosting (if applicable)

**Output Files**:
- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/oauth.md`
- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/sms.md`
- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/email.md`
- `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/logging.md`

---

### Task 8: Generate Database ERD
**Priority**: Medium  
**Estimated Effort**: 2-3 hours

**Requirements**:
1. Create visual Entity Relationship Diagram using Mermaid syntax
2. Include all 16 core entities documented in `database/entities.md`
3. Show all relationships (one-to-many, many-to-one, many-to-many)
4. Color-code by functional area:
   - **Election Management**: Election, Location, Teller, JoinElectionUser, Computer
   - **People & Voting**: Person, OnlineVoter, OnlineVotingInfo, Ballot, Vote
   - **Results**: Result, ResultSummary, ResultTie
   - **Imports & Logs**: ImportFile, Message, C_Log, SmsLog
   - **Identity**: AspNetUsers, AspNetRoles, AspNetUserLogins, AspNetUserClaims

5. Include cardinality notations (1, *, 0..1, 1..*)
6. Reference key fields (primary keys, foreign keys)

**Output**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/erd.mmd`

**Verification**: Mermaid diagram renders correctly in GitHub and Markdown viewers

---

### Task 9: Create Migration Architecture Document
**Priority**: High  
**Estimated Effort**: 4-6 hours

**Requirements**:

Synthesize all documentation into a comprehensive migration guide that serves as the primary blueprint for rebuilding the system.

**Contents**:

1. **Executive Summary**
   - System overview
   - Current state (ASP.NET Framework 4.8)
   - Target state (.NET Core 8 + Vue 3)
   - Migration rationale
   - Success criteria

2. **Current vs. Target Architecture**
   - Side-by-side comparison table
   - Technology stack mapping
   - Architecture pattern changes (MVC → SPA with API)
   - Deployment model changes

3. **Migration Strategy Overview**
   - 9 phases from `spec.md` (summarized)
   - Critical path items
   - Dependencies between phases
   - Estimated timeline (24 weeks from spec.md)

4. **Component Migration Mapping**
   - Controllers → API endpoints
   - Razor views → Vue 3 components
   - SignalR hubs → SignalR Core hubs
   - Entity Framework 6 → EF Core 8
   - ASP.NET Identity → ASP.NET Core Identity + JWT
   - Unity DI → Built-in DI
   - LESS → CSS Modules/Tailwind
   - jQuery/Vue 2 → Vue 3 Composition API

5. **Critical Components Deep Dive**
   - 3 authentication systems (link to `security/authentication.md`)
   - Tally algorithms (link to `business-logic/tally-algorithms.md`)
   - SignalR architecture (link to `signalr/hubs-overview.md`)
   - Database migration (link to `database/entities.md`)

6. **Risk Assessment & Mitigation**
   - High-risk areas (tally algorithm accuracy, authentication complexity)
   - Testing strategy (comparison testing with current system)
   - Rollback plans
   - Data migration risks

7. **Implementation Checklist**
   - Phase-by-phase tasks
   - Verification steps per phase
   - Testing requirements
   - Performance benchmarks

8. **Documentation Index**
   - Complete list of all documentation files
   - Reading order for developers
   - Quick reference guide

**Output**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/migration/architecture.md`

---

### Task 10: Create Final Summary and Handoff Document
**Priority**: High  
**Estimated Effort**: 2-3 hours

**Requirements**:

Update the existing `REVERSE-ENGINEERING-SUMMARY.md` with final status and create a "start here" guide for AI or development team rebuilding the system.

**Updates to REVERSE-ENGINEERING-SUMMARY.md**:

1. **Mark all tasks 6-11 as complete** in the statistics table
2. **Update "What Remains To Document"** section (should be empty or "All documentation complete")
3. **Add new section: "How to Start Implementation"**
   - Step-by-step guide for beginning development
   - First commands to run
   - Project structure recommendations
   - Development environment setup

4. **Add "AI Prompt Templates" section**
   - Template prompts for implementing specific components
   - Example: "Implement Admin Authentication based on security/authentication.md"
   - Example: "Implement Tally Algorithm based on business-logic/tally-algorithms.md"
   - Example: "Create Vue 3 component for Ballot Entry based on ui-screenshots-analysis.md"

5. **Add "Known Gaps and Assumptions"**
   - Any areas where documentation may be incomplete
   - Assumptions made during reverse engineering
   - Areas that may need runtime testing to verify

6. **Add "Testing Strategy"**
   - How to verify migration accuracy
   - Tally comparison testing approach
   - Integration testing recommendations
   - Performance testing benchmarks

7. **Add "Maintenance and Updates"**
   - How to keep documentation in sync if original system changes
   - Versioning strategy

**Output**: Update `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/REVERSE-ENGINEERING-SUMMARY.md`

---

### Task 11: Review and Validation
**Priority**: High  
**Estimated Effort**: 2-3 hours

**Requirements**:

Final review of all documentation for completeness, accuracy, and usability.

**Review Checklist**:

1. **Completeness Review**
   - ✅ All 12 controllers documented (verify)
   - ✅ All 16 database entities documented (verify)
   - ✅ All 10 SignalR hubs documented (verify)
   - ✅ All 3 authentication systems documented (verify)
   - ✅ Tally algorithms fully documented (verify)
   - ✅ Authorization model complete (verify)
   - ✅ Configuration extracted and documented
   - ✅ All external integrations documented
   - ✅ Database ERD generated
   - ✅ Migration architecture comprehensive

2. **Accuracy Review**
   - Cross-check entity relationships against source code
   - Verify controller endpoint counts and signatures
   - Validate SignalR hub method signatures
   - Confirm tally algorithm logic matches source
   - Review authentication flow diagrams

3. **Usability Review**
   - Documentation is well-organized and easy to navigate
   - Cross-references between documents are accurate
   - Code examples are clear and complete
   - Migration recommendations are actionable
   - AI prompt templates are effective

4. **Consistency Review**
   - Terminology is consistent across all documents
   - Formatting is consistent
   - Section numbering and structure is consistent
   - No contradictory information between documents

5. **Gap Analysis**
   - Identify any missing critical information
   - Document any assumptions made
   - Note any areas requiring runtime testing

6. **Create Documentation Index**
   - Complete table of contents for all files
   - File sizes and line counts
   - Brief description of each file's purpose
   - Recommended reading order

**Output**: 
- Updated `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/plan.md` with review results
- New file: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/DOCUMENTATION-INDEX.md`

---

## Success Criteria

Documentation is complete when:

1. ✅ All configuration settings are extracted and mapped to .NET Core equivalents
2. ✅ All external integrations are documented with migration paths
3. ✅ Database ERD visually represents all entities and relationships
4. ✅ Migration architecture document provides complete rebuild blueprint
5. ✅ Final summary document serves as effective "start here" guide
6. ✅ All documentation passes completeness and accuracy review
7. ✅ An AI or development team can begin implementation without additional reverse engineering

## Implementation Workflow

1. **Task 6**: Document configuration settings from Web.config and related files
2. **Task 7**: Document all external integrations (OAuth, SMS, email, logging)
3. **Task 8**: Generate Mermaid ERD from entity documentation
4. **Task 9**: Create comprehensive migration architecture document
5. **Task 10**: Update summary document with final status and implementation guide
6. **Task 11**: Review all documentation for completeness and accuracy

**Estimated Total Time**: 1-2 days

## Key Resources

**Source Code Location**: `C:\Dev\TallyJ\v3\Site`  
**Documentation Location**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`  
**Previous Work**: ~55,000 lines of documentation already complete

## Out of Scope

The following are NOT part of this documentation effort:
- Actual implementation/coding of the new system
- Setting up development environments
- Creating new repositories
- Deploying infrastructure
- Writing actual migration scripts

This is **documentation only** to enable future implementation.
