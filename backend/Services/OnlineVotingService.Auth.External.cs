using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.Entities;
using Backend.DTOs.OnlineVoting;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class OnlineVotingService
{
    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> AuthenticateVoterWithGoogleAsync(GoogleAuthForVoterDto dto)
    {
        try
        {
            var validation = await _googleIdTokenValidator.ValidateAsync(dto.Credential);
            if (validation == null)
            {
                var googleClientId = _configuration["Google:ClientId"];
                if (string.IsNullOrWhiteSpace(googleClientId) || googleClientId.StartsWith('<'))
                {
                    _logger.LogWarning("Google OAuth attempted but Google Client ID is not configured");
                    return (false, "voting.auth.google.notConfigured", null);
                }

                _logger.LogWarning("Google OAuth for voter: Invalid Google ID token");
                return (false, "voting.auth.google.invalidCredential", null);
            }

            var email = validation.Email;
            if (string.IsNullOrEmpty(email))
            {
                return (false, "voting.auth.google.noEmail", null);
            }

            if (!validation.EmailVerified)
            {
                _logger.LogWarning("Google OAuth for voter: Email {Email} not verified by Google", email);
                return (false, "voting.auth.google.emailNotVerified", null);
            }

            // 3. Find all open elections where this voter is registered
            var now = DateTimeOffset.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.UseOnlineVoting &&
                           e.OnlineWhenOpen != null &&
                           e.OnlineWhenClose != null &&
                           e.OnlineWhenOpen <= now &&
                           e.OnlineWhenClose >= now)
                .Select(e => e.ElectionGuid)
                .ToListAsync();

            if (!openElections.Any())
            {
                _logger.LogWarning("Google OAuth rejected: No elections currently open for online voting");
                return (false, "voting.auth.google.noOpenElections", null);
            }

            // 4. Check if voter's email is registered in ANY of the open elections
            var isVoterRegistered = await _context.People
                .AnyAsync(p => openElections.Contains(p.ElectionGuid) && p.Email == email);

            if (!isVoterRegistered)
            {
                _logger.LogWarning("Google OAuth rejected: Email {Email} not found in any open election", email);
                return (false, "voting.auth.google.notRegistered", null);
            }

            // 5. Create or update OnlineVoter record for tracking
            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == email);

            if (onlineVoter == null)
            {
                onlineVoter = new OnlineVoter
                {
                    VoterId = email,
                    VoterIdType = "E",
                    WhenRegistered = DateTimeOffset.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            // 6. Generate JWT token (same format as code verification)
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
            _logger.LogInformation("Voter {Email} authenticated successfully via Google OAuth (registered in {Count} open election(s))",
                email, openElections.Count);

            return (true, null, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating voter with Google");
            return (false, "voting.auth.google.error", null);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> FacebookAuthAsync(FacebookAuthForVoterDto dto)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Facebook");
            var response = await client.GetAsync($"/me?fields=id,email&access_token={Uri.EscapeDataString(dto.AccessToken)}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook Graph API returned non-success for voter auth: {Status}", response.StatusCode);
                return (false, "voting.auth.facebook.invalidToken", null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("email", out var emailElement))
            {
                return (false, "voting.auth.facebook.noEmail", null);
            }

            var email = emailElement.GetString();
            if (string.IsNullOrEmpty(email))
            {
                return (false, "voting.auth.facebook.noEmail", null);
            }

            var now = DateTimeOffset.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.UseOnlineVoting &&
                           e.OnlineWhenOpen != null &&
                           e.OnlineWhenClose != null &&
                           e.OnlineWhenOpen <= now &&
                           e.OnlineWhenClose >= now)
                .Select(e => e.ElectionGuid)
                .ToListAsync();

            if (!openElections.Any())
            {
                return (false, "voting.auth.facebook.noOpenElections", null);
            }

            var isVoterRegistered = await _context.People
                .AnyAsync(p => openElections.Contains(p.ElectionGuid) && p.Email == email);

            if (!isVoterRegistered)
            {
                _logger.LogWarning("Facebook auth rejected: Email {Email} not found in any open election", email);
                return (false, "voting.auth.facebook.notRegistered", null);
            }

            var onlineVoter = await _context.OnlineVoters.FirstOrDefaultAsync(ov => ov.VoterId == email);
            if (onlineVoter == null)
            {
                onlineVoter = new OnlineVoter
                {
                    VoterId = email,
                    VoterIdType = "E",
                    WhenRegistered = DateTimeOffset.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(onlineVoter);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

            var authResponse = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = expiresAt
            };

            await NotifyLoginElsewhereAsync(onlineVoter.VoterId);

            _logger.LogInformation("Voter {Email} authenticated via Facebook OAuth (in {Count} open election(s))", email, openElections.Count);
            return (true, null, authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating voter with Facebook");
            return (false, "voting.auth.facebook.error", null);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> KakaoAuthAsync(KakaoAuthForVoterDto dto)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Kakao");
            var request = new HttpRequestMessage(HttpMethod.Get, "/v2/user/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dto.AccessToken);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kakao API returned non-success for voter auth: {Status}", response.StatusCode);
                return (false, "voting.auth.kakao.invalidToken", null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            string? email = null;
            string? phone = null;

            if (doc.RootElement.TryGetProperty("kakao_account", out var account))
            {
                if (account.TryGetProperty("email", out var emailEl))
                    email = emailEl.GetString();

                if (account.TryGetProperty("phone_number", out var phoneEl))
                    phone = NormalizeKakaoPhone(phoneEl.GetString());
            }

            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                return (false, "voting.auth.kakao.noContact", null);
            }

            var now = DateTimeOffset.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.UseOnlineVoting &&
                           e.OnlineWhenOpen != null &&
                           e.OnlineWhenClose != null &&
                           e.OnlineWhenOpen <= now &&
                           e.OnlineWhenClose >= now)
                .Select(e => e.ElectionGuid)
                .ToListAsync();

            if (!openElections.Any())
            {
                return (false, "voting.auth.kakao.noOpenElections", null);
            }

            string? matchedVoterId = null;
            string? matchedVoterIdType = null;

            if (!string.IsNullOrEmpty(email))
            {
                var found = await _context.People.AnyAsync(p => openElections.Contains(p.ElectionGuid) && p.Email == email);
                if (found) { matchedVoterId = email; matchedVoterIdType = "E"; }
            }

            if (matchedVoterId == null && !string.IsNullOrEmpty(phone))
            {
                var found = await _context.People.AnyAsync(p => openElections.Contains(p.ElectionGuid) && p.Phone == phone);
                if (found) { matchedVoterId = phone; matchedVoterIdType = "P"; }
            }

            if (matchedVoterId == null)
            {
                _logger.LogWarning("Kakao auth rejected: email/phone not found in any open election");
                return (false, "voting.auth.kakao.notRegistered", null);
            }

            var onlineVoter = await _context.OnlineVoters.FirstOrDefaultAsync(ov => ov.VoterId == matchedVoterId);
            if (onlineVoter == null)
            {
                onlineVoter = new OnlineVoter
                {
                    VoterId = matchedVoterId,
                    VoterIdType = matchedVoterIdType!,
                    WhenRegistered = DateTimeOffset.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(onlineVoter);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

            var authResponse = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = expiresAt
            };

            await NotifyLoginElsewhereAsync(onlineVoter.VoterId);
            _logger.LogInformation("Voter {VoterId} authenticated via Kakao OAuth (in {Count} open election(s))", matchedVoterId, openElections.Count);
            return (true, null, authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating voter with Kakao");
            return (false, "voting.auth.kakao.error", null);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> TelegramAuthAsync(TelegramAuthForVoterDto dto)
    {
        try
        {
            if (!ValidateTelegramHash(dto.Id, dto.FirstName, dto.LastName, dto.Username, dto.PhotoUrl, dto.AuthDate, dto.Hash))
            {
                _logger.LogWarning("Telegram voter auth: invalid hash for Telegram ID {TelegramId}", dto.Id);
                return (false, "voting.auth.telegram.invalidHash", null);
            }

            // Reject auth data older than 24 hours
            var authAge = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - dto.AuthDate;
            if (authAge > 86400)
            {
                _logger.LogWarning("Telegram voter auth: auth_date too old for Telegram ID {TelegramId}", dto.Id);
                return (false, "voting.auth.telegram.expired", null);
            }

            var telegramVoterId = dto.Id.ToString();

            // Check if there is an open election where this voter is registered by Telegram ID
            var now = DateTimeOffset.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.UseOnlineVoting &&
                           e.OnlineWhenOpen != null &&
                           e.OnlineWhenClose != null &&
                           e.OnlineWhenOpen <= now &&
                           e.OnlineWhenClose >= now)
                .Select(e => e.ElectionGuid)
                .ToListAsync();

            if (!openElections.Any())
            {
                return (false, "voting.auth.telegram.noOpenElections", null);
            }

            // Look up existing OnlineVoter record with this Telegram ID
            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == telegramVoterId && ov.VoterIdType == "T");

            if (onlineVoter == null)
            {
                _logger.LogWarning("Telegram voter auth: Telegram ID {TelegramId} not registered as a voter", dto.Id);
                return (false, "voting.auth.telegram.notRegistered", null);
            }

            onlineVoter.WhenLastLogin = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(onlineVoter);
            var authResponse = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
            };

            await NotifyLoginElsewhereAsync(onlineVoter.VoterId);
            _logger.LogInformation("Voter {TelegramId} authenticated via Telegram Login Widget", dto.Id);
            return (true, null, authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating voter with Telegram");
            return (false, "voting.auth.telegram.error", null);
        }
    }

    /// <summary>
    /// Validates the HMAC-SHA256 hash from the Telegram Login Widget callback per the official spec.
    /// See https://core.telegram.org/widgets/login#checking-authorization
    /// </summary>
    private bool ValidateTelegramHash(long id, string firstName, string? lastName, string? username, string? photoUrl, long authDate, string hash)
    {
        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken) || botToken.StartsWith("<"))
        {
            _logger.LogWarning("Telegram bot token is not configured");
            return false;
        }

        // Build sorted data-check string (all non-hash fields)
        var fields = new SortedDictionary<string, string>
        {
            ["auth_date"] = authDate.ToString(),
            ["first_name"] = firstName,
            ["id"] = id.ToString()
        };
        if (!string.IsNullOrEmpty(lastName)) fields["last_name"] = lastName;
        if (!string.IsNullOrEmpty(photoUrl)) fields["photo_url"] = photoUrl;
        if (!string.IsNullOrEmpty(username)) fields["username"] = username;

        var dataCheckString = string.Join("\n", fields.Select(kv => $"{kv.Key}={kv.Value}"));

        // Secret key = SHA256(bot_token)
        using var sha256 = SHA256.Create();
        var secretKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(botToken));

        using var hmac = new HMACSHA256(secretKey);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
        var computedHashHex = Convert.ToHexString(computedHash).ToLowerInvariant();

        return computedHashHex == hash.ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a phone number from Kakao by extracting digits and adding a + prefix.
    /// </summary>
    /// <param name="phone">The phone number from Kakao to normalize.</param>
    /// <returns>The normalized phone number with + prefix, or null if invalid.</returns>
    private static string? NormalizeKakaoPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length > 0 ? $"+{digits}" : null;
    }
}
