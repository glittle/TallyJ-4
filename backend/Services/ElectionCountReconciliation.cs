using Backend.Context;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Compares Front Desk registrations, entered ballots (including spoiled), and
/// pending online rows. Returns named mismatch rows where v4 can identify them.
/// Does not link accepted online ballots back to <see cref="OnlineVotingInfo"/>
/// (acceptance wipes <c>BallotGuid</c>).
/// </summary>
public static class ElectionCountReconciliation
{
    private static readonly HashSet<string> PaperOrImportedMethods = new(StringComparer.Ordinal)
    {
        "P", "M", "D", "C", "I", "1", "2", "3"
    };

    public static async Task<CountReconciliationReportDto> EvaluateAsync(
        MainDbContext context,
        Guid electionGuid)
    {
        var people = await context.People
            .AsNoTracking()
            .Where(p => p.ElectionGuid == electionGuid)
            .Select(p => new PersonSnapshot(
                p.PersonGuid,
                p.FirstName,
                p.LastName,
                p.VotingMethod,
                p.EnvNum,
                p.HasOnlineBallot))
            .ToListAsync();

        var ballots = await context.Ballots
            .AsNoTracking()
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .Select(b => new BallotSnapshot(
                b.BallotGuid,
                b.ComputerCode,
                b.BallotNumAtComputer,
                b.StatusCode))
            .ToListAsync();

        var onlineRows = await context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid)
            .Select(o => new OnlineSnapshot(o.PersonGuid, o.Status))
            .ToListAsync();

        return BuildReport(people, ballots, onlineRows);
    }

    public static async Task EnsureReconciledAsync(
        MainDbContext context,
        Guid electionGuid)
    {
        var report = await EvaluateAsync(context, electionGuid);
        if (!report.IsReconciled)
        {
            throw new InvalidOperationException(
                ElectionStageMessageKeys.WithParam(
                    ElectionStageMessageKeys.CountsDoNotReconcile,
                    "count",
                    report.Mismatches.Count));
        }
    }

    internal static CountReconciliationReportDto BuildReport(
        IReadOnlyList<PersonSnapshot> people,
        IReadOnlyList<BallotSnapshot> ballots,
        IReadOnlyList<OnlineSnapshot> onlineRows)
    {
        var pendingByPerson = onlineRows
            .Where(o => OnlineBallotStatus.IsSubmitted(o.Status)
                        || OnlineBallotStatus.IsProcessing(o.Status))
            .GroupBy(o => o.PersonGuid)
            .ToDictionary(g => g.Key, g => g.ToList());

        var processedPersonGuids = onlineRows
            .Where(o => OnlineBallotStatus.IsProcessed(o.Status))
            .Select(o => o.PersonGuid)
            .ToHashSet();

        var peopleByGuid = people.ToDictionary(p => p.PersonGuid);

        var mismatches = new List<CountReconciliationMismatchDto>();

        foreach (var (personGuid, rows) in pendingByPerson.OrderBy(p => p.Key))
        {
            peopleByGuid.TryGetValue(personGuid, out var person);
            mismatches.Add(new CountReconciliationMismatchDto
            {
                Kind = CountReconciliationMismatchKinds.PendingOnline,
                PersonGuid = personGuid,
                PersonName = FormatPersonName(person),
                VotingMethod = person.VotingMethod,
                EnvNum = person.EnvNum,
                OnlineStatus = rows[0].Status
            });
        }

        var duplicateEnvGroups = people
            .Where(p => p.EnvNum.HasValue)
            .GroupBy(p => p.EnvNum!.Value)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateEnvGroups.OrderBy(g => g.Key))
        {
            foreach (var person in group.OrderBy(p => p.LastName).ThenBy(p => p.FirstName))
            {
                mismatches.Add(new CountReconciliationMismatchDto
                {
                    Kind = CountReconciliationMismatchKinds.DuplicateEnvelope,
                    PersonGuid = person.PersonGuid,
                    PersonName = FormatPersonName(person),
                    VotingMethod = person.VotingMethod,
                    EnvNum = person.EnvNum
                });
            }
        }

        foreach (var person in people.OrderBy(p => p.LastName).ThenBy(p => p.FirstName))
        {
            if (!IsPaperOrImportedMethod(person.VotingMethod))
            {
                continue;
            }

            var hasOnlinePath = pendingByPerson.ContainsKey(person.PersonGuid)
                                || processedPersonGuids.Contains(person.PersonGuid);
            if (!hasOnlinePath)
            {
                continue;
            }

            pendingByPerson.TryGetValue(person.PersonGuid, out var pendingRows);
            mismatches.Add(new CountReconciliationMismatchDto
            {
                Kind = CountReconciliationMismatchKinds.DuplicateVotingPath,
                PersonGuid = person.PersonGuid,
                PersonName = FormatPersonName(person),
                VotingMethod = person.VotingMethod,
                EnvNum = person.EnvNum,
                OnlineStatus = pendingRows?[0].Status
                    ?? (processedPersonGuids.Contains(person.PersonGuid)
                        ? OnlineBallotStatus.Processed
                        : null)
            });
        }

        var pendingPersonGuids = pendingByPerson.Keys.ToHashSet();
        var frontDeskAccounted = people.Count(p =>
            IsRegisteredForBallotCount(p, processedPersonGuids)
            && !pendingPersonGuids.Contains(p.PersonGuid));

        var ballotCount = ballots.Count;
        var spoiledBallotCount = ballots.Count(b => b.StatusCode != BallotStatus.Ok);

        if (frontDeskAccounted != ballotCount)
        {
            mismatches.Add(new CountReconciliationMismatchDto
            {
                Kind = CountReconciliationMismatchKinds.FrontDeskVsBallots,
                FrontDeskCount = frontDeskAccounted,
                BallotCount = ballotCount
            });
        }

        return new CountReconciliationReportDto
        {
            IsReconciled = mismatches.Count == 0,
            FrontDeskCount = frontDeskAccounted,
            BallotCount = ballotCount,
            PendingOnlineCount = pendingByPerson.Count,
            SpoiledBallotCount = spoiledBallotCount,
            Mismatches = mismatches
        };
    }

    /// <summary>
    /// Front Desk side of the ballot-count comparison: a recorded voting method,
    /// or a Processed online row. v4 submit/accept does not set <c>VotingMethod</c>,
    /// so Processed is the person-level record that an accepted online vote exists.
    /// Pending online people are excluded by the caller — they have no Ballot yet.
    /// </summary>
    private static bool IsRegisteredForBallotCount(
        PersonSnapshot person,
        HashSet<Guid> processedPersonGuids)
    {
        return !string.IsNullOrEmpty(person.VotingMethod)
               || processedPersonGuids.Contains(person.PersonGuid);
    }

    private static bool IsPaperOrImportedMethod(string? votingMethod) =>
        !string.IsNullOrEmpty(votingMethod) && PaperOrImportedMethods.Contains(votingMethod);

    private static string FormatPersonName(PersonSnapshot person)
    {
        if (person.PersonGuid == Guid.Empty)
        {
            return string.Empty;
        }

        return PersonNameHelper.ComputeFullNameFl(new Person
        {
            FirstName = person.FirstName,
            LastName = person.LastName ?? string.Empty
        }) ?? string.Empty;
    }

    internal readonly record struct PersonSnapshot(
        Guid PersonGuid,
        string? FirstName,
        string LastName,
        string? VotingMethod,
        int? EnvNum,
        bool? HasOnlineBallot);

    internal readonly record struct BallotSnapshot(
        Guid BallotGuid,
        string ComputerCode,
        int BallotNumAtComputer,
        BallotStatus StatusCode);

    internal readonly record struct OnlineSnapshot(Guid PersonGuid, string Status);
}
