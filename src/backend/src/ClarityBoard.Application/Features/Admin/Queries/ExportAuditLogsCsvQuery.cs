using System.Globalization;
using System.Text;
using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Queries;

[RequirePermission("admin.audit.view")]
public record ExportAuditLogsCsvQuery : IRequest<byte[]>
{
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
}

public class ExportAuditLogsCsvQueryValidator : AbstractValidator<ExportAuditLogsCsvQuery>
{
    public ExportAuditLogsCsvQueryValidator()
    {
        // At least a date range should be specified for CSV exports to avoid huge files
        RuleFor(x => x).Must(x =>
            x.UserId.HasValue || x.DateFrom.HasValue || x.DateTo.HasValue ||
            !string.IsNullOrWhiteSpace(x.Action) || !string.IsNullOrWhiteSpace(x.EntityType))
            .WithMessage("At least one filter must be specified for CSV export.");
    }
}

public class ExportAuditLogsCsvQueryHandler : IRequestHandler<ExportAuditLogsCsvQuery, byte[]>
{
    private readonly IAppDbContext _db;

    public ExportAuditLogsCsvQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> Handle(ExportAuditLogsCsvQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.TableName == request.EntityType);

        if (request.DateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(a => a.CreatedAt <= request.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            query = query.Where(a =>
                (a.OldValues != null && a.OldValues.ToLower().Contains(search)) ||
                (a.NewValues != null && a.NewValues.ToLower().Contains(search)) ||
                (a.RecordId != null && a.RecordId.ToLower().Contains(search)) ||
                a.TableName.ToLower().Contains(search) ||
                a.Action.ToLower().Contains(search));
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .GroupJoin(
                _db.Users,
                a => a.UserId,
                u => u.Id,
                (a, users) => new { AuditLog = a, Users = users })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.AuditLog.Id,
                    x.AuditLog.CreatedAt,
                    x.AuditLog.UserId,
                    UserEmail = user != null ? user.Email : "",
                    x.AuditLog.Action,
                    x.AuditLog.TableName,
                    x.AuditLog.RecordId,
                    x.AuditLog.OldValues,
                    x.AuditLog.NewValues,
                    x.AuditLog.IpAddress,
                    x.AuditLog.UserAgent,
                    x.AuditLog.EntityId,
                })
            .Take(10_000) // Safety limit
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();

        // BOM for Excel compatibility
        sb.Append('\uFEFF');

        // CSV header
        sb.AppendLine("Id,CreatedAt,UserId,UserEmail,Action,TableName,RecordId,OldValues,NewValues,IpAddress,UserAgent,EntityId");

        foreach (var log in logs)
        {
            sb.Append(log.Id);
            sb.Append(',');
            sb.Append(log.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(log.UserId);
            sb.Append(',');
            sb.Append(EscapeCsv(log.UserEmail));
            sb.Append(',');
            sb.Append(EscapeCsv(log.Action));
            sb.Append(',');
            sb.Append(EscapeCsv(log.TableName));
            sb.Append(',');
            sb.Append(EscapeCsv(log.RecordId ?? ""));
            sb.Append(',');
            sb.Append(EscapeCsv(log.OldValues ?? ""));
            sb.Append(',');
            sb.Append(EscapeCsv(log.NewValues ?? ""));
            sb.Append(',');
            sb.Append(EscapeCsv(log.IpAddress ?? ""));
            sb.Append(',');
            sb.Append(EscapeCsv(log.UserAgent ?? ""));
            sb.Append(',');
            sb.Append(log.EntityId);
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
