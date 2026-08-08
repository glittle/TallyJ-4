using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;

namespace Backend.EF.Data;

public static partial class DbSeeder
{
    private static async Task SeedLogsAsync(MainDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding operational audit logs...");

        var electionGuid = CreateGuid("SpringfieldLSA2024");
        var logs = new[]
        {
            new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.Now.AddDays(-30),
                EventType = SecurityEventType.OperationalActivity,
                ElectionGuid = electionGuid,
                Details = "Election created",
                Severity = SecurityEventSeverity.Info
            },
            new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.Now.AddDays(-25),
                EventType = SecurityEventType.OperationalActivity,
                ElectionGuid = electionGuid,
                Details = "Voters imported from CSV",
                Severity = SecurityEventSeverity.Info
            },
            new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.Now.AddDays(-20),
                EventType = SecurityEventType.OperationalActivity,
                ElectionGuid = electionGuid,
                Details = "Online voting enabled",
                Severity = SecurityEventSeverity.Info
            },
            new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.Now.AddDays(-7),
                EventType = SecurityEventType.OperationalActivity,
                ElectionGuid = electionGuid,
                Details = "Voting period started",
                Severity = SecurityEventSeverity.Info
            },
            new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.Now.AddDays(-3),
                EventType = SecurityEventType.OperationalActivity,
                ElectionGuid = electionGuid,
                Details = "Ballot entry began",
                Severity = SecurityEventSeverity.Info
            }
        };
        context.SecurityAuditLogs.AddRange(logs);
        await Task.CompletedTask;
    }
}
