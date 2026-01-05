# TallyJ Twilio SMS Integration Documentation

## 1. Overview

TallyJ integrates with **Twilio** to deliver **6-digit verification codes** to voters via SMS, voice calls, and WhatsApp. This enables passwordless authentication for voters who wish to use their phone number instead of email.

### 1.1 Use Cases

- **Voter Authentication**: One-time 6-digit verification codes for voter login
- **Phone Number Verification**: Confirm voter's phone number before election day
- **Multi-Channel Delivery**: SMS, voice call, or WhatsApp (voter's choice)

### 1.2 Supported Channels

- **SMS**: Standard text message delivery
- **Voice Call**: Automated voice call reading the verification code
- **WhatsApp**: Message via WhatsApp Business API

### 1.3 Technology Stack

- **Service Provider**: Twilio (https://www.twilio.com)
- **API**: Twilio REST API
- **SDK**: Twilio C# SDK (estimated version: 5.x - 6.x)
- **Transport**: HTTPS REST API calls

---

## 2. Configuration Settings

### 2.1 Required Settings

**Location**: `App_Data/AppSettings.config` (external file referenced by Web.config)

```xml
<!-- Twilio Account Credentials -->
<add key="twilio-SID" value="[your Twilio Account SID]" />
<add key="twilio-Token" value="[your Twilio Auth Token]" />

<!-- Messaging Configuration -->
<add key="twilio-MessagingSid" value="[your Twilio Messaging Service SID]" />
<add key="twilio-FromNumber" value="[your Twilio phone number]" />
<add key="twilio-WhatsAppFromNumber" value="[your Twilio WhatsApp number]" />

<!-- Webhook Configuration -->
<add key="twilio-CallbackUrl" value="[path to your server]/Public/SmsStatus" />
```

### 2.2 Configuration Details

| Key                         | Format                             | Example                               | Purpose                           |
| --------------------------- | ---------------------------------- | ------------------------------------- | --------------------------------- |
| `twilio-SID`                | ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx | `AC1234567890abcdef1234567890abcd`    | Twilio Account SID (identifier)   |
| `twilio-Token`              | 32-character hex string            | `1234567890abcdef1234567890abcdef`    | Twilio Auth Token (secret)        |
| `twilio-MessagingSid`       | MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx | `MG1234567890abcdef1234567890abcd`    | Messaging Service SID (optional)  |
| `twilio-FromNumber`         | E.164 format                       | `+15551234567`                        | Twilio phone number for SMS/voice |
| `twilio-WhatsAppFromNumber` | whatsapp:+...                      | `whatsapp:+15551234567`               | WhatsApp sender number            |
| `twilio-CallbackUrl`        | Full URL                           | `https://tallyj.com/Public/SmsStatus` | Status webhook endpoint           |

### 2.3 Feature Flags

**Location**: `App_Data/AppSettings.config`

```xml
<!-- SMS/Voice/WhatsApp Feature Toggles -->
<add key="SupportOnlineSmsLogin" value="true" />
<add key="SmsAvailable" value="true" />
<add key="VoiceAvailable" value="true" />
<add key="SupportOnlineWhatsAppLogin" value="true" />

<!-- Rate Limiting -->
<add key="UserAttemptMax" value="10" />
```

| Key                          | Default | Purpose                                        |
| ---------------------------- | ------- | ---------------------------------------------- |
| `SupportOnlineSmsLogin`      | `true`  | Enable SMS/Voice authentication globally       |
| `SmsAvailable`               | `true`  | Enable SMS delivery method                     |
| `VoiceAvailable`             | `true`  | Enable voice call delivery method              |
| `SupportOnlineWhatsAppLogin` | `true`  | Enable WhatsApp delivery method                |
| `UserAttemptMax`             | `10`    | Max SMS/Voice/WhatsApp attempts per 15 minutes |

### 2.4 Obtaining Twilio Credentials

**Steps**:

1. Sign up at https://www.twilio.com/try-twilio
2. Verify your account (provide phone number, credit card)
3. Navigate to Console → Account Info:
   - **Account SID**: Copy this value → `twilio-SID`
   - **Auth Token**: Click "View" → Copy → `twilio-Token`
4. Purchase a phone number:
   - Console → Phone Numbers → Buy a Number
   - Choose a number with SMS and Voice capabilities
   - Copy number in E.164 format → `twilio-FromNumber`
5. (Optional) Create Messaging Service:
   - Console → Messaging → Services → Create Messaging Service
   - Copy Messaging Service SID → `twilio-MessagingSid`
6. (Optional) Enable WhatsApp:
   - Console → Messaging → WhatsApp → Senders
   - Request WhatsApp number or use Twilio sandbox
   - Copy WhatsApp number → `twilio-WhatsAppFromNumber`

---

## 3. SMS Delivery Flow

### 3.1 Voter Requests Verification Code

#### Step 1: Voter Clicks "Vote Online" → "Using your phone"

**UI**: Home page (`/`) → Modal dialog → Phone number input field

#### Step 2: Voter Enters Phone Number

**Format**: Various formats accepted, normalized to E.164

- Input: `555-123-4567`, `(555) 123-4567`, `+1 555 123 4567`
- Normalized: `+15551234567`

#### Step 3: API Request to Issue Code

**Code**: `PublicController.cs:143-146` → `VoterCodeHelper.IssueCode()`

```csharp
public JsonResult IssueCode(string type, string method, string target, string hubKey)
{
    // type: "voter"
    // method: "sms", "voice", or "whatsapp"
    // target: phone number (any format)
    // hubKey: SignalR connection ID

    var helper = new VoterCodeHelper(hubKey);
    return helper.IssueCode(type, method, target).AsJsonResult();
}
```

### 3.2 Code Generation

**Code**: `VoterCodeHelper.cs:IssueCode()` (estimated)

```csharp
private string GenerateVerificationCode()
{
    // Generate random 6-digit code
    var random = new Random();
    return random.Next(100000, 999999).ToString();
}
```

**Format**: 6-digit numeric code (e.g., `123456`)
**Storage**: `OnlineVoter.VerifyCode` (plaintext)
**Expiration**: Tracked via `OnlineVoter.VerifyCodeDate` (typically 10-15 minutes)
**Next Version**: Should use better random generator for security

```csharp
    // Generate cryptographically secure random 6-digit code
    var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000);
    return code.ToString();
```

### 3.3 Twilio API Call (SMS)

**Code**: Estimated implementation based on Twilio SDK

```csharp
public bool SendSms(string toPhoneNumber, string message)
{
    try
    {
        var accountSid = ConfigurationManager.AppSettings["twilio-SID"];
        var authToken = ConfigurationManager.AppSettings["twilio-Token"];
        var fromNumber = ConfigurationManager.AppSettings["twilio-FromNumber"];
        var callbackUrl = ConfigurationManager.AppSettings["twilio-CallbackUrl"];

        TwilioClient.Init(accountSid, authToken);

        var messageOptions = new CreateMessageOptions(new PhoneNumber(toPhoneNumber))
        {
            From = new PhoneNumber(fromNumber),
            Body = message,
            StatusCallback = new Uri(callbackUrl)
        };

        var messageResult = MessageResource.Create(messageOptions);

        // Log to SmsLog table
        LogSmsDelivery(toPhoneNumber, messageResult.Sid, messageResult.Status.ToString());

        return messageResult.Status != MessageResource.StatusEnum.Failed;
    }
    catch (Exception ex)
    {
        new LogHelper().Add($"Twilio SMS error: {ex.Message}", false);
        return false;
    }
}
```

**HTTP Request** (underlying Twilio API):

```http
POST /2010-04-01/Accounts/{AccountSid}/Messages.json
Host: api.twilio.com
Authorization: Basic {base64(AccountSid:AuthToken)}
Content-Type: application/x-www-form-urlencoded

To=+15551234567&
From=+15559876543&
Body=Your%20TallyJ%20verification%20code%20is%3A%20123456&
StatusCallback=https://tallyj.com/Public/SmsStatus
```

### 3.4 SMS Template

**Default Message**:

```
Your TallyJ verification code is: {CODE}

This code will expire in 15 minutes. Do not share this code with anyone.
```

**Customizable Template**: Election owners can customize the SMS text via election settings.

**Variables Available**:

- `{CODE}`: 6-digit verification code
- `{ELECTION_NAME}`: Name of the election
- `{EXPIRES_IN}`: Minutes until code expires

**Example Custom Template**:

```
Election: {ELECTION_NAME}

Your verification code is: {CODE}

Valid for {EXPIRES_IN} minutes.
```

### 3.5 Status Webhook (Delivery Confirmation)

**Endpoint**: `/Public/SmsStatus` (publicly accessible, no authentication required)

**Code**: `PublicController.cs:SmsStatus()` (estimated)

```csharp
[HttpPost]
[AllowAnonymous]
public ActionResult SmsStatus()
{
    // Twilio sends POST request with delivery status
    var messageSid = Request.Form["MessageSid"];
    var messageStatus = Request.Form["MessageStatus"];
    var errorCode = Request.Form["ErrorCode"];
    var errorMessage = Request.Form["ErrorMessage"];

    // Update SmsLog table
    var db = new TallyJ3Entities();
    var smsLog = db.SmsLog.FirstOrDefault(s => s.MessageSid == messageSid);

    if (smsLog != null)
    {
        smsLog.Status = messageStatus;
        smsLog.ErrorCode = errorCode;
        smsLog.ErrorMessage = errorMessage;
        smsLog.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();
    }

    return new HttpStatusCodeResult(200); // Twilio expects 200 OK
}
```

**Twilio Status Values**:

- `queued`: Message accepted by Twilio, not yet sent
- `sending`: Currently being sent
- `sent`: Successfully delivered to carrier
- `delivered`: Confirmed delivery to recipient's device
- `undelivered`: Failed to deliver
- `failed`: Permanent failure (invalid number, blocked, etc.)

---

## 4. Voice Call Delivery

### 4.1 Voice Call Flow

Same as SMS, but uses Twilio Voice API instead of Messaging API.

### 4.2 Twilio API Call (Voice)

**Code**: Estimated implementation

```csharp
public bool SendVoiceCall(string toPhoneNumber, string verificationCode)
{
    try
    {
        var accountSid = ConfigurationManager.AppSettings["twilio-SID"];
        var authToken = ConfigurationManager.AppSettings["twilio-Token"];
        var fromNumber = ConfigurationManager.AppSettings["twilio-FromNumber"];
        var callbackUrl = ConfigurationManager.AppSettings["twilio-CallbackUrl"];

        TwilioClient.Init(accountSid, authToken);

        // Generate TwiML for voice message
        var twiml = GenerateVoiceTwiml(verificationCode);

        var call = CallResource.Create(
            to: new PhoneNumber(toPhoneNumber),
            from: new PhoneNumber(fromNumber),
            twiml: new Twilio.Types.Twiml(twiml),
            statusCallback: new Uri(callbackUrl)
        );

        LogSmsDelivery(toPhoneNumber, call.Sid, call.Status.ToString(), "voice");

        return call.Status != CallResource.StatusEnum.Failed;
    }
    catch (Exception ex)
    {
        new LogHelper().Add($"Twilio Voice error: {ex.Message}", false);
        return false;
    }
}
```

### 4.3 TwiML Voice Script

**TwiML** (Twilio Markup Language) for voice message:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Response>
  <Say voice="alice" language="en-US">
    Hello. Your TallyJ verification code is:
  </Say>
  <Pause length="1"/>
  <Say voice="alice" language="en-US">
    1, 2, 3, 4, 5, 6
  </Say>
  <Pause length="1"/>
  <Say voice="alice" language="en-US">
    I repeat: 1, 2, 3, 4, 5, 6
  </Say>
  <Say voice="alice" language="en-US">
    Thank you.
  </Say>
</Response>
```

**Code** to generate TwiML:

```csharp
private string GenerateVoiceTwiml(string code)
{
    var twiml = new VoiceResponse();

    twiml.Say("Hello. Your TallyJ verification code is:", voice: "alice", language: "en-US");
    twiml.Pause(1);

    // Read digits individually with pauses
    var digitsSeparated = string.Join(", ", code.ToCharArray());
    twiml.Say(digitsSeparated, voice: "alice", language: "en-US");

    twiml.Pause(1);
    twiml.Say($"I repeat: {digitsSeparated}", voice: "alice", language: "en-US");
    twiml.Say("Thank you.", voice: "alice", language: "en-US");

    return twiml.ToString();
}
```

**Voice Options**:

- **Voice**: `alice` (clear, friendly female voice)
- **Language**: `en-US` (English - United States)
- **Alternative Voices**: `man`, `woman`, `Polly.Matthew`, `Polly.Joanna`

---

## 5. WhatsApp Delivery

### 5.1 WhatsApp Configuration

**Twilio WhatsApp Options**:

1. **Twilio Sandbox** (testing only): Pre-approved WhatsApp number
2. **Approved WhatsApp Sender** (production): Requires Facebook Business verification

### 5.2 Twilio API Call (WhatsApp)

**Code**: Estimated implementation

```csharp
public bool SendWhatsApp(string toPhoneNumber, string message)
{
    try
    {
        var accountSid = ConfigurationManager.AppSettings["twilio-SID"];
        var authToken = ConfigurationManager.AppSettings["twilio-Token"];
        var fromNumber = ConfigurationManager.AppSettings["twilio-WhatsAppFromNumber"];

        TwilioClient.Init(accountSid, authToken);

        var messageOptions = new CreateMessageOptions(
            new PhoneNumber($"whatsapp:{toPhoneNumber}"))
        {
            From = new PhoneNumber(fromNumber), // Already includes "whatsapp:" prefix
            Body = message
        };

        var messageResult = MessageResource.Create(messageOptions);

        LogSmsDelivery(toPhoneNumber, messageResult.Sid, messageResult.Status.ToString(), "whatsapp");

        return messageResult.Status != MessageResource.StatusEnum.Failed;
    }
    catch (Exception ex)
    {
        new LogHelper().Add($"Twilio WhatsApp error: {ex.Message}", false);
        return false;
    }
}
```

**Key Difference**: Phone numbers must be prefixed with `whatsapp:` (e.g., `whatsapp:+15551234567`)

### 5.3 WhatsApp Template

**Message**:

```
Your TallyJ verification code is: 123456

This code will expire in 15 minutes.
```

**Note**: WhatsApp requires pre-approved message templates for production use. Twilio Sandbox allows free-form messages for testing.

---

## 6. Rate Limiting & Abuse Prevention

### 6.1 Rate Limiting Configuration

**Setting**: `UserAttemptMax = 10` (max attempts per 15 minutes)

**Tracking**: `OnlineVoter` table

| Field                 | Purpose                                 |
| --------------------- | --------------------------------------- |
| `VerifyAttempts`      | Count of failed verification attempts   |
| `VerifyAttemptsStart` | Timestamp when attempt counting started |
| `VerifyCodeDate`      | When current code was issued            |

### 6.2 Rate Limiting Logic

**Code**: `VoterCodeHelper.cs:IssueCode()` (estimated)

```csharp
public object IssueCode(string type, string method, string target)
{
    var db = new TallyJ3Entities();
    var voterId = NormalizePhoneNumber(target);
    var voterIdType = "sms";

    var onlineVoter = db.OnlineVoter.FirstOrDefault(ov =>
        ov.VoterId == voterId && ov.VoterIdType == voterIdType);

    if (onlineVoter == null)
    {
        onlineVoter = new OnlineVoter
        {
            VoterId = voterId,
            VoterIdType = voterIdType,
            WhenRegistered = DateTime.UtcNow
        };
        db.OnlineVoter.Add(onlineVoter);
    }

    // Check rate limiting
    var now = DateTime.UtcNow;
    var attemptWindow = now.AddMinutes(-15);

    if (onlineVoter.VerifyAttemptsStart.HasValue
        && onlineVoter.VerifyAttemptsStart > attemptWindow)
    {
        // Within rate limit window
        if (onlineVoter.VerifyAttempts >= GetUserAttemptMax())
        {
            return new { Error = "Too many attempts. Please try again in 15 minutes." };
        }

        onlineVoter.VerifyAttempts++;
    }
    else
    {
        // Reset rate limit window
        onlineVoter.VerifyAttempts = 1;
        onlineVoter.VerifyAttemptsStart = now;
    }

    // Generate and send code
    var code = GenerateVerificationCode();
    onlineVoter.VerifyCode = code;
    onlineVoter.VerifyCodeDate = now;

    db.SaveChanges();

    // Send via selected method
    bool sent = false;
    switch (method)
    {
        case "sms":
            sent = SendSms(voterId, $"Your TallyJ verification code is: {code}");
            break;
        case "voice":
            sent = SendVoiceCall(voterId, code);
            break;
        case "whatsapp":
            sent = SendWhatsApp(voterId, $"Your TallyJ verification code is: {code}");
            break;
    }

    return new { Sent = sent, Method = method };
}
```

### 6.3 Additional Abuse Prevention

**IP-Based Rate Limiting** (recommended for .NET Core):

- Max 20 code requests per IP per hour
- Max 100 code requests per IP per day

**CAPTCHA** (recommended):

- Add CAPTCHA before phone number submission
- Prevents automated abuse

**Phone Number Validation**:

- Check against known disposable/virtual phone number databases
- Validate number format and carrier information

---

## 7. Error Handling

### 7.1 Common Twilio Errors

| Error Code | Description                             | Cause                                 | Solution                                              |
| ---------- | --------------------------------------- | ------------------------------------- | ----------------------------------------------------- |
| `20003`    | Authentication error                    | Invalid Account SID or Auth Token     | Verify credentials in Twilio console                  |
| `21211`    | Invalid 'To' phone number               | Phone number format incorrect         | Normalize to E.164 format                             |
| `21408`    | Permission to send to unverified number | Trial account, recipient not verified | Verify recipient in Twilio console or upgrade account |
| `21610`    | Unsubscribed recipient                  | Recipient opted out of messages       | Remove from list, notify user                         |
| `30003`    | Unreachable destination                 | Phone number invalid or carrier issue | Verify number, try alternative method                 |
| `30005`    | Unknown destination                     | Phone number does not exist           | Validate number format                                |
| `30006`    | Landline or unreachable carrier         | Cannot deliver SMS to landline        | Suggest voice call instead                            |

### 7.2 Error Handling Code

```csharp
public object SendSmsWithErrorHandling(string phoneNumber, string message)
{
    try
    {
        var result = SendSms(phoneNumber, message);
        return new { Success = true };
    }
    catch (Twilio.Exceptions.ApiException ex)
    {
        var errorCode = ex.Code;
        var errorMessage = ex.Message;

        // Log error
        new LogHelper().Add($"Twilio API error {errorCode}: {errorMessage}", false);

        // User-friendly error messages
        switch (errorCode)
        {
            case 21211:
                return new { Error = "Invalid phone number format. Please check and try again." };
            case 21408:
                return new { Error = "Cannot send to this number. Please try a different number." };
            case 30003:
            case 30005:
                return new { Error = "Phone number is unreachable. Please verify the number." };
            case 30006:
                return new { Error = "Cannot send SMS to landline. Try voice call instead." };
            default:
                return new { Error = "SMS delivery failed. Please try again or use email instead." };
        }
    }
    catch (Exception ex)
    {
        new LogHelper().Add($"SMS error: {ex.Message}", false);
        return new { Error = "An error occurred. Please try again." };
    }
}
```

---

## 8. Database Logging

### 8.1 SmsLog Table

**Purpose**: Track all SMS/Voice/WhatsApp deliveries for auditing and debugging

**Schema**: See `database/entities.md` for full definition

**Key Fields**:

| Field          | Type          | Purpose                                           |
| -------------- | ------------- | ------------------------------------------------- |
| `SmsLogId`     | int (PK)      | Unique identifier                                 |
| `ToNumber`     | nvarchar(50)  | Recipient phone number                            |
| `MessageSid`   | nvarchar(50)  | Twilio message SID                                |
| `Status`       | nvarchar(20)  | Delivery status (queued, sent, delivered, failed) |
| `ErrorCode`    | nvarchar(10)  | Twilio error code (if failed)                     |
| `ErrorMessage` | nvarchar(500) | Error description                                 |
| `Method`       | nvarchar(20)  | Delivery method (sms, voice, whatsapp)            |
| `CreatedAt`    | datetime      | When message was sent                             |
| `UpdatedAt`    | datetime      | Last status update from Twilio                    |

### 8.2 Logging Code

```csharp
private void LogSmsDelivery(string toNumber, string messageSid, string status, string method = "sms")
{
    var db = new TallyJ3Entities();

    var smsLog = new SmsLog
    {
        ToNumber = toNumber,
        MessageSid = messageSid,
        Status = status,
        Method = method,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.SmsLog.Add(smsLog);
    db.SaveChanges();
}
```

---

## 9. Cost Considerations

### 9.1 Twilio Pricing (as of 2024)

**SMS (US)**:

- Outbound SMS: $0.0079 per message
- Inbound SMS: $0.0079 per message

**Voice (US)**:

- Outbound call (per minute): $0.013
- Inbound call (per minute): $0.0085
- Average verification call: ~30 seconds = $0.0065

**WhatsApp**:

- User-initiated conversation: Free
- Business-initiated conversation: $0.005 - $0.08 per message (varies by country)

**Phone Number (US)**:

- Monthly rental: $1.15 per number
- SMS + Voice capable: $1.15/month
- Toll-free: $2.00/month

### 9.2 Cost Estimation Examples

**Scenario 1**: 1,000 voters, all use SMS

- Cost: 1,000 × $0.0079 = **$7.90**
- Plus phone number rental: **$1.15/month**
- **Total**: ~$9 for election

**Scenario 2**: 1,000 voters, 70% SMS, 30% voice

- SMS: 700 × $0.0079 = $5.53
- Voice: 300 × $0.0065 = $1.95
- **Total**: ~$7.50 + $1.15/month = **$8.65**

**Scenario 3**: 10,000 voters (large election)

- Cost: 10,000 × $0.0079 = **$79.00**
- Plus phone number: **$1.15/month**
- **Total**: ~$80 for election

### 9.3 Cost Optimization Strategies

1. **Prefer SMS over Voice**: SMS is ~18% cheaper than voice calls
2. **Code Expiration**: Shorter expiration (10 min) → fewer duplicate requests
3. **Rate Limiting**: Prevent abuse and excessive resends
4. **Email Fallback**: Offer email as free alternative
5. **Bulk Credits**: Purchase Twilio credits in bulk for discount
6. **Messaging Services**: Use Twilio Messaging Services for better deliverability and lower costs

---

## 10. .NET Core Migration Strategy

### 10.1 Technology Mapping

| ASP.NET Framework 4.8              | .NET Core 8                           |
| ---------------------------------- | ------------------------------------- |
| `Twilio SDK 5.x`                   | `Twilio SDK 6.x` (latest)             |
| `ConfigurationManager.AppSettings` | `IConfiguration` dependency injection |
| `TallyJ3Entities` (EF6)            | `TallyJDbContext` (EF Core 8)         |

### 10.2 Code Migration Example

#### Current (ASP.NET Framework)

```csharp
public class SmsService
{
    public bool SendSms(string toNumber, string message)
    {
        var accountSid = ConfigurationManager.AppSettings["twilio-SID"];
        var authToken = ConfigurationManager.AppSettings["twilio-Token"];
        var fromNumber = ConfigurationManager.AppSettings["twilio-FromNumber"];

        TwilioClient.Init(accountSid, authToken);

        var result = MessageResource.Create(
            to: new PhoneNumber(toNumber),
            from: new PhoneNumber(fromNumber),
            body: message
        );

        return result.Status != MessageResource.StatusEnum.Failed;
    }
}
```

#### Target (.NET Core 8)

```csharp
public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly TallyJDbContext _dbContext;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        IConfiguration configuration,
        TallyJDbContext dbContext,
        ILogger<SmsService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;

        // Initialize Twilio client once
        var accountSid = _configuration["TallyJ:Twilio:AccountSid"];
        var authToken = _configuration["TallyJ:Twilio:AuthToken"];
        TwilioClient.Init(accountSid, authToken);
    }

    public async Task<SmsResult> SendSmsAsync(string toNumber, string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fromNumber = _configuration["TallyJ:Twilio:FromNumber"];
            var callbackUrl = _configuration["TallyJ:Twilio:CallbackUrl"];

            var messageOptions = new CreateMessageOptions(new PhoneNumber(toNumber))
            {
                From = new PhoneNumber(fromNumber),
                Body = message,
                StatusCallback = new Uri(callbackUrl)
            };

            var result = await MessageResource.CreateAsync(messageOptions);

            // Log to database
            await LogSmsDeliveryAsync(toNumber, result.Sid, result.Status.ToString(), "sms");

            return new SmsResult
            {
                Success = result.Status != MessageResource.StatusEnum.Failed,
                MessageSid = result.Sid,
                Status = result.Status.ToString()
            };
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Twilio API error: {ErrorCode} - {ErrorMessage}",
                ex.Code, ex.Message);

            return new SmsResult
            {
                Success = false,
                ErrorCode = ex.Code.ToString(),
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task LogSmsDeliveryAsync(string toNumber, string messageSid,
        string status, string method)
    {
        var smsLog = new SmsLog
        {
            ToNumber = toNumber,
            MessageSid = messageSid,
            Status = status,
            Method = method,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.SmsLogs.Add(smsLog);
        await _dbContext.SaveChangesAsync();
    }
}
```

### 10.3 Configuration Migration

#### Current (AppSettings.config)

```xml
<add key="twilio-SID" value="AC..." />
<add key="twilio-Token" value="..." />
<add key="twilio-FromNumber" value="+15551234567" />
```

#### Target (appsettings.json)

```json
{
  "TallyJ": {
    "Twilio": {
      "AccountSid": "AC...",
      "AuthToken": "...",
      "FromNumber": "+15551234567",
      "MessagingServiceSid": "MG...",
      "WhatsAppFromNumber": "whatsapp:+15551234567",
      "CallbackUrl": "https://tallyj.com/api/webhooks/sms-status"
    },
    "Features": {
      "SmsEnabled": true,
      "VoiceEnabled": true,
      "WhatsAppEnabled": true,
      "SmsRateLimit": {
        "MaxAttemptsPerUser": 10,
        "WindowMinutes": 15
      }
    }
  }
}
```

**Security**: Store `AuthToken` in **Azure Key Vault** or **AWS Secrets Manager** for production.

### 10.4 Dependency Injection Registration

**Program.cs**:

```csharp
builder.Services.AddSingleton<ISmsService, SmsService>();

// Or with configuration validation
builder.Services.AddOptions<TwilioOptions>()
    .Bind(builder.Configuration.GetSection("TallyJ:Twilio"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<ISmsService, SmsService>();
```

### 10.5 Enhanced Features for .NET Core

#### Async/Await Throughout

- All Twilio SDK calls now async: `MessageResource.CreateAsync()`
- Better performance and scalability

#### Retry Logic with Polly

```csharp
services.AddHttpClient<ISmsService, SmsService>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

#### Background Job Queue for SMS

```csharp
public class SmsBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var smsToSend = await _queue.DequeueAsync(stoppingToken);
            await _smsService.SendSmsAsync(smsToSend.PhoneNumber, smsToSend.Message);
        }
    }
}
```

---

## 11. Testing Strategy

### 11.1 Unit Tests

```csharp
[Fact]
public async Task SendSmsAsync_ValidNumber_ReturnsSuccess()
{
    // Arrange
    var mockConfig = new Mock<IConfiguration>();
    mockConfig.Setup(c => c["TallyJ:Twilio:AccountSid"]).Returns("AC_test");
    mockConfig.Setup(c => c["TallyJ:Twilio:AuthToken"]).Returns("test_token");

    var smsService = new SmsService(mockConfig.Object, _dbContext, _logger);

    // Act
    var result = await smsService.SendSmsAsync("+15551234567", "Test message");

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.MessageSid);
}
```

### 11.2 Integration Tests (with Twilio Test Credentials)

```csharp
[Fact]
public async Task SendSms_ToTwilioTestNumber_Succeeds()
{
    // Twilio provides test phone numbers that always succeed/fail
    var testNumber = "+15005550006"; // Twilio test number (always succeeds)

    var result = await _smsService.SendSmsAsync(testNumber, "Integration test");

    Assert.True(result.Success);
    Assert.Equal("queued", result.Status.ToLower());
}

[Fact]
public async Task SendSms_ToInvalidTestNumber_Fails()
{
    var invalidNumber = "+15005550001"; // Twilio test number (always fails)

    var result = await _smsService.SendSmsAsync(invalidNumber, "Test");

    Assert.False(result.Success);
    Assert.NotNull(result.ErrorCode);
}
```

**Twilio Test Numbers**:

- `+15005550006`: Always succeeds
- `+15005550001`: Invalid phone number
- `+15005550007`: Cannot route to this number
- Full list: https://www.twilio.com/docs/iam/test-credentials

### 11.3 Manual Testing Checklist

- [ ] Send SMS with valid phone number
- [ ] Send voice call with valid phone number
- [ ] Send WhatsApp message (sandbox mode)
- [ ] Verify 6-digit code delivery
- [ ] Test rate limiting (11 attempts in 15 minutes)
- [ ] Test invalid phone number error handling
- [ ] Test landline number (should suggest voice)
- [ ] Verify status webhook updates SmsLog table
- [ ] Test code expiration (after 15 minutes)
- [ ] Test delivery to international numbers

---

## 12. Monitoring & Logging

### 12.1 Events to Log

| Event                   | Log Level | Data to Include                                  |
| ----------------------- | --------- | ------------------------------------------------ |
| SMS sent successfully   | Info      | Phone number (masked), MessageSid, Method        |
| SMS delivery failed     | Warning   | Phone number (masked), Error code, Error message |
| Rate limit exceeded     | Warning   | Phone number (masked), Attempt count             |
| Twilio API error        | Error     | Error code, Error message, Phone number (masked) |
| Status webhook received | Debug     | MessageSid, Status                               |

### 12.2 Metrics to Track

- SMS delivery success rate (per method: SMS, voice, WhatsApp)
- Average delivery time (webhook timestamp - send timestamp)
- Error distribution (by error code)
- Cost tracking (total messages sent per month)
- Rate limit violations

---

## 13. Security Best Practices

### 13.1 Twilio Credentials

- ✅ Store Auth Token in secure configuration (Key Vault, Secrets Manager)
- ✅ Rotate Auth Token every 90 days
- ✅ Use Twilio API Keys for additional security
- ✅ Enable IP whitelisting in Twilio console (if static IPs available)

### 13.2 Webhook Security

- ✅ Validate Twilio webhook signatures (prevent spoofing)
- ✅ Use HTTPS for callback URLs
- ✅ Rate limit webhook endpoint (prevent DoS)

**Webhook Signature Validation**:

```csharp
[HttpPost]
public IActionResult SmsStatus()
{
    var signature = Request.Headers["X-Twilio-Signature"];
    var url = $"{Request.Scheme}://{Request.Host}{Request.Path}";
    var formValues = Request.Form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

    var authToken = _configuration["TallyJ:Twilio:AuthToken"];
    var validator = new RequestValidator(authToken);

    if (!validator.Validate(url, formValues, signature))
    {
        _logger.LogWarning("Invalid Twilio webhook signature");
        return Unauthorized();
    }

    // Process webhook...
    return Ok();
}
```

### 13.3 Phone Number Privacy

- ✅ Mask phone numbers in logs (show last 4 digits only)
- ✅ Encrypt phone numbers in database (GDPR compliance)
- ✅ Allow users to delete their phone number
- ✅ Don't share phone numbers with third parties

---

## 14. Known Limitations

### 14.1 Current Implementation

1. **No international number validation**: May send SMS to unsupported countries
2. **No carrier lookup**: Cannot detect landlines before sending SMS
3. **No delivery retry logic**: If SMS fails, user must request new code
4. **No cost tracking**: No dashboard to monitor Twilio spend
5. **No A2P 10DLC compliance**: May have lower deliverability (US only)

### 14.2 Migration Opportunities

**Add in .NET Core version**:

- ✅ Carrier lookup before sending (Twilio Lookup API)
- ✅ Automatic retry with different method (SMS fails → try voice)
- ✅ Cost tracking dashboard
- ✅ A2P 10DLC registration for better deliverability
- ✅ International phone number validation
- ✅ Delivery analytics and reporting

---

## 15. Quick Reference

### 15.1 Configuration Summary

| Setting                     | Required | Format                              |
| --------------------------- | -------- | ----------------------------------- |
| `twilio-SID`                | Yes      | ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx  |
| `twilio-Token`              | Yes      | 32-character hex string             |
| `twilio-FromNumber`         | Yes      | +1XXXXXXXXXX (E.164 format)         |
| `twilio-MessagingSid`       | No       | MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx  |
| `twilio-WhatsAppFromNumber` | No       | whatsapp:+1XXXXXXXXXX               |
| `twilio-CallbackUrl`        | No       | https://domain.com/Public/SmsStatus |

### 15.2 API Endpoints

| Endpoint                | Method | Purpose                                        |
| ----------------------- | ------ | ---------------------------------------------- |
| `/Public/IssueCode`     | POST   | Request verification code (SMS/Voice/WhatsApp) |
| `/Public/LoginWithCode` | POST   | Verify code and authenticate voter             |
| `/Public/SmsStatus`     | POST   | Twilio status webhook (public, no auth)        |

### 15.3 Database Tables

| Table         | Purpose                              | Key Fields                                |
| ------------- | ------------------------------------ | ----------------------------------------- |
| `OnlineVoter` | Voter records and verification codes | `VoterId`, `VerifyCode`, `VerifyCodeDate` |
| `SmsLog`      | SMS/Voice/WhatsApp delivery logs     | `MessageSid`, `Status`, `ErrorCode`       |

---

**End of Twilio SMS Integration Documentation**
