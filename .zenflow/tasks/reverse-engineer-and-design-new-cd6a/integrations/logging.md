# TallyJ Logging Integrations Documentation

## 1. Overview

TallyJ integrates with **two external logging services** to provide comprehensive application monitoring, error tracking, and activity auditing:

1. **LogEntries** (Rapid7 InsightOps): Cloud-based centralized logging and log analysis
2. **IFTTT**: High-level activity notifications via webhook integration

### 1.1 Logging Architecture

```
Application Events
       |
       v
+----------------+
| LogHelper.cs   |  (Central logging facade)
+----------------+
       |
       +----> Local Logs (file system)
       |
       +----> Database (C_Log table)
       |
       +----> LogEntries (cloud logging)
       |
       +----> IFTTT (webhooks for important events)
```

### 1.2 Use Cases

**LogEntries**:
- Application errors and exceptions
- Performance monitoring
- User activity auditing
- API request/response logging
- Security events (login attempts, authorization failures)
- Database query logging

**IFTTT**:
- High-level election events (election created, voting opened/closed)
- Admin actions (configuration changes)
- System alerts (critical errors)
- Integration with external services (Slack, email, SMS)

---

## 2. LogEntries Integration

### 2.1 Configuration Settings

**Location**: `App_Data/AppSettings.config` (external file referenced by Web.config)

```xml
<!-- LogEntries Configuration -->
<add key="LOGENTRIES_ACCOUNT_KEY" value="3936024A-7709-4FAA-9D24-24F7FF933AEE" />
<add key="LOGENTRIES_TOKEN" value="" />
<add key="LOGENTRIES_LOCATION" value="" />
```

| Setting | Format | Purpose |
|---------|--------|---------|
| `LOGENTRIES_ACCOUNT_KEY` | GUID | LogEntries account identifier |
| `LOGENTRIES_TOKEN` | GUID or string | Log ingestion token (stream-specific) |
| `LOGENTRIES_LOCATION` | String | Region/datacenter location (e.g., `us`, `eu`) |

### 2.2 Obtaining LogEntries Credentials

**Steps**:
1. Sign up at https://www.rapid7.com/products/insightops/ (formerly LogEntries)
2. Navigate to Log Management → Add New Log
3. Choose "Manual" or "Token TCP" method
4. Copy token → `LOGENTRIES_TOKEN`
5. Account key is visible in Account Settings → API Keys
6. Select region (US, EU) → `LOGENTRIES_LOCATION`

**Pricing** (as of 2024):
- Free tier: 5 GB/month, 7-day retention
- Paid tiers: Starting at $89/month for 50 GB/month

### 2.3 LogEntries SDK Integration

**Library**: `logentries.core` or `NLog.Targets.LogEntries` (NuGet package)

**Configuration** (NLog.config):

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <extensions>
    <add assembly="NLog.Targets.Logentries"/>
  </extensions>

  <targets>
    <!-- LogEntries Target -->
    <target name="logentries" 
            xsi:type="Logentries" 
            token="${environment:LOGENTRIES_TOKEN}" 
            location="${environment:LOGENTRIES_LOCATION}"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=ToString}}" />
    
    <!-- Local File Target (fallback) -->
    <target name="file" 
            xsi:type="File" 
            fileName="${basedir}/logs/${shortdate}.log"
            layout="${longdate} ${uppercase:${level}} ${message}" />
  </targets>

  <rules>
    <logger name="*" minlevel="Info" writeTo="logentries" />
    <logger name="*" minlevel="Debug" writeTo="file" />
  </rules>
</nlog>
```

### 2.4 LogEntries Logging Code

**Code**: Estimated implementation using NLog

```csharp
public class LogHelper
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    
    public void Add(string message, bool isImportant, string userId = null)
    {
        try
        {
            var logLevel = isImportant ? LogLevel.Warn : LogLevel.Info;
            
            var logEvent = new LogEventInfo(logLevel, _logger.Name, message);
            
            // Add contextual properties
            if (!string.IsNullOrEmpty(userId))
            {
                logEvent.Properties["UserId"] = userId;
            }
            
            logEvent.Properties["SessionId"] = HttpContext.Current?.Session?.SessionID;
            logEvent.Properties["RequestUrl"] = HttpContext.Current?.Request?.Url?.ToString();
            logEvent.Properties["UserAgent"] = HttpContext.Current?.Request?.UserAgent;
            logEvent.Properties["IpAddress"] = GetClientIpAddress();
            logEvent.Properties["ElectionGuid"] = UserSession.CurrentElectionGuid;
            
            _logger.Log(logEvent);
            
            // Also log to database
            LogToDatabase(message, isImportant, userId);
        }
        catch (Exception ex)
        {
            // Fallback: Log to file if LogEntries fails
            System.Diagnostics.Trace.WriteLine($"Logging error: {ex.Message}");
        }
    }
    
    private string GetClientIpAddress()
    {
        var request = HttpContext.Current?.Request;
        if (request == null) return null;
        
        return request.ServerVariables["HTTP_X_FORWARDED_FOR"] 
            ?? request.ServerVariables["REMOTE_ADDR"];
    }
    
    private void LogToDatabase(string message, bool isImportant, string userId)
    {
        var db = new TallyJ3Entities();
        
        var logEntry = new C_Log
        {
            AsOf = DateTime.UtcNow,
            Message = message,
            Important = isImportant,
            UserId = userId,
            SessionId = HttpContext.Current?.Session?.SessionID,
            ElectionGuid = UserSession.CurrentElectionGuid
        };
        
        db.C_Log.Add(logEntry);
        db.SaveChanges();
    }
}
```

### 2.5 Log Levels and Categories

**Log Levels**:

| Level | When to Use | LogEntries | Database | IFTTT |
|-------|-------------|------------|----------|-------|
| **Debug** | Development/troubleshooting | ❌ | ✅ | ❌ |
| **Info** | Normal operations, user actions | ✅ | ✅ | ❌ |
| **Warn** | Important events, potential issues | ✅ | ✅ | ⚠️ (optional) |
| **Error** | Application errors, exceptions | ✅ | ✅ | ✅ |
| **Fatal** | Critical system failures | ✅ | ✅ | ✅ |

**Log Categories** (estimated based on usage):

| Category | Examples |
|----------|----------|
| **Authentication** | Login attempts, logout, OAuth, verification codes |
| **Authorization** | Permission checks, access denied |
| **Elections** | Election created/updated/deleted, voting opened/closed |
| **Ballots** | Ballot submitted, vote recorded, tally completed |
| **People** | Person added/updated/deleted, voter registered |
| **Results** | Results calculated, ties detected, results published |
| **SignalR** | Hub connections, disconnections, errors |
| **Database** | Query performance, deadlocks, connection errors |
| **External API** | Twilio calls, SendGrid emails, OAuth callbacks |
| **Performance** | Slow queries, high memory usage |

### 2.6 Structured Logging

**Best Practice**: Use structured logging for better searchability in LogEntries

```csharp
_logger.Info("Voter logged in", new 
{
    Action = "VoterLogin",
    VoterId = voterId,
    VoterIdType = voterIdType,
    ElectionGuid = electionGuid,
    LoginMethod = "email",
    Timestamp = DateTime.UtcNow
});
```

**LogEntries Query Example**:
```
where(Action = "VoterLogin" AND ElectionGuid = "abc-123-def-456") calculate(count)
```

### 2.7 LogEntries Features

**Available in LogEntries Dashboard**:
- ✅ Real-time log streaming
- ✅ Full-text search across all logs
- ✅ Advanced filtering (by level, category, custom fields)
- ✅ Alerts (email, webhook, Slack when specific events occur)
- ✅ Dashboards and visualizations
- ✅ Log retention policies
- ✅ Anomaly detection
- ✅ Log export (CSV, JSON)

---

## 3. IFTTT Integration

### 3.1 Configuration Settings

**Location**: `App_Data/AppSettings.config`

```xml
<!-- IFTTT Webhook Configuration -->
<add key="iftttKey" value="cGJ7Y8OhmAwE2B1aq0V-nk" />
```

| Setting | Format | Purpose |
|---------|--------|---------|
| `iftttKey` | Alphanumeric string | IFTTT webhook key for authentication |

### 3.2 Obtaining IFTTT Webhook Key

**Steps**:
1. Sign up at https://ifttt.com
2. Navigate to https://ifttt.com/maker_webhooks
3. Click "Documentation"
4. Copy your unique webhook key → `iftttKey`

**Example Webhook Key**: `cGJ7Y8OhmAwE2B1aq0V-nk`

### 3.3 IFTTT Webhook URL Format

**Webhook URL**:
```
https://maker.ifttt.com/trigger/{event_name}/with/key/{iftttKey}
```

**Example**:
```
https://maker.ifttt.com/trigger/election_created/with/key/cGJ7Y8OhmAwE2B1aq0V-nk
```

### 3.4 IFTTT Events Logged

**Estimated Events** (based on TallyJ functionality):

| Event Name | When Triggered | Payload |
|------------|----------------|---------|
| `election_created` | Admin creates new election | `value1`: Election name, `value2`: Election date |
| `voting_opened` | Online voting starts | `value1`: Election name, `value2`: Number of voters |
| `voting_closed` | Online voting ends | `value1`: Election name, `value2`: Ballots cast |
| `ballot_submitted` | Voter submits ballot | `value1`: Election name, `value2`: Voter ID (masked) |
| `tally_completed` | Election results calculated | `value1`: Election name, `value2`: Winner name(s) |
| `critical_error` | Fatal error occurs | `value1`: Error message, `value2`: Timestamp |
| `admin_login` | Admin logs in | `value1`: Username, `value2`: IP address |
| `election_deleted` | Election permanently deleted | `value1`: Election name, `value2`: Deleted by |

### 3.5 IFTTT Integration Code

**Code**: Estimated implementation

```csharp
public class IftttLogger
{
    private readonly string _webhookKey;
    private readonly HttpClient _httpClient;
    
    public IftttLogger()
    {
        _webhookKey = ConfigurationManager.AppSettings["iftttKey"];
        _httpClient = new HttpClient();
    }
    
    public async Task LogEventAsync(string eventName, string value1 = null, 
        string value2 = null, string value3 = null)
    {
        if (string.IsNullOrEmpty(_webhookKey))
        {
            return; // IFTTT not configured
        }
        
        try
        {
            var url = $"https://maker.ifttt.com/trigger/{eventName}/with/key/{_webhookKey}";
            
            var payload = new
            {
                value1 = value1,
                value2 = value2,
                value3 = value3
            };
            
            var content = new StringContent(
                JsonConvert.SerializeObject(payload), 
                Encoding.UTF8, 
                "application/json"
            );
            
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                new LogHelper().Add($"IFTTT event logged: {eventName}", false);
            }
            else
            {
                new LogHelper().Add($"IFTTT webhook failed: {response.StatusCode}", false);
            }
        }
        catch (Exception ex)
        {
            new LogHelper().Add($"IFTTT error: {ex.Message}", false);
        }
    }
}
```

### 3.6 Usage Examples

#### Example 1: Election Created
```csharp
public ActionResult CreateElection(Election model)
{
    // ... create election logic ...
    
    // Log to IFTTT
    var ifttt = new IftttLogger();
    await ifttt.LogEventAsync(
        "election_created",
        value1: model.ElectionName,
        value2: model.ElectionDate.ToString("yyyy-MM-dd")
    );
    
    return RedirectToAction("Index");
}
```

#### Example 2: Voting Opened
```csharp
public void OpenVoting(Guid electionGuid)
{
    var election = Db.Election.Find(electionGuid);
    election.OnlineWhenOpen = DateTime.UtcNow;
    Db.SaveChanges();
    
    // Log to IFTTT
    var voterCount = GetVoterCount(electionGuid);
    var ifttt = new IftttLogger();
    await ifttt.LogEventAsync(
        "voting_opened",
        value1: election.ElectionName,
        value2: voterCount.ToString()
    );
}
```

#### Example 3: Critical Error
```csharp
public void LogCriticalError(Exception ex)
{
    new LogHelper().Add($"FATAL ERROR: {ex.Message}", true);
    
    // Also notify via IFTTT
    var ifttt = new IftttLogger();
    await ifttt.LogEventAsync(
        "critical_error",
        value1: ex.Message.Truncate(100),
        value2: DateTime.UtcNow.ToString("o"),
        value3: ex.StackTrace?.Truncate(100)
    );
}
```

### 3.7 IFTTT Applets (Actions)

**Create Applets** to respond to webhook events:

#### Applet 1: Notify Slack when election created
- **IF**: Webhook event `election_created`
- **THEN**: Send message to Slack channel `#elections`
  - Message: `New election created: {{Value1}} on {{Value2}}`

#### Applet 2: Send email when voting opens
- **IF**: Webhook event `voting_opened`
- **THEN**: Send email to admin
  - Subject: `Voting opened: {{Value1}}`
  - Body: `{{Value2}} voters registered`

#### Applet 3: SMS alert for critical errors
- **IF**: Webhook event `critical_error`
- **THEN**: Send SMS to admin phone
  - Message: `TallyJ CRITICAL ERROR: {{Value1}}`

#### Applet 4: Log to Google Sheets
- **IF**: Webhook event (any)
- **THEN**: Add row to Google Sheets
  - Columns: Event Name, Value1, Value2, Value3, Timestamp

### 3.8 IFTTT Limitations

**Free Tier**:
- Max 2 applets
- Max 1 webhook per applet
- No premium integrations (Slack business, etc.)

**Pro Tier** ($2.50/month):
- Unlimited applets
- Multiple actions per trigger
- Faster execution
- Advanced filtering

**Webhook Limits**:
- Max 100 requests per minute
- Max 1,000 requests per hour
- No guaranteed delivery (best-effort)

---

## 4. Database Logging (C_Log Table)

### 4.1 C_Log Table Schema

**Purpose**: Local database logging for auditing and troubleshooting

**Schema**: See `database/entities.md` for full definition

**Key Fields**:

| Field | Type | Purpose |
|-------|------|---------|
| `C_LogId` | int (PK) | Unique identifier |
| `AsOf` | datetime | When event occurred |
| `Message` | nvarchar(MAX) | Log message |
| `Important` | bit | Flag for important events |
| `UserId` | nvarchar(128) | User who triggered event (if applicable) |
| `SessionId` | nvarchar(50) | HTTP session ID |
| `ElectionGuid` | uniqueidentifier | Related election (if applicable) |

### 4.2 Querying Logs

**Example Queries**:

```sql
-- Recent important events
SELECT TOP 100 * 
FROM C_Log 
WHERE Important = 1 
ORDER BY AsOf DESC;

-- Logs for specific election
SELECT * 
FROM C_Log 
WHERE ElectionGuid = 'abc-123-def-456' 
ORDER BY AsOf;

-- Error logs (estimated pattern)
SELECT * 
FROM C_Log 
WHERE Message LIKE '%error%' OR Message LIKE '%exception%' 
ORDER BY AsOf DESC;

-- User activity audit trail
SELECT * 
FROM C_Log 
WHERE UserId = 'admin@example.com' 
ORDER BY AsOf DESC;
```

### 4.3 Log Retention

**Current Implementation**: Logs stored indefinitely (no automatic cleanup)

**Recommendation**: Implement log archival/cleanup strategy:
- Keep recent logs (30-90 days) in main table
- Archive older logs to separate table or file storage
- Delete very old logs (1+ year) after compliance requirements met

---

## 5. .NET Core Migration Strategy

### 5.1 Technology Mapping

| ASP.NET Framework 4.8 | .NET Core 8 |
|----------------------|-------------|
| `NLog` | `Serilog` (recommended) or `NLog` (still supported) |
| `NLog.Targets.Logentries` | `Serilog.Sinks.Logentries` or `Serilog.Sinks.Rapid7InsightOps` |
| Custom IFTTT integration | Same (HTTP webhook calls) |
| `C_Log` table | Same (EF Core 8) |

**Why Serilog?**
- ✅ Better structured logging support
- ✅ Rich ecosystem of sinks (destinations)
- ✅ Better performance
- ✅ Native async support
- ✅ Better .NET Core integration

### 5.2 Code Migration Example

#### Current (ASP.NET Framework with NLog)
```csharp
public class LogHelper
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    
    public void Add(string message, bool isImportant, string userId = null)
    {
        var logLevel = isImportant ? LogLevel.Warn : LogLevel.Info;
        _logger.Log(logLevel, message);
    }
}
```

#### Target (.NET Core 8 with Serilog)
```csharp
public class LogService : ILogService
{
    private readonly ILogger<LogService> _logger;
    private readonly TallyJDbContext _dbContext;
    private readonly IftttService _iftttService;
    
    public LogService(
        ILogger<LogService> logger, 
        TallyJDbContext dbContext,
        IftttService iftttService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _iftttService = iftttService;
    }
    
    public async Task LogAsync(string message, bool isImportant, 
        string userId = null, Guid? electionGuid = null)
    {
        try
        {
            // Log to Serilog (which will send to LogEntries via sink)
            if (isImportant)
            {
                _logger.LogWarning("{Message} {@Context}", message, new
                {
                    UserId = userId,
                    ElectionGuid = electionGuid,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogInformation("{Message} {@Context}", message, new
                {
                    UserId = userId,
                    ElectionGuid = electionGuid,
                    Timestamp = DateTime.UtcNow
                });
            }
            
            // Log to database
            await LogToDatabaseAsync(message, isImportant, userId, electionGuid);
            
            // Log critical events to IFTTT
            if (isImportant && ShouldNotifyIfttt(message))
            {
                await _iftttService.LogEventAsync("important_event", message);
            }
        }
        catch (Exception ex)
        {
            // Fallback logging
            Console.WriteLine($"Logging error: {ex.Message}");
        }
    }
    
    private async Task LogToDatabaseAsync(string message, bool isImportant, 
        string userId, Guid? electionGuid)
    {
        var logEntry = new C_Log
        {
            AsOf = DateTime.UtcNow,
            Message = message,
            Important = isImportant,
            UserId = userId,
            ElectionGuid = electionGuid
        };
        
        _dbContext.C_Log.Add(logEntry);
        await _dbContext.SaveChangesAsync();
    }
    
    private bool ShouldNotifyIfttt(string message)
    {
        // Determine if message warrants IFTTT notification
        return message.Contains("error", StringComparison.OrdinalIgnoreCase) 
            || message.Contains("critical", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 5.3 Serilog Configuration

**Program.cs**:
```csharp
using Serilog;
using Serilog.Sinks.Rapid7InsightOps;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TallyJ")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/tallyj-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30
    )
    .WriteTo.Rapid7InsightOps(
        token: builder.Configuration["TallyJ:Logging:LogEntries:Token"],
        region: builder.Configuration["TallyJ:Logging:LogEntries:Region"]
    )
    .CreateLogger();

builder.Host.UseSerilog();
```

**appsettings.json**:
```json
{
  "TallyJ": {
    "Logging": {
      "LogEntries": {
        "Enabled": true,
        "Token": "[LogEntries token]",
        "Region": "us"
      },
      "IFTTT": {
        "Enabled": true,
        "WebhookKey": "cGJ7Y8OhmAwE2B1aq0V-nk"
      }
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### 5.4 Dependency Injection Registration

**Program.cs**:
```csharp
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddSingleton<IftttService>();

// Add HTTP client for IFTTT
builder.Services.AddHttpClient<IftttService>();
```

### 5.5 Enhanced Features for .NET Core

#### Structured Logging with Serilog
```csharp
_logger.LogInformation("Voter {VoterId} submitted ballot in election {ElectionGuid}", 
    voterId, electionGuid);
```

#### Application Insights Integration (Azure)
```csharp
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["TallyJ:Logging:ApplicationInsights:InstrumentationKey"]);
```

#### Seq Integration (Self-hosted alternative to LogEntries)
```csharp
.WriteTo.Seq("http://localhost:5341")
```

---

## 6. Monitoring & Alerting

### 6.1 Recommended Alerts (LogEntries)

| Alert Name | Condition | Action |
|------------|-----------|--------|
| **High Error Rate** | Error count > 10 in 5 minutes | Email admin, Slack notification |
| **Critical Error** | Log level = Fatal | SMS admin, IFTTT webhook |
| **Login Failures** | Failed login count > 5 from same IP | Email security team, block IP |
| **Slow Queries** | Query duration > 5 seconds | Log to performance dashboard |
| **SignalR Disconnects** | Disconnect count > 20 in 1 minute | Investigate server health |

### 6.2 Dashboards

**LogEntries Dashboard Widgets**:
- ✅ Error rate over time (line chart)
- ✅ Top 10 error messages (table)
- ✅ User activity by hour (bar chart)
- ✅ Election events timeline (event stream)
- ✅ API response time distribution (histogram)

**Custom Metrics to Track**:
- Voter login success/failure rate
- Ballot submission latency
- Tally calculation duration
- Email delivery success rate
- SMS delivery success rate

---

## 7. Security Considerations

### 7.1 Sensitive Data in Logs

**DO NOT LOG**:
- ❌ Passwords (plaintext or hashed)
- ❌ Verification codes (only log "code sent", not the actual code)
- ❌ OAuth tokens or API keys
- ❌ Full email addresses (mask: `u***@example.com`)
- ❌ Full phone numbers (mask: `***-***-1234`)
- ❌ Credit card numbers or payment info

**Safe to Log**:
- ✅ Usernames (non-sensitive identifiers)
- ✅ Election GUIDs
- ✅ IP addresses (for security auditing)
- ✅ User agents
- ✅ API request paths (without query parameters containing sensitive data)
- ✅ Timestamps

### 7.2 Log Access Control

**LogEntries**:
- ✅ Restrict access to authorized personnel only
- ✅ Use role-based access control (RBAC)
- ✅ Enable two-factor authentication (2FA) for LogEntries accounts
- ✅ Audit log access (who viewed what logs when)

**Database Logs (C_Log)**:
- ✅ Restrict direct database access
- ✅ Provide read-only view for log queries
- ✅ Encrypt database backups containing logs

### 7.3 IFTTT Webhook Security

**Webhook Key Protection**:
- ✅ Store webhook key in secure configuration (not in source control)
- ✅ Rotate webhook key periodically (every 90 days)
- ✅ Use HTTPS for all webhook calls (default)
- ✅ Validate webhook responses (check for 200 OK status)

**Payload Sanitization**:
- ✅ Truncate long messages to avoid injection attacks
- ✅ Remove newlines and special characters
- ✅ Encode HTML entities if logging to Slack/email

---

## 8. Testing Strategy

### 8.1 Unit Tests
```csharp
[Fact]
public async Task LogAsync_ImportantEvent_LogsToAllDestinations()
{
    // Arrange
    var mockLogger = new Mock<ILogger<LogService>>();
    var mockDbContext = CreateMockDbContext();
    var mockIfttt = new Mock<IftttService>();
    
    var logService = new LogService(mockLogger.Object, mockDbContext, mockIfttt.Object);
    
    // Act
    await logService.LogAsync("Test important message", isImportant: true);
    
    // Assert
    mockLogger.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), 
        It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), 
        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    
    mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
    mockIfttt.Verify(i => i.LogEventAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}
```

### 8.2 Integration Tests
```csharp
[Fact]
public async Task IftttService_RealWebhook_Succeeds()
{
    // Requires real IFTTT webhook key
    var ifttt = new IftttService(_configuration, _httpClient);
    
    var result = await ifttt.LogEventAsync("test_event", "Test value 1", "Test value 2");
    
    Assert.True(result);
    // Verify event appears in IFTTT activity log
}
```

### 8.3 Manual Testing Checklist
- [ ] Log info message (verify in LogEntries and C_Log table)
- [ ] Log warning message (verify in LogEntries and C_Log table)
- [ ] Log error with exception (verify stack trace in LogEntries)
- [ ] Trigger IFTTT webhook (verify event received)
- [ ] Test LogEntries dashboard queries
- [ ] Test LogEntries alerts (trigger condition, verify notification)
- [ ] Verify sensitive data not logged (passwords, codes, tokens)

---

## 9. Cost Considerations

### 9.1 LogEntries Pricing (Rapid7 InsightOps)

**Tiers** (as of 2024):

| Tier | Log Volume | Retention | Price |
|------|------------|-----------|-------|
| Free | 5 GB/month | 7 days | $0 |
| Standard | 50 GB/month | 30 days | $89/month |
| Professional | 200 GB/month | 90 days | $329/month |
| Enterprise | 1+ TB/month | Custom | Custom pricing |

**Estimation**:
- Small election (100 voters): ~100 MB logs/month → **Free tier**
- Medium election (1,000 voters): ~1 GB logs/month → **Free tier**
- Large organization (10,000 voters/month): ~10 GB logs/month → **Standard tier ($89/month)**

### 9.2 IFTTT Pricing

| Tier | Applets | Price |
|------|---------|-------|
| Free | 2 applets | $0 |
| Pro | Unlimited | $2.50/month |
| Pro+ | Unlimited + advanced features | $5/month |

**Recommendation**: Start with Free tier (2 applets: critical errors + election events)

---

## 10. Quick Reference

### 10.1 Configuration Summary

| Setting | Required | Format |
|---------|----------|--------|
| `LOGENTRIES_ACCOUNT_KEY` | Yes (if using LogEntries) | GUID |
| `LOGENTRIES_TOKEN` | Yes (if using LogEntries) | String |
| `LOGENTRIES_LOCATION` | Yes (if using LogEntries) | `us`, `eu`, etc. |
| `iftttKey` | Yes (if using IFTTT) | Alphanumeric string |

### 10.2 Common LogEntries Queries

```
# All errors in last hour
where(level = "Error") calculate(count) groupby(logger)

# Election-specific logs
where(ElectionGuid = "abc-123") sortby(timestamp)

# Slow queries
where(duration > 5000) calculate(average:duration)

# User activity
where(UserId = "admin@example.com") sortby(timestamp)
```

### 10.3 IFTTT Event Names

| Event | Trigger | Values |
|-------|---------|--------|
| `election_created` | New election | Name, Date |
| `voting_opened` | Voting starts | Name, Voter count |
| `voting_closed` | Voting ends | Name, Ballots cast |
| `critical_error` | Fatal error | Error message, Timestamp |
| `admin_login` | Admin login | Username, IP |

---

**End of Logging Integrations Documentation**
