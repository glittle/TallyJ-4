namespace Backend.Authorization;

/// <summary>
/// Policy names registered in <c>ProgramAuthSetup.ConfigureAuthorization</c>.
/// </summary>
public static class ElectionAccessPolicies
{
    public const string ElectionAccess = "ElectionAccess";
    public const string TellerAccess = "TellerAccess";
    public const string HeadTellerAccess = "HeadTellerAccess";
    public const string FullTellerAccess = "FullTellerAccess";
}
