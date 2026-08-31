using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Ensures the reserved Online location exists when online voting is enabled.
/// Disable keeps the row when it still has ballots or computers; unused rows are removed.
/// Identity is LocationTypeCode, not the display name.
/// </summary>
public static class OnlineLocationHelper
{
    public static async Task<Location> EnsureExistsAsync(
        MainDbContext context,
        Guid electionGuid,
        CancellationToken cancellationToken = default)
    {
        var location = await FindOnlineLocationAsync(context, electionGuid, cancellationToken);
        if (location != null)
        {
            return location;
        }

        location = new Location
        {
            LocationGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            // Fallback for reports; UI labels this location from LocationType, not Name.
            Name = nameof(LocationType.Online),
            SortOrder = 999,
            LocationTypeCode = nameof(LocationType.Online),
            LocationTallyStatus = LocationTallyStatus.NotStarted,
            BallotsCollected = 0
        };
        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);
        return location;
    }

    public static async Task RemoveIfUnusedAsync(
        MainDbContext context,
        Guid electionGuid,
        CancellationToken cancellationToken = default)
    {
        var location = await FindOnlineLocationAsync(context, electionGuid, cancellationToken);
        if (location == null)
        {
            return;
        }

        var hasBallots = await context.Ballots.AnyAsync(
            b => b.LocationGuid == location.LocationGuid,
            cancellationToken);
        if (hasBallots)
        {
            return;
        }

        var hasComputers = await context.Computers.AnyAsync(
            c => c.LocationGuid == location.LocationGuid,
            cancellationToken);
        if (hasComputers)
        {
            return;
        }

        context.Locations.Remove(location);
        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task SyncAsync(
        MainDbContext context,
        Guid electionGuid,
        bool useOnlineVoting,
        CancellationToken cancellationToken = default)
    {
        if (useOnlineVoting)
        {
            await EnsureExistsAsync(context, electionGuid, cancellationToken);
            return;
        }

        await RemoveIfUnusedAsync(context, electionGuid, cancellationToken);
    }

    private static Task<Location?> FindOnlineLocationAsync(
        MainDbContext context,
        Guid electionGuid,
        CancellationToken cancellationToken) =>
        context.Locations.FirstOrDefaultAsync(
            l => l.ElectionGuid == electionGuid && l.LocationTypeCode == nameof(LocationType.Online),
            cancellationToken);
}
