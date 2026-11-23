ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

WORKDIR /src

# Restore first so dependency layers are cached when only source changes.
COPY ["KeycloakAuthService.API/KeycloakAuthService.API.csproj", "KeycloakAuthService.API/"]
COPY ["KeycloakAuthService.Application/KeycloakAuthService.Application.csproj", "KeycloakAuthService.Application/"]
COPY ["KeycloakAuthService.Domain/KeycloakAuthService.Domain.csproj", "KeycloakAuthService.Domain/"]
COPY ["KeycloakAuthService.Infrastructure/KeycloakAuthService.Infrastructure.csproj", "KeycloakAuthService.Infrastructure/"]

RUN dotnet restore "KeycloakAuthService.API/KeycloakAuthService.API.csproj"

COPY . .

RUN dotnet publish "KeycloakAuthService.API/KeycloakAuthService.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "KeycloakAuthService.API.dll"]
