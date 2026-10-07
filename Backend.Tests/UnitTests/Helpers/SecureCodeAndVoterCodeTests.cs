using System.Security.Cryptography;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

/// <summary>
/// Login codes and kiosk codes are drawn from <see cref="RandomNumberGenerator"/>,
/// and a stored login code is compared in constant time.
/// </summary>
public class SecureCodeAndVoterCodeTests
{
    [Fact]
    public void FromAlphabet_UsesTheSuppliedSource()
    {
        var code = SecureCode.FromAlphabet("ABC", 4, _ => 1);

        Assert.Equal("BBBB", code);
    }

    [Fact]
    public void NextInt32_IsRandomNumberGeneratorGetInt32()
    {
        var method = typeof(RandomNumberGenerator).GetMethod(
            nameof(RandomNumberGenerator.GetInt32),
            new[] { typeof(int) });

        Assert.NotNull(method);
        Assert.Equal(method, SecureCode.NextInt32.Method);
    }

    [Fact]
    public void KioskCode_DefaultSource_IsTheSameGenerator()
    {
        var code = KioskCodeHelper.GenerateCode("Nguyen");

        Assert.Equal(5, code.Length);
        Assert.Equal('N', code[0]);
        Assert.All(code[1..], letter => Assert.Contains(letter, KioskCodeHelper.DistinctLetters));
    }

    [Theory]
    [InlineData("AB23XY", "AB23XY", true)]
    [InlineData("AB23XY", "AB23XZ", false)]
    [InlineData("AB23XY", "ab23xy", false)]
    [InlineData("ABC", "ABCD", false)]
    [InlineData("", "", false)]
    [InlineData(null, "ABC", false)]
    public void VoterCodeComparer_MatchesOrdinalBytes(string? stored, string? submitted, bool expected)
    {
        Assert.Equal(expected, VoterCodeComparer.FixedTimeEquals(stored, submitted));
    }

    [Fact]
    public void MaskForLog_DropsTheEmailDomain()
    {
        Assert.Equal("a***@example.com", DestinationMask.Mask("ada@example.com"));
        Assert.Equal("a***", DestinationMask.MaskForLog("ada@example.com"));
        Assert.Equal(DestinationMask.Mask("+14168972671"), DestinationMask.MaskForLog("+14168972671"));
    }

    [Theory]
    [InlineData("+14168972671", 6, "141689")]
    [InlineData("+1 416 897 2671", 6, "141689")]
    public void PhonePrefix_IsTheLeadingE164Digits(string phone, int digits, string expected)
    {
        Assert.True(PhonePrefix.TryExtract(phone, digits, out var prefix));
        Assert.Equal(expected, prefix);
    }
}
