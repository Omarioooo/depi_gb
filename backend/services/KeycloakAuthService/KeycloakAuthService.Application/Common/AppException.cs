namespace KeycloakAuthService.Application.Common;

public class AppException : Exception
{
    public AppException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public AppException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message, 400)
    {
    }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message, 401)
    {
    }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden") : base(message, 403)
    {
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message = "Not found") : base(message, 404)
    {
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message = "Conflict") : base(message, 409)
    {
    }
}

public sealed class KeycloakException : AppException
{
    public KeycloakException(string message = "Keycloak request failed") : base(message, 502)
    {
    }

    public KeycloakException(string message, Exception innerException) : base(message, 502, innerException)
    {
    }
}
