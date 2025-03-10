using System.ComponentModel.DataAnnotations;

namespace BloggingPlatformApi.Entities;

public class MediaAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(500)]
    public string FileUrl { get; set; } = null!;

    [Required, MaxLength(50)]
    public string FileType { get; set; } = null!; 
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
}

