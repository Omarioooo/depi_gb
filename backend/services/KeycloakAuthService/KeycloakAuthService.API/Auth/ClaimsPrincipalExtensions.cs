using System.Security.Claims;
using KeycloakAuthService.Application.Common;

namespace KeycloakAuthService.API.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the current user's Keycloak id from the validated "sub" claim.
    /// Never read a user id from the request body for current-user operations.
    /// </summary>
    public static Guid GetKeycloakId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(subject, out var keycloakId))
        {
            throw new UnauthorizedException("The access token does not contain a valid user identifier.");
        }

        return keycloakId;
    }
}
