# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/WaglBackend.Api/WaglBackend.Api.csproj", "src/WaglBackend.Api/"]
COPY ["src/WaglBackend.Infrastructure/WaglBackend.Infrastructure.csproj", "src/WaglBackend.Infrastructure/"]
COPY ["src/WaglBackend.Domain/WaglBackend.Domain.csproj", "src/WaglBackend.Domain/"]
COPY ["src/WaglBackend.Core/WaglBackend.Core.csproj", "src/WaglBackend.Core/"]
COPY ["global.json", "./"]

RUN dotnet restore "src/WaglBackend.Api/WaglBackend.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/WaglBackend.Api"
RUN dotnet build "WaglBackend.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "WaglBackend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Create a health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "WaglBackend.Api.dll"]