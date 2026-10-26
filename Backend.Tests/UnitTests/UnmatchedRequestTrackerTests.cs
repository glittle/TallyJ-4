using System.Globalization;
using Backend.Middleware;

namespace Backend.Tests.UnitTests;

public class UnmatchedRequestTrackerTests
{
    [Fact]
    public void Format_SortsByCountDescendingThenPath()
    {
        var text = UnmatchedRequestSummary.Format(
        [
            new UnmatchedRequestCount("/account/billing", 2),
            new UnmatchedRequestCount("/index", 5)
        ]);

        Assert.Equal(
            "Unmatched request summary (last 5 min): /index (5 calls), /account/billing (2 calls)",
            text);
    }

    [Fact]
    public void Format_UsesInvariantDigitsAndSingularCall()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var text = UnmatchedRequestSummary.Format(
            [
                new UnmatchedRequestCount("/index", 1),
                new UnmatchedRequestCount("/account/api-keys", 1000)
            ]);

            Assert.Equal(
                "Unmatched request summary (last 5 min): /account/api-keys (1000 calls), /index (1 call)",
                text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Format_Empty_ReturnsNull()
    {
        Assert.Null(UnmatchedRequestSummary.Format([]));
    }

    [Fact]
    public void Normalize_LowercasesStripsQueryAndCollapsesSlashes()
    {
        Assert.Equal("/account/billing", UnmatchedRequestPath.Normalize("/Account/Billing?x=1"));
        Assert.Equal("/account/billing", UnmatchedRequestPath.Normalize("/account/billing?y=2#section"));
        Assert.Equal("/account/billing", UnmatchedRequestPath.Normalize("account/billing/"));
        Assert.Equal("/api/account/billing", UnmatchedRequestPath.Normalize("/api//account/billing"));
        Assert.Equal("/", UnmatchedRequestPath.Normalize("  "));
        Assert.Equal("/index", UnmatchedRequestPath.Normalize("/ind\nex"));
    }

    [Fact]
    public void Normalize_UsesInvariantCaseUnderTurkishCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal("/index", UnmatchedRequestPath.Normalize("/INDEX"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Record_MergesNormalizedPaths()
    {
        var tracker = new UnmatchedRequestTracker();
        tracker.Record("/Account/Billing?a=1");
        tracker.Record("/account/billing?b=2");
        tracker.Record("/INDEX");

        var rows = tracker.Drain();

        Assert.Equal(
        [
            new UnmatchedRequestCount("/account/billing", 2),
            new UnmatchedRequestCount("/index", 1)
        ], rows);
        Assert.Empty(tracker.Drain());
    }

    [Fact]
    public void Record_CapsDistinctPathsAndFoldsTheRestIntoOther()
    {
        var tracker = new UnmatchedRequestTracker();
        for (var i = 0; i < 250; i++)
        {
            tracker.Record($"/probe/{i}");
        }

        var rows = tracker.Drain();
        var other = Assert.Single(rows, row => row.Path == UnmatchedRequestTracker.OtherBucket);

        Assert.Equal(50, other.Count);
        Assert.Equal(200, rows.Count(row => row.Path != UnmatchedRequestTracker.OtherBucket));
        Assert.Equal(250, rows.Sum(row => row.Count));
        Assert.Equal(UnmatchedRequestTracker.OtherBucket, rows[0].Path);
    }

    [Fact]
    public void Record_FoldsOverlongPathsIntoOther()
    {
        var tracker = new UnmatchedRequestTracker();
        tracker.Record("/" + new string('a', UnmatchedRequestTracker.MaxPathLength));
        tracker.Record("/short");

        var rows = tracker.Drain();

        Assert.Equal(
        [
            new UnmatchedRequestCount("/short", 1),
            new UnmatchedRequestCount(UnmatchedRequestTracker.OtherBucket, 1)
        ], rows);
    }

    [Fact]
    public void Record_IsThreadSafe()
    {
        var tracker = new UnmatchedRequestTracker();
        const int threads = 8;
        const int perThread = 250;

        Parallel.For(0, threads, worker =>
        {
            for (var i = 0; i < perThread; i++)
            {
                tracker.Record($"/probe/{worker}-{i % 20}?q={i}");
            }
        });

        var rows = tracker.Drain();
        Assert.Equal(threads * perThread, rows.Sum(row => row.Count));
        Assert.True(rows.Count <= UnmatchedRequestTracker.MaxDistinctPaths + 1);
    }
}
