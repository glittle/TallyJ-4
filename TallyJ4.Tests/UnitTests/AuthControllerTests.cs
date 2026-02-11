using Xunit;

namespace TallyJ4.Tests.UnitTests;

/// <summary>
/// Unit tests for AuthController PKCE (Proof Key for Code Exchange) implementation.
/// </summary>
public class AuthControllerTests
{
    [Fact]
    public void GenerateCodeVerifier_ReturnsValidBase64UrlString()
    {
        // Act
        var codeVerifier = TallyJ4.Backend.Controllers.AuthController.GenerateCodeVerifier();

        // Assert
        Assert.NotNull(codeVerifier);
        Assert.True(codeVerifier.Length >= 43); // Minimum length for PKCE
        Assert.True(codeVerifier.Length <= 128); // Maximum length for PKCE

        // Should only contain URL-safe characters
        Assert.DoesNotContain(codeVerifier, '+');
        Assert.DoesNotContain(codeVerifier, '/');
        Assert.DoesNotContain(codeVerifier, '=');
    }

    [Fact]
    public void GenerateCodeChallenge_ReturnsValidBase64UrlString()
    {
        // Arrange
        var codeVerifier = "test_code_verifier_12345";

        // Act
        var codeChallenge = TallyJ4.Backend.Controllers.AuthController.GenerateCodeChallenge(codeVerifier);

        // Assert
        Assert.NotNull(codeChallenge);
        Assert.True(codeChallenge.Length > 0);

        // Should only contain URL-safe characters
        Assert.DoesNotContain(codeChallenge, '+');
        Assert.DoesNotContain(codeChallenge, '/');
        Assert.DoesNotContain(codeChallenge, '=');
    }

    [Fact]
    public void GenerateCodeChallenge_IsDeterministic()
    {
        // Arrange
        var codeVerifier = "test_code_verifier_12345";

        // Act
        var codeChallenge1 = TallyJ4.Backend.Controllers.AuthController.GenerateCodeChallenge(codeVerifier);
        var codeChallenge2 = TallyJ4.Backend.Controllers.AuthController.GenerateCodeChallenge(codeVerifier);

        // Assert
        Assert.Equal(codeChallenge1, codeChallenge2);
    }

    [Fact]
    public void Base64UrlEncode_EncodesCorrectly()
    {
        // Arrange
        var bytes = new byte[] { 255, 254, 253, 252 };

        // Act
        var encoded = TallyJ4.Backend.Controllers.AuthController.Base64UrlEncode(bytes);

        // Assert
        Assert.Equal("_f7_z_w", encoded); // Expected base64url encoding
    }
}