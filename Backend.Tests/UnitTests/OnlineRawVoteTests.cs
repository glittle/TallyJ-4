using Backend.Models;
using Xunit;

namespace Backend.Tests.UnitTests;

public class OnlineRawVoteTests
{
    [Fact]
    public void Constructor_SplitsFirstLastOnSpace()
    {
        var raw = new OnlineRawVote("Jane Marie Doe");

        Assert.Equal("Jane Marie", raw.First);
        Assert.Equal("Doe", raw.Last);
        Assert.Equal("Jane Marie Doe", raw.OtherInfo);
        Assert.Equal("Jane Marie Doe", raw.ToDisplayName());
    }

    [Fact]
    public void Constructor_SplitsLastFirstOnComma()
    {
        var raw = new OnlineRawVote("Doe, Jane");

        Assert.Equal("Jane", raw.First);
        Assert.Equal("Doe", raw.Last);
        Assert.Equal("Doe, Jane", raw.OtherInfo);
    }

    [Fact]
    public void Parse_ReadsV3PascalCaseJson()
    {
        var raw = OnlineRawVote.Parse("""{"First":"Ada","Last":"Lovelace","OtherInfo":"Ada Lovelace"}""");

        Assert.Equal("Ada", raw.First);
        Assert.Equal("Lovelace", raw.Last);
        Assert.Equal("Ada Lovelace", raw.OtherInfo);
    }

    [Fact]
    public void Parse_ReadsCamelCaseJson()
    {
        var raw = OnlineRawVote.Parse("""{"first":"Ada","last":"Lovelace","otherInfo":"note"}""");

        Assert.Equal("Ada", raw.First);
        Assert.Equal("Lovelace", raw.Last);
        Assert.Equal("note", raw.OtherInfo);
    }

    [Fact]
    public void Parse_FallsBackToPlainText()
    {
        var raw = OnlineRawVote.Parse("Ada Lovelace");

        Assert.Equal("Ada", raw.First);
        Assert.Equal("Lovelace", raw.Last);
        Assert.Equal("Ada Lovelace", raw.OtherInfo);
    }

    [Fact]
    public void ToJson_RoundTripsThroughParse()
    {
        var original = new OnlineRawVote("Ada Lovelace");
        var parsed = OnlineRawVote.Parse(original.ToJson());

        Assert.Equal(original.First, parsed.First);
        Assert.Equal(original.Last, parsed.Last);
        Assert.Equal(original.OtherInfo, parsed.OtherInfo);
    }
}
