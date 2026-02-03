# TallyJ 4 Deployment Script (Windows PowerShell)
# Usage: .\deploy.ps1 -Environment production
# Example: .\deploy.ps1 -Environment production

param(
    [string]$Environment = "production"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "TallyJ 4 Deployment Script" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Load environment variables
$EnvFile = Join-Path $ProjectRoot ".env.$Environment"
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            Set-Variable -Name $matches[1] -Value $matches[2] -Scope Script
        }
    }
    Write-Host "✓ Loaded environment variables from .env.$Environment" -ForegroundColor Green
} else {
    Write-Host "⚠ Warning: .env.$Environment not found" -ForegroundColor Yellow
}

# Step 1: Build Backend
Write-Host ""
Write-Host "Step 1: Building Backend..." -ForegroundColor Cyan
Set-Location (Join-Path $ProjectRoot "backend")
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -o .\publish
Write-Host "✓ Backend build complete" -ForegroundColor Green

# Step 2: Build Frontend
Write-Host ""
Write-Host "Step 2: Building Frontend..." -ForegroundColor Cyan
Set-Location (Join-Path $ProjectRoot "frontend")
npm ci
npm run build
Write-Host "✓ Frontend build complete" -ForegroundColor Green

# Step 3: Run Database Migrations
Write-Host ""
Write-Host "Step 3: Running Database Migrations..." -ForegroundColor Cyan
Set-Location (Join-Path $ProjectRoot "backend")
if ($DB_CONNECTION_STRING) {
    dotnet ef database update --connection $DB_CONNECTION_STRING
    Write-Host "✓ Database migrations applied" -ForegroundColor Green
} else {
    Write-Host "⚠ Skipping migrations: DB_CONNECTION_STRING not set" -ForegroundColor Yellow
}

# Step 4: Deploy Backend
Write-Host ""
Write-Host "Step 4: Deploying Backend..." -ForegroundColor Cyan
if ($BACKEND_DEPLOY_PATH) {
    Copy-Item -Path ".\publish\*" -Destination $BACKEND_DEPLOY_PATH -Recurse -Force
    Write-Host "✓ Backend deployed to $BACKEND_DEPLOY_PATH" -ForegroundColor Green
} else {
    Write-Host "⚠ Skipping backend deployment: BACKEND_DEPLOY_PATH not set" -ForegroundColor Yellow
}

# Step 5: Deploy Frontend
Write-Host ""
Write-Host "Step 5: Deploying Frontend..." -ForegroundColor Cyan
if ($FRONTEND_DEPLOY_PATH) {
    $FrontendDist = Join-Path $ProjectRoot "frontend\dist"
    Copy-Item -Path "$FrontendDist\*" -Destination $FRONTEND_DEPLOY_PATH -Recurse -Force
    Write-Host "✓ Frontend deployed to $FRONTEND_DEPLOY_PATH" -ForegroundColor Green
} else {
    Write-Host "⚠ Skipping frontend deployment: FRONTEND_DEPLOY_PATH not set" -ForegroundColor Yellow
}

# Step 6: Restart Services
Write-Host ""
Write-Host "Step 6: Restarting Services..." -ForegroundColor Cyan
if ($RESTART_COMMAND) {
    Invoke-Expression $RESTART_COMMAND
    Write-Host "✓ Services restarted" -ForegroundColor Green
} else {
    Write-Host "⚠ Skipping service restart: RESTART_COMMAND not set" -ForegroundColor Yellow
}

# Step 7: Smoke Tests
Write-Host ""
Write-Host "Step 7: Running Smoke Tests..." -ForegroundColor Cyan
if ($API_URL) {
    try {
        $response = Invoke-WebRequest -Uri "$API_URL/health" -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ Backend health check passed" -ForegroundColor Green
        } else {
            Write-Host "✗ Backend health check failed (HTTP $($response.StatusCode))" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "✗ Backend health check failed: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "⚠ Skipping health check: API_URL not set" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
