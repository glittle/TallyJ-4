using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class ElectionServiceTests : ServiceTestBase
{
    private readonly ElectionService _service;
    private readonly Mock<ILogger<ElectionService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Guid _testUserId;

    public ElectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ElectionService>>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        _testUserId = Guid.NewGuid();
        var userId = _testUserId.ToString();
        var claims = new List<Claim>
        {
            new Claim("sub", userId),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        
        _service = new ElectionService(Context, _loggerMock.Object, _signalRMock.Object, _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task CreateElectionAsync_CreatesElectionSuccessfully()
    {
        var createDto = new CreateElectionDto
        {
            Name = "Test Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 5
        };

        var result = await _service.CreateElectionAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal("Test Election", result.Name);
        Assert.Equal(5, result.NumberToElect);
        Assert.Equal(ElectionStage.SettingUp, result.ElectionStage);
        Assert.NotEqual(Guid.Empty, result.ElectionGuid);

        var electionInDb = Context.Elections.FirstOrDefault(e => e.ElectionGuid == result.ElectionGuid);
        Assert.NotNull(electionInDb);
        Assert.Equal("Test Election", electionInDb.Name);
    }

    [Fact]
    public async Task GetElectionByGuidAsync_WithValidGuid_ReturnsElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.SettingUp,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.GetElectionByGuidAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(election.ElectionGuid, result.ElectionGuid);
        Assert.Equal("Test Election", result.Name);
        Assert.Equal(3, result.NumberToElect);
    }

    [Fact]
    public async Task GetElectionByGuidAsync_WithInvalidGuid_ReturnsNull()
    {
        var result = await _service.GetElectionByGuidAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetElectionByGuidAsync_WithAggregateData_ReturnsElectionWithoutRunningStatsQuery()
    {
        var electionGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();

        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Slim Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.ProcessingBallots,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        });
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Main Hall"
        });
        Context.People.Add(new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Voter",
            LastName = "One",
            CanVote = true,
            RowVersion = new byte[8]
        });
        Context.Ballots.Add(new Ballot
        {
            BallotGuid = Guid.NewGuid(),
            LocationGuid = locationGuid,
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A1",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var election = await _service.GetElectionByGuidAsync(electionGuid);
        var stats = await _service.GetElectionStatsAsync(electionGuid);

        Assert.NotNull(election);
        Assert.Equal("Slim Election", election.Name);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.VoterCount);
        Assert.Equal(1, stats.BallotCount);
        Assert.Equal(1, stats.LocationCount);
    }

    [Fact]
    public async Task GetElectionStatsAsync_ComputesAggregateCounts()
    {
        var electionGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();

        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Count Test Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.ProcessingBallots,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        });
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Main Hall"
        });
        Context.People.AddRange(
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                FirstName = "Voter",
                LastName = "One",
                CanVote = true,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                FirstName = "Voter",
                LastName = "Two",
                CanVote = true,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                FirstName = "Non",
                LastName = "Voter",
                CanVote = false,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                FirstName = "Unit",
                LastName = "Member",
                CanVote = true,
                UnitName = "Area 1",
                RowVersion = new byte[8]
            });
        Context.Ballots.AddRange(
            new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                LocationGuid = locationGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "A1",
                BallotNumAtComputer = 1,
                RowVersion = new byte[8]
            },
            new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                LocationGuid = locationGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "A1",
                BallotNumAtComputer = 2,
                RowVersion = new byte[8]
            },
            new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                LocationGuid = locationGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "A1",
                BallotNumAtComputer = 3,
                RowVersion = new byte[8]
            });
        await Context.SaveChangesAsync();

        var result = await _service.GetElectionStatsAsync(electionGuid);

        Assert.NotNull(result);
        Assert.Equal(3, result.VoterCount);
        Assert.Equal(3, result.BallotCount);
        Assert.Equal(1, result.LocationCount);
    }

    [Fact]
    public async Task GetElectionStatsAsync_WithInvalidGuid_ReturnsNull()
    {
        var result = await _service.GetElectionStatsAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateElectionAsync_WithValidGuid_UpdatesElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Original Name",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.SettingUp,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateElectionDto
        {
            Name = "Updated Name",
            NumberToElect = 7,
            DateOfElection = DateTime.UtcNow.AddDays(20),
            ElectionStage = ElectionStage.GatheringBallots
        };

        var result = await _service.UpdateElectionAsync(election.ElectionGuid, updateDto);

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(7, result.NumberToElect);
        Assert.Equal(ElectionStage.GatheringBallots, result.ElectionStage);
        _signalRMock.Verify(
            s => s.SendOnlineElectionUpdateAsync(It.IsAny<Backend.DTOs.SignalR.OnlineElectionUpdateDto>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateElectionAsync_WhenOnlineWindowChanges_SendsOnlineElectionUpdate()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Online Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.GatheringBallots,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddDays(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddDays(1),
            OnlineCloseIsEstimate = true,
            OnlineSelectionProcess = "A",
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var originalClose = election.OnlineWhenClose;
        var newClose = DateTimeOffset.UtcNow.AddDays(2);
        var updateDto = new UpdateElectionDto
        {
            Name = election.Name,
            OnlineWhenClose = newClose
        };

        Backend.DTOs.SignalR.OnlineElectionUpdateDto? captured = null;
        _signalRMock
            .Setup(s => s.SendOnlineElectionUpdateAsync(It.IsAny<Backend.DTOs.SignalR.OnlineElectionUpdateDto>()))
            .Callback<Backend.DTOs.SignalR.OnlineElectionUpdateDto>(u => captured = u)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateElectionAsync(election.ElectionGuid, updateDto);

        Assert.NotNull(result);
        Assert.NotNull(captured);
        Assert.Equal(election.ElectionGuid, captured!.ElectionGuid);
        Assert.True(captured.OnlineWhenClose.HasValue);
        Assert.True(captured.OnlineWhenClose > originalClose);
        Assert.Equal("A", captured.OnlineSelectionProcess);
        _signalRMock.Verify(
            s => s.SendOnlineElectionUpdateAsync(It.IsAny<Backend.DTOs.SignalR.OnlineElectionUpdateDto>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateElectionAsync_WithInvalidGuid_ReturnsNull()
    {
        var updateDto = new UpdateElectionDto
        {
            Name = "Updated Name",
            NumberToElect = 5
        };

        var result = await _service.UpdateElectionAsync(Guid.NewGuid(), updateDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteElectionAsync_WithValidGuid_DeletesElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Election to Delete",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.SettingUp,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.DeleteElectionAsync(election.ElectionGuid);

        Assert.True(result);

        var deletedElection = Context.Elections.FirstOrDefault(e => e.ElectionGuid == election.ElectionGuid);
        Assert.Null(deletedElection);
    }

    [Fact]
    public async Task DeleteElectionAsync_WithInvalidGuid_ReturnsFalse()
    {
        var result = await _service.DeleteElectionAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task GetElectionsAsync_ReturnsPaginatedResults()
    {
        for (int i = 0; i < 15; i++)
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
            {
                ElectionGuid = electionGuid,
                Name = $"Election {i}",
                ElectionType = "LSA",
                NumberToElect = 3,
                ElectionStage = i % 2 == 0 ? ElectionStage.SettingUp : ElectionStage.GatheringBallots,
                DateOfElection = DateTime.UtcNow.AddDays(i),
                RowVersion = new byte[8]
            };
            Context.Elections.Add(election);
            Context.JoinElectionUsers.Add(new Backend.Entities.JoinElectionUser
            {
                ElectionGuid = electionGuid,
                UserId = _testUserId
            });
        }
        await Context.SaveChangesAsync();

        var result = await _service.GetElectionsAsync(pageNumber: 1, pageSize: 10);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task ChangeElectionStageAsync_FromProcessingBallotsToFinalized_SucceedsWhenReady()
    {
        var electionGuid = Guid.NewGuid();
        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Stage Test Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.ProcessingBallots,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        var personGuid = Guid.NewGuid();
        Context.Elections.Add(election);
        Context.People.Add(new Person
        {
            PersonGuid = personGuid,
            ElectionGuid = electionGuid,
            FirstName = "Test",
            LastName = "Person",
            RowVersion = new byte[8]
        });
        Context.Results.Add(new Result
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            Rank = 1,
            Section = "E",
            VoteCount = 10
        });
        Context.ResultSummaries.Add(new ResultSummary
        {
            ElectionGuid = electionGuid,
            ResultType = "F",
            UseOnReports = true,
            BallotsNeedingReview = 0
        });
        await Context.SaveChangesAsync();

        var result = await _service.ChangeElectionStageAsync(electionGuid, new ChangeElectionStageDto
        {
            ElectionStage = ElectionStage.Finalized
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Election);
        Assert.Equal(ElectionStage.Finalized, result.Election.ElectionStage);

        var inDb = Context.Elections.Single(e => e.ElectionGuid == electionGuid);
        Assert.Equal(ElectionStage.Finalized, inDb.ElectionStage);
    }

    [Fact]
    public async Task ChangeElectionStageAsync_ToFinalized_IsRejectedWhenNotReady()
    {
        var electionGuid = Guid.NewGuid();
        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Not Ready Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.ProcessingBallots,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.ChangeElectionStageAsync(electionGuid, new ChangeElectionStageDto
        {
            ElectionStage = ElectionStage.Finalized
        });

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        var inDb = Context.Elections.Single(e => e.ElectionGuid == electionGuid);
        Assert.Equal(ElectionStage.ProcessingBallots, inDb.ElectionStage);
    }

    [Fact]
    public async Task ChangeElectionStageAsync_FromFinalized_RequiresConfirmation()
    {
        var electionGuid = Guid.NewGuid();
        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Finalized Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.Finalized,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.ChangeElectionStageAsync(electionGuid, new ChangeElectionStageDto
        {
            ElectionStage = ElectionStage.ProcessingBallots
        });

        Assert.True(result.RequiresConfirmation);
        Assert.NotNull(result.ConfirmationReason);

        var inDb = Context.Elections.Single(e => e.ElectionGuid == electionGuid);
        Assert.Equal(ElectionStage.Finalized, inDb.ElectionStage);
    }

    [Fact]
    public async Task ChangeElectionStageAsync_FromFinalized_SucceedsWithConfirmation()
    {
        var electionGuid = Guid.NewGuid();
        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Finalized Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.Finalized,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.ChangeElectionStageAsync(electionGuid, new ChangeElectionStageDto
        {
            ElectionStage = ElectionStage.ProcessingBallots,
            ConfirmLeavingFinalized = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ElectionStage.ProcessingBallots, result.Election!.ElectionStage);
    }

    [Fact]
    public async Task ChangeElectionStageAsync_SkipStage_IsAllowed()
    {
        var electionGuid = Guid.NewGuid();
        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Skip Stage Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.SettingUp,
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.ChangeElectionStageAsync(electionGuid, new ChangeElectionStageDto
        {
            ElectionStage = ElectionStage.ProcessingBallots
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Election);

        var inDb = Context.Elections.Single(e => e.ElectionGuid == electionGuid);
        Assert.Equal(ElectionStage.ProcessingBallots, inDb.ElectionStage);
    }

    [Fact]
    public async Task GetElectionsAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        for (int i = 0; i < 10; i++)
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
            {
                ElectionGuid = electionGuid,
                Name = $"Election {i}",
                ElectionType = "LSA",
                NumberToElect = 3,
                ElectionStage = i % 2 == 0 ? ElectionStage.SettingUp : ElectionStage.GatheringBallots,
                DateOfElection = DateTime.UtcNow.AddDays(i),
                RowVersion = new byte[8]
            };
            Context.Elections.Add(election);
            Context.JoinElectionUsers.Add(new Backend.Entities.JoinElectionUser
            {
                ElectionGuid = electionGuid,
                UserId = _testUserId
            });
        }
        await Context.SaveChangesAsync();

        var result = await _service.GetElectionsAsync(pageNumber: 1, pageSize: 10, status: "SettingUp");

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.All(result.Items, e => Assert.Equal(ElectionStage.SettingUp, e.ElectionStage));
    }

    [Fact]
    public async Task ToggleTellerAccessAsync_broadcastsPublicElectionListUpdate()
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Guest Access Election",
            ElectionType = ElectionTypeEnum.LSA.Code,
            ElectionMode = ElectionModeEnum.Normal.Code,
            NumberToElect = 3,
            OwnerLoginId = "owner@test",
            RowVersion = new byte[8],
        });
        await Context.SaveChangesAsync();

        var result = await _service.ToggleTellerAccessAsync(electionGuid, isOpen: true);

        Assert.NotNull(result);
        Assert.True(result!.IsTellerAccessOpen);
        _signalRMock.Verify(
            s => s.SendPublicElectionListUpdateAsync(electionGuid, true),
            Times.Once);
        _signalRMock.Verify(
            s => s.CloseOutGuestTellersAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ToggleTellerAccessAsync_close_broadcastsGuestCloseoutAndListUpdate()
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Closing Guest Access",
            ElectionType = ElectionTypeEnum.LSA.Code,
            ElectionMode = ElectionModeEnum.Normal.Code,
            NumberToElect = 3,
            OwnerLoginId = "owner@test",
            ListedForPublicAsOf = DateTimeOffset.UtcNow.AddHours(-1),
            RowVersion = new byte[8],
        });
        await Context.SaveChangesAsync();

        var result = await _service.ToggleTellerAccessAsync(electionGuid, isOpen: false);

        Assert.NotNull(result);
        Assert.False(result!.IsTellerAccessOpen);
        _signalRMock.Verify(
            s => s.SendPublicElectionListUpdateAsync(electionGuid, false),
            Times.Once);
        _signalRMock.Verify(
            s => s.CloseOutGuestTellersAsync(electionGuid),
            Times.Once);
    }

    [Fact]
    public async Task DuplicateElectionAsync_CreatesNewTestElectionWithCopiedPeopleAndSettings()
    {
        var source = await SeedSourceElectionForDuplicateAsync();

        var result = await _service.DuplicateElectionAsync(source.ElectionGuid, new DuplicateElectionDto());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Election);
        Assert.NotEqual(source.ElectionGuid, result.Election.ElectionGuid);
        Assert.Equal("Copy of Source Election", result.Election.Name);
        Assert.True(result.Election.ShowAsTest);
        Assert.Equal(ElectionStage.SettingUp, result.Election.ElectionStage);
        Assert.Equal(9, result.Election.NumberToElect);
        Assert.Equal(2, result.Election.NumberExtra);
        Assert.Equal(ElectionTypeCode.LSA, result.Election.ElectionType);
        Assert.Equal("PDM", result.Election.VotingMethods);
        Assert.False(result.Election.UseOnlineVoting);
        Assert.Null(result.Election.OnlineWhenOpen);
        Assert.Null(result.Election.OnlineWhenClose);
        Assert.Equal("A", result.Election.OnlineSelectionProcess);

        var copyInDb = Context.Elections.Single(e => e.ElectionGuid == result.Election.ElectionGuid);
        Assert.True(copyInDb.ShowAsTest);
        Assert.Null(copyInDb.LastEnvNum);
        Assert.Null(copyInDb.ListedForPublicAsOf);
        Assert.Null(copyInDb.OwnerLoginId);

        var copiedPeople = Context.People.Where(p => p.ElectionGuid == copyInDb.ElectionGuid).ToList();
        Assert.Equal(2, copiedPeople.Count);
        Assert.Contains(copiedPeople, p => p.FirstName == "Ada" && p.LastName == "Lovelace" && p.Phone == "+15551212" && p.CanVote == true);
        Assert.Contains(copiedPeople, p => p.FirstName == "Grace" && p.LastName == "Hopper" && p.IneligibleReasonCode == "X01");
        Assert.All(copiedPeople, p =>
        {
            Assert.NotEqual(Guid.Empty, p.PersonGuid);
            Assert.DoesNotContain(source.PeopleGuids, g => g == p.PersonGuid);
            Assert.Null(p.RegistrationTime);
            Assert.Null(p.VotingLocationGuid);
            Assert.Null(p.VotingMethod);
            Assert.Null(p.EnvNum);
            Assert.Null(p.Teller1);
            Assert.Null(p.HasOnlineBallot);
            Assert.Null(p.RegistrationHistory);
        });

        var copiedLocations = Context.Locations.Where(l => l.ElectionGuid == copyInDb.ElectionGuid).ToList();
        Assert.Single(copiedLocations);
        Assert.Equal("Main Hall", copiedLocations[0].Name);
        Assert.NotEqual(source.LocationGuid, copiedLocations[0].LocationGuid);
        Assert.Null(copiedLocations[0].LocationTallyStatus);
        Assert.Null(copiedLocations[0].BallotsCollected);

        var join = Context.JoinElectionUsers.Single(j => j.ElectionGuid == copyInDb.ElectionGuid);
        Assert.Equal(_testUserId, join.UserId);
        Assert.Equal("Admin", join.Role);

        Assert.True(Context.OnlineVoters.Any(ov => ov.VoterId == "+15551212" && ov.VoterIdType == "P"));
    }

    [Fact]
    public async Task DuplicateElectionAsync_UsesSuppliedName()
    {
        var source = await SeedSourceElectionForDuplicateAsync();

        var result = await _service.DuplicateElectionAsync(
            source.ElectionGuid,
            new DuplicateElectionDto { Name = "  My Test Copy  " });

        Assert.True(result.IsSuccess);
        Assert.Equal("My Test Copy", result.Election!.Name);
    }

    [Fact]
    public async Task DuplicateElectionAsync_DoesNotCopyBallotsResultsOrRuntimeRows()
    {
        var source = await SeedSourceElectionForDuplicateAsync();

        var result = await _service.DuplicateElectionAsync(source.ElectionGuid, new DuplicateElectionDto());

        Assert.True(result.IsSuccess);
        var copyGuid = result.Election!.ElectionGuid;
        var copyLocationGuids = Context.Locations
            .Where(l => l.ElectionGuid == copyGuid)
            .Select(l => l.LocationGuid)
            .ToList();

        Assert.Equal(1, Context.Ballots.Count(b => b.LocationGuid == source.LocationGuid));
        Assert.Equal(0, Context.Ballots.Count(b => copyLocationGuids.Contains(b.LocationGuid)));
        Assert.Equal(1, Context.Results.Count(r => r.ElectionGuid == source.ElectionGuid));
        Assert.Equal(0, Context.Results.Count(r => r.ElectionGuid == copyGuid));
        Assert.Equal(1, Context.ResultSummaries.Count(r => r.ElectionGuid == source.ElectionGuid));
        Assert.Equal(0, Context.ResultSummaries.Count(r => r.ElectionGuid == copyGuid));
        Assert.Equal(1, Context.Computers.Count(c => c.ElectionGuid == source.ElectionGuid));
        Assert.Equal(0, Context.Computers.Count(c => c.ElectionGuid == copyGuid));
        Assert.Equal(1, Context.Tellers.Count(t => t.ElectionGuid == source.ElectionGuid));
        Assert.Equal(0, Context.Tellers.Count(t => t.ElectionGuid == copyGuid));
        Assert.Equal(1, Context.OnlineVotingInfos.Count(o => o.ElectionGuid == source.ElectionGuid));
        Assert.Equal(0, Context.OnlineVotingInfos.Count(o => o.ElectionGuid == copyGuid));
        Assert.Equal(1, Context.SmsLogs.Count(s => s.ElectionGuid == source.ElectionGuid));
        Assert.Equal(0, Context.SmsLogs.Count(s => s.ElectionGuid == copyGuid));
    }

    [Fact]
    public async Task DuplicateElectionAsync_LeavesOriginalElectionUnchanged()
    {
        var source = await SeedSourceElectionForDuplicateAsync();

        await _service.DuplicateElectionAsync(source.ElectionGuid, new DuplicateElectionDto());

        var original = Context.Elections.Single(e => e.ElectionGuid == source.ElectionGuid);
        Assert.Equal("Source Election", original.Name);
        Assert.False(original.ShowAsTest);
        Assert.Equal(ElectionStage.ProcessingBallots, original.ElectionStage);
        Assert.Equal(42, original.LastEnvNum);
        Assert.NotNull(original.ListedForPublicAsOf);
        Assert.Equal("original-owner", original.OwnerLoginId);
        Assert.True(original.UseOnlineVoting);
        Assert.NotNull(original.OnlineWhenOpen);
        Assert.NotNull(original.OnlineWhenClose);
        Assert.Equal(2, Context.People.Count(p => p.ElectionGuid == source.ElectionGuid));
        Assert.Equal(1, Context.Locations.Count(l => l.ElectionGuid == source.ElectionGuid));
        Assert.Equal(1, Context.Ballots.Count(b => b.LocationGuid == source.LocationGuid));
        Assert.Equal(1, Context.Results.Count(r => r.ElectionGuid == source.ElectionGuid));
    }

    [Fact]
    public async Task DuplicateElectionAsync_OpenOnlineWindow_DoesNotLeaveCopyAvailable()
    {
        var source = await SeedSourceElectionForDuplicateAsync();
        var now = DateTimeOffset.UtcNow;
        var original = Context.Elections.Single(e => e.ElectionGuid == source.ElectionGuid);
        Assert.True(IsAvailableToOnlineVoters(original, now));

        var result = await _service.DuplicateElectionAsync(source.ElectionGuid, new DuplicateElectionDto());

        Assert.True(result.IsSuccess);
        var copy = Context.Elections.Single(e => e.ElectionGuid == result.Election!.ElectionGuid);
        Assert.False(copy.UseOnlineVoting);
        Assert.Null(copy.OnlineWhenOpen);
        Assert.Null(copy.OnlineWhenClose);
        Assert.False(IsAvailableToOnlineVoters(copy, now));

        Context.ChangeTracker.Clear();
        var sourceAfter = Context.Elections.Single(e => e.ElectionGuid == source.ElectionGuid);
        Assert.True(IsAvailableToOnlineVoters(sourceAfter, now));
    }

    [Fact]
    public async Task DuplicateElectionAsync_UnauthorizedUser_IsForbidden()
    {
        var source = await SeedSourceElectionForDuplicateAsync();
        SetCurrentUser(Guid.NewGuid());

        var result = await _service.DuplicateElectionAsync(source.ElectionGuid, new DuplicateElectionDto());

        Assert.True(result.IsForbidden);
        Assert.False(result.IsSuccess);
        Assert.Equal(1, Context.Elections.Count(e => e.Name == "Source Election"));
        Assert.Equal(0, Context.Elections.Count(e => e.Name.StartsWith("Copy of")));
    }

    [Fact]
    public async Task DuplicateElectionAsync_TellerRole_IsForbidden()
    {
        var source = await SeedSourceElectionForDuplicateAsync();
        var tellerId = Guid.NewGuid();
        Context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = source.ElectionGuid,
            UserId = tellerId,
            Role = "Teller"
        });
        await Context.SaveChangesAsync();
        SetCurrentUser(tellerId);

        var result = await _service.DuplicateElectionAsync(source.ElectionGuid, new DuplicateElectionDto());

        Assert.True(result.IsForbidden);
        Assert.Equal(0, Context.Elections.Count(e => e.Name.StartsWith("Copy of")));
    }

    [Fact]
    public async Task DuplicateElectionAsync_MissingElection_IsNotFound()
    {
        var result = await _service.DuplicateElectionAsync(Guid.NewGuid(), new DuplicateElectionDto());

        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ResolveDuplicateName_TruncatesDefaultTo150()
    {
        var longName = new string('A', 150);
        var resolved = ElectionService.ResolveDuplicateName(null, longName);
        Assert.Equal(150, resolved.Length);
        Assert.StartsWith("Copy of ", resolved);
    }

    private void SetCurrentUser(Guid userId)
    {
        var userIdString = userId.ToString();
        var claims = new List<Claim>
        {
            new Claim("sub", userIdString),
            new Claim(ClaimTypes.NameIdentifier, userIdString)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private async Task<SeededSourceElection> SeedSourceElectionForDuplicateAsync()
    {
        var electionGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();
        var personGuid = Guid.NewGuid();
        var otherPersonGuid = Guid.NewGuid();
        var listedAt = DateTimeOffset.UtcNow.AddHours(-2);

        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Source Election",
            Convenor = "Local Assembly",
            DateOfElection = DateTimeOffset.UtcNow.AddDays(10),
            ElectionType = "LSA",
            ElectionMode = "N",
            NumberToElect = 9,
            NumberExtra = 2,
            ElectionStage = ElectionStage.ProcessingBallots,
            ShowAsTest = false,
            LastEnvNum = 42,
            ListedForPublicAsOf = listedAt,
            OwnerLoginId = "original-owner",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-2),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddDays(2),
            OnlineSelectionProcess = "A",
            VotingMethods = "PDM",
            ElectionPasscode = "secret",
            RowVersion = new byte[8]
        });
        Context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = electionGuid,
            UserId = _testUserId,
            Role = "Admin"
        });
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Main Hall",
            ContactInfo = "front desk",
            SortOrder = 1,
            LocationTallyStatus = LocationTallyStatus.Complete,
            BallotsCollected = 12
        });
        Context.People.AddRange(
            new Person
            {
                PersonGuid = personGuid,
                ElectionGuid = electionGuid,
                FirstName = "Ada",
                LastName = "Lovelace",
                Phone = "+15551212",
                Email = "ada@example.com",
                CanVote = true,
                CanReceiveVotes = true,
                RegistrationTime = DateTimeOffset.UtcNow.AddHours(-1),
                VotingLocationGuid = locationGuid,
                VotingMethod = "P",
                EnvNum = 7,
                Teller1 = "Pat",
                HasOnlineBallot = true,
                RegistrationHistory = "[{}]",
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = otherPersonGuid,
                ElectionGuid = electionGuid,
                FirstName = "Grace",
                LastName = "Hopper",
                CanVote = false,
                CanReceiveVotes = false,
                IneligibleReasonCode = "X01",
                RowVersion = new byte[8]
            });
        Context.Ballots.Add(new Ballot
        {
            BallotGuid = Guid.NewGuid(),
            LocationGuid = locationGuid,
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A1",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        });
        Context.Results.Add(new Result
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            Rank = 1,
            Section = "E",
            VoteCount = 10
        });
        Context.ResultSummaries.Add(new ResultSummary
        {
            ElectionGuid = electionGuid,
            ResultType = "F",
            UseOnReports = true,
            BallotsNeedingReview = 0
        });
        Context.Computers.Add(new Computer
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            ComputerGuid = Guid.NewGuid(),
            ComputerCode = "A1"
        });
        Context.Tellers.Add(new Teller
        {
            ElectionGuid = electionGuid,
            Name = "Head Teller",
            RowVersion = new byte[8]
        });
        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            Status = "Submitted"
        });
        Context.SmsLogs.Add(new SmsLog
        {
            SmsSid = "SM-test-1",
            Phone = "+15551212",
            SentDate = DateTimeOffset.UtcNow,
            ElectionGuid = electionGuid,
            PersonGuid = personGuid
        });
        await Context.SaveChangesAsync();

        return new SeededSourceElection(electionGuid, locationGuid, [personGuid, otherPersonGuid]);
    }

    private sealed record SeededSourceElection(Guid ElectionGuid, Guid LocationGuid, Guid[] PeopleGuids);

    /// <summary>
    /// Same open predicate as <c>OnlineVotingService.GetAvailableElectionsAsync</c>
    /// (UseOnlineVoting plus a null window counts as open; ShowAsTest is not checked).
    /// </summary>
    private static bool IsAvailableToOnlineVoters(Election election, DateTimeOffset now) =>
        election.UseOnlineVoting
        && (election.OnlineWhenOpen == null || election.OnlineWhenOpen <= now)
        && (election.OnlineWhenClose == null || election.OnlineWhenClose > now);
}



