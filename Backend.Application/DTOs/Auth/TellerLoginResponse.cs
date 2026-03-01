namespace Backend.Application.DTOs.Auth;

public class TellerLoginResponse
{
    public Guid ElectionGuid { get; set; }
    public string ElectionName { get; set; } = null!;
}
