# Technical Specification: Use i18n in Backend

## Task Complexity Assessment
**Difficulty**: Medium

This task involves:
- Integrating a NuGet package for JSON-based localization
- Migrating existing resx resources to JSON
- Restructuring large JSON files into smaller, organized files
- Updating both backend and frontend configuration
- Ensuring consistency across locales

The task has moderate complexity due to the need to maintain compatibility between frontend and backend, restructure existing files, and ensure no translations are lost.

---

## Technical Context

### Current State

#### Frontend
- **Framework**: Vue 3 + vue-i18n
- **Locale Files Location**: `frontend/src/locales/`
- **File Structure**:
  - `en.json` (~20.6 KB, 424 lines) - English translations
  - `fr.json` (~21.24 KB, 389 lines) - French translations
  - `common.json` (497 B) - Shared configuration (non-translatable)
  - `shared.json` (497 B) - Shared translations
  - `index.ts` - Loader and i18n configuration

- **Key Format**: Flat dotted keys (e.g., `"common.language": "Language"`)
- **Loading Strategy**: 
  - Merges `shared.json` into locale-specific files
  - Converts flat dotted keys to nested objects
  - Uses `deepMerge` utility for combining translations

#### Backend
- **Framework**: ASP.NET Core (.NET 10)
- **Current Localization**: Standard .NET resource files (.resx)
- **Resource Files Location**: `backend/Resources/`
  - `ErrorMessages.en.resx` - English error messages
  - `ErrorMessages.fr.resx` - French error messages
  - `Common.en.resx` - English common strings (empty)
  - `Common.fr.resx` - French common strings (empty)

- **Current Resource Keys** (from ErrorMessages.resx):
  - `InvalidCredentials`
  - `EmailAlreadyExists`
  - `EmailRequired`
  - `PasswordRequired`
  - `UserNotFound`
  - `InvalidToken`
  - `TwoFactorRequired`
  - `Invalid2FACode`
  - `TwoFactorAlreadyEnabled`
  - `TwoFactorNotSetup`
  - `InvalidTwoFactorCode`
  - `InvalidPassword`
  - `PasswordResetNotAvailableForOAuth`
  - `FailedToGenerateResetToken`
  - `InvalidResetToken`
  - `ResetTokenExpired`

- **Usage**: `IStringLocalizer<T>` in service classes:
  - `TallyJ4.Application.Services.Auth.LocalAuthService`
  - `TallyJ4.Application.Services.Auth.PasswordResetService`
  - `TallyJ4.Application.Services.Auth.TwoFactorService`

- **Configuration**: `services.AddLocalization();` in `Program.cs` (line 207)

---

## Implementation Approach

### 1. NuGet Package Selection
**Selected Package**: `My.Extensions.Localization.Json` (or alternative if not available)
- Provides `IStringLocalizer` implementation for JSON files
- Compatible with ASP.NET Core 10
- Supports flat dotted key notation
- Allows configurable resource paths

**Alternative Approach**: If no suitable package exists, create a custom `IStringLocalizer` implementation that:
- Reads JSON files from configured path
- Caches translations in memory
- Supports culture-specific file lookup (e.g., `auth.en.json`, `auth.fr.json`)
- Implements `IStringLocalizer` and `IStringLocalizerFactory` interfaces

### 2. Locale Files Restructuring

#### Strategy: Grouped Files by Functional Area
Split large locale files into smaller, theme-based files. Each group must have matching translations across all locales.

**Proposed File Structure**:
```
frontend/src/locales/
├── en/
│   ├── common.json          # Common UI strings (buttons, actions, etc.)
│   ├── auth.json            # Authentication and authorization
│   ├── elections.json       # Election management
│   ├── people.json          # People management
│   ├── ballots.json         # Ballot management
│   ├── votes.json           # Vote recording and validation
│   ├── results.json         # Results and reporting
│   ├── dashboard.json       # Dashboard and statistics
│   ├── errors.json          # Error messages
│   ├── validation.json      # Validation messages
│   └── nav.json             # Navigation labels
├── fr/
│   ├── common.json
│   ├── auth.json
│   ├── elections.json
│   ├── people.json
│   ├── ballots.json
│   ├── votes.json
│   ├── results.json
│   ├── dashboard.json
│   ├── errors.json
│   ├── validation.json
│   └── nav.json
├── common.json              # Non-translatable shared config (keep as-is)
└── index.ts                 # Updated loader
```

**Key Grouping Examples**:
- **common.json**: `common.*`, generic UI elements
- **auth.json**: `auth.*`, login, registration, 2FA, password reset
- **elections.json**: `elections.*`, election CRUD, settings
- **people.json**: `people.*`, person management, import
- **ballots.json**: `ballots.*`, ballot entry, validation
- **votes.json**: `votes.*`, vote recording, invalidation
- **results.json**: `results.*`, tally, reports, exports
- **dashboard.json**: `dashboard.*`, statistics, overview
- **errors.json**: `error.*`, error messages, notifications
- **validation.json**: Validation error messages
- **nav.json**: `nav.*`, navigation labels

**Benefits**:
- Easier to maintain and edit specific areas
- Better organization for developers
- Smaller file sizes for easier review
- Can lazy-load sections in frontend (future optimization)
- Backend can load only needed sections (future optimization)

### 3. Backend Configuration

#### appsettings.json Addition
```json
{
  "Localization": {
    "ResourcesPath": "../frontend/src/locales",
    "SupportedCultures": ["en", "fr"],
    "DefaultCulture": "en"
  }
}
```

**Notes**:
- Path is relative to backend project root
- In production, this could point to an absolute path or shared volume
- Supports adding more languages in the future

#### Program.cs Changes
Replace the current localization setup:
```csharp
// Current (line 207):
services.AddLocalization();

// New:
services.AddJsonLocalization(options =>
{
    options.ResourcesPath = builder.Configuration["Localization:ResourcesPath"];
    options.SupportedCultures = builder.Configuration.GetSection("Localization:SupportedCultures").Get<string[]>();
    options.DefaultCulture = builder.Configuration["Localization:DefaultCulture"];
});
```

### 4. Frontend Loader Updates

#### Updated index.ts
The loader needs to:
1. Import all JSON files from locale subdirectories
2. Merge them into single locale objects
3. Handle the `common.json` config file separately
4. Maintain backward compatibility with current usage

```typescript
import { createI18n } from "vue-i18n";
import commonConfig from "./common.json";

// Import English translations
import enCommon from "./en/common.json";
import enAuth from "./en/auth.json";
import enElections from "./en/elections.json";
// ... import all other en files

// Import French translations
import frCommon from "./fr/common.json";
import frAuth from "./fr/auth.json";
import frElections from "./fr/elections.json";
// ... import all other fr files

// Utility function to deep merge objects
function deepMerge(target: any, source: any): any {
  const result = { ...target };
  for (const key in source) {
    if (
      source[key] &&
      typeof source[key] === "object" &&
      !Array.isArray(source[key])
    ) {
      result[key] = deepMerge(result[key] || {}, source[key]);
    } else {
      result[key] = source[key];
    }
  }
  return result;
}

// Utility function to convert flat dotted keys to nested objects
function flatToNested(flat: any): any {
  const result: any = {};
  for (const key in flat) {
    const keys = key.split(".");
    let current = result;
    for (let i = 0; i < keys.length - 1; i++) {
      const k = keys[i]!;
      if (!current[k]) {
        current[k] = {};
      }
      current = current[k];
    }
    current[keys[keys.length - 1]!] = flat[key];
  }
  return result;
}

// Merge all English translations
const en = deepMerge(
  enCommon,
  deepMerge(
    enAuth,
    deepMerge(enElections, /* ... merge all other en files */)
  )
);

// Merge all French translations
const fr = deepMerge(
  frCommon,
  deepMerge(
    frAuth,
    deepMerge(frElections, /* ... merge all other fr files */)
  )
);

const savedLocale = localStorage.getItem("preferred-language") || "en";

export const i18n = createI18n({
  locale: savedLocale,
  fallbackLocale: "en",
  messages: {
    en: flatToNested(en),
    fr: flatToNested(fr),
  },
});

export function setLocale(locale: "en" | "fr") {
  i18n.global.locale = locale;
  localStorage.setItem("preferred-language", locale);
}
```

**Alternative**: Use dynamic imports or Vite's glob import feature for cleaner code.

---

## Data Model / API / Interface Changes

### No Database Changes Required
This task only affects localization infrastructure, not data models.

### Service Layer Changes
- Services using `IStringLocalizer<T>` will continue to work unchanged
- The underlying implementation will switch from .resx to JSON
- Key names in services may need updates to match new JSON structure

### New Configuration Model
```csharp
public class LocalizationOptions
{
    public string ResourcesPath { get; set; }
    public string[] SupportedCultures { get; set; }
    public string DefaultCulture { get; set; }
}
```

---

## Source Code Files

### Files to Create
1. `backend/TallyJ4.Infrastructure/Localization/JsonStringLocalizer.cs` (if custom implementation)
2. `backend/TallyJ4.Infrastructure/Localization/JsonStringLocalizerFactory.cs` (if custom implementation)
3. `backend/TallyJ4.Infrastructure/Localization/LocalizationOptions.cs` (if custom implementation)
4. `frontend/src/locales/en/*.json` (11 new files)
5. `frontend/src/locales/fr/*.json` (11 new files)

### Files to Modify
1. `backend/Program.cs` - Update localization configuration
2. `backend/appsettings.json` - Add Localization section
3. `backend/appsettings.Development.json` - Add Localization section
4. `backend/TallyJ4.csproj` - Add NuGet package reference
5. `frontend/src/locales/index.ts` - Update loader for new structure
6. Any service files using localization keys that change

### Files to Delete
1. `backend/Resources/ErrorMessages.en.resx`
2. `backend/Resources/ErrorMessages.fr.resx`
3. `backend/Resources/Common.en.resx`
4. `backend/Resources/Common.fr.resx`
5. `frontend/src/locales/en.json` (split into multiple files)
6. `frontend/src/locales/fr.json` (split into multiple files)
7. `frontend/src/locales/shared.json` (merge into locale-specific files)

---

## Migration Strategy

### Phase 1: Backend Resource Migration
1. Extract all keys from ErrorMessages.en.resx and ErrorMessages.fr.resx
2. Map them to appropriate new JSON files (mostly `errors.json` or `auth.json`)
3. Add backend-specific keys to frontend JSON structure

### Phase 2: Frontend File Restructuring
1. Split `en.json` into categorical files under `en/` directory
2. Split `fr.json` into corresponding files under `fr/` directory
3. Ensure key parity across all locale files
4. Merge `shared.json` content into appropriate files
5. Keep `common.json` (non-translatable config) as-is

### Phase 3: Backend Integration
1. Add NuGet package or implement custom JSON localizer
2. Update `Program.cs` configuration
3. Update `appsettings.json` files
4. Test service classes using localization

### Phase 4: Frontend Loader Update
1. Update `index.ts` to import from subdirectories
2. Implement merging logic for multiple files per locale
3. Test i18n functionality in UI

### Phase 5: Cleanup
1. Remove old .resx files
2. Remove old `en.json` and `fr.json`
3. Remove `shared.json`

---

## Verification Approach

### Backend Tests
1. **Unit Tests**: Create tests for JSON localizer (if custom implementation)
   - Test file reading from configured path
   - Test culture-specific key lookup
   - Test fallback to default culture
   - Test missing key handling

2. **Integration Tests**: Update existing service tests
   - Test that auth services receive correct localized messages
   - Test locale switching
   - Test missing translation handling

3. **Manual Verification**:
   - Start backend with `ASPNETCORE_CULTURE=en` and verify English messages
   - Start backend with `ASPNETCORE_CULTURE=fr` and verify French messages
   - Trigger validation errors and check localized messages in API responses

### Frontend Tests
1. **Unit Tests**: Update or create tests for i18n loader
   - Test that all locale files are loaded
   - Test deep merge functionality
   - Test flatToNested conversion
   - Test missing translation handling

2. **E2E Tests**: Verify UI translations
   - Switch language and verify all text changes
   - Test forms with validation messages
   - Test error message display

3. **Manual Verification**:
   - Browse all pages in English and French
   - Trigger validation errors and check messages
   - Check console for missing translation warnings

### File Structure Validation
1. **Script**: Create a validation script that ensures:
   - All `en/*.json` files have corresponding `fr/*.json` files
   - All keys in `en/*.json` files exist in `fr/*.json` files (and vice versa)
   - No duplicate keys across different files in the same locale
   - All translations are non-empty strings

### Build and Lint
1. Run `dotnet build` in backend to ensure no compilation errors
2. Run `dotnet test` to execute all backend tests
3. Run `npm run build` in frontend to ensure no build errors
4. Run `npm run test` in frontend to execute all frontend tests
5. Run `npx vue-tsc --noEmit` to check TypeScript types

---

## Potential Challenges and Mitigations

### Challenge 1: Path Resolution
**Issue**: Backend needs to access frontend folder, which may not be in the same location in production.

**Mitigation**:
- Use configurable path in appsettings.json
- For production, copy JSON files to backend during build or deployment
- Alternative: Serve JSON files from backend as static assets
- Alternative: Use a shared volume or centralized translation service

### Challenge 2: Key Naming Conflicts
**Issue**: Backend uses simple key names (e.g., `InvalidCredentials`), frontend uses dotted notation (e.g., `auth.errors.invalidCredentials`).

**Mitigation**:
- Standardize on dotted notation for all new keys
- Keep backward compatibility by supporting both formats initially
- Gradually migrate backend services to use dotted notation
- Document naming convention in developer guide

### Challenge 3: File Split Coordination
**Issue**: Ensuring all translations are moved correctly and no keys are lost during restructuring.

**Mitigation**:
- Create a script to extract and validate all keys before/after split
- Use JSON diff tools to verify completeness
- Perform the split for one locale first, then mirror to others
- Keep old files until verification is complete

### Challenge 4: Build Performance
**Issue**: Loading many small JSON files may impact build/load time.

**Mitigation**:
- Measure before and after performance
- Use Vite's static imports (tree-shakeable)
- Consider build-time merging in production
- Lazy-load locale files on demand (future enhancement)

### Challenge 5: Developer Experience
**Issue**: Developers need to know which file to edit for specific translations.

**Mitigation**:
- Document the file organization clearly
- Create helper script to search for keys across files
- Use consistent naming conventions
- Consider using VS Code workspace search for quick lookup

---

## Reasons NOT to Proceed (Risk Assessment)

### Potential Concerns

1. **Increased Complexity**: Multiple JSON files instead of single files per locale
   - **Counter**: Better organization outweighs the slight complexity increase
   - **Counter**: Modern tooling handles multiple files easily

2. **Backend Dependency on Frontend Structure**: Backend now depends on frontend folder structure
   - **Counter**: Configuration makes this flexible
   - **Counter**: Can deploy JSON files independently if needed

3. **Duplicate Translations**: Same translations exist in both frontend and backend contexts
   - **Counter**: This is the goal - single source of truth
   - **Counter**: Eliminates divergence between frontend and backend messages

4. **Build Tooling Differences**: Frontend uses Vite, backend uses MSBuild
   - **Counter**: Both can read JSON files natively
   - **Counter**: No special build steps required

5. **Culture Header Handling**: Need to ensure backend respects Accept-Language header
   - **Counter**: ASP.NET Core localization middleware handles this automatically
   - **Counter**: Configuration is straightforward

### Recommendation
**Proceed**: The benefits of a unified translation system, easier maintenance, and single source of truth significantly outweigh the concerns. The task is achievable with the proposed approach, and the risks can be effectively mitigated with proper configuration and testing.

---

## Summary

This task will:
1. ✅ Unify frontend and backend localization using JSON files
2. ✅ Restructure large locale files into smaller, organized files
3. ✅ Migrate existing .resx resources to JSON format
4. ✅ Configure backend to read from frontend's locale folder
5. ✅ Maintain backward compatibility with existing code
6. ✅ Provide a scalable foundation for adding more languages

The implementation is medium complexity, requiring careful coordination between frontend and backend changes, but provides significant long-term benefits for maintainability and consistency.
