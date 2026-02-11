using TallyJ4.Application.Services.Auth;

namespace TallyJ4.Tests.UnitTests;

public class OAuthStateServiceTests
{
    private readonly OAuthStateService _service;

    public OAuthStateServiceTests()
    {
        _service = new OAuthStateService();
    }

    [Fact]
    public void GenerateState_NoReturnUrl_ReturnsValidState()
    {
        // Act
        var state = _service.GenerateState();

        // Assert
        Assert.NotNull(state);
        Assert.NotEmpty(state);
        Assert.True(state.Length > 20); // Base64 encoded 32 bytes should be longer than 20 chars
        Assert.DoesNotContain('+', state); // Should use URL-safe base64
        Assert.DoesNotContain('/', state);
        Assert.DoesNotContain('=', state);
    }

    [Fact]
    public void GenerateState_WithReturnUrl_ReturnsValidState()
    {
        // Arrange
        var returnUrl = "https://example.com/callback";

        // Act
        var state = _service.GenerateState(returnUrl);

        // Assert
        Assert.NotNull(state);
        Assert.NotEmpty(state);
    }

    [Fact]
    public void GenerateState_MultipleCalls_ReturnDifferentStates()
    {
        // Act
        var state1 = _service.GenerateState();
        var state2 = _service.GenerateState();

        // Assert
        Assert.NotEqual(state1, state2);
    }

    [Fact]
    public void ValidateState_ValidState_ReturnsReturnUrl()
    {
        // Arrange
        var returnUrl = "https://example.com/callback";
        var state = _service.GenerateState(returnUrl);

        // Act
        var result = _service.ValidateState(state);

        // Assert
        Assert.Equal(returnUrl, result);
    }

    [Fact]
    public void ValidateState_ValidStateWithoutReturnUrl_ReturnsNull()
    {
        // Arrange
        var state = _service.GenerateState();

        // Act
        var result = _service.ValidateState(state);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateState_InvalidState_ReturnsNull()
    {
        // Act
        var result = _service.ValidateState("invalid-state");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateState_EmptyState_ReturnsNull()
    {
        // Act
        var result = _service.ValidateState("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateState_NullState_ReturnsNull()
    {
        // Act
        var result = _service.ValidateState(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateState_UsedState_ReturnsNullOnSecondCall()
    {
        // Arrange
        var state = _service.GenerateState();

        // Act
        var result1 = _service.ValidateState(state);
        var result2 = _service.ValidateState(state);

        // Assert
        Assert.NotNull(result1); // First call should succeed
        Assert.Null(result2); // Second call should fail (state consumed)
    }

    [Fact]
    public void ValidateState_ExpiredState_ReturnsNull()
    {
        // Arrange
        var service = new OAuthStateService();
        var state = service.GenerateState();

        // Simulate expiration by directly accessing private field (for testing only)
        // In a real scenario, we'd wait for expiration, but that's not practical for unit tests
        var field = typeof(OAuthStateService).GetField("_stateStore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var store = field?.GetValue(service) as System.Collections.Concurrent.ConcurrentDictionary<string, object>;

        if (store != null)
        {
            // Find and expire the state
            foreach (var key in store.Keys)
            {
                if (store.TryGetValue(key, out var entry))
                {
                    var entryType = entry.GetType();
                    var expiresAtField = entryType.GetProperty("ExpiresAt");
                    if (expiresAtField != null)
                    {
                        expiresAtField.SetValue(entry, DateTime.UtcNow.AddMinutes(-1));
                    }
                }
            }
        }

        // Act
        var result = service.ValidateState(state);

        // Assert
        Assert.Null(result);
    }
}