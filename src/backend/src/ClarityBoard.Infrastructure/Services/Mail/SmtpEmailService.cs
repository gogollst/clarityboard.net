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
/// Loads SMTP config from the database, cached in Redis for 10 minutes.
/// Writes an EmailLog entry for every send attempt.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private const string DefaultAppUrl    = "https://app.clarityboard.net";
    private const string MailConfigCacheKey = "mail:config";
    private static readonly TimeSpan ConfigCacheTtl = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider   _sp;
    private readonly ICacheService      _cache;
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

    // ── Public API ────────────────────────────────────────────────────────────

    public Task SendWelcomeEmailAsync(string toEmail, string firstName, string temporaryPassword, CancellationToken ct)
    {
        var (subject, html) = EmailTemplates.Welcome(firstName, temporaryPassword, DefaultAppUrl);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.WelcomeEmail, userId: null, ct);
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetToken, CancellationToken ct)
    {
        var resetUrl = $"{DefaultAppUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        var (subject, html) = EmailTemplates.PasswordReset(firstName, resetUrl);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.PasswordReset, userId: null, ct);
    }

    public Task SendInvitationEmailAsync(string toEmail, string firstName, string temporaryPassword, string invitedBy, CancellationToken ct)
    {
        var (subject, html) = EmailTemplates.Invitation(firstName, temporaryPassword, invitedBy, DefaultAppUrl);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.UserInvitation, userId: null, ct);
    }

    public Task SendInvitationLinkEmailAsync(string toEmail, string firstName, string invitationToken, string invitedBy, CancellationToken ct)
    {
        var link = $"{DefaultAppUrl}/invite/accept?token={Uri.EscapeDataString(invitationToken)}";
        var (subject, html) = EmailTemplates.InvitationLink(firstName, link, invitedBy);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.UserInvitation, userId: null, ct);
    }

    public Task SendTwoFactorCodeEmailAsync(string toEmail, string firstName, string code, CancellationToken ct)
    {
        var (subject, html) = EmailTemplates.TwoFactorCode(firstName, code);
        return SendWithRetryAsync(toEmail, subject, html, EmailType.TwoFactorCode, userId: null, ct);
    }

    public async Task SendSystemWarningAsync(string subject, string bodyText, IEnumerable<string> adminEmails, CancellationToken ct)
    {
        var (emailSubject, html) = EmailTemplates.SystemWarning(subject, bodyText);
        foreach (var email in adminEmails)
            await SendWithRetryAsync(email, emailSubject, html, EmailType.SystemWarning, userId: null, ct);
    }

    // ── Core Send with Retry ──────────────────────────────────────────────────

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
                // Exponential backoff: attempt 2 → 2s, attempt 3 → 4s
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(
                    "Email send attempt {Attempt}/{Max} to {Email}. Retrying in {Delay}s...",
                    attempt, maxAttempts, toEmail, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }

            try
            {
                var config = await LoadMailConfigAsync(ct)
                    ?? throw new InvalidOperationException(
                        "No active mail configuration found. Configure SMTP via Admin → Mail.");

                var password = _encryption.Decrypt(config.EncryptedPassword);

                using var client = new SmtpClient(config.Host, config.Port)
                {
                    Credentials    = new NetworkCredential(config.Username, password),
                    EnableSsl      = config.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout        = 15_000,
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

                await WriteLogAsync(type, toEmail, subject, EmailStatus.Sent, attempt, error: null, userId, ct);
                _logger.LogInformation(
                    "Email [{Type}] sent to {Email} on attempt {Attempt}.", type, toEmail, attempt);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastEx = ex;
                _logger.LogWarning(ex,
                    "Email [{Type}] to {Email}: attempt {Attempt}/{Max} failed.",
                    type, toEmail, attempt, maxAttempts);
            }
        }

        // All attempts exhausted
        var status = attempt > 2 ? EmailStatus.Retried : EmailStatus.Failed;
        await WriteLogAsync(type, toEmail, subject, EmailStatus.Failed, attempt - 1,
            lastEx?.Message, userId, ct);
        _logger.LogError(lastEx,
            "Email [{Type}] to {Email} failed after {Max} attempts.", type, toEmail, maxAttempts);
        // Do NOT rethrow – email failure must not break the calling command
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<MailConfig?> LoadMailConfigAsync(CancellationToken ct)
    {
        var cached = await _cache.GetAsync<MailConfigCacheEntry>(MailConfigCacheKey, ct);
        if (cached is not null)
        {
            return MailConfig.Create(
                cached.Host, cached.Port, cached.Username,
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
            await _cache.SetAsync(MailConfigCacheKey, new MailConfigCacheEntry(
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
            _logger.LogError(ex, "Failed to persist EmailLog entry.");
        }
    }

    // ── Cache DTO ─────────────────────────────────────────────────────────────

    private sealed record MailConfigCacheEntry(
        string Host, int Port, string Username, string EncryptedPassword,
        string FromEmail, string FromName, bool EnableSsl);
}
