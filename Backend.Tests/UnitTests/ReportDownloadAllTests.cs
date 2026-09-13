using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Localization;
using Moq;
using Backend.Entities;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class ReportDownloadAllTests : ServiceTestBase
{
    private readonly ReportService _service;
    private readonly Guid _electionGuid = Guid.NewGuid();

    public ReportDownloadAllTests()
    {
        var localizer = new Mock<IStringLocalizer<ReportService>>();
        localizer
            .Setup(l => l[LocationDisplayHelper.TypeOnlineKey])
            .Returns(new LocalizedString(LocationDisplayHelper.TypeOnlineKey, "Online"));
        _service = new ReportService(Context, localizer.Object);
    }

    [Fact]
    public async Task GetAllReportsZip_ContainsCsvForEachAvailableReport()
    {
        SeedElection();
        Context.Locations.Add(new Location
        {
            ElectionGuid = _electionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "Hall"
        });
        Context.People.Add(new Person
        {
            ElectionGuid = _electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = "Youth",
            FirstName = "Pat",
            CanVote = true,
            CanReceiveVotes = false,
            IneligibleReasonCode = Backend.Enumerations.IneligibleReasonEnum.V01_YouthAged181920.Code,
            VotingMethod = "I",
            Area = "North",
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var available = await _service.GetAvailableReportsAsync(_electionGuid);
        var (zipBytes, fileName) = await _service.GetAllReportsZipAsync(_electionGuid);

        Assert.EndsWith("-reports.zip", fileName);
        Assert.Contains("CountCheck", fileName);

        using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.Name).ToHashSet();
        foreach (var report in available)
        {
            Assert.Contains($"{report.Code}.csv", names);
        }

        var byArea = zip.GetEntry("VotersByArea.csv");
        Assert.NotNull(byArea);
        using var reader = new StreamReader(byArea!.Open(), Encoding.UTF8);
        var csv = await reader.ReadToEndAsync();
        Assert.Contains("18+", csv);
        Assert.Contains("18-21", csv);
        Assert.Contains("Imported", csv);
    }

    [Fact]
    public async Task GetReportByCode_Unknown_Throws()
    {
        SeedElection();
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetReportByCodeAsync(_electionGuid, "NotAReport"));
    }

    private void SeedElection()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionGuid,
            Name = "CountCheck",
            ElectionType = "LSA",
            NumberToElect = 3,
            VotingMethods = "P,IM",
            RowVersion = new byte[8]
        });
    }
}
