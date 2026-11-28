param([string]$BackendProject = "backend/DBMSLab.Api/DBMSLab.Api.csproj")

Write-Host "=== Running demo data seeding ===" -ForegroundColor Cyan

# Check environment: seeding only works in Development
if ($env:ASPNETCORE_ENVIRONMENT -ne "Development") {
    Write-Host "Error: seeding is only available in Development environment" -ForegroundColor Red
    Write-Host "Set environment variable: export ASPNETCORE_ENVIRONMENT=Development" -ForegroundColor Yellow
    exit 1
}

# Check if project exists
if (-not (Test-Path $BackendProject)) {
    Write-Host "Project not found: $BackendProject" -ForegroundColor Red
    exit 1
}

Write-Host "Running dotnet run --project $BackendProject -- --seed" -ForegroundColor Gray
dotnet run --project $BackendProject -- --seed

if ($LASTEXITCODE -eq 0) {
    Write-Host "Seeding completed successfully." -ForegroundColor Green
} else {
    Write-Host "Seeding failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}