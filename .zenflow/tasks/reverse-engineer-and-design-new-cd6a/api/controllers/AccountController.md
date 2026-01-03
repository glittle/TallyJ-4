# AccountController API Documentation

## Overview
**Purpose**: Admin authentication, registration, and password management  
**Base Route**: `/Account`  
**Authorization**: Mixed (mostly `[AllowAnonymous]`)  
**Authentication System**: ASP.NET Membership + OWIN Cookie Authentication with Claims

## Authentication Details

This controller implements **Admin Authentication** (System 1 of 3) - username/password authentication for election organizers.

**Authentication Flow**:
1. User submits username + password
2. ASP.NET Membership validates credentials
3. Claims-based identity created with UserName, UniqueID, IsKnownTeller, IsSysAdmin
4. OWIN Cookie issued (7-day expiration)
5. UserSession state initialized

**Session Claims**:
- `UserName`: Admin's username
- `UniqueID`: Prefixed with "A:" (e.g., "A:admin123")
- `IsKnownTeller`: Always "true" for admins
- `IsSysAdmin`: Set if user's Comment field = "SysAdmin"

---

## Endpoints

### 1. GET `/Account/LogOn`
**Purpose**: Display login page  
**HTTP Method**: GET  
**Authorization**: `[AllowAnonymous]`  
**Returns**: View (HTML page or partial view based on `?content` query param)

**Response**:
- Full view if no query string
- Partial view if `?content` present
- `ViewBag.FormAction` set to "LogOn"

**Business Logic**:
- None (view only)

**SignalR**: None

---

### 2. POST `/Account/LogOn`
**Purpose**: Authenticate admin user with username/password  
**HTTP Method**: POST  
**Authorization**: `[AllowAnonymous]`  
**Returns**: `ActionResult` (redirect or view)

**Request Body** (`LogOnModelV1`):
```csharp
{
  "UserName": "string",      // Required, no @ symbol allowed
  "PasswordV1": "string",    // Required
  "RememberMe": bool         // Optional (not currently used)
}
```

**Success Response** (HTTP 302 Redirect):
- **Default**: Redirect to `/Dashboard/Index`
- **If returnUrl valid**: Redirect to returnUrl

**Error Response** (HTTP 200 with view):
```csharp
{
  ModelState.Errors: ["The user name or password provided is incorrect."]
}
```

**Business Logic**:
1. Reset `UserSession.UserGuidHasBeenLoaded = false`
2. Call `Membership.ValidateUser(userName, password)`
3. If valid:
   - Get `MembershipUser` to retrieve email
   - Create claims: UserName, UniqueID ("A:" + userName), IsKnownTeller
   - Check if `user.Comment == "SysAdmin"` → Add IsSysAdmin claim
   - Create `ClaimsIdentity` with CookieAuthentication
   - Set cookie expiration: 7 days
   - Set `UserSession.IsKnownTeller = true`
   - Set `UserSession.AdminAccountEmail = email`
   - Log event: "Admin Logged In - {userName} ({email})"
4. If invalid:
   - Add ModelState error
   - Return view with model

**SignalR**: None (session-based authentication)

**Side Effects**:
- Creates authentication cookie (`.AspNet.Cookies`)
- Writes to `C_Log` table
- Sets UserSession properties

**Notes**:
- RememberMe parameter is accepted but not actively used (expiration always 7 days)
- Password validation happens in ASP.NET Membership Provider
- SysAdmin role determined by user.Comment field (not role table)

---

### 3. GET `/Account/LogOff`
**Purpose**: Log out admin user  
**HTTP Method**: GET  
**Authorization**: `[AllowAnonymous]`  
**Returns**: Redirect to `/Public/Index`

**Business Logic**:
1. Call `Authentication.SignOut(ExternalCookie, ApplicationCookie)`
2. Explicitly delete `.AspNet.Cookies` cookie (set expiration to -99 days)
3. Call `UserSession.ProcessLogout()` to clear session state
4. Redirect to public homepage

**Side Effects**:
- Deletes authentication cookies
- Clears all UserSession properties
- Does NOT log to database

**Notes**:
- Cookie is explicitly deleted to ensure logout on load-balanced servers
- Secure flag on cookie determined by `Web.config: secure` setting

---

### 4. GET `/Account/LogOut`
**Purpose**: Alias for LogOff  
**HTTP Method**: GET  
**Authorization**: `[AllowAnonymous]`  
**Returns**: Redirect to `/Public/Index` (via LogOff)

**Business Logic**:
- Calls `LogOff()` internally

---

### 5. GET `/Account/Register`
**Purpose**: Display registration page  
**HTTP Method**: GET  
**Authorization**: `[AllowAnonymous]`  
**Returns**: View (HTML page or partial view)

**Response**:
- Full view if no query string
- Partial view if `?content` present
- `ViewBag.FormAction` set to "Register"

**Business Logic**:
- None (view only)

---

### 6. POST `/Account/Register`
**Purpose**: Create new admin account  
**HTTP Method**: POST  
**Authorization**: `[AllowAnonymous]`  
**Returns**: Redirect to `/Dashboard/Index` on success, or view with errors

**Request Body** (`RegisterModel`):
```csharp
{
  "UserName": "string",         // Required, max 50 chars
  "Email": "string",            // Required, valid email format
  "Password": "string",         // Required, min 6 chars, max 100 chars
  "ConfirmPassword": "string"   // Required, must match Password
}
```

**Success Response** (HTTP 302 Redirect):
- Redirect to `/Dashboard/Index`
- Auto-login after registration

**Error Response** (HTTP 200 with view):
```csharp
{
  ModelState.Errors: [
    "User name already exists. Please enter a different user name.",
    "The e-mail address provided is invalid. Please check the value and try again.",
    // ... other validation errors
  ]
}
```

**Business Logic**:
1. Validate model
2. Call `Membership.CreateUser(userName, password, email, ...)`
3. If successful:
   - Create claims: UserName, UniqueID ("A:" + userName), IsKnownTeller
   - Create `ClaimsIdentity` with CookieAuthentication
   - Set cookie expiration: 7 days
   - Set `UserSession.IsKnownTeller = true`
   - Set `UserSession.AdminAccountEmail = email`
   - Log event: "Admin Registered - {userName} ({email})"
   - Call `ElectionsListViewModel().CheckIfAddedToNew()` (check for election invitations)
   - Auto-login user
4. If failed:
   - Add ModelState error with specific failure reason
   - Return view

**SignalR**: None

**Side Effects**:
- Creates user in `AspNetUsers` table
- Creates authentication cookie
- Writes to `C_Log` table
- May add user to existing elections if invited

**Validation Rules**:
- UserName: Required, max 50 characters, no @ symbol
- Email: Required, valid email format, unique
- Password: Required, 6-100 characters
- ConfirmPassword: Must match Password

**Error Codes** (from `MembershipCreateStatus`):
- `DuplicateUserName`: "User name already exists..."
- `DuplicateEmail`: "A user name for that e-mail address already exists..."
- `InvalidPassword`: "The password provided is invalid..."
- `InvalidEmail`: "The e-mail address provided is invalid..."
- `InvalidUserName`: "The user name provided is invalid..."
- `ProviderError`: "The authentication provider returned an error..."
- `UserRejected`: "The user creation request has been canceled..."

**Notes**:
- New users are automatically logged in after successful registration
- New users are NOT SysAdmin by default (Comment field is empty)
- The system checks if user was invited to elections via `CheckIfAddedToNew()`

---

### 7. GET `/Account/ChangePassword`
**Purpose**: Display change password page  
**HTTP Method**: GET  
**Authorization**: `[AllowAnonymous]` (but requires authenticated session)  
**Returns**: View

**Business Logic**:
- None (view only)

---

### 8. POST `/Account/ChangePassword`
**Purpose**: Change current user's password  
**HTTP Method**: POST  
**Authorization**: None (requires authenticated session via UserSession.LoginId)  
**Returns**: Redirect to `ChangePasswordSuccess` on success, or view with errors

**Request Body** (`ChangePasswordModel`):
```csharp
{
  "OldPassword": "string",      // Required
  "NewPassword": "string",      // Required, min 6 chars, max 100 chars
  "ConfirmPassword": "string"   // Required, must match NewPassword
}
```

**Success Response** (HTTP 302 Redirect):
- Redirect to `/Account/ChangePasswordSuccess`

**Error Response** (HTTP 200 with view):
```csharp
{
  ModelState.Errors: [
    "The current password is incorrect or the new password is invalid."
  ]
}
```

**Business Logic**:
1. Validate model
2. Get current user via `Membership.GetUser(UserSession.LoginId)`
3. Call `currentUser.ChangePassword(oldPassword, newPassword)`
4. If successful → Redirect to success page
5. If failed (exception or false return) → Add error and return view

**Side Effects**:
- Updates password in `AspNetUsers` table
- Does NOT log out user or invalidate existing sessions

**Notes**:
- User must be authenticated (UserSession.LoginId must be set)
- Password change does NOT require re-authentication
- Existing authentication cookies remain valid

---

### 9. GET `/Account/ChangePasswordSuccess`
**Purpose**: Display password change success page  
**HTTP Method**: GET  
**Authorization**: None (requires authenticated session)  
**Returns**: View

**Business Logic**:
- None (view only)

---

### 10. POST `/Account/IdentitySignout`
**Purpose**: Internal helper to sign out OWIN authentication  
**HTTP Method**: POST (called internally)  
**Authorization**: None  
**Returns**: void

**Business Logic**:
- Calls `Authentication.SignOut(ApplicationCookie, ExternalCookie)`

**Notes**:
- Internal method, not exposed as public route
- Used by LogOff method

---

## Authorization Attributes Used

| Attribute | Purpose |
|-----------|---------|
| `[AllowAnonymous]` | Allow unauthenticated access (login, register, logout) |
| None (requires auth) | ChangePassword methods (implicitly require authenticated session via UserSession.LoginId) |

---

## Business Logic Classes Called

| Class | Methods | Purpose |
|-------|---------|---------|
| `Membership` | ValidateUser, CreateUser, GetUser | ASP.NET Membership authentication |
| `UserSession` | ProcessLogin, ProcessLogout, LoginId | Session state management |
| `LogHelper` | Add | Write audit logs to C_Log table |
| `ElectionsListViewModel` | CheckIfAddedToNew | Check for election invitations |

---

## SignalR Hubs Triggered

**None** - This controller does not trigger SignalR updates. Authentication state changes are session-based.

---

## Request/Response Models

### LogOnModelV1
```csharp
public class LogOnModelV1
{
  [Required]
  [Display(Name = "User name")]
  public string UserName { get; set; }

  [Required]
  [DataType(DataType.Password)]
  [Display(Name = "Password")]
  public string PasswordV1 { get; set; }

  [Display(Name = "Remember me?")]
  public bool RememberMe { get; set; }
}
```

### RegisterModel
```csharp
public class RegisterModel
{
  [Required]
  [Display(Name = "Login ID")]
  [StringLength(50, ErrorMessage = "Login ID must be less than 50 characters long.")]
  public string UserName { get; set; }

  [Required]
  [DataType(DataType.EmailAddress)]
  [Display(Name = "Email address")]
  public string Email { get; set; }

  [Required]
  [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
  [DataType(DataType.Password)]
  [Display(Name = "Password")]
  public string Password { get; set; }

  [DataType(DataType.Password)]
  [Display(Name = "Confirm password")]
  [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
  public string ConfirmPassword { get; set; }
}
```

### ChangePasswordModel
```csharp
public class ChangePasswordModel
{
  [Required]
  [DataType(DataType.Password)]
  [Display(Name = "Current password")]
  public string OldPassword { get; set; }

  [Required]
  [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
  [DataType(DataType.Password)]
  [Display(Name = "New password")]
  public string NewPassword { get; set; }

  [DataType(DataType.Password)]
  [Display(Name = "Confirm new password")]
  [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
  public string ConfirmPassword { get; set; }
}
```

---

## Session State Management

**UserSession Properties Set**:
- `UserSession.UserGuidHasBeenLoaded` - Reset on login
- `UserSession.IsKnownTeller` - Set to true for admins
- `UserSession.AdminAccountEmail` - Set to user's email
- `UserSession.LoginId` - Set to username (used by ChangePassword)

**Authentication Cookie**:
- Name: `.AspNet.Cookies`
- Type: OWIN CookieAuthentication
- Expiration: 7 days
- Secure: Based on Web.config `secure` setting
- HttpOnly: true (default)

---

## Database Tables Accessed

| Table | Operations | Purpose |
|-------|------------|---------|
| `AspNetUsers` | SELECT, INSERT, UPDATE | User account storage (via Membership) |
| `C_Log` | INSERT | Audit logging (login/register events) |

---

## External Integrations

**None** - This controller uses internal ASP.NET Membership.

**Note**: OAuth integrations (Google, Facebook) are commented out in code. External authentication was previously supported but disabled.

---

## Commented-Out Code

### OAuth External Login (Lines 34-72)
**Status**: Disabled  
**Reason**: Feature removed or not currently used  
**Functionality**: Google/Facebook OAuth login via ChallengeResult

**Methods Commented Out**:
- `POST /Account/LogOnLocal` - Local username/password login
- `POST /Account/LogOnExt` - External OAuth login

---

## Migration Notes for .NET Core

### Authentication Migration
1. **Replace ASP.NET Membership** → ASP.NET Core Identity
2. **Replace OWIN Cookie Auth** → ASP.NET Core Cookie Authentication
3. **Claims remain similar** (UserName, IsSysAdmin)
4. **Session management** → Replace with distributed cache (Redis) or JWT claims

### Recommended .NET Core Structure
```csharp
// .NET Core equivalent
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly SignInManager<ApplicationUser> _signInManager;

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginDto model) { ... }

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterDto model) { ... }

  [HttpPost("logout")]
  [Authorize]
  public async Task<IActionResult> Logout() { ... }

  [HttpPost("change-password")]
  [Authorize]
  public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model) { ... }
}
```

### Key Differences
- RESTful API (no views) → Frontend handles UI
- JWT tokens OR cookie authentication
- `UserManager<T>` instead of `Membership`
- `SignInManager<T>` instead of `FormsAuthentication`
- Policy-based authorization instead of custom attributes

### Session State Migration
- **Option 1**: JWT tokens with claims (stateless)
- **Option 2**: Redis-backed distributed cache
- **Option 3**: Cookie-based sessions with Redis

---

## Testing Scenarios

1. **Admin Login**
   - POST `/Account/LogOn` with valid credentials → Redirect to Dashboard
   - POST `/Account/LogOn` with invalid credentials → Error message
   - Verify cookie created and claims set correctly
   - Verify IsSysAdmin claim for sysadmin users

2. **Admin Registration**
   - POST `/Account/Register` with valid data → Account created, auto-login
   - POST `/Account/Register` with duplicate username → Error
   - POST `/Account/Register` with invalid email → Error
   - Verify CheckIfAddedToNew() called

3. **Password Change**
   - POST `/Account/ChangePassword` with correct old password → Success
   - POST `/Account/ChangePassword` with incorrect old password → Error
   - Verify user remains logged in after password change

4. **Logout**
   - GET `/Account/LogOff` → Cookie deleted, session cleared, redirect to home
   - Verify cannot access protected pages after logout

---

## Security Considerations

1. **Password Requirements**: Minimum 6 characters (weak by modern standards)
2. **No Account Lockout**: Repeated failed login attempts not tracked
3. **No 2FA**: Two-factor authentication not implemented
4. **SysAdmin Role**: Stored in Comment field (not normalized role table)
5. **RememberMe**: Accepted but not used (always 7-day expiration)
6. **Cookie Security**: Depends on Web.config `secure` setting

### Recommendations for Migration
- Implement ASP.NET Core Identity with:
  - Password requirements (min 8 chars, complexity rules)
  - Account lockout after failed attempts
  - 2FA support (TOTP or email/SMS)
  - Role-based authorization (proper role table)
  - Secure cookies (SameSite, Secure, HttpOnly)
  - Password reset flow
  - Email confirmation

---

## API Endpoint Summary

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Account/LogOn` | Display login page | Anonymous |
| POST | `/Account/LogOn` | Authenticate admin | Anonymous |
| GET | `/Account/LogOff` | Log out admin | Anonymous |
| GET | `/Account/LogOut` | Alias for LogOff | Anonymous |
| GET | `/Account/Register` | Display registration page | Anonymous |
| POST | `/Account/Register` | Create admin account | Anonymous |
| GET | `/Account/ChangePassword` | Display password change page | Session |
| POST | `/Account/ChangePassword` | Change password | Session |
| GET | `/Account/ChangePasswordSuccess` | Display success page | Session |

**Total Endpoints**: 9 (5 GET, 4 POST)
