# TallyJ Authentication Systems Documentation

## Overview

TallyJ implements **three completely independent authentication systems** to serve different user types. These systems DO NOT share authentication mechanisms, user databases, or session management.

---

## System 1: Admin Authentication (Username + Password + Optional 2FA)

### Purpose
Authenticates **system administrators and election owners** who need full access to create elections, configure settings, and manage the system.

### User Database
- **Table**: `AspNetUsers` (ASP.NET Identity 2.2.4)
- **Storage**: SQL Server database
- **Management**: Admin accounts must be created by system administrators

### Authentication Flow

#### Step 1: Login Page
- **URL**: `/Account/LogOn`
- **View**: Razor view with username/password form
- **Method**: POST to `/Account/LogOn`

#### Step 2: Credential Validation
**Code**: `AccountController.cs:77-127`

```csharp
public ActionResult LogOn(LogOnModelV1 model, string returnUrl)
{
    if (ModelState.IsValid)
    {
        // Validate using ASP.NET Membership Provider
        if (Membership.ValidateUser(model.UserName, model.PasswordV1))
        {
            var membershipUser = Membership.GetUser(model.UserName);
            var email = membershipUser?.Email;

            // Create claims-based identity
            var claims = new List<Claim>
            {
                new Claim("UserName", model.UserName),
                new Claim("UniqueID", "A:" + model.UserName),  // "A:" prefix = Admin
                new Claim("IsKnownTeller", "true")
            };

            // Check for SysAdmin role
            if (membershipUser?.Comment == "SysAdmin") 
                claims.Add(new Claim("IsSysAdmin", "true"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);

            var authenticationProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = false,
                ExpiresUtc = DateTime.UtcNow.AddDays(7)
            };

            HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);

            new LogHelper().Add("Admin Logged In - {0} ({1})".FilledWith(model.UserName, email), true);

            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError("", "The user name or password provided is incorrect.");
    }

    return View(model);
}
```

#### Step 3: Session Establishment
- **Technology**: OWIN Cookie Authentication
- **Cookie Name**: `.AspNet.Cookies`
- **Cookie Security**: 
  - `HttpOnly`: true
  - `Secure`: true (production)
  - `SameSite`: Strict (production) / Lax (dev)
- **Expiration**: 7 days
- **Session State**: StateServer (TCP on localhost:42424)

**Code**: `OwinStartup.cs:31-38`

```csharp
app.UseCookieAuthentication(new CookieAuthenticationOptions
{
    AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
    LoginPath = new PathString("/"),
    CookieSecure = useSecure ? CookieSecureOption.Always : CookieSecureOption.Never,
    CookieSameSite = useSecure ? SameSiteMode.Strict : SameSiteMode.Lax,
    CookieHttpOnly = true
});
```

### Claims Stored in Identity

| Claim Type | Value | Purpose |
|------------|-------|---------|
| UserName | Admin's username | Display name, audit logs |
| UniqueID | "A:{username}" | Unique identifier (prefix "A" = Admin) |
| IsKnownTeller | "true" | Has teller privileges |
| IsSysAdmin | "true" | System administrator (optional, only if Comment="SysAdmin") |

### Authorization Checks
- **Attribute**: `[ForAuthenticatedTeller]`
- **Check**: `UserSession.IsKnownTeller` (checks for "IsKnownTeller" claim)
- **Code**: `ForAuthenticatedTellerAttribute.cs:8-11`

```csharp
protected override bool AuthorizeCore(HttpContextBase httpContext)
{
    return UserSession.IsKnownTeller;
}
```

### Session Management
**Code**: `UserSession.cs:101-104`

```csharp
public static bool IsLoggedInTeller
{
    get { return LoginId.HasContent() && HttpContext.Current.User.Identity.Name.HasContent(); }
}
```

### Logout
**Code**: `AccountController.cs:133-148`

```csharp
public ActionResult LogOff()
{
    HttpContext.GetOwinContext().Authentication.SignOut(
        DefaultAuthenticationTypes.ExternalCookie,
        DefaultAuthenticationTypes.ApplicationCookie);

    // Ensure cookie is removed
    Response.Cookies.Add(new HttpCookie(".AspNet.Cookies")
    {
        Secure = AppSettings["secure"].AsBoolean(true),
        Expires = DateTime.Now.AddDays(-99)
    });

    UserSession.ProcessLogout();

    return RedirectToAction("Index", "Public");
}
```

### Security Features
1. **Password Storage**: Hashed in AspNetUsers table (ASP.NET Identity hashing)
2. **Account Lockout**: Configurable (MaxInvalidPasswordAttempts in web.config)
3. **2FA**: Enabled via `TwoFactorEnabled` column (not currently implemented in UI)
4. **External OAuth** (Optional):
   - Google OAuth2
   - Facebook
   - Stored in `AspNetUserLogins` table

### Migration to .NET Core
**Recommended Changes**:
- Replace ASP.NET Identity 2.x with ASP.NET Core Identity
- Replace Membership Provider with Identity UserManager
- Replace OWIN authentication with ASP.NET Core cookie authentication
- Add built-in 2FA support (TOTP)
- Consider adding passwordless options (email magic link, WebAuthn)

---

## System 2: Guest Teller Authentication (Access Code Only)

### Purpose
Allows **election workers without system accounts** to join elections and assist with voter registration and ballot entry using a shared election access code.

### User Database
- **NO user database**
- **NO AspNetUsers records**
- **NO passwords**
- Authentication based solely on knowing the election's access code

### Authentication Flow

#### Step 1: Join as Teller
- **URL**: `/` (Home page)
- **UI**: Modal dialog "Join as a Teller"
- **Inputs**:
  1. Select election from dropdown (only elections with `ListForPublic=true` shown)
  2. Enter access code (validates against `Election.ElectionPasscode`)

#### Step 2: Access Code Validation
**Code**: `PublicController.cs:78-81` → `TellerModel.cs:21-74`

```csharp
public JsonResult GrantAccessToGuestTeller(Guid electionGuid, string codeToTry, Guid oldComputerGuid)
{
    var passcode = new PublicElectionLister().GetPasscodeIfAvailable(electionGuid);
    
    if (passcode == null)
    {
        return new { Error = "Sorry, unknown election id" }.AsJsonResult();
    }
    
    if (passcode != codeToTry)
    {
        return new { Error = "Sorry, invalid code entered" }.AsJsonResult();
    }

    // Create temporary/fake authentication
    if (!UserSession.IsLoggedInTeller)
    {
        // Generate fake username from session ID
        var fakeUserName = "T:" + HttpContext.Current.Session.SessionID.Substring(0, 5) 
                                + Guid.NewGuid().ToString().Substring(0, 5);

        var claims = new List<Claim>
        {
            new Claim("UserName", fakeUserName),
            new Claim("IsGuestTeller", "true"),           // Identifies as guest
            new Claim("UniqueID", fakeUserName),          // Prefix "T:" = Teller
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);

        var authenticationProperties = new AuthenticationProperties()
        {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddDays(7)
        };

        HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);
        
        UserSession.IsGuestTeller = true;
    }

    electionModel.JoinIntoElection(electionGuid, oldComputerGuid);

    return new
    {
        LoggedIn = true,
        CompGuid = UserSession.CurrentComputer.ComputerGuid
    }.AsJsonResult();
}
```

#### Step 3: Session Establishment
- **Technology**: Same OWIN Cookie Authentication as admins
- **Cookie**: `.AspNet.Cookies`
- **Username**: `T:{sessionId}{guid}` (e.g., "T:a1b2c3d4e")
- **No password**: Guest tellers cannot log out and back in
- **Session-bound**: If session expires, must re-enter access code

### Claims Stored in Identity

| Claim Type | Value | Purpose |
|------------|-------|---------|
| UserName | "T:{random}" | Fake username for guest |
| UniqueID | "T:{random}" | Unique identifier (prefix "T" = Teller) |
| IsGuestTeller | "true" | Identifies as guest (not full admin) |

### Authorization Checks
- **Attribute**: `[AllowTellersInActiveElection]`
- **Check**: Either `IsKnownTeller` (admin) OR `IsGuestTeller` (guest)
- **Code**: `AllowTellersInActiveElectionAttribute.cs`

### Limitations for Guest Tellers
1. **Cannot create or delete elections**
2. **Cannot modify election settings** (only head teller can)
3. **Cannot access System Administration** pages
4. **Cannot add other tellers** to the election
5. **Can only work in the election they joined**
6. **Session-dependent**: Loss of session = loss of access

### Security Model
- **Access Code Storage**: Plaintext in `Election.ElectionPasscode` column (max 50 chars)
- **Access Code Scope**: Per-election (each election has its own code)
- **Access Code Visibility**: 
  - Shown to election owner on Monitor page
  - Can be changed any time by owner
  - Must be shared out-of-band (email, phone, in-person)
- **No Brute Force Protection**: Simple string comparison (no rate limiting in code)
- **No Audit Trail**: Guest teller actions logged but tied to temporary username

### Access Code Management
- **Set during election setup**: `/Setup` page, Step 2
- **Displayed on**: Monitor Progress page (`/After/Monitor`)
- **Can be changed**: Anytime by election owner
- **Format**: Free-form text (can include spaces, any characters, up to 50 chars)
- **Recommendation**: Use pass phrase (e.g., "blue sky convention 2026")

### Migration to .NET Core
**Recommended Changes**:
- Keep access code model (it works well for this use case)
- Add rate limiting to prevent brute force attacks
- Consider time-limited access codes (optional expiration)
- Add audit logging for access code usage
- Consider JWTs instead of session-bound authentication for better scalability
- Add option to generate random secure codes

---

## System 3: Voter Authentication (One-Time Code via Email/SMS)

### Purpose
Allows **voters** to authenticate using their registered email or phone number with a one-time verification code. **No passwords, no accounts**.

### User Database
- **Table**: `OnlineVoter`
- **Scope**: Global (not per-election)
- **Key Fields**:
  - `VoterId`: Email address or phone number
  - `VoterIdType`: "email" or "sms"
  - `VerifyCode`: Current 6-digit code
  - `VerifyCodeDate`: When code was issued
  - `VerifyAttempts`: Failed verification attempts
  - `WhenRegistered`: First use of this email/phone
  - `WhenLastLogin`: Most recent successful login

### Authentication Flow

#### Step 1: Request Verification Code
**URL**: `/` (Home page) → "Vote Online" button

**UI Flow**:
1. Click "Vote Online"
2. Choose authentication method:
   - **Email**: "Using your email"
   - **Phone**: "Using your phone"

**Code**: `PublicController.cs:143-146` → `VoterCodeHelper.IssueCode()`

```csharp
public JsonResult IssueCode(string type, string method, string target, string hubKey)
{
    var helper = new VoterCodeHelper(hubKey);
    return helper.IssueCode(type, method, target).AsJsonResult();
}
```

**Process**:
1. Voter enters email or phone number
2. System generates 6-digit random code
3. Code stored in `OnlineVoter.VerifyCode` with timestamp
4. Code sent via:
   - **Email**: SMTP (configured in web.config)
   - **SMS**: Twilio API
5. Code expires after set time (configurable)

#### Step 2: Enter Verification Code
**Code**: `PublicController.cs:149-152` → `VoterCodeHelper.LoginWithCode()`

```csharp
public JsonResult LoginWithCode(string code, string hubKey)
{
    var helper = new VoterCodeHelper(hubKey);
    return helper.LoginWithCode(code).AsJsonResult();
}
```

**Validation**:
1. Check code matches `OnlineVoter.VerifyCode`
2. Check code is not expired (`VerifyCodeDate`)
3. Check attempt count (`VerifyAttempts` < max allowed)
4. If valid → create claims-based identity

#### Step 3: Claims Creation
**Code**: `UserSession.cs:285-332` (old flow, but illustrates principle)

```csharp
public static void RecordVoterLogin_old(string voterId, string voterIdType, string source, string country)
{
    var db = GetNewDbContext;
    var onlineVoter = db.OnlineVoter.FirstOrDefault(ov => 
        ov.VoterId == voterId && ov.VoterIdType == voterIdType);

    var utcNow = DateTime.UtcNow;

    if (onlineVoter == null)
    {
        // First time voter - create record
        onlineVoter = new OnlineVoter
        {
            VoterId = voterId,
            VoterIdType = voterIdType,
            WhenRegistered = utcNow,
            Country = country
        };
        db.OnlineVoter.Add(onlineVoter);
    }
    else
    {
        VoterLastLogin = onlineVoter.WhenLastLogin.AsUtc() ?? DateTime.MinValue;
    }

    onlineVoter.WhenLastLogin = utcNow;
    db.SaveChanges();

    VoterLoginSource = source;

    logHelper.Add($"Voter login via {source} {voterId}", true, voterId);

    new VoterPersonalHub().Login(voterId); // Notify SignalR hub
}
```

**Claims**:
- **VoterId**: Email or phone
- **VoterIdType**: "email" or "sms" 
- **IsVoter**: "true"
- **UniqueID**: "V:{voterId}" (prefix "V" = Voter)
- **Source**: Login method (email, sms, google-oauth2, facebook, etc.)

**Code**: `UserSession.cs:253-256`

```csharp
public static string UniqueId => ActivePrincipal.FindFirst("UniqueId")?.Value;
public static string VoterId => ActivePrincipal.FindFirst("VoterId")?.Value;
public static string VoterIdType => ActivePrincipal.FindFirst("VoterIdType")?.Value;
public static bool IsVoter => ActivePrincipal.FindFirst("IsVoter")?.Value == "true";
```

#### Step 4: Join Election
**Code**: `VoteController.cs:41-141`

Once authenticated, voter must select an election:

```csharp
public JsonResult JoinElection(Guid electionGuid)
{
    var voterId = UserSession.VoterId;
    
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

    var electionInfo = personQuery
        .Join(Db.Election, p => p.ElectionGuid, e => e.ElectionGuid, (p, e) => new { e, p })
        .SingleOrDefault();

    if (electionInfo == null)
    {
        return new { Error = "Invalid election" }.AsJsonResult();
    }

    var utcNow = DateTime.UtcNow;

    // Check if online voting is open
    if (electionInfo.e.OnlineWhenOpen.AsUtc() <= utcNow 
        && electionInfo.e.OnlineWhenClose.AsUtc() > utcNow)
    {
        UserSession.CurrentElectionGuid = electionInfo.e.ElectionGuid;
        UserSession.VoterInElectionPersonGuid = electionInfo.p.PersonGuid;

        // Get or create online voting info
        var votingInfo = Db.OnlineVotingInfo
            .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid 
                                 && ovi.PersonGuid == electionInfo.p.PersonGuid);

        if (votingInfo == null)
        {
            votingInfo = new OnlineVotingInfo
            {
                ElectionGuid = electionInfo.e.ElectionGuid,
                PersonGuid = electionInfo.p.PersonGuid,
                Status = OnlineBallotStatusEnum.New,
                WhenStatus = utcNow
            };
            Db.OnlineVotingInfo.Add(votingInfo);
            Db.SaveChanges();
        }

        return new
        {
            open = true,
            voterName = electionInfo.p.C_FullName,
            NumberToElect = electionInfo.e.NumberToElect,
            votingInfo
        }.AsJsonResult();
    }

    return new { closed = true, votingInfo }.AsJsonResult();
}
```

### Authorization Checks
- **Attribute**: `[AllowVoter]`
- **Check**: `UserSession.IsVoter` AND `HostSupportsOnlineElections`
- **Code**: `AllowVoterAttribute.cs:6-18`

```csharp
public class AllowVoterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var okay = UserSession.IsVoter && SettingsHelper.HostSupportsOnlineElections;
        if (!okay)
        {
            filterContext.Result = new RedirectResult("~/");
            filterContext.HttpContext.Response.StatusCode = 302;
        }
    }
}
```

### Voter Matching Logic
**Critical**: Voters are matched to `Person` records by email/phone:

| Person.Email | Person.Phone | VoterId Match |
|--------------|--------------|---------------|
| voter@example.com | null | Email: "voter@example.com" |
| null | +15551234567 | Phone: "+15551234567" |
| voter@example.com | +15551234567 | Either email OR phone |

**Implication**: If voter's email/phone in `Person` record doesn't match their login credentials, they cannot vote.

### Verification Code Security

#### Code Generation
- **Format**: 6-digit random number (000000-999999)
- **Storage**: Plaintext in `OnlineVoter.VerifyCode`
- **Expiration**: Configurable (typically 10-15 minutes)

#### Brute Force Protection
- **Max Attempts**: Tracked in `OnlineVoter.VerifyAttempts`
- **Window**: `VerifyAttemptsStart` to current time
- **Lockout**: After N failed attempts (configurable)
- **Reset**: After successful login or timeout

#### Code Delivery
**Email**:
- SMTP server configured in web.config
- Email template in `Election.EmailText`
- Subject in `Election.EmailSubject`
- From address in `Election.EmailFromAddress`

**SMS**:
- Twilio API integration
- SMS template in `Election.SmsText`
- Delivery status tracked in `SmsLog`
- Callback endpoint: `/Public/SmsStatus`

### Session Management
- **Session State**: StateServer (same as admins/tellers)
- **Session Keys**:
  - `VoterLoginSource`: How they authenticated (email, sms, google-oauth2, etc.)
  - `VoterInElectionPersonGuid`: PersonGuid in current election
  - `VoterLastLogin`: Previous login timestamp
  - `CurrentElectionGuid`: Election they're currently in

### No Persistent Credentials
**Critical Design**: Voters CANNOT:
- Set a password
- "Remember me"
- Log back in without requesting a new code

**Rationale**: 
- Simplicity for voters (no password management)
- Security (no password reuse, no password leaks)
- Fresh authentication each session

### Kiosk Mode
**Special case**: Voters can use a kiosk (shared computer) with a unique code:

- **Field**: `Person.KioskCode`
- **Matching**: `VoterIdType = "Kiosk"`, `VoterId = KioskCode`
- **Use Case**: In-person voting on election day at tellers' station

### Migration to .NET Core
**Recommended Changes**:
1. **Keep passwordless model** (it's excellent for voters)
2. **Add WebAuthn** for biometric authentication (optional)
3. **Add social login** (Google, Facebook, Apple) for easier access
4. **Improve code generation**: Use cryptographically secure random
5. **Add rate limiting** per email/phone to prevent abuse
6. **Consider magic links** as alternative to codes
7. **JWT tokens** for stateless voter sessions
8. **Add CAPTCHA** to prevent automated code requests

---

## Authentication System Comparison

| Feature | System 1: Admin | System 2: Guest Teller | System 3: Voter |
|---------|----------------|------------------------|-----------------|
| **User Database** | AspNetUsers | None | OnlineVoter |
| **Credential** | Username + Password | Access Code | Email/Phone + Code |
| **Password** | Yes (hashed) | No | No |
| **2FA** | Optional | No | N/A (code IS 2FA) |
| **Session Type** | Persistent (7 days) | Session-bound | Persistent (7 days) |
| **Can Re-login** | Yes | No (must re-enter code) | No (must request new code) |
| **OAuth** | Yes (Google, Facebook) | No | Possible (not implemented) |
| **Authorization** | ForAuthenticatedTeller | AllowTellersInActiveElection | AllowVoter |
| **Unique ID Prefix** | A: | T: | V: |
| **Claim: IsKnownTeller** | Yes | No | No |
| **Claim: IsGuestTeller** | No | Yes | No |
| **Claim: IsVoter** | No | No | Yes |
| **Claim: IsSysAdmin** | Maybe | No | No |
| **Access Scope** | All elections owned | One election | Elections with email/phone match |
| **Can Create Elections** | Yes | No | No |
| **Can Join Elections** | Yes | Yes (with code) | Yes (if registered) |
| **Can Vote** | No | No | Yes |

---

## Unified Session Management

Despite having 3 separate authentication systems, all use the same session infrastructure:

### OWIN Cookie Authentication
**Code**: `OwinStartup.cs:27-38`

```csharp
app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

app.UseCookieAuthentication(new CookieAuthenticationOptions
{
    AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
    LoginPath = new PathString("/"),
    CookieSecure = useSecure ? CookieSecureOption.Always : CookieSecureOption.Never,
    CookieSameSite = useSecure ? SameSiteMode.Strict : SameSiteMode.Lax,
    CookieHttpOnly = true
});
```

### StateServer Session State
**Code**: `web.config:115`

```xml
<sessionState mode="StateServer" 
              cookieless="false" 
              stateConnectionString="tcpip=localhost:42424" 
              timeout="360">
</sessionState>
```

**Why StateServer**:
- Shared session across multiple web servers (if scaled out)
- Survives application pool recycling
- Better for long elections (6-hour timeout)

### Anti-Forgery Token
**Code**: `OwinStartup.cs:25`

```csharp
AntiForgeryConfig.UniqueClaimTypeIdentifier = "UniqueID";
```

All three systems use the "UniqueID" claim for CSRF protection:
- Admin: `A:{username}`
- Guest Teller: `T:{sessionId}{guid}`
- Voter: `V:{email or phone}`

---

## SignalR Connection Security

Each authenticated user type joins different SignalR hubs:

**Admin/Teller**:
```csharp
public void JoinMainHub(string connId, string electionGuid)
{
    if (UserSession.CurrentElectionGuid == Guid.Empty) return;
    
    new MainHub().Join(connId);
}
```

**Voter**:
```csharp
public void JoinVoterHubs(string connId)
{
    new AllVotersHub().Join(connId);
    new VoterPersonalHub().Join(connId);
}
```

**Public** (unauthenticated):
```csharp
public JsonResult PublicHub(string connId)
{
    new PublicHub().Join(connId);
    return OpenElections();
}
```

---

## Screenshot: SignalR Disconnection Handling

Based on provided screenshot (`ca8c5e4b-6a25-4741-8663-dc2ddcfe0d07.png`):

**Error Message**: 
> "We've been disconnected from the server for too long. Please reload/refresh this page to reconnect and continue."

**Display**: 
- Red banner at top of page
- Prominent X button to dismiss
- Blocks user interaction until reloaded

**Detection**:
- Client-side JavaScript detects SignalR connection loss
- After timeout (configurable), shows error banner
- User must reload page to re-establish connection

**Code Location**: Likely in client-side SignalR initialization (`.js` files)

---

## Security Recommendations for Migration

### System 1: Admin Authentication
1. ✅ **Add mandatory 2FA** for all admin accounts (TOTP, SMS backup)
2. ✅ **Implement account recovery** (email-based, security questions)
3. ✅ **Add password complexity requirements** (min length, special chars)
4. ✅ **Implement account lockout** after N failed attempts
5. ✅ **Add audit logging** for all admin actions
6. ✅ **Implement session timeout warnings** (notify before session expires)
7. ✅ **Add "trusted devices"** to reduce 2FA friction

### System 2: Guest Teller Authentication
1. ✅ **Add rate limiting** on access code attempts (per IP, per session)
2. ✅ **Add time-limited codes** (optional expiration)
3. ✅ **Add IP whitelisting** (optional, for secure elections)
4. ✅ **Improve audit trail** (link guest actions to real teller names)
5. ⚠️ **Consider removing** in favor of lightweight admin accounts (debatable)

### System 3: Voter Authentication
1. ✅ **Keep passwordless model** (it's excellent)
2. ✅ **Add social login** (Google, Facebook, Apple)
3. ✅ **Add WebAuthn** for biometric authentication
4. ✅ **Improve code generation** (cryptographically secure)
5. ✅ **Add CAPTCHA** to prevent automated code requests
6. ✅ **Add magic links** as alternative to codes
7. ✅ **Add email/phone verification** before election day
8. ✅ **Consider device fingerprinting** for anomaly detection

### Cross-System
1. ✅ **Migrate to JWT tokens** for stateless authentication
2. ✅ **Add refresh tokens** for long-lived sessions
3. ✅ **Implement OIDC** for future third-party integrations
4. ✅ **Add Redis caching** for session data (replace StateServer)
5. ✅ **Improve logging** (centralized, structured, searchable)
6. ✅ **Add rate limiting** globally (per IP, per user)
7. ✅ **Add intrusion detection** (unusual login patterns)

---

**End of Authentication Documentation**
