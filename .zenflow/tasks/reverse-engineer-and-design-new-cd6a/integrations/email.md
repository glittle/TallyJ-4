# TallyJ Email Integration Documentation

## 1. Overview

TallyJ integrates email delivery for multiple purposes including voter authentication (verification codes), election notifications, results distribution, and teller invitations. The system supports **two email delivery methods**: SendGrid API (preferred) and traditional SMTP.

### 1.1 Use Cases
- **Voter Authentication**: One-time 6-digit verification codes for passwordless login
- **Election Invitations**: Invite voters to participate in online elections
- **Results Distribution**: Send election results to stakeholders
- **Teller Invitations**: Invite election workers to join as tellers
- **System Notifications**: Account creation, password reset, security alerts

### 1.2 Supported Delivery Methods
1. **SendGrid API** (preferred): RESTful API, better deliverability, analytics
2. **SMTP** (fallback): Traditional email protocol, universal compatibility

### 1.3 Technology Stack
- **Primary**: SendGrid API (HTTP REST calls)
- **Fallback**: SMTP (System.Net.Mail.SmtpClient or MailKit)
- **Templates**: HTML and plain text
- **Encoding**: UTF-8

---

## 2. Configuration Settings

### 2.1 SendGrid API Configuration (Preferred)

**Location**: `App_Data/AppSettings.config` (external file referenced by Web.config)

```xml
<!-- SendGrid API (Preferred Method) -->
<add key="SendGridApiKey" value="[your SendGrid API key]" />
```

**Format**: `SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

**Where to Obtain**:
1. Sign up at https://sendgrid.com
2. Navigate to Settings → API Keys
3. Create new API key with "Mail Send" permissions
4. Copy key → `SendGridApiKey`

**Benefits**:
- ✅ Better deliverability (99%+ delivery rate)
- ✅ Email analytics (opens, clicks, bounces)
- ✅ No SMTP server management
- ✅ Faster delivery
- ✅ Built-in spam prevention
- ✅ Free tier: 100 emails/day

### 2.2 SMTP Configuration (Alternative/Fallback)

**Location**: `App_Data/AppSettings.config`

```xml
<!-- SMTP Settings (Alternative Method) -->
<add key="SmtpHost" value="[your SMTP host]" />
<add key="SmtpPort" value="587" />
<add key="SmtpUsername" value="[your SMTP username]" />
<add key="SmtpPassword" value="[your SMTP password]" />
<add key="SmtpSecure" value="true" />
<add key="SmtpDefaultFromAddress" value="noreply@tallyj.com" />
<add key="SmtpDefaultFromName" value="TallyJ Election System" />
```

| Setting | Format | Example | Purpose |
|---------|--------|---------|---------|
| `SmtpHost` | Hostname or IP | `smtp.gmail.com` | SMTP server address |
| `SmtpPort` | Port number | `587` | SMTP port (25, 587, 465) |
| `SmtpUsername` | Email or username | `user@example.com` | SMTP authentication username |
| `SmtpPassword` | Password | `password123` | SMTP authentication password |
| `SmtpSecure` | Boolean | `true` | Use SSL/TLS encryption |
| `SmtpDefaultFromAddress` | Email | `noreply@tallyj.com` | Default sender email |
| `SmtpDefaultFromName` | Text | `TallyJ System` | Default sender display name |

**Common SMTP Providers**:

| Provider | Host | Port | Secure | Notes |
|----------|------|------|--------|-------|
| Gmail | `smtp.gmail.com` | 587 | TLS | Requires "App Password" (not account password) |
| Outlook/Office 365 | `smtp.office365.com` | 587 | TLS | Requires OAuth2 or App Password |
| SendGrid SMTP | `smtp.sendgrid.net` | 587 | TLS | API key as password |
| Mailgun | `smtp.mailgun.org` | 587 | TLS | Domain-specific credentials |
| Amazon SES | `email-smtp.us-east-1.amazonaws.com` | 587 | TLS | SMTP credentials from SES console |

### 2.3 Development/Testing Configuration

**Location**: `App_Data/AppSettings.config`

```xml
<!-- Email Testing (Development Only) -->
<add key="SmtpPickupDirectory" value="D:\Temp\Emails" />
```

**Purpose**: Write emails to local directory instead of sending (does NOT work with SendGrid API)

**Behavior**:
- Emails saved as `.eml` files in specified directory
- Can be opened with Outlook, Thunderbird, etc.
- No actual email delivery
- Useful for testing email templates without sending

**Note**: Only works with SMTP method, not SendGrid API.

---

## 3. Email Templates & Content

### 3.1 Voter Verification Code Email

**Purpose**: Send 6-digit verification code for passwordless voter authentication

**Subject**: `Your TallyJ Verification Code`

**Template** (estimated):

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .code { font-size: 32px; font-weight: bold; text-align: center; 
                background: #f0f0f0; padding: 20px; margin: 20px 0; 
                letter-spacing: 5px; color: #007bff; }
        .footer { font-size: 12px; color: #666; margin-top: 30px; 
                  border-top: 1px solid #ddd; padding-top: 10px; }
    </style>
</head>
<body>
    <div class="container">
        <h2>Your TallyJ Verification Code</h2>
        
        <p>Hello,</p>
        
        <p>You requested a verification code to vote in the following election:</p>
        
        <p><strong>{{ELECTION_NAME}}</strong></p>
        
        <p>Your verification code is:</p>
        
        <div class="code">{{CODE}}</div>
        
        <p>This code will expire in <strong>{{EXPIRES_IN}} minutes</strong>.</p>
        
        <p><strong>Do not share this code with anyone.</strong> TallyJ will never ask you 
        for your verification code via email or phone.</p>
        
        <p>If you did not request this code, please ignore this email.</p>
        
        <div class="footer">
            <p>This is an automated message from TallyJ Election System.<br>
            For support, contact: {{SUPPORT_EMAIL}}</p>
        </div>
    </div>
</body>
</html>
```

**Plain Text Version**:
```
Your TallyJ Verification Code

Hello,

You requested a verification code to vote in: {{ELECTION_NAME}}

Your verification code is: {{CODE}}

This code will expire in {{EXPIRES_IN}} minutes.

Do not share this code with anyone.

If you did not request this code, please ignore this email.

---
TallyJ Election System
Support: {{SUPPORT_EMAIL}}
```

**Variables**:
- `{{CODE}}`: 6-digit verification code
- `{{ELECTION_NAME}}`: Name of the election
- `{{EXPIRES_IN}}`: Minutes until code expires (typically 10-15)
- `{{SUPPORT_EMAIL}}`: Technical support email from configuration

### 3.2 Voter Invitation Email

**Purpose**: Invite registered voters to participate in online election

**Subject**: `You're invited to vote in {{ELECTION_NAME}}`

**Template**:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .button { display: inline-block; padding: 12px 30px; background: #007bff; 
                 color: white; text-decoration: none; border-radius: 5px; 
                 font-weight: bold; margin: 20px 0; }
        .info-box { background: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; 
                    margin: 20px 0; }
        .footer { font-size: 12px; color: #666; margin-top: 30px; 
                  border-top: 1px solid #ddd; padding-top: 10px; }
    </style>
</head>
<body>
    <div class="container">
        <h2>You're Invited to Vote</h2>
        
        <p>Dear {{VOTER_NAME}},</p>
        
        <p>You have been registered to vote in the following election:</p>
        
        <h3>{{ELECTION_NAME}}</h3>
        
        <div class="info-box">
            <strong>Voting Opens:</strong> {{VOTING_OPEN_DATE}}<br>
            <strong>Voting Closes:</strong> {{VOTING_CLOSE_DATE}}<br>
            <strong>Positions to Elect:</strong> {{NUMBER_TO_ELECT}}
        </div>
        
        <p>To cast your vote, click the button below when voting is open:</p>
        
        <a href="{{VOTING_URL}}" class="button">Vote Now</a>
        
        <p>You will be asked to verify your identity using your email address: 
        <strong>{{VOTER_EMAIL}}</strong></p>
        
        <p>If you have any questions, please contact the election administrator.</p>
        
        <div class="footer">
            <p>TallyJ Election System<br>
            Election ID: {{ELECTION_ID}}<br>
            Support: {{SUPPORT_EMAIL}}</p>
        </div>
    </div>
</body>
</html>
```

**Variables**:
- `{{VOTER_NAME}}`: Full name of voter
- `{{VOTER_EMAIL}}`: Voter's email address
- `{{ELECTION_NAME}}`: Name of election
- `{{VOTING_OPEN_DATE}}`: When voting opens
- `{{VOTING_CLOSE_DATE}}`: When voting closes
- `{{NUMBER_TO_ELECT}}`: Positions available
- `{{VOTING_URL}}`: Direct link to voting page
- `{{ELECTION_ID}}`: Election identifier
- `{{SUPPORT_EMAIL}}`: Support contact

### 3.3 Election Results Email

**Purpose**: Send final election results to stakeholders

**Subject**: `{{ELECTION_NAME}} - Final Results`

**Template**:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 800px; margin: 0 auto; padding: 20px; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background: #007bff; color: white; }
        .elected { background: #d4edda; font-weight: bold; }
        .stats { background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .footer { font-size: 12px; color: #666; margin-top: 30px; }
    </style>
</head>
<body>
    <div class="container">
        <h2>{{ELECTION_NAME}} - Final Results</h2>
        
        <div class="stats">
            <strong>Voting Period:</strong> {{VOTING_OPEN}} to {{VOTING_CLOSE}}<br>
            <strong>Total Voters:</strong> {{TOTAL_VOTERS}}<br>
            <strong>Ballots Cast:</strong> {{BALLOTS_CAST}}<br>
            <strong>Voter Turnout:</strong> {{TURNOUT_PERCENTAGE}}%<br>
            <strong>Positions to Elect:</strong> {{NUMBER_TO_ELECT}}
        </div>
        
        <h3>Elected Candidates</h3>
        
        <table>
            <thead>
                <tr>
                    <th>Rank</th>
                    <th>Candidate</th>
                    <th>Votes</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                {{RESULTS_TABLE}}
            </tbody>
        </table>
        
        <p>For detailed results and breakdown, please visit:</p>
        <p><a href="{{RESULTS_URL}}">{{RESULTS_URL}}</a></p>
        
        <div class="footer">
            <p>Generated by TallyJ Election System<br>
            {{GENERATION_TIMESTAMP}}</p>
        </div>
    </div>
</body>
</html>
```

**Variables**:
- `{{ELECTION_NAME}}`: Election name
- `{{VOTING_OPEN}}`: Opening date/time
- `{{VOTING_CLOSE}}`: Closing date/time
- `{{TOTAL_VOTERS}}`: Number of registered voters
- `{{BALLOTS_CAST}}`: Number of ballots submitted
- `{{TURNOUT_PERCENTAGE}}`: Percentage turnout
- `{{NUMBER_TO_ELECT}}`: Positions available
- `{{RESULTS_TABLE}}`: HTML table rows with results
- `{{RESULTS_URL}}`: Link to full results page
- `{{GENERATION_TIMESTAMP}}`: When report was generated

### 3.4 Teller Invitation Email

**Purpose**: Invite tellers to join election as assistants

**Subject**: `You're invited to assist with {{ELECTION_NAME}}`

**Template**:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .access-code { font-size: 24px; font-weight: bold; text-align: center; 
                       background: #fff3cd; padding: 20px; margin: 20px 0; 
                       border: 2px dashed #856404; color: #856404; }
        .button { display: inline-block; padding: 12px 30px; background: #28a745; 
                 color: white; text-decoration: none; border-radius: 5px; 
                 font-weight: bold; margin: 20px 0; }
        .footer { font-size: 12px; color: #666; margin-top: 30px; }
    </style>
</head>
<body>
    <div class="container">
        <h2>You're Invited as a Teller</h2>
        
        <p>Hello,</p>
        
        <p>You have been invited to assist with the following election:</p>
        
        <h3>{{ELECTION_NAME}}</h3>
        
        <p><strong>Election Date:</strong> {{ELECTION_DATE}}</p>
        
        <p>To join as a teller, use the following access code:</p>
        
        <div class="access-code">{{ACCESS_CODE}}</div>
        
        <p><strong>How to Join:</strong></p>
        <ol>
            <li>Visit <a href="{{TALLYJ_URL}}">{{TALLYJ_URL}}</a></li>
            <li>Click "Join as a Teller"</li>
            <li>Select the election: <strong>{{ELECTION_NAME}}</strong></li>
            <li>Enter the access code above</li>
        </ol>
        
        <a href="{{TALLYJ_URL}}" class="button">Join Now</a>
        
        <p><strong>Important:</strong> Keep this access code confidential. 
        Only share with authorized tellers.</p>
        
        <div class="footer">
            <p>TallyJ Election System<br>
            Invitation sent by: {{SENDER_NAME}}<br>
            Support: {{SUPPORT_EMAIL}}</p>
        </div>
    </div>
</body>
</html>
```

**Variables**:
- `{{ELECTION_NAME}}`: Election name
- `{{ELECTION_DATE}}`: Election date
- `{{ACCESS_CODE}}`: Teller access code (from `Election.ElectionPasscode`)
- `{{TALLYJ_URL}}`: TallyJ system URL
- `{{SENDER_NAME}}`: Name of person sending invitation
- `{{SUPPORT_EMAIL}}`: Support email

---

## 4. Email Delivery Implementation

### 4.1 SendGrid API Implementation

**Code**: Estimated implementation based on SendGrid SDK

```csharp
public class SendGridEmailService : IEmailService
{
    private readonly string _apiKey;
    
    public SendGridEmailService()
    {
        _apiKey = ConfigurationManager.AppSettings["SendGridApiKey"];
    }
    
    public async Task<bool> SendEmailAsync(string to, string subject, 
        string htmlContent, string plainTextContent = null)
    {
        try
        {
            var client = new SendGridClient(_apiKey);
            
            var from = new EmailAddress(
                ConfigurationManager.AppSettings["SmtpDefaultFromAddress"] ?? "noreply@tallyj.com",
                ConfigurationManager.AppSettings["SmtpDefaultFromName"] ?? "TallyJ Election System"
            );
            
            var recipient = new EmailAddress(to);
            
            var msg = MailHelper.CreateSingleEmail(
                from, 
                recipient, 
                subject, 
                plainTextContent ?? StripHtml(htmlContent), 
                htmlContent
            );
            
            var response = await client.SendEmailAsync(msg);
            
            // Log delivery
            new LogHelper().Add($"Email sent via SendGrid to {to}: {subject}", true);
            
            return response.StatusCode == System.Net.HttpStatusCode.Accepted 
                || response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            new LogHelper().Add($"SendGrid email error: {ex.Message}", false);
            return false;
        }
    }
    
    private string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }
}
```

### 4.2 SMTP Implementation

**Code**: Estimated implementation using System.Net.Mail

```csharp
public class SmtpEmailService : IEmailService
{
    public async Task<bool> SendEmailAsync(string to, string subject, 
        string htmlContent, string plainTextContent = null)
    {
        try
        {
            var smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
            var smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            var smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
            var smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            var smtpSecure = bool.Parse(ConfigurationManager.AppSettings["SmtpSecure"] ?? "true");
            var fromAddress = ConfigurationManager.AppSettings["SmtpDefaultFromAddress"];
            var fromName = ConfigurationManager.AppSettings["SmtpDefaultFromName"];
            
            // Check for pickup directory (development mode)
            var pickupDirectory = ConfigurationManager.AppSettings["SmtpPickupDirectory"];
            
            using (var client = new SmtpClient())
            {
                if (!string.IsNullOrEmpty(pickupDirectory))
                {
                    // Development: Write emails to directory
                    client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    client.PickupDirectoryLocation = pickupDirectory;
                }
                else
                {
                    // Production: Send via SMTP
                    client.Host = smtpHost;
                    client.Port = smtpPort;
                    client.EnableSsl = smtpSecure;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                }
                
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromAddress, fromName);
                    message.To.Add(to);
                    message.Subject = subject;
                    message.Body = htmlContent;
                    message.IsBodyHtml = true;
                    
                    // Add plain text alternative
                    if (!string.IsNullOrEmpty(plainTextContent))
                    {
                        var plainView = AlternateView.CreateAlternateViewFromString(
                            plainTextContent, 
                            Encoding.UTF8, 
                            "text/plain"
                        );
                        message.AlternateViews.Add(plainView);
                    }
                    
                    await client.SendMailAsync(message);
                    
                    new LogHelper().Add($"Email sent via SMTP to {to}: {subject}", true);
                    
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            new LogHelper().Add($"SMTP email error: {ex.Message}", false);
            return false;
        }
    }
}
```

### 4.3 Fallback Strategy

**Code**: Use SendGrid if available, fall back to SMTP

```csharp
public class EmailService : IEmailService
{
    private readonly IEmailService _primaryService;
    private readonly IEmailService _fallbackService;
    
    public EmailService()
    {
        var sendGridApiKey = ConfigurationManager.AppSettings["SendGridApiKey"];
        
        if (!string.IsNullOrEmpty(sendGridApiKey))
        {
            _primaryService = new SendGridEmailService();
            _fallbackService = new SmtpEmailService();
        }
        else
        {
            _primaryService = new SmtpEmailService();
            _fallbackService = null;
        }
    }
    
    public async Task<bool> SendEmailAsync(string to, string subject, 
        string htmlContent, string plainTextContent = null)
    {
        try
        {
            var success = await _primaryService.SendEmailAsync(to, subject, 
                htmlContent, plainTextContent);
            
            if (success)
            {
                return true;
            }
            
            // Fallback to secondary service if primary fails
            if (_fallbackService != null)
            {
                new LogHelper().Add("Primary email service failed, trying fallback", true);
                return await _fallbackService.SendEmailAsync(to, subject, 
                    htmlContent, plainTextContent);
            }
            
            return false;
        }
        catch (Exception ex)
        {
            new LogHelper().Add($"Email service error: {ex.Message}", false);
            return false;
        }
    }
}
```

---

## 5. Email Sending Examples

### 5.1 Send Verification Code Email

**Code**: `VoterCodeHelper.cs:IssueCode()` (estimated)

```csharp
public object IssueCode(string type, string method, string target)
{
    // ... generate code logic ...
    
    if (method == "email")
    {
        var code = GenerateVerificationCode();
        var subject = "Your TallyJ Verification Code";
        
        var htmlBody = GetEmailTemplate("VerificationCode")
            .Replace("{{CODE}}", code)
            .Replace("{{ELECTION_NAME}}", currentElection.ElectionName)
            .Replace("{{EXPIRES_IN}}", "15")
            .Replace("{{SUPPORT_EMAIL}}", GetSupportEmail());
        
        var emailService = new EmailService();
        var sent = await emailService.SendEmailAsync(target, subject, htmlBody);
        
        return new { Sent = sent, Method = "email" };
    }
}
```

### 5.2 Send Voter Invitation Email

**Code**: Estimated

```csharp
public async Task<bool> SendVoterInvitationAsync(Person voter, Election election)
{
    var subject = $"You're invited to vote in {election.ElectionName}";
    
    var htmlBody = GetEmailTemplate("VoterInvitation")
        .Replace("{{VOTER_NAME}}", voter.C_FullName)
        .Replace("{{VOTER_EMAIL}}", voter.Email)
        .Replace("{{ELECTION_NAME}}", election.ElectionName)
        .Replace("{{VOTING_OPEN_DATE}}", election.OnlineWhenOpen.ToString("f"))
        .Replace("{{VOTING_CLOSE_DATE}}", election.OnlineWhenClose.ToString("f"))
        .Replace("{{NUMBER_TO_ELECT}}", election.NumberToElect.ToString())
        .Replace("{{VOTING_URL}}", GetVotingUrl(election.ElectionGuid))
        .Replace("{{ELECTION_ID}}", election.ElectionGuid.ToString())
        .Replace("{{SUPPORT_EMAIL}}", GetSupportEmail());
    
    var emailService = new EmailService();
    return await emailService.SendEmailAsync(voter.Email, subject, htmlBody);
}
```

### 5.3 Send Election Results Email

**Code**: Estimated

```csharp
public async Task<bool> SendResultsEmailAsync(Election election, List<Result> results, 
    string recipientEmail)
{
    var subject = $"{election.ElectionName} - Final Results";
    
    var resultsTable = BuildResultsTable(results);
    
    var htmlBody = GetEmailTemplate("ElectionResults")
        .Replace("{{ELECTION_NAME}}", election.ElectionName)
        .Replace("{{VOTING_OPEN}}", election.OnlineWhenOpen.ToString("f"))
        .Replace("{{VOTING_CLOSE}}", election.OnlineWhenClose.ToString("f"))
        .Replace("{{TOTAL_VOTERS}}", GetTotalVoters(election).ToString())
        .Replace("{{BALLOTS_CAST}}", GetBallotsCast(election).ToString())
        .Replace("{{TURNOUT_PERCENTAGE}}", CalculateTurnout(election).ToString("F2"))
        .Replace("{{NUMBER_TO_ELECT}}", election.NumberToElect.ToString())
        .Replace("{{RESULTS_TABLE}}", resultsTable)
        .Replace("{{RESULTS_URL}}", GetResultsUrl(election.ElectionGuid))
        .Replace("{{GENERATION_TIMESTAMP}}", DateTime.Now.ToString("f"));
    
    var emailService = new EmailService();
    return await emailService.SendEmailAsync(recipientEmail, subject, htmlBody);
}

private string BuildResultsTable(List<Result> results)
{
    var sb = new StringBuilder();
    var rank = 1;
    
    foreach (var result in results.OrderByDescending(r => r.VoteCount))
    {
        var isElected = result.IsElected ? " class=\"elected\"" : "";
        var status = result.IsElected ? "ELECTED" : "";
        
        sb.AppendLine($"<tr{isElected}>");
        sb.AppendLine($"  <td>{rank}</td>");
        sb.AppendLine($"  <td>{result.PersonName}</td>");
        sb.AppendLine($"  <td>{result.VoteCount}</td>");
        sb.AppendLine($"  <td>{status}</td>");
        sb.AppendLine("</tr>");
        
        rank++;
    }
    
    return sb.ToString();
}
```

---

## 6. Error Handling

### 6.1 Common Email Errors

| Error | Cause | Solution |
|-------|-------|----------|
| Authentication failed | Invalid SMTP credentials | Verify username/password, check for 2FA/App Password requirement |
| Connection timeout | SMTP server unreachable | Check firewall, verify host/port |
| Mailbox unavailable | Invalid recipient email | Validate email format, check for typos |
| Quota exceeded | SendGrid/SMTP daily limit reached | Upgrade plan or wait for reset |
| Spam filter rejection | Email flagged as spam | Improve email content, add SPF/DKIM records |
| TLS/SSL error | Certificate issue | Update .NET Framework, check TLS version support |

### 6.2 Error Handling Code

```csharp
public async Task<EmailResult> SendEmailWithErrorHandlingAsync(string to, 
    string subject, string htmlContent)
{
    try
    {
        var sent = await SendEmailAsync(to, subject, htmlContent);
        
        if (sent)
        {
            return new EmailResult { Success = true };
        }
        else
        {
            return new EmailResult 
            { 
                Success = false, 
                ErrorMessage = "Email delivery failed" 
            };
        }
    }
    catch (SmtpException ex)
    {
        _logger.LogError(ex, "SMTP error sending email to {To}", to);
        
        return new EmailResult
        {
            Success = false,
            ErrorMessage = GetUserFriendlyErrorMessage(ex),
            TechnicalError = ex.Message
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error sending email to {To}", to);
        
        return new EmailResult
        {
            Success = false,
            ErrorMessage = "An unexpected error occurred sending email",
            TechnicalError = ex.Message
        };
    }
}

private string GetUserFriendlyErrorMessage(SmtpException ex)
{
    if (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
    {
        return "Email address is invalid or does not exist";
    }
    else if (ex.StatusCode == SmtpStatusCode.ExceededStorageAllocation)
    {
        return "Recipient's mailbox is full";
    }
    else if (ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
    {
        return "Email server authentication failed";
    }
    else
    {
        return "Email delivery failed. Please try again later.";
    }
}
```

---

## 7. .NET Core Migration Strategy

### 7.1 Technology Mapping

| ASP.NET Framework 4.8 | .NET Core 8 |
|----------------------|-------------|
| `System.Net.Mail.SmtpClient` | **MailKit** (recommended) or `FluentEmail` |
| `SendGrid SDK 9.x` | `SendGrid SDK 9.x` (same, compatible) |
| `ConfigurationManager.AppSettings` | `IConfiguration` dependency injection |

**Why MailKit?**
- ✅ `System.Net.Mail.SmtpClient` is obsolete in .NET Core
- ✅ Better performance and modern protocols
- ✅ Cross-platform compatibility
- ✅ Better error handling

### 7.2 Code Migration Example

#### Current (ASP.NET Framework)
```csharp
using System.Net.Mail;

var client = new SmtpClient("smtp.gmail.com", 587)
{
    EnableSsl = true,
    Credentials = new NetworkCredential(username, password)
};

var message = new MailMessage(from, to, subject, body)
{
    IsBodyHtml = true
};

await client.SendMailAsync(message);
```

#### Target (.NET Core 8 with MailKit)
```csharp
using MailKit.Net.Smtp;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    
    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<bool> SendEmailAsync(string to, string subject, 
        string htmlContent, string plainTextContent = null)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _configuration["TallyJ:Email:FromName"],
                _configuration["TallyJ:Email:FromAddress"]
            ));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlContent,
                TextBody = plainTextContent ?? StripHtml(htmlContent)
            };
            
            message.Body = bodyBuilder.ToMessageBody();
            
            using var client = new SmtpClient();
            
            await client.ConnectAsync(
                _configuration["TallyJ:Email:Smtp:Host"],
                _configuration.GetValue<int>("TallyJ:Email:Smtp:Port"),
                _configuration.GetValue<bool>("TallyJ:Email:Smtp:UseSsl")
            );
            
            await client.AuthenticateAsync(
                _configuration["TallyJ:Email:Smtp:Username"],
                _configuration["TallyJ:Email:Smtp:Password"]
            );
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}
```

### 7.3 Configuration Migration

#### Current (AppSettings.config)
```xml
<add key="SendGridApiKey" value="SG.xxxx" />
<add key="SmtpHost" value="smtp.gmail.com" />
<add key="SmtpPort" value="587" />
<add key="SmtpUsername" value="user@gmail.com" />
<add key="SmtpPassword" value="password" />
```

#### Target (appsettings.json)
```json
{
  "TallyJ": {
    "Email": {
      "Provider": "SendGrid",
      "SendGrid": {
        "ApiKey": "SG.xxxx"
      },
      "Smtp": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "UseSsl": true,
        "Username": "user@gmail.com",
        "Password": "password"
      },
      "FromAddress": "noreply@tallyj.com",
      "FromName": "TallyJ Election System"
    }
  }
}
```

### 7.4 Dependency Injection Registration

**Program.cs**:
```csharp
// Register email service
builder.Services.AddSingleton<IEmailService, EmailService>();

// Or with FluentEmail (alternative)
builder.Services
    .AddFluentEmail(builder.Configuration["TallyJ:Email:FromAddress"], 
                    builder.Configuration["TallyJ:Email:FromName"])
    .AddMailKitSender(new SmtpClientOptions
    {
        Server = builder.Configuration["TallyJ:Email:Smtp:Host"],
        Port = builder.Configuration.GetValue<int>("TallyJ:Email:Smtp:Port"),
        UseSsl = builder.Configuration.GetValue<bool>("TallyJ:Email:Smtp:UseSsl"),
        User = builder.Configuration["TallyJ:Email:Smtp:Username"],
        Password = builder.Configuration["TallyJ:Email:Smtp:Password"]
    });
```

---

## 8. Testing Strategy

### 8.1 Unit Tests
```csharp
[Fact]
public async Task SendEmailAsync_ValidRecipient_ReturnsTrue()
{
    // Arrange
    var mockConfig = CreateMockConfiguration();
    var emailService = new EmailService(mockConfig, _logger);
    
    // Act
    var result = await emailService.SendEmailAsync(
        "test@example.com", 
        "Test Subject", 
        "<p>Test body</p>"
    );
    
    // Assert
    Assert.True(result);
}
```

### 8.2 Integration Tests
```csharp
[Fact]
public async Task SendEmail_ToMailtrap_Succeeds()
{
    // Mailtrap: Test SMTP service that captures emails
    var emailService = new EmailService(_configuration, _logger);
    
    var result = await emailService.SendEmailAsync(
        "recipient@example.com",
        "Integration Test",
        "<p>This is a test email</p>"
    );
    
    Assert.True(result);
    // Verify email in Mailtrap inbox via API
}
```

### 8.3 Manual Testing Checklist
- [ ] Send verification code email (HTML)
- [ ] Send voter invitation email
- [ ] Send election results email
- [ ] Send teller invitation email
- [ ] Test SMTP fallback when SendGrid unavailable
- [ ] Test pickup directory mode (development)
- [ ] Test invalid email address error handling
- [ ] Test SMTP authentication failure
- [ ] Verify email rendering in multiple clients (Gmail, Outlook, Apple Mail)

---

## 9. Security Best Practices

### 9.1 Configuration Security
- ✅ Store SendGrid API key in Azure Key Vault or AWS Secrets Manager
- ✅ Use environment-specific configurations (dev/prod)
- ✅ Rotate SendGrid API key every 90 days
- ✅ Use least-privilege API keys (Mail Send only, not full access)

### 9.2 Email Content Security
- ✅ Sanitize email addresses (prevent header injection)
- ✅ Use HTML encoding for all dynamic content
- ✅ Add SPF and DKIM records to domain
- ✅ Implement DMARC policy
- ✅ Avoid clickbait subject lines (spam filters)

### 9.3 Privacy & Compliance
- ✅ Include unsubscribe link in marketing emails
- ✅ Add physical address in footer (CAN-SPAM Act)
- ✅ Honor email preferences (don't email if user opted out)
- ✅ Encrypt emails with sensitive data (optional: PGP/S/MIME)

---

## 10. Monitoring & Logging

### 10.1 Events to Log

| Event | Log Level | Data to Include |
|-------|-----------|----------------|
| Email sent successfully | Info | Recipient (masked), Subject, Provider (SendGrid/SMTP) |
| Email delivery failed | Warning | Recipient (masked), Error message |
| SendGrid API error | Error | Error code, Error message |
| SMTP authentication failed | Error | SMTP host, Username |

### 10.2 Metrics to Track
- Email delivery success rate (per provider)
- Email delivery latency
- SendGrid quota usage
- Bounce rate
- Spam complaint rate

---

## 11. Quick Reference

### 11.1 Configuration Summary

| Setting | Required | Format |
|---------|----------|--------|
| `SendGridApiKey` | Yes (if using SendGrid) | SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx |
| `SmtpHost` | Yes (if using SMTP) | smtp.example.com |
| `SmtpPort` | Yes | 587 |
| `SmtpUsername` | Yes | username or email |
| `SmtpPassword` | Yes | password or API key |
| `SmtpDefaultFromAddress` | Yes | email@example.com |

### 11.2 Common SMTP Ports

| Port | Protocol | Security | Use Case |
|------|----------|----------|----------|
| 25 | SMTP | None | Server-to-server (often blocked by ISPs) |
| 587 | SMTP | STARTTLS | Recommended for client submission |
| 465 | SMTP | SSL/TLS | Legacy (still supported) |

### 11.3 Email Template Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{{CODE}}` | Verification code | `123456` |
| `{{ELECTION_NAME}}` | Election name | `Annual Board Election 2026` |
| `{{VOTER_NAME}}` | Full name | `John Doe` |
| `{{EXPIRES_IN}}` | Minutes until expiration | `15` |
| `{{SUPPORT_EMAIL}}` | Support contact | `support@tallyj.com` |

---

**End of Email Integration Documentation**
