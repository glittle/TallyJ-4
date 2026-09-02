using Backend;
using Backend.Entities;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class AcceptAllOnlineBallotsAuditTests
{
    [Fact]
    public void FormatDetails_IncludesPendingAndAcceptedCounts()
    {
        var details = AcceptAllOnlineBallotsAudit.FormatDetails(3, 1, 0, 4);
        Assert.StartsWith(AcceptAllOnlineBallotsAudit.DetailsPrefix, details);
        Assert.Contains("pending 3->0", details);
        Assert.Contains("accepted 1->4", details);
    }

    [Fact]
    public void FormatMetadata_StoresCountsAndDisplayName_NotEmail()
    {
        var metadata = AcceptAllOnlineBallotsAudit.FormatMetadata(3, 1, 0, 4, "Jane Teller");
        Assert.Equal(AcceptAllOnlineBallotsAudit.MetadataKind, metadata["kind"]);
        Assert.Equal("3", metadata["pendingBefore"]);
        Assert.Equal("1", metadata["acceptedBefore"]);
        Assert.Equal("0", metadata["pendingAfter"]);
        Assert.Equal("4", metadata["acceptedAfter"]);
        Assert.Equal("Jane Teller", metadata["acceptedBy"]);
        Assert.DoesNotContain(metadata.Values, v => v.Contains('@'));
    }

    [Fact]
    public void ToRunDto_ReadsWhoWhenAndCounts()
    {
        var when = DateTimeOffset.Parse("2026-09-02T12:00:00Z");
        var log = new SecurityAuditLog
        {
            Timestamp = when,
            EventType = SecurityEventType.OperationalActivity,
            UserId = "teller-1",
            Details = AcceptAllOnlineBallotsAudit.FormatDetails(3, 1, 0, 4),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(
                AcceptAllOnlineBallotsAudit.FormatMetadata(3, 1, 0, 4, "Jane Teller"))
        };

        Assert.True(AcceptAllOnlineBallotsAudit.IsAcceptAllLog(log));
        var run = AcceptAllOnlineBallotsAudit.ToRunDto(log);
        Assert.Equal(when, run.When);
        Assert.Equal("teller-1", run.AcceptedByUserId);
        Assert.Equal("Jane Teller", run.AcceptedBy);
        Assert.Equal(3, run.PendingBefore);
        Assert.Equal(1, run.AcceptedBefore);
        Assert.Equal(0, run.PendingAfter);
        Assert.Equal(4, run.AcceptedAfter);
    }

    [Fact]
    public void IsAcceptAllLog_IgnoresOtherOperationalRows()
    {
        var log = new SecurityAuditLog
        {
            EventType = SecurityEventType.OperationalActivity,
            Details = "Ballot entry began"
        };
        Assert.False(AcceptAllOnlineBallotsAudit.IsAcceptAllLog(log));
    }
}
