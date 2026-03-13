# Agent Instructions for TallyJ-4

## Development Workflow Notes

### Frontend Build Process

- **Do NOT run `npm run build`** after making frontend changes - the developer has it running in watch mode
- Focus on code changes and let the watch process handle the build

### Vue Component Structure

Vue files **MUST** follow this exact order:

1. `<script setup lang="ts">` - TypeScript script (Composition API)
2. `<template>` - HTML template
3. `<style lang="less">` - Styles using Less

**Important Style Rules:**

- **NEVER use `<style scoped>`** - This breaks the component styling pattern
- All CSS content must be nested inside the component's root element CSS selector
- Example:
  ```vue
  <style lang="less">
  .my-component {
    // All component styles nested here
    .child-element {
      // Child styles
    }
  }
  </style>
  ```

### Coding Conventions

#### Backend (.NET)

- Use DTOs for all API communication (never expose EF entities directly)
- Use AutoMapper for entity-DTO mapping
- All endpoints require JWT authentication unless explicitly marked with `[AllowAnonymous]`
- Follow service layer pattern with interfaces
- Use FluentValidation for request validation
- SignalR hub group names: `election-{electionGuid}`

#### Frontend (Vue 3 + TypeScript)

- Use Composition API with `<script setup>`
- TypeScript strict mode enabled - all types must be defined
- Use Pinia stores for state management
- API calls via Axios with interceptors for auth
- Element Plus for UI components
- Use `$t()` for all user-facing strings (i18n)
- Responsive design required (mobile-first approach)

### Testing Requirements

- Backend: xUnit tests in `Backend.Tests/`
- Frontend: Vitest tests co-located with components
- All new features require tests
- Run tests before committing changes

### Database Changes

- Use EF Core migrations: `dotnet ef migrations add <Name>`
- Test migrations with reset script: `backend/scripts/reset-database.ps1` or `.sh`
- Database seeding is idempotent and automated

### Package Management

- When upgrading NuGet packages, prefer manual editing of `.csproj` files over `dotnet add package` if CLI commands fail
- If `dotnet list package --outdated` fails with "Value cannot be null" errors, check for project reference issues before retrying
- As fallback for checking outdated packages, use NuGet API directly: `https://api.nuget.org/v3-flatcontainer/{package-id}/index.json`
- Always verify package upgrades by running `dotnet build` after changes
- For security vulnerabilities, prioritize upgrading to latest patch versions (e.g., 4.15.0 → 4.15.1)

### Environment-Specific Commands

#### Windows CMD Environment (Local Development)

- **Path syntax**: Always use backslashes (`\`) in file paths, never forward slashes (`/`)
- **Drive letters**: Use uppercase (e.g., `C:\Dev\TallyJ\v4\repo`, not `c:\dev\tallyj\v4\repo`)
- **Directory changes**: Use `cd /d "C:\full\path\to\directory"` for changing drives/directories
- **Command chaining**: Use `&&` for sequential commands, not `;` (which is ignored in CMD)
- **Quotes**: Use double quotes for paths with spaces: `"C:\Program Files\app.exe"`
- **Common mistake**: Avoid Unix-style commands like `cat`, `grep`, `ls` - use Windows equivalents (`type`, `findstr`, `dir`)

#### Linux/bash Environment (GitHub Copilot/CI)

- **Path syntax**: Use forward slashes (`/`) in file paths
- **Directory changes**: Use `cd /full/path/to/directory`
- **Command chaining**: Use `&&` for sequential commands or `;` for unconditional chaining
- **Quotes**: Use double quotes for paths with spaces: `"/path/to/my dir/app"`
- **Available commands**: Standard Unix tools (`cat`, `grep`, `ls`, etc.) are available

### Documentation

- Update relevant README files when adding features
- Document API changes in Swagger (XML comments)
- Update `.zenflow/tasks/` documentation for major features
- Follow the v3 vs v4 feature matrix when implementing features

# Adding a New Language to TallyJ

This document outlines the steps required to add a new language to the TallyJ application.

## Prerequisites

- Language code (ISO 639-1 format, e.g., "ko" for Korean)
- Country flag code (ISO 3166-1 alpha-2 format, e.g., "kr" for South Korea)
- Native language name in the target language

## Steps

### 1. Create Language Directory and Stub Translations

Create a new directory under `frontend/src/locales/` with the language code:

```
frontend/src/locales/{lang}/
```

Create at least a `common.json` file with minimal translations:

```json
{
  "title": "TallyJ v4",
  "{lang}": "{Native Language Name}"
}
```

Example for Korean:

```json
{
  "title": "TallyJ v4",
  "korean": "한국어"
}
```

### 2. Update I18n Configuration

Edit `frontend/src/locales/index.ts`:

1. Add the language module import:

   ```typescript
   const {lang}Modules = import.meta.glob("./{lang}/*.json", { eager: true });
   ```

2. Add the language to the messages object:

   ```typescript
   messages: {
     en: deepMerge(common, mergeLocaleFiles(enModules)),
     fr: deepMerge(common, mergeLocaleFiles(frModules)),
     {lang}: deepMerge(common, mergeLocaleFiles({lang}Modules)),
   },
   ```

3. Update the setLocale function type:
   ```typescript
   export function setLocale(locale: "en" | "fr" | "{lang}") {
   ```

### 3. Update Common Translations

Add the language name to `frontend/src/locales/common.json`:

```json
{
  "title": "TallyJ v4",
  "english": "English",
  "french": "Français",
  "{lang}": "{Native Language Name}",
  ...
}
```

### 4. Update Language Selector Components

**LanguageSelector.vue** (dropdown selector):
Add the new language to the languages array:

```typescript
const languages = [
  { value: "en", flag: "us", label: t("english") },
  { value: "fr", flag: "fr", label: t("french") },
  { value: "{lang}", flag: "{flag}", label: t("{lang}") },
];
```

**LanguageFlagsSelector.vue** (flag button selector):
Add the new language to the languages array:

```typescript
const languages = [
  { value: "en", flag: "us", label: "English" },
  { value: "fr", flag: "fr", label: "Français" },
  { value: "{lang}", flag: "{flag}", label: "{Native Language Name}" },
];
```

### 5. Add CSS Font Support

Add language-specific font-family rules to `frontend/src/style.css`:

```css
/* {Language Name} Language Support */
:lang({lang}) {
  font-family:
    "{Primary Font}",
    "{Secondary Font}",
    ...additional fonts...,
    var(--font-family-primary);
}
```

For Korean example:

```css
/* Korean Language Support */
:lang(ko) {
  font-family:
    "Noto Sans KR", "Malgun Gothic", "Apple Gothic", "Apple SD Gothic Neo",
    "Nanum Gothic", "돋움", "Dotum", "굴림", "Gulim", var(--font-family-primary);
}
```

### 5. Update Language Selector Component

Edit `frontend/src/components/common/LanguageSelector.vue`:

Add the new language to the languages array:

```typescript
const languages = [
  { value: "en", flag: "us", label: t("english") },
  { value: "fr", flag: "fr", label: t("french") },
  { value: "{lang}", flag: "{flag}", label: t("{lang}") },
];
```

## Validation

Run the translation validation script to ensure the new language is recognized:

```bash
cd frontend
npm run validate:i18n
```

Or run the validation script directly:

```bash
cd frontend
node src/locales/validate-translations.js
```

## Critical Checklist

When adding a new language, ensure you complete ALL of these steps:

### ✅ Required Steps

- [ ] Create language directory: `frontend/src/locales/{lang}/`
- [ ] Add translation files with proper JSON structure
- [ ] Update `frontend/src/locales/index.ts`:
  - Add language module import
  - Add to supportedLocales array
  - Add to messages object
- [ ] Add language name to `frontend/src/locales/common.json`
- [ ] **Update LanguageSelector.vue**: Add to languages array
- [ ] **Update LanguageFlagsSelector.vue**: Add to languages array
- [ ] Add CSS font support in `frontend/src/style.css` (if needed)
- [ ] Run translation validation: `node src/locales/validate-translations.js`

### 🚨 Critical Reminders

- **DO NOT forget the language selector components!** Both `LanguageSelector.vue` and `LanguageFlagsSelector.vue` must be updated.
- Test that the language appears in both the dropdown selector (AppHeader) and flag buttons (PublicLayout).
- Use appropriate ISO country codes for flags (e.g., "fi" for Finland, "kr" for South Korea).

## Windows CMD Syntax Reminder

When working on Windows CMD environment:

- **Delete files**: `del c:\full\path\to\file.ext` (no quotes needed for full paths)
- **Copy files**: `xcopy /E /I "source" "destination"`
- **Change directory**: `cd /d "c:\full\path\to\directory"`
- **Directory operations**: Use backslashes `\` in paths, not forward slashes `/`

## Notes

- Start with minimal translations (just the language name) and expand as needed
- Use appropriate country flags for the language selector
- Test font rendering with actual Korean/Hangul characters
- The application will fall back to English for any missing translations
- Consider right-to-left (RTL) language support if adding languages like Arabic or Hebrew

## Example: Adding Korean

Following the steps above with these values:

- Language code: `ko`
- Flag code: `kr`
- Native name: `한국어`

Results in Korean language support with South Korean flag in both the dropdown and flag button language selectors.

---

description: Architecture details and patterns discovered through debugging
alwaysApply: true

---

# Architecture Details

## Authentication & Identity

- **Identity provider**: ASP.NET Core Identity with `AppUser : IdentityUser` (string-based `Id` that stores GUIDs)
- **JWT generation**: `backend/TallyJ4.Application/Services/Auth/JwtTokenService.cs` uses `JwtSecurityTokenHandler`
- **JWT claims**: User ID is stored in the `sub` claim (`JwtRegisteredClaimNames.Sub`) with value from `AppUser.Id`
- **.NET 10 claim mapping**: `JsonWebTokenHandler` is the default validator (not `JwtSecurityTokenHandler`). It does NOT map `sub` to `ClaimTypes.NameIdentifier`. The claim stays as `"sub"`. Code that reads the user ID must check both:
  ```csharp
  User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value
  ```
- **JoinElectionUser.UserId**: `Guid` type. Parsed from `AppUser.Id` (which is a GUID-formatted string).
- **Auth config**: `backend/Program.cs` — `AddIdentity` is called first, then `AddAuthentication` overrides default scheme to `JwtBearerDefaults.AuthenticationScheme`.

## API Response Patterns

Two different response wrappers are used — be careful which one an endpoint returns:

1. **`ApiResponse<T>`** — wraps data in a `.data` property. Used by endpoints like `GetElection`, `CreateElection`, `UpdateElection`.
   - Frontend access: `response.data?.data`
2. **`PaginatedResponse<T>`** — has `.items` directly at root level (no `.data` wrapper). Used by `GetElections`.
   - Frontend access: `response.data?.items`

When writing frontend service code, check the controller's return type to determine the correct access pattern.

## Generated API Client

- **Location**: `frontend/src/api/gen/configService/`
- **Files**: `sdk.gen.ts` (call functions), `types.gen.ts` (TypeScript types), `transformers.gen.ts` (response transformers)
- **Generated from**: OpenAPI spec at `frontend/openapi/tallyj.json` (auto-generated on dev startup via `Program.cs`)
- **Response shape**: SDK calls return `{ data: <ResponseBodyType> }` — so `response.data` is the deserialized HTTP body.

## Frontend Service Layer

- **Location**: `frontend/src/services/`
- **Pattern**: Each service wraps generated SDK calls and transforms data for store consumption.
- **Stores**: Pinia stores in `frontend/src/stores/` call services and manage state.
- **Flow**: `Vue component` -> `Pinia store` -> `service` -> `generated SDK` -> backend API

## Database Seeding

- **Seeder**: `backend/EF/Data/DbSeeder.cs`
- **Idempotent**: Skips if `Elections` table already has data.
- **Test users**: `admin@tallyj.test`, `teller@tallyj.test`, `voter@tallyj.test` (password: `TestPass123!`)
- **GUID generation**: Uses MD5 hash of seed strings for deterministic GUIDs (`CreateGuid` method).
- **JoinElectionUser records**: Created per user per election. Links users to elections with roles (Owner, Teller, etc.).

## Key Backend Files

| Purpose                   | File                                                                   |
| ------------------------- | ---------------------------------------------------------------------- |
| App startup & middleware  | `backend/Program.cs`                                                   |
| JWT token creation        | `backend/TallyJ4.Application/Services/Auth/JwtTokenService.cs`         |
| Election CRUD             | `backend/Controllers/ElectionsController.cs`                           |
| Election business logic   | `backend/Services/ElectionService.cs`                                  |
| User-election link entity | `backend/TallyJ4.Domain/Entities/JoinElectionUser.cs`                  |
| Identity user model       | `backend/TallyJ4.Domain/Identity/AppUser.cs`                           |
| DB context                | `backend/TallyJ4.Domain/Context/MainDbContext.cs`                      |
| Response models           | `backend/Models/PaginatedResponse.cs`, `backend/Models/ApiResponse.cs` |

## Key Frontend Files

| Purpose                | File                                                |
| ---------------------- | --------------------------------------------------- |
| Election service       | `frontend/src/services/electionService.ts`          |
| Election store (Pinia) | `frontend/src/stores/electionStore.ts`              |
| Election list page     | `frontend/src/pages/elections/ElectionListPage.vue` |
| Auth store             | `frontend/src/stores/authStore.ts`                  |
| Router                 | `frontend/src/router/router.ts`                     |
| Generated SDK          | `frontend/src/api/gen/configService/sdk.gen.ts`     |

---

description: Repository Information Overview
alwaysApply: true

---

# TallyJ 4 Repository Information Overview

## Repository Summary

TallyJ 4 is an election management and ballot tallying system designed for Bahá’í communities. It provides a comprehensive platform for managing elections, people, ballots, votes, and results with real-time capabilities.

## Authentication System

TallyJ 4 implements a multi-tier authentication system supporting four distinct user types:

### User Types & Authentication Methods

1. **Local Users** - Email/password authentication via ASP.NET Core Identity
   - Full application access including election management
   - Can be assigned super admin privileges
   - JWT tokens stored in httpOnly cookies

2. **Google Users** - OAuth authentication via Google One Tap
   - Full application access equivalent to local users
   - Can be assigned super admin privileges
   - JWT tokens stored in httpOnly cookies

3. **Tellers** - Access code authentication (guest users)
   - Limited access to specific election management features
   - No user accounts in database, authenticated via election-specific access codes
   - Cannot be super admins
   - JWT tokens with restricted claims

4. **Voters** - OTP or Google authentication for online voting
   - Isolated authentication system using separate token storage (localStorage)
   - Access limited to online voting pages only
   - Completely separate from main authentication system
   - No integration with user management or permissions

### Authentication Architecture

- **Main System**: ASP.NET Core Identity with JWT tokens for users 1-3
- **Voter System**: Separate token-based authentication for online voting
- **Security**: HttpOnly cookies for main auth, localStorage for voter tokens
- **Authorization**: Role-based access control with election-specific permissions

## Repository Structure

- **backend/**: ASP.NET Core Web API with Entity Framework, SignalR, and JWT authentication
- **frontend/**: Vue 3 + Vite single-page application with TypeScript
- **TallyJ4.Tests/**: Unit and integration tests using xUnit
- **.github/**: GitHub Actions workflows for CI/CD
- **.vscode/**: VS Code configuration
- **.zencoder/**: Custom tooling workflows
- **.zenflow/**: Reverse engineering documentation

### Main Repository Components

- **Backend API**: Handles all business logic, database operations, and real-time notifications
- **Frontend SPA**: User interface for election management and voting
- **Test Suite**: Comprehensive testing for backend functionality

## Projects

### Backend (ASP.NET Core Web API)

**Configuration File**: backend/TallyJ4.csproj

#### Language & Runtime

**Language**: C#  
**Version**: .NET 10.0  
**Build System**: MSBuild  
**Package Manager**: NuGet

#### Dependencies

**Main Dependencies**:

- AutoMapper (12.0.1)
- FluentValidation (11.11.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (9.0.10)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.1)
- Microsoft.AspNetCore.OpenApi (9.0.10)
- Microsoft.AspNetCore.SignalR (1.2.0)
- Microsoft.EntityFrameworkCore.SqlServer (10.0.1)
- Newtonsoft.Json (13.0.4)
- Serilog (4.3.0)
- Swashbuckle.AspNetCore (9.0.6)

**Development Dependencies**:

- Microsoft.EntityFrameworkCore.Design (10.0.1)
- Microsoft.EntityFrameworkCore.Tools (10.0.1)

#### Build & Installation

```bash
cd backend
dotnet restore
dotnet build
```

#### Main Files & Resources

**Entry Point**: backend/Program.cs  
**Configuration Files**: backend/appsettings.json, backend/appsettings.Development.json  
**Database Scripts**: backend/scripts/  
**API Documentation**: Swagger UI at /swagger

#### Testing

**Framework**: xUnit  
**Test Location**: TallyJ4.Tests/  
**Naming Convention**: \*Tests.cs  
**Configuration**: TallyJ4.Tests/TallyJ4.Tests.csproj

**Run Command**:

```bash
dotnet test
```

### Frontend (Vue 3 + Vite SPA)

**Configuration File**: frontend/package.json

#### Language & Runtime

**Language**: TypeScript/JavaScript  
**Version**: Node.js (not specified, uses modern versions)  
**Build System**: Vite  
**Package Manager**: npm

#### Dependencies

**Main Dependencies**:

- Vue (3.5.22)
- Vue Router (4.6.3)
- Pinia (3.0.3)
- Element Plus (2.11.5)
- Axios (1.13.2)
- Vue I18n (11.0.0)
- Microsoft SignalR (9.0.6)

**Development Dependencies**:

- Vite (7.1.14)
- Vitest (4.0.18)
- Vue TSC (3.1.0)
- TypeScript (5.9.3)

#### Build & Installation

```bash
cd frontend
npm install
npm run build
```

#### Main Files & Resources

**Entry Point**: frontend/index.html  
**Configuration Files**: frontend/vite.config.ts, frontend/tsconfig.json  
**Source Code**: frontend/src/  
**Static Assets**: frontend/public/

#### Testing

**Framework**: Vitest  
**Test Location**: frontend/src/ (tests alongside code)  
**Naming Convention**: _.test.ts, _.spec.ts  
**Configuration**: frontend/vitest.config.ts

**Run Command**:

```bash
npm run test
```

### Tests (xUnit Test Project)

**Configuration File**: TallyJ4.Tests/TallyJ4.Tests.csproj

#### Language & Runtime

**Language**: C#  
**Version**: .NET 10.0  
**Build System**: MSBuild  
**Package Manager**: NuGet

#### Dependencies

**Main Dependencies**:

- Microsoft.NET.Test.Sdk (17.14.1)
- xUnit (2.9.3)
- Moq (4.20.70)
- Microsoft.AspNetCore.Mvc.Testing (9.0.0)
- Microsoft.EntityFrameworkCore.InMemory (10.0.1)

#### Build & Installation

```bash
dotnet restore
dotnet build
```

#### Testing

**Framework**: xUnit  
**Test Location**: TallyJ4.Tests/UnitTests/, TallyJ4.Tests/IntegrationTests/  
**Naming Convention**: \*Tests.cs  
**Configuration**: TallyJ4.Tests/TallyJ4.Tests.csproj

**Run Command**:

```bash
dotnet test
```
