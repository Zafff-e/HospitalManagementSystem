# Multi-stage Dockerfile for HMS .NET Web API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY Hms.sln ./
COPY src/Hms.Api/Hms.Api.csproj ./src/Hms.Api/
COPY src/Hms.Application/Hms.Application.csproj ./src/Hms.Application/
COPY src/Hms.Domain/Hms.Domain.csproj ./src/Hms.Domain/
COPY src/Hms.Infrastructure/Hms.Infrastructure.csproj ./src/Hms.Infrastructure/
COPY tests/Hms.UnitTests/Hms.UnitTests.csproj ./tests/Hms.UnitTests/
COPY tests/Hms.IntegrationTests/Hms.IntegrationTests.csproj ./tests/Hms.IntegrationTests/

# Restore dependencies
RUN dotnet restore Hms.sln

# Copy the rest of the source code
COPY . .

# Build and publish
RUN dotnet publish src/Hms.Api/Hms.Api.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Hms.Api.dll"]
