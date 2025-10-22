using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace TallyJ4.Backend.Helpers;

public static class ProgramExtensions
{
  public static IMvcBuilder AddJsonSerializerOptions(this IMvcBuilder builder)
  {
    builder.AddJsonOptions(
      (opts) =>
      {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.AllowTrailingCommas = true;
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        //   opts.JsonSerializerOptions.Converters.Add(new DateOnlyConverter());
        //   opts.JsonSerializerOptions.Converters.Add(new NullableGuidConverter());
        //   opts.JsonSerializerOptions.Converters.Add(new NullableDateTimeOffsetConverter());
      }
    );
    return builder;
  }

  public static IServiceCollection AddJsonSerializerOptionsSingleton(
    this IServiceCollection services
  )
  {
    services.AddSingleton(
      (s) =>
      {
        JsonSerializerOptions jsonSerializerOptions =
          new(JsonSerializerDefaults.Web) { AllowTrailingCommas = true };
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        //   jsonSerializerOptions.Converters.Add(new DateOnlyConverter());
        //   jsonSerializerOptions.Converters.Add(new NullableGuidConverter());
        //   jsonSerializerOptions.Converters.Add(new NullableDateTimeOffsetConverter());
        return jsonSerializerOptions;
      }
    );
    return services;
  }

  public static IServiceCollection AddDbContext<TContext>(
    this IServiceCollection services,
    string connectionStringName,
    string connectionString
  )
    where TContext : DbContext
  {
    services.AddDbContext<TContext>(
      (serviceProvider, optionsBuilder) =>
      {
        // Gets the correct connection string from the current appsettings.json (based on ASPNETCORE_ENVIRONMENT)
        optionsBuilder.UseSqlServer(
          connectionString,
          sqlServerDbContextOptionsBuilder =>
          {
            // --> We do not use this default, as most queries are better not using this
            // --> If testing shows that splitting a query would help, apply it in that LINQ statement.
            //sqlServerDbContextOptionsBuilder.UseQuerySplittingBehavior(
            //  QuerySplittingBehavior.SplitQuery
            //);
            sqlServerDbContextOptionsBuilder
              .MigrationsHistoryTable("__EFMigrations_" + connectionStringName)
              .MigrationsAssembly("Compliance");
          }
        );

        //     optionsBuilder.AddInterceptors(
        //   serviceProvider.GetRequiredService<TimeStampedEntitiesInterceptor>()
        // );

        // configure warnings
        optionsBuilder.ConfigureWarnings(w =>
        {
          // for production, we want to log warnings. See below for development.
          w.Default(WarningBehavior.Log);
        });

        // configure for development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
          optionsBuilder.EnableDetailedErrors();
          optionsBuilder.EnableSensitiveDataLogging();
          optionsBuilder.ConfigureWarnings(w =>
          {
            // try this for now...
            w.Default(WarningBehavior.Throw);

            w.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning);
          });
        }
      }
    );

    // add a factory too, for the rare time that we need a second context
    services.AddDbContextFactory<TContext>(
      options => options.UseSqlServer(connectionString),
      ServiceLifetime.Scoped
    );

    return services;
  }

  public static IServiceCollection AddLocalCors(
    this IServiceCollection services,
    ConfigurationManager config
  )
  {
    services.AddCors(options =>
    {
      options.AddDefaultPolicy(policyBuilder =>
      {
        var origins = config.GetSection("LocalSettings:AllowedOrigins").Get<string[]>();

        if (origins == null || origins.Length == 0)
        {
          throw new Exception("AllowedOrigins is not configured properly.");
        }

        Log.Information("CORS Allowed Origins: {origins}", string.Join(", ", origins));

        policyBuilder.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
      });
    });
    return services;
  }
}