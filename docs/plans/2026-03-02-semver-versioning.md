# SemVer Versioning Strategy Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a fully automated SemVer versioning system with a single `version.json` as source of truth, a public `/api/version` endpoint, version display in the frontend Sidebar, and PATCH auto-bump + git tag on every deploy.

**Architecture:** `version.json` lives in the API project root and is copied to the build output. A simple `[AllowAnonymous]` controller reads it at runtime and exposes it publicly. The frontend fetches this endpoint on load and displays it in the Sidebar. `deploy.sh` bumps PATCH, commits, deploys, then pushes a git tag on success.

**Tech Stack:** .NET 10 / ASP.NET Core (no MediatR needed for this), React 19 + TanStack Query, Bash, SemVer, git

---

### Task 1: Create `version.json` (single source of truth)

**Files:**
- Create: `src/backend/src/ClarityBoard.API/version.json`

**Step 1: Create the file**

```json
{
  "version": "0.4.0",
  "buildDate": "2026-03-02"
}
```

Save to `src/backend/src/ClarityBoard.API/version.json`.

**Step 2: Add to `.csproj` so it's copied to publish output**

In `src/backend/src/ClarityBoard.API/ClarityBoard.API.csproj`, add inside `<Project>`:

```xml
<ItemGroup>
  <Content Include="version.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

**Step 3: Verify file structure**

Run: `cat src/backend/src/ClarityBoard.API/version.json`
Expected: valid JSON with `version` and `buildDate` keys.

**Step 4: Commit**

```bash
git add src/backend/src/ClarityBoard.API/version.json \
        src/backend/src/ClarityBoard.API/ClarityBoard.API.csproj
git commit -m "feat: add version.json as SemVer single source of truth"
```

---

### Task 2: Backend – `VersionController.cs`

**Files:**
- Create: `src/backend/src/ClarityBoard.API/Controllers/VersionController.cs`

**Step 1: Write the controller**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClarityBoard.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private static readonly string VersionFilePath =
        Path.Combine(AppContext.BaseDirectory, "version.json");

    [HttpGet]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        if (!System.IO.File.Exists(VersionFilePath))
            return Ok(new VersionResponse("0.0.0", DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")));

        var json = System.IO.File.ReadAllText(VersionFilePath);
        using var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("version").GetString() ?? "0.0.0";
        var buildDate = doc.RootElement.GetProperty("buildDate").GetString()
                        ?? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        return Ok(new VersionResponse(version, buildDate));
    }

    private record VersionResponse(string Version, string BuildDate);
}
```

**Step 2: Verify it compiles (no dotnet locally – check visually)**

Confirm:
- `[AllowAnonymous]` present → no JWT required
- `[Route("api/[controller]")]` → maps to `GET /api/version`
- Reads from `AppContext.BaseDirectory` (correct for Docker published output at `/app/version.json`)
- Returns `{ "version": "...", "buildDate": "..." }` (C# record serializes to camelCase by default)

**Step 3: Commit**

```bash
git add src/backend/src/ClarityBoard.API/Controllers/VersionController.cs
git commit -m "feat: add GET /api/version public endpoint"
```

---

### Task 3: Frontend – `useVersion` hook

**Files:**
- Modify: `src/frontend/src/lib/queryKeys.ts`
- Create: `src/frontend/src/hooks/useVersion.ts`

**Step 1: Add query key to `queryKeys.ts`**

At the end of the `queryKeys` object (before the closing `}`), add:

```typescript
  version: {
    all: () => ['version'] as const,
  },
```

**Step 2: Create `useVersion.ts`**

```typescript
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';

interface VersionInfo {
  version: string;
  buildDate: string;
}

export function useVersion() {
  return useQuery({
    queryKey: queryKeys.version.all(),
    queryFn: async () => {
      const { data } = await api.get<VersionInfo>('/version');
      return data;
    },
    staleTime: 10 * 60 * 1000, // 10 minutes – version rarely changes
    retry: false,
  });
}
```

**Note on API call:** The `/api/version` endpoint is public (`[AllowAnonymous]`). The Axios interceptor will still send the JWT if present, which is fine – the endpoint ignores it. We call `/version` (without `/api` prefix) because `api.ts` sets `baseURL: '/api'`.

**Step 3: Commit**

```bash
git add src/frontend/src/lib/queryKeys.ts \
        src/frontend/src/hooks/useVersion.ts
git commit -m "feat: add useVersion hook for /api/version endpoint"
```

---

### Task 4: Frontend – Version display in Sidebar

**Files:**
- Modify: `src/frontend/src/components/layout/Sidebar.tsx`

**Step 1: Add import at the top of Sidebar.tsx**

After the existing imports, add:

```typescript
import { useVersion } from '@/hooks/useVersion';
```

**Step 2: Call the hook inside the `Sidebar` component**

Inside `export default function Sidebar()`, after the existing hooks (`useLocation`, `useAuth`, `useUiStore`), add:

```typescript
const { data: versionInfo } = useVersion();
```

**Step 3: Add version display element**

In the JSX, add a version badge **between the `<nav>` block and the Collapse Toggle `<div>`**:

```tsx
{/* Version */}
{sidebarOpen && versionInfo && (
  <div className="px-4 py-2 text-center">
    <span className="text-[10px] text-slate-600 tabular-nums">
      v{versionInfo.version}
    </span>
  </div>
)}
```

Exact location in JSX – after `</nav>` closing tag, before the collapse toggle `<div className="border-t border-white/10 p-2">`:

```tsx
      </nav>

      {/* Version */}
      {sidebarOpen && versionInfo && (
        <div className="px-4 py-2 text-center">
          <span className="text-[10px] text-slate-600 tabular-nums">
            v{versionInfo.version}
          </span>
        </div>
      )}

      {/* Collapse Toggle */}
      <div className="border-t border-white/10 p-2">
```

**Step 4: Verify build passes**

```bash
cd src/frontend && npm run build
```
Expected: `✓ built in X.XXs` (no TypeScript errors)

**Step 5: Commit**

```bash
git add src/frontend/src/components/layout/Sidebar.tsx
git commit -m "feat: show app version in sidebar footer"
```

---

### Task 5: Update `deploy.sh` – PATCH bump + git tag

**Files:**
- Modify: `deploy.sh`

**Current state of `deploy.sh` (relevant sections):**

```bash
#!/bin/bash
set -euo pipefail

PROJECT_DIR="/home/stefan/Documents/GitHub/clarityboard.net"
# ... [steps 1-6]
```

**Step 1: Add version bump logic at the very top (after variable declarations)**

Replace the existing header section:

```bash
#!/bin/bash
set -euo pipefail

PROJECT_DIR="/home/stefan/Documents/GitHub/clarityboard.net"
FRONTEND_DIST="/var/www/clarityboard/frontend"
VERSION_FILE="$PROJECT_DIR/src/backend/src/ClarityBoard.API/version.json"

echo "=== ClarityBoard Production Deploy ==="
echo ""

# ── Bump PATCH version ──────────────────────────────────────────────────────
echo "[0/6] Bumping patch version..."
CURRENT_VERSION=$(python3 -c "import json; d=json.load(open('$VERSION_FILE')); print(d['version'])")
MAJOR=$(echo "$CURRENT_VERSION" | cut -d. -f1)
MINOR=$(echo "$CURRENT_VERSION" | cut -d. -f2)
PATCH=$(echo "$CURRENT_VERSION" | cut -d. -f3)
NEW_PATCH=$(( PATCH + 1 ))
NEW_VERSION="$MAJOR.$MINOR.$NEW_PATCH"
BUILD_DATE=$(date +%Y-%m-%d)

python3 -c "
import json, sys
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
```

**Step 2: Add git tag + push at the very end (after health check, before final echo)**

Replace the final echo block:

```bash
# ── Tag release ─────────────────────────────────────────────────────────────
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
```

**Step 3: Update step labels in echo statements**

The old steps were `[1/6]` through `[6/6]`. With the new step 0 and step 7, they should become `[1/7]` through `[6/7]` (bump is step 0, tag is step 7).

**Full resulting `deploy.sh`:**

```bash
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
```

**Step 4: Commit**

```bash
git add deploy.sh
git commit -m "feat: auto-bump PATCH version and tag on deploy"
```

---

### Task 6: Integration check & final commit

**Step 1: Verify frontend build is clean**

```bash
cd src/frontend && npm run build 2>&1 | tail -3
```
Expected: `✓ built in X.XXs`

**Step 2: Verify version.json is present and valid**

```bash
cat src/backend/src/ClarityBoard.API/version.json
python3 -c "import json; d=json.load(open('src/backend/src/ClarityBoard.API/version.json')); print('OK:', d['version'])"
```
Expected: `OK: 0.4.0`

**Step 3: Verify deploy.sh is executable and parses cleanly**

```bash
bash -n deploy.sh && echo "Syntax OK"
```
Expected: `Syntax OK`

**Step 4: Push all commits**

```bash
git push origin main
```

---

## Verification Checklist

After running `sudo bash deploy.sh`:

- [ ] `version.json` contains new PATCH version (e.g., `0.4.1`)
- [ ] Git log shows `chore: bump version to 0.4.1` commit
- [ ] `curl https://api.clarityboard.net/api/version` returns `{"version":"0.4.1","buildDate":"YYYY-MM-DD"}`
- [ ] Sidebar shows `v0.4.1` at the bottom when expanded
- [ ] `git tag` lists `v0.4.1`
- [ ] GitHub shows the tag under Releases/Tags

## Notes

- `version.json` is committed to git → full audit trail of version history
- `[AllowAnonymous]` on `VersionController` means no JWT needed for health monitors, CI pipelines, etc.
- TanStack Query caches the version for 10 minutes → no excessive API calls
- The `set -euo pipefail` in `deploy.sh` means any step failure aborts the deploy before tagging – version bump commit will be in local git but won't be pushed until step 7 succeeds
- MAJOR/MINOR bumps are manual: edit `version.json` directly, then run deploy (deploy only auto-bumps PATCH)
