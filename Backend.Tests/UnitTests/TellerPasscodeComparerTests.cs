using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Backend.Helpers;
using FluentAssertions;

namespace Backend.Tests.UnitTests;

public class TellerPasscodeComparerTests
{
    [Fact]
    public void EqualsUtf8_MatchesEqualNonEmptyPasscodes()
    {
        TellerPasscodeComparer.EqualsUtf8("secret1", "secret1").Should().BeTrue();
    }

    [Fact]
    public void EqualsUtf8_RejectsDifferentContentOfTheSameLength()
    {
        TellerPasscodeComparer.EqualsUtf8("secret1", "secret2").Should().BeFalse();
    }

    [Fact]
    public void EqualsUtf8_RejectsDifferentLengthsWithoutThrowing()
    {
        var differentLength = () => TellerPasscodeComparer.EqualsUtf8("secret1", "no");
        differentLength.Should().NotThrow();
        differentLength().Should().BeFalse();
    }

    [Fact]
    public void EqualsUtf8_RejectsAnEmptyStoredPasscode()
    {
        TellerPasscodeComparer.EqualsUtf8("", "").Should().BeFalse();
        TellerPasscodeComparer.EqualsUtf8(null, "secret1").Should().BeFalse();
    }

    [Fact]
    public void EqualsUtf8_AgreesWithFixedTimeEqualsOnUtf8Bytes()
    {
        const string passcode = "secret-code";
        var bytes = Encoding.UTF8.GetBytes(passcode);
        CryptographicOperations.FixedTimeEquals(bytes, bytes).Should().BeTrue();
        TellerPasscodeComparer.EqualsUtf8(passcode, passcode).Should().BeTrue();
    }

    [Fact]
    public void EqualsUtf8_CallsCryptographicOperationsFixedTimeEquals()
    {
        var method = typeof(TellerPasscodeComparer).GetMethod(
            nameof(TellerPasscodeComparer.EqualsUtf8),
            BindingFlags.Public | BindingFlags.Static);
        method.Should().NotBeNull();

        var called = CalledMethods(method!).Select(item => $"{item.DeclaringType?.FullName}.{item.Name}");
        called.Should().Contain("System.Security.Cryptography.CryptographicOperations.FixedTimeEquals");
    }

    private static IEnumerable<MethodBase> CalledMethods(MethodInfo caller)
    {
        var body = caller.GetMethodBody();
        body.Should().NotBeNull();
        var il = body!.GetILAsByteArray();
        il.Should().NotBeNull();

        var module = caller.Module;
        for (var i = 0; i < il!.Length - 4; i++)
        {
            if (il[i] != 0x28 && il[i] != 0x6F)
            {
                continue;
            }

            var token = BitConverter.ToInt32(il, i + 1);
            var table = token >> 24;
            if (table != 0x06 && table != 0x0A)
            {
                continue;
            }

            MethodBase? resolved;
            try
            {
                resolved = module.ResolveMethod(token);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (resolved != null)
            {
                yield return resolved;
            }
        }
    }
}
