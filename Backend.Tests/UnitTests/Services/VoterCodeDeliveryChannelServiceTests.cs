using System.Text.Json;
using Backend.DTOs.SignalR;
using Backend.Services;

namespace Backend.Tests.UnitTests.Services;

public class VoterCodeDeliveryChannelServiceTests
{
    [Fact]
    public void Issue_returns_high_entropy_token_and_does_not_store_raw_value()
    {
        var service = new VoterCodeDeliveryChannelService();

        var issued = service.Issue();

        Assert.Equal(VoterCodeDeliveryChannelService.TokenBytes * 2, issued.RawToken.Length);
        Assert.False(string.IsNullOrWhiteSpace(issued.ChannelId));
        Assert.NotEqual(issued.RawToken, issued.ChannelId);
        Assert.NotEqual(issued.RawToken, VoterCodeDeliveryChannelService.HashToken(issued.RawToken));
    }

    [Fact]
    public void TryJoin_accepts_valid_token_and_rejects_unknown()
    {
        var service = new VoterCodeDeliveryChannelService();
        var issued = service.Issue();

        Assert.True(service.TryJoin(issued.RawToken, "conn-1", out var join));
        Assert.Equal(issued.ChannelId, join.ChannelId);
        Assert.False(service.TryJoin("deadbeef", "conn-2", out _));
        Assert.False(service.TryJoin("", "conn-2", out _));
    }

    [Fact]
    public void TryJoin_rejects_second_live_connection_then_allows_after_leave()
    {
        var service = new VoterCodeDeliveryChannelService();
        var issued = service.Issue();

        Assert.True(service.TryJoin(issued.RawToken, "conn-1", out _));
        Assert.False(service.TryJoin(issued.RawToken, "conn-2", out _));

        service.Leave("conn-1");

        Assert.True(service.TryJoin(issued.RawToken, "conn-2", out var rejoin));
        Assert.Equal(issued.ChannelId, rejoin.ChannelId);
    }

    [Fact]
    public void TryJoin_rejects_expired_token()
    {
        var clock = new ShiftTimeProvider(DateTimeOffset.Parse("2026-09-20T12:00:00Z"));
        var service = new VoterCodeDeliveryChannelService(clock);
        var issued = service.Issue();

        clock.Advance(VoterCodeDeliveryChannelService.Lifetime.Add(TimeSpan.FromSeconds(1)));

        Assert.False(service.TryJoin(issued.RawToken, "conn-1", out _));
    }

    [Fact]
    public void Recorded_status_replay_never_includes_otp_or_contact_fields()
    {
        var service = new VoterCodeDeliveryChannelService();
        var issued = service.Issue();
        service.RecordStatus(issued.ChannelId, VoterCodeDeliveryStatusDto.Sent("ABC123OTP"));

        Assert.True(service.TryJoin(issued.RawToken, "conn-1", out var join));
        var status = Assert.Single(join.BufferedStatuses);
        var names = status.GetType().GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("VerifyCode", names);
        Assert.DoesNotContain("Code", names);
        Assert.DoesNotContain("Otp", names);
        Assert.DoesNotContain("VoterId", names);
        Assert.DoesNotContain("Phone", names);
        Assert.DoesNotContain("Email", names);
        Assert.Equal("other", status.ProviderStatus);

        var json = JsonSerializer.Serialize(status);
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            Assert.DoesNotContain("code", prop.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("otp", prop.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("voter", prop.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("phone", prop.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("email", prop.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BindProviderSid_round_trips_until_expiry()
    {
        var service = new VoterCodeDeliveryChannelService();
        var issued = service.Issue();

        service.BindProviderSid(issued.ChannelId, "SM123");

        Assert.True(service.TryGetChannelIdByProviderSid("SM123", out var channelId));
        Assert.Equal(issued.ChannelId, channelId);
        Assert.False(service.TryGetChannelIdByProviderSid("SM999", out _));
    }

    private sealed class ShiftTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ShiftTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }
}
