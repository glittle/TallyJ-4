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

## Generated client error shape (hey-api)

**Status:** active  
**Evidence:** confirmed  
**Source:** `frontend/src/api/gen/configService/client/client.gen.ts` (`throw jsonError`); incident on Calculate Tally showing raw `elections.stageChangeError.*|count=N`  
**Revisit when:** client library or global error interceptor normalizes thrown errors to another shape

With `throwOnError: true`, failed API calls throw the **parsed JSON body** (e.g. `{ message, error, title, ... }`), not an axios-style `{ response: { data } }` wrapper.

UI that must translate server phrase keys (including `elections.stageChangeError.*` with `|count=N` params) should read the message via `extractApiErrorMessage` from `frontend/src/utils/errorHandler.ts`, then pass it through `translateElectionStageChangeError`. Reading only `error.response.data.message` skips the real payload and often falls through to showing the raw key.

**Rejected alternative:** assume axios error nesting in new pages. Rejected — the generated client is fetch-based; StageControl already uses `extractApiErrorMessage` for the same stage-change keys.
