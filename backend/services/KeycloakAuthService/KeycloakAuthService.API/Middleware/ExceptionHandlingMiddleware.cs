using System.Text.Json;
using KeycloakAuthService.Application.Common;
using KeycloakAuthService.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakAuthService.API.Middleware;

/// <summary>
/// Converts every unhandled exception into the shared API response envelope.
/// Internal details are logged, never returned to the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException exception)
        {
            _logger.LogWarning(
                exception,
                "Request {Method} {Path} failed with a handled error: {Message}",
                context.Request.Method,
                context.Request.Path,
                exception.Message);

            await WriteAsync(
                context,
                exception.StatusCode,
                ApiResponse<object>.Fail(exception.Message, statusCode: exception.StatusCode));
        }
        catch (Exception exception)
            when (!context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ApiResponse<object>.InternalServerError());
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        ApiResponse<object> response)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            response,
            JsonSerializerOptions.Web,
            context.RequestAborted);
    }
}
