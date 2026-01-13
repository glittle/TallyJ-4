namespace TallyJ4.Application.DTOs.Auth;

public class TwoFactorSetupResponse
{
    public string Secret { get; set; } = null!;
    public string QrCodeDataUrl { get; set; } = null!;
}
