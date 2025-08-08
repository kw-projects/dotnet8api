using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.Data;

[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IAuthService authService, JwtSettings jwtSettings)
    {
        _authService = authService;
        _jwtSettings = jwtSettings;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(string name, RegisterRequest request)
    {
        var user = await _authService.Register(name, request.Email, request.Password);
        return CreatedAtAction(nameof(Login), new { id = user.Id }, user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var authResult = await _authService.Login(request);

        //Create a http-only cookie to store refresh token
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays)
        };

        Response.Cookies.Append("refreshToken", authResult.RefreshToken, cookieOptions);

        return Ok(new
        {
            accessToken = authResult.AccessToken
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.Logout();
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {        

        //get cookie token
        string currentRefreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
        if(String.IsNullOrEmpty(currentRefreshToken))
        {            
            throw new UnauthorizedAccessException("Invalid token");
        }

        //create new refresh token for user and update it
        var authResult = await _authService.RefreshToken(currentRefreshToken);

        //Create a http-only cookie to store refresh token
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays)
        };

        Response.Cookies.Append("refreshToken", authResult.RefreshToken, cookieOptions);

        return Ok(new
        {
            accessToken = authResult.AccessToken
        });
    }
}
