using System.Text.Json.Serialization;

namespace Backend.Tests.V3AnalysisComparison;

/// <summary>
/// Optional per-fixture metadata. <c>source</c> must stay honest:
/// <c>synthetic-pipeline</c> proves import → Analyze → diff;
/// <c>known-good-v3</c> is a real v3 export after Analyze.
/// </summary>
public sealed class FixtureManifest
{
    public string Name { get; set; } = "";
    public string Scenario { get; set; } = "";
    public string Source { get; set; } = "";
    public string? Notes { get; set; }
    public string? Analyzer { get; set; }

    [JsonIgnore]
    public bool IsKnownGoodV3 =>
        string.Equals(Source, "known-good-v3", StringComparison.OrdinalIgnoreCase);
}
