using KeycloakAuthService.Application.Models;

namespace KeycloakAuthService.Application.Interfaces;

public interface IKeycloakService
{
    Task<KeycloakToken> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<KeycloakToken> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<bool> VerifyPasswordAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid keycloakUserId,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateUserAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task UpdateUserAsync(
        Guid keycloakUserId,
        string username,
        string email,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(Guid keycloakUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUserRolesAsync(
        Guid keycloakUserId,
        CancellationToken cancellationToken = default);

    Task AssignRealmRoleAsync(
        Guid keycloakUserId,
        string roleName,
        CancellationToken cancellationToken = default);

    Task RemoveRealmRoleAsync(
        Guid keycloakUserId,
        string roleName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KeycloakRole>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<KeycloakRole?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<KeycloakRole> CreateRoleAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default);

    Task<KeycloakRole> UpdateRoleAsync(
        Guid roleId,
        string name,
        string? description,
        CancellationToken cancellationToken = default);

    Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
