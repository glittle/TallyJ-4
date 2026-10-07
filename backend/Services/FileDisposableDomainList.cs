using Microsoft.Extensions.Hosting;

namespace Backend.Services;

/// <summary>
/// Loads <c>Data/disposable-email-domains.txt</c>. The file's first lines name the source.
/// </summary>
public sealed class FileDisposableDomainList : IDisposableDomainList
{
    private readonly HashSet<string> _domains;

    /// <summary>
    /// Reads the list from the content root or the build output directory.
    /// </summary>
    public FileDisposableDomainList(IHostEnvironment environment)
    {
        var path = new[]
        {
            Path.Combine(environment.ContentRootPath, "Data", "disposable-email-domains.txt"),
            Path.Combine(AppContext.BaseDirectory, "Data", "disposable-email-domains.txt")
        }.FirstOrDefault(File.Exists);

        _domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (path == null)
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var domain = line.Trim().TrimStart('.').ToLowerInvariant();
            if (domain.Length == 0 || domain.StartsWith('#'))
            {
                continue;
            }

            _domains.Add(domain);
        }
    }

    /// <inheritdoc />
    public bool Contains(string domain)
    {
        var current = domain.Trim().TrimStart('.').ToLowerInvariant();
        while (current.Length > 0)
        {
            if (_domains.Contains(current))
            {
                return true;
            }

            var dot = current.IndexOf('.');
            if (dot < 0)
            {
                return false;
            }

            current = current[(dot + 1)..];
        }

        return false;
    }
}
