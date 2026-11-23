using FakeItEasy;
using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Application.Services;
using KeycloakAuthService.Domain.Common;
using KeycloakAuthService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KeycloakAuthService.Tests.UnitTests;

public class UserServiceTests
{
    private readonly IUserRepository _users = A.Fake<IUserRepository>();
    private readonly IKeycloakService _keycloak = A.Fake<IKeycloakService>();
    private readonly UserService _service;

    public UserServiceTests()
    {
        var logger = A.Fake<ILogger<UserService>>();
        _service = new UserService(_users, _keycloak, logger);
    }

    [Fact]
    public async Task RegisterAsync_creates_keycloak_user_assigns_staff_and_saves_locally()
    {
        var keycloakId = Guid.NewGuid();
        A.CallTo(() => _keycloak.CreateUserAsync(
                "jane", "jane@example.com", "Password_123!", A<CancellationToken>._))
            .Returns(keycloakId);

        var result = await _service.RegisterAsync(NewRegisterRequest());

        Assert.Equal("jane", result.Username);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Equal("Jane", result.FirstName);

        A.CallTo(() => _keycloak.AssignRealmRoleAsync(
                keycloakId, DefaultRoles.Staff, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _users.Add(A<User>.That.Matches(
                user => user.KeycloakId == keycloakId && user.Username == "jane")))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _users.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RegisterAsync_throws_conflict_when_username_already_exists()
    {
        A.CallTo(() => _users.UsernameExistsAsync("jane", A<CancellationToken>._))
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(NewRegisterRequest()));

        A.CallTo(() => _keycloak.CreateUserAsync(
                A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RegisterAsync_throws_conflict_when_email_already_exists()
    {
        A.CallTo(() => _users.EmailExistsAsync("jane@example.com", A<CancellationToken>._))
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(NewRegisterRequest()));

        A.CallTo(() => _keycloak.CreateUserAsync(
                A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RegisterAsync_deletes_keycloak_user_when_default_role_assignment_fails()
    {
        var keycloakId = Guid.NewGuid();
        A.CallTo(() => _keycloak.CreateUserAsync(
                A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(keycloakId);
        A.CallTo(() => _keycloak.AssignRealmRoleAsync(
                keycloakId, DefaultRoles.Staff, A<CancellationToken>._))
            .ThrowsAsync(new KeycloakException("Role is missing."));

        await Assert.ThrowsAsync<KeycloakException>(() => _service.RegisterAsync(NewRegisterRequest()));

        A.CallTo(() => _keycloak.DeleteUserAsync(keycloakId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _users.Add(A<User>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task RegisterAsync_deletes_keycloak_user_when_local_save_fails()
    {
        var keycloakId = Guid.NewGuid();
        A.CallTo(() => _keycloak.CreateUserAsync(
                A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(keycloakId);
        A.CallTo(() => _users.SaveChangesAsync(A<CancellationToken>._))
            .ThrowsAsync(new InvalidOperationException("Database is down."));

        await Assert.ThrowsAsync<AppException>(() => _service.RegisterAsync(NewRegisterRequest()));

        A.CallTo(() => _keycloak.DeleteUserAsync(keycloakId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetPagedAsync_returns_page_metadata()
    {
        var users = new List<User> { NewUser("jane") };
        A.CallTo(() => _users.GetPagedAsync(2, 10, A<CancellationToken>._))
            .Returns(Task.FromResult<(IReadOnlyList<User> Items, int TotalCount)>((users, 25)));

        var result = await _service.GetPagedAsync(2, 10);

        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPagedAsync_clamps_invalid_paging_values()
    {
        A.CallTo(() => _users.GetPagedAsync(1, 10, A<CancellationToken>._))
            .Returns(Task.FromResult<(IReadOnlyList<User> Items, int TotalCount)>(
                (Array.Empty<User>(), 0)));

        var result = await _service.GetPagedAsync(0, 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetByIdAsync_throws_not_found_when_user_does_not_exist()
    {
        A.CallTo(() => _users.GetByIdAsync(A<Guid>._, A<CancellationToken>._))
            .Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_updates_keycloak_before_local_user()
    {
        var user = NewUser("jane");
        A.CallTo(() => _users.GetByIdAsync(user.Id, A<CancellationToken>._)).Returns(user);

        var request = new UpdateUserRequest
        {
            Username = "jane.updated",
            Email = "jane.updated@example.com",
            FirstName = "Jane",
            LastName = "Doe"
        };

        var result = await _service.UpdateAsync(user.Id, request);

        A.CallTo(() => _keycloak.UpdateUserAsync(
                user.KeycloakId, "jane.updated", "jane.updated@example.com", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _users.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        Assert.Equal("jane.updated", result.Username);
    }

    [Fact]
    public async Task UpdateAsync_throws_conflict_when_username_belongs_to_another_user()
    {
        var user = NewUser("jane");
        A.CallTo(() => _users.GetByIdAsync(user.Id, A<CancellationToken>._)).Returns(user);
        A.CallTo(() => _users.UsernameExistsForOtherUserAsync(
                "taken", user.Id, A<CancellationToken>._))
            .Returns(true);

        var request = new UpdateUserRequest
        {
            Username = "taken",
            Email = "jane@example.com"
        };

        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(user.Id, request));

        A.CallTo(() => _keycloak.UpdateUserAsync(
                A<Guid>._, A<string>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task DeleteAsync_deletes_keycloak_user_then_local_user()
    {
        var user = NewUser("jane");
        A.CallTo(() => _users.GetByIdAsync(user.Id, A<CancellationToken>._)).Returns(user);

        await _service.DeleteAsync(user.Id);

        A.CallTo(() => _keycloak.DeleteUserAsync(user.KeycloakId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _users.Remove(user)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _users.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private static RegisterRequest NewRegisterRequest() => new()
    {
        Username = "jane",
        Email = "jane@example.com",
        Password = "Password_123!",
        FirstName = "Jane",
        LastName = "Doe"
    };

    private static User NewUser(string username) => new()
    {
        KeycloakId = Guid.NewGuid(),
        Username = username,
        Email = $"{username}@example.com",
        FirstName = "Jane",
        LastName = "Doe"
    };
}
