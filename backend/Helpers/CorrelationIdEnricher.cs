using Serilog.Core;
using Serilog.Events;

namespace TallyJ4.Backend.Helpers;

/// <summary>
/// Serilog enricher that adds correlation ID from the current HTTP context to all log events
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the CorrelationIdEnricher.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor for retrieving correlation IDs.</param>
    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Enriches the log event with a correlation ID from the current HTTP context.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The property factory for creating log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var correlationId = httpContext?.TryGetCorrelationId();

        if (!string.IsNullOrEmpty(correlationId))
        {
            var property = propertyFactory.CreateProperty("CorrelationId", correlationId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}