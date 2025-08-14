using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

public static class TokenHelper
{

    //create jwt token
    public static string GenerateJwtToken(User user, string secret, int expireMinutes, int? expiryDays = null)
    {
        // Implementation for generating a JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim("id", user.Id.ToString())
            }),
            NotBefore = DateTime.UtcNow,
            Expires = expiryDays.HasValue ? DateTime.UtcNow.AddDays(expiryDays.Value) : DateTime.UtcNow.AddMinutes(expireMinutes),
            //sign jwt token
            Issuer = "TaskManagerApi",
            Audience = "TaskManagerApi",    
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
