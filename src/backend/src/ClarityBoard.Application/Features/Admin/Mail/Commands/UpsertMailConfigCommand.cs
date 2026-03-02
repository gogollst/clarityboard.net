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
    private readonly IAppDbContext      _db;
    private readonly IEncryptionService _encryption;
    private readonly ICacheService      _cache;

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
            existing.Update(
                request.Host, request.Port, request.Username, encryptedPassword,
                request.FromEmail, request.FromName, request.EnableSsl);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Invalidate Redis cache so SmtpEmailService picks up the new config immediately
        await _cache.RemoveAsync("mail:config", cancellationToken);
    }
}
