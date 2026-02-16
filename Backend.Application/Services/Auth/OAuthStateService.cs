using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Backend.Application.Services.Auth;

/// <summary>
/// Service for managing OAuth state parameters to prevent CSRF attacks.
/// Stores state parameters in memory with expiration.
/// </summary>
public class OAuthStateService
{
    private readonly ConcurrentDictionary<string, OAuthStateEntry> _stateStore = new();
    private readonly TimeSpan _stateExpiration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Represents a stored OAuth state entry.
    /// </summary>
    private class OAuthStateEntry
    {
        public string State { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// Generates a cryptographically secure random state parameter and stores it.
    /// </summary>
    /// <param name="returnUrl">Optional return URL to store with the state.</param>
    /// <returns>The generated state parameter.</returns>
    public string GenerateState(string? returnUrl = null)
    {
        var stateBytes = new byte[32]; // 256 bits of entropy
        RandomNumberGenerator.Fill(stateBytes);
        var state = Convert.ToBase64String(stateBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var entry = new OAuthStateEntry
        {
            State = state,
            ExpiresAt = DateTime.UtcNow.Add(_stateExpiration),
            ReturnUrl = returnUrl
        };

        _stateStore[state] = entry;

        // Clean up expired entries periodically
        CleanupExpiredStates();

        return state;
    }

    /// <summary>
    /// Validates a state parameter and returns the associated return URL if valid.
    /// </summary>
    /// <param name="state">The state parameter to validate.</param>
    /// <returns>The return URL if state is valid, null otherwise.</returns>
    public string? ValidateState(string state)
    {
        if (_stateStore.TryRemove(state, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            return entry.ReturnUrl;
        }

        return null;
    }

    /// <summary>
    /// Removes expired state entries from the store.
    /// </summary>
    private void CleanupExpiredStates()
    {
        var expiredKeys = _stateStore
            .Where(kvp => kvp.Value.ExpiresAt <= DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _stateStore.TryRemove(key, out _);
        }
    }
}

