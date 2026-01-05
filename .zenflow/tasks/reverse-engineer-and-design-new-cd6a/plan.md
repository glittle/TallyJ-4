# Full SDD workflow

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Workflow Steps

### [x] Step: Requirements
<!-- chat-id: e5270cff-83a2-46f1-97b8-f7ca4d6de4d9 -->

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `{@artifacts_path}/requirements.md`.

### [x] Step: Technical Specification
<!-- chat-id: 851f1371-4a21-4559-aaca-d66f0d31482e -->

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
<!-- chat-id: 30b12372-3dee-4700-b850-6a0eff3280c2 -->

Create a detailed implementation plan based on `{@artifacts_path}/spec.md`.

1. Break down the work into concrete tasks
2. Each task should reference relevant contracts and include verification steps
3. Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint, write tests for a module). Avoid steps that are too granular (single function) or too broad (entire feature).

If the feature is trivial and doesn't warrant full specification, update this workflow to remove unnecessary steps and explain the reasoning to the user.

Save to `{@artifacts_path}/plan.md`.

---

## Implementation Tasks

Based on reverse engineering work completed (database, authentication, SignalR, tally algorithms, UI screenshots), the following tasks complete the documentation needed for AI-driven rebuild. Estimated total effort: 2-3 days.

### [x] Task 1: Document Controller API Endpoints (AccountController, AfterController, BallotsController)
<!-- chat-id: a1473062-eac6-44cc-8e07-2c2f32d41c04 -->

Document the first 3 of 12 controllers with all endpoints, request/response models, and authorization requirements.

**Controllers**: AccountController (auth/login/OAuth), AfterController (reports/finalization), BallotsController (ballot entry)

**For each controller**:
- All public ActionResult methods (name, HTTP method, route)
- Request parameters and response types
- Authorization attributes
- Business logic classes called
- SignalR hub methods triggered

**Output**: `api/controllers/AccountController.md`, `AfterController.md`, `BallotsController.md`

**Verification**: Complete endpoint inventory with examples

---

### [x] Task 2: Document Controller API Endpoints (BeforeController, DashboardController, ElectionsController, Manage2Controller)
<!-- chat-id: 1ead24f1-40da-481e-ab82-f4b4ca5b0b54 -->

Document the next 4 controllers.

**Controllers**: BeforeController (front desk/roll call), DashboardController (dashboard), ElectionsController (CRUD), Manage2Controller (management)

**Output**: `api/controllers/BeforeController.md`, `DashboardController.md`, `ElectionsController.md`, `Manage2Controller.md`

**Verification**: Complete endpoint inventory with examples

---

### [x] Task 3: Document Controller API Endpoints (PeopleController, PublicController, SetupController, SysAdminController, VoteController)
<!-- chat-id: 03994676-aa95-4210-a6d5-55983bf9eb5c -->

Document the final 5 controllers.

**Controllers**: PeopleController (voters), PublicController (public pages), SetupController (setup wizard), SysAdminController (admin), VoteController (tallying)

**Output**: `api/controllers/PeopleController.md`, `PublicController.md`, `SetupController.md`, `SysAdminController.md`, `VoteController.md`

**Verification**: Complete endpoint inventory with examples

---

### [x] Task 4: Create Consolidated API Endpoints Summary
<!-- chat-id: 2c9e6849-783c-48f0-9191-6039dc1bbe91 -->

Create single reference document listing all API endpoints across all 12 controllers.

**Contents**:
- Complete endpoint inventory (method, route, controller, auth)
- Grouped by functional area
- REST API design recommendations for .NET Core

**Output**: `api/endpoints.md`

**Verification**: Quick reference for all 100+ endpoints

---

### [x] Task 5: Document Authorization Rules & Security Model
<!-- chat-id: 52e518ba-1285-4c93-9aab-493399e997c3 -->

Map all authorization attributes and security policies for .NET Core policy-based authorization.

**Contents**:
- Custom authorization attributes (`[ForAuthenticatedTeller]`, `[AllowVoter]`, etc.)
- FluentSecurity configuration
- Role definitions (Admin, Teller, Voter)
- Policy mapping for .NET Core migration

**Output**: `security/authorization.md`

**Verification**: All authorization patterns documented with migration recommendations

---

### [ ] Task 6: Document Configuration Settings
<!-- chat-id: 674d06a5-58d3-48ec-9f64-aed730a09bb8 -->

Extract and document all configuration from Web.config and code.

**Contents**:
- Web.config settings (connection strings, session state, SMTP)
- Unity DI registrations
- Environment-specific settings
- .NET Core migration (appsettings.json structure)

**Output**: `configuration/settings.md`

**Verification**: All configuration documented with migration path

---

### [ ] Task 7: Document External Integrations

Document all third-party service integrations.

**Integrations**:
- OAuth Providers (Google, Facebook)
- Twilio SMS (phone verification)
- Email Service (SMTP)
- Logging Services (LogEntries, IFTTT)

**Output**: `integrations/oauth.md`, `sms.md`, `email.md`, `logging.md`

**Verification**: All integration points documented with configuration requirements

---

### [ ] Task 8: Generate Database ERD

Create visual entity relationship diagram from existing entity documentation.

**Approach**:
- Use Mermaid ER diagram syntax
- Include all 16 core entities
- Show all relationships
- Color-code by functional area

**Output**: `database/erd.mmd`

**Verification**: Renders correctly in GitHub/Markdown viewers

---

### [x] Task 9: Create Migration Architecture Document

Synthesize all documentation into comprehensive migration guide.

**Contents**:
- Current vs. target architecture summary
- Migration strategy (9 phases from spec.md)
- Component mapping (ASP.NET → .NET Core)
- Critical path items and risk mitigation

**Output**: `migration/architecture.md`

**Verification**: Complete blueprint for rebuild ✅

---

### [ ] Task 10: Create Final Summary and Handoff Document

Create "start here" guide for AI or development team rebuilding the system.

**Contents**:
- Documentation index and reading order
- Implementation checklist
- AI prompt templates
- Known gaps and assumptions

**Output**: Update `REVERSE-ENGINEERING-SUMMARY.md` with final status

**Verification**: Serves as complete handoff package

---

### [ ] Task 11: Review and Validation

Final review of all documentation for completeness and accuracy.

**Review Checklist**:
- All 12 controllers documented
- Authorization model complete
- Configuration extracted
- All integrations documented
- ERD generated
- Migration architecture comprehensive
- Cross-references valid
- No critical business logic missing

**Output**: Updated plan.md with review results

**Verification**: Documentation ready for AI-driven rebuild
