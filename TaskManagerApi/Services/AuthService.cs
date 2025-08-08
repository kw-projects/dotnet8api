using Microsoft.EntityFrameworkCore;
using TaskManagerApi.DATA;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

public class AuthService : IAuthService
{
    private readonly TaskDbContext _dbContext;
    private readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    public AuthService(TaskDbContext dbContext, ILogger<AuthService> logger, JwtSettings jwtSettings)
    {
        _dbContext = dbContext;
        _logger = logger;
        _jwtSettings = jwtSettings;
    }

    private bool VerifyPassword(string name, string password, string hashedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(name, hashedPassword, password);
        return result == PasswordVerificationResult.Success;
    }

    public async Task<User> Register(string name, string email, string password)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null) throw new Exception("User already exists");

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(name, password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<AuthResult> Login(LoginRequest request)
    {
        _logger.LogInformation($"Attempting login for user: {request.Email}");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !VerifyPassword(request.Email, request.Password, user.PasswordHash))
        {
            _logger.LogWarning($"Login failed for user: {request.Email}");
            throw new UnauthorizedAccessException("Invalid credentials");
        }
        //Generate access token
        var accessToken = TokenHelper.GenerateJwtToken(user, _jwtSettings.Key, _jwtSettings.AccessTokenExpireMinutes);

        //Generate refresh token and store against user record
        user.RefreshHashedToken = TokenHelper.GenerateJwtToken(user, _jwtSettings.Key, _jwtSettings.RefreshTokenExpireDays);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays);
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation($"Updated user id: {user.Id}");
        _logger.LogInformation($"Login successful for user: {user.Name}");


        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshHashedToken
        };
    }

    public async Task<AuthResult> RefreshToken(string token)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.RefreshHashedToken == token);
        if (user == null)
        {
            _logger.LogWarning($"Token refresh failed for user: {user?.Name}");
            throw new UnauthorizedAccessException("Invalid token");
        }

        user.RefreshHashedToken = TokenHelper.GenerateJwtToken(user, _jwtSettings.Key, _jwtSettings.RefreshTokenExpireDays);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays);
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Token refreshed successfully for user: {user.Name}");
        return new AuthResult
        {
            AccessToken = TokenHelper.GenerateJwtToken(user, _jwtSettings.Key, _jwtSettings.AccessTokenExpireMinutes),
            RefreshToken = user.RefreshHashedToken
        };
    }

    public async Task Logout()
    {
        // Implement logout logic (e.g., invalidate token)
        _logger.LogInformation("User logged out successfully.");
        await Task.CompletedTask;
    }
    

}
