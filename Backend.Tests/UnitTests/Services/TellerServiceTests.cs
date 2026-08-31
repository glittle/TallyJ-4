using Backend.DTOs.SignalR;
using Backend.DTOs.Tellers;
using Backend.Entities;
using Backend.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

public class TellerServiceTests : ServiceTestBase
{
    private static readonly Guid ElectionGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly TellerService _service;

    public TellerServiceTests()
    {
        _signalRMock = new Mock<ISignalRNotificationService>();
        _service = new TellerService(
            Context,
            Mock.Of<ILogger<TellerService>>(),
            _signalRMock.Object);
        SeedElection();
    }

    [Fact]
    public async Task CreateTellerAsync_persists_name_and_notifies_added()
    {
        var created = await _service.CreateTellerAsync(new CreateTellerDto
        {
            ElectionGuid = ElectionGuid,
            Name = "Pat"
        });

        Assert.Equal("Pat", created.Name);
        Assert.Equal(ElectionGuid, created.ElectionGuid);
        Assert.True(created.RowId > 0);

        var listed = await _service.GetTellersByElectionAsync(ElectionGuid);
        Assert.Single(listed.Items);
        Assert.Equal("Pat", listed.Items[0].Name);

        _signalRMock.Verify(
            s => s.SendTellerUpdateAsync(It.Is<TellerUpdateDto>(u =>
                u.ElectionGuid == ElectionGuid
                && u.RowId == created.RowId
                && u.Name == "Pat"
                && u.Action == "added")),
            Times.Once);
    }

    [Fact]
    public async Task GetTellersByElectionAsync_returns_names_alphabetically()
    {
        await _service.CreateTellerAsync(new CreateTellerDto { ElectionGuid = ElectionGuid, Name = "Zoe" });
        await _service.CreateTellerAsync(new CreateTellerDto { ElectionGuid = ElectionGuid, Name = "Ann" });
        await _service.CreateTellerAsync(new CreateTellerDto { ElectionGuid = ElectionGuid, Name = "Mia" });

        var listed = await _service.GetTellersByElectionAsync(ElectionGuid);

        Assert.Equal(new[] { "Ann", "Mia", "Zoe" }, listed.Items.Select(t => t.Name));
    }

    [Fact]
    public async Task CreateTellerAsync_does_not_remove_existing_name()
    {
        var first = await _service.CreateTellerAsync(new CreateTellerDto
        {
            ElectionGuid = ElectionGuid,
            Name = "Pat"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateTellerAsync(new CreateTellerDto
            {
                ElectionGuid = ElectionGuid,
                Name = "Pat"
            }));

        var stillThere = await _service.GetTellerByIdAsync(first.RowId);
        Assert.NotNull(stillThere);
        Assert.Equal("Pat", stillThere!.Name);
    }

    [Fact]
    public async Task DeleteTellerAsync_removes_name_and_notifies_deleted()
    {
        var created = await _service.CreateTellerAsync(new CreateTellerDto
        {
            ElectionGuid = ElectionGuid,
            Name = "Sam"
        });
        _signalRMock.Invocations.Clear();

        var deleted = await _service.DeleteTellerAsync(created.RowId);

        Assert.True(deleted);
        Assert.Null(await _service.GetTellerByIdAsync(created.RowId));

        _signalRMock.Verify(
            s => s.SendTellerUpdateAsync(It.Is<TellerUpdateDto>(u =>
                u.ElectionGuid == ElectionGuid
                && u.RowId == created.RowId
                && u.Name == "Sam"
                && u.Action == "deleted")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTellerAsync_notifies_updated()
    {
        var created = await _service.CreateTellerAsync(new CreateTellerDto
        {
            ElectionGuid = ElectionGuid,
            Name = "Old"
        });
        _signalRMock.Invocations.Clear();

        var updated = await _service.UpdateTellerAsync(created.RowId, new UpdateTellerDto { Name = "New" });

        Assert.NotNull(updated);
        Assert.Equal("New", updated!.Name);
        _signalRMock.Verify(
            s => s.SendTellerUpdateAsync(It.Is<TellerUpdateDto>(u =>
                u.RowId == created.RowId && u.Name == "New" && u.Action == "updated")),
            Times.Once);
    }

    private void SeedElection()
    {
        Context.Elections.Add(new Election
        {
            RowId = 1,
            ElectionGuid = ElectionGuid,
            Name = "Test Election",
            NumberToElect = 3,
            ElectionType = "Loc",
            RowVersion = new byte[8]
        });
        Context.SaveChanges();
    }
}
