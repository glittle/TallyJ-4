using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Identity;

namespace Backend.Tests.IntegrationTests;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly JsonSerializerOptions JsonOptions;
    private string? _currentAuthToken;

    private static bool _databaseSeeded = false;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Seed database once per test session
        if (!_databaseSeeded)
        {
            SeedDatabaseAsync().GetAwaiter().GetResult();
            _databaseSeeded = true;
        }
    }

    protected async Task<string> GetAuthTokenAsync(string email = "admin@tallyj.com", string password = "Test1234!")
    {
        // Ensure test user exists first with the correct password
        await CreateTestUserAsync(email, password);

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
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed with status {response.StatusCode}: {errorContent}");
        }

        var loginResponse = await JsonSerializer.DeserializeAsync<LoginResponse>(
            await response.Content.ReadAsStreamAsync(),
            JsonOptions);

        var token = loginResponse?.Token ?? throw new Exception("Failed to get access token");
        Console.WriteLine($"[TEST] Got auth token: {token.Substring(0, Math.Min(50, token.Length))}...");
        return token;
    }

    protected void SetAuthToken(string token)
    {
        _currentAuthToken = token;
        Console.WriteLine($"[TEST] SetAuthToken called with token length: {token?.Length ?? 0}, starts with 'eyJ': {token?.StartsWith("eyJ")}");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Console.WriteLine($"[TEST] Client.DefaultRequestHeaders.Authorization set to: {Client.DefaultRequestHeaders.Authorization?.ToString() ?? "NULL"}");
    }

    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json")
        };

        if (!string.IsNullOrEmpty(_currentAuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentAuthToken);
            Console.WriteLine($"[TEST] POST {url} with auth header: Bearer {_currentAuthToken.Substring(0, Math.Min(30, _currentAuthToken.Length))}...");
        }
        else
        {
            Console.WriteLine($"[TEST] POST {url} WITHOUT auth header");
        }

        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string url, T data)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json")
        };

        if (!string.IsNullOrEmpty(_currentAuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentAuthToken);
        }

        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> GetAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(_currentAuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentAuthToken);
        }

        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);

        if (!string.IsNullOrEmpty(_currentAuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentAuthToken);
        }

        return await Client.SendAsync(request);
    }

    protected async Task<TResponse?> DeserializeResponseAsync<TResponse>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception($"Response has empty body. Status: {response.StatusCode}, ReasonPhrase: {response.ReasonPhrase}");
        }
        
        try
        {
            return JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new Exception($"Failed to deserialize response. Status: {response.StatusCode}, Content: {content}", ex);
        }
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Only seed if database doesn't have our test data
        if (await dbContext.Users.AnyAsync(u => u.Email == "admin@tallyj.com"))
        {
            return;
        }

        // Create test users
        var adminUser = new AppUser
        {
            UserName = "admin@tallyj.com",
            Email = "admin@tallyj.com",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123!");

        var testUser = new AppUser
        {
            UserName = "test@tallyj.com",
            Email = "test@tallyj.com",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(testUser, "Test123!");

        // Create test elections
        var election1 = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Election 1",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = "LSA",
            NumberToElect = 3,
            TallyStatus = "NotStarted",
            ShowAsTest = true,
            RowVersion = new byte[8] // Initialize RowVersion for concurrency
        };

        var election2 = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Election 2",
            DateOfElection = DateTime.UtcNow.AddDays(60),
            ElectionType = "NSA",
            NumberToElect = 1,
            TallyStatus = "NotStarted",
            ShowAsTest = true,
            RowVersion = new byte[8] // Initialize RowVersion for concurrency
        };

        dbContext.Elections.AddRange(election1, election2);
        await dbContext.SaveChangesAsync();

        // Create user-election relationships
        var join1 = new JoinElectionUser
        {
            ElectionGuid = election1.ElectionGuid,
            UserId = Guid.Parse(adminUser.Id),
            Role = "Admin"
        };

        var join2 = new JoinElectionUser
        {
            ElectionGuid = election2.ElectionGuid,
            UserId = Guid.Parse(adminUser.Id),
            Role = "Admin"
        };

        var join3 = new JoinElectionUser
        {
            ElectionGuid = election1.ElectionGuid,
            UserId = Guid.Parse(testUser.Id),
            Role = "Teller"
        };

        dbContext.JoinElectionUsers.AddRange(join1, join2, join3);
        await dbContext.SaveChangesAsync();
    }

    private async Task CreateTestUserAsync(string email, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var passwordValid = await userManager.CheckPasswordAsync(existingUser, password);
            if (passwordValid)
            {
                return;
            }
            
            await userManager.DeleteAsync(existingUser);
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to create test user: {errors}");
        }
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}



