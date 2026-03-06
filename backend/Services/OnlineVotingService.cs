using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.DTOs.OnlineVoting;
using Google.Apis.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Backend.Services;

/// <summary>
/// Service for managing online voting operations.
/// </summary>
public class OnlineVotingService : IOnlineVotingService
{
    private readonly MainDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnlineVotingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineVotingService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public OnlineVotingService(
        MainDbContext context,
        IConfiguration configuration,
        ILogger<OnlineVotingService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public async Task<string> RequestVerificationCodeAsync(RequestCodeDto dto)
    {
        try
        {
            // 1. Find all open elections where this voter is registered (SMS pumping prevention)
            var now = DateTime.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.OnlineWhenOpen != null && e.OnlineWhenOpen <= now &&
                           (e.OnlineWhenClose == null || e.OnlineWhenClose > now))
                .Select(e => e.ElectionGuid)
                .ToListAsync();

            if (!openElections.Any())
            {
                _logger.LogWarning("Login code request rejected: No elections currently open for online voting");
                return "voting.auth.requestCode.noOpenElections";
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
                return "voting.auth.requestCode.notRegistered";
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
                    WhenRegistered = DateTime.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            var verifyCode = GenerateVerificationCode();

            onlineVoter.VerifyCode = verifyCode;
            onlineVoter.VerifyCodeDate = DateTime.UtcNow;
            onlineVoter.VerifyAttempts = 0;
            onlineVoter.WhenLastLogin = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var sent = await SendVerificationCodeAsync(dto.VoterId, dto.DeliveryMethod, verifyCode);

            if (!sent)
            {
                return "voting.auth.requestCode.sendFailed";
            }

            _logger.LogInformation("Verification code sent to {VoterId} via {Method} (registered in {Count} open election(s))",
                dto.VoterId, dto.DeliveryMethod, openElections.Count);

            return "voting.auth.requestCode.sent";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting verification code for {VoterId}", dto.VoterId);
            return "voting.auth.requestCode.error";
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> VerifyCodeAsync(VerifyCodeDto dto)
    {
        try
        {
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
                onlineVoter.VerifyCodeDate.Value.AddMinutes(15) < DateTime.UtcNow)
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

            onlineVoter.WhenLastLogin = DateTime.UtcNow;
            onlineVoter.VerifyCode = null;
            onlineVoter.VerifyAttempts = 0;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(onlineVoter);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var response = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = expiresAt
            };

            _logger.LogInformation("Voter {VoterId} authenticated successfully", dto.VoterId);

            return (true, null, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying code for {VoterId}", dto.VoterId);
            return (false, "voting.auth.verify.error", null);
        }
    }

    /// <inheritdoc/>
    public async Task<OnlineElectionInfoDto?> GetElectionInfoAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Where(e => e.ElectionGuid == electionGuid)
            .FirstOrDefaultAsync();

        if (election == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var isOpen = (election.OnlineWhenOpen == null || election.OnlineWhenOpen <= now) &&
                     (election.OnlineWhenClose == null || election.OnlineWhenClose > now);

        return new OnlineElectionInfoDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            Convenor = election.Convenor,
            DateOfElection = election.DateOfElection,
            NumberToElect = election.NumberToElect,
            OnlineWhenOpen = election.OnlineWhenOpen,
            OnlineWhenClose = election.OnlineWhenClose,
            IsOpen = isOpen,
            Instructions = $"voting.election.instructions:{election.NumberToElect ?? 9}"
        };
    }

    /// <inheritdoc/>
    public async Task<List<OnlineCandidateDto>> GetCandidatesAsync(Guid electionGuid)
    {
        var candidates = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
            .OrderBy(p => p.FullName)
            .Select(p => new OnlineCandidateDto
            {
                PersonGuid = p.PersonGuid,
                FullName = p.FullName ?? "",
                Area = p.Area,
                OtherInfo = p.OtherInfo
            })
            .ToListAsync();

        return candidates;
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> SubmitBallotAsync(SubmitOnlineBallotDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var election = await _context.Elections
                .FirstOrDefaultAsync(e => e.ElectionGuid == dto.ElectionGuid);

            if (election == null)
            {
                return (false, "voting.submit.electionNotFound");
            }

            var now = DateTime.UtcNow;
            if ((election.OnlineWhenOpen != null && election.OnlineWhenOpen > now) ||
                (election.OnlineWhenClose != null && election.OnlineWhenClose <= now))
            {
                return (false, "voting.submit.notOpen");
            }

            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId);

            if (onlineVoter == null)
            {
                return (false, "voting.submit.voterNotFound");
            }

            var person = await _context.People
                .FirstOrDefaultAsync(p => p.ElectionGuid == dto.ElectionGuid &&
                                        (p.Email == dto.VoterId || p.Phone == dto.VoterId || p.KioskCode == dto.VoterId));

            if (person != null && person.HasOnlineBallot == true)
            {
                return (false, "voting.submit.alreadyVoted");
            }

            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.ElectionGuid == dto.ElectionGuid);

            if (location == null)
            {
                location = new Location
                {
                    ElectionGuid = dto.ElectionGuid,
                    Name = "Online",
                    ContactInfo = "Online voting",
                    SortOrder = 999
                };
                _context.Locations.Add(location);
                await _context.SaveChangesAsync();
            }

            var ballot = new Ballot
            {
                LocationGuid = location.LocationGuid,
                BallotGuid = Guid.NewGuid(),
                StatusCode = BallotStatus.Ok,
                ComputerCode = "WW",
                BallotNumAtComputer = 0,
                Teller1 = "Online",
                RowVersion = new byte[8]
            };

            _context.Ballots.Add(ballot);
            await _context.SaveChangesAsync();

            foreach (var voteDto in dto.Votes.OrderBy(v => v.PositionOnBallot))
            {
                var vote = new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PositionOnBallot = voteDto.PositionOnBallot,
                    PersonGuid = voteDto.PersonGuid,
                    StatusCode = voteDto.PersonGuid.HasValue ? "Valid" : "Invalid",
                    OnlineVoteRaw = voteDto.VoteName,
                    RowVersion = new byte[8]
                };

                if (voteDto.PersonGuid.HasValue)
                {
                    var votedPerson = await _context.People
                        .FirstOrDefaultAsync(p => p.PersonGuid == voteDto.PersonGuid.Value);

                    if (votedPerson != null)
                    {
                        vote.PersonCombinedInfo = votedPerson.CombinedInfo;
                    }
                }

                _context.Votes.Add(vote);
            }

            var votingInfo = new OnlineVotingInfo
            {
                ElectionGuid = dto.ElectionGuid,
                PersonGuid = person?.PersonGuid ?? Guid.NewGuid(),
                WhenBallotCreated = DateTime.UtcNow,
                Status = "Submitted",
                WhenStatus = DateTime.UtcNow
            };

            _context.OnlineVotingInfos.Add(votingInfo);

            if (person != null)
            {
                person.HasOnlineBallot = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Online ballot submitted for voter {VoterId} in election {ElectionGuid}",
                dto.VoterId, dto.ElectionGuid);

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error submitting online ballot for voter {VoterId}", dto.VoterId);
            return (false, "voting.submit.error");
        }
    }

    /// <inheritdoc/>
    public async Task<OnlineVoteStatusDto> GetVoteStatusAsync(Guid electionGuid, string voterId)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(p => p.ElectionGuid == electionGuid &&
                                    (p.Email == voterId || p.Phone == voterId || p.KioskCode == voterId));

        if (person == null)
        {
            return new OnlineVoteStatusDto
            {
                HasVoted = false,
                Message = "voting.status.voterNotFound"
            };
        }

        var votingInfo = await _context.OnlineVotingInfos
            .Where(ov => ov.ElectionGuid == electionGuid && ov.PersonGuid == person.PersonGuid)
            .OrderByDescending(ov => ov.WhenBallotCreated)
            .FirstOrDefaultAsync();

        return new OnlineVoteStatusDto
        {
            HasVoted = person.HasOnlineBallot == true,
            WhenSubmitted = votingInfo?.WhenBallotCreated,
            Message = person.HasOnlineBallot == true
                ? "voting.status.alreadyVoted"
                : "voting.status.notVoted"
        };
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> AuthenticateVoterWithGoogleAsync(GoogleAuthForVoterDto dto)
    {
        try
        {
            // 1. Get Google Client ID from configuration
            var googleClientId = _configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(googleClientId) || googleClientId.StartsWith("<"))
            {
                _logger.LogWarning("Google OAuth attempted but Google Client ID is not configured");
                return (false, "voting.auth.google.notConfigured", null);
            }

            // 2. Validate Google JWT token
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.Credential, settings);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Google OAuth for voter: Invalid Google ID token");
                return (false, "voting.auth.google.invalidCredential", null);
            }

            var email = payload.Email;
            if (string.IsNullOrEmpty(email))
            {
                return (false, "voting.auth.google.noEmail", null);
            }

            // Only accept verified emails
            if (!payload.EmailVerified)
            {
                _logger.LogWarning("Google OAuth for voter: Email {Email} not verified by Google", email);
                return (false, "voting.auth.google.emailNotVerified", null);
            }

            // 3. Find all open elections where this voter is registered
            var now = DateTime.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.OnlineWhenOpen != null &&
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
                    WhenRegistered = DateTime.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            onlineVoter.WhenLastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 6. Generate JWT token (same format as code verification)
            var token = GenerateJwtToken(onlineVoter);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var response = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = expiresAt
            };

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
    public async Task<List<AvailableElectionDto>> GetAvailableElectionsAsync(string voterId)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Find all open elections where this voter is registered
            // Use GroupBy to handle potential duplicate Person records per election
            var availableElections = await _context.People
                .Where(p => (p.Email == voterId || p.Phone == voterId || p.KioskCode == voterId))
                .Join(_context.Elections,
                    person => person.ElectionGuid,
                    election => election.ElectionGuid,
                    (person, election) => new { Person = person, Election = election })
                .Where(x => x.Election.OnlineWhenOpen != null &&
                           x.Election.OnlineWhenClose != null &&
                           x.Election.OnlineWhenOpen <= now &&
                           x.Election.OnlineWhenClose >= now)
                .GroupBy(x => x.Election.ElectionGuid)
                .Select(g => g.First())
                .Select(x => new AvailableElectionDto
                {
                    ElectionGuid = x.Election.ElectionGuid,
                    Name = x.Election.Name,
                    OnlineWhenOpen = x.Election.OnlineWhenOpen,
                    OnlineWhenClose = x.Election.OnlineWhenClose,
                    DateOfElection = x.Election.DateOfElection,
                    HasVoted = x.Person.HasOnlineBallot == true
                })
                .OrderBy(e => e.Name)
                .ToListAsync();

            _logger.LogInformation("Found {Count} available elections for voter {VoterId}",
                availableElections.Count, voterId);

            return availableElections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available elections for {VoterId}", voterId);
            return new List<AvailableElectionDto>();
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

            var now = DateTime.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.OnlineWhenOpen != null &&
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
                    WhenRegistered = DateTime.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            onlineVoter.WhenLastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(onlineVoter);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var authResponse = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = expiresAt
            };

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

            var now = DateTime.UtcNow;
            var openElections = await _context.Elections
                .Where(e => e.OnlineWhenOpen != null &&
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
                    WhenRegistered = DateTime.UtcNow
                };
                _context.OnlineVoters.Add(onlineVoter);
            }

            onlineVoter.WhenLastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(onlineVoter);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var authResponse = new OnlineVoterAuthResponse
            {
                Token = token,
                VoterId = onlineVoter.VoterId,
                VoterIdType = onlineVoter.VoterIdType,
                ExpiresAt = expiresAt
            };

            _logger.LogInformation("Voter {VoterId} authenticated via Kakao OAuth (in {Count} open election(s))", matchedVoterId, openElections.Count);
            return (true, null, authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating voter with Kakao");
            return (false, "voting.auth.kakao.error", null);
        }
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
        var smtpHost = _configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost.StartsWith("<"))
        {
            _logger.LogWarning("Email not configured; skipping send for {Email}", email);
            return true;
        }

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

        using var client = new SmtpClient();
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var useSsl = bool.Parse(_configuration["Email:UseSsl"] ?? "true");
        await client.ConnectAsync(smtpHost, smtpPort, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(username, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
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

    /// <summary>
    /// Generates a JWT token for an authenticated online voter.
    /// </summary>
    /// <param name="onlineVoter">The online voter to generate the token for.</param>
    /// <returns>A JWT token string valid for 24 hours.</returns>
    private string GenerateJwtToken(OnlineVoter onlineVoter)
    {
        var key = _configuration["Jwt:Key"] ?? "DefaultSecretKeyForDevelopmentPurposesOnly123456789";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("voterId", onlineVoter.VoterId),
            new Claim("voterIdType", onlineVoter.VoterIdType),
            new Claim("voterType", "online")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "Backend",
            audience: _configuration["Jwt:Audience"] ?? "BackendClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}



