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
            return BadRequest(ErrorResponse.BadRequest(
                error ?? "Registration failed",
                HttpContext.Request.Path));
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
            return Unauthorized(ErrorResponse.Unauthorized(
                error ?? "Login failed",
                HttpContext.Request.Path));
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
            return Unauthorized(ErrorResponse.Unauthorized(
                "Invalid token",
                HttpContext.Request.Path));
        }

        var (success, user, error) = await _authService.UpdateProfileAsync(userId, request.Name);

        if (!success || user == null)
        {
            return BadRequest(ErrorResponse.BadRequest(
                error ?? "Update failed",
                HttpContext.Request.Path));
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
            return Unauthorized(ErrorResponse.Unauthorized(
                "Invalid token",
                HttpContext.Request.Path));
        }

        var (success, error) = await _authService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword
        );

        if (!success)
        {
            return BadRequest(ErrorResponse.BadRequest(
                error ?? "Password change failed",
                HttpContext.Request.Path));
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
            return Unauthorized(ErrorResponse.Unauthorized(
                "Invalid token",
                HttpContext.Request.Path));
        }

        var (success, error) = await _authService.DeleteAccountAsync(userId, request.Password);

        if (!success)
        {
            return BadRequest(ErrorResponse.BadRequest(
                error ?? "Account deletion failed",
                HttpContext.Request.Path));
        }

        return Ok(new { message = "Account deleted successfully" });
    }
}
