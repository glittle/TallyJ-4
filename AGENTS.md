# TallyJ 4 agent notes

This file contains the repo-specific guidance that should stay current for AI agents and human contributors.

## Canonical docs

Use these as the primary sources of current project guidance:

- `AGENTS_REASONING.md`
- `README.md`
- `backend/README.md`
- `frontend/README.md`
- `docs/DEPLOYMENT.md`
- `E2E_TESTING_GUIDE.md`

Treat `.zenflow/tasks/**` and `.zencoder/**` as historical planning and research artifacts unless a task explicitly tells you to consult them.

## Repository shape

- `backend/` - ASP.NET Core API host (contains the complete application: all controllers, services, DTOs, entities, MainDbContext, identity models, enumerations, validators, mappings, hubs, etc.)
- `Backend.Tests/` - xUnit tests
- `frontend/` - Vue 3 SPA

## Project layering reality (important for AI agents)

After the domain consolidation (May 2025), the previous `Backend.Domain` and `Backend.Application` projects have been fully merged into the `backend/` host project. There is now effectively a single C# application project.

All code lives under `backend/`:

- **Data / Persistence**: `Context/MainDbContext.cs`, `Entities/`, `Enumerations/`, `Identity/AppUser.cs`, `Interfaces/`, `SecurityEnums.cs`
- **Auth layer** (previously the bulk of Backend.Application): `DTOs/Auth/`, `Services/Auth/` (JwtTokenService, LocalAuthService, TwoFactorService, etc.), plus related controllers and validators
- **Domain functionality** (the vast majority of the system): 
  - All controllers under `Controllers/`
  - DTOs under `DTOs/` (Elections, People, Ballots, Results, OnlineVoting, FrontDesk, etc.)
  - ~60+ services under `Services/` (ElectionService, TallyService, BallotService, PeopleService, DashboardService, ReportService, import/export services, analyzers, etc.)
  - All FluentValidation validators (`Validators/`)
  - All Mapster mapping profiles (`Mappings/`)
  - All SignalR hubs (`Hubs/`)
  - Authorization handlers, custom middleware, JSON localization provider, EF migrations and seeder, etc.

**Practical rule of thumb**:
Virtually all C# changes for features (election-scoped or otherwise) now happen inside the single `backend/` project. The test project is `Backend.Tests/`. Update DI registration lists in `backend/Program.cs` (`RegisterApplicationServices`, `RegisterAuthServices`, `RegisterBackgroundServices`) when adding new services.

The previous "dead duplicate ImportService" note no longer applies (the old Backend.Application project has been removed).

## Core conventions

### Frontend

- Vue SFC order is always:
  1. `<script setup lang="ts">`
  2. `<template>`
  3. `<style lang="less">`
- Do not use scoped `<style>`
- Nest style rules under the component root selector
- Use Pinia stores for state management
- Use `$t()` for all user-facing strings
- Standard flow: `component -> store -> service -> generated API client -> backend`

### Backend

- Use DTOs for API input/output
- Use **Mapster** (not AutoMapper) for entity-to-DTO mapping. Registration + scan in `backend/Program.cs:181-185` (`AddMapster()` + `TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly)`). Create profiles in `backend/Mappings/*Profile.cs` that implement `IRegister`. Inject `IMapper` (MapsterMapper namespace) in services. See `backend/Mappings/ElectionProfile.cs:12-42` for a full example with enum helpers and `IgnoreNullValues`.
- Use FluentValidation for request validation
- Keep controller logic thin and use services for business rules
- Default API auth is JWT unless an endpoint is explicitly anonymous

## Important architecture details

### Auth claim lookup

User IDs are stored in JWT `sub` claims. On .NET 10, code that reads the current user ID should check both:

```csharp
User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value
```

### API response wrappers

Two response patterns are in active use:

1. `ApiResponse<T>` returns payload in `.data`
2. `PaginatedResponse<T>` returns `.items` at the root

Do not assume every backend response uses the same wrapper.

### SignalR group naming and hubs

There are 7 hubs under `backend/Hubs/`. **Do not assume a single `election-{guid}` convention** (the previous short recommendation is not present in code).

Group name patterns (constructed via `GetGroupName` statics in each hub + used for broadcasts in `SignalRNotificationService.cs`):

- `Main{electionGuid}` — MainHub (election updates, statusChanged, electionClosed). Also creates `Main{ guid }Known` / `Main{ guid }Guest` variants.
- `Analyze{electionGuid}` — AnalyzeHub (tallyProgress / tallyComplete).
- `FrontDesk{electionGuid}` — FrontDeskHub (updatePeople, reloadPage, updateBallots, updateOnlineElection, etc.).
- `BallotImport{electionGuid}` — BallotImportHub (importProgress, importError, importComplete).
- `PeopleImport{electionGuid}` — PeopleImportHub (same import events).
- `online-election-{electionGuid}` — OnlineVotingHub.
- `public-display-{electionGuid}` — PublicHub (per-election public display pages).
- `Public` (static group) — PublicHub for global elections list / status broadcasts.

**Frontend side**: `src/services/signalrService.ts` provides `connectToMainHub()`, `connectToAnalyzeHub()`, `connectToFrontDeskHub()`, etc. + `joinElection(guid)`, `joinTallySession(guid)`, etc. Stores (electionStore, ballotStore, peopleStore, etc.) call these and wire `connection.on("eventName", handler)`.

When adding or touching real-time features, update the matching hub + the corresponding method in `SignalRNotificationService.cs` (see examples at lines 55, 73, 92, 111, 135, 152, 170, 189) + the frontend service + any store subscriptions together.

See also `backend/Hubs/MainHub.cs:107`, `AnalyzeHub.cs:119`, `FrontDeskHub.cs:118` and siblings for the `GetGroupName` implementations.

## Validation commands

### Frontend

```bash
cd frontend
npm run check
npm run test:run
```

## Regenerating the OpenAPI / TypeScript client

Any change to backend DTOs, controllers, or new endpoints requires regenerating the frontend client so that `src/api/gen/configService/` and the services/stores that depend on it stay in sync.

**Steps (clean dev environment)**:

1. Start (or restart) the backend in the **Development** environment (`dotnet run` from the `backend/` directory).  
   On startup it automatically executes `app.WriteOpenApiSpecToFile(...)` which writes the live contract to `frontend/openApi/tallyj.json` (see `backend/Program.cs:470-472`).

2. In another terminal: `cd frontend`

3. `npm run gen` (runs `@hey-api/openapi-ts` using the config at `frontend/openApi/config.backend.ts`).

   The script in `package.json` invokes the CLI with `--file ../openApi/config.backend.ts`. When `npm run gen` (or `npm start`) is executed from inside the `frontend/` directory — the standard local development workflow — this successfully loads the actual config file at `frontend/openApi/config.backend.ts`. The generator resolves relative paths declared inside the config file (for example `input.path: "./openApi/tallyj.json"`) relative to the directory containing that config file, not relative to the current working directory. This is why the documented commands work reliably without modification in normal use.

4. (Recommended after significant changes) `npm run check && npm run test:run`.

5. Inspect the generated output (`src/api/gen/configService/types.gen.ts`, `sdk.gen.ts`) and update any hand-written adapters, date converters, or custom `ElectionDto` / response mappings inside `src/services/*Service.ts` and the corresponding Pinia stores. All calls to
   the backend server should be through the generated API wrappers.

**Verification**:

- The changed or new endpoint appears in the generated SDK (grep the `sdk.gen.ts` file).
- Your feature works end-to-end (Swagger at `/swagger` on the running backend is always the source of truth for the current contract).

**Never** hand-edit anything under `src/api/gen/` — it is fully overwritten on the next successful regen.

See also:

- `frontend/openApi/config.backend.ts` (input spec path + output dir + plugins)
- `frontend/src/services/` (many thin wrappers + manual mapping examples, e.g. `electionService.ts`)
- The "Adding a frontend feature" workflow below once it is added.

## Locales

Run `npm run validate:i18n` when you touch locale files.

When adding new user-facing strings, **only add them to the English locale** (`src/locales/en/`).
Other languages are updated separately in periodic review cycles — do not add placeholder or machine-translated strings to non-English locales.

**Never edit files in `src/locales/bundled/`** — these are auto-generated during build by `npm run merge-locales` and will be overwritten. Always edit the source files in individual locale directories (e.g., `src/locales/en/auth.json`).

## Local development assumptions

- backend default dev URL: `http://localhost:5016`
- frontend default dev URL: `http://localhost:8095`
- backend development config seeds the database on startup
- Swagger is the source of truth for current routes and schemas

### Configuration sources (Program.cs)

The backend merges settings from many locations (later sources override earlier ones). In rough order:

- `appsettings.json` + `appsettings.{Environment}.json` (Development / Production / Testing)
- `appsettings.{MachineName}.json` (optional, reload-on-change)
- `appsettings.{SiteType}.json` (Development / Production / UAT / etc.)
- `version.json` (required, loaded from repo root)
- `TALLYJ_CONFIG_PATH` environment variable (points at a single JSON file — very useful for Docker / shared hosting)
- Hard-coded fallback locations: `c:\AppSettings\TallyJ4.json` and `TallyJ4.{SiteType}.json` (intentionally outside the repo tree)
- User secrets and environment variables (the recommended place for all secrets: `ConnectionStrings:TallyJ4`, `Jwt:Key`, `Google:*`, `Twilio:*`, `SuperAdmin:Emails`, etc.)
- `launchSettings.json` only affects `dotnet run` / `dotnet watch` profiles.

`backend/Program.cs:59-86` (and the startup logging that reports exactly which files were loaded) is the authoritative implementation. For local development work, prefer user secrets or a machine-specific file so you never accidentally commit secrets.

## Shell environment — detect before running commands

This repo is developed on both **Windows** and **Linux** (Azure DevOps CI runs on Windows,
GitHub Copilot / CI agents may run on Linux). Before issuing shell commands, check the
tool environment (e.g. the `env` block that identifies `OS Name`, or `uname` / `ver`
output) and pick the matching syntax. The sections below are split accordingly.

General rules that apply everywhere:

- Prefer the `Grep` (ripgrep) and `Read` tools over shell `findstr` / `grep` / `cat` — they work
  identically on both platforms and have no quoting pitfalls.
- Use forward slashes in code and config (`./path/to/file`); only switch to backslashes when
  invoking a native Windows command that requires them.
- Any code, tests, or scripts you write must run on **both** Windows and Linux unless the
  feature is explicitly platform-gated.

## Windows CMD file operations

_Applies only when the tool harness reports Windows / `cmd.exe`. On Linux use standard
POSIX equivalents (`rm -f`, `test -e`, etc.)._

When deleting repo files from the Windows CMD-based shell tooling in this workspace:

- run from the workspace root and use repo-relative paths
- for a single file, prefer `if exist "docs\OLD.md" del /q "docs\OLD.md"`
- for multiple files, prefer a guarded loop instead of one long `del` command:

```bat
for %f in ("API_DOCUMENTATION.md" "docs\ADMIN_GUIDE.md" "backend\API_EXAMPLES.md") do @if exist %f del /q %f
```

- verify deletions from the same worktree context with `if exist ...` and `git status --short`
- if `git status` does not show a tracked file as deleted, the removal did not affect the active worktree

## Windows CMD search and output parsing

_Applies only when the tool harness reports Windows / `cmd.exe`. On Linux, use `grep`
(or better, the `Grep` tool) with standard POSIX quoting — none of the pitfalls below apply._

When searching command output or files from CMD-based tooling, prefer the Grep tool over `findstr`. If you must use `findstr`:

- `findstr` does not accept multiple space-separated terms inside a single quoted string the way ripgrep does. Use `/c:"phrase"` for literal phrases, or repeat `/c:` for OR-style matching:

```bat
findstr /i /c:"canvote" /c:"canreceive" /c:"failed" file.txt
```

- Never chain searches with `&` and reuse quoted patterns containing spaces — CMD will treat trailing tokens as filenames and you get `FINDSTR: Cannot open ...` errors.
- Do not pipe `type file` into `more +0` as a way to "show all" — `more` paginates and truncates in this tool harness. Read the file directly with the `Read` tool when you want the full contents.
- For multi-term searches across the repo, the `Grep` tool (ripgrep) is faster and quoting is sane.

## Windows CMD directory changes

_Applies only when the tool harness reports Windows / `cmd.exe`. On Linux use `cd <path> && ...`
with normal POSIX quoting._

- `cd /d <path>` with an **unquoted** path works reliably in this tool harness:
  `cd /d c:\Dev\TallyJ\v4\repo && dotnet build ...`
- `cd /d "<path>"` with the path wrapped in **double quotes** has been observed to fail with
  `The filename, directory name, or volume label syntax is incorrect.` even for valid paths.
  Quote the path only if it actually contains spaces; otherwise leave it unquoted.
- CMD chaining uses `&&` (on success) or `&` (always). Semicolons (`;`) are **ignored** —
  do not port bash one-liners verbatim.

## Running dotnet commands

These apply on both Windows and Linux:

- For targeted test runs, `dotnet test --filter "FullyQualifiedName~ClassName.MethodPrefix"` works
  well. Multiple filters can be OR-combined with `|`:
  `--filter "FullyQualifiedName~A|FullyQualifiedName~B"`.
- Prefer `--no-build` on repeat test runs after a successful build to save ~10s per invocation.
- Backend must build and test cleanly on both OSes. SQL Server LocalDB is **not** available on
  Linux; integration tests use SQLite via `CustomWebApplicationFactory`. Any EF Core model tweaks
  should keep both providers working (e.g. the `DateTimeOffset` value converter applied when the
  provider is not SQL Server in `MainDbContext.OnModelCreating`).

Windows-only caveat:

- Do NOT pipe `dotnet build` / `dotnet test` output through `findstr` when the command itself
  contains double-quoted arguments (e.g. `--filter "FullyQualifiedName~..."`). CMD collapses the
  nested quotes and `findstr` ends up with mangled patterns like `FINDSTR: Cannot open test"`.
  Run the `dotnet` command without the pipe — the Bash tool captures full output — or save to a
  file first and use the `Grep` tool on that file. On Linux, piping through `grep` is safe.

## Frontend workflow note

For routine local development, prefer `npm run dev`. Only use `npm run build` when you explicitly need a production build or are validating production output.

## Reviewing and actioning external PR feedback (Copilot, human reviewers)

When a task is "review the Copilot suggestions on PR N and fix them here" (or equivalent for human review comments):

**Preferred extraction flow (use this pattern):**
1. Use the available `gh` CLI (it is usually logged in). Avoid web scraping PR pages.
2. Get structured metadata: `gh pr view N -R owner/repo --json files,baseRefName,headRefName`
3. Extract **only** the actionable comments with projection (critical for low context noise):
   ```bash
   gh api repos/owner/repo/pulls/N/comments \
     --jq 'map(select(.user.login | contains("copilot"))) | map({path, line, body, diff_hunk: (.diff_hunk | gsub("\n"; " | "))})' \
     > copilot-comments.json
   ```
4. Also save a clean diff for reference: `gh pr diff N -R owner/repo > pr.diff`
5. Use `read_file` on the small clean `.json`, not the raw full API response.
6. Read **only the files mentioned** in the comments (use `offset`/`limit` for large Vue or .ts files).

**During the work:**
- Track the set of comments + files with `todo_write`.
- For any frontend user-facing string changes, **strictly** obey the "Locales" section above (only edit `src/locales/en/`, run `npm run validate:i18n`).
- Correlate comments to code using the saved `diff_hunk` + the actual source (or the saved `pr.diff`).
- After edits, run the documented validation commands for the area changed (`npm run check` in frontend, relevant tests in backend).

**Environment notes:**
- This harness has shell quirks with complex pipes (`head`, `tail`, `Select-String`, `Out-String`). Prefer writing output to a file then using the `Read` or `Grep` tools on it.
- Consider invoking the available `code-review` or `review` skills (see system skills) to analyze the suggestions before you start editing — especially when there are >5 comments or the intent is ambiguous.

**Goal:** Minimal context bloat, fast comment → root cause → minimal correct fix, while respecting all project conventions.

## Documentation hygiene

When features change, update the canonical docs above instead of adding one-off summary files.

Avoid leaving behind implementation summaries, phase reports, or temporary investigation notes as long-term guidance unless the repository explicitly wants them preserved as archives.
