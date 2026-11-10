using System.Security.Claims;
using Backend.Authorization;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Identity;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Co-admin add and remove: who may change the list, and the #414 rule that
/// one approved owner or co-admin keeps the election unrestricted.
/// </summary>
public class ElectionAdminServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Add_WhenCallerIsNotAnOwnerOrAdmin_IsForbidden()
    {
        var electionId = await SeedElectionAsync();
        var tellerId = await SeedUserAsync("teller@example.com", "Teller Person");
        await SeedJoinAsync(electionId, tellerId, "Teller");
        var strangerId = Guid.NewGuid();
        var service = CreateService();

        var asTeller = await service.AddAsync(electionId, "other@example.com", Principal(tellerId));
        var asStranger = await service.AddAsync(electionId, "other@example.com", Principal(strangerId));

        Assert.True(asTeller.IsForbidden);
        Assert.True(asStranger.IsForbidden);
        Assert.Empty(Context.JoinElectionUsers.Where(link =>
            link.ElectionGuid == electionId && link.Role == ElectionAdminService.AdminRole));
    }

    [Fact]
    public async Task Add_UnknownEmail_DoesNotCreateAJoin()
    {
        var electionId = await SeedElectionAsync();
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.AdminRole);
        var service = CreateService();

        var result = await service.AddAsync(
            electionId,
            "nobody@example.com",
            Principal(ownerId));

        Assert.True(result.IsNoAccount);
        Assert.Equal(1, await Context.JoinElectionUsers.CountAsync(link => link.ElectionGuid == electionId));
    }

    [Fact]
    public async Task Add_DuplicateEmail_DoesNotCreateASecondRow()
    {
        var electionId = await SeedElectionAsync();
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        var coAdminId = await SeedUserAsync("co-admin@example.com", "Co Admin");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.AdminRole);
        var service = CreateService();

        var first = await service.AddAsync(electionId, " Co-Admin@Example.com ", Principal(ownerId));
        var second = await service.AddAsync(electionId, "co-admin@example.com", Principal(ownerId));

        Assert.False(first.AlreadyAdmin);
        Assert.Equal(coAdminId, first.Admin!.UserId);
        Assert.True(second.AlreadyAdmin);
        Assert.Equal(coAdminId, second.Admin!.UserId);
        Assert.Equal(
            1,
            await Context.JoinElectionUsers.CountAsync(link =>
                link.ElectionGuid == electionId && link.UserId == coAdminId));
    }

    [Fact]
    public async Task Add_RestrictedCoAdmin_DoesNotRestrictAnApprovedOwner()
    {
        var electionId = await SeedElectionAsync(useOnlineVoting: true);
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        var coAdminId = await SeedUserAsync("co-admin@example.com", "Co Admin");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.AdminRole);
        await SeedControlAsync(ownerId, OwnerRestrictionStatus.Approved);
        await SeedControlAsync(coAdminId, OwnerRestrictionStatus.Restricted);
        var service = CreateService();

        var result = await service.AddAsync(electionId, "co-admin@example.com", Principal(ownerId));

        Assert.False(result.AlreadyAdmin);
        Assert.False(await IsRestrictedAsync(electionId));
        Assert.True(await OnlineVotingStillOnAsync(electionId));
    }

    [Fact]
    public async Task Remove_OwnerRole_IsRejected()
    {
        var electionId = await SeedElectionAsync();
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        var coAdminId = await SeedUserAsync("co-admin@example.com", "Co Admin");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.OwnerRole);
        await SeedJoinAsync(electionId, coAdminId, ElectionAdminService.AdminRole);
        var service = CreateService();

        var result = await service.RemoveAsync(electionId, ownerId, Principal(coAdminId));

        Assert.True(result.IsOwner);
        Assert.True(await Context.JoinElectionUsers.AnyAsync(link =>
            link.ElectionGuid == electionId
            && link.UserId == ownerId
            && link.Role == ElectionAdminService.OwnerRole));
    }

    [Fact]
    public async Task Remove_LastAdmin_IsRejected()
    {
        var electionId = await SeedElectionAsync();
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.AdminRole);
        var service = CreateService();

        var result = await service.RemoveAsync(electionId, ownerId, Principal(ownerId));

        Assert.True(result.IsLastOwner);
        Assert.Equal(1, await Context.JoinElectionUsers.CountAsync(link => link.ElectionGuid == electionId));
    }

    [Fact]
    public async Task Remove_OnlyApprovedCoAdmin_RestrictsTheElectionAgain()
    {
        var electionId = await SeedElectionAsync(useOnlineVoting: true);
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        var coAdminId = await SeedUserAsync("co-admin@example.com", "Co Admin");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.OwnerRole);
        await SeedJoinAsync(electionId, coAdminId, ElectionAdminService.AdminRole);
        await SeedControlAsync(ownerId, OwnerRestrictionStatus.Restricted);
        await SeedControlAsync(coAdminId, OwnerRestrictionStatus.Approved);
        var service = CreateService();

        Assert.False(await IsRestrictedAsync(electionId));

        var result = await service.RemoveAsync(electionId, coAdminId, Principal(ownerId));

        Assert.False(result.IsForbidden);
        Assert.False(result.IsOwner);
        Assert.False(result.IsLastOwner);
        Assert.True(await IsRestrictedAsync(electionId));
        Assert.True(await OnlineVotingStillOnAsync(electionId));
        Assert.False(await Context.ElectionSendControls.AnyAsync(row =>
            row.ElectionGuid == electionId && row.SendsFrozen));
    }

    [Fact]
    public async Task Remove_RestrictedCoAdmin_LeavesAnApprovedOwnerUnrestricted()
    {
        var electionId = await SeedElectionAsync();
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        var coAdminId = await SeedUserAsync("co-admin@example.com", "Co Admin");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.AdminRole);
        await SeedJoinAsync(electionId, coAdminId, ElectionAdminService.AdminRole);
        await SeedControlAsync(ownerId, OwnerRestrictionStatus.Approved);
        await SeedControlAsync(coAdminId, OwnerRestrictionStatus.Restricted);
        var service = CreateService();

        var result = await service.RemoveAsync(electionId, coAdminId, Principal(ownerId));

        Assert.False(result.IsLastOwner);
        Assert.False(await IsRestrictedAsync(electionId));
        var listed = await service.ListAsync(electionId, Principal(ownerId));
        Assert.Single(listed.Admins);
        Assert.False(listed.Admins[0].CanRemove);
    }

    [Fact]
    public async Task Add_SuperAdminWithoutAJoin_IsAllowed()
    {
        var electionId = await SeedElectionAsync();
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        var coAdminId = await SeedUserAsync("co-admin@example.com", "Co Admin");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.AdminRole);
        var service = CreateService(superAdmins: ["root@example.com"]);

        var result = await service.AddAsync(
            electionId,
            "co-admin@example.com",
            Principal(Guid.NewGuid(), "root@example.com"));

        Assert.False(result.IsForbidden);
        Assert.Equal(coAdminId, result.Admin!.UserId);
    }

    [Fact]
    public async Task Add_IdentityAdminWithoutAJoin_IsAllowed()
    {
        var electionId = await SeedElectionAsync();
        var ownerId = await SeedUserAsync("owner@example.com", "Owner");
        var siteAdminId = Guid.NewGuid();
        var coAdminId = await SeedUserAsync("co-admin@example.com", "Co Admin");
        await SeedJoinAsync(electionId, ownerId, ElectionAdminService.AdminRole);
        var service = CreateService(identityAdmin: true);

        var result = await service.AddAsync(electionId, "co-admin@example.com", Principal(siteAdminId));

        Assert.False(result.IsForbidden);
        Assert.Equal(coAdminId, result.Admin!.UserId);
    }

    private ElectionAdminService CreateService(bool identityAdmin = false, string[]? superAdmins = null)
    {
        var store = new Mock<IUserStore<AppUser>>();
        var users = new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(manager => manager.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => new AppUser { Id = id });
        users.Setup(manager => manager.IsInRoleAsync(It.IsAny<AppUser>(), "Admin"))
            .ReturnsAsync(identityAdmin);
        var access = new ElectionAccessEvaluator(
            Context,
            users.Object,
            Options.Create(new SuperAdminSettings { Emails = superAdmins ?? [] }));
        return new ElectionAdminService(Context, access, NullLogger<ElectionAdminService>.Instance);
    }

    private async Task<bool> IsRestrictedAsync(Guid electionId)
    {
        var owners = await OwnerRestrictionQueries.LoadOwnersAsync(Context, electionId);
        return OwnerRestrictionQueries.ElectionIsOwnerRestricted(owners);
    }

    private async Task<bool> OnlineVotingStillOnAsync(Guid electionId)
    {
        var election = await Context.Elections.AsNoTracking()
            .SingleAsync(item => item.ElectionGuid == electionId);
        return election.UseOnlineVoting == true;
    }

    private async Task<Guid> SeedElectionAsync(bool useOnlineVoting = false)
    {
        var id = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = id,
            Name = "Admin test",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.SettingUp,
            UseOnlineVoting = useOnlineVoting,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedUserAsync(string email, string displayName)
    {
        var id = Guid.NewGuid();
        Context.Users.Add(new AppUser
        {
            Id = id.ToString(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            EmailConfirmed = true,
            AuthMethod = "Local",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        await Context.SaveChangesAsync();
        return id;
    }

    private async Task SeedJoinAsync(Guid electionId, Guid userId, string role)
    {
        Context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = electionId,
            UserId = userId,
            Role = role
        });
        await Context.SaveChangesAsync();
    }

    private async Task SeedControlAsync(Guid userId, string status)
    {
        Context.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = userId,
            RestrictionStatus = status,
            PaidSendsApproved = status == OwnerRestrictionStatus.Approved
        });
        await Context.SaveChangesAsync();
    }

    private static ClaimsPrincipal Principal(Guid userId, string? email = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString())
        };
        if (email != null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim("email", email));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
