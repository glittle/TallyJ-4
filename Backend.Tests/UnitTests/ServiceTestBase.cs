using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        Context = new TestMainDbContext(options);

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

internal class TestMainDbContext : MainDbContext
{
    public TestMainDbContext(DbContextOptions<MainDbContext> options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsConcurrencyToken &&
                    property.Metadata.ClrType == typeof(byte[]) &&
                    property.CurrentValue == null)
                {
                    property.CurrentValue = new byte[8];
                }
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}



