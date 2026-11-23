using KeycloakAuthService.API.Auth;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakAuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PasswordController : ControllerBase
{
    private readonly AuthenticationService _authentication;

    public PasswordController(AuthenticationService authentication)
    {
        _authentication = authentication;
    }

    [HttpPut("change")]
    public async Task<IActionResult> Change(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authentication.ChangePasswordAsync(
            User.GetKeycloakId(),
            request,
            cancellationToken);

        return NoContent();
    }
}
