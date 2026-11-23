using System.ComponentModel.DataAnnotations;

namespace KeycloakAuthService.Application.DTOs;

public class RegisterRequest : IValidatableObject
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // [Required] already rejects null, empty and whitespace-only values.
        if (string.IsNullOrWhiteSpace(Username))
        {
            yield break;
        }

        // Apply the length rules to the value the service will actually send to Keycloak.
        var trimmedLength = Username.Trim().Length;

        if (trimmedLength < 3 || trimmedLength > 100)
        {
            yield return new ValidationResult(
                "Username must be between 3 and 100 characters.",
                new[] { nameof(Username) });
        }
    }
}

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public int RefreshExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
}

public class MeResponse
{
    public Guid Id { get; set; }
    public Guid KeycloakId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
