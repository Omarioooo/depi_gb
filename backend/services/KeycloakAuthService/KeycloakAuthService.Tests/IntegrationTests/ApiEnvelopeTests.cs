using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FakeItEasy;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Results;
using KeycloakAuthService.Domain.Common;
using KeycloakAuthService.Domain.Entities;

namespace KeycloakAuthService.Tests.IntegrationTests;

public class ApiEnvelopeTests
{
    [Fact]
    public async Task Register_returns_created_envelope()
    {
        using var factory = new AuthApiFactory();
        A.CallTo(() => factory.Keycloak.CreateUserAsync(
                "new.user", "new@example.com", "Password_123!", A<CancellationToken>._))
            .Returns(Guid.NewGuid());

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "new.user",
            email = "new@example.com",
            password = "Password_123!",
            firstName = "New",
            lastName = "User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.Equal(201, payload.StatusCode);
        Assert.Empty(payload.Errors);
        Assert.Equal("new.user", payload.Data!.Username);
    }

    [Fact]
    public async Task Register_with_invalid_body_returns_validation_envelope()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal(422, payload.StatusCode);
        Assert.NotEmpty(payload.Errors);
    }

    [Fact]
    public async Task Register_trims_username_before_calling_keycloak()
    {
        using var factory = new AuthApiFactory();
        A.CallTo(() => factory.Keycloak.CreateUserAsync(
                "new.user", "new@example.com", "Password_123!", A<CancellationToken>._))
            .Returns(Guid.NewGuid());

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "  new.user  ",
            email = "new@example.com",
            password = "Password_123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.Equal("new.user", payload.Data!.Username);

        A.CallTo(() => factory.Keycloak.CreateUserAsync(
                "new.user", "new@example.com", "Password_123!", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Register_rejects_whitespace_only_username()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "   ",
            email = "new@example.com",
            password = "Password_123!"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal(422, payload.StatusCode);
        Assert.NotEmpty(payload.Errors);

        A.CallTo(() => factory.Keycloak.CreateUserAsync(
                A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Register_rejects_username_shorter_than_minimum_after_trimming()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "  ab  ",
            email = "new@example.com",
            password = "Password_123!"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal(422, payload.StatusCode);
        Assert.Contains(payload.Errors, error => error.Contains("Username"));

        A.CallTo(() => factory.Keycloak.CreateUserAsync(
                A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Admin_endpoint_without_token_returns_401_envelope()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal(401, payload.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoint_without_admin_role_returns_403_envelope()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClientAs(Guid.NewGuid(), DefaultRoles.Staff);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal(403, payload.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoint_with_admin_role_returns_users_with_pagination_meta()
    {
        using var factory = new AuthApiFactory();
        await factory.SeedUserAsync(NewUser("jane"));
        await factory.SeedUserAsync(NewUser("john"));

        var client = factory.CreateClientAs(Guid.NewGuid(), DefaultRoles.SystemAdministrator);

        var response = await client.GetAsync("/api/admin/users?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.NotNull(payload);
        Assert.True(payload!["success"]!.GetValue<bool>());
        Assert.Equal(200, payload["statusCode"]!.GetValue<int>());
        Assert.Single(payload["data"]!.AsArray());

        var meta = payload["meta"]!;
        Assert.Equal(1, meta["page"]!.GetValue<int>());
        Assert.Equal(1, meta["pageSize"]!.GetValue<int>());
        Assert.Equal(2, meta["totalCount"]!.GetValue<int>());
        Assert.Equal(2, meta["totalPages"]!.GetValue<int>());
    }

    [Fact]
    public async Task Me_returns_the_local_profile_and_roles_for_the_token_subject()
    {
        using var factory = new AuthApiFactory();
        var user = NewUser("jane");
        await factory.SeedUserAsync(user);
        A.CallTo(() => factory.Keycloak.GetUserRolesAsync(user.KeycloakId, A<CancellationToken>._))
            .Returns(new List<string> { DefaultRoles.Staff });

        var client = factory.CreateClientAs(user.KeycloakId);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MeResponse>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.Equal(user.Id, payload.Data!.Id);
        Assert.Equal("jane", payload.Data.Username);
        Assert.Contains(DefaultRoles.Staff, payload.Data.Roles);
    }

    [Fact]
    public async Task Unknown_route_returns_404_envelope()
    {
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(
            JsonSerializerOptions.Web);

        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal(404, payload.StatusCode);
    }

    private static User NewUser(string username) => new()
    {
        KeycloakId = Guid.NewGuid(),
        Username = username,
        Email = $"{username}@example.com",
        FirstName = username,
        LastName = "Doe"
    };
}
