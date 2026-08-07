# API contracts

## Dual response wrappers

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`)  
**Revisit when:** API envelope types are unified or a new envelope is introduced

Two response patterns are in active use:

1. `ApiResponse<T>` — payload in `.data`
2. `PaginatedResponse<T>` — `.items` at the root

**Reason:** pagination and simple success payloads evolved with different shapes; callers and generated clients must match the endpoint’s actual wrapper. Assuming every endpoint uses the same envelope causes empty UI data and mapping bugs.

**Rejected alternative:** force every controller onto a single envelope without a coordinated migration. Desirable long-term, but not current truth — code and clients must handle both until unified.

## OpenAPI TypeScript client regeneration

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`); `frontend/openApi/config.backend.ts`  
**Revisit when:** OpenAPI generation toolchain or client path changes

Backend DTO/controller/route changes require regenerating the frontend client (`frontend/src/api/gen/`). Dev flow: backend Development startup writes `frontend/openApi/tallyj.json`; then `npm run gen` from `frontend/`. Never hand-edit generated files.

**Reason:** the generated SDK is the contract surface for stores/services. Hand edits are overwritten and drift from Swagger.

**Rejected alternative:** hand-written fetch wrappers for each new endpoint. Rejected for this project’s scale — regeneration keeps types aligned with the running API.
