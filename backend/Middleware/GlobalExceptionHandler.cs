using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TallyJ4.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

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
