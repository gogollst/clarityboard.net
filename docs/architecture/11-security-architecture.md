# Security Architecture

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Defense in Depth

```
Layer 1: Network
  ├── TLS 1.3 everywhere (API, WebSocket, internal)
  ├── Firewall rules (only expose 443)
  ├── DDoS protection (CDN / cloud provider)
  └── IP allowlisting for webhooks (optional)

Layer 2: Application Gateway
  ├── Rate limiting (per-IP, per-user, per-source)
  ├── Request size limits (5 MB body)
  ├── CORS strict configuration
  └── Security headers (CSP, HSTS, X-Frame-Options)

Layer 3: Authentication
  ├── JWT RS256 (15 min expiry)
  ├── Refresh token rotation
  ├── Optional TOTP 2FA
  └── Account lockout (escalating)

Layer 4: Authorization
  ├── RBAC permission checks
  ├── Entity-level access control
  ├── Row-Level Security (PostgreSQL)
  └── API endpoint authorization

Layer 5: Application
  ├── Input validation (FluentValidation + Zod)
  ├── SQL injection protection (parameterized queries / EF Core)
  ├── XSS protection (React escaping + CSP)
  ├── CSRF protection (SameSite cookies + anti-forgery)
  └── Business rule enforcement (domain layer)

Layer 6: Data
  ├── Encryption at rest (AES-256)
  ├── Encryption in transit (TLS)
  ├── Field-level encryption (PII, tax IDs)
  ├── Hash-chained audit logs (GoBD)
  └── Backup encryption
```

---

## 2. Threat Model (STRIDE)

| Threat | Category | Mitigation |
|--------|----------|-----------|
| Token theft via XSS | Spoofing | Access token in memory only, refresh in HttpOnly cookie, strict CSP |
| Brute force login | Spoofing | Account lockout, rate limiting, bcrypt cost 12 |
| Session hijacking | Spoofing | Device fingerprint on refresh token, IP binding |
| Privilege escalation | Tampering | Server-side RBAC, JWT claims validated server-side |
| Journal entry manipulation | Tampering | Hash-chained entries (GoBD), audit log, optimistic concurrency |
| Data exfiltration | Information Disclosure | Entity-level isolation, RLS, field-level encryption |
| PII leak via AI | Information Disclosure | PII filter on all AI requests, no PII in logs |
| API DoS | Denial of Service | Rate limiting, circuit breakers, queue-based processing |
| Webhook flood | Denial of Service | Per-source rate limits, async processing, back-pressure |
| Unauthorized entity access | Elevation of Privilege | Entity access middleware, JWT entity claims |
| Admin impersonation | Elevation of Privilege | Separate admin auth flow, 2FA required for admin |

---

## 3. Encryption

### At Rest

| Data | Method | Key Management |
|------|--------|---------------|
| Database (disk) | PostgreSQL TDE or LUKS | OS-level key |
| Database backups | AES-256-GCM | Backup encryption key in vault |
| Object storage (MinIO) | Server-side encryption | MinIO KMS |
| Redis | Not encrypted (ephemeral cache) | N/A |
| Field-level (PII) | AES-256-GCM | Application-managed key in vault |

### In Transit

| Connection | Protocol | Minimum Version |
|-----------|----------|----------------|
| Client ↔ API | TLS | 1.2 (prefer 1.3) |
| Client ↔ WebSocket | WSS | TLS 1.2+ |
| API ↔ PostgreSQL | TLS | 1.2 |
| API ↔ Redis | TLS | 1.2 |
| API ↔ RabbitMQ | TLS | 1.2 |
| API ↔ AI Providers | TLS | 1.2+ |
| API ↔ MinIO | TLS | 1.2 |

### Field-Level Encryption

```csharp
public class FieldEncryptionService
{
    // Encrypt sensitive fields before database storage
    public string Encrypt(string plaintext)
    {
        var key = _vault.GetKey("field-encryption-key");
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    // Applied to: TaxId, BankAccount (IBAN), personal email, phone
}
```

---

## 4. Secrets Management

### Secrets Inventory

| Secret | Storage | Rotation |
|--------|---------|----------|
| Database password | Vault / env var | 90 days |
| Redis password | Vault / env var | 90 days |
| RabbitMQ password | Vault / env var | 90 days |
| JWT signing key (RSA) | Vault | Monthly |
| AI provider API keys | Vault | On demand |
| Webhook shared secrets | Database (encrypted) | Per-source |
| Field encryption key | Vault | Yearly |
| SMTP credentials | Vault | 90 days |
| MinIO credentials | Vault / env var | 90 days |

### Vault Integration

```csharp
// HashiCorp Vault or Azure Key Vault
services.AddSingleton<ISecretManager>(sp =>
{
    var environment = sp.GetRequiredService<IHostEnvironment>();

    if (environment.IsProduction())
    {
        return new VaultSecretManager(Configuration["Vault:Url"], Configuration["Vault:Token"]);
    }
    else
    {
        // Development: use .NET Secret Manager or env vars
        return new EnvironmentSecretManager(Configuration);
    }
});
```

---

## 5. Security Headers

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    // Content Security Policy
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +     // Required for Tailwind
        "img-src 'self' data: blob:; " +
        "font-src 'self'; " +
        "connect-src 'self' wss:; " +               // WebSocket connections
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    // Prevent clickjacking
    headers["X-Frame-Options"] = "DENY";

    // Prevent MIME type sniffing
    headers["X-Content-Type-Options"] = "nosniff";

    // Referrer policy
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Permissions policy
    headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=(), payment=()";

    // HSTS (1 year, include subdomains)
    headers["Strict-Transport-Security"] =
        "max-age=31536000; includeSubDomains; preload";

    await next();
});
```

---

## 6. Rate Limiting

```csharp
services.AddRateLimiter(options =>
{
    // Global: 100 requests/minute per IP
    options.AddFixedWindowLimiter("global", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 10;
    });

    // Auth endpoints: 10 attempts/15 minutes per IP
    options.AddSlidingWindowLimiter("auth", config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromMinutes(15);
        config.SegmentsPerWindow = 3;
    });

    // Webhook: 1000/minute per source
    options.AddTokenBucketLimiter("webhook", config =>
    {
        config.TokenLimit = 1000;
        config.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        config.TokensPerPeriod = 1000;
    });

    // AI endpoints: 30/minute per user
    options.AddFixedWindowLimiter("ai", config =>
    {
        config.PermitLimit = 30;
        config.Window = TimeSpan.FromMinutes(1);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
```

---

## 7. OWASP Top 10 Mitigations

| # | Vulnerability | Mitigation |
|---|--------------|-----------|
| A01 | Broken Access Control | RBAC + entity-level isolation + RLS + authorization middleware |
| A02 | Cryptographic Failures | TLS 1.2+, AES-256 at rest, bcrypt passwords, RS256 JWT |
| A03 | Injection | EF Core parameterized queries, FluentValidation, Zod schemas |
| A04 | Insecure Design | Threat modeling (STRIDE), security review in PR process |
| A05 | Security Misconfiguration | Hardened Docker images, no default credentials, security headers |
| A06 | Vulnerable Components | Dependabot, `dotnet list package --vulnerable`, `npm audit` |
| A07 | Authentication Failures | Account lockout, 2FA, token rotation, secure password policy |
| A08 | Data Integrity Failures | Hash-chained journal entries, signed JWT, HMAC webhooks |
| A09 | Logging & Monitoring | Structured logging (Serilog), audit trail, alert on anomalies |
| A10 | SSRF | No user-controlled URLs in server requests, allowlist for AI providers |

---

## 8. Audit Logging

### Immutable Audit Trail

```sql
CREATE TABLE public.audit_logs (
    id              UUID NOT NULL DEFAULT gen_random_uuid(),
    entity_id       UUID,
    user_id         UUID,
    action          VARCHAR(100) NOT NULL,      -- e.g. 'journal_entry.created'
    resource_type   VARCHAR(100) NOT NULL,      -- e.g. 'JournalEntry'
    resource_id     UUID,
    old_values      JSONB,                      -- Previous state (for updates)
    new_values      JSONB,                      -- New state
    ip_address      INET,
    user_agent      VARCHAR(500),
    correlation_id  UUID,
    hash            VARCHAR(64) NOT NULL,       -- SHA-256 of record
    previous_hash   VARCHAR(64),                -- Chain to previous entry
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);

-- Append-only: no UPDATE or DELETE permissions granted to application user
REVOKE UPDATE, DELETE ON public.audit_logs FROM app;
```

### Hash Chain (GoBD Compliance)

```csharp
public class AuditHashService
{
    public string ComputeHash(AuditLog entry, string previousHash)
    {
        var content = $"{entry.Id}|{entry.EntityId}|{entry.UserId}|" +
                      $"{entry.Action}|{entry.ResourceType}|{entry.ResourceId}|" +
                      $"{entry.CreatedAt:O}|{previousHash}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Verification: walk the chain and verify each hash
    public async Task<bool> VerifyChainIntegrity(Guid entityId, DateOnly from, DateOnly to)
    {
        var entries = await _repo.GetChronologicalAsync(entityId, from, to);
        string? expectedPreviousHash = entries.First().PreviousHash;

        foreach (var entry in entries)
        {
            var computed = ComputeHash(entry, entry.PreviousHash ?? "");
            if (computed != entry.Hash) return false;
        }
        return true;
    }
}
```

---

## 9. GDPR Compliance

### Data Categories

| Category | Examples | Basis | Retention |
|----------|---------|-------|-----------|
| Account data | Email, name, role | Contract | Account lifetime + 30 days |
| Audit trail | Actions, IP addresses | Legitimate interest | 7 years (GoBD) |
| Financial data | Journal entries, KPIs | Contract + legal obligation | 10 years (AO §147) |
| AI interaction logs | Prompts (no PII), responses | Legitimate interest | 90 days |
| Session data | Tokens, device fingerprints | Contract | Session duration |

### Data Subject Rights Implementation

| Right | Endpoint | Implementation |
|-------|----------|---------------|
| Access (Art. 15) | `GET /api/v1/privacy/export` | Export all personal data as JSON |
| Rectification (Art. 16) | `PUT /api/v1/profile` | User updates own profile |
| Erasure (Art. 17) | `POST /api/v1/privacy/delete` | Anonymize user data (cannot delete audit trail) |
| Portability (Art. 20) | `GET /api/v1/privacy/export?format=json` | Machine-readable export |

### Anonymization (Right to Erasure)

```csharp
// Anonymize user data while preserving audit integrity
public async Task AnonymizeUser(Guid userId)
{
    var user = await _userRepo.GetAsync(userId);

    // Replace PII with anonymous identifiers
    user.Email = $"deleted-{user.Id}@anonymized.local";
    user.Name = "Deleted User";
    user.Phone = null;

    // Revoke all sessions
    await _tokenRepo.RevokeAllForUser(userId);

    // Mark as anonymized
    user.IsAnonymized = true;
    user.AnonymizedAt = DateTimeOffset.UtcNow;

    // Audit trail entries remain (with anonymized user reference)
    await _userRepo.UpdateAsync(user);
}
```

---

## 10. Dependency Security

```yaml
# Automated vulnerability scanning
# .github/workflows/security.yml
name: Security Scan

on:
  schedule:
    - cron: '0 6 * * 1'  # Weekly Monday 06:00
  push:
    branches: [main]

jobs:
  dotnet-audit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: dotnet list package --vulnerable --include-transitive
      - run: dotnet list package --outdated

  npm-audit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: cd src/frontend && npm audit --audit-level=high

  docker-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: aquasecurity/trivy-action@master
        with:
          scan-type: 'fs'
          severity: 'HIGH,CRITICAL'
```

---

## Document Navigation

- Previous: [Deployment & Infrastructure](./10-deployment.md)
- Next: [Integration Architecture](./12-integration-architecture.md)
- [Back to Index](./README.md)
