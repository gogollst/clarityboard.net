# Security & Compliance

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Authentication

### JWT + Optional 2FA

```
Login Flow:

1. User submits email + password
   → POST /api/v1/auth/login { email, password }

2. Password verified against bcrypt hash (cost factor 12)
   → If invalid: increment failed attempts counter
   → If 5 failed attempts: lock account for 15 minutes (escalating)

3. If 2FA enabled:
   → Return { requires2FA: true, challengeToken: "..." }
   → User enters TOTP code from authenticator app
   → POST /api/v1/auth/verify-2fa { challengeToken, totpCode }

4. On success:
   → Issue access token (JWT, 15 min expiry)
   → Issue refresh token (opaque, 7 days, rotating)
   → Store refresh token hash in DB with device fingerprint

5. Token refresh:
   → POST /api/v1/auth/refresh { refreshToken }
   → Validate refresh token (hash + device fingerprint + expiry)
   → Issue new access + refresh token pair
   → Invalidate old refresh token (rotation)

6. Logout:
   → POST /api/v1/auth/logout
   → Invalidate refresh token in DB
   → Client discards access token
```

### JWT Token Structure

```json
{
  "header": {
    "alg": "RS256",
    "typ": "JWT",
    "kid": "key-2026-01"
  },
  "payload": {
    "sub": "usr_abc123",
    "iss": "clarityboard",
    "iat": 1709049600,
    "exp": 1709050500,
    "role": "finance",
    "entities": ["ent_001", "ent_002"],
    "permissions": ["kpi:read", "kpi:finance", "scenario:write", "datev:export"],
    "sessionId": "sess_xyz789"
  }
}
```

### Password Policy

| Rule | Requirement |
|------|------------|
| Minimum length | 12 characters |
| Complexity | Uppercase + lowercase + number + special character |
| Breach check | Validated against Have I Been Pwned API (k-anonymity) |
| Max failed attempts | 5 → 15 min lockout, 10 → 1 hour, 15 → account disabled |
| Password rotation | Every 90 days (configurable, can be disabled) |
| History | Cannot reuse last 5 passwords |
| Session limit | Maximum 5 active sessions per user |

### 2FA Configuration

| Feature | Detail |
|---------|--------|
| Method | TOTP (RFC 6238) - Google Authenticator, Authy, etc. |
| Enforcement | Optional per user, can be mandated per role by Admin |
| Recovery | 10 backup codes generated at setup (one-time use) |
| Grace period | New users have 7 days to set up 2FA if mandated |
| Device trust | Optional "remember this device for 30 days" |

---

## 2. Role-Based Access Control (RBAC)

### Permission Matrix

| Permission | Admin | Finance | Sales | Marketing | HR | Executive | Auditor |
|-----------|-------|---------|-------|-----------|-----|-----------|---------|
| View Financial KPIs | Y | Y | - | - | - | Y | Y |
| View Sales KPIs | Y | Y | Y | - | - | Y | Y |
| View Marketing KPIs | Y | Y | - | Y | - | Y | Y |
| View HR KPIs | Y | - | - | - | Y | Y | Y |
| View General KPIs | Y | Y | Y | Y | Y | Y | Y |
| Create/Edit Scenarios | Y | Y | - | - | - | Y | - |
| View Scenarios | Y | Y | Y | Y | Y | Y | Y |
| Create/Edit Budgets | Y | Y | - | - | - | Y | - |
| Process Documents | Y | Y | - | - | - | - | - |
| Export DATEV | Y | Y | - | - | - | - | Y |
| Configure Webhooks | Y | - | - | - | - | - | - |
| Manage Users | Y | - | - | - | - | - | - |
| Manage Entities | Y | - | - | - | - | - | - |
| View Audit Logs | Y | - | - | - | - | - | Y |
| System Configuration | Y | - | - | - | - | - | - |
| Generate Reports | Y | Y | Y | Y | Y | Y | Y |
| Natural Language Query | Y | Y | Y | Y | Y | Y | Y |
| View Consolidated | Y | Y | - | - | - | Y | Y |

### Entity-Level Access

Users can be restricted to specific entities:

```json
{
  "userId": "usr_controller_a",
  "role": "finance",
  "entityAccess": {
    "entities": ["ent_company_a", "ent_subsidiary_a1"],
    "consolidatedAccess": false,
    "crossEntityComparison": false
  }
}
```

### Permission Enforcement

```
Every API request:
  1. Extract JWT from Authorization header
  2. Validate JWT signature and expiry
  3. Extract user role and entity access
  4. Check requested resource against permission matrix
  5. Check requested entity against user's entity access list
  6. If consolidated view: verify user has access to ALL included entities
  7. Log access attempt (success or denied) in audit log
  8. Return 403 Forbidden if any check fails
```

---

## 3. Data Security

### Encryption

| Layer | Standard | Detail |
|-------|----------|--------|
| **Transport** | TLS 1.3 (min TLS 1.2) | HSTS enabled, certificate pinning for mobile |
| **At Rest (Database)** | AES-256 | PostgreSQL TDE or disk-level encryption |
| **At Rest (Documents)** | AES-256 | Each document encrypted with entity-specific key |
| **At Rest (Backups)** | AES-256 | Backup encryption with separate key |
| **Secrets** | HashiCorp Vault / env vars | API keys, DB credentials, JWT signing keys |
| **Connections** | TLS | DB connections, Redis, message queue |

### Application Security

| Measure | Implementation |
|---------|---------------|
| **Input Validation** | All inputs validated against schema (type, length, format) |
| **SQL Injection Prevention** | Entity Framework parameterized queries exclusively |
| **XSS Protection** | Content-Security-Policy headers, output encoding in React |
| **CSRF Protection** | SameSite cookies, CSRF token for state-changing operations |
| **Rate Limiting** | 100 requests/minute per user (configurable per endpoint) |
| **Request Size Limits** | 5 MB max request body (20 MB for document upload) |
| **CORS** | Strict origin allowlist |
| **Security Headers** | X-Content-Type-Options, X-Frame-Options, Referrer-Policy |
| **Dependency Scanning** | Automated CVE scanning on build, weekly for runtime |

---

## 4. GDPR Compliance

### Data Inventory

| Data Category | Fields | Legal Basis | Retention |
|--------------|--------|-------------|-----------|
| **User Accounts** | Name, email, role | Legitimate interest (Art. 6(1)(f)) | Duration of account + 30 days |
| **Financial Data** | Invoices, transactions, KPIs | Legal obligation (Art. 6(1)(c)) - HGB | 10 years |
| **Employee Data** | Names, salaries (from HR webhook) | Contract performance (Art. 6(1)(b)) | Duration of employment + statutory |
| **Customer Data** | Names, addresses (from CRM) | Contract performance (Art. 6(1)(b)) | Duration of relationship + statutory |
| **Audit Logs** | User actions, timestamps, IPs | Legitimate interest (Art. 6(1)(f)) | 10 years |
| **AI Processing Logs** | Anonymized requests/responses | Legitimate interest (Art. 6(1)(f)) | 1 year |
| **Documents** | Uploaded receipts/invoices | Legal obligation (Art. 6(1)(c)) - GoBD | 10 years |

### Data Subject Rights Implementation

| Right | Implementation | SLA |
|-------|---------------|-----|
| **Access (Art. 15)** | Export all personal data as JSON/PDF | 30 days |
| **Rectification (Art. 16)** | Edit personal data fields, correction journal entries | 30 days |
| **Erasure (Art. 17)** | Anonymize personal data (except legally required retention) | 30 days |
| **Portability (Art. 20)** | Export in machine-readable format (JSON) | 30 days |
| **Restriction (Art. 18)** | Mark data as restricted, exclude from processing | Immediate |
| **Objection (Art. 21)** | Opt out of AI profiling, optional data processing | Immediate |

### Data Processing Agreements

Required for:
- Each AI provider (Anthropic, xAI, DeepL, ElevenLabs, Google)
- Cloud hosting provider
- Backup service provider
- Email notification service
- Any subprocessor

Content per GDPR Art. 28:
- Subject matter and duration of processing
- Nature and purpose of processing
- Types of personal data
- Categories of data subjects
- Obligations and rights of the controller
- Technical and organizational measures
- Sub-processor management
- Audit rights

---

## 5. Audit Logging

### What is Logged

Every significant action creates an audit log entry:

```json
{
  "id": "aud_2026022714230001",
  "timestamp": "2026-02-27T14:23:45.123Z",
  "userId": "usr_abc123",
  "userRole": "finance",
  "action": "DATEV_EXPORT",
  "resource": "datev_export",
  "entityId": "ent_company_a",
  "details": {
    "period": "2026-01",
    "recordCount": 1247,
    "totalDebits": 2456789.00,
    "totalCredits": 2456789.00,
    "checksum": "sha256:a1b2c3..."
  },
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)...",
  "sessionId": "sess_xyz789",
  "result": "SUCCESS",
  "correlationId": "corr_001"
}
```

### Logged Actions

| Category | Actions |
|----------|---------|
| **Authentication** | Login, logout, failed login, 2FA challenge, password change, account lockout |
| **Authorization** | Permission denied, role change, entity access change |
| **Data Access** | KPI view, report generation, export, document download |
| **Data Modification** | Journal entry create/edit, scenario create/edit, budget change |
| **Configuration** | User management, entity config, webhook setup, role changes |
| **System** | Webhook processing, daily recalculation, DATEV export, AI requests |
| **Security** | Suspicious activity, rate limit hit, invalid token usage |

### Log Integrity

- **Append-only**: Logs cannot be modified or deleted (even by Admin)
- **Hash-chained**: Each entry includes hash of previous entry
- **Retention**: 10 years minimum (GoBD compliance)
- **Access**: Only Admin and Auditor roles can view logs
- **Export**: Logs exportable for external audit tools
- **Tamper detection**: Chain verification runs daily

---

## 6. OWASP Top 10 Mitigation

| Risk | Mitigation |
|------|-----------|
| **A01: Broken Access Control** | RBAC enforced on every endpoint, entity-level isolation, permission matrix |
| **A02: Cryptographic Failures** | TLS 1.3, AES-256 at rest, bcrypt for passwords, RS256 for JWT |
| **A03: Injection** | Entity Framework parameterized queries, no raw SQL, input validation |
| **A04: Insecure Design** | Threat modeling before implementation, principle of least privilege |
| **A05: Security Misconfiguration** | Hardened defaults, no debug in production, automated security scanning |
| **A06: Vulnerable Components** | Automated dependency scanning (Snyk/Dependabot), patch SLA: critical 24h |
| **A07: Auth Failures** | JWT + 2FA, rate limiting, account lockout, session management |
| **A08: Data Integrity Failures** | Hash-chained audit logs, signed JWT, immutable event log |
| **A09: Logging & Monitoring** | Comprehensive audit logging, anomaly alerting, log integrity verification |
| **A10: SSRF** | Webhook URL validation, IP allowlisting, no internal network access from webhook handler |

---

## 7. Incident Response

### Response Plan

| Severity | Response Time | Escalation |
|----------|-------------|------------|
| **P1 (Critical)** | < 1 hour | CTO + Security Officer immediately |
| **P2 (High)** | < 4 hours | Security team + CTO |
| **P3 (Medium)** | < 24 hours | Security team |
| **P4 (Low)** | < 72 hours | Addressed in next sprint |

### Breach Notification (GDPR Art. 33/34)

- **Supervisory Authority**: Within 72 hours of awareness
- **Data Subjects**: Without undue delay if high risk
- **Internal**: Immediate escalation to DPO and management
- **Documentation**: Full incident report within 7 days

---

## Document Navigation

- Previous: [AI Integrations](./17-ai-integrations.md)
- Next: [UI/UX Principles](./19-ui-ux-principles.md)
- [Back to Index](./README.md)
