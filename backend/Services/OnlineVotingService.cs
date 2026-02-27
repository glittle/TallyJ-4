using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.DTOs.OnlineVoting;
using Google.Apis.Auth;

namespace Backend.Services;

/// <summary>
/// Service for managing online voting operations.
/// </summary>
public class OnlineVotingService : IOnlineVotingService
{
    private readonly MainDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnlineVotingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineVotingService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public OnlineVotingService(
        MainDbContext context,
        IConfiguration configuration,
        ILogger<OnlineVotingService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> RequestVerificationCodeAsync(RequestCodeDto dto)
    {
        try
        {
            // 1. Verify the election exists and is currently open for online voting
            var election = await _context.Elections
                .FirstOrDefaultAsync(e => e.ElectionGuid == dto.ElectionGuid);

            if (election == null)
            {
                _logger.LogWarning("Login code request rejected: Election {ElectionGuid} not found", dto.ElectionGuid);
                return (false, "Election not found.");
            }

            var now = DateTime.UtcNow;
            var isElectionOpen = election.OnlineWhenOpen != null &&
                                 election.OnlineWhenClose != null &&
                                 election.OnlineWhenOpen <= now &&
                                 election.OnlineWhenClose >= now;

            if (!isElectionOpen)
            {
                _logger.LogWarning("Login code request rejected: Election {ElectionGuid} is not currently open for online voting", dto.ElectionGuid);
                return (false, "Online voting is not currently open for this election.");
            }

            // 2. Verify the voter is listed in this election (SMS pumping prevention)
            Person? person = dto.VoterIdType switch
            {
                "E" => await _context.People
                    .FirstOrDefaultAsync(p => p.ElectionGuid == dto.ElectionGuid && p.Email == dto.VoterId),
                "P" => await _context.People
                    .FirstOrDefaultAsync(p => p.ElectionGuid == dto.ElectionGuid && p.Phone == dto.VoterId),
                "C" => await _context.People
                    .FirstOrDefaultAsync(p => p.ElectionGuid == dto.ElectionGuid && p.KioskCode == dto.VoterId),
                _ => null
            };

            if (person == null)
            {
                _logger.LogWarning("Login code request rejected: VoterId {VoterId} (type: {VoterIdType}) not found in election {ElectionGuid}",
                    dto.VoterId, dto.VoterIdType, dto.ElectionGuid);
                return (false, "You are not registered to vote in this election.");
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
                return (false, "Failed to send verification code. Please try again.");
            }

            _logger.LogInformation("Verification code sent to {VoterId} via {Method} for election {ElectionGuid}",
                dto.VoterId, dto.DeliveryMethod, dto.ElectionGuid);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting verification code for {VoterId} in election {ElectionGuid}",
                dto.VoterId, dto.ElectionGuid);
            return (false, "An error occurred while processing your request.");
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
                return (false, "Voter not found. Please request a verification code first.", null);
            }

            if (string.IsNullOrEmpty(onlineVoter.VerifyCode))
            {
                return (false, "No verification code found. Please request a new code.", null);
            }

            if (onlineVoter.VerifyCodeDate == null ||
                onlineVoter.VerifyCodeDate.Value.AddMinutes(15) < DateTime.UtcNow)
            {
                return (false, "Verification code has expired. Please request a new code.", null);
            }

            if (onlineVoter.VerifyAttempts >= 5)
            {
                return (false, "Too many failed attempts. Please request a new code.", null);
            }

            if (onlineVoter.VerifyCode != dto.VerifyCode)
            {
                onlineVoter.VerifyAttempts = (onlineVoter.VerifyAttempts ?? 0) + 1;
                await _context.SaveChangesAsync();

                return (false, $"Invalid verification code. {5 - onlineVoter.VerifyAttempts} attempts remaining.", null);
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
            return (false, "An error occurred while verifying your code.", null);
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
        var isOpen = election.OnlineWhenOpen != null &&
                     election.OnlineWhenClose != null &&
                     election.OnlineWhenOpen <= now &&
                     election.OnlineWhenClose >= now;

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
            Instructions = $"Please vote for {election.NumberToElect ?? 9} candidates."
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
                return (false, "Election not found.");
            }

            var now = DateTime.UtcNow;
            if (election.OnlineWhenOpen == null || election.OnlineWhenClose == null ||
                election.OnlineWhenOpen > now || election.OnlineWhenClose < now)
            {
                return (false, "Online voting is not currently open for this election.");
            }

            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId);

            if (onlineVoter == null)
            {
                return (false, "Voter not found. Please authenticate first.");
            }

            var person = await _context.People
                .FirstOrDefaultAsync(p => p.ElectionGuid == dto.ElectionGuid &&
                                        (p.Email == dto.VoterId || p.Phone == dto.VoterId || p.KioskCode == dto.VoterId));

            if (person != null && person.HasOnlineBallot == true)
            {
                return (false, "You have already submitted a ballot for this election.");
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
                StatusCode = "Ok",
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
            return (false, "An error occurred while submitting your ballot. Please try again.");
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
                Message = "Voter not found in this election."
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
                ? "You have already submitted your ballot for this election."
                : "You have not yet submitted a ballot for this election."
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
                return (false, "Google authentication is not configured on this server.", null);
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
                return (false, "Invalid Google credential.", null);
            }

            var email = payload.Email;
            if (string.IsNullOrEmpty(email))
            {
                return (false, "Email not provided by Google.", null);
            }

            // Only accept verified emails
            if (!payload.EmailVerified)
            {
                _logger.LogWarning("Google OAuth for voter: Email {Email} not verified by Google", email);
                return (false, "Google email must be verified to vote.", null);
            }

            // 3. Check election exists and is currently open
            var election = await _context.Elections
                .FirstOrDefaultAsync(e => e.ElectionGuid == dto.ElectionGuid);

            if (election == null)
            {
                _logger.LogWarning("Google OAuth rejected: Election {ElectionGuid} not found", dto.ElectionGuid);
                return (false, "Election not found.", null);
            }

            var now = DateTime.UtcNow;
            var isElectionOpen = election.OnlineWhenOpen != null &&
                                 election.OnlineWhenClose != null &&
                                 election.OnlineWhenOpen <= now &&
                                 election.OnlineWhenClose >= now;

            if (!isElectionOpen)
            {
                _logger.LogWarning("Google OAuth rejected: Election {ElectionGuid} is not currently open for online voting", dto.ElectionGuid);
                return (false, "Online voting is not currently open for this election.", null);
            }

            // 4. Find person by Google email in the election
            var person = await _context.People
                .FirstOrDefaultAsync(p => p.ElectionGuid == dto.ElectionGuid && p.Email == email);

            if (person == null)
            {
                _logger.LogWarning("Google OAuth rejected: Email {Email} not found in election {ElectionGuid}", email, dto.ElectionGuid);
                return (false, "You are not registered to vote in this election.", null);
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

            _logger.LogInformation("Voter {Email} authenticated successfully via Google OAuth for election {ElectionGuid}",
                email, dto.ElectionGuid);

            return (true, null, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating voter with Google for election {ElectionGuid}", dto.ElectionGuid);
            return (false, "An error occurred while processing your Google authentication.", null);
        }
    }

    private string GenerateVerificationCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async Task<bool> SendVerificationCodeAsync(string recipient, string method, string code)
    {
        _logger.LogInformation("Sending verification code {Code} to {Recipient} via {Method}",
            code, recipient, method);

        await Task.Delay(100);

        return true;
    }

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



