using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User
{
    public int Id { get; set; }    
    [MaxLength(50)]
    public required string Name { get; set; }
    [MaxLength(100)]
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? RefreshHashedToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
    public string? Role { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }

}