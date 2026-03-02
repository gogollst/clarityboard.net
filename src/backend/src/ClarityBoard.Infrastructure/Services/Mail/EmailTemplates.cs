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

    // ── 1. Welcome Email ──────────────────────────────────────────────────────

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

    // ── 2. Password Reset ─────────────────────────────────────────────────────

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

    // ── 3. User Invitation ────────────────────────────────────────────────────

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

    // ── 4. Two-Factor Code ────────────────────────────────────────────────────

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

    // ── 5. System Warning ─────────────────────────────────────────────────────

    public static (string subject, string html) SystemWarning(string warningSubject, string bodyText)
    {
        var content = $"""
            <h2 style="margin:0 0 16px;font-size:22px;font-weight:700;color:#DC2626;">
              &#9888; Systemwarnung
            </h2>
            <p><strong>{warningSubject}</strong></p>
            <pre style="background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:16px;font-size:13px;color:#991b1b;white-space:pre-wrap;word-break:break-all;">{bodyText}</pre>
            <p style="font-size:13px;color:#64748b;">
              Zeitstempel: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            </p>
            """;
        return ($"[ClarityBoard] {warningSubject}", Wrap(warningSubject, content));
    }
}
