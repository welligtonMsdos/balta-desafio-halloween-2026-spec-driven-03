FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["LinkShortener.sln", "./"]
COPY ["src/LinkShortener.Domain/LinkShortener.Domain.csproj", "src/LinkShortener.Domain/"]
COPY ["src/LinkShortener.Application/LinkShortener.Application.csproj", "src/LinkShortener.Application/"]
COPY ["src/LinkShortener.Infrastructure/LinkShortener.Infrastructure.csproj", "src/LinkShortener.Infrastructure/"]
COPY ["src/LinkShortener.Api/LinkShortener.Api.csproj", "src/LinkShortener.Api/"]
COPY ["tests/LinkShortener.UnitTests/LinkShortener.UnitTests.csproj", "tests/LinkShortener.UnitTests/"]
COPY ["tests/LinkShortener.IntegrationTests/LinkShortener.IntegrationTests.csproj", "tests/LinkShortener.IntegrationTests/"]
RUN dotnet restore "LinkShortener.sln"

COPY . .
WORKDIR /src/src/LinkShortener.Api
RUN dotnet publish "LinkShortener.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkShortener.Api.dll"]
