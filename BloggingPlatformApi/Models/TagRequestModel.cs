using System.ComponentModel.DataAnnotations;

namespace BloggingPlatformApi.Models;

public class TagRequestModel
{
    [Required]
    public string Name { get; set; } = null!;
}



