#!/bin/bash

# Set environment variables
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__PostgreSQL="Host=wagl-backend-aurora.cluster-cexeows4418s.us-east-1.rds.amazonaws.com;Database=waglbackend;Username=postgres;Password=WaglBackend2024!"

# Run migrations using Docker
docker run --rm \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e ConnectionStrings__PostgreSQL="Host=wagl-backend-aurora.cluster-cexeows4418s.us-east-1.rds.amazonaws.com;Database=waglbackend;Username=postgres;Password=WaglBackend2024!" \
    wagl-backend:latest \
    dotnet WaglBackend.Api.dll --migrate-database

echo "Database migration completed"