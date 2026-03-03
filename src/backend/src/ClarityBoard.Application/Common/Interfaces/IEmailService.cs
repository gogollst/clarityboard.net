namespace ClarityBoard.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>Sends a welcome email to a newly created user.</summary>
    Task SendWelcomeEmailAsync(string toEmail, string firstName, string temporaryPassword, CancellationToken ct = default);

    /// <summary>Sends a password-reset link with a time-limited token.</summary>
    Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetToken, CancellationToken ct = default);

    /// <summary>Sends an invitation email with a temporary password.</summary>
    Task SendInvitationEmailAsync(string toEmail, string firstName, string temporaryPassword, string invitedBy, CancellationToken ct = default);

    /// <summary>Sends an invitation link for the user to set their first password.</summary>
    Task SendInvitationLinkEmailAsync(string toEmail, string firstName, string invitationToken, string invitedBy, CancellationToken ct = default);

    /// <summary>Sends a 6-digit 2FA code via email.</summary>
    Task SendTwoFactorCodeEmailAsync(string toEmail, string firstName, string code, CancellationToken ct = default);

    /// <summary>Sends a system warning to admin addresses.</summary>
    Task SendSystemWarningAsync(string subject, string bodyText, IEnumerable<string> adminEmails, CancellationToken ct = default);
}
