# Technical Specification for Jan 25 B

## Technical Context
- **Language/Framework**: C#/.NET 10, ASP.NET Core Web API
- **Database**: SQL Server with Entity Framework Core
- **Testing**: xUnit, Microsoft.AspNetCore.Mvc.Testing
- **Architecture**: Layered architecture with Services, Controllers, EF Data layer

## Implementation Approach

### 1. ElectionAccessHandler
Create an authorization handler that implements `IAuthorizationHandler` to check if the current user has access to a specific election. The handler will:
- Extract the election GUID from the route parameters
- Get the current user ID from claims
- Check if the user is joined to the election via the `JoinElectionUser` table
- Succeed the authorization requirement if access is granted

Register the handler as a scoped service in `Program.cs`.

### 2. Database Seeding for Tests
Modify the test setup to properly seed test data:
- Create a test-specific seeder that runs after database setup
- Ensure test data is consistent and isolated between tests
- Seed users, elections, and join relationships for testing access control

### 3. Database Migration Testing
Add integration tests that verify migrations work correctly:
- Test that migrations can be applied to a clean database
- Verify that seeded data is present after migrations
- Test migration rollback if needed

### 4. SQL Server LocalDB for Integration Tests
Replace the in-memory database with SQL Server LocalDB:
- Update `CustomWebApplicationFactory` to use LocalDB connection
- Add LocalDB package reference to test project
- Ensure LocalDB is available in CI/CD environment
- Configure unique database names per test run to avoid conflicts

## Source Code Structure Changes

### New Files
- `backend/Services/ElectionAccessHandler.cs` - Authorization handler
- `backend/Authorization/ElectionAccessRequirement.cs` - Authorization requirement
- `TallyJ4.Tests/IntegrationTests/MigrationTests.cs` - Migration verification tests

### Modified Files
- `backend/Program.cs` - Register ElectionAccessHandler as scoped
- `TallyJ4.Tests/IntegrationTests/CustomWebApplicationFactory.cs` - Use LocalDB, add seeding
- `TallyJ4.Tests/TallyJ4.Tests.csproj` - Add LocalDB package
- `backend/Controllers/ElectionsController.cs` - Add authorization policy

## Data Model / API / Interface Changes
- No schema changes required (uses existing `JoinElectionUser` table)
- New authorization requirement: `ElectionAccessRequirement`
- API endpoints will now enforce election-specific access control

## Verification Approach
1. **Unit Tests**: Test ElectionAccessHandler logic with mocked dependencies
2. **Integration Tests**: Test full request flow with LocalDB and seeded data
3. **Migration Tests**: Verify database schema and seeding after migrations
4. **Lint/Type Check**: Run `dotnet build` and ensure no warnings

Run tests with: `dotnet test`
Run lint: `dotnet build --no-restore`
Run typecheck: `dotnet build`