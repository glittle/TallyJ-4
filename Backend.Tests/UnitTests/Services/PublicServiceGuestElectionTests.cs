using Backend.Context;
using Backend.Enumerations;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Services;

public class PublicServiceGuestElectionTests
{
    [Fact]
    public async Task GetAvailableElectionsAsync_returns_only_elections_with_active_main_teller()
    {
        var electionWithMain = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var electionWithoutMain = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var listedAt = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new MainDbContext(options);
        context.Elections.AddRange(
            new Backend.Entities.Election
            {
                ElectionGuid = electionWithMain,
                Name = "With Main",
                ElectionType = ElectionTypeEnum.LSA.Code,
                ElectionMode = ElectionModeEnum.Normal.Code,
                NumberToElect = 1,
                ListedForPublicAsOf = listedAt.AddMinutes(-1),
                OwnerLoginId = "owner@test",
                RowVersion = new byte[8],
            },
            new Backend.Entities.Election
            {
                ElectionGuid = electionWithoutMain,
                Name = "Without Main",
                ElectionType = ElectionTypeEnum.LSA.Code,
                ElectionMode = ElectionModeEnum.Normal.Code,
                NumberToElect = 1,
                ListedForPublicAsOf = listedAt.AddMinutes(-1),
                OwnerLoginId = "owner@test",
                RowVersion = new byte[8],
            });
        await context.SaveChangesAsync();

        var assignmentService = new Mock<IComputerAssignmentService>();
        assignmentService.Setup(s => s.HasActiveMainTeller(electionWithMain)).Returns(true);
        assignmentService.Setup(s => s.HasActiveMainTeller(electionWithoutMain)).Returns(false);

        var service = new PublicService(
            context,
            assignmentService.Object,
            NullLogger<PublicService>.Instance);

        var elections = await service.GetAvailableElectionsAsync();

        Assert.Single(elections);
        Assert.Equal(electionWithMain, elections[0].ElectionGuid);
        Assert.Equal("With Main", elections[0].Name);
    }
}