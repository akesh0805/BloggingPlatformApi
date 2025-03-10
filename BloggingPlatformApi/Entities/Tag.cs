using System.ComponentModel.DataAnnotations;

namespace BloggingPlatformApi.Entities;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    public ICollection<PostTag> PostTags { get; set; } = [];
}

