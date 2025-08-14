using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using TaskManagerApi.DATA;

namespace TaskManagerApi.IntegrationTests;

public class JwtAuthTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public JwtAuthTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();        
    }

    private TaskDbContext GetDbContext()
    {
         // Get TaskDbContext from the factory's DI container
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        var loginRequest = new LoginRequest
        {
            Email = "a@a.com",
            Password = "test",
            TwoFactorCode = null,
            TwoFactorRecoveryCode = null
        };

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result?.accessToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest
        {
            Email = "a@a.com",
            Password = "wrongpassword",
            TwoFactorCode = null,
            TwoFactorRecoveryCode = null
        };
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task Access_ProtectedEndpoint_WithValidJwt_Succeeds()
    {
        // First, login to get JWT
        var loginRequest = new LoginRequest
        {
            Email = "a@a.com",
            Password = "test",
            TwoFactorCode = null,
            TwoFactorRecoveryCode = null
        };
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = result?.accessToken;

        // Use the returned access token to access a protected endpoint
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var protectedResponse = await _client.GetAsync("/task/1");

        Assert.Equal(System.Net.HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewJwtToken()
    {
        // First, login to get JWT
        var loginRequest = new LoginRequest
        {
            Email = "a@a.com",
            Password = "test",
            TwoFactorCode = null,
            TwoFactorRecoveryCode = null
        };
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var refreshToken = result?.refreshToken;

        // Use the returned refresh token to request a new access token
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshToken);
        var protectedResponse = await _client.PostAsync("/auth/refresh", null);

        Assert.Equal(System.Net.HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    //create test to register a new user
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated_ReRegisterCreatedUser_ReturnsConflict()
    {

        var newUserName = "NewUser";
        var registerUserRequest = new UserRegistrationRequest
        {
            Name = newUserName,
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", registerUserRequest);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        //try to register duplicate user
        response = await _client.PostAsJsonAsync("/auth/register", registerUserRequest);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);

        //clean up registered user
        var dbContext = GetDbContext();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Name == newUserName);
        if (user != null)
        {
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();
        }
    }


}

public class LoginResponse
{
    public string? accessToken { get; set; }
    public string? refreshToken { get; set; }
}
//

public class UserRegistrationRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}