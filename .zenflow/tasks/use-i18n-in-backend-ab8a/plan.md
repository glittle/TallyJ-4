# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: a176dc58-ef76-43e0-9af1-8da39790765b -->

Assess the task's difficulty, as underestimating it leads to poor outcomes.
- easy: Straightforward implementation, trivial bug fix or feature
- medium: Moderate complexity, some edge cases or caveats to consider
- hard: Complex logic, many caveats, architectural considerations, or high-risk changes

Create a technical specification for the task that is appropriate for the complexity level:
- Review the existing codebase architecture and identify reusable components.
- Define the implementation approach based on established patterns in the project.
- Identify all source code files that will be created or modified.
- Define any necessary data model, API, or interface changes.
- Describe verification steps using the project's test and lint commands.

Save the output to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach
- Source code structure changes
- Data model / API / interface changes
- Verification approach

If the task is complex enough, create a detailed implementation plan based on `{@artifacts_path}/spec.md`:
- Break down the work into concrete tasks (incrementable, testable milestones)
- Each task should reference relevant contracts and include verification steps
- Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint, write tests for a module). Avoid steps that are too granular (single function).

Important: unit tests must be part of each implementation task, not separate tasks. Each task should implement the code and its tests together, if relevant.

Save to `{@artifacts_path}/plan.md`. If the feature is trivial and doesn't warrant this breakdown, keep the Implementation step below as is.

---

### [x] Step: Create Translation Validation Script
<!-- chat-id: 09161336-1daa-4fb4-9830-79049a7359b6 -->

Create a Node.js script to validate translation file consistency.

**Tasks**:
- Create `frontend/src/locales/validate-translations.js`
- Script should check:
  - All locales have the same JSON files
  - All keys match across corresponding locale files
  - No duplicate keys within same locale
  - All values are non-empty strings
- Run script to validate current state before restructuring

**Verification**:
- Run script: `node frontend/src/locales/validate-translations.js`
- Script should report any inconsistencies
- Add script to package.json as `npm run validate:i18n`

---

### [x] Step: Restructure Frontend Locale Files
<!-- chat-id: 552aa3b2-da78-4a25-b3a5-0e270781aeb7 -->

Split large `en.json` and `fr.json` into smaller categorical files organized by locale subdirectories.

**Tasks**:
- Create directories: `frontend/src/locales/en/` and `frontend/src/locales/fr/`
- Split translations into these files (matching across locales):
  - `common.json` - Common UI strings (common.*)
  - `auth.json` - Authentication (auth.*)
  - `elections.json` - Elections (elections.*)
  - `people.json` - People (people.*)
  - `ballots.json` - Ballots (ballots.*)
  - `votes.json` - Votes (votes.*)
  - `results.json` - Results (results.*)
  - `dashboard.json` - Dashboard (dashboard.*)
  - `errors.json` - Errors (error.*, notification.*)
  - `nav.json` - Navigation (nav.*)
  - `profile.json` - Profile and settings
- Merge `shared.json` content into appropriate files
- Preserve existing `common.json` (non-translatable config) at root level
- Keep old files temporarily for verification

**Verification**:
- Run validation script: `npm run validate:i18n`
- Ensure all keys from original files are present in new structure
- Verify no duplicate keys exist

---

### [x] Step: Update Frontend Loader
<!-- chat-id: 166bebe4-e64a-40d7-8f6d-218d5d96cf9a -->

Update `frontend/src/locales/index.ts` to load from new file structure.

**Tasks**:
- Import all JSON files from `en/` and `fr/` subdirectories
- Update merge logic to combine multiple files per locale
- Maintain existing `deepMerge` and `flatToNested` utilities
- Keep backward compatibility with existing i18n usage
- Consider using Vite's glob import for cleaner code

**Verification**:
- Run `npm run dev` and verify app loads without errors
- Test language switching in UI
- Check browser console for missing translation warnings
- Run `npm run build` to ensure production build works
- Run `npm run test` to verify no tests are broken

---

### [x] Step: Migrate Backend Resources to JSON
<!-- chat-id: 09161336-1daa-4fb4-9830-79049a7359b6 -->

Extract backend .resx translations and add them to frontend JSON locale files.

**Tasks**:
- Extract all keys/values from `backend/Resources/ErrorMessages.en.resx` and `ErrorMessages.fr.resx`
- Map backend keys to appropriate JSON files:
  - Auth-related errors → `auth.json` under `auth.errors.*`
  - Generic errors → `errors.json` under `backend.errors.*`
- Add all backend translations to both `en/` and `fr/` JSON files
- Document mapping of old resx keys to new dotted keys
- Ensure no translations are lost

**Verification**:
- Run validation script: `npm run validate:i18n`
- Create a mapping document showing old key → new key
- Manually verify all translations from resx files are in JSON

---

### [ ] Step: Add JSON Localization to Backend

Integrate JSON-based localization into the ASP.NET Core backend.

**Tasks**:
- Research and add NuGet package (e.g., `My.Extensions.Localization.Json` or `AspNetCore.Localizer.Json`)
- If no suitable package, create custom implementation:
  - Create `backend/TallyJ4.Infrastructure/Localization/JsonStringLocalizer.cs`
  - Create `backend/TallyJ4.Infrastructure/Localization/JsonStringLocalizerFactory.cs`
  - Implement `IStringLocalizer` and `IStringLocalizerFactory`
- Add extension method for service registration
- Support flat dotted key notation (e.g., `auth.errors.invalidCredentials`)
- Implement culture-specific file lookup (en/*.json, fr/*.json)
- Add in-memory caching for performance

**Verification**:
- Create unit tests for JSON localizer:
  - Test reading files from configured path
  - Test culture-specific lookup
  - Test fallback to default culture
  - Test missing key handling
- Run `dotnet test`
- Run `dotnet build` to ensure no compilation errors

---

### [ ] Step: Configure Backend Localization

Update backend configuration to use JSON localization.

**Tasks**:
- Update `backend/appsettings.json`:
  - Add `Localization` section with `ResourcesPath`, `SupportedCultures`, `DefaultCulture`
- Update `backend/appsettings.Development.json` with relative path: `../frontend/src/locales`
- Update `backend/Program.cs`:
  - Replace `services.AddLocalization();` with JSON localization configuration
  - Configure supported cultures
  - Add request localization middleware if not present
- Update service classes using `IStringLocalizer` to use new dotted key notation:
  - `TallyJ4.Application.Services.Auth.LocalAuthService`
  - `TallyJ4.Application.Services.Auth.PasswordResetService`
  - `TallyJ4.Application.Services.Auth.TwoFactorService`

**Verification**:
- Run `dotnet build`
- Run backend: `dotnet run --project backend`
- Verify backend starts without errors
- Check logs for localization initialization messages
- Run `dotnet test` to ensure integration tests pass

---

### [ ] Step: Test and Verify Complete System

Perform end-to-end testing of both frontend and backend localization.

**Tasks**:
- **Backend Testing**:
  - Test API endpoints that return localized messages (auth errors, validation)
  - Test with `Accept-Language: en` header → verify English responses
  - Test with `Accept-Language: fr` header → verify French responses
  - Test missing translation handling (should fall back to key or default culture)
- **Frontend Testing**:
  - Browse all pages in English and French
  - Trigger validation errors and verify messages
  - Test authentication flows (login, register, 2FA, password reset)
  - Verify error messages display correctly
- **Integration Testing**:
  - Test that backend error messages match frontend expectations
  - Verify consistency between frontend and backend translations
- Run all linters and type checkers:
  - Backend: `dotnet format --verify-no-changes` (if configured)
  - Frontend: `npx vue-tsc --noEmit`

**Verification**:
- All manual tests pass
- No console errors or warnings about missing translations
- `npm run build` succeeds
- `npm run test` passes
- `dotnet test` passes
- `dotnet build` succeeds

---

### [ ] Step: Cleanup and Documentation

Remove old resource files and update documentation.

**Tasks**:
- Delete old resx files:
  - `backend/Resources/ErrorMessages.en.resx`
  - `backend/Resources/ErrorMessages.fr.resx`
  - `backend/Resources/Common.en.resx`
  - `backend/Resources/Common.fr.resx`
- Delete old frontend locale files:
  - `frontend/src/locales/en.json`
  - `frontend/src/locales/fr.json`
  - `frontend/src/locales/shared.json`
- Delete `backend/Resources/` directory if empty
- Update documentation:
  - Add section to README or developer docs about localization
  - Document how to add new translations
  - Document file organization and naming conventions
- Create `{@artifacts_path}/report.md` with:
  - Summary of implementation
  - Testing approach and results
  - Any challenges encountered
  - Future enhancement suggestions

**Verification**:
- Final build and test run:
  - `dotnet build && dotnet test` (backend)
  - `npm run build && npm run test` (frontend)
- Validation script confirms no errors: `npm run validate:i18n`
- All old files removed
- Documentation is clear and helpful
