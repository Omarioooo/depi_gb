using FakeItEasy;
using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Application.Models;
using KeycloakAuthService.Application.Services;
using KeycloakAuthService.Domain.Entities;

namespace KeycloakAuthService.Tests.UnitTests;

public class AuthenticationServiceTests
{
    private readonly IKeycloakService _keycloak = A.Fake<IKeycloakService>();
    private readonly IUserRepository _users = A.Fake<IUserRepository>();
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _service = new AuthenticationService(_keycloak, _users);
    }

    [Fact]
    public async Task LoginAsync_returns_tokens_from_keycloak()
    {
        A.CallTo(() => _keycloak.LoginAsync("jane", "Password_123!", A<CancellationToken>._))
            .Returns(NewToken());

        var result = await _service.LoginAsync(new LoginRequest
        {
            Username = " jane ",
            Password = "Password_123!"
        });

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(300, result.ExpiresIn);
        Assert.Equal("Bearer", result.TokenType);
    }

    [Fact]
    public async Task LoginAsync_propagates_invalid_credentials_error()
    {
        A.CallTo(() => _keycloak.LoginAsync("jane", "wrong", A<CancellationToken>._))
            .ThrowsAsync(new UnauthorizedException("Invalid username or password."));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(new LoginRequest
        {
            Username = "jane",
            Password = "wrong"
        }));
    }

    [Fact]
    public async Task RefreshTokenAsync_returns_new_tokens()
    {
        A.CallTo(() => _keycloak.RefreshTokenAsync("refresh-token", A<CancellationToken>._))
            .Returns(NewToken());

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = "refresh-token"
        });

        Assert.Equal("access-token", result.AccessToken);
    }

    [Fact]
    public async Task LogoutAsync_revokes_the_refresh_token_in_keycloak()
    {
        await _service.LogoutAsync(new LogoutRequest { RefreshToken = "refresh-token" });

        A.CallTo(() => _keycloak.LogoutAsync("refresh-token", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetMeAsync_returns_local_profile_with_keycloak_roles()
    {
        var user = NewUser();
        A.CallTo(() => _users.GetByKeycloakIdAsync(user.KeycloakId, A<CancellationToken>._))
            .Returns(user);
        A.CallTo(() => _keycloak.GetUserRolesAsync(user.KeycloakId, A<CancellationToken>._))
            .Returns(new List<string> { "Staff", "Manager" });

        var result = await _service.GetMeAsync(user.KeycloakId);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("jane", result.Username);
        Assert.Equal(new[] { "Staff", "Manager" }, result.Roles);
    }

    [Fact]
    public async Task GetMeAsync_throws_not_found_when_local_user_is_missing()
    {
        A.CallTo(() => _users.GetByKeycloakIdAsync(A<Guid>._, A<CancellationToken>._))
            .Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetMeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ChangePasswordAsync_rejects_a_wrong_current_password()
    {
        var user = NewUser();
        A.CallTo(() => _users.GetByKeycloakIdAsync(user.KeycloakId, A<CancellationToken>._))
            .Returns(user);
        A.CallTo(() => _keycloak.VerifyPasswordAsync("jane", "wrong", A<CancellationToken>._))
            .Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.ChangePasswordAsync(
            user.KeycloakId,
            new ChangePasswordRequest
            {
                CurrentPassword = "wrong",
                NewPassword = "NewPassword_123!"
            }));

        A.CallTo(() => _keycloak.ChangePasswordAsync(
                A<Guid>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ChangePasswordAsync_updates_the_password_after_verification()
    {
        var user = NewUser();
        A.CallTo(() => _users.GetByKeycloakIdAsync(user.KeycloakId, A<CancellationToken>._))
            .Returns(user);
        A.CallTo(() => _keycloak.VerifyPasswordAsync("jane", "Password_123!", A<CancellationToken>._))
            .Returns(true);

        await _service.ChangePasswordAsync(
            user.KeycloakId,
            new ChangePasswordRequest
            {
                CurrentPassword = "Password_123!",
                NewPassword = "NewPassword_123!"
            });

        A.CallTo(() => _keycloak.ChangePasswordAsync(
                user.KeycloakId, "NewPassword_123!", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private static KeycloakToken NewToken() => new(
        "access-token",
        "refresh-token",
        300,
        1800,
        "Bearer");

    private static User NewUser() => new()
    {
        KeycloakId = Guid.NewGuid(),
        Username = "jane",
        Email = "jane@example.com",
        FirstName = "Jane",
        LastName = "Doe"
    };
}
