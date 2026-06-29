using Backend.Helpers;
using Xunit;

namespace Backend.Tests.UnitTests.Services;

public class ComputerCodeHelperTests
{
    [Theory]
    [InlineData("A", true)]
    [InlineData("Z", true)]
    [InlineData("AA", true)]
    [InlineData("AZ", true)]
    [InlineData("BA", true)]
    [InlineData("A1", false)]
    [InlineData("ABC", false)]
    [InlineData("", false)]
    public void IsValidCode_accepts_letter_only_codes(string code, bool expected)
    {
        Assert.Equal(expected, ComputerCodeHelper.IsValidCode(code));
    }

    [Theory]
    [InlineData("A", 0)]
    [InlineData("B", 1)]
    [InlineData("Z", 25)]
    [InlineData("AA", 26)]
    [InlineData("AB", 27)]
    [InlineData("AZ", 51)]
    [InlineData("BA", 52)]
    public void CodeToIndex_and_IndexToCode_are_inverses(string code, int index)
    {
        Assert.Equal(index, ComputerCodeHelper.CodeToIndex(code));
        Assert.Equal(code, ComputerCodeHelper.IndexToCode(index));
    }

    [Fact]
    public void GetNextCodeAfterMax_returns_next_sequential_code()
    {
        Assert.Equal("C", ComputerCodeHelper.GetNextCodeAfterMax(["A", "B"]));
        Assert.Equal("E", ComputerCodeHelper.GetNextCodeAfterMax(["A", "D"]));
        Assert.Equal("AA", ComputerCodeHelper.GetNextCodeAfterMax(["Z"]));
    }
}