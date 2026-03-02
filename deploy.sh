#!/bin/bash
set -euo pipefail

PROJECT_DIR="/home/stefan/Documents/GitHub/clarityboard.net"
FRONTEND_DIST="/var/www/clarityboard/frontend"
VERSION_FILE="$PROJECT_DIR/src/backend/src/ClarityBoard.API/version.json"

echo "=== ClarityBoard Production Deploy ==="
echo ""

# ── 0. Bump PATCH version ────────────────────────────────────────────────────
echo "[0/7] Bumping patch version..."
CURRENT_VERSION=$(python3 -c "import json; d=json.load(open('$VERSION_FILE')); print(d['version'])")
MAJOR=$(echo "$CURRENT_VERSION" | cut -d. -f1)
MINOR=$(echo "$CURRENT_VERSION" | cut -d. -f2)
PATCH=$(echo "$CURRENT_VERSION" | cut -d. -f3)
NEW_PATCH=$(( PATCH + 1 ))
NEW_VERSION="$MAJOR.$MINOR.$NEW_PATCH"
BUILD_DATE=$(date +%Y-%m-%d)

python3 -c "
import json
with open('$VERSION_FILE') as f:
    d = json.load(f)
d['version'] = '$NEW_VERSION'
d['buildDate'] = '$BUILD_DATE'
with open('$VERSION_FILE', 'w') as f:
    json.dump(d, f, indent=2)
    f.write('\n')
"
echo "      Version: $CURRENT_VERSION → $NEW_VERSION (build: $BUILD_DATE)"
git -C "$PROJECT_DIR" add "$VERSION_FILE"
git -C "$PROJECT_DIR" commit -m "chore: bump version to $NEW_VERSION"

# ── 1. Build frontend ────────────────────────────────────────────────────────
echo "[1/7] Building frontend..."
cd "$PROJECT_DIR/src/frontend"
npm ci --silent
npm run build
echo "      Frontend built successfully."

# ── 2. Deploy frontend static files ─────────────────────────────────────────
echo "[2/7] Deploying frontend to $FRONTEND_DIST..."
sudo mkdir -p "$FRONTEND_DIST"
sudo rm -rf "${FRONTEND_DIST:?}/"*
sudo cp -r dist/* "$FRONTEND_DIST/"
sudo chown -R www-data:www-data "$FRONTEND_DIST"
echo "      Frontend deployed."

# ── 3. Build & start Docker services (API + infra) ───────────────────────────
echo "[3/7] Building and starting Docker services..."
cd "$PROJECT_DIR"
sg docker -c "docker compose -f docker-compose.prod.yml --env-file .env.production build api"
sg docker -c "docker compose -f docker-compose.prod.yml --env-file .env.production up -d"
echo "      Docker services started."

# ── 4. Install Nginx configs ─────────────────────────────────────────────────
echo "[4/7] Configuring Nginx..."
sudo cp "$PROJECT_DIR/infrastructure/nginx/app.clarityboard.net" /etc/nginx/sites-available/
sudo cp "$PROJECT_DIR/infrastructure/nginx/api.clarityboard.net" /etc/nginx/sites-available/
sudo ln -sf /etc/nginx/sites-available/app.clarityboard.net /etc/nginx/sites-enabled/
sudo ln -sf /etc/nginx/sites-available/api.clarityboard.net /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
echo "      Nginx configured and reloaded."

# ── 5. SSL certificates (Let's Encrypt) ─────────────────────────────────────
echo "[5/7] Setting up SSL certificates..."
sudo certbot --nginx -d app.clarityboard.net -d api.clarityboard.net --non-interactive --agree-tos --redirect --email stefan@clarityboard.net
echo "      SSL certificates installed."

# ── 6. Health check ──────────────────────────────────────────────────────────
echo "[6/7] Running health checks..."
sleep 5
if curl -sf http://127.0.0.1:5000/health > /dev/null 2>&1; then
    echo "      API health check: OK"
else
    echo "      API health check: FAILED - check logs with: docker compose -f docker-compose.prod.yml logs api"
fi

# ── 7. Tag release ───────────────────────────────────────────────────────────
echo "[7/7] Tagging release..."
git -C "$PROJECT_DIR" tag "v$NEW_VERSION"
git -C "$PROJECT_DIR" push origin main
git -C "$PROJECT_DIR" push origin "v$NEW_VERSION"
echo "      Tagged: v$NEW_VERSION pushed to origin"

echo ""
echo "=== Deploy complete ==="
echo "  Version:  v$NEW_VERSION"
echo "  Frontend: https://app.clarityboard.net"
echo "  API:      https://api.clarityboard.net"
echo "  Swagger:  https://api.clarityboard.net/swagger"
