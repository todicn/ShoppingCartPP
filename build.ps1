#!/usr/bin/env pwsh

# Shopping Cart Build Script
Write-Host "=== Shopping Cart Build Script ===" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Error "Clean failed"
    exit $LASTEXITCODE
}

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed"
    exit $LASTEXITCODE
}

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit $LASTEXITCODE
}

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test --configuration Release --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed"
    exit $LASTEXITCODE
}

# Run demo
Write-Host "Running demo application..." -ForegroundColor Yellow
Write-Host "Press Ctrl+C to skip demo or any key in demo to continue..." -ForegroundColor Gray
dotnet run --project ShoppingCart.Demo --configuration Release --no-build

Write-Host ""
Write-Host "=== Build Completed Successfully ===" -ForegroundColor Green
Write-Host "✅ Clean completed" -ForegroundColor Green
Write-Host "✅ Restore completed" -ForegroundColor Green
Write-Host "✅ Build completed" -ForegroundColor Green
Write-Host "✅ Tests passed" -ForegroundColor Green
Write-Host "✅ Demo executed" -ForegroundColor Green 