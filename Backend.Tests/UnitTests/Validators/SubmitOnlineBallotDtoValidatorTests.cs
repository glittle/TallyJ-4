using Backend.DTOs.OnlineVoting;
using Backend.Validators;

namespace Backend.Tests.UnitTests.Validators;

public class SubmitOnlineBallotDtoValidatorTests
{
    private readonly SubmitOnlineBallotDtoValidator _validator = new();

    [Fact]
    public void EmptyVotes_DraftOverwrite_Passes()
    {
        var result = _validator.Validate(ValidDto(isDraft: true, emptyVotes: true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyVotes_SubmittedPayloadClear_Passes()
    {
        var result = _validator.Validate(ValidDto(isDraft: false, emptyVotes: true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NullVotes_Fails()
    {
        var dto = ValidDto(isDraft: true, emptyVotes: true);
        dto.Votes = null!;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SubmitOnlineBallotDto.Votes));
    }

    [Fact]
    public void VoteWithZeroPosition_Fails()
    {
        var dto = ValidDto(isDraft: false, emptyVotes: false);
        dto.Votes[0].PositionOnBallot = 0;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
    }

    private static SubmitOnlineBallotDto ValidDto(bool isDraft, bool emptyVotes)
    {
        return new SubmitOnlineBallotDto
        {
            ElectionGuid = Guid.NewGuid(),
            VoterId = "voter@example.com",
            IsDraft = isDraft,
            Votes = emptyVotes
                ? []
                :
                [
                    new OnlineVoteDto
                    {
                        VoteName = "Alice",
                        PositionOnBallot = 1
                    }
                ]
        };
    }
}
