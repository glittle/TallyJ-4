using System.Text;
using Backend.DTOs.Elections;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Package import stores name parts; FullName/FullNameFl are computed in memory (issue #247).
/// </summary>
public class ElectionPackageImportServiceTests : ServiceTestBase
{
    private readonly Mock<IElectionService> _electionServiceMock = new();
    private readonly Mock<ISignalRNotificationService> _signalRMock = new();

    public ElectionPackageImportServiceTests()
    {
        _electionServiceMock
            .Setup(s => s.GetElectionByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid guid) => new ElectionDto { ElectionGuid = guid, Name = "Imported" });
    }

    [Fact]
    public async Task ImportTallyJv3ElectionAsync_PeopleHaveComputedDisplayNames()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <TallyJ2 xmlns="urn:tallyj.bahai:v2" Exported="2026-01-01T00:00:00Z" Version="3.7.0">
              <election ElectionGuid="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" Name="Name Import Test"
                        ElectionType="Con" ElectionMode="N" NumberToElect="1" NumberExtra="0" />
              <person PersonGuid="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" LastName="Roel" FirstName="Diego"
                      OtherLastNames="Smith" OtherNames="D" />
              <person PersonGuid="cccccccc-cccc-cccc-cccc-cccccccccccc" LastName="Eligible" FirstName="Alice" />
            </TallyJ2>
            """;

        var service = new TallyJv3ElectionImportService(Context, _electionServiceMock.Object, _signalRMock.Object);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var election = await service.ImportTallyJv3ElectionAsync(stream);

        var people = await Context.People
            .Where(p => p.ElectionGuid == election.ElectionGuid)
            .OrderBy(p => p.LastName)
            .ToListAsync();

        Assert.Equal(2, people.Count);

        var diego = people.Single(p => p.LastName == "Roel");
        Assert.Equal("Diego", diego.FirstName);
        Assert.Equal("Roel [Smith], Diego [D]", diego.FullName);
        Assert.Equal("Diego Roel [D] [Smith]", diego.FullNameFl);
        Assert.True(diego.CanVote);
        Assert.True(diego.CanReceiveVotes);

        var alice = people.Single(p => p.LastName == "Eligible");
        Assert.Equal("Eligible, Alice", alice.FullName);
        Assert.Equal("Alice Eligible", alice.FullNameFl);
        Assert.True(alice.CanVote);
        Assert.True(alice.CanReceiveVotes);
    }

    [Fact]
    public async Task ImportElectionFromJsonAsync_PeopleHaveComputedDisplayNames()
    {
        var personGuid = Guid.NewGuid();
        var json = $$"""
            {
              "format": "TallyJ4",
              "version": "4.0",
              "exportedAt": "2026-01-01T00:00:00Z",
              "election": {
                "ElectionGuid": "{{Guid.NewGuid()}}",
                "Name": "JSON Name Import Test",
                "ElectionType": "Con",
                "ElectionMode": "N",
                "NumberToElect": 1
              },
              "locations": [],
              "people": [
                {
                  "PersonGuid": "{{personGuid}}",
                  "LastName": "Roel",
                  "FirstName": "Diego",
                  "OtherLastNames": "Smith",
                  "OtherNames": "D"
                }
              ],
              "ballots": [],
              "tellers": [],
              "results": [],
              "resultSummaries": [],
              "resultTies": [],
              "onlineVotingInfos": [],
              "logs": []
            }
            """;

        var service = new JsonElectionImportExportService(Context, _electionServiceMock.Object, _signalRMock.Object);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var election = await service.ImportElectionFromJsonAsync(stream);

        var person = await Context.People.SingleAsync(p => p.ElectionGuid == election.ElectionGuid);

        Assert.Equal("Roel [Smith], Diego [D]", person.FullName);
        Assert.Equal("Diego Roel [D] [Smith]", person.FullNameFl);
        Assert.True(person.CanVote);
        Assert.True(person.CanReceiveVotes);
    }
}
