using System.Globalization;
using System.Text;

namespace Backend.Middleware;

/// <summary>
/// One normalized path and how many unmatched requests used it.
/// </summary>
public readonly record struct UnmatchedRequestCount(string Path, int Count);

/// <summary>
/// In-memory counts of unmatched probe requests. Paths are lowercase, query-free,
/// and capped so a flood of distinct URLs cannot grow without bound.
/// </summary>
public sealed class UnmatchedRequestTracker
{
    public const int MaxDistinctPaths = 200;
    public const int MaxPathLength = 300;
    public const string OtherBucket = "other";

    private readonly object _gate = new();
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);
    private int _other;

    /// <summary>
    /// Records one unmatched request. Safe to call from concurrent requests.
    /// Existing paths only update a counter; new paths past the cap fold into <see cref="OtherBucket"/>.
    /// </summary>
    public void Record(string? path)
    {
        var key = UnmatchedRequestPath.Normalize(path);
        lock (_gate)
        {
            if (key.Length > MaxPathLength)
            {
                _other++;
                return;
            }

            if (_counts.TryGetValue(key, out var count))
            {
                _counts[key] = count + 1;
                return;
            }

            if (_counts.Count >= MaxDistinctPaths)
            {
                _other++;
                return;
            }

            _counts[key] = 1;
        }
    }

    /// <summary>
    /// Returns the current counts sorted by count descending, then path, and clears them.
    /// </summary>
    public IReadOnlyList<UnmatchedRequestCount> Drain()
    {
        lock (_gate)
        {
            if (_counts.Count == 0 && _other == 0)
            {
                return [];
            }

            var rows = new List<UnmatchedRequestCount>(_counts.Count + (_other > 0 ? 1 : 0));
            foreach (var pair in _counts)
            {
                rows.Add(new UnmatchedRequestCount(pair.Key, pair.Value));
            }

            if (_other > 0)
            {
                rows.Add(new UnmatchedRequestCount(OtherBucket, _other));
            }

            _counts.Clear();
            _other = 0;
            UnmatchedRequestSummary.Sort(rows);
            return rows;
        }
    }

    /// <summary>
    /// Drains and formats the summary line, or returns null when there is nothing to report.
    /// </summary>
    public string? DrainFormatted()
    {
        var rows = Drain();
        return rows.Count == 0 ? null : UnmatchedRequestSummary.Format(rows);
    }
}

/// <summary>
/// Path normalization for unmatched-request counts.
/// </summary>
public static class UnmatchedRequestPath
{
    /// <summary>
    /// Lowercases with the invariant culture, strips the query and fragment, collapses
    /// duplicate slashes, and drops a trailing slash. Control characters are removed so a
    /// probe cannot break the summary onto a second log line.
    /// </summary>
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var value = path.Trim();
        var query = value.IndexOf('?');
        if (query >= 0)
        {
            value = value[..query];
        }

        var hash = value.IndexOf('#');
        if (hash >= 0)
        {
            value = value[..hash];
        }

        value = value.Trim();
        if (value.Length == 0)
        {
            return "/";
        }

        var buffer = new char[value.Length + 1];
        var length = 0;
        if (value[0] != '/')
        {
            buffer[length++] = '/';
        }

        var previousSlash = false;
        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                continue;
            }

            var lower = char.ToLowerInvariant(ch);
            if (lower == '/')
            {
                if (previousSlash)
                {
                    continue;
                }

                previousSlash = true;
            }
            else
            {
                previousSlash = false;
            }

            buffer[length++] = lower;
        }

        if (length > 1 && buffer[length - 1] == '/')
        {
            length--;
        }

        if (length == 0)
        {
            return "/";
        }

        return new string(buffer, 0, length);
    }
}

/// <summary>
/// Formats the single periodic summary line.
/// </summary>
public static class UnmatchedRequestSummary
{
    public const string Prefix = "Unmatched request summary (last 5 min): ";

    public static void Sort(List<UnmatchedRequestCount> rows)
    {
        rows.Sort(static (left, right) =>
        {
            var byCount = right.Count.CompareTo(left.Count);
            return byCount != 0
                ? byCount
                : string.CompareOrdinal(left.Path, right.Path);
        });
    }

    /// <summary>
    /// Builds the summary. Counts are ordered by count descending, then path.
    /// Returns null when <paramref name="counts"/> is empty.
    /// </summary>
    public static string? Format(IReadOnlyList<UnmatchedRequestCount> counts)
    {
        if (counts.Count == 0)
        {
            return null;
        }

        var ordered = counts as IReadOnlyList<UnmatchedRequestCount>;
        List<UnmatchedRequestCount>? copy = null;
        if (!IsSorted(counts))
        {
            copy = new List<UnmatchedRequestCount>(counts);
            Sort(copy);
            ordered = copy;
        }

        var builder = new StringBuilder(Prefix);
        for (var i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var row = ordered[i];
            builder.Append(row.Path);
            builder.Append(" (");
            builder.Append(row.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(row.Count == 1 ? " call)" : " calls)");
        }

        return builder.ToString();
    }

    private static bool IsSorted(IReadOnlyList<UnmatchedRequestCount> counts)
    {
        for (var i = 1; i < counts.Count; i++)
        {
            var byCount = counts[i - 1].Count.CompareTo(counts[i].Count);
            if (byCount < 0)
            {
                return false;
            }

            if (byCount == 0 && string.CompareOrdinal(counts[i - 1].Path, counts[i].Path) > 0)
            {
                return false;
            }
        }

        return true;
    }
}
