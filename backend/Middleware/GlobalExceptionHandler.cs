using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Middleware;

/// <summary>
/// Global exception handler that catches unhandled exceptions and returns appropriate HTTP error responses.
/// Converts exceptions to RFC 7807 Problem Details format for consistent error handling.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IRemoteLogService _remoteLogService;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionHandler.
    /// </summary>
    /// <param name="logger">Logger for recording exception details and error information.</param>
    /// <param name="remoteLogService">Service for sending logs to remote endpoints.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IRemoteLogService remoteLogService)
    {
        _logger = logger;
        _remoteLogService = remoteLogService;
    }

    /// <summary>
    /// Attempts to handle the specified exception by logging it and returning an appropriate HTTP error response.
    /// Maps specific exception types to appropriate HTTP status codes and Problem Details.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that occurred during request processing.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the exception was handled, false otherwise.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unhandled exception occurred: {Message}",
            exception.Message
        );

        // Send to remote log asynchronously without blocking the response
        _ = Task.Run(() => _remoteLogService.SendLogAsync(
            $"Unhandled exception: {exception.Message}",
            "Error",
            exception.StackTrace
        ));

        var problemDetails = new ProblemDetails
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "An error occurred while processing your request.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        if (exception is ArgumentException or ArgumentNullException)
        {
            problemDetails.Status = (int)HttpStatusCode.BadRequest;
            problemDetails.Title = exception.Message;
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        }
        else if (exception is UnauthorizedAccessException)
        {
            problemDetails.Status = (int)HttpStatusCode.Unauthorized;
            problemDetails.Title = "Unauthorized access.";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
        }
        else if (exception is KeyNotFoundException)
        {
            problemDetails.Status = (int)HttpStatusCode.NotFound;
            problemDetails.Title = "Resource not found.";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}



