namespace DecisionReplay.API.DTOs;

/// <summary>
/// Standardized response for profile update
/// </summary>
public class UpdateProfileResponse
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
