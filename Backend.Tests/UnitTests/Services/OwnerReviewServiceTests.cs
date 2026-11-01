using Backend.Authorization;
using Backend.Context;
using Backend.DTOs.Security;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Identity;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Per-owner review: request throttle, approve, reject, and the 30-day delete.
/// </summary>
public class OwnerReviewServiceTests : ServiceTestBase
{
    private readonly MutableClock _clock = new();
    private readonly CapturingEmailSender _email = new();
    private readonly Mock<ISecurityAuditService> _audit = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _electionId = Guid.NewGuid();

    public OwnerReviewServiceTests()
    {
        _audit
            .Setup(audit => audit.LogSecurityEventAsync(It.IsAny<CreateSecurityAuditLogDto>()))
            .Returns(Task.CompletedTask);

        Context.Users.Add(new AppUser
        {
            Id = _ownerId.ToString(),
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            DisplayName = "Ada Owner",
            AuthMethod = "Local",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        Context.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = _ownerId,
            RestrictionStatus = OwnerRestrictionStatus.Restricted,
            PaidSendsApproved = false
        });
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Review election",
            ElectionType = "LSA",
            DateOfElection = DateTimeOffset.Parse("2026-04-21T00:00:00Z"),
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.Parse("2026-04-20T00:00:00Z"),
            ListedForPublicAsOf = DateTimeOffset.Parse("2026-04-01T00:00:00Z"),
            ElectionStage = ElectionStage.SettingUp,
            RowVersion = new byte[8]
        });
        Context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = _electionId,
            UserId = _ownerId,
            Role = "Admin"
        });
        Context.People.Add(new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = _electionId,
            FirstName = "Ada",
            LastName = "Voter",
            Email = "ada@congregation.example",
            Phone = "+14165550100",
            RowVersion = new byte[8]
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task RequestReview_EmailsSuperAdmins_AndThrottlesForADay()
    {
        var service = CreateService();

        var first = await service.RequestReviewAsync(_ownerId.ToString());
        Assert.True(first.Success);
        Assert.Equal(OwnerRestrictionStatus.ReviewRequested, first.Restriction!.Status);
        Assert.Single(_email.Messages);
        Assert.StartsWith(OwnerRestrictionStatus.ReviewRequestSubjectPrefix, _email.Messages[0].Subject, StringComparison.Ordinal);
        Assert.Contains("owner@example.com", _email.Messages[0].Subject, StringComparison.Ordinal);
        var body = await ReadBodyAsync(_email.Messages[0]);
        Assert.Contains("Ada Owner", body, StringComparison.Ordinal);
        Assert.Contains("congregation.example 1", body, StringComparison.Ordinal);
        Assert.Contains("/super-admin/owner-reviews/" + _ownerId, body, StringComparison.Ordinal);

        _clock.UtcNow = _clock.UtcNow.AddHours(2);
        var throttled = await service.RequestReviewAsync(_ownerId.ToString());
        Assert.False(throttled.Success);
        Assert.Equal(OwnerReviewService.ThrottledKey, throttled.ErrorKey);
        Assert.Single(_email.Messages);

        _clock.UtcNow = _clock.UtcNow.AddHours(23);
        var again = await service.RequestReviewAsync(_ownerId.ToString());
        Assert.True(again.Success);
        Assert.Equal(2, _email.Messages.Count);
    }

    [Fact]
    public async Task Approve_TrustsPaidSends_Reject_FreezesAndHides_DeleteWaits30Days()
    {
        var service = CreateService();

        var approved = await service.ApproveAsync(_ownerId, "admin-1");
        Assert.True(approved.Success);
        var row = await Context.OwnerSendControls.SingleAsync();
        Assert.Equal(OwnerRestrictionStatus.Approved, row.RestrictionStatus);
        Assert.True(row.PaidSendsApproved);
        Assert.Contains(_email.Messages, message => message.Subject == "[TallyJ] Your account is approved");

        var rejected = await service.RejectAsync(_ownerId, "admin-1", "Not a real community");
        Assert.True(rejected.Success);
        row = await Context.OwnerSendControls.SingleAsync();
        Assert.Equal(OwnerRestrictionStatus.Rejected, row.RestrictionStatus);
        Assert.False(row.PaidSendsApproved);
        Assert.False(row.SendsFrozen);
        var election = await Context.Elections.SingleAsync();
        Assert.False(election.UseOnlineVoting);
        Assert.Null(election.OnlineWhenOpen);
        Assert.Null(election.ListedForPublicAsOf);
        Assert.True((await Context.ElectionSendControls.SingleAsync()).SendsFrozen);

        _clock.UtcNow = row.RejectedAt!.Value.AddDays(29);
        Assert.Equal(0, await service.DeleteExpiredRejectedElectionsAsync());
        Assert.NotNull(await Context.Elections.SingleOrDefaultAsync());

        _clock.UtcNow = row.RejectedAt.Value.AddDays(30);
        Assert.Equal(1, await service.DeleteExpiredRejectedElectionsAsync());
        Assert.Null(await Context.Elections.SingleOrDefaultAsync());
    }

    [Fact]
    public async Task Hold_StoresTheNote_AndLeavesTheAccountRestricted()
    {
        var service = CreateService();
        await service.RequestReviewAsync(_ownerId.ToString());

        var held = await service.HoldAsync(_ownerId, "admin-1", "Please use a community address");
        Assert.True(held.Success);
        Assert.Equal(OwnerRestrictionStatus.Restricted, held.Restriction!.Status);
        Assert.Equal("Please use a community address", held.Restriction.Note);
        Assert.False(held.Restriction.CanRequestReview);
        var body = await ReadBodyAsync(_email.Messages[^1]);
        Assert.Contains("Please use a community address", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteNow_RemovesRejectedElections_AndKeepsOneSharedWithAnApprovedOwner()
    {
        var coOwner = Guid.NewGuid();
        var shared = Guid.NewGuid();
        Context.Users.Add(new AppUser
        {
            Id = coOwner.ToString(),
            UserName = "co@example.com",
            NormalizedUserName = "CO@EXAMPLE.COM",
            Email = "co@example.com",
            NormalizedEmail = "CO@EXAMPLE.COM",
            EmailConfirmed = true,
            AuthMethod = "Local",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        Context.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = coOwner,
            RestrictionStatus = OwnerRestrictionStatus.Approved,
            PaidSendsApproved = true
        });
        Context.Elections.Add(new Election
        {
            ElectionGuid = shared,
            Name = "Shared",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.Parse("2026-04-20T00:00:00Z"),
            ElectionStage = ElectionStage.SettingUp,
            RowVersion = new byte[8]
        });
        Context.JoinElectionUsers.AddRange(
            new JoinElectionUser { ElectionGuid = shared, UserId = _ownerId, Role = "Admin" },
            new JoinElectionUser { ElectionGuid = shared, UserId = coOwner, Role = "Owner" });
        await Context.SaveChangesAsync();

        var service = CreateService();
        await service.RejectAsync(_ownerId, "admin-1", null);
        var sharedElection = await Context.Elections.SingleAsync(election => election.ElectionGuid == shared);
        Assert.True(sharedElection.UseOnlineVoting);
        Assert.False(await Context.ElectionSendControls.AnyAsync(row =>
            row.ElectionGuid == shared && row.SendsFrozen));
        Assert.True(await service.DeleteElectionsNowAsync(_ownerId, "admin-1") is { Success: true });

        var remaining = await Context.Elections.Select(election => election.ElectionGuid).ToListAsync();
        Assert.DoesNotContain(_electionId, remaining);
        Assert.Contains(shared, remaining);
    }

    private OwnerReviewService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientEnv:frontendUrl"] = "https://localhost:8095",
                ["Email:FromAddress"] = "noreply@tallyj.com",
                ["Email:FromName"] = "TallyJ4"
            })
            .Build();

        return new OwnerReviewService(
            Context,
            _email,
            _audit.Object,
            _clock,
            configuration,
            new DevelopmentEnvironment(),
            Options.Create(new SuperAdminSettings { Emails = ["admin@tallyj.test"] }),
            NullLogger<OwnerReviewService>.Instance);
    }

    private static async Task<string> ReadBodyAsync(MimeMessage message)
    {
        using var reader = new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(message.TextBody ?? "")));
        return message.TextBody ?? "";
    }

    private sealed class MutableClock : ICodeSendClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-11-01T12:00:00Z");

        public DateOnly UtcDate => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<MimeMessage> Messages { get; } = new();

        public Task SendAsync(MimeMessage message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class DevelopmentEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Backend.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
