using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using TaskManagerApi.DATA;
using System.Net.Http.Json;

namespace TaskManagerApi.IntegrationTests;

public class JwtAuthTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JwtAuthTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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
        // Arrange
        var _client = _factory.CreateClient();

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
        // Arrange
        var _client = _factory.CreateClient();

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
        // Arrange
        var _client = _factory.CreateClient();
        
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
        // Arrange
        var _client = _factory.CreateClient();

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

        //Creates new http-only cookie with refresh token
        var protectedResponse = await _client.PostAsync("/auth/refresh", null);
        Assert.Equal(System.Net.HttpStatusCode.OK, protectedResponse.StatusCode);

        //inspect the http-only cookie
        var cookie = protectedResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(cookie);
        Assert.Contains("refreshToken", cookie);

    }

    //create test to register a new user
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated_ReRegisterCreatedUser_ReturnsConflict()
    {

        // Arrange
        var _client = _factory.CreateClient();

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

    //test for expired token based on jwtsettings
    [Fact]
    public async Task Access_ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var _client = _factory.CreateClient();

        //get user from dbcontext
        var dbContext = GetDbContext();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "a@a.com");
        if (user != null)
        {
            // First, login to get JWT
            var loginRequest = new LoginRequest
            {
                Email = user.Email,
                Password = "test",
                TwoFactorCode = null,
                TwoFactorRecoveryCode = null
            };
            var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            var accessToken = result?.accessToken;

            //Simulate token expiration            
            // Get JwtSettings from DI
            var jwtSettings = _factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;

            // Read the JWT secret from appsettings.json
            var config = _factory.Services.GetRequiredService<IConfiguration>();
            var secretFromConfig = config["Jwt:Secret"] ?? config["Jwt:Key"];
            if (!string.IsNullOrEmpty(secretFromConfig))
            {
                jwtSettings.Key = secretFromConfig;
            }

            // re-sign the access token to be expired using same key and claims
            var expiredToken = TokenHelper.GenerateExpiredJwtToken(user, jwtSettings.Key);

            // Use the expired token to access a protected endpoint
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);
            var protectedResponse = await _client.GetAsync("/task/1");

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        }
        else
        {
            //fail as user couldnt be found in db
            Assert.Fail("User not found");
        }
    }
    //test create expired token and use refresh token from login to protected endpoint with success
    [Fact]
    public async Task Access_ProtectedEndpoint_WithExpiredToken_UsingRefreshToken_ReturnsOk()
    {
        // Arrange
        var _client = _factory.CreateClient();

        //get user from dbcontext
        var dbContext = GetDbContext();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "a@a.com");
        if (user != null)
        {
            // First, login to get JWT
            var loginRequest = new LoginRequest
            {
                Email = user.Email,
                Password = "test",
                TwoFactorCode = null,
                TwoFactorRecoveryCode = null
            };
            var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            var accessToken = result?.accessToken;

            // Extract the refresh token from the Set-Cookie header if present
            string? refreshToken = null;
            if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    if (cookie.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase))
                    {
                        var tokenPart = cookie.Split(';')[0];
                        refreshToken = tokenPart.Substring("refreshToken=".Length);
                        break;
                    }
                }
            }
            refreshToken ??= result?.refreshToken;

            //Simulate token expiration
            // Get JwtSettings from DI
            var jwtSettings = _factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;

            // Read the JWT secret from appsettings.json
            var config = _factory.Services.GetRequiredService<IConfiguration>();
            var secretFromConfig = config["Jwt:Secret"] ?? config["Jwt:Key"];
            if (!string.IsNullOrEmpty(secretFromConfig))
            {
                jwtSettings.Key = secretFromConfig;
            }

            // Use the access token to access a protected endpoint
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var protectedResponse = await _client.GetAsync("/task/1");
            Assert.Equal(System.Net.HttpStatusCode.OK, protectedResponse.StatusCode);

            // re-sign the access token to be expired using same key and claims
            var expiredToken = TokenHelper.GenerateExpiredJwtToken(user, jwtSettings.Key);

            // Use the expired token to access a protected endpoint
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);
            var invalidResponse = await _client.GetAsync("/task/1");
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
            
            //create new access token from refresh token
            if (string.IsNullOrEmpty(refreshToken))
            {
                Assert.Fail("Refresh token is null or empty");
            }
            using var authScope = _factory.Services.CreateScope();
            var authService = authScope.ServiceProvider.GetRequiredService<IAuthService>();

            var authResult = await authService.RefreshToken(refreshToken!);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            var protectedRefreshResponse = await _client.GetAsync("/task/1");
            Assert.Equal(System.Net.HttpStatusCode.OK, protectedRefreshResponse.StatusCode);

        }
        else
        {
            //fail as user couldnt be found in db
            Assert.Fail("User not found");
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