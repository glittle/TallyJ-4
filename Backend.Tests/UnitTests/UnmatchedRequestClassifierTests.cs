using Backend.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.UnitTests;

public class UnmatchedRequestClassifierTests
{
    [Fact]
    public void Unhandled404_IsUnmatched()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Items[UnmatchedRequestClassifier.RequestUnhandledItemKey] = true;

        Assert.True(UnmatchedRequestClassifier.IsUnmatched(context));
    }

    [Fact]
    public void MatchedEndpoint404_IsNotUnmatched()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.SetEndpoint(Endpoint("ElectionsController.GetElection"));

        Assert.False(UnmatchedRequestClassifier.IsUnmatched(context));
    }

    [Fact]
    public void SpaFallback404_IsNotUnmatched()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.SetEndpoint(Endpoint("Fallback {*path}"));

        Assert.False(UnmatchedRequestClassifier.IsUnmatched(context));
    }

    [Fact]
    public void Framework405_IsUnmatched()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.SetEndpoint(Endpoint(UnmatchedRequestClassifier.MethodNotAllowedDisplayName));

        Assert.True(UnmatchedRequestClassifier.IsUnmatched(context));
    }

    [Fact]
    public void Action405_IsNotUnmatched()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.SetEndpoint(Endpoint("PublicController.HealthCheck"));

        Assert.False(UnmatchedRequestClassifier.IsUnmatched(context));
    }

    [Fact]
    public void Exception404_IsNotUnmatched()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Items[UnmatchedRequestClassifier.RequestUnhandledItemKey] = true;
        context.Features.Set<IExceptionHandlerFeature>(
            new StubExceptionFeature(new KeyNotFoundException("missing")));

        Assert.False(UnmatchedRequestClassifier.IsUnmatched(context));
    }

    private static Endpoint Endpoint(string displayName) =>
        new(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, displayName);

    private sealed class StubExceptionFeature(Exception error) : IExceptionHandlerFeature
    {
        public Exception Error { get; } = error;
    }
}
