using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.API.DTOs;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, token, user, error) = await _authService.RegisterAsync(
            request.Name,
            request.Email,
            request.Password
        );

        if (!success || user == null || token == null)
        {
            return BadRequest(new { message = error ?? "Registration failed" });
        }

        var response = new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        };

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, token, user, error) = await _authService.LoginAsync(
            request.Email,
            request.Password
        );

        if (!success || user == null || token == null)
        {
            return Unauthorized(new { message = error ?? "Login failed" });
        }

        var response = new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        };

        return Ok(response);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var (success, user, error) = await _authService.UpdateProfileAsync(userId, request.Name);

        if (!success || user == null)
        {
            return BadRequest(new { message = error ?? "Update failed" });
        }

        return Ok(new { name = user.Name, email = user.Email });
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var (success, error) = await _authService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword
        );

        if (!success)
        {
            return BadRequest(new { message = error ?? "Password change failed" });
        }

        return Ok(new { message = "Password updated successfully" });
    }

    [Authorize]
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var (success, error) = await _authService.DeleteAccountAsync(userId, request.Password);

        if (!success)
        {
            return BadRequest(new { message = error ?? "Account deletion failed" });
        }

        return Ok(new { message = "Account deleted successfully" });
    }
}
