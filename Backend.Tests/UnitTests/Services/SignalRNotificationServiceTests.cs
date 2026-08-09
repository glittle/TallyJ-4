using Backend.DTOs.FrontDesk;
using Backend.DTOs.SignalR;
using Backend.Enumerations;
using Backend.Hubs;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Contract tests for hub fan-out: event names and group targets must match the SPA.
/// </summary>
public class SignalRNotificationServiceTests
{
    private readonly Guid _electionGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private (
        SignalRNotificationService Service,
        Mock<IHubClients> MainClients,
        Mock<IHubClients> FrontDeskClients,
        Mock<IHubClients> BallotImportClients,
        Mock<IHubClients> PeopleImportClients,
        Mock<IHubClients> ElectionPackageImportClients,
        Mock<IHubClients> AllVotersClients,
        Mock<IHubClients> VoterPersonalClients,
        Dictionary<string, Mock<IClientProxy>> GroupProxies) CreateService()
    {
        var mainHub = new Mock<IHubContext<MainHub>>();
        var analyzeHub = new Mock<IHubContext<AnalyzeHub>>();
        var ballotImportHub = new Mock<IHubContext<BallotImportHub>>();
        var peopleImportHub = new Mock<IHubContext<PeopleImportHub>>();
        var electionPackageImportHub = new Mock<IHubContext<ElectionPackageImportHub>>();
        var frontDeskHub = new Mock<IHubContext<FrontDeskHub>>();
        var publicHub = new Mock<IHubContext<PublicHub>>();
        var allVotersHub = new Mock<IHubContext<AllVotersHub>>();
        var voterPersonalHub = new Mock<IHubContext<VoterPersonalHub>>();
        var mainClients = new Mock<IHubClients>();
        var frontDeskClients = new Mock<IHubClients>();
        var ballotImportClients = new Mock<IHubClients>();
        var peopleImportClients = new Mock<IHubClients>();
        var electionPackageImportClients = new Mock<IHubClients>();
        var allVotersClients = new Mock<IHubClients>();
        var voterPersonalClients = new Mock<IHubClients>();
        var groupProxies = new Dictionary<string, Mock<IClientProxy>>();

        Mock<IClientProxy> GetOrCreateProxy(string groupName)
        {
            if (!groupProxies.TryGetValue(groupName, out var proxy))
            {
                proxy = new Mock<IClientProxy>();
                proxy
                    .Setup(p => p.SendCoreAsync(
                        It.IsAny<string>(),
                        It.IsAny<object?[]>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                groupProxies[groupName] = proxy;
            }

            return proxy;
        }

        mainHub.Setup(h => h.Clients).Returns(mainClients.Object);
        frontDeskHub.Setup(h => h.Clients).Returns(frontDeskClients.Object);
        ballotImportHub.Setup(h => h.Clients).Returns(ballotImportClients.Object);
        peopleImportHub.Setup(h => h.Clients).Returns(peopleImportClients.Object);
        electionPackageImportHub.Setup(h => h.Clients).Returns(electionPackageImportClients.Object);
        allVotersHub.Setup(h => h.Clients).Returns(allVotersClients.Object);
        voterPersonalHub.Setup(h => h.Clients).Returns(voterPersonalClients.Object);
        mainClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) => GetOrCreateProxy(groupName).Object);
        frontDeskClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) => GetOrCreateProxy(groupName).Object);
        ballotImportClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) => GetOrCreateProxy(groupName).Object);
        peopleImportClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) => GetOrCreateProxy(groupName).Object);
        electionPackageImportClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) => GetOrCreateProxy(groupName).Object);
        allVotersClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) => GetOrCreateProxy(groupName).Object);
        voterPersonalClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) => GetOrCreateProxy(groupName).Object);

        var service = new SignalRNotificationService(
            mainHub.Object,
            analyzeHub.Object,
            ballotImportHub.Object,
            peopleImportHub.Object,
            electionPackageImportHub.Object,
            frontDeskHub.Object,
            publicHub.Object,
            allVotersHub.Object,
            voterPersonalHub.Object,
            NullLogger<SignalRNotificationService>.Instance);

        return (
            service,
            mainClients,
            frontDeskClients,
            ballotImportClients,
            peopleImportClients,
            electionPackageImportClients,
            allVotersClients,
            voterPersonalClients,
            groupProxies);
    }

    [Fact]
    public async Task SendElectionUpdateAsync_sends_statusChanged_to_base_Main_group()
    {
        var (service, mainClients, _, _, _, _, _, _, groupProxies) = CreateService();
        var update = new ElectionUpdateDto
        {
            ElectionGuid = _electionGuid,
            Name = "Test Election",
            ElectionStage = ElectionStage.GatheringBallots,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var expectedGroup = MainHub.GetGroupName(_electionGuid);

        await service.SendElectionUpdateAsync(update);

        mainClients.Verify(c => c.Group(expectedGroup), Times.Once);
        mainClients.Verify(c => c.Group(expectedGroup + "Known"), Times.Never);
        mainClients.Verify(c => c.Group(expectedGroup + "Guest"), Times.Never);

        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "statusChanged",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "statusChanged"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Single(capturedArgs);
        var dto = Assert.IsType<ElectionUpdateDto>(capturedArgs[0]);
        Assert.Equal(_electionGuid, dto.ElectionGuid);
        Assert.Equal("Test Election", dto.Name);
        Assert.Equal(ElectionStage.GatheringBallots, dto.ElectionStage);
    }

    [Fact]
    public async Task CloseOutGuestTellersAsync_sends_electionClosed_to_Guest_group_only()
    {
        var (service, mainClients, _, _, _, _, _, _, groupProxies) = CreateService();
        var baseGroup = MainHub.GetGroupName(_electionGuid);
        var guestGroup = baseGroup + "Guest";

        await service.CloseOutGuestTellersAsync(_electionGuid);

        mainClients.Verify(c => c.Group(guestGroup), Times.Once);
        mainClients.Verify(c => c.Group(baseGroup), Times.Never);
        mainClients.Verify(c => c.Group(baseGroup + "Known"), Times.Never);

        Assert.True(groupProxies.TryGetValue(guestGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "electionClosed",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("added", "PersonAdded")]
    [InlineData("updated", "PersonUpdated")]
    [InlineData("deleted", "PersonDeleted")]
    [InlineData("unknown", "PersonUpdated")]
    public async Task SendPersonUpdateAsync_sends_action_event_to_FrontDesk_group(
        string action,
        string expectedEvent)
    {
        var (service, _, frontDeskClients, _, _, _, _, _, groupProxies) = CreateService();
        var personGuid = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var update = new PersonUpdateDto
        {
            ElectionGuid = _electionGuid,
            PersonGuid = personGuid,
            Action = action,
            FirstName = "Ada",
            LastName = "Lovelace",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var expectedGroup = FrontDeskHub.GetGroupName(_electionGuid);

        await service.SendPersonUpdateAsync(update);

        frontDeskClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                expectedEvent,
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], expectedEvent));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Single(capturedArgs);
        var dto = Assert.IsType<PersonUpdateDto>(capturedArgs[0]);
        Assert.Equal(personGuid, dto.PersonGuid);
        Assert.Equal(action, dto.Action);
    }

    [Fact]
    public async Task NotifyVoterCountUpdatedAsync_sends_VoterCountUpdated_to_FrontDesk_group()
    {
        var (service, _, frontDeskClients, _, _, _, _, _, groupProxies) = CreateService();
        var stats = new FrontDeskStatsDto
        {
            TotalEligible = 100,
            CheckedIn = 40,
            NotYetCheckedIn = 60
        };
        var expectedGroup = FrontDeskHub.GetGroupName(_electionGuid);

        await service.NotifyVoterCountUpdatedAsync(_electionGuid, stats);

        frontDeskClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "VoterCountUpdated",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "VoterCountUpdated"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Single(capturedArgs);
        var dto = Assert.IsType<FrontDeskStatsDto>(capturedArgs[0]);
        Assert.Equal(100, dto.TotalEligible);
        Assert.Equal(40, dto.CheckedIn);
        Assert.Equal(60, dto.NotYetCheckedIn);
    }

    [Fact]
    public async Task SendOnlineElectionUpdateAsync_sends_updateOnlineElection_to_FrontDesk_and_updateVoters_to_AllVoters()
    {
        var (service, _, frontDeskClients, _, _, _, allVotersClients, _, groupProxies) = CreateService();
        var open = DateTimeOffset.Parse("2026-04-01T12:00:00Z");
        var close = DateTimeOffset.Parse("2026-04-02T12:00:00Z");
        var update = new OnlineElectionUpdateDto
        {
            ElectionGuid = _electionGuid,
            UseOnlineVoting = true,
            OnlineWhenOpen = open,
            OnlineWhenClose = close,
            OnlineCloseIsEstimate = true,
            OnlineSelectionProcess = "A"
        };
        var frontDeskGroup = FrontDeskHub.GetGroupName(_electionGuid);
        var allVotersGroup = AllVotersHub.GetGroupName();

        await service.SendOnlineElectionUpdateAsync(update);

        frontDeskClients.Verify(c => c.Group(frontDeskGroup), Times.Once);
        allVotersClients.Verify(c => c.Group(allVotersGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(frontDeskGroup, out var frontDeskProxy));
        frontDeskProxy!.Verify(
            p => p.SendCoreAsync(
                "updateOnlineElection",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(groupProxies.TryGetValue(allVotersGroup, out var allVotersProxy));
        allVotersProxy!.Verify(
            p => p.SendCoreAsync(
                "updateVoters",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var invocation = frontDeskProxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "updateOnlineElection"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Single(capturedArgs);
        var dto = Assert.IsType<OnlineElectionUpdateDto>(capturedArgs[0]);
        Assert.Equal(_electionGuid, dto.ElectionGuid);
        Assert.Equal(open, dto.OnlineWhenOpen);
        Assert.Equal(close, dto.OnlineWhenClose);
        Assert.True(dto.OnlineCloseIsEstimate);
        Assert.Equal("A", dto.OnlineSelectionProcess);
    }

    [Fact]
    public async Task NotifyVoterPersonalUpdateAsync_sends_updateVoter_only_to_person_identity_groups()
    {
        var (service, _, _, _, _, _, _, voterPersonalClients, groupProxies) = CreateService();
        var update = new VoterPersonalUpdateDto
        {
            UpdateRegistration = true,
            ElectionGuid = _electionGuid,
            VotingMethod = "P",
            RegistrationTime = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
        };

        await service.NotifyVoterPersonalUpdateAsync(
            "alice@example.com",
            "+15551212",
            null,
            update);

        var emailGroup = VoterPersonalHub.GetGroupName("alice@example.com");
        var phoneGroup = VoterPersonalHub.GetGroupName("+15551212");
        var otherGroup = VoterPersonalHub.GetGroupName("bob@example.com");

        voterPersonalClients.Verify(c => c.Group(emailGroup), Times.Once);
        voterPersonalClients.Verify(c => c.Group(phoneGroup), Times.Once);
        voterPersonalClients.Verify(c => c.Group(otherGroup), Times.Never);

        Assert.True(groupProxies.TryGetValue(emailGroup, out var emailProxy));
        emailProxy!.Verify(
            p => p.SendCoreAsync(
                "updateVoter",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.False(groupProxies.ContainsKey(otherGroup));
    }

    [Fact]
    public async Task NotifyVoterLoginElsewhereAsync_sends_login_updateVoter_to_voter_group()
    {
        var (service, _, _, _, _, _, _, voterPersonalClients, groupProxies) = CreateService();
        const string voterId = "alice@example.com";
        var expectedGroup = VoterPersonalHub.GetGroupName(voterId);

        await service.NotifyVoterLoginElsewhereAsync(voterId);

        voterPersonalClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "updateVoter",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "updateVoter"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        var dto = Assert.IsType<VoterPersonalUpdateDto>(capturedArgs[0]);
        Assert.True(dto.Login);
        Assert.False(dto.UpdateRegistration);
    }

    [Fact]
    public async Task NotifyVoterLoginElsewhereAsync_trims_voterId_for_group_name()
    {
        var (service, _, _, _, _, _, _, voterPersonalClients, groupProxies) = CreateService();
        var expectedGroup = VoterPersonalHub.GetGroupName("alice@example.com");

        await service.NotifyVoterLoginElsewhereAsync("  alice@example.com  ");

        voterPersonalClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "updateVoter",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestFrontDeskReloadAsync_sends_reloadPage_to_FrontDesk_group()
    {
        var (service, _, frontDeskClients, _, _, _, _, _, groupProxies) = CreateService();
        var expectedGroup = FrontDeskHub.GetGroupName(_electionGuid);

        await service.RequestFrontDeskReloadAsync(_electionGuid);

        frontDeskClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "reloadPage",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendImportProgressAsync_sends_camelCase_importProgress_to_BallotImport_group()
    {
        var (service, _, _, ballotImportClients, _, _, _, _, groupProxies) = CreateService();
        var progress = new ImportProgressDto
        {
            ElectionGuid = _electionGuid,
            TotalRows = 10,
            ProcessedRows = 4,
            SuccessCount = 3,
            ErrorCount = 1,
            CurrentStatus = "Processing",
            PercentComplete = 40,
            IsComplete = false
        };
        var expectedGroup = BallotImportHub.GetGroupName(_electionGuid);

        await service.SendImportProgressAsync(progress);

        ballotImportClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "importProgress",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Must not use PascalCase names that miss SPA listeners
        proxy.Verify(
            p => p.SendCoreAsync(
                "ImportProgress",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "importProgress"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Single(capturedArgs);
        var dto = Assert.IsType<ImportProgressDto>(capturedArgs[0]);
        Assert.Equal(4, dto.ProcessedRows);
        Assert.Equal(10, dto.TotalRows);
    }

    [Fact]
    public async Task SendImportCompleteAsync_sends_camelCase_importComplete_to_BallotImport_group()
    {
        var (service, _, _, ballotImportClients, _, _, _, _, groupProxies) = CreateService();
        var expectedGroup = BallotImportHub.GetGroupName(_electionGuid);
        var summary = new { ballotsCreated = 5 };

        await service.SendImportCompleteAsync(_electionGuid, summary);

        ballotImportClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "importComplete",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        proxy.Verify(
            p => p.SendCoreAsync(
                "ImportComplete",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendImportErrorAsync_sends_importError_with_message_and_row()
    {
        var (service, _, _, ballotImportClients, _, _, _, _, groupProxies) = CreateService();
        var expectedGroup = BallotImportHub.GetGroupName(_electionGuid);

        await service.SendImportErrorAsync(_electionGuid, "bad row", 12);

        ballotImportClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        var invocation = proxy!.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "importError"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Equal(2, capturedArgs.Length);
        Assert.Equal("bad row", capturedArgs[0]);
        Assert.Equal(12, capturedArgs[1]);
    }

    [Fact]
    public async Task SendPeopleImportProgressAsync_sends_object_payload_to_PeopleImport_group()
    {
        var (service, _, _, _, peopleImportClients, _, _, _, groupProxies) = CreateService();
        var expectedGroup = PeopleImportHub.GetGroupName(_electionGuid);

        await service.SendPeopleImportProgressAsync(_electionGuid, 3, 10, "Processing row 3");

        peopleImportClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "importProgress",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "importProgress"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Single(capturedArgs);
        // Anonymous object: verify via reflection / dynamic
        var payload = capturedArgs[0]!;
        var processed = payload.GetType().GetProperty("processed")!.GetValue(payload);
        var total = payload.GetType().GetProperty("total")!.GetValue(payload);
        var status = payload.GetType().GetProperty("status")!.GetValue(payload);
        Assert.Equal(3, processed);
        Assert.Equal(10, total);
        Assert.Equal("Processing row 3", status);
    }

    [Fact]
    public async Task SendElectionPackageLoaderStatusAsync_sends_loaderStatus_to_user_group()
    {
        var (service, _, _, _, _, packageClients, _, _, groupProxies) = CreateService();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var expectedGroup = ElectionPackageImportHub.GetGroupName(userId);

        await service.SendElectionPackageLoaderStatusAsync(userId, "Importing people…", isTemporary: true);

        packageClients.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        var invocation = proxy!.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "loaderStatus"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Equal(2, capturedArgs.Length);
        Assert.Equal("Importing people…", capturedArgs[0]);
        Assert.Equal(true, capturedArgs[1]);
    }
}
