using Backend.Configuration;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;
using Microsoft.Extensions.Options;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Owner-facing reason when a co-owner is frozen or not approved.
/// </summary>
public class PaidChannelStatusServiceTests : ServiceTestBase
{
    private readonly Guid _electionId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _coOwnerId = Guid.NewGuid();

    public PaidChannelStatusServiceTests()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Co-owned election",
            ElectionStage = ElectionStage.SettingUp,
            RowVersion = new byte[8]
        });
        Context.JoinElectionUsers.AddRange(
            new JoinElectionUser { ElectionGuid = _electionId, UserId = _ownerId, Role = "Admin" },
            new JoinElectionUser { ElectionGuid = _electionId, UserId = _coOwnerId, Role = "Owner" });
        Context.SaveChanges();
    }

    [Fact]
    public async Task FrozenCoOwner_ShowsFrozen()
    {
        Context.OwnerSendControls.AddRange(
            new OwnerSendControl { UserId = _ownerId, PaidSendsApproved = true },
            new OwnerSendControl { UserId = _coOwnerId, PaidSendsApproved = true, SendsFrozen = true });
        await Context.SaveChangesAsync();

        var dto = await ApplyAsync();

        Assert.Equal(PaidChannelStatus.Frozen, dto.PaidChannelBlockReason);
    }

    [Fact]
    public async Task UnapprovedCoOwner_ShowsNotApproved()
    {
        Context.OwnerSendControls.Add(new OwnerSendControl { UserId = _ownerId, PaidSendsApproved = true });
        await Context.SaveChangesAsync();

        var dto = await ApplyAsync();

        Assert.Equal(PaidChannelStatus.NotApproved, dto.PaidChannelBlockReason);
    }

    [Fact]
    public async Task EveryCoOwnerApproved_ShowsNoBlock()
    {
        Context.OwnerSendControls.AddRange(
            new OwnerSendControl { UserId = _ownerId, PaidSendsApproved = true },
            new OwnerSendControl { UserId = _coOwnerId, PaidSendsApproved = true });
        await Context.SaveChangesAsync();

        var dto = await ApplyAsync();

        Assert.Null(dto.PaidChannelBlockReason);
    }

    private async Task<ElectionDto> ApplyAsync()
    {
        var service = new PaidChannelStatusService(
            Context,
            new FixedClock(),
            Options.Create(new AntiAbuseOptions()));
        var dto = new ElectionDto { Name = "Co-owned election" };
        await service.ApplyAsync(_electionId, dto);
        return dto;
    }

    private sealed class FixedClock : ICodeSendClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

        public DateOnly UtcDate => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}
