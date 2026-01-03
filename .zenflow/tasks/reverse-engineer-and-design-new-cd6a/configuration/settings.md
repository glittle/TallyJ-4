# TallyJ Configuration Settings Documentation

## 1. Overview

### 1.1 Configuration File Locations
- **Primary Configuration**: `Web.config` (ASP.NET Framework configuration)
- **External AppSettings**: `App_Data/AppSettings.config` (referenced from Web.config line 32)
- **Sample Configuration**: `AppDataSample/AppSettings.config` (template for deployment)
- **Connection Strings**: Configured at IIS/machine level (MainConnection3)

### 1.2 Environment-Specific Configurations
- **Development**: Uses local database, local StateServer, debug mode enabled
- **Production**: Uses production database, production StateServer, debug mode disabled, HTTPS enforced

### 1.3 External Configuration Files
The `Web.config` references an external file for sensitive settings:
```xml
<appSettings file="..\AppSettings.config">
```

This pattern allows separating sensitive configuration (OAuth keys, API tokens, connection strings) from the main configuration file.

### 1.4 Source Code Accessibility
**Status**: ✅ **ACCESSIBLE** - Source code at `C:\Dev\TallyJ\v3\Site\Web.config` was successfully accessed and analyzed.

---

## 2. Connection Strings

### 2.1 MainConnection3 (Primary Database Connection)
**Location**: Configured at IIS/machine level (not in Web.config)

**Format Options**:

**Option A - Local SQL Server Express (Development)**:
```
Connect Timeout=120; Data Source=.\SQLEXPRESS; Database=TallyJ3; Integrated Security=True; Min Pool Size=1; MultipleActiveResultSets=True
```

**Option B - Azure SQL Database (Production)**:
```
Connect Timeout=30; Server=tcp:myAzureServer.database.windows.net,1433; Initial Catalog=MyTallyJ; Persist Security Info=False; User ID=myId; Password=myPw; Encrypt=True; TrustServerCertificate=False
```

**Provider**: `System.Data.SqlClient`

**Usage**:
- Entity Framework data context
- ASP.NET Membership Provider
- ASP.NET Profile Provider

---

## 3. AppSettings

### 3.1 Environment Configuration

#### Application Version
```xml
<TallyJ.Properties.Settings>
  <setting name="VersionNum" serializeAs="String">
    <value>3.5.28</value>
  </setting>
  <setting name="VersionDate" serializeAs="String">
    <value>4 April 2024 / 17 Splendor 181</value>
  </setting>
</TallyJ.Properties.Settings>
```

#### Environment Settings (AppSettings.config)
| Key | Value | Purpose |
|-----|-------|---------|
| `Environment` | `Dev` | Environment identifier (Dev/Prod) |
| `HostSite` | `https://webserver/pathToTallyJ` | Public URL for email links |
| `UseProductionFiles` | `false` | Use production static files |
| `secure` | `true` | Enable HTTPS enforcement and secure cookies |

#### Online Election Support
| Key | Value | Purpose |
|-----|-------|---------|
| `SupportOnlineElections` | `true` | Enable online voting features |
| `EncryptionKeysFolder` | `C:\TallyJ3-Keys` | Storage location for ballot encryption keys |

#### Phone Login Support (SMS/Voice/WhatsApp)
| Key | Value | Purpose |
|-----|-------|---------|
| `SupportOnlineSmsLogin` | `true` | Enable SMS/Voice authentication |
| `SmsAvailable` | `true` | Enable SMS delivery |
| `VoiceAvailable` | `true` | Enable voice call delivery |
| `UserAttemptMax` | `10` | Max SMS/Voice attempts per 15 minutes |
| `SupportOnlineWhatsAppLogin` | `true` | Enable WhatsApp authentication |

### 3.2 Logging Configuration

#### IFTTT Webhook Integration
```xml
<add key="iftttKey" value="cGJ7Y8OhmAwE2B1aq0V-nk" />
```
**Purpose**: High-level activity logging via IFTTT webhooks

#### LogEntries Integration
| Key | Value | Purpose |
|-----|-------|---------|
| `LOGENTRIES_ACCOUNT_KEY` | `3936024A-7709-4FAA-9D24-24F7FF933AEE` | LogEntries account identifier (placeholder) |
| `LOGENTRIES_TOKEN` | *(empty)* | LogEntries ingestion token |
| `LOGENTRIES_LOCATION` | *(empty)* | LogEntries region/location |

**Note**: AppHarbor automatically injects correct LOGENTRIES_ACCOUNT_KEY value at runtime.

### 3.3 OAuth Configuration

**Storage**: OAuth credentials stored in external `AppSettings.config` file (not in Web.config)

**Referenced Keys** (from authentication.md):
- `FacebookAppId`: Facebook OAuth App ID
- `FacebookAppSecret`: Facebook OAuth App Secret
- `GoogleClientId`: Google OAuth Client ID
- `GoogleClientSecret`: Google OAuth Client Secret

### 3.4 Twilio SMS Integration

| Key | Value | Purpose |
|-----|-------|---------|
| `twilio-SID` | `[your SID]` | Twilio Account SID |
| `twilio-Token` | `[your Token]` | Twilio Auth Token |
| `twilio-MessagingSid` | `[your Twilio Messaging Service SID]` | Messaging Service SID |
| `twilio-FromNumber` | `[your Twilio phone number]` | SMS sender phone number (optional) |
| `twilio-WhatsAppFromNumber` | `[your Twilio WhatsApp phone number]` | WhatsApp sender number |
| `twilio-CallbackUrl` | `[path to your server]/Public/SmsStatus` | Status callback URL |

**Note**: Callback URL must be publicly accessible for Twilio webhooks.

### 3.5 Email Configuration

#### SendGrid API (Preferred Method)
```xml
<add key="SendGridApiKey" value="[your SendGrid API key]" />
```

#### SMTP Settings (Alternative Method)
| Key | Value | Purpose |
|-----|-------|---------|
| `SmtpUsername` | `[your smtp user name]` | SMTP authentication username |
| `SmtpPassword` | `[your smtp password]` | SMTP authentication password |
| `SmtpHost` | `[your smtp host]` | SMTP server hostname |
| `SmtpPort` | `25` | SMTP port (25/587/465) |
| `SmtpSecure` | `false` | Use SSL/TLS |
| `SmtpDefaultFromAddress` | `noreply@tallyj.com` | Default sender email |
| `SmtpDefaultFromName` | `TallyJ System` | Default sender name |

#### Email Testing (Development)
```xml
<add key="SmtpPickupDirectory" value="D:\Temp\Emails" />
```
**Purpose**: Write emails to local directory instead of sending (does not work with SendGrid API)

### 3.6 Technical Support Configuration

| Key | Value | Purpose |
|-----|-------|---------|
| `TechSupportContactName` | `Glen Little` | Displayed to users for support |
| `TechSupportContactEmail` | `glen.little+tallyj@gmail.com` | Support email address |
| `TawkToAccount` | `6071235af7ce18270938eb61/1f2sv4688` | Tawk.to chat widget ID |

### 3.7 Security Configuration

```xml
<add key="XsrfValue" value="wegws662342$242" />
```
**Purpose**: CSRF/XSRF protection token

### 3.8 ASP.NET MVC Configuration

| Key | Value | Purpose |
|-----|-------|---------|
| `webpages:Version` | `3.0.0.0` | Razor pages version |
| `PreserveLoginUrl` | `true` | Maintain login URL after authentication |
| `ClientValidationEnabled` | `true` | Enable client-side validation |
| `UnobtrusiveJavaScriptEnabled` | `true` | Use unobtrusive JavaScript |

### 3.9 OWIN Configuration

```xml
<add key="owin:AutomaticAppStartup" value="true" />
```
**Purpose**: Enable automatic OWIN middleware initialization

### 3.10 ASP.NET Data Protection

```xml
<add key="aspnet:dataProtectionStartupType" value="" />
```
**Purpose**: Configure ASP.NET Core Data Protection compatibility (empty = use default)

---

## 4. System.Web Configuration

### 4.1 Compilation Settings
```xml
<compilation debug="true" targetFramework="4.8" />
```

| Setting | Value | Purpose |
|---------|-------|---------|
| `debug` | `true` | Enable debug mode (should be `false` in production) |
| `targetFramework` | `4.8` | .NET Framework version |

### 4.2 HTTP Runtime
```xml
<httpRuntime maxRequestLength="9999999" targetFramework="4.5" />
```

| Setting | Value | Purpose |
|---------|-------|---------|
| `maxRequestLength` | `9999999` | Max request size in kilobytes (~9.5 GB) |
| `targetFramework` | `4.5` | HTTP runtime target framework |

**Note**: Large maxRequestLength suggests support for large file uploads (possibly ballot data or results exports).

### 4.3 Custom Errors
```xml
<customErrors mode="RemoteOnly" />
```

**Modes**:
- `RemoteOnly`: Show detailed errors locally, generic errors remotely
- `On`: Always show generic errors
- `Off`: Always show detailed errors

### 4.4 Authentication

#### Primary Authentication Mode
```xml
<authentication mode="Windows" />
```

**Note**: Commented out Forms authentication exists:
```xml
<!--<authentication mode="Forms">
  <forms loginUrl="~/" timeout="120" />
</authentication>-->
```

Actual authentication is handled by **OWIN Cookie Authentication** (see Section 9).

#### Authorization
```xml
<authorization>
  <allow users="*" />
</authorization>
```
**Default**: Allow all users (authorization enforced at controller/action level)

### 4.5 Session State

```xml
<sessionState mode="StateServer" cookieless="false" stateConnectionString="tcpip=localhost:42424" timeout="360">
</sessionState>
```

| Setting | Value | Purpose |
|---------|-------|---------|
| `mode` | `StateServer` | Use out-of-process session state server |
| `cookieless` | `false` | Use cookies for session tracking |
| `stateConnectionString` | `tcpip=localhost:42424` | State server address |
| `timeout` | `360` | Session timeout in minutes (6 hours) |

**Why StateServer?**
- Supports multiple web server instances (load balancing)
- Session survives application pool restarts
- Required for cloud deployments with multiple instances

**State Server Setup**:
- Windows: ASP.NET State Service must be running
- Port: 42424 (default)
- Protocol: TCP

### 4.6 HTTP Cookies
```xml
<httpCookies httpOnlyCookies="true" requireSSL="true" />
```

| Setting | Value | Purpose |
|---------|-------|---------|
| `httpOnlyCookies` | `true` | Prevent JavaScript access to cookies (XSS protection) |
| `requireSSL` | `true` | Require HTTPS for cookies (production) |

### 4.7 MachineKey (Data Protection)
```xml
<machineKey 
  compatibilityMode="Framework45" 
  dataProtectorType="Microsoft.AspNetCore.DataProtection.SystemWeb.CompatibilityDataProtector, Microsoft.AspNetCore.DataProtection.SystemWeb" 
/>
```

**Purpose**: Uses ASP.NET Core Data Protection for encryption/decryption (cross-platform compatibility)

### 4.8 Profile Provider
```xml
<profile defaultProvider="DefaultProfileProvider">
  <providers>
    <add name="DefaultProfileProvider" 
         type="System.Web.Providers.DefaultProfileProvider, System.Web.Providers, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" 
         connectionStringName="MainConnection3" 
         applicationName="/" />
  </providers>
</profile>
```

**Provider**: SQL Server-based profile storage

### 4.9 Membership Provider
```xml
<membership defaultProvider="DefaultMembershipProvider">
  <providers>
    <add name="DefaultMembershipProvider" 
         type="System.Web.Providers.DefaultMembershipProvider, System.Web.Providers, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" 
         connectionStringName="MainConnection3" 
         enablePasswordRetrieval="false" 
         enablePasswordReset="true" 
         requiresQuestionAndAnswer="false" 
         requiresUniqueEmail="false" 
         maxInvalidPasswordAttempts="5" 
         minRequiredPasswordLength="6" 
         minRequiredNonalphanumericCharacters="0" 
         passwordAttemptWindow="10" 
         applicationName="/" />
  </providers>
</membership>
```

| Setting | Value | Purpose |
|---------|-------|---------|
| `enablePasswordRetrieval` | `false` | Cannot retrieve passwords (hashed) |
| `enablePasswordReset` | `true` | Allow password reset |
| `requiresQuestionAndAnswer` | `false` | No security questions required |
| `requiresUniqueEmail` | `false` | Multiple accounts can share email |
| `maxInvalidPasswordAttempts` | `5` | Lock account after 5 failed attempts |
| `minRequiredPasswordLength` | `6` | Minimum password length |
| `minRequiredNonalphanumericCharacters` | `0` | No special character requirement |
| `passwordAttemptWindow` | `10` | 10-minute window for counting failed attempts |

**Note**: Uses SQL Server database (MainConnection3) for user storage.

### 4.10 Role Manager (Commented Out)
```xml
<!--<roleManager enabled="true" defaultProvider="DefaultRoleProvider">
  <providers>
    <add name="DefaultRoleProvider" 
         type="System.Web.Providers.DefaultRoleProvider, System.Web.Providers, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" 
         connectionStringName="MainConnection3" 
         applicationName="/" />
  </providers>
</roleManager>-->
```

**Note**: Role management is disabled. Authorization uses custom claims-based approach (see authentication.md).

---

## 5. System.WebServer Configuration

### 5.1 Modules
```xml
<modules>
  <remove name="UrlRoutingModule-4.0" />
  <add name="UrlRoutingModule-4.0" type="System.Web.Routing.UrlRoutingModule" preCondition="" />
  <remove name="Session" />
  <add name="Session" type="System.Web.SessionState.SessionStateModule" preCondition="" />
</modules>
```

**Purpose**: Configure IIS modules for routing and session management.

### 5.2 Static Content Caching
```xml
<staticContent>
  <clientCache cacheControlCustom="public" cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
  <remove fileExtension=".json" />
  <mimeMap fileExtension=".json" mimeType="application/json" />
</staticContent>
```

| Setting | Value | Purpose |
|---------|-------|---------|
| `cacheControlCustom` | `public` | Allow public caching |
| `cacheControlMode` | `UseMaxAge` | Use max-age directive |
| `cacheControlMaxAge` | `7.00:00:00` | Cache for 7 days |

**JSON MIME Type**: Explicitly configured to ensure proper content type for JSON files.

### 5.3 Security Headers
```xml
<httpProtocol>
  <customHeaders>
    <remove name="X-Powered-By" />
    <add name="X-XSS-Protection" value="1; mode=block" />
    <add name="X-Content-Type-Options" value="nosniff" />
    <add name="Referrer-Policy" value="no-referrer-when-downgrade" />
    <add name="Content-Security-Policy" value="script-src 'self' 'unsafe-inline' 'unsafe-eval' www.googletagmanager.com cdn.jsdelivr.net cdnjs.cloudflare.com localhost:44399 *.tawk.to" />
    <add name="X-Content-Security-Policy" value="script-src 'self' 'unsafe-inline' 'unsafe-eval' www.googletagmanager.com cdn.jsdelivr.net cdnjs.cloudflare.com *.tawk.to" />
    <add name="X-WebKit-CSP" value="script-src 'self' 'unsafe-inline' 'unsafe-eval' www.googletagmanager.com cdn.jsdelivr.net cdnjs.cloudflare.com *.tawk.to" />
    <add name="X-Frame-Options" value="SAMEORIGIN" />
    <add name="Permissions-Policy" value="fullscreen=()" />
  </customHeaders>
</httpProtocol>
```

#### Security Headers Breakdown

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Powered-By` | *(removed)* | Hide server technology (security through obscurity) |
| `X-XSS-Protection` | `1; mode=block` | Enable browser XSS filter |
| `X-Content-Type-Options` | `nosniff` | Prevent MIME type sniffing |
| `Referrer-Policy` | `no-referrer-when-downgrade` | Send referrer only on HTTPS→HTTPS |
| `Content-Security-Policy` | *(see below)* | Control resource loading sources |
| `X-Frame-Options` | `SAMEORIGIN` | Prevent clickjacking (only allow same-origin frames) |
| `Permissions-Policy` | `fullscreen=()` | Disable fullscreen permission |

#### Content Security Policy (CSP)
**Allowed Script Sources**:
- `'self'`: Same origin
- `'unsafe-inline'`: Inline scripts (required for legacy code)
- `'unsafe-eval'`: `eval()` and similar (required for some frameworks)
- `www.googletagmanager.com`: Google Analytics
- `cdn.jsdelivr.net`: CDN for libraries
- `cdnjs.cloudflare.com`: CDN for libraries
- `localhost:44399`: Local development
- `*.tawk.to`: Tawk.to chat widget

**Note**: `'unsafe-inline'` and `'unsafe-eval'` reduce CSP effectiveness. Consider removing in migration.

### 5.4 Request Filtering
```xml
<security>
  <requestFiltering>
    <fileExtensions>
      <remove fileExtension=".json" />
    </fileExtensions>
  </requestFiltering>
</security>
```

**Purpose**: Allow `.json` file requests (normally blocked by IIS default settings).

### 5.5 Location-Based Authorization

#### Public Access (Allow Anonymous)
```xml
<location path="Content">
  <system.web><authorization><allow users="*" /></authorization></system.web>
</location>
<location path="Images">
  <system.web><authorization><allow users="*" /></authorization></system.web>
</location>
<location path="Scripts">
  <system.web><authorization><allow users="*" /></authorization></system.web>
</location>
<location path="Public/SmsStatus">
  <system.web><authorization><allow users="*" /></authorization></system.web>
</location>
<location path="Download/open">
  <system.web><authorization><allow users="*" /></authorization></system.web>
</location>
```

#### Authenticated Access Only (Deny Anonymous)
```xml
<location path="Setup">
  <system.web><authorization><deny users="?" /></authorization></system.web>
</location>
<location path="After">
  <system.web><authorization><deny users="?" /></authorization></system.web>
</location>
<location path="Ballots">
  <system.web><authorization><deny users="?" /></authorization></system.web>
</location>
<location path="Before">
  <system.web><authorization><deny users="?" /></authorization></system.web>
</location>
<location path="Dashboard">
  <system.web><authorization><deny users="?" /></authorization></system.web>
</location>
```

#### Mixed Access (Allow Only Anonymous)
```xml
<location path="Download">
  <system.web><authorization><allow users="?" /></authorization></system.web>
</location>
```

**Notes**:
- `*` = All users (authenticated and anonymous)
- `?` = Anonymous users only
- `deny users="?"` = Deny anonymous (require authentication)

---

## 6. Unity Dependency Injection

### 6.1 Configuration Section
```xml
<section name="unity" type="Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration" />
```

### 6.2 Container Registrations
```xml
<unity xmlns="http://schemas.microsoft.com/practices/2010/unity">
  <container>
    <register type="IDbContextFactory" mapTo="DbContextFactory">
      <lifetime type="PerWebRequest" />
    </register>
    <register type="IViewResourcesHelper" mapTo="ViewResourcesHelper">
      <lifetime type="PerWebRequest" />
    </register>
    <register type="IViewResourcesCache" mapTo="ViewResourcesCache">
      <lifetime type="PerWebRequest" />
    </register>
    <register type="ILinkedResourcesHelper" mapTo="LinkedResourcesHelper">
      <lifetime type="PerWebRequest" />
    </register>
    <register type="IPolicyViolationHandler" mapTo="RequireElectionPolicyViolationHandler">
      <lifetime type="PerWebRequest" />
    </register>
    <register type="ISecurityHandler" mapTo="SecurityServiceLocator">
      <lifetime type="PerWebRequest" />
    </register>
  </container>
</unity>
```

### 6.3 Type Registrations

| Interface | Implementation | Lifetime | Purpose |
|-----------|----------------|----------|---------|
| `IDbContextFactory` | `DbContextFactory` | PerWebRequest | Entity Framework context factory |
| `IViewResourcesHelper` | `ViewResourcesHelper` | PerWebRequest | View resource management |
| `IViewResourcesCache` | `ViewResourcesCache` | PerWebRequest | Resource caching |
| `ILinkedResourcesHelper` | `LinkedResourcesHelper` | PerWebRequest | Linked resource management |
| `IPolicyViolationHandler` | `RequireElectionPolicyViolationHandler` | PerWebRequest | Authorization policy violations |
| `ISecurityHandler` | `SecurityServiceLocator` | PerWebRequest | Security services locator |

**Lifetime**: `PerWebRequest` - New instance per HTTP request, disposed at end of request.

---

## 7. Entity Framework Configuration

```xml
<section name="entityFramework" type="System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false" />
```

### 7.1 Entity Framework Settings (from Web.config)
```xml
<entityFramework>
  <defaultConnectionFactory type="System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework">
    <parameters>
      <parameter value="Data Source=.\SQLEXPRESS; Integrated Security=True; MultipleActiveResultSets=True" />
    </parameters>
  </defaultConnectionFactory>
  <providers>
    <provider invariantName="System.Data.SqlClient" type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" />
  </providers>
</entityFramework>
```

**Version**: Entity Framework 6.0

**Provider**: SQL Server

**Default Connection**: Local SQL Server Express (development)

---

## 8. SMTP Configuration (Legacy, Commented Out)

**Note**: SMTP configuration is now handled via `AppSettings.config` (see Section 3.5).

---

## 9. OWIN Configuration

OWIN configuration is defined in code (not Web.config). Based on authentication.md:

### 9.1 Cookie Authentication
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

### 9.2 OAuth Middleware
- Google OAuth
- Facebook OAuth

**Configuration**: Uses keys from `AppSettings.config`

---

## 10. Assembly Binding Redirects (Runtime)

### 10.1 Key Assembly Redirects
```xml
<runtime>
  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    <!-- NLog -->
    <dependentAssembly>
      <assemblyIdentity name="NLog" publicKeyToken="5120E14C03D0593C" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-4.0.0.0" newVersion="4.0.0.0" />
    </dependentAssembly>
    
    <!-- Entity Framework -->
    <dependentAssembly>
      <assemblyIdentity name="EntityFramework" publicKeyToken="b77a5c561934e089" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0" />
    </dependentAssembly>
    
    <!-- Unity -->
    <dependentAssembly>
      <assemblyIdentity name="Microsoft.Practices.Unity" publicKeyToken="31bf3856ad364e35" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-3.5.0.0" newVersion="3.5.0.0" />
    </dependentAssembly>
    
    <!-- SignalR -->
    <dependentAssembly>
      <assemblyIdentity name="Microsoft.AspNet.SignalR.Core" publicKeyToken="31bf3856ad364e35" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-2.0.0.0" newVersion="2.0.0.0" />
    </dependentAssembly>
    
    <!-- OWIN -->
    <dependentAssembly>
      <assemblyIdentity name="Microsoft.Owin" publicKeyToken="31bf3856ad364e35" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-4.2.2.0" newVersion="4.2.2.0" />
    </dependentAssembly>
    
    <!-- Newtonsoft.Json -->
    <dependentAssembly>
      <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
    </dependentAssembly>
    
    <!-- Microsoft.IdentityModel.* -->
    <dependentAssembly>
      <assemblyIdentity name="System.IdentityModel.Tokens.Jwt" publicKeyToken="31bf3856ad364e35" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-7.0.3.0" newVersion="7.0.3.0" />
    </dependentAssembly>
    
    <!-- ASP.NET Core Compatibility -->
    <dependentAssembly>
      <assemblyIdentity name="Microsoft.AspNetCore.DataProtection.Abstractions" publicKeyToken="adb9793829ddae60" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-7.0.13.0" newVersion="7.0.13.0" />
    </dependentAssembly>
  </assemblyBinding>
</runtime>
```

**Purpose**: Resolve version conflicts when multiple packages reference different versions of the same assembly.

---

## 11. Code Compilation Settings

```xml
<system.codedom>
  <compilers>
    <compiler extension=".cs" language="c#;cs;csharp" warningLevel="4" 
              compilerOptions="/langversion:7.3 /nowarn:1659;1699;1701;612;618" 
              type="Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Version=4.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" />
  </compilers>
</system.codedom>
```

**C# Language Version**: 7.3

**Compiler Warnings Suppressed**:
- `1659`: XML comments
- `1699`: Command line option
- `1701`: Assembly version mismatch
- `612`: Obsolete member
- `618`: Obsolete member with message

---

## 12. .NET Core Migration Mapping

### 12.1 Web.config → appsettings.json Structure

#### Current (ASP.NET Framework - Web.config)
```xml
<configuration>
  <connectionStrings>
    <add name="MainConnection3" connectionString="..." />
  </connectionStrings>
  <appSettings>
    <add key="Environment" value="Dev" />
    <add key="HostSite" value="https://..." />
  </appSettings>
</configuration>
```

#### Target (.NET Core - appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "TallyJ": {
    "Environment": "Dev",
    "HostSite": "https://...",
    "Logging": {
      "IftttKey": "...",
      "LogEntries": {
        "AccountKey": "...",
        "Token": "...",
        "Location": "..."
      }
    },
    "OAuth": {
      "Google": {
        "ClientId": "...",
        "ClientSecret": "..."
      },
      "Facebook": {
        "AppId": "...",
        "AppSecret": "..."
      }
    },
    "Twilio": {
      "AccountSid": "...",
      "AuthToken": "...",
      "MessagingServiceSid": "...",
      "FromNumber": "...",
      "WhatsAppFromNumber": "...",
      "CallbackUrl": "..."
    },
    "Email": {
      "SendGridApiKey": "...",
      "Smtp": {
        "Host": "...",
        "Port": 587,
        "Username": "...",
        "Password": "...",
        "Secure": true,
        "DefaultFromAddress": "...",
        "DefaultFromName": "..."
      }
    },
    "Security": {
      "XsrfValue": "...",
      "EncryptionKeysFolder": "..."
    },
    "Features": {
      "SupportOnlineElections": true,
      "SupportOnlineSmsLogin": true,
      "SmsAvailable": true,
      "VoiceAvailable": true,
      "SupportOnlineWhatsAppLogin": true,
      "UserAttemptMax": 10
    },
    "Support": {
      "ContactName": "...",
      "ContactEmail": "...",
      "TawkToAccount": "..."
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 12.2 Environment-Specific Configuration

#### Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.\\SQLEXPRESS;Database=TallyJ3;Integrated Security=True;MultipleActiveResultSets=True"
  },
  "TallyJ": {
    "Environment": "Dev",
    "HostSite": "https://localhost:7001",
    "Security": {
      "RequireHttps": false
    },
    "Email": {
      "Smtp": {
        "PickupDirectory": "D:\\Temp\\Emails"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:prodserver.database.windows.net,1433;Initial Catalog=TallyJ;User ID=...;Password=...;Encrypt=True;"
  },
  "TallyJ": {
    "Environment": "Prod",
    "HostSite": "https://www.tallyj.com",
    "Security": {
      "RequireHttps": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  }
}
```

### 12.3 Configuration Access in .NET Core

#### Reading Configuration
```csharp
public class MyService
{
    private readonly IConfiguration _configuration;
    
    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void DoSomething()
    {
        // Direct access
        var hostSite = _configuration["TallyJ:HostSite"];
        
        // Connection string
        var connString = _configuration.GetConnectionString("DefaultConnection");
    }
}
```

#### Options Pattern (Recommended)
```csharp
// Options class
public class TallyJOptions
{
    public string Environment { get; set; }
    public string HostSite { get; set; }
    public OAuthOptions OAuth { get; set; }
    public TwilioOptions Twilio { get; set; }
    public EmailOptions Email { get; set; }
    // ... other nested options
}

// Registration in Program.cs
builder.Services.Configure<TallyJOptions>(
    builder.Configuration.GetSection("TallyJ"));

// Usage in services
public class MyService
{
    private readonly TallyJOptions _options;
    
    public MyService(IOptions<TallyJOptions> options)
    {
        _options = options.Value;
    }
    
    public void DoSomething()
    {
        var hostSite = _options.HostSite;
        var twilioSid = _options.Twilio.AccountSid;
    }
}
```

### 12.4 Dependency Injection Migration (Unity → ServiceCollection)

#### Current (Unity - Web.config)
```xml
<register type="IDbContextFactory" mapTo="DbContextFactory">
  <lifetime type="PerWebRequest" />
</register>
```

#### Target (.NET Core - Program.cs)
```csharp
// Program.cs
builder.Services.AddScoped<IDbContextFactory, DbContextFactory>();
builder.Services.AddScoped<IViewResourcesHelper, ViewResourcesHelper>();
builder.Services.AddScoped<IViewResourcesCache, ViewResourcesCache>();
builder.Services.AddScoped<ILinkedResourcesHelper, LinkedResourcesHelper>();
builder.Services.AddScoped<IPolicyViolationHandler, RequireElectionPolicyViolationHandler>();
builder.Services.AddScoped<ISecurityHandler, SecurityServiceLocator>();
```

**Lifetime Mapping**:
- Unity `PerWebRequest` → .NET Core `AddScoped`
- Unity `Transient` → .NET Core `AddTransient`
- Unity `Singleton` → .NET Core `AddSingleton`

### 12.5 Session State Migration

#### Current (StateServer)
```xml
<sessionState mode="StateServer" stateConnectionString="tcpip=localhost:42424" timeout="360" />
```

#### Option A: Distributed Cache (Redis)
```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "TallyJ_";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(6);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Middleware
app.UseSession();
```

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

#### Option B: SQL Server Distributed Cache
```csharp
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.SchemaName = "dbo";
    options.TableName = "SessionState";
});
```

#### Option C: Stateless JWT (No Session State)
**Recommended for modern cloud-native applications**:
- Replace session storage with JWT tokens
- Store minimal state in encrypted JWT claims
- Use short-lived access tokens + refresh tokens

### 12.6 Authentication Migration

#### Current (OWIN Cookie + OAuth)
- OWIN Cookie Authentication
- Google OAuth 2.0 (OWIN middleware)
- Facebook OAuth (OWIN middleware)
- ASP.NET Membership Provider

#### Target (.NET Core)
```csharp
// Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["TallyJ:OAuth:Google:ClientId"];
    options.ClientSecret = builder.Configuration["TallyJ:OAuth:Google:ClientSecret"];
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["TallyJ:OAuth:Facebook:AppId"];
    options.AppSecret = builder.Configuration["TallyJ:OAuth:Facebook:AppSecret"];
});

// Replace ASP.NET Membership with ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```

### 12.7 Entity Framework Migration

#### Current (EF6)
```csharp
public class TallyJ3Entities : DbContext
{
    public TallyJ3Entities() 
        : base("name=MainConnection3")
    {
    }
}
```

#### Target (EF Core)
```csharp
// Program.cs
builder.Services.AddDbContext<TallyJ3Context>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// Context
public class TallyJ3Context : DbContext
{
    public TallyJ3Context(DbContextOptions<TallyJ3Context> options)
        : base(options)
    {
    }
    
    // DbSets...
}
```

### 12.8 Security Headers Migration

#### Current (Web.config)
```xml
<httpProtocol>
  <customHeaders>
    <add name="X-XSS-Protection" value="1; mode=block" />
    <!-- etc. -->
  </customHeaders>
</httpProtocol>
```

#### Target (ASP.NET Core Middleware)
```csharp
// Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer-when-downgrade");
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Add("Permissions-Policy", "fullscreen=()");
    
    // CSP (consider using NWebsec package for better CSP management)
    context.Response.Headers.Add("Content-Security-Policy", 
        "script-src 'self' www.googletagmanager.com cdn.jsdelivr.net cdnjs.cloudflare.com *.tawk.to");
    
    await next();
});
```

**Recommended Package**: Use `NWebsec.AspNetCore.Middleware` for comprehensive security header management.

---

## 13. Security Considerations

### 13.1 Secret Management

#### Development Environment
**Option 1: User Secrets**
```bash
dotnet user-secrets init
dotnet user-secrets set "TallyJ:OAuth:Google:ClientSecret" "actual_secret"
dotnet user-secrets set "TallyJ:Twilio:AuthToken" "actual_token"
```

**Option 2: Environment Variables**
```bash
export TallyJ__OAuth__Google__ClientSecret="actual_secret"
export TallyJ__Twilio__AuthToken="actual_token"
```

#### Production Environment
**Option 1: Azure Key Vault**
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

**Option 2: AWS Secrets Manager**
```csharp
builder.Configuration.AddSecretsManager();
```

**Option 3: Environment Variables (Docker/Kubernetes)**
```yaml
# docker-compose.yml
environment:
  - TallyJ__OAuth__Google__ClientSecret=${GOOGLE_SECRET}
  - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
```

### 13.2 Connection String Security

#### Current Risk
Connection strings in IIS machine-level configuration can be exposed if server is compromised.

#### Mitigation Strategies
1. **Azure SQL**: Use Managed Identity (no credentials in connection string)
   ```csharp
   var credential = new DefaultAzureCredential();
   var token = await credential.GetTokenAsync(
       new TokenRequestContext(new[] { "https://database.windows.net/.default" }));
   ```

2. **Encrypt Connection Strings**: Use Data Protection API
   ```csharp
   builder.Services.AddDataProtection()
       .PersistKeysToAzureBlobStorage(...)
       .ProtectKeysWithAzureKeyVault(...);
   ```

3. **Rotate Credentials**: Implement automated credential rotation

### 13.3 OAuth Credential Storage

#### Current Issues
- Credentials in plain text file (`AppSettings.config`)
- File-based secrets difficult to rotate
- No audit trail for secret access

#### Recommendations
1. Use Azure Key Vault or AWS Secrets Manager
2. Implement secret rotation (30-90 days)
3. Monitor secret access via audit logs
4. Use separate OAuth apps per environment (Dev/Staging/Prod)

### 13.4 XSRF/CSRF Protection

#### Current Implementation
```xml
<add key="XsrfValue" value="wegws662342$242" />
```

#### .NET Core Migration
```csharp
// Program.cs
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Controllers
[ValidateAntiForgeryToken]
public class MyController : Controller
{
    // ...
}
```

### 13.5 Encryption Keys

#### Current (Online Ballot Encryption)
```xml
<add key="EncryptionKeysFolder" value="C:\TallyJ3-Keys" />
```

#### Migration Strategy
1. **Development**: Store in User Secrets or local file
2. **Production**: Store in Azure Key Vault
3. **Key Rotation**: Implement versioned keys
4. **Backup**: Automated backup to secure storage

**Recommendation**: Use ASP.NET Core Data Protection for key management:
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(new Uri("https://..."))
    .ProtectKeysWithAzureKeyVault(new Uri("https://..."));
```

---

## 14. Configuration Migration Checklist

### 14.1 Pre-Migration
- [ ] Document all configuration keys and their usage
- [ ] Identify environment-specific vs. shared settings
- [ ] Locate all secret/credential values
- [ ] Review location-based authorization rules
- [ ] Map Unity DI registrations to ServiceCollection

### 14.2 Migration
- [ ] Create `appsettings.json` structure
- [ ] Create `appsettings.Development.json`
- [ ] Create `appsettings.Production.json`
- [ ] Set up User Secrets for development
- [ ] Configure Azure Key Vault for production
- [ ] Migrate connection strings
- [ ] Migrate session state to distributed cache
- [ ] Migrate authentication to ASP.NET Core Identity + OAuth
- [ ] Migrate DI container from Unity to ServiceCollection
- [ ] Migrate security headers to middleware
- [ ] Configure CORS policies (if needed for SPA)
- [ ] Set up health checks
- [ ] Configure logging (Serilog + LogEntries sink)

### 14.3 Post-Migration Verification
- [ ] Verify all configuration keys accessible
- [ ] Test environment-specific configuration loading
- [ ] Verify secret retrieval from Key Vault
- [ ] Test authentication flows (Admin, OAuth, SMS)
- [ ] Verify session state persistence
- [ ] Test DI container resolution
- [ ] Verify security headers present in responses
- [ ] Load test session state under concurrent load
- [ ] Verify encryption key management
- [ ] Test email sending (SendGrid/SMTP)
- [ ] Test SMS sending (Twilio)
- [ ] Verify logging to LogEntries

---

## 15. Known Limitations and Assumptions

### 15.1 Assumptions Made
1. **StateServer Location**: Assumed `localhost:42424` for development; production likely uses remote StateServer or Azure Cache for Redis
2. **OAuth Configuration**: OAuth keys not present in source Web.config; referenced from external `AppSettings.config` (not committed to source control)
3. **Database Schema**: Connection string references `TallyJ3` database; actual schema defined in Entity Framework migrations (see entities.md)
4. **Security Headers**: CSP allows `'unsafe-inline'` and `'unsafe-eval'`; should be removed in migration for better security
5. **Email Configuration**: Two options (SendGrid API preferred); SMTP as fallback

### 15.2 Configuration Not Found in Web.config
The following settings are referenced in existing documentation but not found in Web.config:
- **SignalR Configuration**: Likely configured in code (`Startup.cs` or `OwinStartup.cs`)
- **NLog Configuration**: Likely in separate `NLog.config` file
- **Route Configuration**: Defined in `RouteConfig.cs`

### 15.3 Production-Specific Configuration
The following likely differ in production (not documented in source Web.config):
- `debug="false"` in `<compilation>`
- Remote StateServer address (not `localhost:42424`)
- Actual LogEntries keys (AppHarbor injects at runtime)
- Production database connection string
- Actual OAuth keys
- Actual Twilio credentials
- Actual SendGrid API key

---

## 16. References

### 16.1 Related Documentation
- **Authentication Systems**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/security/authentication.md`
- **Authorization Model**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/security/authorization.md`
- **Database Entities**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/entities.md`
- **API Controllers**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/api/controllers/*.md`
- **SignalR Hubs**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/signalr/hubs-overview.md`

### 16.2 Source Files
- **Web.config**: `C:\Dev\TallyJ\v3\Site\Web.config` (430 lines)
- **AppSettings.config Sample**: `C:\Dev\TallyJ\v3\Site\AppDataSample\AppSettings.config` (154 lines)

### 16.3 Migration Resources
- [ASP.NET to ASP.NET Core Migration](https://learn.microsoft.com/en-us/aspnet/core/migration/proper-to-2x/)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [Entity Framework 6 to EF Core Migration](https://learn.microsoft.com/en-us/ef/efcore-and-ef6/porting/)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)

---

**Document Version**: 1.0  
**Created**: 2026-01-03  
**Last Updated**: 2026-01-03  
**Total Sections**: 16  
**Lines of Documentation**: ~1,450  
