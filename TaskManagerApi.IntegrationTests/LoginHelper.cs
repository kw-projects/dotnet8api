
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity.Data;
using TaskManagerApi.IntegrationTests;

public static class LoginHelper
{
    public static async Task<string?> LoginAsync(HttpClient client)
    {
        var loginRequest = new LoginRequest
        {
            Email = "a@a.com",
            Password = "test",
            TwoFactorCode = null,
            TwoFactorRecoveryCode = null
        };
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = result?.accessToken;
        return accessToken;
    }
}