using Backend.Entities;
using Backend.DTOs.OnlineVoting;
using Backend.DTOs.SignalR;
using Backend.Helpers;
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
            var paidChannel = PaidDestinationPhone.IsPaidChannel(dto.DeliveryMethod);

            // 1. Paid channels: reject reserved/fictional/malformed destinations before any DB or provider work.
            if (dto.VoterIdType == "P" && paidChannel)
            {
                if (!PaidDestinationPhone.TryExplain(dto.VoterId, out var reason))
                {
                    _logger.LogWarning(
                        "Login code request rejected: paid destination blocked ({Reason})",
                        reason);
                    return BuildRequestCodeResponse("voting.auth.requestCode.invalidPhone");
                }
            }

            // 2. Phone + paid: durable SmsStatus on an existing phone row blocks send (null = not yet checked).
            //    Load once here and reuse below — do not create a row just to store status.
            var phonePaid = dto.VoterIdType == "P" && paidChannel;
            OnlineVoter? onlineVoter = null;
            if (phonePaid)
            {
                onlineVoter = await _context.OnlineVoters
                    .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId && ov.VoterIdType == "P");

                if (onlineVoter != null && !OnlineVoterSmsStatus.AllowsPaidSend(onlineVoter.SmsStatus))
                {
                    _logger.LogWarning(
                        "Login code request skipped: SmsStatus blocks paid send ({Method}, {SmsStatus})",
                        KnownDeliveryMethod(dto.DeliveryMethod),
                        SanitizeForLog(onlineVoter.SmsStatus));
                    return BuildRequestCodeResponse("voting.auth.requestCode.invalidPhone");
                }

                // WhatsApp presence is separate from SmsStatus. Skip WhatsApp send only.
                if (onlineVoter != null
                    && dto.DeliveryMethod == "whatsapp"
                    && !OnlineVoterWhatsAppStatus.AllowsSend(onlineVoter.WhatsAppStatus))
                {
                    _logger.LogWarning(
                        "Login code request skipped: WhatsAppStatus blocks WhatsApp send ({WhatsAppStatus})",
                        SanitizeForLog(onlineVoter.WhatsAppStatus));
                    return BuildRequestCodeResponse("voting.auth.requestCode.invalidPhone");
                }
            }

            // 3. Find all open elections where this voter is registered (SMS pumping prevention)
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

            // 4. Check if voter is registered in ANY of the open elections
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
                _logger.LogWarning("Login code request rejected: voter not found in any open election (type: {VoterIdType})",
                    KnownVoterIdType(dto.VoterIdType));
                return BuildRequestCodeResponse("voting.auth.requestCode.notRegistered");
            }

            // Kiosk login is teller-stamped and election-scoped; do not create a global row here.
            if (dto.VoterIdType == KioskCodeLifetime.VoterIdType)
            {
                return BuildRequestCodeResponse("voting.auth.requestCode.sent");
            }

            // 5. Create or update OnlineVoter record for tracking (reuse the phone+paid load when present)
            if (onlineVoter == null)
            {
                var alreadyLookedUp = phonePaid;
                if (!alreadyLookedUp)
                {
                    onlineVoter = await _context.OnlineVoters
                        .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId);
                }

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
            }

            // Person create/import may have inserted the phone row with WhenRegistered null.
            // First successful code request is the first auth use — stamp it then, never earlier.
            if (onlineVoter.WhenRegistered == null)
            {
                onlineVoter.WhenRegistered = DateTimeOffset.UtcNow;
            }

            var verifyCode = GenerateVerificationCode();

            onlineVoter.VerifyCode = verifyCode;
            onlineVoter.VerifyCodeDate = DateTimeOffset.UtcNow;
            onlineVoter.VerifyAttempts = 0;
            onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            var channel = TryIssueDeliveryChannel();
            if (channel != null)
            {
                await PushDeliveryStatusAsync(channel.ChannelId, VoterCodeDeliveryStatusDto.Sending());
            }

            var sent = await SendVerificationCodeAsync(dto.VoterId, dto.DeliveryMethod, verifyCode);

            if (channel != null)
            {
                await CompleteSendPathStatusAsync(
                    channel.ChannelId,
                    dto.VoterId,
                    dto.DeliveryMethod,
                    sent);
            }

            var messageKey = sent
                ? "voting.auth.requestCode.sent"
                : "voting.auth.requestCode.sendFailed";

            _logger.LogInformation("Verification code sent via {Method} (registered in {Count} open election(s))",
                KnownDeliveryMethod(dto.DeliveryMethod), openElections.Count);

            return BuildRequestCodeResponse(messageKey, verifyCode, channel?.RawToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting verification code");
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
                if (kioskResult.Success || kioskResult.Error == VoterVerifyError.CodeExpired)
                {
                    return kioskResult;
                }
            }

            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId);

            if (onlineVoter == null)
            {
                return (false, VoterVerifyError.VoterNotFound, null);
            }

            if (string.IsNullOrEmpty(onlineVoter.VerifyCode))
            {
                return (false, VoterVerifyError.MissingCodeKey(onlineVoter.VerifyCodeDate), null);
            }

            if (onlineVoter.VerifyCodeDate == null ||
                onlineVoter.VerifyCodeDate.Value.AddMinutes(15) < DateTimeOffset.UtcNow)
            {
                return (false, VoterVerifyError.CodeExpired, null);
            }

            if (onlineVoter.VerifyAttempts >= VoterVerifyError.MaxAttempts)
            {
                return (false, VoterVerifyError.TooManyAttempts, null);
            }

            if (onlineVoter.VerifyCode != dto.VerifyCode)
            {
                onlineVoter.VerifyAttempts = (onlineVoter.VerifyAttempts ?? 0) + 1;
                await _context.SaveChangesAsync();

                if (onlineVoter.VerifyAttempts >= VoterVerifyError.MaxAttempts)
                {
                    return (false, VoterVerifyError.TooManyAttempts, null);
                }

                var remaining = VoterVerifyError.MaxAttempts - onlineVoter.VerifyAttempts.Value;
                return (false, VoterVerifyError.InvalidCodeWithAttempts(remaining), null);
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
            _logger.LogInformation("Voter authenticated successfully");

            return (true, null, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying code");
            return (false, VoterVerifyError.Error, null);
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
        _logger.LogInformation("Sending verification code via {Method}", KnownDeliveryMethod(method));

        try
        {
            return method switch
            {
                "email" => await SendEmailCodeAsync(recipient, code),
                "sms" => await _paidVerificationSender.SendSmsAsync(recipient, code),
                "voice" => await _paidVerificationSender.SendVoiceAsync(recipient, code),
                "whatsapp" => await _paidVerificationSender.SendWhatsAppAsync(recipient, code),
                _ => throw new ArgumentException($"Unknown delivery method: {method}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification code via {Method}", KnownDeliveryMethod(method));
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
        _logger.LogInformation("Email verification code sent");
        return true;
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
            return (false, VoterVerifyError.VoterNotFound, null);
        }

        var people = await _context.People
            .Where(p => openElectionGuids.Contains(p.ElectionGuid) &&
                        p.KioskCode != null &&
                        p.KioskCode != string.Empty &&
                        p.KioskCode.ToUpper() == normalizedCode)
            .ToListAsync();

        if (people.Count == 0)
        {
            return (false, VoterVerifyError.VoterNotFound, null);
        }

        OnlineVoter? onlineVoter = null;
        foreach (var candidate in people)
        {
            var scopedId = KioskCodeLifetime.ToVoterId(candidate.ElectionGuid, normalizedCode);
            var row = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov =>
                    ov.VoterId == scopedId &&
                    ov.VoterIdType == KioskCodeLifetime.VoterIdType);
            if (row == null || !KioskCodeLifetime.IsLoginWindowOpen(row.VerifyCodeDate, now))
            {
                continue;
            }

            if (onlineVoter != null)
            {
                return (false, VoterVerifyError.VoterNotFound, null);
            }

            onlineVoter = row;
        }

        if (onlineVoter == null)
        {
            return (false, VoterVerifyError.CodeExpired, null);
        }

        onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;
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
        _logger.LogInformation("Voter authenticated via direct kiosk/personal code");
        return (true, null, response);
    }

    private RequestCodeResponseDto BuildRequestCodeResponse(
        string messageKey,
        string? verifyCode = null,
        string? channelToken = null)
    {
        var echoDevCode = (_hostEnvironment.IsDevelopment() || _hostEnvironment.IsEnvironment("Testing"))
                          && !string.IsNullOrEmpty(verifyCode);

        return new RequestCodeResponseDto
        {
            MessageKey = messageKey,
            DevVerificationCode = echoDevCode ? verifyCode : null,
            ChannelToken = channelToken
        };
    }

    private VoterCodeChannelIssue? TryIssueDeliveryChannel()
    {
        if (_voterCodeChannels == null)
        {
            return null;
        }

        var issued = _voterCodeChannels.Issue();
        _logger.LogInformation("Issued voter-code delivery channel {ChannelId}", issued.ChannelId);
        return issued;
    }

    private async Task CompleteSendPathStatusAsync(
        string channelId,
        string voterId,
        string deliveryMethod,
        bool sent)
    {
        var awaitsProviderCallback = sent
            && (deliveryMethod is "sms" or "voice")
            && await TryBindLatestSmsSidAsync(channelId, voterId);

        if (sent)
        {
            await PushDeliveryStatusAsync(channelId, VoterCodeDeliveryStatusDto.Sent());
            if (!awaitsProviderCallback)
            {
                await PushDeliveryStatusAsync(channelId, VoterCodeDeliveryStatusDto.Final(true));
            }

            return;
        }

        await PushDeliveryStatusAsync(channelId, VoterCodeDeliveryStatusDto.Failed());
        await PushDeliveryStatusAsync(channelId, VoterCodeDeliveryStatusDto.Final(false));
    }

    private async Task<bool> TryBindLatestSmsSidAsync(string channelId, string phone)
    {
        if (_voterCodeChannels == null)
        {
            return false;
        }

        var sid = await _context.SmsLogs
            .AsNoTracking()
            .Where(l => l.Phone == phone)
            .OrderByDescending(l => l.RowId)
            .Select(l => l.SmsSid)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(sid))
        {
            return false;
        }

        _voterCodeChannels.BindProviderSid(channelId, sid);
        return true;
    }

    private async Task PushDeliveryStatusAsync(string channelId, VoterCodeDeliveryStatusDto status)
    {
        _voterCodeChannels?.RecordStatus(channelId, status);
        await _signalRNotificationService.SendVoterCodeDeliveryStatusAsync(channelId, status);
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
