using Serilog.Sinks.SystemConsole.Themes;

namespace TallyJ4.Backend.Helpers;

/// <summary>
/// Custom console theme for colorful logging output
/// </summary>
public static class CustomConsoleTheme
{
  /// <summary>
  /// A rich color theme for console output with distinct colors for different log elements
  /// </summary>
  public static AnsiConsoleTheme RichColors { get; } =
    new AnsiConsoleTheme(
      new Dictionary<ConsoleThemeStyle, string>
      {
        [ConsoleThemeStyle.Text] = "\x1b[37m", // White for regular text
        [ConsoleThemeStyle.SecondaryText] = "\x1b[90m", // Dark gray for secondary text
        [ConsoleThemeStyle.TertiaryText] = "\x1b[37m", // White for tertiary text
        [ConsoleThemeStyle.Invalid] = "\x1b[91m", // Bright red for invalid
        [ConsoleThemeStyle.Null] = "\x1b[95m", // Bright magenta for null
        [ConsoleThemeStyle.Name] = "\x1b[93m", // Bright yellow for names
        [ConsoleThemeStyle.String] = "\x1b[96m", // Bright cyan for strings
        [ConsoleThemeStyle.Number] = "\x1b[95m", // Bright magenta for numbers
        [ConsoleThemeStyle.Boolean] = "\x1b[94m", // Bright blue for booleans
        [ConsoleThemeStyle.Scalar] = "\x1b[96m", // Bright cyan for scalars
        [ConsoleThemeStyle.LevelVerbose] = "\x1b[90m", // Dark gray for verbose
        [ConsoleThemeStyle.LevelDebug] = "\x1b[36m", // Cyan for debug
        [ConsoleThemeStyle.LevelInformation] = "\x1b[92m", // Bright green for info
        [ConsoleThemeStyle.LevelWarning] = "\x1b[93m", // Bright yellow for warning
        [ConsoleThemeStyle.LevelError] = "\x1b[91m", // Bright red for error
        [ConsoleThemeStyle.LevelFatal] = "\x1b[97;41m", // White on red background for fatal
      }
    );

  /// <summary>
  /// A more subtle color theme for console output
  /// </summary>
  public static AnsiConsoleTheme Subtle { get; } =
    new AnsiConsoleTheme(
      new Dictionary<ConsoleThemeStyle, string>
      {
        [ConsoleThemeStyle.Text] = "\x1b[37m", // White for regular text
        [ConsoleThemeStyle.SecondaryText] = "\x1b[90m", // Dark gray for secondary text
        [ConsoleThemeStyle.TertiaryText] = "\x1b[37m", // White for tertiary text
        [ConsoleThemeStyle.Invalid] = "\x1b[31m", // Red for invalid
        [ConsoleThemeStyle.Null] = "\x1b[35m", // Magenta for null
        [ConsoleThemeStyle.Name] = "\x1b[33m", // Yellow for names
        [ConsoleThemeStyle.String] = "\x1b[36m", // Cyan for strings
        [ConsoleThemeStyle.Number] = "\x1b[35m", // Magenta for numbers
        [ConsoleThemeStyle.Boolean] = "\x1b[34m", // Blue for booleans
        [ConsoleThemeStyle.Scalar] = "\x1b[36m", // Cyan for scalars
        [ConsoleThemeStyle.LevelVerbose] = "\x1b[90m", // Dark gray for verbose
        [ConsoleThemeStyle.LevelDebug] = "\x1b[36m", // Cyan for debug
        [ConsoleThemeStyle.LevelInformation] = "\x1b[32m", // Green for info
        [ConsoleThemeStyle.LevelWarning] = "\x1b[33m", // Yellow for warning
        [ConsoleThemeStyle.LevelError] = "\x1b[31m", // Red for error
        [ConsoleThemeStyle.LevelFatal] = "\x1b[37;41m", // White on red background for fatal
      }
    );
}