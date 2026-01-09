#!/bin/bash

echo "WARNING: This will drop and recreate the database!"
read -p "Are you sure? (yes/no): " confirm
if [ "$confirm" != "yes" ]; then 
    echo "Database reset cancelled."
    exit 1
fi

echo "Dropping database..."
dotnet ef database drop --project TallyJ4.csproj --force

echo "Applying migrations..."
dotnet ef database update --project TallyJ4.csproj

echo ""
echo "Database reset complete!"
echo "Run 'dotnet run' to seed the database with test data."
