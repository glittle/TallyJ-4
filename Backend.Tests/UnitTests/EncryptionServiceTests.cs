using Microsoft.Extensions.Configuration;
using Backend.Application.Services.Auth;
using System.Security.Cryptography;

namespace Backend.Tests.UnitTests;

public class EncryptionServiceTests
{
    private readonly IConfiguration _configuration;

    public EncryptionServiceTests()
    {
        var config = new Dictionary<string, string>
        {
            ["Encryption:Key"] = "ThisIsATestEncryptionKeyThatIsLongEnoughForAES256"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
    }

    [Fact]
    public void Constructor_ValidKey_CreatesService()
    {
        // Act
        var service = new EncryptionService(_configuration);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_MissingKey_ThrowsException()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new EncryptionService(config));
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_Succeeds()
    {
        // Arrange
        var service = new EncryptionService(_configuration);
        var originalText = "This is a test secret for 2FA";

        // Act
        var encrypted = service.Encrypt(originalText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        Assert.NotEqual(originalText, encrypted); // Encrypted text should be different
        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void Encrypt_SameInput_DifferentOutputs()
    {
        // Arrange
        var service = new EncryptionService(_configuration);
        var input = "Same input text";

        // Act
        var encrypted1 = service.Encrypt(input);
        var encrypted2 = service.Encrypt(input);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2); // Should be different due to random nonce
    }

    [Fact]
    public void Encrypt_EmptyString_ThrowsException()
    {
        // Arrange
        var service = new EncryptionService(_configuration);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Encrypt(""));
    }

    [Fact]
    public void Encrypt_NullString_ThrowsException()
    {
        // Arrange
        var service = new EncryptionService(_configuration);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Encrypt(null!));
    }

    [Fact]
    public void Decrypt_EmptyString_ThrowsException()
    {
        // Arrange
        var service = new EncryptionService(_configuration);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Decrypt(""));
    }

    [Fact]
    public void Decrypt_InvalidData_ThrowsException()
    {
        // Arrange
        var service = new EncryptionService(_configuration);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Decrypt("invalid-base64-data"));
    }

    [Fact]
    public void Decrypt_TamperedData_ThrowsException()
    {
        // Arrange
        var service = new EncryptionService(_configuration);
        var originalText = "Test data";
        var encrypted = service.Encrypt(originalText);

        // Tamper with the encrypted data
        var tampered = encrypted.Replace(encrypted[10], 'X');

        // Act & Assert
        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(tampered));
    }
}


