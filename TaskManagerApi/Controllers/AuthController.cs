using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.Data;


public class UserRegistrationRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

[ApiController]
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
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Name, Email, and Password are required.");

        try
        {
            var user = await _authService.Register(request.Name, request.Email, request.Password);

            return CreatedAtAction(nameof(Login), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User already exists"))
        {
            return Conflict(new { message = "User with this email already exists" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        //Remove the refresh token cookie
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        });
        
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
            return Unauthorized("Invalid token");
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
