using Backend.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Backend.Services.Auth;

/// <summary>
/// Identity data-protection tokens used only for local password reset.
/// Email confirmation stays on the default provider (one day). This one expires in one hour.
/// </summary>
public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public PasswordResetTokenProviderOptions()
    {
        Name = PasswordResetTokenProvider.ProviderName;
        TokenLifespan = PasswordResetTokenProvider.Lifespan;
    }
}

public class PasswordResetTokenProvider : DataProtectorTokenProvider<AppUser>
{
    public const string ProviderName = "PasswordReset";

    /// <summary>
    /// Shared with the reset email wording. Do not describe a different lifetime in the message.
    /// </summary>
    public static readonly TimeSpan Lifespan = TimeSpan.FromHours(1);

    public PasswordResetTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<PasswordResetTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<AppUser>> logger)
        : base(
            dataProtectionProvider,
            Microsoft.Extensions.Options.Options.Create(
                (DataProtectionTokenProviderOptions)options.Value),
            logger)
    {
    }
}
