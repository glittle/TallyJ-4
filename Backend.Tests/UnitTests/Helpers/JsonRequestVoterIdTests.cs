using System.Text;
using Backend.Helpers;
using Backend.Middleware;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.UnitTests.Helpers;

public class JsonRequestVoterIdTests
{
    [Fact]
    public async Task TryReadAsync_ReadsCamelCaseVoterId_AndRewindsBody()
    {
        var context = BodyContext("""{"voterId":"Alex@Example.com","voterIdType":"E"}""");

        var peek = await JsonRequestVoterId.TryReadAsync(context.Request, CancellationToken.None);

        Assert.Equal(JsonRequestVoterIdStatus.Found, peek.Status);
        Assert.Equal("Alex@Example.com", peek.VoterId);
        Assert.Equal(0, context.Request.Body.Position);
    }

    [Fact]
    public async Task TryReadAsync_ReadsPascalCaseVoterId()
    {
        var context = BodyContext("""{"VoterId":"rate-limit@example.com","VerifyCode":"XXXXXX"}""");

        var peek = await JsonRequestVoterId.TryReadAsync(context.Request, CancellationToken.None);

        Assert.Equal(JsonRequestVoterIdStatus.Found, peek.Status);
        Assert.Equal("rate-limit@example.com", peek.VoterId);
    }

    [Fact]
    public async Task TryReadAsync_EmptyOrMissing_IsMissingNotTooLarge()
    {
        Assert.Equal(
            JsonRequestVoterIdStatus.Missing,
            (await JsonRequestVoterId.TryReadAsync(BodyContext("").Request, CancellationToken.None)).Status);
        Assert.Equal(
            JsonRequestVoterIdStatus.Missing,
            (await JsonRequestVoterId.TryReadAsync(BodyContext("{}").Request, CancellationToken.None)).Status);
        Assert.Equal(
            JsonRequestVoterIdStatus.Missing,
            (await JsonRequestVoterId.TryReadAsync(BodyContext("""{"voterId":"  "}""").Request, CancellationToken.None)).Status);
        Assert.Equal(
            JsonRequestVoterIdStatus.Missing,
            (await JsonRequestVoterId.TryReadAsync(BodyContext("not-json").Request, CancellationToken.None)).Status);
    }

    [Fact]
    public async Task TryReadAsync_ChunkedBodyWithinMax_ReadsVoterId_AndRewinds()
    {
        var context = BodyContext("""{"voterId":"chunked@example.com"}""", setContentLength: false);

        var peek = await JsonRequestVoterId.TryReadAsync(context.Request, CancellationToken.None);

        Assert.Equal(JsonRequestVoterIdStatus.Found, peek.Status);
        Assert.Equal("chunked@example.com", peek.VoterId);
        Assert.Equal(0, context.Request.Body.Position);
    }

    [Fact]
    public async Task TryReadAsync_ChunkedBodyOverMax_IsTooLarge_WithoutReadingWholeBody()
    {
        var huge = new byte[JsonRequestVoterId.MaxBodyBytes + 64 * 1024];
        Encoding.UTF8.GetBytes("{\"voterId\":\"overflow@example.com\",\"pad\":\"").CopyTo(huge, 0);
        var stream = new CountingReadStream(huge);
        var context = new DefaultHttpContext();
        context.Request.Body = stream;
        context.Request.ContentType = "application/json";

        var peek = await JsonRequestVoterId.TryReadAsync(context.Request, CancellationToken.None);

        Assert.Equal(JsonRequestVoterIdStatus.TooLarge, peek.Status);
        Assert.Null(peek.VoterId);
        Assert.Equal(JsonRequestVoterId.MaxBodyBytes + 1, stream.BytesRead);
    }

    [Fact]
    public async Task TryReadAsync_ContentLengthOverMax_IsTooLarge_WithoutReading()
    {
        var huge = new byte[JsonRequestVoterId.MaxBodyBytes + 1];
        var stream = new CountingReadStream(huge);
        var context = new DefaultHttpContext();
        context.Request.Body = stream;
        context.Request.ContentLength = huge.Length;
        context.Request.ContentType = "application/json";

        var peek = await JsonRequestVoterId.TryReadAsync(context.Request, CancellationToken.None);

        Assert.Equal(JsonRequestVoterIdStatus.TooLarge, peek.Status);
        Assert.Null(peek.VoterId);
        Assert.Equal(0, stream.BytesRead);
    }

    [Fact]
    public async Task ReadAtMostAsync_Overflow_StopsAfterMaxPlusOne()
    {
        var huge = new byte[JsonRequestVoterId.MaxBodyBytes + 64 * 1024];
        var stream = new CountingReadStream(huge);

        var result = await JsonRequestVoterId.ReadAtMostAsync(
            stream,
            JsonRequestVoterId.MaxBodyBytes,
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(JsonRequestVoterId.MaxBodyBytes + 1, stream.BytesRead);
    }

    [Fact]
    public void NormalizeForRateLimit_FoldsCase()
    {
        Assert.Equal(
            JsonRequestVoterId.NormalizeForRateLimit("Alex@Example.com"),
            JsonRequestVoterId.NormalizeForRateLimit("alex@example.com"));
    }

    [Fact]
    public void GetIdentifierKey_SameNormalizedId_SameKey()
    {
        var path = "/api/online-voting/requestCode";
        var a = RateLimitingMiddleware.GetIdentifierKey(path, JsonRequestVoterId.NormalizeForRateLimit("A@x.com"));
        var b = RateLimitingMiddleware.GetIdentifierKey(path, JsonRequestVoterId.NormalizeForRateLimit("a@x.com"));

        Assert.Equal(a, b);
        Assert.DoesNotContain("a@x.com", a, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetClientKey_RequestCode_TwoPublicIps_AreTwoBuckets()
    {
        var path = "/api/online-voting/requestCode";
        Assert.NotEqual(
            RateLimitingMiddleware.GetClientKey(path, "203.0.113.10"),
            RateLimitingMiddleware.GetClientKey(path, "203.0.113.20"));
    }

    private static DefaultHttpContext BodyContext(string json, bool setContentLength = true)
    {
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(json);
        context.Request.Body = new MemoryStream(bytes);
        if (setContentLength)
        {
            context.Request.ContentLength = bytes.Length;
        }

        context.Request.ContentType = "application/json";
        return context;
    }

    private sealed class CountingReadStream : MemoryStream
    {
        public int BytesRead { get; private set; }

        public CountingReadStream(byte[] buffer) : base(buffer)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }
    }
}
