namespace Backend.Services.Auth;

/// <summary>
/// Development/testing validator that accepts <c>dev-google:{email}</c> credentials
/// and optionally delegates to production validation for real GIS tokens.
/// </summary>
public class DevelopmentGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    private const string DevPrefix = "dev-google:";
    private readonly ProductionGoogleIdTokenValidator? _productionValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentGoogleIdTokenValidator"/> class.
    /// </summary>
    /// <param name="productionValidator">Optional production validator for real GIS tokens in local dev.</param>
    public DevelopmentGoogleIdTokenValidator(ProductionGoogleIdTokenValidator? productionValidator = null)
    {
        _productionValidator = productionValidator;
    }

    /// <inheritdoc/>
    public async Task<GoogleIdTokenValidationResult?> ValidateAsync(
        string credential,
        CancellationToken cancellationToken = default)
    {
        if (credential.StartsWith(DevPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var email = credential[DevPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return new GoogleIdTokenValidationResult
            {
                Email = email,
                EmailVerified = true
            };
        }

        if (_productionValidator != null)
        {
            return await _productionValidator.ValidateAsync(credential, cancellationToken);
        }

        return null;
    }
}