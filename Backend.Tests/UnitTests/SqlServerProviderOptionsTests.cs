using Backend.Context;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Backend.Tests.UnitTests;

public class SqlServerProviderOptionsTests
{
    [Fact]
    public void ConfigureSqlServerProvider_EnablesRetryAndSingleQuerySplitting()
    {
        var builder = new DbContextOptionsBuilder<MainDbContext>();
        builder.UseSqlServer(
            "Server=127.0.0.1;Database=unused;TrustServerCertificate=True",
            ProgramExtensions.ConfigureSqlServerProvider);

        using var context = new MainDbContext(builder.Options);
        Assert.True(context.Database.CreateExecutionStrategy().RetriesOnFailure);

        var relational = builder.Options.Extensions.OfType<RelationalOptionsExtension>().Single();
        Assert.Equal(QuerySplittingBehavior.SingleQuery, relational.QuerySplittingBehavior);
    }
}
