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
