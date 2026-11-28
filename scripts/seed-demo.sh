#!/usr/bin/env bash
set -euo pipefail

BACKEND_PROJECT="${1:-backend/DBMSLab.Api/DBMSLab.Api.csproj}"

# Check environment: seeding only works in Development
if [ "${ASPNETCORE_ENVIRONMENT:-}" != "Development" ]; then
    echo "Error: seeding is only available in Development environment" >&2
    echo "Set environment variable: export ASPNETCORE_ENVIRONMENT=Development" >&2
    exit 1
fi

echo "=== Running demo data seeding ==="

if [ ! -f "$BACKEND_PROJECT" ]; then
    echo "Error: project not found: $BACKEND_PROJECT" >&2
    exit 1
fi

echo "Running dotnet run --project $BACKEND_PROJECT -- --seed"
dotnet run --project "$BACKEND_PROJECT" -- --seed

echo "Seeding completed successfully."