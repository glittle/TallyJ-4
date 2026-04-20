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

- `backend/` - ASP.NET Core API host
- `Backend.Application/` - application services
- `Backend.Domain/` - domain and persistence layer
- `Backend.Tests/` - xUnit tests
- `frontend/` - Vue 3 SPA

## Core conventions

### Frontend

- Vue SFC order is always:
  1. `<script setup lang="ts">`
  2. `<template>`
  3. `<style lang="less">`
- Do not use `<style scoped>`
- Nest style rules under the component root selector
- Use Pinia stores for state management
- Use `$t()` for all user-facing strings
- Standard flow: `component -> store -> service -> generated API client -> backend`

### Backend

- Use DTOs for API input/output
- Use AutoMapper for entity-to-DTO mapping
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

### SignalR group naming

Election-scoped hub groups should use:

```text
election-{electionGuid}
```

## Validation commands

### Frontend

```bash
cd frontend
npm run check
npm run test:run
```

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

*Applies only when the tool harness reports Windows / `cmd.exe`. On Linux use standard
POSIX equivalents (`rm -f`, `test -e`, etc.).*

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

*Applies only when the tool harness reports Windows / `cmd.exe`. On Linux, use `grep`
(or better, the `Grep` tool) with standard POSIX quoting — none of the pitfalls below apply.*

When searching command output or files from CMD-based tooling, prefer the Grep tool over `findstr`. If you must use `findstr`:

- `findstr` does not accept multiple space-separated terms inside a single quoted string the way ripgrep does. Use `/c:"phrase"` for literal phrases, or repeat `/c:` for OR-style matching:

```bat
findstr /i /c:"canvote" /c:"canreceive" /c:"failed" file.txt
```

- Never chain searches with `&` and reuse quoted patterns containing spaces — CMD will treat trailing tokens as filenames and you get `FINDSTR: Cannot open ...` errors.
- Do not pipe `type file` into `more +0` as a way to "show all" — `more` paginates and truncates in this tool harness. Read the file directly with the `Read` tool when you want the full contents.
- For multi-term searches across the repo, the `Grep` tool (ripgrep) is faster and quoting is sane.

## Windows CMD directory changes

*Applies only when the tool harness reports Windows / `cmd.exe`. On Linux use `cd <path> && ...`
with normal POSIX quoting.*

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

## Documentation hygiene

When features change, update the canonical docs above instead of adding one-off summary files.

Avoid leaving behind implementation summaries, phase reports, or temporary investigation notes as long-term guidance unless the repository explicitly wants them preserved as archives.
