using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Domain.Common;
using KeycloakAuthService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KeycloakAuthService.Application.Services;

public class UserService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly IUserRepository _users;
    private readonly IKeycloakService _keycloak;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository users,
        IKeycloakService keycloak,
        ILogger<UserService> logger)
    {
        _users = users;
        _keycloak = keycloak;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await _users.UsernameExistsAsync(username, cancellationToken))
        {
            throw new ConflictException("Username is already taken.");
        }

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("Email is already registered.");
        }

        var keycloakId = await _keycloak.CreateUserAsync(
            username,
            email,
            request.Password,
            cancellationToken);

        try
        {
            await _keycloak.AssignRealmRoleAsync(keycloakId, DefaultRoles.Staff, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RollbackKeycloakUserAsync(keycloakId, "assign the default role", cancellationToken);

            throw new KeycloakException(
                "The default role could not be assigned, so the registration was cancelled. " +
                "Please contact an administrator.",
                exception);
        }

        var user = new User
        {
            KeycloakId = keycloakId,
            Username = username,
            Email = email,
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty
        };

        try
        {
            _users.Add(user);
            await _users.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RollbackKeycloakUserAsync(keycloakId, "save the local user", cancellationToken);

            throw new AppException(
                "The user could not be created. Please try again.",
                500,
                exception);
        }

        return UserResponse.From(user);
    }

    public async Task<PagedUsersResult> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var (items, totalCount) = await _users.GetPagedAsync(page, pageSize, cancellationToken);

        return new PagedUsersResult
        {
            Page = page,
            PageSize = pageSize,
            Items = items.Select(UserResponse.From).ToList(),
            TotalCount = totalCount
        };
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return UserResponse.From(user);
    }

    public async Task<UserResponse> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await _users.UsernameExistsForOtherUserAsync(username, id, cancellationToken))
        {
            throw new ConflictException("Username is already taken.");
        }

        if (await _users.EmailExistsForOtherUserAsync(email, id, cancellationToken))
        {
            throw new ConflictException("Email is already registered.");
        }

        await _keycloak.UpdateUserAsync(user.KeycloakId, username, email, cancellationToken);

        user.Username = username;
        user.Email = email;
        user.FirstName = request.FirstName?.Trim() ?? string.Empty;
        user.LastName = request.LastName?.Trim() ?? string.Empty;

        await _users.SaveChangesAsync(cancellationToken);

        return UserResponse.From(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        // Delete in Keycloak first: a local user without a Keycloak account cannot log in.
        await _keycloak.DeleteUserAsync(user.KeycloakId, cancellationToken);

        _users.Remove(user);
        await _users.SaveChangesAsync(cancellationToken);
    }

    private async Task RollbackKeycloakUserAsync(
        Guid keycloakId,
        string failedStep,
        CancellationToken cancellationToken)
    {
        try
        {
            await _keycloak.DeleteUserAsync(keycloakId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not delete Keycloak user {KeycloakId} after failing to {FailedStep}. " +
                "The Keycloak user must be removed manually.",
                keycloakId,
                failedStep);
        }
    }
}
