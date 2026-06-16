# TallyJ v4 Development Context & Workflow

**Last Updated:** June 15, 2026  
**Purpose:** This document captures key decisions, priorities, and working practices from active development discussions. It helps maintain continuity across chat sessions and between the web interface and Grok CLI.

## Current Phase
- **Hardening Phase** (as of June 2026)
- Primary focus: Critical issues that affect correctness, data integrity, and reliability of election results.
- Master Tracker issue: [#167](https://github.com/glittle/TallyJ-4/issues/167)

## Priority Critical Issues
These are the highest-risk areas and should generally be addressed first:

| Issue | Title | Risk Area | Notes |
|-------|-------|-----------|-------|
| #168 | Core Election Analysis Engine | Analysis / Correctness | Heart of the system. Validate against known-good v3 data. |
| #190 / #189 | Ballot Validation & Pre-Finalization Integrity | Data Integrity | Catch problems before analysis/finalize. |
| #170 | Voter List Import / Export / Editing | Import/Export | Historical source of bugs in v3. |
| #171 | Front Desk Functionality | Front Desk | Heavily used during ballot gathering. |
| #172 | Election State Management & Teller Coordination | Core Framework | "Move all tellers to this state" reliability. |
| #188 / #169 / #187 | Online Ballot Acceptance & Random Name Resolution | Online Voting | Highest-risk new functionality. |
| #195 | Robust Error Handling & Recovery | Core Framework | Resilience layer. |
| #191 / #192 | Multi-Teller Sync & Authentication | Security / Coordination | Important for real-world multi-teller use. |

**Strategy:** Risk-first. Core analysis + validation before polishing UI or adding features.

## Recommended Workflow (Credit-Conscious)

### Division of Labor
- **Web chat (grok.com)** — Primary workspace for:
  - Prioritization and roadmap decisions
  - Breaking issues into scoped sub-tasks
  - Architecture and rule-based discussions
  - GitHub issue management and progress tracking
  - High-level planning and synthesis

- **Grok CLI (Composer)** — Used surgically for:
  - Code exploration inside the repo
  - Implementation and refactoring
  - Running tests, builds, and local verification
  - Short, focused sessions only

### Session Strategy
- **Web chat**: Prefer **long-running threads** for TallyJ v4 work. This preserves full context and decision history.
- **CLI**: Prefer **many short, well-scoped sessions**. This is more token-efficient given limited CLI credits.

### Handoff Practice
1. Do planning and scoping in the web chat first.
2. Create a narrow, well-defined task (ideally mapped to a checklist item).
3. Hand off to CLI with clear instructions.
4. Return to web chat to review results, update issues, and decide next steps.

## Key Decisions Made (June 2026)
- Focus on Critical issues from the Master Tracker before moving to High/Medium items.
- Use dedicated test elections (simple, ties, mixed methods, edge cases) for verification.
- CLI has direct GitHub issue reading capability (via MCP) and can pull issue bodies, checklists, and comments.
- Minimize unnecessary back-and-forth in the CLI to conserve credits.
- Long web chat threads are the preferred "control center" for this project.

## How to Use This Document
- Reference this file when starting new chat threads.
- Update it when major decisions or priority shifts occur.
- The CLI can also be instructed to read this file for context.

---

**Next Review:** After significant progress on #168 or the validation cluster (#190/#189).