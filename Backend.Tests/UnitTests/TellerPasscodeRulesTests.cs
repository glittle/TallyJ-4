using Backend.Configuration;
using Backend.Helpers;
using FluentAssertions;

namespace Backend.Tests.UnitTests;

public class TellerPasscodeRulesTests
{
    [Fact]
    public void IsAcceptableValue_AllowsEmptyOnCreate()
    {
        TellerPasscodeRules.IsAcceptableValue(null, stored: null, minimumLength: 6, isCreate: true)
            .Should().BeTrue();
        TellerPasscodeRules.IsAcceptableValue("", stored: null, minimumLength: 6, isCreate: true)
            .Should().BeTrue();
    }

    [Fact]
    public void IsAcceptableValue_RejectsShortValueOnCreate()
    {
        TellerPasscodeRules.IsAcceptableValue("short", stored: null, minimumLength: 6, isCreate: true)
            .Should().BeFalse();
    }

    [Fact]
    public void IsAcceptableValue_AllowsMinimumLengthOnCreate()
    {
        TellerPasscodeRules.IsAcceptableValue("secret", stored: null, minimumLength: 6, isCreate: true)
            .Should().BeTrue();
    }

    [Fact]
    public void IsAcceptableValue_AllowsUnchangedLegacyPasscodeOnUpdate()
    {
        TellerPasscodeRules.IsAcceptableValue("abc", stored: "abc", minimumLength: 6, isCreate: false)
            .Should().BeTrue();
    }

    [Fact]
    public void IsAcceptableValue_RejectsChangingToADifferentShortPasscode()
    {
        TellerPasscodeRules.IsAcceptableValue("no", stored: "abc", minimumLength: 6, isCreate: false)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(0, TellerLoginProtectionOptions.DefaultMaxConsecutiveFailures)]
    [InlineData(-1, TellerLoginProtectionOptions.DefaultMaxConsecutiveFailures)]
    [InlineData(10, 10)]
    public void ResolvedMaxConsecutiveFailures_UsesDefaultWhenUnset(int configured, int expected)
    {
        var options = new TellerLoginProtectionOptions { MaxConsecutiveFailures = configured };
        options.ResolvedMaxConsecutiveFailures.Should().Be(expected);
    }
}
