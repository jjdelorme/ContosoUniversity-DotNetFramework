#!/bin/bash

# Setup script for Contoso University database

echo "Setting up Contoso University database..."

# Check if dotnet ef is installed
if ! command -v dotnet-ef &> /dev/null
then
    echo "Installing dotnet-ef tool..."
    dotnet tool install --global dotnet-ef
fi

# Create initial migration
echo "Creating initial migration..."
dotnet ef migrations add InitialCreate

# Update database
echo "Updating database..."
dotnet ef database update

echo "Database setup complete!"
echo "The application will seed initial data on first run."
echo ""
echo "Default admin credentials:"
echo "Email: admin@contoso.edu"
echo "Password: Admin123!"