using Google.Apis.Auth;

namespace Backend.Services.Auth;

/// <summary>
/// Validates Google ID tokens using the Google APIs library.
/// </summary>
public class ProductionGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductionGoogleIdTokenValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductionGoogleIdTokenValidator"/> class.
    /// </summary>
    public ProductionGoogleIdTokenValidator(
        IConfiguration configuration,
        ILogger<ProductionGoogleIdTokenValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GoogleIdTokenValidationResult?> ValidateAsync(
        string credential,
        CancellationToken cancellationToken = default)
    {
        var googleClientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(googleClientId) || googleClientId.StartsWith('<'))
        {
            _logger.LogWarning("Google ID token validation skipped: Client ID is not configured");
            return null;
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
            if (string.IsNullOrEmpty(payload.Email))
            {
                return null;
            }

            return new GoogleIdTokenValidationResult
            {
                Email = payload.Email,
                EmailVerified = payload.EmailVerified
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token");
            return null;
        }
    }
}