# ==============================================================================
# Build Stage - Official Microsoft .NET 10 SDK
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY DocuMind.sln ./
COPY src/DocuMind.Domain/DocuMind.Domain.csproj src/DocuMind.Domain/
COPY src/DocuMind.Application/DocuMind.Application.csproj src/DocuMind.Application/
COPY src/DocuMind.Infrastructure/DocuMind.Infrastructure.csproj src/DocuMind.Infrastructure/
COPY src/DocuMind.WebApi/DocuMind.WebApi.csproj src/DocuMind.WebApi/
COPY tests/DocuMind.Application.Tests/DocuMind.Application.Tests.csproj tests/DocuMind.Application.Tests/
COPY tests/DocuMind.Infrastructure.Tests/DocuMind.Infrastructure.Tests.csproj tests/DocuMind.Infrastructure.Tests/

# Restore dependencies
RUN dotnet restore DocuMind.sln

# Copy all source files
COPY src/ src/
COPY tests/ tests/

# Build and Publish WebApi in Release mode
WORKDIR /app/src/DocuMind.WebApi
RUN dotnet publish DocuMind.WebApi.csproj -c Release -o /app/publish --no-restore

# ==============================================================================
# Runtime Stage - Official Microsoft ASP.NET Core .NET 10 Runtime
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Configure default ASP.NET Core URL for local and Docker networking
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

# Install curl for container health check
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Copy published artifacts from build stage
COPY --from=build /app/publish .

# Create storage and logs directories, ensuring correct ownership for non-root 'app' user
RUN mkdir -p /app/App_Data/uploads /app/logs && \
    chown -R app:app /app

# Switch to standard non-root user
USER app

ENTRYPOINT ["dotnet", "DocuMind.WebApi.dll"]
