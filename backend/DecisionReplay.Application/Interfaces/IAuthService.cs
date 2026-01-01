using DecisionReplay.Domain.Entities;

namespace DecisionReplay.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Token, User? User, string? Error)> RegisterAsync(string name, string email, string password);
    Task<(bool Success, string? Token, User? User, string? Error)> LoginAsync(string email, string password);
    Task<(bool Success, User? User, string? Error)> UpdateProfileAsync(string userId, string newName);
    Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<(bool Success, string? Error)> DeleteAccountAsync(string userId, string password);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    string GenerateJwtToken(User user);
}
