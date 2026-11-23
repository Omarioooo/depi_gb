namespace KeycloakAuthService.Infrastructure.Keycloak;

public interface IKeycloakAdminTokenProvider
{
    /// <summary>
    /// Returns a cached admin access token, requesting a new one only when needed.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
