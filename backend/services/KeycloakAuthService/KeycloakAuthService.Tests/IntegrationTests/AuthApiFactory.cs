using FakeItEasy;
using KeycloakAuthService.Application.Interfaces;
using KeycloakAuthService.Domain.Entities;
using KeycloakAuthService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KeycloakAuthService.Tests.IntegrationTests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"auth-tests-{Guid.NewGuid():N}";

    public IKeycloakService Keycloak { get; } = A.Fake<IKeycloakService>();

    public HttpClient CreateClientAs(Guid? keycloakId = null, params string[] roles)
    {
        var client = CreateClient();

        if (keycloakId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-User", keycloakId.Value.ToString());
        }

        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(',', roles));
        }

        return client;
    }

    public async Task SeedUserAsync(User user)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] = "Server=unused;Database=unused;",
                ["Keycloak:BaseUrl"] = "http://keycloak.test",
                ["Keycloak:Realm"] = "test-realm",
                ["Keycloak:ClientId"] = "test-client",
                ["Keycloak:ClientSecret"] = "test-secret",
                ["Keycloak:AdminClientId"] = "test-admin",
                ["Keycloak:AdminClientSecret"] = "test-admin-secret",
                ["Keycloak:Audience"] = "account"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace the real SQL Server context and Keycloak client with test doubles.
            services.RemoveAll<IDbContextOptionsConfiguration<AuthDbContext>>();
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AuthDbContext>();
            services.AddDbContext<AuthDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IKeycloakService>();
            services.AddSingleton(Keycloak);

            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}
