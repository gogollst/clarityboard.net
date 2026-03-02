# Mail Infrastructure Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a universal, reusable email service for all system notifications (password reset, user invitation, 2FA codes, system warnings, welcome emails) with SMTP config stored in the database.

**Architecture:** `IEmailService` interface in the Application layer, `SmtpEmailService` in Infrastructure with retry logic (3 attempts, exponential backoff) and email logging. `MailConfig` entity (schema `mail`) stores encrypted SMTP credentials in the DB – loaded once and cached in Redis. `EmailLog` entity tracks every sent/failed message.

**Tech Stack:** .NET 9, System.Net.Mail (built-in SMTP), Polly (already in project), EF Core, IEncryptionService (AES-256-GCM, already exists), Redis cache (already exists), FluentValidation, MediatR.

---

## Conventions (read before starting)

- Entity pattern: **private constructor + static `Create()` factory + private setters**
- CQRS: command record → validator class → handler class, all in **one file**
- Permission attribute: `[RequirePermission("admin.mail.manage")]`
- Schema for new tables: **`mail`**
- Migration timestamp format: see existing files in `Migrations/` folder – use `20260302200000` prefix
- `IEncryptionService` is in `ClarityBoard.Infrastructure.Services.AI.AesEncryptionService` (registered as singleton)
- `ICacheService` for Redis cache (already registered as singleton)
- No `dotnet` CLI available locally – write migration `.cs` file manually, no Designer.cs needed for `MigrateAsync()` to run
- Test with: `cd src/frontend && npm run build` (no backend test runner available)

---

## Task 1: Domain Entities

**Files:**
- Create: `src/backend/src/ClarityBoard.Domain/Entities/Mail/MailConfig.cs`
- Create: `src/backend/src/ClarityBoard.Domain/Entities/Mail/EmailLog.cs`
- Modify: `src/backend/src/ClarityBoard.Domain/Entities/Identity/User.cs`

### Step 1: Create `MailConfig.cs`

```csharp
namespace ClarityBoard.Domain.Entities.Mail;

/// <summary>
/// Singleton SMTP configuration stored in the database.
/// Password is AES-256-GCM encrypted via IEncryptionService.
/// </summary>
public class MailConfig
{
    public Guid Id { get; private set; }
    public string Host { get; private set; } = default!;
    public int Port { get; private set; }
    public string Username { get; private set; } = default!;
    /// <summary>AES-256-GCM encrypted SMTP password.</summary>
    public string EncryptedPassword { get; private set; } = default!;
    public string FromEmail { get; private set; } = default!;
    public string FromName { get; private set; } = default!;
    public bool EnableSsl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private MailConfig() { }

    public static MailConfig Create(
        string host, int port, string username, string encryptedPassword,
        string fromEmail, string fromName, bool enableSsl)
    {
        return new MailConfig
        {
            Id                = Guid.NewGuid(),
            Host              = host,
            Port              = port,
            Username          = username,
            EncryptedPassword = encryptedPassword,
            FromEmail         = fromEmail,
            FromName          = fromName,
            EnableSsl         = enableSsl,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow,
        };
    }

    public void Update(string host, int port, string username, string encryptedPassword,
        string fromEmail, string fromName, bool enableSsl)
    {
        Host              = host;
        Port              = port;
        Username          = username;
        EncryptedPassword = encryptedPassword;
        FromEmail         = fromEmail;
        FromName          = fromName;
        EnableSsl         = enableSsl;
        UpdatedAt         = DateTime.UtcNow;
    }
}
```

### Step 2: Create `EmailLog.cs`

```csharp
namespace ClarityBoard.Domain.Entities.Mail;

public enum EmailType
{
    WelcomeEmail       = 1,
    PasswordReset      = 2,
    UserInvitation     = 3,
    TwoFactorCode      = 4,
    SystemWarning      = 5,
}

public enum EmailStatus
{
    Sent    = 1,
    Failed  = 2,
    Retried = 3,
}

public class EmailLog
{
    public Guid Id { get; private set; }
    public EmailType Type { get; private set; }
    public string ToEmail { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public EmailStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime SentAt { get; private set; }

    private EmailLog() { }

    public static EmailLog Create(
        EmailType type, string toEmail, string subject,
        EmailStatus status, int attempts, string? errorMessage, Guid? userId)
    {
        return new EmailLog
        {
            Id           = Guid.NewGuid(),
            Type         = type,
            ToEmail      = toEmail,
            Subject      = subject,
            Status       = status,
            Attempts     = attempts,
            ErrorMessage = errorMessage,
            UserId       = userId,
            SentAt       = DateTime.UtcNow,
        };
    }
}
```

### Step 3: Add reset-token fields to `User.cs`

Read `User.cs` first, then add two nullable properties (with private setters) and a domain method:

```csharp
// Add to the User class (after existing properties):
public string? PasswordResetToken { get; private set; }
public DateTime? PasswordResetTokenExpiry { get; private set; }

// Add method:
public void SetPasswordResetToken(string token, DateTime expiry)
{
    PasswordResetToken       = token;
    PasswordResetTokenExpiry = expiry;
    UpdatedAt                = DateTime.UtcNow;
}

public void ClearPasswordResetToken()
{
    PasswordResetToken       = null;
    PasswordResetTokenExpiry = null;
    UpdatedAt                = DateTime.UtcNow;
}
```

### Step 4: Commit

```bash
cd /home/stefan/Documents/GitHub/clarityboard.net
git add src/backend/src/ClarityBoard.Domain/Entities/Mail/ \
        src/backend/src/ClarityBoard.Domain/Entities/Identity/User.cs
git commit -m "feat(mail): add MailConfig, EmailLog entities and User reset-token fields"
```

---

## Task 2: Application Interface

**Files:**
- Create: `src/backend/src/ClarityBoard.Application/Common/Interfaces/IEmailService.cs`

### Step 1: Create `IEmailService.cs`

```csharp
using ClarityBoard.Domain.Entities.Mail;

namespace ClarityBoard.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>Sends a welcome email to a newly created user.</summary>
    Task SendWelcomeEmailAsync(string toEmail, string firstName, string temporaryPassword, CancellationToken ct = default);

    /// <summary>Sends a password-reset link with a time-limited token.</summary>
    Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetToken, CancellationToken ct = default);

    /// <summary>Sends an invitation email with a temporary password.</summary>
    Task SendInvitationEmailAsync(string toEmail, string firstName, string temporaryPassword, string invitedBy, CancellationToken ct = default);

    /// <summary>Sends a 6-digit 2FA code via email.</summary>
    Task SendTwoFactorCodeEmailAsync(string toEmail, string firstName, string code, CancellationToken ct = default);

    /// <summary>Sends a system warning to admin addresses.</summary>
    Task SendSystemWarningAsync(string subject, string bodyText, IEnumerable<string> adminEmails, CancellationToken ct = default);
}
```

### Step 2: Commit

```bash
git add src/backend/src/ClarityBoard.Application/Common/Interfaces/IEmailService.cs
git commit -m "feat(mail): add IEmailService interface"
```

---

## Task 3: EF Core – DbContext & Migration

**Files:**
- Modify: `src/backend/src/ClarityBoard.Application/Common/Interfaces/IAppDbContext.cs`
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Persistence/ClarityBoardContext.cs`
- Create: `src/backend/src/ClarityBoard.Infrastructure/Persistence/Migrations/20260302200000_AddMailInfrastructure.cs`

### Step 1: Add DbSets to `IAppDbContext.cs`

After the `// AI Management` section add:

```csharp
// Mail
DbSet<MailConfig> MailConfigs { get; }
DbSet<EmailLog> EmailLogs { get; }
```

Also add the using at the top:
```csharp
using ClarityBoard.Domain.Entities.Mail;
```

### Step 2: Add DbSets + EF configuration to `ClarityBoardContext.cs`

Read the file first. Then:

a) Add DbSets (near the AI Management section):
```csharp
// Mail
public DbSet<MailConfig> MailConfigs => Set<MailConfig>();
public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
```

b) In `OnModelCreating`, add (near the AI configuration):
```csharp
// ── Mail ──────────────────────────────────────────────────────────────────
modelBuilder.Entity<MailConfig>(e =>
{
    e.ToTable("mail_configs", "mail");
    e.HasKey(x => x.Id);
    e.Property(x => x.Host).HasMaxLength(500).IsRequired();
    e.Property(x => x.Username).HasMaxLength(500).IsRequired();
    e.Property(x => x.EncryptedPassword).HasMaxLength(2000).IsRequired();
    e.Property(x => x.FromEmail).HasMaxLength(256).IsRequired();
    e.Property(x => x.FromName).HasMaxLength(256).IsRequired();
});

modelBuilder.Entity<EmailLog>(e =>
{
    e.ToTable("email_logs", "mail");
    e.HasKey(x => x.Id);
    e.Property(x => x.ToEmail).HasMaxLength(256).IsRequired();
    e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
    e.Property(x => x.ErrorMessage).HasMaxLength(2000);
    e.HasIndex(x => x.SentAt);
    e.HasIndex(x => x.UserId);
});
```

c) In the User entity configuration in `OnModelCreating`, add:
```csharp
e.Property(x => x.PasswordResetToken).HasMaxLength(256);
e.Property(x => x.PasswordResetTokenExpiry);
```

(Read the existing User configuration block to find the right place.)

### Step 3: Create migration file manually

Create `src/backend/src/ClarityBoard.Infrastructure/Persistence/Migrations/20260302200000_AddMailInfrastructure.cs`:

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClarityBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMailInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "mail");

            migrationBuilder.CreateTable(
                name: "mail_configs",
                schema: "mail",
                columns: table => new
                {
                    Id                = table.Column<Guid>(type: "uuid", nullable: false),
                    Host              = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Port              = table.Column<int>(type: "integer", nullable: false),
                    Username          = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FromEmail         = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FromName          = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EnableSsl         = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive          = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                schema: "mail",
                columns: table => new
                {
                    Id           = table.Column<Guid>(type: "uuid", nullable: false),
                    Type         = table.Column<int>(type: "integer", nullable: false),
                    ToEmail      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject      = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status       = table.Column<int>(type: "integer", nullable: false),
                    Attempts     = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UserId       = table.Column<Guid>(type: "uuid", nullable: true),
                    SentAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_SentAt",
                schema: "mail",
                table: "email_logs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_UserId",
                schema: "mail",
                table: "email_logs",
                column: "UserId");

            // Add reset-token columns to existing users table
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                schema: "public",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "email_logs", schema: "mail");
            migrationBuilder.DropTable(name: "mail_configs", schema: "mail");
            migrationBuilder.DropColumn(name: "PasswordResetToken", schema: "public", table: "users");
            migrationBuilder.DropColumn(name: "PasswordResetTokenExpiry", schema: "public", table: "users");
        }
    }
}
```

**Note:** Check the actual schema name of the `users` table in `ClarityBoardContext.cs` – it might be `"public"` or just no schema. Read the User entity configuration first.

### Step 4: Commit

```bash
git add src/backend/src/ClarityBoard.Application/Common/Interfaces/IAppDbContext.cs \
        src/backend/src/ClarityBoard.Infrastructure/Persistence/ClarityBoardContext.cs \
        src/backend/src/ClarityBoard.Infrastructure/Persistence/Migrations/20260302200000_AddMailInfrastructure.cs
git commit -m "feat(mail): EF Core DbSets, entity config, and migration"
```

---

## Task 4: HTML Email Templates

**Files:**
- Create: `src/backend/src/ClarityBoard.Infrastructure/Services/Mail/EmailTemplates.cs`

### Step 1: Create `EmailTemplates.cs`

```csharp
namespace ClarityBoard.Infrastructure.Services.Mail;

/// <summary>
/// Provides branded HTML email templates for all email types.
/// Uses inline styles for maximum email client compatibility.
/// </summary>
internal static class EmailTemplates
{
    private const string BrandColor = "#4F46E5";
    private const string LogoText   = "Clarity Board";

    private static string Wrap(string title, string content) => $"""
        <!DOCTYPE html>
        <html lang="de">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>{title}</title>
        </head>
        <body style="margin:0;padding:0;background:#f8fafc;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f8fafc;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="560" cellpadding="0" cellspacing="0"
                       style="background:#ffffff;border-radius:12px;border:1px solid #e2e8f0;overflow:hidden;">
                  <!-- Header -->
                  <tr>
                    <td style="background:{BrandColor};padding:24px 32px;">
                      <span style="color:#ffffff;font-size:20px;font-weight:700;letter-spacing:-0.5px;">{LogoText}</span>
                    </td>
                  </tr>
                  <!-- Body -->
                  <tr>
                    <td style="padding:32px;color:#1e293b;font-size:15px;line-height:1.6;">
                      {content}
                    </td>
                  </tr>
                  <!-- Footer -->
                  <tr>
                    <td style="background:#f1f5f9;padding:16px 32px;border-top:1px solid #e2e8f0;">
                      <p style="margin:0;font-size:12px;color:#64748b;text-align:center;">
                        &copy; {DateTime.UtcNow.Year} ClarityBoard &middot;
                        Diese E-Mail wurde automatisch generiert. Bitte nicht antworten.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string Button(string href, string label) =>
        $"""<a href="{href}" style="display:inline-block;background:{BrandColor};color:#ffffff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;font-size:15px;margin:16px 0;">{label}</a>""";

    // ── 1. Welcome Email ──────────────────────────────────────────────────

    public static (string subject, string html) Welcome(string firstName, string tempPassword, string appUrl)
    {
        var subject = $"Willkommen bei {LogoText}";
        var content = $"""
            <h2 style="margin:0 0 16px;font-size:22px;font-weight:700;color:#1e293b;">
              Willkommen, {firstName}!
            </h2>
            <p>Ihr Konto bei <strong>{LogoText}</strong> wurde erfolgreich erstellt.</p>
            <p>Hier sind Ihre Zugangsdaten:</p>
            <table style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px 20px;margin:16px 0;width:100%;">
              <tr><td style="color:#64748b;font-size:13px;">Temporäres Passwort</td></tr>
              <tr><td style="font-size:18px;font-weight:700;letter-spacing:2px;color:{BrandColor};">{tempPassword}</td></tr>
            </table>
            <p>Bitte ändern Sie Ihr Passwort nach der ersten Anmeldung.</p>
            {Button(appUrl, "Jetzt anmelden")}
            <p style="font-size:13px;color:#64748b;margin-top:24px;">
              Falls Sie diese E-Mail nicht erwartet haben, können Sie sie ignorieren.
            </p>
            """;
        return (subject, Wrap(subject, content));
    }

    // ── 2. Password Reset ─────────────────────────────────────────────────

    public static (string subject, string html) PasswordReset(string firstName, string resetUrl)
    {
        var subject = "Passwort zurücksetzen";
        var content = $"""
            <h2 style="margin:0 0 16px;font-size:22px;font-weight:700;color:#1e293b;">
              Passwort zurücksetzen
            </h2>
            <p>Hallo {firstName},</p>
            <p>wir haben eine Anfrage erhalten, das Passwort für Ihr Konto zurückzusetzen.</p>
            {Button(resetUrl, "Passwort zurücksetzen")}
            <p style="font-size:13px;color:#64748b;">
              Dieser Link ist <strong>15 Minuten</strong> gültig. Falls Sie keine
              Zurücksetzung angefordert haben, können Sie diese E-Mail ignorieren.
            </p>
            """;
        return (subject, Wrap(subject, content));
    }

    // ── 3. User Invitation ────────────────────────────────────────────────

    public static (string subject, string html) Invitation(
        string firstName, string tempPassword, string invitedBy, string appUrl)
    {
        var subject = $"Sie wurden zu {LogoText} eingeladen";
        var content = $"""
            <h2 style="margin:0 0 16px;font-size:22px;font-weight:700;color:#1e293b;">
              Sie wurden eingeladen!
            </h2>
            <p>Hallo {firstName},</p>
            <p><strong>{invitedBy}</strong> hat Sie zu <strong>{LogoText}</strong> eingeladen.</p>
            <p>Hier sind Ihre Zugangsdaten:</p>
            <table style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px 20px;margin:16px 0;width:100%;">
              <tr><td style="color:#64748b;font-size:13px;">Temporäres Passwort</td></tr>
              <tr><td style="font-size:18px;font-weight:700;letter-spacing:2px;color:{BrandColor};">{tempPassword}</td></tr>
            </table>
            <p>Bitte ändern Sie Ihr Passwort nach der ersten Anmeldung.</p>
            {Button(appUrl, "Einladung annehmen")}
            """;
        return (subject, Wrap(subject, content));
    }

    // ── 4. Two-Factor Code ────────────────────────────────────────────────

    public static (string subject, string html) TwoFactorCode(string firstName, string code)
    {
        var subject = $"Ihr Bestätigungscode für {LogoText}";
        var content = $"""
            <h2 style="margin:0 0 16px;font-size:22px;font-weight:700;color:#1e293b;">
              Ihr Bestätigungscode
            </h2>
            <p>Hallo {firstName},</p>
            <p>Hier ist Ihr Einmalcode zur Anmeldung:</p>
            <table style="background:#f8fafc;border:2px solid {BrandColor};border-radius:12px;padding:20px;margin:16px auto;text-align:center;">
              <tr><td style="font-size:36px;font-weight:700;letter-spacing:8px;color:{BrandColor};">{code}</td></tr>
            </table>
            <p style="font-size:13px;color:#64748b;">
              Dieser Code ist <strong>10 Minuten</strong> gültig.
              Falls Sie diese Anfrage nicht gestellt haben, sperren Sie bitte umgehend Ihr Konto.
            </p>
            """;
        return (subject, Wrap(subject, content));
    }

    // ── 5. System Warning ─────────────────────────────────────────────────

    public static (string subject, string html) SystemWarning(string subject, string bodyText)
    {
        var content = $"""
            <h2 style="margin:0 0 16px;font-size:22px;font-weight:700;color:#DC2626;">
              ⚠ Systemwarnung
            </h2>
            <p><strong>{subject}</strong></p>
            <pre style="background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:16px;font-size:13px;color:#991b1b;white-space:pre-wrap;word-break:break-all;">{bodyText}</pre>
            <p style="font-size:13px;color:#64748b;">
              Zeitstempel: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            </p>
            """;
        return ($"[ClarityBoard] {subject}", Wrap(subject, content));
    }
}
```

### Step 2: Commit

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Services/Mail/EmailTemplates.cs
git commit -m "feat(mail): add branded HTML email templates"
```

---

## Task 5: SmtpEmailService Implementation

**Files:**
- Create: `src/backend/src/ClarityBoard.Infrastructure/Services/Mail/SmtpEmailService.cs`

### Step 1: Create `SmtpEmailService.cs`

```csharp
using System.Net;
using System.Net.Mail;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Mail;

/// <summary>
/// SMTP email service with retry logic (3 attempts, exponential backoff).
/// Loads SMTP config from the database (cached in Redis for 10 minutes).
/// Writes an EmailLog entry for every attempted send.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    // App URL for links in emails – can be overridden via appsettings
    private const string DefaultAppUrl = "https://app.clarityboard.net";
    private static readonly TimeSpan ConfigCacheTtl = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _sp;
    private readonly ICacheService _cache;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IServiceProvider sp,
        ICacheService cache,
        IEncryptionService encryption,
        ILogger<SmtpEmailService> logger)
    {
        _sp         = sp;
        _cache      = cache;
        _encryption = encryption;
        _logger     = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public Task SendWelcomeEmailAsync(string toEmail, string firstName, string temporaryPassword, CancellationToken ct)
    {
        var (subject, html) = EmailTemplates.Welcome(firstName, temporaryPassword, DefaultAppUrl);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.WelcomeEmail, null, ct);
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetToken, CancellationToken ct)
    {
        var resetUrl = $"{DefaultAppUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        var (subject, html) = EmailTemplates.PasswordReset(firstName, resetUrl);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.PasswordReset, null, ct);
    }

    public Task SendInvitationEmailAsync(string toEmail, string firstName, string temporaryPassword, string invitedBy, CancellationToken ct)
    {
        var (subject, html) = EmailTemplates.Invitation(firstName, temporaryPassword, invitedBy, DefaultAppUrl);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.UserInvitation, null, ct);
    }

    public Task SendTwoFactorCodeEmailAsync(string toEmail, string firstName, string code, CancellationToken ct)
    {
        var (subject, html) = EmailTemplates.TwoFactorCode(firstName, code);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.TwoFactorCode, null, ct);
    }

    public async Task SendSystemWarningAsync(string subject, string bodyText, IEnumerable<string> adminEmails, CancellationToken ct)
    {
        var (emailSubject, html) = EmailTemplates.SystemWarning(subject, bodyText);
        foreach (var email in adminEmails)
            await SendWithRetryAsync(email, emailSubject, html, EmailType.SystemWarning, null, ct);
    }

    // ── Core Send with Retry ──────────────────────────────────────────────

    private async Task SendWithRetryAsync(
        string toEmail, string subject, string htmlBody,
        EmailType type, Guid? userId, CancellationToken ct)
    {
        const int maxAttempts = 3;
        Exception? lastEx = null;
        int attempt = 0;

        for (attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt > 1)
            {
                // Exponential backoff: 2s, 4s
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning("Email send attempt {Attempt}/{Max} to {Email}. Waiting {Delay}s...",
                    attempt, maxAttempts, toEmail, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }

            try
            {
                var config = await LoadMailConfigAsync(ct)
                    ?? throw new InvalidOperationException("No mail configuration found. Configure SMTP in the admin panel.");

                var password = _encryption.Decrypt(config.EncryptedPassword);

                using var client = new SmtpClient(config.Host, config.Port)
                {
                    Credentials    = new NetworkCredential(config.Username, password),
                    EnableSsl      = config.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout        = 15_000, // 15s per attempt
                };

                using var message = new MailMessage
                {
                    From       = new MailAddress(config.FromEmail, config.FromName),
                    Subject    = subject,
                    Body       = htmlBody,
                    IsBodyHtml = true,
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message, ct);

                await WriteLogAsync(type, toEmail, subject, EmailStatus.Sent, attempt, null, userId, ct);
                _logger.LogInformation("Email [{Type}] sent to {Email} on attempt {Attempt}.", type, toEmail, attempt);
                return; // success
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastEx = ex;
                _logger.LogWarning(ex, "Email [{Type}] to {Email}: attempt {Attempt} failed.", type, toEmail, attempt);
            }
        }

        // All attempts failed
        await WriteLogAsync(type, toEmail, subject, EmailStatus.Failed, attempt - 1, lastEx?.Message, userId, ct);
        _logger.LogError(lastEx, "Email [{Type}] to {Email} failed after {Max} attempts.", type, toEmail, maxAttempts);
        // Do not rethrow – email failure should not break the calling command
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<MailConfig?> LoadMailConfigAsync(CancellationToken ct)
    {
        const string cacheKey = "mail:config";

        var cached = await _cache.GetAsync<MailConfigCacheEntry>(cacheKey, ct);
        if (cached is not null)
        {
            // Rebuild domain object from cache entry (we can't cache EF entities)
            return MailConfig.Create(cached.Host, cached.Port, cached.Username,
                cached.EncryptedPassword, cached.FromEmail, cached.FromName, cached.EnableSsl);
        }

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var config = await db.MailConfigs
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (config is not null)
        {
            await _cache.SetAsync(cacheKey, new MailConfigCacheEntry(
                config.Host, config.Port, config.Username,
                config.EncryptedPassword, config.FromEmail, config.FromName, config.EnableSsl),
                ConfigCacheTtl, ct);
        }

        return config;
    }

    private async Task WriteLogAsync(
        EmailType type, string toEmail, string subject,
        EmailStatus status, int attempts, string? error, Guid? userId, CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var log = EmailLog.Create(type, toEmail, subject, status, attempts, error, userId);
            db.EmailLogs.Add(log);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write EmailLog entry.");
        }
    }

    private record MailConfigCacheEntry(
        string Host, int Port, string Username, string EncryptedPassword,
        string FromEmail, string FromName, bool EnableSsl);
}
```

**Note on `ICacheService`:** Check the existing `ICacheService` interface to confirm the method signatures `GetAsync<T>` and `SetAsync<T>`. The `RedisCacheService` uses JSON serialization internally. Read the file at `src/backend/src/ClarityBoard.Application/Common/Interfaces/ICacheService.cs` to confirm, and adjust method calls if needed.

### Step 2: Commit

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Services/Mail/SmtpEmailService.cs
git commit -m "feat(mail): implement SmtpEmailService with retry and email logging"
```

---

## Task 6: Mail Config Admin (CRUD)

**Files:**
- Create: `src/backend/src/ClarityBoard.Application/Features/Admin/Mail/Commands/UpsertMailConfigCommand.cs`
- Create: `src/backend/src/ClarityBoard.Application/Features/Admin/Mail/Queries/GetMailConfigQuery.cs`
- Create: `src/backend/src/ClarityBoard.API/Controllers/Admin/MailConfigController.cs`

### Step 1: Create `UpsertMailConfigCommand.cs`

```csharp
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Mail;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Mail.Commands;

[RequirePermission("admin.mail.manage")]
public record UpsertMailConfigCommand : IRequest
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    /// <summary>Plain-text password – will be encrypted before storage.</summary>
    public required string Password { get; init; }
    public required string FromEmail { get; init; }
    public required string FromName { get; init; }
    public bool EnableSsl { get; init; } = true;
}

public class UpsertMailConfigValidator : AbstractValidator<UpsertMailConfigCommand>
{
    public UpsertMailConfigValidator()
    {
        RuleFor(x => x.Host).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.FromEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FromName).NotEmpty().MaximumLength(256);
    }
}

public class UpsertMailConfigHandler : IRequestHandler<UpsertMailConfigCommand>
{
    private readonly IAppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly ICacheService _cache;

    public UpsertMailConfigHandler(IAppDbContext db, IEncryptionService encryption, ICacheService cache)
    {
        _db         = db;
        _encryption = encryption;
        _cache      = cache;
    }

    public async Task Handle(UpsertMailConfigCommand request, CancellationToken cancellationToken)
    {
        var encryptedPassword = _encryption.Encrypt(request.Password);

        var existing = await _db.MailConfigs
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            var config = MailConfig.Create(
                request.Host, request.Port, request.Username, encryptedPassword,
                request.FromEmail, request.FromName, request.EnableSsl);
            _db.MailConfigs.Add(config);
        }
        else
        {
            existing.Update(request.Host, request.Port, request.Username, encryptedPassword,
                request.FromEmail, request.FromName, request.EnableSsl);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Invalidate cache so SmtpEmailService picks up new config
        await _cache.RemoveAsync("mail:config", cancellationToken);
    }
}
```

### Step 2: Create `GetMailConfigQuery.cs`

```csharp
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Mail.Queries;

[RequirePermission("admin.mail.manage")]
public record GetMailConfigQuery : IRequest<MailConfigResponse?>;

public record MailConfigResponse(
    Guid Id,
    string Host,
    int Port,
    string Username,
    string FromEmail,
    string FromName,
    bool EnableSsl,
    bool IsActive,
    DateTime UpdatedAt);

public class GetMailConfigHandler : IRequestHandler<GetMailConfigQuery, MailConfigResponse?>
{
    private readonly IAppDbContext _db;

    public GetMailConfigHandler(IAppDbContext db) => _db = db;

    public async Task<MailConfigResponse?> Handle(GetMailConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await _db.MailConfigs
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (config is null) return null;

        return new MailConfigResponse(
            config.Id, config.Host, config.Port, config.Username,
            config.FromEmail, config.FromName, config.EnableSsl,
            config.IsActive, config.UpdatedAt);
        // Note: Password is intentionally NOT returned
    }
}
```

### Step 3: Create `MailConfigController.cs`

```csharp
using ClarityBoard.Application.Features.Admin.Mail.Commands;
using ClarityBoard.Application.Features.Admin.Mail.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/mail")]
public class MailConfigController : ControllerBase
{
    private readonly IMediator _mediator;

    public MailConfigController(IMediator mediator) => _mediator = mediator;

    /// <summary>Gets the current SMTP configuration (password not returned).</summary>
    [HttpGet("config")]
    public async Task<ActionResult<MailConfigResponse?>> GetConfig(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMailConfigQuery(), ct);
        return Ok(result);
    }

    /// <summary>Creates or updates the SMTP configuration.</summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpsertConfig([FromBody] UpsertMailConfigCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }
}
```

**Note on `ICacheService.RemoveAsync`:** Check that this method exists on the interface. If it is named differently (e.g., `DeleteAsync`), adjust accordingly. Read `ICacheService.cs` first.

### Step 4: Commit

```bash
git add src/backend/src/ClarityBoard.Application/Features/Admin/Mail/ \
        src/backend/src/ClarityBoard.API/Controllers/Admin/MailConfigController.cs
git commit -m "feat(mail): admin CRUD for SMTP configuration"
```

---

## Task 7: Forgot Password Flow (New Public API)

**Files:**
- Create: `src/backend/src/ClarityBoard.Application/Features/Auth/Commands/ForgotPasswordCommand.cs`
- Create: `src/backend/src/ClarityBoard.Application/Features/Auth/Commands/ResetPasswordViaTokenCommand.cs`
- Modify: `src/backend/src/ClarityBoard.API/Controllers/AuthController.cs`

### Step 1: Create `ForgotPasswordCommand.cs`

```csharp
using System.Security.Cryptography;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

/// <summary>
/// Initiates a password reset. If the email exists, a reset token
/// is generated and an email is sent. Always returns 204 (no information leakage).
/// </summary>
public record ForgotPasswordCommand : IRequest
{
    public required string Email { get; init; }
}

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;

    public ForgotPasswordHandler(IAppDbContext db, IEmailService email)
    {
        _db    = db;
        _email = email;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Always return success to prevent email enumeration
        if (user is null || !user.IsActive) return;

        // Generate a secure 32-byte URL-safe token (expiry: 15 minutes)
        var token  = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                           .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var expiry = DateTime.UtcNow.AddMinutes(15);

        user.SetPasswordResetToken(token, expiry);
        await _db.SaveChangesAsync(cancellationToken);

        await _email.SendPasswordResetEmailAsync(user.Email, user.FirstName, token, cancellationToken);
    }
}
```

### Step 2: Create `ResetPasswordViaTokenCommand.cs`

```csharp
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Auth.Commands;

public record ResetPasswordViaTokenCommand : IRequest
{
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}

public class ResetPasswordViaTokenValidator : AbstractValidator<ResetPasswordViaTokenCommand>
{
    public ResetPasswordViaTokenValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

public class ResetPasswordViaTokenHandler : IRequestHandler<ResetPasswordViaTokenCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordViaTokenHandler(IAppDbContext db, IPasswordHasher passwordHasher)
    {
        _db             = db;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetPasswordViaTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user is null
            || user.PasswordResetTokenExpiry is null
            || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            throw new InvalidOperationException("The password reset token is invalid or has expired.");
        }

        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(newHash);  // read User.cs to confirm method name
        user.ClearPasswordResetToken();
        // Revoke existing refresh tokens so all sessions are invalidated
        var tokens = _db.RefreshTokens.Where(t => t.UserId == user.Id && !t.IsRevoked);
        await tokens.ForEachAsync(t => t.Revoke(), cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

**Note:** Read `User.cs` to confirm the method name for updating the password hash (might be `UpdatePassword`, `SetPasswordHash`, or similar). If it doesn't exist, add it in Task 1's User modifications.

### Step 3: Add endpoints to `AuthController.cs`

Read `AuthController.cs` first, then add:

```csharp
[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
{
    await _mediator.Send(command, ct);
    return NoContent(); // Always 204 – no information leakage
}

[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViaTokenCommand command, CancellationToken ct)
{
    await _mediator.Send(command, ct);
    return NoContent();
}
```

### Step 4: Commit

```bash
git add src/backend/src/ClarityBoard.Application/Features/Auth/Commands/ForgotPasswordCommand.cs \
        src/backend/src/ClarityBoard.Application/Features/Auth/Commands/ResetPasswordViaTokenCommand.cs \
        src/backend/src/ClarityBoard.API/Controllers/AuthController.cs
git commit -m "feat(mail): forgot-password and reset-password-via-token flow"
```

---

## Task 8: Email Integrations into Existing Commands

**Files:**
- Modify: `src/backend/src/ClarityBoard.Application/Features/Admin/Commands/CreateUserCommand.cs`
- Modify: `src/backend/src/ClarityBoard.Application/Features/Admin/Commands/ResetPasswordCommand.cs`

### Step 1: Modify `CreateUserCommand.cs`

Read the file first. Then:

1. Add `IEmailService _email` to the handler constructor
2. Inject `IEmailService email` in the constructor
3. After `await _db.SaveChangesAsync(cancellationToken);` add:

```csharp
// Send invitation email (fire-and-forget – don't fail the user creation)
try
{
    await _email.SendInvitationEmailAsync(
        user.Email, user.FirstName, temporaryPassword,
        invitedBy: _currentUser.UserName ?? "Administrator", cancellationToken);
}
catch (Exception ex)
{
    // Log but don't fail – user was already created successfully
    // (ILogger is not currently in this handler – add it or just swallow)
}
```

**Note on `_currentUser.UserName`:** Check the `ICurrentUser` interface to see what properties are available. Use `UserId.ToString()` as fallback if `UserName` doesn't exist.

### Step 2: Modify `ResetPasswordCommand.cs`

Read the file first. Then:

1. Add `IEmailService _email` to the handler
2. After saving changes, add:

```csharp
// Notify user of the admin-triggered password reset
try
{
    var user2 = await _db.Users
        .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
    if (user2 is not null)
    {
        await _email.SendInvitationEmailAsync(
            user2.Email, user2.FirstName, temporaryPassword,
            invitedBy: "Administrator", cancellationToken);
    }
}
catch { /* swallow – password was already reset */ }
```

(The variable `user` from the existing handler may already hold the user object – check and reuse it to avoid the second DB call.)

### Step 3: Commit

```bash
git add src/backend/src/ClarityBoard.Application/Features/Admin/Commands/CreateUserCommand.cs \
        src/backend/src/ClarityBoard.Application/Features/Admin/Commands/ResetPasswordCommand.cs
git commit -m "feat(mail): integrate email sending into CreateUser and ResetPassword commands"
```

---

## Task 9: Register Services in DependencyInjection.cs

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/DependencyInjection.cs`

### Step 1: Add mail service registration

Add after the existing encryption service registration:

```csharp
// Mail Service (SMTP + retry + DB logging)
services.AddScoped<IEmailService, ClarityBoard.Infrastructure.Services.Mail.SmtpEmailService>();
```

Also add the using directive at the top if needed (the service references interfaces that are already imported).

### Step 2: Commit

```bash
git add src/backend/src/ClarityBoard.Infrastructure/DependencyInjection.cs
git commit -m "feat(mail): register SmtpEmailService in DI container"
```

---

## Task 10: Verify Build & Final Cleanup

### Step 1: Check ICacheService interface

Read `src/backend/src/ClarityBoard.Application/Common/Interfaces/ICacheService.cs` and verify:
- `GetAsync<T>(string key, CancellationToken ct)` method signature
- `SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)` method signature
- `RemoveAsync(string key, CancellationToken ct)` method signature

Adjust `SmtpEmailService.cs` and `UpsertMailConfigCommand.cs` to match the actual interface.

### Step 2: Check User.cs password update method

Read `src/backend/src/ClarityBoard.Domain/Entities/Identity/User.cs` and confirm the method for updating the password hash. In `ResetPasswordViaTokenCommand.cs`, use the correct method name (e.g., `user.UpdatePasswordHash(hash)` or `user.SetPassword(hash)`). If no such method exists, add it in `User.cs`:

```csharp
public void UpdatePasswordHash(string passwordHash)
{
    PasswordHash = passwordHash;
    UpdatedAt    = DateTime.UtcNow;
}
```

### Step 3: Check `users` table schema in ClarityBoardContext.cs

Read the User entity configuration in `ClarityBoardContext.cs` to find the actual schema name for the `users` table. Update the `AddColumn` calls in the migration file (`20260302200000_AddMailInfrastructure.cs`) to use the correct schema (might be `"public"` or omitted).

### Step 4: Build frontend (no backend build locally)

```bash
cd /home/stefan/Documents/GitHub/clarityboard.net/src/frontend
npm run build
npm run lint
```

Both must pass with 0 new errors.

### Step 5: Commit all remaining changes

```bash
cd /home/stefan/Documents/GitHub/clarityboard.net
git add -p   # review all remaining changes
git commit -m "feat(mail): complete mail infrastructure implementation"
```

---

## Task 11: Production Deployment Notes

> These steps must be run manually on the server after `deploy.sh` builds and starts the containers.

### Migration will auto-apply

`SeedData.InitializeAsync()` calls `context.Database.MigrateAsync()`, which will apply `AddMailInfrastructure` automatically on container startup.

### If migration fails, apply SQL manually

```sql
-- Run inside psql on the production database

CREATE SCHEMA IF NOT EXISTS mail;

CREATE TABLE mail.mail_configs (
    "Id"                UUID NOT NULL,
    "Host"              VARCHAR(500) NOT NULL,
    "Port"              INTEGER NOT NULL,
    "Username"          VARCHAR(500) NOT NULL,
    "EncryptedPassword" VARCHAR(2000) NOT NULL,
    "FromEmail"         VARCHAR(256) NOT NULL,
    "FromName"          VARCHAR(256) NOT NULL,
    "EnableSsl"         BOOLEAN NOT NULL,
    "IsActive"          BOOLEAN NOT NULL,
    "CreatedAt"         TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt"         TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT "PK_mail_configs" PRIMARY KEY ("Id")
);

CREATE TABLE mail.email_logs (
    "Id"           UUID NOT NULL,
    "Type"         INTEGER NOT NULL,
    "ToEmail"      VARCHAR(256) NOT NULL,
    "Subject"      VARCHAR(500) NOT NULL,
    "Status"       INTEGER NOT NULL,
    "Attempts"     INTEGER NOT NULL,
    "ErrorMessage" VARCHAR(2000),
    "UserId"       UUID,
    "SentAt"       TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT "PK_email_logs" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_email_logs_SentAt" ON mail.email_logs ("SentAt");
CREATE INDEX "IX_email_logs_UserId" ON mail.email_logs ("UserId");

-- Check actual schema name for users table first!
ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS "PasswordResetToken" VARCHAR(256),
    ADD COLUMN IF NOT EXISTS "PasswordResetTokenExpiry" TIMESTAMP WITH TIME ZONE;

-- Record migration as applied
INSERT INTO public."__EFMigsHistoryHistory" ("MigrationId", "ProductVersion")
VALUES ('20260302200000_AddMailInfrastructure', '9.0.0');
```

### Configure SMTP after deployment

```
PUT https://api.clarityboard.net/api/admin/mail/config
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "host": "smtp.yourprovider.com",
  "port": 587,
  "username": "noreply@clarityboard.net",
  "password": "your_smtp_password",
  "fromEmail": "noreply@clarityboard.net",
  "fromName": "ClarityBoard",
  "enableSsl": true
}
```

---

## Summary of All Files

| Action  | File |
|---------|------|
| Create  | `ClarityBoard.Domain/Entities/Mail/MailConfig.cs` |
| Create  | `ClarityBoard.Domain/Entities/Mail/EmailLog.cs` |
| Modify  | `ClarityBoard.Domain/Entities/Identity/User.cs` |
| Create  | `ClarityBoard.Application/Common/Interfaces/IEmailService.cs` |
| Modify  | `ClarityBoard.Application/Common/Interfaces/IAppDbContext.cs` |
| Modify  | `ClarityBoard.Infrastructure/Persistence/ClarityBoardContext.cs` |
| Create  | `ClarityBoard.Infrastructure/Persistence/Migrations/20260302200000_AddMailInfrastructure.cs` |
| Create  | `ClarityBoard.Infrastructure/Services/Mail/EmailTemplates.cs` |
| Create  | `ClarityBoard.Infrastructure/Services/Mail/SmtpEmailService.cs` |
| Create  | `ClarityBoard.Application/Features/Admin/Mail/Commands/UpsertMailConfigCommand.cs` |
| Create  | `ClarityBoard.Application/Features/Admin/Mail/Queries/GetMailConfigQuery.cs` |
| Create  | `ClarityBoard.API/Controllers/Admin/MailConfigController.cs` |
| Create  | `ClarityBoard.Application/Features/Auth/Commands/ForgotPasswordCommand.cs` |
| Create  | `ClarityBoard.Application/Features/Auth/Commands/ResetPasswordViaTokenCommand.cs` |
| Modify  | `ClarityBoard.API/Controllers/AuthController.cs` |
| Modify  | `ClarityBoard.Application/Features/Admin/Commands/CreateUserCommand.cs` |
| Modify  | `ClarityBoard.Application/Features/Admin/Commands/ResetPasswordCommand.cs` |
| Modify  | `ClarityBoard.Infrastructure/DependencyInjection.cs` |
