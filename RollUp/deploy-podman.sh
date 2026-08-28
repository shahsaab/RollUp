#!/usr/bin/env bash
# ==============================================================================
# RollUp Deployment Script for RHEL / Podman
# Domain: rollup.eraconnect.net
# Exposed Port: 5088
# ==============================================================================

set -e

APP_NAME="rollup-app"
IMAGE_TAG="rollup-app:latest"
HOST_PORT="5088"
CONTAINER_PORT="8080"

# Change these to your production PostgreSQL credentials if hosted on host
DB_HOST="${DB_HOST:-127.0.0.1}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-rollup}"
DB_USER="${DB_USER:-postgres}"
DB_PASS="${DB_PASS:-postgres}"

echo "=========================================="
echo "🚀 Building & Deploying RollUp with Podman"
echo "=========================================="

# 1. Build the container image
echo "🔨 Building image $IMAGE_TAG..."
podman build -t $IMAGE_TAG -f Dockerfile .

# 2. Stop and remove existing container if running
echo "🛑 Cleaning up previous container instance..."
podman stop $APP_NAME 2>/dev/null || true
podman rm $APP_NAME 2>/dev/null || true

# 3. Run the container
echo "▶️ Starting container on port $HOST_PORT..."
podman run -d \
  --name $APP_NAME \
  --restart unless-stopped \
  -p $HOST_PORT:$CONTAINER_PORT \
  --network=slirp4netns:allow_host_loopback=true \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:$CONTAINER_PORT \
  -e DBOption=Postgres \
  -e "ConnectionStrings__DefaultConnection=Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASS" \
  -e "Jwt__Secret=YourSuperSecretKeyWithAtLeast32CharactersLongForSecurity!" \
  -e "Jwt__Issuer=RollUpPlatform" \
  -e "Jwt__Audience=RollUpUsers" \
  $IMAGE_TAG

echo ""
echo "=========================================="
echo "✅ RollUp deployed successfully!"
echo "📡 Local Access: http://127.0.0.1:$HOST_PORT"
echo "🌐 Domain Target: https://rollup.eraconnect.net"
echo "=========================================="
podman ps | grep $APP_NAME
