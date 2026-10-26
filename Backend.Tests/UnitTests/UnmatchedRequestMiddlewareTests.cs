using Backend.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.UnitTests;

public class UnmatchedRequestMiddlewareTests
{
    [Fact]
    public async Task Invoke_CountsUnhandled404_AndDoesNotCountMatchedNotFound()
    {
        var tracker = new UnmatchedRequestTracker();
        var middleware = new UnmatchedRequestMiddleware(next: async context =>
        {
            var path = context.Request.Path.Value ?? "";
            if (path.Contains("missing-election", StringComparison.Ordinal))
            {
                context.SetEndpoint(new Endpoint(
                    _ => Task.CompletedTask,
                    EndpointMetadataCollection.Empty,
                    "ElectionsController.GetElection"));
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Items[UnmatchedRequestClassifier.RequestUnhandledItemKey] = true;
            await Task.CompletedTask;
        }, tracker);

        var probe = Context("/api/account/billing", "?x=1");
        var repeat = Context("/API/Account/Billing", "?y=2");
        var missingElection = Context("/api/elections/missing-election/election", "");

        await middleware.InvokeAsync(probe);
        await middleware.InvokeAsync(repeat);
        await middleware.InvokeAsync(missingElection);

        Assert.True(probe.Items.ContainsKey(UnmatchedRequestClassifier.SuppressLoggingItemKey));
        Assert.True(repeat.Items.ContainsKey(UnmatchedRequestClassifier.SuppressLoggingItemKey));
        Assert.False(missingElection.Items.ContainsKey(UnmatchedRequestClassifier.SuppressLoggingItemKey));
        Assert.Equal(
            [new UnmatchedRequestCount("/api/account/billing", 2)],
            tracker.Drain());
    }

    [Fact]
    public async Task Invoke_CountsFramework405()
    {
        var tracker = new UnmatchedRequestTracker();
        var middleware = new UnmatchedRequestMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                EndpointMetadataCollection.Empty,
                UnmatchedRequestClassifier.MethodNotAllowedDisplayName));
            return Task.CompletedTask;
        }, tracker);

        var context = Context("/api/public/health", "");
        context.Request.Method = HttpMethods.Post;
        await middleware.InvokeAsync(context);

        Assert.True(context.Items.ContainsKey(UnmatchedRequestClassifier.SuppressLoggingItemKey));
        Assert.Equal(
            [new UnmatchedRequestCount("/api/public/health", 1)],
            tracker.Drain());
    }

    [Fact]
    public async Task Invoke_DoesNotCountException404()
    {
        var tracker = new UnmatchedRequestTracker();
        var middleware = new UnmatchedRequestMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Items[UnmatchedRequestClassifier.RequestUnhandledItemKey] = true;
            context.Features.Set<IExceptionHandlerFeature>(new StubExceptionFeature());
            return Task.CompletedTask;
        }, tracker);

        await middleware.InvokeAsync(Context("/api/people/1", ""));

        Assert.Empty(tracker.Drain());
    }

    private static DefaultHttpContext Context(string path, string query)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (query.Length > 0)
        {
            context.Request.QueryString = new QueryString(query);
        }

        return context;
    }

    private sealed class StubExceptionFeature : IExceptionHandlerFeature
    {
        public Exception Error { get; } = new KeyNotFoundException("missing");
    }
}
