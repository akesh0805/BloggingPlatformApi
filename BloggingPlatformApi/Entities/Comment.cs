using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BloggingPlatformApi.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
}

