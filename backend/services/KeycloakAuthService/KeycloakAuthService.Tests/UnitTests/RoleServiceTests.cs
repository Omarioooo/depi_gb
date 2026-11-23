using FakeItEasy;
using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Application.Models;
using KeycloakAuthService.Application.Services;
using KeycloakAuthService.Domain.Entities;

namespace KeycloakAuthService.Tests.UnitTests;

public class RoleServiceTests
{
    private readonly IKeycloakService _keycloak = A.Fake<IKeycloakService>();
    private readonly IUserRepository _users = A.Fake<IUserRepository>();
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        _service = new RoleService(_keycloak, _users);
    }

    [Fact]
    public async Task CreateAsync_returns_the_role_created_in_keycloak()
    {
        var role = new KeycloakRole(Guid.NewGuid(), "Warehouse Clerk", "Handles stock");
        A.CallTo(() => _keycloak.CreateRoleAsync("Warehouse Clerk", "Handles stock", A<CancellationToken>._))
            .Returns(role);

        var result = await _service.CreateAsync(new CreateRoleRequest
        {
            Name = " Warehouse Clerk ",
            Description = "Handles stock"
        });

        Assert.Equal(role.Id, result.Id);
        Assert.Equal("Warehouse Clerk", result.Name);
    }

    [Fact]
    public async Task GetAllAsync_maps_keycloak_roles()
    {
        A.CallTo(() => _keycloak.GetRolesAsync(A<CancellationToken>._))
            .Returns(new List<KeycloakRole>
            {
                new(Guid.NewGuid(), "Manager", null),
                new(Guid.NewGuid(), "Staff", "Default role")
            });

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Manager", result[0].Name);
        Assert.Equal("Default role", result[1].Description);
    }

    [Fact]
    public async Task GetByIdAsync_throws_not_found_when_role_is_missing()
    {
        A.CallTo(() => _keycloak.GetRoleByIdAsync(A<Guid>._, A<CancellationToken>._))
            .Returns((KeycloakRole?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_deletes_the_role_in_keycloak()
    {
        var roleId = Guid.NewGuid();

        await _service.DeleteAsync(roleId);

        A.CallTo(() => _keycloak.DeleteRoleAsync(roleId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AssignAsync_assigns_the_role_to_the_users_keycloak_id()
    {
        var user = NewUser();
        A.CallTo(() => _users.GetByIdAsync(user.Id, A<CancellationToken>._)).Returns(user);

        await _service.AssignAsync(user.Id, "Manager");

        A.CallTo(() => _keycloak.AssignRealmRoleAsync(
                user.KeycloakId, "Manager", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AssignAsync_throws_not_found_when_local_user_is_missing()
    {
        A.CallTo(() => _users.GetByIdAsync(A<Guid>._, A<CancellationToken>._))
            .Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AssignAsync(Guid.NewGuid(), "Manager"));

        A.CallTo(() => _keycloak.AssignRealmRoleAsync(
                A<Guid>._, A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RemoveAsync_removes_the_role_from_the_users_keycloak_id()
    {
        var user = NewUser();
        A.CallTo(() => _users.GetByIdAsync(user.Id, A<CancellationToken>._)).Returns(user);

        await _service.RemoveAsync(user.Id, "Manager");

        A.CallTo(() => _keycloak.RemoveRealmRoleAsync(
                user.KeycloakId, "Manager", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserRolesAsync_returns_the_keycloak_realm_roles()
    {
        var user = NewUser();
        A.CallTo(() => _users.GetByIdAsync(user.Id, A<CancellationToken>._)).Returns(user);
        A.CallTo(() => _keycloak.GetUserRolesAsync(user.KeycloakId, A<CancellationToken>._))
            .Returns(new List<string> { "Staff" });

        var result = await _service.GetUserRolesAsync(user.Id);

        Assert.Equal(new[] { "Staff" }, result);
    }

    private static User NewUser() => new()
    {
        KeycloakId = Guid.NewGuid(),
        Username = "jane",
        Email = "jane@example.com",
        FirstName = "Jane",
        LastName = "Doe"
    };
}
