using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.EF.Context;
using TallyJ4.Mappings;

namespace TallyJ4.Tests.UnitTests;

public abstract class ServiceTestBase : IDisposable
{
    protected readonly MainDbContext Context;
    protected readonly IMapper Mapper;

    protected ServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new MainDbContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ElectionProfile>();
            cfg.AddProfile<PersonProfile>();
            cfg.AddProfile<BallotProfile>();
            cfg.AddProfile<VoteProfile>();
        });

        Mapper = config.CreateMapper();
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
