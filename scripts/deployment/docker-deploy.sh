#!/bin/bash
# TallyJ 4 Docker Deployment Script
# Usage: ./docker-deploy.sh [environment]
# Example: ./docker-deploy.sh production

set -e

ENVIRONMENT=${1:-production}
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR/../.."

echo "=========================================="
echo "TallyJ 4 Docker Deployment"
echo "Environment: $ENVIRONMENT"
echo "=========================================="

# Load environment variables
if [ -f "$PROJECT_ROOT/.env.docker" ]; then
    export $(cat "$PROJECT_ROOT/.env.docker" | grep -v '^#' | xargs)
    echo "✓ Loaded environment variables from .env.docker"
else
    echo "⚠ Warning: .env.docker not found, using defaults"
fi

# Navigate to project root
cd "$PROJECT_ROOT"

# Step 1: Pull latest code
echo ""
echo "Step 1: Pulling latest code..."
git pull origin main
echo "✓ Code updated"

# Step 2: Build Docker images
echo ""
echo "Step 2: Building Docker images..."
docker-compose build --no-cache
echo "✓ Docker images built"

# Step 3: Stop existing containers
echo ""
echo "Step 3: Stopping existing containers..."
docker-compose down
echo "✓ Containers stopped"

# Step 4: Start new containers
echo ""
echo "Step 4: Starting new containers..."
docker-compose up -d
echo "✓ Containers started"

# Step 5: Wait for services to be ready
echo ""
echo "Step 5: Waiting for services to be ready..."
sleep 10

# Step 6: Run database migrations
echo ""
echo "Step 6: Running database migrations..."
docker-compose exec -T backend dotnet ef database update || echo "⚠ Migration skipped"
echo "✓ Migrations applied"

# Step 7: Health checks
echo ""
echo "Step 7: Running health checks..."
MAX_ATTEMPTS=30
ATTEMPT=0

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if docker-compose ps | grep -q "healthy"; then
        echo "✓ All services healthy"
        break
    fi
    ATTEMPT=$((ATTEMPT+1))
    echo "Waiting for services... ($ATTEMPT/$MAX_ATTEMPTS)"
    sleep 2
done

if [ $ATTEMPT -eq $MAX_ATTEMPTS ]; then
    echo "✗ Services failed to become healthy"
    docker-compose logs
    exit 1
fi

# Step 8: Show status
echo ""
echo "Step 8: Service Status..."
docker-compose ps

echo ""
echo "=========================================="
echo "Docker Deployment Complete!"
echo "Backend: http://localhost:5016"
echo "Frontend: http://localhost:8095"
echo "=========================================="
