using System.Security.Claims;
using System.Text.Json;
using KeycloakAuthService.API.Auth;
using KeycloakAuthService.API.Middleware;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Application.Results;
using KeycloakAuthService.Application.Services;
using KeycloakAuthService.Infrastructure.Keycloak;
using KeycloakAuthService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? $"Invalid value for '{entry.Key}'."
                    : error.ErrorMessage))
            .ToList();

        var response = ApiResponse<object>.ValidationError(errors);

        return new ObjectResult(response) { StatusCode = response.StatusCode };
    };
});

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AuthDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("AuthDb");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'AuthDb' is not configured. " +
            "Set the ConnectionStrings__AuthDb environment variable or use user secrets.");
    }

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<RoleService>();

// ---------------------------------------------------------------- Keycloak

var keycloakSection = builder.Configuration.GetSection(KeycloakOptions.SectionName);
var keycloakOptions = keycloakSection.Get<KeycloakOptions>() ?? new KeycloakOptions();

builder.Services
    .AddOptions<KeycloakOptions>()
    .Bind(keycloakSection)
    .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl),
        "Keycloak:BaseUrl is required. Set Keycloak__BaseUrl (for example http://localhost:6666).")
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "Keycloak:BaseUrl must be an absolute URL.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Realm),
        "Keycloak:Realm is required. Set Keycloak__Realm.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId),
        "Keycloak:ClientId is required. Set Keycloak__ClientId.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientSecret),
        "Keycloak:ClientSecret is required. Set Keycloak__ClientSecret or a user secret.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.AdminClientId),
        "Keycloak:AdminClientId is required. Set Keycloak__AdminClientId.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.AdminClientSecret),
        "Keycloak:AdminClientSecret is required. Set Keycloak__AdminClientSecret or a user secret.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
        "Keycloak:Audience is required. Set Keycloak__Audience.")
    .Validate(options => options.AdminTokenRefreshMarginSeconds >= 0,
        "Keycloak:AdminTokenRefreshMarginSeconds must be zero or greater.")
    .ValidateOnStart();
builder.Services.AddHttpClient(KeycloakHttpClient.Name);
builder.Services.AddSingleton<IKeycloakAdminTokenProvider, KeycloakAdminTokenProvider>();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();

// ---------------------------------------------------------------- JWT Bearer

var metadataAddress = $"{keycloakOptions.RealmBaseUrl}/.well-known/openid-configuration";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakOptions.RealmBaseUrl;
        options.MetadataAddress = metadataAddress;
        options.Audience = keycloakOptions.Audience;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakOptions.EffectiveIssuer,
            ValidateAudience = true,
            ValidAudience = keycloakOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                KeycloakClaims.AddRealmRoles(context.Principal);
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();

                await WriteErrorAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    ApiResponse<object>.Unauthorized("Authentication is required."));
            },
            OnForbidden = context => WriteErrorAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                ApiResponse<object>.Forbidden("You do not have permission to access this resource."))
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------- Pipeline

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;

    var body = response.StatusCode switch
    {
        StatusCodes.Status404NotFound =>
            ApiResponse<object>.NotFound("The requested resource was not found."),
        StatusCodes.Status405MethodNotAllowed =>
            ApiResponse<object>.Fail(
                "The HTTP method is not allowed for this resource.",
                statusCode: StatusCodes.Status405MethodNotAllowed),
        _ => null
    };

    if (body is null)
    {
        return;
    }

    response.ContentType = "application/json";
    await response.WriteAsJsonAsync(body, JsonSerializerOptions.Web);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task WriteErrorAsync(HttpContext context, int statusCode, ApiResponse<object> response)
{
    if (context.Response.HasStarted)
    {
        return;
    }

    context.Response.Clear();
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";

    await context.Response.WriteAsJsonAsync(response, JsonSerializerOptions.Web);
}

public partial class Program;
