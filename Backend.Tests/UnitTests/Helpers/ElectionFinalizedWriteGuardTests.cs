using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class ElectionFinalizedWriteGuardTests : ServiceTestBase
{
    [Fact]
    public void IsLocked_OnlyFinalized()
    {
        Assert.False(ElectionFinalizedWriteGuard.IsLocked(ElectionStage.SettingUp));
        Assert.False(ElectionFinalizedWriteGuard.IsLocked(ElectionStage.GatheringBallots));
        Assert.False(ElectionFinalizedWriteGuard.IsLocked(ElectionStage.ProcessingBallots));
        Assert.True(ElectionFinalizedWriteGuard.IsLocked(ElectionStage.Finalized));
    }

    [Fact]
    public void ThrowIfLocked_Finalized_ThrowsPhraseKey()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ElectionFinalizedWriteGuard.ThrowIfLocked(ElectionStage.Finalized));

        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, ex.Message);
    }

    [Fact]
    public async Task ThrowIfLockedAsync_MissingElection_DoesNotThrow()
    {
        await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(Context, Guid.NewGuid());
    }

    [Fact]
    public async Task ThrowIfLockedAsync_FinalizedElection_Throws()
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Locked",
            NumberToElect = 3,
            ElectionType = "LSA",
            ElectionStage = ElectionStage.Finalized,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ElectionFinalizedWriteGuard.ThrowIfLockedAsync(Context, electionGuid));

        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, ex.Message);
    }

    [Fact]
    public async Task IsLockedAsync_ProcessingBallots_IsFalse()
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Open",
            NumberToElect = 3,
            ElectionType = "LSA",
            ElectionStage = ElectionStage.ProcessingBallots,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        Assert.False(await ElectionFinalizedWriteGuard.IsLockedAsync(Context, electionGuid));
    }
}
