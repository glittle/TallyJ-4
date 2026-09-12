using Backend.Helpers;

namespace Backend.Tests.UnitTests;

public class KioskCodeHelperTests
{
    [Theory]
    [InlineData("Smith", 'S')]
    [InlineData("O'Brien", 'O')]
    [InlineData(" García ", 'G')]
    [InlineData("", 'A')]
    [InlineData("123", 'A')]
    public void GetLastNameInitial_ReturnsExpectedInitial(string lastName, char expected)
    {
        Assert.Equal(expected, KioskCodeHelper.GetLastNameInitial(lastName));
    }

    [Fact]
    public void GenerateCode_UsesLastInitialPlusFourDistinctLetters()
    {
        var code = KioskCodeHelper.GenerateCode("Johnson", new Random(7));

        Assert.Equal(5, code.Length);
        Assert.Equal('J', code[0]);
        Assert.All(code[1..], c => Assert.Contains(c, KioskCodeHelper.DistinctLetters));
    }

    [Fact]
    public void GenerateUniqueCode_AvoidsExistingCodes()
    {
        var existing = new[] { "SMART", "SMARS" };

        var code = KioskCodeHelper.GenerateUniqueCode("Smith", existing, new Random(3));

        Assert.DoesNotContain(code, existing, StringComparer.OrdinalIgnoreCase);
        Assert.StartsWith("S", code);
    }

    [Fact]
    public void IsLoginWindowOpen_IsTrueWithinFifteenMinutes()
    {
        var minted = DateTimeOffset.Parse("2026-09-12T12:00:00Z");
        Assert.True(KioskCodeLifetime.IsLoginWindowOpen(minted, minted.AddMinutes(14)));
        Assert.False(KioskCodeLifetime.IsLoginWindowOpen(minted, minted.AddMinutes(15)));
        Assert.False(KioskCodeLifetime.IsLoginWindowOpen(null, minted));
        Assert.True(KioskCodeLifetime.IsConsumed(string.Empty));
        Assert.False(KioskCodeLifetime.IsConsumed(null));
        Assert.False(KioskCodeLifetime.HasLiveCode(string.Empty));
    }

    [Fact]
    public void ToVoterId_IsElectionScopedAndRoundTrips()
    {
        var electionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var electionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var idA = KioskCodeLifetime.ToVoterId(electionA, "smart");
        var idB = KioskCodeLifetime.ToVoterId(electionB, "SMART");

        Assert.NotEqual(idA, idB);
        Assert.True(KioskCodeLifetime.TryParseVoterId(idA, out var parsedElection, out var code));
        Assert.Equal(electionA, parsedElection);
        Assert.Equal("SMART", code);
        Assert.True(KioskCodeLifetime.PersonMatchesVoterId(electionA, "SMART", idA));
        Assert.False(KioskCodeLifetime.PersonMatchesVoterId(electionB, "SMART", idA));
        Assert.True(KioskCodeLifetime.PersonMatchesVoterId(electionA, "SMART", "SMART"));
    }
}