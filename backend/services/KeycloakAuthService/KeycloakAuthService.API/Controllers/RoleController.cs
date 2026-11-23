using KeycloakAuthService.Application.DTOs;
using KeycloakAuthService.Application.Results;
using KeycloakAuthService.Application.Services;
using KeycloakAuthService.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakAuthService.API.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = DefaultRoles.SystemAdministrator)]
public class RoleController : ControllerBase
{
    private readonly RoleService _roles;

    public RoleController(RoleService roles)
    {
        _roles = roles;
    }

    [HttpPost("roles")]
    public async Task<IActionResult> Create(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.CreateAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<RoleResponse>.Created(role, "Role created successfully."));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _roles.GetAllAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<IReadOnlyList<RoleResponse>>.Ok(roles, "Roles retrieved successfully."));
    }

    [HttpGet("roles/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(id, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<RoleResponse>.Ok(role, "Role retrieved successfully."));
    }

    [HttpPut("roles/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.UpdateAsync(id, request, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<RoleResponse>.Ok(role, "Role updated successfully."));
    }

    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roles.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpGet("users/{userId:guid}/roles")]
    public async Task<IActionResult> GetUserRoles(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await _roles.GetUserRolesAsync(userId, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<IReadOnlyList<string>>.Ok(roles, "User roles retrieved successfully."));
    }

    [HttpPost("users/{userId:guid}/roles/{roleName}")]
    public async Task<IActionResult> AssignUserRole(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        await _roles.AssignAsync(userId, roleName, cancellationToken);

        return StatusCode(
            StatusCodes.Status200OK,
            ApiResponse<object>.Ok(new { role = roleName }, "Role assigned successfully."));
    }

    [HttpDelete("users/{userId:guid}/roles/{roleName}")]
    public async Task<IActionResult> RemoveUserRole(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        await _roles.RemoveAsync(userId, roleName, cancellationToken);

        return NoContent();
    }
}
