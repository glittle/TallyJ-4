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
}