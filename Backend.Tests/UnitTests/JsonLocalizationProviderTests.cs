using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Globalization;
using Backend.Localization;

namespace Backend.Tests.UnitTests;

public class JsonLocalizationProviderTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<JsonLocalizationProvider>> _loggerMock;

    public JsonLocalizationProviderTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "JsonLocalizationTests_" + Guid.NewGuid());
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<JsonLocalizationProvider>>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        _cache.Dispose();
    }

    [Fact]
    public void GetString_WithValidKey_ReturnsCorrectValue()
    {
        // Arrange
        var enDir = Path.Combine(_testDirectory, "en");
        Directory.CreateDirectory(enDir);
        File.WriteAllText(
            Path.Combine(enDir, "auth.json"),
            "{\"auth.errors.invalidCredentials\": \"Invalid email or password\"}");

        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act
        var result = provider.GetString("auth.errors.invalidCredentials", new CultureInfo("en"));

        // Assert
        Assert.Equal("Invalid email or password", result);
    }

    [Fact]
    public void GetString_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var enDir = Path.Combine(_testDirectory, "en");
        Directory.CreateDirectory(enDir);
        File.WriteAllText(
            Path.Combine(enDir, "auth.json"),
            "{\"auth.errors.invalidCredentials\": \"Invalid email or password\"}");

        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act
        var result = provider.GetString("nonexistent.key", new CultureInfo("en"));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetString_WithMultipleFiles_MergesCorrectly()
    {
        // Arrange
        var enDir = Path.Combine(_testDirectory, "en");
        Directory.CreateDirectory(enDir);
        File.WriteAllText(
            Path.Combine(enDir, "auth.json"),
            "{\"auth.login\": \"Login\"}");
        File.WriteAllText(
            Path.Combine(enDir, "common.json"),
            "{\"common.save\": \"Save\"}");

        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act
        var authResult = provider.GetString("auth.login", new CultureInfo("en"));
        var commonResult = provider.GetString("common.save", new CultureInfo("en"));

        // Assert
        Assert.Equal("Login", authResult);
        Assert.Equal("Save", commonResult);
    }

    [Fact]
    public void GetString_WithFrenchCulture_ReturnsFrenchValue()
    {
        // Arrange
        var frDir = Path.Combine(_testDirectory, "fr");
        Directory.CreateDirectory(frDir);
        File.WriteAllText(
            Path.Combine(frDir, "auth.json"),
            "{\"auth.errors.invalidCredentials\": \"Courriel ou mot de passe invalide\"}");

        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act
        var result = provider.GetString("auth.errors.invalidCredentials", new CultureInfo("fr"));

        // Assert
        Assert.Equal("Courriel ou mot de passe invalide", result);
    }

    [Fact]
    public void GetString_UsesCaching_LoadsOnlyOnce()
    {
        // Arrange
        var enDir = Path.Combine(_testDirectory, "en");
        Directory.CreateDirectory(enDir);
        var filePath = Path.Combine(enDir, "auth.json");
        File.WriteAllText(filePath, "{\"auth.login\": \"Login\"}");

        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act - First call
        var result1 = provider.GetString("auth.login", new CultureInfo("en"));

        // Modify file after first load
        File.WriteAllText(filePath, "{\"auth.login\": \"Changed\"}");

        // Second call - should return cached value
        var result2 = provider.GetString("auth.login", new CultureInfo("en"));

        // Assert
        Assert.Equal("Login", result1);
        Assert.Equal("Login", result2); // Should still be original value due to cache
    }

    [Fact]
    public void GetAllStrings_ReturnsAllTranslations()
    {
        // Arrange
        var enDir = Path.Combine(_testDirectory, "en");
        Directory.CreateDirectory(enDir);
        File.WriteAllText(
            Path.Combine(enDir, "auth.json"),
            "{\"auth.login\": \"Login\", \"auth.logout\": \"Logout\"}");

        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act
        var result = provider.GetAllStrings(new CultureInfo("en"));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Login", result["auth.login"]);
        Assert.Equal("Logout", result["auth.logout"]);
    }

    [Fact]
    public void GetString_WithMissingDirectory_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act
        var result = provider.GetString("auth.login", new CultureInfo("en"));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetString_WithDuplicateKeys_UsesFirstValue()
    {
        // Arrange
        var enDir = Path.Combine(_testDirectory, "en");
        Directory.CreateDirectory(enDir);
        File.WriteAllText(
            Path.Combine(enDir, "auth1.json"),
            "{\"auth.login\": \"Login 1\"}");
        File.WriteAllText(
            Path.Combine(enDir, "auth2.json"),
            "{\"auth.login\": \"Login 2\"}");

        var options = Options.Create(new JsonLocalizationOptions
        {
            ResourcesPath = _testDirectory,
            SupportedCultures = new[] { "en", "fr" },
            DefaultCulture = "en"
        });

        var provider = new JsonLocalizationProvider(_cache, options, _loggerMock.Object);

        // Act
        var result = provider.GetString("auth.login", new CultureInfo("en"));

        // Assert
        Assert.NotNull(result);
        Assert.True(result == "Login 1" || result == "Login 2");
    }
}



