#!/bin/bash

# Deployment script for Wagl Backend
# This script handles database setup and application deployment

set -e  # Exit on any error

echo "🚀 Wagl Backend Deployment Script"
echo "=================================="

# Configuration
ENVIRONMENT=${ENVIRONMENT:-"local"}
echo "📍 Deploying for environment: $ENVIRONMENT"

# Load environment variables
if [ "$ENVIRONMENT" = "production" ]; then
    if [ -f ".env.production" ]; then
        export $(grep -v '^#' .env.production | xargs)
        echo "✅ Loaded production environment variables"
    else
        echo "❌ .env.production file not found!"
        exit 1
    fi
elif [ -f ".env.local" ]; then
    export $(grep -v '^#' .env.local | xargs)
    echo "✅ Loaded local environment variables"
fi

# Add dotnet tools to PATH
export PATH="$PATH:/home/bash/.dotnet/tools"

# Change to the project root
cd "$(dirname "$0")/.." || exit 1

echo "📂 Current directory: $(pwd)"

# Build the application
echo "🔨 Building the application..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo "✅ Build successful!"

# Database operations
echo "🗄️  Setting up database..."

# Create connection string for PostgreSQL
CONNECTION_STRING="Host=${DATABASE_HOST};Port=${DATABASE_PORT};Database=${DATABASE_NAME};Username=${DATABASE_USER};Password=${DATABASE_PASSWORD};SSL Mode=${DATABASE_SSL_MODE}"
export ConnectionStrings__PostgreSQL="$CONNECTION_STRING"

echo "🔧 Connection string configured for ${DATABASE_HOST}:${DATABASE_PORT}/${DATABASE_NAME}"

# Run database migrations (if they exist)
if [ -d "src/WaglBackend.Infrastructure/Persistence/Migrations" ] && [ "$(ls -A src/WaglBackend.Infrastructure/Persistence/Migrations)" ]; then
    echo "📊 Running database migrations..."
    dotnet ef database update \
        --project src/WaglBackend.Infrastructure \
        --startup-project src/WaglBackend.Api \
        --verbose
    
    if [ $? -eq 0 ]; then
        echo "✅ Database migrations completed!"
    else
        echo "❌ Database migration failed!"
        exit 1
    fi
else
    echo "ℹ️  No migrations found. Database will be created on first run."
fi

# Run the application (for local development)
if [ "$ENVIRONMENT" = "local" ]; then
    echo "🚀 Starting the application locally..."
    echo "📝 Available endpoints:"
    echo "   - API: https://localhost:5001"
    echo "   - Swagger: https://localhost:5001 (in development)"
    echo "   - Health: https://localhost:5001/health"
    echo ""
    echo "🔐 Demo accounts (after seeding):"
    echo "   - Tier1: tier1@wagl.com / Tier1Pass123!"
    echo "   - Tier2: tier2@wagl.com / Tier2Pass123!"
    echo "   - Tier3: tier3@wagl.com / Tier3Pass123!"
    echo "   - Admin: admin@wagl.com / AdminPass123!"
    echo ""
    
    dotnet run --project src/WaglBackend.Api
else
    echo "✅ Deployment completed successfully!"
    echo "📝 Remember to:"
    echo "   1. Configure your reverse proxy (nginx/Apache)"
    echo "   2. Set up SSL certificates"
    echo "   3. Configure monitoring and logging"
    echo "   4. Set up Redis cache if not already configured"
fi