namespace KeycloakAuthService.Application.Results;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }
    public object? Meta { get; set; }

    public static ApiResponse<T> Ok(
        T data,
        string message = "Success",
        object? meta = null)
        => new()
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200,
            Meta = meta
        };

    public static ApiResponse<T> Created(
        T data,
        string message = "Created successfully")
        => new()
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 201
        };

    public static ApiResponse<T> Fail(
        string message,
        List<string>? errors = null,
        int statusCode = 400)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new(),
            StatusCode = statusCode
        };

    public static ApiResponse<T> Unauthorized(
        string message = "Unauthorized")
        => new()
        {
            Success = false,
            Message = message,
            StatusCode = 401
        };

    public static ApiResponse<T> Forbidden(
        string message = "Forbidden")
        => new()
        {
            Success = false,
            Message = message,
            StatusCode = 403
        };

    public static ApiResponse<T> NotFound(
        string message = "Not found")
        => new()
        {
            Success = false,
            Message = message,
            StatusCode = 404
        };

    public static ApiResponse<T> Conflict(
        string message = "Conflict")
        => new()
        {
            Success = false,
            Message = message,
            StatusCode = 409
        };

    public static ApiResponse<T> ValidationError(
        List<string> errors,
        string message = "Validation failed")
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors,
            StatusCode = 422
        };

    public static ApiResponse<T> TooManyRequests(
        string message = "Too many requests")
        => new()
        {
            Success = false,
            Message = message,
            StatusCode = 429
        };

    public static ApiResponse<T> InternalServerError(
        string message = "An unexpected error occurred")
        => new()
        {
            Success = false,
            Message = message,
            StatusCode = 500
        };

    public static ApiResponse<T> BadGateway(
        string message = "Upstream service error")
        => new()
        {
            Success = false,
            Message = message,
            StatusCode = 502
        };
}
