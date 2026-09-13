using System.Net;
using Backend.Helpers;
using Backend.Middleware;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.UnitTests.Helpers;

public class HttpContextExtensionsClientIpTests
{
    [Fact]
    public void GetClientIpAddress_WithoutForwardedHeaders_UsesRemoteAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.10");

        Assert.Equal("198.51.100.10", context.GetClientIpAddress());
    }

    [Fact]
    public void GetClientIpAddress_WithoutRemoteOrForwarded_ReturnsUnknown()
    {
        var context = new DefaultHttpContext();

        Assert.Equal("unknown", context.GetClientIpAddress());
    }

    [Fact]
    public void GetClientIpAddress_PublicRemoteIp_IgnoresSpoofedXForwardedFor()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.10");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.99";

        Assert.Equal("198.51.100.10", context.GetClientIpAddress());
    }

    [Fact]
    public void GetClientIpAddress_InfrastructurePeer_UsesRightmostPublicNotLeftmost()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.1, 203.0.113.10, 10.0.0.4";

        Assert.Equal("203.0.113.10", context.GetClientIpAddress());
    }

    [Fact]
    public void GetClientIpAddress_TwoClientsBehindOneProxy_AreDistinct()
    {
        var proxy = IPAddress.Parse("10.0.0.4");

        var clientA = new DefaultHttpContext();
        clientA.Connection.RemoteIpAddress = proxy;
        clientA.Request.Headers["X-Forwarded-For"] = "203.0.113.10, 10.0.0.4";

        var clientB = new DefaultHttpContext();
        clientB.Connection.RemoteIpAddress = proxy;
        clientB.Request.Headers["X-Forwarded-For"] = "203.0.113.20, 10.0.0.4";

        var ipA = clientA.GetClientIpAddress();
        var ipB = clientB.GetClientIpAddress();

        Assert.Equal("203.0.113.10", ipA);
        Assert.Equal("203.0.113.20", ipB);
        Assert.NotEqual(
            RateLimitingMiddleware.GetClientKey("/api/auth/login", ipA),
            RateLimitingMiddleware.GetClientKey("/api/auth/login", ipB));
    }

    [Fact]
    public void GetClientIpAddress_SpoofedLeftmost_StaysSameBucket()
    {
        var proxy = IPAddress.Parse("10.0.0.4");

        var first = new DefaultHttpContext();
        first.Connection.RemoteIpAddress = proxy;
        first.Request.Headers["X-Forwarded-For"] = "198.51.100.1, 203.0.113.10, 10.0.0.4";

        var spoofed = new DefaultHttpContext();
        spoofed.Connection.RemoteIpAddress = proxy;
        spoofed.Request.Headers["X-Forwarded-For"] = "198.51.100.99, 203.0.113.10, 10.0.0.4";

        var keyA = RateLimitingMiddleware.GetClientKey("/api/auth/login", first.GetClientIpAddress());
        var keyB = RateLimitingMiddleware.GetClientKey("/api/auth/login", spoofed.GetClientIpAddress());

        Assert.Equal("203.0.113.10", first.GetClientIpAddress());
        Assert.Equal(keyA, keyB);
    }

    [Fact]
    public void GetClientIpAddress_ForwardedHeader_UsesRightmostPublicFor()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        context.Request.Headers["Forwarded"] =
            "for=198.51.100.1;proto=https, for=198.51.100.22;proto=https;by=10.0.0.4";

        Assert.Equal("198.51.100.22", context.GetClientIpAddress());
    }

    [Fact]
    public void GetClientIpAddress_ForwardedHeaderIpv6_UsesForParameter()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        context.Request.Headers["Forwarded"] = "for=\"[2001:db8:cafe::17]:4711\"";

        Assert.Equal("2001:db8:cafe::17", context.GetClientIpAddress());
    }

    [Fact]
    public void GetClientKey_NormalizesPathCase()
    {
        var keyA = RateLimitingMiddleware.GetClientKey("/api/auth/login", "203.0.113.10");
        var keyB = RateLimitingMiddleware.GetClientKey("/api/Auth/login", "203.0.113.10");

        Assert.Equal(keyA, keyB);
    }
}
