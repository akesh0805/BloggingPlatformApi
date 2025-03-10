using Microsoft.AspNetCore.Identity;

namespace BloggingPlatformApi.Entities;

public class Like
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
}

