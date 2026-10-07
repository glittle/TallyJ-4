using Backend.Configuration;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Super-admin approve, raise-cap, and freeze actions.
/// </summary>
public class PaidSendAdminServiceTests : ServiceTestBase
{
    private readonly Guid _electionId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();

    public PaidSendAdminServiceTests()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Admin election",
            ElectionStage = ElectionStage.SettingUp,
            RowVersion = new byte[8]
        });
        Context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = _electionId,
            UserId = _ownerId,
            Role = "Owner"
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task Approve_ThenRaiseAllowance_ThenFreeze()
    {
        var service = CreateService();

        Assert.True(await service.ApproveOwnerAsync(_ownerId, "admin"));
        var overview = await service.GetOverviewAsync();
        Assert.DoesNotContain(overview.PendingOwners, owner => owner.UserId == _ownerId);

        Assert.False(await service.RaiseElectionAllowanceAsync(_electionId, 25, "admin"));
        Assert.True(await service.RaiseElectionAllowanceAsync(_electionId, 40, "admin"));
        Assert.Equal(40, (await Context.ElectionSendControls.SingleAsync()).AllowanceOverride);

        Assert.True(await service.SetElectionFrozenAsync(_electionId, true, "admin"));
        Assert.True(await service.SetOwnerFrozenAsync(_ownerId, true, "admin"));
        overview = await service.GetOverviewAsync();
        Assert.Contains(overview.FrozenElections, election => election.ElectionGuid == _electionId);
        Assert.Contains(overview.FrozenOwners, owner => owner.UserId == _ownerId);

        Assert.Contains(
            Context.SecurityAuditLogs,
            log => log.EventType == SecurityEventType.PaidSendOwnerApproved);
        Assert.Contains(
            Context.SecurityAuditLogs,
            log => log.EventType == SecurityEventType.PaidSendFrozen);
    }

    [Fact]
    public async Task ClearFlag_RemovesTheElectionFromTheFlaggedList()
    {
        Context.ElectionSendControls.Add(new ElectionSendControl
        {
            ElectionGuid = _electionId,
            Flagged = true,
            FlaggedAt = DateTimeOffset.UtcNow,
            FlaggedEntryCount = 4
        });
        await Context.SaveChangesAsync();

        var service = CreateService();
        Assert.True(await service.ClearElectionFlagAsync(_electionId, "admin"));

        var overview = await service.GetOverviewAsync();
        Assert.DoesNotContain(overview.FlaggedElections, election => election.ElectionGuid == _electionId);
        Assert.False((await Context.ElectionSendControls.SingleAsync()).Flagged);
        Assert.Contains(
            Context.SecurityAuditLogs,
            log => log.EventType == SecurityEventType.ElectionFlagCleared);
    }

    [Fact]
    public async Task PendingList_IncludesAnUnapprovedOwner()
    {
        var overview = await CreateService().GetOverviewAsync();

        Assert.Contains(overview.PendingOwners, owner => owner.UserId == _ownerId);
    }

    private PaidSendAdminService CreateService()
    {
        return new PaidSendAdminService(
            Context,
            new SecurityAuditService(Context, NullLogger<SecurityAuditService>.Instance),
            new SystemCodeSendClock(),
            Options.Create(new AntiAbuseOptions()),
            NullLogger<PaidSendAdminService>.Instance);
    }
}
