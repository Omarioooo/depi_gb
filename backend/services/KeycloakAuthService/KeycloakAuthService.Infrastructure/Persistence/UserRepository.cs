using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KeycloakAuthService.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _dbContext;

    public UserRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.KeycloakId == keycloakId, cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
        => _dbContext.Users.AnyAsync(x => x.Username == username, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);

    public Task<bool> UsernameExistsForOtherUserAsync(
        string username,
        Guid excludedUserId,
        CancellationToken cancellationToken = default)
        => _dbContext.Users.AnyAsync(
            x => x.Username == username && x.Id != excludedUserId,
            cancellationToken);

    public Task<bool> EmailExistsForOtherUserAsync(
        string email,
        Guid excludedUserId,
        CancellationToken cancellationToken = default)
        => _dbContext.Users.AnyAsync(
            x => x.Email == email && x.Id != excludedUserId,
            cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking().OrderBy(x => x.Username);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(User user) => _dbContext.Users.Add(user);

    public void Remove(User user) => _dbContext.Users.Remove(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
