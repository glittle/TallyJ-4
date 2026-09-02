using System.Globalization;
using System.Text.Json;
using Backend.DTOs.Results;
using Backend.Entities;
using SecurityEventType = Backend.SecurityEventType;

namespace Backend.Helpers;

/// <summary>
/// One successful Accept-all run is stored as a <see cref="SecurityAuditLog"/>
/// (OperationalActivity — the former C_Log / Logs table). This is the teller-run
/// record (who / when / pending and accepted counts before and after).
/// Per-voter <c>OnlineVotingInfo.HistoryStatus</c> is a different thing: it only
/// appends that voter's Processed timestamp.
/// </summary>
public static class AcceptAllOnlineBallotsAudit
{
    public const string DetailsPrefix = "Accept-all online ballots";
    public const string MetadataKind = "AcceptAllOnlineBallots";

    public static string FormatDetails(
        int pendingBefore,
        int acceptedBefore,
        int pendingAfter,
        int acceptedAfter)
    {
        return $"{DetailsPrefix}: pending {pendingBefore}->{pendingAfter}, accepted {acceptedBefore}->{acceptedAfter}";
    }

    public static Dictionary<string, string> FormatMetadata(
        int pendingBefore,
        int acceptedBefore,
        int pendingAfter,
        int acceptedAfter,
        string? acceptedByDisplayName)
    {
        var metadata = new Dictionary<string, string>
        {
            ["kind"] = MetadataKind,
            ["pendingBefore"] = pendingBefore.ToString(CultureInfo.InvariantCulture),
            ["acceptedBefore"] = acceptedBefore.ToString(CultureInfo.InvariantCulture),
            ["pendingAfter"] = pendingAfter.ToString(CultureInfo.InvariantCulture),
            ["acceptedAfter"] = acceptedAfter.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(acceptedByDisplayName))
        {
            metadata["acceptedBy"] = acceptedByDisplayName;
        }

        return metadata;
    }

    public static bool IsAcceptAllLog(SecurityAuditLog log)
    {
        return log.EventType == SecurityEventType.OperationalActivity
               && log.Details != null
               && log.Details.StartsWith(DetailsPrefix, StringComparison.Ordinal);
    }

    public static AcceptAllOnlineBallotsRunDto ToRunDto(SecurityAuditLog log)
    {
        var metadata = ParseMetadata(log.MetadataJson);
        return new AcceptAllOnlineBallotsRunDto
        {
            When = log.Timestamp,
            AcceptedByUserId = log.UserId,
            AcceptedBy = GetMetadata(metadata, "acceptedBy"),
            PendingBefore = ReadCount(metadata, "pendingBefore"),
            AcceptedBefore = ReadCount(metadata, "acceptedBefore"),
            PendingAfter = ReadCount(metadata, "pendingAfter"),
            AcceptedAfter = ReadCount(metadata, "acceptedAfter")
        };
    }

    private static Dictionary<string, string> ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private static string? GetMetadata(Dictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static int ReadCount(Dictionary<string, string> metadata, string key)
    {
        if (metadata.TryGetValue(key, out var value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return n;
        }

        return 0;
    }
}
