using Backend.DTOs.Reports;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class ReportService
{
    public async Task<VotesByNumDto> GetVotesByNumAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .ThenBy(r => r.Person!.LastName)
            .ThenBy(r => r.Person!.FirstName)
            .ToListAsync();

        var tieGroups = TieBreakResolution.Evaluate(results);
        var people = new List<VotePersonDto>();
        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            people.Add(new VotePersonDto
            {
                PersonName = r.Person?.FullNameFl ?? "",
                VoteCount = r.VoteCount ?? 0,
                TieBreakCount = r.TieBreakCount,
                TieBreakRequired = TieBreakResolution.FlagsFor(r, tieGroups).TieBreakRequired,
                Section = r.Section,
                ShowBreak = i == 0 || r.Section != results[i - 1].Section
            });
        }

        return new VotesByNumDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people,
            HasUnresolvedTie = people.Any(p => p.TieBreakRequired)
        };
    }

    public async Task<VotesByNameDto> GetVotesByNameAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Person!.LastName)
            .ThenBy(r => r.Person!.FirstName)
            .ToListAsync();

        var tieGroups = TieBreakResolution.Evaluate(results);
        var people = results.Select(r => new VotePersonDto
        {
            PersonName = r.Person?.FullName ?? "",
            VoteCount = r.VoteCount ?? 0,
            TieBreakCount = r.TieBreakCount,
            TieBreakRequired = TieBreakResolution.FlagsFor(r, tieGroups).TieBreakRequired,
            Section = r.Section
        }).ToList();

        return new VotesByNameDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people,
            HasUnresolvedTie = people.Any(p => p.TieBreakRequired)
        };
    }
}
