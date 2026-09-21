using System.Text.Json;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class VoterVerifyErrorTests
{
    [Fact]
    public void MissingCodeKey_WithIssueDate_IsAlreadyUsed()
    {
        Assert.Equal(
            VoterVerifyError.AlreadyUsed,
            VoterVerifyError.MissingCodeKey(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MissingCodeKey_WithoutIssueDate_IsNoCodeFound()
    {
        Assert.Equal(VoterVerifyError.NoCodeFound, VoterVerifyError.MissingCodeKey(null));
    }

    [Fact]
    public void ToBadRequestBody_SplitsInvalidCodeAttempts()
    {
        var json = JsonSerializer.Serialize(
            VoterVerifyError.ToBadRequestBody(VoterVerifyError.InvalidCodeWithAttempts(3)));

        Assert.Contains("\"error\":\"voting.auth.verify.invalidCode\"", json);
        Assert.Contains("\"attempts\":3", json);
        Assert.DoesNotContain("invalidCode:", json);
    }

    [Fact]
    public void ToBadRequestBody_LeavesStableKeysUnchanged()
    {
        var json = JsonSerializer.Serialize(
            VoterVerifyError.ToBadRequestBody(VoterVerifyError.AlreadyUsed));

        Assert.Contains("\"error\":\"voting.auth.verify.alreadyUsed\"", json);
        Assert.DoesNotContain("attempts", json);
    }
}
