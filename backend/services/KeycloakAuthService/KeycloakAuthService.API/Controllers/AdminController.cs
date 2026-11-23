using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Results;
using KeycloakAuthService.Application.Services;
using KeycloakAuthService.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakAuthService.API.Controllers;

[ApiController]
[Route("api/[controller]/users")]
[Authorize(Roles = DefaultRoles.SystemAdministrator)]
public class AdminController : ControllerBase
{
    private readonly UserService _users;

    public AdminController(UserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _users.GetPagedAsync(page, pageSize, cancellationToken);

        var meta = new PaginationMeta
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        };

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<IReadOnlyList<UserResponse>>.Ok(
                result.Items,
                "Users retrieved successfully.",
                meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<UserResponse>.Ok(user, "User retrieved successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.UpdateAsync(id, request, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<UserResponse>.Ok(user, "User updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await _users.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
