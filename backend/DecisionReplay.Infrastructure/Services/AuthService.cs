using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace DecisionReplay.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? throw new InvalidOperationException("JWT_SECRET not configured");
        _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "DecisionReplay";
    }

    public async Task<(bool Success, string? Token, User? User, string? Error)> RegisterAsync(
        string name, string email, string password)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(email))
        {
            return (false, null, null, "Email already registered");
        }

        // Create user
        var user = new User
        {
            Name = name,
            Email = email.ToLower(),
            PasswordHash = HashPassword(password)
        };

        try
        {
            await _userRepository.CreateAsync(user);
            var token = GenerateJwtToken(user);
            return (true, token, user, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Registration failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Token, User? User, string? Error)> LoginAsync(
        string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return (false, null, null, "Invalid email or password");
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return (false, null, null, "Invalid email or password");
        }

        var token = GenerateJwtToken(user);
        return (true, token, user, null);
    }

    public async Task<(bool Success, User? User, string? Error)> UpdateProfileAsync(string userId, string newName)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return (false, null, "User not found");
        }

        user.Name = newName;
        await _userRepository.UpdateAsync(user);

        return (true, user, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return (false, "User not found");
        }

        if (!VerifyPassword(currentPassword, user.PasswordHash))
        {
            return (false, "Current password is incorrect");
        }

        user.PasswordHash = HashPassword(newPassword);
        await _userRepository.UpdateAsync(user);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAccountAsync(string userId, string password)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return (false, "User not found");
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return (false, "Password is incorrect");
        }

        var deleted = await _userRepository.DeleteAsync(userId);

        if (!deleted)
        {
            return (false, "Failed to delete account");
        }

        return (true, null);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _jwtIssuer,
            Audience = _jwtIssuer,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
