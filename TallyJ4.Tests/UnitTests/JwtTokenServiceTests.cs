using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using TallyJ4.Application.Services.Auth;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Tests.UnitTests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceTests()
    {
        var config = new Dictionary<string, string>
        {
            ["Jwt:Key"] = "test-secret-key-minimum-32-characters-for-jwt-signing",
            ["Jwt:Issuer"] = "TallyJ4API",
            ["Jwt:Audience"] = "TallyJ4Client",
            ["Jwt:ExpiryMinutes"] = "15",
            ["Jwt:RefreshTokenExpiryDays"] = "30"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        _service = new JwtTokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            AuthMethod = "local"
        };

        // Act
        var token = _service.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify token can be parsed
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("TallyJ4API", jwtToken.Issuer);
        Assert.Equal("TallyJ4Client", jwtToken.Audiences.First());
        Assert.Equal(user.Id, jwtToken.Subject);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("local", jwtToken.Claims.First(c => c.Type == "authMethod").Value);
    }

    [Fact]
    public void GenerateToken_TokenExpiresInConfiguredMinutes()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            AuthMethod = "local"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _service.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiry = beforeGeneration.AddMinutes(15);
        var actualExpiry = jwtToken.ValidTo;

        // Allow for small timing differences (within 1 second)
        var timeDifference = Math.Abs((expectedExpiry - actualExpiry).TotalSeconds);
        Assert.True(timeDifference < 1, $"Expected expiry around {expectedExpiry}, but got {actualExpiry}");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var refreshToken = _service.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);

        // Should be valid Base64
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(32, bytes.Length); // 32 bytes = 256 bits
    }

    [Fact]
    public void CreateRefreshToken_WithValidParameters_ReturnsRefreshTokenEntity()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "test-refresh-token";

        // Act
        var refreshToken = _service.CreateRefreshToken(userId, token);

        // Assert
        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal(token, refreshToken.Token);
        Assert.False(refreshToken.IsRevoked);
        Assert.NotEqual(default, refreshToken.CreatedAt);

        // Should expire in 30 days
        var expectedExpiry = DateTime.UtcNow.AddDays(30);
        var timeDifference = Math.Abs((expectedExpiry - refreshToken.ExpiresAt).TotalSeconds);
        Assert.True(timeDifference < 1, $"Expected expiry around {expectedExpiry}, but got {refreshToken.ExpiresAt}");
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidExpiredToken_ReturnsPrincipal()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            AuthMethod = "local"
        };

        // Create a token that expires immediately
        var configWithShortExpiry = new Dictionary<string, string>
        {
            ["Jwt:Key"] = "test-secret-key-minimum-32-characters-for-jwt-signing",
            ["Jwt:Issuer"] = "TallyJ4API",
            ["Jwt:Audience"] = "TallyJ4Client",
            ["Jwt:ExpiryMinutes"] = "-1" // Already expired
        };

        var expiredConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(configWithShortExpiry)
            .Build();

        var expiredService = new JwtTokenService(expiredConfig);
        var expiredToken = expiredService.GenerateToken(user);

        // Act
        var principal = _service.GetPrincipalFromExpiredToken(expiredToken);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(user.Id, principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value);
        Assert.Equal(user.Email, principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
    }

    [Fact]
    public void GenerateToken_WithMissingConfiguration_ThrowsException()
    {
        // Arrange
        var incompleteConfig = new Dictionary<string, string>
        {
            // Missing required JWT settings
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(incompleteConfig)
            .Build();

        var service = new JwtTokenService(configuration);
        var user = new AppUser { Id = "test", Email = "test@example.com" };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.GenerateToken(user));
        Assert.Contains("JWT key not configured", exception.Message);
    }

    [Fact]
    public void GenerateToken_IncludesUniqueJtiClaim()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            AuthMethod = "local"
        };

        // Act
        var token1 = _service.GenerateToken(user);
        var token2 = _service.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken1 = handler.ReadJwtToken(token1);
        var jwtToken2 = handler.ReadJwtToken(token2);

        var jti1 = jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2); // JTI should be unique for each token
    }

    [Fact]
    public void HashRefreshToken_WithSameInput_ReturnsSameHash()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var hash1 = _service.HashRefreshToken(token);
        var hash2 = _service.HashRefreshToken(token);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 produces 64 character hex string
        Assert.Matches("^[a-f0-9]{64}$", hash1); // Should be lowercase hex
    }

    [Fact]
    public void HashRefreshToken_WithDifferentInputs_ReturnsDifferentHashes()
    {
        // Arrange
        var token1 = "test-refresh-token-1";
        var token2 = "test-refresh-token-2";

        // Act
        var hash1 = _service.HashRefreshToken(token1);
        var hash2 = _service.HashRefreshToken(token2);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.Equal(64, hash1.Length);
        Assert.Equal(64, hash2.Length);
    }

    [Fact]
    public void CreateRefreshToken_IncludesTokenHash()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "test-refresh-token";

        // Act
        var refreshToken = _service.CreateRefreshToken(userId, token);

        // Assert
        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal(token, refreshToken.Token);
        Assert.NotNull(refreshToken.TokenHash);
        Assert.Equal(64, refreshToken.TokenHash.Length);

        // Verify hash matches expected value
        var expectedHash = _service.HashRefreshToken(token);
        Assert.Equal(expectedHash, refreshToken.TokenHash);
    }
}