FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Congreso.Api.csproj", "."]
RUN dotnet restore "./Congreso.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Congreso.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Congreso.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1001 appuser
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Expose port (Render provides PORT at runtime; default to 8080 locally)
EXPOSE 8080

# Use bash to allow runtime env expansion of PORT
HEALTHCHECK --interval=60s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f "http://localhost:${PORT:-8080}/healthz" || exit 1

ENTRYPOINT ["/bin/bash", "-lc", "ASPNETCORE_URLS=\"http://+:${PORT:-8080}\" dotnet Congreso.Api.dll"]