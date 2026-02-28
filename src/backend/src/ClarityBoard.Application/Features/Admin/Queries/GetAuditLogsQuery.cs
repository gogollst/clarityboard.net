using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Admin.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Queries;

[RequirePermission("admin.audit.view")]
public record GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
}

public class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAppDbContext _db;

    public GetAuditLogsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsQueryable();

        // Filter by user
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        // Filter by action
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        // Filter by entity type (table name)
        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.TableName == request.EntityType);

        // Filter by date range
        if (request.DateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(a => a.CreatedAt <= request.DateTo.Value);

        // Full-text search on old/new values, record ID, and table name
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

        var totalCount = await query.CountAsync(cancellationToken);

        // Load user emails for the audit log entries
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .GroupJoin(
                _db.Users,
                a => a.UserId,
                u => u.Id,
                (a, users) => new { AuditLog = a, Users = users })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new AuditLogDto
                {
                    Id = x.AuditLog.Id,
                    EntityId = x.AuditLog.EntityId,
                    UserId = x.AuditLog.UserId,
                    UserEmail = user != null ? user.Email : null,
                    Action = x.AuditLog.Action,
                    TableName = x.AuditLog.TableName,
                    RecordId = x.AuditLog.RecordId,
                    OldValues = x.AuditLog.OldValues,
                    NewValues = x.AuditLog.NewValues,
                    IpAddress = x.AuditLog.IpAddress,
                    UserAgent = x.AuditLog.UserAgent,
                    CreatedAt = x.AuditLog.CreatedAt,
                })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
