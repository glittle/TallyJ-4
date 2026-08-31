namespace Backend.Helpers;

/// <summary>
/// Command-line flags for one-shot startup (OpenAPI write without SQL or a listening host).
/// </summary>
public static class StartupCommandLine
{
    public const string ExitAfterStartArgument = "--exit-after-start";
    public const string SkipDatabaseArgument = "--skip-database";

    public static bool ExitAfterStart(IEnumerable<string> arguments) =>
        arguments.Contains(ExitAfterStartArgument);

    /// <summary>
    /// True when --skip-database is present, or when --exit-after-start is present
    /// so OpenAPI regen does not need SQL.
    /// </summary>
    public static bool SkipDatabase(IEnumerable<string> arguments) =>
        arguments.Contains(SkipDatabaseArgument) || ExitAfterStart(arguments);
}
