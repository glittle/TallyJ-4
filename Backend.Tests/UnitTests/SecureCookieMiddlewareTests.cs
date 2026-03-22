using Microsoft.AspNetCore.Http;
using Backend.Middleware;
using Xunit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Backend.Tests.UnitTests;

public class SecureCookieMiddlewareTests
{
    [Fact]
    public void SetAuthCookies_SetsAllRequiredCookies()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessToken = "test_access_token";
        var refreshToken = "test_refresh_token";
        var email = "test@example.com";
        var name = "Test User";
        var authMethod = "Local";
        var isHttps = true;

        // Act
        SecureCookieMiddleware.SetAuthCookies(httpContext, accessToken, refreshToken, email, name, authMethod, isHttps: isHttps);

        // Assert
        var cookies = httpContext.Response.GetTypedHeaders().SetCookie;
        Assert.Equal(5, cookies.Count); // access_token, refresh_token, user_email, user_name, auth_method

        // Check access token cookie
        var accessCookie = cookies.First(c => c.Name == SecureCookieMiddleware.AccessTokenCookieName);
        Assert.Equal(accessToken, accessCookie.Value.Value);
        Assert.True(accessCookie.HttpOnly);
        Assert.True(accessCookie.Secure);
        Assert.Equal("Strict", accessCookie.SameSite.ToString());

        // Check refresh token cookie
        var refreshCookie = cookies.First(c => c.Name == SecureCookieMiddleware.RefreshTokenCookieName);
        Assert.Equal(refreshToken, refreshCookie.Value.Value);
        Assert.True(refreshCookie.HttpOnly);
        Assert.True(refreshCookie.Secure);
        Assert.Equal("Strict", refreshCookie.SameSite.ToString());

        // Check user email cookie
        var emailCookie = cookies.First(c => c.Name == SecureCookieMiddleware.UserEmailCookieName);
        Assert.Equal(email, Uri.UnescapeDataString(emailCookie.Value.Value));
        Assert.False(emailCookie.HttpOnly); // User info cookies are not httpOnly
        Assert.True(emailCookie.Secure);
        Assert.Equal("Strict", emailCookie.SameSite.ToString());

        // Check user name cookie
        var nameCookie = cookies.First(c => c.Name == SecureCookieMiddleware.UserNameCookieName);
        Assert.Equal(name, Uri.UnescapeDataString(nameCookie.Value.Value));
        Assert.False(nameCookie.HttpOnly);
        Assert.True(nameCookie.Secure);
        Assert.Equal("Strict", nameCookie.SameSite.ToString());

        // Check auth method cookie
        var methodCookie = cookies.First(c => c.Name == SecureCookieMiddleware.AuthMethodCookieName);
        Assert.Equal(authMethod, Uri.UnescapeDataString(methodCookie.Value.Value));
        Assert.False(methodCookie.HttpOnly);
        Assert.True(methodCookie.Secure);
        Assert.Equal("Strict", methodCookie.SameSite.ToString());
    }

    [Fact]
    public void SetAuthCookies_HandlesNullName()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessToken = "test_access_token";
        var refreshToken = "test_refresh_token";
        var email = "test@example.com";
        string? name = null;
        var authMethod = "Local";
        var isHttps = true;

        // Act
        SecureCookieMiddleware.SetAuthCookies(httpContext, accessToken, refreshToken, email, name, authMethod, isHttps: isHttps);

        // Assert
        var cookies = httpContext.Response.GetTypedHeaders().SetCookie;
        Assert.Equal(4, cookies.Count); // access_token, refresh_token, user_email, auth_method (no user_name)

        // Check that user_name cookie is not set
        Assert.DoesNotContain(cookies, c => c.Name == SecureCookieMiddleware.UserNameCookieName);
    }

    [Fact]
    public void SetAuthCookies_RespectsHttpHttpsSetting()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessToken = "test_access_token";
        var refreshToken = "test_refresh_token";
        var email = "test@example.com";
        var name = "Test User";
        var authMethod = "Local";
        var isHttps = false; // HTTP, not HTTPS

        // Act
        SecureCookieMiddleware.SetAuthCookies(httpContext, accessToken, refreshToken, email, name, authMethod, isHttps: isHttps);

        // Assert
        var cookies = httpContext.Response.GetTypedHeaders().SetCookie;

        // All cookies should have Secure = false for HTTP
        foreach (var cookie in cookies)
        {
            Assert.False(cookie.Secure);
        }
    }

    [Fact]
    public void ClearAuthCookies_ClearsAllCookies()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.IsHttps = true;

        // Act
        SecureCookieMiddleware.ClearAuthCookies(httpContext);

        // Assert
        var cookies = httpContext.Response.GetTypedHeaders().SetCookie;
        Assert.Equal(5, cookies.Count); // All cookies should be cleared

        // Check that all expected cookies are cleared
        var cookieNames = cookies.Select(c => c.Name.Value).ToList();
        Assert.Contains(SecureCookieMiddleware.AccessTokenCookieName, cookieNames);
        Assert.Contains(SecureCookieMiddleware.RefreshTokenCookieName, cookieNames);
        Assert.Contains(SecureCookieMiddleware.UserEmailCookieName, cookieNames);
        Assert.Contains(SecureCookieMiddleware.UserNameCookieName, cookieNames);
        Assert.Contains(SecureCookieMiddleware.AuthMethodCookieName, cookieNames);

        // All cookies should be expired (max-age=0 or negative expires)
        foreach (var cookie in cookies)
        {
            Assert.True(cookie.Expires < DateTimeOffset.UtcNow || cookie.MaxAge == TimeSpan.Zero);
        }
    }

    [Fact]
    public void GetAccessToken_ReturnsTokenFromCookies()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var expectedToken = "test_access_token";
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [SecureCookieMiddleware.AccessTokenCookieName] = expectedToken
        });

        // Act
        var token = SecureCookieMiddleware.GetAccessToken(httpContext);

        // Assert
        Assert.Equal(expectedToken, token);
    }

    [Fact]
    public void GetAccessToken_ReturnsNullWhenNoCookie()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>());

        // Act
        var token = SecureCookieMiddleware.GetAccessToken(httpContext);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void GetRefreshToken_ReturnsTokenFromCookies()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var expectedToken = "test_refresh_token";
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [SecureCookieMiddleware.RefreshTokenCookieName] = expectedToken
        });

        // Act
        var token = SecureCookieMiddleware.GetRefreshToken(httpContext);

        // Assert
        Assert.Equal(expectedToken, token);
    }

    [Fact]
    public void GetUserEmail_ReturnsEmailFromCookies()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var expectedEmail = "test@example.com";
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [SecureCookieMiddleware.UserEmailCookieName] = expectedEmail
        });

        // Act
        var email = SecureCookieMiddleware.GetUserEmail(httpContext);

        // Assert
        Assert.Equal(expectedEmail, email);
    }

    [Fact]
    public void GetUserName_ReturnsNameFromCookies()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var expectedName = "Test User";
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [SecureCookieMiddleware.UserNameCookieName] = Uri.EscapeDataString(expectedName)
        });

        // Act
        var name = SecureCookieMiddleware.GetUserName(httpContext);

        // Assert
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void GetAuthMethod_ReturnsMethodFromCookies()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var expectedMethod = "Google";
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [SecureCookieMiddleware.AuthMethodCookieName] = expectedMethod
        });

        // Act
        var method = SecureCookieMiddleware.GetAuthMethod(httpContext);

        // Assert
        Assert.Equal(expectedMethod, method);
    }
}

/// <summary>
/// Simple test cookie collection for testing
/// </summary>
public class TestCookieCollection : IRequestCookieCollection
{
    private readonly IDictionary<string, string> _cookies;

    public TestCookieCollection(IDictionary<string, string> cookies)
    {
        _cookies = cookies;
    }

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

    public int Count => _cookies.Count;

    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public bool TryGetValue(string key, out string? value) => _cookies.TryGetValue(key, out value!);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


