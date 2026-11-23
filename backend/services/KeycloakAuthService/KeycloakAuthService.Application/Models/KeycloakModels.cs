namespace KeycloakAuthService.Application.Models;

public sealed record KeycloakToken(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    int RefreshExpiresIn,
    string TokenType);

public sealed record KeycloakRole(
    Guid Id,
    string Name,
    string? Description);
