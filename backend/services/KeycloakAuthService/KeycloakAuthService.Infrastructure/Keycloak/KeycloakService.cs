using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeycloakAuthService.Infrastructure.Keycloak;

public sealed class KeycloakService : IKeycloakService
{
    private const string UpstreamErrorMessage =
        "The authentication service could not complete the request. Please try again later.";

    private const string DirectAccessGrantDisabledMessage =
        "Direct access grant is not enabled for the configured Keycloak client.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IKeycloakAdminTokenProvider _adminTokenProvider;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakService> _logger;

    public KeycloakService(
        IHttpClientFactory httpClientFactory,
        IKeycloakAdminTokenProvider adminTokenProvider,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _adminTokenProvider = adminTokenProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<KeycloakToken> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendTokenRequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["username"] = username,
                ["password"] = password,
                ["scope"] = "openid"
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadTokenErrorAsync(response, "password login", cancellationToken);

            if (error == "invalid_grant")
            {
                throw new UnauthorizedException("Invalid username or password.");
            }

            if (error is "unauthorized_client" or "invalid_client")
            {
                throw new KeycloakException(DirectAccessGrantDisabledMessage);
            }

            throw new KeycloakException(UpstreamErrorMessage);
        }

        return await ReadTokenAsync(response, cancellationToken);
    }

    public async Task<KeycloakToken> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendTokenRequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["refresh_token"] = refreshToken
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadTokenErrorAsync(response, "token refresh", cancellationToken);

            if (error == "invalid_grant")
            {
                throw new UnauthorizedException("The refresh token is invalid or has expired.");
            }

            throw new KeycloakException(UpstreamErrorMessage);
        }

        return await ReadTokenAsync(response, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.RealmBaseUrl}/protocol/openid-connect/logout")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["refresh_token"] = refreshToken
                })
            };

            var client = CreateClient();
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                // Logout is idempotent: an already revoked token is not an error for the caller.
                _logger.LogWarning(
                    "Keycloak logout returned {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    body);
            }
        }
        catch (Exception exception)
            when (!cancellationToken.IsCancellationRequested
                  && exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Keycloak logout request failed; treating logout as complete.");
        }
    }

    public async Task<bool> VerifyPasswordAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendTokenRequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["username"] = username,
                ["password"] = password,
                ["scope"] = "openid"
            },
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            var error = await ReadTokenErrorAsync(response, "password verification", cancellationToken);

            if (error is "unauthorized_client" or "invalid_client")
            {
                throw new KeycloakException(DirectAccessGrantDisabledMessage);
            }

            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            await ReadTokenErrorAsync(response, "password verification", cancellationToken);
            throw new KeycloakException(UpstreamErrorMessage);
        }

        var token = await ReadTokenAsync(response, cancellationToken);

        // The check created a Keycloak session; close it so it does not linger.
        await LogoutAsync(token.RefreshToken, cancellationToken);

        return true;
    }

    public async Task ChangePasswordAsync(
        Guid keycloakUserId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Put,
            $"/users/{keycloakUserId}/reset-password",
            new { type = "password", value = newPassword, temporary = false },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("User not found in Keycloak.");
        }

        await EnsureAdminSuccessAsync(response, "reset password", cancellationToken);
    }

    public async Task<Guid> CreateUserAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Post,
            "/users",
            new
            {
                username,
                email,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new { type = "password", value = password, temporary = false }
                }
            },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            await LogAdminFailureAsync(response, "create user", cancellationToken);
            throw new ConflictException("A user with the same username or email already exists.");
        }

        await EnsureAdminSuccessAsync(response, "create user", cancellationToken);

        var location = response.Headers.Location?.ToString();
        var idSegment = location?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        if (!Guid.TryParse(idSegment, out var keycloakId))
        {
            _logger.LogError(
                "Keycloak did not return a valid user id. Location header: {Location}",
                location);

            throw new KeycloakException(UpstreamErrorMessage);
        }

        return keycloakId;
    }

    public async Task UpdateUserAsync(
        Guid keycloakUserId,
        string username,
        string email,
        CancellationToken cancellationToken = default)
    {
        // Keycloak replaces the whole user on PUT, so read the current representation first.
        using var getResponse = await SendAdminAsync(
            HttpMethod.Get,
            $"/users/{keycloakUserId}",
            null,
            cancellationToken);

        if (getResponse.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("User not found in Keycloak.");
        }

        await EnsureAdminSuccessAsync(getResponse, "get user", cancellationToken);

        var representation = await getResponse.Content.ReadFromJsonAsync<JsonObject>(
            JsonSerializerOptions.Web,
            cancellationToken) ?? throw new KeycloakException(UpstreamErrorMessage);

        representation["username"] = username;
        representation["email"] = email;
        representation["emailVerified"] = true;

        using var putResponse = await SendAdminAsync(
            HttpMethod.Put,
            $"/users/{keycloakUserId}",
            representation,
            cancellationToken);

        if (putResponse.StatusCode == HttpStatusCode.Conflict)
        {
            await LogAdminFailureAsync(putResponse, "update user", cancellationToken);
            throw new ConflictException("A user with the same username or email already exists.");
        }

        await EnsureAdminSuccessAsync(putResponse, "update user", cancellationToken);
    }

    public async Task DeleteUserAsync(
        Guid keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Delete,
            $"/users/{keycloakUserId}",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureAdminSuccessAsync(response, "delete user", cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(
        Guid keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Get,
            $"/users/{keycloakUserId}/role-mappings/realm",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("User not found in Keycloak.");
        }

        await EnsureAdminSuccessAsync(response, "get user roles", cancellationToken);

        var roles = await response.Content.ReadFromJsonAsync<List<RoleRepresentation>>(
            JsonSerializerOptions.Web,
            cancellationToken);

        return roles?.Select(x => x.Name).ToList() ?? new List<string>();
    }

    public async Task AssignRealmRoleAsync(
        Guid keycloakUserId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var role = await GetRoleByNameAsync(roleName, cancellationToken)
            ?? throw new NotFoundException($"Role '{roleName}' was not found.");

        using var response = await SendAdminAsync(
            HttpMethod.Post,
            $"/users/{keycloakUserId}/role-mappings/realm",
            new[] { new { id = role.Id, name = role.Name } },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("User not found in Keycloak.");
        }

        await EnsureAdminSuccessAsync(response, "assign realm role", cancellationToken);
    }

    public async Task RemoveRealmRoleAsync(
        Guid keycloakUserId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var role = await GetRoleByNameAsync(roleName, cancellationToken)
            ?? throw new NotFoundException($"Role '{roleName}' was not found.");

        using var response = await SendAdminAsync(
            HttpMethod.Delete,
            $"/users/{keycloakUserId}/role-mappings/realm",
            new[] { new { id = role.Id, name = role.Name } },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("User not found in Keycloak.");
        }

        await EnsureAdminSuccessAsync(response, "remove realm role", cancellationToken);
    }

    public async Task<IReadOnlyList<KeycloakRole>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Get,
            "/roles",
            null,
            cancellationToken);

        await EnsureAdminSuccessAsync(response, "list roles", cancellationToken);

        var roles = await response.Content.ReadFromJsonAsync<List<RoleRepresentation>>(
            JsonSerializerOptions.Web,
            cancellationToken);

        return roles?.Select(Map).ToList() ?? new List<KeycloakRole>();
    }

    public async Task<KeycloakRole?> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Get,
            $"/roles-by-id/{roleId}",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureAdminSuccessAsync(response, "get role", cancellationToken);

        var role = await response.Content.ReadFromJsonAsync<RoleRepresentation>(
            JsonSerializerOptions.Web,
            cancellationToken);

        return role is null ? null : Map(role);
    }

    public async Task<KeycloakRole> CreateRoleAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Post,
            "/roles",
            new { name, description },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            await LogAdminFailureAsync(response, "create role", cancellationToken);
            throw new ConflictException($"Role '{name}' already exists.");
        }

        await EnsureAdminSuccessAsync(response, "create role", cancellationToken);

        return await GetRoleByNameAsync(name, cancellationToken)
            ?? throw new KeycloakException(UpstreamErrorMessage);
    }

    public async Task<KeycloakRole> UpdateRoleAsync(
        Guid roleId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (await GetRoleByIdAsync(roleId, cancellationToken) is null)
        {
            throw new NotFoundException("Role not found.");
        }

        using var response = await SendAdminAsync(
            HttpMethod.Put,
            $"/roles-by-id/{roleId}",
            new { id = roleId, name, description },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            await LogAdminFailureAsync(response, "update role", cancellationToken);
            throw new ConflictException($"Role '{name}' already exists.");
        }

        await EnsureAdminSuccessAsync(response, "update role", cancellationToken);

        return new KeycloakRole(roleId, name, description);
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Delete,
            $"/roles-by-id/{roleId}",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Role not found.");
        }

        await EnsureAdminSuccessAsync(response, "delete role", cancellationToken);
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient(KeycloakHttpClient.Name);

    private async Task<HttpResponseMessage> SendTokenRequestAsync(
        Dictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.RealmBaseUrl}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(form)
        };

        return await SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAdminAsync(
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken cancellationToken)
    {
        var accessToken = await _adminTokenProvider.GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            method,
            $"{_options.AdminBaseUrl}{relativePath}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonSerializerOptions.Web);
        }

        return await SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateClient();

            return await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception)
            when (!cancellationToken.IsCancellationRequested
                  && exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "Keycloak could not be reached.");

            throw new KeycloakException(
                "The authentication service could not contact Keycloak. Please try again later.",
                exception);
        }
    }

    private async Task<KeycloakRole?> GetRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        using var response = await SendAdminAsync(
            HttpMethod.Get,
            $"/roles/{Uri.EscapeDataString(roleName)}",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureAdminSuccessAsync(response, "get role by name", cancellationToken);

        var role = await response.Content.ReadFromJsonAsync<RoleRepresentation>(
            JsonSerializerOptions.Web,
            cancellationToken);

        return role is null ? null : Map(role);
    }

    private async Task<KeycloakToken> ReadTokenAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>(
            JsonSerializerOptions.Web,
            cancellationToken) ?? throw new KeycloakException(UpstreamErrorMessage);

        if (string.IsNullOrEmpty(payload.AccessToken))
        {
            throw new KeycloakException(UpstreamErrorMessage);
        }

        return new KeycloakToken(
            payload.AccessToken,
            payload.RefreshToken,
            payload.ExpiresIn,
            payload.RefreshExpiresIn,
            payload.TokenType);
    }

    private async Task<string?> ReadTokenErrorAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning(
            "Keycloak {Operation} failed with {StatusCode}: {Body}",
            operation,
            (int)response.StatusCode,
            body);

        try
        {
            return JsonSerializer.Deserialize<KeycloakErrorResponse>(
                body,
                JsonSerializerOptions.Web)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task EnsureAdminSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        await LogAdminFailureAsync(response, operation, cancellationToken);

        throw new KeycloakException(UpstreamErrorMessage);
    }

    private async Task LogAdminFailureAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning(
            "Keycloak admin operation '{Operation}' failed with {StatusCode}: {Body}",
            operation,
            (int)response.StatusCode,
            body);
    }

    private static KeycloakRole Map(RoleRepresentation role)
        => new(role.Id, role.Name, role.Description);
}
