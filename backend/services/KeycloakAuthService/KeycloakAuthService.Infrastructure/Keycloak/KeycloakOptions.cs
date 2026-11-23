namespace KeycloakAuthService.Infrastructure.Keycloak;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Base URL the service uses to reach Keycloak (public URL locally, internal URL in Docker).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Public client used for Direct Access Grant (login, refresh, password check).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Confidential service-account client used for the Keycloak Admin API.
    /// </summary>
    public string AdminClientId { get; set; } = string.Empty;

    public string AdminClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Renew the cached admin token this many seconds before it actually expires.
    /// </summary>
    public int AdminTokenRefreshMarginSeconds { get; set; } = 30;

    /// <summary>
    /// Expected access-token audience. Must match the "aud" claim of the tokens
    /// Keycloak actually issues for the configured realm and client.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Issuer written into tokens. Defaults to <see cref="BaseUrl"/> + realm when empty.
    /// Set this when Keycloak is published under a different hostname than the internal URL.
    /// </summary>
    public string? Issuer { get; set; }

    public string RealmBaseUrl => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}";

    public string AdminBaseUrl => $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}";

    public string EffectiveIssuer =>
        string.IsNullOrWhiteSpace(Issuer) ? RealmBaseUrl : Issuer.TrimEnd('/');
}
