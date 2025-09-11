#!/bin/bash

# Script to create Entity Framework migrations for the Wagl Backend
# This script handles the migration creation when EF tools have path issues

echo "Creating Entity Framework migration..."

# Add dotnet tools to PATH
export PATH="$PATH:/home/bash/.dotnet/tools"

# Change to the project root
cd "$(dirname "$0")/.." || exit 1

echo "Current directory: $(pwd)"
echo "Creating initial migration..."

# Create the initial migration
dotnet ef migrations add InitialCreate \
    --project src/WaglBackend.Infrastructure \
    --startup-project src/WaglBackend.Api \
    --output-dir Persistence/Migrations \
    --verbose

if [ $? -eq 0 ]; then
    echo "✅ Migration created successfully!"
    echo "Next steps:"
    echo "1. Review the migration files in src/WaglBackend.Infrastructure/Persistence/Migrations/"
    echo "2. Update your database using: dotnet ef database update --project src/WaglBackend.Infrastructure --startup-project src/WaglBackend.Api"
else
    echo "❌ Migration creation failed. Please check the error messages above."
    exit 1
fi