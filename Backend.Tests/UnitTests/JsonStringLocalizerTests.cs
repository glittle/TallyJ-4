using Microsoft.Extensions.Localization;
using Moq;
using System.Globalization;
using Backend.Localization;

namespace Backend.Tests.UnitTests;

public class JsonStringLocalizerTests
{
    [Fact]
    public void Indexer_WithValidKey_ReturnsLocalizedString()
    {
        // Arrange
        var providerMock = new Mock<IJsonLocalizationProvider>();
        providerMock
            .Setup(p => p.GetString("auth.login", It.IsAny<CultureInfo>()))
            .Returns("Login");

        var localizer = new JsonStringLocalizer(providerMock.Object, CultureInfo.CurrentUICulture);

        // Act
        var result = localizer["auth.login"];

        // Assert
        Assert.Equal("Login", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WithMissingKey_ReturnsKeyAsValue()
    {
        // Arrange
        var providerMock = new Mock<IJsonLocalizationProvider>();
        providerMock
            .Setup(p => p.GetString("missing.key", It.IsAny<CultureInfo>()))
            .Returns((string?)null);

        var localizer = new JsonStringLocalizer(providerMock.Object, CultureInfo.CurrentUICulture);

        // Act
        var result = localizer["missing.key"];

        // Assert
        Assert.Equal("missing.key", result.Value);
        Assert.True(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WithFormatting_ReturnsFormattedString()
    {
        // Arrange
        var providerMock = new Mock<IJsonLocalizationProvider>();
        providerMock
            .Setup(p => p.GetString("greeting", It.IsAny<CultureInfo>()))
            .Returns("Hello, {0}!");

        var localizer = new JsonStringLocalizer(providerMock.Object, CultureInfo.CurrentUICulture);

        // Act
        var result = localizer["greeting", "World"];

        // Assert
        Assert.Equal("Hello, World!", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WithMissingKeyAndFormatting_ReturnsKeyAsValue()
    {
        // Arrange
        var providerMock = new Mock<IJsonLocalizationProvider>();
        providerMock
            .Setup(p => p.GetString("missing.key", It.IsAny<CultureInfo>()))
            .Returns((string?)null);

        var localizer = new JsonStringLocalizer(providerMock.Object, CultureInfo.CurrentUICulture);

        // Act
        var result = localizer["missing.key", "arg1", "arg2"];

        // Assert
        Assert.Equal("missing.key", result.Value);
        Assert.True(result.ResourceNotFound);
    }

    [Fact]
    public void GetAllStrings_ReturnsAllLocalizedStrings()
    {
        // Arrange
        var providerMock = new Mock<IJsonLocalizationProvider>();
        var resources = new Dictionary<string, string>
        {
            { "auth.login", "Login" },
            { "auth.logout", "Logout" }
        };
        providerMock
            .Setup(p => p.GetAllStrings(It.IsAny<CultureInfo>()))
            .Returns(resources);

        var localizer = new JsonStringLocalizer(providerMock.Object, CultureInfo.CurrentUICulture);

        // Act
        var result = localizer.GetAllStrings(includeParentCultures: false).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "auth.login" && r.Value == "Login");
        Assert.Contains(result, r => r.Name == "auth.logout" && r.Value == "Logout");
    }
}



