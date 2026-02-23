using Backend.Domain.Enumerations;

namespace Backend.Tests.UnitTests;

public class IneligibleReasonEnumTests
{
    [Fact]
    public void All_ContainsAll20Reasons()
    {
        // Act
        var allReasons = IneligibleReasonEnum.All;

        // Assert
        Assert.Equal(20, allReasons.Count);
    }

    [Fact]
    public void AllCodes_ContainsAll20Codes()
    {
        // Act
        var allCodes = IneligibleReasonEnum.AllCodes;

        // Assert
        Assert.Equal(20, allCodes.Count);
        Assert.Contains("X01", allCodes);
        Assert.Contains("U02", allCodes);
    }

    [Theory]
    [InlineData("D227534D-D7E8-E011-A095-002269C41D11", "X01", "Deceased", false, false, false)]
    [InlineData("CF27534D-D7E8-E011-A095-002269C41D11", "X02", "Moved elsewhere recently", false, false, false)]
    [InlineData("2add3a15-ec2d-437c-916f-7c581e693baa", "X03", "Not in this local unit", false, false, false)]
    [InlineData("D127534D-D7E8-E011-A095-002269C41D11", "X04", "Not a registered Bahá’í", false, false, false)]
    [InlineData("32e44592-a7d8-408a-b169-8871800f62aa", "X05", "Under 18 years old", false, false, false)]
    [InlineData("D327534D-D7E8-E011-A095-002269C41D11", "X06", "Resides elsewhere", false, false, false)]
    [InlineData("D027534D-D7E8-E011-A095-002269C41D11", "X07", "Rights removed (entirely)", false, false, false)]
    [InlineData("E027534D-D7E8-E011-A095-002269C41D11", "X08", "Not a delegate and on other Institution", false, false, false)]
    [InlineData("D527534D-D7E8-E011-A095-002269C41D11", "X09", "Other (cannot vote or be voted for)", false, false, false)]
    [InlineData("e6dd1cdd-5da0-4222-9f17-f02ce6313b0a", "V01", "Youth aged 18/19/20", true, false, false)]
    [InlineData("C05EAE49-B01B-E111-A7FB-002269C41D11", "V02", "By-election: On Institution already", true, false, false)]
    [InlineData("D427534D-D7E8-E011-A095-002269C41D11", "V03", "On other Institution (e.g. Counsellor)", true, false, false)]
    [InlineData("920A1A55-C4A5-42E5-9BCE-31756B6A20B9", "V04", "Rights removed (cannot be voted for)", true, false, false)]
    [InlineData("EB159A43-FB09-4FA9-AC12-3F451073010B", "V05", "Tie-break election: Not tied", true, false, false)]
    [InlineData("24278180-fe1b-4604-9f86-d453b151d824", "V06", "Other (can vote but not be voted for)", true, false, false)]
    [InlineData("4B2B0F32-4E14-43A4-9103-C5E9C81E8783", "R01", "Not a delegate in this election", false, true, false)]
    [InlineData("84FA30C9-F007-44E8-B097-CCA430AAA3AA", "R02", "Rights removed (cannot vote)", false, true, false)]
    [InlineData("f4c7de9e-d487-49ae-9868-5cd208cd863a", "R03", "Other (cannot vote but can be voted for)", false, true, false)]
    [InlineData("CE27534D-D7E8-E011-A095-002269C41D11", "U01", "Unidentifiable", false, false, true)]
    [InlineData("CD27534D-D7E8-E011-A095-002269C41D11", "U02", "Unreadable", false, false, true)]
    public void AllReasons_HaveCorrectV3GuidsAndProperties(string guidString, string expectedCode, string expectedDescription, bool expectedCanVote, bool expectedCanReceiveVotes, bool expectedInternalOnly)
    {
        // Arrange
        var guid = Guid.Parse(guidString);

        // Act
        var reason = IneligibleReasonEnum.GetByGuid(guid);

        // Assert
        Assert.NotNull(reason);
        Assert.Equal(guid, reason.ReasonGuid);
        Assert.Equal(expectedCode, reason.Code);
        Assert.Equal(expectedDescription, reason.Description);
        Assert.Equal(expectedCanVote, reason.CanVote);
        Assert.Equal(expectedCanReceiveVotes, reason.CanReceiveVotes);
        Assert.Equal(expectedInternalOnly, reason.InternalOnly);
    }

    [Fact]
    public void GetByGuid_CanonicalGuids_ReturnCorrectReasons()
    {
        // Test a few key ones
        var deceased = IneligibleReasonEnum.GetByGuid(Guid.Parse("D227534D-D7E8-E011-A095-002269C41D11"));
        Assert.NotNull(deceased);
        Assert.Equal("X01", deceased.Code);

        var youth = IneligibleReasonEnum.GetByGuid(Guid.Parse("e6dd1cdd-5da0-4222-9f17-f02ce6313b0a"));
        Assert.NotNull(youth);
        Assert.Equal("V01", youth.Code);

        var unidentifiable = IneligibleReasonEnum.GetByGuid(Guid.Parse("CE27534D-D7E8-E011-A095-002269C41D11"));
        Assert.NotNull(unidentifiable);
        Assert.Equal("U01", unidentifiable.Code);
    }

    [Theory]
    [InlineData("C927534D-D7E8-E011-A095-002269C41D11")] // Legacy Unidentifiable
    [InlineData("CB27534D-D7E8-E011-A095-002269C41D11")] // Legacy Unidentifiable
    [InlineData("CC27534D-D7E8-E011-A095-002269C41D11")] // Legacy Unidentifiable
    [InlineData("CA27534D-D7E8-E011-A095-002269C41D11")] // Legacy Unidentifiable
    [InlineData("C827534D-D7E8-E011-A095-002269C41D11")] // Legacy Unreadable
    [InlineData("C727534D-D7E8-E011-A095-002269C41D11")] // Legacy Unreadable
    [InlineData("C627534D-D7E8-E011-A095-002269C41D11")] // Legacy Unreadable
    public void GetByGuid_LegacySubGuids_ResolveToCorrectReasons(string legacyGuidString)
    {
        // Arrange
        var legacyGuid = Guid.Parse(legacyGuidString);

        // Act
        var reason = IneligibleReasonEnum.GetByGuid(legacyGuid);

        // Assert
        Assert.NotNull(reason);

        // First 4 legacy GUIDs should map to U01 (Unidentifiable)
        if (legacyGuidString.StartsWith("C9") || legacyGuidString.StartsWith("CB") ||
            legacyGuidString.StartsWith("CC") || legacyGuidString.StartsWith("CA"))
        {
            Assert.Equal("U01", reason.Code);
        }
        // Last 3 should map to U02 (Unreadable)
        else
        {
            Assert.Equal("U02", reason.Code);
        }
    }

    [Fact]
    public void GetByGuid_NullGuid_ReturnsNull()
    {
        // Act
        var reason = IneligibleReasonEnum.GetByGuid(null);

        // Assert
        Assert.Null(reason);
    }

    [Fact]
    public void GetByGuid_UnknownGuid_ReturnsNull()
    {
        // Act
        var reason = IneligibleReasonEnum.GetByGuid(Guid.NewGuid());

        // Assert
        Assert.Null(reason);
    }

    [Theory]
    [InlineData("X01", "Deceased")]
    [InlineData("V01", "Youth aged 18/19/20")]
    [InlineData("R01", "Not a delegate in this election")]
    [InlineData("U01", "Unidentifiable")]
    public void GetByCode_ValidCodes_ReturnCorrectReasons(string code, string expectedDescription)
    {
        // Act
        var reason = IneligibleReasonEnum.GetByCode(code);

        // Assert
        Assert.NotNull(reason);
        Assert.Equal(code, reason.Code);
        Assert.Equal(expectedDescription, reason.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID")]
    [InlineData("x01")] // Wrong case
    public void GetByCode_InvalidCodes_ReturnNull(string? code)
    {
        // Act
        var reason = IneligibleReasonEnum.GetByCode(code);

        // Assert
        Assert.Null(reason);
    }

    [Theory]
    [InlineData("Deceased", "X01")]
    [InlineData("deceased", "X01")] // Case insensitive
    [InlineData("DECEASED", "X01")] // Case insensitive
    [InlineData("Youth aged 18/19/20", "V01")]
    [InlineData("Unidentifiable", "U01")]
    public void GetByDescription_ValidDescriptions_ReturnCorrectReasons(string description, string expectedCode)
    {
        // Act
        var reason = IneligibleReasonEnum.GetByDescription(description);

        // Assert
        Assert.NotNull(reason);
        Assert.Equal(expectedCode, reason.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown description")]
    public void GetByDescription_InvalidDescriptions_ReturnNull(string? description)
    {
        // Act
        var reason = IneligibleReasonEnum.GetByDescription(description);

        // Assert
        Assert.Null(reason);
    }

    [Fact]
    public void PersonReasons_ExcludesInternalOnlyReasons()
    {
        // Act
        var personReasons = IneligibleReasonEnum.PersonReasons;

        // Assert
        Assert.Equal(18, personReasons.Count); // 20 total - 2 internal (U01, U02)
        Assert.DoesNotContain(personReasons, r => r.InternalOnly);
        Assert.DoesNotContain(personReasons, r => r.Code == "U01");
        Assert.DoesNotContain(personReasons, r => r.Code == "U02");
    }

    [Theory]
    [InlineData("X01", false, false)] // Group X
    [InlineData("X09", false, false)] // Group X
    [InlineData("V01", true, false)]  // Group V
    [InlineData("V06", true, false)]  // Group V
    [InlineData("R01", false, true)]  // Group R
    [InlineData("R03", false, true)]  // Group R
    [InlineData("U01", false, false)] // Group U
    [InlineData("U02", false, false)] // Group U
    public void Reasons_HaveCorrectCanVoteCanReceiveVotesFlags(string code, bool expectedCanVote, bool expectedCanReceiveVotes)
    {
        // Act
        var reason = IneligibleReasonEnum.GetByCode(code);

        // Assert
        Assert.NotNull(reason);
        Assert.Equal(expectedCanVote, reason.CanVote);
        Assert.Equal(expectedCanReceiveVotes, reason.CanReceiveVotes);
    }

    [Theory]
    [InlineData("U01", true)]
    [InlineData("U02", true)]
    [InlineData("X01", false)]
    [InlineData("V01", false)]
    [InlineData("R01", false)]
    public void InternalOnly_IsTrueOnlyForUReasons(string code, bool expectedInternalOnly)
    {
        // Act
        var reason = IneligibleReasonEnum.GetByCode(code);

        // Assert
        Assert.NotNull(reason);
        Assert.Equal(expectedInternalOnly, reason.InternalOnly);
    }
}