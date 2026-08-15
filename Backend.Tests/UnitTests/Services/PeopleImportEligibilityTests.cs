using System.Globalization;
using Backend.Enumerations;
using Backend.Localization;
using Backend.Services;
using Moq;

namespace Backend.Tests.UnitTests.Services;

public class PeopleImportEligibilityTests
{
    private readonly Mock<IJsonLocalizationProvider> _localization = new();

    [Fact]
    public void TryResolve_EmptyValue_IsEligible()
    {
        var lookup = PeopleImportEligibility.BuildValueLookup(_localization.Object, CultureInfo.InvariantCulture);

        Assert.True(PeopleImportEligibility.TryResolve("", lookup, out var reason));
        Assert.Null(reason);
        Assert.True(PeopleImportEligibility.TryResolve(null, lookup, out reason));
        Assert.Null(reason);
    }

    [Fact]
    public void TryResolve_EnglishEligibleAndCodeAndDescription()
    {
        var lookup = PeopleImportEligibility.BuildValueLookup(_localization.Object, CultureInfo.GetCultureInfo("en"));

        Assert.True(PeopleImportEligibility.TryResolve("Eligible", lookup, out var eligible));
        Assert.Null(eligible);

        Assert.True(PeopleImportEligibility.TryResolve("V04", lookup, out var byCode));
        Assert.Equal(IneligibleReasonEnum.V04_RightsRemovedCannotBeVotedFor.ReasonGuid, byCode?.ReasonGuid);

        Assert.True(PeopleImportEligibility.TryResolve("Deceased", lookup, out var byDescription));
        Assert.Equal(IneligibleReasonEnum.X01_Deceased.ReasonGuid, byDescription?.ReasonGuid);
    }

    [Fact]
    public void TryResolve_LocalizedDescription_MatchesPersonReason()
    {
        _localization
            .Setup(p => p.GetString("eligibility.V04", It.IsAny<CultureInfo>()))
            .Returns("Droits retirés (ne peut pas être voté)");

        var lookup = PeopleImportEligibility.BuildValueLookup(_localization.Object, CultureInfo.GetCultureInfo("fr"));

        Assert.True(PeopleImportEligibility.TryResolve("Droits retirés (ne peut pas être voté)", lookup, out var reason));
        Assert.Equal(IneligibleReasonEnum.V04_RightsRemovedCannotBeVotedFor.ReasonGuid, reason?.ReasonGuid);
    }

    [Fact]
    public void TryResolve_UnrecognizedOrInternalOnly_Fails()
    {
        var lookup = PeopleImportEligibility.BuildValueLookup(_localization.Object, CultureInfo.GetCultureInfo("en"));

        Assert.False(PeopleImportEligibility.TryResolve("Ineligible", lookup, out _));
        Assert.False(PeopleImportEligibility.TryResolve("v04", lookup, out _));
        Assert.False(PeopleImportEligibility.TryResolve("U01", lookup, out _));
        Assert.False(PeopleImportEligibility.TryResolve("Unidentifiable", lookup, out _));
    }
}
