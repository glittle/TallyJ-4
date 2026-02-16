using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json.Linq;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;

namespace Backend.Helpers;

/// <summary>
/// Extension methods for configuring ASP.NET Core services and middleware.
/// Provides utility methods for setting up JSON serialization, database contexts, CORS, and OpenAPI specifications.
/// </summary>
public static class ProgramExtensions
{
    /// <summary>
    /// Configures JSON serializer options for MVC builders.
    /// Sets up reference handling, trailing commas, and enum conversion.
    /// </summary>
    /// <param name="builder">The MVC builder to configure.</param>
    /// <returns>The configured MVC builder.</returns>
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

    /// <summary>
    /// Registers a singleton JsonSerializerOptions instance with configured settings.
    /// </summary>
    /// <param name="services">The service collection to add the singleton to.</param>
    /// <returns>The service collection with the singleton registered.</returns>
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

    /// <summary>
    /// Registers a DbContext with SQL Server configuration and connection string.
    /// </summary>
    /// <typeparam name="TContext">The type of DbContext to register.</typeparam>
    /// <param name="services">The service collection to add the DbContext to.</param>
    /// <param name="connectionStringName">The name of the connection string configuration.</param>
    /// <param name="connectionString">The connection string to use for the database.</param>
    /// <returns>The service collection with the DbContext registered.</returns>
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
              .MigrationsAssembly("Backend");
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
                  w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
              });
              }
          }
        );

        // add a factory too, for the rare time that we need a second context
        // Skip factory registration in Testing environment to avoid conflicts with test database providers
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Testing")
        {
            services.AddDbContextFactory<TContext>(
              options => options.UseSqlServer(connectionString),
              ServiceLifetime.Scoped
            );
        }

        return services;
    }

    /// <summary>
    /// Configures CORS policy for local development with allowed origins from configuration.
    /// </summary>
    /// <param name="services">The service collection to add CORS to.</param>
    /// <param name="config">The configuration manager containing CORS settings.</param>
    /// <returns>The service collection with CORS configured.</returns>
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

    /// <summary>
    /// Writes the OpenAPI specification to a file for documentation purposes.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="path">The file path where the OpenAPI spec should be written.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder WriteOpenApiSpecToFile(
      this IApplicationBuilder app,
      string path
    )
    {
        var scope = app.ApplicationServices.CreateScope();
        var swaggerProvider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();
        var swaggerDocument = swaggerProvider.GetSwagger("v1");

        // Generate new content in memory
        string newContent;
        using (var memoryStream = new MemoryStream())
        using (var streamWriter = new StreamWriter(memoryStream))
        {
            var writer = new OpenApiJsonWriter(streamWriter);
            swaggerDocument.SerializeAsV3(writer);
            streamWriter.Flush();
            newContent = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        // Normalize line endings to LF for consistency across platforms
        newContent = newContent.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\\r\\n", "\\n");

        // Check if file exists and compare content
        bool shouldWrite = true;
        if (File.Exists(path))
        {
            var existingContent = File.ReadAllText(path);
            try
            {
                var existingJson = JToken.Parse(existingContent);
                var newJson = JToken.Parse(newContent);
                shouldWrite = !JToken.DeepEquals(existingJson, newJson);
            }
            catch (JsonException)
            {
                // If parsing fails, fall back to string comparison or assume different
                shouldWrite = true;
            }
        }

        if (shouldWrite)
        {
            File.WriteAllText(path, newContent);
            // use Information and visually highlight to make it stand out more in the logs
            Log.Information("{Highlight} OpenAPI file generated at {OpenAPIFilePath}.", ">>>>>>", path);
            Log.Information("{Lines} Restart Frontend", "------------------------"); // to catch the developer's eye... can now start the front end!
        }
        else
        {
            Log.Information("OpenAPI file is not changed");
        }

        return app;
    }
}


