# Technical Specification: TallyJ 4 - Phase 2: Core API Development

## 1. Technical Context

### 1.1 Current State (Phase 1 Complete ✅)

**Completed Components**:
- ✅ Database schema with 16 entities + ASP.NET Identity tables
- ✅ EF Core 9.0 migrations
- ✅ Database seeding (3 users, 2 elections, 45 people, 35 ballots, 100+ votes)
- ✅ JWT authentication with ASP.NET Core Identity
- ✅ 4 basic CRUD controllers (Elections, People, Ballots, Votes)
- ✅ Serilog logging configured
- ✅ Build passing (0 errors, 5 warnings)

**Technology Stack**:
- .NET 10.0
- ASP.NET Core Web API
- Entity Framework Core 9.0.10
- SQL Server
- ASP.NET Core Identity with JWT Bearer auth
- Serilog 4.3.0

**Current Limitations**:
- ❌ Controllers expose entities directly (no DTOs)
- ❌ No input validation
- ❌ No service layer (business logic in controllers)
- ❌ Limited error handling
- ❌ No API documentation (Swagger not configured)
- ❌ No tests
- ❌ Only 4 of 12 required controllers implemented

### 1.2 Reference Documentation

**Comprehensive reverse engineering documentation** (~70,000 lines) in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`:

**Key Files for Phase 2**:
- `api/endpoints.md` - All 100+ API endpoints across 12 controllers
- `api/controllers/*.md` - Detailed documentation for each controller
- `business-logic/tally-algorithms.md` - Core election tally logic
- `security/authentication.md` - 3 authentication systems
- `security/authorization.md` - Authorization rules

**Controllers to Implement** (from reverse engineering):
1. ✅ **ElectionsController** - Basic CRUD exists, needs enhancement
2. ✅ **PeopleController** - Basic CRUD exists, needs enhancement
3. ✅ **BallotsController** - Basic CRUD exists, needs enhancement
4. ✅ **VotesController** - Basic CRUD exists, needs enhancement
5. ❌ **AccountController** - Admin authentication (OAuth, 2FA)
6. ❌ **DashboardController** - Election lists and summaries
7. ❌ **SetupController** - Election wizard, CSV imports
8. ❌ **BeforeController** - Voter registration, front desk
9. ❌ **AfterController** - Tally execution, results
10. ❌ **PublicController** - Public pages, guest/voter auth
11. ❌ **SysAdminController** - System monitoring
12. ❌ **VoteController** - Online voting flow

### 1.3 Phase 2 Scope

**Objective**: Build production-ready API foundation with proper architecture, validation, and core business logic.

**In Scope**:
1. API architecture (DTOs, services, validation, error handling)
2. Enhance 4 existing controllers with DTOs and validation
3. Implement 4 additional core controllers (Dashboard, Setup, Account, Public)
4. Global exception handling and standardized responses
5. Swagger/OpenAPI documentation
6. Basic testing infrastructure (xUnit project setup)
7. Core business logic services (election management, tally preparation)

**Out of Scope** (Future Phases):
- SignalR hubs (Phase 5)
- Tally algorithm implementation (Phase 6)
- Advanced features (CSV import, SMS/Email, OAuth) (Phase 7+)
- Frontend development (Phase 7)
- Deployment and production configuration (Phase 9)

---

## 2. Implementation Approach

### 2.1 Architecture Overview

**Layered Architecture**:

```
┌─────────────────────────────────────┐
│   Controllers (API Layer)           │  ← HTTP endpoints, DTOs, routing
├─────────────────────────────────────┤
│   Services (Business Logic Layer)   │  ← Business rules, workflows
├─────────────────────────────────────┤
│   EF Core DbContext (Data Layer)    │  ← Database access, entities
└─────────────────────────────────────┘
```

**Key Architectural Decisions**:

1. **DTOs (Data Transfer Objects)**: 
   - Separate API contracts from database entities
   - Location: `backend/DTOs/` with subdirectories by feature
   - Naming: `{Entity}Dto`, `Create{Entity}Dto`, `Update{Entity}Dto`

2. **Services**:
   - Encapsulate business logic outside controllers
   - Location: `backend/Services/`
   - Interface-based (`I{Service}`, `{Service}`)
   - Dependency injection via `Program.cs`

3. **Validation**:
   - FluentValidation library for input validation
   - Validators in `backend/Validators/`
   - Auto-registration with DI

4. **Error Handling**:
   - Global exception handler middleware
   - Standardized `ApiResponse<T>` wrapper
   - Problem Details (RFC 7807) for errors

5. **Mapping**:
   - AutoMapper for entity ↔ DTO conversions
   - Profiles in `backend/Mappings/`

### 2.2 Project Structure Changes

**New Directories**:
```
backend/
├── Controllers/          # Existing + 4 new controllers
├── DTOs/                 # NEW: Data Transfer Objects
│   ├── Elections/
│   ├── People/
│   ├── Ballots/
│   ├── Votes/
│   ├── Dashboard/
│   ├── Setup/
│   ├── Account/
│   └── Common/
├── Services/             # NEW: Business logic services
│   ├── IElectionService.cs
│   ├── ElectionService.cs
│   ├── IPeopleService.cs
│   ├── PeopleService.cs
│   ├── IBallotService.cs
│   ├── BallotService.cs
│   ├── IVoteService.cs
│   ├── VoteService.cs
│   └── IDashboardService.cs
│   └── DashboardService.cs
├── Validators/           # NEW: FluentValidation validators
│   ├── CreateElectionDtoValidator.cs
│   ├── UpdateElectionDtoValidator.cs
│   ├── CreatePersonDtoValidator.cs
│   └── ...
├── Mappings/             # NEW: AutoMapper profiles
│   ├── ElectionProfile.cs
│   ├── PersonProfile.cs
│   ├── BallotProfile.cs
│   └── VoteProfile.cs
├── Middleware/           # NEW: Custom middleware
│   ├── ExceptionHandlerMiddleware.cs
│   └── CorrelationIdMiddleware.cs
├── Models/               # NEW: API response models
│   ├── ApiResponse.cs
│   └── PaginatedResponse.cs
├── EF/                   # Existing
├── Helpers/              # Existing
└── Program.cs            # Modified: register new services
```

### 2.3 NuGet Packages to Add

```xml
<!-- Validation -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />

<!-- Mapping -->
<PackageReference Include="AutoMapper" Version="13.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="13.0.1" />

<!-- API Documentation -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.6" /> <!-- Already included -->
<PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="9.0.6" />

<!-- Testing (new test project) -->
<PackageReference Include="xunit" Version="2.6.6" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
<PackageReference Include="Moq" Version="4.20.70" />
```

---

## 3. Detailed Implementation Plan

### 3.1 Phase 2.1: API Infrastructure Setup

**Objective**: Set up architectural foundation (DTOs, validation, error handling, Swagger)

**Tasks**:
1. Install NuGet packages (FluentValidation, AutoMapper)
2. Create directory structure (`DTOs/`, `Services/`, `Validators/`, `Mappings/`, `Middleware/`, `Models/`)
3. Create `ApiResponse<T>` and `PaginatedResponse<T>` models
4. Implement global exception handler middleware
5. Configure Swagger with authentication support
6. Register services in `Program.cs`

**Files to Create**:
- `Models/ApiResponse.cs`
- `Models/PaginatedResponse.cs`
- `Middleware/ExceptionHandlerMiddleware.cs`
- `Program.cs` updates

**Verification**:
- Build succeeds
- Swagger UI accessible at `/swagger`
- Global exception handler catches errors and returns Problem Details

---

### 3.2 Phase 2.2: Elections API Enhancement

**Objective**: Refactor ElectionsController with DTOs, validation, and service layer

**Tasks**:
1. Create DTOs: `ElectionDto`, `CreateElectionDto`, `UpdateElectionDto`, `ElectionSummaryDto`
2. Create `IElectionService` and `ElectionService`
3. Create AutoMapper profile: `ElectionProfile.cs`
4. Create validators: `CreateElectionDtoValidator`, `UpdateElectionDtoValidator`
5. Refactor `ElectionsController` to use service and DTOs
6. Add pagination to `GET /api/elections`
7. Add filtering (by status, date range)

**DTOs**:
```csharp
// ElectionDto - Full election details
public class ElectionDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; }
    public DateTime? DateOfElection { get; set; }
    public string ElectionType { get; set; }
    public int NumberToElect { get; set; }
    public string TallyStatus { get; set; }
    public int VoterCount { get; set; }
    public int BallotCount { get; set; }
}

// CreateElectionDto - Input for creating election
public class CreateElectionDto
{
    public string Name { get; set; }
    public DateTime? DateOfElection { get; set; }
    public string ElectionType { get; set; }
    public int NumberToElect { get; set; }
    public string Reason { get; set; }
}

// UpdateElectionDto - Input for updating election
public class UpdateElectionDto
{
    public string Name { get; set; }
    public DateTime? DateOfElection { get; set; }
    public int NumberToElect { get; set; }
    public string TallyStatus { get; set; }
}

// ElectionSummaryDto - Lightweight for lists
public class ElectionSummaryDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; }
    public DateTime? DateOfElection { get; set; }
    public string TallyStatus { get; set; }
}
```

**Endpoints** (enhanced):
- `GET /api/elections` - List all elections (paginated, filtered)
- `GET /api/elections/{guid}` - Get election by GUID
- `POST /api/elections` - Create election (with validation)
- `PUT /api/elections/{guid}` - Update election (with validation)
- `DELETE /api/elections/{guid}` - Delete election
- `GET /api/elections/{guid}/summary` - Get election summary with counts

**Verification**:
- All endpoints return DTOs (not entities)
- Validation errors return 400 with details
- Invalid GUIDs return 404
- Unauthorized requests return 401
- Pagination works correctly

---

### 3.3 Phase 2.3: People API Enhancement

**Objective**: Refactor PeopleController with DTOs, validation, and service layer

**Tasks**:
1. Create DTOs: `PersonDto`, `CreatePersonDto`, `UpdatePersonDto`
2. Create `IPeopleService` and `PeopleService`
3. Create AutoMapper profile: `PersonProfile.cs`
4. Create validators: `CreatePersonDtoValidator`, `UpdatePersonDtoValidator`
5. Refactor `PeopleController` to use service and DTOs
6. Add search/filtering (by name, eligibility)
7. Add pagination

**DTOs**:
```csharp
public class PersonDto
{
    public Guid PersonGuid { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; } // Computed
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool CanReceiveVotes { get; set; }
    public bool CanVote { get; set; }
    public string Area { get; set; }
    public int VoteCount { get; set; } // From Results
}

public class CreatePersonDto
{
    public Guid ElectionGuid { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool CanReceiveVotes { get; set; }
    public bool CanVote { get; set; }
    public string Area { get; set; }
}

public class UpdatePersonDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool CanReceiveVotes { get; set; }
    public bool CanVote { get; set; }
}
```

**Endpoints** (enhanced):
- `GET /api/people/election/{electionGuid}` - List people by election (paginated, searchable)
- `GET /api/people/{guid}` - Get person by GUID
- `POST /api/people` - Create person (with validation)
- `PUT /api/people/{guid}` - Update person (with validation)
- `DELETE /api/people/{guid}` - Delete person
- `GET /api/people/election/{electionGuid}/search?q={query}` - Search people

**Validation Rules**:
- FirstName and LastName required
- Email format validation
- Phone format validation (optional)
- Email unique within election (filtered)
- Phone unique within election (filtered)

**Verification**:
- DTOs returned instead of entities
- Validation works for email/phone uniqueness
- Search returns relevant results
- Pagination works

---

### 3.4 Phase 2.4: Ballots API Enhancement

**Objective**: Refactor BallotsController with DTOs, validation, and service layer

**Tasks**:
1. Create DTOs: `BallotDto`, `CreateBallotDto`, `UpdateBallotDto`
2. Create `IBallotService` and `BallotService`
3. Create AutoMapper profile: `BallotProfile.cs`
4. Create validators
5. Refactor `BallotsController`
6. Add vote count aggregation

**DTOs**:
```csharp
public class BallotDto
{
    public Guid BallotGuid { get; set; }
    public string BallotCode { get; set; } // Computed (e.g., "A1", "B5")
    public Guid LocationGuid { get; set; }
    public string LocationName { get; set; }
    public int BallotNumAtComputer { get; set; }
    public string ComputerCode { get; set; }
    public string StatusCode { get; set; }
    public int VoteCount { get; set; }
    public List<VoteDto> Votes { get; set; }
}

public class CreateBallotDto
{
    public Guid LocationGuid { get; set; }
    public string ComputerCode { get; set; }
}

public class UpdateBallotDto
{
    public string StatusCode { get; set; }
}
```

**Endpoints** (enhanced):
- `GET /api/ballots/election/{electionGuid}` - List ballots by election
- `GET /api/ballots/{guid}` - Get ballot by GUID (includes votes)
- `POST /api/ballots` - Create ballot
- `PUT /api/ballots/{guid}` - Update ballot status
- `DELETE /api/ballots/{guid}` - Delete ballot

**Verification**:
- BallotCode computed correctly
- Votes included in response
- StatusCode validation works

---

### 3.5 Phase 2.5: Votes API Enhancement

**Objective**: Refactor VotesController with DTOs, validation, and service layer

**Tasks**:
1. Create DTOs: `VoteDto`, `CreateVoteDto`
2. Create `IVoteService` and `VoteService`
3. Create AutoMapper profile: `VoteProfile.cs`
4. Create validators
5. Refactor `VotesController`

**DTOs**:
```csharp
public class VoteDto
{
    public int RowId { get; set; }
    public Guid BallotGuid { get; set; }
    public Guid PersonGuid { get; set; }
    public string PersonFullName { get; set; }
    public int PositionOnBallot { get; set; }
    public string StatusCode { get; set; }
}

public class CreateVoteDto
{
    public Guid BallotGuid { get; set; }
    public Guid PersonGuid { get; set; }
    public int PositionOnBallot { get; set; }
}
```

**Endpoints** (enhanced):
- `GET /api/votes/ballot/{ballotGuid}` - Get votes for ballot
- `GET /api/votes/election/{electionGuid}` - Get all votes for election
- `POST /api/votes` - Create vote
- `PUT /api/votes/{id}` - Update vote
- `DELETE /api/votes/{id}` - Delete vote

**Validation Rules**:
- BallotGuid must exist
- PersonGuid must exist and be eligible (CanReceiveVotes = true)
- PositionOnBallot must be 1-{NumberToElect}
- No duplicate votes on same ballot for same person

**Verification**:
- Validation prevents invalid votes
- PersonFullName populated from Person entity

---

### 3.6 Phase 2.6: Dashboard Controller (New)

**Objective**: Implement dashboard/summary endpoints for election lists and statistics

**Tasks**:
1. Create `DashboardController`
2. Create DTOs: `DashboardSummaryDto`, `ElectionCardDto`
3. Create `IDashboardService` and `DashboardService`
4. Implement summary calculations (vote counts, ballot counts, tally status)

**DTOs**:
```csharp
public class DashboardSummaryDto
{
    public int ActiveElectionCount { get; set; }
    public int CompletedElectionCount { get; set; }
    public List<ElectionCardDto> RecentElections { get; set; }
}

public class ElectionCardDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; }
    public DateTime? DateOfElection { get; set; }
    public string TallyStatus { get; set; }
    public int VoterCount { get; set; }
    public int BallotCount { get; set; }
    public int VoteCount { get; set; }
    public double PercentComplete { get; set; }
}
```

**Endpoints**:
- `GET /api/dashboard/summary` - Overall dashboard summary
- `GET /api/dashboard/elections` - Recent elections with stats

**Reference**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/api/controllers/DashboardController.md`

**Verification**:
- Counts accurate
- Recent elections sorted by date
- Percent complete calculated correctly

---

### 3.7 Phase 2.7: Setup Controller (New - Partial)

**Objective**: Implement election creation wizard endpoints (basic version, no CSV import yet)

**Tasks**:
1. Create `SetupController`
2. Create DTOs: `ElectionWizardDto`, `ElectionStep1Dto`, `ElectionStep2Dto`
3. Create `ISetupService` and `SetupService`
4. Implement multi-step election creation workflow

**DTOs**:
```csharp
public class ElectionStep1Dto
{
    public string Name { get; set; }
    public DateTime? DateOfElection { get; set; }
    public string Reason { get; set; }
}

public class ElectionStep2Dto
{
    public Guid ElectionGuid { get; set; }
    public int NumberToElect { get; set; }
    public string ElectionType { get; set; }
    public string ElectionMode { get; set; }
}
```

**Endpoints**:
- `POST /api/setup/election/step1` - Create election (step 1)
- `PUT /api/setup/election/{guid}/step2` - Configure election (step 2)
- `GET /api/setup/election/{guid}/status` - Get setup progress

**Reference**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/api/controllers/SetupController.md`

**Verification**:
- Multi-step workflow creates valid election
- Progress tracked correctly

---

### 3.8 Phase 2.8: Account Controller (New - Partial)

**Objective**: Extend Identity authentication with custom admin endpoints

**Tasks**:
1. Create `AccountController`
2. Create DTOs: `LoginResponseDto`, `UserProfileDto`
3. Implement profile management endpoints
4. Add custom login/logout (wrapping Identity endpoints)

**DTOs**:
```csharp
public class LoginResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; }
}

public class UserProfileDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
}
```

**Endpoints**:
- `GET /api/account/profile` - Get current user profile
- `PUT /api/account/profile` - Update profile
- `POST /api/account/change-password` - Change password

**Note**: OAuth and 2FA deferred to Phase 7

**Reference**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/api/controllers/AccountController.md`

**Verification**:
- Profile endpoints require authentication
- Change password works correctly

---

### 3.9 Phase 2.9: Public Controller (New - Partial)

**Objective**: Implement public (unauthenticated) endpoints for home page and status

**Tasks**:
1. Create `PublicController`
2. Create DTOs: `PublicHomeDto`, `ElectionStatusDto`
3. Implement public endpoints (no auth required)

**DTOs**:
```csharp
public class PublicHomeDto
{
    public string ApplicationName { get; set; }
    public string Version { get; set; }
    public int ActiveElectionCount { get; set; }
}

public class ElectionStatusDto
{
    public string Name { get; set; }
    public string Status { get; set; }
    public bool VotingOpen { get; set; }
}
```

**Endpoints**:
- `GET /api/public/home` - Public home page data
- `GET /api/public/elections/{guid}/status` - Election status (public)

**Note**: Guest teller and voter authentication deferred to Phase 7

**Reference**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/api/controllers/PublicController.md`

**Verification**:
- Endpoints accessible without authentication
- No sensitive data exposed

---

### 3.10 Phase 2.10: Testing Infrastructure Setup

**Objective**: Create test project and basic integration tests

**Tasks**:
1. Create `TallyJ4.Tests` project (xUnit)
2. Set up `WebApplicationFactory<Program>` for integration tests
3. Create test fixtures and helpers
4. Write integration tests for Elections API
5. Write unit tests for ElectionService

**Test Project Structure**:
```
TallyJ4.Tests/
├── Integration/
│   ├── ElectionsControllerTests.cs
│   ├── PeopleControllerTests.cs
│   └── TestWebApplicationFactory.cs
├── Unit/
│   ├── Services/
│   │   ├── ElectionServiceTests.cs
│   │   └── PeopleServiceTests.cs
│   └── Validators/
│       ├── CreateElectionDtoValidatorTests.cs
│       └── CreatePersonDtoValidatorTests.cs
└── Helpers/
    └── TestDataBuilder.cs
```

**Sample Tests**:
- `ElectionsControllerTests.GetElections_ReturnsOk`
- `ElectionsControllerTests.CreateElection_WithValidData_ReturnsCreated`
- `ElectionsControllerTests.CreateElection_WithInvalidData_ReturnsBadRequest`
- `ElectionServiceTests.CreateElection_WithValidDto_CreatesElection`

**Verification**:
- Tests run successfully (`dotnet test`)
- Integration tests use in-memory database
- Test coverage > 60% for new code

---

## 4. API Response Standardization

### 4.1 ApiResponse<T> Wrapper

**Purpose**: Consistent response format across all endpoints

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

**Usage**:
```csharp
[HttpGet]
public async Task<ActionResult<ApiResponse<List<ElectionDto>>>> GetElections()
{
    var elections = await _electionService.GetAllAsync();
    return Ok(ApiResponse<List<ElectionDto>>.SuccessResponse(elections));
}
```

### 4.2 Pagination Response

```csharp
public class PaginatedResponse<T> : ApiResponse<List<T>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
```

---

## 5. Error Handling Strategy

### 5.1 Global Exception Handler Middleware

```csharp
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        logger.LogError(exception, "An unhandled exception occurred");

        var response = exception switch
        {
            ValidationException validationEx => new
            {
                status = StatusCodes.Status400BadRequest,
                title = "Validation Error",
                errors = validationEx.Errors
            },
            NotFoundException notFoundEx => new
            {
                status = StatusCodes.Status404NotFound,
                title = "Resource Not Found",
                detail = notFoundEx.Message
            },
            UnauthorizedAccessException => new
            {
                status = StatusCodes.Status401Unauthorized,
                title = "Unauthorized"
            },
            _ => new
            {
                status = StatusCodes.Status500InternalServerError,
                title = "Internal Server Error",
                detail = "An unexpected error occurred"
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.status;
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

### 5.2 Custom Exceptions

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public List<string> Errors { get; }
    public ValidationException(List<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}
```

---

## 6. Swagger Configuration

### 6.1 Enhanced Swagger Setup

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TallyJ 4 API",
        Version = "v1",
        Description = "Election management and ballot tallying system API"
    });

    // JWT Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Enable annotations
    options.EnableAnnotations();
});
```

---

## 7. Verification Approach

### 7.1 Build & Lint

```bash
# Clean build
dotnet clean backend/TallyJ4.csproj
dotnet restore backend/TallyJ4.csproj
dotnet build backend/TallyJ4.csproj --configuration Release

# Expected: 0 errors, minimal warnings
```

### 7.2 Manual API Testing

**Test Scenarios**:
1. Get all elections (paginated)
2. Create election with valid data
3. Create election with invalid data (verify validation)
4. Get single election by GUID
5. Update election
6. Delete election
7. Repeat for People, Ballots, Votes
8. Test Dashboard summary
9. Test Setup wizard
10. Test Account profile endpoints

**Tools**: 
- Swagger UI (`/swagger`)
- `TallyJ4.http` file (update with new endpoints)
- Postman/Insomnia (optional)

### 7.3 Automated Testing

```bash
# Run all tests
dotnet test

# Expected: All tests pass, >60% coverage
```

### 7.4 Database Verification

```sql
-- Verify seeded data still intact
SELECT COUNT(*) FROM Election;  -- Should be 2
SELECT COUNT(*) FROM Person;    -- Should be 45
SELECT COUNT(*) FROM Ballot;    -- Should be 35
SELECT COUNT(*) FROM Vote;      -- Should be 100+

-- Verify API operations created correct data
SELECT * FROM Election WHERE Name = 'Test Election from API';
```

---

## 8. Success Criteria

Phase 2 is complete when:

1. ✅ All NuGet packages installed and configured
2. ✅ API infrastructure in place (DTOs, services, validation, error handling)
3. ✅ Swagger UI working with JWT authentication
4. ✅ 4 existing controllers refactored (Elections, People, Ballots, Votes)
5. ✅ 4 new controllers implemented (Dashboard, Setup, Account, Public)
6. ✅ AutoMapper configured for all DTOs
7. ✅ FluentValidation working for all create/update operations
8. ✅ Global exception handler catches and logs errors
9. ✅ Test project created with >10 tests passing
10. ✅ Build succeeds with 0 errors
11. ✅ All API endpoints documented in Swagger
12. ✅ Manual API testing passes all scenarios

**Estimated Effort**: 5-7 days

**Deliverables**:
- 8 controllers with full DTO/service/validation support
- 30+ DTOs
- 8+ service interfaces and implementations
- 15+ validators
- Global error handling
- Swagger documentation
- Test project with integration and unit tests
- Updated README.md with API usage examples

---

## 9. Known Risks & Mitigations

**Risk 1**: Validation rules too strict, blocking legitimate data
- **Mitigation**: Start permissive, tighten based on testing

**Risk 2**: AutoMapper configuration errors causing runtime exceptions
- **Mitigation**: Unit test all mapping profiles

**Risk 3**: Global exception handler hiding important error details
- **Mitigation**: Log full exceptions, return sanitized responses

**Risk 4**: DTOs diverging from entities, causing mapping issues
- **Mitigation**: Keep DTOs close to entity structure initially

**Risk 5**: Integration tests failing due to database state
- **Mitigation**: Use in-memory database or transaction rollback per test

---

## 10. Out of Scope (Deferred to Future Phases)

- SignalR hubs (Phase 5)
- Tally algorithm implementation (Phase 6)
- CSV import functionality (Phase 7)
- SMS/Email integration (Phase 7)
- OAuth authentication (Phase 7)
- Guest teller authentication (Phase 7)
- Voter passwordless authentication (Phase 7)
- Results calculation endpoints (Phase 6)
- Frontend development (Phase 7)
- Deployment configuration (Phase 9)
