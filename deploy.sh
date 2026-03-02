#!/bin/bash
set -euo pipefail

PROJECT_DIR="/home/stefan/Documents/GitHub/clarityboard.net"
FRONTEND_DIST="/var/www/clarityboard/frontend"

echo "=== ClarityBoard Production Deploy ==="
echo ""

# 1. Build frontend
echo "[1/6] Building frontend..."
cd "$PROJECT_DIR/src/frontend"
npm ci --silent
npm run build
echo "      Frontend built successfully."

# 2. Deploy frontend static files
echo "[2/6] Deploying frontend to $FRONTEND_DIST..."
sudo mkdir -p "$FRONTEND_DIST"
sudo rm -rf "${FRONTEND_DIST:?}/"*
sudo cp -r dist/* "$FRONTEND_DIST/"
sudo chown -R www-data:www-data "$FRONTEND_DIST"
echo "      Frontend deployed."

# 3. Build & start Docker services (API + infra)
echo "[3/6] Building and starting Docker services..."
cd "$PROJECT_DIR"
sg docker -c "docker compose -f docker-compose.prod.yml build api"
sg docker -c "docker compose -f docker-compose.prod.yml up -d"
echo "      Docker services started."

# 4. Install Nginx configs
echo "[4/6] Configuring Nginx..."
sudo cp "$PROJECT_DIR/infrastructure/nginx/app.clarityboard.net" /etc/nginx/sites-available/
sudo cp "$PROJECT_DIR/infrastructure/nginx/api.clarityboard.net" /etc/nginx/sites-available/
sudo ln -sf /etc/nginx/sites-available/app.clarityboard.net /etc/nginx/sites-enabled/
sudo ln -sf /etc/nginx/sites-available/api.clarityboard.net /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
echo "      Nginx configured and reloaded."

# 5. SSL certificates (Let's Encrypt)
echo "[5/6] Setting up SSL certificates..."
sudo certbot --nginx -d app.clarityboard.net -d api.clarityboard.net --non-interactive --agree-tos --redirect --email stefan@clarityboard.net
echo "      SSL certificates installed."

# 6. Health check
echo "[6/6] Running health checks..."
sleep 5
if curl -sf http://127.0.0.1:5000/health > /dev/null 2>&1; then
    echo "      API health check: OK"
else
    echo "      API health check: FAILED - check logs with: docker compose -f docker-compose.prod.yml logs api"
fi
echo ""
echo "=== Deploy complete ==="
echo "  Frontend: https://app.clarityboard.net"
echo "  API:      https://api.clarityboard.net"
echo "  Swagger:  https://api.clarityboard.net/swagger"
