using Backend.Services;
using Xunit;

namespace Backend.Tests.UnitTests.Services;

public class OnlineVoterPresenceServiceTests
{
    private readonly Guid _electionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _electionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void CountSessions_is_zero_when_empty()
    {
        var service = new OnlineVoterPresenceService();

        Assert.Equal(0, service.CountSessions(_electionA));
        Assert.Equal(0, service.CountSessions(Guid.Empty));
    }

    [Fact]
    public void AddSession_counts_per_election_and_isolates_elections()
    {
        var service = new OnlineVoterPresenceService();

        service.AddSession(_electionA, "conn-1");
        service.AddSession(_electionA, "conn-2");
        service.AddSession(_electionB, "conn-3");

        Assert.Equal(2, service.CountSessions(_electionA));
        Assert.Equal(1, service.CountSessions(_electionB));
    }

    [Fact]
    public void AddSession_moves_connection_to_the_new_election()
    {
        var service = new OnlineVoterPresenceService();

        service.AddSession(_electionA, "conn-1");
        service.AddSession(_electionB, "conn-1");

        Assert.Equal(0, service.CountSessions(_electionA));
        Assert.Equal(1, service.CountSessions(_electionB));
    }

    [Fact]
    public void RemoveSession_drops_only_that_connection()
    {
        var service = new OnlineVoterPresenceService();
        service.AddSession(_electionA, "conn-1");
        service.AddSession(_electionA, "conn-2");

        service.RemoveSession("conn-1");
        service.RemoveSession("missing");
        service.RemoveSession(" ");

        Assert.Equal(1, service.CountSessions(_electionA));
    }

    [Fact]
    public void AddSession_rejects_empty_election_or_connection()
    {
        var service = new OnlineVoterPresenceService();

        Assert.Throws<ArgumentException>(() => service.AddSession(Guid.Empty, "conn-1"));
        Assert.Throws<ArgumentException>(() => service.AddSession(_electionA, " "));
    }

    [Fact]
    public void Interface_does_not_accept_voter_identity()
    {
        var names = typeof(IOnlineVoterPresenceService).GetMethods()
            .SelectMany(m => m.GetParameters().Select(p => p.Name))
            .ToList();

        Assert.DoesNotContain("voterId", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("personGuid", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", names, StringComparer.OrdinalIgnoreCase);
    }
}
