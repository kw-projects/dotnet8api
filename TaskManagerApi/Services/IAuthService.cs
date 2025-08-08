using Microsoft.AspNetCore.Identity.Data;

public interface IAuthService
{
    Task<User> Register(string name, string email, string password);
    Task<AuthResult> Login(LoginRequest request);
    Task<AuthResult> RefreshToken(string token);
    Task Logout();
}
