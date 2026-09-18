# Build and publish the LedgerLoop API. Context is the repository root.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY LedgerLoop.sln ./
COPY src/LedgerLoop.Api/LedgerLoop.Api.csproj src/LedgerLoop.Api/
COPY tests/LedgerLoop.UnitTests/LedgerLoop.UnitTests.csproj tests/LedgerLoop.UnitTests/
COPY tests/LedgerLoop.IntegrationTests/LedgerLoop.IntegrationTests.csproj tests/LedgerLoop.IntegrationTests/
RUN dotnet restore src/LedgerLoop.Api/LedgerLoop.Api.csproj

COPY src/LedgerLoop.Api/ src/LedgerLoop.Api/
RUN dotnet publish src/LedgerLoop.Api/LedgerLoop.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "LedgerLoop.Api.dll"]
