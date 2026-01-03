# TallyJ OAuth Integration Documentation

## 1. Overview

TallyJ integrates with external OAuth 2.0 providers to enable **social login** for admin users. This allows administrators to sign in using their existing Google or Facebook accounts instead of creating separate TallyJ credentials.

### 1.1 Supported Providers
- **Google OAuth 2.0** (Sign in with Google)
- **Facebook OAuth** (Sign in with Facebook)

### 1.2 User Scope
OAuth authentication is **only available for admins** (System 1 authentication). It is NOT used for:
- Guest tellers (access code only)
- Voters (email/SMS verification code only)

### 1.3 Technology Stack
- **Framework**: ASP.NET Framework 4.8 with OWIN middleware
- **Library**: Microsoft.Owin.Security.Google, Microsoft.Owin.Security.Facebook
- **Configuration**: External AppSettings.config file (not in Web.config)
- **Storage**: OAuth provider mappings stored in `AspNetUserLogins` table

---

## 2. Google OAuth 2.0 Integration

### 2.1 Configuration Settings

**Location**: `App_Data/AppSettings.config` (external file referenced by Web.config)

```xml
<add key="GoogleClientId" value="[your Google OAuth client ID]" />
<add key="GoogleClientSecret" value="[your Google OAuth client secret]" />
```

**Where to Obtain**:
1. Google Cloud Console: https://console.cloud.google.com
2. Create new OAuth 2.0 credentials (Web application)
3. Configure authorized redirect URIs:
   - `https://yourdomain.com/signin-google`
   - `https://localhost:44399/signin-google` (development)

### 2.2 OWIN Middleware Configuration

**Code**: `OwinStartup.cs` (estimated based on authentication.md)

```csharp
var googleOptions = new GoogleOAuth2AuthenticationOptions
{
    ClientId = ConfigurationManager.AppSettings["GoogleClientId"],
    ClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"],
    Provider = new GoogleOAuth2AuthenticationProvider
    {
        OnAuthenticated = async context =>
        {
            // Extract user claims from Google
            context.Identity.AddClaim(new Claim("urn:google:name", context.Identity.FindFirstValue(ClaimTypes.Name)));
            context.Identity.AddClaim(new Claim("urn:google:email", context.Identity.FindFirstValue(ClaimTypes.Email)));
            context.Identity.AddClaim(new Claim("urn:google:profile", context.User.ToString()));
        }
    }
};

app.UseGoogleAuthentication(googleOptions);
```

### 2.3 OAuth Flow

#### Step 1: User Clicks "Sign in with Google"
- **URL**: `/Account/ExternalLogin?provider=Google&returnUrl=%2F`
- **Action**: Redirects to Google OAuth consent screen

#### Step 2: User Authorizes on Google
- **Consent Screen**: User approves sharing email, profile, and basic info
- **Scopes Requested**:
  - `openid` (identity)
  - `profile` (name, photo)
  - `email` (email address)

#### Step 3: Google Redirects Back to TallyJ
- **Callback URL**: `/signin-google` (handled by OWIN middleware)
- **Data Received**:
  - `id_token`: JWT containing user identity
  - `access_token`: OAuth access token (not stored)
  - User claims: email, name, Google user ID

#### Step 4: TallyJ Creates/Links Account
**Code**: `AccountController.cs:ExternalLoginCallback()`

```csharp
public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
{
    var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
    
    if (loginInfo == null)
    {
        return RedirectToAction("LogOn");
    }

    // Check if user already linked this OAuth provider
    var user = await UserManager.FindAsync(loginInfo.Login);

    if (user != null)
    {
        // Existing OAuth link - sign in directly
        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        return RedirectToLocal(returnUrl);
    }
    else
    {
        // New OAuth user - must register or link to existing account
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
        return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel 
        { 
            Email = loginInfo.Email 
        });
    }
}
```

#### Step 5: User Confirms Email and Creates Account
**Code**: `AccountController.cs:ExternalLoginConfirmation()`

```csharp
public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
{
    if (User.Identity.IsAuthenticated)
    {
        return RedirectToAction("Index", "Dashboard");
    }

    if (ModelState.IsValid)
    {
        var info = await AuthenticationManager.GetExternalLoginInfoAsync();
        
        if (info == null)
        {
            return View("ExternalLoginFailure");
        }

        // Create new AspNetUsers record
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await UserManager.CreateAsync(user);

        if (result.Succeeded)
        {
            // Link OAuth provider to user account
            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            
            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                return RedirectToLocal(returnUrl);
            }
        }

        AddErrors(result);
    }

    ViewBag.ReturnUrl = returnUrl;
    return View(model);
}
```

### 2.4 Database Storage

#### AspNetUsers Table
When user creates account via Google OAuth:

| Column | Value | Example |
|--------|-------|---------|
| `Id` | GUID (auto-generated) | `12345678-1234-1234-1234-123456789012` |
| `Email` | User's Google email | `user@gmail.com` |
| `UserName` | User's Google email | `user@gmail.com` |
| `PasswordHash` | NULL | *(no password needed)* |
| `SecurityStamp` | Auto-generated | *(GUID)* |
| `TwoFactorEnabled` | 0 | *(optional)* |

#### AspNetUserLogins Table
Links OAuth provider to user account:

| Column | Value | Example |
|--------|-------|---------|
| `LoginProvider` | `Google` | `Google` |
| `ProviderKey` | Google user ID | `1234567890123456789` |
| `UserId` | AspNetUsers.Id | `12345678-1234-1234-1234-123456789012` |

### 2.5 User Claims from Google

| Claim Type | Description | Example |
|------------|-------------|---------|
| `sub` | Google user ID (unique) | `1234567890123456789` |
| `name` | Full name | `John Doe` |
| `given_name` | First name | `John` |
| `family_name` | Last name | `Doe` |
| `email` | Email address | `john.doe@gmail.com` |
| `email_verified` | Email verification status | `true` |
| `picture` | Profile photo URL | `https://lh3.googleusercontent.com/...` |
| `locale` | User's locale | `en` |

### 2.6 Security Considerations

**Current Implementation**:
- ✅ OAuth state parameter (CSRF protection) - handled by OWIN middleware
- ✅ HTTPS required for production
- ✅ Client secret stored in external config file (not in source control)
- ⚠️ No explicit scope limiting beyond defaults
- ⚠️ Email domain restrictions not implemented (anyone with Google account can register)

**Potential Vulnerabilities**:
- No email domain whitelist (e.g., only allow `@organization.org`)
- No admin approval workflow for OAuth-created accounts
- OAuth token not stored (can't revoke or refresh)

---

## 3. Facebook OAuth Integration

### 3.1 Configuration Settings

**Location**: `App_Data/AppSettings.config` (external file referenced by Web.config)

```xml
<add key="FacebookAppId" value="[your Facebook App ID]" />
<add key="FacebookAppSecret" value="[your Facebook App Secret]" />
```

**Where to Obtain**:
1. Facebook Developers: https://developers.facebook.com/apps
2. Create new app (Business or Consumer)
3. Add Facebook Login product
4. Configure OAuth redirect URIs:
   - `https://yourdomain.com/signin-facebook`
   - `https://localhost:44399/signin-facebook` (development)

### 3.2 OWIN Middleware Configuration

**Code**: `OwinStartup.cs` (estimated)

```csharp
var facebookOptions = new FacebookAuthenticationOptions
{
    AppId = ConfigurationManager.AppSettings["FacebookAppId"],
    AppSecret = ConfigurationManager.AppSettings["FacebookAppSecret"],
    Provider = new FacebookAuthenticationProvider
    {
        OnAuthenticated = async context =>
        {
            // Extract user claims from Facebook
            context.Identity.AddClaim(new Claim("urn:facebook:access_token", context.AccessToken));
            context.Identity.AddClaim(new Claim("urn:facebook:name", context.Identity.FindFirstValue(ClaimTypes.Name)));
            context.Identity.AddClaim(new Claim("urn:facebook:email", context.Email));
        }
    },
    Scope = { "email", "public_profile" }
};

app.UseFacebookAuthentication(facebookOptions);
```

### 3.3 OAuth Flow

**Identical to Google OAuth** (see Section 2.3), with provider name changed to "Facebook".

#### Callback URL
- **Endpoint**: `/signin-facebook`
- **Handler**: OWIN middleware

#### Scopes Requested
- `email`: User's email address
- `public_profile`: Name, profile picture, gender, age range

### 3.4 Database Storage

#### AspNetUserLogins Table
Links Facebook provider to user account:

| Column | Value | Example |
|--------|-------|---------|
| `LoginProvider` | `Facebook` | `Facebook` |
| `ProviderKey` | Facebook user ID | `10223456789012345` |
| `UserId` | AspNetUsers.Id | `12345678-1234-1234-1234-123456789012` |

### 3.5 User Claims from Facebook

| Claim Type | Description | Example |
|------------|-------------|---------|
| `id` | Facebook user ID (unique) | `10223456789012345` |
| `name` | Full name | `John Doe` |
| `email` | Email address | `john.doe@example.com` |
| `picture.data.url` | Profile photo URL | `https://graph.facebook.com/v12.0/...` |

### 3.6 Security Considerations

**Current Implementation**:
- ✅ OAuth state parameter (CSRF protection) - handled by OWIN middleware
- ✅ HTTPS required for production
- ✅ Client secret stored in external config file
- ⚠️ Email may not be provided if user denies permission
- ⚠️ No Facebook Graph API token stored for future API calls

---

## 4. OAuth Account Linking

### 4.1 Linking Multiple Providers to One Account

Users can link **both** Google and Facebook to a single TallyJ account:

**Database**: `AspNetUserLogins` table

| UserId | LoginProvider | ProviderKey |
|--------|---------------|-------------|
| `user-guid-123` | `Google` | `google-id-456` |
| `user-guid-123` | `Facebook` | `facebook-id-789` |

**Benefit**: User can log in with either Google or Facebook.

### 4.2 Unlinking OAuth Providers

**Code**: `Manage2Controller.cs:RemoveLogin()`

```csharp
public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
{
    var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), 
        new UserLoginInfo(loginProvider, providerKey));

    if (result.Succeeded)
    {
        var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
        if (user != null)
        {
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        }
    }

    return RedirectToAction("ManageLogins");
}
```

**Constraint**: User must have at least one authentication method (password OR OAuth provider) to prevent account lockout.

---

## 5. .NET Core Migration Strategy

### 5.1 Technology Mapping

| ASP.NET Framework 4.8 | .NET Core 8 |
|----------------------|-------------|
| `Microsoft.Owin.Security.Google` | `Microsoft.AspNetCore.Authentication.Google` |
| `Microsoft.Owin.Security.Facebook` | `Microsoft.AspNetCore.Authentication.Facebook` |
| `OwinStartup.cs` | `Program.cs` (Startup.cs) |
| `AspNetUserLogins` table | Same (ASP.NET Core Identity uses same schema) |

### 5.2 Code Migration Example

#### Current (ASP.NET Framework - OwinStartup.cs)
```csharp
public void Configuration(IAppBuilder app)
{
    app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
    {
        ClientId = ConfigurationManager.AppSettings["GoogleClientId"],
        ClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"]
    });

    app.UseFacebookAuthentication(
        appId: ConfigurationManager.AppSettings["FacebookAppId"],
        appSecret: ConfigurationManager.AppSettings["FacebookAppSecret"]);
}
```

#### Target (.NET Core 8 - Program.cs)
```csharp
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["TallyJ:OAuth:Google:ClientId"];
        options.ClientSecret = builder.Configuration["TallyJ:OAuth:Google:ClientSecret"];
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;  // Store access_token and refresh_token
        
        options.Events.OnCreatingTicket = context =>
        {
            // Extract additional claims if needed
            return Task.CompletedTask;
        };
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["TallyJ:OAuth:Facebook:AppId"];
        options.AppSecret = builder.Configuration["TallyJ:OAuth:Facebook:AppSecret"];
        options.CallbackPath = "/signin-facebook";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
    });
```

### 5.3 Configuration Migration

#### Current (AppSettings.config)
```xml
<add key="GoogleClientId" value="..." />
<add key="GoogleClientSecret" value="..." />
<add key="FacebookAppId" value="..." />
<add key="FacebookAppSecret" value="..." />
```

#### Target (appsettings.json)
```json
{
  "TallyJ": {
    "OAuth": {
      "Google": {
        "ClientId": "123456789-abcdef.apps.googleusercontent.com",
        "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxx"
      },
      "Facebook": {
        "AppId": "1234567890123456",
        "AppSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
      }
    }
  }
}
```

**Security**: Use **User Secrets** for development, **Azure Key Vault** or **AWS Secrets Manager** for production.

### 5.4 Controller Migration

#### Current (ASP.NET Framework)
```csharp
public ActionResult ExternalLogin(string provider, string returnUrl)
{
    return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", 
        new { ReturnUrl = returnUrl }));
}
```

#### Target (.NET Core 8)
```csharp
[HttpGet]
public IActionResult ExternalLogin(string provider, string returnUrl = null)
{
    var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", 
        new { ReturnUrl = returnUrl });
    
    var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
    
    return Challenge(properties, provider);
}
```

### 5.5 Database Migration

**Good News**: ASP.NET Core Identity uses the **same database schema** as ASP.NET Identity 2.x.

**Tables to Migrate**:
- ✅ `AspNetUsers` - compatible (minor schema updates needed)
- ✅ `AspNetUserLogins` - fully compatible
- ✅ `AspNetUserClaims` - fully compatible
- ✅ `AspNetUserRoles` - fully compatible
- ✅ `AspNetRoles` - fully compatible

**Migration Steps**:
1. Run `dotnet ef migrations add UpdateIdentitySchema`
2. Review migration (should be minimal changes)
3. Apply migration: `dotnet ef database update`

### 5.6 Enhanced Features for .NET Core

#### Add Apple Sign-In
```csharp
.AddApple(options =>
{
    options.ClientId = builder.Configuration["TallyJ:OAuth:Apple:ClientId"];
    options.KeyId = builder.Configuration["TallyJ:OAuth:Apple:KeyId"];
    options.TeamId = builder.Configuration["TallyJ:OAuth:Apple:TeamId"];
    options.UsePrivateKey(keyId => 
        new StreamReader(Path.Combine("keys", $"{keyId}.p8")).ReadToEnd());
});
```

#### Add Microsoft Account
```csharp
.AddMicrosoftAccount(options =>
{
    options.ClientId = builder.Configuration["TallyJ:OAuth:Microsoft:ClientId"];
    options.ClientSecret = builder.Configuration["TallyJ:OAuth:Microsoft:ClientSecret"];
});
```

#### Token Storage for API Access
```csharp
options.SaveTokens = true;  // Enables token storage

// Later, retrieve tokens:
var accessToken = await HttpContext.GetTokenAsync("access_token");
var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
```

---

## 6. Security Best Practices

### 6.1 OAuth Security Checklist

#### Configuration
- ✅ Store client secrets in secure configuration (Azure Key Vault, AWS Secrets Manager)
- ✅ Use environment-specific credentials (dev vs. prod)
- ✅ Rotate client secrets periodically (every 90 days)
- ✅ Restrict authorized redirect URIs to known domains only

#### Runtime
- ✅ Validate OAuth state parameter (CSRF protection) - handled by middleware
- ✅ Require HTTPS for all OAuth flows
- ✅ Validate `email_verified` claim from Google before granting access
- ✅ Implement email domain whitelisting if needed (e.g., only `@organization.org`)
- ✅ Add logging for all OAuth authentication attempts
- ✅ Implement rate limiting on OAuth endpoints

#### Account Management
- ✅ Allow users to unlink OAuth providers (with safeguards)
- ✅ Require re-authentication before unlinking last authentication method
- ✅ Send email notifications when OAuth provider is linked/unlinked
- ✅ Display linked providers in user account settings

### 6.2 Error Handling

**Common OAuth Errors**:

| Error | Cause | Solution |
|-------|-------|----------|
| `invalid_client` | Wrong client ID/secret | Verify credentials in Google/Facebook console |
| `redirect_uri_mismatch` | Callback URL not registered | Add callback URL to OAuth provider settings |
| `access_denied` | User declined authorization | Show friendly message, allow retry |
| `invalid_grant` | Authorization code expired | Re-initiate OAuth flow |

### 6.3 Privacy & Compliance

**GDPR Considerations**:
- ✅ User can delete account (including OAuth links)
- ✅ User can export their data
- ✅ OAuth data (email, name) stored with consent
- ✅ Privacy policy links OAuth provider usage

**Data Retention**:
- OAuth provider keys (`ProviderKey`) stored indefinitely
- OAuth tokens not stored (current implementation)
- Consider token storage for future API integrations

---

## 7. Testing Strategy

### 7.1 Unit Tests
```csharp
[Fact]
public async Task ExternalLoginCallback_NewUser_CreatesAccount()
{
    // Arrange
    var mockUserManager = CreateMockUserManager();
    var controller = new AccountController(mockUserManager);
    
    // Mock external login info
    var loginInfo = new ExternalLoginInfo
    {
        Login = new UserLoginInfo("Google", "google-user-123"),
        Email = "newuser@gmail.com"
    };
    
    // Act
    var result = await controller.ExternalLoginCallback(returnUrl: "/");
    
    // Assert
    Assert.IsType<RedirectToActionResult>(result);
    mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
}
```

### 7.2 Integration Tests
```csharp
[Fact]
public async Task GoogleOAuth_EndToEnd_CreatesAuthenticatedSession()
{
    // Requires test Google OAuth credentials and HttpClient simulation
    var client = _factory.CreateClient();
    
    var response = await client.GetAsync("/Account/ExternalLogin?provider=Google");
    
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Contains("accounts.google.com", response.Headers.Location.ToString());
}
```

### 7.3 Manual Testing Checklist

- [ ] Google OAuth: New user registration
- [ ] Google OAuth: Existing user login
- [ ] Google OAuth: Link to existing account
- [ ] Facebook OAuth: New user registration
- [ ] Facebook OAuth: Existing user login
- [ ] Facebook OAuth: Link to existing account
- [ ] Unlink OAuth provider (with password fallback)
- [ ] Unlink OAuth provider (prevent account lockout if no password)
- [ ] OAuth error handling (user denies permission)
- [ ] HTTPS enforcement in production
- [ ] Callback URL validation

---

## 8. Monitoring & Logging

### 8.1 Events to Log

| Event | Log Level | Data to Include |
|-------|-----------|----------------|
| OAuth login initiated | Info | Provider, User IP |
| OAuth callback received | Info | Provider, Email, Success/Failure |
| New account created via OAuth | Info | Provider, Email, User ID |
| OAuth provider linked to account | Info | Provider, User ID |
| OAuth provider unlinked | Warning | Provider, User ID |
| OAuth error occurred | Error | Provider, Error code, Error message |

### 8.2 Metrics to Track

- OAuth login success rate (per provider)
- OAuth login failure rate (per provider)
- New user registrations via OAuth (per provider)
- OAuth account linking events
- OAuth error distribution

---

## 9. Known Limitations

### 9.1 Current Implementation Gaps

1. **No token refresh**: OAuth access tokens not stored, cannot make API calls to Google/Facebook on user's behalf
2. **No email verification enforcement**: Users can create account with unverified email from OAuth provider
3. **No admin approval workflow**: Anyone with Google/Facebook account can create admin account
4. **No scoped permissions**: All OAuth users get same permissions (no role assignment during OAuth registration)
5. **No account merging**: If user creates account with email then tries OAuth with same email, two separate accounts created

### 9.2 Migration Opportunities

**Add in .NET Core version**:
- ✅ Token storage for future API integrations
- ✅ Email verification enforcement
- ✅ Admin approval workflow for new OAuth accounts
- ✅ Role assignment during registration
- ✅ Account merging (detect existing email and offer to link)
- ✅ More OAuth providers (Apple, Microsoft, GitHub)

---

## 10. Quick Reference

### 10.1 Configuration Summary

| Setting | Location | Required |
|---------|----------|----------|
| Google Client ID | `TallyJ:OAuth:Google:ClientId` | Yes (if using Google) |
| Google Client Secret | `TallyJ:OAuth:Google:ClientSecret` | Yes (if using Google) |
| Facebook App ID | `TallyJ:OAuth:Facebook:AppId` | Yes (if using Facebook) |
| Facebook App Secret | `TallyJ:OAuth:Facebook:AppSecret` | Yes (if using Facebook) |

### 10.2 Callback URLs

| Provider | Development | Production |
|----------|-------------|------------|
| Google | `https://localhost:44399/signin-google` | `https://yourdomain.com/signin-google` |
| Facebook | `https://localhost:44399/signin-facebook` | `https://yourdomain.com/signin-facebook` |

### 10.3 Database Tables

| Table | Purpose | Key Fields |
|-------|---------|------------|
| `AspNetUsers` | User accounts | `Id`, `Email`, `UserName`, `PasswordHash` |
| `AspNetUserLogins` | OAuth provider links | `UserId`, `LoginProvider`, `ProviderKey` |

---

**End of OAuth Integration Documentation**
