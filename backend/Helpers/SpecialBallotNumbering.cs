using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Sequential numbers for reserved online / imported ballot computer codes.
/// </summary>
public static class SpecialBallotNumbering
{
    /// <summary>
    /// Repairs WW/OL ballots that were stored as 0 or with the leftover WW code,
    /// then returns the next online ballot number at the location.
    /// </summary>
    public static async Task<int> RepairOnlineAndGetNextAsync(
        MainDbContext context,
        Guid locationGuid)
    {
        var ballots = await context.Ballots
            .Where(b => b.LocationGuid == locationGuid &&
                        (b.ComputerCode == ComputerCodeHelper.Online || b.ComputerCode == "WW"))
            .OrderBy(b => b.DateCreated)
            .ThenBy(b => b.RowId)
            .ToListAsync();

        return await RepairAndGetNextAsync(context, ballots, ComputerCodeHelper.Online);
    }

    /// <summary>
    /// Repairs any online ballots in the election that used WW or number 0.
    /// </summary>
    public static async Task RepairOnlineForElectionAsync(
        MainDbContext context,
        Guid electionGuid)
    {
        var ballots = await context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid &&
                        (b.ComputerCode == ComputerCodeHelper.Online || b.ComputerCode == "WW"))
            .OrderBy(b => b.LocationGuid)
            .ThenBy(b => b.DateCreated)
            .ThenBy(b => b.RowId)
            .ToListAsync();

        foreach (var group in ballots.GroupBy(b => b.LocationGuid))
        {
            await RepairAndGetNextAsync(context, group.ToList(), ComputerCodeHelper.Online);
        }
    }

    public static async Task<int> GetNextImportedNumberAsync(
        MainDbContext context,
        Guid locationGuid)
    {
        var max = await context.Ballots
            .Where(b => b.LocationGuid == locationGuid &&
                        (b.ComputerCode == ComputerCodeHelper.Imported || b.ComputerCode == "IMPORT"))
            .MaxAsync(b => (int?)b.BallotNumAtComputer) ?? 0;

        return max + 1;
    }

    private static async Task<int> RepairAndGetNextAsync(
        MainDbContext context,
        List<Ballot> ballots,
        string canonicalCode)
    {
        var numbers = ballots.Select(b => b.BallotNumAtComputer).ToList();
        var needsRepair = ballots.Exists(b =>
            !string.Equals(b.ComputerCode, canonicalCode, StringComparison.Ordinal)
            || b.BallotNumAtComputer <= 0
            || string.IsNullOrEmpty(b.BallotCode));

        if (!needsRepair && numbers.Count == numbers.Distinct().Count())
        {
            return (numbers.Count == 0 ? 0 : numbers.Max()) + 1;
        }

        var next = 1;
        foreach (var ballot in ballots)
        {
            ballot.ComputerCode = canonicalCode;
            ballot.BallotNumAtComputer = next;
            ballot.BallotCode = $"{canonicalCode}{next}";
            next++;
        }

        await context.SaveChangesAsync();
        return next;
    }
}
