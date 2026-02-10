using Moq;
using System.Globalization;
using TallyJ4.Localization;

namespace TallyJ4.Tests.UnitTests;

public class JsonStringLocalizerFactoryTests
{
    [Fact]
    public void Create_WithResourceSource_ReturnsLocalizer()
    {
        // Arrange
        var providerMock = new Mock<IJsonLocalizationProvider>();
        providerMock
            .Setup(p => p.GetString(It.IsAny<string>(), It.IsAny<CultureInfo>()))
            .Returns("Test");

        var factory = new JsonStringLocalizerFactory(providerMock.Object);

        // Act
        var localizer = factory.Create(typeof(JsonStringLocalizerFactoryTests));

        // Assert
        Assert.NotNull(localizer);
        var result = localizer["test.key"];
        Assert.NotNull(result);
    }

    [Fact]
    public void Create_WithBaseNameAndLocation_ReturnsLocalizer()
    {
        // Arrange
        var providerMock = new Mock<IJsonLocalizationProvider>();
        providerMock
            .Setup(p => p.GetString(It.IsAny<string>(), It.IsAny<CultureInfo>()))
            .Returns("Test");

        var factory = new JsonStringLocalizerFactory(providerMock.Object);

        // Act
        var localizer = factory.Create("Resources", "Location");

        // Assert
        Assert.NotNull(localizer);
        var result = localizer["test.key"];
        Assert.NotNull(result);
    }
}
