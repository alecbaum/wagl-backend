# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/WaglBackend.Api/WaglBackend.Api.csproj", "src/WaglBackend.Api/"]
COPY ["src/WaglBackend.Infrastructure/WaglBackend.Infrastructure.csproj", "src/WaglBackend.Infrastructure/"]
COPY ["src/WaglBackend.Domain/WaglBackend.Domain.csproj", "src/WaglBackend.Domain/"]
COPY ["src/WaglBackend.Core/WaglBackend.Core.csproj", "src/WaglBackend.Core/"]

RUN dotnet restore "src/WaglBackend.Api/WaglBackend.Api.csproj"

# Copy the entire source code
COPY . .

# Build the application
WORKDIR "/src/src/WaglBackend.Api"
RUN dotnet build "WaglBackend.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WaglBackend.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-privileged user that the app will run under
RUN adduser --disabled-password --gecos "" --home "/nonexistent" --shell "/sbin/nologin" --no-create-home --uid 65532 appuser
USER appuser

ENTRYPOINT ["dotnet", "WaglBackend.Api.dll"]