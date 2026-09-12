using Backend.DTOs.Reports;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class ReportService
{
    public async Task<AllCanReceiveReportDto> GetAllCanReceiveAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return new AllCanReceiveReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people.Select(p => p.FullName ?? "").ToList()
        };
    }

    public async Task<VotersReportDto> GetVotersAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;
        var locationMap = locations.ToDictionary(l => l.LocationGuid, l => l.Name);

        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return new VotersReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            HasMultipleLocations = hasMultipleLocations,
            TotalCount = people.Count,
            People = people.Select(p => new VoterItemDto
            {
                PersonName = p.FullName ?? "",
                VotingMethod = GetVotingMethodText(p.VotingMethod),
                BahaiId = p.BahaiId,
                Location = p.VotingLocationGuid.HasValue && locationMap.TryGetValue(p.VotingLocationGuid.Value, out var name) ? name : null,
                RegistrationTime = p.RegistrationTime,
                Teller1 = p.Teller1,
                Teller2 = p.Teller2,
                RegistrationLog = p.RegistrationHistory
            }).ToList()
        };
    }

    public async Task<FlagsReportDto> GetFlagsReportAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;
        var locationMap = locations.ToDictionary(l => l.LocationGuid, l => l.Name);

        var flagNames = string.IsNullOrEmpty(election.Flags)
            ? new List<string>()
            : election.Flags.Split('|').Where(f => !string.IsNullOrEmpty(f)).ToList();

        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return new FlagsReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            HasMultipleLocations = hasMultipleLocations,
            FlagNames = flagNames,
            People = people.Select(p => new FlagPersonDto
            {
                RowId = p.RowId,
                PersonName = p.FullName ?? "",
                Location = p.VotingLocationGuid.HasValue && locationMap.TryGetValue(p.VotingLocationGuid.Value, out var name) ? name : null,
                Flags = string.IsNullOrEmpty(p.Flags) ? new List<string>() : p.Flags.Split('|').Where(f => !string.IsNullOrEmpty(f)).ToList()
            }).ToList()
        };
    }

    public async Task<VotersOnlineReportDto> GetVotersOnlineAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);

        var onlineInfos = await _context.OnlineVotingInfos
            .Where(ovi => ovi.ElectionGuid == electionGuid)
            .Join(_context.People.Where(p => p.ElectionGuid == electionGuid),
                ovi => ovi.PersonGuid, p => p.PersonGuid, (ovi, p) => new { ovi, p })
            .OrderByDescending(j => j.ovi.WhenStatus)
            .ThenBy(j => j.p.LastName)
            .ThenBy(j => j.p.FirstName)
            .ToListAsync();

        return new VotersOnlineReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = onlineInfos.Select(j => new OnlineVoterItemDto
            {
                PersonId = j.p.RowId,
                FullName = j.p.FullName ?? "",
                VotingMethodDisplay = GetVotingMethodText(j.p.VotingMethod),
                Status = j.ovi.Status,
                WhenStatus = j.ovi.WhenStatus,
                Email = j.p.Email,
                Phone = j.p.Phone
            }).ToList()
        };
    }

    public async Task<VotersByAreaReportDto> GetVotersByAreaAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .ToListAsync();
        var processedOnline = await LoadProcessedOnlinePersonGuidsAsync(electionGuid);

        var areas = people
            .GroupBy(p => p.Area ?? "(unknown)")
            .OrderBy(g => g.Key)
            .Select(g => BuildAreaRow(g.Key, g.ToList(), processedOnline))
            .ToList();

        return new VotersByAreaReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            Custom1Name = ParseCustomMethodName(election.CustomMethods, 0),
            Custom2Name = ParseCustomMethodName(election.CustomMethods, 1),
            Custom3Name = ParseCustomMethodName(election.CustomMethods, 2),
            Areas = areas,
            Total = BuildAreaRow("Total", people, processedOnline)
        };
    }

    private static AreaRowDto BuildAreaRow(
        string name,
        List<Person> people,
        HashSet<Guid> processedOnline)
    {
        var breakdown = VotingMethodCodes.Count(people.Select(p =>
            (p.VotingMethod, processedOnline.Contains(p.PersonGuid))));
        return new AreaRowDto
        {
            AreaName = name,
            TotalEligible = people.Count,
            Voted = people.Count(p =>
                VotingMethodCodes.HasVotedForCounts(
                    p.VotingMethod,
                    processedOnline.Contains(p.PersonGuid))),
            InPerson = breakdown.InPerson,
            MailedIn = breakdown.Mailed,
            DroppedOff = breakdown.DroppedOff,
            CalledIn = breakdown.CalledIn,
            Custom1 = breakdown.Custom1,
            Custom2 = breakdown.Custom2,
            Custom3 = breakdown.Custom3,
            Online = breakdown.Online,
            OnlineKiosk = breakdown.Kiosk,
            Imported = breakdown.Imported
        };
    }

    public async Task<VotersByLocationReportDto> GetVotersByLocationAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .ToListAsync();
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var processedOnline = await LoadProcessedOnlinePersonGuidsAsync(electionGuid);

        var locationRows = locations
            .GroupJoin(people, l => l.LocationGuid, p => p.VotingLocationGuid ?? Guid.Empty,
                (l, pList) => BuildLocationRow(FormatLocationName(l), pList.ToList(), processedOnline))
            .OrderBy(r => r.LocationName)
            .ToList();

        var totalRow = BuildLocationRow("Total", people, processedOnline);

        return new VotersByLocationReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            Custom1Name = ParseCustomMethodName(election.CustomMethods, 0),
            Custom2Name = ParseCustomMethodName(election.CustomMethods, 1),
            Custom3Name = ParseCustomMethodName(election.CustomMethods, 2),
            Locations = locationRows,
            Total = totalRow
        };
    }

    private static LocationRowDto BuildLocationRow(
        string name,
        List<Person> people,
        HashSet<Guid> processedOnline)
    {
        var breakdown = VotingMethodCodes.Count(people.Select(p =>
            (p.VotingMethod, processedOnline.Contains(p.PersonGuid))));
        return new LocationRowDto
        {
            LocationName = name,
            TotalVoters = people.Count,
            InPerson = breakdown.InPerson,
            MailedIn = breakdown.Mailed,
            DroppedOff = breakdown.DroppedOff,
            CalledIn = breakdown.CalledIn,
            Custom1 = breakdown.Custom1,
            Custom2 = breakdown.Custom2,
            Custom3 = breakdown.Custom3,
            Online = breakdown.Online,
            OnlineKiosk = breakdown.Kiosk,
            Imported = breakdown.Imported
        };
    }

    private async Task<HashSet<Guid>> LoadProcessedOnlinePersonGuidsAsync(Guid electionGuid)
    {
        var guids = await _context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid && o.Status == OnlineBallotStatus.Processed)
            .Select(o => o.PersonGuid)
            .ToListAsync();
        return guids.ToHashSet();
    }

    public async Task<VotersByLocationAreaReportDto> GetVotersByLocationAreaAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true && p.VotingMethod == "P")
            .ToListAsync();
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();

        var locationGroups = people
            .Where(p => p.VotingLocationGuid.HasValue)
            .Join(locations, p => p.VotingLocationGuid, l => l.LocationGuid, (p, l) => new { l, p })
            .GroupBy(x => FormatLocationName(x.l))
            .OrderBy(g => g.Key)
            .Select(g => new LocationAreaGroupDto
            {
                LocationName = g.Key,
                TotalCount = g.Count(),
                Areas = g.GroupBy(x => x.p.Area ?? "(unknown)")
                    .OrderBy(a => a.Key)
                    .Select(a => new AreaCountDto { AreaName = a.Key, Count = a.Count() })
                    .ToList()
            })
            .ToList();

        return new VotersByLocationAreaReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            Locations = locationGroups
        };
    }

    public async Task<ChangedPeopleReportDto> GetChangedPeopleAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CombinedInfo != p.CombinedInfoAtStart)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return new ChangedPeopleReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people.Select(p => new ChangedPersonDto
            {
                Change = string.IsNullOrEmpty(p.CombinedInfoAtStart) ? "New" : "Changed",
                FirstName = p.FirstName,
                LastName = p.LastName,
                OtherNames = p.OtherNames,
                OtherLastNames = p.OtherLastNames,
                OtherInfo = p.OtherInfo,
                BahaiId = p.BahaiId,
                CanVote = p.CanVote == true,
                CanReceiveVotes = p.CanReceiveVotes == true,
                InvalidReasonDesc = GetIneligibleDescription(p.IneligibleReasonCode)
            }).ToList()
        };
    }

    public async Task<AllNonEligibleReportDto> GetAllNonEligibleAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && (p.CanVote != true || p.CanReceiveVotes != true))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return new AllNonEligibleReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people.Select(p => new NonEligiblePersonDto
            {
                PersonName = p.FullName ?? "",
                CanReceiveVotes = p.CanReceiveVotes == true,
                CanVote = p.CanVote == true,
                InvalidReasonDesc = GetIneligibleDescription(p.IneligibleReasonCode),
                VotingMethod = GetVotingMethodText(p.VotingMethod)
            }).ToList()
        };
    }

    public async Task<VoterEmailsReportDto> GetVoterEmailsAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && (p.Email != null || p.Phone != null))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        // Collect the distinct email and phone values for this election's people
        var personEmails = people
            .Where(p => !string.IsNullOrEmpty(p.Email))
            .Select(p => p.Email!)
            .Distinct()
            .ToList();

        var personPhones = people
            .Where(p => !string.IsNullOrEmpty(p.Phone))
            .Select(p => p.Phone!)
            .Distinct()
            .ToList();

        // Query only relevant online voters by type and matching voter IDs, projecting just VoterId
        var emailVoterIds = await _context.OnlineVoters
            .Where(ov => ov.VoterIdType == "E" && personEmails.Contains(ov.VoterId))
            .Select(ov => ov.VoterId)
            .ToHashSetAsync();

        var phoneVoterIds = await _context.OnlineVoters
            .Where(ov => ov.VoterIdType == "P" && personPhones.Contains(ov.VoterId))
            .Select(ov => ov.VoterId)
            .ToHashSetAsync();
        return new VoterEmailsReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people.Select(p => new VoterEmailItemDto
            {
                FullName = p.FullName ?? "",
                BahaiId = p.BahaiId,
                Email = p.Email,
                Phone = p.Phone,
                CanVote = p.CanVote == true,
                HasSignedInEmail = !string.IsNullOrEmpty(p.Email) && emailVoterIds.Contains(p.Email),
                HasSignedInPhone = !string.IsNullOrEmpty(p.Phone) && phoneVoterIds.Contains(p.Phone),
                VotingMethod = GetVotingMethodText(p.VotingMethod)
            }).ToList()
        };
    }
}
