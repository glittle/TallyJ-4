using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.Reports;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Backend.Services;

public partial class ReportService : IReportService
{
    private readonly MainDbContext _context;
    private readonly IStringLocalizer<ReportService> _localizer;

    private static readonly Dictionary<string, string> VotingMethodNames = new()
    {
        [VotingMethodCodes.InPerson] = "In Person",
        [VotingMethodCodes.Mailed] = "Mailed In",
        [VotingMethodCodes.DroppedOff] = "Dropped Off",
        [VotingMethodCodes.CalledIn] = "Called In",
        [VotingMethodCodes.Online] = "Online",
        [VotingMethodCodes.Kiosk] = "Kiosk",
        [VotingMethodCodes.Imported] = "Imported",
        [VotingMethodCodes.Custom1] = "Custom 1",
        [VotingMethodCodes.Custom2] = "Custom 2",
        [VotingMethodCodes.Custom3] = "Custom 3"
    };

    public ReportService(MainDbContext context, IStringLocalizer<ReportService> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    private string FormatLocationName(string? storedName, string? locationTypeCode) =>
        LocationDisplayHelper.FormatName(storedName, locationTypeCode, key => _localizer[key]);

    private string FormatLocationName(Location location) =>
        FormatLocationName(location.Name, location.LocationTypeCode);

    private async Task<Election> GetElectionAsync(Guid electionGuid)
    {
        return await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid)
               ?? throw new ArgumentException($"Election {electionGuid} not found");
    }

    private static string GetVotingMethodText(string? code)
    {
        if (string.IsNullOrEmpty(code)) return "-";
        return VotingMethodNames.TryGetValue(code, out var name) ? name : code;
    }

    /// <summary>
    /// True only when <see cref="Election.ElectionType"/> is
    /// <see cref="ElectionTypeEnum.Con"/> or <see cref="ElectionTypeEnum.NSA"/>.
    /// Ballot reports then sort votes by name instead of position, and the
    /// JSON property stays <c>IsSingleNameElection</c>. The tally single-name
    /// type is <see cref="ElectionTypeEnum.Oth"/> and is not this flag.
    /// </summary>
    private static bool IsConventionOrNsa(Election election) =>
        election.ElectionType == ElectionTypeEnum.Con.Code
        || election.ElectionType == ElectionTypeEnum.NSA.Code;

    private static string? GetIneligibleDescription(string? code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        var reason = IneligibleReasonEnum.GetByCode(code);
        return reason?.Description;
    }

    private string? ParseCustomMethodName(string? customMethods, int index)
    {
        if (string.IsNullOrEmpty(customMethods)) return null;
        var parts = customMethods.Split('|');
        return index < parts.Length ? parts[index] : null;
    }

    public async Task<List<ReportListItemDto>> GetAvailableReportsAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;
        // Catalog gate only: an open timestamp is stored. This is not
        // OnlineVotingWindow and does not read UseOnlineVoting or the close.
        var onlineEnabled = election.OnlineWhenOpen.HasValue;
        // Ballots (Imported Only) when a comma-separated token is exactly "IM"
        // (case-sensitive). The letter I inside a concatenated string, and
        // imported ballot rows, do not add this report. Voters by Area uses
        // ParseElectionVotingMethods plus any imported vote count.
        var hasImported = election.VotingMethods?.Split(',').Contains("IM") == true;

        const string ballotReports = "Ballot Reports";
        const string voterReports = "Voter Reports";

        var reports = new List<ReportListItemDto>
        {
            new() { Code = "Main", Name = "Main Election Report", Category =  ballotReports },
            new() { Code = "VotesByNum", Name = "Tellers' Report, by Votes", Category = ballotReports },
            new() { Code = "VotesByName", Name = "Tellers' Report, by Name", Category = ballotReports },
            new() { Code = "Ballots", Name = "Ballots (All for Review)", Category = ballotReports },
        };

        if (onlineEnabled)
            reports.Add(new() { Code = "BallotsOnline", Name = "Ballots (Online Only)", Category = ballotReports });

        if (hasImported)
            reports.Add(new() { Code = "BallotsImported", Name = "Ballots (Imported Only)", Category = ballotReports });

        reports.AddRange(new[]
        {
            new ReportListItemDto { Code = "BallotsTied", Name = "Ballots (For Tied)", Category = ballotReports },
            new() { Code = "SpoiledVotes", Name = "Spoiled Votes", Category = ballotReports },
            new() { Code = "BallotAlignment", Name = "Ballot Alignment", Category = ballotReports },
            new() { Code = "BallotsSame", Name = "Duplicate Ballots", Category = ballotReports },
            new() { Code = "BallotsSummary", Name = "Ballots Summary", Category = ballotReports },
        });

        reports.Add(new() { Code = "AllCanReceive", Name = "Can Be Voted For", Category = voterReports });
        reports.Add(new() { Code = "Voters", Name = "Participation", Category = voterReports });
        reports.Add(new() { Code = "Flags", Name = "Attendance Checklists", Category = voterReports });

        if (onlineEnabled)
            reports.Add(new() { Code = "VotersOnline", Name = "Voted Online", Category = voterReports });

        reports.Add(new() { Code = "VotersByArea", Name = "Eligible and Voted by Area", Category = voterReports });

        if (hasMultipleLocations)
        {
            reports.Add(new() { Code = "VotersByLocation", Name = "Voting Method by Venue", Category = voterReports });
            reports.Add(new() { Code = "VotersByLocationArea", Name = "Attendance by Venue", Category = voterReports });
        }

        reports.Add(new() { Code = "ChangedPeople", Name = "Updated People Records", Category = voterReports });
        reports.Add(new() { Code = "AllNonEligible", Name = "With Eligibility Status", Category = voterReports });

        if (onlineEnabled)
            reports.Add(new() { Code = "VoterEmails", Name = "Email & Phone List", Category = voterReports });

        return reports;
    }
}
