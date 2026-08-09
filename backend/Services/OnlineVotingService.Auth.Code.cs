using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Entities;
using Backend.DTOs.OnlineVoting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MimeKit;

namespace Backend.Services;

public partial class OnlineVotingService
{
    /// <inheritdoc/>
    public async Task<RequestCodeResponseDto> RequestVerificationCodeAsync(RequestCodeDto dto)
    {
        try
        {
            // 1. Find all open elections where this voter is registered (SMS pumping prevention)
            var now = DateTimeOffset.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.UseOnlineVoting &&
                           e.OnlineWhenOpen != null && e.OnlineWhenOpen <= now &&
                           (e.OnlineWhenClose == null || e.OnlineWhenClose > now))
                .Select(e => e.ElectionGuid)
                .ToListAsync();

            if (!openElections.Any())
            {
                _logger.LogWarning("Login code request rejected: No elections currently open for online voting");
                return BuildRequestCodeResponse("voting.auth.requestCode.noOpenElections");
            }

            // 2. Check if voter is registered in ANY of the open elections
            var isVoterRegistered = dto.VoterIdType switch
            {
                "E" => await _context.People.AnyAsync(p =>
                    openElections.Contains(p.ElectionGuid) && p.Email == dto.VoterId),
                "P" => await _context.People.AnyAsync(p =>
                    openElections.Contains(p.ElectionGuid) && p.Phone == dto.VoterId),
                "C" => await _context.People.AnyAsync(p =>
                    openElections.Contains(p.ElectionGuid) && p.KioskCode == dto.VoterId),
                _ => false
            };

            if (!isVoterRegistered)
            {
                _logger.LogWarning("Login code request rejected: VoterId {VoterId} (type: {VoterIdType}) not found in any open election",
                    dto.VoterId, dto.VoterIdType);
                return BuildRequestCodeResponse("voting.auth.requestCode.notRegistered");
            }

            // 3. Create or update OnlineVoter record for tracking
            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId);

            if (onlineVoter == null)
            {
                onlineVoter = new OnlineVoter
                {
                    VoterId = dto.VoterId,
                    VoterIdType = dto.VoterIdType,
                    WhenRegistered = DateTimeOffset.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            var verifyCode = GenerateVerificationCode();

            onlineVoter.VerifyCode = verifyCode;
            onlineVoter.VerifyCodeDate = DateTimeOffset.UtcNow;
            onlineVoter.VerifyAttempts = 0;
            onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            var sent = await SendVerificationCodeAsync(dto.VoterId, dto.DeliveryMethod, verifyCode);

            var messageKey = sent
                ? "voting.auth.requestCode.sent"
                : "voting.auth.requestCode.sendFailed";

            _logger.LogInformation("Verification code sent to {VoterId} via {Method} (registered in {Count} open election(s))",
                dto.VoterId, dto.DeliveryMethod, openElections.Count);

            return BuildRequestCodeResponse(messageKey, verifyCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting verification code for {VoterId}", dto.VoterId);
            return BuildRequestCodeResponse("voting.auth.requestCode.error");
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> VerifyCodeAsync(VerifyCodeDto dto)
    {
        try
        {
            if (string.Equals(dto.VoterId, dto.VerifyCode, StringComparison.OrdinalIgnoreCase))
            {
                var kioskResult = await TryAuthenticateWithDirectCodeAsync(dto.VoterId);
                if (kioskResult.Success)
                {
                    return kioskResult;
                }
            }

            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId);

            if (onlineVoter == null)
            {
                return (false, "voting.auth.verify.voterNotFound", null);
            }

            if (string.IsNullOrEmpty(onlineVoter.VerifyCode))
            {
                return (false, "voting.auth.verify.noCodeFound", null);
            }

            if (onlineVoter.VerifyCodeDate == null ||
                onlineVoter.VerifyCodeDate.Value.AddMinutes(15) < DateTimeOffset.UtcNow)
            {
                return (false, "voting.auth.verify.codeExpired", null);
            }

            if (onlineVoter.VerifyAttempts >= 5)
            {
                return (false, "voting.auth.verify.tooManyAttempts", null);
            }

            if (onlineVoter.VerifyCode != dto.VerifyCode)
            {
                onlineVoter.VerifyAttempts = (onlineVoter.VerifyAttempts ?? 0) + 1;
                await _context.SaveChangesAsync();

                return (false, $"voting.auth.verify.invalidCode:{5 - onlineVoter.VerifyAttempts}", null);
            }

            onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;
            onlineVoter.VerifyCode = null;
            onlineVoter.VerifyAttempts = 0;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(onlineVoter);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

            var response = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = expiresAt
            };

            await NotifyLoginElsewhereAsync(onlineVoter.VoterId);
            _logger.LogInformation("Voter {VoterId} authenticated successfully", dto.VoterId);

            return (true, null, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying code for {VoterId}", dto.VoterId);
            return (false, "voting.auth.verify.error", null);
        }
    }

    /// <summary>
    /// Generates a random 6-character verification code using alphanumeric characters.
    /// </summary>
    /// <returns>A 6-character verification code.</returns>
    private string GenerateVerificationCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Sends a verification code to the recipient using the specified delivery method.
    /// </summary>
    /// <param name="recipient">The recipient's contact information (email or phone).</param>
    /// <param name="method">The delivery method (email, sms, voice, whatsapp).</param>
    /// <param name="code">The verification code to send.</param>
    /// <returns>True if the code was sent successfully, false otherwise.</returns>
    private async Task<bool> SendVerificationCodeAsync(string recipient, string method, string code)
    {
        _logger.LogInformation("Sending verification code to {Recipient} via {Method}", recipient, method);

        try
        {
            return method switch
            {
                "email" => await SendEmailCodeAsync(recipient, code),
                "sms" => await SendSmsCodeAsync(recipient, code),
                "voice" => await SendVoiceCodeAsync(recipient, code),
                "whatsapp" => await SendWhatsAppCodeAsync(recipient, code),
                _ => throw new ArgumentException($"Unknown delivery method: {method}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification code to {Recipient} via {Method}", recipient, method);
            return false;
        }
    }

    /// <summary>
    /// Sends a verification code via email using SMTP.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="code">The verification code to send.</param>
    /// <returns>True if the email was sent successfully, false otherwise.</returns>
    private async Task<bool> SendEmailCodeAsync(string email, string code)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "TallyJ4",
            _configuration["Email:FromAddress"] ?? "noreply@tallyj.local"));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = "Your TallyJ Voting Code";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"<h2>Your Voting Verification Code</h2>
<p>Your one-time code is: <strong style=""font-size:1.5em;letter-spacing:0.15em"">{code}</strong></p>
<p>This code expires in 15 minutes.</p>
<p>If you did not request this code, please ignore this email.</p>",
            TextBody = $"Your TallyJ voting code is: {code}\n\nThis code expires in 15 minutes."
        };
        message.Body = bodyBuilder.ToMessageBody();

        await _emailSender.SendAsync(message);
        _logger.LogInformation("Email verification code sent to {Email}", email);
        return true;
    }

    /// <summary>
    /// Sends a verification code via SMS using Twilio API.
    /// </summary>
    /// <param name="phone">The recipient's phone number.</param>
    /// <param name="code">The verification code to send.</param>
    /// <returns>True if the SMS was sent successfully, false otherwise.</returns>
    private async Task<bool> SendSmsCodeAsync(string phone, string code)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid) || accountSid.StartsWith("<"))
        {
            _logger.LogWarning("Twilio not configured; skipping SMS for {Phone}", phone);
            return true;
        }

        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", phone),
            new KeyValuePair<string, string>("From", fromNumber ?? ""),
            new KeyValuePair<string, string>("Body", $"Your TallyJ voting code is: {code}\n\nThis code expires in 15 minutes.")
        });

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
            formData);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Twilio SMS failed for {Phone}: {Status} - {Body}", phone, response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("SMS verification code sent to {Phone}", phone);
        return true;
    }

    /// <summary>
    /// Sends a verification code via voice call using Twilio API.
    /// </summary>
    /// <param name="phone">The recipient's phone number.</param>
    /// <param name="code">The verification code to send.</param>
    /// <returns>True if the voice call was initiated successfully, false otherwise.</returns>
    private async Task<bool> SendVoiceCodeAsync(string phone, string code)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid) || accountSid.StartsWith("<"))
        {
            _logger.LogWarning("Twilio not configured; skipping voice call for {Phone}", phone);
            return true;
        }

        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        var spokenCode = string.Join(". ", code.ToCharArray());
        var twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
  <Say language=""en-US"">Your TallyJ voting code is: {spokenCode}. I repeat: {spokenCode}. This code expires in 15 minutes.</Say>
</Response>";

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", phone),
            new KeyValuePair<string, string>("From", fromNumber ?? ""),
            new KeyValuePair<string, string>("Twiml", twiml)
        });

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Calls.json",
            formData);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Twilio voice call failed for {Phone}: {Status} - {Body}", phone, response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("Voice verification code sent to {Phone}", phone);
        return true;
    }

    /// <summary>
    /// Sends a verification code via WhatsApp using GreenAPI.
    /// </summary>
    /// <param name="phone">The recipient's phone number.</param>
    /// <param name="code">The verification code to send.</param>
    /// <returns>True if the WhatsApp message was sent successfully, false otherwise.</returns>
    private async Task<bool> SendWhatsAppCodeAsync(string phone, string code)
    {
        var idInstance = _configuration["GreenApi:IdInstance"];
        var apiToken = _configuration["GreenApi:ApiToken"];
        var baseUrl = _configuration["GreenApi:BaseUrl"] ?? "https://api.green-api.com";

        if (string.IsNullOrWhiteSpace(idInstance) || idInstance.StartsWith("<"))
        {
            _logger.LogWarning("GreenAPI not configured; skipping WhatsApp for {Phone}", phone);
            return true;
        }

        var normalizedPhone = NormalizePhoneForWhatsApp(phone);
        var chatId = $"{normalizedPhone}@c.us";

        var client = _httpClientFactory.CreateClient("GreenApi");
        var url = $"{baseUrl}/waInstance{idInstance}/sendMessage/{apiToken}";

        var payload = new
        {
            chatId,
            message = $"Your TallyJ voting code is: {code}\n\nThis code expires in 15 minutes."
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("GreenAPI WhatsApp failed for {Phone}: {Status} - {Body}", phone, response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("WhatsApp verification code sent to {Phone}", phone);
        return true;
    }

    /// <summary>
    /// Normalizes a phone number for WhatsApp by extracting only the digits.
    /// </summary>
    /// <param name="phone">The phone number to normalize.</param>
    /// <returns>The normalized phone number containing only digits.</returns>
    private static string NormalizePhoneForWhatsApp(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits;
    }

    private async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> TryAuthenticateWithDirectCodeAsync(string code)
    {
        var normalizedCode = NormalizeVoterCode(code);
        var now = DateTimeOffset.UtcNow;

        var openElectionGuids = await _context.Elections
            .Where(e => e.UseOnlineVoting &&
                        e.OnlineWhenOpen != null && e.OnlineWhenOpen <= now &&
                        (e.OnlineWhenClose == null || e.OnlineWhenClose > now))
            .Select(e => e.ElectionGuid)
            .ToListAsync();

        if (!openElectionGuids.Any())
        {
            return (false, "voting.auth.verify.voterNotFound", null);
        }

        var person = await _context.People
            .FirstOrDefaultAsync(p => openElectionGuids.Contains(p.ElectionGuid) &&
                                      p.KioskCode != null &&
                                      p.KioskCode.ToUpper() == normalizedCode);

        if (person == null)
        {
            return (false, "voting.auth.verify.voterNotFound", null);
        }

        var onlineVoter = await _context.OnlineVoters
            .FirstOrDefaultAsync(ov => ov.VoterId == normalizedCode);

        if (onlineVoter == null)
        {
            onlineVoter = new OnlineVoter
            {
                VoterId = normalizedCode,
                VoterIdType = "C",
                WhenRegistered = DateTimeOffset.UtcNow
            };
            _context.OnlineVoters.Add(onlineVoter);
        }
        else
        {
            onlineVoter.VoterIdType = "C";
        }

        onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;
        onlineVoter.VerifyCode = null;
        onlineVoter.VerifyAttempts = 0;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(onlineVoter);
        var response = new OnlineVoterAuthResponse
        {
            Token = token,
            VoterId = onlineVoter.VoterId,
            VoterIdType = onlineVoter.VoterIdType,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        await NotifyLoginElsewhereAsync(onlineVoter.VoterId);
        var safeVoterIdForLog = SanitizeForLog(normalizedCode);
        _logger.LogInformation("Voter {VoterId} authenticated via direct kiosk/personal code", safeVoterIdForLog);
        return (true, null, response);
    }

    private RequestCodeResponseDto BuildRequestCodeResponse(string messageKey, string? verifyCode = null)
    {
        var echoDevCode = (_hostEnvironment.IsDevelopment() || _hostEnvironment.IsEnvironment("Testing"))
                          && !string.IsNullOrEmpty(verifyCode);

        return new RequestCodeResponseDto
        {
            MessageKey = messageKey,
            DevVerificationCode = echoDevCode ? verifyCode : null
        };
    }

    private static string NormalizeVoterCode(string code)
    {
        var trimmed = code.Trim();
        if (trimmed.StartsWith("K_", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..];
        }

        return trimmed.ToUpperInvariant();
    }
}
