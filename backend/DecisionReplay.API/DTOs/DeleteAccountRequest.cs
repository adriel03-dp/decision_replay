using System.ComponentModel.DataAnnotations;

namespace DecisionReplay.API.DTOs;

public class DeleteAccountRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
