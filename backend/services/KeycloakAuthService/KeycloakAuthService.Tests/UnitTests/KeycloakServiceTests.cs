using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using FakeItEasy;
using KeycloakAuthService.Infrastructure.Keycloak;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KeycloakAuthService.Tests.UnitTests;

public class KeycloakServiceTests
{
    [Fact]
    public async Task UpdateUserAsync_preserves_first_name_last_name_and_other_fields()
    {
        var keycloakId = Guid.NewGuid();
        var handler = new RecordingKeycloakHandler(keycloakId);
        var service = CreateService(handler);

        await service.UpdateUserAsync(keycloakId, "jane.doe", "jane.doe@example.com");

        Assert.Equal(1, handler.GetRequests);
        Assert.Equal(1, handler.PutRequests);

        var body = Assert.IsType<JsonObject>(handler.PutBody);

        Assert.Equal("jane.doe", body["username"]!.GetValue<string>());
        Assert.Equal("jane.doe@example.com", body["email"]!.GetValue<string>());
        Assert.True(body["emailVerified"]!.GetValue<bool>());

        // Fields owned by other operations must survive the update.
        Assert.Equal("Jane", body["firstName"]!.GetValue<string>());
        Assert.Equal("Doe", body["lastName"]!.GetValue<string>());
        Assert.True(body["enabled"]!.GetValue<bool>());

        var attributes = body["attributes"]!.AsObject();
        Assert.Equal("ops", attributes["department"]!.AsArray()[0]!.GetValue<string>());
    }

    private static KeycloakService CreateService(HttpMessageHandler handler)
    {
        var tokenProvider = A.Fake<IKeycloakAdminTokenProvider>();
        A.CallTo(() => tokenProvider.GetAccessTokenAsync(A<CancellationToken>._))
            .Returns("test-admin-token");

        var options = Options.Create(new KeycloakOptions
        {
            BaseUrl = "http://keycloak.test",
            Realm = "test-realm",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            AdminClientId = "test-admin",
            AdminClientSecret = "test-admin-secret",
            Audience = "test-audience"
        });

        return new KeycloakService(
            new StubHttpClientFactory(handler),
            tokenProvider,
            options,
            NullLogger<KeycloakService>.Instance);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpMessageHandler handler)
            => _client = new HttpClient(handler);

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class RecordingKeycloakHandler : HttpMessageHandler
    {
        private readonly Guid _keycloakId;

        public RecordingKeycloakHandler(Guid keycloakId) => _keycloakId = keycloakId;

        public JsonObject? PutBody { get; private set; }

        public int GetRequests { get; private set; }

        public int PutRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                GetRequests++;

                var user = new JsonObject
                {
                    ["id"] = _keycloakId.ToString(),
                    ["username"] = "old.username",
                    ["email"] = "old@example.com",
                    ["firstName"] = "Jane",
                    ["lastName"] = "Doe",
                    ["emailVerified"] = false,
                    ["enabled"] = true,
                    ["attributes"] = new JsonObject
                    {
                        ["department"] = new JsonArray("ops")
                    }
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        user.ToJsonString(),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            if (request.Method == HttpMethod.Put)
            {
                PutRequests++;

                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                PutBody = JsonNode.Parse(body)!.AsObject();

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }
    }
}
