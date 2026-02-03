#!/bin/bash
# TallyJ 4 Deployment Script (Linux/macOS)
# Usage: ./deploy.sh [environment]
# Example: ./deploy.sh production

set -e

ENVIRONMENT=${1:-production}
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR/../.."

echo "=========================================="
echo "TallyJ 4 Deployment Script"
echo "Environment: $ENVIRONMENT"
echo "=========================================="

# Load environment variables
if [ -f "$PROJECT_ROOT/.env.$ENVIRONMENT" ]; then
    source "$PROJECT_ROOT/.env.$ENVIRONMENT"
    echo "✓ Loaded environment variables from .env.$ENVIRONMENT"
else
    echo "⚠ Warning: .env.$ENVIRONMENT not found"
fi

# Step 1: Build Backend
echo ""
echo "Step 1: Building Backend..."
cd "$PROJECT_ROOT/backend"
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -o ./publish
echo "✓ Backend build complete"

# Step 2: Build Frontend
echo ""
echo "Step 2: Building Frontend..."
cd "$PROJECT_ROOT/frontend"
npm ci
npm run build
echo "✓ Frontend build complete"

# Step 3: Run Database Migrations
echo ""
echo "Step 3: Running Database Migrations..."
cd "$PROJECT_ROOT/backend"
if [ -n "$DB_CONNECTION_STRING" ]; then
    dotnet ef database update --connection "$DB_CONNECTION_STRING"
    echo "✓ Database migrations applied"
else
    echo "⚠ Skipping migrations: DB_CONNECTION_STRING not set"
fi

# Step 4: Deploy Backend
echo ""
echo "Step 4: Deploying Backend..."
if [ -n "$BACKEND_DEPLOY_PATH" ]; then
    rsync -avz --delete ./publish/ "$BACKEND_DEPLOY_PATH/"
    echo "✓ Backend deployed to $BACKEND_DEPLOY_PATH"
else
    echo "⚠ Skipping backend deployment: BACKEND_DEPLOY_PATH not set"
fi

# Step 5: Deploy Frontend
echo ""
echo "Step 5: Deploying Frontend..."
if [ -n "$FRONTEND_DEPLOY_PATH" ]; then
    rsync -avz --delete "$PROJECT_ROOT/frontend/dist/" "$FRONTEND_DEPLOY_PATH/"
    echo "✓ Frontend deployed to $FRONTEND_DEPLOY_PATH"
else
    echo "⚠ Skipping frontend deployment: FRONTEND_DEPLOY_PATH not set"
fi

# Step 6: Restart Services
echo ""
echo "Step 6: Restarting Services..."
if [ -n "$RESTART_COMMAND" ]; then
    eval "$RESTART_COMMAND"
    echo "✓ Services restarted"
else
    echo "⚠ Skipping service restart: RESTART_COMMAND not set"
fi

# Step 7: Smoke Tests
echo ""
echo "Step 7: Running Smoke Tests..."
if [ -n "$API_URL" ]; then
    response=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL/health")
    if [ "$response" = "200" ]; then
        echo "✓ Backend health check passed"
    else
        echo "✗ Backend health check failed (HTTP $response)"
        exit 1
    fi
else
    echo "⚠ Skipping health check: API_URL not set"
fi

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
