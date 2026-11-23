using System.Net.Http.Json;
using System.Text.Json;
using KeycloakAuthService.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeycloakAuthService.Infrastructure.Keycloak;

/// <summary>
/// Caches the Keycloak admin token in memory so administrative calls reuse it until it expires.
/// Registered as a singleton.
/// </summary>
public sealed class KeycloakAdminTokenProvider : IKeycloakAdminTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakAdminTokenProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public KeycloakAdminTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakAdminTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasUsableToken())
        {
            return _accessToken!;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            // Another caller may have refreshed the token while we waited for the lock.
            if (HasUsableToken())
            {
                return _accessToken!;
            }

            var token = await RequestTokenAsync(cancellationToken);

            _accessToken = token.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool HasUsableToken()
        => !string.IsNullOrEmpty(_accessToken)
           && DateTimeOffset.UtcNow
              < _expiresAt.AddSeconds(-_options.AdminTokenRefreshMarginSeconds);

    private async Task<TokenEndpointResponse> RequestTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.RealmBaseUrl}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.AdminClientId,
                ["client_secret"] = _options.AdminClientSecret
            })
        };

        var client = _httpClientFactory.CreateClient(KeycloakHttpClient.Name);

        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception)
            when (!cancellationToken.IsCancellationRequested
                  && exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Keycloak could not be reached for an admin token.");

            throw new KeycloakException(
                "The authentication service could not contact Keycloak. Please try again later.",
                exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogError(
                    "Keycloak admin token request failed with status {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    body);

                throw new KeycloakException(
                    "The authentication service could not contact Keycloak. Please try again later.");
            }

            var payload = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>(
                JsonSerializerOptions.Web,
                cancellationToken);

            if (payload is null || string.IsNullOrEmpty(payload.AccessToken))
            {
                throw new KeycloakException(
                    "The authentication service received an invalid response from Keycloak.");
            }

            return payload;
        }
    }
}
