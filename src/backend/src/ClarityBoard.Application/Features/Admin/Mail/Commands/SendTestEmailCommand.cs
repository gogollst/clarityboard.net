using ClarityBoard.Application.Common.Attributes;
using FluentValidation;
using MediatR;
using System.Net;
using System.Net.Mail;

namespace ClarityBoard.Application.Features.Admin.Mail.Commands;

[RequirePermission("admin.mail.manage")]
public record SendTestEmailCommand : IRequest<SendTestEmailResult>
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string FromEmail { get; init; }
    public required string FromName { get; init; }
    public bool EnableSsl { get; init; } = true;
    public required string RecipientEmail { get; init; }
}

public record SendTestEmailResult(bool Success, string? ErrorMessage);

public class SendTestEmailValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailValidator()
    {
        RuleFor(x => x.Host).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.FromEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.FromName).NotEmpty();
        RuleFor(x => x.RecipientEmail).NotEmpty().EmailAddress();
    }
}

public class SendTestEmailHandler : IRequestHandler<SendTestEmailCommand, SendTestEmailResult>
{
    public async Task<SendTestEmailResult> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SmtpClient(request.Host, request.Port)
            {
                Credentials    = new NetworkCredential(request.Username, request.Password),
                EnableSsl      = request.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout        = 15_000,
            };

            using var message = new MailMessage
            {
                From       = new MailAddress(request.FromEmail, request.FromName),
                Subject    = "ClarityBoard — Test Email",
                Body       = "<p>This is a test email from <strong>ClarityBoard</strong> to verify your SMTP configuration is working correctly.</p>",
                IsBodyHtml = true,
            };
            message.To.Add(request.RecipientEmail);

            await client.SendMailAsync(message, cancellationToken);
            return new SendTestEmailResult(true, null);
        }
        catch (Exception ex)
        {
            return new SendTestEmailResult(false, ex.Message);
        }
    }
}
