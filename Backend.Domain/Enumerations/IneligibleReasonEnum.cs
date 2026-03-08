using System.Text.Json.Serialization;

namespace Backend.Domain.Enumerations;

/// <summary>
/// Represents an eligibility reason that determines a person's voting and candidacy rights.
/// </summary>
public class IneligibleReason
{
    /// <summary>
    /// Initializes a new instance of the IneligibleReason class.
    /// </summary>
    /// <param name="reasonGuid">The unique GUID identifier for this reason.</param>
    /// <param name="code">The short code identifier (e.g., "X01", "V01").</param>
    /// <param name="description">The English human-readable description of the reason.</param>
    /// <param name="canVote">Whether a person with this reason can vote.</param>
    /// <param name="canReceiveVotes">Whether a person with this reason can receive votes (be a candidate).</param>
    /// <param name="internalOnly">Whether this reason is for internal use only (not for person forms).</param>
    public IneligibleReason(Guid reasonGuid, string code, string description, bool canVote, bool canReceiveVotes, bool internalOnly)
    {
        ReasonGuid = reasonGuid;
        Code = code;
        Description = description;
        CanVote = canVote;
        CanReceiveVotes = canReceiveVotes;
        InternalOnly = internalOnly;
    }

    /// <summary>
    /// The unique GUID identifier for this reason (preserves v3 compatibility).
    /// </summary>
    public Guid ReasonGuid { get; }

    /// <summary>
    /// The short code identifier (e.g., "X01", "V01").
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The human-readable description of the reason.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Whether a person with this reason can vote.
    /// </summary>
    public bool CanVote { get; }

    /// <summary>
    /// Whether a person with this reason can receive votes (be a candidate).
    /// </summary>
    public bool CanReceiveVotes { get; }

    /// <summary>
    /// Whether this reason is for vote use only. A person cannot have this eligibility reason, only a vote not associated to a person record.
    /// </summary>
    public bool InternalOnly { get; }

    /// <summary>
    /// Returns a string representation of the reason.
    /// </summary>
    public override string ToString() => Code;
}

/// <summary>
/// Static class containing all predefined eligibility reasons with lookup methods.
/// </summary>
public static class IneligibleReasonEnum
{
    // Group X: Cannot vote, cannot receive votes
    public static readonly IneligibleReason X01_Deceased = new(
        Guid.Parse("D227534D-D7E8-E011-A095-002269C41D11"), "X01", "Deceased", false, false, false);
    public static readonly IneligibleReason X02_MovedElsewhereRecently = new(
        Guid.Parse("CF27534D-D7E8-E011-A095-002269C41D11"), "X02", "Moved elsewhere recently", false, false, false);
    public static readonly IneligibleReason X03_NotInThisLocalUnit = new(
        Guid.Parse("2add3a15-ec2d-437c-916f-7c581e693baa"), "X03", "Not in this local unit", false, false, false);
    public static readonly IneligibleReason X04_NotARegisteredBahai = new(
        Guid.Parse("D127534D-D7E8-E011-A095-002269C41D11"), "X04", "Not a registered Bahá’í", false, false, false);
    public static readonly IneligibleReason X05_Under18YearsOld = new(
        Guid.Parse("32e44592-a7d8-408a-b169-8871800f62aa"), "X05", "Under 18 years old", false, false, false);
    public static readonly IneligibleReason X06_ResidesElsewhere = new(
        Guid.Parse("D327534D-D7E8-E011-A095-002269C41D11"), "X06", "Resides elsewhere", false, false, false);
    public static readonly IneligibleReason X07_RightsRemovedEntirely = new(
        Guid.Parse("D027534D-D7E8-E011-A095-002269C41D11"), "X07", "Rights removed (entirely)", false, false, false);
    public static readonly IneligibleReason X08_NotADelegateAndOnOtherInstitution = new(
        Guid.Parse("E027534D-D7E8-E011-A095-002269C41D11"), "X08", "Not a delegate and on other Institution", false, false, false);
    public static readonly IneligibleReason X09_OtherCannotVoteOrBeVotedFor = new(
        Guid.Parse("D527534D-D7E8-E011-A095-002269C41D11"), "X09", "Other (cannot vote or be voted for)", false, false, false);

    // Group V: Can vote only, cannot receive votes
    public static readonly IneligibleReason V01_YouthAged181920 = new(
        Guid.Parse("e6dd1cdd-5da0-4222-9f17-f02ce6313b0a"), "V01", "Youth aged 18/19/20", true, false, false);
    public static readonly IneligibleReason V02_ByElectionOnInstitutionAlready = new(
        Guid.Parse("C05EAE49-B01B-E111-A7FB-002269C41D11"), "V02", "By-election: On Institution already", true, false, false);
    public static readonly IneligibleReason V03_OnOtherInstitutionCounsellor = new(
        Guid.Parse("D427534D-D7E8-E011-A095-002269C41D11"), "V03", "On other Institution (e.g. Counsellor)", true, false, false);
    public static readonly IneligibleReason V04_RightsRemovedCannotBeVotedFor = new(
        Guid.Parse("920A1A55-C4A5-42E5-9BCE-31756B6A20B9"), "V04", "Rights removed (cannot be voted for)", true, false, false);
    public static readonly IneligibleReason V05_TieBreakElectionNotTied = new(
        Guid.Parse("EB159A43-FB09-4FA9-AC12-3F451073010B"), "V05", "Tie-break election: Not tied", true, false, false);
    public static readonly IneligibleReason V06_OtherCanVoteButNotBeVotedFor = new(
        Guid.Parse("24278180-fe1b-4604-9f86-d453b151d824"), "V06", "Other (can vote but not be voted for)", true, false, false);

    // Group R: Can receive votes only, cannot vote
    public static readonly IneligibleReason R01_NotADelegateInThisElection = new(
        Guid.Parse("4B2B0F32-4E14-43A4-9103-C5E9C81E8783"), "R01", "Not a delegate in this election", false, true, false);
    public static readonly IneligibleReason R02_RightsRemovedCannotVote = new(
        Guid.Parse("84FA30C9-F007-44E8-B097-CCA430AAA3AA"), "R02", "Rights removed (cannot vote)", false, true, false);
    public static readonly IneligibleReason R03_OtherCannotVoteButCanBeVotedFor = new(
        Guid.Parse("f4c7de9e-d487-49ae-9868-5cd208cd863a"), "R03", "Other (cannot vote but can be voted for)", false, true, false);

    // Group U: Internal only (ballot entry problems)
    public static readonly IneligibleReason U01_Unidentifiable = new(
        Guid.Parse("CE27534D-D7E8-E011-A095-002269C41D11"), "U01", "Unidentifiable", false, false, true);
    public static readonly IneligibleReason U02_Unreadable = new(
        Guid.Parse("CD27534D-D7E8-E011-A095-002269C41D11"), "U02", "Unreadable", false, false, true);

    /// <summary>
    /// All eligibility reasons.
    /// </summary>
    public static readonly IReadOnlyList<IneligibleReason> All = new List<IneligibleReason>
    {
        X01_Deceased, X02_MovedElsewhereRecently, X03_NotInThisLocalUnit, X04_NotARegisteredBahai,
        X05_Under18YearsOld, X06_ResidesElsewhere, X07_RightsRemovedEntirely, X08_NotADelegateAndOnOtherInstitution,
        X09_OtherCannotVoteOrBeVotedFor,
        V01_YouthAged181920, V02_ByElectionOnInstitutionAlready, V03_OnOtherInstitutionCounsellor,
        V04_RightsRemovedCannotBeVotedFor, V05_TieBreakElectionNotTied, V06_OtherCanVoteButNotBeVotedFor,
        R01_NotADelegateInThisElection, R02_RightsRemovedCannotVote, R03_OtherCannotVoteButCanBeVotedFor,
        U01_Unidentifiable, U02_Unreadable
    };

    /// <summary>
    /// All codes for quick lookup.
    /// </summary>
    public static readonly IReadOnlyList<string> AllCodes = All.Select(x => x.Code).ToList();

    /// <summary>
    /// Reasons that can be used for people (excludes internal-only reasons).
    /// </summary>
    public static readonly IReadOnlyList<IneligibleReason> PersonReasons = All.Where(x => !x.InternalOnly).ToList();

    /// <summary>
    /// Legacy GUID mappings for backward compatibility with v3 sub-reasons.
    /// Maps additional v3 GUIDs to U01 (Unidentifiable) or U02 (Unreadable).
    /// </summary>
    private static readonly IReadOnlyDictionary<Guid, IneligibleReason> LegacyGuidMappings = new Dictionary<Guid, IneligibleReason>
    {
        // Additional Unidentifiable GUIDs from v3
        { Guid.Parse("C927534D-D7E8-E011-A095-002269C41D11"), U01_Unidentifiable },
        { Guid.Parse("CB27534D-D7E8-E011-A095-002269C41D11"), U01_Unidentifiable },
        { Guid.Parse("CC27534D-D7E8-E011-A095-002269C41D11"), U01_Unidentifiable },
        { Guid.Parse("CA27534D-D7E8-E011-A095-002269C41D11"), U01_Unidentifiable },

        // Additional Unreadable GUIDs from v3
        { Guid.Parse("C827534D-D7E8-E011-A095-002269C41D11"), U02_Unreadable },
        { Guid.Parse("C727534D-D7E8-E011-A095-002269C41D11"), U02_Unreadable },
        { Guid.Parse("C627534D-D7E8-E011-A095-002269C41D11"), U02_Unreadable }
    };

    /// <summary>
    /// Gets an eligibility reason by its GUID.
    /// Supports both canonical GUIDs and legacy v3 sub-reason GUIDs.
    /// </summary>
    /// <param name="guid">The GUID to search for.</param>
    /// <returns>The matching IneligibleReason, or null if not found.</returns>
    public static IneligibleReason? GetByGuid(Guid? guid)
    {
        if (guid == null) return null;

        // First try direct lookup
        var reason = All.FirstOrDefault(x => x.ReasonGuid == guid);
        if (reason != null) return reason;

        // Then try legacy mappings
        return LegacyGuidMappings.TryGetValue(guid.Value, out var legacyReason) ? legacyReason : null;
    }

    /// <summary>
    /// Gets an eligibility reason by its code (case-sensitive).
    /// </summary>
    /// <param name="code">The code to search for (e.g., "X01").</param>
    /// <returns>The matching IneligibleReason, or null if not found.</returns>
    public static IneligibleReason? GetByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return All.FirstOrDefault(x => x.Code == code);
    }

    /// <summary>
    /// Gets an eligibility reason by its description (case-insensitive).
    /// Used for backward compatibility with v3 import files.
    /// </summary>
    /// <param name="description">The description to search for.</param>
    /// <returns>The matching IneligibleReason, or null if not found.</returns>
    public static IneligibleReason? GetByDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        return All.FirstOrDefault(x => x.Description.Equals(description, StringComparison.OrdinalIgnoreCase));
    }
}