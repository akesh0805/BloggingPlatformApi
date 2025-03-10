using System.ComponentModel.DataAnnotations;

namespace BloggingPlatformApi.Models;

public class PostRequestModel
{
    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Content { get; set; } = null!;

    [Required]
    public string Status { get; set; } = "Draft"; // Draft, Published

    public DateTime? PublishedAt { get; set; }
    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = new();
}



