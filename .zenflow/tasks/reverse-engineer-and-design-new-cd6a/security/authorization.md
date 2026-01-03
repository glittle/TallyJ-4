# TallyJ Authorization Rules & Security Model

## Overview

TallyJ implements a **custom authorization model** using custom authorization attributes and FluentSecurity middleware. This document maps all authorization attributes, policies, and role definitions for migration to .NET Core's policy-based authorization.

**Key Characteristics**:
- **3 distinct user types**: Admin, Guest Teller, Voter
- **Custom authorization attributes**: Not standard `[Authorize(Roles="...")]`
- **Session-based authorization**: Checks `UserSession` state in addition to claims
- **FluentSecurity integration**: Declarative security configuration
- **No role-based authorization**: Uses custom attributes instead of ASP.NET roles

---

## Custom Authorization Attributes

### 1. `[ForAuthenticatedTeller]`

**Purpose**: Restrict access to authenticated admin users only (excludes guest tellers and voters)

**Implementation**:
```csharp
public class ForAuthenticatedTellerAttribute : AuthorizeAttribute
{
    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        return UserSession.IsKnownTeller;
    }
}
```

**Authorization Logic**:
- Checks `UserSession.IsKnownTeller` property
- `IsKnownTeller` is set from the `IsKnownTeller` claim in the user's identity
- Only admin users (System 1) have this claim set to "true"
- Guest tellers do NOT have this claim (they have `IsGuestTeller` instead)

**When to Use**:
- Operations that require admin privileges
- Election creation, deletion
- Sensitive configuration changes
- System-wide administrative tasks

**Usage Examples** (from controllers):
- `ElectionsController.CreateElection`
- `ElectionsController.DeleteElection`
- `DashboardController.ReloadElections`
- `AfterController.Analyze`
- `SetupController.Index`
- `SetupController.ImportCsv`

**Controllers Using This Attribute**:
- DashboardController (some actions)
- ElectionsController (most actions)
- AfterController (Analyze, SaveTieCounts)
- SetupController (wizard, import, locations)
- SysAdminController (all actions)

---

### 2. `[AllowTellersInActiveElection]`

**Purpose**: Restrict access to any teller (admin or guest) who has selected an active election

**Implementation**:
```csharp
public class AllowTellersInActiveElectionAttribute : AuthorizeAttribute
{
    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        // Check 1: Must be a teller (admin or guest)
        var isTeller = UserSession.IsKnownTeller || UserSession.IsGuestTeller;
        
        // Check 2: Must have an active election selected
        var hasActiveElection = UserSession.CurrentElectionGuid != Guid.Empty;
        
        return isTeller && hasActiveElection;
    }
}
```

**Authorization Logic**:
- Checks if user is either an admin teller OR a guest teller
- Checks if `UserSession.CurrentElectionGuid` is set (not empty)
- Election is selected via `ElectionsController.SelectElection`
- Session state is maintained via StateServer

**Session Dependencies**:
- `UserSession.CurrentElectionGuid` - Set when election is selected
- `UserSession.IsKnownTeller` - Admin user flag
- `UserSession.IsGuestTeller` - Guest teller flag

**When to Use**:
- Most election-specific operations
- Front desk, ballot entry, monitoring
- Reports, roll call
- Any operation that requires an active election context

**Usage Examples** (from controllers):
- `DashboardController.Index`
- `BeforeController.FrontDesk`
- `BeforeController.RollCall`
- `BallotsController` (all actions)
- `AfterController.Reports`
- `AfterController.Monitor`
- `PeopleController.GetAll`
- `SetupController.People`

**Controllers Using This Attribute**:
- DashboardController (most actions)
- BeforeController (all actions)
- BallotsController (all actions)
- AfterController (most actions)
- PeopleController (teller-specific actions)
- SetupController (people management)

---

### 3. `[AllowVoter]`

**Purpose**: Restrict access to authenticated voters only

**Implementation**:
```csharp
public class AllowVoterAttribute : AuthorizeAttribute
{
    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        return UserSession.IsVoter;
    }
}
```

**Authorization Logic**:
- Checks `UserSession.IsVoter` property
- `IsVoter` is set from the `IsVoter` claim in the user's identity
- Only users authenticated via System 3 (voter authentication) have this claim
- Voters are authenticated via email/SMS one-time codes

**Session Dependencies**:
- `UserSession.VoterId` - Email or phone number
- `UserSession.VoterIdType` - "email", "sms", or "kiosk"
- `UserSession.IsVoter` - Voter flag (from claim)

**When to Use**:
- Online voting ballot pages
- Voter-specific election lists
- Vote submission endpoints
- Voter login history

**Usage Examples** (from controllers):
- `VoteController.Index`
- `VoteController.JoinElection`
- `VoteController.SavePool`
- `VoteController.LockPool`
- `PeopleController.GetForVoter`

**Controllers Using This Attribute**:
- VoteController (all actions)
- PeopleController (GetForVoter)

---

### 4. `[AllowAnonymous]`

**Purpose**: Allow access without any authentication (standard ASP.NET attribute)

**Implementation**: Built-in ASP.NET attribute

**When to Use**:
- Public pages (home, about, contact)
- Login pages (admin, teller join, voter authentication)
- Public election listings
- Heartbeat/health check endpoints

**Usage Examples** (from controllers):
- `PublicController.Index`
- `PublicController.About`
- `PublicController.TellerJoin`
- `PublicController.IssueCode`
- `PublicController.LoginWithCode`
- `AccountController.LogOn`
- `AccountController.Register`

**Controllers Using This Attribute**:
- PublicController (most actions)
- AccountController (login/register)

---

## Role Definitions

TallyJ does **NOT use ASP.NET Roles** (no AspNetRoles table usage). Instead, it uses **claims-based identity** with custom user types.

### User Type 1: Admin

**Authentication System**: System 1 (Username + Password)

**Identity Claims**:
| Claim Type | Example Value | Purpose |
|------------|---------------|---------|
| UserName | "admin123" | Display name, audit logs |
| UniqueID | "A:admin123" | Unique identifier (prefix "A" = Admin) |
| IsKnownTeller | "true" | Admin privilege flag |
| IsSysAdmin | "true" | System administrator flag (optional) |

**Authorization Attributes**:
- ✅ `[ForAuthenticatedTeller]` - YES
- ✅ `[AllowTellersInActiveElection]` - YES (if election selected)
- ❌ `[AllowVoter]` - NO

**Session State**:
```csharp
UserSession.IsKnownTeller = true
UserSession.IsGuestTeller = false
UserSession.IsVoter = false
UserSession.AdminAccountEmail = "admin@example.com"
UserSession.CurrentElectionGuid = Guid (if election selected)
```

**Capabilities**:
- Create/delete elections
- Configure election settings
- Import voters from CSV
- Import ballots
- Send notifications
- Manage locations
- Run tallies
- View all reports
- System administration (if IsSysAdmin)

---

### User Type 2: Guest Teller

**Authentication System**: System 2 (Access Code Only)

**Identity Claims**:
| Claim Type | Example Value | Purpose |
|------------|---------------|---------|
| UserName | "T:a1b2c3d4e" | Fake username from session ID |
| UniqueID | "T:a1b2c3d4e" | Unique identifier (prefix "T" = Teller) |
| IsGuestTeller | "true" | Guest teller flag |

**Authorization Attributes**:
- ❌ `[ForAuthenticatedTeller]` - NO
- ✅ `[AllowTellersInActiveElection]` - YES
- ❌ `[AllowVoter]` - NO

**Session State**:
```csharp
UserSession.IsKnownTeller = false
UserSession.IsGuestTeller = true
UserSession.IsVoter = false
UserSession.CurrentElectionGuid = Guid (set automatically on join)
UserSession.CurrentComputer.ComputerGuid = Guid
```

**Capabilities** (Limited compared to Admin):
- ✅ View dashboard
- ✅ Front desk voter registration
- ✅ Ballot entry
- ✅ Roll call display
- ✅ View reports
- ✅ Monitor progress
- ✅ Edit people (if allowed by admin)
- ❌ Create/delete elections
- ❌ Import CSV
- ❌ Send notifications
- ❌ Run tallies
- ❌ Manage locations

**Access Code Management**:
- Access code is stored in `Election.ElectionPasscode`
- Admin can regenerate access code
- Guest teller never sees the access code after initial entry
- Access code is specific to one election

---

### User Type 3: Voter

**Authentication System**: System 3 (Email/SMS One-Time Code)

**Identity Claims**:
| Claim Type | Example Value | Purpose |
|------------|---------------|---------|
| VoterId | "voter@example.com" | Email or phone number |
| VoterIdType | "email" | Authentication method |
| IsVoter | "true" | Voter flag |
| UniqueID | "V:voter@example.com" | Unique identifier (prefix "V" = Voter) |
| Source | "email" | Login source (email, sms, google-oauth2, facebook) |

**Authorization Attributes**:
- ❌ `[ForAuthenticatedTeller]` - NO
- ❌ `[AllowTellersInActiveElection]` - NO
- ✅ `[AllowVoter]` - YES

**Session State**:
```csharp
UserSession.IsKnownTeller = false
UserSession.IsGuestTeller = false
UserSession.IsVoter = true
UserSession.VoterId = "voter@example.com"
UserSession.VoterIdType = VoterIdTypeEnum.Email
UserSession.VoterLoginSource = "email"
```

**Capabilities**:
- ✅ View eligible elections (where Person.Email or Person.Phone matches VoterId)
- ✅ Join election (if eligible)
- ✅ View candidates
- ✅ Submit ballot
- ✅ Save draft ballot
- ✅ View login history
- ❌ Access any teller functions

**Voter Matching Logic**:
```csharp
// Find voter's Person record in election by email/phone
var personQuery = Db.Person.Where(p => p.ElectionGuid == electionGuid);

if (UserSession.VoterIdType == VoterIdTypeEnum.Email)
{
    personQuery = personQuery.Where(p => p.Email == voterId);
}
else if (UserSession.VoterIdType == VoterIdTypeEnum.Phone)
{
    personQuery = personQuery.Where(p => p.Phone == voterId);
}
else if (UserSession.VoterIdType == VoterIdTypeEnum.Kiosk)
{
    personQuery = personQuery.Where(p => p.KioskCode == voterId);
}
```

**Voter Authentication Types**:
- Email (one-time code)
- SMS (one-time code)
- Kiosk (pre-generated code printed on paper)
- Google OAuth2 (email verified by Google)
- Facebook OAuth (email verified by Facebook)

---

## FluentSecurity Configuration

**Package**: FluentSecurity 2.1.0

**Configuration Location**: `App_Start/SecurityConfig.cs` or `Global.asax.cs`

**Purpose**: Declarative security policy configuration using fluent API

**Expected Configuration Pattern** (based on standard FluentSecurity usage):

```csharp
public static class SecurityConfiguration
{
    public static void ConfigureSecurity(ISecurityConfiguration configuration)
    {
        configuration.GetAuthenticationStatusFrom(() => 
            HttpContext.Current.User.Identity.IsAuthenticated);

        // Default policy: require authentication
        configuration.ForAllControllers()
            .DenyAnonymousAccess();

        // Public pages
        configuration.For<PublicController>()
            .Ignore();

        configuration.For<AccountController>()
            .Ignore();

        // Admin-only controllers
        configuration.For<SysAdminController>()
            .RequireRole(() => UserSession.IsSysAdmin);

        // Teller controllers
        configuration.For<DashboardController>()
            .RequireRole(() => UserSession.IsKnownTeller || UserSession.IsGuestTeller);

        configuration.For<BallotsController>()
            .RequireRole(() => UserSession.IsKnownTeller || UserSession.IsGuestTeller)
            .RequireRole(() => UserSession.CurrentElectionGuid != Guid.Empty);

        // Voter controllers
        configuration.For<VoteController>()
            .RequireRole(() => UserSession.IsVoter);
    }
}
```

**Note**: The actual FluentSecurity configuration code was not found in the documentation, so the above is an expected pattern based on standard FluentSecurity usage. During migration, verify the actual implementation.

---

## Session State Management

### StateServer Configuration

**Technology**: ASP.NET StateServer (out-of-process session state)

**Configuration** (Web.config):
```xml
<sessionState 
    mode="StateServer" 
    stateConnectionString="tcpip=127.0.0.1:42424" 
    timeout="360">
</sessionState>
```

**Session Timeout**: 6 hours (360 minutes)

**Session Variables Used**:
- `CurrentElectionGuid` - Selected election
- `CurrentLocationGuid` - Selected location
- `CurrentComputer.ComputerGuid` - Computer/device identifier
- `UserSession` properties (cached from claims)

---

## Authorization Decision Flow

### Admin User Flow

```
1. User submits username + password
2. Membership.ValidateUser() checks credentials
3. Claims created: UserName, UniqueID, IsKnownTeller, IsSysAdmin
4. OWIN cookie issued
5. UserSession.IsKnownTeller = true

Authorization Checks:
- [ForAuthenticatedTeller] → Check IsKnownTeller claim → ALLOW
- [AllowTellersInActiveElection] → Check IsKnownTeller + CurrentElectionGuid → ALLOW (if election selected)
- [AllowVoter] → Check IsVoter claim → DENY
```

### Guest Teller User Flow

```
1. User selects election and enters access code
2. Election.ElectionPasscode is validated
3. Claims created: UserName (fake), UniqueID, IsGuestTeller
4. OWIN cookie issued
5. UserSession.IsGuestTeller = true
6. UserSession.CurrentElectionGuid = electionGuid (automatically set)

Authorization Checks:
- [ForAuthenticatedTeller] → Check IsKnownTeller claim → DENY (guest tellers don't have this)
- [AllowTellersInActiveElection] → Check IsGuestTeller + CurrentElectionGuid → ALLOW
- [AllowVoter] → Check IsVoter claim → DENY
```

### Voter User Flow

```
1. User requests one-time code (email/SMS)
2. Code sent and stored in OnlineVoter table
3. User enters code
4. Code is validated
5. Claims created: VoterId, VoterIdType, IsVoter, UniqueID, Source
6. OWIN cookie issued
7. UserSession.IsVoter = true

Authorization Checks:
- [ForAuthenticatedTeller] → Check IsKnownTeller claim → DENY
- [AllowTellersInActiveElection] → Check IsKnownTeller OR IsGuestTeller → DENY
- [AllowVoter] → Check IsVoter claim → ALLOW
```

---

## Authorization Matrix by Controller

| Controller | Anonymous | Admin | Guest Teller | Voter |
|------------|-----------|-------|--------------|-------|
| PublicController | ✅ Most actions | ✅ All | ✅ All | ✅ All |
| AccountController | ✅ Login/Register | ✅ All | ❌ | ❌ |
| DashboardController | ❌ | ✅ All | ✅ Most | ❌ |
| ElectionsController | ❌ | ✅ All | ✅ Limited | ❌ |
| BeforeController | ❌ | ✅ All | ✅ All | ❌ |
| BallotsController | ❌ | ✅ All | ✅ All | ❌ |
| AfterController | ❌ | ✅ All | ✅ Reports/Monitor | ❌ |
| SetupController | ❌ | ✅ All | ✅ People only | ❌ |
| PeopleController | ❌ | ✅ All | ✅ All | ✅ GetForVoter only |
| VoteController | ❌ | ❌ | ❌ | ✅ All |
| SysAdminController | ❌ | ✅ If IsSysAdmin | ❌ | ❌ |
| Manage2Controller | ❌ | ❌ DISABLED | ❌ DISABLED | ❌ DISABLED |

---

## .NET Core Migration Recommendations

### Policy-Based Authorization

Replace custom authorization attributes with ASP.NET Core policies:

```csharp
// Startup.cs or Program.cs
services.AddAuthorization(options =>
{
    // Policy 1: Authenticated Admin Only
    options.AddPolicy("ForAuthenticatedTeller", policy =>
        policy.RequireClaim("IsKnownTeller", "true"));

    // Policy 2: Any Teller with Active Election
    options.AddPolicy("AllowTellersInActiveElection", policy =>
        policy.Requirements.Add(new TellerWithActiveElectionRequirement()));

    // Policy 3: Voter Only
    options.AddPolicy("AllowVoter", policy =>
        policy.RequireClaim("IsVoter", "true"));

    // Policy 4: System Administrator
    options.AddPolicy("SysAdminOnly", policy =>
        policy.RequireClaim("IsSysAdmin", "true"));
});

// Custom requirement for active election check
public class TellerWithActiveElectionRequirement : IAuthorizationRequirement { }

public class TellerWithActiveElectionHandler : AuthorizationHandler<TellerWithActiveElectionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TellerWithActiveElectionHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TellerWithActiveElectionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // Check 1: User is a teller (admin or guest)
        var isKnownTeller = context.User.HasClaim("IsKnownTeller", "true");
        var isGuestTeller = context.User.HasClaim("IsGuestTeller", "true");
        var isTeller = isKnownTeller || isGuestTeller;
        
        // Check 2: Has active election
        // Option A: From session/cache
        var currentElectionGuid = httpContext.Session.GetString("CurrentElectionGuid");
        var hasActiveElection = !string.IsNullOrEmpty(currentElectionGuid) && 
                                Guid.TryParse(currentElectionGuid, out var guid) && 
                                guid != Guid.Empty;
        
        // Option B: From JWT claim (if using stateless auth)
        // var hasActiveElection = context.User.HasClaim(c => 
        //     c.Type == "CurrentElectionGuid" && !string.IsNullOrEmpty(c.Value));
        
        if (isTeller && hasActiveElection)
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}
```

### Controller Usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class BallotsController : ControllerBase
{
    // All actions require teller + active election
    [Authorize(Policy = "AllowTellersInActiveElection")]
    [HttpPost("save-vote")]
    public async Task<IActionResult> SaveVote([FromBody] SaveVoteRequest request)
    {
        // Implementation
    }
}

[ApiController]
[Route("api/[controller]")]
public class VoteController : ControllerBase
{
    // All actions require voter authentication
    [Authorize(Policy = "AllowVoter")]
    [HttpPost("join-election")]
    public async Task<IActionResult> JoinElection([FromBody] JoinElectionRequest request)
    {
        // Implementation
    }
}

[ApiController]
[Route("api/[controller]")]
public class ElectionsController : ControllerBase
{
    // Most actions require authenticated admin
    [Authorize(Policy = "ForAuthenticatedTeller")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateElection([FromBody] CreateElectionRequest request)
    {
        // Implementation
    }

    // Some actions allow guest tellers
    [Authorize(Policy = "AllowTellersInActiveElection")]
    [HttpPost("select")]
    public async Task<IActionResult> SelectElection([FromBody] SelectElectionRequest request)
    {
        // Implementation
    }
}
```

### Session State Alternatives

**Option 1: Distributed Cache (Redis)**
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// In controller
_cache.SetString($"User:{userId}:CurrentElectionGuid", electionGuid.ToString());
```

**Option 2: JWT Claims (Stateless)**
```csharp
var claims = new List<Claim>
{
    new Claim("UserName", userName),
    new Claim("UniqueID", uniqueId),
    new Claim("IsKnownTeller", "true"),
    new Claim("CurrentElectionGuid", electionGuid.ToString()) // Include in token
};

var token = new JwtSecurityToken(
    issuer: _configuration["Jwt:Issuer"],
    audience: _configuration["Jwt:Audience"],
    claims: claims,
    expires: DateTime.UtcNow.AddHours(6),
    signingCredentials: credentials
);
```

**Option 3: Hybrid (JWT + Redis)**
- Store immutable claims in JWT (UserName, IsKnownTeller, IsVoter)
- Store mutable session state in Redis (CurrentElectionGuid, CurrentLocationGuid)

---

## Security Best Practices for Migration

### 1. Separation of Concerns

✅ **DO**: Keep the three authentication systems independent
```csharp
services.AddAuthentication()
    .AddCookie("AdminAuth")
    .AddCookie("GuestTellerAuth")
    .AddCookie("VoterAuth");
```

❌ **DON'T**: Mix authentication systems or allow escalation

### 2. Session Management

✅ **DO**: Use distributed cache (Redis) for session state in production
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
});
```

❌ **DON'T**: Use in-memory session state in multi-server deployments

### 3. Claims Validation

✅ **DO**: Validate claims on every request
```csharp
protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    TellerWithActiveElectionRequirement requirement)
{
    // Always validate claims exist and have expected values
    if (!context.User.Identity.IsAuthenticated)
        return Task.CompletedTask;
    
    // ... validation logic
}
```

❌ **DON'T**: Trust session state without authentication

### 4. Access Code Security

✅ **DO**: Generate cryptographically random access codes
```csharp
using var rng = RandomNumberGenerator.Create();
var bytes = new byte[4];
rng.GetBytes(bytes);
var code = BitConverter.ToUInt32(bytes, 0).ToString("D8");
```

✅ **DO**: Allow admins to regenerate access codes if compromised

❌ **DON'T**: Use predictable or sequential access codes

### 5. Voter Code Security

✅ **DO**: Expire verification codes after short time (5-10 minutes)
```csharp
var codeAge = DateTime.UtcNow - onlineVoter.VerifyCodeDate;
if (codeAge.TotalMinutes > 10)
    return new { Error = "Code expired" };
```

✅ **DO**: Limit verification attempts (3-5 max)
```csharp
if (onlineVoter.VerifyAttempts >= 5)
    return new { Error = "Too many attempts. Request a new code." };
```

✅ **DO**: Rate-limit code issuance per email/phone (prevent spam)

❌ **DON'T**: Allow unlimited verification attempts

### 6. SignalR Authorization

✅ **DO**: Validate user type before adding to connection groups
```csharp
public override async Task OnConnectedAsync()
{
    var isKnownTeller = Context.User.HasClaim("IsKnownTeller", "true");
    var isGuestTeller = Context.User.HasClaim("IsGuestTeller", "true");
    
    if (isKnownTeller || isGuestTeller)
    {
        var electionGuid = GetCurrentElectionGuid();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Election_{electionGuid}");
    }
    
    await base.OnConnectedAsync();
}
```

❌ **DON'T**: Allow unauthenticated connections to hubs

### 7. CORS Configuration

✅ **DO**: Configure CORS for SPA frontend
```csharp
services.AddCors(options =>
{
    options.AddPolicy("VueFrontend", builder =>
    {
        builder
            .WithOrigins("https://tallyj.example.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for cookies/SignalR
    });
});
```

---

## Testing Authorization Rules

### Unit Tests

```csharp
[Fact]
public async Task AdminUser_CanAccessForAuthenticatedTellerEndpoint()
{
    // Arrange
    var claims = new[]
    {
        new Claim("UserName", "admin123"),
        new Claim("IsKnownTeller", "true")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var user = new ClaimsPrincipal(identity);
    
    var context = new AuthorizationHandlerContext(
        new[] { new ForAuthenticatedTellerRequirement() },
        user,
        null);
    
    var handler = new ForAuthenticatedTellerHandler();
    
    // Act
    await handler.HandleAsync(context);
    
    // Assert
    Assert.True(context.HasSucceeded);
}

[Fact]
public async Task GuestTeller_CannotAccessForAuthenticatedTellerEndpoint()
{
    // Arrange
    var claims = new[]
    {
        new Claim("UserName", "T:guest123"),
        new Claim("IsGuestTeller", "true")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var user = new ClaimsPrincipal(identity);
    
    var context = new AuthorizationHandlerContext(
        new[] { new ForAuthenticatedTellerRequirement() },
        user,
        null);
    
    var handler = new ForAuthenticatedTellerHandler();
    
    // Act
    await handler.HandleAsync(context);
    
    // Assert
    Assert.False(context.HasSucceeded);
}

[Fact]
public async Task GuestTeller_WithActiveElection_CanAccessBallotsController()
{
    // Arrange
    var claims = new[]
    {
        new Claim("UserName", "T:guest123"),
        new Claim("IsGuestTeller", "true")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var user = new ClaimsPrincipal(identity);
    
    var httpContext = new DefaultHttpContext();
    httpContext.Session = new TestSession();
    httpContext.Session.SetString("CurrentElectionGuid", Guid.NewGuid().ToString());
    
    var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
    
    var context = new AuthorizationHandlerContext(
        new[] { new TellerWithActiveElectionRequirement() },
        user,
        null);
    
    var handler = new TellerWithActiveElectionHandler(httpContextAccessor);
    
    // Act
    await handler.HandleAsync(context);
    
    // Assert
    Assert.True(context.HasSucceeded);
}
```

### Integration Tests

```csharp
[Fact]
public async Task CreateElection_WithoutAdminAuth_Returns401()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new { ElectionName = "Test Election" };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/elections/create", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task CreateElection_WithGuestTellerAuth_Returns403()
{
    // Arrange
    var client = _factory.CreateClient();
    await AuthenticateAsGuestTeller(client, electionGuid: Guid.NewGuid());
    var request = new { ElectionName = "Test Election" };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/elections/create", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task CreateElection_WithAdminAuth_Returns200()
{
    // Arrange
    var client = _factory.CreateClient();
    await AuthenticateAsAdmin(client, username: "admin123");
    var request = new { ElectionName = "Test Election" };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/elections/create", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

---

## Summary

**Custom Authorization Attributes** (4):
1. `[ForAuthenticatedTeller]` - Admin only
2. `[AllowTellersInActiveElection]` - Admin or Guest Teller + active election
3. `[AllowVoter]` - Voter only
4. `[AllowAnonymous]` - Public access

**User Types** (3):
1. **Admin** - Full privileges, username/password auth
2. **Guest Teller** - Limited privileges, access code auth, single election
3. **Voter** - Online voting only, one-time code auth

**Session State**:
- StateServer (out-of-process) with 6-hour timeout
- Migrate to Redis or JWT claims for .NET Core

**FluentSecurity**:
- Used for declarative security configuration
- Migrate to ASP.NET Core policy-based authorization

**Migration Approach**:
- Replace custom attributes with policies
- Implement custom authorization handlers for complex logic
- Maintain separation of 3 authentication systems
- Use distributed cache or JWT for session state
- Comprehensive testing of authorization rules

---

## Related Documentation

- **Authentication Details**: `security/authentication.md` (12,000+ lines covering all 3 systems)
- **API Endpoints**: `api/endpoints.md` (complete endpoint inventory with auth requirements)
- **Controller Documentation**: `api/controllers/*.md` (per-controller authorization details)
- **Database Schema**: `database/entities.md` (AspNetUsers, OnlineVoter, Election tables)
- **SignalR Security**: `signalr/hubs-overview.md` (hub authorization patterns)

---

**Document Status**: ✅ Complete  
**Last Updated**: 2026-01-03  
**Migration Target**: .NET Core 8.0 + ASP.NET Core Identity + Policy-Based Authorization
