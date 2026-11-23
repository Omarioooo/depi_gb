using System.Security.Claims;
using System.Text.Json;

namespace KeycloakAuthService.API.Auth;

internal static class KeycloakClaims
{
    private const string RealmAccessClaim = "realm_access";

    /// <summary>
    /// Keycloak puts realm roles inside a nested "realm_access" claim. Flatten them so
    /// [Authorize(Roles = "...")] works without a custom authorization policy.
    /// </summary>
    public static void AddRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        foreach (var role in ReadRealmRoles(principal))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    private static IEnumerable<string> ReadRealmRoles(ClaimsPrincipal principal)
    {
        var rawValue = principal.FindFirst(RealmAccessClaim)?.Value;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            yield break;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(rawValue);
        }
        catch (JsonException)
        {
            yield break;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }

            if (!document.RootElement.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var role in roles.EnumerateArray())
            {
                var name = role.GetString();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    yield return name;
                }
            }
        }
    }
}
