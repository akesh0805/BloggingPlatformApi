using System.ComponentModel.DataAnnotations;

namespace BloggingPlatformApi.Models;

public class CommentRequestModel
{
    [Required]
    public string Content { get; set; } = null!;
}



