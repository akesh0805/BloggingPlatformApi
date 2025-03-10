using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BloggingPlatformApi.Entities;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(255)]
    public string Title { get; set; } = null!;

    [Required]
    public string Content { get; set; } = null!;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
    public ICollection<MediaAttachment> MediaAttachments { get; set; } = [];
    public ICollection<PostTag> PostTags { get; set; } = [];
}

