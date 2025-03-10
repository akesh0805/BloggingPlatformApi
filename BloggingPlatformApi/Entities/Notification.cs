using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BloggingPlatformApi.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
    [Required, MaxLength(500)]
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

