using System.Globalization;

namespace Backend.Helpers;

public static partial class ExtensionsSimple
{
    /// <summary>
    ///   Determine if Prod, UAT or Dev.  Defaults to Prod.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>The site type</returns>
    public static string DetermineSiteType(this string path)
    {
        if (path.Contains("Debug", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Local", StringComparison.OrdinalIgnoreCase))
        {
            return "Dev";
        }

        if (path.Contains("Preview", StringComparison.OrdinalIgnoreCase)
            || path.Contains("UAT", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Staging", StringComparison.OrdinalIgnoreCase))
        {
            return "UAT";
        }

        return "Prod";
    }

    /// <summary>
    /// Attempts to extract the browser name from a user agent string.
    /// </summary>
    /// <param name="input">The user agent string to parse.</param>
    /// <param name="defaultValue">The default value to return if browser cannot be identified.</param>
    /// <returns>The browser name if identified, otherwise the default value.</returns>
    public static string GetBrowserName(this string input, string defaultValue)
    {
        if (input.HasNoContent())
        {
            return defaultValue;
        }

        // try to get the name of the browser
        if (input.Contains("Edg/"))
        {
            return "Edge";
        }

        if (input.Contains("Edge/"))
        {
            return "Edge (old)";
        }

        // let .NET guess the rest
        return defaultValue;
    }

    /// <summary>
    /// Returns the MIME type for an image file extension, suitable for use in base64 data URIs.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns>The MIME type string for the image format, or "image/*" if unrecognized.</returns>
    public static string TypeForBase64Image(this string extension)
    {
        switch (extension.ToLower().Replace(".", ""))
        {
            case "svg":
                return "image/svg+xml";

            case "jpg":
            case "jpeg":
                return "image/jpeg";

            case "png":
                return "image/png";
        }

        return "image/*";
    }

    /// <summary>
    /// Converts the string "0" to a dash "-", otherwise returns the input unchanged.
    /// </summary>
    /// <param name="input">The string to check and potentially convert.</param>
    /// <returns>A dash if the input is "0", otherwise the original input.</returns>
    public static string? ZeroToDash(this string? input)
    {
        return input == "0" ? "-" : input;
    }

    /// <summary>
    /// Parses a comma-separated string of hexadecimal values into an array of integers.
    /// </summary>
    /// <param name="input">The comma-separated string of hexadecimal values (with or without # prefix).</param>
    /// <returns>An array of integers parsed from the hexadecimal values.</returns>
    public static int[] FromHexListAsIntArray(this string input)
    {
        return input
          .Split(',')
          .Select(s => int.Parse(s.Trim().Replace("#", ""), NumberStyles.HexNumber))
          .ToArray();
    }
}
