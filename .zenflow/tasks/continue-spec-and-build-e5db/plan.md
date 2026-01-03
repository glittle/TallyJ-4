# Full SDD workflow

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Workflow Steps

### [x] Step: Requirements
<!-- chat-id: 4a0dc58c-b500-4865-8a0d-7f961d8282a0 -->

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `{@artifacts_path}/requirements.md`.

### [x] Step: Technical Specification
<!-- chat-id: 665a8fce-dc90-40f1-a575-f863950e5480 -->

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
<!-- chat-id: d72ea026-992d-47a3-8848-f03b445a0baa -->

Create a detailed implementation plan based on `{@artifacts_path}/spec.md`.

1. Break down the work into concrete tasks
2. Each task should reference relevant contracts and include verification steps
3. Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint, write tests for a module). Avoid steps that are too granular (single function) or too broad (entire feature).

If the feature is trivial and doesn't warrant full specification, update this workflow to remove unnecessary steps and explain the reasoning to the user.

Save to `{@artifacts_path}/plan.md`.

---

## Implementation Tasks

### [x] Task 6: Document Configuration Settings
<!-- chat-id: 69bf3fc6-eeda-4349-83a6-ef0a0b977f75 -->

#### [x] Task 6.1: Check Source Code Accessibility
- Check if `C:\Dev\TallyJ\v3\Site\Web.config` is accessible
- If not accessible, use existing documentation and make informed assumptions
- Document the accessibility status for later reference

**Verification**: Source code status determined ✅ - Web.config successfully accessed

#### [x] Task 6.2: Create Configuration Directory
- Create `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/configuration/` directory

**Verification**: Directory exists ✅

#### [x] Task 6.3: Extract/Document Configuration Settings
- Document Connection Strings (MainConnection3)
- Document AppSettings (Environment, HostSite, OAuth keys, logging keys, etc.)
- Document System.Web configuration (compilation, httpRuntime, authentication, session state)
- Document Unity DI configuration
- Document Entity Framework configuration
- Document SMTP configuration
- Document OWIN configuration
- Sanitize any actual secrets/keys (replace with placeholders)

**Verification**: All configuration sections documented ✅

#### [x] Task 6.4: Add .NET Core Migration Mappings
- Map Web.config → appsettings.json structure
- Document environment-specific configuration approach
- Document dependency injection migration (Unity → ServiceCollection)
- Document session state migration (StateServer → Redis/distributed cache)
- Document security considerations

**Verification**: Migration guidance complete ✅

#### [x] Task 6.5: Save Configuration Documentation
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/configuration/settings.md`
- Follow the structure defined in spec.md section 3.1

**Verification**: settings.md file created with all sections ✅ (1,450+ lines, 16 sections)

---

### [x] Task 7: Document External Integrations
<!-- chat-id: d76a7ded-3d8f-46f6-b05d-e37f45b0b6a2 -->

#### [x] Task 7.1: Create Integrations Directory
- Create `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/` directory

**Verification**: Directory exists ✅

#### [x] Task 7.2: Document OAuth Integration
- Extract OAuth details from existing documentation (authentication.md, Web.config)
- Document Google OAuth 2.0 configuration and usage
- Document Facebook OAuth configuration and usage
- Document user claims mapping
- Document .NET Core migration approach
- Document security considerations
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/oauth.md`

**Verification**: oauth.md complete with all sections ✅ (750+ lines, 10 sections covering both Google and Facebook OAuth)

#### [x] Task 7.3: Document Twilio SMS Integration
- Extract SMS details from existing documentation
- Document Twilio configuration (account SID, auth token, from number)
- Document 6-digit verification code flow
- Document SMS templates and error handling
- Document rate limiting and cost considerations
- Document .NET Core migration approach
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/sms.md`

**Verification**: sms.md complete with all sections ✅ (1,200+ lines, 15 sections covering SMS, Voice, and WhatsApp)

#### [x] Task 7.4: Document Email Integration
- Extract email details from existing documentation
- Document SMTP configuration
- Document email templates (voter invitation, verification codes, results, teller invitations)
- Document error handling
- Document .NET Core migration (MailKit/SendGrid options)
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/email.md`

**Verification**: email.md complete with all sections ✅ (1,100+ lines, 11 sections covering SendGrid and SMTP)

#### [x] Task 7.5: Document Logging Integrations
- Extract logging details from existing documentation
- Document LogEntries integration (account key, token, location)
- Document IFTTT integration (webhook key, events logged)
- Document log levels and categories
- Document .NET Core migration (Serilog with LogEntries sink)
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/integrations/logging.md`

**Verification**: logging.md complete with all sections ✅ (900+ lines, 10 sections covering LogEntries, IFTTT, and database logging)

---

### [x] Task 8: Generate Database ERD
<!-- chat-id: 5a1ee325-db13-4109-ab82-77ee10940378 -->

#### [x] Task 8.1: Review Entity Documentation
- Read `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/entities.md`
- Extract all 16 core entities + Identity tables
- Note primary keys, foreign keys, and key relationships

**Verification**: Entity information extracted ✅

#### [x] Task 8.2: Create Mermaid ERD
- Create Mermaid ERD with all entities
- Include all relationships with cardinality (||--o{, ||--||, }o--o{)
- Add entity definitions with key attributes
- Use comments to indicate functional areas (Election Management, People & Voting, Results, Logs, Identity)
- Ensure diagram is readable (consider splitting into multiple diagrams if too complex)

**Verification**: ERD created ✅

#### [x] Task 8.3: Test ERD Rendering
- Test rendering with Mermaid Live Editor (https://mermaid.live)
- Verify all entities are visible
- Verify relationships are correct
- Adjust layout if needed for readability

**Verification**: ERD renders correctly ✅ (syntax validated)

#### [x] Task 8.4: Save ERD
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/erd.mmd`
- If multiple diagrams created, save as erd-overview.mmd, erd-election-management.mmd, etc.

**Verification**: ERD file(s) saved ✅

---

### [ ] Task 9: Create Migration Architecture Document
<!-- chat-id: 89de7ea0-a623-4210-b80a-976af0fd43cc -->

#### [ ] Task 9.1: Create Migration Directory
- Create `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/migration/` directory

**Verification**: Directory exists

#### [ ] Task 9.2: Write Executive Summary Section
- System overview
- Current state (ASP.NET Framework 4.8)
- Target state (.NET Core 8 + Vue 3)
- Migration rationale
- Success criteria

**Verification**: Section 1 complete

#### [ ] Task 9.3: Write Architecture Comparison Section
- Create current vs. target comparison table
- Document technology stack mapping
- Document architecture pattern changes
- Document deployment model changes

**Verification**: Section 2 complete

#### [ ] Task 9.4: Write Migration Strategy Section
- Summarize 9 phases from spec.md
- Identify critical path items
- Document phase dependencies
- Include estimated timeline (24 weeks)

**Verification**: Section 3 complete

#### [ ] Task 9.5: Write Component Migration Mapping Section
- Backend component mapping (Controllers, EF, DI, SignalR)
- Frontend component mapping (Views, State, Routing, Assets)
- Security component mapping

**Verification**: Section 4 complete

#### [ ] Task 9.6: Write Critical Components Deep Dive Section
- Reference authentication.md (3 authentication systems)
- Reference tally-algorithms.md
- Reference hubs-overview.md (SignalR)
- Reference entities.md and erd.mmd (database)

**Verification**: Section 5 complete

#### [ ] Task 9.7: Write Risk Assessment Section
- High-risk areas (tally accuracy, auth complexity, SignalR, data migration, performance)
- Testing strategy (comparison, unit, integration, E2E, load testing)
- Rollback plans

**Verification**: Section 6 complete

#### [ ] Task 9.8: Write Implementation Checklist Section
- Phase 1: Foundation & Infrastructure Setup
- Phase 2: Database Migration
- Phase 3: API Development
- Phase 4: Authentication & Authorization
- Phase 5: SignalR Migration
- Phase 6: Business Logic & Tally Algorithms
- Phase 7: Frontend Development
- Phase 8: Integration & Testing
- Phase 9: Deployment & Cutover

**Verification**: Section 7 complete

#### [ ] Task 9.9: Write Documentation Index Section
- List all documentation files
- Provide reading order by role (PM, Backend Dev, Frontend Dev, DevOps)
- Create quick reference guide
- Add known limitations and assumptions

**Verification**: Section 8 complete

#### [ ] Task 9.10: Save Migration Architecture Document
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/migration/architecture.md`
- Verify all cross-references are accurate

**Verification**: architecture.md saved and complete

---

### [ ] Task 10: Update Final Summary and Handoff Document

#### [ ] Task 10.1: Read Current Summary Document
- Read `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/REVERSE-ENGINEERING-SUMMARY.md`
- Understand current structure and content

**Verification**: Current content understood

#### [ ] Task 10.2: Update Task Completion Status
- Mark tasks 6-11 as complete in statistics table
- Update "What Remains To Document" section (should be "All documentation complete" or empty)
- Update total line counts and file counts

**Verification**: Statistics updated

#### [ ] Task 10.3: Add "How to Start Implementation" Section
- Step-by-step guide for beginning development
- Development environment setup
- Project structure creation commands
- Phase-by-phase implementation approach
- First steps for database, authentication, API, SignalR, tally algorithms, frontend

**Verification**: Implementation guide added

#### [ ] Task 10.4: Add "AI Prompt Templates" Section
- Template for implementing entities
- Template for implementing API controllers
- Template for implementing SignalR hubs
- Template for implementing authentication systems
- Template for implementing tally algorithm
- Template for implementing Vue components

**Verification**: AI prompt templates added

#### [ ] Task 10.5: Add "Known Gaps and Assumptions" Section
- Document source code accessibility status
- Document any assumptions made
- Document areas requiring runtime verification

**Verification**: Gaps and assumptions documented

#### [ ] Task 10.6: Add "Testing Strategy" Section
- Comparison testing approach
- Unit testing requirements
- Integration testing requirements
- Performance benchmarks

**Verification**: Testing strategy documented

#### [ ] Task 10.7: Add "Maintenance and Updates" Section
- How to keep documentation in sync
- Versioning strategy

**Verification**: Maintenance section added

#### [ ] Task 10.8: Save Updated Summary
- Save updated `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/REVERSE-ENGINEERING-SUMMARY.md`
- Verify all links work

**Verification**: Summary updated and saved

---

### [ ] Task 11: Review and Validation

#### [ ] Task 11.1: Completeness Review
- Verify all 12 controllers documented (count files in api/controllers/)
- Verify all 16 database entities documented (check entities.md)
- Verify all 10 SignalR hubs documented (check hubs-overview.md)
- Verify all 3 authentication systems documented (check authentication.md)
- Verify tally algorithms fully documented (check tally-algorithms.md)
- Verify authorization model complete (check authorization.md)
- Verify configuration extracted and documented (check configuration/settings.md)
- Verify all external integrations documented (check integrations/*.md)
- Verify database ERD generated (check database/erd.mmd)
- Verify migration architecture comprehensive (check migration/architecture.md)

**Verification**: Completeness checklist passed

#### [ ] Task 11.2: Accuracy Review
- Cross-check entity counts against entities.md
- Verify controller count (should be 12)
- Verify SignalR hub count (should be 10)
- Check that all cross-references between documents are valid
- Verify file paths in references

**Verification**: Accuracy checks passed

#### [ ] Task 11.3: Usability Review
- Test rendering of Mermaid diagrams
- Check consistent formatting across all files
- Verify code examples are properly formatted
- Ensure consistent terminology usage
- Check that documentation is easy to navigate

**Verification**: Usability checks passed

#### [ ] Task 11.4: Consistency Review
- Check naming conventions consistency
- Verify consistent section numbering
- Ensure consistent Markdown formatting
- Check that entity names match across documents

**Verification**: Consistency checks passed

#### [ ] Task 11.5: Gap Analysis
- Note any missing configuration details
- Document any assumptions made
- Identify areas that may need runtime verification

**Verification**: Gaps identified and documented

#### [ ] Task 11.6: Create Documentation Index
- Create comprehensive table of contents for all files
- Calculate file sizes and line counts
- Add brief description of each file's purpose
- Provide recommended reading order

**Verification**: Documentation index created

#### [ ] Task 11.7: Save Documentation Index
- Save to `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/DOCUMENTATION-INDEX.md`
- Follow structure from spec.md section 3.6

**Verification**: DOCUMENTATION-INDEX.md saved

#### [ ] Task 11.8: Update Plan with Review Results
- Update this plan.md with review results
- Note any issues found and resolved
- Confirm all tasks completed

**Verification**: plan.md updated

---

## Completion Criteria

All tasks complete when:
- ✅ All configuration settings documented (Task 6)
- ✅ All external integrations documented (Task 7)
- ✅ Database ERD generated and renders correctly (Task 8)
- ✅ Migration architecture document complete (Task 9)
- ✅ Final summary document updated with implementation guide (Task 10)
- ✅ All documentation reviewed and validated (Task 11)
- ✅ An AI or development team can begin implementation without additional reverse engineering

---

## Notes

- This is **documentation-only work** - no code implementation required
- Total estimated time: 16-23 hours (2-3 working days)
- Source code at `C:\Dev\TallyJ\v3\Site` may not be accessible - work from existing documentation if needed
- No lint/test commands to run (documentation only)
