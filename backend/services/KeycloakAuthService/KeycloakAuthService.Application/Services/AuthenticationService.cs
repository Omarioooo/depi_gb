using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Application.Models;

namespace KeycloakAuthService.Application.Services;

public class AuthenticationService
{
    private readonly IKeycloakService _keycloak;
    private readonly IUserRepository _users;

    public AuthenticationService(IKeycloakService keycloak, IUserRepository users)
    {
        _keycloak = keycloak;
        _users = users;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await _keycloak.LoginAsync(
            request.Username.Trim(),
            request.Password,
            cancellationToken);

        return Map(token);
    }

    public async Task<LoginResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await _keycloak.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        return Map(token);
    }

    public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
        => _keycloak.LogoutAsync(request.RefreshToken, cancellationToken);

    public async Task<MeResponse> GetMeAsync(
        Guid keycloakId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByKeycloakIdAsync(keycloakId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var roles = await _keycloak.GetUserRolesAsync(keycloakId, cancellationToken);

        return new MeResponse
        {
            Id = user.Id,
            KeycloakId = user.KeycloakId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles
        };
    }

    public async Task ChangePasswordAsync(
        Guid keycloakId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByKeycloakIdAsync(keycloakId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var passwordIsValid = await _keycloak.VerifyPasswordAsync(
            user.Username,
            request.CurrentPassword,
            cancellationToken);

        if (!passwordIsValid)
        {
            throw new BadRequestException("Current password is incorrect.");
        }

        await _keycloak.ChangePasswordAsync(keycloakId, request.NewPassword, cancellationToken);
    }

    private static LoginResponse Map(KeycloakToken token) => new()
    {
        AccessToken = token.AccessToken,
        RefreshToken = token.RefreshToken,
        ExpiresIn = token.ExpiresIn,
        RefreshExpiresIn = token.RefreshExpiresIn,
        TokenType = token.TokenType
    };
}
