using Backend.DTOs.OnlineVoting;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineBallotNameCountTests
{
    private static readonly Guid Ann = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Bo = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Cy = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static Dictionary<Guid, string?> Eligible()
    {
        return new Dictionary<Guid, string?>
        {
            [Ann] = "Doe, Ann",
            [Bo] = "Lee, Bo"
        };
    }

    [Fact]
    public void ListMode_CountsDistinctEligiblePeople_AndIgnoresTypedNames()
    {
        var votes = new List<OnlineVoteDto>
        {
            new() { PersonGuid = Ann, VoteName = "Doe, Ann", PositionOnBallot = 1 },
            new() { PersonGuid = Ann, VoteName = "Doe, Ann", PositionOnBallot = 2 },
            new() { VoteName = "Someone Else", PositionOnBallot = 3 },
            new() { PersonGuid = Cy, VoteName = "Not Eligible", PositionOnBallot = 4 }
        };

        Assert.False(OnlineBallotNameCount.AllowsWriteIn("A"));
        Assert.False(OnlineBallotNameCount.AllowsWriteIn(null));
        Assert.Equal(1, OnlineBallotNameCount.CountDistinctValidNames(votes, "A", Eligible()));
    }

    [Fact]
    public void ListMode_SameDisplayNameOnTwoPeople_CountsOnce()
    {
        var votes = new List<OnlineVoteDto>
        {
            new() { PersonGuid = Ann, VoteName = "Doe, Ann", PositionOnBallot = 1 },
            new() { PersonGuid = Bo, VoteName = "Doe, Ann", PositionOnBallot = 2 }
        };

        Assert.Equal(1, OnlineBallotNameCount.CountDistinctValidNames(votes, "A", Eligible()));
    }

    [Fact]
    public void WriteInMode_CountsDistinctTypedNames_AndCollapsesCaseAndSpacing()
    {
        var votes = new List<OnlineVoteDto>
        {
            new() { VoteName = "Ann Doe", PositionOnBallot = 1 },
            new() { VoteName = "  ann   doe ", PositionOnBallot = 2 },
            new() { VoteName = "Bo Lee", PositionOnBallot = 3 },
            new() { VoteName = "   ", PositionOnBallot = 4 }
        };

        Assert.True(OnlineBallotNameCount.AllowsWriteIn("B"));
        Assert.True(OnlineBallotNameCount.AllowsWriteIn("C"));
        Assert.Equal(2, OnlineBallotNameCount.CountDistinctValidNames(votes, "B", Eligible()));
    }

    [Fact]
    public void WriteInMode_DoesNotCountATypedNameTwiceWhenThatPersonWasSelected()
    {
        var votes = new List<OnlineVoteDto>
        {
            new() { PersonGuid = Ann, VoteName = "Doe, Ann", PositionOnBallot = 1 },
            new() { VoteName = "Doe, Ann", PositionOnBallot = 2 }
        };

        Assert.Equal(1, OnlineBallotNameCount.CountDistinctValidNames(votes, "C", Eligible()));
    }
}
