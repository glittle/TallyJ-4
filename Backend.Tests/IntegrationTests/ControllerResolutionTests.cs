using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Builds the application service collection and activates every controller.
/// A missing registration (such as IReportExportService) fails here.
/// </summary>
public class ControllerResolutionTests
{
    [Fact]
    public void EveryController_ResolvesFromTheApplicationServiceCollection()
    {
        using var factory = new CustomWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var controllers = typeof(Program).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(controllers);

        var failures = new List<string>();
        foreach (var controller in controllers)
        {
            try
            {
                ActivatorUtilities.CreateInstance(provider, controller);
            }
            catch (Exception ex)
            {
                failures.Add($"{controller.FullName}: {ex.GetBaseException().Message}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
