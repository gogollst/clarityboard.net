# Authentication & Authorization

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Authentication Flow

### Login Sequence

```
Client                          API                           Database
──────                          ───                           ────────
  │                               │                               │
  │  POST /auth/login             │                               │
  │  { email, password }          │                               │
  │──────────────────────────────▶│                               │
  │                               │  Verify bcrypt hash           │
  │                               │──────────────────────────────▶│
  │                               │◀──────────────────────────────│
  │                               │                               │
  │  [If 2FA enabled]            │                               │
  │◀──────────────────────────────│                               │
  │  { requires2FA, challenge }   │                               │
  │                               │                               │
  │  POST /auth/verify-2fa       │                               │
  │  { challenge, totpCode }      │                               │
  │──────────────────────────────▶│  Verify TOTP                  │
  │                               │──────────────────────────────▶│
  │                               │◀──────────────────────────────│
  │                               │                               │
  │  [On success]                │  Store refresh token hash     │
  │◀──────────────────────────────│──────────────────────────────▶│
  │  { accessToken, refreshToken }│                               │
  │                               │                               │
  │  GET /api/v1/dashboard       │                               │
  │  Authorization: Bearer {JWT}  │                               │
  │──────────────────────────────▶│  Validate JWT (no DB call)    │
  │                               │                               │
  │  POST /auth/refresh          │                               │
  │  { refreshToken }             │                               │
  │──────────────────────────────▶│  Validate + rotate token     │
  │                               │──────────────────────────────▶│
  │◀──────────────────────────────│                               │
  │  { newAccessToken,           │                               │
  │    newRefreshToken }          │                               │
```

---

## 2. JWT Token Design

### Access Token (Short-Lived)

```json
{
  "header": {
    "alg": "RS256",
    "typ": "JWT",
    "kid": "key-2026-01"
  },
  "payload": {
    "sub": "user-uuid",
    "email": "user@company.com",
    "name": "Max Mustermann",
    "role": "finance",
    "entities": ["entity-uuid-1", "entity-uuid-2"],
    "permissions": ["kpi.read", "journal.write", "report.generate"],
    "active_entity": "entity-uuid-1",
    "iss": "clarityboard.net",
    "aud": "clarityboard-api",
    "iat": 1740672000,
    "exp": 1740672900,
    "jti": "unique-token-id"
  }
}
```

| Property | Value | Rationale |
|----------|-------|-----------|
| Algorithm | RS256 | Asymmetric: API validates with public key only |
| Expiry | 15 minutes | Short window limits token theft impact |
| Key Rotation | Monthly | `kid` header enables seamless rotation |
| Entity List | In token | Avoids DB lookup on every request |
| Permissions | In token | Enables stateless authorization checks |

### Refresh Token (Long-Lived)

```sql
CREATE TABLE public.refresh_tokens (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL REFERENCES public.users(id),
    token_hash      VARCHAR(64) NOT NULL,       -- SHA-256 of token
    device_fingerprint VARCHAR(200),            -- Browser/device identifier
    ip_address      INET,
    expires_at      TIMESTAMPTZ NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    revoked_at      TIMESTAMPTZ,
    replaced_by     UUID,                       -- Chain tracking for rotation

    INDEX idx_refresh_token_hash (token_hash),
    INDEX idx_refresh_user (user_id) WHERE revoked_at IS NULL
);
```

| Property | Value | Rationale |
|----------|-------|-----------|
| Format | Opaque (random 256-bit) | No payload to decode if stolen |
| Storage | Hash in DB | Token never stored in plain text |
| Expiry | 7 days | Balance between UX and security |
| Rotation | On every use | Old token invalidated when new one issued |
| Device Binding | Fingerprint + IP | Detect token theft (different device) |

---

## 3. Password Security

```csharp
public class PasswordPolicy
{
    public int MinLength => 10;
    public bool RequireUppercase => true;
    public bool RequireLowercase => true;
    public bool RequireDigit => true;
    public bool RequireSpecialChar => true;
    public int MaxFailedAttempts => 5;
    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(15);
    public TimeSpan LockoutEscalation => TimeSpan.FromMinutes(15);  // +15 min per lockout
    public int PasswordHistoryCount => 5;  // Cannot reuse last 5 passwords
}

// Hashing: bcrypt with cost factor 12
public string HashPassword(string password)
    => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
```

### Account Lockout Strategy

```
Attempt 1-4: Normal login
Attempt 5:   Lock for 15 minutes
Attempt 6:   Lock for 30 minutes (escalating)
Attempt 7:   Lock for 45 minutes
Attempt 10+: Lock for 2 hours + alert to admin
```

---

## 4. Two-Factor Authentication (TOTP)

### Setup Flow

```
1. User enables 2FA in profile settings
2. Server generates TOTP secret (RFC 6238)
3. Server returns QR code URI for authenticator app
4. User scans QR code with Google Authenticator / Authy
5. User enters current TOTP code to verify setup
6. Server stores encrypted TOTP secret
7. Server generates 10 recovery codes (one-time use, bcrypt hashed)
```

### Verification

```csharp
public class TotpService
{
    private const int TimeStep = 30;       // 30-second window
    private const int AllowedDrift = 1;    // Allow ±1 step (clock skew)
    private const int CodeLength = 6;

    public bool VerifyCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret),
            step: TimeStep,
            totpSize: CodeLength);

        return totp.VerifyTotp(code,
            out _,
            window: new VerificationWindow(previous: AllowedDrift, future: AllowedDrift));
    }
}
```

---

## 5. Role-Based Access Control (RBAC)

### Role Hierarchy

```
                    ┌──────────┐
                    │  Admin   │  Full system access
                    └────┬─────┘
                         │
                    ┌────▼─────┐
                    │Executive │  All KPIs, all entities, scenarios
                    └────┬─────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
     ┌────▼────┐   ┌────▼────┐   ┌────▼────┐
     │ Finance │   │  Sales  │   │Marketing│   Domain-specific KPIs
     └────┬────┘   └────┬────┘   └────┬────┘
          │              │              │
          └──────────────┼──────────────┘
                         │
                    ┌────▼────┐
                    │   HR    │   HR KPIs, personnel data
                    └────┬────┘
                         │
                    ┌────▼────┐
                    │ Auditor │   Read-only, all data, audit logs
                    └─────────┘
```

### Permission Matrix

| Permission | Admin | Executive | Finance | Sales | Marketing | HR | Auditor |
|-----------|:-----:|:---------:|:-------:|:-----:|:---------:|:--:|:-------:|
| `kpi.financial.read` | x | x | x | | | | x |
| `kpi.financial.write` | x | | x | | | | |
| `kpi.sales.read` | x | x | | x | | | x |
| `kpi.sales.write` | x | | | x | | | |
| `kpi.marketing.read` | x | x | | | x | | x |
| `kpi.marketing.write` | x | | | | x | | |
| `kpi.hr.read` | x | x | | | | x | x |
| `kpi.hr.write` | x | | | | | x | |
| `journal.read` | x | x | x | | | | x |
| `journal.write` | x | | x | | | | |
| `scenario.read` | x | x | x | | | | x |
| `scenario.write` | x | x | x | | | | |
| `document.upload` | x | | x | x | | | |
| `datev.export` | x | | x | | | | x |
| `budget.read` | x | x | x | x | x | x | x |
| `budget.write` | x | | x | | | | |
| `admin.users` | x | | | | | | |
| `admin.entities` | x | | | | | | |
| `admin.webhooks` | x | | | | | | |
| `admin.audit` | x | | | | | | x |

### Authorization Middleware

```csharp
// Custom authorization attribute
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        var permissions = user.FindAll("permissions").Select(c => c.Value);

        if (!permissions.Contains(Permission))
        {
            context.Result = new ForbidResult();
        }
    }
}

// Usage in controller
[HttpGet("financial")]
[RequirePermission("kpi.financial.read")]
public async Task<ActionResult<List<KpiDto>>> GetFinancialKpis(Guid entityId)
{
    // ...
}
```

### Entity Access Control

```csharp
// MediatR pipeline behavior: Verify user has access to requested entity
public class EntityAccessBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IEntityScoped
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var entityId = request.EntityId;

        var hasAccess = await _userEntityRepo.HasAccessAsync(userId, entityId);
        if (!hasAccess)
        {
            throw new EntityAccessDeniedException(userId, entityId);
        }

        return await next();
    }
}
```

---

## 6. Session Management

### Token Storage (Client)

```typescript
// Access token: In-memory only (never in localStorage)
let accessToken: string | null = null;

// Refresh token: HttpOnly secure cookie (set by server)
// Cannot be accessed by JavaScript → XSS-safe
```

### Concurrent Session Control

| Policy | Implementation |
|--------|---------------|
| Max sessions per user | 5 (configurable per role) |
| Session on new device | Allowed (notification sent) |
| Force logout | Admin can revoke all refresh tokens |
| Idle timeout | 30 minutes of inactivity (configurable) |
| Absolute timeout | 12 hours max session length |

### Token Refresh Strategy

```typescript
// Axios interceptor: Auto-refresh on 401
api.interceptors.response.use(
    (response) => response,
    async (error) => {
        if (error.response?.status === 401 && !error.config._retry) {
            error.config._retry = true;

            try {
                const { data } = await api.post('/auth/refresh');
                accessToken = data.accessToken;
                error.config.headers.Authorization = `Bearer ${accessToken}`;
                return api(error.config);
            } catch (refreshError) {
                // Refresh failed → redirect to login
                accessToken = null;
                window.location.href = '/login';
                return Promise.reject(refreshError);
            }
        }
        return Promise.reject(error);
    }
);
```

---

## 7. Key Rotation

### RSA Key Pair Management

```
Key Rotation Schedule:
  1. Generate new key pair monthly (1st of month, 00:00 UTC)
  2. New tokens signed with new key (kid: "key-YYYY-MM")
  3. Old key kept active for validation (grace period: 48 hours)
  4. Old key archived after grace period
  5. Stored in Azure Key Vault / HashiCorp Vault
```

### Key Resolution

```csharp
public class JwtKeyResolver
{
    // Supports multiple active keys for seamless rotation
    public SecurityKey ResolveKey(string kid)
    {
        return _keyCache.GetOrAdd(kid, async (keyId) =>
        {
            var keyData = await _vault.GetSecretAsync($"jwt-signing-key-{keyId}");
            return new RsaSecurityKey(RSA.Create(keyData));
        });
    }
}
```

---

## 8. Audit Trail

### Auth Events Logged

| Event | Data Captured |
|-------|--------------|
| Login success | User, IP, device, timestamp |
| Login failure | Email attempted, IP, reason, timestamp |
| 2FA challenge | User, method, timestamp |
| 2FA success/failure | User, IP, timestamp |
| Token refresh | User, old token ID, new token ID |
| Logout | User, session duration |
| Password change | User, timestamp (not the password) |
| Role change | User, old role, new role, changed by |
| Account lock/unlock | User, reason, duration |
| Permission change | Target user, permission, action, changed by |

---

## Document Navigation

- Previous: [Data Ingestion & Event Processing](./06-data-ingestion.md)
- Next: [Real-Time Communication](./08-realtime.md)
- [Back to Index](./README.md)
