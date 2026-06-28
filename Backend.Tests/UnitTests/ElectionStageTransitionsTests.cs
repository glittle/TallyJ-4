using Backend.Enumerations;

namespace Backend.Tests.UnitTests;

public class ElectionStageTransitionsTests
{
    [Theory]
    [InlineData(ElectionStage.SettingUp, ElectionStage.GatheringBallots, true)]
    [InlineData(ElectionStage.GatheringBallots, ElectionStage.ProcessingBallots, true)]
    [InlineData(ElectionStage.ProcessingBallots, ElectionStage.Finalized, true)]
    [InlineData(ElectionStage.ProcessingBallots, ElectionStage.GatheringBallots, true)]
    [InlineData(ElectionStage.SettingUp, ElectionStage.ProcessingBallots, true)]
    [InlineData(ElectionStage.SettingUp, ElectionStage.Finalized, true)]
    [InlineData(ElectionStage.GatheringBallots, ElectionStage.Finalized, true)]
    [InlineData(ElectionStage.Finalized, ElectionStage.ProcessingBallots, true)]
    [InlineData(ElectionStage.Finalized, ElectionStage.SettingUp, true)]
    public void CanTransition_ReturnsExpected(
        ElectionStage current,
        ElectionStage target,
        bool expected)
    {
        var allowed = ElectionStageTransitions.CanTransition(current, target, out _);
        Assert.Equal(expected, allowed);
    }
}