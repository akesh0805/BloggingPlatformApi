using System.ComponentModel.DataAnnotations;

namespace BloggingPlatformApi.Models;

public class RegisterModel
{
    [Required]
    public string? Username { get; set; }
    [Required]
    [MinLength(6)]
    public string? Password { get; set; }
    [Required]
    public string Role { get; set; } = "user";
}