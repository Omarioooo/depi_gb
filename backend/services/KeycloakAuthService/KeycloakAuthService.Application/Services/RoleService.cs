using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Application.Models;
using KeycloakAuthService.Domain.Entities;

namespace KeycloakAuthService.Application.Services;

public class RoleService
{
    private readonly IKeycloakService _keycloak;
    private readonly IUserRepository _users;

    public RoleService(IKeycloakService keycloak, IUserRepository users)
    {
        _keycloak = keycloak;
        _users = users;
    }

    public async Task<RoleResponse> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await _keycloak.CreateRoleAsync(
            request.Name.Trim(),
            request.Description,
            cancellationToken);

        return Map(role);
    }

    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _keycloak.GetRolesAsync(cancellationToken);

        return roles.Select(Map).ToList();
    }

    public async Task<RoleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _keycloak.GetRoleByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        return Map(role);
    }

    public async Task<RoleResponse> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await _keycloak.UpdateRoleAsync(
            id,
            request.Name.Trim(),
            request.Description,
            cancellationToken);

        return Map(role);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _keycloak.DeleteRoleAsync(id, cancellationToken);

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        return await _keycloak.GetUserRolesAsync(user.KeycloakId, cancellationToken);
    }

    public async Task AssignAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        await _keycloak.AssignRealmRoleAsync(user.KeycloakId, roleName, cancellationToken);
    }

    public async Task RemoveAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        await _keycloak.RemoveRealmRoleAsync(user.KeycloakId, roleName, cancellationToken);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        => await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

    private static RoleResponse Map(KeycloakRole role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description
    };
}
