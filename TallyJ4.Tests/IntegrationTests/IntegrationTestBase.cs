using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Tests.IntegrationTests;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly JsonSerializerOptions JsonOptions;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    protected async Task<string> GetAuthTokenAsync(string email = "admin@tallyj.com", string password = "Admin123!")
    {
        var loginRequest = new
        {
            email,
            password
        };

        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync("/api/auth/login", content);
        
        if (!response.IsSuccessStatusCode)
        {
            await CreateTestUserAsync(email, password);
            response = await Client.PostAsync("/api/auth/login", content);
        }

        response.EnsureSuccessStatusCode();
        
        var loginResponse = await JsonSerializer.DeserializeAsync<LoginResponse>(
            await response.Content.ReadAsStreamAsync(),
            JsonOptions);

        return loginResponse?.AccessToken ?? throw new Exception("Failed to get access token");
    }

    protected void SetAuthToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T data)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(data),
            Encoding.UTF8,
            "application/json");

        return await Client.PostAsync(url, content);
    }

    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string url, T data)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(data),
            Encoding.UTF8,
            "application/json");

        return await Client.PutAsync(url, content);
    }

    protected async Task<TResponse?> DeserializeResponseAsync<TResponse>(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonOptions);
    }

    private async Task CreateTestUserAsync(string email, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, password);
    }

    private class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
