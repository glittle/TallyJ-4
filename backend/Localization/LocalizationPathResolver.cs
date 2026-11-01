namespace Backend.Localization;

/// <summary>
/// Resolves <c>Localization:ResourcesPath</c> without depending on the process working directory.
/// </summary>
internal static class LocalizationPathResolver
{
    /// <summary>
    /// A rooted path is used as given. A relative path is tried against the content root,
    /// then the application base directory. A <c>locales</c> folder beside either of those
    /// is the published-layout fallback. When nothing exists, the content-root interpretation
    /// is returned so the missing-directory log names a stable path.
    /// </summary>
    public static string Resolve(string? configuredPath, string? contentRoot)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new List<string>();
        if (!string.IsNullOrEmpty(contentRoot))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(contentRoot, configuredPath)));
        }

        candidates.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath)));

        if (!string.IsNullOrEmpty(contentRoot))
        {
            candidates.Add(Path.Combine(contentRoot, "locales"));
        }

        candidates.Add(Path.Combine(AppContext.BaseDirectory, "locales"));

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }
}
