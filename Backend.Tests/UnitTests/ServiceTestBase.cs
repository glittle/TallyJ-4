using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Backend.Domain.Context;
using Backend.Mappings;

namespace Backend.Tests.UnitTests;

public abstract class ServiceTestBase : IDisposable
{
    protected readonly MainDbContext Context;
    protected readonly IMapper Mapper;

    protected ServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new MainDbContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ElectionProfile>();
            cfg.AddProfile<PersonProfile>();
            cfg.AddProfile<BallotProfile>();
            cfg.AddProfile<VoteProfile>();
            cfg.AddProfile<SecurityAuditLogProfile>();
        });

        Mapper = config.CreateMapper();
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}



