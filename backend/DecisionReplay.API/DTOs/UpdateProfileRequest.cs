using System.ComponentModel.DataAnnotations;

namespace DecisionReplay.API.DTOs;

public class UpdateProfileRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;
}
