# Full SDD workflow

## Workflow Steps

### [x] Step: Requirements

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `c:\Dev\TallyJ\v4\repo\.zencoder\chats\44a6743a-a8b4-45af-bf5f-df5990f49083/requirements.md`.

### [x] Step: Technical Specification

Create a technical specification based on the PRD in `c:\Dev\TallyJ\v4\repo\.zencoder\chats\44a6743a-a8b4-45af-bf5f-df5990f49083/requirements.md`.

1. Review existing codebase architecture and identify reusable components
2. Define the implementation approach

Save to `c:\Dev\TallyJ\v4\repo\.zencoder\chats\44a6743a-a8b4-45af-bf5f-df5990f49083/spec.md` with:

- Technical context (language, dependencies)
- Implementation approach referencing existing code patterns
- Source code structure changes
- Data model / API / interface changes
- Delivery phases (incremental, testable milestones)
- Verification approach using project lint/test commands

### [x] Step: Planning

Create a detailed implementation plan based on `c:\Dev\TallyJ\v4\repo\.zencoder\chats\44a6743a-a8b4-45af-bf5f-df5990f49083/spec.md`.

1. Break down the work into concrete tasks
2. Each task should reference relevant contracts and include verification steps
3. Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint, write tests for a module). Avoid steps that are too granular (single function) or too broad (entire feature).

If the feature is trivial and doesn't warrant full specification, update this workflow to remove unnecessary steps and explain the reasoning to the user.

Save to `c:\Dev\TallyJ\v4\repo\.zencoder\chats\44a6743a-a8b4-45af-bf5f-df5990f49083/plan.md`.

### [x] Step: Implementation

#### Phase 1: Content Analysis (Preparation)
- **Task 1.1**: Read all 10 source .md files and analyze content structure
  - Read files: reverse-engineer-and-design-new-cd6a/requirements.md, plan-new-build-b9db/requirements.md, jan-26-b-57ab/spec.md, jan-26-872f/spec.md, jan-26-b-57ab/plan.md, jan-26-872f/plan.md, jan-26-872f/report.md, database/entities.md, business-logic/tally-algorithms.md, security/authentication.md
  - Document content themes and organization patterns
  - Verification: Content analysis summary created

- **Task 1.2**: Plan merging strategy for each document type
  - Define section headers and organization for requirements consolidation
  - Define section headers and organization for specifications consolidation
  - Define section headers and organization for plans consolidation
  - Define section headers and organization for reports consolidation
  - Define section headers and organization for reference consolidation
  - Verification: Merging strategy documented

#### Phase 2: Document Consolidation (Implementation)
- **Task 2.1**: Create consolidated requirements.md
  - Merge 3 requirements documents with chronological organization
  - Add section headers for different phases (reverse engineering, build planning)
  - Preserve all technical specifications and requirements
  - Verification: File created, content verified against originals

- **Task 2.2**: Create consolidated specifications.md
  - Merge 2 technical specification documents
  - Organize by system component (backend, frontend, database)
  - Preserve all implementation details and approaches
  - Verification: File created, content verified against originals

- **Task 2.3**: Create consolidated plans.md
  - Merge 2 implementation plan documents
  - Organize by development phase
  - Preserve task breakdowns and milestones
  - Verification: File created, content verified against originals

- **Task 2.4**: Create consolidated reports.md
  - Merge testing reports and issue documentation
  - Organize by severity and component
  - Preserve all findings and recommendations
  - Verification: File created, content verified against originals

- **Task 2.5**: Create consolidated reference.md
  - Include specialized technical documentation (entities, algorithms, authentication)
  - Organize by functional area (database, business logic, security)
  - Preserve all detailed technical information
  - Verification: File created, content verified against originals

#### Phase 3: Quality Assurance (Verification)
- **Task 3.1**: Verify content preservation
  - Cross-check all consolidated documents against original files
  - Ensure no content loss or corruption
  - Verify logical organization and flow
  - Verification: Content verification checklist completed

- **Task 3.2**: Validate document formatting and links
  - Check Markdown formatting in all consolidated documents
  - Verify internal references and links are valid
  - Test table of contents functionality
  - Verification: Format validation completed

#### Phase 4: Cleanup (Deployment)
- **Task 4.1**: Remove old task folders
  - Delete all subfolders under .zenflow/tasks/
  - Remove empty directories
  - Ensure no orphaned files remain
  - Verification: Directory listing shows only 5 .md files

- **Task 4.2**: Final verification and backup
  - Confirm clean directory structure
  - Create backup of consolidated documents if needed
  - Document completion status
  - Verification: Final directory structure verified
