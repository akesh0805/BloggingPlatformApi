using System.ComponentModel.DataAnnotations;

namespace BloggingPlatformApi.Models;

public class CategoryRequestModel
{
    [Required]
    public string Name { get; set; } = null!;
}



