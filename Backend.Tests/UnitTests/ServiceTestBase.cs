using Mapster;
using MapsterMapper;
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

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(ElectionProfile).Assembly);

        Mapper = new Mapper(config);
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}



