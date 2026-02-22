
# Copilot Instructions for TallyJ-4

## Project Overview
TallyJ-4 is a full-stack, real-time election management and ballot tallying system for Bahá'í communities. It uses a .NET 10 ASP.NET Core Web API backend and a Vue 3 + Vite SPA frontend. The system is designed for multi-user collaboration, secure authentication, and robust election workflows, with a focus on feature parity and modernization from TallyJ v3.

## Architecture & Key Patterns
- **Backend** (`backend/`):
  - ASP.NET Core Web API (controllers in `Controllers/`)
  - Entity Framework Core for data access (`EF/`)
  - DTOs and AutoMapper profiles for entity-DTO mapping (`DTOs/`, `Mappings/`)
  - SignalR hubs for real-time updates (`Hubs/`)
  - Global error handling, JWT authentication, and Serilog logging
  - Database seeding on startup (see `SETUP.md`)
  - Service layer with interface-based design
  - Testing: xUnit (unit/integration), see `TallyJ4.Tests/`
- **Frontend** (`frontend/`):
  - Vue 3 (Composition API), TypeScript, Vite
  - State management with Pinia, UI with Element Plus
  - API calls via Axios, real-time via SignalR
  - Environment config via `.env` files
  - Testing: Vitest, @vue/test-utils
  - Design system: Element Plus + custom tokens/components

## Developer Workflows
- **Backend:**
  - Restore/build/run: `cd backend && dotnet restore && dotnet build && dotnet run`
  - Database migration: `dotnet ef database update`
  - Reset DB: `cd backend/scripts && ./reset-database.ps1` (Win) or `./reset-database.sh` (Unix)
  - Test users auto-seeded; see `SETUP.md` for credentials
  - Run tests: `dotnet test` (see `TallyJ4.Tests/`)
  - Lint/format: `dotnet format` (optional)
- **Frontend:**
  - Install deps: `cd frontend && npm install`
  - Dev server: `npm run dev` (default port 8095)
  - Build: `npm run build`
  - Test: `npm run test` or `npm run test:coverage`
  - Type check: `npx vue-tsc --noEmit`
  - API base URL set in `.env` as `VITE_API_URL`

## Planning & Documentation
- All major requirements, specs, and plans are consolidated in `.zenflow/tasks/` (see `requirements.md`, `specifications.md`, `plans.md`, `reports.md`, `reference.md`).
- Use the v3 vs v4 feature matrix (`v3_vs_v4_feature_matrix.md`) to identify feature gaps and priorities.
- UI/UX patterns and workflows are documented in `v3_ui_patterns.md` (multi-step wizards, state-based navigation, admin/assistant roles).
- Implementation is phased (A-G): documentation, critical fixes, missing features, UI polish, testing, reporting, deployment.

## Conventions & Integration
- **API endpoints**: `/auth/*`, `/api/elections`, `/api/people`, etc. (see `README.md` and `.zenflow/tasks/specifications.md` for full list)
- **DTOs**: All API input/output uses DTOs in `backend/DTOs/`
- **AutoMapper**: Entity-DTO mapping in `backend/Mappings/`
- **Real-time**: SignalR hubs in `backend/Hubs/` and frontend SignalR clients; group names standardized as `election-{electionGuid}`
- **Testing**: Unit/integration tests in `TallyJ4.Tests/` (backend), Vitest for frontend
- **Seeding**: On first run, DB is seeded with users, elections, ballots, etc.
- **Reverse engineering docs**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`
- **Documentation consolidation**: `.zenflow` and `.zencoder` contain all planning, requirements, and technical docs; see consolidation strategy in `.zencoder/chats/44a6743a-a8b4-45af-bf5f-df5990f49083/`.

## Notable Patterns & Tips
- Use DTOs for all API communication; never expose EF entities directly
- Use AutoMapper for mapping between entities and DTOs
- Real-time updates (e.g., ballot status, results) use SignalR hubs; follow group naming conventions
- All authentication is JWT-based; tokens required for protected endpoints
- Database seeding is idempotent and can be disabled in `appsettings.Development.json`
- For new migrations, use `dotnet ef migrations add <Name>`
- For troubleshooting, see `backend/SETUP.md` and logs via Serilog
- UI/UX: Use multi-step wizards for election setup, state-based navigation, and responsive layouts (see `v3_ui_patterns.md`)
- Feature development should reference the v3 vs v4 matrix to avoid regressions
- Documentation and planning: follow the SDD workflow and consolidation patterns in `.zenflow` and `.zencoder`
- **Important**: See `AGENTS.md` for critical Vue component structure requirements and build workflow notes

## Feature Gaps & Priorities
- Use `v3_vs_v4_feature_matrix.md` to identify missing/partial features (e.g., location management, teller assignment, online voting, audit logs, public display)
- Prioritize features marked HIGH in the matrix
- Expose all entity fields in UI forms as per v3 parity requirements

## References
- [backend/SETUP.md](../backend/SETUP.md)
- [README.md](../README.md)
- [frontend/README.md](../frontend/README.md)
- [backend/Controllers/], [backend/DTOs/], [backend/Hubs/], [backend/Mappings/]
- [TallyJ4.Tests/]
- [.zenflow/tasks/requirements.md], [.zenflow/tasks/specifications.md], [.zenflow/tasks/plans.md], [.zenflow/tasks/reports.md], [.zenflow/tasks/reference.md]
- [.zenflow/tasks/v3_vs_v4_feature_matrix.md], [.zenflow/tasks/v3_ui_patterns.md]
- [.zencoder/rules/repo.md], [.zencoder/chats/44a6743a-a8b4-45af-bf5f-df5990f49083/plan.md]

---
For any unclear or missing conventions, please request clarification or review the referenced documentation.