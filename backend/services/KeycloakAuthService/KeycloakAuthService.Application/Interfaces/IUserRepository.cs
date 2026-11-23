using KeycloakAuthService.Domain.Entities;

namespace KeycloakAuthService.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsForOtherUserAsync(
        string username,
        Guid excludedUserId,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsForOtherUserAsync(
        string email,
        Guid excludedUserId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(User user);

    void Remove(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
