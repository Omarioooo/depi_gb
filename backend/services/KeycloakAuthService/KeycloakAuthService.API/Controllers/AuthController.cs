using KeycloakAuthService.API.Auth;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Results;
using KeycloakAuthService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakAuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _users;
    private readonly AuthenticationService _authentication;

    public AuthController(UserService users, AuthenticationService authentication)
    {
        _users = users;
        _authentication = authentication;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.RegisterAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Created(user, "User registered successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _authentication.LoginAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<LoginResponse>.Ok(tokens, "Login successful."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _authentication.LogoutAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _authentication.RefreshTokenAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<LoginResponse>.Ok(tokens, "Token refreshed successfully."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var profile = await _authentication.GetMeAsync(User.GetKeycloakId(), cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<MeResponse>.Ok(profile, "User profile retrieved successfully."));
    }
}
