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

        // Password is intentionally NOT returned
        return new MailConfigResponse(
            config.Id, config.Host, config.Port, config.Username,
            config.FromEmail, config.FromName, config.EnableSsl,
            config.IsActive, config.UpdatedAt);
    }
}
